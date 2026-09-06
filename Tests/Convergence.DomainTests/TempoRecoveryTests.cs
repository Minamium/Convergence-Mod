using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Prism targets every standing member with bounded immutable rays")]
    private static void SimultaneousPrismRoster()
    {
        for (int count = 1; count <= 4; count++)
        {
            var targets = new List<FirstSeverancePrismTarget>();
            for (int i = 0; i < count; i++) targets.Add(new(i + 2, 3500 + i * 350, 3500, i * 2, -i));
            for (byte step = 0; step < 8; step++)
            {
                var volley = FirstSeveranceAttackPatterns.CreatePrism(1, 100, step, targets);
                AssertEqual(count, volley.Rays.Count, "one simultaneous ray per standing player");
                AssertEqual(128ul, volley.FireTick, "all warnings resolve on the same tick");
                for (int i = 0; i < count; i++)
                {
                    var target = targets[i];
                    var expected = FirstSeveranceAttackPatterns.Create(1, 100, FirstSeveranceSubstate.PylonCheck,
                        step, target.Slot, target.X, target.Y, target.VelocityX, target.VelocityY);
                    AssertEqual(expected.Rays[0], volley.Rays[i], "authority-locked predicted origin and angle");
                }
                var remembered = volley.Rays[0];
                targets[0] = targets[0] with { X = targets[0].X + 5 };
                AssertEqual(remembered, volley.Rays[0], "assignment cannot follow a later target mutation");
            }
        }
        var valid = new FirstSeverancePrismTarget(1, 4000, 3500, 0, 0);
        AssertThrows<ArgumentException>(() => FirstSeveranceAttackPatterns.CreatePrism(1, 100, 0,
            Array.Empty<FirstSeverancePrismTarget>()), "empty targets");
        AssertThrows<ArgumentException>(() => FirstSeveranceAttackPatterns.CreatePrism(1, 100, 0,
            new[] { valid, valid }), "duplicate slot");
        AssertThrows<ArgumentException>(() => FirstSeveranceAttackPatterns.CreatePrism(1, 100, 0,
            new[] { valid, valid, valid, valid, valid }), "five targets rejected before allocation");
        AssertThrows<ArgumentException>(() => FirstSeveranceAttackPatterns.CreatePrism(1, 100, 0,
            new[] { valid with { VelocityY = float.NaN } }), "invalid target motion");
    }

    [DomainTest("Horizontal floods share deployment, widening and moving safe pockets")]
    private static void FloodSafePockets()
    {
        var state = FirstSeveranceSubstate.RemoteClaws;
        foreach (int step in new[] { 0, 5 })
            for (int pulse = 0; pulse < 3; pulse++)
            {
                float safeY = 3440 + FirstSeveranceScoreGeometry.FloodSafeY(step, pulse);
                var previous = FirstSeveranceScoreGeometry.Rays(state, step, pulse * 200 + 48, 4000, 4000);
                for (int local = 0; local < 200; local++)
                {
                    var rays = FirstSeveranceScoreGeometry.Rays(state, step, pulse * 200 + local, 4000, 4000);
                    foreach (var item in rays)
                    {
                        AssertEqual(local >= 48 && local < FirstSeveranceScoreGeometry.FloodEndTick, item.Live, "exact harmless anticipation and decay");
                        foreach (float x in new[] { 2730f, 4000f, 5270f })
                            AssertEqual(false, FirstSeveranceScoreGeometry.RayHits(state, item, pulse * 200 + local,
                                x, safeY, 10, 21), "whole body safe throughout deployment and widening");
                    }
                    if (local > 48 && local <= 102)
                    {
                        for (int i = 0; i < rays.Count; i++)
                        {
                            AssertEqual(true, rays[i].Ray.HalfWidth >= previous[i].Ray.HalfWidth, "no snapping backward during widening");
                            AssertEqual(true, rays[i].Ray.Length >= previous[i].Ray.Length, "smooth expanding beam front");
                        }
                        previous = rays;
                    }
                }
                var full = FirstSeveranceScoreGeometry.Rays(state, step, pulse * 200 + 102, 4000, 4000);
                for (int band = 0; band < 2; band++)
                {
                    var expected = FirstSeveranceScoreGeometry.FloodBand(step, pulse, band, 4000, 4000);
                    AssertEqual(expected, full[band].Ray, "visible final band equals actual full hit volume");
                    AssertEqual(true, expected.Intersects(4000, expected.Y, 10, 21), "filled band hits");
                }
                AssertEqual(true, Math.Abs(FirstSeveranceScoreGeometry.FloodSafeY(step, pulse)
                    - FirstSeveranceScoreGeometry.FloodSafeY(step, (pulse + 1) % 3)) >= 220, "safe strip moves substantially");
            }
        float early = FirstSeveranceScoreGeometry.FloodGrowth(69) - FirstSeveranceScoreGeometry.FloodGrowth(68);
        float middle = FirstSeveranceScoreGeometry.FloodGrowth(83) - FirstSeveranceScoreGeometry.FloodGrowth(82);
        AssertEqual(true, middle > early * 2, "width grows with acceleration before easing into full hold");
    }

    [DomainTest("Final cadence accelerates and repeated grids have no fixed safe cell")]
    private static void FinalAccelerationAndShifts()
    {
        var score = FirstSeveranceChoreography.Final;
        for (int station = 1; station < 8; station++)
            for (int action = 0; action < 3; action++)
                if (action < 2 || station >= 2)
                    AssertEqual(true, score[station * 3 + action].Ticks < score[(station - (action == 2 ? 2 : 1)) * 3 + action].Ticks,
                        "each category accelerates with terminal progress (compare slicers to slicers)");
        AssertEqual(55, FirstSeveranceScoreGeometry.SlicerCadence(2), "initial beam cadence includes 15 warning ticks");
        AssertEqual(43, FirstSeveranceScoreGeometry.SlicerCadence(23), "final beam cadence includes 15 warning ticks");
        AssertEqual(true, FirstSeveranceScoreGeometry.BulletSpeed(20) > FirstSeveranceScoreGeometry.BulletSpeed(2), "bullet motion accelerates too");
        foreach (int step in new[] { 5, 11, 17, 23 })
        {
            AssertEqual(true, FirstSeveranceScoreGeometry.SlicerPulses * FirstSeveranceScoreGeometry.SlicerCadence(step)
                <= score[step].Ticks, "last full telegraph and recovery fit the action");
            // For each axis, its three changing offsets must eventually catch a
            // stationary whole player, including positions adjacent to the wall.
            for (int axis = 0; axis < 2; axis++)
                for (float at = axis == 0 ? -1270 : -539; at <= (axis == 0 ? 1270 : 539); at += 4)
                {
                    bool everHit = false;
                    for (int pulse = axis; pulse < 6; pulse += 2)
                    {
                        double age = pulse * FirstSeveranceScoreGeometry.SlicerCadence(step) + FirstSeveranceScoreGeometry.SlicerFire(step);
                        foreach (var item in FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer, step, age, 4000, 4000))
                            everHit |= item.Live && item.Ray.Intersects(4000 + (axis == 0 ? at : 0), 3440 + (axis == 1 ? at : 0), 10, 21);
                    }
                    AssertEqual(true, everHit, "no permanent stationary safe lane");
                }
        }
    }
}
