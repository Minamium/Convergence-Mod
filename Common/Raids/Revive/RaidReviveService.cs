#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Raids.Revive;

// Pure authoritative Raid domain. Terraria death hooks, controls, packets, and
// presentation are adapters around this service and are intentionally absent here.
internal sealed class RaidReviveService
{
    private sealed class ParticipantState
    {
        public ParticipantState(RaidParticipantBinding binding)
        {
            Binding = binding;
            IsConnected = true;
        }

        public RaidParticipantBinding Binding { get; set; }

        public bool IsConnected { get; set; }

        public RaidParticipantCombatState CombatState { get; set; }

        public ulong DownedDeadlineTick { get; set; }

        public ulong DisconnectDeadlineTick { get; set; }

        public ulong InvulnerabilityUntilTick { get; set; }

        public ulong WeaknessUntilTick { get; set; }

        public ulong ReviveLockoutUntilTick { get; set; }

        public bool HasAcceptedRequestNonce { get; set; }

        public uint LastAcceptedRequestNonce { get; set; }
    }

    private sealed record ReviveChannel(
        ParticipantId Reviver,
        ParticipantId Target,
        uint ChannelNonce,
        ulong StartedTick,
        ulong CompletesTick);

    private readonly FightId fightId;
    private readonly RaidReviveSettings settings;
    private readonly Dictionary<ParticipantId, ParticipantState> participants = new();
    private readonly Dictionary<ParticipantId, ReviveChannel> channelsByReviver = new();
    private int remainingTokenCount;
    private uint revision;
    private ulong lastAuthorityTick;
    private ulong lastCommittedTick;
    private ulong lastProcessedStartBatchTick;
    private RaidReviveFailureReason failureReason;
    private bool hasCommittedTick;
    private bool hasProcessedStartBatch;
    private bool isCleaned;

    public RaidReviveService(
        FightId fightId,
        RaidReviveSettings settings,
        IReadOnlyList<RaidParticipantBinding> roster)
    {
        if (fightId.IsNone)
        {
            throw new ArgumentException("A Raid revive service requires a Fight ID.", nameof(fightId));
        }

        if (!settings.IsValid)
        {
            throw new ArgumentException("Raid revive settings are invalid.", nameof(settings));
        }

        ArgumentNullException.ThrowIfNull(roster);
        if (roster.Count != settings.ParticipantCount)
        {
            throw new ArgumentException(
                "The authoritative roster must match the configured participant count.",
                nameof(roster));
        }

        HashSet<int> playerSlots = new();
        foreach (RaidParticipantBinding binding in roster)
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException("The roster contains an invalid binding.", nameof(roster));
            }

            if (!participants.TryAdd(binding.ParticipantId, new ParticipantState(binding)))
            {
                throw new ArgumentException("Participant IDs must be unique within a Fight.", nameof(roster));
            }

