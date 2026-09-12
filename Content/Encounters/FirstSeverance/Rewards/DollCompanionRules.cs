#nullable enable
using System;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Native summon rules, independent of the encounter roster and its authority tick.
internal static class DollCompanionRules
{
    internal const int Slots = 10;
    internal const int Cycle = 210;
    internal const int Verdict = 96;
    internal const int Charge = 60, Merge = 64, Tension = 78;
    internal const int BeamTicks = 72, BeamAfterglow = 12, BeamHitCadence = 12;
    internal const float BeamLength = 1900, BeamWidth = 44;
    internal const float NeedleDamageScale = .70f, BeamDamageScale = .80f;
    internal const int FrameCount = 36;
    internal const int BroomFrames = 16;
    internal const int BroomRecoveryEnd = Verdict + BeamTicks + 18;
    internal static bool CanSummon(int capacity, int owned) => capacity >= Slots && owned == 0;
    internal static bool NeedleAt(int tick) => tick is 24 or 44 or 58;
    internal static int SigilBirth(int index) => index switch { 0 => 12, 1 => 38, _ => 54 };
    internal static int NeedleTick(int index) => index switch { 0 => 24, 1 => 44, _ => 58 };
    internal static int Damage => RitualArmamentRules.Damage(RitualArmamentKind.Summon) * Slots;
    internal static bool BeamLive(float age) => age >= 0 && age < BeamTicks;
    // Shared by the actual beam's collision and its surface pass, including the
    // rapid opening and the last eight ticks' pressure loss. No invisible tail.
    internal static float BeamScale(float age) => !BeamLive(age) ? 0
        : (1 - MathF.Pow(1 - Math.Clamp((age + 1) / 5, 0, 1), 4))
            * (1 - RitualArmamentRules.Smooth((age - (BeamTicks - 8)) / 8));
    internal static float MergeAmount(float age) => MathF.Pow(Math.Clamp((age - Merge) / (Tension - Merge), 0, 1), 4);
    internal static float ChargeAmount(float age) => age < Tension ? 0
        : .18f + .82f * MathF.Pow(Math.Clamp((age - (Verdict - 8)) / 8, 0, 1), 4);
    internal static int BroomFrame(float attack, int idle)
    {
        if (attack <= 0 || attack >= BroomRecoveryEnd) return idle / 7 % 8;
        if (attack < 12) return 8;
        if (attack < Charge)
        {
            for (int i = 0; i < 3; i++)
            {
                int beat = NeedleTick(i);
                if (attack >= beat - 4 && attack < beat + 6) return attack < beat ? 10 : 11;
            }
            return 9;
        }
        if (attack < Verdict - 6) return 9; // Palm raised; stored pressure, not recovery cels.
        if (attack < Verdict) return 10;
        if (attack < Verdict + 7) return 11;
        if (attack < Verdict + BeamTicks) return (int)(attack - Verdict) % 18 < 2 ? 10 : 11;
        if (attack < Verdict + BeamTicks + 4) return 12;
        if (attack < Verdict + BeamTicks + 9) return 13;
        return attack < Verdict + BeamTicks + 13 ? 14 : 15;
    }
    internal static int Frame(float attack, bool flying, float walk, int idle)
    {
        if (attack > 0 && attack < Verdict + BeamTicks + BeamAfterglow)
        {
            if (attack < 12) return 24;
            if (attack < 22) return 25;
            if (attack < Charge)
            {
                int castBeat = (int)(attack - 22) % 20;
                return 26 + (castBeat < 3 ? 0 : castBeat < 7 ? 1 : 2);
            }
            if (attack < Tension) return 29;
            if (attack < Verdict - 8) return 30;
            if (attack < Verdict) return 31;
            if (attack < Verdict + 7) return 32;
            if (attack < Verdict + BeamTicks) return 32 + (int)(attack - Verdict) / 9 % 2;
            if (attack < Verdict + BeamTicks + 5) return 34;
            return 35;
        }
        if (flying) return 20 + idle / 9 % 4;
        if (walk >= 0) return 12 + (int)(walk / 5) % 8;
        int beat = idle % 420;
        if (beat is >= 60 and < 108) return 3 + (beat - 60) / 16;
        if (beat is >= 180 and < 198) return beat < 186 ? 1 : beat < 192 ? 2 : 1;
        if (beat is >= 260 and < 340) return 6 + Math.Min(5, (beat - 260) / 14);
        return 0;
    }
}
