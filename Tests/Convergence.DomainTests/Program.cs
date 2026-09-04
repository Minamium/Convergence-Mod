#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Replication;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Revive;

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

    private sealed class TestEncounterDefinition : EncounterDefinition
    {
        public const string TestKey = "domain_test_encounter";

        private readonly IEncounterRuntimeFactory runtimeFactory;

        public TestEncounterDefinition(
            IEncounterRuntimeFactory runtimeFactory,
            IReadOnlyList<EncounterTerminationDescriptor> externalTerminations)
            : base(FirstSeveranceTerminationContract.Instance, externalTerminations)
        {
            this.runtimeFactory = runtimeFactory;
        }

        public override string Key => TestKey;

        public override EncounterKind Kind => EncounterKind.Raid;

        public override IEncounterRuntimeFactory RuntimeFactory => runtimeFactory;
    }

    private sealed class TestRuntimeFactory : IEncounterRuntimeFactory
    {
        private readonly Func<EncounterRuntimeCreationContext, IEncounterRuntime> create;

        public TestRuntimeFactory(Func<EncounterRuntimeCreationContext, IEncounterRuntime> create)
        {
            this.create = create;
        }

        public IEncounterRuntime Create(
            EncounterDefinition definition,
            in EncounterRuntimeCreationContext context)
        {
            _ = definition;
            return create(context);
        }
    }

    private sealed class TestRuntime : IEncounterRuntime
    {
        private readonly Func<EncounterRuntimeContext, EncounterRuntimeUpdate> tick;
        private readonly Action<EncounterCleanupContext>? onCleanup;

        public TestRuntime(
            Func<EncounterRuntimeContext, EncounterRuntimeUpdate> tick,
            Action<EncounterCleanupContext>? onCleanup = null)
        {
            this.tick = tick;
            this.onCleanup = onCleanup;
        }

        public int TickCount { get; private set; }

        public EncounterCleanupContext? CleanupContext { get; private set; }

        public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
        {
            TickCount++;
            return tick(context);
        }

        public void Cleanup(in EncounterCleanupContext context)
        {
            CleanupContext = context;
            onCleanup?.Invoke(context);
        }
    }

    private sealed class TestCleanupParticipant : IEncounterCleanupParticipant
    {
        public EncounterCleanupContext? Context { get; private set; }

        public void Cleanup(in EncounterCleanupContext context)
        {
            Context = context;
        }
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
            new("Default First Severance plans validate", DefaultFirstSeverancePlansValidate),
            new("Invalid First Severance plans are rejected", InvalidFirstSeverancePlansAreRejected),
            new("Pylons complete early and at the deadline", PylonsCompleteWithoutOverload),
            new("Pylon failures reach the third-Overload defeat", PylonFailuresReachOverloadDefeat),
            new("The full loop preserves Boss life", FullLoopPreservesBossLife),
            new("Exposure victory wins on its deadline tick", ExposureVictoryWinsDeadlineCollision),
            new("The eighth exposure ends at the loop cap", EighthExposureEndsAtLoopCap),
            new("Terminal cause values and mappings are stable", TerminalCauseValuesAndMappingsAreStable),
            new("Feature terminal priority is total", FeatureTerminalPriorityIsTotal),
            new("External terminal maps reject invalid definitions", ExternalTerminalMapsRejectInvalidDefinitions),
            new("Runtime End publishes feature metadata before cleanup", RuntimeEndPublishesFeatureMetadata),
            new("Invalid runtime End becomes InternalFailure", InvalidRuntimeEndBecomesInternalFailure),
            new("External termination priority preempts runtime", ExternalTerminationPriorityPreemptsRuntime),
            new("Runtime exceptions use immutable external mapping", RuntimeExceptionsUseExternalMapping),
            new("Runtime creation failure has no tombstone", RuntimeCreationFailureHasNoTombstone),
            new("Replica retains the feature terminal tombstone", ReplicaRetainsFeatureTerminalTombstone),
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

    private static void ArenaLayoutIsCoreAnchored()
    {
        var worldBounds = new TileRectangle(0, 0, 8_400, 2_400);
        var core = new ResolvedFirstSeveranceCoreAnchor(
            new TilePoint(4_200, 1_250),
            BaseY: 1_300,
            ServerTileEntityId: 7);

        bool created = FirstSeveranceArenaBlueprint.Instance.TryCreateLayout(
            core,
            worldBounds,
            out FirstSeveranceArenaLayout? layout,
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
        var core = new ResolvedFirstSeveranceCoreAnchor(
            new TilePoint(160, 70),
            BaseY: 140,
            ServerTileEntityId: 1);

        bool created = FirstSeveranceArenaBlueprint.Instance.TryCreateLayout(
            core,
            worldBounds,
            out FirstSeveranceArenaLayout? layout,
            out string failureCode);
        AssertEqual(false, created, "World-edge layout creation");
        AssertEqual<FirstSeveranceArenaLayout?>(null, layout, "rejected Arena layout");
        AssertEqual(
            "first_severance.arena_world_edge_margin_violation",
            failureCode,
            "Arena rejection code");
    }

    private static void OutsiderResponsesAreDeterministic()
    {
        FirstSeveranceBoundaryRule? outsiderRule = null;
        IReadOnlyList<FirstSeveranceBoundaryRule> rules =
            FirstSeveranceArenaAccessPolicy.Instance.Rules;
        for (int index = 0; index < rules.Count; index++)
        {
            if (rules[index].ViolationKind == FirstSeveranceBoundaryViolationKind.OutsiderInsideBarrier)
            {
                outsiderRule = rules[index];
                break;
            }
        }

        if (outsiderRule is null)
        {
            throw new InvalidOperationException("Outsider boundary rule is missing.");
        }

        IReadOnlyList<FirstSeveranceBoundaryResponse> initial =
            outsiderRule.GetEligibleResponses(continuousTicks: 1, violationCount: 1);
        AssertEqual(2, initial.Count, "initial outsider response count");
        AssertEqual(FirstSeveranceBoundaryResponse.ServerWarning, initial[0], "first outsider response");
        AssertEqual(
            FirstSeveranceBoundaryResponse.SuppressEncounterInteraction,
            initial[1],
            "second outsider response");

        IReadOnlyList<FirstSeveranceBoundaryResponse> escalated =
            outsiderRule.GetEligibleResponses(continuousTicks: 120, violationCount: 3);
        AssertEqual(4, escalated.Count, "escalated outsider response count");
        AssertEqual(
            FirstSeveranceBoundaryResponse.ExcludeFromArena,
            escalated[3],
            "final outsider response");
        AssertEqual(false, FirstSeveranceArenaAccessPolicy.Instance.UsesLethalExclusion, "lethal exclusion");
    }

    private static void ArenaOccupantIdentityRejectsSlotReuse()
    {
        var currentOutsider = new FirstSeveranceArenaOccupant(
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

    private static void DefaultFirstSeverancePlansValidate()
    {
        FirstSeveranceEncounterPlan plan = FirstSeveranceEncounterPlan.Instance;
        AssertEqual(FirstSeveranceSubstate.SpawnIntro, plan.FirstSubstate, "first substate");
        AssertEqual(6, plan.Substates.Count, "substate count");
        AssertEqual(320, plan.Arena.Profile.WidthInTiles, "Arena width profile");
        AssertEqual(140, plan.Arena.Profile.HeightInTiles, "Arena height profile");
        AssertEqual(3, plan.OverloadThreshold, "Overload threshold");
        AssertEqual(8, plan.MaximumCompletedExposures, "exposure cap");
        AssertEqual(true, plan.Boss.UsesPersistentLifePool, "persistent Boss life");

        int[] expectedPylons = { 2, 3, 4 };
        int[] expectedShares = { 2, 2, 3 };
        for (int participantCount = 2; participantCount <= 4; participantCount++)
        {
            AssertEqual(
                expectedPylons[participantCount - 2],
                plan.PylonCount.GetValue(participantCount),
                $"{participantCount}-player Pylon count");
            AssertEqual(
                expectedShares[participantCount - 2],
                plan.StackRequiredShares.GetValue(participantCount),
                $"{participantCount}-player Stack shares");
        }

        AssertEqual(180, plan.Timing.SpawnIntroTicks, "SpawnIntro duration");
        AssertEqual(660, plan.Timing.PylonCheckTicks, "Pylon duration");
        AssertEqual(180, plan.Timing.StackTelegraphTicks, "Stack duration");
        AssertEqual(180, plan.Timing.SpreadTelegraphTicks, "Spread duration");
        AssertEqual(720, plan.Timing.NormalExposureTicks, "normal exposure duration");
        AssertEqual(360, plan.Timing.PenalizedExposureTicks, "penalized exposure duration");
        AssertEqual(90, plan.Timing.ResetTicks, "Reset duration");

        FirstSeveranceSubstate[] expectedSubstates =
        {
            FirstSeveranceSubstate.None,
            FirstSeveranceSubstate.SpawnIntro,
            FirstSeveranceSubstate.PylonCheck,
            FirstSeveranceSubstate.Stack,
            FirstSeveranceSubstate.Spread,
            FirstSeveranceSubstate.CoreExposure,
            FirstSeveranceSubstate.Reset,
        };
        FirstSeveranceMechanicKind[] expectedMechanics =
        {
            FirstSeveranceMechanicKind.None,
            FirstSeveranceMechanicKind.PylonCheck,
            FirstSeveranceMechanicKind.Stack,
            FirstSeveranceMechanicKind.Spread,
            FirstSeveranceMechanicKind.CoreExposure,
        };
        AssertEnumValues(expectedSubstates, "active substates");
        AssertEnumValues(expectedMechanics, "active mechanics");
    }

    private static void InvalidFirstSeverancePlansAreRejected()
    {
        FirstSeveranceEncounterPlan baseline = FirstSeveranceEncounterPlan.Instance;
        AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new FirstSeveranceTimingPlan(
                0,
                60,
                600,
                180,
                180,
                720,
                360,
                90),
            "zero duration");

        AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new FirstSeveranceEncounterPlan(
                baseline.Arena,
                baseline.AccessPolicy,
                baseline.Boss,
                baseline.Timing,
                new FirstSeveranceParticipantScaledInt(2, 3, 5),
                baseline.StackRequiredShares,
                baseline.OverloadThreshold,
                baseline.MaximumCompletedExposures,
                baseline.Substates,
                baseline.TerminationContract),
            "Pylon count");

        AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new FirstSeveranceEncounterPlan(
                baseline.Arena,
                baseline.AccessPolicy,
                baseline.Boss,
                baseline.Timing,
                baseline.PylonCount,
                baseline.StackRequiredShares,
                overloadThreshold: 0,
                baseline.MaximumCompletedExposures,
                baseline.Substates,
                baseline.TerminationContract),
            "Overload threshold");

        var duplicateMechanic = new List<FirstSeveranceSubstateDefinition>(baseline.Substates);
        FirstSeveranceSubstateDefinition spread = baseline.GetSubstate(
            FirstSeveranceSubstate.Spread);
        for (int index = 0; index < duplicateMechanic.Count; index++)
        {
            if (duplicateMechanic[index].Substate == FirstSeveranceSubstate.Spread)
            {
                duplicateMechanic[index] = new FirstSeveranceSubstateDefinition(
                    spread.Substate,
                    FirstSeveranceMechanicKind.Stack,
                    spread.DurationTicks,
                    spread.SuccessEdge,
                    spread.FailureEdge);
                break;
            }
        }

        AssertThrows<ArgumentException>(
            () => _ = new FirstSeveranceEncounterPlan(
                baseline.Arena,
                baseline.AccessPolicy,
                baseline.Boss,
                baseline.Timing,
                baseline.PylonCount,
                baseline.StackRequiredShares,
                baseline.OverloadThreshold,
                baseline.MaximumCompletedExposures,
                duplicateMechanic,
                baseline.TerminationContract),
            "duplicate mechanic owner");
    }

    private static void PylonsCompleteWithoutOverload()
    {
        FirstSeveranceLoopStateMachine early = CreateLoopMachine(participantCount: 4);
        EnterPylonCheck(early);
        ulong firstDamageableTick = early.State.SubstateEnteredTick
            + (ulong)FirstSeveranceEncounterPlan.Instance.Timing.PylonTelegraphTicks;
        AssertApplied(early.Advance(new FirstSeveranceLoopInput(
            firstDamageableTick,
            early.State.RemainingPylons,
            AcceptedBossDamage: 0)));
        AssertEqual(FirstSeveranceSubstate.Stack, early.State.Substate, "early Pylon edge");
        AssertEqual(0, early.State.Overload, "early Pylon Overload");

        FirstSeveranceLoopStateMachine closingTick = CreateLoopMachine(participantCount: 3);
        EnterPylonCheck(closingTick);
        AssertApplied(closingTick.Advance(new FirstSeveranceLoopInput(
            closingTick.State.ResolveTick,
            closingTick.State.RemainingPylons,
            AcceptedBossDamage: 0)));
        AssertEqual(
            FirstSeveranceSubstate.Stack,
            closingTick.State.Substate,
            "closing-tick Pylon edge");
        AssertEqual(0, closingTick.State.Overload, "closing-tick Pylon Overload");
    }

    private static void PylonFailuresReachOverloadDefeat()
    {
        FirstSeveranceLoopStateMachine machine = CreateLoopMachine(participantCount: 2);
        EnterPylonCheck(machine);

        for (int failure = 1; failure <= 3; failure++)
        {
            FirstSeveranceLoopUpdate result = machine.Advance(new FirstSeveranceLoopInput(
                machine.State.ResolveTick,
                DestroyedPylons: 0,
                AcceptedBossDamage: 0));
            AssertEqual(failure, machine.State.Overload, $"Overload after failure {failure}");

            if (failure == 3)
            {
                AssertEnded(result);
                break;
            }

            AssertApplied(result);
            AssertEqual(FirstSeveranceSubstate.Stack, machine.State.Substate, "post-failure Stack");
            AssertEqual(true, machine.State.IsPenalizedExposure, "penalized loop flag");
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.Spread);
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.CoreExposure);
            AssertEqual(
                (ulong)FirstSeveranceEncounterPlan.Instance.Timing.PenalizedExposureTicks,
                machine.State.ResolveTick - machine.State.SubstateEnteredTick,
                "penalized exposure duration");
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.Reset);
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.PylonCheck);
        }

        AssertTermination(
            machine.State.Termination,
            EncounterEndReason.Defeat,
            FirstSeveranceTerminalCause.OverloadLimit);
        AssertEqual(FirstSeveranceSubstate.PylonCheck, machine.State.Substate, "terminal source state");
    }

    private static void FullLoopPreservesBossLife()
    {
        FirstSeveranceLoopStateMachine machine = CreateLoopMachine(
            participantCount: 4,
            bossMaximumLife: 100);

        AssertApplied(machine.Advance(new FirstSeveranceLoopInput(
            AuthorityTick: 1,
            DestroyedPylons: 0,
            AcceptedBossDamage: 40)));
        AssertEqual(100, machine.State.BossLife, "shielded Boss life");

        EnterPylonCheck(machine);
        ClearCurrentPylons(machine);
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.Spread);
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.CoreExposure);
        AssertApplied(machine.Advance(new FirstSeveranceLoopInput(
            machine.State.SubstateEnteredTick + 1,
            DestroyedPylons: 0,
            AcceptedBossDamage: 30)));
        AssertEqual(70, machine.State.BossLife, "exposure Boss life");
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.Reset);
        AssertEqual(1, machine.State.CompletedExposures, "completed exposures");
        AssertEqual(70, machine.State.BossLife, "Boss life after exposure");
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.PylonCheck);
        AssertEqual(1, machine.State.ZeroBasedLoopIndex, "second loop index");
        AssertEqual(70, machine.State.BossLife, "Boss life entering second loop");
        AssertEqual(false, machine.State.IsPenalizedExposure, "new clean loop flag");
    }

    private static void ExposureVictoryWinsDeadlineCollision()
    {
        FirstSeveranceLoopStateMachine machine = CreateLoopMachine(
            participantCount: 3,
            bossMaximumLife: 100);
        EnterPylonCheck(machine);
        ClearCurrentPylons(machine);
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.Spread);
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.CoreExposure);

        FirstSeveranceLoopUpdate result = machine.Advance(new FirstSeveranceLoopInput(
            machine.State.ResolveTick,
            DestroyedPylons: 0,
            AcceptedBossDamage: 100));
        AssertEnded(result);
        AssertTermination(
            machine.State.Termination,
            EncounterEndReason.Victory,
            FirstSeveranceTerminalCause.BossLifeZero);
        AssertEqual(0, machine.State.CompletedExposures, "closing-tick exposure count");
    }

    private static void EighthExposureEndsAtLoopCap()
    {
        FirstSeveranceLoopStateMachine machine = CreateLoopMachine(participantCount: 4);
        EnterPylonCheck(machine);

        for (int exposure = 1; exposure <= 8; exposure++)
        {
            ClearCurrentPylons(machine);
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.Spread);
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.CoreExposure);
            FirstSeveranceLoopUpdate result = machine.Advance(new FirstSeveranceLoopInput(
                machine.State.ResolveTick,
                DestroyedPylons: 0,
                AcceptedBossDamage: 0));

            if (exposure == 8)
            {
                AssertEnded(result);
                break;
            }

            AssertApplied(result);
            AssertEqual(FirstSeveranceSubstate.Reset, machine.State.Substate, "Reset edge");
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.PylonCheck);
        }

        AssertEqual(8, machine.State.CompletedExposures, "completed exposure cap");
        AssertTermination(
            machine.State.Termination,
            EncounterEndReason.Defeat,
            FirstSeveranceTerminalCause.LoopCapExceeded);

        FirstSeveranceLoopState frozen = machine.State;
        AssertEqual(
            FirstSeveranceLoopUpdateDisposition.NoChange,
            machine.Advance(new FirstSeveranceLoopInput(
                frozen.LastAuthorityTick + 1,
                0,
                0)).Disposition,
            "post-terminal update");
        AssertEqual(frozen, machine.State, "post-terminal state");
    }

    private static void TerminalCauseValuesAndMappingsAreStable()
    {
        var expected = new (FirstSeveranceTerminalCause Cause, byte Value, EncounterEndReason Reason)[]
        {
            (FirstSeveranceTerminalCause.BossLifeZero, 1, EncounterEndReason.Victory),
            (FirstSeveranceTerminalCause.OverloadLimit, 2, EncounterEndReason.Defeat),
            (FirstSeveranceTerminalCause.AllParticipantsDowned, 3, EncounterEndReason.Defeat),
            (FirstSeveranceTerminalCause.RecoveryImpossible, 4, EncounterEndReason.Defeat),
            (FirstSeveranceTerminalCause.LoopCapExceeded, 5, EncounterEndReason.Defeat),
            (FirstSeveranceTerminalCause.UserCancelled, 6, EncounterEndReason.Cancelled),
            (FirstSeveranceTerminalCause.FoundationCoreLost, 7, EncounterEndReason.AnchorDestroyed),
            (FirstSeveranceTerminalCause.BossActorMissing, 8, EncounterEndReason.EncounterActorMissing),
            (FirstSeveranceTerminalCause.RuntimeInvariantBroken, 9, EncounterEndReason.Invalidated),
            (FirstSeveranceTerminalCause.AdministrativeAbort, 10, EncounterEndReason.Invalidated),
            (FirstSeveranceTerminalCause.WorldUnload, 11, EncounterEndReason.WorldUnload),
            (FirstSeveranceTerminalCause.ProtocolFailure, 12, EncounterEndReason.ProtocolFailure),
            (FirstSeveranceTerminalCause.InternalFailure, 13, EncounterEndReason.InternalFailure),
        };

        FirstSeveranceTerminationContract contract = FirstSeveranceTerminationContract.Instance;
        AssertEqual((byte)0, (byte)FirstSeveranceTerminalCause.None, "None cause value");
        for (int index = 0; index < expected.Length; index++)
        {
            AssertEqual(expected[index].Value, (byte)expected[index].Cause, "cause byte value");
            EncounterTerminationDescriptor descriptor = contract.Create(expected[index].Cause);
            AssertEqual(true, contract.IsValid(descriptor), "terminal descriptor validity");
            AssertEqual(expected[index].Reason, descriptor.EndReason, "generic terminal reason");
            AssertEqual(
                expected[index].Cause,
                contract.GetCause(descriptor),
                "feature terminal cause");
            AssertEqual(
                FirstSeveranceTerminationContract.SchemaId,
                descriptor.Feature.SchemaId,
                "feature schema ID");
            AssertEqual(
                FirstSeveranceTerminationContract.SchemaVersion,
                descriptor.Feature.SchemaVersion,
                "feature schema version");
        }

        AssertThrows<ArgumentOutOfRangeException>(
            () => contract.Create(FirstSeveranceTerminalCause.None),
            "None terminal cause");
    }

    private static void FeatureTerminalPriorityIsTotal()
    {
        FirstSeveranceTerminalCause[] descending =
        {
            FirstSeveranceTerminalCause.BossActorMissing,
            FirstSeveranceTerminalCause.FoundationCoreLost,
            FirstSeveranceTerminalCause.RuntimeInvariantBroken,
            FirstSeveranceTerminalCause.AdministrativeAbort,
            FirstSeveranceTerminalCause.BossLifeZero,
            FirstSeveranceTerminalCause.OverloadLimit,
            FirstSeveranceTerminalCause.AllParticipantsDowned,
            FirstSeveranceTerminalCause.RecoveryImpossible,
            FirstSeveranceTerminalCause.LoopCapExceeded,
            FirstSeveranceTerminalCause.UserCancelled,
        };

        FirstSeveranceTerminationContract contract = FirstSeveranceTerminationContract.Instance;
        for (int index = 0; index < descending.Length - 1; index++)
        {
            AssertEqual(
                descending[index],
                contract.SelectHigherPriority(descending[index], descending[index + 1]),
                "forward terminal priority");
            AssertEqual(
                descending[index],
                contract.SelectHigherPriority(descending[index + 1], descending[index]),
                "reverse terminal priority");
        }
    }

    private static void ExternalTerminalMapsRejectInvalidDefinitions()
    {
        FirstSeveranceTerminationContract contract = FirstSeveranceTerminationContract.Instance;
        IEncounterRuntimeFactory factory = new TestRuntimeFactory(
            _ => new TestRuntime(_ => EncounterRuntimeUpdate.None));

        AssertThrows<ArgumentException>(
            () => _ = new TestEncounterDefinition(
                factory,
                new[]
                {
                    contract.Create(FirstSeveranceTerminalCause.WorldUnload),
                    contract.Create(FirstSeveranceTerminalCause.InternalFailure),
                }),
            "missing ProtocolFailure mapping");

        AssertThrows<ArgumentException>(
            () => _ = new TestEncounterDefinition(
                factory,
                new[]
                {
                    contract.Create(FirstSeveranceTerminalCause.WorldUnload),
                    contract.Create(FirstSeveranceTerminalCause.WorldUnload),
                    contract.Create(FirstSeveranceTerminalCause.ProtocolFailure),
                }),
            "duplicate WorldUnload mapping");

        AssertThrows<ArgumentException>(
            () => _ = new TestEncounterDefinition(
                factory,
                new[]
                {
                    EncounterTerminationDescriptor.None,
                    contract.Create(FirstSeveranceTerminalCause.InternalFailure),
                    contract.Create(FirstSeveranceTerminalCause.ProtocolFailure),
                }),
            "None external mapping");

        EncounterTerminationDescriptor incompatible = EncounterTerminationDescriptor.Create(
            EncounterEndReason.WorldUnload,
            EncounterFeatureTermination.Create(
                FirstSeveranceTerminationContract.SchemaId,
                FirstSeveranceTerminationContract.SchemaVersion,
                (byte)FirstSeveranceTerminalCause.InternalFailure));
        AssertThrows<ArgumentException>(
            () => _ = new TestEncounterDefinition(
                factory,
                new[]
                {
                    incompatible,
                    contract.Create(FirstSeveranceTerminalCause.InternalFailure),
                    contract.Create(FirstSeveranceTerminalCause.ProtocolFailure),
                }),
            "incompatible external mapping");
    }

    private static void RuntimeEndPublishesFeatureMetadata()
    {
        EncounterCoordinator? coordinator = null;
        var runtime = new TestRuntime(
            _ => EncounterRuntimeUpdate.End(
                FirstSeveranceTerminationContract.Instance.Create(
                    FirstSeveranceTerminalCause.LoopCapExceeded)),
            onCleanup: context =>
            {
                AssertEqual(true, coordinator?.LastTerminalSnapshot.HasValue, "publication before cleanup");
                AssertEqual(
                    FirstSeveranceTerminalCause.LoopCapExceeded,
                    GetCause(context.Termination),
                    "cleanup feature cause");
            });
        coordinator = CreateCoordinator(new TestRuntimeFactory(_ => runtime));
        StartTestEncounter(coordinator);
        coordinator.Tick();

        AssertEqual(false, coordinator.HasActiveSession, "active session after runtime End");
        EncounterSnapshot terminal = coordinator.LastTerminalSnapshot
            ?? throw new InvalidOperationException("Terminal snapshot was not retained.");
        AssertEqual(EncounterLifecycle.Cleanup, terminal.Lifecycle, "terminal lifecycle");
        AssertTermination(
            terminal.Termination,
            EncounterEndReason.Defeat,
            FirstSeveranceTerminalCause.LoopCapExceeded);
        AssertEqual(true, runtime.CleanupContext.HasValue, "runtime cleanup");

        AssertEqual(true, coordinator.TryTakePublishedSnapshot(out EncounterSnapshot published), "outbox terminal");
        AssertEqual(terminal, published, "terminal publication priority");
    }

    private static void ExternalTerminationPriorityPreemptsRuntime()
    {
        var runtime = new TestRuntime(_ => EncounterRuntimeUpdate.ObservableChange());
        EncounterCoordinator coordinator = CreateCoordinator(new TestRuntimeFactory(_ => runtime));
        StartTestEncounter(coordinator);

        AssertEqual(
            true,
            coordinator.RequestExternalTermination(EncounterEndReason.ProtocolFailure),
            "ProtocolFailure request");
        AssertEqual(
            true,
            coordinator.RequestExternalTermination(EncounterEndReason.InternalFailure),
            "InternalFailure request");
        AssertEqual(
            true,
            coordinator.RequestExternalTermination(EncounterEndReason.WorldUnload),
            "WorldUnload request");
        coordinator.Tick();

        AssertEqual(0, runtime.TickCount, "preempted runtime tick count");
        EncounterSnapshot terminal = coordinator.LastTerminalSnapshot
            ?? throw new InvalidOperationException("External terminal snapshot was not retained.");
        AssertTermination(
            terminal.Termination,
            EncounterEndReason.WorldUnload,
            FirstSeveranceTerminalCause.WorldUnload);

        var protocolRuntime = new TestRuntime(_ => EncounterRuntimeUpdate.None);
        EncounterCoordinator protocolCoordinator = CreateCoordinator(
            new TestRuntimeFactory(_ => protocolRuntime));
        StartTestEncounter(protocolCoordinator);
        AssertEqual(
            true,
            protocolCoordinator.RequestExternalTermination(EncounterEndReason.ProtocolFailure),
            "standalone protocol request");
        protocolCoordinator.Tick();
        AssertTermination(
            protocolCoordinator.LastTerminalSnapshot?.Termination
                ?? throw new InvalidOperationException("Protocol terminal snapshot was not retained."),
            EncounterEndReason.ProtocolFailure,
            FirstSeveranceTerminalCause.ProtocolFailure);

        var resetRuntime = new TestRuntime(_ => EncounterRuntimeUpdate.None);
        EncounterCoordinator resetCoordinator = CreateCoordinator(
            new TestRuntimeFactory(_ => resetRuntime));
        StartTestEncounter(resetCoordinator);
        resetCoordinator.Reset(EncounterEndReason.WorldUnload);
        AssertTermination(
            resetCoordinator.LastTerminalSnapshot?.Termination
                ?? throw new InvalidOperationException("Reset terminal snapshot was not retained."),
            EncounterEndReason.WorldUnload,
            FirstSeveranceTerminalCause.WorldUnload);
        AssertEqual(0, resetRuntime.TickCount, "Reset runtime tick count");
        AssertEqual(true, resetRuntime.CleanupContext.HasValue, "Reset cleanup");
    }

    private static void InvalidRuntimeEndBecomesInternalFailure()
    {
        EncounterTerminationDescriptor incompatible = EncounterTerminationDescriptor.Create(
            EncounterEndReason.Defeat,
            EncounterFeatureTermination.Create(
                FirstSeveranceTerminationContract.SchemaId,
                FirstSeveranceTerminationContract.SchemaVersion,
                (byte)FirstSeveranceTerminalCause.BossLifeZero));
        var exceptions = new List<Exception>();
        var runtime = new TestRuntime(_ => EncounterRuntimeUpdate.End(incompatible));
        EncounterCoordinator coordinator = CreateCoordinator(
            new TestRuntimeFactory(_ => runtime),
            exceptions.Add);
        StartTestEncounter(coordinator);
        coordinator.Tick();

        AssertEqual(1, exceptions.Count, "incompatible runtime diagnostic");
        AssertTermination(
            coordinator.LastTerminalSnapshot?.Termination
                ?? throw new InvalidOperationException("Fallback terminal snapshot was not retained."),
            EncounterEndReason.InternalFailure,
            FirstSeveranceTerminalCause.InternalFailure);
    }

    private static void RuntimeExceptionsUseExternalMapping()
    {
        var exceptions = new List<Exception>();
        var runtime = new TestRuntime(_ => throw new InvalidOperationException("tick failed"));
        EncounterCoordinator coordinator = CreateCoordinator(
            new TestRuntimeFactory(_ => runtime),
            exceptions.Add);
        StartTestEncounter(coordinator);
        coordinator.Tick();

        AssertEqual(1, runtime.TickCount, "throwing runtime tick count");
        AssertEqual(1, exceptions.Count, "reported runtime exceptions");
        AssertTermination(
            coordinator.LastTerminalSnapshot?.Termination
                ?? throw new InvalidOperationException("Internal terminal snapshot was not retained."),
            EncounterEndReason.InternalFailure,
            FirstSeveranceTerminalCause.InternalFailure);
        AssertEqual(true, runtime.CleanupContext.HasValue, "exception cleanup");
    }

    private static void RuntimeCreationFailureHasNoTombstone()
    {
        var cleanup = new TestCleanupParticipant();
        var factory = new TestRuntimeFactory(context =>
        {
            context.CleanupRegistrar.Register(cleanup);
            throw new InvalidOperationException("construction failed");
        });
        var exceptions = new List<Exception>();
        EncounterCoordinator coordinator = CreateCoordinator(factory, exceptions.Add);

        bool started = coordinator.TryStart(
            TestStartCommand(),
            out EncounterSnapshot snapshot,
            out string failureCode);
        AssertEqual(false, started, "runtime creation start result");
        AssertEqual("encounter.runtime_creation_failure", failureCode, "creation failure code");
        AssertEqual(false, coordinator.HasActiveSession, "creation-failure active session");
        AssertEqual<EncounterSnapshot?>(null, coordinator.LastTerminalSnapshot, "creation tombstone");
        AssertEqual(false, coordinator.TryTakePublishedSnapshot(out _), "creation publication");
        AssertEqual(0UL, snapshot.EncounterSequence, "creation snapshot sequence");
        AssertEqual(1, exceptions.Count, "creation exception report");
        AssertEqual(true, cleanup.Context.HasValue, "partial construction cleanup");
        AssertTermination(
            cleanup.Context?.Termination
                ?? throw new InvalidOperationException("Creation cleanup context is missing."),
            EncounterEndReason.InternalFailure,
            FirstSeveranceTerminalCause.InternalFailure);
    }

    private static void ReplicaRetainsFeatureTerminalTombstone()
    {
        FightId fightId = FightId.FromWire(
            new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        FirstSeveranceTerminationContract contract = FirstSeveranceTerminationContract.Instance;
        var replica = new EncounterReplica();
        var live = new EncounterSnapshot(
            EncounterSequence: 1,
            fightId,
            DefinitionKey: "first_severance",
            EncounterLifecycle.Active,
            Revision: 1,
            AuthorityTick: 10,
            LifecycleEnteredTick: 5,
            ActiveFightTick: 5,
            EncounterTerminationDescriptor.None);
        AssertEqual(true, replica.ApplyFullSnapshot(live, contract), "live replica snapshot");

        EncounterTerminationDescriptor loopCap = contract.Create(
            FirstSeveranceTerminalCause.LoopCapExceeded);
        EncounterSnapshot terminal = live with
        {
            Lifecycle = EncounterLifecycle.Cleanup,
            Revision = 2,
            AuthorityTick = 11,
            Termination = loopCap,
        };
        AssertEqual(true, replica.ApplyFullSnapshot(terminal, contract), "terminal replica snapshot");
        AssertEqual(terminal, replica.LastTerminalSnapshot, "retained terminal snapshot");
        AssertEqual(
            false,
            replica.ApplyFullSnapshot(live with { Revision = 3, AuthorityTick = 12 }, contract),
            "delayed live snapshot");
        AssertTermination(
            replica.Snapshot.Termination,
            EncounterEndReason.Defeat,
            FirstSeveranceTerminalCause.LoopCapExceeded);

        EncounterTerminationDescriptor unknown = EncounterTerminationDescriptor.Create(
            EncounterEndReason.Defeat,
            EncounterFeatureTermination.Create(
                FirstSeveranceTerminationContract.SchemaId,
                FirstSeveranceTerminationContract.SchemaVersion,
                byte.MaxValue));
        EncounterSnapshot unknownSnapshot = terminal with
        {
            EncounterSequence = 2,
            FightId = FightId.FromWire(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            Revision = 1,
            AuthorityTick = 20,
            Termination = unknown,
        };
        AssertEqual(
            false,
            replica.ApplyFullSnapshot(unknownSnapshot, contract),
            "unknown feature cause");

        EncounterTerminationDescriptor incompatible = EncounterTerminationDescriptor.Create(
            EncounterEndReason.Defeat,
            EncounterFeatureTermination.Create(
                FirstSeveranceTerminationContract.SchemaId,
                FirstSeveranceTerminationContract.SchemaVersion,
                (byte)FirstSeveranceTerminalCause.BossLifeZero));
        AssertEqual(
            false,
            replica.ApplyFullSnapshot(unknownSnapshot with { Termination = incompatible }, contract),
            "incompatible feature cause");
    }

    private static FirstSeveranceLoopStateMachine CreateLoopMachine(
        int participantCount,
        int bossMaximumLife = 1_000)
    {
        return new FirstSeveranceLoopStateMachine(
            FirstSeveranceEncounterPlan.Instance,
            participantCount,
            bossMaximumLife,
            startTick: 0);
    }

    private static void EnterPylonCheck(FirstSeveranceLoopStateMachine machine)
    {
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.PylonCheck);
    }

    private static void ClearCurrentPylons(FirstSeveranceLoopStateMachine machine)
    {
        AssertEqual(FirstSeveranceSubstate.PylonCheck, machine.State.Substate, "Pylon source state");
        ulong tick = machine.State.SubstateEnteredTick
            + (ulong)FirstSeveranceEncounterPlan.Instance.Timing.PylonTelegraphTicks;
        AssertApplied(machine.Advance(new FirstSeveranceLoopInput(
            tick,
            machine.State.RemainingPylons,
            AcceptedBossDamage: 0)));
        AssertEqual(FirstSeveranceSubstate.Stack, machine.State.Substate, "Pylon success edge");
    }

    private static void AdvanceAtDeadline(
        FirstSeveranceLoopStateMachine machine,
        FirstSeveranceSubstate expectedSubstate)
    {
        AssertApplied(machine.Advance(new FirstSeveranceLoopInput(
            machine.State.ResolveTick,
            DestroyedPylons: 0,
            AcceptedBossDamage: 0)));
        AssertEqual(expectedSubstate, machine.State.Substate, "deadline edge");
    }

    private static EncounterCoordinator CreateCoordinator(
        IEncounterRuntimeFactory factory,
        Action<Exception>? exceptionSink = null)
    {
        var registry = new EncounterRegistry();
        registry.Register(new TestEncounterDefinition(
            factory,
            FirstSeveranceTerminationContract.Instance.ExternalTerminations));
        return new EncounterCoordinator(
            registry,
            Array.Empty<IEncounterActivationPolicy>(),
            exceptionSink ?? (_ => { }));
    }

    private static void StartTestEncounter(EncounterCoordinator coordinator)
    {
        if (!coordinator.TryStart(
            TestStartCommand(),
            out _,
            out string failureCode))
        {
            throw new InvalidOperationException($"Test encounter start failed: '{failureCode}'.");
        }
    }

    private static EncounterStartCommand TestStartCommand()
    {
        return new EncounterStartCommand(
            SenderWhoAmI: 0,
            DefinitionKey: TestEncounterDefinition.TestKey,
            RequestedAnchor: new TilePoint(1, 1),
            RequestNonce: 1);
    }

    private static FirstSeveranceTerminalCause GetCause(
        in EncounterTerminationDescriptor termination)
    {
        return FirstSeveranceTerminationContract.Instance.GetCause(termination);
    }

    private static void AssertTermination(
        in EncounterTerminationDescriptor termination,
        EncounterEndReason expectedReason,
        FirstSeveranceTerminalCause expectedCause)
    {
        AssertEqual(expectedReason, termination.EndReason, "terminal generic reason");
        AssertEqual(expectedCause, GetCause(termination), "terminal feature cause");
    }

    private static void AssertEnumValues<TEnum>(
        IReadOnlyList<TEnum> expected,
        string context)
        where TEnum : struct, Enum
    {
        TEnum[] actual = Enum.GetValues<TEnum>();
        AssertEqual(expected.Count, actual.Length, $"{context} count");
        for (int index = 0; index < actual.Length; index++)
        {
            AssertEqual(expected[index], actual[index], $"{context}[{index}]");
        }
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

    private static void AssertApplied(FirstSeveranceLoopUpdate result)
    {
        if (result.Disposition != FirstSeveranceLoopUpdateDisposition.Applied)
        {
            throw new InvalidOperationException(
                $"Expected applied loop update, got {result.Disposition} "
                + $"'{result.FailureCode}'.");
        }
    }

    private static void AssertEnded(FirstSeveranceLoopUpdate result)
    {
        if (result.Disposition != FirstSeveranceLoopUpdateDisposition.Ended)
        {
            throw new InvalidOperationException(
                $"Expected terminal loop update, got {result.Disposition} "
                + $"'{result.FailureCode}'.");
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

    private static void AssertThrows<TException>(Action action, string context)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected {context} to throw {typeof(TException).Name}.");
    }
}
