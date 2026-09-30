using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;

namespace Convergence.Client.Weapons;

// Called only by the visual owner's accepted release and native hit events.
// SoundStyle is created inside the guarded method: dedicated servers never
// construct or load a presentation asset.
internal static class DXOboroAudio
{
    private const string Root = "Convergence/Assets/Sounds/Weapons/Soboro/";

    internal static void Swing(int step, Vector2 at)
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused || !Main.hasFocus)
            return;

        string name = step switch
        {
            0 => "CutDown",
            1 => "CutReverse",
            2 => "CutHeavy",
            _ => "CutDown",
        };
        float volume = step == 2 ? .86f : step == 1 ? .68f : .72f;
        SoundEngine.PlaySound(new SoundStyle(Root + name)
        {
            Volume = volume,
            PitchVariance = .06f,
            MaxInstances = 2,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused,
            PlayOnlyIfFocused = true,
        }, at);
    }

    internal static void Hit(Vector2 at, bool heavy, bool metallic)
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused || !Main.hasFocus)
            return;

        SoundEngine.PlaySound(new SoundStyle(Root + (metallic ? "HitMetal" : "HitOrganic"))
        {
            Volume = heavy ? .62f : .52f,
            Pitch = heavy ? -.09f : 0f,
            PitchVariance = .1f,
            MaxInstances = 2,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused,
            PlayOnlyIfFocused = true,
        }, at);
    }
}
