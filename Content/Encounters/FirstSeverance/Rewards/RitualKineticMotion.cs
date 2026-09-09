using System;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Reusable micro-beat curves. RitualGrandScore owns current macro timelines.
internal static class RitualKineticMotion
{
    internal static float Clamp(float t) => Math.Clamp(t, 0, 1);
    internal static float Arrive(float t) => 1 - MathF.Pow(1 - Clamp(t), 4);
    internal static float Strike(float t) => MathF.Pow(Clamp(t), 4);
    internal static float Settle(float t) => NullCantorClawMotion.Smooth(t);
    internal static float Recoil(float age) => age < 0 ? 0 : Arrive(age / 1.5f) * (1 - Settle((age - 1.5f) / 10));
    internal static float VerdictClosure(float age)
        => Strike((age - 23) / (RitualArmamentChoreography.VerdictHit - 23));
    internal static float VerdictRadius(float age)
        => 265 + (1 - Arrive(age / 5)) * 170 - VerdictClosure(age) * (265 - RitualArmamentChoreography.VerdictRadius);
    internal static float ChoirFlare(float phase)
        => phase < 88 ? 0 : Arrive((phase - 88) / 1.5f) * (1 - Settle((phase - 90) / 15));
}
