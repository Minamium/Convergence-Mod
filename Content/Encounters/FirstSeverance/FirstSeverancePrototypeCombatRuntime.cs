#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance.Actors;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeverancePrototypeCombatRuntime
{

    private const float StackRadiusPixels = FirstSeveranceLanceTuning.StackRadius;
    private const float SpreadSeparationPixels = FirstSeveranceLanceTuning.SpreadSeparation;

    private readonly ulong encounterSequence;
    private readonly FightId fightId;
    private readonly FirstSeveranceRoster roster;
    private readonly FirstSeverancePartyScaling partyScaling;
    private readonly int serverTileEntityId;
    private readonly TilePoint coreTopLeft;
    private readonly Vector2 groundCenter;
    private readonly FirstSeveranceArenaLayout fieldLayout;
    private readonly FirstSeveranceEncounterPlan plan;

    private readonly FirstSeveranceRecoveryController recovery;
    private readonly FirstSeveranceActorSet actors;
    private readonly FirstSeveranceAttackController attacks;
    private readonly FirstSeveranceCombatTelemetry telemetry;
    private FirstSeveranceLoopStateMachine? loop;

    private FirstSeveranceMechanicResult lastMechanicResult;
    private uint mechanicRevision;
    private bool isAttached;
    private bool isStarted;
    private bool isCleaned;

    internal FirstSeverancePrototypeCombatRuntime(
        ulong encounterSequence,
        FightId fightId,
        FirstSeveranceRoster roster,
        int serverTileEntityId,
        TilePoint coreTopLeft,
        FirstSeveranceArenaLayout arena,
        FirstSeveranceDebugAssistLease? debugAssist = null)
    {
        if (encounterSequence == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(encounterSequence));
        }

        if (fightId.IsNone)
        {
            throw new ArgumentException("Combat requires a Fight ID.", nameof(fightId));
        }

        this.encounterSequence = encounterSequence;
        this.fightId = fightId;
        this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
        partyScaling = FirstSeverancePartyScaling.ForCount(roster.Count);
        this.serverTileEntityId = serverTileEntityId;
        this.coreTopLeft = coreTopLeft;
        groundCenter = new(arena.Core.LogicalCenter.X * 16f, arena.Core.BaseY * 16f);
        fieldLayout = arena;
        plan = FirstSeveranceEncounterPlan.Instance;
        recovery = new(fightId, roster, groundCenter, debugAssist, Log,
            () => isStarted && !isCleaned && isAttached && loop?.State.IsTerminal == false);
        actors = new(fightId, partyScaling, groundCenter, plan);
        attacks = new(roster, groundCenter, plan, recovery, Log);
        telemetry = new(actors, partyScaling, roster.Count, plan, Log);
    }

    internal bool Matches(ulong candidateSequence, FightId candidateFightId)
    {
        return isStarted
            && !isCleaned
            && encounterSequence == candidateSequence
            && fightId == candidateFightId;
    }

    internal bool TryStart(ulong authorityTick, out string failureCode)
    {
        if (isStarted || isCleaned)
        {
            failureCode = "first_severance.combat_already_started";
            return false;
        }

        if (!FoundationCoreProtectionSystem.TryResolveOwnedCore(
                serverTileEntityId,
                fightId,
                out var ownedCore))
        {
            failureCode = "first_severance.combat_core_unavailable";
            return false;
        }

        if (!FirstSeveranceCoreResolver.RevalidateForCombat(ownedCore, fieldLayout, roster.Count, out failureCode))
        {
            Log(authorityTick, "event=ContainmentActivationRejected failure=" + failureCode);
            return false;
        }
        if (!FoundationCoreProtectionSystem.TryEnterActive(serverTileEntityId, fightId))
        {
            failureCode = "first_severance.combat_core_unavailable";
            return false;
        }

        if (!recovery.TryInitialize(out failureCode))
        {
            return false;
        }

        actors.BeginFight();
        if (!actors.TrySpawnBoss(out _))
        {
            failureCode = "first_severance.combat_boss_spawn_failed";
            return false;
        }

        loop = new FirstSeveranceLoopStateMachine(
            plan,
            roster.Count,
            partyScaling.BossLife,
            authorityTick, FirstSeveranceBossPhasePlan.Instance);
        if (!FirstSeveranceCombatAuthority.TryAttach(this))
        {
            failureCode = "first_severance.combat_authority_busy";
            return false;
        }

        isAttached = true;
        isStarted = true;
        recovery.GrantPrototypeReviveKits();
        recovery.SynchronizeRevivePlayers(authorityTick);
        telemetry.Start(loop.State, authorityTick);
        Log(authorityTick, $"event=CombatStarted participants={roster.Count} solo_debug={roster.Count == 1} solo_start_enabled={FirstSeveranceDevelopmentPolicy.AllowSoloDebugStart} hp_policy=FrozenRosterDevelopment stack_policy=MissingRosterFraction boss_max_life={partyScaling.BossLife} pylon_max_life={partyScaling.PylonLife} pylon_count={plan.PylonCount.GetValue(roster.Count)} pylon_open_ticks={plan.Timing.PylonActiveTicks} core_open_ticks={plan.Timing.NormalExposureTicks} core_penalized_ticks={plan.Timing.PenalizedExposureTicks} dps_basis=AuthorityNetHpLoss");
        failureCode = string.Empty;
        return true;
    }

    internal void RecordActorDeath(NPC npc)
    {
        if (isStarted && !isCleaned && loop is not null) actors.RecordActorDeath(npc, loop.State);
    }

    internal bool ProtectBossPhaseBoundary(NPC npc)
        => isStarted && !isCleaned && loop is not null
            && actors.ProtectBossPhaseBoundary(npc, loop.State, loop.DamageFloor);

    internal bool TryQueueCancel(int sender, ulong epoch, uint nonce, out string failure)
        => recovery.TryQueueCancel(sender, epoch, nonce, out failure);
    internal bool TryQueuePrototypeDown(int sender, ulong epoch, uint nonce, out string failure)
        => recovery.TryQueuePrototypeDown(sender, epoch, nonce, out failure);
    internal bool TryQueueReviveNearest(int sender, ulong epoch, uint nonce, out string failure)
        => recovery.TryQueueReviveNearest(sender, epoch, nonce, out failure);

    internal bool CanHitActor(NPC npc, int playerSlot)
    {
        if (!isStarted || isCleaned || !actors.Owns(npc)
            || !FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(playerSlot, out ulong epoch)
            || !roster.TryResolveCurrentBinding(playerSlot, epoch, out var member))
            return false;
        return recovery.CanHitActor(member);
    }

    internal EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (!Matches(context.EncounterSequence, context.FightId)
            || context.Lifecycle != EncounterLifecycle.Active
            || loop is null)
        {
            return End(FirstSeveranceTerminalCause.RuntimeInvariantBroken);
        }

        if (!FoundationCoreProtectionSystem.TryResolveOwnedCore(
                serverTileEntityId,
                fightId,
                out _))
        {
            return End(FirstSeveranceTerminalCause.FoundationCoreLost);
        }

        recovery.ApplyConnectionChanges(context.AuthorityTick);
        foreach (FirstSeveranceRosterMember member in roster.Members)
        {
            // The experimental adapter does not reinterpret ordinary Terraria death
            // or assign a rejoined slot the original participant's identity.
            if (!recovery.TryGetPlayer(member, out Player player) || player.dead)
                return End(FirstSeveranceTerminalCause.AdministrativeAbort);
        }
        recovery.ApplyPendingIntents(context.AuthorityTick);
        if (recovery.CancelRequested)
            return End(FirstSeveranceTerminalCause.UserCancelled);

        FirstSeveranceLoopState before = loop.State;
        ResolveMechanicIfDue(before, context.AuthorityTick);

        if (before.Substate == FirstSeveranceSubstate.PylonCheck)
            telemetry.ObservePylonProgress(context.AuthorityTick,
                context.AuthorityTick >= before.SubstateEnteredTick + (ulong)plan.Timing.PylonTelegraphTicks);
        if (!actors.TryObservePylons(before, context.AuthorityTick, out int destroyedPylons))
        {
            return End(FirstSeveranceTerminalCause.RuntimeInvariantBroken);
        }

        if (!actors.TryObserveBoss(before, out int acceptedBossDamage))
        {
            return End(FirstSeveranceTerminalCause.BossActorMissing);
        }

        bool lanceChanged = attacks.UpdateLances(before, context.AuthorityTick,
            before.Substate == FirstSeveranceSubstate.PylonCheck && destroyedPylons >= before.RemainingPylons);
        lanceChanged |= attacks.UpdateGrid(before, context.AuthorityTick, false);
        attacks.UpdateScoreHazards(before, context.AuthorityTick);

        FirstSeveranceLoopUpdate loopUpdate = loop.Advance(
            new FirstSeveranceLoopInput(
                context.AuthorityTick,
                destroyedPylons,
                acceptedBossDamage));
        if (loopUpdate.Disposition == FirstSeveranceLoopUpdateDisposition.Rejected)
        {
            return End(FirstSeveranceTerminalCause.RuntimeInvariantBroken);
        }

        FirstSeveranceLoopState after = loop.State;
        if (before.BossLife > loop.DamageFloor && after.BossLife == loop.DamageFloor)
            Log(context.AuthorityTick, $"event=HpGateReached boss_phase={after.BossPhase} life={after.BossLife} completed_cycles={after.CompletedPhaseCycles} action={after.ActionIndex}");
        telemetry.PublishDamageProgress(after, context.AuthorityTick);
        if (after.Overload > before.Overload)
        {
            foreach (FirstSeveranceRosterMember member in roster.Members)
            {
                if (recovery.TryGetPlayer(member, out Player player))
                    recovery.ApplyRaidDamage(member, Math.Min(player.statLife - 1,
                        player.statLifeMax2 * 35 / 100), context.AuthorityTick, "PylonPulse");
            }
        }

        // Instant starts are adjudicated after all same-tick damage/invalidations.
        recovery.ApplyPendingRevives(context.AuthorityTick);
        EncounterRuntimeUpdate reviveUpdate = recovery.Commit(context);
        recovery.SynchronizeRevivePlayers(context.AuthorityTick);
        if (after.IsTerminal)
        {
            if (!reviveUpdate.RequestedTermination.IsNone)
            {
                FirstSeveranceTerminalCause selected =
                    FirstSeveranceTerminationContract.Instance.SelectHigherPriority(
                        FirstSeveranceTerminationContract.Instance.GetCause(after.Termination),
                        FirstSeveranceTerminationContract.Instance.GetCause(reviveUpdate.RequestedTermination));
                return End(selected);
            }
            return EncounterRuntimeUpdate.End(after.Termination);
        }
        if (!reviveUpdate.RequestedTermination.IsNone)
            return reviveUpdate;

        bool windowChanged = before.SubstateEnteredTick != after.SubstateEnteredTick;
        if (windowChanged
            && !ApplySubstateTransition(before.Substate, after.Substate, context.AuthorityTick))
        {
            return End(FirstSeveranceTerminalCause.RuntimeInvariantBroken);
        }

        if (windowChanged)
        {
            Log(context.AuthorityTick, $"event=PhaseChanged phase={after.Substate} boss_phase={after.BossPhase} phase_start_tick={after.BossPhaseStartedTick} action={after.ActionIndex} completed_cycles={after.CompletedPhaseCycles} resolve_tick={after.ResolveTick} loop={after.ZeroBasedLoopIndex + 1} overload={after.Overload} boss_life={after.BossLife}");
            telemetry.BeginDamageProgress(after, loop.DamageFloor);
        }

        actors.UpdateDamageWindows(after, context.AuthorityTick, loop.DamageFloor);
        actors.SynchronizeBossLife(after);

        bool combatObservable = windowChanged
            || before.RemainingPylons != after.RemainingPylons
            || before.ZeroBasedLoopIndex != after.ZeroBasedLoopIndex
            || context.AuthorityTick % 30 == 0;
        return reviveUpdate.HasObservableChange || combatObservable || lanceChanged
            ? EncounterRuntimeUpdate.ObservableChange()
            : EncounterRuntimeUpdate.None;
    }

    internal bool TryCreateProjection(out FirstSeveranceCombatProjection? projection)
    {
        if (!isStarted || isCleaned || loop is null || !recovery.IsInitialized)
        {
            projection = null;
            return false;
        }

        RaidReviveSnapshot reviveSnapshot = recovery.CreateSnapshot()!.Value;
        var participants = recovery.CreateParticipants(reviveSnapshot);

        FirstSeveranceLoopState state = loop.State;
        projection = new FirstSeveranceCombatProjection(
            encounterSequence,
            fightId,
            state.Substate,
            state.ResolveTick,
            state.ZeroBasedLoopIndex,
            state.RemainingPylons,
            state.BossLife,
            state.BossMaximumLife,
            reviveSnapshot.RemainingTokenCount,
            reviveSnapshot.Revision,
            StackWorldCenter.X,
            StackWorldCenter.Y,
            CoreWorldCenter.X,
            CoreWorldCenter.Y,
            lastMechanicResult,
            mechanicRevision,
            Array.AsReadOnly(participants),
            attacks.Lance, state.BossPhase, state.BossPhaseStartedTick, attacks.Grid,
            state.SubstateEnteredTick, state.ActionIndex, state.CompletedPhaseCycles);
        return true;
    }

    internal void Cleanup(in EncounterCleanupContext context)
    {
        if (isCleaned)
        {
            return;
        }

        if (context.EncounterSequence != encounterSequence || context.FightId != fightId)
        {
            throw new InvalidOperationException("Combat cleanup has the wrong identity.");
        }

        string terminalCause = FirstSeveranceTerminationContract.Instance.IsValid(context.Termination)
            ? FirstSeveranceTerminationContract.Instance.GetCause(context.Termination).ToString() : "Unknown";
        Log(Main.GameUpdateCount, $"event=CombatEnded reason={context.EndReason} cause={terminalCause} phase={loop?.State.Substate} overload={loop?.State.Overload}");
        telemetry.FinishDamageProgress(Main.GameUpdateCount, terminalCause);

        actors.Cleanup(context);
        recovery.Cleanup(context);
        attacks.Cleanup();
        if (isAttached)
        {
            FirstSeveranceCombatAuthority.Detach(this);
            isAttached = false;
        }

        isCleaned = true;
    }

    private void ResolveMechanicIfDue(
        in FirstSeveranceLoopState state,
        ulong authorityTick)
    {
        if (authorityTick < state.ResolveTick)
        {
            return;
        }

        if (state.Substate == FirstSeveranceSubstate.Stack)
        {
            int required = roster.Count;
            int stacked = 0;
            Vector2 center = StackWorldCenter;
            float radiusSquared = StackRadiusPixels * StackRadiusPixels;
            for (int index = 0; index < roster.Count; index++)
            {
                FirstSeveranceRosterMember member = roster.Members[index];
                if (recovery.IsAlive(member.ParticipantId)
                    && recovery.TryGetPlayer(member, out Player player)
                    && Vector2.DistanceSquared(player.Center, center) <= radiusSquared)
                {
                    stacked++;
                }
            }

            SetMechanicResult(stacked >= required
                ? FirstSeveranceMechanicResult.StackPassed
                : FirstSeveranceMechanicResult.StackFailed);
            Log(authorityTick, FormattableString.Invariant($"event=StackResolved anchor=ClockSite action={state.ActionIndex} x={center.X:F1} y={center.Y:F1} present={stacked} required={required} missing={required - stacked} radius={StackRadiusPixels}"));
            foreach (var member in roster.Members)
                if (recovery.TryGetPlayer(member, out Player measured))
                    Log(authorityTick, FormattableString.Invariant($"event=StackAttendance participant={member.ParticipantId.Value} slot={member.ServerWhoAmI} alive={recovery.IsAlive(member.ParticipantId)} x={measured.Center.X:F1} y={measured.Center.Y:F1} vx={measured.velocity.X:F2} vy={measured.velocity.Y:F2} left={measured.controlLeft} right={measured.controlRight} jump={measured.controlJump} distance={Vector2.Distance(measured.Center, center):F1} inside={Vector2.DistanceSquared(measured.Center, center) <= radiusSquared}"));
            // Failure is the missing fraction of each survivor's own maximum HP.
            // This includes missed soakers so abandoning the group is not immunity.
            foreach (FirstSeveranceRosterMember member in roster.Members)
                if (recovery.TryGetPlayer(member, out Player player))
                    recovery.ApplyRaidDamage(member, FirstSeveranceCombatRules.StackDamage(
                        player.statLifeMax2, required, stacked), authorityTick, "Stack");
        }
        else if (state.Substate == FirstSeveranceSubstate.Spread)
        {
            var alive = new List<Player>(roster.Count);
            for (int index = 0; index < roster.Count; index++)
            {
                FirstSeveranceRosterMember member = roster.Members[index];
                if (recovery.IsAlive(member.ParticipantId) && recovery.TryGetPlayer(member, out Player player))
                {
                    alive.Add(player);
                }
            }

            bool passed = true;
            var failedSlots = new HashSet<int>();
            float separationSquared = SpreadSeparationPixels * SpreadSeparationPixels;
            for (int left = 0; left < alive.Count; left++)
            {
                for (int right = left + 1; right < alive.Count; right++)
                {
                    if (Vector2.DistanceSquared(alive[left].Center, alive[right].Center)
                        < separationSquared)
                    {
                        passed = false;
                        failedSlots.Add(alive[left].whoAmI);
                        failedSlots.Add(alive[right].whoAmI);
                    }
                }
            }

            SetMechanicResult(passed
                ? FirstSeveranceMechanicResult.SpreadPassed
                : FirstSeveranceMechanicResult.SpreadFailed);
            Log(authorityTick, $"event=SpreadResolved alive={alive.Count} overlapped={failedSlots.Count} separation={SpreadSeparationPixels} passed={passed}");
            foreach (FirstSeveranceRosterMember member in roster.Members)
                if (failedSlots.Contains(member.ServerWhoAmI)
                    && recovery.TryGetPlayer(member, out Player player))
                    recovery.ApplyRaidDamage(member, Math.Max(1, player.statLifeMax2 * 70 / 100), authorityTick, "Spread");
        }
    }

    private bool ApplySubstateTransition(
        FirstSeveranceSubstate before,
        FirstSeveranceSubstate after,
        ulong authorityTick)
    {
        attacks.EnterSubstate(after, authorityTick);
        if (before == FirstSeveranceSubstate.PylonCheck)
        {
            actors.CleanupPylons();
        }

        if (after == FirstSeveranceSubstate.PylonCheck)
        {
            return actors.TrySpawnPylons(plan.PylonCount.GetValue(roster.Count), authorityTick);
        }

        return true;
    }

    private void SetMechanicResult(FirstSeveranceMechanicResult result)
    {
        lastMechanicResult = result;
        mechanicRevision = mechanicRevision == uint.MaxValue
            ? 1
            : mechanicRevision + 1;
    }

    private Vector2 CoreWorldCenter => groundCenter;
    private Vector2 StackWorldCenter
    {
        get
        {
            var point = FirstSeveranceChoreography.StackPosition(groundCenter.X, groundCenter.Y,
                loop?.State.BossPhase ?? FirstSeveranceBossPhase.Sealed, loop?.State.ActionIndex ?? -1);
            return new(point.X, point.Y);
        }
    }

    private void Log(ulong tick, string detail)
    {
        try
        {
            global::Convergence.ConvergenceMod.Instance.Logger.Info(
                $"FirstSeverance seq={encounterSequence} fight={fightId} tick={tick} {detail}");
        }
        catch (Exception) { } // A failed log sink cannot interrupt gameplay or cleanup.
    }

    private static EncounterRuntimeUpdate End(FirstSeveranceTerminalCause cause)
    {
        return EncounterRuntimeUpdate.End(
            FirstSeveranceTerminationContract.Instance.Create(cause));
    }
}
