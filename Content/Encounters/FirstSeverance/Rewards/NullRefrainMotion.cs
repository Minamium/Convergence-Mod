using System;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Shared pose/damage window. Rendering never changes reach or attack timing.
internal static class NullRefrainMotion
{
    internal static int Duration(int combo, float speed) =>
        Math.Clamp((int)MathF.Round((combo == 2 ? 36 : 24) / Math.Clamp(speed, .25f, 4f)), 10, 90);
    internal static float Reach(int combo) => combo == 2 ? 330 : 250;
    internal static bool Live(float progress) => progress >= .24f && progress < .76f;
    internal static float Angle(int combo, int direction, float aim, float progress)
    {
        float p = Math.Clamp(progress, 0, 1);
        float stroke = Math.Clamp((p - .24f) / .52f, 0, 1);
        stroke = stroke * stroke * (3 - 2 * stroke);
        float sign = direction * (combo == 1 ? -1 : 1);
        float arc = combo == 2 ? 4.8f : 3.6f;
        float anticipation = p < .24f ? MathF.Sin(p / .24f * MathF.PI) * .12f : 0;
        return aim + sign * (arc * (stroke - .5f) - anticipation);
    }
}
