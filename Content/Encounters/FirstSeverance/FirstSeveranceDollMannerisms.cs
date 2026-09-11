using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// Presentation-only native NPC frame selection. Does not move the actor, alter
// readiness, or schedule capture; pause follows the game's simulation clock.
internal static class FirstSeveranceDollMannerisms
{
    internal const int Frames = 12, CycleTicks = 1380;
    internal static int Frame(ulong tick, bool talking)
    {
        int t = (int)(tick % CycleTicks);
        if (talking) return (tick % 240) switch { < 12 => 6, < 115 => 7, < 150 => 8, < 166 => 6, _ => 0 };
        if (t is >= 241 and < 257) return Blink(t - 241);
        if (t is >= 491 and < 601) return (t - 491) switch { < 12 => 3, < 76 => 4, < 96 => 5, _ => 3 };
        if (t is >= 752 and < 862) return (t - 752) switch { < 12 => 6, < 63 => 7, < 86 => 8, < 99 => 7, _ => 6 };
        if (t is >= 1043 and < 1177) return (t - 1043) switch { < 14 => 9, < 73 => 10, < 95 => 9, < 120 => 11, _ => 5 };
        if (t is >= 1299 and < 1315) return Blink(t - 1299);
        return 0;
    }
    private static int Blink(int t) => t is >= 4 and < 11 ? 2 : 1;
}
