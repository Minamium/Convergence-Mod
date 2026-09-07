using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceSafeMechanic { Stack, Spread }
internal readonly record struct FirstSeveranceSafeWindow(FirstSeveranceSafeMechanic Kind,
    ulong StartTick, ulong ResolveTick, float X, float Y);

// Derived from the accepted action clock; no client assignment or extra packet.
// The same sanctuaries cut the actual grid and its rendered segments.
internal static class FirstSeveranceSafeWindows
{
    internal static FirstSeveranceSafeWindow? At(FirstSeveranceSubstate state, int step,
        ulong started, ulong tick, float x, float groundY)
    {
        if (tick < started) return null;
        ulong age = tick - started;
        if (state == FirstSeveranceSubstate.Lattice)
        {
            if (age >= 180 && age < 344) return new(FirstSeveranceSafeMechanic.Spread, started + 180, started + 336, x, groundY - 560);
        }
        if (state == FirstSeveranceSubstate.RemoteClaws && age < 600)
        {
            int pulse = (int)age / FirstSeveranceScoreGeometry.FloodInterval;
            ulong begin = started + (ulong)(pulse * FirstSeveranceScoreGeometry.FloodInterval);
            return new(pulse % 2 == 0 ? FirstSeveranceSafeMechanic.Stack : FirstSeveranceSafeMechanic.Spread,
                begin, begin + 170, x, groundY - 560 + FirstSeveranceScoreGeometry.FloodSafeY(step, pulse));
        }
        return null;
    }

    internal static byte GridPattern(uint serial, ulong actionAge)
    {
        int index = actionAge < 60 ? -1 : (int)((actionAge - 60) / FirstSeveranceGridVolley.CadenceTicks);
        return (byte)(((serial - 1) % 4) | (index == 2 ? 8u : 0u));
    }

    internal static IReadOnlyList<(float X, float Y, float Half)> Pockets(byte pattern, float x, float groundY)
    {
        if ((pattern & 12) == 8) return new[]
        {
            (x - 820, groundY - 960, 64f), (x + 820, groundY - 960, 64f),
            (x - 820, groundY - 160, 64f), (x + 820, groundY - 160, 64f)
        };
        return Array.Empty<(float, float, float)>();
    }

    internal static List<FirstSeveranceLanceRay> CutGrid(List<FirstSeveranceLanceRay> rays,
        byte pattern, float x, float groundY)
    {
        foreach (var p in Pockets(pattern, x, groundY))
        {
            var clipped = new List<FirstSeveranceLanceRay>(rays.Count + 12);
            foreach (var ray in rays)
            {
                bool vertical = ray.DirectionY == 1;
                float across = vertical ? ray.X : ray.Y;
                float center = vertical ? p.X : p.Y;
                if (Math.Abs(across - center) > p.Half + ray.HalfWidth) { clipped.Add(ray); continue; }
                float along = vertical ? ray.Y : ray.X;
                float target = vertical ? p.Y : p.X;
                float low = Math.Clamp(target - p.Half - ray.HalfWidth - along, 0, ray.Length);
                float high = Math.Clamp(target + p.Half + ray.HalfWidth - along, 0, ray.Length);
                if (low > .01f) clipped.Add(ray with { Length = low });
                if (high < ray.Length - .01f)
                    clipped.Add(ray with { X = ray.X + ray.DirectionX * high,
                        Y = ray.Y + ray.DirectionY * high, Length = ray.Length - high });
            }
            rays = clipped;
        }
        return rays;
    }
}
