using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    // Independent oracle for the grid: the first bar head at or after a tick, found by walking the bars.
    private static int ScarletFirstBarAtOrAfter(int earliest)
    {
        int bar = 0;
        while (CrimsonMeter.BarTick(bar) < earliest) bar++;
        return bar;
    }

    [DomainTest("Scarlet phrases alternate forecast and strike on grid beats from bar head to bar head")]
    private static void ScarletActualRhythm()
    {
        int earliest = CrimsonChoreography.OpeningTicks;
        for (int serial = 0; serial < 300; serial++)
        {
            var phrase = CrimsonChoreography.Create(earliest, serial, serial % 2 == 1);
            int first = ScarletFirstBarAtOrAfter(earliest) * CrimsonMeter.BeatsPerBar;
            AssertEqual(true, phrase.Start >= earliest, "never start a late warning");
            AssertEqual(CrimsonMeter.BeatTick(first), phrase.Start, "phrase opens on a bar head");
            AssertEqual(5, phrase.Hits.Count, "four basic answers and one crossflow per two-bar phrase");
            AssertEqual(CrimsonRhythmKind.Groove, phrase.Kind, "no fills or Final rolls");
            for (int i = 0; i < 4; i++)
            {
                var hit = phrase.Hits[i];
                AssertEqual(CrimsonMeter.BeatTick(first + i), hit.Warning, "forecast on beat i");
                AssertEqual(CrimsonMeter.BeatTick(first + i + 1), hit.Fire, "strike one beat after its forecast");
                AssertEqual((byte)1, hit.Accent, "constant attack accent");
                AssertEqual(true, hit.Fire - hit.Warning >= CrimsonRhythm.MinimumWarningTicks, "fast attacks retain full warning");
                AssertEqual(32, hit.End - hit.Fire, "flight lifetime does not stop at the following forecast beat");
                if (i > 0) AssertEqual(true, hit.Warning > phrase.Hits[i - 1].Warning, "ordered call");
            }
            var closing = phrase.Hits[4];
            AssertEqual(CrimsonMeter.BeatTick(first + 4), closing.Warning, "crossflow forecast on beat four");
            AssertEqual(CrimsonMeter.BeatTick(first + 6), closing.Fire, "crossflow strike on beat six");
            AssertEqual(CrimsonMeter.BeatTick(first + 8), closing.End, "crossflow collapse on beat eight");
            AssertEqual(phrase.End, closing.End, "the phrase ends where its last note ends");
            var next = CrimsonChoreography.Create(phrase.End, serial + 1, false);
            AssertEqual(phrase.End, next.Start, "next phrase resumes on the very bar head the last one ended");
            AssertEqual(true, closing.End + CrimsonRhythm.PoseRecoveryTicks <= next.Hits[0].Fire, "old body reaches its exit before the next strike");
            AssertEqual(true, closing.End + CrimsonRhythm.ResidueTicks > next.Hits[0].Warning, "residue may overlap the next forecast");
            earliest = phrase.End;
        }
    }

    [DomainTest("Scarlet basic pulse warns then answers one beat later on the measured grid")]
    private static void ScarletCallResponse()
    {
        var phrase = CrimsonChoreography.Create(900, 3, false);
        int[] warnings = { 900, 928, 956, 984 }, fires = { 928, 956, 984, 1013 };
        AssertEqual(900, phrase.Start, "bar eight opens the phrase");
        for (int i = 0; i < warnings.Length; i++)
        {
            AssertEqual(warnings[i], phrase.Hits[i].Warning, "forecast on its beat");
            AssertEqual(fires[i], phrase.Hits[i].Fire, "answer exactly one beat later, never unannounced");
            AssertEqual(fires[i] + 32, phrase.Hits[i].End, "flight lives 32 ticks");
        }
        AssertEqual(1013, phrase.Hits[4].Warning, "crossflow forecast on beat four");
        AssertEqual(1069, phrase.Hits[4].Fire, "crossflow strike on beat six");
        AssertEqual(1125, phrase.End, "two-bar phrase");
        AssertEqual(1013, CrimsonChoreography.Create(901, 0, false).Start, "a late tick waits for the next bar head");
        AssertEqual(1125, CrimsonChoreography.Create(1125, 0, false).Start, "a tick on the bar head belongs to that bar");
        AssertEqual(1238, CrimsonChoreography.Create(1126, 0, false).Start, "one tick past the bar head waits for the following bar");
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

    [DomainTest("Scarlet phrase times derive from the absolute grid without cumulative rounding")]
    private static void ScarletLoopBoundaries()
    {
        int earliest = CrimsonChoreography.OpeningTicks;
        for (int n = 0; n < 300; n++)
        {
            var phrase = CrimsonChoreography.Create(earliest, n, false);
            // Eight beats are exactly 225 ticks, so a phrase chain never drifts from musicStart + 900 + 225n.
            AssertEqual(900 + 225 * n, phrase.Start, "no accumulated rounding across phrases");
            AssertEqual(900 + 225 * (n + 1), phrase.End, "exact two-bar length");
            earliest = phrase.End;
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
