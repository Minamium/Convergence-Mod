using System;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Accepted phrase/note determine the entire lattice on every peer. No local RNG.
internal static class CrimsonSpatialCuts
{
    internal const int LiveTicks = 12, ResidueTicks = 20;
    internal const float Radius = 7, Spacing = 256, Shift = 64;
    internal static float Reach(float release) => CrimsonInvocation.Ease(release / 2);
    internal static float Width(float release, float duration)
        => Reach(release) * (1 - CrimsonInvocation.Ease((release - (duration - 8)) / 8));
    internal static int WriteGrid(in CrimsonGesturePlan p, float age, Span<CrimsonStroke> output, bool forecast)
    {
        if (!forecast && !p.Live(age)) return 0;
        var f = p.Field;
        float offset = ((p.Phrase % 4) * 32 + p.Pulse * Shift) % Spacing;
        float reach = forecast ? 1 : Reach(age - p.Fire);
        float radius = Radius * (forecast ? 1 : Width(age - p.Fire, p.End - p.Fire));
        if (radius <= 0) return 0;
        int count = 0;
        // Alternate travel direction without changing the already shown warning.
        bool reverse = (p.Pulse & 1) != 0;
        for (float x = f.Left + offset; x <= f.Right; x += Spacing)
            Add(output, ref count, new(x, reverse ? f.Bottom : f.Top), new(x, reverse ? f.Top : f.Bottom), reach, radius);
        for (float y = f.Top + offset; y <= f.Bottom; y += Spacing)
            Add(output, ref count, new(reverse ? f.Left : f.Right, y), new(reverse ? f.Right : f.Left, y), reach, radius);
        return count;
    }
    private static void Add(Span<CrimsonStroke> output, ref int count, CrimsonPoint a, CrimsonPoint b, float reach, float radius)
    {
        if (count >= output.Length) throw new ArgumentException("crimson.stroke_capacity");
        output[count++] = new(a, CrimsonPoint.Lerp(a, b, reach), radius);
    }
}
