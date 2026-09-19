using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static CrimsonScore ScarletRecordedScore() => CrimsonScore.Read(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "CrimsonScore.json")));

    [DomainTest("Scarlet actual score alternates forecast and strike on chorus beats across loops")]
    private static void ScarletActualRhythm()
    {
        var score = ScarletRecordedScore();
        int earliest = score.IntroTicks;
        for (int serial = 0; serial < 1500; serial++)
        {
            var phrase = CrimsonRhythm.Create(score, earliest, serial, serial % 2 == 1);
            AssertEqual(true, phrase.Start >= earliest, "never start a late warning");
            var beats = CrimsonRhythm.NextBeats(score, Math.Max(0, earliest - .499999d), 5);
            AssertEqual(2, phrase.Hits.Count, "two answers per four-beat phrase");
            AssertEqual(CrimsonRhythmKind.Groove, phrase.Kind, "no fills or Final rolls");
            for (int i = 0; i < phrase.Hits.Count; i++)
            {
                var hit = phrase.Hits[i];
                AssertEqual((int)Math.Round(beats[i * 2 + 1]), hit.Warning, "forecast moved to second/fourth beat");
                AssertEqual((int)Math.Round(beats[i * 2 + 2]), hit.Fire, "strike moved to third/fifth beat after priming");
                AssertEqual((byte)1, hit.Accent, "constant attack accent");
                AssertEqual(true, hit.Fire - hit.Warning >= CrimsonRhythm.MinimumWarningTicks, "fast attacks retain full warning");
                AssertEqual(32, hit.End - hit.Fire, "flight lifetime does not stop at the following forecast beat");
                if (i > 0)
                {
                    AssertEqual(true, hit.Warning > phrase.Hits[i - 1].Warning, "ordered call");
                    AssertEqual(true, hit.Fire > phrase.Hits[i - 1].End, "no simultaneous incompatible live fields");
                }
            }
            var next = CrimsonRhythm.Create(score, phrase.End, serial + 1, false);
            var last = phrase.Hits[^1];
            AssertEqual(true, last.End + CrimsonRhythm.PoseRecoveryTicks <= next.Hits[0].Fire, "old body reaches its exit before the next strike");
            AssertEqual(true, last.End + CrimsonRhythm.ResidueTicks > next.Hits[0].Warning, "residue may overlap the next forecast");
            AssertEqual((int)Math.Round(beats[4]), phrase.End, "next phrase resumes on fifth beat");
            earliest = phrase.End;
        }
    }

    [DomainTest("Scarlet basic pulse warns then answers one beat later twice per phrase")]
    private static void ScarletCallResponse()
    {
        var score = new CrimsonScore { SampleRate = 48000, LoopStartSample = 480000, LoopEndSample = 1440000,
            IntroTicks = 120, BeatTicks = new[] { 600, 628, 656, 684, 712, 740, 768, 796 },
            Energy = new[] { .9f, .9f, .9f, .9f, .9f, .9f, .9f, .9f } };
        var phrase = CrimsonRhythm.Create(score, 600, 3, false);
        int[] offsets = { 0, 56 };
        for (int i = 0; i < offsets.Length; i++)
        {
            AssertEqual(628 + offsets[i], phrase.Hits[i].Warning, "forecast shifted by one beat");
            AssertEqual(656 + offsets[i], phrase.Hits[i].Fire, "answer exactly one beat later, never unannounced");
        }
        AssertEqual(712, phrase.End, "four-beat phrase");
    }

    [DomainTest("Scarlet shortest one-beat warning survives the codec and rejects a shorter forecast")]
    private static void ScarletOneBeatWarningCodec()
    {
        var p = TechniqueExample(CrimsonTechnique.CrownRain) with { Born = 580 };
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) p.Write(writer);
        using var reader = new BinaryReader(new MemoryStream(stream.ToArray()));
        AssertEqual(p, CrimsonGesturePlan.Read(reader), "twenty-tick warning accepted by the receiving peer");
        bool rejected = false;
        try { (p with { Born = 581 }).Validate(); } catch (InvalidDataException) { rejected = true; }
        AssertEqual(true, rejected, "sub-beat warning below the current bound is rejected");
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
            for (int step = 0; step < 5; step++) // Retained legacy beam descriptors, not the new physical phrase.
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
