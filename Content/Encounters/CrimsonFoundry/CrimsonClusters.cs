using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal readonly record struct CrimsonClusterRay(CrimsonPoint Start, CrimsonPoint Direction, float Length, float Radius, float Speed);

// One server-issued note owns five bouquets, each with one large, two medium
// and two small carriers. No child RNG, retargeting or additional hit authority.
internal static class CrimsonClusters
{
    internal const int Count = 25, FlightTicks = 96;
    internal const float OrbRadius = 190;
    internal static CrimsonPoint Emitter(RaidFieldGeometry field) => new(field.CenterX, field.CenterY - 250);
    internal static CrimsonClusterRay Ray(in CrimsonGesturePlan p, int index)
    {
        if (index is < 0 or >= Count) throw new ArgumentOutOfRangeException(nameof(index));
        int petal = index % 5, group = index / 5;
        float radius = petal == 0 ? 58 : petal < 3 ? 32 : 16;
        float speed = petal == 0 ? 18 : petal < 3 ? 25 : 33;
        float offset = petal switch { 1 => -.16f, 2 => .16f, 3 => -.34f, 4 => .34f, _ => 0 };
        float angle = group * MathF.Tau / 5 + (p.Phrase % 11) * .137f + offset;
        var direction = CrimsonPoint.Polar(angle, 1);
        var start = Emitter(p.Field) + direction * (OrbRadius + radius * .25f);
        var f = p.Field;
        float x = Math.Abs(direction.X) < .0001f ? float.MaxValue
            : ((direction.X > 0 ? f.Right - radius : f.Left + radius) - start.X) / direction.X;
        float y = Math.Abs(direction.Y) < .0001f ? float.MaxValue
            : ((direction.Y > 0 ? f.Bottom - radius : f.Top + radius) - start.Y) / direction.Y;
        return new(start, direction, Math.Max(1, Math.Min(x, y)), radius, speed);
    }
    internal static float Distance(float elapsed, float speed)
    {
        float t = Math.Max(0, elapsed);
        return speed * (t - 3 * (1 - MathF.Exp(-t / 3))); // connected acceleration, then high-speed flight
    }
    internal static CrimsonStroke Carrier(in CrimsonGesturePlan p, int index, float age)
    {
        var r = Ray(p, index);
        float distance = Distance(age - p.Fire, r.Speed);
        float opening = CrimsonInvocation.Ease((age - p.Fire) / 4)
            * CrimsonInvocation.Ease((p.End - age) / 8)
            * CrimsonInvocation.Ease((r.Length - distance) / (r.Speed * 6));
        if (!p.Live(age) || distance >= r.Length || opening <= 0) return new(r.Start, r.Start, 0);
        var previous = r.Start + r.Direction * Distance(Math.Max(0, age - p.Fire - 1), r.Speed);
        return new(previous, r.Start + r.Direction * distance, r.Radius * opening);
    }
    internal static int Write(in CrimsonGesturePlan p, float age, Span<CrimsonStroke> output, bool forecast)
    {
        if (output.Length < Count) throw new ArgumentException("crimson.cluster_capacity");
        int count = 0;
        for (int i = 0; i < Count; i++)
        {
            var r = Ray(p, i);
            var s = forecast ? new CrimsonStroke(r.Start, r.Start + r.Direction * r.Length, r.Radius) : Carrier(p, i, age);
            if (s.Radius > 0) output[count++] = s;
        }
        return count;
    }
}
