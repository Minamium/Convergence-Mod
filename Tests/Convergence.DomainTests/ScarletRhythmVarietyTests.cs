using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet basic pulse ignores serial energy and Final instead of adding irregular fills")]
    private static void ScarletGrooveVariety()
    {
        var score = new CrimsonScore { SampleRate = 48000, LoopStartSample = 480000, LoopEndSample = 1440000,
            IntroTicks = 120, BeatTicks = new[] { 600, 628, 656, 684, 712, 740, 768, 796 },
            Energy = new[] { .9f, .9f, .9f, .9f, .9f, .9f, .9f, .9f } };
        var patterns = new HashSet<string>();
        for (int serial = 0; serial < 12; serial++)
        foreach (bool final in new[] { false, true })
        foreach (float energy in new[] { 0f, 1f })
        {
            Array.Fill(score.Energy, energy);
            var phrase = CrimsonRhythm.Create(score, 600, serial, final);
            AssertEqual(CrimsonRhythmKind.Groove, phrase.Kind, "same basic pulse in every phase");
            patterns.Add(string.Join(",", phrase.Hits));
            foreach (var hit in phrase.Hits) AssertEqual(28, hit.Fire - hit.Warning, "one measured beat of warning");
        }
        AssertEqual(1, patterns.Count, "one deliberately repeated metronome pattern");
    }
    [DomainTest("Scarlet warning and strike ticks agree with the chorus visual pulse within one frame")]
    private static void ScarletChorusPulseAlignment()
    {
        var score = ScarletRecordedScore();
        int earliest = score.IntroTicks;
        for (int serial = 0; serial < 800; serial++)
        {
            var phrase = CrimsonRhythm.Create(score, earliest, serial, true);
            foreach (var hit in phrase.Hits)
            foreach (int tick in new[] { hit.Warning, hit.Fire })
            {
                // Gameplay rounds to a tick, whereas the chorus draws at fractional age.
                // The first half-frame after that tick must be on the same pulse peak.
                double renderAge = tick + .50001d;
                float energy = score.Intensity(renderAge);
                AssertEqual(true, score.Pulse(renderAge) >= energy * MathF.Exp(-1.0001f / 5f), "same score pulse, including fractional loop seams");
            }
            earliest = phrase.End;
        }
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
