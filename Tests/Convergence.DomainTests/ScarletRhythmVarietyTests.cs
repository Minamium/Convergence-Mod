using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet straight offbeat delayed and flam grooves echo distinct call rhythms")]
    private static void ScarletGrooveVariety()
    {
        var score = new CrimsonScore { SampleRate = 48000, LoopStartSample = 480000, LoopEndSample = 1440000,
            IntroTicks = 120, BeatTicks = new[] { 600, 628, 656, 684, 712, 740, 768, 796 },
            Energy = new[] { .9f, .9f, .9f, .9f, .9f, .9f, .9f, .9f } };
        var patterns = new HashSet<string>();
        foreach (int serial in new[] { 0, 1, 2, 4 })
        {
            var phrase = CrimsonRhythm.Create(score, 600, serial, false);
            AssertEqual(CrimsonRhythmKind.Groove, phrase.Kind, "variety does not erase Fill/Roll identity");
            patterns.Add(string.Join(",", phrase.Hits));
            foreach (var hit in phrase.Hits) AssertEqual(42, hit.Fire - hit.Warning, "same figure answers one and a half beats later");
        }
        AssertEqual(4, patterns.Count, "not a single repeated metronome pattern");
    }
    [DomainTest("Scarlet early phrase reservation preserves rounded loop boundaries without dropping beats")]
    private static void ScarletBarContinuity()
    {
        var score = ScarletRecordedScore();
        int earliest = score.IntroTicks;
        for (int serial = 0; serial < 800; serial++)
        {
            var first = CrimsonRhythm.Create(score, earliest, serial, true);
            var next = CrimsonRhythm.Create(score, first.End, serial + 1, true);
            AssertEqual(first.End, next.Start, "reservation lead does not insert a cooldown or skip a rounded beat");
            earliest = next.End;
        }
    }
}
