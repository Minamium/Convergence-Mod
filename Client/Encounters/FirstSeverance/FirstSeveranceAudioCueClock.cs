using System.Collections.Generic;

namespace Convergence.Client.Encounters.FirstSeverance;

// One action-local clock ledger, independent of a ray's short damaging interval.
// A future event is not consumed when a snapshot arrives ahead of the local clock.
internal sealed class FirstSeveranceAudioCueClock
{
    private readonly HashSet<int> consumed = new();

    internal bool Take(int key, ulong now, ulong due, ulong maximumLateness = 30)
    {
        if (now < due || !consumed.Add(key)) return false;
        return now - due <= maximumLateness;
    }

    internal void Reset() => consumed.Clear();
}
