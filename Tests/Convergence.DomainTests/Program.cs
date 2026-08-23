#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.ThirdSeverance;
using Convergence.Content.Encounters.ThirdSeverance.Revive;

namespace Convergence.DomainTests;

internal static class Program
{
    private sealed class TestContext
    {
        public TestContext(
            FightId fightId,
            RaidParticipantBinding[] roster,
            RaidReviveService service)
        {
            FightId = fightId;
            Roster = roster;
            Service = service;
        }

        public FightId FightId { get; }

        public RaidParticipantBinding[] Roster { get; }

        public RaidReviveService Service { get; }
    }

    private readonly record struct TestCase(string Name, Action Body);

    private static int nextFightNumber = 1;

    private static int Main()
    {
        TestCase[] tests =
        {
            new("Token scaling", TokenScaling),
            new("All Downed fails only on commit", AllDownedFailsOnlyOnCommit),
            new("120-tick revive consumes one token", ReviveCompletesAtOneHundredTwentyTicks),
            new("Same-tick competing starts are deterministic", CompetingStartsAreDeterministic),
            new("Start-batch envelope is enforced", StartBatchEnvelopeIsEnforced),
            new("Stale start nonce is rejected", StaleStartNonceIsRejected),
            new("Stale cancel cannot interrupt a newer channel", StaleCancelCannotInterruptNewChannel),
            new("Rejected future tick does not ratchet authority time", RejectedFutureTickDoesNotRatchet),
            new("Committed tick rejects later commands", CommittedTickRejectsCommands),
            new("Exact reconnect deadline is rejected", ExactReconnectDeadlineIsRejected),
            new("Exact completion deadline cancels revive", ExactCompletionDeadlineCancelsRevive),
            new("Interrupt releases reservation without token use", InterruptDoesNotConsumeToken),
            new("Downed timeout with token eliminates participant", TimeoutWithTokenEliminatesParticipant),
            new("Rejoin replaces binding epoch", RejoinReplacesBindingEpoch),
            new("Terminal event is last", TerminalEventIsLast),
            new("Undefined interrupt is rejected without ratchet", UndefinedInterruptIsRejected),
            new("Inactive interrupt does not ratchet authority time", InactiveInterruptDoesNotRatchet),
            new("Cleanup is exact and idempotent", CleanupIsExactAndIdempotent),
            new("Boundary carries Apply dirty state", BoundaryCarriesApplyDirtyState),
            new("Arena layout is Core-anchored", ArenaLayoutIsCoreAnchored),
            new("Arena rejects a World-edge Core", ArenaRejectsWorldEdgeCore),
            new("Arena occupant identity rejects slot reuse", ArenaOccupantIdentityRejectsSlotReuse),
            new("Outsider responses are deterministic", OutsiderResponsesAreDeterministic),
            new("Default Boss encounter plan validates", DefaultBossEncounterPlanValidates),
        };

        int failures = 0;
        foreach (TestCase test in tests)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
            }
        }

        Console.WriteLine($"Executed {tests.Length} deterministic domain tests; failures: {failures}.");
        return failures == 0 ? 0 : 1;
    }

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

    private static void BoundaryCarriesApplyDirtyState()
    {
        TestContext context = CreateContext(3);
        ThirdSeveranceReviveBoundary boundary = new(context.FightId);
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
            EndReason: EncounterEndReason.Cancelled));
    }

    private static void ArenaLayoutIsCoreAnchored()
    {
        var worldBounds = new TileRectangle(0, 0, 8_400, 2_400);
        var core = new ResolvedThirdSeveranceCoreAnchor(
            new TilePoint(4_200, 1_250),
            BaseY: 1_300,
            ServerTileEntityId: 7);

        bool created = ThirdSeveranceArenaBlueprint.Instance.TryCreateLayout(
            core,
            worldBounds,
            out ThirdSeveranceArenaLayout? layout,
            out string failureCode);
        if (!created || layout is null)
        {
            throw new InvalidOperationException($"Expected a valid layout, got '{failureCode}'.");
        }

        AssertEqual(new TileRectangle(4_040, 1_160, 320, 140), layout.ArenaBounds, "Arena bounds");
        AssertEqual(new TileRectangle(4_042, 1_162, 316, 136), layout.BarrierBounds, "Barrier bounds");
        AssertEqual(4, layout.Pylons.Count, "Pylon count");
        AssertEqual(new TilePoint(4_054, 1_174), layout.Pylons[0].TilePosition, "NW Pylon");
        AssertEqual(new TilePoint(4_345, 1_285), layout.Pylons[3].TilePosition, "SE Pylon");
    }

    private static void ArenaRejectsWorldEdgeCore()
    {
        var worldBounds = new TileRectangle(0, 0, 320, 141);
        var core = new ResolvedThirdSeveranceCoreAnchor(
            new TilePoint(160, 70),
            BaseY: 140,
            ServerTileEntityId: 1);

        bool created = ThirdSeveranceArenaBlueprint.Instance.TryCreateLayout(
            core,
            worldBounds,
            out ThirdSeveranceArenaLayout? layout,
            out string failureCode);
        AssertEqual(false, created, "World-edge layout creation");
        AssertEqual<ThirdSeveranceArenaLayout?>(null, layout, "rejected Arena layout");
        AssertEqual(
            "third_severance.arena_world_edge_margin_violation",
            failureCode,
            "Arena rejection code");
    }

    private static void OutsiderResponsesAreDeterministic()
    {
        ThirdSeveranceBoundaryRule? outsiderRule = null;
        IReadOnlyList<ThirdSeveranceBoundaryRule> rules =
            ThirdSeveranceArenaAccessPolicy.Instance.Rules;
        for (int index = 0; index < rules.Count; index++)
        {
            if (rules[index].ViolationKind == ThirdSeveranceBoundaryViolationKind.OutsiderInsideBarrier)
            {
                outsiderRule = rules[index];
                break;
            }
        }

        if (outsiderRule is null)
        {
            throw new InvalidOperationException("Outsider boundary rule is missing.");
        }

        IReadOnlyList<ThirdSeveranceBoundaryResponse> initial =
            outsiderRule.GetEligibleResponses(continuousTicks: 1, violationCount: 1);
        AssertEqual(2, initial.Count, "initial outsider response count");
        AssertEqual(ThirdSeveranceBoundaryResponse.ServerWarning, initial[0], "first outsider response");
        AssertEqual(
            ThirdSeveranceBoundaryResponse.SuppressEncounterInteraction,
            initial[1],
            "second outsider response");

        IReadOnlyList<ThirdSeveranceBoundaryResponse> escalated =
            outsiderRule.GetEligibleResponses(continuousTicks: 120, violationCount: 3);
        AssertEqual(4, escalated.Count, "escalated outsider response count");
        AssertEqual(
            ThirdSeveranceBoundaryResponse.ExcludeFromArena,
            escalated[3],
            "final outsider response");
        AssertEqual(false, ThirdSeveranceArenaAccessPolicy.Instance.UsesLethalExclusion, "lethal exclusion");
    }

    private static void ArenaOccupantIdentityRejectsSlotReuse()
    {
        var currentOutsider = new ThirdSeveranceArenaOccupant(
            ServerWhoAmI: 8,
            ConnectionEpoch: 42,
            ParticipantId.Invalid);
        var staleOutsider = currentOutsider with { ConnectionEpoch = 0 };
        var sentinel = currentOutsider with { ServerWhoAmI = byte.MaxValue };

        AssertEqual(true, currentOutsider.IsValid, "current outsider identity");
        AssertEqual(false, staleOutsider.IsValid, "stale outsider identity");
        AssertEqual(false, sentinel.IsValid, "server sentinel identity");
        AssertEqual(false, currentOutsider.IsParticipant, "outsider participant flag");
    }

    private static void DefaultBossEncounterPlanValidates()
    {
        ThirdSeveranceEncounterPlan plan = ThirdSeveranceEncounterPlan.Instance;
        AssertEqual(ThirdSeverancePhaseId.BaseActivation, plan.FirstPhase, "first phase");
        AssertEqual(7, plan.Phases.Count, "phase count");
        AssertEqual(320, plan.Arena.Profile.WidthInTiles, "Arena width profile");
        AssertEqual(140, plan.Arena.Profile.HeightInTiles, "Arena height profile");
        AssertEqual(3, plan.HardEnrageOverloadThreshold, "Hard Enrage threshold");
        AssertEqual(
            ThirdSeveranceFailureOutcome.AdvanceHardEnrage,
            plan.LoopExhaustionOutcome,
            "loop exhaustion outcome");
        AssertEqual(
            ThirdSeverancePhaseId.LastStand,
            plan.Phases[plan.Phases.Count - 1].Id,
            "terminal phase");
    }

    private static TestContext CreateContext(
        int participantCount,
        RaidReviveSettings? customSettings = null)
    {
        FightId fightId = FightId.FromWire(
            new Guid($"00000000-0000-0000-0000-{nextFightNumber++:X12}"));
        RaidParticipantBinding[] roster = new RaidParticipantBinding[participantCount];
        for (int index = 0; index < roster.Length; index++)
        {
            roster[index] = new RaidParticipantBinding(
                new ParticipantId((byte)index),
                PlayerSlot: index,
                ConnectionEpoch: (ulong)(100 + index));
        }

        RaidReviveSettings settings = customSettings
            ?? RaidReviveSettings.CreateInitial(participantCount);
        return new TestContext(
            fightId,
            roster,
            new RaidReviveService(fightId, settings, roster));
    }

    private static RaidReviveCommandResult Down(
        TestContext context,
        int participantIndex,
        ulong authorityTick)
    {
        AuthoritativeParticipantDownedCommand command = new(
            context.FightId,
            context.Roster[participantIndex],
            authorityTick);
        return context.Service.Apply(command);
    }

    private static RaidReviveCommandResult StartRevive(
        TestContext context,
        int reviverIndex,
        int targetIndex,
        uint nonce,
        ulong authorityTick)
    {
        RaidReviveStartCommand command = CreateStartCommand(
            context,
            reviverIndex,
            targetIndex,
            nonce,
            authorityTick);
        RaidReviveStartBatchResult batch = context.Service.ApplyStartBatch(
            new[] { command });
        AssertAccepted(batch);
        AssertEqual(1, batch.CommandResults.Count, "single-start result count");
        return batch.CommandResults[0];
    }

    private static RaidReviveStartCommand CreateStartCommand(
        TestContext context,
        int reviverIndex,
        int targetIndex,
        uint nonce,
        ulong authorityTick)
    {
        return new RaidReviveStartCommand(
            context.FightId,
            context.Roster[reviverIndex],
            context.Roster[targetIndex].ParticipantId,
            nonce,
            authorityTick);
    }

    private static RaidParticipantReviveSnapshot ParticipantSnapshot(
        TestContext context,
        int participantIndex)
    {
        ParticipantId expected = context.Roster[participantIndex].ParticipantId;
        IReadOnlyList<RaidParticipantReviveSnapshot> participants =
            context.Service.CreateSnapshot().Participants;
        for (int index = 0; index < participants.Count; index++)
        {
            if (participants[index].Binding.ParticipantId == expected)
            {
                return participants[index];
            }
        }

        throw new InvalidOperationException($"Participant {expected.Value} is missing from the snapshot.");
    }

    private static void AssertAccepted(RaidReviveCommandResult result)
    {
        if (!result.IsAccepted)
        {
            throw new InvalidOperationException($"Expected accepted result, got '{result.FailureCode}'.");
        }
    }

    private static void AssertAccepted(RaidReviveStartBatchResult result)
    {
        if (!result.IsAccepted)
        {
            throw new InvalidOperationException(
                $"Expected accepted start batch, got '{result.FailureCode}'.");
        }
    }

    private static void AssertApplied(RaidReviveCommandResult result)
    {
        if (result.Disposition != RaidReviveCommandDisposition.Applied)
        {
            throw new InvalidOperationException(
                $"Expected applied result, got {result.Disposition} '{result.FailureCode}'.");
        }
    }

    private static void AssertRejected(RaidReviveCommandResult result, string expectedFailureCode)
    {
        if (result.Disposition != RaidReviveCommandDisposition.Rejected
            || !string.Equals(result.FailureCode, expectedFailureCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected rejection '{expectedFailureCode}', got {result.Disposition} "
                + $"'{result.FailureCode}'.");
        }
    }

    private static void AssertRejected(
        RaidReviveStartBatchResult result,
        string expectedFailureCode)
    {
        if (result.IsAccepted
            || !string.Equals(result.FailureCode, expectedFailureCode, StringComparison.Ordinal)
            || result.CommandResults.Count != 0)
        {
            throw new InvalidOperationException(
                $"Expected batch rejection '{expectedFailureCode}', got "
                + $"'{result.FailureCode}' with {result.CommandResults.Count} command results.");
        }
    }

    private static void AssertContainsEvent(
        RaidReviveCommandResult result,
        RaidReviveEventKind expectedKind)
    {
        _ = FindEvent(result, expectedKind);
    }

    private static RaidReviveEvent FindEvent(
        RaidReviveCommandResult result,
        RaidReviveEventKind expectedKind)
    {
        for (int index = 0; index < result.Events.Count; index++)
        {
            if (result.Events[index].Kind == expectedKind)
            {
                return result.Events[index];
            }
        }

        throw new InvalidOperationException($"Event '{expectedKind}' was not emitted.");
    }

    private static void AssertDoesNotContainEvent(
        RaidReviveCommandResult result,
        RaidReviveEventKind unexpectedKind)
    {
        for (int index = 0; index < result.Events.Count; index++)
        {
            if (result.Events[index].Kind == unexpectedKind)
            {
                throw new InvalidOperationException(
                    $"Unexpected event '{unexpectedKind}' was emitted.");
            }
        }
    }

    private static void AssertLastEvent(
        RaidReviveCommandResult result,
        RaidReviveEventKind expectedKind)
    {
        if (result.Events.Count == 0
            || result.Events[result.Events.Count - 1].Kind != expectedKind)
        {
            throw new InvalidOperationException($"Expected final event '{expectedKind}'.");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Expected {context} '{expected}', got '{actual}'.");
        }
    }
}
