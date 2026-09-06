namespace Convergence.Content.Encounters.FirstSeverance;

// Supplemental vanilla movement sync, not a new client outcome/position protocol.
// A stationary owner still publishes; change-only sync cannot bound replica drift.
internal sealed class FirstSeveranceMovementSyncCadence
{
    internal const ulong IntervalTicks = 6;
    private bool sent;
    private ulong lastTick;

    internal bool TryAdvance(ulong tick, bool eligible)
    {
        if (!eligible)
        {
            Clear();
            return false;
        }
        if (sent && tick >= lastTick && tick - lastTick < IntervalTicks) return false;
        sent = true;
        lastTick = tick;
        return true;
    }

    internal void Clear()
    {
        sent = false;
        lastTick = 0;
    }
}
