using System;
using System.Collections.Generic;
using System.Numerics;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.EbonManor;

// The severing web's strands: wall-to-wall chords on two different sides,
// deterministic per call seed. A pending gathering circle (plus the strand's
// own radius and a margin) is never crossed, so a Stack call stays satisfiable.
internal static class EbonWeb
{
    internal const float MinimumLength = 700, EdgeInset = .08f;

    internal static uint Mix(uint x)
    {
        unchecked { x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16; }
        return x;
    }
    internal static float Unit(uint h) => (h & 0xFFFFFF) / 16777216f;
    internal static uint Seed(Guid fight, int beat) => Mix(unchecked((uint)fight.GetHashCode() ^ (uint)beat * 2654435761u));

    internal static Vector2 Edge(RaidFieldGeometry f, int side, float u)
    {
        u = EdgeInset + (1 - 2 * EdgeInset) * u;
        return side switch
        {
            0 => new(f.Left + (f.Right - f.Left) * u, f.Top),
            1 => new(f.Right, f.Top + (f.Bottom - f.Top) * u),
            2 => new(f.Left + (f.Right - f.Left) * u, f.Bottom),
            _ => new(f.Left, f.Top + (f.Bottom - f.Top) * u),
        };
    }

    internal static float Distance(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float t = Math.Clamp(Vector2.Dot(p - a, ab) / Math.Max(1e-3f, ab.LengthSquared()), 0, 1);
        return Vector2.Distance(p, a + ab * t);
    }

    internal static List<(Vector2 A, Vector2 B)> Strands(RaidFieldGeometry f, uint seed, int count, Vector2? gather)
    {
        var strands = new List<(Vector2 A, Vector2 B)>(count);
        for (int attempt = 0; strands.Count < count && attempt < count * 8; attempt++)
        {
            uint h = Mix(seed + (uint)attempt * 40503u);
            int sideA = (int)(h % 4), sideB = (sideA + 1 + (int)(h / 4 % 3)) % 4;
            var a = Edge(f, sideA, Unit(Mix(h ^ 0x9E3779B9u)));
            var b = Edge(f, sideB, Unit(Mix(h ^ 0x85EBCA6Bu)));
            if (Vector2.Distance(a, b) < MinimumLength) continue;
            if (gather is { } g && Distance(g, a, b) < EbonStitchRules.StackRadius + EbonRules.WebRadius + EbonRules.WebMargin) continue;
            strands.Add((a, b));
        }
        return strands;
    }
}
