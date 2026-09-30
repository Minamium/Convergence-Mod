#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Shared;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor;

internal sealed record EbonActorSource(EbonRuntime Runtime, EbonState State) : IEntitySource { public string Context => "EbonManor"; }

// One exact-Fight owner, one authority tick, one terminal commit. Noirette and
// the hazards carry projections and native damage only.
internal sealed class EbonRuntime : IEncounterRuntime
{
    private readonly FightId fight;
    private readonly ulong sequence;
    private readonly int summoner;
    private readonly IRaidPedestal pedestal;
    private EbonBoss? boss;
    private EbonMember[] members = Array.Empty<EbonMember>();
    private EbonStage stage;
    private EbonPhase phase;
    private int phaseAt = -1;
    private int age, music = -1, unlock = -1, ending = -1, max;
    private bool claimed, cleaned, cancelled, defeated;
    private readonly EbonStitchDirector stitch = new();
    private EbonRecoveryController? recovery;
    private int previousDamage;
    private bool projectionDirty;

    internal EbonRuntime(FightId id, int sender, IRaidPedestal core, ulong seq)
    { fight = id; summoner = sender; pedestal = core; sequence = seq; max = EbonRules.Life(1); }

    private int Life => defeated ? 0 : Math.Clamp(boss?.NPC.life ?? max, 0, max);
    private EbonState State => new(fight.Value, age, music, unlock, ending, stage, members,
        (int)pedestal.Ground.X, (int)pedestal.Ground.Y, max, Life, phase, phase == EbonPhase.ActOne ? music : phaseAt);

