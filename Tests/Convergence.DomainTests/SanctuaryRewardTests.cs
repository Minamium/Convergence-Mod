using System;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Final slicers complete four read-ahead pursuit pulses")]
    private static void SlicerWarningExtension()
    {
        foreach (int step in new[] { 5, 11, 17, 23 })
        {
            AssertEqual(12, FirstSeveranceScoreGeometry.SlicerEnd(step) - FirstSeveranceScoreGeometry.SlicerFire(step), "pursuit live duration");
            AssertEqual(true, FirstSeveranceScoreGeometry.SlicerFire(step) - FirstSeveranceScoreGeometry.SlicerReveal(3) >= 84,
                "full reading grace after fourth reveal");
            for (int age = 0; age < FirstSeveranceScoreGeometry.SlicerFire(step); age++)
            {
                var seen = new System.Collections.Generic.HashSet<int>();
                foreach (var ray in FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer, step, age, 4000, 4000))
                {
                    AssertEqual(false, ray.Live, "all four previews harmless");
                    seen.Add(ray.Pulse);
                }
                AssertEqual(Math.Min(4, age / 30 + 1), seen.Count, "four cumulative forecasts");
            }
            for (int pulse = 0; pulse < 4; pulse++)
            {
                int fire = pulse * FirstSeveranceScoreGeometry.SlicerCadence(step) + FirstSeveranceScoreGeometry.SlicerFire(step);
                foreach (var ray in FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer, step, fire - 1, 4000, 4000))
                    AssertEqual(false, ray.Live, "last warning frame harmless");
                AssertEqual(true, fire < FirstSeveranceChoreography.Final[step].Ticks, "fourth shot not truncated");
            }
        }
    }

    [DomainTest("Lattice preserves four Spread pockets and rejects retired Stack layouts")]
    private static void LatticeSanctuaries()
    {
        for (byte pattern = 8; pattern < 12; pattern++)
        {
            var grid = new FirstSeveranceGridVolley(3, 100, pattern, 4000, 4000);
            var pockets = FirstSeveranceSafeWindows.Pockets(pattern, 4000, 4000);
            AssertEqual(4, pockets.Count, "explicit bounded sanctuary layout");
            AssertEqual(true, grid.Rays.Count <= FirstSeveranceGridVolley.MaximumLines, "bounded post-cut segments");
            foreach (var pocket in pockets)
            {
                const float acceptance = 36;
                for (float angle = 0; angle < MathF.Tau; angle += .1f)
                    AssertEqual(false, grid.Intersects(grid.FireTick, pocket.X + MathF.Cos(angle) * acceptance,
                        pocket.Y + MathF.Sin(angle) * acceptance, 10, 21), "the entire visible acceptance circle is safe for a body");
            }
            for (int a = 0; a < pockets.Count; a++)
                for (int b = a + 1; b < pockets.Count; b++)
                    AssertEqual(true, MathF.Sqrt(MathF.Pow(pockets[a].X - pockets[b].X, 2)
                        + MathF.Pow(pockets[a].Y - pockets[b].Y, 2)) >= FirstSeveranceLanceTuning.SpreadSeparation + 72,
                        "four spread destinations retain separation throughout both 36px acceptance discs");
            AssertEqual(true, grid.Intersects(grid.LineFireTick(0) + FirstSeveranceBeamIgnition.FullWidthTicks,
                grid.Rays[0].X, grid.Rays[0].Y, 10, 21), "amplified hazards remain outside sanctuaries");
            AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(3, 100, pattern, 4000, 4000,
                new[] { FirstSeveranceGridVolley.AimCoreBeam(4000, 4000, 4200, 3200) }), "no aimed core salvo through a sanctuary");
        }
        AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(3, 100, 12, 4000, 4000), "both sanctuary bits invalid");
        for (byte retired = 4; retired < 8; retired++)
            AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(3, 100, retired, 4000, 4000), "Stack layout retired");
        for (uint serial = 1; serial < 13; serial++)
        {
            AssertEqual((byte)((serial - 1) % 4), FirstSeveranceSafeWindows.GridPattern(serial, 60), "first volley now ordinary");
            AssertEqual((byte)((serial - 1) % 4), FirstSeveranceSafeWindows.GridPattern(serial, 162), "second volley ordinary");
            AssertEqual((byte)(8 | ((serial - 1) % 4)), FirstSeveranceSafeWindows.GridPattern(serial, 264), "third volley Spread");
        }
    }

    [DomainTest("Stack and Spread resolve during the matching safe hold, never in transitions")]
    private static void SafeWindowClock()
    {
        const ulong start = 1000;
        AssertEqual(false, FirstSeveranceSafeWindows.At(FirstSeveranceSubstate.Lattice, 0, start, start - 1, 4000, 4000).HasValue, "future state harmless");
        AssertEqual(false, FirstSeveranceSafeWindows.At(FirstSeveranceSubstate.PhaseTransition, 0, start, start + 132, 4000, 4000).HasValue, "no transition mechanic");
        for (ulong age = 0; age < 450; age++)
        {
            var task = FirstSeveranceSafeWindows.At(FirstSeveranceSubstate.Lattice, 0, start, start + age, 4000, 4000);
            AssertEqual(false, task is { Kind: FirstSeveranceSafeMechanic.Stack }, "Phase II never assigns Stack");
        }
        foreach (var action in FirstSeveranceChoreography.Unbound)
            AssertEqual(false, action.State == FirstSeveranceSubstate.Stack, "no standalone Phase II Stack");
        foreach (int local in new[] { 336 })
        {
            var window = FirstSeveranceSafeWindows.At(FirstSeveranceSubstate.Lattice, 0, start, start + (ulong)local, 4000, 4000)!.Value;
            const ulong volleyAge = 264;
            var grid = new FirstSeveranceGridVolley(3, start + volleyAge,
                FirstSeveranceSafeWindows.GridPattern(3, volleyAge), 4000, 4000);
            AssertEqual(window.ResolveTick, start + (ulong)local, "deadline derived from authority action epoch");
            AssertEqual(true, grid.IsFiring(window.ResolveTick), "resolve during safe hold, not after it");
        }
        foreach (int step in new[] { 0, 5 })
            for (int pulse = 0; pulse < 3; pulse++)
            {
                var window = FirstSeveranceSafeWindows.At(FirstSeveranceSubstate.RemoteClaws, step, start,
                    start + (ulong)(pulse * 200 + 170), 4000, 4000)!.Value;
                AssertEqual(pulse % 2 == 0 ? FirstSeveranceSafeMechanic.Stack : FirstSeveranceSafeMechanic.Spread,
                    window.Kind, "alternating safe-strip tasks");
                for (int local = 102; local < FirstSeveranceScoreGeometry.FloodEndTick; local++)
                    foreach (var item in FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RemoteClaws, step, pulse * 200 + local, 4000, 4000))
                    {
                        float dy = window.Kind == FirstSeveranceSafeMechanic.Stack ? FirstSeveranceLanceTuning.StackRadius : 0;
                        AssertEqual(false, item.Ray.Intersects(window.X, window.Y + dy, 10, 21), "whole Stack circle fits below");
                        AssertEqual(false, item.Ray.Intersects(window.X, window.Y - dy, 10, 21), "whole Stack circle fits above");
                        foreach (int dx in new[] { -1110, -370, 370, 1110 })
                            AssertEqual(false, item.Ray.Intersects(window.X + dx, window.Y, 10, 21), "four spread centers fit the horizontal strip");
                    }
            }
    }

    [DomainTest("Null Refrain has bounded smooth strokes and no anticipation/recovery damage")]
    private static void RewardStrokeMotion()
    {
        AssertEqual(false, NullRefrainMotion.Live(.239f), "windup harmless");
        AssertEqual(true, NullRefrainMotion.Live(.24f), "stroke starts");
        AssertEqual(false, NullRefrainMotion.Live(.76f), "recovery harmless");
        for (int combo = 0; combo < 3; combo++)
        {
            foreach (float speed in new[] { .1f, 1, 2, 8 })
                AssertEqual(true, NullRefrainMotion.Duration(combo, speed) is >= 10 and <= 90, "duration bounded with attack speed");
            foreach (int facing in new[] { -1, 1 })
            {
                float previous = NullRefrainMotion.Angle(combo, facing, 1, 0);
                for (int tick = 1; tick <= 1000; tick++)
                {
                    float angle = NullRefrainMotion.Angle(combo, facing, 1, tick / 1000f);
                    AssertEqual(true, float.IsFinite(angle) && Math.Abs(angle - previous) < .02f, "continuous finite stroke pose");
                    previous = angle;
                }
            }
        }
        AssertEqual(true, NullRefrainMotion.Reach(2) > NullRefrainMotion.Reach(0), "finisher extends physical reach");
    }
}
