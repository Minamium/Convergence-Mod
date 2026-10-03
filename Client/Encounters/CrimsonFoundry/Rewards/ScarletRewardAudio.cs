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
// - Every cue plays at its role's offset against Graceful Ordeal and the Raid's sound set (ScarletRewardCues.RoleDecibels);
//   the call sites never pass a literal volume.
// - Build tolls are heard by their owner only. Every other cue plays for its owner at its role's level and for other
//   players CrimsonRewardRules.RemoteCueDecibels lower, positional, so a crowd of other players' weapons stays under the
//   Raid's warnings and the local player's own weapon (REWARDS.md#levels-against-the-raid, the four-player dense mix).
// - Every call names the cue's owner, and other players' voices are a pool of their own (a ":peer" Identifier), so
//   another player's cue never cuts one of the local player's: the local player holds the table's Voices (one owner's
//   budget, replace oldest); other players together share ScarletCue.PeerVoices (one voice of a per-shot file, replace
//   oldest; one owner's Voices of any other file, ignore new, so a ringing windup or finale is never cut).
// - A cue whose file is missing from the package is skipped (HasAsset, cached), never thrown.
internal static class ScarletRewardAudio
{
    private const string Identity = "Convergence:ScarletReward:";
    private static readonly Dictionary<string, ScarletCue> table = Index();
    private static readonly Dictionary<string, bool> present = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SoundStyle> own = new(StringComparer.Ordinal), peer = new(StringComparer.Ordinal);

    private static bool Audible => !Main.dedServ && !Main.gameMenu && !Main.gamePaused && Main.hasFocus;

    // Positional for everyone: windups, releases, finales and the reliquary.
    internal static void Play(string cue, int owner, Vector2 at, float decibels = 0) => Emit(table[cue], 0, at, decibels, Remote(owner));

    // Build tolls: Toll(step) for the owner only.
    internal static void BuildToll(int step, int owner, Vector2 at)
    {
        if (!Remote(owner)) Emit(table[ScarletRewardCues.Toll(step)], 0, at, 0, false);
    }

    // The Sealed Score's playback: the toll chosen by height, positional for everyone because it is part of the release.
    internal static void Toll(int step, int owner, Vector2 at) => Emit(table[ScarletRewardCues.Toll(step)], 0, at, 0, Remote(owner));

    // Per-swing and per-shot cues: other players share one voice of the file (ScarletCue.PeerVoices).
    internal static void Shot(string cue, int owner, Vector2 at, float decibels = 0) => Emit(table[cue], 0, at, decibels, Remote(owner));

    // Each organ pipe chiffs with its own recording (OrganShot1..OrganShot4).
    internal static void OrganShot(int pipe, int owner, Vector2 at) => Emit(table[ScarletRewardCues.OrganShot], pipe, at, 0, Remote(owner));

    private static bool Remote(int owner) => owner != Main.myPlayer;

    private static void Emit(in ScarletCue cue, int variant, Vector2 at, float decibels, bool remote)
    {
        if (!Audible) return;
        string file = cue.File(variant);
        if (!Exists(file)) return;
        SoundStyle style = Style(cue, file, remote);
        float db = ScarletRewardCues.RoleDecibels(cue.Role) + decibels + (remote ? CrimsonRewardRules.RemoteCueDecibels : 0);
        style.Volume = Math.Clamp(ScarletRewardCues.Gain * ScarletRewardCues.Decibels(db), 0f, 1f);
        SoundEngine.PlaySound(style, at);
    }

    private static SoundStyle Style(in ScarletCue cue, string file, bool remote)
    {
        var styles = remote ? peer : own;
        if (!styles.TryGetValue(file, out SoundStyle style))
            styles[file] = style = new SoundStyle(ScarletRewardCues.Root + file)
            {
                Identifier = Identity + file + (remote ? ":peer" : ""),
                MaxInstances = remote ? cue.PeerVoices : cue.Voices,
                SoundLimitBehavior = !remote || cue.PeerReplacesOldest ? SoundLimitBehavior.ReplaceOldest : SoundLimitBehavior.IgnoreNew,
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