    internal bool Matches(EbonBoss value) => !cleaned && ReferenceEquals(boss, value) && value.State.Fight == fight.Value;
    internal bool RecoveryRequest(int sender, EbonRecoveryRequest request, bool revive)
        => !cleaned && ending < 0 && stage is EbonStage.Countdown or EbonStage.Performance
            && recovery?.Receive(sender, request, revive) == true;
    internal void Defeated()
    {
        if (cleaned || defeated || !State.Live || phase != EbonPhase.Finale) return;
        defeated = true; ClearHazards(); Project(true);
        EbonPackets.Log($"event=BossDefeated fight={fight.Value} age={age}");
    }
    internal bool Request(int sender, Guid connection, bool ready, bool cancel)
    {
        if (cleaned || stage != EbonStage.Ready || sender < 0 || sender >= Main.maxPlayers) return false;
        var player = Main.player[sender];
        if (!player.active || player.dead || player.GetModPlayer<EbonConnection>().Token != connection) return false;
        int index = Array.FindIndex(members, m => m.Slot == sender && m.Connection == connection);
        if (index < 0) return false;
        if (cancel) { if (sender != summoner) return false; cancelled = true; }
        else members[index] = members[index] with { Ready = ready };
        Project(true); return true;
    }

    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (cleaned || Main.netMode == NetmodeID.MultiplayerClient) return EncounterRuntimeUpdate.None;
        if (context.FightId != fight || context.EncounterSequence != sequence) return End(EncounterEndReason.Invalidated);
        if (context.Lifecycle == EncounterLifecycle.Validating)
        {
            if (!Roster() || !pedestal.Claim(sequence, fight)) return End(EncounterEndReason.Invalidated);
            claimed = true; Scale();
            var f = State.Field;
            int slot = NPC.NewNPC(new EbonActorSource(this, State), (int)f.CenterX, (int)(f.Top + (f.Bottom - f.Top) * .3f), ModContent.NPCType<EbonBoss>());
            if (slot >= Main.maxNPCs) return End(EncounterEndReason.EncounterActorMissing);
            boss = (EbonBoss)Main.npc[slot].ModNPC; boss.NPC.life = boss.NPC.lifeMax = max;
            Move(); Project(true);
            EbonPackets.Log($"event=Preparing fight={fight.Value} members={members.Length}");
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Preparing);
        }
        if (boss is null || !boss.NPC.active || boss.NPC.ModNPC != boss || !pedestal.IsOwned(fight)) return End(EncounterEndReason.EncounterActorMissing);
        age++;
        // The safety cap never overrides an already committed Victory/Defeat ending.
        if (cancelled || ending < 0 && age >= 60 * 60 * 15) return End(EncounterEndReason.Cancelled);
        if (context.Lifecycle == EncounterLifecycle.Preparing)
        {
            if (!Roster()) return End(EncounterEndReason.Invalidated);
            if (age >= EbonRules.Deploy) stage = EbonStage.Ready;
            if (stage == EbonStage.Ready && Array.TrueForAll(members, m => m.Ready && !Main.player[m.Slot].dead))
            {
                Scale(); boss.NPC.life = boss.NPC.lifeMax = max;
                music = age + 45; unlock = music + EbonRules.Intro; stage = EbonStage.Countdown;
                if (!pedestal.Activate(fight)) return End(EncounterEndReason.Invalidated);
                recovery = new(fight.Value, members); recovery.Project(members);
                Project(true);
                EbonPackets.Log($"event=AllReady fight={fight.Value} players={members.Length} hp={max} music={music} unlock={unlock}");
                return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Active);
            }
            if (age > 60 * 180) return End(EncounterEndReason.Cancelled);
        }
        else if (context.Lifecycle == EncounterLifecycle.Active)
        {
            for (int i = 0; i < members.Length; i++)
            {
                var p = Main.player[members[i].Slot];
                if (!p.active || p.dead || p.ghost || p.GetModPlayer<EbonConnection>().Token != members[i].Connection)
                    members[i] = members[i] with { Out = true };
            }
            if (ending < 0 && recovery?.Tick(State, members) == true) Project(true);
            bool allOut = Array.TrueForAll(members, m => m.Out) || recovery?.Failed == true;
            if (ending < 0 && (allOut || defeated))
            {
                ending = age; stage = allOut ? EbonStage.Defeat : EbonStage.Victory;
                ClearHazards(); Project(true);
                EbonPackets.Log($"event={stage} fight={fight.Value} age={age} phase={phase} remaining_hp={Life}");
            }
            if (ending >= 0)
            {
                boss.NPC.velocity = Vector2.Zero;
                if (age >= ending + (stage == EbonStage.Victory ? EbonRules.VictoryEnding : EbonRules.Ending))
                    return End(stage == EbonStage.Victory ? EncounterEndReason.Victory : EncounterEndReason.Defeat);
                Project(age % 6 == 0); return Publish();
            }
            if (age >= unlock) stage = EbonStage.Performance;
            int floor = EbonRules.Floor(max, phase);
            if (floor > 0 && boss.NPC.life < floor) boss.NPC.life = floor; // DoT cannot skip an act
            if (stage == EbonStage.Performance && State.Live && floor > 0 && boss.NPC.life <= floor)
            {
                phase++; phaseAt = age; ClearHazards(); Project(true);
                EbonPackets.Log($"event=Act fight={fight.Value} phase={phase} age={age} epoch={State.Epoch} hp={Life}");
            }
            Move();
            if (State.Live) { stitch.Tick(State); Schedule(); }
            if (age % 300 == 0 && stage == EbonStage.Performance)
            {
                int damage = max - Life;
                EbonPackets.Log($"event=Progress fight={fight.Value} age={age} phase={phase} hp={Life} interval_dps={Math.Max(0, damage - previousDamage) / 5}");
                previousDamage = damage;
            }
        }
        Project(age % 6 == 0); return Publish();
    }

    private EncounterRuntimeUpdate Publish()
    { bool dirty = projectionDirty; projectionDirty = false; return dirty ? EncounterRuntimeUpdate.ObservableChange() : EncounterRuntimeUpdate.None; }
    private bool Roster()
    {
        var next = new List<EbonMember>();
        foreach (var p in Main.ActivePlayers)
        {
            if (p.ghost) continue;
            var id = p.GetModPlayer<EbonConnection>().Token;
            var old = Array.Find(members, m => m.Slot == p.whoAmI && m.Connection == id);
            next.Add(new((byte)p.whoAmI, id, old.Ready && !p.dead, false));
        }
        if (next.Count is 0 or > EbonRules.Members) return false;
        bool changed = next.Count != members.Length;
        for (int i = 0; i < next.Count && !changed; i++) changed |= next[i].Slot != members[i].Slot || next[i].Connection != members[i].Connection;
        if (changed) for (int i = 0; i < next.Count; i++) next[i] = next[i] with { Ready = false };
        members = next.ToArray(); if (changed) Project(true); return true;
    }
    private void Scale() => max = EbonRules.Life(members.Length);

    private Vector2 Focus(int salt)
    {
        var alive = Array.FindAll(members, m => !m.Out && !m.Recovery.Downed);
        return alive.Length == 0 ? boss!.NPC.Center : Main.player[alive[Math.Abs(salt) % alive.Length].Slot].Center;
    }
    private void Move()
    {
        var s = State;
        var p = EbonRules.BossPosition(s.Field, stage >= EbonStage.Performance ? s.Epoch : int.MaxValue, age);
        boss!.NPC.Center = new(p.X, p.Y); boss.NPC.velocity = Vector2.Zero;
        boss.NPC.spriteDirection = Focus(age / 120).X < p.X ? -1 : 1;
    }

    private void Schedule()
    {
        var s = State; int beat = EbonRules.BeatAt(s.Epoch, age);
        if (beat < 0) return;
        int serial = 0;
        foreach (var cue in EbonSchedule.At(phase, beat)) Execute(EbonSchedule.Stitch(cue, beat), beat, serial++);
    }
    private void Execute(EbonCue cue, int beat, int serial)
    {
        var s = State; var f = s.Field; int epoch = s.Epoch, cycle = EbonSchedule.Cycle(beat);
        float width = f.Right - f.Left, height = f.Bottom - f.Top;
        switch (cue)
        {
            case EbonCue.Throw:
            {
                var target = Focus(beat + cycle);
                Vector2 origin = beat % 3 == 2
                    ? new(f.Left + 320 + (beat * 11 % 7) * (width - 640) / 6, f.Top + 70)
                    : new(beat % 2 == 0 ? f.Left + 80 : f.Right - 80, f.Top + 170 + (beat * 7 % 5) * (height - 420) / 4);
                var d = (target - origin).SafeNormalize(Vector2.UnitX);
                if (!f.ClipAxis(origin.X, origin.Y, d.X, d.Y, out _, out float last)) return;
                float length = Math.Clamp(last + 160, 200, 5800);
                int fire = EbonRules.Beat(epoch, beat + 2);
                Spawn(EbonAttackKind.Thread, origin, d.ToRotation(), length, EbonRules.PropRadius, EbonRules.ThreadDamage,
                    fire, fire + EbonRules.PropFlightTicks(length) + 1, (byte)((beat * 3 + cycle) % 7));
                break;
            }
            case EbonCue.Chandelier:
            {
                // Drops never land on a pending gather point.
                float x = stitchPending ? f.CenterX + (beat % 2 == 0 ? -1 : 1) * width * .3f : Focus(beat + 1).X;
                x = Math.Clamp(x, f.Left + 180, f.Right - 180);
                float anchor = f.Top + 30, length = f.Bottom - anchor;
                int fire = EbonRules.Beat(epoch, beat + 3);
                var plan = Plan(EbonAttackKind.Chandelier, new(x, anchor), MathHelper.PiOver2, length, EbonRules.ChandelierHalfWidth,
                    EbonRules.ChandelierDamage, fire, 0, (byte)(beat % 2));
                SpawnPlan(plan with { End = EbonGeometry.ImpactTick(plan) + EbonRules.BurstTicks });
                break;
            }
            case EbonCue.LoomRising or EbonCue.LoomFalling:
            {
                float angle = cue == EbonCue.LoomRising ? -.64f : .64f;
                var d = angle.ToRotationVector2(); var n = new Vector2(-d.Y, d.X);
                float offset = (cycle * 131 + (cue == EbonCue.LoomRising ? 0 : 150)) % EbonRules.LoomSpacing;
                int fire = EbonRules.Beat(epoch, beat + 3), lines = 0;
                for (int k = -6; k <= 6; k++)
                {
                    var through = new Vector2(f.CenterX, f.CenterY) + n * (offset + k * EbonRules.LoomSpacing);
                    if (!EbonGeometry.Line(f, through, d, out _, out _)) continue;
                    Spawn(EbonAttackKind.Loom, through, angle, 3000, EbonRules.LoomRadius, EbonRules.LoomDamage, fire, fire + EbonRules.LoomLive);
                    lines++;
                }
                EbonPackets.Log($"event=Loom fight={fight.Value} age={age} rising={cue == EbonCue.LoomRising} lines={lines} fire={fire}");
                break;
            }
            case EbonCue.ShearsAcross or EbonCue.ShearsDown:
            {
                var target = Focus(beat + serial + 2);
                bool across = cue == EbonCue.ShearsAcross;
                var through = across ? new Vector2(f.CenterX, Math.Clamp(target.Y, f.Top + 90, f.Bottom - 90))
                    : new Vector2(Math.Clamp(target.X, f.Left + 90, f.Right - 90), f.CenterY);
                int fire = EbonRules.Beat(epoch, beat + 2);
                Spawn(EbonAttackKind.Shears, through, across ? 0 : MathHelper.PiOver2, 3000, EbonRules.ShearsRadius,
                    EbonRules.ShearsDamage, fire, fire + EbonRules.ShearsLive);
                break;
            }
            case EbonCue.Waltz:
            {
                bool finale = phase == EbonPhase.Finale;
                float spin = (cycle % 2 == 0 ? 1 : -1) * (finale ? .0102f : .0076f);
                var center = EbonGeometry.Center(s, age);
                Spawn(EbonAttackKind.Waltz, center, cycle * .7f % MathHelper.TwoPi, EbonRules.SpokeReach, EbonRules.SpokeRadius,
                    EbonRules.WaltzDamage, EbonRules.Beat(epoch, beat + 4), EbonRules.Beat(epoch, beat + 16), (byte)(finale ? 8 : 6), spin);
                break;
            }
            case EbonCue.Stack or EbonCue.Spread:
                stitch.Call(s, boss!, cue == EbonCue.Stack ? EbonStitchKind.Stack : EbonStitchKind.Spread,
                    new(f.CenterX, f.Bottom - 220));
                break;
        }
    }
    private bool stitchPending
    {
        get
        {
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.ModProjectile is EbonStitch m && m.Plan.Fight == fight.Value && !m.Resolved) return true;
            return false;
        }
    }
    private EbonAttackPlan Plan(EbonAttackKind kind, Vector2 at, float angle, float length, float width, int damage,
        int fire, int end, byte variant = 0, float spin = 0)
        => new(fight.Value, (short)boss!.NPC.whoAmI, kind, age, fire, end, at.X, at.Y, angle, length, width, damage, variant, spin);
    private void Spawn(EbonAttackKind kind, Vector2 at, float angle, float length, float width, int damage,
        int fire, int end, byte variant = 0, float spin = 0)
        => SpawnPlan(Plan(kind, at, angle, length, width, damage, fire, end, variant, spin));
    private void SpawnPlan(EbonAttackPlan plan)
    {
        int slot = Projectile.NewProjectile(new EbonAttackSource(plan), new(plan.X, plan.Y), Vector2.Zero,
            ModContent.ProjectileType<EbonAttack>(), EbonRules.NativeSourceDamage(plan.Damage), 0, Main.myPlayer);
        if (slot >= Main.maxProjectiles) throw new InvalidOperationException("ebon.attack_capacity");
        Main.projectile[slot].netUpdate = true;
    }
    private void Project(bool sync)
    {
        if (boss is null) return;
        boss.State = State;
        EbonRecoveryPlayer.Apply(boss.State);
        if (sync) { projectionDirty = true; boss.NPC.netUpdate = true; }
    }
    private EncounterRuntimeUpdate End(EncounterEndReason reason) => EncounterRuntimeUpdate.End(EbonTermination.End(reason));
    private void ClearHazards()
    {
        stitch.Clear(fight.Value);
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EbonAttack a && a.Plan.Fight == fight.Value) p.Kill();
    }
    public void Cleanup(in EncounterCleanupContext context)
    {
        if (cleaned || context.FightId != fight) return;
        try
        {
            ClearHazards();
            recovery?.Cleanup();
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.ModNPC is not EbonBoss b || b.State.Fight != fight.Value) continue;
                n.active = false; if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: n.whoAmI);
            }
        }
        finally { if (claimed) { pedestal.Release(fight); claimed = false; } cleaned = true; }
        EbonPackets.Log($"event=Cleaned fight={fight.Value} reason={context.EndReason} age={age}");
    }
}
