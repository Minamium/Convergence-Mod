using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal enum CrimsonRhythmKind : byte { Groove, Fill, Roll }
internal readonly record struct CrimsonRhythmHit(int Warning, int Fire, int End, byte Accent);
internal sealed record CrimsonRhythmPhrase(int Start, int End, CrimsonRhythmKind Kind, IReadOnlyList<CrimsonRhythmHit> Hits);

// Four-beat musical call/response. Absolute score times prevent rounding drift.
internal static class CrimsonRhythm
{
    internal const int LookAheadTicks = 30;
    internal const int MinimumWarningTicks = 40;
    internal const int MaximumWarningTicks = 180;
    internal const int MaximumHits = 5;
    internal const int MaximumLanes = 40;
    private static readonly int[] groove = { 0, 2, 4 };
    private static readonly int[] syncopated = { 0, 3, 4 };
    private static readonly int[] delayed = { 0, 2, 5 };
    private static readonly int[] flam = { 0, 1, 4 };
    private static readonly int[] fill = { 0, 1, 3, 5, 6 };
    private static readonly int[] roll = { 0, 1, 2, 3 };

    internal static CrimsonRhythmPhrase Create(CrimsonScore score, int earliest, int serial, bool final)
    {
        if (earliest < 0 || serial < 0) throw new ArgumentOutOfRangeException();
        // A fractional loop beat that rounds to earliest belongs to this bar.
        var beats = NextBeats(score, Math.Max(0, earliest - .499999d), 5);
        float energy = score.Intensity(beats[0]);
        var kind = serial % 4 == 3 ? CrimsonRhythmKind.Fill
            : final && energy >= .76f && serial % 3 == 2 ? CrimsonRhythmKind.Roll : CrimsonRhythmKind.Groove;
        int[] pattern = kind == CrimsonRhythmKind.Fill ? fill : kind == CrimsonRhythmKind.Roll ? roll
            : serial % 6 == 1 ? syncopated : serial % 6 == 2 ? delayed : serial % 6 == 4 ? flam : groove;
        int At(int step) => step == 16 ? (int)Math.Round(beats[4])
            : (int)Math.Round(beats[step / 4] + (beats[step / 4 + 1] - beats[step / 4]) * (step % 4) / 4d);
        var hits = new CrimsonRhythmHit[pattern.Length];
        for (int i = 0; i < pattern.Length; i++)
        {
            int warning = At(pattern[i]), fire = At(pattern[i] + 8);
            int next = i + 1 == pattern.Length ? At(16) : At(pattern[i + 1] + 8);
            if (fire - warning < MinimumWarningTicks || fire - warning > MaximumWarningTicks || next - fire < 4)
                throw new InvalidOperationException("crimson.rhythm_unreadable_score");
            int live = Math.Min(5, next - fire - 2);
            hits[i] = new(warning, fire, fire + live, (byte)(i == pattern.Length - 1 ? 2 : i == 0 ? 1 : 0));
        }
        return new(At(0), At(16), kind, Array.AsReadOnly(hits));
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
