#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Compatibility.Calamity;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Replication;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Token scaling")]
    private static void TokenScaling()
    {
        for (int participantCount = 2; participantCount <= 4; participantCount++)
        {
            TestContext context = CreateContext(participantCount);
            int expectedTokens = participantCount - 1;
            AssertEqual(expectedTokens, context.Service.RemainingTokenCount, "remaining token count");
            AssertEqual(
                expectedTokens,
                context.Service.CreateSnapshot().InitialTokenCount,
                "snapshot initial token count");
        }
    }

    [DomainTest("Instant unlimited revive commits in the request tick")]
    private static void InstantUnlimitedRevive()
    {
        TestContext context = CreateContext(2, RaidReviveSettings.CreateInstantUnlimited(2));
        AssertApplied(Down(context, 0, 10));
        AssertApplied(StartRevive(context, 1, 0, 1, 10));
        RaidReviveCommandResult commit = context.Service.CommitTick(context.FightId, 10);
        AssertApplied(commit);
        AssertEqual(1, commit.Events.Count, "one instant completion");
        AssertEqual(RaidReviveEventKind.ParticipantRevived, commit.Events[0].Kind, "completion event");
        RaidParticipantReviveSnapshot target = ParticipantSnapshot(context, 0);
        AssertEqual(RaidParticipantCombatState.Alive, target.CombatState, "alive in request tick");
        AssertEqual(3_610ul, target.ReviveLockoutUntilTick, "sixty-second authority deadline");
        AssertEqual(190ul, target.InvulnerabilityUntilTick, "three-second immunity retained");
        AssertEqual(0, context.Service.RemainingTokenCount, "no artificial unlimited-token balance");
        AssertEqual(0, context.Service.ReservedTokenCount, "no shared token reservation");
        AssertEqual(0, context.Service.CreateSnapshot().Channels.Count, "no held-use lease remains");
    }

    [DomainTest("Recipient lockout expires at exactly sixty seconds")]
    private static void RecipientLockoutDeadline()
    {
        TestContext context = CreateContext(2, RaidReviveSettings.CreateInstantUnlimited(2));
        AssertApplied(Down(context, 0, 1));
        AssertApplied(StartRevive(context, 1, 0, 1, 2));
        AssertApplied(context.Service.CommitTick(context.FightId, 2));
        AssertApplied(Down(context, 0, 3_590));
        AssertEqual(3_602ul, ParticipantSnapshot(context, 0).ReviveLockoutUntilTick,
            "Down must not erase the recipient lockout");
        RaidReviveCommandResult early = StartRevive(context, 1, 0, 2, 3_601);
        AssertEqual("revive.target_recovery_locked", early.FailureCode, "one tick early rejected");
        AssertApplied(StartRevive(context, 1, 0, 2, 3_602));
        AssertApplied(context.Service.CommitTick(context.FightId, 3_602));
        AssertEqual(7_202ul, ParticipantSnapshot(context, 0).ReviveLockoutUntilTick, "new lockout");
        AssertApplied(Down(context, 0, 7_190));
        AssertApplied(StartRevive(context, 1, 0, 3, 7_202));
        AssertApplied(context.Service.CommitTick(context.FightId, 7_202));
        AssertEqual(0, context.Service.RemainingTokenCount, "repeated revives never spend tokens");
    }

    [DomainTest("Locked recipient may still revive another ally")]
    private static void LockoutDoesNotBlockRescuer()
    {
        TestContext context = CreateContext(3, RaidReviveSettings.CreateInstantUnlimited(3));
        AssertApplied(Down(context, 0, 1));
        AssertApplied(StartRevive(context, 1, 0, 1, 2));
        AssertApplied(context.Service.CommitTick(context.FightId, 2));
        AssertApplied(Down(context, 2, 3));
        AssertApplied(StartRevive(context, 0, 2, 1, 3));
        AssertApplied(context.Service.CommitTick(context.FightId, 3));
        AssertEqual(RaidParticipantCombatState.Alive, ParticipantSnapshot(context, 2).CombatState,
            "recently revived participant can rescue someone else");
        AssertEqual(0ul, ParticipantSnapshot(context, 1).ReviveLockoutUntilTick,
            "using the kit does not give the rescuer a lockout");
    }

    [DomainTest("Instant revival race has one winner")]
    private static void InstantRevivalRace()
    {
        TestContext context = CreateContext(3, RaidReviveSettings.CreateInstantUnlimited(3));
        AssertApplied(Down(context, 2, 1));
        RaidReviveStartBatchResult batch = context.Service.ApplyStartBatch(new[]
        {
            CreateStartCommand(context, 1, 2, 1, 2),
            CreateStartCommand(context, 0, 2, 1, 2),
        });
        AssertAccepted(batch);
        AssertApplied(batch.CommandResults[0]);
        AssertEqual("revive.target_already_reserved", batch.CommandResults[1].FailureCode,
            "second rescuer does not duplicate the revive");
        RaidReviveCommandResult commit = context.Service.CommitTick(context.FightId, 2);
        AssertEqual(1, commit.Events.Count, "one completion");
        AssertEqual(context.Roster[0].ParticipantId, commit.Events[0].Actor,
            "stable ID wins rather than packet arrival order");
    }

    [DomainTest("Untimed Down survives lockout, remains rescuable and cleans up")]
    private static void UnlimitedRecoveryTimeoutAndCleanup()
    {
        TestContext context = CreateContext(2, RaidReviveSettings.CreateInstantUnlimited(2));
        AssertApplied(Down(context, 0, 1));
        AssertApplied(StartRevive(context, 1, 0, 1, 2));
        AssertApplied(context.Service.CommitTick(context.FightId, 2));
        AssertApplied(Down(context, 0, 200));
        AssertEqual(RaidReviveCommandDisposition.NoChange, context.Service.CommitTick(context.FightId, 2_000).Disposition,
            "former timeout no longer creates a state transition");
        AssertEqual(RaidParticipantCombatState.Downed, ParticipantSnapshot(context, 0).CombatState,
            "Down remains rescuable after the former thirty-second deadline");
        AssertEqual(0ul, ParticipantSnapshot(context, 0).DownedDeadlineTick, "zero means no Down expiry");
        AssertEqual("revive.target_recovery_locked", StartRevive(context, 1, 0, 2, 3_601).FailureCode,
            "recipient restriction still applies one tick before eligibility");
        AssertEqual(RaidReviveCommandDisposition.NoChange, context.Service.CommitTick(context.FightId, 3_601).Disposition,
            "waiting while locked causes no elimination event");
        AssertApplied(StartRevive(context, 1, 0, 2, 3_602));
        AssertApplied(context.Service.CommitTick(context.FightId, 3_602));
        AssertEqual(RaidParticipantCombatState.Alive, ParticipantSnapshot(context, 0).CombatState,
            "rescued after outlasting lockout and the former elimination time");
        AssertApplied(Down(context, 0, 3_800));
        AssertEqual(RaidReviveCommandDisposition.NoChange, context.Service.CommitTick(context.FightId, 36_000).Disposition,
            "long wait remains event-free");
        AssertEqual(RaidParticipantCombatState.Downed, ParticipantSnapshot(context, 0).CombatState,
            "an arbitrarily long Down has no elimination");
        AssertEqual(RaidReviveFailureReason.None, context.Service.FailureReason,
            "zero legacy tokens do not create an unlimited-mode timeout wipe");
        AssertApplied(Down(context, 1, 36_001));
        AssertApplied(context.Service.CommitTick(context.FightId, 36_001));
        AssertEqual(RaidReviveFailureReason.AllParticipantsDowned, context.Service.FailureReason,
            "no elimination does not grant an all-Down self-rescue grace");
        if (!context.Service.TryCleanup(context.FightId) || !context.Service.TryCleanup(context.FightId))
            throw new InvalidOperationException("Cleanup must remain idempotent.");
        AssertEqual(0, context.Service.CreateSnapshot().Participants.Count, "cleanup clears lockouts");
    }

    [DomainTest("All Downed fails only on commit")]
    private static void AllDownedFailsOnlyOnCommit()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 0, authorityTick: 10));
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 10));
        AssertEqual(
            RaidReviveFailureReason.None,
            context.Service.FailureReason,
            "failure must be deferred until commit");

        RaidReviveCommandResult commit = context.Service.CommitTick(context.FightId, 10);
        AssertApplied(commit);
        AssertEqual(
            RaidReviveFailureReason.AllParticipantsDowned,
            context.Service.FailureReason,
            "all-Downed failure reason");
        AssertLastEvent(commit, RaidReviveEventKind.RaidFailed);
    }

    [DomainTest("120-tick revive consumes one token")]
    private static void ReviveCompletesAtOneHundredTwentyTicks()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertApplied(StartRevive(context, reviverIndex: 0, targetIndex: 1, nonce: 1, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 120));
        AssertEqual(1, context.Service.RemainingTokenCount, "token before completion");
        AssertEqual(
            RaidParticipantCombatState.Downed,
            ParticipantSnapshot(context, 1).CombatState,
            "target state before completion");

        RaidReviveCommandResult completion = context.Service.CommitTick(context.FightId, 121);
        AssertApplied(completion);
        AssertEqual(0, context.Service.RemainingTokenCount, "token after completion");
        AssertEqual(
            RaidParticipantCombatState.Alive,
            ParticipantSnapshot(context, 1).CombatState,
            "target state after completion");
        RaidReviveEvent revived = FindEvent(
            completion,
            RaidReviveEventKind.ParticipantRevived);
        AssertEqual(0.35f, revived.RestoredLifeRatio, "restored life ratio");
        AssertEqual(301ul, revived.InvulnerabilityUntilTick, "invulnerability deadline");
        AssertEqual(721ul, revived.WeaknessUntilTick, "Weakness deadline");
    }

    [DomainTest("Same-tick competing starts are deterministic")]
    private static void CompetingStartsAreDeterministic()
    {
        ParticipantId? firstWinner = null;
        for (int order = 0; order < 2; order++)
        {
            TestContext context = CreateContext(3);
            AssertApplied(Down(context, participantIndex: 2, authorityTick: 1));

            RaidReviveStartCommand lowerParticipant = CreateStartCommand(
                context,
                reviverIndex: 0,
                targetIndex: 2,
                nonce: 10,
                authorityTick: 1);
            RaidReviveStartCommand higherParticipant = CreateStartCommand(
                context,
                reviverIndex: 1,
                targetIndex: 2,
                nonce: 11,
                authorityTick: 1);
            RaidReviveStartCommand[] commands = order == 0
                ? new[] { lowerParticipant, higherParticipant }
                : new[] { higherParticipant, lowerParticipant };

            RaidReviveStartBatchResult batch = context.Service.ApplyStartBatch(commands);
            AssertAccepted(batch);
            AssertEqual(2, batch.CommandResults.Count, "start result count");
            AssertApplied(batch.CommandResults[0]);
            AssertRejected(batch.CommandResults[1], "revive.target_already_reserved");

            IReadOnlyList<RaidReviveChannelSnapshot> channels =
                context.Service.CreateSnapshot().Channels;
            AssertEqual(1, channels.Count, "winning channel count");
            AssertEqual(
                context.Roster[0].ParticipantId,
                channels[0].Reviver,
                "lower ParticipantId winner");

            if (firstWinner is null)
            {
                firstWinner = channels[0].Reviver;
            }
            else
            {
                AssertEqual(firstWinner.Value, channels[0].Reviver, "winner across input orders");
            }

            uint revisionBeforeDuplicate = context.Service.Revision;
            AssertRejected(
                context.Service.ApplyStartBatch(commands),
                "revive.start_batch_already_processed");
            AssertEqual(
                revisionBeforeDuplicate,
                context.Service.Revision,
                "revision after duplicate start batch");
        }
    }

    [DomainTest("Start-batch envelope is enforced")]
    private static void StartBatchEnvelopeIsEnforced()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        RaidReviveStartCommand valid = CreateStartCommand(
            context,
            reviverIndex: 0,
            targetIndex: 1,
            nonce: 1,
            authorityTick: 1);

        AssertRejected(
            context.Service.ApplyStartBatch(Array.Empty<RaidReviveStartCommand>()),
            "revive.start_batch_empty");
        AssertRejected(
            context.Service.ApplyStartBatch(new[]
            {
                valid,
                valid with { RequestNonce = 2 },
            }),
            "revive.start_batch_duplicate_reviver");
        AssertRejected(
            context.Service.ApplyStartBatch(new[]
            {
                valid,
                valid with { AuthorityTick = 2 },
            }),
            "revive.start_batch_tick_mismatch");
        AssertRejected(
            context.Service.ApplyStartBatch(new[]
            {
                valid with
                {
                    FightId = FightId.FromWire(
                        new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")),
                },
            }),
            "revive.start_batch_fight_id_mismatch");
        AssertRejected(
            context.Service.ApplyStartBatch(new[] { valid, valid, valid }),
            "revive.start_batch_too_large");
        AssertEqual(0, context.Service.CreateSnapshot().Channels.Count, "channels after malformed batches");

        RaidReviveStartBatchResult accepted = context.Service.ApplyStartBatch(
            new[] { valid });
        AssertAccepted(accepted);
        AssertApplied(accepted.CommandResults[0]);
        AssertEqual(1, context.Service.CreateSnapshot().Channels.Count, "channel after valid batch");
    }

    [DomainTest("Stale start nonce is rejected")]
    private static void StaleStartNonceIsRejected()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertApplied(StartRevive(context, reviverIndex: 0, targetIndex: 1, nonce: 10, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));

        RaidReviveInterruptCommand interrupt = new(
            context.FightId,
            context.Roster[0],
            ExpectedChannelNonce: 10,
            Reason: RaidReviveCancelReason.Manual,
            AuthorityTick: 2);
        AssertApplied(context.Service.Apply(interrupt));

        RaidReviveCommandResult stale = StartRevive(
            context,
            reviverIndex: 0,
            targetIndex: 1,
            nonce: 9,
            authorityTick: 2);
        AssertRejected(stale, "revive.stale_request_nonce");
        AssertEqual(0, context.Service.CreateSnapshot().Channels.Count, "active channel count");
    }

    [DomainTest("Rejected future tick does not ratchet authority time")]
    private static void RejectedFutureTickDoesNotRatchet()
    {
        TestContext context = CreateContext(2);
        RaidReviveCommandResult invalidFutureStart = StartRevive(
            context,
            reviverIndex: 0,
            targetIndex: 1,
            nonce: 1,
            authorityTick: 1_000);
        AssertRejected(invalidFutureStart, "revive.target_not_revivable");
        AssertEqual(0u, context.Service.Revision, "revision after rejected future command");

        RaidReviveCommandResult validEarlierDown = Down(
            context,
            participantIndex: 1,
            authorityTick: 10);
        AssertApplied(validEarlierDown);
        AssertApplied(StartRevive(
            context,
            reviverIndex: 0,
            targetIndex: 1,
            nonce: 2,
            authorityTick: 10));
        AssertAccepted(context.Service.CommitTick(context.FightId, 10));
    }

    [DomainTest("Stale cancel cannot interrupt a newer channel")]
    private static void StaleCancelCannotInterruptNewChannel()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertApplied(StartRevive(context, reviverIndex: 0, targetIndex: 1, nonce: 10, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));

        RaidReviveInterruptCommand firstCancel = new(
            context.FightId,
            context.Roster[0],
            ExpectedChannelNonce: 10,
            Reason: RaidReviveCancelReason.Manual,
            AuthorityTick: 2);
        AssertApplied(context.Service.Apply(firstCancel));
        AssertApplied(StartRevive(context, reviverIndex: 0, targetIndex: 1, nonce: 11, authorityTick: 2));
        AssertAccepted(context.Service.CommitTick(context.FightId, 2));

        RaidReviveInterruptCommand delayedOldCancel = firstCancel with { AuthorityTick = 3 };
        AssertRejected(
            context.Service.Apply(delayedOldCancel),
            "revive.channel_nonce_stale");

        IReadOnlyList<RaidReviveChannelSnapshot> channels =
            context.Service.CreateSnapshot().Channels;
        AssertEqual(1, channels.Count, "active channel count after stale cancel");
        AssertEqual(11u, channels[0].ChannelNonce, "new channel nonce after stale cancel");
    }

    [DomainTest("Exact reconnect deadline is rejected")]
    private static void ExactReconnectDeadlineIsRejected()
    {
        TestContext context = CreateContext(2);
        RaidParticipantDisconnectedCommand disconnect = new(
            context.FightId,
            context.Roster[1],
            AuthorityTick: 10);
        AssertApplied(context.Service.Apply(disconnect));

        RaidParticipantRejoinedCommand rejoin = new(
            context.FightId,
            context.Roster[1].ParticipantId,
            NewPlayerSlot: 3,
            NewConnectionEpoch: context.Roster[1].ConnectionEpoch + 1,
            AuthorityTick: 1_810);
        AssertRejected(context.Service.Apply(rejoin), "revive.rejoin_grace_expired");
        AssertAccepted(context.Service.CommitTick(context.FightId, 1_810));
    }

    [DomainTest("Committed tick rejects later commands")]
    private static void CommittedTickRejectsCommands()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));
        uint revisionBefore = context.Service.Revision;

        RaidReviveStartCommand command = CreateStartCommand(
            context,
            reviverIndex: 0,
            targetIndex: 1,
            nonce: 1,
            authorityTick: 1);
        AssertRejected(
            context.Service.ApplyStartBatch(new[] { command }),
            "revive.authority_tick_already_committed");
        AssertEqual(revisionBefore, context.Service.Revision, "revision after committed-tick command");
    }

    [DomainTest("Exact completion deadline cancels revive")]
    private static void ExactCompletionDeadlineCancelsRevive()
    {
        RaidReviveSettings settings = new(
            ParticipantCount: 2,
            InitialTokenCount: 1,
            ChannelDurationTicks: 4,
            DownedTimeoutTicks: 5,
            DisconnectGraceTicks: 5,
            InvulnerabilityTicks: 1,
            WeaknessTicks: 1,
            RestoredLifeRatio: 0.5f);
        TestContext context = CreateContext(2, settings);

        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));
        AssertApplied(StartRevive(
            context,
            reviverIndex: 0,
            targetIndex: 1,
            nonce: 1,
            authorityTick: 2));
        AssertAccepted(context.Service.CommitTick(context.FightId, 2));

        RaidReviveCommandResult deadline = context.Service.CommitTick(context.FightId, 6);
        AssertApplied(deadline);
        AssertContainsEvent(deadline, RaidReviveEventKind.ChannelCancelled);
        AssertContainsEvent(deadline, RaidReviveEventKind.ParticipantEliminated);
        AssertDoesNotContainEvent(deadline, RaidReviveEventKind.ParticipantRevived);
        AssertEqual(1, context.Service.RemainingTokenCount, "token after deadline cancellation");
        AssertEqual(
            RaidParticipantCombatState.Eliminated,
            ParticipantSnapshot(context, 1).CombatState,
            "target after exact deadline");
    }

    [DomainTest("Interrupt releases reservation without token use")]
    private static void InterruptDoesNotConsumeToken()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertApplied(StartRevive(context, reviverIndex: 0, targetIndex: 1, nonce: 7, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));

        RaidReviveInterruptCommand interrupt = new(
            context.FightId,
            context.Roster[0],
            ExpectedChannelNonce: 7,
            Reason: RaidReviveCancelReason.ReviverDamaged,
            AuthorityTick: 2);
        RaidReviveCommandResult result = context.Service.Apply(interrupt);
        AssertApplied(result);
        RaidReviveEvent cancelled = FindEvent(result, RaidReviveEventKind.ChannelCancelled);
        AssertEqual(RaidReviveCancelReason.ReviverDamaged, cancelled.CancelReason, "cancel reason");
        AssertEqual(1, context.Service.RemainingTokenCount, "token after interruption");
        AssertEqual(0, context.Service.ReservedTokenCount, "reservation after interruption");
    }

    [DomainTest("Downed timeout with token eliminates participant")]
    private static void TimeoutWithTokenEliminatesParticipant()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));

        RaidReviveCommandResult timeout = context.Service.CommitTick(context.FightId, 1_801);
        AssertApplied(timeout);
        RaidReviveEvent eliminated = FindEvent(
            timeout,
            RaidReviveEventKind.ParticipantEliminated);
        AssertEqual(
            RaidParticipantEliminationReason.DownedTimeout,
            eliminated.EliminationReason,
            "timeout elimination reason");
        AssertEqual(1, context.Service.RemainingTokenCount, "token after elimination");
        AssertEqual(RaidReviveFailureReason.None, context.Service.FailureReason, "failure after elimination");
    }

    [DomainTest("Rejoin replaces binding epoch")]
    private static void RejoinReplacesBindingEpoch()
    {
        TestContext context = CreateContext(2);
        RaidParticipantBinding oldBinding = context.Roster[1];
        AssertApplied(context.Service.Apply(new RaidParticipantDisconnectedCommand(
            context.FightId,
            oldBinding,
            AuthorityTick: 10)));

        RaidParticipantBinding newBinding = new(
            oldBinding.ParticipantId,
            PlayerSlot: 3,
            ConnectionEpoch: oldBinding.ConnectionEpoch + 1);
        AssertApplied(context.Service.Apply(new RaidParticipantRejoinedCommand(
            context.FightId,
            oldBinding.ParticipantId,
            newBinding.PlayerSlot,
            newBinding.ConnectionEpoch,
            AuthorityTick: 11)));

        AssertEqual(
            false,
            context.Service.TryGetControlProjection(oldBinding, 11, out _),
            "old binding projection");
        AssertEqual(
            true,
            context.Service.TryGetControlProjection(newBinding, 11, out _),
            "new binding projection");
        AssertEqual(newBinding, ParticipantSnapshot(context, 1).Binding, "snapshot binding after rejoin");
    }

    [DomainTest("Terminal event is last")]
    private static void TerminalEventIsLast()
    {
        RaidReviveSettings settings = new(
            ParticipantCount: 2,
            InitialTokenCount: 1,
            ChannelDurationTicks: 2,
            DownedTimeoutTicks: 5,
            DisconnectGraceTicks: 5,
            InvulnerabilityTicks: 1,
            WeaknessTicks: 1,
            RestoredLifeRatio: 0.5f);
        TestContext context = CreateContext(2, settings);

        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertApplied(StartRevive(context, reviverIndex: 0, targetIndex: 1, nonce: 1, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));
        AssertApplied(context.Service.CommitTick(context.FightId, 3));
        AssertEqual(0, context.Service.RemainingTokenCount, "consumed token count");

        AssertApplied(Down(context, participantIndex: 1, authorityTick: 4));
        RaidParticipantDisconnectedCommand disconnect = new(
            context.FightId,
            context.Roster[0],
            AuthorityTick: 4);
        AssertApplied(context.Service.Apply(disconnect));
        AssertAccepted(context.Service.CommitTick(context.FightId, 4));

        RaidReviveCommandResult terminal = context.Service.CommitTick(context.FightId, 9);
        AssertApplied(terminal);
        AssertEqual(
            RaidReviveFailureReason.DownedTimeoutWithoutToken,
            context.Service.FailureReason,
            "terminal failure reason");
        AssertLastEvent(terminal, RaidReviveEventKind.RaidFailed);
    }

    [DomainTest("Undefined interrupt is rejected without ratchet")]
    private static void UndefinedInterruptIsRejected()
    {
        TestContext context = CreateContext(2);
        AssertApplied(Down(context, participantIndex: 1, authorityTick: 1));
        AssertApplied(StartRevive(context, reviverIndex: 0, targetIndex: 1, nonce: 1, authorityTick: 1));
        AssertAccepted(context.Service.CommitTick(context.FightId, 1));

        RaidReviveInterruptCommand undefined = new(
            context.FightId,
            context.Roster[0],
            ExpectedChannelNonce: 1,
            Reason: (RaidReviveCancelReason)255,
            AuthorityTick: 1_000);
        AssertRejected(context.Service.Apply(undefined), "revive.cancel_reason_invalid");

        RaidReviveInterruptCommand valid = new(
            context.FightId,
            context.Roster[0],
            ExpectedChannelNonce: 1,
            Reason: RaidReviveCancelReason.ReviverMoved,
            AuthorityTick: 2);
        AssertApplied(context.Service.Apply(valid));
    }

    [DomainTest("Inactive interrupt does not ratchet authority time")]
    private static void InactiveInterruptDoesNotRatchet()
    {
        TestContext context = CreateContext(2);
        RaidReviveInterruptCommand zeroNonce = new(
            context.FightId,
            context.Roster[0],
            ExpectedChannelNonce: 0,
            Reason: RaidReviveCancelReason.Manual,
            AuthorityTick: 2_000);
        AssertRejected(context.Service.Apply(zeroNonce), "revive.channel_nonce_stale");

        RaidReviveInterruptCommand inactive = zeroNonce with
        {
            ExpectedChannelNonce = 1,
            AuthorityTick = 1_000,
        };
        RaidReviveCommandResult noChange = context.Service.Apply(inactive);
        AssertEqual(RaidReviveCommandDisposition.NoChange, noChange.Disposition, "inactive cancel disposition");
        AssertEqual("revive.channel_not_active", noChange.FailureCode, "inactive cancel reason");

        AssertApplied(Down(context, participantIndex: 1, authorityTick: 10));
        AssertAccepted(context.Service.CommitTick(context.FightId, 10));
    }

    [DomainTest("Cleanup is exact and idempotent")]
    private static void CleanupIsExactAndIdempotent()
    {
        TestContext context = CreateContext(2);
        FightId staleFightId = FightId.FromWire(
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));

        AssertEqual(false, context.Service.TryCleanup(staleFightId), "stale cleanup before exact cleanup");
        AssertEqual(false, context.Service.IsCleaned, "cleaned state after stale cleanup");
        AssertEqual(true, context.Service.TryCleanup(context.FightId), "first exact cleanup");
        AssertEqual(true, context.Service.IsCleaned, "cleaned state after exact cleanup");
        AssertEqual(0, context.Service.CreateSnapshot().Participants.Count, "participants after cleanup");
        AssertEqual(true, context.Service.TryCleanup(context.FightId), "second exact cleanup");
        AssertEqual(false, context.Service.TryCleanup(staleFightId), "stale cleanup after exact cleanup");
    }

    [DomainTest("Boundary carries Apply dirty state")]
    private static void BoundaryCarriesApplyDirtyState()
    {
        TestContext context = CreateContext(3);
        FirstSeveranceReviveBoundary boundary = new(context.FightId);
        if (!boundary.TryInitialize(context.Roster, out string failureCode))
        {
            throw new InvalidOperationException($"Boundary initialization failed: '{failureCode}'.");
        }

        AuthoritativeParticipantDownedCommand downed = new(
            context.FightId,
            context.Roster[2],
            AuthorityTick: 1);
        AssertApplied(boundary.Apply(downed));

        EncounterRuntimeContext runtimeContext = new(
            EncounterSequence: 1,
            FightId: context.FightId,
            Lifecycle: EncounterLifecycle.Active,
            AuthorityTick: 1,
            LifecycleEnteredTick: 0,
            ActiveFightTick: 1);
        EncounterRuntimeUpdate update = boundary.Tick(runtimeContext);
        if (!update.HasObservableChange)
        {
            throw new InvalidOperationException("Boundary dropped an observable Apply change.");
        }

        RaidReviveStartCommand rejectedStart = CreateStartCommand(
            context,
            reviverIndex: 0,
            targetIndex: 0,
            nonce: 1,
            authorityTick: 2);
        RaidReviveStartCommand appliedStart = CreateStartCommand(
            context,
            reviverIndex: 1,
            targetIndex: 2,
            nonce: 2,
            authorityTick: 2);
        RaidReviveStartBatchResult batch = boundary.ApplyStartBatch(
            new[] { appliedStart, rejectedStart });
        AssertAccepted(batch);
        AssertRejected(batch.CommandResults[0], "revive.target_invalid");
        AssertApplied(batch.CommandResults[1]);

        EncounterRuntimeUpdate batchUpdate = boundary.Tick(runtimeContext with
        {
            AuthorityTick = 2,
            ActiveFightTick = 2,
        });
        if (!batchUpdate.HasObservableChange)
        {
            throw new InvalidOperationException("Boundary dropped an observable start-batch change.");
        }

        boundary.Cleanup(new EncounterCleanupContext(
            EncounterSequence: 1,
            FightId: context.FightId,
            Termination: FirstSeveranceTerminationContract.Instance.Create(
                FirstSeveranceTerminalCause.UserCancelled)));
    }
}
