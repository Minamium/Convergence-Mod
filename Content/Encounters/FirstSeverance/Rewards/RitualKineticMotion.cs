using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Named beats shared by the damaging beam and its material/voice presentation.
internal static class RitualKineticMotion
{
    internal const int MagicFire = 16, MagicEndHit = 22, MagicDuration = 34;
    internal const float MagicLength = 1800;
    internal static float Clamp(float t) => Math.Clamp(t, 0, 1);
    internal static float Arrive(float t) => 1 - MathF.Pow(1 - Clamp(t), 4);
    internal static float Strike(float t) => MathF.Pow(Clamp(t), 4);
    internal static float Settle(float t) => NullCantorClawMotion.Smooth(t);
    internal static float Recoil(float age) => age < 0 ? 0 : Arrive(age / 1.5f) * (1 - Settle((age - 1.5f) / 10));
    internal static float SigilArrival(float age, int ordinal) => Arrive((age - ordinal * 2) / 3);
    internal static float MagicCharge(float age) => Settle((age - 10) / (MagicFire - 10));
    internal static bool MagicLive(float age) => age >= MagicFire && age < MagicEndHit;
    internal static float MagicWidth(bool empowered) => empowered ? 112 : 72;
    internal static float MagicReach(float age) => MagicLength * Arrive((age - MagicFire) / 2);
    internal static float MagicFade(float age) => 1 - Settle((age - MagicEndHit) / (MagicDuration - MagicEndHit));
    internal static float MagicShare(int cast) => 3 * RitualArmamentChoreography.MagicRayShare(cast);
    internal static Vector2 SigilOffset(float age, int ordinal)
    {
        float arrival = SigilArrival(age, ordinal), charge = MagicCharge(age);
        float angle = ordinal * MathF.Tau / 5 - MathF.PI / 2;
        float radius = 210 + (1 - arrival) * 125 - charge * 38;
        return new(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * .72f - 28);
    }
    internal static float VerdictClosure(float age)
        => Strike((age - 23) / (RitualArmamentChoreography.VerdictHit - 23));
    internal static float VerdictRadius(float age)
        => 265 + (1 - Arrive(age / 5)) * 170 - VerdictClosure(age) * (265 - RitualArmamentChoreography.VerdictRadius);
    internal static float ChoirFlare(float phase)
        => phase < 88 ? 0 : Arrive((phase - 88) / 1.5f) * (1 - Settle((phase - 90) / 15));
}
