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

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal sealed record CrimsonActorSource(CrimsonRuntime Runtime, CrimsonState State) : IEntitySource
{ public string Context => "CrimsonFoundry"; }
internal sealed record CrimsonEffigySource(CrimsonRuntime Runtime, CrimsonEffigyState State, int Life) : IEntitySource
{ public string Context => "CrimsonInvocation"; }
internal sealed record CrimsonAttackSource(CrimsonHazard Hazard) : IEntitySource
{ public string Context => "CrimsonFoundryScore"; }

// Authority update/terminal order stays here. The three independently damageable
// summons, their projections and hazards are resources of this exact session.
internal sealed class CrimsonRuntime : IEncounterRuntime
{
    private readonly FightId fight;
    private readonly int summoner;
    private readonly IRaidPedestal pedestal;
    private readonly ulong sequence;
    private readonly Vector2 ground;
    private readonly CrimsonEffigy?[] summons = new CrimsonEffigy?[CrimsonInvocation.SummonCount];
    private bool claimed;
    private CrimsonBoss? actor;
    private CrimsonMember[] members = Array.Empty<CrimsonMember>();
    private int age, musicStart = -1, finalStart = -1, ending = -1, nextVolley, volleyIndex;
    private int targetLife, previousDamage, previousLogAge;
    private byte defeated;
    private CrimsonStage stage;
    private bool cleaned, killed, cancelled;
    internal CrimsonRuntime(FightId fight, int summoner, IRaidPedestal pedestal, ulong sequence)
    { this.fight = fight; this.summoner = summoner; this.pedestal = pedestal; this.sequence = sequence; ground = pedestal.Ground; }
    private CrimsonState State => new(fight.Value, age, musicStart, finalStart, stage, members, (int)ground.X, (int)ground.Y, defeated);
    internal bool Matches(CrimsonBoss value) => !cleaned && ReferenceEquals(value, actor) && value.State.Fight == fight.Value;
    internal bool Matches(CrimsonEffigy value) => !cleaned && value.State.Fight == fight.Value
        && value.State.Index < summons.Length && ReferenceEquals(summons[value.State.Index], value);
    internal void Killed(CrimsonBoss value) { if (Matches(value) && State.Vulnerable(age)) killed = true; }
    internal void SummonKilled(CrimsonEffigy value)
    {
        if (!Matches(value) || !State.SummonVulnerable(value.State.Index)) return;
        defeated = CrimsonInvocation.Defeat(defeated, value.State.Index);
        ClearHazards(value.State.Index);
        CrimsonPackets.Log($"event=SummonDefeated fight={fight.Value} index={value.State.Index} mask={defeated} age={age}");
        Project(true);
    }
    internal bool Request(int sender, Guid connection, bool ready, bool cancel)
    {
        if (cleaned || stage != CrimsonStage.Ready || sender < 0 || sender >= Main.maxPlayers) return false;
        if (!Main.player[sender].active || Main.player[sender].dead || Main.player[sender].GetModPlayer<CrimsonConnection>().Token != connection) return false;
        int index = Array.FindIndex(members, m => m.Slot == sender && m.Connection == connection);
        if (index < 0) return false;
        if (cancel) { if (sender != summoner) return false; cancelled = true; }
        else members[index] = members[index] with { Ready = ready };
        Project(true); return true;
    }
    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (cleaned || Main.netMode == NetmodeID.MultiplayerClient) return EncounterRuntimeUpdate.None;
        if (context.Lifecycle == EncounterLifecycle.Validating)
        {
            if (!RefreshRoster() || !pedestal.Claim(sequence, fight)) return End(EncounterEndReason.Invalidated);
            claimed = true; targetLife = CrimsonInvocation.TargetLife(members.Length);
            int slot = NPC.NewNPC(new CrimsonActorSource(this, State), (int)ground.X, (int)ground.Y - 100, ModContent.NPCType<CrimsonBoss>());
            if (slot >= Main.maxNPCs) return End(EncounterEndReason.EncounterActorMissing);
            actor = (CrimsonBoss)Main.npc[slot].ModNPC;
            actor.NPC.life = actor.NPC.lifeMax = targetLife;
            Project(true);
            CrimsonPackets.Log($"event=Preparing fight={fight.Value} members={members.Length} target_life={targetLife} targets=4");
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Preparing);
        }
        if (actor is null || !actor.NPC.active || actor.NPC.ModNPC != actor) return End(EncounterEndReason.EncounterActorMissing);
        if (!pedestal.IsOwned(fight)) return End(EncounterEndReason.Invalidated);
        age++;
        if (cancelled || age > 60 * 60 * 15) return End(EncounterEndReason.Cancelled);
        if (context.Lifecycle == EncounterLifecycle.Preparing)
        {
            if (!RefreshRoster()) return End(EncounterEndReason.Invalidated);
            if (age >= CrimsonInvocation.DeploymentTicks) stage = CrimsonStage.Ready;
            if (stage == CrimsonStage.Ready && Array.TrueForAll(members, m => m.Ready && !Main.player[m.Slot].dead))
            {
                // Cinematic starts immediately; music lead never brings the HUD back.
                musicStart = age + CrimsonInvocation.MusicLeadTicks; stage = CrimsonStage.Countdown;
                targetLife = CrimsonInvocation.TargetLife(members.Length);
                actor.NPC.life = actor.NPC.lifeMax = targetLife;
                if (!pedestal.Activate(fight)) return End(EncounterEndReason.Invalidated);
                Project(true);
                CrimsonPackets.Log($"event=AllReady fight={fight.Value} music_start={musicStart}");
                return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Active);
            }
            if (age > 60 * 180) return End(EncounterEndReason.Cancelled);
        }
        else if (context.Lifecycle == EncounterLifecycle.Active)
        {
            for (int i = 0; i < members.Length; i++)
            {
                Player p = Main.player[members[i].Slot];
                if (!p.active || p.dead || p.ghost || p.GetModPlayer<CrimsonConnection>().Token != members[i].Connection)
                    members[i] = members[i] with { Out = true };
            }
            if (ending < 0 && (killed || Array.TrueForAll(members, m => m.Out)))
            {
                ending = age; stage = killed ? CrimsonStage.Victory : CrimsonStage.Defeat;
                actor.NPC.dontTakeDamage = true; ClearHazards();
                CrimsonPackets.Log($"event={stage} fight={fight.Value} age={age} remaining={RemainingLife()} total={targetLife * 4} mask={defeated}");
                Project(true);
            }
            if (ending >= 0)
            {
                if (age >= ending + 150) return End(killed ? EncounterEndReason.Victory : EncounterEndReason.Defeat);
                Project(age % 10 == 0); return EncounterRuntimeUpdate.None;
            }
            int songAge = age - musicStart;
            var score = CrimsonRegistration.Score;
            for (byte i = 0; i < summons.Length; i++)
            {
                if ((defeated & 1 << i) != 0) continue;
                if (summons[i] is null && songAge >= SummonBeat(i)) SpawnSummon(i);
                else if (summons[i] is { } child && (!child.NPC.active || child.NPC.ModNPC != child))
                    return End(EncounterEndReason.EncounterActorMissing); // Missing is not defeated.
            }
            if (songAge >= score.IntroTicks) stage = CrimsonStage.Performance;
            if (stage == CrimsonStage.Performance && defeated == CrimsonInvocation.AllDefeated && finalStart < 0)
            {
                finalStart = age; ClearHazards();
                CrimsonPackets.Log($"event=FinalManifest fight={fight.Value} age={age} life={actor.NPC.life}");
                Project(true);
            }
            actor.NPC.dontTakeDamage = !State.Vulnerable(age);
            actor.NPC.boss = State.Vulnerable(age);
            if (stage == CrimsonStage.Performance && (finalStart < 0 || State.Vulnerable(age))) score.Events(songAge, Schedule);
        }
        if (age % 300 == 0)
        {
            int damage = targetLife * 4 - RemainingLife();
            CrimsonPackets.Log(FormattableString.Invariant($"event=Progress fight={fight.Value} age={age} stage={stage} remaining={RemainingLife()} total={targetLife * 4} interval_damage={damage - previousDamage} interval_dps={(damage - previousDamage) * 60d / Math.Max(1, age - previousLogAge):F0} final={finalStart >= 0} mask={defeated} members={members.Length}"));
            previousDamage = damage; previousLogAge = age;
        }
        Move(); Project(age % 10 == 0); return EncounterRuntimeUpdate.None;
    }
    private int SummonBeat(int index)
    {
        int desired = 110 + index * 105;
        foreach (int tick in CrimsonRegistration.Score.BeatTicks) if (tick >= desired) return tick;
        return desired;
    }
    private void SpawnSummon(byte index)
    {
        if (actor is null) return;
        Vector2 position = SummonPosition(index);
        var state = new CrimsonEffigyState(fight.Value, (short)actor.NPC.whoAmI, index, age);
        int slot = NPC.NewNPC(new CrimsonEffigySource(this, state, targetLife), (int)position.X, (int)position.Y, ModContent.NPCType<CrimsonEffigy>());
        if (slot >= Main.maxNPCs) throw new InvalidOperationException("crimson.summon_capacity");
        summons[index] = (CrimsonEffigy)Main.npc[slot].ModNPC; Main.npc[slot].netUpdate = true;
        CrimsonPackets.Log($"event=SummonAppeared fight={fight.Value} index={index} life={targetLife} age={age}");
    }
    private int RemainingLife()
    {
        int remaining = actor is null ? targetLife : Math.Clamp(actor.NPC.life, 0, targetLife);
        for (int i = 0; i < summons.Length; i++) if ((defeated & 1 << i) == 0)
            remaining += summons[i] is { } summon ? Math.Clamp(summon.NPC.life, 0, targetLife) : targetLife;
        return remaining;
    }
    private bool RefreshRoster()
    {
        var next = new List<CrimsonMember>();
        foreach (Player p in Main.ActivePlayers)
        {
            if (p.ghost) continue;
            var token = p.GetModPlayer<CrimsonConnection>().Token;
            var previous = Array.Find(members, m => m.Slot == p.whoAmI && m.Connection == token);
            next.Add(new((byte)p.whoAmI, token, previous.Ready && !p.dead, false));
        }
        if (next.Count is 0 or > CrimsonState.MaxMembers) return false;
        bool changed = next.Count != members.Length;
        for (int i = 0; i < next.Count && !changed; i++) changed |= next[i].Slot != members[i].Slot || next[i].Connection != members[i].Connection;
        if (changed) for (int i = 0; i < next.Count; i++) next[i] = next[i] with { Ready = false };
        members = next.ToArray(); if (changed) Project(true); return true;
    }
    private Vector2 SummonPosition(int i)
    {
        var f = State.Field;
        return new(f.CenterX + (i - 1) * 650 + MathF.Sin(age * .013f + i * 2) * 46,
            f.CenterY + (i == 1 ? -180 : 40) + MathF.Sin(age * .018f + i) * 32);
    }
    private void Move()
    {
        if (actor is null) return;
        var f = State.Field;
        Vector2 preparing = ground + new Vector2(0, -95);
        float depart = musicStart < 0 ? 0 : CrimsonInvocation.Ease((age - (musicStart - 90)) / 150f);
        Vector2 background = new(f.CenterX, f.Top + 170);
        Vector2 front = new(f.CenterX + MathF.Sin(age * .017f) * 260, f.CenterY - 60 + MathF.Sin(age * .025f) * 70);
        Vector2 goal = Vector2.Lerp(Vector2.Lerp(preparing, background, depart), front, CrimsonInvocation.Manifest(age, finalStart));
        actor.NPC.velocity = (goal - actor.NPC.Center) * .075f;
        for (int i = 0; i < summons.Length; i++) if (summons[i] is { } child && child.NPC.active && child.NPC.ModNPC == child)
        { child.NPC.velocity = (SummonPosition(i) - child.NPC.Center) * .05f; if (age % 15 == 0) child.NPC.netUpdate = true; }
    }
    private void Schedule(int id, int fire, float intensity, bool offbeat)
    {
        if (actor is null || age < nextVolley) return;
        bool final = State.Vulnerable(age);
        int source = final ? 3 : CrimsonInvocation.SelectAlive(volleyIndex, defeated);
        if (!final && (source >= 3 || summons[source] is not { } child || !child.NPC.active)) return;
        nextVolley = age + (final ? 72 : intensity > .76f ? 90 : 110); volleyIndex++;
        var barrage = CrimsonBarrageGeometry.Build(State.Field, id, source, final);
        var shape = source == 1 && !final ? CrimsonShape.Bolt : CrimsonShape.Slash;
        foreach (var lane in barrage.Lanes)
        {
            var hazard = new CrimsonHazard(fight.Value, (short)actor.NPC.whoAmI, age, musicStart + fire,
                musicStart + fire + (shape == CrimsonShape.Bolt ? 30 : 42), shape,
                lane.X, lane.Y, lane.DX, lane.DY, lane.Length, lane.HalfWidth, final ? 510 : 450, (byte)source);
            int slot = Projectile.NewProjectile(new CrimsonAttackSource(hazard), new Vector2(lane.X, lane.Y), Vector2.Zero,
                ModContent.ProjectileType<CrimsonAttack>(), hazard.Damage, 0, Main.myPlayer);
            if (slot >= Main.maxProjectiles) throw new InvalidOperationException("crimson.projectile_capacity");
            Main.projectile[slot].netUpdate = true;
        }
        CrimsonPackets.Log(FormattableString.Invariant($"event=ScoreBarrage fight={fight.Value} cue={id} source={source} fire={musicStart + fire} lanes={barrage.Lanes.Count} safe={barrage.SafeOffset:F0} gap={barrage.SafeWidth:F0} energy={intensity:F2} final={final}"));
    }
    private void Project(bool sync) { if (actor is null) return; actor.State = State; if (sync) actor.NPC.netUpdate = true; }
    private EncounterRuntimeUpdate End(EncounterEndReason reason) => EncounterRuntimeUpdate.End(CrimsonTermination.End(reason));
    private void ClearHazards(int source = -1)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is CrimsonAttack a && a.Hazard.Fight == fight.Value && (source < 0 || a.Hazard.Source == source)) p.Kill();
    }
    public void Cleanup(in EncounterCleanupContext context)
    {
        if (cleaned || context.FightId != fight) return;
        cleaned = true;
        try { ClearHazards(); }
        finally
        {
            try
            {
                // Exact-Fight scan covers partially constructed actors too.
                foreach (NPC n in Main.ActiveNPCs)
                {
                    bool owned = n.ModNPC is CrimsonBoss b && b.State.Fight == fight.Value
                        || n.ModNPC is CrimsonEffigy e && e.State.Fight == fight.Value;
                    if (!owned) continue;
                    n.active = false;
                    if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: n.whoAmI);
                }
            }
            finally
            {
                if (claimed) { pedestal.Release(fight); claimed = false; }
                CrimsonPackets.Log($"event=Cleaned fight={fight.Value} reason={context.EndReason} age={age}");
            }
        }
    }
}
