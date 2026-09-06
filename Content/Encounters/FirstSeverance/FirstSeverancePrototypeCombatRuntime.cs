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

internal enum FirstSeveranceCombatIntentKind : byte
{
    Cancel = 0,
    PrototypeDown = 1,
    ReviveNearest = 2,
}

internal readonly record struct FirstSeveranceQueuedCombatIntent(
    FirstSeveranceCombatIntentKind Kind,
    FirstSeveranceRosterMember Member,
    uint RequestNonce);

internal sealed class FirstSeverancePrototypeCombatRuntime
{
    private const int MaximumPendingIntents = 16;
    private const float ReviveRangePixels = 8f * 16f;
    private const float StackRadiusPixels = FirstSeveranceLanceTuning.StackRadius;
    private const float SpreadSeparationPixels = FirstSeveranceLanceTuning.SpreadSeparation;

    private static int nextActorToken;

    private readonly ulong encounterSequence;
    private readonly FightId fightId;
    private readonly FirstSeveranceRoster roster;
    private readonly FirstSeverancePartyScaling partyScaling;
    private readonly int serverTileEntityId;
    private readonly TilePoint coreTopLeft;
    private readonly Vector2 groundCenter;
    private readonly FirstSeveranceArenaLayout fieldLayout;
    private readonly FirstSeveranceEncounterPlan plan;
    private readonly FirstSeveranceReviveBoundary revive;
    private readonly FirstSeveranceDebugAssistLease? debugAssist;
    private readonly uint[] lastQueuedNonces;
    private readonly bool[] observedConnected;
    private readonly RaidParticipantCombatState[] projectedCombatStates;
    private readonly uint[] healthRevisions;
    private readonly int[] correctedLife;
    private readonly Vector2[] downedPositions;
    private readonly ulong[] projectedReviveLockouts;
    private readonly HashSet<int> killedPylons = new();
    private readonly List<FirstSeveranceQueuedCombatIntent> pendingIntents = new();
    private readonly List<int> pylonNpcIndices = new();
    private FirstSeveranceLoopStateMachine? loop;
    private int actorToken;
    private int bossNpcIndex = -1;
    private int lastObservedBossLife;
    private FirstSeveranceGridVolley? gridVolley;
    private uint gridSerial;
    private ulong nextGridTick;
    private readonly HashSet<ParticipantId> gridHitParticipants = new();
    private bool bossKilled;
    private int boundaryDamage;
    private readonly HashSet<(int Pulse, ParticipantId Participant)> scoreHits = new();
    private readonly HashSet<int> scoreFires = new();
    private bool cancelRequested;
    private FirstSeveranceMechanicResult lastMechanicResult;
    private uint mechanicRevision;
    private bool isAttached;
    private bool isStarted;
    private bool isCleaned;
    private FirstSeveranceLanceVolley? lanceVolley;
    private uint lanceSerial;
    private ulong nextLanceTick;
    private byte attackStep;
    private int attackTargetSlot = -1;
    private uint attackSequence;
    private readonly HashSet<ParticipantId> lanceHitParticipants = new();
    private FirstSeveranceDamageWindow? damageWindow;
    private FirstSeveranceSubstate damageWindowPhase;
    private int damageWindowLoop;
    private int damageWindowFloor;
    private ulong nextProgressTick;
    private ulong combatStartedTick;
    private int[] progressPylonSlots = Array.Empty<int>();
    private int[] progressPylonLife = Array.Empty<int>();
    private bool progressObservationMissing;
    private long totalPylonDamage;
    private long totalCoreDamage;
    private ulong totalPylonOpenTicks;
    private ulong totalCoreOpenTicks;

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
        this.debugAssist = debugAssist;
        plan = FirstSeveranceEncounterPlan.Instance;
        revive = new FirstSeveranceReviveBoundary(fightId, LogReviveEvent);
        lastQueuedNonces = new uint[roster.Count];
        observedConnected = new bool[roster.Count];
        projectedCombatStates = new RaidParticipantCombatState[roster.Count];
        healthRevisions = new uint[roster.Count];
        correctedLife = new int[roster.Count];
        downedPositions = new Vector2[roster.Count];
        projectedReviveLockouts = new ulong[roster.Count];
        Array.Fill(observedConnected, true);
        Array.Fill(projectedCombatStates, RaidParticipantCombatState.Alive);
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

