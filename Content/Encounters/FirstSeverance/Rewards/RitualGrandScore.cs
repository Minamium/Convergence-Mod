using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Real ticks: micro-beats live inside these long-form scores. A held trigger
// never restarts construction at the ordinary item use-time boundary.
internal static class RitualGrandScore
{
    internal const int MagicMerge = 340, MagicCharge = 362, MagicFire = 410;
    internal const int MagicHitCadence = 10, MagicManaCadence = 8;
    internal const float MagicLength = 2600, MagicWidth = 116;
    internal const int BatteryLock = 282, BatteryFire = 348;
    internal const int ChoirAssemble = 276, ChoirCharge = 324, ChoirFire = 372;
    internal const int ChoirReleaseEnd = 552, ChoirCycle = 660;
    internal const int WitnessMerge = 174, WitnessFire = 218, WitnessEnd = 282;
    internal static int SigilBirth(int index) => index switch
    { 0 => 0, 1 => 108, 2 => 183, 3 => 233, 4 => 269, 5 => 296, _ => 316 };
    internal static int BatteryBirth(int index) => index switch
    { 0 => 0, 1 => 90, 2 => 162, 3 => 213, _ => 246 };
    internal static int Count(float age, bool magic)
    {
        int count = 0;
        for (int i = 0; i < (magic ? 7 : 5); i++)
            if (age >= (magic ? SigilBirth(i) : BatteryBirth(i))) count++;
        return count;
    }
    internal static float Arrival(float age, int birth) => RitualKineticMotion.Arrive((age - birth) / 12);
    internal static float Merge(float age) => RitualKineticMotion.Strike((age - MagicMerge) / (MagicCharge - MagicMerge));
    internal static float Charge(float age) => RitualKineticMotion.Strike((age - MagicCharge) / (MagicFire - MagicCharge));
    internal static float BeamOpening(float age, int fire) => RitualKineticMotion.Arrive((age - fire) / 7);
    internal static Vector2 SigilOffset(int index, int facing)
    {
        if (index == 0) return new Vector2(facing * 96, -32);
        int rank = (index + 1) / 2;
        return new Vector2(facing * (-48 - rank * 44), (index % 2 == 1 ? -1 : 1) * (85 + rank * 74));
    }
    internal static Vector2 BatteryOffset(int index)
    {
        if (index == 0) return new Vector2(105, -4);
        int rank = (index + 1) / 2;
        return new Vector2(-80 - rank * 50, (index % 2 == 1 ? -1 : 1) * (60 + rank * 86));
    }
    internal static bool MagicBoltAt(int tick, int lane) => tick < MagicMerge
        && tick >= SigilBirth(lane) + 15 && (tick - SigilBirth(lane) - 15) % 38 == 0;
    internal static bool MagicPays(int tick) => tick > 0 && (tick < MagicFire
        ? tick % 30 == 0 : (tick - MagicFire) % MagicManaCadence == 0);
    internal static int BatteryLane(int tick)
    {
        if (tick < 12 || tick >= BatteryLock && tick < BatteryFire) return -1;
        int count = Count(tick, false), cadence = tick >= BatteryFire ? 3 : 26 - count * 3;
        return tick % cadence == 0 ? tick / cadence % count : -1;
    }
    internal static float ChoirAssembly(float age) => RitualArmamentChoreography.Smooth((age - ChoirAssemble) / 48)
        * (1 - RitualArmamentChoreography.Smooth((age - ChoirReleaseEnd) / 80));
    internal static bool ChoirNoteAt(int tick, int ordinal) => tick < ChoirAssemble
        && tick >= 18 + ordinal % 8 * 7 && (tick - 18 - ordinal % 8 * 7) % 46 == 0;
    internal static bool ChoirLive(float age) => age >= ChoirFire && age < ChoirReleaseEnd;
    internal static int WitnessCount(float age) => Math.Clamp(1 + (int)(age / 29), 1, 6);
    internal static float WitnessFold(float age) => RitualKineticMotion.Strike((age - WitnessMerge) / 20);
    internal static float WitnessStrike(float age) => RitualKineticMotion.Strike((age - WitnessFire) / 5);
}
