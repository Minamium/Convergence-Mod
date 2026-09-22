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
internal sealed partial class CrimsonRuntime : IEncounterRuntime
{
    private readonly FightId fight;
    private readonly int summoner;
    private readonly IRaidPedestal pedestal;
    private readonly ulong sequence;
    private readonly Vector2 ground;
    private readonly CrimsonEffigy?[] summons = new CrimsonEffigy?[3];
    private readonly CrimsonActCycle cycle = new();
    private bool thresholdLatched;
    private readonly int[] techniqueCursor = new int[4];
    private readonly int[] poseUntil = new int[4];
    private readonly CrimsonPoint[] poseExit = new CrimsonPoint[4];
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
        phraseStart, phraseEnd, phraseKind, targetLife, Life(0), Life(1), Life(2), Life(3), performerDefeated, cycle.Completed);
    private int Life(int index)
    {
        if (index == 3) return performerDefeated ? 0 : Math.Clamp(actor?.NPC.life ?? targetLife, 0, CrimsonPhaseRules.BarMaximum(phase, targetLife));
        return (defeated & (1 << index)) != 0 ? 0 : Math.Clamp(summons[index]?.NPC.life ?? targetLife, 0, targetLife);
    }
    internal bool Matches(CrimsonBoss value) => !cleaned && ReferenceEquals(value, actor) && value.State.Fight == fight.Value;
    internal bool Matches(CrimsonEffigy value) => !cleaned && value.State.Fight == fight.Value
        && value.State.Index < 3 && ReferenceEquals(summons[value.State.Index], value);
    internal void Killed(CrimsonBoss value)
    {
        if (!Matches(value) || cycle.Completed == 0 || !State.Vulnerable(age)) return;
        performerDefeated = true; ClearHazards(3); Project(true);
    }
    internal void SummonKilled(CrimsonEffigy value)
    {
        if (!Matches(value) || cycle.Completed == 0 || phase != 3 || !State.SummonVulnerable(value.State.Index)) return;
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
            var initialCenter = CrimsonChoreography.Conductor(State.Field);
            actor.NPC.Center = new(initialCenter.X, initialCenter.Y);
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
                unlockAt = musicStart + Math.Max(CrimsonRegistration.Score.IntroTicks, CrimsonChoreography.OpeningTicks);
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
                actor.NPC.dontTakeDamage = true; ClearHazards(preserveVerdict: allOut);
                CrimsonPackets.Log($"event={stage} fight={fight.Value} age={age} remaining={RemainingLife()} mask={defeated}");
                Project(true);
            }
            if (ending >= 0)
            {
                if (age >= ending + 150) return End(stage == CrimsonStage.Victory ? EncounterEndReason.Victory : EncounterEndReason.Defeat);
                Project(age % 10 == 0); return EncounterRuntimeUpdate.None;
            }
            Retarget();
            if (summons[0] is null && age - musicStart >= CrimsonChoreography.SummonAt) SpawnSummon(0);
            if (phase == 3 && defeated != CrimsonInvocation.AllDefeated && age - phaseStart >= CrimsonEnsemble.SacrificeComplete
                && !SacrificeSummons()) return End(EncounterEndReason.EncounterActorMissing);
            for (int i = 0; i < 3; i++)
                if (summons[i] is { } child && (defeated & (1 << i)) == 0
                    && (!child.NPC.active || child.NPC.ModNPC != child)) return End(EncounterEndReason.EncounterActorMissing);
            if (stage == CrimsonStage.Countdown && age >= unlockAt) stage = CrimsonStage.Performance;
            TickChorus();
            if (thresholdLatched && phase < 3 && summons[phase] is { } held)
                held.NPC.life = CrimsonPhaseRules.RetreatLife(targetLife);
            if (stage == CrimsonStage.Performance && phase < 3 && age >= unlockAt
                && summons[phase] is { } current && CrimsonPhaseRules.ShouldRetreat(phase, phase, current.NPC.life, targetLife))
            {
                current.NPC.life = CrimsonPhaseRules.RetreatLife(targetLife);
                if (!thresholdLatched)
                {
                    thresholdLatched = true; current.NPC.netUpdate = true;
                    CrimsonPackets.Log($"event=HpGateHeld fight={fight.Value} phase={phase} age={age} issued={cycle.Issued}");
                }
            }
            if (cycle.TryComplete(age, chorus is not null))
            {
                CrimsonPackets.Log($"event=PhaseCycleCompleted fight={fight.Value} phase={phase} age={age} cycles={cycle.Completed}");
                if (phase < 3 && thresholdLatched && summons[phase] is { } retired) AdvancePhase(retired);
                else { nextPhrase = age; Project(true); }
            }
            if (!cycle.Full && age >= nextPhrase && stage is CrimsonStage.Countdown or CrimsonStage.Performance)
                if (!TryScheduleChorus()) SchedulePhrase();
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
        ClearHazards(); Array.Clear(poseUntil); Array.Clear(techniqueCursor);
        cycle.Reset(); thresholdLatched = false;
        phase++; phaseStart = age; unlockAt = age + CrimsonEnsemble.Transition(phase);
        phraseStart = phraseEnd = -1; phraseKind = CrimsonRhythmKind.Groove;
        nextPhrase = unlockAt - CrimsonRhythm.LookAheadTicks;
        if (phase < 3) SpawnSummon(phase);
        else
        {
            finalStart = age;
            // Transfer the three retained remainders exactly once. The avatar
            // is the only Final target; neither a heal nor an additional budget.
            actor!.NPC.life = actor.NPC.lifeMax = CrimsonPhaseRules.BarMaximum(3, targetLife);
        }
        Project(true);
        CrimsonPackets.Log($"event=PhaseChanged fight={fight.Value} phase={phase} epoch={phaseStart} unlock={unlockAt} retained_life={previous.NPC.life}");
    }
    private bool SacrificeSummons()
    {
        if (defeated == CrimsonInvocation.AllDefeated) return true;
        // Validate the complete owned set before mutating any slot. A reused
        // native NPC slot is not a sacrifice and must never be deactivated.
        foreach (var child in summons)
            if (child is null || !child.NPC.active || child.NPC.ModNPC != child || !Matches(child)) return false;
        for (int i = 0; i < summons.Length; i++)
        {
            var child = summons[i]!;
            child.NPC.life = 0; child.NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: child.NPC.whoAmI);
        }
        defeated = CrimsonInvocation.AllDefeated;
        CrimsonPackets.Log($"event=SummonsSacrificed fight={fight.Value} age={age} avatar_life={actor!.NPC.life}");
        Project(true);
        return true;
    }
    private void SpawnSummon(byte index)
    {
        if (actor is null || summons[index] is not null) return;
        var field=State.Field;
        var gate=CrimsonChoreography.SummoningGate(field);
        Vector2 position = new(gate.X, gate.Y);
        var state = new CrimsonEffigyState(fight.Value, (short)actor.NPC.whoAmI, index, age);
        int slot = NPC.NewNPC(new CrimsonEffigySource(this, state, targetLife), (int)position.X, (int)position.Y, ModContent.NPCType<CrimsonEffigy>());
        if (slot >= Main.maxNPCs) throw new InvalidOperationException("crimson.summon_capacity");
        summons[index] = (CrimsonEffigy)Main.npc[slot].ModNPC;
        Main.npc[slot].Center = position; Main.npc[slot].netUpdate = true;
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
    private int RemainingLife() => phase == 3 ? Life(3) : Life(0) + Life(1) + Life(2) + Life(3);
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
        // Vespera is the immovable conductor; only her summoned actors stage/move.
        var fixedCenter = CrimsonChoreography.Conductor(f);
        actor.NPC.Center = new(fixedCenter.X, fixedCenter.Y);
        actor.NPC.velocity = Vector2.Zero;
        actor.NPC.rotation = 0;
        for (int i = 0; i < 3; i++)
        {
            if (summons[i] is not { } child || !child.NPC.active || child.NPC.ModNPC != child) continue;
            bool active = phase == 3 ? (defeated & (1 << i)) == 0 : i == phase;
            Vector2 offset = i switch { 0 => new(-230, -180), 1 => new(270, -80), _ => new(-150, -290) };
            Vector2 goal = focus + offset + new Vector2(MathF.Sin(age * .023f + i * 2) * 65, MathF.Sin(age * .019f + i) * 32);
            if (!active) goal = phase < 3 && i == phase - 1 && age < unlockAt
                ? new(f.CenterX, f.CenterY - 200) : ground + new Vector2((i - 1) * 130, -80);
            else if (phase == 3)
            {
                var binding = CrimsonEnsemble.Binding(f, i);
                var consume = CrimsonPoint.Lerp(binding, CrimsonChoreography.Conductor(f), CrimsonEnsemble.Absorption(age - phaseStart));
                goal = new(consume.X, consume.Y);
            }
            else if (age < unlockAt) goal = new(f.CenterX, f.CenterY - 200);
            child.NPC.target = target < 0 ? 255 : target;
            if (phase == 3 && active && age >= unlockAt)
            { child.NPC.Center = goal; child.NPC.velocity = Vector2.Zero; child.NPC.rotation = 0; }
            else if (!active || age >= poseUntil[i]) MoveTo(child.NPC, goal, age < unlockAt ? 38 : 15);
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
            if (Math.Abs(npc.velocity.X) > .5f) npc.spriteDirection = npc.velocity.X < 0 ? -1 : 1;
        }
    }
    private void SchedulePhrase()
    {
        if (actor is null || cycle.Full) return;
        var score = CrimsonRegistration.Score;
        var rhythm = CrimsonChoreography.Create(score, Math.Max(unlockAt, age + CrimsonRhythm.LookAheadTicks) - musicStart,
            phraseSerial, phase == 3);
        phraseStart = musicStart + rhythm.Start; phraseEnd = musicStart + rhythm.End; phraseKind = rhythm.Kind;
        int serial = ++phraseSerial, count = rhythm.Hits.Count + (phase == 3 ? CrimsonChoreography.BasicNotes : 0), free = 0;
        foreach (Projectile p in Main.projectile) if (!p.active) free++;
        if (free < count) throw new InvalidOperationException("crimson.phrase_capacity");
        var sources = new int[count]; var counts = new int[4]; var steps = new int[4];
        var first = new int[4]; var last = new int[4]; var minimum = new int[4];
        Array.Fill(first, int.MaxValue); Array.Fill(minimum, int.MaxValue);
        for (int i = 0; i < count; i++)
        {
            int source = phase < 3 ? phase : SelectFinalSource(i + serial);
            if (source < 0) throw new InvalidOperationException("crimson.no_phrase_source");
            sources[i] = source; counts[source]++;
            var note = rhythm.Hits[i % rhythm.Hits.Count];
            first[source] = Math.Min(first[source], musicStart + note.Fire);
            var technique = CrimsonEnsemble.Technique(phase, serial, i % rhythm.Hits.Count, i >= rhythm.Hits.Count);
            last[source] = Math.Max(last[source], musicStart + CrimsonEnsemble.NoteEnd(technique, note));
            minimum[source] = Math.Min(minimum[source], note.End - note.Fire - 1);
        }
        var techniques = new CrimsonTechnique[4]; var from = new CrimsonPoint[4];
        var staging = new CrimsonPoint[4]; var targets = new CrimsonPoint[4]; var begins = new int[4];
        var field = State.Field;
        var focus = target < 0 ? new CrimsonPoint(field.CenterX, field.CenterY)
            : new CrimsonPoint(Main.player[target].Center.X, Main.player[target].Center.Y);
        for (int source = 0; source < 4; source++)
        {
            if (counts[source] == 0) continue;
            NPC body = source == 3 ? actor.NPC : summons[source]!.NPC;
            techniques[source] = CrimsonTechnique.TrackingBeam;
            from[source] = age < poseUntil[source] ? poseExit[source] : new(body.Center.X, body.Center.Y);
            // The fixed Final conductor can announce its next phrase while
            // the previous orb's carriers are still travelling across the field.
            begins[source] = phase == 3 ? age : Math.Max(age, poseUntil[source]);
            staging[source] = CrimsonTechniqueGeometry.Stage(field, focus, techniques[source], serial);
            if (phase == 3) staging[source] = source == 3 ? CrimsonChoreography.Conductor(field) : CrimsonEnsemble.Binding(field, source);
            targets[source] = CrimsonTechniqueGeometry.Target(field, focus, techniques[source], serial);
            bool moves = techniques[source] is CrimsonTechnique.CrownCrash or CrimsonTechnique.MantleRush;
            if (moves) targets[source] = CrimsonTechniqueGeometry.LimitTravel(staging[source], targets[source], counts[source], minimum[source]);
            poseUntil[source] = last[source] + CrimsonRhythm.PoseRecoveryTicks;
            poseExit[source] = moves ? targets[source] : staging[source];
        }
        // Validate the entire phrase before allocating any native resources.
        var plans = new CrimsonGesturePlan[count];
        for (int i = 0; i < count; i++)
        {
            int source = sources[i], note = i % rhythm.Hits.Count; var hit = rhythm.Hits[note];
            var eligible = Array.FindAll(members, m => !m.Out && Main.player[m.Slot].active && !Main.player[m.Slot].dead);
            if (eligible.Length == 0) throw new InvalidOperationException("crimson.no_phrase_target");
            var aimed = eligible[CrimsonTrackingBeam.TargetIndex(serial, note, eligible.Length)];
            var playerCenter = Main.player[aimed.Slot].Center;
            var aim = CrimsonTechniqueGeometry.Clamp(field, new(playerCenter.X, playerCenter.Y), 100);
            var technique = CrimsonEnsemble.Technique(phase, serial, note, i >= rhythm.Hits.Count);
            int end = CrimsonEnsemble.NoteEnd(technique, hit);
            plans[i] = new(fight.Value, (short)actor.NPC.whoAmI, phaseStart, serial, (byte)i, (byte)source,
                technique, (byte)steps[source]++, (byte)counts[source], hit.Accent,
                begins[source], musicStart + hit.Warning, musicStart + hit.Fire, musicStart + end,
                first[source], last[source], from[source], staging[source], aim,
                (int)ground.X, (int)ground.Y, CrimsonPlaytestTuning.AttackDamage,
                technique is CrimsonTechnique.SpatialGrid or CrimsonTechnique.ClusterVolley ? (short)-1 : aimed.Slot,
                technique is CrimsonTechnique.SpatialGrid or CrimsonTechnique.ClusterVolley ? Guid.Empty : aimed.Connection);
            plans[i].Validate();
        }
        foreach (var plan in plans)
        {
            int slot = Projectile.NewProjectile(new CrimsonGestureSource(plan), new Vector2(plan.Stage.X, plan.Stage.Y),
                Vector2.Zero, ModContent.ProjectileType<CrimsonGesture>(), plan.Damage, 0, Main.myPlayer);
            if (slot >= Main.maxProjectiles) throw new InvalidOperationException("crimson.phrase_capacity");
            Main.projectile[slot].timeLeft = plan.LastEnd + CrimsonRhythm.LeaseTicks - age; Main.projectile[slot].netUpdate = true;
        }
        // Reservation lead is not an extra musical rest between every bar.
        int recoveryEnd = phraseEnd;
        int lastEmissionEnd = 0;
        foreach (var plan in plans)
        {
            lastEmissionEnd = Math.Max(lastEmissionEnd, plan.LastEnd);
            recoveryEnd = Math.Max(recoveryEnd, plan.LastEnd + CrimsonRhythm.LeaseTicks);
        }
        cycle.Admit(phraseEnd, recoveryEnd); phrasesSinceChorus++;
        nextPhrase = cycle.Full ? cycle.FinishAt : phraseEnd - CrimsonRhythm.LookAheadTicks;
        Project(true);
        CrimsonPackets.Log($"event=PhysicalPhrase fight={fight.Value} phase={phase} epoch={phaseStart} serial={serial} rhythm={rhythm.Kind} notes={count} start={phraseStart} end={phraseEnd} issued={age} score_start={rhythm.Start} first_fire={plans[0].Fire} warning_ticks={plans[0].Fire - plans[0].Born} last_end={lastEmissionEnd} skills={string.Join(",", Array.ConvertAll(plans, p => p.Technique.ToString()))}");
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
    private void ClearHazards(int source = -1, bool preserveVerdict = false)
    {
        ClearChorus(source, preserveVerdict);
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is CrimsonAttack a && a.Hazard.Fight == fight.Value && (source < 0 || a.Hazard.Source == source)
                || p.ModProjectile is CrimsonGesture g && g.Plan.Fight == fight.Value && (source < 0 || g.Plan.Source == source)) p.Kill();
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
        cleaned = true;
        CrimsonPackets.Log($"event=Cleaned fight={fight.Value} reason={context.EndReason} age={age}");
    }
}
