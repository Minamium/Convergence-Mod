#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;

namespace Convergence.Client.Encounters.AzureCathedral;

internal enum AzureCue : byte
{
    PrisonBreak, SwordLight, RiftOpen, WormArrival,
    LioraFall, WormRetreat, DevourRush, DevourBite, FuryAwaken, FinalBlow, MeltRush, MeltContact, ChainMelt, Victory, Defeat,
    FanCharge, FanRelease, RainCharge, RainRelease, BeamCharge, BeamFire, BeamSweep, CutCharge, CutRelease,
    LatticeVolley, LatticeSlice, LatticeEnd, LioraHit,
    RushWarn, RushPass, MissileVolley, WormHit,
    StackCall, SpreadCall, ChorusTick, StackHold, StackShatter, SpreadFade, SpreadPierce,
    Downed, Revived,
    Count,
}

// Reduced Effects keeps every danger signal at full level and only lowers staging / decoration.
internal enum AzureWeight : byte { Hazard, Cinematic, Detail }

// Who may silence a voice when its source stops existing (Liora falls, the worm retreats, the Fight ends).
[Flags]
internal enum AzureOwner : byte { None = 0, Liora = 1, Worm = 2 }

// Path is relative to Assets/Sounds. Variants>0: path1..pathN chosen at random by tModLoader.
// Split>0: separate sound files path1..pathN that the caller selects explicitly (ordered cues).
internal readonly record struct AzureSpec(string Path, int Variants, int Split, float Volume, float PitchVariance,
    int Max, AzureWeight Weight, AzureOwner Owner, float Reach, int MinGap, int Late);

internal static class AzureAudio
{
    private const string Root = "Convergence/Assets/Sounds/";
    private const AzureWeight Hazard = AzureWeight.Hazard, Cinematic = AzureWeight.Cinematic, Detail = AzureWeight.Detail;
    private const AzureOwner None = AzureOwner.None, Liora = AzureOwner.Liora, Worm = AzureOwner.Worm;
    internal static readonly AzureSpec[] Specs = new AzureSpec[(int)AzureCue.Count];
    private static readonly SoundStyle[][] styles = new SoundStyle[(int)AzureCue.Count][];

