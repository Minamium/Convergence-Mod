#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The 35 Scarlet reward cues (REWARDS.md#art-and-audio), under Assets/Sounds/Weapons/ScarletRewards/. Recordings
// come later (tools/generate_scarlet_reward_sfx.py); until a cue is packaged it is skipped, so nothing plays yet.
internal static class ScarletRewardCues
{
    // Shared
    internal const string ReliquaryOpen = nameof(ReliquaryOpen), Cadence = nameof(Cadence);
    internal const int Tolls = 8; // Toll0..Toll7: E-flat sus2 ladder Eb3 F3 Bb3 Eb4 F4 Bb4 Eb5 F5
    // Scythe
    internal const string ScytheSwingHigh = nameof(ScytheSwingHigh), ScytheSwingLow = nameof(ScytheSwingLow), ScytheWhipBrace = nameof(ScytheWhipBrace),
        ScytheWhip = nameof(ScytheWhip), StaffWindup = nameof(StaffWindup), StaffCut = nameof(StaffCut), StaffBarline = nameof(StaffBarline);
    // Organ
    internal const string OrganShot = nameof(OrganShot), HymnInhale = nameof(HymnInhale), HandSlam = nameof(HandSlam), ChoirClasp = nameof(ChoirClasp);
    // Baton
    internal const string BatonStroke = nameof(BatonStroke), BatonLift = nameof(BatonLift), InkIgnite = nameof(InkIgnite), RiverRelease = nameof(RiverRelease);
    // Censer
    internal const string CenserSummon = nameof(CenserSummon), CenserSwing = nameof(CenserSwing), CenserPour = nameof(CenserPour),
        CenserBrace = nameof(CenserBrace), CenserGrandPour = nameof(CenserGrandPour);
    // Quill
    internal const string QuillThrow = nameof(QuillThrow), QuillStick = nameof(QuillStick), ScoreUnseal = nameof(ScoreUnseal),
        InkBlaze = nameof(InkBlaze), ScoreChord = nameof(ScoreChord);

    internal static string Toll(int step) => "Toll" + Math.Clamp(step, 0, Tolls - 1);

    internal static readonly string[] All =
    {
        ReliquaryOpen, "Toll0", "Toll1", "Toll2", "Toll3", "Toll4", "Toll5", "Toll6", "Toll7", Cadence,
        ScytheSwingHigh, ScytheSwingLow, ScytheWhipBrace, ScytheWhip, StaffWindup, StaffCut, StaffBarline,
        OrganShot, HymnInhale, HandSlam, ChoirClasp,
        BatonStroke, BatonLift, InkIgnite, RiverRelease,
        CenserSummon, CenserSwing, CenserPour, CenserBrace, CenserGrandPour,
        QuillThrow, QuillStick, ScoreUnseal, InkBlaze, ScoreChord,
    };
}

// Plays the reward cues (REWARDS.md#multiplayer-readability, #audio). SoundStyle is built only inside the client
// guard, so a Dedicated Server never touches audio; voices are bounded (MaxInstances, replace oldest), stop while the
// game is paused and play only with focus; a cue whose file is not packaged is skipped instead of throwing.
internal static class ScarletRewardAudio
{
    private const string Root = "Convergence/Assets/Sounds/Weapons/ScarletRewards/";
    private static readonly Dictionary<string, bool> present = new();

    // Positional for everyone: windups, releases, finales, the reliquary.
    internal static void Play(string cue, Vector2 at, float volume = .7f, float pitch = 0, float variance = 0, int instances = 3)
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused || !Main.hasFocus || !Exists(cue)) return;
        SoundEngine.PlaySound(new SoundStyle(Root + cue)
        {
            Volume = volume,
            Pitch = pitch,
            PitchVariance = variance,
            MaxInstances = instances,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused,
            PlayOnlyIfFocused = true,
        }, at);
    }

    // Build steps: only the owner hears them.
    internal static void OwnerOnly(string cue, int owner, Vector2 at, float volume = .5f, int instances = 2)
    {
        if (owner == Main.myPlayer) Play(cue, at, volume, 0, 0, instances);
    }

    // Build tolls: Toll(step) for the owner only.
    internal static void BuildToll(int step, int owner, Vector2 at, float volume = .45f)
        => OwnerOnly(ScarletRewardCues.Toll(step), owner, at, volume, 4);

    // Per-swing and per-shot cues: the owner at full level, other players 8 dB lower with one voice.
    internal static void Shot(string cue, int owner, Vector2 at, float volume = .6f, float variance = .04f)
    {
        bool own = owner == Main.myPlayer;
        Play(cue, at, own ? volume : volume * CrimsonRewardRules.Decibels(CrimsonRewardRules.RemoteShotDecibels), 0, variance, own ? 3 : 1);
    }

    private static bool Exists(string cue)
    {
        if (!present.TryGetValue(cue, out bool ok))
            present[cue] = ok = ModContent.HasAsset(Root + cue);
        return ok;
    }

    internal static void Reset() => present.Clear();
}
