#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance.Actors;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
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
    private readonly int serverTileEntityId;
    private readonly TilePoint coreTopLeft;
    private readonly FirstSeveranceEncounterPlan plan;
    private readonly FirstSeveranceReviveBoundary revive;
    private readonly uint[] lastQueuedNonces;
    private readonly bool[] observedConnected;
    private readonly RaidParticipantCombatState[] projectedCombatStates;
    private readonly uint[] healthRevisions;
    private readonly int[] correctedLife;
    private readonly Vector2[] downedPositions;
    private readonly Dictionary<ParticipantId, (uint Nonce, Vector2 Position, int Life)> channelObservations = new();
    private readonly HashSet<int> killedPylons = new();
    private readonly List<FirstSeveranceQueuedCombatIntent> pendingIntents = new();
    private readonly List<int> pylonNpcIndices = new();
    private FirstSeveranceLoopStateMachine? loop;
    private int actorToken;
    private int bossNpcIndex = -1;
    private int lastObservedBossLife;
    private int stackTargetSlot = -1;
    private int stackDamagePool;
    private bool bossKilled;
    private bool cancelRequested;
    private FirstSeveranceMechanicResult lastMechanicResult;
    private uint mechanicRevision;
    private bool isAttached;
    private bool isStarted;
    private bool isCleaned;
    private FirstSeveranceLanceVolley? lanceVolley;
    private uint lanceSerial;
    private ulong nextLanceTick;
    private readonly HashSet<ParticipantId> lanceHitParticipants = new();

    internal FirstSeverancePrototypeCombatRuntime(
        ulong encounterSequence,
        FightId fightId,
        FirstSeveranceRoster roster,
        int serverTileEntityId,
        TilePoint coreTopLeft)
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
        this.serverTileEntityId = serverTileEntityId;
        this.coreTopLeft = coreTopLeft;
        plan = FirstSeveranceEncounterPlan.Instance;
        revive = new FirstSeveranceReviveBoundary(fightId, LogReviveEvent);
        lastQueuedNonces = new uint[roster.Count];
        observedConnected = new bool[roster.Count];
        projectedCombatStates = new RaidParticipantCombatState[roster.Count];
        healthRevisions = new uint[roster.Count];
        correctedLife = new int[roster.Count];
        downedPositions = new Vector2[roster.Count];
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
                out _)
            || !FoundationCoreProtectionSystem.TryEnterActive(serverTileEntityId, fightId))
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
            FirstSeverancePrototypeBoss.MaximumLife,
            authorityTick);
        lastObservedBossLife = FirstSeverancePrototypeBoss.MaximumLife;
        int totalLife = 0;
        foreach (FirstSeveranceRosterMember member in roster.Members)
            totalLife += Main.player[member.ServerWhoAmI].statLifeMax2;
        stackDamagePool = Math.Max(100, totalLife / roster.Count * 9 / 10);
        if (!FirstSeveranceCombatAuthority.TryAttach(this))
        {
            failureCode = "first_severance.combat_authority_busy";
            return false;
        }

        isAttached = true;
        isStarted = true;
        GrantPrototypeReviveKits();
        Log(authorityTick, $"event=CombatStarted participants={roster.Count} stack_pool={stackDamagePool}");
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
            && loop.State.Substate == FirstSeveranceSubstate.CoreExposure)
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

        RaidReviveSnapshot? state = revive.CreateSnapshot();
        if (!TryGetPlayer(member, out Player player) || player.dead
            || player.HeldItem.type != ModContent.ItemType<ResuscitationKitItem>()
            || player.mount.Active || !IsAlive(member.ParticipantId))
        {
            failureCode = "first_severance.revive_requires_held_kit_and_alive_sender";
            return false;
        }
        if (state is null || state.Value.RemainingTokenCount <= state.Value.ReservedTokenCount)
        {
            failureCode = "first_severance.revive_no_available_token";
            return false;
        }
        if (IsReviver(state.Value, member.ParticipantId))
        {
            failureCode = "first_severance.revive_already_channeling";
            return false;
        }
        if (!TryFindNearestDowned(member, out _))
        {
            failureCode = "first_severance.revive_no_downed_ally_in_range";
            return false;
        }

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
        InterruptOutOfRangeChannels(context.AuthorityTick);

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
            acceptedBossDamage >= before.BossLife
                || (before.Substate == FirstSeveranceSubstate.PylonCheck
                    && destroyedPylons >= before.RemainingPylons));

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
        if (after.Overload > before.Overload)
        {
            foreach (FirstSeveranceRosterMember member in roster.Members)
            {
                if (TryGetPlayer(member, out Player player))
                    ApplyRaidDamage(member, Math.Min(player.statLife - 1,
                        player.statLifeMax2 / 4), context.AuthorityTick, "PylonPulse");
            }
        }

        InterruptOutOfRangeChannels(context.AuthorityTick);
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

        if (before.Substate != after.Substate
            && !ApplySubstateTransition(before.Substate, after.Substate, context.AuthorityTick))
        {
            return End(FirstSeveranceTerminalCause.RuntimeInvariantBroken);
        }

        if (before.Substate != after.Substate)
            Log(context.AuthorityTick, $"event=PhaseChanged phase={after.Substate} loop={after.ZeroBasedLoopIndex + 1} overload={after.Overload} boss_life={after.BossLife}");

        UpdateDamageWindows(after, context.AuthorityTick);
        SynchronizeBossLife(after);

        bool combatObservable = before.Substate != after.Substate
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
            stackTargetSlot,
            CoreWorldCenter.X,
            CoreWorldCenter.Y,
            lastMechanicResult,
            mechanicRevision,
            Array.AsReadOnly(participants),
            lanceVolley);
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

        CleanupPylons();
        CleanupNpc(bossNpcIndex, ModContent.NPCType<FirstSeverancePrototypeBoss>());
        bossNpcIndex = -1;
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            if (TryGetPlayer(member, out Player player))
            {
                player.GetModPlayer<FirstSeveranceRaidPlayer>().ClearRaidState();
            }
        }

        revive.Cleanup(context);
        channelObservations.Clear();
        lanceVolley = null;
        lanceHitParticipants.Clear();
        nextLanceTick = 0;
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
        var starts = new List<RaidReviveStartCommand>(roster.Count);
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
            else if (intent.Kind == FirstSeveranceCombatIntentKind.ReviveNearest
                && sender.HeldItem.type == ModContent.ItemType<ResuscitationKitItem>()
                && TryFindNearestDowned(intent.Member, out ParticipantId target))
            {
                starts.Add(new RaidReviveStartCommand(
                    fightId,
                    binding,
                    target,
                    intent.RequestNonce,
                    authorityTick));
            }
        }

        if (starts.Count > 0)
        {
            revive.ApplyStartBatch(starts);
        }

        pendingIntents.Clear();
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

    private void InterruptOutOfRangeChannels(ulong authorityTick)
    {
        RaidReviveSnapshot? snapshot = revive.CreateSnapshot();
        if (snapshot is null)
        {
            return;
        }

        float rangeSquared = ReviveRangePixels * ReviveRangePixels;
        for (int index = 0; index < snapshot.Value.Channels.Count; index++)
        {
            RaidReviveChannelSnapshot channel = snapshot.Value.Channels[index];
            if (!roster.TryGet(channel.Reviver, out FirstSeveranceRosterMember reviver))
                continue;
            RaidReviveCancelReason reason = RaidReviveCancelReason.None;
            if (!roster.TryGet(channel.Target, out FirstSeveranceRosterMember target)
                || !TryGetPlayer(reviver, out Player reviverPlayer)
                || !TryGetPlayer(target, out Player targetPlayer)
                || Vector2.DistanceSquared(reviverPlayer.Center, targetPlayer.Center) > rangeSquared)
            {
                reason = RaidReviveCancelReason.ReviverMoved;
            }
            else
            {
                if (!channelObservations.TryGetValue(channel.Reviver, out var observation)
                    || observation.Nonce != channel.ChannelNonce)
                    observation = (channel.ChannelNonce, reviverPlayer.position, reviverPlayer.statLife);
                if (reviverPlayer.statLife < observation.Life)
                    reason = RaidReviveCancelReason.ReviverDamaged;
                else if (Vector2.DistanceSquared(reviverPlayer.position, observation.Position) > 16f * 16f
                    || reviverPlayer.mount.Active || reviverPlayer.grapCount > 0)
                    reason = RaidReviveCancelReason.ReviverMoved;
                else if (reviverPlayer.HeldItem.type != ModContent.ItemType<ResuscitationKitItem>()
                    || (authorityTick > channel.StartedTick + 10
                        && !reviverPlayer.controlUseItem))
                    reason = RaidReviveCancelReason.Manual;
                channelObservations[channel.Reviver] =
                    (observation.Nonce, observation.Position, reviverPlayer.statLife);
            }

            if (reason != RaidReviveCancelReason.None)
            {
                revive.Apply(new RaidReviveInterruptCommand(fightId, ToReviveBinding(reviver),
                    channel.ChannelNonce, reason, authorityTick));
                channelObservations.Remove(channel.Reviver);
            }
        }
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
                && before == RaidParticipantCombatState.Downed)
            {
                int restoredLife = Math.Max(1, (int)Math.Ceiling(player.statLifeMax2 * 0.35f));
                SetCorrectedLife(index, player, restoredLife);
                lifeChanged = true;
            }

            FirstSeveranceCombatParticipantProjection projection =
                CreateParticipantProjection(snapshot.Value, participant, index);
            player.GetModPlayer<FirstSeveranceRaidPlayer>().ApplyProjection(
                fightId, projection, authorityTick);

            if (lifeChanged && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.PlayerLifeMana, number: member.ServerWhoAmI);
            }

            projectedCombatStates[index] = participant.CombatState;
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
            participant.InvulnerabilityUntilTick, participant.WeaknessUntilTick);
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
            int required = plan.StackRequiredShares.GetValue(roster.Count);
            int stacked = 0;
            Vector2 center = stackTargetSlot >= 0 && Main.player[stackTargetSlot].active
                ? Main.player[stackTargetSlot].Center : CoreWorldCenter;
            var occupants = new List<FirstSeveranceRosterMember>(roster.Count);
            float radiusSquared = StackRadiusPixels * StackRadiusPixels;
            for (int index = 0; index < roster.Count; index++)
            {
                FirstSeveranceRosterMember member = roster.Members[index];
                if (IsAlive(member.ParticipantId)
                    && TryGetPlayer(member, out Player player)
                    && Vector2.DistanceSquared(player.Center, center) <= radiusSquared)
                {
                    stacked++;
                    occupants.Add(member);
                }
            }

            SetMechanicResult(stacked >= required
                ? FirstSeveranceMechanicResult.StackPassed
                : FirstSeveranceMechanicResult.StackFailed);
            for (int index = 0; index < occupants.Count; index++)
            {
                int share = stackDamagePool / occupants.Count
                    + (index < stackDamagePool % occupants.Count ? 1 : 0);
                ApplyRaidDamage(occupants[index], share, authorityTick, "Stack");
            }
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

            bool passed = alive.Count > 1;
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
            foreach (FirstSeveranceRosterMember member in roster.Members)
                if (failedSlots.Contains(member.ServerWhoAmI)
                    && TryGetPlayer(member, out Player player))
                    ApplyRaidDamage(member, Math.Max(1, player.statLifeMax2 * 2 / 5), authorityTick, "Spread");
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
            if (state.Substate == FirstSeveranceSubstate.CoreExposure && bossKilled)
            {
                acceptedBossDamage = lastObservedBossLife;
                return true;
            }

            return false;
        }

        int observedLife = Math.Clamp(boss.life, 0, state.BossMaximumLife);
        if (state.Substate == FirstSeveranceSubstate.CoreExposure)
        {
            acceptedBossDamage = Math.Max(0, lastObservedBossLife - observedLife);
        }
        else if (observedLife != state.BossLife)
        {
            boss.life = state.BossLife;
            boss.netUpdate = true;
        }

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
        nextLanceTick = authorityTick + (after == FirstSeveranceSubstate.PylonCheck
            ? (ulong)plan.Timing.PylonTelegraphTicks : 24UL);
        if (before == FirstSeveranceSubstate.PylonCheck)
        {
            CleanupPylons();
        }

        if (after == FirstSeveranceSubstate.PylonCheck)
        {
            return TrySpawnPylons(plan.PylonCount.GetValue(roster.Count), authorityTick);
        }

        stackTargetSlot = -1;
        if (after == FirstSeveranceSubstate.Stack)
        {
            int start = loop!.State.ZeroBasedLoopIndex % roster.Count;
            for (int offset = 0; offset < roster.Count; offset++)
            {
                FirstSeveranceRosterMember member = roster.Members[(start + offset) % roster.Count];
                if (IsAlive(member.ParticipantId) && TryGetPlayer(member, out _))
                {
                    stackTargetSlot = member.ServerWhoAmI;
                    break;
                }
            }
        }

        return true;
    }

    private void UpdateDamageWindows(
        in FirstSeveranceLoopState state,
        ulong authorityTick)
    {
        if (TryResolveOwnedNpc<FirstSeverancePrototypeBoss>(bossNpcIndex, out NPC boss))
        {
            bool shielded = !plan.Boss.CanTakeDamage(state.Substate);
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
            new(-360f, -100f),
            new(360f, -100f),
            new(-530f, -290f),
            new(530f, -290f),
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
        npc.lifeMax = npcType == ModContent.NPCType<FirstSeverancePrototypeBoss>()
            ? FirstSeverancePrototypeBoss.MaximumLife : FirstSeverancePrototypePylon.MaximumLife;
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
        if (lanceVolley is null && tick >= nextLanceTick
            && state.ResolveTick - tick > FirstSeveranceLanceTuning.TelegraphTicks
                + FirstSeveranceLanceTuning.ActiveTicks)
        {
            var targets = new List<Player>(roster.Count);
            int first = (int)(lanceSerial % (uint)roster.Count);
            for (int offset = 0; offset < roster.Count; offset++)
            {
                FirstSeveranceRosterMember member = roster.Members[(first + offset) % roster.Count];
                if (IsAlive(member.ParticipantId) && TryGetPlayer(member, out Player player))
                    targets.Add(player);
            }
            if (targets.Count > 0)
            {
                // Alternate single / double shots, independent of party size.
                int count = Math.Min(targets.Count, (lanceSerial & 1) == 0 ? 1 : 2);
                var rays = new FirstSeveranceLanceRay[count];
                Vector2 origin = CoreWorldCenter
                    - new Vector2(0f, FirstSeveranceLanceTuning.BossHeightAboveCore);
                for (int index = 0; index < count; index++)
                {
                    Vector2 direction = targets[index].Center - origin;
                    if (direction.LengthSquared() < 1f)
                        direction = Vector2.UnitY;
                    direction.Normalize();
                    rays[index] = new FirstSeveranceLanceRay(origin.X, origin.Y, direction.X, direction.Y);
                }
                lanceVolley = new FirstSeveranceLanceVolley(++lanceSerial, tick, Array.AsReadOnly(rays));
                nextLanceTick = tick + FirstSeveranceLanceTuning.CadenceTicks;
                lanceHitParticipants.Clear();
                changed = true; // Publish the locked aim immediately, not on the 30-tick heartbeat.
                Log(tick, $"event=LanceTelegraph cast={lanceSerial} rays={count} fire_tick={lanceVolley.FireTick}");
            }
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
            foreach (FirstSeveranceLanceRay ray in lanceVolley.Rays)
            {
                if (!ray.Intersects(player.Center.X, player.Center.Y, player.width * 0.5f, player.height * 0.5f))
                    continue;
                // One hit per volley even at a crossing or during all 18 live ticks.
                lanceHitParticipants.Add(member.ParticipantId);
                ApplyRaidDamage(member, Math.Max(1, player.statLifeMax2 * 35 / 100), tick, "ObservationLance");
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

    private bool TryFindNearestDowned(
        FirstSeveranceRosterMember reviver,
        out ParticipantId target)
    {
        target = ParticipantId.Invalid;
        if (!TryGetPlayer(reviver, out Player reviverPlayer))
        {
            return false;
        }

        RaidReviveSnapshot? snapshot = revive.CreateSnapshot();
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
            if (distanceSquared <= nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                target = member.ParticipantId;
            }
        }

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

    private Vector2 CoreWorldCenter => new(
        (coreTopLeft.X + 1f) * 16f,
        (coreTopLeft.Y + 1f) * 16f);

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

    private void LogReviveEvent(RaidReviveEvent entry)
    {
        Log(entry.AuthorityTick, $"event={entry.Kind} participant={entry.Subject.Value} actor={entry.Actor.Value} cancel={entry.CancelReason} failure={entry.FailureReason} elimination={entry.EliminationReason}");
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