    // Constructing a SoundStyle loads nothing, so the table is safe to build once per process.
    // IgnoreNew never cuts a playing voice (ReplaceOldest stops all matching voices without a ramp: a click).
    static AzureAudio()
    {
        //  cue                          path                             var spl  vol   pvar  max weight    owner  reach gap late
        Add(AzureCue.PrisonBreak,   "AzureCathedral/PrisonBreak",   0, 0, 1.00f, 0f,   2, Cinematic, None,  600, 0, 20);
        Add(AzureCue.SwordLight,    "AzureCathedral/SwordLight",    0, 0, 1.00f, 0f,   2, Cinematic, None,  600, 0, 20);
        Add(AzureCue.RiftOpen,      "AzureCathedral/RiftOpen",      0, 0, 0.90f, 0f,   2, Cinematic, None,  900, 0, 20);
        Add(AzureCue.WormArrival,   "AzureCathedral/WormArrival",   0, 0, 1.00f, 0f,   2, Cinematic, None,  900, 0, 20);
        Add(AzureCue.LioraFall,     "AzureCathedral/LioraFall",     0, 0, 1.00f, 0f,   2, Cinematic, None,  600, 0, 0);
        Add(AzureCue.WormRetreat,   "AzureCathedral/WormRetreat",   0, 0, 1.00f, 0f,   2, Cinematic, None,  900, 0, 30);
        Add(AzureCue.DevourRush,    "AzureCathedral/DevourRush",    0, 0, 1.00f, 0f,   2, Cinematic, None,  900, 0, 20);
        Add(AzureCue.DevourBite,    "AzureCathedral/DevourBite",    0, 0, 1.00f, 0f,   2, Cinematic, None,  600, 0, 12);
        Add(AzureCue.FuryAwaken,    "AzureCathedral/FuryAwaken",    0, 0, 1.00f, 0f,   2, Cinematic, None,  900, 0, 30);
        Add(AzureCue.FinalBlow,     "AzureCathedral/FinalBlow",     0, 0, 1.00f, 0f,   2, Cinematic, None,  900, 0, 30);
        Add(AzureCue.MeltRush,      "AzureCathedral/MeltRush",      0, 0, 1.00f, 0f,   2, Cinematic, None,  900, 0, 20);
        Add(AzureCue.MeltContact,   "AzureCathedral/MeltContact",   0, 0, 1.00f, 0f,   2, Cinematic, None,  600, 0, 20);
        Add(AzureCue.ChainMelt,     "AzureCathedral/ChainMelt",     0, 0, 0.80f, 0f,   2, Cinematic, None,  0,   0, 20);
        Add(AzureCue.Victory,       "AzureCathedral/Victory",       0, 0, 1.00f, 0f,   2, Cinematic, None,  0,   0, 30);
        // Defeat has no v2 cue: the Doll-shared RaidDefeat stays and is never cut off by the Fight's end.
        Add(AzureCue.Defeat,        "FirstSeverance/RaidDefeat",    0, 0, 0.45f, 0f,   2, Cinematic, None,  0,   0, 20);
        Add(AzureCue.FanCharge,     "AzureCathedral/FanCharge",     0, 0, 0.70f, 0f,   2, Hazard,    Liora, 700, 0, 10);
        Add(AzureCue.FanRelease,    "AzureCathedral/FanRelease",    3, 0, 0.90f, 0.06f, 3, Hazard,   Liora, 700, 0, 8);
        Add(AzureCue.RainCharge,    "AzureCathedral/RainCharge",    0, 0, 0.60f, 0f,   2, Hazard,    Liora, 700, 0, 10);
        Add(AzureCue.RainRelease,   "AzureCathedral/RainRelease",   3, 0, 0.90f, 0.05f, 3, Hazard,   Liora, 700, 0, 8);
        Add(AzureCue.BeamCharge,    "AzureCathedral/BeamCharge",    0, 0, 0.70f, 0f,   1, Hazard,    Liora, 700, 0, 10);
        Add(AzureCue.BeamFire,      "AzureCathedral/BeamFire",      0, 0, 1.00f, 0f,   1, Hazard,    Liora, 700, 0, 8);
        Add(AzureCue.BeamSweep,     "AzureCathedral/BeamSweep",     0, 0, 0.80f, 0f,   1, Hazard,    Liora, 800, 0, 24);
        Add(AzureCue.CutCharge,     "AzureCathedral/CutCharge",     0, 0, 0.70f, 0f,   2, Hazard,    Liora, 800, 0, 10);
        Add(AzureCue.CutRelease,    "AzureCathedral/CutRelease",    2, 0, 0.95f, 0.05f, 3, Hazard,   Liora, 800, 0, 8);
        Add(AzureCue.LatticeVolley, "AzureCathedral/LatticeVolley", 0, 0, 1.00f, 0f,   2, Hazard,    Liora, 0,   0, 8);
        // Fires every 3 ticks for 14-22 lines: a 0.2s cue, so up to 8 may overlap. Never stepped in pitch.
        Add(AzureCue.LatticeSlice,  "AzureCathedral/LatticeSlice",  4, 0, 0.80f, 0.06f, 8, Detail,   Liora, 900, 2, 6);
        Add(AzureCue.LatticeEnd,    "AzureCathedral/LatticeEnd",    0, 0, 0.90f, 0f,   2, Hazard,    Liora, 0,   0, 10);
        Add(AzureCue.LioraHit,      "AzureCathedral/LioraHit",      2, 0, 0.60f, 0.06f, 3, Hazard,   None,  800, 5, 0);
        Add(AzureCue.RushWarn,      "AzureCathedral/RushWarn",      0, 0, 0.85f, 0f,   2, Hazard,    Worm,  800, 0, 12);
        Add(AzureCue.RushPass,      "AzureCathedral/RushPass",      0, 0, 1.00f, 0f,   2, Hazard,    Worm,  700, 0, 12);
        Add(AzureCue.MissileVolley, "AzureCathedral/MissileVolley", 0, 0, 1.00f, 0f,   2, Hazard,    Worm,  0,   0, 8);
        Add(AzureCue.WormHit,       "AzureCathedral/WormHit",       3, 0, 0.60f, 0.06f, 3, Hazard,   None,  900, 5, 0);
        Add(AzureCue.StackCall,     "AzureCathedral/StackCall",     0, 0, 0.90f, 0f,   2, Hazard,    Liora, 700, 0, 12);
        Add(AzureCue.SpreadCall,    "AzureCathedral/SpreadCall",    0, 0, 0.90f, 0f,   2, Hazard,    Liora, 700, 0, 12);
        // Three separate files (ordered countdown); the caller raises the pitch 0 / +.25 / +.583 octave (A, C, E).
        Add(AzureCue.ChorusTick,    "AzureCathedral/ChorusTick",    0, 3, 0.75f, 0f,   2, Hazard,    Liora, 0,   0, 12);
        Add(AzureCue.StackHold,     "AzureCathedral/StackHold",     0, 0, 1.00f, 0f,   2, Hazard,    None,  700, 0, 0);
        Add(AzureCue.StackShatter,  "AzureCathedral/StackShatter",  0, 0, 1.00f, 0f,   2, Hazard,    None,  700, 0, 0);
        Add(AzureCue.SpreadFade,    "AzureCathedral/SpreadFade",    0, 0, 1.00f, 0f,   2, Hazard,    None,  700, 0, 0);
        Add(AzureCue.SpreadPierce,  "AzureCathedral/SpreadPierce",  0, 0, 1.00f, 0f,   2, Hazard,    None,  700, 0, 0);
        Add(AzureCue.Downed,        "AzureCathedral/Downed",        0, 0, 1.00f, 0f,   2, Hazard,    None,  900, 12, 0);
        Add(AzureCue.Revived,       "AzureCathedral/Revived",       0, 0, 1.00f, 0f,   2, Hazard,    None,  900, 12, 0);
    }

