#nullable enable
using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Streams the stage-sequenced arrangement (CrimsonMusicMixer) into one dynamic voice.
// The authority clock leads: the cursor follows VisualAge from musicStart, stages follow
// the replicated bar-aligned phaseStart, and a stall re-anchors with a short blend.
[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonAudio : ModSystem
{
    private const int ChunkFrames = 1024, QueuedChunks = 3;
    private static bool tailPlaying;
    private CrimsonMusicMixer? mixer;
    private DynamicSoundEffectInstance? voice;
    private readonly float[] mix = new float[ChunkFrames * 2], blend = new float[ChunkFrames * 2];
    private readonly byte[] bytes = new byte[ChunkFrames * 4];
    private Guid fight;
    private long cursor;
    private int knownPhase, lastResync;
    private bool failed, ending;
    private float gain;
    private long reanchorFrom = -1;

    // Keeps the vanilla music suppressed until the Victory/Defeat tail has rung out.
    internal static bool TailPlaying => tailPlaying;

    // Where the score stands on this Fight's Victory cut into the song's full stop. The
    // mixer picks that beat from the write head, up to QueuedChunks ahead of the speakers
    // (and on the update after the stage changes), so presentation that must land on the
    // cut asks here instead of recomputing the beat from VisualAge.
    internal static ScoreCut VictoryCut(Guid fight)
    {
        var self = ModContent.GetInstance<CrimsonAudio>();
        if (self.failed || self.mixer is null || self.voice is null || self.fight != fight) return ScoreCut.Absent;
        if (!self.ending || self.mixer.EndAt < 0) return ScoreCut.Pending;
        // The playing chunk is the oldest pending one; the cut counts as heard from its middle.
        return self.Audible() + ChunkFrames / 2 >= self.mixer.EndAt ? ScoreCut.Heard : ScoreCut.Pending;
    }

    public override void PostSetupContent()
    {
        try
        {
            using var stream = new MemoryStream(Mod.GetFileBytes("Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg"));
            using var reader = new VorbisReader(stream, false);
            if (reader.Channels != 2 || reader.SampleRate != CrimsonMeter.SampleRate || reader.TotalSamples > CrimsonMeter.SampleRate * 300)
                throw new InvalidDataException("crimson.audio_format");
            var pcm = new short[reader.TotalSamples * 2];
            float[] samples = new float[8192]; int count, written = 0;
            while ((count = reader.ReadSamples(samples, 0, samples.Length)) > 0)
                for (int i = 0; i < count && written < pcm.Length; i++)
                    pcm[written++] = (short)Math.Clamp((int)(samples[i] * 32767), short.MinValue, short.MaxValue);
            mixer = new CrimsonMusicMixer(pcm);
        }
        catch (Exception e) { failed = true; Mod.Logger.Error("CrimsonFoundry score audio unavailable; gameplay remains independent.", e); }
    }

    public override void PostUpdateInput()
    {
        if (failed || mixer is null) return;
        var boss = CrimsonPackets.Boss;
        if (Main.gameMenu) { Stop(); return; }
        bool present = boss is not null && Array.Exists(boss.State.Members, m => m.Slot == Main.myPlayer);
        if (ending)
        {
            // The tail outlives the encounter's 150-tick terminal window and its NPC.
            if (mixer.Finished(cursor) || voice is null) { Stop(); return; }
            Pump(); return;
        }
        if (!present || boss!.State.MusicStart < 0 || boss.VisualAge < boss.State.MusicStart)
        {
            if (voice is not null) { gain = Math.Max(0, gain - .035f); voice.Volume = gain * Main.musicVolume; if (gain <= 0) Stop(); }
            return;
        }
        var state = boss.State;
        long expected = (long)Math.Round((boss.VisualAge - state.MusicStart) * CrimsonMeter.SamplesPerTick);
        if (fight != state.Fight || voice is null)
        {
            Stop(); fight = state.Fight;
            if (state.Stage is not (CrimsonStage.Countdown or CrimsonStage.Performance)) return;
            Start(expected);
        }
        if (voice is null) return;
        if (state.Phase > knownPhase && state.Phase <= 3 && state.PhaseStart >= state.MusicStart)
        {
            knownPhase = state.Phase;
            mixer.SetStage(knownPhase, CrimsonMeter.BarAt(state.PhaseStart - state.MusicStart));
        }
        if (state.Stage is CrimsonStage.Victory or CrimsonStage.Defeat)
        {
            ending = tailPlaying = true;
            mixer.End(state.Stage == CrimsonStage.Victory, CrimsonMusicMixer.NextBeat(cursor));
            Pump(); return;
        }
        if (Main.gamePaused || !Main.hasFocus)
        {
            if (voice.State == SoundState.Playing) voice.Pause();
            return;
        }
        if (voice.State == SoundState.Paused)
        {
            // Host play continues while unfocused; never resume at an old bar.
            voice.Resume();
            if (Math.Abs(expected - Audible()) > 8 * CrimsonMeter.SamplesPerTick) Reanchor(expected);
        }
        // Soft recovery only after a major device/stall discrepancy. Normal packets
        // never restart the music, and drift is not a source of gameplay RNG.
        if (boss.Fresh && Math.Abs(expected - Audible()) > 15 * CrimsonMeter.SamplesPerTick && boss.VisualAge - lastResync > 180)
        {
            lastResync = (int)boss.VisualAge; Reanchor(expected);
            Mod.Logger.Info($"CrimsonFoundry event=AudioReanchor score_tick={(int)(expected / CrimsonMeter.SamplesPerTick)}");
        }
        // The source is ~2.1dB hotter than Doll P1 and this direct PCM path lacks
        // Terraria's music-track mixing headroom.
        gain = CrimsonInvocation.MusicGain(expected / (double)CrimsonMeter.SamplesPerTick);
        voice.Volume = gain * Main.musicVolume;
        Pump();
    }

    private long Audible() => cursor - (voice?.PendingBufferCount ?? 0) * (long)ChunkFrames;

    private void Start(long frame)
    {
        try
        {
            mixer!.Reset(); knownPhase = 0; ending = tailPlaying = false; reanchorFrom = -1;
            voice = new DynamicSoundEffectInstance(CrimsonMeter.SampleRate, AudioChannels.Stereo) { Volume = 0 };
            cursor = Math.Max(0, frame); gain = 0;
            Pump(); voice.Play();
        }
        catch (Exception e) { failed = true; Mod.Logger.Error("CrimsonFoundry audio device could not start.", e); Stop(); }
    }

    // Jump the stream to where the authority clock says the music is, blending the old
    // continuation into the new position over the mixer's fade.
    private void Reanchor(long frame)
    {
        reanchorFrom = cursor; cursor = Math.Max(0, frame + (voice?.PendingBufferCount ?? 0) * (long)ChunkFrames);
    }

    private void Pump()
    {
        if (voice is null || mixer is null) return;
        while (voice.PendingBufferCount < QueuedChunks)
        {
            mixer.Render(cursor, mix, ChunkFrames);
            if (reanchorFrom >= 0)
            {
                mixer.Render(reanchorFrom, blend, ChunkFrames);
                for (int i = 0; i < ChunkFrames; i++)
                {
                    float t = Math.Min(1, (i + .5f) / CrimsonMusicMixer.FadeFrames);
                    float fadeIn = MathF.Sin(t * MathF.PI / 2), fadeOut = MathF.Cos(t * MathF.PI / 2);
                    mix[i * 2] = mix[i * 2] * fadeIn + blend[i * 2] * fadeOut;
                    mix[i * 2 + 1] = mix[i * 2 + 1] * fadeIn + blend[i * 2 + 1] * fadeOut;
                }
                reanchorFrom = -1;
            }
            for (int i = 0; i < mix.Length; i++)
            {
                short s = (short)Math.Clamp((int)(mix[i] * 32767), short.MinValue, short.MaxValue);
                bytes[i * 2] = (byte)s; bytes[i * 2 + 1] = (byte)(s >> 8);
            }
            voice.SubmitBuffer(bytes);
            cursor += ChunkFrames;
        }
    }

    private void Stop()
    {
        voice?.Stop(); voice?.Dispose(); voice = null;
        fight = Guid.Empty; gain = 0; knownPhase = 0; ending = tailPlaying = false; reanchorFrom = -1;
    }
    public override void OnWorldUnload() => Stop();
    public override void ClearWorld() => Stop();
    public override void Unload() { Stop(); mixer = null; failed = false; }
}

// Absent: no score streams for that Fight (failed, not started or another Fight).
internal enum ScoreCut : byte { Absent, Pending, Heard }

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonMusicScene : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    public override int Music => 0; // Scoped suppression of the normal music mix; never changes settings.
    public override bool IsSceneEffectActive(Player player) => !Main.gameMenu && (CrimsonAudio.TailPlaying
        || CrimsonPackets.Boss is { } boss && Array.Exists(boss.State.Members, m => m.Slot == player.whoAmI));
}
