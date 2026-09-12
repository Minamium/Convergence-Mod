using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// One pure geometry envelope for authority collision and fractional client art.
// The wire carries the final locked ray, never a client's animation state.
internal static class FirstSeveranceBeamIgnition
{
    internal const int TravelTicks = 3, FullWidthTicks = 7;
    internal const float PilotHalfWidth = 1.5f;

    internal static float LengthFactor(double age)
    {
        float t = Math.Clamp((float)(age / TravelTicks), 0, 1);
        return 1 - (1 - t) * (1 - t) * (1 - t);
    }

    internal static float WidthFactor(double age)
    {
        float t = Math.Clamp((float)((age - 1.5) / (FullWidthTicks - 1.5)), 0, 1);
        return t * t * t * (10 + t * (-15 + 6 * t));
    }

    internal static FirstSeveranceLanceRay At(in FirstSeveranceLanceRay full, double age)
    {
        float pilot = Math.Min(PilotHalfWidth, full.HalfWidth);
        return full with { Length = full.Length * LengthFactor(age),
            HalfWidth = pilot + (full.HalfWidth - pilot) * WidthFactor(age) };
    }
}
