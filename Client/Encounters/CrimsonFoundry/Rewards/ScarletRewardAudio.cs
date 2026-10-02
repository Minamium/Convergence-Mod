#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// Plays the Scarlet reward cues (REWARDS.md#multiplayer-readability, #audio; the cue table is ScarletRewardCues).
// - SoundStyle is built only on a client outside the main menu, so a Dedicated Server never touches audio.
// - Voices are bounded per cue file (MaxInstances, replace oldest), stop while the game is paused and start only with
//   focus, like the Ebon reward audio. Tuned files play at their recorded pitch (no pitch variance).
// - Build tolls are heard by their owner only. A per-swing or per-shot cue plays for its owner at full level and for
//   other players RemoteShotDecibels lower, all of them sharing one voice (a separate Identifier). Everything else is
//   positional for everyone.
// - A cue whose file is missing from the package is skipped (HasAsset, cached), never thrown.
internal static class ScarletRewardAudio
{
    private const string Identity = "Convergence:ScarletReward:";
    private static readonly Dictionary<string, ScarletCue> table = Index();
    private static readonly Dictionary<string, bool> present = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SoundStyle> own = new(StringComparer.Ordinal), peer = new(StringComparer.Ordinal);

    private static bool Audible => !Main.dedServ && !Main.gameMenu && !Main.gamePaused && Main.hasFocus;

    // Positional for everyone: windups, releases, finales and the reliquary.
    internal static void Play(string cue, Vector2 at, float decibels = 0) => Emit(table[cue], 0, at, decibels, false);

    // Build tolls: Toll(step) for the owner only.
    internal static void BuildToll(int step, int owner, Vector2 at)
    {
        if (owner == Main.myPlayer) Emit(table[ScarletRewardCues.Toll(step)], 0, at, 0, false);
    }

    // The Sealed Score's playback: the toll chosen by height, positional for everyone because it is part of the release.
    internal static void Toll(int step, Vector2 at) => Emit(table[ScarletRewardCues.Toll(step)], 0, at, 0, false);

    // Per-swing and per-shot cues: the owner at full level, other players RemoteShotDecibels lower with one voice.
    internal static void Shot(string cue, int owner, Vector2 at, float decibels = 0) => Shot(table[cue], 0, owner, at, decibels);

    // Each organ pipe chiffs with its own recording (OrganShot1..OrganShot4).
    internal static void OrganShot(int pipe, int owner, Vector2 at) => Shot(table[ScarletRewardCues.OrganShot], pipe, owner, at, 0);

    private static void Shot(in ScarletCue cue, int variant, int owner, Vector2 at, float decibels)
    {
        bool remote = owner != Main.myPlayer;
        Emit(cue, variant, at, remote ? decibels + CrimsonRewardRules.RemoteShotDecibels : decibels, remote);
    }

    private static void Emit(in ScarletCue cue, int variant, Vector2 at, float decibels, bool remote)
    {
        if (!Audible) return;
        string file = cue.File(variant);
        if (!Exists(file)) return;
        SoundStyle style = Style(cue, file, remote);
        style.Volume = Math.Clamp(ScarletRewardCues.Gain * ScarletRewardCues.Decibels(decibels), 0f, 1f);
        SoundEngine.PlaySound(style, at);
    }

    private static SoundStyle Style(in ScarletCue cue, string file, bool remote)
    {
        var styles = remote ? peer : own;
        if (!styles.TryGetValue(file, out SoundStyle style))
            styles[file] = style = new SoundStyle(ScarletRewardCues.Root + file)
            {
                Identifier = Identity + file + (remote ? ":peer" : ""),
                MaxInstances = remote ? 1 : cue.Voices,
                SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                PauseBehavior = PauseBehavior.StopWhenGamePaused,
                PlayOnlyIfFocused = true,
            };
        return style;
    }

    private static bool Exists(string file)
    {
        if (!present.TryGetValue(file, out bool ok))
            present[file] = ok = ModContent.HasAsset(ScarletRewardCues.Root + file);
        return ok;
    }

    private static Dictionary<string, ScarletCue> Index()
    {
        var index = new Dictionary<string, ScarletCue>(StringComparer.Ordinal);
        foreach (ScarletCue cue in ScarletRewardCues.All) index.Add(cue.Name, cue);
        return index;
    }

    internal static void Reset()
    {
        present.Clear();
        own.Clear();
        peer.Clear();
    }
}