    private static void Add(AzureCue cue, string path, int variants, int split, float volume, float pitchVariance, int max,
        AzureWeight weight, AzureOwner owner, float reach, int minGap, int late)
    {
        Specs[(int)cue] = new(path, variants, split, volume, pitchVariance, max, weight, owner, reach, minGap, late);
        var list = new SoundStyle[Math.Max(1, split)];
        for (int i = 0; i < list.Length; i++)
        {
            string asset = Root + (split > 0 ? path + (i + 1) : path);
            var style = variants > 0 ? new SoundStyle(asset, variants) : new SoundStyle(asset);
            style.Volume = volume;
            style.PitchVariance = pitchVariance;
            style.MaxInstances = max;
            style.SoundLimitBehavior = SoundLimitBehavior.IgnoreNew;
            style.PauseBehavior = PauseBehavior.PauseWithGame;
            style.PlayOnlyIfFocused = true;
            list[i] = style;
        }
        styles[(int)cue] = list;
    }

    internal static SoundStyle Style(AzureCue cue, int variant = 0)
    {
        var list = styles[(int)cue];
        return list[Math.Clamp(variant, 0, list.Length - 1)];
    }

    internal static float Scale(AzureWeight weight)
        => !AzureVisuals.Reduced || weight == AzureWeight.Hazard ? 1f : .75f;

    // The listener of a positioned sound is the camera centre (ActiveSound.Update). A source farther than `reach`
    // is placed `reach` px away in the same direction: left/right is kept, and the sound stays above
    // 1 - reach/2500 of its level instead of vanishing with the 2500px falloff (or the 10000px play limit).
    internal static Vector2 Anchor(Vector2 source, float reach)
    {
        Vector2 listener = Main.Camera.Center;
        Vector2 delta = source - listener;
        float length = delta.Length();
        return length <= reach ? source : listener + delta / length * reach;
    }
}

// Tracks the voices this Mod started so they can follow a moving source, be faded, or be released.
// Ramps are written by this system every tick (ActiveSound.Volume is a multiplier of the style gain).
internal sealed class AzureVoices
{
    private sealed class Voice
    {
        internal SlotId Id;
        internal AzureOwner Owner;
        internal Func<Vector2?>? Where;
        internal float Reach;
        internal ulong FollowUntil;
        internal int FadeLeft, FadeTotal;
        internal float FadeFrom = -1; // the voice's volume when the current ramp began
    }

    private readonly List<Voice> voices = new();
    internal int Count => voices.Count;

    internal void Add(SlotId id, AzureOwner owner) => voices.Add(new Voice { Id = id, Owner = owner });

    internal void Follow(SlotId id, Func<Vector2?> where, float reach, int ticks)
    {
        foreach (var voice in voices)
            if (voice.Id.Equals(id)) { voice.Where = where; voice.Reach = reach; voice.FollowUntil = Main.GameUpdateCount + (ulong)Math.Max(0, ticks); }
    }

    internal void Fade(SlotId id, int ticks)
    {
        foreach (var voice in voices) if (voice.Id.Equals(id)) Begin(voice, ticks);
    }

    internal void FadeOwned(AzureOwner mask, int ticks)
    {
        foreach (var voice in voices) if ((voice.Owner & mask) != 0) Begin(voice, ticks);
    }

    // Natural end: ticks==0 releases every voice (no owner, no follower) so tails play out, but keeps
    // tracking them so a world change can still stop them. Otherwise ramp all voices down.
    internal void Leave(int ticks)
    {
        foreach (var voice in voices)
            if (ticks <= 0) { voice.Owner = AzureOwner.None; voice.Where = null; }
            else Begin(voice, ticks);
    }

    // Hard stop, only for a world change or unload.
    internal void StopAll()
    {
        foreach (var voice in voices) if (SoundEngine.TryGetActiveSound(voice.Id, out var sound)) sound.Stop();
        voices.Clear();
    }

    private static void Begin(Voice voice, int ticks)
    {
        ticks = Math.Max(1, ticks);
        if (voice.FadeTotal == 0 || ticks < voice.FadeLeft) { voice.FadeLeft = voice.FadeTotal = ticks; voice.FadeFrom = -1; }
    }

    internal void Tick()
    {
        ulong now = Main.GameUpdateCount;
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            var voice = voices[i];
            if (!SoundEngine.TryGetActiveSound(voice.Id, out var sound)) { voices.RemoveAt(i); continue; }
            if (voice.Where is not null)
            {
                if (now < voice.FollowUntil) { if (voice.Where() is { } at) sound.Position = AzureAudio.Anchor(at, voice.Reach); }
                else voice.Where = null;
            }
            if (voice.FadeTotal > 0)
            {
                if (voice.FadeFrom < 0) voice.FadeFrom = sound.Volume; // a shorter ramp continues from the current level
                if (--voice.FadeLeft <= 0) { sound.Stop(); voices.RemoveAt(i); continue; }
                sound.Volume = voice.FadeFrom * voice.FadeLeft / voice.FadeTotal;
            }
        }
    }
}
