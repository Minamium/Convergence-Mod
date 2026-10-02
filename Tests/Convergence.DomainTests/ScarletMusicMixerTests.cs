using System;
using Convergence.Client.Encounters.CrimsonFoundry;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    // A synthetic song of 80 bars in which every sample of song bar n equals 400n (left) and
    // 200n (right), so any rendered value names the song bar it was read from.
    private const int MixerSongBars = 80;
    private static short[]? mixerSong;
    private static short[] MixerSong()
    {
        if (mixerSong is not null) return mixerSong;
        var pcm = new short[CrimsonMeter.BarSamples * MixerSongBars * 2];
        for (int bar = 0; bar < MixerSongBars; bar++)
        {
            int start = bar * CrimsonMeter.BarSamples * 2;
            for (int i = 0; i < CrimsonMeter.BarSamples; i++)
            {
                pcm[start + i * 2] = (short)(bar * 400);
                pcm[start + i * 2 + 1] = (short)(bar * 200);
            }
        }
        return mixerSong = pcm;
    }
    private static float MixerLevel(int songBar) => songBar * 400 / 32768f;
    private static float MixerLevelRight(int songBar) => songBar * 200 / 32768f;
    private static (float Left, float Right) MixerFrame(CrimsonMusicMixer mixer, long frame)
    {
        var output = new float[2];
        mixer.Render(frame, output, 1);
        return (output[0], output[1]);
    }
    private static bool MixerNear(float expected, float actual, float tolerance = .0005f) => Math.Abs(expected - actual) <= tolerance;

    [DomainTest("Scarlet mixer rejects a truncated song and plays Act I's bars in song order")]
    private static void ScarletMixerStageZero()
    {
        AssertThrows<ArgumentException>(() => new CrimsonMusicMixer(new short[CrimsonMeter.BarSamples * 71 * 2]), "71 bars is not a whole song");
        AssertThrows<ArgumentException>(() => new CrimsonMusicMixer(new short[CrimsonMeter.BarSamples * 72 * 2 + 1]), "odd sample count is not stereo");
        var mixer = new CrimsonMusicMixer(MixerSong());
        for (int bar = 0; bar < 40; bar++)
        {
            int song = CrimsonArrangement.SongBar(0, bar);
            AssertEqual(song, mixer.SongBarAt(bar), "mixer follows the arrangement");
            // Past the 14 ms blend every frame of an arrangement bar is that song bar.
            foreach (int within in new[] { CrimsonMusicMixer.FadeFrames, 5000, CrimsonMeter.BarSamples - 1 })
            {
                var (left, right) = MixerFrame(mixer, (long)bar * CrimsonMeter.BarSamples + within);
                AssertEqual(true, MixerNear(MixerLevel(song), left) && MixerNear(MixerLevelRight(song), right), $"arrangement bar {bar} plays song bar {song}");
            }
        }
        for (int bar = 1; bar < 8; bar++)
        {
            // The opening is contiguous song: no blend, so the very first frame is already the new bar.
            var (left, _) = MixerFrame(mixer, (long)bar * CrimsonMeter.BarSamples);
            AssertEqual(true, MixerNear(MixerLevel(bar + 1), left, 0), "contiguous bars are not crossfaded");
        }
        var first = MixerFrame(mixer, 0);
        AssertEqual(true, MixerNear(MixerLevel(1), first.Left, 0), "musicStart plays song bar one, not the silent lead-in");
        AssertEqual(false, mixer.Finished(0), "an unfinished piece has no end");
        AssertEqual(-1L, mixer.EndAt, "no ending yet");
    }

    [DomainTest("Scarlet mixer continues the previous stage into each act's first bar and reads the new bar after it")]
    private static void ScarletMixerStages()
    {
        var mixer = new CrimsonMusicMixer(MixerSong());
        mixer.SetStage(1, 30);
        for (int bar = 0; bar < 29; bar++) AssertEqual(CrimsonArrangement.SongBar(0, bar), mixer.SongBarAt(bar), "Act I until the act starts");
        // Bar 30 is Act II's first bar: Act I's body carries on.
        AssertEqual(CrimsonArrangement.SongBar(0, 30), mixer.SongBarAt(30), "the first Act II bar keeps Act I playing");
        AssertEqual(26, mixer.SongBarAt(31), "Act II's second bar is song bar 26");
        for (int bar = 31; bar < 80; bar++) AssertEqual(CrimsonArrangement.SongBar(1, bar - 30), mixer.SongBarAt(bar), "Act II arrangement");
        AssertEqual(31, mixer.SongBarAt(36), "Act II's body starts after its entry");
        {
            var (left, right) = MixerFrame(mixer, 30L * CrimsonMeter.BarSamples + 10000);
            int carried = CrimsonArrangement.SongBar(0, 30);
            AssertEqual(true, MixerNear(MixerLevel(carried), left) && MixerNear(MixerLevelRight(carried), right), "rendered continuation matches the previous stage");
        }
        mixer.SetStage(2, 50);
        AssertEqual(CrimsonArrangement.SongBar(1, 50 - 30), mixer.SongBarAt(50), "Act III's first bar continues Act II");
        AssertEqual(56, mixer.SongBarAt(51), "Act III entry");
        mixer.SetStage(3, 70);
        AssertEqual(CrimsonArrangement.SongBar(2, 70 - 50), mixer.SongBarAt(70), "Final's first bar continues Act III");
        AssertEqual(26, mixer.SongBarAt(71), "Final entry is the stop");
        AssertEqual(15, mixer.SongBarAt(72), "Final riser");
        AssertEqual(48, mixer.SongBarAt(75), "Final body");

        // A stage that arrives before this peer ever heard its predecessor (a late joiner)
        // continues that predecessor's loop instead of a silent bar.
        var late = new CrimsonMusicMixer(MixerSong());
        late.SetStage(2, 100);
        int expected = CrimsonArrangement.SongBar(1, 64 + 100 % 64);
        AssertEqual(expected, late.SongBarAt(100), "late joiner continuation");
        AssertEqual(true, expected is >= 1 and <= 70, "the continuation is a real song bar");
        late.Reset();
        AssertEqual(CrimsonArrangement.SongBar(0, 100), late.SongBarAt(100), "Reset returns to the opening arrangement");

        AssertThrows<ArgumentOutOfRangeException>(() => mixer.SetStage(0, 5), "Act I is not a later stage");
        AssertThrows<ArgumentOutOfRangeException>(() => mixer.SetStage(4, 5), "unknown stage");
        AssertThrows<ArgumentOutOfRangeException>(() => mixer.SetStage(1, 0), "a later stage starts after bar zero");
    }

    [DomainTest("Scarlet mixer crossfades only the head of a bar that jumps and leaves contiguous bars untouched")]
    private static void ScarletMixerJumpCrossfade()
    {
        var mixer = new CrimsonMusicMixer(MixerSong());
        mixer.SetStage(1, 20);
        // Bar 20 continues Act I (song bar 21); bar 21 jumps to song bar 26 although bar 20 would continue into 22.
        long head = 21L * CrimsonMeter.BarSamples;
        AssertEqual(21, mixer.SongBarAt(20), "bar before the jump");
        AssertEqual(26, mixer.SongBarAt(21), "bar after the jump");
        var start = MixerFrame(mixer, head);
        AssertEqual(true, MixerNear(MixerLevel(22), start.Left, .004f), "the first frame still carries the old music on");
        AssertEqual(true, MixerNear(MixerLevelRight(22), start.Right, .004f), "right channel carries on too");
        var early = MixerFrame(mixer, head + 10);
        AssertEqual(true, Math.Abs(early.Left - MixerLevel(22)) < Math.Abs(early.Left - MixerLevel(26)), "ten frames in is still mostly old");
        var middle = MixerFrame(mixer, head + CrimsonMusicMixer.FadeFrames / 2);
        AssertEqual(true, middle.Left > MixerLevel(22) * .5f && middle.Left < 1.5f, "halfway the two are blended with equal power");
        var late = MixerFrame(mixer, head + CrimsonMusicMixer.FadeFrames - 1);
        AssertEqual(true, Math.Abs(late.Left - MixerLevel(26)) < Math.Abs(late.Left - MixerLevel(22)), "the last frames of the blend are mostly new");
        foreach (long after in new[] { CrimsonMusicMixer.FadeFrames, CrimsonMusicMixer.FadeFrames + 1, 20000, CrimsonMeter.BarSamples - 1 })
        {
            var settled = MixerFrame(mixer, head + after);
            AssertEqual(true, MixerNear(MixerLevel(26), settled.Left, 0) && MixerNear(MixerLevelRight(26), settled.Right, 0), $"from frame {after} on only the new bar sounds");
        }
        float last = 0;
        for (int i = 0; i < CrimsonMusicMixer.FadeFrames; i += 8)
        {
            last = MixerFrame(mixer, head + i).Left;
            AssertEqual(true, float.IsFinite(last) && Math.Abs(last) <= 1.5f, "blend stays finite and bounded");
        }
        AssertEqual(true, last > MixerLevel(22), "the blend ends above where it began");
        // Bars that follow their predecessor are not blended even at their very first frame.
        var contiguous = MixerFrame(mixer, 22L * CrimsonMeter.BarSamples);
        AssertEqual(true, MixerNear(MixerLevel(27), contiguous.Left, 0), "song bar 26 into 27 is a straight continuation");
    }

    [DomainTest("Scarlet mixer swells only the Final's riser from near silence")]
    private static void ScarletMixerSwell()
    {
        var mixer = new CrimsonMusicMixer(MixerSong());
        mixer.SetStage(3, 40);
        long riser = 42L * CrimsonMeter.BarSamples;
        AssertEqual(15, mixer.SongBarAt(42), "riser plays song bar 15");
        float full = MixerLevel(15);
        float first = MixerFrame(mixer, riser).Left;
        // Its first frames still blend in the stop's ring (song bar 27) at the swell's 12 percent floor.
        AssertEqual(true, first >= 0 && first < MixerLevel(27) * .15f, "the riser enters near silence out of the stop's ring");
        float early = MixerFrame(mixer, riser + 1000).Left;
        float quarter = MixerFrame(mixer, riser + CrimsonMeter.BeatSamples / 2).Left;
        float half = MixerFrame(mixer, riser + CrimsonMeter.BeatSamples).Left;
        float nearEnd = MixerFrame(mixer, riser + CrimsonMeter.BeatSamples * 2 - 1).Left;
        AssertEqual(true, early < full * .2f, "after the blend the riser is still quiet");
        AssertEqual(true, early < quarter && quarter < half && half < nearEnd, "the swell rises monotonically");
        AssertEqual(true, nearEnd < full && nearEnd > full * .98f, "the swell reaches full level at two beats");
        AssertEqual(true, MixerNear(full, MixerFrame(mixer, riser + CrimsonMeter.BeatSamples * 2).Left, 0), "no gain change after the swell");
        AssertEqual(true, MixerNear(full, MixerFrame(mixer, riser + CrimsonMeter.BarSamples - 1).Left, 0), "the rest of the bar is full level");
        // The same arrangement bar position in another stage is not touched.
        var actTwo = new CrimsonMusicMixer(MixerSong());
        actTwo.SetStage(1, 40);
        var (left, _) = MixerFrame(actTwo, 42L * CrimsonMeter.BarSamples);
        AssertEqual(true, MixerNear(MixerLevel(27), left, 0), "Act II's third bar is not swelled");
        // And the Final's other bars are not.
        var (other, _) = MixerFrame(mixer, 43L * CrimsonMeter.BarSamples);
        AssertEqual(16, mixer.SongBarAt(43), "the bar after the riser");
        AssertEqual(true, MixerNear(MixerLevel(16), other, 0), "the bar after the riser is at full level");
    }

    [DomainTest("Scarlet mixer Victory cuts to the song's full stop with a ring that ends and stays bounded")]
    private static void ScarletMixerVictory()
    {
        var mixer = new CrimsonMusicMixer(MixerSong());
        long at = 6L * CrimsonMeter.BarSamples;
        AssertEqual(false, mixer.Finished(at + CrimsonMeter.BarSamples * 100L), "nothing ends before End is called");
        mixer.End(true, at);
        mixer.End(false, at + 1000); // The first ending wins.
        AssertEqual(at, mixer.EndAt, "ending frame recorded once");
        long total = CrimsonMeter.BarSamples + CrimsonMusicMixer.VictoryTailFrames;
        AssertEqual(false, mixer.Finished(at + total - 1), "still ringing one frame before the tail ends");
        AssertEqual(true, mixer.Finished(at + total), "finished once the stop and its tail have played");
        AssertEqual(false, mixer.Finished(at - 1), "not finished before the ending frame");

        var chunk = new float[4096 * 2];
        float stopLevel = MixerLevel(CrimsonArrangement.VictoryStopBar);
        float sumStop = 0, sumRing = 0, sumTail = 0; int stopCount = 0, ringCount = 0, tailCount = 0;
        long from = at - 8192;
        // Consecutive chunks: the hall tail is stateful.
        while (from < at + total + 4096)
        {
            mixer.Render(from, chunk, 4096);
            for (int i = 0; i < chunk.Length; i++)
                AssertEqual(true, float.IsFinite(chunk[i]) && Math.Abs(chunk[i]) <= 1.5f, "output is finite and bounded");
            for (int i = 0; i < 4096; i++)
            {
                long t = from + i - at;
                float l = chunk[i * 2];
                if (t >= CrimsonMusicMixer.FadeFrames * 2 && t < CrimsonMeter.BarSamples - CrimsonMeter.BeatSamples)
                { sumStop += l; stopCount++; }
                if (t >= CrimsonMeter.BarSamples + 2000 && t < CrimsonMeter.BarSamples + 12000) { sumRing += Math.Abs(l); ringCount++; }
                if (t >= total - 4096 && t < total) { sumTail += Math.Abs(l); tailCount++; }
            }
            from += 4096;
        }
        float stop = sumStop / stopCount;
        // The test song is a constant level, the worst case for a reverb, so the wash is allowed to be large.
        AssertEqual(true, stop > stopLevel * .9f && stop < stopLevel * 2.2f, "the stop bar sounds at its own level plus the hall wash");
        AssertEqual(true, Math.Abs(stop - stopLevel) < Math.Abs(stop - MixerLevel(mixer.SongBarAt(6))), "it replaced the stage music");
        AssertEqual(true, sumRing / ringCount > .005f, "after the stop the hall keeps ringing instead of cutting dead");
        AssertEqual(true, sumTail / tailCount < .002f && sumTail / tailCount < sumRing / ringCount * .1f, "the tail has rung out when Finished turns true");
    }

    [DomainTest("Scarlet mixer Defeat falls through a closing filter to exact silence")]
    private static void ScarletMixerDefeat()
    {
        var mixer = new CrimsonMusicMixer(MixerSong());
        long at = 3L * CrimsonMeter.BarSamples;
        mixer.End(false, at);
        AssertEqual(CrimsonMusicMixer.DefeatFrames, CrimsonMeter.BarSamples * 3 / 2, "a bar and a half");
        AssertEqual(false, mixer.Finished(at + CrimsonMusicMixer.DefeatFrames - 1), "still fading");
        AssertEqual(true, mixer.Finished(at + CrimsonMusicMixer.DefeatFrames), "done after DefeatFrames");

        var chunk = new float[4096 * 2];
        float early = 0, late = 0; int earlyCount = 0, lateCount = 0;
        long from = at - 4096;
        while (from < at + CrimsonMusicMixer.DefeatFrames + 8192)
        {
            mixer.Render(from, chunk, 4096);
            for (int i = 0; i < 4096; i++)
            {
                long t = from + i - at;
                float l = chunk[i * 2], r = chunk[i * 2 + 1];
                AssertEqual(true, float.IsFinite(l) && float.IsFinite(r) && Math.Abs(l) <= 1.5f && Math.Abs(r) <= 1.5f, "output is finite and bounded");
                if (t < 0) AssertEqual(true, MixerNear(MixerLevel(mixer.SongBarAt(2)), l, .0005f), "the stage music plays untouched until the ending");
                if (t >= 2000 && t < 12000) { early += Math.Abs(l); earlyCount++; }
                if (t >= CrimsonMusicMixer.DefeatFrames - 12000 && t < CrimsonMusicMixer.DefeatFrames - 2000) { late += Math.Abs(l); lateCount++; }
                if (t >= CrimsonMusicMixer.DefeatFrames) AssertEqual(0f, l + r, "silent from DefeatFrames on");
            }
            from += 4096;
        }
        AssertEqual(true, early / earlyCount > 0 && late / lateCount < early / earlyCount * .2f, "the level falls away before it reaches silence");
    }

    [DomainTest("Scarlet mixer output stays finite and bounded across stage changes, endings and bad arguments")]
    private static void ScarletMixerBounds()
    {
        var mixer = new CrimsonMusicMixer(MixerSong());
        mixer.SetStage(1, 25); mixer.SetStage(2, 45); mixer.SetStage(3, 70);
        var chunk = new float[2048 * 2];
        for (long from = 0; from < 100L * CrimsonMeter.BarSamples; from += 2048 * 7)
        {
            mixer.Render(from, chunk, 2048);
            for (int i = 0; i < chunk.Length; i++)
                AssertEqual(true, float.IsFinite(chunk[i]) && Math.Abs(chunk[i]) <= 1.5f, "arrangement output is finite and bounded");
        }
        // Rendering beyond the song's end is silence, never an exception.
        var tail = new float[8];
        mixer.Render(10_000_000_000L, tail, 4);
        foreach (float v in tail) AssertEqual(true, float.IsFinite(v) && Math.Abs(v) <= 1.5f, "far beyond the end");
        AssertEqual(0L, CrimsonMusicMixer.NextBeat(0), "a beat head stays");
        AssertEqual(22500L, CrimsonMusicMixer.NextBeat(1), "one frame late waits for the next beat");
        AssertEqual(22500L, CrimsonMusicMixer.NextBeat(22500), "a beat head stays");
        AssertEqual(67500L, CrimsonMusicMixer.NextBeat(45001), "rounds up to the next beat head");
        mixer.End(true, -1); // Negative ending frames are ignored.
        AssertEqual(-1L, mixer.EndAt, "negative end ignored");
        AssertThrows<ArgumentOutOfRangeException>(() => mixer.Render(-1, chunk, 8), "negative start frame");
        AssertThrows<ArgumentOutOfRangeException>(() => mixer.Render(0, new float[3], 2), "output too small");
        AssertThrows<ArgumentOutOfRangeException>(() => mixer.Render(0, chunk, -1), "negative length");
    }
}
