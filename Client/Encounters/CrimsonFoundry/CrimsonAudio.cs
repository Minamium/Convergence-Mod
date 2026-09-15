#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// One native looping buffer: no update-tick restart or compressed silence at the
// seam. A late join/focus recovery starts at the accepted score position.
[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonAudio : ModSystem
{
    private byte[]? pcm;
    private SoundEffect? effect;
    private SoundEffectInstance? voice;
    private Guid fight;
    private readonly Stopwatch playback = new();
    private double startAge;
    private bool failed;
    private float gain;
    private int lastResync;

    public override void PostSetupContent()
    {
        try
        {
            using var stream = new MemoryStream(Mod.GetFileBytes("Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg"));
            using var reader = new VorbisReader(stream, false);
            if (reader.Channels != 2 || reader.SampleRate != 48000 || reader.TotalSamples > 48000 * 300)
                throw new InvalidDataException("crimson.audio_format");
            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output);
            float[] samples = new float[8192]; int count;
            while ((count = reader.ReadSamples(samples, 0, samples.Length)) > 0)
                for (int i = 0; i < count; i++) writer.Write((short)Math.Clamp((int)(samples[i] * 32767), short.MinValue, short.MaxValue));
            pcm = output.ToArray();
        }
        catch (Exception e) { failed = true; Mod.Logger.Error("CrimsonFoundry score audio unavailable; gameplay remains independent.", e); }
    }
    public override void PostUpdateInput()
    {
        if (failed || pcm is null) return;
        var boss = CrimsonPackets.Boss;
        bool present = !Main.gameMenu && boss is not null && Array.Exists(boss.State.Members, m => m.Slot == Main.myPlayer);
        bool audible = present && boss!.State.MusicStart >= 0 && boss.VisualAge >= boss.State.MusicStart
            && boss.State.Stage is CrimsonStage.Countdown or CrimsonStage.Performance;
        if (!audible)
        {
            gain = Math.Max(0, gain - .035f);
            if (voice is not null) voice.Volume = gain * Main.musicVolume;
            if (gain <= 0 || Main.gameMenu) Stop();
            return;
        }
        double age = boss!.VisualAge - boss.State.MusicStart;
        if (fight != boss.State.Fight || voice is null)
        {
            Stop(); fight = boss.State.Fight;
            Start(age);
        }
        if (voice is null) return;
        if (Main.gamePaused || !Main.hasFocus)
        {
            if (voice.State == SoundState.Playing) { voice.Pause(); playback.Stop(); }
            return;
        }
        if (voice.State == SoundState.Paused)
        {
            // Host play continues while unfocused; never resume at an old bar.
            if (Math.Abs(startAge + playback.Elapsed.TotalSeconds * 60 - age) > 8) Start(age);
            else { voice.Resume(); playback.Start(); }
        }
        // Soft recovery only after a major device/stall discrepancy. Normal
        // packets do not restart music, and drift is not a source of gameplay RNG.
        if (boss.Fresh && Math.Abs(startAge + playback.Elapsed.TotalSeconds * 60 - age) > 15
            && age - lastResync > 180 && CrimsonRegistration.Score.Pulse(age) > .65f)
        {
            lastResync = (int)age; Start(age);
            Mod.Logger.Info($"CrimsonFoundry event=AudioReanchor score_tick={(int)age}");
        }
        gain = Math.Min(.88f, gain + .06f);
        if (voice is not null) voice.Volume = gain * Main.musicVolume;
    }
    private void Start(double age)
    {
        if (pcm is null) return;
        voice?.Stop(); voice?.Dispose(); effect?.Dispose(); voice = null; effect = null;
        try
        {
            var score = CrimsonRegistration.Score;
            int position = score.SampleAt(age), end = score.LoopEndSample, start = score.LoopStartSample;
            int intro = end - position, loop = end - start;
            if (pcm.Length < end * 4) throw new InvalidDataException("crimson.audio_truncated");
            if (position <= start)
                effect = new SoundEffect(pcm, position * 4, intro * 4, score.SampleRate, AudioChannels.Stereo, start - position, loop);
            else
            {
                byte[] joined = new byte[(intro + loop) * 4];
                Buffer.BlockCopy(pcm, position * 4, joined, 0, intro * 4);
                Buffer.BlockCopy(pcm, start * 4, joined, intro * 4, loop * 4);
                effect = new SoundEffect(joined, 0, joined.Length, score.SampleRate, AudioChannels.Stereo, intro, loop);
            }
            voice = effect.CreateInstance(); voice.IsLooped = true; voice.Volume = 0;
            startAge = age; gain = 0; playback.Restart(); voice.Play();
        }
        catch (Exception e) { failed = true; Mod.Logger.Error("CrimsonFoundry audio device could not start.", e); Stop(); }
    }
    private void Stop()
    {
        voice?.Stop(); voice?.Dispose(); voice = null; effect?.Dispose(); effect = null;
        playback.Reset(); fight = Guid.Empty; gain = 0;
    }
    public override void OnWorldUnload() => Stop();
    public override void ClearWorld() => Stop();
    public override void Unload() { Stop(); pcm = null; failed = false; }
}

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonMusicScene : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    public override int Music => 0; // Scoped suppression of the normal music mix; never changes settings.
    public override bool IsSceneEffectActive(Player player) => !Main.gameMenu && CrimsonPackets.Boss is { } boss
        && Array.Exists(boss.State.Members, m => m.Slot == player.whoAmI);
}