        var bindings = new RaidParticipantBinding[roster.Count];
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            bindings[index] = ToReviveBinding(member);
        }

        if (!revive.TryInitialize(Array.AsReadOnly(bindings), out failureCode))
        {
            return false;
        }

        actorToken = AllocateActorToken();
        if (!TrySpawnBoss(out bossNpcIndex))
        {
            failureCode = "first_severance.combat_boss_spawn_failed";
            return false;
        }

        loop = new FirstSeveranceLoopStateMachine(
            plan,
            roster.Count,
            partyScaling.BossLife,
            authorityTick, FirstSeveranceBossPhasePlan.Instance);
        lastObservedBossLife = partyScaling.BossLife;
        if (!FirstSeveranceCombatAuthority.TryAttach(this))
        {
            failureCode = "first_severance.combat_authority_busy";
            return false;
        }

        isAttached = true;
        isStarted = true;
        GrantPrototypeReviveKits();
        SynchronizeRevivePlayers(authorityTick);
        combatStartedTick = authorityTick;
        Log(authorityTick, $"event=CombatStarted participants={roster.Count} solo_debug={roster.Count == 1} solo_start_enabled={FirstSeveranceDevelopmentPolicy.AllowSoloDebugStart} hp_policy=FrozenRosterDevelopment stack_policy=MissingRosterFraction boss_max_life={partyScaling.BossLife} pylon_max_life={partyScaling.PylonLife} pylon_count={plan.PylonCount.GetValue(roster.Count)} pylon_open_ticks={plan.Timing.PylonActiveTicks} core_open_ticks={plan.Timing.NormalExposureTicks} core_penalized_ticks={plan.Timing.PenalizedExposureTicks} dps_basis=AuthorityNetHpLoss");
        failureCode = string.Empty;
        return true;
    }

    internal bool TryQueueCancel(int senderWhoAmI, ulong connectionEpoch,
        uint requestNonce, out string failureCode)
    {
        if (!TryValidateIntent(senderWhoAmI, connectionEpoch, requestNonce,
                out FirstSeveranceRosterMember member, out failureCode))
            return false;
        if (member.ParticipantId != roster.InitiatorParticipantId)
        {
            failureCode = "first_severance.cancel_not_initiator";
            return false;
        }
        QueueIntent(FirstSeveranceCombatIntentKind.Cancel, member, requestNonce);
        return true;
    }

    internal void RecordActorDeath(NPC npc)
    {
        if (!isStarted || isCleaned || (int)npc.ai[0] != actorToken || loop is null)
            return;
        if (npc.whoAmI == bossNpcIndex
            && npc.type == ModContent.NPCType<FirstSeverancePrototypeBoss>()
            && FirstSeveranceBossPhasePlan.IsDamageState(loop.State.Substate))
            bossKilled = true;
        else if (npc.type == ModContent.NPCType<FirstSeverancePrototypePylon>()
            && pylonNpcIndices.Contains(npc.whoAmI))
            killedPylons.Add(npc.whoAmI);
    }

    internal bool CanHitActor(NPC npc, int playerSlot)
    {
        if (!isStarted || isCleaned || (int)npc.ai[0] != actorToken
            || !FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(playerSlot, out ulong epoch)
            || !roster.TryResolveCurrentBinding(playerSlot, epoch, out var member))
            return false;
        return projectedCombatStates[member.ParticipantId.Value] == RaidParticipantCombatState.Alive
            && !Main.player[playerSlot].GetModPlayer<FirstSeveranceRaidPlayer>().IsReviving;
    }

    internal bool ProtectBossPhaseBoundary(NPC npc)
    {
        if (!isStarted || isCleaned || loop is null || npc.whoAmI != bossNpcIndex
            || (int)npc.ai[0] != actorToken)
            return false;
        boundaryDamage = Math.Max(boundaryDamage, Math.Max(0, loop.State.BossLife - loop.DamageFloor));
        npc.life = Math.Max(1, loop.DamageFloor);
        npc.netUpdate = true;
        return true;
    }

    internal bool TryQueuePrototypeDown(
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out string failureCode)
    {
        if (!TryValidateIntent(
                senderWhoAmI,
                connectionEpoch,
                requestNonce,
                out FirstSeveranceRosterMember member,
                out failureCode))
        {
            return false;
        }

        if (!TryGetReviveParticipant(member.ParticipantId, out RaidParticipantReviveSnapshot state)
            || state.CombatState != RaidParticipantCombatState.Alive)
        {
            failureCode = "first_severance.prototype_down_not_alive";
            return false;
        }

        QueueIntent(FirstSeveranceCombatIntentKind.PrototypeDown, member, requestNonce);
        failureCode = string.Empty;
        return true;
    }

    internal bool TryQueueReviveNearest(
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out string failureCode)
    {
        if (!TryValidateIntent(
                senderWhoAmI,
                connectionEpoch,
                requestNonce,
                out FirstSeveranceRosterMember member,
                out failureCode))
        {
            return false;
        }

        if (!TryValidateRevive(member, Main.GameUpdateCount, out _, out failureCode))
            return false;

        QueueIntent(FirstSeveranceCombatIntentKind.ReviveNearest, member, requestNonce);
        failureCode = string.Empty;
        return true;
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

        ApplyConnectionChanges(context.AuthorityTick);
        foreach (FirstSeveranceRosterMember member in roster.Members)
        {
            // The experimental adapter does not reinterpret ordinary Terraria death
            // or assign a rejoined slot the original participant's identity.
            if (!TryGetPlayer(member, out Player player) || player.dead)
                return End(FirstSeveranceTerminalCause.AdministrativeAbort);
        }
        ApplyPendingIntents(context.AuthorityTick);
        if (cancelRequested)
            return End(FirstSeveranceTerminalCause.UserCancelled);

        FirstSeveranceLoopState before = loop.State;
        ResolveMechanicIfDue(before, context.AuthorityTick);

        if (!TryObservePylons(before, context.AuthorityTick, out int destroyedPylons))
        {
            return End(FirstSeveranceTerminalCause.RuntimeInvariantBroken);
        }

        if (!TryObserveBoss(before, out int acceptedBossDamage))
        {
            return End(FirstSeveranceTerminalCause.BossActorMissing);
        }

        bool lanceChanged = UpdateLances(before, context.AuthorityTick,
            before.Substate == FirstSeveranceSubstate.PylonCheck && destroyedPylons >= before.RemainingPylons);
        lanceChanged |= UpdateGrid(before, context.AuthorityTick, false);
        UpdateScoreHazards(before, context.AuthorityTick);

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
        PublishDamageProgress(after, context.AuthorityTick);
        if (after.Overload > before.Overload)
        {
            foreach (FirstSeveranceRosterMember member in roster.Members)
            {
                if (TryGetPlayer(member, out Player player))
                    ApplyRaidDamage(member, Math.Min(player.statLife - 1,
                        player.statLifeMax2 * 35 / 100), context.AuthorityTick, "PylonPulse");
            }
        }

        // Instant starts are adjudicated after all same-tick damage/invalidations.
        ApplyPendingRevives(context.AuthorityTick);
        EncounterRuntimeUpdate reviveUpdate = revive.Tick(context);
        SynchronizeRevivePlayers(context.AuthorityTick);
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
            BeginDamageProgress(after);
        }

        UpdateDamageWindows(after, context.AuthorityTick);
        SynchronizeBossLife(after);

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
        if (!isStarted || isCleaned || loop is null || !revive.IsInitialized)
        {
            projection = null;
            return false;
        }

        RaidReviveSnapshot reviveSnapshot = revive.CreateSnapshot()!.Value;
        var participants = new FirstSeveranceCombatParticipantProjection[roster.Count];
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            RaidParticipantReviveSnapshot reviveParticipant =
                FindReviveParticipant(reviveSnapshot, member.ParticipantId);
            participants[index] = CreateParticipantProjection(reviveSnapshot, reviveParticipant, index);
        }

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
            lanceVolley, state.BossPhase, state.BossPhaseStartedTick, gridVolley,
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
        FinishDamageProgress(Main.GameUpdateCount, terminalCause);

        CleanupPylons();
        CleanupNpc(bossNpcIndex, ModContent.NPCType<FirstSeverancePrototypeBoss>());
        bossNpcIndex = -1;
        debugAssist?.Revoke(); // Clear protection before any terminal player death.
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            if (TryGetPlayer(member, out Player player))
            {
                player.GetModPlayer<FirstSeveranceRaidPlayer>().ClearRaidState();
                if (context.EndReason == EncounterEndReason.Defeat)
                    Log(Main.GameUpdateCount, $"event=DefeatDeathOrdered participant={member.ParticipantId.Value} slot={member.ServerWhoAmI}");
            }
        }

        revive.Cleanup(context);
        scoreHits.Clear();
        scoreFires.Clear();
        boundaryDamage = 0;
        Array.Clear(projectedReviveLockouts);
        lanceVolley = null;
        lanceHitParticipants.Clear();
        gridVolley = null;
        gridHitParticipants.Clear();
        nextLanceTick = 0;
        attackStep = 0;
        attackTargetSlot = -1;
        attackSequence = 0;
        pendingIntents.Clear();
        Array.Clear(lastQueuedNonces);
        if (isAttached)
        {
            FirstSeveranceCombatAuthority.Detach(this);
            isAttached = false;
        }

        isCleaned = true;
    }

    private bool TryValidateIntent(
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out FirstSeveranceRosterMember member,
        out string failureCode)
    {
        if (!isStarted || isCleaned || !isAttached || loop?.State.IsTerminal != false)
        {
            member = default;
            failureCode = "first_severance.combat_not_available";
            return false;
        }

        if (pendingIntents.Count >= MaximumPendingIntents)
        {
            member = default;
            failureCode = "first_severance.combat_intent_queue_full";
            return false;
        }

        if (!roster.TryResolveCurrentBinding(senderWhoAmI, connectionEpoch, out member))
        {
            failureCode = "first_severance.combat_sender_not_bound";
            return false;
        }

        int index = member.ParticipantId.Value;
        ParticipantId pendingParticipant = member.ParticipantId;
        if (pendingIntents.Exists(intent => intent.Member.ParticipantId == pendingParticipant))
        {
            failureCode = "first_severance.combat_request_pending";
            return false;
        }
        if (requestNonce == 0 || requestNonce <= lastQueuedNonces[index])
        {
            failureCode = "first_severance.combat_nonce_stale";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    private void QueueIntent(
        FirstSeveranceCombatIntentKind kind,
        FirstSeveranceRosterMember member,
        uint requestNonce)
    {
        pendingIntents.Add(new FirstSeveranceQueuedCombatIntent(kind, member, requestNonce));
        lastQueuedNonces[member.ParticipantId.Value] = requestNonce;
    }

    private void ApplyPendingIntents(ulong authorityTick)
    {
        if (pendingIntents.Count == 0)
        {
            return;
        }

        pendingIntents.Sort(CompareIntents);
        for (int index = 0; index < pendingIntents.Count; index++)
        {
            FirstSeveranceQueuedCombatIntent intent = pendingIntents[index];
            RaidParticipantBinding binding = ToReviveBinding(intent.Member);
            if (intent.Kind == FirstSeveranceCombatIntentKind.Cancel)
            {
                cancelRequested = true;
                break;
            }
            if (!TryGetPlayer(intent.Member, out Player sender) || sender.dead)
                continue;
            if (intent.Kind == FirstSeveranceCombatIntentKind.PrototypeDown)
            {
                Log(authorityTick, $"event=DownRequested source=DebugCommand participant={intent.Member.ParticipantId.Value} slot={intent.Member.ServerWhoAmI}");
                revive.Apply(new AuthoritativeParticipantDownedCommand(
                    fightId,
                    binding,
                    authorityTick));
            }
        }
    }

    private void ApplyConnectionChanges(ulong authorityTick)
    {
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            bool connected = FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(
                    member.ServerWhoAmI,
                    out ulong currentEpoch)
                && currentEpoch == member.ConnectionEpoch
                && Main.player[member.ServerWhoAmI].active;
            if (connected || !observedConnected[index])
            {
                continue;
            }

            observedConnected[index] = false;
            revive.Apply(new RaidParticipantDisconnectedCommand(
                fightId,
                ToReviveBinding(member),
                authorityTick));
        }
    }

    private void ApplyPendingRevives(ulong authorityTick)
    {
        var starts = new List<RaidReviveStartCommand>(roster.Count);
        foreach (FirstSeveranceQueuedCombatIntent intent in pendingIntents)
        {
            if (intent.Kind != FirstSeveranceCombatIntentKind.ReviveNearest)
                continue;
            if (!TryValidateRevive(intent.Member, authorityTick, out ParticipantId target,
                    out string failureCode))
            {
                FirstSeverancePacketSystem.SendValidation(intent.Member.ServerWhoAmI,
                    intent.RequestNonce, false, failureCode);
                continue;
            }
            starts.Add(new RaidReviveStartCommand(fightId, ToReviveBinding(intent.Member),
                target, intent.RequestNonce, authorityTick));
        }
        if (starts.Count > 0)
        {
            RaidReviveStartBatchResult result = revive.ApplyStartBatch(starts);
            for (int index = 0; index < starts.Count; index++)
            {
                // Both the ingress list and the domain batch are ordered by stable reviver ID.
                RaidReviveCommandResult command = result.IsAccepted ? result.CommandResults[index]
                    : RaidReviveCommandResult.Rejected(result.FailureCode);
                FirstSeverancePacketSystem.SendValidation(starts[index].Reviver.PlayerSlot,
                    starts[index].RequestNonce, command.IsAccepted, command.FailureCode);
            }
        }
        pendingIntents.Clear();
    }

    private void SynchronizeRevivePlayers(ulong authorityTick)
    {
        RaidReviveSnapshot? snapshot = revive.CreateSnapshot();
        if (snapshot is null)
        {
            return;
        }

        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            RaidParticipantReviveSnapshot participant =
                FindReviveParticipant(snapshot.Value, member.ParticipantId);
            if (!TryGetPlayer(member, out Player player))
            {
                projectedCombatStates[index] = participant.CombatState;
                continue;
            }

            RaidParticipantCombatState before = projectedCombatStates[index];
            bool lifeChanged = false;
            if (participant.CombatState == RaidParticipantCombatState.Downed
                && before == RaidParticipantCombatState.Alive)
            {
                downedPositions[index] = player.position;
                SetCorrectedLife(index, player, 1);
                lifeChanged = true;
            }
            else if (participant.CombatState == RaidParticipantCombatState.Alive
                && (before == RaidParticipantCombatState.Downed
                    || participant.ReviveLockoutUntilTick > projectedReviveLockouts[index]))
            {
                int restoredLife = Math.Max(1, (int)Math.Ceiling(player.statLifeMax2 * 0.35f));
                SetCorrectedLife(index, player, restoredLife);
                lifeChanged = true;
            }

            FirstSeveranceCombatParticipantProjection projection =
                CreateParticipantProjection(snapshot.Value, participant, index);
            player.GetModPlayer<FirstSeveranceRaidPlayer>().ApplyProjection(
                fightId, projection, authorityTick);
            player.GetModPlayer<FirstSeveranceContainmentPlayer>().Refresh(fightId,
                FirstSeveranceContainmentBounds.FromGround(groundCenter.X, groundCenter.Y), member.ConnectionEpoch);

            if (lifeChanged && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.PlayerLifeMana, number: member.ServerWhoAmI);
            }

            projectedCombatStates[index] = participant.CombatState;
            projectedReviveLockouts[index] = participant.ReviveLockoutUntilTick;
        }
    }

    private FirstSeveranceCombatParticipantProjection CreateParticipantProjection(
        in RaidReviveSnapshot snapshot, in RaidParticipantReviveSnapshot participant, int index)
    {
        ulong completesTick = 0;
        foreach (RaidReviveChannelSnapshot channel in snapshot.Channels)
            if (channel.Reviver == participant.Binding.ParticipantId)
                completesTick = channel.CompletesTick;
        return new FirstSeveranceCombatParticipantProjection(
            participant.Binding.ParticipantId, participant.Binding.PlayerSlot,
            participant.IsConnected, participant.CombatState, completesTick > 0,
            participant.DownedDeadlineTick, completesTick, healthRevisions[index],
            Math.Max(1, correctedLife[index]), downedPositions[index].X, downedPositions[index].Y,
            participant.InvulnerabilityUntilTick, participant.WeaknessUntilTick,
            participant.ReviveLockoutUntilTick,
            debugAssist?.Protects(fightId, roster.Members[index]) == true
                && participant.IsConnected && participant.CombatState == RaidParticipantCombatState.Alive);
    }

    private void SetCorrectedLife(int index, Player player, int life)
    {
        player.statLife = Math.Clamp(life, 1, player.statLifeMax2);
        correctedLife[index] = player.statLife;
        healthRevisions[index]++;
    }

    private void ApplyRaidDamage(FirstSeveranceRosterMember member, int damage, ulong authorityTick, string source)
    {
        if (damage <= 0 || !TryGetPlayer(member, out Player player) || player.dead
            || !TryGetReviveParticipant(member.ParticipantId, out RaidParticipantReviveSnapshot state)
            || state.CombatState != RaidParticipantCombatState.Alive
            || authorityTick < state.InvulnerabilityUntilTick)
            return;

        if (debugAssist?.Protects(fightId, member) == true)
        {
            Log(authorityTick, $"event=DebugAssistWouldHit source={source} participant={member.ParticipantId.Value} damage={damage} life_before={player.statLife}");
            return;
        }

        Log(authorityTick, $"event=RaidDamage source={source} participant={member.ParticipantId.Value} slot={member.ServerWhoAmI} damage={damage} life_before={player.statLife} max_life={player.statLifeMax2} lethal={damage >= player.statLife}");

        // Experimental encounter-owned damage, not an interception of another Mod's hit.
        // This path never sends a lethal HP value or invokes Terraria's death hooks.
        if (damage >= player.statLife)
            revive.Apply(new AuthoritativeParticipantDownedCommand(
                fightId, ToReviveBinding(member), authorityTick));
        else
            SetCorrectedLife(member.ParticipantId.Value, player, player.statLife - damage);
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
                if (IsAlive(member.ParticipantId)
                    && TryGetPlayer(member, out Player player)
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
                if (TryGetPlayer(member, out Player measured))
                    Log(authorityTick, FormattableString.Invariant($"event=StackAttendance participant={member.ParticipantId.Value} slot={member.ServerWhoAmI} alive={IsAlive(member.ParticipantId)} x={measured.Center.X:F1} y={measured.Center.Y:F1} vx={measured.velocity.X:F2} vy={measured.velocity.Y:F2} left={measured.controlLeft} right={measured.controlRight} jump={measured.controlJump} distance={Vector2.Distance(measured.Center, center):F1} inside={Vector2.DistanceSquared(measured.Center, center) <= radiusSquared}"));
            // Failure is the missing fraction of each survivor's own maximum HP.
            // This includes missed soakers so abandoning the group is not immunity.
            foreach (FirstSeveranceRosterMember member in roster.Members)
                if (TryGetPlayer(member, out Player player))
                    ApplyRaidDamage(member, FirstSeveranceCombatRules.StackDamage(
                        player.statLifeMax2, required, stacked), authorityTick, "Stack");
        }
        else if (state.Substate == FirstSeveranceSubstate.Spread)
        {
            var alive = new List<Player>(roster.Count);
            for (int index = 0; index < roster.Count; index++)
            {
                FirstSeveranceRosterMember member = roster.Members[index];
                if (IsAlive(member.ParticipantId) && TryGetPlayer(member, out Player player))
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
                    && TryGetPlayer(member, out Player player))
                    ApplyRaidDamage(member, Math.Max(1, player.statLifeMax2 * 70 / 100), authorityTick, "Spread");
        }
    }

    private bool TryObservePylons(
        in FirstSeveranceLoopState state,
        ulong authorityTick,
        out int destroyedPylons)
    {
        destroyedPylons = 0;
        if (state.Substate != FirstSeveranceSubstate.PylonCheck)
        {
            return pylonNpcIndices.Count == 0;
        }

        bool damageWindowOpen = authorityTick >= state.SubstateEnteredTick
            + (ulong)plan.Timing.PylonTelegraphTicks;
        ObservePylonProgress(authorityTick, damageWindowOpen);
        for (int index = pylonNpcIndices.Count - 1; index >= 0; index--)
        {
            int npcIndex = pylonNpcIndices[index];
            if (killedPylons.Remove(npcIndex) && damageWindowOpen)
            {
                destroyedPylons++;
                pylonNpcIndices.RemoveAt(index);
                continue;
            }
            if (TryResolveOwnedNpc<FirstSeverancePrototypePylon>(npcIndex, out _))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool TryObserveBoss(
        in FirstSeveranceLoopState state,
        out int acceptedBossDamage)
    {
        acceptedBossDamage = 0;
        if (!TryResolveOwnedNpc<FirstSeverancePrototypeBoss>(bossNpcIndex, out NPC boss))
        {
            if (FirstSeveranceBossPhasePlan.IsDamageState(state.Substate) && bossKilled)
            {
                acceptedBossDamage = lastObservedBossLife;
                return true;
            }

            return false;
        }

        int observedLife = Math.Clamp(boss.life, 0, state.BossMaximumLife);
        if (FirstSeveranceBossPhasePlan.IsDamageState(state.Substate))
        {
            acceptedBossDamage = Math.Max(boundaryDamage, Math.Max(0, lastObservedBossLife - observedLife));
        }
        else if (observedLife != state.BossLife)
        {
            boss.life = Math.Max(1, state.BossLife);
            boss.netUpdate = true;
        }

        boundaryDamage = 0;
        return true;
    }

    private bool ApplySubstateTransition(
        FirstSeveranceSubstate before,
        FirstSeveranceSubstate after,
        ulong authorityTick)
    {
        // Warning, hit ledger and next cast belong to this Fight and phase only.
        lanceVolley = null;
        lanceHitParticipants.Clear();
        scoreHits.Clear();
        scoreFires.Clear();
        nextLanceTick = authorityTick + (after == FirstSeveranceSubstate.PylonCheck
            ? (ulong)plan.Timing.PylonTelegraphTicks : (ulong)FirstSeveranceAttackPatterns.ExposureOpeningRestTicks);
        attackStep = 0;
        attackTargetSlot = -1;
        if (before == FirstSeveranceSubstate.PylonCheck)
        {
            CleanupPylons();
        }

        if (after == FirstSeveranceSubstate.PylonCheck)
        {
            return TrySpawnPylons(plan.PylonCount.GetValue(roster.Count), authorityTick);
        }

        gridVolley = null;
        gridHitParticipants.Clear();
        nextGridTick = authorityTick + 60;

        return true;
    }

    private void UpdateDamageWindows(
        in FirstSeveranceLoopState state,
        ulong authorityTick)
    {
        if (TryResolveOwnedNpc<FirstSeverancePrototypeBoss>(bossNpcIndex, out NPC boss))
        {
            bool shielded = !plan.Boss.CanTakeDamage(state.Substate) || state.BossLife <= loop!.DamageFloor
                || state.BossPhase == FirstSeveranceBossPhase.Final;
            if (boss.dontTakeDamage != shielded || boss.chaseable == shielded)
            {
                boss.dontTakeDamage = shielded;
                boss.chaseable = !shielded;
                boss.ai[2] = shielded ? 0f : 1f;
                boss.netUpdate = true;
            }
        }

        bool pylonsShielded = state.Substate != FirstSeveranceSubstate.PylonCheck
            || authorityTick < state.SubstateEnteredTick
                + (ulong)plan.Timing.PylonTelegraphTicks;
        for (int index = 0; index < pylonNpcIndices.Count; index++)
        {
            if (!TryResolveOwnedNpc<FirstSeverancePrototypePylon>(
                    pylonNpcIndices[index],
                    out NPC pylon)
                || (pylon.dontTakeDamage == pylonsShielded
                    && pylon.chaseable != pylonsShielded))
            {
                continue;
            }

            pylon.dontTakeDamage = pylonsShielded;
            pylon.chaseable = !pylonsShielded;
            pylon.ai[2] = pylonsShielded ? 0f : 1f;
            pylon.netUpdate = true;
        }
    }

    private void SynchronizeBossLife(in FirstSeveranceLoopState state)
    {
        if (!TryResolveOwnedNpc<FirstSeverancePrototypeBoss>(bossNpcIndex, out NPC boss))
        {
            return;
        }

        int life = Math.Max(1, state.BossLife);
        if (boss.life != life)
        {
            boss.life = life;
            boss.netUpdate = true;
        }

        lastObservedBossLife = state.BossLife;
    }

    private bool TrySpawnBoss(out int npcIndex)
    {
        Vector2 center = CoreWorldCenter + new Vector2(0f, -FirstSeveranceLanceTuning.BossHeightAboveCore);
        npcIndex = SpawnNpc(
            ModContent.NPCType<FirstSeverancePrototypeBoss>(),
            center,
            actorIndex: 0);
        return npcIndex >= 0;
    }

    private bool TrySpawnPylons(int count, ulong authorityTick)
    {
        _ = authorityTick;
        CleanupPylons();
        Vector2[] offsets =
        {
            new(-500f, -470f),
            new(500f, -470f),
            new(-650f, -810f),
            new(650f, -810f),
        };
        for (int index = 0; index < count; index++)
        {
            int npcIndex = SpawnNpc(
                ModContent.NPCType<FirstSeverancePrototypePylon>(),
                CoreWorldCenter + offsets[index],
                index + 1);
            if (npcIndex < 0)
            {
                CleanupPylons();
                return false;
            }

        }

        return true;
    }

    private int SpawnNpc(int npcType, Vector2 center, int actorIndex)
    {
        int npcIndex = NPC.NewNPC(
            new EntitySource_Misc("Convergence.FirstSeverancePrototype"),
            (int)center.X,
            (int)center.Y,
            npcType,
            0,
            actorToken,
            actorIndex);
        if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
        {
            return -1;
        }

        // Record ownership before any customization or network call can fail.
        if (npcType == ModContent.NPCType<FirstSeverancePrototypeBoss>())
            bossNpcIndex = npcIndex;
        else
            pylonNpcIndices.Add(npcIndex);

        NPC npc = Main.npc[npcIndex];
        if (npc.ModNPC is FirstSeverancePrototypeBoss boss) boss.ConfigureParty(partyScaling.ParticipantCount);
        else if (npc.ModNPC is FirstSeverancePrototypePylon pylon) pylon.ConfigureParty(partyScaling.ParticipantCount);
        npc.life = npc.lifeMax;
        npc.Center = center;
        npc.ai[0] = actorToken;
        npc.ai[1] = actorIndex;
        npc.netUpdate = true;
        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendData(MessageID.SyncNPC, number: npcIndex);
        }

        return npcIndex;
    }

    private bool TryResolveOwnedNpc<TNpc>(int npcIndex, out NPC npc)
        where TNpc : ModNPC
    {
        if (npcIndex >= 0
            && npcIndex < Main.maxNPCs
            && Main.npc[npcIndex].active
            && Main.npc[npcIndex].type == ModContent.NPCType<TNpc>()
            && (int)Main.npc[npcIndex].ai[0] == actorToken)
        {
            npc = Main.npc[npcIndex];
            return true;
        }

        npc = null!;
        return false;
    }

    private void CleanupPylons()
    {
        int type = ModContent.NPCType<FirstSeverancePrototypePylon>();
        for (int index = 0; index < pylonNpcIndices.Count; index++)
        {
            CleanupNpc(pylonNpcIndices[index], type);
        }

        pylonNpcIndices.Clear();
        killedPylons.Clear();
    }

    private void CleanupNpc(int npcIndex, int expectedType)
    {
        if (npcIndex < 0
            || npcIndex >= Main.maxNPCs
            || !Main.npc[npcIndex].active
            || Main.npc[npcIndex].type != expectedType
            || (int)Main.npc[npcIndex].ai[0] != actorToken)
        {
            return;
        }

        Main.npc[npcIndex].active = false;
        Main.npc[npcIndex].netUpdate = true;
        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendData(MessageID.SyncNPC, number: npcIndex);
        }
    }

    private bool UpdateGrid(in FirstSeveranceLoopState state, ulong tick, bool bossDead)
    {
        if (state.Substate != FirstSeveranceSubstate.Lattice || tick >= state.ResolveTick || bossDead)
        {
            bool had = gridVolley is not null;
            gridVolley = null;
            gridHitParticipants.Clear();
            return had;
        }
        bool changed = false;
        if (gridVolley is not null && tick >= gridVolley.EndTick)
        {
            gridVolley = null;
            gridHitParticipants.Clear();
            changed = true;
        }
        if (gridVolley is null && tick >= nextGridTick
            && state.ResolveTick - tick >= FirstSeveranceGridVolley.TelegraphTicks + FirstSeveranceGridVolley.ActiveTicks)
        {
            uint serial = ++gridSerial;
            var beams = new List<FirstSeveranceLanceRay>(roster.Count);
            if (serial >= FirstSeveranceGridVolley.CoreSalvoFirstSerial)
                foreach (var member in roster.Members)
                    if (IsAlive(member.ParticipantId) && TryGetPlayer(member, out Player target))
                        beams.Add(FirstSeveranceGridVolley.AimCoreBeam(groundCenter.X, groundCenter.Y,
                            target.Center.X, target.Center.Y));
            gridVolley = new(serial, tick, (byte)((serial - 1) % 4), groundCenter.X, groundCenter.Y, beams);
            nextGridTick = tick + FirstSeveranceGridVolley.CadenceTicks;
            if (serial % 4 == 0) nextGridTick += 60;
            changed = true;
            Log(tick, $"event=GridTelegraph cast={serial} pattern={gridVolley.Pattern} lines={gridVolley.Rays.Count} core_beams={gridVolley.CoreBeams.Count} fire_tick={gridVolley.FireTick} end_tick={gridVolley.EndTick}");
        }
        if (gridVolley is null || !gridVolley.IsFiring(tick)) return changed;
        if (tick == gridVolley.FireTick)
        {
            changed = true;
            Log(tick, $"event=GridFired cast={gridVolley.Serial} pattern={gridVolley.Pattern} core_beams={gridVolley.CoreBeams.Count} fixed_damage={FirstSeveranceCombatRules.BeamDamage} hit_cap=OnePerVolley");
        }
        foreach (var member in roster.Members)
        {
            if (gridHitParticipants.Contains(member.ParticipantId) || !IsAlive(member.ParticipantId)
                || !TryGetPlayer(member, out Player player)
                || !gridVolley.Intersects(tick, player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f))
                continue;
            gridHitParticipants.Add(member.ParticipantId); // Crossings cannot double-hit a participant.
            string source = gridVolley.CoreIntersects(tick, player.Center.X, player.Center.Y,
                player.width * .5f, player.height * .5f) ? "CoreSalvo" : "Lattice";
            ApplyRaidDamage(member, FirstSeveranceCombatRules.BeamDamage, tick, source);
            changed = true;
        }
        return changed;
    }

    private bool UpdateLances(in FirstSeveranceLoopState state, ulong tick, bool phaseComplete)
    {
        if (phaseComplete || !FirstSeveranceLanceTuning.IsAttackPhase(state.Substate)
            || tick >= state.ResolveTick)
        {
            bool hadVolley = lanceVolley is not null;
            lanceVolley = null;
            lanceHitParticipants.Clear();
            return hadVolley;
        }

        bool changed = false;
        if (lanceVolley is not null && tick >= lanceVolley.EndTick)
        {
            lanceVolley = null;
            lanceHitParticipants.Clear();
            changed = true;
        }

        // Never truncate a new telegraph at the phase boundary. Do not catch up
        // missed casts in a burst; scheduling is relative to the actual start.
        int neededTicks = attackStep == 0 ? FirstSeveranceAttackPatterns.SequenceTicks(state.Substate)
            : FirstSeveranceAttackPatterns.StepTicks(state.Substate, attackStep);
        if (lanceVolley is null && tick >= nextLanceTick && state.ResolveTick - tick >= (ulong)neededTicks)
        {
            var targets = new List<Player>(roster.Count);
            int first = (int)(attackSequence % (uint)roster.Count);
            for (int offset = 0; offset < roster.Count; offset++)
            {
                FirstSeveranceRosterMember member = roster.Members[(first + offset) % roster.Count];
                if (IsAlive(member.ParticipantId) && TryGetPlayer(member, out Player player))
                    targets.Add(player);
            }
            if (targets.Count > 0)
            {
                Player target = attackStep == 0 ? targets[0]
                    : targets.Find(player => player.whoAmI == attackTargetSlot) ?? targets[0];
                if (attackStep == 0)
                    attackSequence++;
                attackTargetSlot = target.whoAmI;
                if (state.Substate == FirstSeveranceSubstate.PylonCheck)
                {
                    var aims = new List<FirstSeverancePrismTarget>(targets.Count);
                    foreach (Player participant in targets)
                        aims.Add(new(participant.whoAmI, participant.Center.X, participant.Center.Y,
                            participant.velocity.X, participant.velocity.Y));
                    // One simultaneous, locked ray per standing participant. A crossed
                    // group of rays is still capped to one hit per player per step.
                    lanceVolley = FirstSeveranceAttackPatterns.CreatePrism(++lanceSerial, tick, attackStep, aims);
                }
                else
                    lanceVolley = FirstSeveranceAttackPatterns.Create(++lanceSerial, tick,
                        state.Substate, attackStep, target.whoAmI, target.Center.X, target.Center.Y,
                        target.velocity.X, target.velocity.Y);
                nextLanceTick = tick + (ulong)FirstSeveranceAttackPatterns.StepCadence(state.Substate);
                attackStep++;
                if (attackStep >= FirstSeveranceAttackPatterns.StepCount(state.Substate))
                {
                    attackStep = 0;
                    nextLanceTick += (ulong)FirstSeveranceAttackPatterns.SequenceRestTicks;
                }
                lanceHitParticipants.Clear();
                changed = true; // Publish the locked aim immediately, not on the 30-tick heartbeat.
                string assignedSlots = lanceVolley.Kind == FirstSeveranceAttackKind.PursuitPrism
                    ? string.Join(",", targets.ConvertAll(player => player.whoAmI)) : target.whoAmI.ToString();
                Log(tick, $"event=LanceTelegraph cast={lanceSerial} kind={lanceVolley.Kind} step={lanceVolley.Step + 1} target_slot={target.whoAmI} target_slots={assignedSlots} rays={lanceVolley.Rays.Count} fire_tick={lanceVolley.FireTick}");
            }
        }

        if (lanceVolley is { IsCharge: true } body)
        {
            Player focus = Main.player[body.TargetSlot];
            // No target replacement mid-charge. If the focus is Down, pursue its
            // last anchor and finish the same bounded trajectory, never an outsider.
            lanceVolley = body.AdvanceCharge(tick, focus.Center.X, focus.Center.Y,
                focus.velocity.X, focus.velocity.Y);
            changed |= (tick < body.LockTick && tick % 4ul == 0)
                || tick == body.LockTick;
            if (tick == body.LockTick)
                Log(tick, $"event=EnergyChargeLocked cast={body.Serial} speed=104 target_slot={body.TargetSlot}");
        }
        if (lanceVolley is null || !lanceVolley.IsFiring(tick))
            return changed;
        if (tick == lanceVolley.FireTick)
        {
            changed = true;
            Log(tick, $"event=LanceFired cast={lanceVolley.Serial} rays={lanceVolley.Rays.Count}");
        }
        foreach (FirstSeveranceRosterMember member in roster.Members)
        {
            if (lanceHitParticipants.Contains(member.ParticipantId) || !IsAlive(member.ParticipantId)
                || !TryGetPlayer(member, out Player player))
                continue;
            // Contact-style energy charges respect legitimate engine/dash i-frames.
            // Stack/Spread remain authority percentage mechanics, not dodgeable hits.
            if (lanceVolley.IsCharge && player.immune && player.immuneTime > 0)
                continue;
            for (int index = 0; index < lanceVolley.Rays.Count; index++)
            {
                FirstSeveranceLanceRay ray = lanceVolley.RayAt(index, tick);
                if (!ray.Intersects(player.Center.X, player.Center.Y, player.width * 0.5f, player.height * 0.5f))
                    continue;
                // One hit per step, including overlapping curtains or moving blades.
                lanceHitParticipants.Add(member.ParticipantId);
                ApplyRaidDamage(member, FirstSeveranceCombatRules.AttackDamage(
                    lanceVolley.Kind, player.statLifeMax2), tick, lanceVolley.Kind.ToString());
                changed = true;
                break;
            }
        }
        return changed;
    }

    private void GrantPrototypeReviveKits()
    {
        int itemType = ModContent.ItemType<ResuscitationKitItem>();
        var source = new EntitySource_Misc("Convergence.FirstSeverancePrototype");
        for (int index = 0; index < roster.Count; index++)
        {
            if (TryGetPlayer(roster.Members[index], out Player player)
                && !player.HasItem(itemType))
            {
                player.QuickSpawnItem(source, itemType);
            }
        }
    }

    private bool TryValidateRevive(
        FirstSeveranceRosterMember reviver,
        ulong authorityTick,
        out ParticipantId target,
        out string failureCode)
    {
        target = ParticipantId.Invalid;
        failureCode = "first_severance.revive_sender_not_alive";
        if (!TryGetPlayer(reviver, out Player reviverPlayer) || reviverPlayer.dead
            || !IsAlive(reviver.ParticipantId))
        {
            return false;
        }
        if (reviverPlayer.HeldItem.type != ModContent.ItemType<ResuscitationKitItem>()
            || reviverPlayer.HeldItem.stack < 1)
        {
            failureCode = "first_severance.revive_kit_not_selected";
            return false;
        }

        RaidReviveSnapshot? snapshot = revive.CreateSnapshot();
        failureCode = "first_severance.revive_no_downed_ally_in_range";
        if (snapshot is null)
        {
            return false;
        }

        float nearestDistanceSquared = ReviveRangePixels * ReviveRangePixels;
        for (int index = 0; index < snapshot.Value.Participants.Count; index++)
        {
            RaidParticipantReviveSnapshot candidate = snapshot.Value.Participants[index];
            bool reserved = false;
            foreach (RaidReviveChannelSnapshot channel in snapshot.Value.Channels)
                reserved |= channel.Target == candidate.Binding.ParticipantId;
            if (candidate.Binding.ParticipantId == reviver.ParticipantId
                || reserved
                || candidate.CombatState != RaidParticipantCombatState.Downed
                || !candidate.IsConnected
                || !roster.TryGet(candidate.Binding.ParticipantId, out FirstSeveranceRosterMember member)
                || !TryGetPlayer(member, out Player targetPlayer))
            {
                continue;
            }

            float distanceSquared = Vector2.DistanceSquared(
                reviverPlayer.Center,
                targetPlayer.Center);
            if (candidate.DownedDeadlineTick != 0 && candidate.DownedDeadlineTick <= authorityTick)
                continue;
            if (candidate.ReviveLockoutUntilTick > authorityTick)
            {
                if (distanceSquared <= ReviveRangePixels * ReviveRangePixels)
                    failureCode = "first_severance.revive_target_recovery_locked";
                continue;
            }
            if (distanceSquared <= nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                target = member.ParticipantId;
            }
        }

        if (target.IsValid)
            failureCode = string.Empty;
        return target.IsValid;
    }

    private bool TryGetReviveParticipant(
        ParticipantId participantId,
        out RaidParticipantReviveSnapshot participant)
    {
        RaidReviveSnapshot? snapshot = revive.CreateSnapshot();
        if (snapshot is not null)
        {
            for (int index = 0; index < snapshot.Value.Participants.Count; index++)
            {
                if (snapshot.Value.Participants[index].Binding.ParticipantId == participantId)
                {
                    participant = snapshot.Value.Participants[index];
                    return true;
                }
            }
        }

        participant = default;
        return false;
    }

    private bool IsAlive(ParticipantId participantId)
    {
        return TryGetReviveParticipant(participantId, out RaidParticipantReviveSnapshot participant)
            && participant.CombatState == RaidParticipantCombatState.Alive;
    }

    private bool TryGetPlayer(FirstSeveranceRosterMember member, out Player player)
    {
        if (member.ServerWhoAmI >= 0
            && member.ServerWhoAmI < Main.maxPlayers
            && Main.player[member.ServerWhoAmI].active
            && FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(member.ServerWhoAmI, out ulong epoch)
            && epoch == member.ConnectionEpoch)
        {
            player = Main.player[member.ServerWhoAmI];
            return true;
        }

        player = null!;
        return false;
    }

    private static RaidParticipantBinding ToReviveBinding(
        FirstSeveranceRosterMember member)
    {
        return new RaidParticipantBinding(
            member.ParticipantId,
            member.ServerWhoAmI,
            member.ConnectionEpoch);
    }

    private static RaidParticipantReviveSnapshot FindReviveParticipant(
        in RaidReviveSnapshot snapshot,
        ParticipantId participantId)
    {
        for (int index = 0; index < snapshot.Participants.Count; index++)
        {
            if (snapshot.Participants[index].Binding.ParticipantId == participantId)
            {
                return snapshot.Participants[index];
            }
        }

        throw new InvalidOperationException("The revive snapshot lost a roster participant.");
    }

    private static bool IsReviver(
        in RaidReviveSnapshot snapshot,
        ParticipantId participantId)
    {
        for (int index = 0; index < snapshot.Channels.Count; index++)
        {
            if (snapshot.Channels[index].Reviver == participantId)
            {
                return true;
            }
        }

        return false;
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

    private void UpdateScoreHazards(in FirstSeveranceLoopState state, ulong tick)
    {
        if (!FirstSeveranceScoreGeometry.HasHazards(state.Substate) || tick >= state.ResolveTick) return;
        double age = tick - state.SubstateEnteredTick;
        var rays = FirstSeveranceScoreGeometry.Rays(state.Substate, state.ActionIndex, age, groundCenter.X, groundCenter.Y);
        var bullets = state.Substate == FirstSeveranceSubstate.FinalBullets
            ? FirstSeveranceScoreGeometry.Bullets(state.ActionIndex, age, groundCenter.X, groundCenter.Y) : null;
        bool lethal = state.Substate == FirstSeveranceSubstate.RemoteCrush;
        foreach (var ray in rays)
            if (ray.Live && scoreFires.Add(ray.Pulse))
                Log(tick, $"event=ScoreAttackFired boss_phase={state.BossPhase} attack={state.Substate} action={state.ActionIndex} pulse={ray.Pulse} damage_kind={(lethal ? "LethalToDown" : "Fixed")} fixed_damage={(lethal ? 0 : FirstSeveranceScoreGeometry.FixedDamage)}");
        foreach (var member in roster.Members)
        {
            if (!IsAlive(member.ParticipantId) || !TryGetPlayer(member, out Player player)) continue;
            int pulse = -1;
            foreach (var ray in rays)
                if (ray.Live && !scoreHits.Contains((ray.Pulse, member.ParticipantId))
                    && FirstSeveranceScoreGeometry.RayHits(state.Substate, ray, age,
                        player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f))
                { pulse = ray.Pulse; break; }
            if (bullets is not null)
                foreach (var bullet in bullets)
                    if (!scoreHits.Contains((bullet.Wave, member.ParticipantId))
                        && FirstSeveranceScoreGeometry.BulletHits(bullet, player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f))
                    { pulse = bullet.Wave; break; }
            if (pulse < 0) continue;
            scoreHits.Add((pulse, member.ParticipantId));
            ApplyRaidDamage(member, lethal ? Math.Max(player.statLife, player.statLifeMax2)
                : FirstSeveranceScoreGeometry.FixedDamage, tick, state.Substate.ToString());
        }
    }

    private static int CompareIntents(
        FirstSeveranceQueuedCombatIntent left,
        FirstSeveranceQueuedCombatIntent right)
    {
        int kind = left.Kind.CompareTo(right.Kind);
        if (kind != 0)
        {
            return kind;
        }

        int participant = left.Member.ParticipantId.Value.CompareTo(
            right.Member.ParticipantId.Value);
        return participant != 0
            ? participant
            : left.RequestNonce.CompareTo(right.RequestNonce);
    }

    private static int AllocateActorToken()
    {
        nextActorToken = nextActorToken >= 1_000_000 ? 1 : nextActorToken + 1;
        return nextActorToken;
    }

    private void BeginDamageProgress(in FirstSeveranceLoopState state)
    {
        try
        {
            if (state.Substate != FirstSeveranceSubstate.PylonCheck && !FirstSeveranceBossPhasePlan.IsDamageState(state.Substate))
                return;
            damageWindowFloor = state.Substate == FirstSeveranceSubstate.PylonCheck ? 0 : loop!.DamageFloor;
            if (state.Substate != FirstSeveranceSubstate.PylonCheck && state.BossLife <= damageWindowFloor) return;
            damageWindowPhase = state.Substate;
            damageWindowLoop = state.ZeroBasedLoopIndex + 1;
            progressObservationMissing = false;
            ulong open = state.SubstateEnteredTick;
            int life = state.BossLife - damageWindowFloor;
            if (state.Substate == FirstSeveranceSubstate.PylonCheck)
            {
                open += (ulong)plan.Timing.PylonTelegraphTicks;
                progressPylonSlots = pylonNpcIndices.ToArray();
                progressPylonLife = new int[progressPylonSlots.Length];
                Array.Fill(progressPylonLife, partyScaling.PylonLife);
                life = progressPylonSlots.Length * partyScaling.PylonLife;
            }
            damageWindow = new(life, open, state.ResolveTick);
            nextProgressTick = open + 120;
            Log(state.SubstateEnteredTick, $"event=DamageWindowStarted phase={damageWindowPhase} loop={damageWindowLoop} open_tick={open} close_tick={state.ResolveTick} target_hp={life} boss_life={state.BossLife} boss_max_life={state.BossMaximumLife} penalized={state.IsPenalizedExposure}");
        }
        catch (Exception) { damageWindow = null; } // Diagnostics never drive combat.
    }

    private void ObservePylonProgress(ulong tick, bool damageWindowOpen)
    {
        try
        {
            if (damageWindow is null || damageWindowPhase != FirstSeveranceSubstate.PylonCheck || !damageWindowOpen)
                return;
            for (int index = 0; index < progressPylonSlots.Length; index++)
            {
                if (progressPylonLife[index] == 0)
                    continue;
                int slot = progressPylonSlots[index];
                if (killedPylons.Contains(slot))
                {
                    progressPylonLife[index] = 0;
                    Log(tick, $"event=PylonDestroyed loop={damageWindowLoop} pylon={index + 1} open_elapsed_ticks={tick - damageWindow.OpenTick}");
                }
                else if (TryResolveOwnedNpc<FirstSeverancePrototypePylon>(slot, out NPC npc))
                    // Only confirmed OnKill may represent completion; disappearance is not a kill.
                    progressPylonLife[index] = Math.Clamp(npc.life, 1, partyScaling.PylonLife);
                else
                    progressObservationMissing = true;
            }
        }
        catch (Exception) { progressObservationMissing = true; }
    }

    private void PublishDamageProgress(in FirstSeveranceLoopState state, ulong tick)
    {
        try
        {
            if (damageWindow is null)
                return;
            bool gated = damageWindowPhase != FirstSeveranceSubstate.PylonCheck && state.BossLife <= damageWindowFloor;
            bool ended = gated || state.Substate != damageWindowPhase || state.IsTerminal || tick >= damageWindow.CloseTick;
            if (ended || tick >= nextProgressTick)
            {
                int remaining = DamageProgressRemainingLife();
                string outcome = gated ? "HpGateReached" : remaining == 0 ? "Cleared"
                    : state.Substate == FirstSeveranceSubstate.PhaseTransition ? "PhaseTransition"
                    : tick >= damageWindow.CloseTick ? "Deadline" : "Interrupted";
                WriteDamageProgress(tick, ended, ended ? outcome : "Open");
                nextProgressTick = tick + 120;
            }
        }
        catch (Exception) { } // Never change an accepted gameplay transition for logging.
    }

    private int DamageProgressRemainingLife()
    {
        if (FirstSeveranceBossPhasePlan.IsDamageState(damageWindowPhase))
            return Math.Max(0, loop!.State.BossLife - damageWindowFloor);
        int remaining = 0;
        foreach (int life in progressPylonLife)
            remaining += life;
        return remaining;
    }

    private void WriteDamageProgress(ulong tick, bool end, string outcome)
    {
        if (damageWindow is null)
            return;
        FirstSeveranceDamageSample sample = damageWindow.Sample(tick, DamageProgressRemainingLife());
        string required = sample.RequiredDps?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "unavailable";
        string pylons = damageWindowPhase == FirstSeveranceSubstate.PylonCheck
            ? string.Join(",", progressPylonLife) : "none";
        string kind = end ? "DamageWindowEnded" : "DamageProgress";
        Log(tick, FormattableString.Invariant($"event={kind} phase={damageWindowPhase} loop={damageWindowLoop} outcome={outcome} hp_observation={(progressObservationMissing ? "Incomplete" : "Complete")} open_seconds={sample.ElapsedTicks / 60d:F2} remaining_seconds={sample.RemainingTicks / 60d:F2} effective_damage={sample.EffectiveDamage} target_hp_start={damageWindow.StartingLife} target_hp_remaining={sample.RemainingLife} window_progress_pct={sample.ProgressPercent:F2} window_dps={sample.WindowDps:F1} recent_dps={sample.RecentDps:F1} required_dps_by_deadline={required} pylon_hp={pylons} boss_life={loop!.State.BossLife} boss_remaining_pct={100d * loop.State.BossLife / loop.State.BossMaximumLife:F2} overload={loop.State.Overload}"));
        if (!end)
            return;
        if (damageWindowPhase == FirstSeveranceSubstate.PylonCheck)
        {
            totalPylonDamage += sample.EffectiveDamage;
            totalPylonOpenTicks += sample.ElapsedTicks;
        }
        else
        {
            totalCoreDamage += sample.EffectiveDamage;
            totalCoreOpenTicks += sample.ElapsedTicks;
        }
        damageWindow = null; // Final sample and totals are emitted once, including terminal cleanup.
        progressPylonSlots = Array.Empty<int>();
        progressPylonLife = Array.Empty<int>();
    }

    private void FinishDamageProgress(ulong tick, string cause)
    {
        try
        {
            if (!isStarted || loop is null)
                return;
            WriteDamageProgress(tick, end: true, outcome: cause);
            double coreDps = FirstSeveranceDamageWindow.Rate(totalCoreDamage, totalCoreOpenTicks);
            double pylonDps = FirstSeveranceDamageWindow.Rate(totalPylonDamage, totalPylonOpenTicks);
            ulong elapsed = tick >= combatStartedTick ? tick - combatStartedTick : 0;
            Log(tick, FormattableString.Invariant($"event=CombatDpsSummary cause={cause} participants={roster.Count} elapsed_seconds={elapsed / 60d:F2} completed_exposures={loop.State.CompletedExposures} overload={loop.State.Overload} core_effective_damage={totalCoreDamage} core_open_seconds={totalCoreOpenTicks / 60d:F2} core_window_dps={coreDps:F1} pylon_effective_damage={totalPylonDamage} pylon_open_seconds={totalPylonOpenTicks / 60d:F2} pylon_window_dps={pylonDps:F1} boss_life={loop.State.BossLife} boss_max_life={loop.State.BossMaximumLife} boss_remaining_pct={100d * loop.State.BossLife / loop.State.BossMaximumLife:F2} dps_basis=AuthorityNetHpLoss"));
        }
        catch (Exception) { }
        finally
        {
            damageWindow = null;
            progressPylonSlots = Array.Empty<int>();
            progressPylonLife = Array.Empty<int>();
        }
    }

    private void LogReviveEvent(RaidReviveEvent entry)
    {
        Log(entry.AuthorityTick, $"event={entry.Kind} participant={entry.Subject.Value} actor={entry.Actor.Value} cancel={entry.CancelReason} failure={entry.FailureReason} elimination={entry.EliminationReason} recovery_lockout_until={entry.ReviveLockoutUntilTick}");
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
