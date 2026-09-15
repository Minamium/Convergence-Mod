#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

// One coordinator-owned fight. The NPC is a damageable actor and read-only network
// projection; it does not own a second AI or a client-side random attack selector.
internal sealed partial class GhostSamuraiRuntime : IEncounterRuntime
{
    private readonly FightId fight;
    private readonly int summoner;
    private readonly SamuraiArenaBounds arena;
    private readonly List<GhostSamuraiAttackProjectile> hazards = new();
    private readonly int[] nextHit = new int[256];
    private GhostSamuraiBoss? actor;
    private bool cleaned, killed;
    private int age, timer, transition;
    private SamuraiTarget lockedTarget = SamuraiTarget.None;
    private readonly SamuraiTargetCandidate[] candidates = new SamuraiTargetCandidate[256];
    private SamuraiPhase phase = SamuraiPhase.Phase1;
    private SamuraiAttack attack, previous;
    private SamuraiBeat beat;
    private float baseAngle;
    private Vector2 contactFrom;
    private int contactDamage;
    private int dashSide;
    private int wispOpportunity, lastWaveEnd;
    private GhostSamuraiAttackProjectile? aimedSlash, firstGridWave;
    private int gridSequenceOffset;

    internal GhostSamuraiRuntime(FightId fight, int summoner, SamuraiArenaBounds arena)
    { this.fight = fight; this.summoner = summoner; this.arena = arena; }
    internal bool Matches(GhostSamuraiBoss npc) => !cleaned && ReferenceEquals(actor, npc) && npc.Fight == fight.Value;
    internal void RecordDeath(GhostSamuraiBoss npc) { if (Matches(npc)) killed = true; }

    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || cleaned) return EncounterRuntimeUpdate.None;
        if (context.Lifecycle == EncounterLifecycle.Validating)
        {
            Player p = Main.player[summoner];
            if (!p.active || p.dead) return End(EncounterEndReason.Defeat);
            int slot = NPC.NewNPC(new GhostSamuraiActorSource(this, fight.Value, summoner, arena), (int)p.Center.X, (int)p.Center.Y - 280,
                ModContent.NPCType<GhostSamuraiBoss>());
            if (slot < 0 || slot >= Main.maxNPCs) return End(EncounterEndReason.EncounterActorMissing);
            actor = (GhostSamuraiBoss)Main.npc[slot].ModNPC;
            RefreshField();
            lockedTarget = new(summoner, p.GetModPlayer<GhostSamuraiContainmentPlayer>().Connection);
            ResolveTarget(actor.NPC);
            actor.NPC.netUpdate = true;
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Preparing);
        }
        if (killed) return End(EncounterEndReason.Victory);
        if (actor is null || !actor.NPC.active || actor.NPC.ModNPC != actor) return End(EncounterEndReason.EncounterActorMissing);
        if (context.Lifecycle == EncounterLifecycle.Preparing)
        {
            GhostSamuraiPackets.Log($"event=CombatStarted seq={context.EncounterSequence} fight={fight.Value} boss_slot={actor.NPC.whoAmI} target_slot={actor.NPC.target} max_life={actor.NPC.lifeMax}");
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Active);
        }
        if (context.Lifecycle != EncounterLifecycle.Active) return EncounterRuntimeUpdate.None;

        NPC npc = actor.NPC;
        age++;
        RefreshField();
        contactDamage = 0; // No body damage survives a tick, interruption or recovery.
        // Native projectile slots can be reused before this authority tick. An
        // active replacement must not keep an old ModProjectile in our budget.
        hazards.RemoveAll(p => !p.Projectile.active || p.Projectile.ModProjectile != p
            || p.Fight != fight.Value || age >= p.DisplayHazard.End);
        Player? target = ResolveTarget(npc);
        // Do not leave a three-second resurrect/re-entry window after a wipe.
        // The coordinator performs terminal -> exact cleanup -> newer Idle.
        if (target is null) return End(EncounterEndReason.Defeat);
        SamuraiPhase next = GhostSamuraiRules.NextPhase(phase, npc.life, npc.lifeMax);
        if (transition == 0 && next != phase)
        {
            phase = next;
            transition = GhostSamuraiRules.TransitionTime;
            attack = SamuraiAttack.Idle;
            timer = 0;
            ClearHazards();
            npc.netUpdate = true;
            GhostSamuraiPackets.Log($"event=PhaseChanged fight={fight.Value} age={age} phase={phase} life={npc.life} max_life={npc.lifeMax}");
        }
        npc.dontTakeDamage = transition > 0;
        if (transition > 0)
        {
            beat = SamuraiBeat.Transition;
            npc.velocity *= .88f;
            if (--transition == 0) FinishAttack();
        }
        else
        {
            UpdateAttack(npc, target);
            foreach (var hazard in hazards) hazard.AdvanceWisp(age, target.Center);
            ApplyDamage(npc);
        }
        Project(npc);
        return EncounterRuntimeUpdate.None;
    }

    private void Project(NPC npc)
    {
        if (actor is null) return;
        actor.Age = age; actor.Phase = phase; actor.Attack = attack; actor.Beat = beat;
        // UpdateAttack has already advanced the next-tick cursor; display the tick
        // that actually produced this age's hazard/pose, rather than one tick ahead.
        actor.AttackTimer = attack == SamuraiAttack.Idle ? timer : Math.Max(0, timer - 1 - (attack == SamuraiAttack.GridSlash ? gridSequenceOffset : 0));
        actor.TransitionRemaining = transition; actor.Combo = combo;
        // Snapshots also re-anchor client-only clocks during long attacks / late join.
        if (age % 15 == 0) npc.netUpdate = true;
    }

    private Player? ResolveTarget(NPC npc)
    {
        if (actor is null) return null;
        int count = 0;
        foreach (Player p in Main.ActivePlayers)
        {
            var membership = p.GetModPlayer<GhostSamuraiContainmentPlayer>();
            candidates[count++] = new(p.whoAmI, membership.Connection, p.active, p.dead, p.ghost,
                membership.BoundTo(actor), Vector2.DistanceSquared(npc.Center, p.Center));
        }
        SamuraiTarget next = SamuraiTargetRules.Select(lockedTarget, candidates.AsSpan(0, count));
        if (next != lockedTarget)
        {
            GhostSamuraiPackets.Log($"event=TargetChanged fight={fight.Value} age={age} old_slot={lockedTarget.Slot} new_slot={next.Slot}");
            if (next.Slot >= 0) nextHit[next.Slot] = 0;
            npc.netUpdate = true;
        }
        lockedTarget = next;
        actor.LockedTarget = next.Slot;
        npc.target = next.Slot >= 0 ? next.Slot : Main.maxPlayers;
        return next.Slot >= 0 ? Main.player[next.Slot] : null;
    }

    private void RefreshField()
    {
        if (actor is null) return;
        foreach (Player p in Main.ActivePlayers)
        {
            var containment = p.GetModPlayer<GhostSamuraiContainmentPlayer>();
            if (!p.dead && !p.ghost && (containment.BoundTo(actor) || arena.Contains(p.Center.X, p.Center.Y)))
                containment.Refresh(actor);
            else containment.Clear(fight.Value);
        }
    }

    private void UpdateAttack(NPC npc, Player target)
    {
        if (attack == SamuraiAttack.Idle)
        {
            beat = SamuraiBeat.Recovery;
            Hover(npc, target.Center + new Vector2(npc.Center.X < target.Center.X ? -300 : 300, -200));
            if (++timer < GhostSamuraiRules.AttackInterval(phase)) return;
            int count = GhostSamuraiRules.AttackCount(phase);
            attack = GhostSamuraiRules.SelectNextAttack(phase, previous, Main.rand.Next(count - (GhostSamuraiRules.AttackAllowed(phase, previous) ? 1 : 0)));
            timer = 0;
            baseAngle = Main.rand.NextFloat(-.35f, .35f);
            dashSide = npc.Center.X < target.Center.X ? -1 : 1;
            npc.netUpdate = true;
        }
        switch (attack)
        {
            case SamuraiAttack.DirectionalSlash: DoDirectionalSlash(npc, target); break;
            case SamuraiAttack.ChargedSlash: DoChargedSlash(npc, target); break;
            case SamuraiAttack.GridSlash: DoGridSlash(npc, target); break;
            case SamuraiAttack.Phase2DashSlash: DoPhase2DashSlash(npc, target); break;
            case SamuraiAttack.Phase3CircleAttack: DoPhase3CircleAttack(npc, target); break;
            case SamuraiAttack.TripleVerticalSlash: DoTripleVerticalSlash(npc, target); break;
            case SamuraiAttack.FrontalCleaveShockwave: DoFrontalCleaveShockwave(npc, target); break;
        }
        if (attack != SamuraiAttack.Idle) timer++;
    }

    private void DoDirectionalSlash(NPC npc, Player target)
    {
        if (timer >= GhostSamuraiRules.DirectionalDuration) { FinishAttack(); return; }
        int pair = Math.Min(timer / GhostSamuraiRules.DirectionalPairInterval, GhostSamuraiRules.DirectionalPairCount - 1);
        Hover(npc, target.Center + new Vector2(pair % 2 == 0 ? -260 : 260, -180));
        beat = GhostSamuraiRules.DirectionalBeat(timer);
        int step = GhostSamuraiRules.DirectionalSpawnStep(timer);
        if (step < 0) return;
        float angle = baseAngle + step / 2 * MathHelper.PiOver4 + (step % 2) * MathHelper.PiOver2 + Main.rand.NextFloat(-.12f, .12f);
        Vector2 direction = angle.ToRotationVector2();
        Vector2 center = target.Center;
        AddSlash(center - direction * GhostSamuraiRules.SlashLength / 2, direction,
            GhostSamuraiRules.SlashLength, GhostSamuraiRules.SlashHalfWidth, GhostSamuraiRules.SlashWarning, GhostSamuraiRules.SlashLive);
        if (phase != SamuraiPhase.Phase1 && step % 2 == 0)
            AddWispBursts(npc.Center, age + GhostSamuraiRules.SlashWarning + GhostSamuraiRules.WispDelay, 3);
    }

    private void DoChargedSlash(NPC npc, Player target)
    {
        if (timer < GhostSamuraiRules.ChargeAimTime)
        {
            beat = SamuraiBeat.Approach;
            Hover(npc, target.Center + new Vector2(dashSide * GhostSamuraiRules.ChargeStandOff, 0), GhostSamuraiRules.DashRetreatSpeed);
            return;
        }
        DoChargeSequence(npc, target, timer - GhostSamuraiRules.ChargeAimTime, false);
    }

    private void DoChargeSequence(NPC npc, Player target, int sequenceTick, bool afterGrid)
    {
        if (sequenceTick >= GhostSamuraiRules.ChargeDuration(phase, afterGrid)
            && age >= lastWaveEnd + GhostSamuraiRules.RecoveryTime) { FinishAttack(); return; }
        npc.velocity *= .86f;
        int pass = GhostSamuraiRules.ChargePass(sequenceTick, phase, afterGrid);
        int local = sequenceTick - GhostSamuraiRules.ChargeStart(pass, afterGrid);
        int warning = GhostSamuraiRules.ChargeWindup(pass, afterGrid);
        beat = local < warning ? SamuraiBeat.Telegraph
            : local < warning + GhostSamuraiRules.ChargeLive ? SamuraiBeat.Strike : SamuraiBeat.Recovery;
        int side = pass % 2 == 0 ? dashSide : -dashSide;
        if (local == 0 && !(afterGrid && pass == 0))
        {
            aimedSlash = AddWave(npc, target, warning, false);
            if (phase != SamuraiPhase.Phase1 && pass == 0 && !afterGrid)
                AddWispBursts(npc.Center, age + warning + GhostSamuraiRules.WispDelay, 5);
            npc.netUpdate = true;
        }
        if (local < warning || afterGrid && pass == 0 && aimedSlash is not null && !aimedSlash.SlashAim.Locked)
            TrackWave(npc, target, side);
        else if (local < warning + GhostSamuraiRules.ChargeLive) npc.velocity = Vector2.Zero;
        else
        {
            // Reposition in the harmless gap so the short later warnings can aim
            // from the opposite side without teleporting the physical body.
            int nextSide = -side;
            if (pass < GhostSamuraiRules.ChargeCount(phase) - 1)
                Hover(npc, target.Center + new Vector2(nextSide * GhostSamuraiRules.ChargeStandOff, 0), GhostSamuraiRules.DashRetreatSpeed);
        }
    }

    private void DoGridSlash(NPC npc, Player target)
    {
        if (timer == 0)
        {
            // Capture an arrival deadline, not a direction. Keep both full
            // warnings; continue aiming and reverse-schedule the wave release
            // using its constant speed and the current target's full hitbox.
            npc.velocity = Vector2.Zero;
            aimedSlash = AddWave(npc, target, GhostSamuraiRules.GridFollowWarning, true);
            firstGridWave = aimedSlash;
            int gridFire = aimedSlash.Hazard.ArrivalTick - GhostSamuraiRules.GridToChargedSlashHitInterval;
            int gridBorn = gridFire - GhostSamuraiRules.GridWarning;
            float halfWidth = GhostSamuraiRules.GridWidth / 2, halfHeight = GhostSamuraiRules.GridHeight / 2;
            // Keep full cells available near world borders. This never changes tiles.
            Vector2 center = new(Math.Clamp(target.Center.X, halfWidth + 160, Main.maxTilesX * 16 - halfWidth - 160),
                Math.Clamp(target.Center.Y, halfHeight + 160, Main.maxTilesY * 16 - halfHeight - 160));
            for (int i = 0; i < GhostSamuraiRules.GridVerticalLineCount; i++)
                Spawn(GhostSamuraiRules.GridLine(true, i, center.X, center.Y, gridBorn));
            for (int i = 0; i < GhostSamuraiRules.GridHorizontalLineCount; i++)
                Spawn(GhostSamuraiRules.GridLine(false, i, center.X, center.Y, gridBorn));
            // Expanding the grid must not move its follow-up wisps out of reach.
            if (phase != SamuraiPhase.Phase1) AddWispBursts(npc.Center,
                gridFire + GhostSamuraiRules.WispDelay, 5);
        }
        if (firstGridWave is not null)
            gridSequenceOffset = firstGridWave.DisplayHazard.Fire - firstGridWave.Hazard.Fire;
        DoChargeSequence(npc, target, timer - gridSequenceOffset, true);
    }

    private GhostSamuraiAttackProjectile AddWave(NPC npc, Player target, int warning, bool afterGrid)
    {
        Rectangle reference = target.Hitbox;
        Vector2 aimPoint = reference.Center.ToVector2();
        Vector2 d = (aimPoint - npc.Center).SafeNormalize(Vector2.UnitX);
        var h = new SamuraiHazard(SamuraiShape.SlashWave, npc.Center.X, npc.Center.Y, d.X, d.Y,
            SamuraiWaveRules.ChargedSlashWaveWidth, SamuraiWaveRules.ChargedSlashWaveHeight / 2,
            age, age + warning, age + warning + SamuraiWaveRules.WaveLife, GhostSamuraiRules.ChargeDamage);
        if (afterGrid)
        {
            int flight = SamuraiWaveRules.FlightTicks(h, reference.Center.X, reference.Center.Y, reference.Width * .5f, reference.Height * .5f);
            int arrival = Math.Max(h.Fire + flight, age + GhostSamuraiRules.GridWarning + GhostSamuraiRules.GridToChargedSlashHitInterval);
            h = h with { ArrivalTick = arrival };
        }
        lastWaveEnd = Math.Max(lastWaveEnd, h.End);
        return Spawn(h, h.Fire - GhostSamuraiRules.AimLockLead);
    }

    private void TrackWave(NPC npc, Player target, int side)
    {
        if (aimedSlash is null || aimedSlash.SlashAim.Locked) { npc.velocity = Vector2.Zero; return; }
        Hover(npc, target.Center + new Vector2(side * GhostSamuraiRules.ChargeStandOff, 0), GhostSamuraiRules.DashRetreatSpeed);
        Rectangle reference = target.Hitbox;
        Vector2 direction = (reference.Center.ToVector2() - npc.Center).SafeNormalize(Vector2.UnitX);
        if (aimedSlash.Hazard.ArrivalTick > 0) aimedSlash.AimArrival(age, npc.Center, direction, reference);
        else aimedSlash.Aim(age, npc.Center, direction);
        // Mutable pre-release schedules may shorten again. Do not retain the
        // largest abandoned estimate as an artificial post-attack idle period.
        lastWaveEnd = 0;
        foreach (var wave in hazards)
            if (wave.Hazard.Shape == SamuraiShape.SlashWave)
                lastWaveEnd = Math.Max(lastWaveEnd, wave.DisplayHazard.End);
        if (aimedSlash.SlashAim.Locked)
        {
            npc.velocity = Vector2.Zero; npc.netUpdate = true;
            if (aimedSlash.Hazard.ArrivalTick > 0)
            {
                gridSequenceOffset = aimedSlash.DisplayHazard.Fire - aimedSlash.Hazard.Fire;
                int flight = SamuraiWaveRules.FlightTicks(aimedSlash.DisplayHazard, reference.Center.X, reference.Center.Y, reference.Width * .5f, reference.Height * .5f);
                GhostSamuraiPackets.Log($"event=GridWaveLocked fight={fight.Value} age={age} grid_hit={aimedSlash.Hazard.ArrivalTick - 60} wave_fire={aimedSlash.DisplayHazard.Fire} reference_arrival={aimedSlash.DisplayHazard.Fire + flight}");
            }
        }
    }

    private void DoPhase2DashSlash(NPC npc, Player target)
    {
        int pass = timer / GhostSamuraiRules.DashCadence, local = timer % GhostSamuraiRules.DashCadence;
        if (pass >= 3) { FinishAttack(); return; }
        int side = pass % 2 == 0 ? dashSide : -dashSide;
        if (local < GhostSamuraiRules.DashApproach)
        {
            beat = SamuraiBeat.Approach;
            Hover(npc, target.Center + new Vector2(side * GhostSamuraiRules.DashStandOff, 0), GhostSamuraiRules.DashRetreatSpeed);
            return;
        }
        int windup = local - GhostSamuraiRules.DashApproach;
        if (windup == 0)
        {
            aimedSlash = AddRush(npc, target, GhostSamuraiRules.DashDistance,
                GhostSamuraiRules.DashHalfWidth, GhostSamuraiRules.DashWarning, GhostSamuraiRules.DashLive);
            npc.netUpdate = true;
        }
        if (windup < GhostSamuraiRules.DashWarning)
        {
            beat = SamuraiBeat.Telegraph;
            TrackRush(npc, target, side, GhostSamuraiRules.DashStandOff);
        }
        else if (windup < GhostSamuraiRules.DashWarning + GhostSamuraiRules.DashLive)
        {
            beat = SamuraiBeat.Strike;
            MoveRush(npc, GhostSamuraiRules.SlashDamage);
        }
        else { beat = SamuraiBeat.Recovery; npc.velocity *= .7f; }
    }

    private GhostSamuraiAttackProjectile AddRush(NPC npc, Player target, float distance, float visualWidth, int warning, int live)
    {
        Vector2 d = RushDirection(npc, target, distance, live);
        return Spawn(new(SamuraiShape.RushVisual, npc.Center.X, npc.Center.Y, d.X, d.Y, distance, visualWidth,
            age, age + warning, age + warning + live, 0), age + warning - GhostSamuraiRules.DashAimLockTime);
    }

    private void TrackRush(NPC npc, Player target, int side, float standOff)
    {
        if (aimedSlash is null) { npc.velocity = Vector2.Zero; return; }
        if (age <= aimedSlash.SlashAim.LockTick)
        {
            Hover(npc, target.Center + new Vector2(side * standOff, 0), GhostSamuraiRules.DashRetreatSpeed);
            aimedSlash.Aim(age, npc.Center, RushDirection(npc, target, aimedSlash.Hazard.Length, aimedSlash.Hazard.End - aimedSlash.Hazard.Fire));
        }
        if (aimedSlash.SlashAim.Locked)
        {
            npc.velocity = Vector2.Zero;
            if (age == aimedSlash.SlashAim.LockTick) npc.netUpdate = true;
        }
    }

    private static Vector2 RushDirection(NPC npc, Player target, float distance, int duration)
    {
        var d = GhostSamuraiRules.RushDirection(npc.Center.X, npc.Center.Y, target.Center.X, target.Center.Y,
            target.velocity.X, target.velocity.Y, distance, duration);
        return new(d.DX, d.DY);
    }

    private void MoveRush(NPC npc, int damage)
    {
        npc.velocity = Vector2.Zero;
        if (aimedSlash is null || !aimedSlash.Projectile.active || !aimedSlash.SlashAim.Locked) return;
        var h = aimedSlash.DisplayHazard;
        if (!GhostSamuraiRules.RushCanContact(h, age, aimedSlash.SlashAim.Locked)) return;
        contactFrom = npc.Center;
        // Runtime runs after native entity movement. Commit the body position now,
        // then sweep only the segment actually travelled in this authority tick.
        npc.Center = GhostSamuraiAttackProjectile.RushCenter(h, age);
        contactDamage = damage;
        if (age == h.Fire || age == h.End - 1 || age % 3 == 0) npc.netUpdate = true;
    }

    private void DoPhase3CircleAttack(NPC npc, Player target)
    {
        npc.velocity *= .85f;
        if (timer >= GhostSamuraiRules.Phase3CircleDuration) { FinishAttack(); return; }
        if (timer == 0)
        {
            // Four bounded immutable entities carry the same captured center and
            // complete schedule, including on late join. No client targeting.
            Vector2 center = target.Center;
            for (int step = 0; step < GhostSamuraiRules.Phase3CircleSteps; step++)
                Spawn(GhostSamuraiRules.CircleStep(step, center.X, center.Y, age));
            npc.netUpdate = true;
        }
        int current = Math.Min(timer / GhostSamuraiRules.Phase3CircleStepInterval, 3);
        var stepHazard = GhostSamuraiRules.CircleStep(current, 0, 0, 0);
        beat = timer < stepHazard.Fire ? SamuraiBeat.Telegraph : stepHazard.Live(timer) ? SamuraiBeat.Strike : SamuraiBeat.Recovery;
    }

    private static void Hover(NPC npc, Vector2 destination, float speed = 18)
    {
        Vector2 delta = destination - npc.Center;
        Vector2 desired = delta.SafeNormalize(Vector2.Zero) * Math.Min(speed, delta.Length() * .08f);
        npc.velocity = Vector2.Lerp(npc.velocity, desired, .16f);
    }

    private GhostSamuraiAttackProjectile AddSlash(Vector2 start, Vector2 direction, float length, float halfWidth, int warning, int live,
        int? lockTick = null, int? damage = null)
        => Spawn(new(SamuraiShape.Slash, start.X, start.Y, direction.X, direction.Y, length, halfWidth,
            age, age + warning, age + warning + live, damage ?? GhostSamuraiRules.Damage(attack)), lockTick);

    private void AddWispBursts(Vector2 origin, int born, int count)
    {
        int bursts = GhostSamuraiRules.WispBursts(wispOpportunity++);
        for (int burst = 0; burst < bursts; burst++)
            AddWisps(origin, born + burst * GhostSamuraiRules.WispBurstInterval, count);
    }

    private void AddWisps(Vector2 origin, int born, int count)
    {
        int existing = 0;
        foreach (var p in hazards)
            if (p.Projectile.active && p.Hazard.Shape == SamuraiShape.Wisp) existing++;
        count = Math.Min(count, GhostSamuraiRules.MaximumWisps - existing);
        float rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        for (int i = 0; i < count; i++)
        {
            Vector2 d = (rotation + i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.18f, .18f)).ToRotationVector2();
            Spawn(new(SamuraiShape.Wisp, origin.X, origin.Y, d.X, d.Y, 0, GhostSamuraiRules.WispRadius,
                born, born + GhostSamuraiRules.SpreadDuration, born + GhostSamuraiRules.SpreadDuration + GhostSamuraiRules.WispLife,
                GhostSamuraiRules.WispDamage));
        }
    }

    private GhostSamuraiAttackProjectile Spawn(SamuraiHazard hazard, int? lockTick = null)
    {
        if (actor is null || !hazard.IsValid || hazards.Count >= GhostSamuraiRules.MaximumHazards)
            throw new InvalidOperationException("ghost_samurai.hazard_capacity_or_shape");
        int lockAt = lockTick ?? hazard.Born;
        if (hazard.HasAim && !SamuraiSlashAim.Spawn(hazard, lockAt).IsValid(hazard))
            throw new InvalidOperationException("ghost_samurai.aim_invalid");
        int slot = Projectile.NewProjectile(new GhostSamuraiAttackSource(fight.Value, actor.NPC.whoAmI, hazard, lockAt), new Vector2(hazard.X, hazard.Y), Vector2.Zero,
            ModContent.ProjectileType<GhostSamuraiAttackProjectile>(), hazard.Shape == SamuraiShape.SlashWave ? hazard.Damage : 0, 0, Main.myPlayer);
        if (slot < 0 || slot >= Main.maxProjectiles) throw new InvalidOperationException("ghost_samurai.projectile_capacity");
        var p = (GhostSamuraiAttackProjectile)Main.projectile[slot].ModProjectile;
        hazards.Add(p); // Register before any further mutation/synchronization.
        p.Projectile.timeLeft = hazard.End - age + 30;
        p.Projectile.netUpdate = true;
        return p;
    }

    private void ApplyDamage(NPC npc)
    {
        foreach (Player p in Main.ActivePlayers)
        {
            if (actor is null || !p.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(actor)) continue;
            if (p.dead || p.ghost || p.immune || p.creativeGodMode || age < nextHit[p.whoAmI]) continue;
            int damage = contactDamage > 0 && GhostSamuraiRules.BodyContact(contactFrom.X, contactFrom.Y, npc.Center.X, npc.Center.Y,
                p.Center.X, p.Center.Y, p.width * .5f, p.height * .5f) ? contactDamage : 0;
            foreach (var hazard in hazards)
            {
                if (damage > 0) break;
                // Waves exclusively use native Projectile.Damage / player dodge
                // hooks. Never apply the authority's manual Hurt path a second time.
                if (hazard.Hazard.Shape == SamuraiShape.SlashWave || !hazard.Projectile.active || !hazard.Hits(age, p)) continue;
                damage = hazard.Hazard.Damage;
            }
            if (damage == 0) continue;
            // One shared native hurt/cooldown path for body, grid, circles and wisps.
            double dealt = p.Hurt(PlayerDeathReason.ByNPC(npc.whoAmI), damage,
                p.Center.X >= npc.Center.X ? 1 : -1, out Player.HurtInfo info, quiet: true);
            nextHit[p.whoAmI] = age + GhostSamuraiRules.HitCooldown;
            if (dealt > 0 && Main.netMode == NetmodeID.Server) NetMessage.SendPlayerHurt(p.whoAmI, info);
        }
    }

    private void FinishAttack()
    {
        if (attack != SamuraiAttack.Idle) previous = attack;
        attack = SamuraiAttack.Idle; timer = 0; beat = SamuraiBeat.Recovery;
        aimedSlash = null;
        contactDamage = 0;
        lastWaveEnd = 0;
        firstGridWave = null;
        gridSequenceOffset = 0;
        ResetCombo();
        if (actor is not null) actor.NPC.netUpdate = true;
    }
    private EncounterRuntimeUpdate End(EncounterEndReason reason)
    {
        contactDamage = 0;
        if (actor is not null) { actor.NPC.velocity = Vector2.Zero; actor.NPC.dontTakeDamage = true; }
        return EncounterRuntimeUpdate.End(GhostSamuraiTermination.End(reason));
    }

    private void ClearHazards()
    {
        // Exact Fight scan catches partially-created resources without touching peers.
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is GhostSamuraiAttackProjectile owned && owned.Fight == fight.Value) p.Kill();
        hazards.Clear();
        aimedSlash = null;
        contactDamage = 0;
        lastWaveEnd = 0;
        firstGridWave = null;
        gridSequenceOffset = 0;
        ResetCombo();
    }
    public void Cleanup(in EncounterCleanupContext context)
    {
        if (cleaned || context.FightId != fight) return;
        ClearHazards();
        GhostSamuraiContainmentPlayer.ClearAll(fight.Value);
        if (actor is not null && actor.NPC.active && actor.NPC.ModNPC == actor && actor.Fight == fight.Value)
        {
            actor.NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: actor.NPC.whoAmI);
        }
        if (actor is not null) actor.Runtime = null;
        cleaned = true;
        GhostSamuraiPackets.Log($"event=CombatEnded seq={context.EncounterSequence} fight={fight.Value} age={age} reason={context.EndReason} phase={phase} life={actor?.NPC.life ?? 0} max_life={actor?.NPC.lifeMax ?? 0} cleanup=complete");
    }
}
