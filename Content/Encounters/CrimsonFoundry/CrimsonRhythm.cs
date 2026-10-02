using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal enum CrimsonRhythmKind : byte { Groove, Fill, Roll }
internal readonly record struct CrimsonRhythmHit(int Warning, int Fire, int End, byte Accent);
internal sealed record CrimsonRhythmPhrase(int Start, int End, CrimsonRhythmKind Kind, IReadOnlyList<CrimsonRhythmHit> Hits);

// Lifetimes on the shared 128 BPM grid (CrimsonMeter). A beat schedules a release,
// never a deadline that erases the previous attack.
internal static class CrimsonRhythm
{
    internal const int LookAheadTicks = 30;
    internal const int MinimumWarningTicks = 20;
    internal const int MaximumWarningTicks = 180;
    internal const int MaximumHits = 10; // Final: four paired beats plus one crossflow (nine notes).
    internal const int MaximumLanes = 40;
    internal const int LiveTicks = 32;
    internal const int ResidueTicks = 24;
    internal const int PoseRecoveryTicks = 6;
    internal const int LeaseTicks = ResidueTicks + 4;
}
