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
        uint sample = Sample(seed, step, pulse);
        int category = (int)(sample % 3);
        return category < 2 ? category : 2 + (int)((sample >> 8) & 1);
    }
    internal static IReadOnlyList<FirstSeveranceLanceRay> Rays(ulong seed, int step, int pulse, float cx, float cy)
    {
        var rays = new List<FirstSeveranceLanceRay>(32);
        int axis = Axis(seed, step, pulse);
        float diagonal = MathF.Sqrt(.5f);
        float dx = axis == 0 ? 0 : axis == 1 ? 1 : diagonal;
        float dy = axis == 0 ? 1 : axis == 1 ? 0 : axis == 2 ? diagonal : -diagonal;
        float nx = -dy, ny = dx;
        float offset = (Sample(seed, step, pulse) >> 10) % FirstSeveranceScoreGeometry.SlicerPitch - FirstSeveranceScoreGeometry.SlicerPitch * .5f;
        for (int i = -20; i <= 20; i++)
        {
            float x = nx * (i * FirstSeveranceScoreGeometry.SlicerPitch + offset);
            float y = ny * (i * FirstSeveranceScoreGeometry.SlicerPitch + offset);
            float lo = -4000, hi = 4000;
            if (!Clip(x, dx, 1280, ref lo, ref hi) || !Clip(y, dy, 560, ref lo, ref hi) || hi - lo < 32) continue;
            rays.Add(new(cx + x + dx * lo, cy + y + dy * lo, dx, dy, hi - lo, FirstSeveranceGridVolley.HalfWidth));
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
