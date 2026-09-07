using Terraria.Audio;

namespace Convergence.Content.Encounters.FirstSeverance.Actors;

// Native NPC hit playback handles local/replicated hits. One voice per material
// prevents high-DPS multi-hit weapons from multiplying or restarting transients.
internal static class FirstSeveranceHitSounds
{
    internal static readonly SoundStyle Shell = Create("ShellHit", .64f);
    internal static readonly SoundStyle Core = Create("CoreHit", .64f);
    internal static readonly SoundStyle Pylon = Create("PylonHit", .60f);
    private static SoundStyle Create(string name, float volume) => new("Convergence/Assets/Sounds/FirstSeverance/" + name)
    {
        Volume = volume * .80f, PitchVariance = .10f, MaxInstances = 1,
        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
        PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
    };
}
