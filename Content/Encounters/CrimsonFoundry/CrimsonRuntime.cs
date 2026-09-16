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

// Sole owner of progression, targeting, phrase admission and terminal commit.
// Native actors transport weapon/player damage; replicas never advance phases.
internal sealed class CrimsonRuntime : IEncounterRuntime
{
    private readonly FightId fight;
    private readonly int summoner;
    private readonly IRaidPedestal pedestal;
    private readonly ulong sequence;
    private readonly Vector2 ground;
    private readonly CrimsonEffigy?[] summons = new CrimsonEffigy?[3];
    private CrimsonBoss? actor;
    private CrimsonMember[] members = Array.Empty<CrimsonMember>();
    private int age, musicStart = -1, finalStart = -1, ending = -1;
    private int phaseStart, unlockAt = -1, target = -1, nextPhrase, phraseSerial;
    private int phraseStart = -1, phraseEnd = -1, targetLife, previousDamage, previousLogAge;
    private CrimsonRhythmKind phraseKind;
    private byte phase, defeated;
    private CrimsonStage stage;
    private bool claimed, cleaned, performerDefeated, cancelled;

    internal CrimsonRuntime(FightId fight, int summoner, IRaidPedestal pedestal, ulong sequence)
    { this.fight = fight; this.summoner = summoner; this.pedestal = pedestal; this.sequence = sequence; ground = pedestal.Ground; }
    private CrimsonState State => new(fight.Value, age, musicStart, finalStart, stage, members,
        (int)ground.X, (int)ground.Y, defeated, phase, phaseStart, unlockAt, (short)target,
        phraseStart, phraseEnd, phraseKind, targetLife, Life(0), Life(1), Life(2), Life(3), performerDefeated);
    private int Life(int index)
    {
        if (index == 3) return performerDefeated ? 0 : Math.Clamp(actor?.NPC.life ?? targetLife, 0, targetLife);
        return (defeated & (1 << index)) != 0 ? 0 : Math.Clamp(summons[index]?.NPC.life ?? targetLife, 0, targetLife);
    }
    internal bool Matches(CrimsonBoss value) => !cleaned && ReferenceEquals(value, actor) && value.State.Fight == fight.Value;
    internal bool Matches(CrimsonEffigy value) => !cleaned && value.State.Fight == fight.Value
        && value.State.Index < 3 && ReferenceEquals(summons[value.State.Index], value);
    internal void Killed(CrimsonBoss value)
    {
        if (!Matches(value) || !State.Vulnerable(age)) return;
        performerDefeated = true; ClearHazards(3); Project(true);
    }
    internal void SummonKilled(CrimsonEffigy value)
    {
        if (!Matches(value) || phase != 3 || !State.SummonVulnerable(value.State.Index)) return;
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
        if (context.FightId != fight || context.EncounterSequence != sequence) return End(EncounterEndReason.Invalidated);
        if (context.Lifecycle == EncounterLifecycle.Validating)
        {
            if (!RefreshRoster() || !pedestal.Claim(sequence, fight)) return End(EncounterEndReason.Invalidated);
            claimed = true; targetLife = CrimsonInvocation.TargetLife(members.Length);
            int slot = NPC.NewNPC(new CrimsonActorSource(this, State), (int)ground.X, (int)ground.Y - 100, ModContent.NPCType<CrimsonBoss>());
            if (slot >= Main.maxNPCs) return End(EncounterEndReason.EncounterActorMissing);
            actor = (CrimsonBoss)Main.npc[slot].ModNPC;
            actor.NPC.life = actor.NPC.lifeMax = targetLife;
            Project(true);
            CrimsonPackets.Log($"event=Preparing fight={fight.Value} members={members.Length} target_life={targetLife} phase=0");
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
                musicStart = age + CrimsonInvocation.MusicLeadTicks;
                unlockAt = musicStart + CrimsonRegistration.Score.IntroTicks;
                nextPhrase = unlockAt - CrimsonRhythm.LookAheadTicks;
                stage = CrimsonStage.Countdown;
                targetLife = CrimsonInvocation.TargetLife(members.Length);
                actor.NPC.life = actor.NPC.lifeMax = targetLife;
                if (!pedestal.Activate(fight)) return End(EncounterEndReason.Invalidated);
                Project(true);
                CrimsonPackets.Log($"event=AllReady fight={fight.Value} music_start={musicStart} unlock={unlockAt}");
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
            bool allOut = Array.TrueForAll(members, m => m.Out);
            bool won = CrimsonPhaseRules.Victory(phase, defeated, performerDefeated, allOut);
            if (ending < 0 && (allOut || won))
            {
                ending = age; stage = allOut ? CrimsonStage.Defeat : CrimsonStage.Victory;
                actor.NPC.dontTakeDamage = true; ClearHazards();
                CrimsonPackets.Log($"event={stage} fight={fight.Value} age={age} remaining={RemainingLife()} mask={defeated}");
                Project(true);
            }
            if (ending >= 0)
            {
                if (age >= ending + 150) return End(stage == CrimsonStage.Victory ? EncounterEndReason.Victory : EncounterEndReason.Defeat);
                Project(age % 10 == 0); return EncounterRuntimeUpdate.None;
            }
            Retarget();
            // Only the first apparition appears during the musical introduction.
            if (summons[0] is null && age - musicStart >= 110) SpawnSummon(0);
            for (int i = 0; i < 3; i++)
                if (summons[i] is { } child && (defeated & (1 << i)) == 0
                    && (!child.NPC.active || child.NPC.ModNPC != child)) return End(EncounterEndReason.EncounterActorMissing);
            if (age >= musicStart + CrimsonRegistration.Score.IntroTicks) stage = CrimsonStage.Performance;
            if (stage == CrimsonStage.Performance && phase < 3 && age >= unlockAt
                && summons[phase] is { } current && CrimsonPhaseRules.ShouldRetreat(phase, phase, current.NPC.life, targetLife))
                AdvancePhase(current);
            // Preannounce a complete bounded phrase. No live packet stream at
            // seven-tick cadence, no delayed catch-up barrage after a transition.
            if (age >= nextPhrase && stage is CrimsonStage.Countdown or CrimsonStage.Performance)
                SchedulePhrase();
        }
        if (age % 300 == 0)
        {
            int damage = targetLife * 4 - RemainingLife();
            CrimsonPackets.Log(FormattableString.Invariant($"event=Progress fight={fight.Value} age={age} phase={phase} stage={stage} remaining={RemainingLife()} total={targetLife * 4} interval_damage={damage - previousDamage} interval_dps={(damage - previousDamage) * 60d / Math.Max(1, age - previousLogAge):F0} mask={defeated}"));
            previousDamage = damage; previousLogAge = age;
        }
        Move(); Project(age % 10 == 0); return EncounterRuntimeUpdate.None;
    }
    private void AdvancePhase(CrimsonEffigy previous)
    {
        previous.NPC.life = CrimsonPhaseRules.RetreatLife(targetLife);
        previous.NPC.dontTakeDamage = true; previous.NPC.netUpdate = true;
        ClearHazards(); phase++; phaseStart = age; unlockAt = age + CrimsonPhaseRules.TransitionTicks;
        phraseStart = phraseEnd = -1; phraseKind = CrimsonRhythmKind.Groove;
        nextPhrase = unlockAt - CrimsonRhythm.LookAheadTicks;
        if (phase < 3) SpawnSummon(phase);
        else finalStart = age; // Kept NPCs retain their actual 20% HP.
        Project(true);
        CrimsonPackets.Log($"event=PhaseChanged fight={fight.Value} phase={phase} epoch={phaseStart} unlock={unlockAt} retained_life={previous.NPC.life}");
    }
    private void SpawnSummon(byte index)
    {
        if (actor is null || summons[index] is not null) return;
        Vector2 position = ground + new Vector2((index - 1) * 170, -140);
        var state = new CrimsonEffigyState(fight.Value, (short)actor.NPC.whoAmI, index, age);
        int slot = NPC.NewNPC(new CrimsonEffigySource(this, state, targetLife), (int)position.X, (int)position.Y, ModContent.NPCType<CrimsonEffigy>());
        if (slot >= Main.maxNPCs) throw new InvalidOperationException("crimson.summon_capacity");
        summons[index] = (CrimsonEffigy)Main.npc[slot].ModNPC; Main.npc[slot].netUpdate = true;
        CrimsonPackets.Log($"event=SummonAppeared fight={fight.Value} index={index} life={targetLife} age={age}");
    }
    private void Retarget()
    {
        if (target >= 0 && Array.Exists(members, m => m.Slot == target && !m.Out)) return;
        target = -1; float best = float.MaxValue;
        foreach (var m in members)
        {
            if (m.Out) continue;
            float distance = Vector2.DistanceSquared(Main.player[m.Slot].Center, actor!.NPC.Center);
            if (distance < best) { best = distance; target = m.Slot; }
        }
        actor!.NPC.target = target < 0 ? 255 : target;
        Project(true);
    }
    private int RemainingLife() => Life(0) + Life(1) + Life(2) + Life(3);
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
    private void Move()
    {
        if (actor is null) return;
        var state = State; var f = state.Field;
        Vector2 focus = target >= 0 ? Main.player[target].Center : new(f.CenterX, f.CenterY);
        bool formation = state.FormationAt(age);
        Vector2 mainGoal = phase == 3 ? focus + new Vector2(170, -200) : new(f.CenterX, f.Top + 130);
        if (musicStart < 0) mainGoal = ground + new Vector2(0, -95);
        if (formation) mainGoal = new(f.CenterX, f.Top + 170);
        MoveTo(actor.NPC, mainGoal, phase == 3 ? 13 : 9);
        for (int i = 0; i < 3; i++)
        {
            if (summons[i] is not { } child || !child.NPC.active || child.NPC.ModNPC != child) continue;
            bool active = phase == 3 ? (defeated & (1 << i)) == 0 : i == phase;
            Vector2 offset = i switch { 0 => new(-230, -180), 1 => new(270, -80), _ => new(-150, -290) };
            Vector2 goal = focus + offset + new Vector2(MathF.Sin(age * .023f + i * 2) * 65, MathF.Sin(age * .019f + i) * 32);
            if (!active) goal = ground + new Vector2((i - 1) * 130, -80);
            else if (formation || age < unlockAt)
                goal = i switch { 0 => new(f.CenterX, f.Top + 200), 1 => new(f.Left + 220, f.CenterY), _ => new(f.Right - 220, f.Top + 250) };
            child.NPC.target = target < 0 ? 255 : target;
            MoveTo(child.NPC, goal, formation ? 22 : 15);
            if (age % 10 == 0) child.NPC.netUpdate = true;
        }
        void MoveTo(NPC npc, Vector2 goal, float speed)
        {
            goal.X = Math.Clamp(goal.X, f.Left + npc.width * .5f + 24, f.Right - npc.width * .5f - 24);
            goal.Y = Math.Clamp(goal.Y, f.Top + npc.height * .5f + 24, f.Bottom - npc.height * .5f - 24);
            Vector2 desired = (goal - npc.Center) * .10f;
            if (desired.LengthSquared() > speed * speed) desired = desired.SafeNormalize(Vector2.UnitY) * speed;
            npc.velocity = Vector2.Lerp(npc.velocity, desired, .13f);
            npc.rotation = npc.velocity.X * .007f;
        }
    }
    private void SchedulePhrase()
    {
        if (actor is null) return;
        var score = CrimsonRegistration.Score;
        var phrase = CrimsonRhythm.Create(score, Math.Max(unlockAt, age + CrimsonRhythm.LookAheadTicks) - musicStart,
            phraseSerial, phase == 3);
        phraseStart = musicStart + phrase.Start; phraseEnd = musicStart + phrase.End; phraseKind = phrase.Kind;
        // One common corridor/orientation across the whole phrase. Successive
        // footprints overlap in safe space even at sixteenth-note cadence.
        var barrages = new CrimsonBarrage[phrase.Hits.Count];
        int required = 0, free = 0;
        for (int i = 0; i < barrages.Length; i++)
        {
            float drift = (i - (barrages.Length - 1) * .5f) * (phase == 3 ? 20 : 28);
            barrages[i] = CrimsonBarrageGeometry.Build(State.Field, phraseSerial, phase, phase == 3, drift);
            if (barrages[i].Lanes.Count > CrimsonRhythm.MaximumLanes) throw new InvalidOperationException("crimson.phrase_capacity");
            required += barrages[i].Lanes.Count;
        }
        foreach (Projectile p in Main.projectile) if (!p.active) free++;
        if (free < required) throw new InvalidOperationException("crimson.phrase_capacity");
        int serial = ++phraseSerial;
        for (int i = 0; i < phrase.Hits.Count; i++)
        {
            int source = phase < 3 ? phase : SelectFinalSource(i + serial);
            if (source < 0) break;
            var hit = phrase.Hits[i];
            foreach (var lane in barrages[i].Lanes)
            {
                var h = new CrimsonHazard(fight.Value, (short)actor.NPC.whoAmI,
                    musicStart + hit.Warning, musicStart + hit.Fire, musicStart + hit.End,
                    CrimsonShape.Slash, lane.X, lane.Y, lane.DX, lane.DY, lane.Length, lane.HalfWidth,
                    phase == 3 ? 510 : 450, (byte)source, phaseStart, serial, hit.Accent);
                int slot = Projectile.NewProjectile(new CrimsonAttackSource(h), new Vector2(lane.X, lane.Y), Vector2.Zero,
                    ModContent.ProjectileType<CrimsonAttack>(), h.Damage, 0, Main.myPlayer);
                if (slot >= Main.maxProjectiles) throw new InvalidOperationException("crimson.phrase_capacity");
                Main.projectile[slot].timeLeft = h.End + 14 - age;
                Main.projectile[slot].netUpdate = true;
            }
        }
        nextPhrase = phraseEnd; Project(true);
        CrimsonPackets.Log($"event=RhythmPhrase fight={fight.Value} phase={phase} epoch={phaseStart} serial={serial} kind={phrase.Kind} warnings={phrase.Hits.Count} start={phraseStart} end={phraseEnd} issued={age}");
    }
    private int SelectFinalSource(int start)
    {
        for (int i = 0; i < 4; i++)
        {
            int source = (start + i) % 4;
            if (CrimsonPhaseRules.ActiveSource(phase, defeated, performerDefeated, source)) return source;
        }
        return -1;
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
        try
        {
            ClearHazards();
            foreach (NPC n in Main.ActiveNPCs)
            {
                bool owned = n.ModNPC is CrimsonBoss b && b.State.Fight == fight.Value
                    || n.ModNPC is CrimsonEffigy e && e.State.Fight == fight.Value;
                if (!owned) continue;
                n.active = false;
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: n.whoAmI);
            }
        }
        finally { if (claimed) { pedestal.Release(fight); claimed = false; } }
        cleaned = true; // A failed cleanup must remain retryable.
        CrimsonPackets.Log($"event=Cleaned fight={fight.Value} reason={context.EndReason} age={age}");
    }
}
