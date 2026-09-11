using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

// Server-owned action epoch is the shared seed, already present in snapshots.
// Explicit integer mixer, never System.Random/GetHashCode or client randomness.
internal static class FirstSeveranceRandomComb
{
    internal static uint Sample(ulong seed, int step, int pulse)
    {
        unchecked
        {
            uint x = (uint)seed ^ (uint)(seed >> 32) ^ (uint)(step * 0x9e3779b9u) ^ (uint)(pulse + 1) * 0x85ebca6bu;
            x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu;
            return x ^ (x >> 16);
        }
    }
    internal static int Axis(ulong seed, int step, int pulse)
    {
        uint sample = Sample(seed, step, 0);
        int first = (int)(sample & 3);
        // Complementary color pairs cover the entire field along two different
        // axes. Every four-color score requires movement, with broad safe bands.
        return pulse < 2 ? first : (first + 1 + (int)((sample >> 8) % 3)) % 4;
    }
    internal static IReadOnlyList<FirstSeveranceLanceRay> Rays(ulong seed, int step, int pulse, float cx, float cy)
    {
        var rays = new List<FirstSeveranceLanceRay>(40);
        int axis = Axis(seed, step, pulse);
        float diagonal = MathF.Sqrt(.5f);
        float dx = axis == 0 ? 0 : axis == 1 ? 1 : diagonal;
        float dy = axis == 0 ? 1 : axis == 1 ? 0 : axis == 2 ? diagonal : -diagonal;
        float nx = -dy, ny = dx;
        float width = FirstSeveranceLanceTuning.HalfWidth * 2;
        float offset = (Sample(seed, step, pulse / 2) >> 10) % FirstSeveranceScoreGeometry.SlicerPitch
            + (pulse % 2) * width * 2;
        for (int i = -8; i <= 8; i++)
        for (int tooth = 0; tooth < 2; tooth++)
        {
            float distance = i * FirstSeveranceScoreGeometry.SlicerPitch + offset + tooth * width;
            float x = nx * distance;
            float y = ny * distance;
            float lo = -4000, hi = 4000;
            // Pad endpoints for finite player boxes and diagonal corners. The
            // containment mask clips decoration outside; SAT uses these same rays.
            if (!Clip(x, dx, 1324, ref lo, ref hi) || !Clip(y, dy, 604, ref lo, ref hi) || hi - lo < 32) continue;
            rays.Add(new(cx + x + dx * lo, cy + y + dy * lo, dx, dy, hi - lo));
        }
        return rays;
    }
    private static bool Clip(float origin, float direction, float half, ref float lo, ref float hi)
    {
        if (MathF.Abs(direction) < .0001f) return MathF.Abs(origin) <= half;
        float a = (-half - origin) / direction, b = (half - origin) / direction;
        lo = Math.Max(lo, Math.Min(a, b)); hi = Math.Min(hi, Math.Max(a, b));
        return hi > lo;
    }
}
