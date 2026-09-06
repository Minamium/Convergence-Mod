using System;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Client.Encounters.FirstSeverance;

// Disposable local presentation, started only from an accepted terminal reason.
// It cannot retain the Raid, delay death/cleanup, or write UI/control flags.
internal sealed class FirstSeveranceEndingTimeline
{
    private ulong startedTick;
    internal EncounterEndReason Reason { get; private set; }
    internal int DurationTicks => Reason switch
    {
        EncounterEndReason.Victory => 210,
        EncounterEndReason.Defeat => 180,
        _ => 0,
    };

    internal void Begin(EncounterEndReason reason, bool wasConnectedParticipant, ulong localTick)
    {
        Reason = wasConnectedParticipant && reason is EncounterEndReason.Victory or EncounterEndReason.Defeat
            ? reason : EncounterEndReason.None;
        startedTick = localTick;
    }

    internal bool IsActive(ulong localTick)
        => DurationTicks > 0 && localTick >= startedTick && localTick - startedTick < (ulong)DurationTicks;

    internal float Age(ulong localTick, double fraction = 0)
    {
        if (!IsActive(localTick)) return 1;
        double partial = double.IsFinite(fraction) ? Math.Clamp(fraction, 0, 1) : 0;
        return (float)Math.Clamp(((localTick - startedTick) + partial) / DurationTicks, 0, 1);
    }

    internal void Clear()
    {
        Reason = EncounterEndReason.None;
        startedTick = 0;
    }
}
