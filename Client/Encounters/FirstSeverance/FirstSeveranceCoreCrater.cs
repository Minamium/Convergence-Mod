using System;

namespace Convergence.Client.Encounters.FirstSeverance;

// Dimensionless physical surface profile. The same continuous depression drives
// mesh displacement, the wall normal and occlusion; no animated black decal.
internal static class FirstSeveranceCoreCrater
{
    internal readonly record struct Surface(float Depth, float AlongSlope, float AcrossSlope, float Interior);

    internal static Surface Sample(float along, float across, float opening)
    {
        opening = Math.Clamp(opening, 0, 1);
        if (opening <= 0) return default;
        float a = .08f + .58f * MathF.Sqrt(opening), b = .035f + .41f * MathF.Sqrt(opening);
        float q = MathF.Sqrt(along * along / (a * a) + across * across / (b * b));
        if (q >= 1.35f) return default;
        float t = Math.Clamp((q - .48f) / .62f, 0, 1);
        float interior = 1 - t * t * (3 - 2 * t);
        float wallSlope = t is > 0 and < 1 ? 6 * t * (1 - t) / .62f : 0;
        // Lip emerges from the intact surface, not an outline at a fixed radius.
        float lipT = Math.Clamp((q - .80f) / .55f, 0, 1);
        float lip = MathF.Sin(lipT * MathF.PI);
        float lipSlope = q is > .80f and < 1.35f
            ? MathF.Sin(2 * lipT * MathF.PI) * MathF.PI / .55f : 0;
        float depth = opening * (-.64f * interior + .10f * lip * lip);
        float slope = opening * (.64f * wallSlope + .10f * lipSlope);
        float inverse = q > .00001f ? 1 / q : 0;
        return new(depth, slope * along / (a * a) * inverse,
            slope * across / (b * b) * inverse, interior * opening);
    }
}
