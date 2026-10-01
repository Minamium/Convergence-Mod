using System.Collections.Generic;
using Convergence.Content.Encounters.EbonManor.Rewards;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// Ebon reward weapon cues (docs/encounters/ebon-manor/REWARDS.md#art-and-audio), rendered by
// tools/generate_ebon_reward_sfx.py. SoundStyle is built inside the guard so dedicated servers
// never touch audio; a cue whose file is not packaged yet is skipped instead of throwing.
internal static class EbonRewardAudio
{
    private const string Root = "Convergence/Assets/Sounds/Weapons/EbonRewards/";
    private static readonly Dictionary<string, bool> present = new();

    internal static void Play(string cue, Vector2 at, float volume = .7f, float pitch = 0, float variance = .05f, int instances = 3)
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused || !Main.hasFocus || !Exists(cue))
            return;
        bool reduced = ModContent.GetInstance<EbonVisualConfig>().ReducedEffects;
        SoundEngine.PlaySound(new SoundStyle(Root + cue)
        {
            Volume = volume * (reduced ? .8f : 1),
            Pitch = pitch,
            PitchVariance = variance,
            MaxInstances = instances,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused,
            PlayOnlyIfFocused = true,
        }, at);
    }

    // One tuned silk pluck of the B-minor arpeggio (Note0..Note7); `step` wraps.
    internal static void Note(int step, Vector2 at, float volume = .55f)
        => Play("Note" + EbonRewardRules.Note(step), at, volume, 0, 0, 4);

    private static bool Exists(string cue)
    {
        if (!present.TryGetValue(cue, out bool ok))
            present[cue] = ok = ModContent.HasAsset(Root + cue);
        return ok;
    }

    internal static void Reset() => present.Clear();
}
