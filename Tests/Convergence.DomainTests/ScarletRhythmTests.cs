using System;
using System.Collections.Generic;
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
    // Eighth note e after musicStart, 14.0625 ticks each, rounded half up (28.125 ticks per beat).
    private static int ScarletEighth(int eighth) => (int)Math.Floor(eighth * 14.0625 + .5);

    // The stated phrase layout (protocol80), in eighth notes from the phrase's bar head: (warning, strike, end or -1 for
    // a live window owned by the technique, accent, pulse). Ordinary notes warn one beat (two eighths) before they strike.
    private static List<(int Warning, int Fire, int End, byte Accent, byte Pulse)> ScarletOracleLayout(int phase, int serial, bool pickup)
    {
        bool signature = phase < 3 && serial % 3 == 0;
        var notes = new List<(int, int, int, byte, byte)>();
        if (pickup && !signature) notes.Add((-4, 0, 4, 2, 9));
        if (signature)
        {
            int[] steps = { 7, 10, 13, 16 };
            for (int step = 0; step < 4; step++) notes.Add((steps[step] - 2, steps[step], -1, (byte)(step == 3 ? 2 : 1), (byte)step));
            return notes;
        }
        var (fires, pulses) = (serial % 3) switch
        {
            1 => (new[] { 6, 9, 12 }, new byte[] { 0, 1, 2 }),
            2 => (new[] { 7, 10, 12 }, new byte[] { 1, 2, 3 }),
            _ => (new[] { 6, 10, 12 }, new byte[] { 0, 2, 3 })
        };
        for (int i = 0; i < 3; i++) notes.Add((fires[i] - 2, fires[i], -1, 1, pulses[i]));
        notes.Add((12, 16, 20, 2, 4));
        return notes;
    }

    [DomainTest("Scarlet phrases keep the key moments on bar heads and three syncopated notes between them")]
    private static void ScarletActualRhythm()
    {
        for (int phase = 0; phase < 4; phase++) foreach (bool pickup in new[] { false, true })
        {
            int earliest = CrimsonChoreography.OpeningTicks;
            for (int serial = 1; serial <= 60; serial++)
            {
                var phrase = CrimsonChoreography.Create(earliest, serial, phase, pickup);
                bool signature = CrimsonSignatureMoves.IsSignaturePhrase(phase, serial);
                AssertEqual(phase < 3 && serial % 3 == 0, signature, "every third Act I-III phrase is a signature phrase");
                int bar = ScarletFirstBarAtOrAfter(earliest), first = bar * 8;
                AssertEqual(CrimsonMeter.BarTick(bar), phrase.Start, "phrase opens on the first bar head at or after the request");
                AssertEqual(CrimsonMeter.BarTick(bar + 2), phrase.End, "two bars");
                AssertEqual(CrimsonRhythmKind.Groove, phrase.Kind, "no fills or rolls");
                var layout = ScarletOracleLayout(phase, serial, pickup);
                AssertEqual(layout.Count, phrase.Hits.Count, "stated note count");
                int lastOrdinaryFire = int.MinValue, crossflowEnd = phrase.Start;
                for (int i = 0; i < layout.Count; i++)
                {
                    var (warning, fire, end, accent, pulse) = layout[i];
                    var hit = phrase.Hits[i];
                    AssertEqual(ScarletEighth(first + warning), hit.Warning, $"warning of note {i} (phase {phase} serial {serial})");
                    AssertEqual(ScarletEighth(first + fire), hit.Fire, $"strike of note {i}");
                    if (end >= 0) AssertEqual(ScarletEighth(first + end), hit.End, "a crossflow collapses two beats after its release");
                    AssertEqual(accent, hit.Accent, "accent");
                    AssertEqual(pulse, hit.Pulse, "note identity");
                    bool crossflow = CrimsonChoreography.IsCrossflow(pulse);
                    bool keyMoment = crossflow || signature && pulse == CrimsonChoreography.SignatureClimax;
                    if (keyMoment)
                    {
                        // Releases (and a signature move's final hit) land exactly on a bar head of the grid.
                        AssertEqual(true, fire % 8 == 0, "key moment on a bar head");
                        AssertEqual(CrimsonMeter.BarTick((first + fire) / 8), hit.Fire, "key moment is the grid's bar head tick");
                    }
                    else
                    {
                        AssertEqual(true, fire % 8 != 0, "an ordinary note never strikes on a bar head");
                        AssertEqual(true, hit.Fire - hit.Warning is 28 or 29, "one measured beat of warning");
                        AssertEqual(true, hit.Warning >= lastOrdinaryFire, "at most one ordinary forecast at a time");
                        AssertEqual(true, hit.Warning >= crossflowEnd, "no ordinary forecast while a pickup crossflow is live");
                        lastOrdinaryFire = hit.Fire;
                    }
                    if (crossflow)
                    {
                        AssertEqual(true, hit.Fire - hit.Warning is 56 or 57, "a crossflow charges for two beats");
                        AssertEqual(true, hit.End - hit.Fire is 56 or 57, "and flows for two beats");
                        if (pulse == CrimsonChoreography.Pickup) { AssertEqual(phrase.Start, hit.Fire, "a pickup releases on this phrase's downbeat"); crossflowEnd = hit.End; }
                        else AssertEqual(phrase.End, hit.Fire, "the closer releases on the next phrase's downbeat");
                    }
                }
                if (signature) AssertEqual(phrase.End, phrase.Hits[^1].Fire, "the signature move's final hit lands on the next downbeat");
                // An ordinary note strikes on beat 6 as the closing crossflow's seals bloom.
                if (!signature) AssertEqual(phrase.Hits[^1].Warning, phrase.Hits[^2].Fire, "third note and seals on beat 6");
                earliest = phrase.End;
            }
        }
    }

    [DomainTest("Scarlet Act I opens with a pickup released on the unlock downbeat and syncopated notes on known ticks")]
    private static void ScarletCallResponse()
    {
        var open = CrimsonChoreography.Create(900, 1, 0, true);
        AssertEqual(900, open.Start, "bar eight opens the phrase");
        AssertEqual(1125, open.End, "two-bar phrase");
        AssertEqual(844, open.FirstWarning, "the pickup's seals bloom on the intro's last two beats");
        int[][] expected =
        {
            new[] { 844, 900, 956 },        // pickup: charge, release on the unlock downbeat, collapse
            new[] { 956, 984 }, new[] { 998, 1027 }, new[] { 1041, 1069 }, // beats 3, 4.5 and 6
            new[] { 1069, 1125, 1181 }      // closer: seals on beat 6, release on bar 10's downbeat
        };
        for (int i = 0; i < expected.Length; i++)
        {
            AssertEqual(expected[i][0], open.Hits[i].Warning, $"warning {i}");
            AssertEqual(expected[i][1], open.Hits[i].Fire, $"strike {i}");
            if (expected[i].Length == 3) AssertEqual(expected[i][2], open.Hits[i].End, $"end {i}");
            else AssertEqual(expected[i][1] + 32, open.Hits[i].End, "an ordinary flight lives 32 ticks");
        }
        var second = CrimsonChoreography.Create(open.End, 2, 0, false);
        AssertEqual(1125, second.Start, "the next phrase starts where the closer releases");
        AssertEqual("1223,1266,1294", string.Join(",", Array.ConvertAll(new[] { 0, 1, 2 }, i => second.Hits[i].Fire)), "beats 3.5, 5 and 6");
        var signature = CrimsonChoreography.Create(second.End, 3, 0, true);
        AssertEqual(1350, signature.Start, "signature phrase on bar twelve");
        AssertEqual(4, signature.Hits.Count, "a signature phrase never takes a pickup");
        AssertEqual("1448,1491,1533,1575", string.Join(",", Array.ConvertAll(new[] { 0, 1, 2, 3 }, i => signature.Hits[i].Fire)), "dotted-quarter steps, the last on bar fourteen");
        AssertEqual(1013, CrimsonChoreography.Create(901, 1, 0, false).Start, "a late tick waits for the next bar head");
        AssertEqual(1125, CrimsonChoreography.Create(1125, 1, 0, false).Start, "a tick on the bar head belongs to that bar");
        AssertEqual(1238, CrimsonChoreography.Create(1126, 1, 0, false).Start, "one tick past the bar head waits for the following bar");
        AssertEqual(113, CrimsonChoreography.Create(0, 1, 0, true).Start, "a pickup needs the two beats before its bar head");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonChoreography.Create(0, 1, 4, false), "unknown phase");
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
        for (int phase = 0; phase < 4; phase++)
        {
            int earliest = CrimsonChoreography.OpeningTicks;
            for (int n = 0; n < 300; n++)
            {
                var phrase = CrimsonChoreography.Create(earliest, n + 1, phase, false);
                // Eight beats are exactly 225 ticks, so a phrase chain never drifts from musicStart + 900 + 225n.
                AssertEqual(900 + 225 * n, phrase.Start, "no accumulated rounding across phrases");
                AssertEqual(900 + 225 * (n + 1), phrase.End, "exact two-bar length");
                earliest = phrase.End;
            }
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
