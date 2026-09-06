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

    private static int nextFightNumber = 1;

private static FirstSeveranceArenaLayout CreateValidArenaLayout()
    {
        var worldBounds = new TileRectangle(0, 0, 8_400, 2_400);
        var core = new ResolvedFirstSeveranceCoreAnchor(
            new TilePoint(4_200, 1_250),
            BaseY: 1_300,
            ServerTileEntityId: 7);
        if (!FirstSeveranceArenaBlueprint.Instance.TryCreateLayout(
                core,
                worldBounds,
                out FirstSeveranceArenaLayout? layout,
                out string failureCode)
            || layout is null)
        {
            throw new InvalidOperationException(
                $"Test Arena layout could not be created: '{failureCode}'.");
        }

        return layout;
    }

    private static FirstSeveranceRosterCandidate SelectableCandidate(
        int serverWhoAmI,
        ulong connectionEpoch)
    {
        return new FirstSeveranceRosterCandidate(
            serverWhoAmI,
            connectionEpoch,
            IsConnected: true,
            IsEligible: true,
            IsWithinParticipationRegion: true);
    }

    private static FirstSeveranceRoster CreatePreparationRoster()
    {
        FirstSeveranceRosterCandidate[] candidates =
        {
            SelectableCandidate(9, 90),
            SelectableCandidate(2, 20),
            SelectableCandidate(7, 70),
        };
        if (!FirstSeveranceRoster.TryCreate(
                candidates,
                initiatorWhoAmI: 7,
                initiatorConnectionEpoch: 70,
                out FirstSeveranceRoster? roster,
                out string failureCode)
            || roster is null)
        {
            throw new InvalidOperationException(
                $"Test preparation roster could not be created: '{failureCode}'.");
        }

        return roster;
    }

    private static FirstSeveranceConnectionObservation[] CurrentPreparationConnections()
    {
        return
        [
            new FirstSeveranceConnectionObservation(2, 20, true),
            new FirstSeveranceConnectionObservation(7, 70, true),
            new FirstSeveranceConnectionObservation(9, 90, true),
        ];
    }

    private static FightId TestFightId(string value)
    {
        return FightId.FromWire(Guid.Parse(value));
    }

    private static void AssertRosterRejected(
        IReadOnlyList<FirstSeveranceRosterCandidate> candidates,
        int initiatorWhoAmI,
        ulong initiatorConnectionEpoch,
        string expectedFailureCode)
    {
        bool created = FirstSeveranceRoster.TryCreate(
            candidates,
            initiatorWhoAmI,
            initiatorConnectionEpoch,
            out FirstSeveranceRoster? roster,
            out string failureCode);
        AssertEqual(false, created, $"roster rejection '{expectedFailureCode}'");
        AssertEqual<FirstSeveranceRoster?>(null, roster, "rejected roster");
        AssertEqual(expectedFailureCode, failureCode, "roster rejection code");
    }

    private static void AssertPreparationRejected(
        in FirstSeverancePreparationUpdate update,
        string expectedFailureCode)
    {
        AssertEqual(false, update.IsAccepted, "preparation rejection acceptance");
        AssertEqual(false, update.HasObservableChange, "preparation rejection change");
        AssertEqual(false, update.RequestsEnd, "preparation rejection terminal");
        AssertEqual(expectedFailureCode, update.FailureCode, "preparation rejection code");
    }

    private static void AssertPreparationEnded(
        in FirstSeverancePreparationUpdate update,
        string expectedDiagnosticCode)
    {
        AssertEqual(true, update.IsAccepted, "preparation end acceptance");
        AssertEqual(true, update.HasObservableChange, "preparation end change");
        AssertEqual(true, update.RequestsEnd, "preparation end terminal");
        AssertEqual(expectedDiagnosticCode, update.FailureCode, "preparation end diagnostics");
        AssertTermination(
            update.RequestedTermination,
            EncounterEndReason.Cancelled,
            FirstSeveranceTerminalCause.UserCancelled);
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
