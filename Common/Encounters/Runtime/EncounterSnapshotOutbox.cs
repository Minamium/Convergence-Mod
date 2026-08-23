#nullable enable

using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterSnapshotOutbox
{
    private const int MaximumPendingTerminalSnapshots = 64;
    private readonly Queue<EncounterSnapshot> terminalSnapshots = new();
    private EncounterSnapshot? latestLiveSnapshot;

    public void Publish(in EncounterSnapshot snapshot)
    {
        if (!snapshot.IsTerminal)
        {
            // Full snapshots are convergent, so stale live revisions provide no value
            // while a consumer is delayed. Keep only the newest projection.
            latestLiveSnapshot = snapshot;
            return;
        }

        if (latestLiveSnapshot is { } pending
            && pending.EncounterSequence == snapshot.EncounterSequence)
        {
            latestLiveSnapshot = null;
        }

        // Terminal state has delivery priority over live projection. The bound still
        // prevents an abandoned consumer from growing memory without limit.
        while (terminalSnapshots.Count >= MaximumPendingTerminalSnapshots)
        {
            terminalSnapshots.Dequeue();
        }

        terminalSnapshots.Enqueue(snapshot);
    }

    public bool TryTake(out EncounterSnapshot snapshot)
    {
        if (terminalSnapshots.TryDequeue(out snapshot))
        {
            return true;
        }

        if (latestLiveSnapshot is { } latest)
        {
            snapshot = latest;
            latestLiveSnapshot = null;
            return true;
        }

        snapshot = default;
        return false;
    }

    public void Clear()
    {
        terminalSnapshots.Clear();
        latestLiveSnapshot = null;
    }
}
