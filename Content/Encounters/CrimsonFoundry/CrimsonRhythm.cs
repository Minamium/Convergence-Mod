using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal enum CrimsonRhythmKind : byte { Groove, Fill, Roll }
// Pulse is the note's identity inside its phrase (CrimsonChoreography: 0..3 ordinary notes or signature
// steps, Closer, Pickup); it is not the note's position in time.
internal readonly record struct CrimsonRhythmHit(int Warning, int Fire, int End, byte Accent, byte Pulse);
internal sealed record CrimsonRhythmPhrase(int Start, int End, CrimsonRhythmKind Kind, IReadOnlyList<CrimsonRhythmHit> Hits)
{
    // The earliest forecast of the phrase: a pickup crossflow warns two beats before Start.
    internal int FirstWarning
    {
        get
        {
            int first = int.MaxValue;
            foreach (var hit in Hits) first = Math.Min(first, hit.Warning);
            return first;
        }
    }
}

// Lifetimes on the shared 128 BPM grid (CrimsonMeter). A beat schedules a release,
// never a deadline that erases the previous attack.
internal static class CrimsonRhythm
{
    // A phrase is issued this long before its first forecast (replication lead), not before its bar head.
    internal const int LookAheadTicks = 30;
    internal const int MinimumWarningTicks = 20;
    internal const int MaximumWarningTicks = 180;
    internal const int MaximumHits = 10; // Final: a pickup, three paired notes and the closing cluster (eight notes); pulses stay below 10.
    internal const int MaximumLanes = 40;
    internal const int LiveTicks = 32;
    internal const int ResidueTicks = 24;
    internal const int PoseRecoveryTicks = 6;
    internal const int LeaseTicks = ResidueTicks + 4;
}
