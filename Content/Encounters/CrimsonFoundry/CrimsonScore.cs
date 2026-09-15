using System;
using System.IO;
using System.Text.Json;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Authored audio analysis is immutable game data. The server clock, never the
// sound card or a client-reported beat, schedules the fight.
internal sealed class CrimsonScore
{
    public int SampleRate { get; set; }
    public int LoopStartSample { get; set; }
    public int LoopEndSample { get; set; }
    public int IntroTicks { get; set; }
    public int[] BeatTicks { get; set; } = Array.Empty<int>();
    public float[] Energy { get; set; } = Array.Empty<float>();
    internal double LoopStart => LoopStartSample * 60d / SampleRate;
    internal double LoopEnd => LoopEndSample * 60d / SampleRate;
    internal double LoopLength => LoopEnd - LoopStart;
    internal const int WarningTicks = 60;

    internal static CrimsonScore Read(byte[] data)
    {
        if (data.Length > 64000) throw new InvalidDataException("crimson.score_too_large");
        var score = JsonSerializer.Deserialize<CrimsonScore>(data) ?? throw new InvalidDataException("crimson.score_missing");
        if (score.SampleRate != 48000 || score.LoopStartSample < 48000 || score.LoopEndSample <= score.LoopStartSample
            || score.LoopEndSample > 48000 * 300 || score.IntroTicks < 120 || score.IntroTicks > 1800
            || score.BeatTicks.Length is < 4 or > 2048 || score.Energy.Length != score.BeatTicks.Length)
            throw new InvalidDataException("crimson.score_invalid");
        for (int i = 0; i < score.BeatTicks.Length; i++)
            if (score.BeatTicks[i] < 0 || score.BeatTicks[i] >= score.LoopEnd
                || i > 0 && score.BeatTicks[i] <= score.BeatTicks[i - 1]
                || !float.IsFinite(score.Energy[i]) || score.Energy[i] is < 0 or > 1)
                throw new InvalidDataException("crimson.score_event_invalid");
        return score;
    }

    internal double Position(double age) => age < LoopEnd ? Math.Max(0, age) : LoopStart + (age - LoopEnd) % LoopLength;
    internal int SampleAt(double age) => Math.Clamp((int)Math.Round(Position(age) * SampleRate / 60), 0, LoopEndSample - 1);
    internal float Intensity(double age)
    {
        double p = Position(age);
        int i = Array.BinarySearch(BeatTicks, (int)p);
        if (i < 0) i = Math.Max(0, ~i - 1);
        return Energy[Math.Min(i, Energy.Length - 1)];
    }
    internal float Pulse(double age)
    {
        double p = Position(age);
        int i = Array.BinarySearch(BeatTicks, (int)p);
        if (i < 0) i = ~i - 1;
        return i < 0 ? 0 : MathF.Exp(-(float)(p - BeatTicks[i]) / 5f) * Energy[i];
    }

    // Enumerate only events whose warnings begin THIS tick. All later geometry
    // is locked there; moving players cannot drag it, including across a loop.
    internal void Events(int age, Action<int, int, float, bool> schedule)
    {
        double future = age + WarningTicks;
        int cycle = future < LoopEnd ? 0 : 1 + (int)((future - LoopEnd) / LoopLength);
        for (int c = Math.Max(0, cycle - 1); c <= cycle + 1; c++)
        {
            double shift = c == 0 ? 0 : LoopEnd - LoopStart + (c - 1) * LoopLength;
            for (int i = 0; i < BeatTicks.Length - 1; i++)
            {
                if (c > 0 && BeatTicks[i] < LoopStart) continue;
                int fire = (int)Math.Round(BeatTicks[i] + shift);
                if (fire < IntroTicks + WarningTicks) continue;
                // Quiet phrases breathe; strong passages add the offbeat.
                int stride = Energy[i] < .43f ? 4 : Energy[i] < .76f ? 2 : 1;
                if (i % stride == 0 && fire - WarningTicks == age)
                    schedule(c * 4096 + i * 2, fire, Energy[i], false);
                if (Energy[i] > .83f && i % 2 == 1)
                {
                    int off = (int)Math.Round((BeatTicks[i] + BeatTicks[i + 1]) * .5 + shift);
                    if (off - WarningTicks == age) schedule(c * 4096 + i * 2 + 1, off, Energy[i], true);
                }
            }
        }
    }
}
