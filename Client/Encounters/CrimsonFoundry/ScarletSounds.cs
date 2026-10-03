#nullable enable
using System;
using System.Collections.Generic;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// The owner-approved Scarlet sound set (variant A of the 2026-10-02 audition): one
// layered CC0 recording per moment, tuned to Graceful Ordeal's E-flat family.
internal enum ScarletCue : byte
{
    Foretell, CrossflowCharge, CrossflowRelease, Impact,
    StackSummon, StackSuccess, StackFail, SpreadSummon, SpreadSuccess, SpreadFail,
    ActChange, Sacrifice, Victory, Down, Revive, Ready
}

internal static class ScarletSounds
{
    internal const string Root = "Convergence/Assets/Sounds/CrimsonFoundry/";
    // The files carry their levels against Graceful Ordeal (ENCOUNTER_SPEC.md#sound-effects:
    // each group lifted once on 2026-10-03, so the balance inside a group is the audition's),
    // so the whole set shares one gain.
    internal const float Gain = 1f;
    // An early release (Fight end) fades over this many ticks; it is never a hard cut.
    internal const int FadeTicks = 8;
    // The sacrifice swell peaks into its flash 0.95 s after the onset.
    internal const int SacrificeFlashTicks = 57;
    // The lease outlasts the file by more than the fade, so a natural ending is never touched.
    private const int LeaseMargin = FadeTicks + 2;
    private readonly record struct Spec(string File, int Ticks, int Instances, SoundLimitBehavior Limit);
    // Ticks = the file's whole length (ceil(seconds * 60)); a lease never cuts a cue short.
    private static readonly Spec[] Specs =
    {
        new("ScarletForetell", 38, 3, SoundLimitBehavior.ReplaceOldest),
        new("ScarletCrossflowCharge", 75, 2, SoundLimitBehavior.ReplaceOldest),
        new("ScarletCrossflowRelease", 96, 2, SoundLimitBehavior.ReplaceOldest),
        new("ScarletImpact", 30, 3, SoundLimitBehavior.ReplaceOldest),
        new("ScarletStackSummon", 93, 2, SoundLimitBehavior.ReplaceOldest),
        new("ScarletStackSuccess", 96, 2, SoundLimitBehavior.ReplaceOldest),
        new("ScarletStackFail", 96, 2, SoundLimitBehavior.ReplaceOldest),
        new("ScarletSpreadSummon", 90, 2, SoundLimitBehavior.ReplaceOldest),
        new("ScarletSpreadSuccess", 96, 2, SoundLimitBehavior.ReplaceOldest),
        new("ScarletSpreadFail", 27, 2, SoundLimitBehavior.ReplaceOldest),
        new("ScarletActChange", 144, 1, SoundLimitBehavior.IgnoreNew),
        new("ScarletSacrifice", 132, 1, SoundLimitBehavior.IgnoreNew),
        new("ScarletVictory", 144, 1, SoundLimitBehavior.IgnoreNew),
        new("ScarletDown", 78, 3, SoundLimitBehavior.IgnoreNew),
        new("ScarletRevive", 96, 3, SoundLimitBehavior.IgnoreNew),
        new("ScarletReady", 66, 2, SoundLimitBehavior.IgnoreNew),
    };
    // Constructing a SoundStyle loads nothing; these systems never load on a dedicated server.
    private static readonly SoundStyle[] Styles = Array.ConvertAll(Specs, s => new SoundStyle(Root + s.File)
    {
        Volume = Gain, MaxInstances = s.Instances, SoundLimitBehavior = s.Limit,
        PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused
    });
    internal static ref readonly SoundStyle Style(ScarletCue cue) => ref Styles[(int)cue];
    internal static int Lease(ScarletCue cue) => Specs[(int)cue].Ticks + LeaseMargin;
}

// One presentation system's voices. Leases run on the game update clock, so a voice
// keeps its owner after the Fight's actor is gone: Release fades it out, and only
// world/mod teardown (when no later update will run) stops it outright.
internal sealed class ScarletVoices
{
    private readonly List<(SlotId Id, uint Until, int Fade)> voices = new();
    private readonly int capacity;
    internal ScarletVoices(int capacity) => this.capacity = capacity;
    private static uint Now => Main.GameUpdateCount;
    internal void Play(ScarletCue cue) => Play(ScarletSounds.Style(cue), ScarletSounds.Lease(cue), ScarletSounds.FadeTicks);
    internal void Play(in SoundStyle style, int leaseTicks, int fadeTicks)
    {
        if (Main.dedServ || voices.Count >= capacity || leaseTicks <= 0) return;
        voices.Add((SoundEngine.PlaySound(style), Now + (uint)leaseTicks, Math.Max(1, fadeTicks)));
    }
    internal void Update()
    {
        uint now = Now;
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            var v = voices[i];
            if (!SoundEngine.TryGetActiveSound(v.Id, out var sound)) { voices.RemoveAt(i); continue; }
            if (now >= v.Until) { sound.Stop(); voices.RemoveAt(i); }
            else sound.Volume = Math.Min(sound.Volume, Math.Clamp((v.Until - now) / (float)v.Fade, 0, 1));
        }
    }
    // Every live voice fades out over FadeTicks and is then stopped. Idempotent per tick.
    internal void Release()
    {
        uint end = Now + ScarletSounds.FadeTicks;
        for (int i = 0; i < voices.Count; i++)
            if (voices[i].Until > end) voices[i] = (voices[i].Id, end, ScarletSounds.FadeTicks);
    }
    internal void Stop()
    {
        foreach (var v in voices) if (SoundEngine.TryGetActiveSound(v.Id, out var sound)) sound.Stop();
        voices.Clear();
    }
}
