#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Renders the stage-sequenced Graceful Ordeal arrangement from the decoded song
// (interleaved 16-bit stereo, 48 kHz). Arrangement frames count from musicStart,
// which plays song bar 1. Only cut, reordered recording is heard: jumps blend the
// real continuation audio for 14 ms, the Final riser swells in, Victory cuts to the
// song's own full stop with a hall tail and Defeat closes behind a falling filter.
internal sealed class CrimsonMusicMixer
{
    internal const int FadeFrames = 672, VictoryTailFrames = CrimsonMeter.SampleRate * 7 / 2;
    internal const int DefeatFrames = CrimsonMeter.BarSamples * 3 / 2;
    private const float VictoryWet = .22f;
    private readonly short[] song;
    private readonly int songFrames;
    private readonly int[] stageBars = new int[CrimsonArrangement.StageCount];
    private long endAt = -1;
    private bool won;
    private float lowLeft, lowRight;
    private readonly HallTail hall = new();

    internal CrimsonMusicMixer(short[] interleaved)
    {
        if (interleaved.Length % 2 != 0 || interleaved.Length / 2 < CrimsonMeter.BarSamples * 72)
            throw new ArgumentException("crimson.audio_truncated", nameof(interleaved));
        song = interleaved; songFrames = interleaved.Length / 2;
        Reset();
    }
    internal long EndAt => endAt;
    internal void Reset()
    {
        Array.Fill(stageBars, -1); stageBars[0] = 0;
        endAt = -1; won = false; lowLeft = lowRight = 0; hall.Clear();
    }
    // A later stage starts at the arrangement bar of its bar-aligned phaseStart.
    internal void SetStage(int stage, int startBar)
    {
        if (stage is < 1 or >= CrimsonArrangement.StageCount || startBar < 1) throw new ArgumentOutOfRangeException();
        stageBars[stage] = startBar;
    }
    // Endings start on a beat of the arrangement grid; they are not stage changes.
    internal void End(bool victory, long frame)
    {
        if (endAt >= 0 || frame < 0) return;
        endAt = frame; won = victory; hall.Clear(); lowLeft = lowRight = 0;
    }
    internal bool Finished(long frame) => endAt >= 0 && frame >= endAt + (won ? CrimsonMeter.BarSamples + VictoryTailFrames : DefeatFrames);
    internal static long NextBeat(long frame) => (frame + CrimsonMeter.BeatSamples - 1) / CrimsonMeter.BeatSamples * CrimsonMeter.BeatSamples;

    internal int SongBarAt(int bar)
    {
        int stage = 0;
        for (int s = 1; s < stageBars.Length; s++) if (stageBars[s] >= 0 && stageBars[s] <= bar) stage = s;
        int mapped = CrimsonArrangement.SongBar(stage, bar - stageBars[stage]);
        if (mapped != CrimsonArrangement.Continue) return mapped;
        // The first bar of a stage keeps its predecessor playing. A late joiner who never
        // saw that predecessor hears its loop instead of a silent bar.
        int previous = stage - 1, start = stageBars[previous];
        return CrimsonArrangement.SongBar(previous, start >= 0 ? bar - start : 64 + bar % 64);
    }
    private bool Swells(int bar)
    {
        int stage = 0;
        for (int s = 1; s < stageBars.Length; s++) if (stageBars[s] >= 0 && stageBars[s] <= bar) stage = s;
        return CrimsonArrangement.Swells(stage, bar - stageBars[stage]);
    }

    // Writes interleaved stereo floats for arrangement frames [from, from + frames).
    // Call with consecutive ranges while an ending plays; its hall tail is stateful.
    internal void Render(long from, float[] output, int frames)
    {
        if (from < 0 || frames < 0 || output.Length < frames * 2) throw new ArgumentOutOfRangeException();
        for (int i = 0; i < frames; i++)
        {
            long frame = from + i;
            Arrangement(frame, out float left, out float right);
            if (endAt >= 0 && frame >= endAt)
                (left, right) = won ? Victory(frame, left, right) : Defeat(frame, left, right);
            output[i * 2] = left; output[i * 2 + 1] = right;
        }
    }