            if (!playerSlots.Add(binding.PlayerSlot))
            {
                throw new ArgumentException("Connected participant slots must be unique.", nameof(roster));
            }
        }

        this.fightId = fightId;
        this.settings = settings;
        remainingTokenCount = settings.InitialTokenCount;
    }

    public FightId FightId => fightId;

    public uint Revision => revision;

    public int RemainingTokenCount => remainingTokenCount;

    public int ReservedTokenCount => settings.UsesSharedTokens ? channelsByReviver.Count : 0;

    public int AvailableTokenCount => Math.Max(0, remainingTokenCount - ReservedTokenCount);

    public RaidReviveFailureReason FailureReason => failureReason;

    public bool IsCleaned => isCleaned;

    public RaidReviveCommandResult Apply(in AuthoritativeParticipantDownedCommand command)
    {
        if (!TryValidateCommand(command.FightId, command.AuthorityTick, out RaidReviveCommandResult rejection))
        {
            return rejection;
        }

        if (!TryResolveConnected(command.Participant, out ParticipantState participant))
        {
            return RaidReviveCommandResult.Rejected("revive.participant_binding_invalid");
        }

        if (participant.CombatState == RaidParticipantCombatState.Downed)
        {
            AcceptCommandTick(command.AuthorityTick);
            return RaidReviveCommandResult.NoChange("revive.participant_already_downed");
        }

        if (participant.CombatState != RaidParticipantCombatState.Alive)
        {
            return RaidReviveCommandResult.Rejected("revive.participant_not_alive");
        }

        AcceptCommandTick(command.AuthorityTick);
        List<RaidReviveEvent> events = new();
        CancelChannelsInvolving(
            command.Participant.ParticipantId,
            RaidReviveCancelReason.ReviverInvalid,
            command.AuthorityTick,
            events);
        participant.CombatState = RaidParticipantCombatState.Downed;
        participant.DownedDeadlineTick = SaturatingAdd(
            command.AuthorityTick,
            settings.DownedTimeoutTicks);
        participant.InvulnerabilityUntilTick = 0;
        participant.WeaknessUntilTick = 0;
        AddEvent(
            events,
            command.AuthorityTick,
            RaidReviveEventKind.ParticipantDowned,
            command.Participant.ParticipantId,
            ParticipantId.Invalid);
        return RaidReviveCommandResult.Applied(events.ToArray());
    }

    // The authority adapter submits the complete bounded ingress set for a tick.
    // Sorting here makes reservation winners independent of packet arrival order.
    public RaidReviveStartBatchResult ApplyStartBatch(
        IReadOnlyList<RaidReviveStartCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        if (commands.Count > settings.ParticipantCount)
        {
            return RaidReviveStartBatchResult.Rejected("revive.start_batch_too_large");
        }

        if (commands.Count == 0)
        {
            return RaidReviveStartBatchResult.Rejected("revive.start_batch_empty");
        }

        FightId batchFightId = commands[0].FightId;
        ulong batchTick = commands[0].AuthorityTick;
        HashSet<ParticipantId> batchRevivers = new();
        for (int index = 0; index < commands.Count; index++)
        {
            RaidReviveStartCommand command = commands[index];
            if (command.FightId != fightId || command.FightId != batchFightId)
            {
                return RaidReviveStartBatchResult.Rejected(
                    "revive.start_batch_fight_id_mismatch");
            }

            if (command.AuthorityTick != batchTick)
            {
                return RaidReviveStartBatchResult.Rejected(
                    "revive.start_batch_tick_mismatch");
            }

            if (!batchRevivers.Add(command.Reviver.ParticipantId))
            {
                return RaidReviveStartBatchResult.Rejected(
                    "revive.start_batch_duplicate_reviver");
            }
        }

        if (hasProcessedStartBatch)
        {
            if (batchTick == lastProcessedStartBatchTick)
            {
                return RaidReviveStartBatchResult.Rejected(
                    "revive.start_batch_already_processed");
            }

            if (batchTick < lastProcessedStartBatchTick)
            {
                return RaidReviveStartBatchResult.Rejected(
                    "revive.start_batch_tick_stale");
            }
        }

        if (!TryValidateCommand(
                batchFightId,
                batchTick,
                out RaidReviveCommandResult envelopeRejection))
        {
            return RaidReviveStartBatchResult.Rejected(
                envelopeRejection.FailureCode);
        }

        RaidReviveStartCommand[] orderedCommands =
            new RaidReviveStartCommand[commands.Count];
        for (int index = 0; index < commands.Count; index++)
        {
            orderedCommands[index] = commands[index];
        }

        Array.Sort(orderedCommands, CompareStartCommands);
        RaidReviveCommandResult[] results =
            new RaidReviveCommandResult[orderedCommands.Length];
        bool hasAcceptedStart = false;
        for (int index = 0; index < orderedCommands.Length; index++)
        {
            results[index] = ApplySingleStart(orderedCommands[index]);
            hasAcceptedStart |= results[index].IsAccepted;
        }

        // A batch containing only rejected commands cannot advance either clock.
        // Single-threaded authority execution makes this marker atomic with the
        // successful adjudication above; no adapter callback is invoked mid-batch.
        if (hasAcceptedStart)
        {
            hasProcessedStartBatch = true;
            lastProcessedStartBatchTick = batchTick;
        }

        return RaidReviveStartBatchResult.Accepted(results);
    }

    private RaidReviveCommandResult ApplySingleStart(in RaidReviveStartCommand command)
    {
        if (!TryValidateCommand(command.FightId, command.AuthorityTick, out RaidReviveCommandResult rejection))
        {
            return rejection;
        }

        if (!TryResolveConnected(command.Reviver, out ParticipantState reviver))
        {
            return RaidReviveCommandResult.Rejected("revive.reviver_binding_invalid");
        }

        if (reviver.CombatState != RaidParticipantCombatState.Alive)
        {
            return RaidReviveCommandResult.Rejected("revive.reviver_not_alive");
        }

        if (!command.Target.IsValid || command.Target == command.Reviver.ParticipantId)
        {
            return RaidReviveCommandResult.Rejected("revive.target_invalid");
        }

        if (!participants.TryGetValue(command.Target, out ParticipantState? target)
            || !target.IsConnected
            || target.CombatState != RaidParticipantCombatState.Downed
            || target.DownedDeadlineTick <= command.AuthorityTick)
        {
            return RaidReviveCommandResult.Rejected("revive.target_not_revivable");
        }

        if (channelsByReviver.ContainsKey(command.Reviver.ParticipantId))
        {
            return RaidReviveCommandResult.Rejected("revive.reviver_already_channeling");
        }

        if (command.AuthorityTick < target.ReviveLockoutUntilTick)
        {
            return RaidReviveCommandResult.Rejected("revive.target_recovery_locked");
        }

        if (IsTargetReserved(command.Target))
        {
            return RaidReviveCommandResult.Rejected("revive.target_already_reserved");
        }

        if (settings.UsesSharedTokens && AvailableTokenCount <= 0)
        {
            return RaidReviveCommandResult.Rejected("revive.no_available_token");
        }

        if (command.RequestNonce == 0
            || (reviver.HasAcceptedRequestNonce
                && !IsNewerSerial(command.RequestNonce, reviver.LastAcceptedRequestNonce)))
        {
            return RaidReviveCommandResult.Rejected("revive.stale_request_nonce");
        }

        AcceptCommandTick(command.AuthorityTick);
        reviver.HasAcceptedRequestNonce = true;
        reviver.LastAcceptedRequestNonce = command.RequestNonce;
        ReviveChannel channel = new(
            command.Reviver.ParticipantId,
            command.Target,
            command.RequestNonce,
            command.AuthorityTick,
            SaturatingAdd(command.AuthorityTick, settings.ChannelDurationTicks));
        channelsByReviver.Add(channel.Reviver, channel);

        List<RaidReviveEvent> events = new();
        AddEvent(
            events,
            command.AuthorityTick,
            settings.ChannelDurationTicks == 0
                ? RaidReviveEventKind.InstantReviveAccepted : RaidReviveEventKind.ChannelStarted,
            channel.Target,
            channel.Reviver,
            channelNonce: channel.ChannelNonce);
        return RaidReviveCommandResult.Applied(events.ToArray());
    }

    public RaidReviveCommandResult Apply(in RaidReviveInterruptCommand command)
    {
        if (!TryValidateCommand(command.FightId, command.AuthorityTick, out RaidReviveCommandResult rejection))
        {
            return rejection;
        }

        if (!TryResolveConnected(command.Reviver, out _))
        {
            return RaidReviveCommandResult.Rejected("revive.reviver_binding_invalid");
        }

        if (!Enum.IsDefined(command.Reason)
            || command.Reason is RaidReviveCancelReason.None or RaidReviveCancelReason.EncounterEnded)
        {
            return RaidReviveCommandResult.Rejected("revive.cancel_reason_invalid");
        }

        if (command.ExpectedChannelNonce == 0)
        {
            return RaidReviveCommandResult.Rejected("revive.channel_nonce_stale");
        }

        if (!channelsByReviver.TryGetValue(
                command.Reviver.ParticipantId,
                out ReviveChannel? channel))
        {
            return RaidReviveCommandResult.NoChange("revive.channel_not_active");
        }

        if (command.ExpectedChannelNonce != channel.ChannelNonce)
        {
            return RaidReviveCommandResult.Rejected("revive.channel_nonce_stale");
        }

        AcceptCommandTick(command.AuthorityTick);
        channelsByReviver.Remove(command.Reviver.ParticipantId);

        List<RaidReviveEvent> events = new();
        AddCancellationEvent(events, channel, command.Reason, command.AuthorityTick);
        return RaidReviveCommandResult.Applied(events.ToArray());
    }

    public RaidReviveCommandResult Apply(in RaidParticipantDisconnectedCommand command)
    {
        if (!TryValidateCommand(command.FightId, command.AuthorityTick, out RaidReviveCommandResult rejection))
        {
            return rejection;
        }

        if (!TryResolveConnected(command.Participant, out ParticipantState participant))
        {
            return RaidReviveCommandResult.Rejected("revive.participant_binding_invalid");
        }

        AcceptCommandTick(command.AuthorityTick);
        List<RaidReviveEvent> events = new();
        CancelChannelsInvolving(
            command.Participant.ParticipantId,
            RaidReviveCancelReason.ParticipantDisconnected,
            command.AuthorityTick,
            events);
        participant.IsConnected = false;
        participant.DisconnectDeadlineTick = SaturatingAdd(
            command.AuthorityTick,
            settings.DisconnectGraceTicks);
        AddEvent(
            events,
            command.AuthorityTick,
            RaidReviveEventKind.ParticipantDisconnected,
            command.Participant.ParticipantId,
            ParticipantId.Invalid);
        return RaidReviveCommandResult.Applied(events.ToArray());
    }

    public RaidReviveCommandResult Apply(in RaidParticipantRejoinedCommand command)
    {
        if (!TryValidateCommand(command.FightId, command.AuthorityTick, out RaidReviveCommandResult rejection))
        {
            return rejection;
        }

        if (!command.ParticipantId.IsValid
            || command.NewPlayerSlot is < 0 or >= RaidParticipantBinding.MaximumPlayerSlotExclusive
            || command.NewConnectionEpoch == 0)
        {
            return RaidReviveCommandResult.Rejected("revive.rejoin_binding_invalid");
        }

        if (!participants.TryGetValue(command.ParticipantId, out ParticipantState? participant))
        {
            return RaidReviveCommandResult.Rejected("revive.participant_unknown");
        }

        if (!participant.IsConnected
            && (participant.DisconnectDeadlineTick == 0
                || participant.DisconnectDeadlineTick <= command.AuthorityTick))
        {
            return RaidReviveCommandResult.Rejected("revive.rejoin_grace_expired");
        }

        if (participant.IsConnected
            && participant.Binding.PlayerSlot == command.NewPlayerSlot
            && participant.Binding.ConnectionEpoch == command.NewConnectionEpoch)
        {
            AcceptCommandTick(command.AuthorityTick);
            return RaidReviveCommandResult.NoChange("revive.rejoin_already_applied");
        }

        if (command.NewConnectionEpoch <= participant.Binding.ConnectionEpoch)
        {
            return RaidReviveCommandResult.Rejected("revive.rejoin_epoch_stale");
        }

        foreach (ParticipantState candidate in participants.Values)
        {
            if (!ReferenceEquals(candidate, participant)
                && candidate.IsConnected
                && candidate.Binding.PlayerSlot == command.NewPlayerSlot)
            {
                return RaidReviveCommandResult.Rejected("revive.rejoin_slot_in_use");
            }
        }

        AcceptCommandTick(command.AuthorityTick);
        List<RaidReviveEvent> events = new();
        if (participant.IsConnected)
        {
            // A newer authority epoch can supersede a connection even if an older
            // disconnect callback was lost or reordered. Channels never migrate.
            CancelChannelsInvolving(
                command.ParticipantId,
                RaidReviveCancelReason.ParticipantDisconnected,
                command.AuthorityTick,
                events);
        }

        participant.Binding = new RaidParticipantBinding(
            command.ParticipantId,
            command.NewPlayerSlot,
            command.NewConnectionEpoch);
        participant.IsConnected = true;
        participant.DisconnectDeadlineTick = 0;
        participant.HasAcceptedRequestNonce = false;
        participant.LastAcceptedRequestNonce = 0;

        AddEvent(
            events,
            command.AuthorityTick,
            RaidReviveEventKind.ParticipantRejoined,
            command.ParticipantId,
            ParticipantId.Invalid);
        return RaidReviveCommandResult.Applied(events.ToArray());
    }

    // This is the only terminal-decision boundary. Authority adapters apply every
    // command for a tick first, then commit that tick exactly once.
    public RaidReviveCommandResult CommitTick(FightId expectedFightId, ulong authorityTick)
    {
        if (!TryEnterCommit(expectedFightId, authorityTick, out RaidReviveCommandResult rejection))
        {
            return rejection;
        }

        List<RaidReviveEvent> events = new();
        ProcessInvalidChannels(authorityTick, events);
        ProcessCompletedChannels(authorityTick, events);
        ProcessDownedTimeouts(authorityTick, events);
        if (failureReason == RaidReviveFailureReason.None)
        {
            ProcessDisconnectedTimeouts(authorityTick, events);
            EvaluateFailure(authorityTick, events);
        }

        return events.Count == 0
            ? RaidReviveCommandResult.NoChange("revive.no_state_change")
            : RaidReviveCommandResult.Applied(events.ToArray());
    }

    public bool TryGetControlProjection(
        in RaidParticipantBinding binding,
        ulong authorityTick,
        out RaidParticipantControlProjection projection)
    {
        if (isCleaned
            || !TryResolveConnected(binding, out ParticipantState participant))
        {
            projection = default;
            return false;
        }

        bool isDowned = participant.CombatState == RaidParticipantCombatState.Downed;
        bool isEliminated = participant.CombatState == RaidParticipantCombatState.Eliminated;
        bool isIncapacitated = isDowned || isEliminated;
        bool isReviving = channelsByReviver.ContainsKey(binding.ParticipantId);
        projection = new RaidParticipantControlProjection(
            IsDowned: isDowned,
            IsEliminated: isEliminated,
            IsReviving: isReviving,
            SuppressMovement: isIncapacitated,
            SuppressItemUse: isIncapacitated || isReviving,
            SuppressCombat: isIncapacitated || isReviving,
            SuppressIncomingDamage: isIncapacitated
                || authorityTick < participant.InvulnerabilityUntilTick,
            HasReviveWeakness: authorityTick < participant.WeaknessUntilTick);
        return true;
    }

    public RaidReviveSnapshot CreateSnapshot()
    {
        RaidParticipantReviveSnapshot[] participantSnapshots =
            new RaidParticipantReviveSnapshot[participants.Count];
        int participantIndex = 0;
        foreach (KeyValuePair<ParticipantId, ParticipantState> pair in SortedParticipants())
        {
            ParticipantState participant = pair.Value;
            participantSnapshots[participantIndex++] = new RaidParticipantReviveSnapshot(
                participant.Binding,
                participant.IsConnected,
                participant.CombatState,
                participant.DownedDeadlineTick,
                participant.DisconnectDeadlineTick,
                participant.InvulnerabilityUntilTick,
                participant.WeaknessUntilTick,
                participant.ReviveLockoutUntilTick);
        }

        RaidReviveChannelSnapshot[] channelSnapshots =
            new RaidReviveChannelSnapshot[channelsByReviver.Count];
        int channelIndex = 0;
        foreach (ReviveChannel channel in SortedChannels())
        {
            channelSnapshots[channelIndex++] = new RaidReviveChannelSnapshot(
                channel.Reviver,
                channel.Target,
                channel.ChannelNonce,
                channel.StartedTick,
                channel.CompletesTick);
        }

        return new RaidReviveSnapshot(
            fightId,
            revision,
            settings.InitialTokenCount,
            remainingTokenCount,
            ReservedTokenCount,
            failureReason,
            participantSnapshots,
            channelSnapshots);
    }

    public bool TryCleanup(FightId expectedFightId)
    {
        if (expectedFightId != fightId)
        {
            return false;
        }

        if (isCleaned)
        {
            return true;
        }

        channelsByReviver.Clear();
        participants.Clear();
        remainingTokenCount = 0;
        isCleaned = true;
        return true;
    }

    private bool TryValidateCommand(
        FightId expectedFightId,
        ulong authorityTick,
        out RaidReviveCommandResult rejection)
    {
        if (isCleaned)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.service_cleaned");
            return false;
        }

        if (expectedFightId != fightId)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.fight_id_stale");
            return false;
        }

        if (authorityTick < lastAuthorityTick)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.authority_tick_stale");
            return false;
        }

        if (hasCommittedTick && authorityTick <= lastCommittedTick)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.authority_tick_already_committed");
            return false;
        }

        if (failureReason != RaidReviveFailureReason.None)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.raid_already_failed");
            return false;
        }

        rejection = default;
        return true;
    }

    private static int CompareStartCommands(
        RaidReviveStartCommand left,
        RaidReviveStartCommand right)
    {
        int comparison = left.AuthorityTick.CompareTo(right.AuthorityTick);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Reviver.ParticipantId.Value.CompareTo(
            right.Reviver.ParticipantId.Value);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Target.Value.CompareTo(right.Target.Value);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.RequestNonce.CompareTo(right.RequestNonce);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Reviver.PlayerSlot.CompareTo(right.Reviver.PlayerSlot);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Reviver.ConnectionEpoch.CompareTo(
            right.Reviver.ConnectionEpoch);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.FightId.Value.CompareTo(right.FightId.Value);
    }

    private void AcceptCommandTick(ulong authorityTick)
    {
        lastAuthorityTick = authorityTick;
    }

    private bool TryEnterCommit(
        FightId expectedFightId,
        ulong authorityTick,
        out RaidReviveCommandResult rejection)
    {
        if (isCleaned)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.service_cleaned");
            return false;
        }

        if (expectedFightId != fightId)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.fight_id_stale");
            return false;
        }

        if (authorityTick < lastAuthorityTick)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.authority_tick_stale");
            return false;
        }

        if (hasCommittedTick && authorityTick <= lastCommittedTick)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.authority_tick_already_committed");
            return false;
        }

        if (failureReason != RaidReviveFailureReason.None)
        {
            rejection = RaidReviveCommandResult.Rejected("revive.raid_already_failed");
            return false;
        }

        lastAuthorityTick = authorityTick;
        lastCommittedTick = authorityTick;
        hasCommittedTick = true;
        rejection = default;
        return true;
    }

    private bool TryResolveConnected(
        in RaidParticipantBinding binding,
        out ParticipantState participant)
    {
        if (binding.IsValid
            && participants.TryGetValue(binding.ParticipantId, out ParticipantState? candidate)
            && candidate.IsConnected
            && candidate.Binding == binding)
        {
            participant = candidate;
            return true;
        }

        participant = null!;
        return false;
    }

    private void ProcessInvalidChannels(ulong authorityTick, List<RaidReviveEvent> events)
    {
        foreach (ReviveChannel channel in SortedChannels())
        {
            if (!participants.TryGetValue(channel.Reviver, out ParticipantState? reviver)
                || !reviver.IsConnected
                || reviver.CombatState != RaidParticipantCombatState.Alive)
            {
                channelsByReviver.Remove(channel.Reviver);
                AddCancellationEvent(
                    events,
                    channel,
                    RaidReviveCancelReason.ReviverInvalid,
                    authorityTick);
                continue;
            }

            if (!participants.TryGetValue(channel.Target, out ParticipantState? target)
                || !target.IsConnected
                || target.CombatState != RaidParticipantCombatState.Downed
                || target.DownedDeadlineTick <= authorityTick)
            {
                channelsByReviver.Remove(channel.Reviver);
                AddCancellationEvent(
                    events,
                    channel,
                    RaidReviveCancelReason.TargetInvalid,
                    authorityTick);
            }
        }
    }

    private void ProcessCompletedChannels(ulong authorityTick, List<RaidReviveEvent> events)
    {
        foreach (ReviveChannel channel in SortedChannels())
        {
            if (channel.CompletesTick > authorityTick)
            {
                continue;
            }

            if (!channelsByReviver.Remove(channel.Reviver)
                || !participants.TryGetValue(channel.Target, out ParticipantState? target)
                || target.CombatState != RaidParticipantCombatState.Downed
                || !target.IsConnected
                || authorityTick < target.ReviveLockoutUntilTick
                || (settings.UsesSharedTokens && remainingTokenCount <= 0))
            {
                continue;
            }

            if (settings.UsesSharedTokens)
                remainingTokenCount--;
            target.CombatState = RaidParticipantCombatState.Alive;
            target.DownedDeadlineTick = 0;
            target.InvulnerabilityUntilTick = SaturatingAdd(
                authorityTick,
                settings.InvulnerabilityTicks);
            target.WeaknessUntilTick = SaturatingAdd(authorityTick, settings.WeaknessTicks);
            target.ReviveLockoutUntilTick = settings.ReviveLockoutTicks == 0
                ? 0 : SaturatingAdd(authorityTick, settings.ReviveLockoutTicks);
            AddEvent(
                events,
                authorityTick,
                RaidReviveEventKind.ParticipantRevived,
                channel.Target,
                channel.Reviver,
                channelNonce: channel.ChannelNonce,
                restoredLifeRatio: settings.RestoredLifeRatio,
                invulnerabilityUntilTick: target.InvulnerabilityUntilTick,
                weaknessUntilTick: target.WeaknessUntilTick,
                reviveLockoutUntilTick: target.ReviveLockoutUntilTick);
        }
    }

    private void ProcessDownedTimeouts(ulong authorityTick, List<RaidReviveEvent> events)
    {
        foreach (KeyValuePair<ParticipantId, ParticipantState> pair in SortedParticipants())
        {
            ParticipantState participant = pair.Value;
            if (participant.CombatState != RaidParticipantCombatState.Downed
                || participant.DownedDeadlineTick > authorityTick)
            {
                continue;
            }

            CancelChannelsInvolving(
                pair.Key,
                RaidReviveCancelReason.TargetInvalid,
                authorityTick,
                events);
            if (settings.UsesSharedTokens && AvailableTokenCount <= 0)
            {
                SetFailure(
                    RaidReviveFailureReason.DownedTimeoutWithoutToken,
                    authorityTick,
                    events);
                return;
            }

            participant.CombatState = RaidParticipantCombatState.Eliminated;
            participant.DownedDeadlineTick = 0;
            AddEvent(
                events,
                authorityTick,
                RaidReviveEventKind.ParticipantEliminated,
                pair.Key,
                ParticipantId.Invalid,
                eliminationReason: RaidParticipantEliminationReason.DownedTimeout);
        }
    }

    private void ProcessDisconnectedTimeouts(
        ulong authorityTick,
        List<RaidReviveEvent> events)
    {
        foreach (KeyValuePair<ParticipantId, ParticipantState> pair in SortedParticipants())
        {
            ParticipantState participant = pair.Value;
            if (participant.IsConnected
                || participant.CombatState == RaidParticipantCombatState.Eliminated
                || participant.DisconnectDeadlineTick == 0
                || participant.DisconnectDeadlineTick > authorityTick)
            {
                continue;
            }

            participant.CombatState = RaidParticipantCombatState.Eliminated;
            participant.DownedDeadlineTick = 0;
            participant.DisconnectDeadlineTick = 0;
            participant.InvulnerabilityUntilTick = 0;
            participant.WeaknessUntilTick = 0;
            AddEvent(
                events,
                authorityTick,
                RaidReviveEventKind.ParticipantEliminated,
                pair.Key,
                ParticipantId.Invalid,
                eliminationReason: RaidParticipantEliminationReason.DisconnectGraceExpired);
        }
    }

    private void EvaluateFailure(ulong authorityTick, List<RaidReviveEvent> events)
    {
        if (failureReason != RaidReviveFailureReason.None || participants.Count == 0)
        {
            return;
        }

        bool allDowned = true;
        bool anyAvailableOrWithinReconnectGrace = false;
        foreach (ParticipantState participant in participants.Values)
        {
            allDowned &= participant.CombatState == RaidParticipantCombatState.Downed;
            anyAvailableOrWithinReconnectGrace |= participant.CombatState
                    == RaidParticipantCombatState.Alive
                && (participant.IsConnected
                    || participant.DisconnectDeadlineTick > authorityTick);
        }

        if (allDowned)
        {
            SetFailure(RaidReviveFailureReason.AllParticipantsDowned, authorityTick, events);
        }
        else if (!anyAvailableOrWithinReconnectGrace)
        {
            SetFailure(RaidReviveFailureReason.NoAvailableParticipants, authorityTick, events);
        }
    }

    private void SetFailure(
        RaidReviveFailureReason reason,
        ulong authorityTick,
        List<RaidReviveEvent> events)
    {
        if (failureReason != RaidReviveFailureReason.None)
        {
            return;
        }

        foreach (ReviveChannel channel in SortedChannels())
        {
            channelsByReviver.Remove(channel.Reviver);
            AddCancellationEvent(
                events,
                channel,
                RaidReviveCancelReason.EncounterEnded,
                authorityTick);
        }

        failureReason = reason;
        AddEvent(
            events,
            authorityTick,
            RaidReviveEventKind.RaidFailed,
            ParticipantId.Invalid,
            ParticipantId.Invalid,
            failureReason: reason);
    }

    private void CancelChannelsInvolving(
        ParticipantId participantId,
        RaidReviveCancelReason reason,
        ulong authorityTick,
        List<RaidReviveEvent> events)
    {
        foreach (ReviveChannel channel in SortedChannels())
        {
            if (channel.Reviver != participantId && channel.Target != participantId)
            {
                continue;
            }

            channelsByReviver.Remove(channel.Reviver);
            AddCancellationEvent(events, channel, reason, authorityTick);
        }
    }

    private bool IsTargetReserved(ParticipantId target)
    {
        foreach (ReviveChannel channel in channelsByReviver.Values)
        {
            if (channel.Target == target)
            {
                return true;
            }
        }

        return false;
    }

    private void AddCancellationEvent(
        List<RaidReviveEvent> events,
        ReviveChannel channel,
        RaidReviveCancelReason reason,
        ulong authorityTick)
    {
        AddEvent(
            events,
            authorityTick,
            RaidReviveEventKind.ChannelCancelled,
            channel.Target,
            channel.Reviver,
            channelNonce: channel.ChannelNonce,
            cancelReason: reason);
    }

    private void AddEvent(
        List<RaidReviveEvent> events,
        ulong authorityTick,
        RaidReviveEventKind kind,
        ParticipantId subject,
        ParticipantId actor,
        uint channelNonce = 0,
        RaidReviveCancelReason cancelReason = RaidReviveCancelReason.None,
        RaidReviveFailureReason failureReason = RaidReviveFailureReason.None,
        RaidParticipantEliminationReason eliminationReason = RaidParticipantEliminationReason.None,
        float restoredLifeRatio = 0f,
        ulong invulnerabilityUntilTick = 0,
        ulong weaknessUntilTick = 0,
        ulong reviveLockoutUntilTick = 0)
    {
        if (revision < uint.MaxValue)
        {
            revision++;
        }

        events.Add(new RaidReviveEvent(
            fightId,
            revision,
            authorityTick,
            kind,
            subject,
            actor,
            channelNonce,
            cancelReason,
            failureReason,
            eliminationReason,
            restoredLifeRatio,
            invulnerabilityUntilTick,
            weaknessUntilTick,
            reviveLockoutUntilTick));
    }

    private IEnumerable<KeyValuePair<ParticipantId, ParticipantState>> SortedParticipants()
    {
        List<KeyValuePair<ParticipantId, ParticipantState>> sorted = new(participants);
        sorted.Sort(static (left, right) => left.Key.Value.CompareTo(right.Key.Value));
        return sorted;
    }

    private IEnumerable<ReviveChannel> SortedChannels()
    {
        List<ReviveChannel> sorted = new(channelsByReviver.Values);
        sorted.Sort(static (left, right) =>
        {
            int tickComparison = left.CompletesTick.CompareTo(right.CompletesTick);
            return tickComparison != 0
                ? tickComparison
                : left.Reviver.Value.CompareTo(right.Reviver.Value);
        });
        return sorted;
    }

    private static bool IsNewerSerial(uint candidate, uint current)
    {
        return candidate != current && unchecked((int)(candidate - current)) > 0;
    }

    private static ulong SaturatingAdd(ulong value, ulong increment)
    {
        return ulong.MaxValue - value < increment ? ulong.MaxValue : value + increment;
    }
}
