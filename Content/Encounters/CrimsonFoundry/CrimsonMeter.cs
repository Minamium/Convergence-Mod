using System;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Graceful Ordeal is a strict 128 BPM recording: one beat is exactly 22,500 samples
// (28.125 ticks) and song bar n starts at sample 90,000n (bar 0 is a silent lead-in).
// Gameplay keeps one linear grid from musicStart, which plays song bar 1. Only the
// client re-sequences bars, so authority never depends on what the speakers play.
internal static class CrimsonMeter
{
    internal const int SampleRate = 48000, SamplesPerTick = SampleRate / 60;
    internal const int BeatSamples = 22500, BeatsPerBar = 4, BarSamples = BeatSamples * BeatsPerBar;
    internal const int OpeningBars = 8, SummonBar = 4;
    internal const int MaximumBeat = 72000 * 8 / 225;

    // Beat k after musicStart, rounded half up to a whole tick (28.125k).
    internal static int BeatTick(int beat)
        => beat is >= 0 and <= MaximumBeat ? (beat * 225 + 4) / 8 : throw new ArgumentOutOfRangeException(nameof(beat));
    internal static int BarTick(int bar) => BeatTick(bar * BeatsPerBar);
    // Eighth note e after musicStart (14.0625e), rounded half up; EighthTick(2k) == BeatTick(k).
    internal static int EighthTick(int eighth)
        => eighth is >= 0 and <= MaximumBeat * 2 ? (eighth * 225 + 8) / 16 : throw new ArgumentOutOfRangeException(nameof(eighth));
    internal static int BeatAtOrAfter(double tick)
    {
        if (!double.IsFinite(tick)) throw new ArgumentOutOfRangeException(nameof(tick));
        int beat = Math.Clamp((int)Math.Floor(tick * 8 / 225), 0, MaximumBeat);
        while (beat < MaximumBeat && BeatTick(beat) < tick) beat++;
        while (beat > 0 && BeatTick(beat - 1) >= tick) beat--;
        return beat;
    }
    internal static int BarAtOrAfter(double tick) => (BeatAtOrAfter(tick) + BeatsPerBar - 1) / BeatsPerBar;
    internal static int BarAt(double tick)
    {
        int bar = BarAtOrAfter(Math.Max(0, tick));
        return BarTick(bar) > tick ? Math.Max(0, bar - 1) : bar;
    }
    internal static double[] NextBeats(double earliest, int count)
    {
        if (count is < 1 or > 17 || !double.IsFinite(earliest) || earliest < 0) throw new ArgumentOutOfRangeException();
        int first = BeatAtOrAfter(earliest);
        var beats = new double[count];
        for (int i = 0; i < count; i++) beats[i] = BeatTick(first + i);
        return beats;
    }
    // Presentation pulse: a short decay after every beat, strongest on the downbeat.
    internal static float Pulse(double tick)
    {
        if (!(tick >= 0)) return 0;
        int beat = BeatAtOrAfter(tick);
        if (BeatTick(beat) > tick) beat--;
        if (beat < 0) return 0;
        return MathF.Exp(-(float)(tick - BeatTick(beat)) / 5f) * (beat % BeatsPerBar == 0 ? 1f : .72f);
    }
}

// Client music only: which song bar sounds in each bar of a stage. Stage 0 starts at
// musicStart; Acts II/III and Final start at their bar-aligned phaseStart. Their first
// bar keeps the previous stage playing, so every peer hears of the change a bar early.
internal static class CrimsonArrangement
{
    internal const int Continue = -1, VictoryStopBar = 26, StageCount = 4;
    private static readonly int[][] Entries =
    {
        new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 },
        new[] { Continue, 26, 27, 28, 29, 30 },
        new[] { Continue, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65 },
        new[] { Continue, 26, 15, 16, 17 },
    };
    // Bodies loop while a stage lasts; every internal jump was chosen from measured seams.
    private static readonly int[][] Bodies =
    {
        new[] { 20, 21, 22, 66, 67, 68, 69, 70, 24, 23 },
        new[] { 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43 },
        new[] { 66, 67, 68, 69, 70, 25, 21, 22 },
        new[] { 48, 49, 50, 51, 52, 53, 54, 55, 67, 68, 69, 70, 18, 19, 20, 21, 22, 40, 41, 42, 43, 44, 45, 46, 47 },
    };
    internal static int TransitionBars(int phase)
        => phase is >= 1 and <= 3 ? phase == 3 ? 5 : 2 : throw new ArgumentOutOfRangeException(nameof(phase));
    internal static int SongBar(int stage, int bar)
    {
        if (stage is < 0 or >= StageCount || bar < 0) throw new ArgumentOutOfRangeException();
        var entry = Entries[stage];
        return bar < entry.Length ? entry[bar] : Bodies[stage][(bar - entry.Length) % Bodies[stage].Length];
    }
    // The Final transition's riser swells out of the stop's ring instead of entering at full level.
    internal static bool Swells(int stage, int bar) => stage == 3 && bar == 2;
}
