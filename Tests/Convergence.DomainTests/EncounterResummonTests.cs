#nullable enable
using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking.Replication;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Re-summon receives terminal then Idle after victory and defeat")]
    private static void ResummonAfterTerminal()
    {
        foreach (var cause in new[] { FirstSeveranceTerminalCause.BossLifeZero, FirstSeveranceTerminalCause.LoopCapExceeded })
        {
            var contract = FirstSeveranceTerminationContract.Instance;
            int cleanups = 0;
            var coordinator = CreateCoordinator(new TestRuntimeFactory(_ => new TestRuntime(
                _ => EncounterRuntimeUpdate.End(contract.Create(cause)), _ => cleanups++)));
            var replica = new EncounterReplica();
            for (int fight = 1; fight <= 2; fight++)
            {
                StartTestEncounter(coordinator);
                AssertEqual(true, coordinator.TryTakePublishedSnapshot(out var live), "start snapshot");
                AssertEqual(true, replica.ApplyFullSnapshot(live, contract), "client accepts new fight");
                coordinator.Tick();
                AssertEqual(true, coordinator.TryTakePublishedSnapshot(out var terminal), "terminal published");
                AssertEqual(EncounterLifecycle.Cleanup, terminal.Lifecycle, "terminal precedes Idle");
                AssertEqual(true, replica.ApplyFullSnapshot(terminal, contract), "client receives outcome");
                AssertEqual(true, coordinator.TryTakePublishedSnapshot(out var idle), "Idle must be published without a repair request");
                AssertEqual(coordinator.Snapshot, idle, "authority Idle projection");
                AssertEqual(true, idle.Revision > terminal.Revision, "Idle is newer even on the same tick");
                AssertEqual(true, replica.ApplyFullSnapshot(idle, null), "client accepts Idle");
                AssertEqual(EncounterLifecycle.Idle, replica.Snapshot.Lifecycle, "summon item becomes usable");
                AssertEqual(terminal, replica.LastTerminalSnapshot!.Value, "outcome retained for presentation");
                AssertEqual(false, replica.ApplyFullSnapshot(terminal, contract), "duplicate terminal cannot restore Cleanup");
                AssertEqual(false, replica.ApplyFullSnapshot(live with { Revision = idle.Revision + 1, AuthorityTick = idle.AuthorityTick + 1 }, contract), "old fight cannot resume");
                AssertEqual(false, coordinator.TryTakePublishedSnapshot(out _), "no repeated Idle flood");
            }
            AssertEqual(2, cleanups, "one cleanup per fight");
        }
    }

    [DomainTest("Re-summon coalescing retains terminal before the next fight")]
    private static void ResummonBeforeOutboxDrain()
    {
        var contract = FirstSeveranceTerminationContract.Instance;
        var coordinator = CreateCoordinator(new TestRuntimeFactory(_ => new TestRuntime(
            _ => EncounterRuntimeUpdate.End(contract.Create(FirstSeveranceTerminalCause.BossLifeZero)))));
        var replica = new EncounterReplica();
        StartTestEncounter(coordinator);
        var first = coordinator.Snapshot;
        AssertEqual(true, replica.ApplyFullSnapshot(first, contract), "first client state");
        coordinator.Tick();
        StartTestEncounter(coordinator); // The new live snapshot may coalesce the pending Idle.
        AssertEqual(true, coordinator.TryTakePublishedSnapshot(out var terminal), "previous outcome retained");
        AssertEqual(true, terminal.IsTerminal, "outcome sent before next fight");
        AssertEqual(true, replica.ApplyFullSnapshot(terminal, contract), "client accepts outcome");
        AssertEqual(true, coordinator.TryTakePublishedSnapshot(out var next), "new fight published");
        AssertEqual(first.EncounterSequence + 1, next.EncounterSequence, "new sequence");
        AssertEqual(false, first.FightId == next.FightId, "new identity");
        AssertEqual(true, replica.ApplyFullSnapshot(next, contract), "new fight supersedes old Idle");
        AssertEqual(false, replica.ApplyFullSnapshot(terminal, contract), "old outcome cannot replace new fight");
    }

    [DomainTest("Re-summon Idle does not bypass pending cleanup")]
    private static void ResummonWaitsForCleanupRetry()
    {
        int attempts = 0;
        var contract = FirstSeveranceTerminationContract.Instance;
        var coordinator = CreateCoordinator(new TestRuntimeFactory(_ => new TestRuntime(
            _ => EncounterRuntimeUpdate.End(contract.Create(FirstSeveranceTerminalCause.BossLifeZero)),
            _ => { if (++attempts == 1) throw new InvalidOperationException("simulated cleanup retry"); })));
        StartTestEncounter(coordinator);
        coordinator.Tick();
        AssertEqual(true, coordinator.HasPendingCleanup, "failed cleanup remains owned");
        AssertEqual(false, coordinator.TryStart(TestStartCommand(), out _, out var failure), "start still blocked on authority");
        AssertEqual("encounter.cleanup_pending", failure, "specific rejection reason");
        for (int tick = 0; tick < 60; tick++) coordinator.Tick();
        AssertEqual(false, coordinator.HasPendingCleanup, "retry completes");
        StartTestEncounter(coordinator);
        AssertEqual(2, attempts, "failed resource retried exactly once");
    }
}
