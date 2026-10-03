using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet ordinary cells vary between phrases instead of one repeated metronome pattern")]
    private static void ScarletGrooveVariety()
    {
        foreach (int earliest in new[] { 0, 600, 900, 901, 5000, 30000 })
        for (int phase = 0; phase < 4; phase++)
        {
            var patterns = new HashSet<string>();
            for (int serial = 1; serial <= 12; serial++)
            {
                var phrase = CrimsonChoreography.Create(earliest, serial, phase, false);
                AssertEqual(CrimsonRhythmKind.Groove, phrase.Kind, "same kind in every phase");
                var offsets = new List<int>();
                foreach (var hit in phrase.Hits)
                {
                    offsets.Add(hit.Fire - phrase.Start);
                    if (!CrimsonChoreography.IsCrossflow(hit.Pulse)) AssertEqual(true, hit.Fire - hit.Warning is 28 or 29, "one measured beat of warning (28.125 ticks rounded)");
                }
                patterns.Add(string.Join(",", offsets));
                // Not a pulse on the beat: the ordinary notes are unevenly spaced or strike off the beat.
                var eighths = new List<int>();
                foreach (var hit in phrase.Hits)
                    if (!CrimsonChoreography.IsCrossflow(hit.Pulse)) eighths.Add((int)Math.Round((hit.Fire - phrase.Start) / 14.0625));
                var gaps = new HashSet<int>();
                for (int i = 1; i < eighths.Count; i++) gaps.Add(eighths[i] - eighths[i - 1]);
                bool signature = CrimsonSignatureMoves.IsSignaturePhrase(phase, serial);
                if (signature) AssertEqual("7,10,13,16", string.Join(",", eighths), "signature steps a dotted quarter apart from beat 3.5");
                else AssertEqual(true, gaps.Count == 2 || eighths.Exists(e => e % 2 == 1), $"syncopated, not a beat pulse ({string.Join(",", eighths)})");
                AssertEqual(true, eighths.TrueForAll(e => e % 8 != 0 || signature && e == 16), "no ordinary strike on a bar head");
            }
            // Acts: two ordinary cells and the signature layout; Final: three ordinary cells.
            AssertEqual(3, patterns.Count, "three distinct phrase layouts per cycle of three");
        }
    }
    [DomainTest("Scarlet key moments land on bar-head pulse peaks and ordinary notes stay on the eighth-note grid")]
    private static void ScarletChorusPulseAlignment()
    {
        for (int phase = 0; phase < 4; phase++)
        {
            int earliest = CrimsonChoreography.OpeningTicks;
            for (int serial = 1; serial <= 120; serial++)
            {
                var phrase = CrimsonChoreography.Create(earliest, serial, phase, serial % 5 == 1);
                foreach (var hit in phrase.Hits)
                {
                    bool key = CrimsonChoreography.IsCrossflow(hit.Pulse)
                        || CrimsonSignatureMoves.IsSignaturePhrase(phase, serial) && hit.Pulse == CrimsonChoreography.SignatureClimax;
                    foreach (int tick in key ? new[] { hit.Fire } : new[] { hit.Warning, hit.Fire })
                    {
                        int eighth = (int)Math.Round(tick / 14.0625);
                        AssertEqual(tick, CrimsonMeter.EighthTick(eighth), "every call and strike is on the eighth-note grid");
                    }
                    if (!key) continue;
                    // Gameplay rounds to a tick, whereas the backdrop pulse draws at fractional age. The first
                    // half-frame after a release is on the bar's full pulse peak.
                    int beat = CrimsonMeter.BeatAtOrAfter(hit.Fire);
                    AssertEqual(hit.Fire, CrimsonMeter.BeatTick(beat), "a key moment is a grid beat");
                    AssertEqual(0, beat % CrimsonMeter.BeatsPerBar, "on a bar head");
                    AssertEqual(true, CrimsonMeter.Pulse(hit.Fire + .50001d) >= MathF.Exp(-1.0001f / 5f), "the downbeat's full pulse peak");
                }
                earliest = phrase.End;
            }
        }
        for (int e = 0; e <= 2 * CrimsonMeter.MaximumBeat; e += 7)
            AssertEqual(ScarletEighth(e), CrimsonMeter.EighthTick(e), "eighth ticks round half up");
        for (int k = 0; k <= CrimsonMeter.MaximumBeat; k += 3)
            AssertEqual(CrimsonMeter.BeatTick(k), CrimsonMeter.EighthTick(2 * k), "two eighths are one beat");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.EighthTick(-1), "no negative eighth");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.EighthTick(2 * CrimsonMeter.MaximumBeat + 1), "bounded eighth");
    }
    [DomainTest("Scarlet early phrase reservation keeps the closer's release on the next phrase's downbeat")]
    private static void ScarletBarContinuity()
    {
        for (int phase = 0; phase < 4; phase++)
        {
            int earliest = CrimsonChoreography.OpeningTicks;
            for (int serial = 1; serial <= 140; serial++)
            {
                var current = CrimsonChoreography.Create(earliest, serial, phase, serial == 1);
                // The runtime books the next phrase on End and issues it before its first forecast.
                var next = CrimsonChoreography.Create(current.End, serial + 1, phase, false);
                AssertEqual(current.End, next.Start, "booking does not insert a cooldown or skip a rounded beat");
                AssertEqual(next.Start, current.Hits[^1].Fire, "the closer (or the signature's final hit) and the next downbeat share one tick");
                AssertEqual(true, next.FirstWarning >= current.Hits[^1].Fire + 20, "the next phrase's first forecast follows that release");
                earliest = next.Start;
            }
        }
    }
}
