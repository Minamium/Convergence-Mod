using System;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet meter keeps 128 BPM without cumulative rounding and agrees with the measured sample grid")]
    private static void ScarletMeterGrid()
    {
        AssertEqual(48000, CrimsonMeter.SampleRate, "decoded song sample rate");
        AssertEqual(800, CrimsonMeter.SamplesPerTick, "samples per 60 Hz tick");
        AssertEqual(22500, CrimsonMeter.BeatSamples, "one beat at 128 BPM");
        AssertEqual(90000, CrimsonMeter.BarSamples, "one bar");
        AssertEqual(CrimsonMeter.BeatSamples * 8, CrimsonMeter.SamplesPerTick * 225, "eight beats are exactly 225 ticks of samples");
        AssertEqual(8, CrimsonMeter.OpeningBars, "eight opening bars");
        AssertEqual(4, CrimsonMeter.SummonBar, "summon on bar four");
        AssertEqual(0, CrimsonMeter.BeatTick(0), "grid starts at musicStart");
        AssertEqual(28, CrimsonMeter.BeatTick(1), "28.125 rounded");
        AssertEqual(56, CrimsonMeter.BeatTick(2), "56.25 rounded");
        AssertEqual(84, CrimsonMeter.BeatTick(3), "84.375 rounded");
        AssertEqual(113, CrimsonMeter.BeatTick(4), "112.5 rounds half up");
        AssertEqual(225, CrimsonMeter.BeatTick(8), "two bars are 225 ticks");
        AssertEqual(900, CrimsonMeter.BarTick(8), "eight bars");
        AssertEqual(72000, CrimsonMeter.BeatTick(CrimsonMeter.MaximumBeat), "the grid covers twenty minutes");
        int previous = -1;
        for (int k = 0; k <= CrimsonMeter.MaximumBeat; k++)
        {
            int tick = CrimsonMeter.BeatTick(k);
            // Exact integer form of |tick - 28.125k| <= .5: nothing accumulates, however long the fight.
            AssertEqual(true, Math.Abs(8 * tick - 225 * k) <= 4, "beat tick is the rounded 28.125k");
            if (k > 0) AssertEqual(true, tick - previous is 28 or 29, "beats are 28 or 29 ticks apart");
            previous = tick;
            if (k % 8 == 0) AssertEqual(225 * (k / 8), tick, "every second bar head is a multiple of 225");
            if (k % 4 == 0)
            {
                AssertEqual(tick, CrimsonMeter.BarTick(k / 4), "bar head is the beat four times over");
                if ((k / 4) % 2 == 0) AssertEqual(0, CrimsonMeter.BarTick(k / 4) % 225, "even bars sit exactly on 225 multiples");
            }
        }
        for (int bar = 0; bar < 500; bar++)
        {
            AssertEqual(true, CrimsonMeter.BarTick(bar + 1) - CrimsonMeter.BarTick(bar) is 112 or 113, "bars are 112 or 113 ticks");
            AssertEqual(225, CrimsonMeter.BarTick(bar + 2) - CrimsonMeter.BarTick(bar), "two bars are always exactly 225 ticks");
            AssertEqual(true, CrimsonMeter.BarTick(bar + 5) - CrimsonMeter.BarTick(bar) is 562 or 563, "five bars are 562 or 563 ticks");
        }
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.BeatTick(-1), "negative beat");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.BeatTick(CrimsonMeter.MaximumBeat + 1), "beyond the grid");
    }

    [DomainTest("Scarlet meter beat, bar and bar-of-tick lookups round trip on every tick")]
    private static void ScarletMeterLookups()
    {
        for (int tick = 0; tick <= 72000; tick++)
        {
            int beat = CrimsonMeter.BeatAtOrAfter(tick);
            AssertEqual(true, CrimsonMeter.BeatTick(beat) >= tick, "beat is not before the tick");
            AssertEqual(true, beat == 0 || CrimsonMeter.BeatTick(beat - 1) < tick, "beat is the first one not before the tick");
            int bar = CrimsonMeter.BarAtOrAfter(tick);
            AssertEqual(true, CrimsonMeter.BarTick(bar) >= tick, "bar head is not before the tick");
            AssertEqual(true, bar == 0 || CrimsonMeter.BarTick(bar - 1) < tick, "bar is the first head not before the tick");
            int within = CrimsonMeter.BarAt(tick);
            AssertEqual(true, CrimsonMeter.BarTick(within) <= tick && (within >= CrimsonMeter.MaximumBeat / 4 || tick < CrimsonMeter.BarTick(within + 1)), "tick lies inside its bar");
        }
        for (int k = 0; k <= CrimsonMeter.MaximumBeat; k++)
            AssertEqual(k, CrimsonMeter.BeatAtOrAfter(CrimsonMeter.BeatTick(k)), "beat head maps back to its beat");
        for (int bar = 0; bar <= CrimsonMeter.MaximumBeat / 4; bar++)
        {
            int head = CrimsonMeter.BarTick(bar);
            AssertEqual(bar, CrimsonMeter.BarAtOrAfter(head), "bar head maps back to its bar");
            AssertEqual(bar, CrimsonMeter.BarAt(head), "bar head belongs to its own bar");
            if (bar > 0) AssertEqual(bar - 1, CrimsonMeter.BarAt(head - 1), "the tick before a bar head belongs to the previous bar");
        }
        AssertEqual(1, CrimsonMeter.BeatAtOrAfter(28), "exact beat tick");
        AssertEqual(2, CrimsonMeter.BeatAtOrAfter(29), "one tick late waits for the next beat");
        AssertEqual(2, CrimsonMeter.BeatAtOrAfter(28.5), "fractional ticks round up to the next beat");
        AssertEqual(1, CrimsonMeter.BarAtOrAfter(113), "bar one head");
        AssertEqual(2, CrimsonMeter.BarAtOrAfter(114), "one tick late waits for bar two");
        AssertEqual(0, CrimsonMeter.BarAt(112), "last tick of bar zero");
        AssertEqual(1, CrimsonMeter.BarAt(113), "first tick of bar one");
        AssertEqual(0, CrimsonMeter.BarAt(-5), "before musicStart is bar zero");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.BeatAtOrAfter(double.NaN), "NaN tick");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.BeatAtOrAfter(double.PositiveInfinity), "infinite tick");
    }

    [DomainTest("Scarlet meter next beats are consecutive grid beats at or after the request")]
    private static void ScarletMeterNextBeats()
    {
        for (int count = 1; count <= 17; count++)
        for (double earliest = 0; earliest < 3000; earliest += 13.37)
        {
            var beats = CrimsonMeter.NextBeats(earliest, count);
            AssertEqual(count, beats.Length, "requested count");
            AssertEqual(true, beats[0] >= earliest && beats[0] - earliest < 29, "first beat is the first one not before earliest");
            int first = CrimsonMeter.BeatAtOrAfter(earliest);
            for (int i = 0; i < count; i++)
            {
                AssertEqual((double)CrimsonMeter.BeatTick(first + i), beats[i], "consecutive grid beats, whole ticks");
                if (i > 0) AssertEqual(true, beats[i] - beats[i - 1] is 28 or 29, "one beat apart");
            }
        }
        AssertEqual(900d, CrimsonMeter.NextBeats(900, 1)[0], "a beat on the request is returned");
        AssertEqual(928d, CrimsonMeter.NextBeats(900.1, 1)[0], "a hair later waits for the next beat");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.NextBeats(0, 0), "zero beats");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.NextBeats(0, 18), "more beats than the descriptor holds");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.NextBeats(-1, 5), "negative request");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonMeter.NextBeats(double.NaN, 5), "NaN request");
    }

    [DomainTest("Scarlet meter pulse peaks on every beat head and hits harder on the downbeat")]
    private static void ScarletMeterPulse()
    {
        for (int k = 0; k < 400; k++)
        {
            int head = CrimsonMeter.BeatTick(k), nextHead = CrimsonMeter.BeatTick(k + 1);
            float peak = CrimsonMeter.Pulse(head);
            AssertEqual(k % 4 == 0 ? 1f : .72f, peak, "beat head strength: downbeat full, other beats .72");
            float last = peak;
            for (int tick = head + 1; tick < nextHead; tick++)
            {
                float value = CrimsonMeter.Pulse(tick);
                AssertEqual(true, value < last && value > 0, "pulse decays inside a beat");
                last = value;
            }
            AssertEqual(true, CrimsonMeter.Pulse(nextHead) > last, "next beat head restores the peak");
            AssertEqual(true, CrimsonMeter.Pulse(head + .5d) < peak && CrimsonMeter.Pulse(head + .5d) > peak * .85f, "half a tick after the head is still near the peak");
            if (k % 4 == 0) AssertEqual(true, CrimsonMeter.Pulse(head) > CrimsonMeter.Pulse(CrimsonMeter.BeatTick(k + 1)), "downbeat is stronger than the beat after it");
        }
        AssertEqual(0f, CrimsonMeter.Pulse(-1), "silent before musicStart");
        AssertEqual(0f, CrimsonMeter.Pulse(double.NaN), "silent for a non-number");
    }

    [DomainTest("Scarlet arrangement plays the song straight for the opening and continues the old music into each act")]
    private static void ScarletArrangementEntries()
    {
        AssertEqual(-1, CrimsonArrangement.Continue, "continue marker");
        AssertEqual(26, CrimsonArrangement.VictoryStopBar, "song's own full stop");
        AssertEqual(4, CrimsonArrangement.StageCount, "three acts and the Final");
        for (int bar = 0; bar < CrimsonMeter.OpeningBars; bar++)
            AssertEqual(bar + 1, CrimsonArrangement.SongBar(0, bar), "the opening is song bars 1 to 8 in order");
        for (int bar = 0; bar < 19; bar++)
            AssertEqual(bar + 1, CrimsonArrangement.SongBar(0, bar), "Act I's entry plays the song straight");
        for (int stage = 1; stage < CrimsonArrangement.StageCount; stage++)
        {
            AssertEqual(CrimsonArrangement.Continue, CrimsonArrangement.SongBar(stage, 0), "the first bar of a later stage keeps the previous music");
            AssertEqual(true, CrimsonArrangement.SongBar(stage, 1) > 0, "the second bar is a real song bar");
        }
        AssertEqual(26, CrimsonArrangement.SongBar(1, 1), "Act II enters on the song's stop");
        AssertEqual(56, CrimsonArrangement.SongBar(2, 1), "Act III enters on its own section");
        AssertEqual(26, CrimsonArrangement.SongBar(3, 1), "Final enters on the stop");
        AssertEqual(15, CrimsonArrangement.SongBar(3, 2), "Final's riser comes from song bar 15");
        AssertEqual(2, CrimsonArrangement.TransitionBars(1), "Act II transition is two bars");
        AssertEqual(2, CrimsonArrangement.TransitionBars(2), "Act III transition is two bars");
        AssertEqual(5, CrimsonArrangement.TransitionBars(3), "Final transition is five bars");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonArrangement.TransitionBars(0), "no transition into Act I");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonArrangement.TransitionBars(4), "no phase after the Final");
    }

    [DomainTest("Scarlet arrangement bodies loop with fixed periods and never leave the song")]
    private static void ScarletArrangementBodies()
    {
        // Body lengths: 10, 13, 8 and 25 bars after entries of 19, 6, 11 and 5 bars.
        int[] entry = { 19, 6, 11, 5 }, body = { 10, 13, 8, 25 };
        AssertEqual(20, CrimsonArrangement.SongBar(0, 19), "Act I body starts on song bar 20");
        AssertEqual(23, CrimsonArrangement.SongBar(0, 28), "Act I body ends on song bar 23");
        AssertEqual(20, CrimsonArrangement.SongBar(0, 29), "Act I body wraps");
        AssertEqual(31, CrimsonArrangement.SongBar(1, 6), "Act II body starts on song bar 31");
        AssertEqual(43, CrimsonArrangement.SongBar(1, 18), "Act II body ends on song bar 43");
        AssertEqual(31, CrimsonArrangement.SongBar(1, 19), "Act II body wraps");
        AssertEqual(66, CrimsonArrangement.SongBar(2, 11), "Act III body starts on song bar 66");
        AssertEqual(22, CrimsonArrangement.SongBar(2, 18), "Act III body ends on song bar 22");
        AssertEqual(66, CrimsonArrangement.SongBar(2, 19), "Act III body wraps");
        AssertEqual(48, CrimsonArrangement.SongBar(3, 5), "Final body starts on song bar 48");
        AssertEqual(47, CrimsonArrangement.SongBar(3, 29), "Final body ends on song bar 47");
        AssertEqual(48, CrimsonArrangement.SongBar(3, 30), "Final body wraps");
        for (int stage = 0; stage < CrimsonArrangement.StageCount; stage++)
        for (int bar = 0; bar < 5000; bar++)
        {
            int song = CrimsonArrangement.SongBar(stage, bar);
            if (bar == 0 && stage > 0) { AssertEqual(CrimsonArrangement.Continue, song, "only the first bar of a later stage continues"); continue; }
            AssertEqual(true, song >= 1 && song <= 70, "every song bar and its continuation exist in the 72-bar minimum song");
            if (bar >= entry[stage] + body[stage])
                AssertEqual(CrimsonArrangement.SongBar(stage, bar - body[stage]), song, "a body repeats with its period");
        }
        for (int stage = 0; stage < CrimsonArrangement.StageCount; stage++)
        for (int bar = 0; bar < 60; bar++)
            AssertEqual(stage == 3 && bar == 2, CrimsonArrangement.Swells(stage, bar), "only the Final's third bar swells");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonArrangement.SongBar(-1, 0), "negative stage");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonArrangement.SongBar(CrimsonArrangement.StageCount, 0), "unknown stage");
        AssertThrows<ArgumentOutOfRangeException>(() => CrimsonArrangement.SongBar(0, -1), "negative bar");
    }
}