    private void Arrangement(long frame, out float left, out float right)
    {
        int bar = (int)(frame / CrimsonMeter.BarSamples), within = (int)(frame % CrimsonMeter.BarSamples);
        int songBar = SongBarAt(bar);
        Read((long)songBar * CrimsonMeter.BarSamples + within, out left, out right);
        if (bar > 0 && within < FadeFrames)
        {
            int previous = SongBarAt(bar - 1);
            if (previous + 1 != songBar)
            {
                // Equal-power blend with how the previous bar really continued.
                Read((long)(previous + 1) * CrimsonMeter.BarSamples + within, out float l0, out float r0);
                float t = (within + .5f) / FadeFrames, fadeIn = MathF.Sin(t * MathF.PI / 2), fadeOut = MathF.Cos(t * MathF.PI / 2);
                left = left * fadeIn + l0 * fadeOut; right = right * fadeIn + r0 * fadeOut;
            }
        }
        if (Swells(bar) && within < CrimsonMeter.BeatSamples * 2)
        {
            float g = .12f + .88f * MathF.Pow(within / (CrimsonMeter.BeatSamples * 2f), 1.6f);
            left *= g; right *= g;
        }
    }

    private (float, float) Victory(long frame, float left, float right)
    {
        long t = frame - endAt;
        float dryL = 0, dryR = 0;
        if (t < CrimsonMeter.BarSamples)
        {
            Read((long)CrimsonArrangement.VictoryStopBar * CrimsonMeter.BarSamples + t, out dryL, out dryR);
            long fadeStart = CrimsonMeter.BarSamples - CrimsonMeter.BeatSamples;
            if (t >= fadeStart) { float g = 1 - (t - fadeStart) / (float)CrimsonMeter.BeatSamples; dryL *= g * g; dryR *= g * g; }
            if (t < FadeFrames)
            {
                float u = (t + .5f) / FadeFrames, fadeIn = MathF.Sin(u * MathF.PI / 2), fadeOut = MathF.Cos(u * MathF.PI / 2);
                dryL = dryL * fadeIn + left * fadeOut; dryR = dryR * fadeIn + right * fadeOut;
            }
        }
        hall.Process(dryL, dryR, out float wetL, out float wetR);
        return (dryL + wetL * VictoryWet, dryR + wetR * VictoryWet);
    }

    private (float, float) Defeat(long frame, float left, float right)
    {
        float t = Math.Min(1, (frame - endAt) / (float)CrimsonMeter.BarSamples);
        float cutoff = 16000 * MathF.Pow(250 / 16000f, t);
        float a = 1 - MathF.Exp(-MathF.Tau * cutoff / CrimsonMeter.SampleRate);
        lowLeft += (left - lowLeft) * a; lowRight += (right - lowRight) * a;
        float gain = Math.Max(0, 1 - (frame - endAt) / (float)DefeatFrames);
        return (lowLeft * gain * gain, lowRight * gain * gain);
    }

    private void Read(long frame, out float left, out float right)
    {
        if (frame < 0 || frame >= songFrames) { left = right = 0; return; }
        left = song[frame * 2] / 32768f; right = song[frame * 2 + 1] / 32768f;
    }

    // A small Schroeder hall (four damped combs and two allpasses per channel).
    private sealed class HallTail
    {
        private static readonly int[] CombDelays = { 1694, 1760, 1622, 1547 };
        private static readonly int[] AllpassDelays = { 605, 245 };
        private const int Spread = 25;
        private readonly float[][] combs = new float[8][], allpasses = new float[4][];
        private readonly int[] combIndex = new int[8], allpassIndex = new int[4];
        private readonly float[] damping = new float[8];
        internal HallTail()
        {
            for (int c = 0; c < 8; c++) combs[c] = new float[CombDelays[c % 4] + (c >= 4 ? Spread : 0)];
            for (int a = 0; a < 4; a++) allpasses[a] = new float[AllpassDelays[a % 2] + (a >= 2 ? Spread : 0)];
        }
        internal void Clear()
        {
            foreach (var b in combs) Array.Clear(b);
            foreach (var b in allpasses) Array.Clear(b);
            Array.Clear(combIndex); Array.Clear(allpassIndex); Array.Clear(damping);
        }
        internal void Process(float left, float right, out float outLeft, out float outRight)
        {
            float input = (left + right) * .03f;
            outLeft = Channel(input, 0); outRight = Channel(input, 4);
        }
        private float Channel(float input, int offset)
        {
            float sum = 0;
            for (int c = offset; c < offset + 4; c++)
            {
                var buffer = combs[c]; int i = combIndex[c];
                float y = buffer[i];
                damping[c] = y * .8f + damping[c] * .2f;
                buffer[i] = input + damping[c] * .93f; // ~3 s to -60 dB
                combIndex[c] = (i + 1) % buffer.Length;
                sum += y;
            }
            for (int a = offset / 2; a < offset / 2 + 2; a++)
            {
                var buffer = allpasses[a]; int i = allpassIndex[a];
                float delayed = buffer[i];
                buffer[i] = sum + delayed * .5f;
                sum = delayed - sum;
                allpassIndex[a] = (i + 1) % buffer.Length;
            }
            return sum;
        }
    }
}
