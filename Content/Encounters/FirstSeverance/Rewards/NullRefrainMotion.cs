#nullable enable
using System;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

internal static class NullRefrainMotion
{
    internal static int Duration(int combo, float speed) => Math.Clamp(
        (int)MathF.Round((combo == 2 ? 32 : 22) / Math.Clamp(float.IsFinite(speed) ? speed : 1, .25f, 4)), 10, 90);
    internal static float Reach(int combo) => combo == 2 ? 405 : 310;
    internal static bool Live(float progress) => progress >= .24f && progress < .76f;
    internal static float Angle(int combo, int direction, float aim, float progress)
    {
        float p = Math.Clamp(progress, 0, 1);
        float x = Math.Clamp((p - .24f) / .52f, 0, 1);
        float stroke = x * x * x * (10 + x * (-15 + 6 * x));
        float sign = direction * (combo == 1 ? -1 : 1);
        float arc = combo == 2 ? 4.8f : 3.6f;
        // Zero velocity at windup/live/recovery boundaries, no discontinuous frame swaps.
        float anticipation = p < .24f ? MathF.Pow(MathF.Sin(p / .24f * MathF.PI), 2) * .16f : 0;
        return aim + sign * (arc * (stroke - .5f) - anticipation);
    }
}
