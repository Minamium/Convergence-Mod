using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static CrimsonScore ScarletRecordedScore() => CrimsonScore.Read(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "CrimsonScore.json")));

    [DomainTest("Scarlet actual score keeps eighth calls and sixteenth fills with readable lead")]
    private static void ScarletActualRhythm()
    {
        var score = ScarletRecordedScore();
        int earliest = score.IntroTicks, fills = 0, fastGaps = 0;
        for (int serial = 0; serial < 1500; serial++)
        {
            var phrase = CrimsonRhythm.Create(score, earliest, serial, serial % 2 == 1);
            AssertEqual(true, phrase.Start >= earliest, "never start a late warning");
            AssertEqual(true, phrase.Hits.Count is >= 3 and <= 5, "bounded phrase");
            if (phrase.Kind == CrimsonRhythmKind.Fill) fills++;
            for (int i = 0; i < phrase.Hits.Count; i++)
            {
                var hit = phrase.Hits[i];
                AssertEqual(true, hit.Fire - hit.Warning >= CrimsonRhythm.MinimumWarningTicks, "fast attacks retain full warning");
                AssertEqual(true, hit.End - hit.Fire is >= 2 and <= 5, "short native collision pulse");
                if (i > 0)
                {
                    AssertEqual(true, hit.Warning > phrase.Hits[i - 1].Warning, "ordered call");
                    AssertEqual(true, hit.Fire > phrase.Hits[i - 1].End, "no simultaneous incompatible live fields");
                    if (hit.Fire - phrase.Hits[i - 1].Fire <= 8) fastGaps++;
                }
                AssertEqual(true, hit.End < phrase.End, "phrase owns its tail");
            }
            earliest = phrase.End + CrimsonRhythm.LookAheadTicks;
        }
        AssertEqual(375, fills, "planned periodic fills, not random cooldown gating");
        AssertEqual(true, fastGaps > 300, "sub-beat bursts actually occur");
    }

    [DomainTest("Scarlet fill echoes its onset pattern rather than changing to uniform shots")]
    private static void ScarletCallResponse()
    {
        var score = new CrimsonScore { SampleRate = 48000, LoopStartSample = 480000, LoopEndSample = 1440000,
            IntroTicks = 120, BeatTicks = new[] { 600, 628, 656, 684, 712, 740, 768, 796 },
            Energy = new[] { .9f, .9f, .9f, .9f, .9f, .9f, .9f, .9f } };
        var phrase = CrimsonRhythm.Create(score, 600, 3, false);
        int[] offsets = { 0, 7, 21, 35, 42 };
        for (int i = 0; i < offsets.Length; i++)
        {
            AssertEqual(600 + offsets[i], phrase.Hits[i].Warning, "fill forecast rhythm");
            AssertEqual(656 + offsets[i], phrase.Hits[i].Fire, "same rhythm returned after two beats");
        }
        AssertEqual(714 - 2, phrase.End, "four-beat phrase");
    }

    [DomainTest("Scarlet phrase times derive from absolute sample loop without cumulative rounding")]
    private static void ScarletLoopBoundaries()
    {
        var score = ScarletRecordedScore();
        for (int cycle = 1; cycle <= 500; cycle += 7)
        {
            double origin = score.LoopEnd + (cycle - 1) * score.LoopLength;
            var times = CrimsonRhythm.NextBeats(score, origin, 5);
            AssertEqual(true, times[0] >= origin, "no replay before loop edge");
            for (int i = 1; i < times.Length; i++) AssertEqual(true, times[i] > times[i - 1], "loop ordering");
            var first = CrimsonRhythm.NextBeats(score, score.LoopEnd, 1)[0];
            AssertEqual(true, Math.Abs(times[0] - first - (cycle - 1) * score.LoopLength) < .00001, "absolute cycle offset");
        }
    }

    [DomainTest("Scarlet successive burst corridors retain shared space for a complete player body")]
    private static void ScarletBurstCorridors()
    {
        var field = Convergence.Common.Raids.Arena.RaidFieldGeometry.FromGround(8000, 6000);
        for (int cue = 0; cue < 100; cue++) for (int phase = 0; phase < 4; phase++)
        {
            float low = float.NegativeInfinity, high = float.PositiveInfinity;
            for (int step = 0; step < CrimsonRhythm.MaximumHits; step++)
            {
                var b = CrimsonBarrageGeometry.Build(field, cue, phase, phase == 3, (step - 2) * (phase == 3 ? 20 : 28));
                low = Math.Max(low, b.SafeOffset - b.SafeWidth * .5f);
                high = Math.Min(high, b.SafeOffset + b.SafeWidth * .5f);
                AssertEqual(true, b.Lanes.Count <= CrimsonRhythm.MaximumLanes, "preflight capacity bound");
            }
            AssertEqual(true, high - low >= 80, "overlapping safe strip across the entire fast phrase");
        }
    }
}
