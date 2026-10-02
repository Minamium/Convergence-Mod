using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet basic pulse ignores serial and Final instead of adding irregular fills")]
    private static void ScarletGrooveVariety()
    {
        // Every earliest tick, serial and Final flag produce the same phrase: the pattern is the grid.
        foreach (int earliest in new[] { 0, 600, 900, 901, 5000, 30000 })
        {
            var patterns = new HashSet<string>();
            for (int serial = 0; serial < 12; serial++)
            foreach (bool final in new[] { false, true })
            {
                var phrase = CrimsonChoreography.Create(earliest, serial, final);
                AssertEqual(CrimsonRhythmKind.Groove, phrase.Kind, "same basic pulse in every phase");
                patterns.Add(string.Join(",", phrase.Hits));
                for (int i = 0; i < CrimsonChoreography.BasicNotes; i++)
                {
                    int warning = phrase.Hits[i].Fire - phrase.Hits[i].Warning;
                    AssertEqual(true, warning is 28 or 29, "one measured beat of warning (28.125 ticks rounded)");
                }
            }
            AssertEqual(1, patterns.Count, "one deliberately repeated metronome pattern");
        }
    }
    [DomainTest("Scarlet warning and strike ticks agree with the chorus visual pulse within one frame")]
    private static void ScarletChorusPulseAlignment()
    {
        int earliest = CrimsonChoreography.OpeningTicks;
        for (int serial = 0; serial < 300; serial++)
        {
            var phrase = CrimsonChoreography.Create(earliest, serial, true);
            foreach (var hit in phrase.Hits)
            foreach (int tick in new[] { hit.Warning, hit.Fire })
            {
                // Gameplay rounds to a tick, whereas the chorus draws at fractional age.
                // The first half-frame after that tick must be on the same pulse peak.
                double renderAge = tick + .50001d;
                int beat = CrimsonMeter.BeatAtOrAfter(tick);
                AssertEqual(tick, CrimsonMeter.BeatTick(beat), "every call and strike is a grid beat");
                float peak = beat % CrimsonMeter.BeatsPerBar == 0 ? 1f : .72f;
                AssertEqual(true, CrimsonMeter.Pulse(renderAge) >= peak * MathF.Exp(-1.0001f / 5f), "same pulse peak, including rounded half-tick beats");
            }
            earliest = phrase.End;
        }
    }
    [DomainTest("Scarlet early phrase reservation preserves rounded beat boundaries without dropping beats")]
    private static void ScarletBarContinuity()
    {
        int earliest = CrimsonChoreography.OpeningTicks;
        for (int serial = 0; serial < 140; serial++)
        {
            var first = CrimsonChoreography.Create(earliest, serial, true);
            // The runtime reserves the next phrase LookAheadTicks early, so it asks from exactly End.
            var next = CrimsonChoreography.Create(first.End, serial + 1, true);
            AssertEqual(first.End, next.Start, "reservation lead does not insert a cooldown or skip a rounded beat");
            AssertEqual(first.Hits[4].End, next.Start, "the crossflow collapse and the next downbeat share one tick");
            earliest = next.End;
        }
    }
}
