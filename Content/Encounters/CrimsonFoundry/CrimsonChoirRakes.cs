using System;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Three parallel, bowed claw paths. The accepted pulse rotates their entry
// edge clockwise; phrase variation shifts all three together without changing
// their corridors. Warning and live capsules sample the same fixed curves.
internal static class CrimsonChoirRakes
{
    internal const int Cuts = 3, SegmentsPerCut = 32, MaximumStrokes = Cuts * SegmentsPerCut;
    internal const float Spacing = 270, Radius = 24, ForecastRadius = 26, EdgeInset = 32;

    internal static int Write(in CrimsonGesturePlan p, float age, Span<CrimsonStroke> output, bool forecast)
    {
        if (!forecast && !p.Live(age)) return 0;
        float release = age - p.Fire, duration = p.End - p.Fire;
        float extension = forecast ? 1 : Math.Clamp(CrimsonTechniqueGeometry.Strike(p.Progress(age)), 0, 1);
        float radius = forecast ? ForecastRadius : Radius * CrimsonInvocation.Ease(release / 1.5f)
            * (1 - CrimsonInvocation.Ease((release - (duration - 4)) / 4));
        if (radius <= 0 || extension <= 0) return 0;
        if (output.Length < MaximumStrokes) throw new ArgumentException("crimson.rakes_capacity");

        var f = p.Field;
        int edge = p.Pulse & 3;
        CrimsonPoint forward = edge switch
        {
            0 => new(0, 1), 1 => new(-1, 0), 2 => new(0, -1), _ => new(1, 0)
        };
        var lateral = new CrimsonPoint(-forward.Y, forward.X);
        float distance = (edge & 1) == 0 ? f.Bottom - f.Top : f.Right - f.Left;
        distance -= 2 * EdgeInset;
        var middle = new CrimsonPoint(f.CenterX, f.CenterY);
        float shift = (p.Phrase % 3 - 1) * 24;
        int count = 0;
        for (int cut = 0; cut < Cuts; cut++)
        {
            float lane = (cut - 1) * Spacing + shift;
            var start = middle - forward * (distance * .5f) + lateral * lane;
            // Controls bend together, then the tip returns across the lane to
            // form a hook. Identical lateral displacement preserves wide gaps.
            var control1 = start + forward * (distance * .34f) + lateral * 105;
            var control2 = start + forward * (distance * .76f) + lateral * 145;
            var tip = start + forward * distance - lateral * 76;
            int segments = (int)MathF.Ceiling(SegmentsPerCut * extension);
            for (int i = 0; i < segments; i++)
            {
                float u0 = i / (float)SegmentsPerCut;
                float u1 = Math.Min(extension, (i + 1f) / SegmentsPerCut);
                if (u1 <= u0) continue;
                var a = Cubic(start, control1, control2, tip, u0);
                var b = Cubic(start, control1, control2, tip, u1);
                output[count++] = new(a, b, radius);
            }
        }
        return count;
    }

    private static CrimsonPoint Cubic(CrimsonPoint a, CrimsonPoint b, CrimsonPoint c, CrimsonPoint d, float t)
    {
        float inverse = 1 - t;
        return a * (inverse * inverse * inverse) + b * (3 * inverse * inverse * t)
            + c * (3 * inverse * t * t) + d * (t * t * t);
    }
}
