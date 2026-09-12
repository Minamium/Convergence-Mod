#nullable enable
using System;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Native summon rules, independent of the encounter roster and its authority tick.
internal static class DollCompanionRules
{
    internal const int Slots = 10;
    internal const int Cycle = 180;
    internal const int Verdict = 96;
    internal const int FrameCount = 36;
    internal static bool CanSummon(int capacity, int owned) => capacity >= Slots && owned == 0;
    internal static bool NeedleAt(int tick) => tick is 24 or 36 or 48;
    internal static int Damage => RitualArmamentRules.Damage(RitualArmamentKind.Summon) * Slots;
    internal static int Frame(float attack, bool flying, float walk, int idle)
    {
        if (attack > 0 && attack < 130)
        {
            if (attack < 12) return 24;
            if (attack < 22) return 25;
            if (attack < 52) return 26 + ((int)(attack - 22) % 12 < 5 ? 0 : 2);
            if (attack < 68) return 29;
            if (attack < 82) return 30;
            if (attack < Verdict) return 31;
            if (attack < 103) return 32;
            if (attack < 112) return 33;
            if (attack < 121) return 34;
            return 35;
        }
        if (flying) return 20 + idle / 9 % 4;
        if (walk >= 0) return 12 + (int)(walk / 5) % 8;
        int beat = idle % 420;
        if (beat is >= 180 and < 198) return beat < 186 ? 1 : beat < 192 ? 2 : 1;
        if (beat is >= 260 and < 340) return 6 + Math.Min(5, (beat - 260) / 14);
        return 0;
    }
}
