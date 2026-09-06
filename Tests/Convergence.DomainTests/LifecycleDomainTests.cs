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
    [DomainTest("Terminal cause values and mappings are stable")]
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

    [DomainTest("Feature terminal priority is total")]
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

    [DomainTest("External terminal maps reject invalid definitions")]
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

    [DomainTest("Runtime End publishes feature metadata before cleanup")]
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

    [DomainTest("External termination priority preempts runtime")]
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

    [DomainTest("Invalid runtime End becomes InternalFailure")]
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

    [DomainTest("Runtime exceptions use immutable external mapping")]
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

    [DomainTest("Runtime creation failure has no tombstone")]
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

    [DomainTest("Replica retains the feature terminal tombstone")]
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
}
