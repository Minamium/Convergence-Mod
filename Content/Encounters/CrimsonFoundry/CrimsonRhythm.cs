using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal enum CrimsonRhythmKind : byte { Groove, Fill, Roll }
internal readonly record struct CrimsonRhythmHit(int Warning, int Fire, int End, byte Accent);
internal sealed record CrimsonRhythmPhrase(int Start, int End, CrimsonRhythmKind Kind, IReadOnlyList<CrimsonRhythmHit> Hits);

// Same basic pulse, reversed roles: even beats warn, following odd beats strike.
// Prime the first warning before firing; nothing can damage before its call.
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

    internal static CrimsonRhythmPhrase Create(CrimsonScore score, int earliest, int serial, bool final)
    {
        if (earliest < 0 || serial < 0) throw new ArgumentOutOfRangeException();
        // A fractional loop beat that rounds to earliest belongs to this bar.
        var beats = NextBeats(score, Math.Max(0, earliest - .499999d), 5);
        // Keep the reserved kind IDs and descriptor capacity; serial/final no
        // longer alter cadence, sound accent or the amount of warning.
        int At(int beat) => (int)Math.Round(beats[beat]);
        var hits = new CrimsonRhythmHit[2];
        for (int i = 0; i < hits.Length; i++)
        {
            int warning = At(i * 2 + 1), fire = At(i * 2 + 2);
            if (fire - warning < MinimumWarningTicks || fire - warning > MaximumWarningTicks)
                throw new InvalidOperationException("crimson.rhythm_unreadable_score");
            // Cadence schedules new releases, not an expiry deadline. Flight
            // continues through the following call, and harmless residue longer.
            hits[i] = new(warning, fire, fire + LiveTicks, 1);
        }
        return new(At(0), At(4), CrimsonRhythmKind.Groove, Array.AsReadOnly(hits));
    }
    internal static double[] NextBeats(CrimsonScore score, double earliest, int count)
    {
        if (count is < 1 or > 17 || !double.IsFinite(earliest) || earliest < 0)
            throw new ArgumentOutOfRangeException();
        var result = new List<double>(count);
        int cycle = earliest < score.LoopEnd ? 0 : 1 + (int)((earliest - score.LoopEnd) / score.LoopLength);
        for (int c = cycle; c <= cycle + count + 1 && result.Count < count; c++)
        {
            double shift = c == 0 ? 0 : c * score.LoopLength;
            foreach (int beat in score.BeatTicks)
            {
                if (c > 0 && beat < score.LoopStart) continue;
                double time = beat + shift;
                if (time < earliest || result.Count > 0 && time <= result[result.Count - 1]) continue;
                result.Add(time);
                if (result.Count == count) break;
            }
        }
        if (result.Count != count) throw new InvalidOperationException("crimson.rhythm_loop_has_no_beats");
        return result.ToArray();
    }
}
