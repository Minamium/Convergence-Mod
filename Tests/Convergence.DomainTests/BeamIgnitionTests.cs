using System;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Beam ignition shares continuous thin launch and full-width bounds")]
    private static void BeamIgnitionBounds()
    {
        foreach (float width in new[] { 6f, 12f, 44f, 72f, 300f })
        {
            var full = new FirstSeveranceLanceRay(4000, 3000, 1, 0, 2600, width);
            var previous = FirstSeveranceBeamIgnition.At(full, -1);
            AssertEqual(false, previous.Intersects(4000, 3000, 10, 21), "zero length cannot hit at source");
            for (int sample = 0; sample <= 700; sample++)
            {
                var ray = FirstSeveranceBeamIgnition.At(full, sample / 100d);
                AssertEqual(true, ray.Length >= previous.Length && ray.Length <= full.Length, "monotone bounded front");
                AssertEqual(true, ray.HalfWidth >= previous.HalfWidth && ray.HalfWidth <= width, "monotone bounded amplification");
                AssertEqual(true, ray.Length - previous.Length < 27 && ray.HalfWidth - previous.HalfWidth < 1.1f, "continuous fractional motion");
                AssertEqual(false, ray.Intersects(ray.X + ray.Length + 1, ray.Y, 0, 0), "no damage ahead of luminous front");
                AssertEqual(false, ray.Intersects(ray.X + ray.Length * .5f, ray.Y + ray.HalfWidth + .01f, 0, 0), "no damage beyond current mantle");
                previous = ray;
            }
            AssertEqual(full, previous, "all attacks settle to original dimensions");
            var pilot = FirstSeveranceBeamIgnition.At(full, 1);
            AssertEqual(1.5f, pilot.HalfWidth, "hold thin before rapid widening");
            AssertEqual(true, pilot.Length > full.Length * .7f, "needle launches rapidly");
        }
    }

    [DomainTest("Beam ignition lattice permutes all lines with equal warnings on every peer")]
    private static void LatticeIgnitionOrder()
    {
        bool changedOrder = false;
        for (uint serial = 1; serial <= 16; serial++)
        {
            var grid = new FirstSeveranceGridVolley(serial, 700, 0, 4000, 4000);
            var peer = new FirstSeveranceGridVolley(serial, 700, 0, 4000, 4000);
            var other = new FirstSeveranceGridVolley(serial + 1, 700, 0, 4000, 4000);
            ulong first = ulong.MaxValue, last = 0;
            for (int line = 0; line < grid.Rays.Count; line++)
            {
                ulong fire = grid.LineFireTick(line), end = grid.LineEndTick(line);
                first = Math.Min(first, fire); last = Math.Max(last, fire);
                changedOrder |= fire != other.LineFireTick(line);
                AssertEqual(peer.LineFireTick(line), fire, "peer never draws local random order");
                AssertEqual(60ul, fire - grid.RevealTick(line), "each late reveal gets the entire warning");
                AssertEqual(20ul, end - fire, "each line retains full active duration");
                AssertEqual(false, grid.LineIsLive(line, fire - 1), "no early line harm");
                AssertEqual(false, grid.LineIsLive(line, end), "no damaging residue");
                AssertEqual(grid.Rays[line], grid.RayAt(line, fire + 7), "restored full locked shape");
                AssertEqual(0f, grid.RayAt(line, fire).Length, "first instant starts at origin");
                for (double age = 0; age <= 7; age += .25)
                    AssertEqual(peer.RayAt(line, fire + age), grid.RayAt(line, fire + age), "fractional shared geometry");
            }
            AssertEqual(grid.FireTick, first, "no global delay added");
            AssertEqual(8ul, last - first, "rapid complete scattered burst");
            AssertEqual(true, grid.EndTick < grid.StartTick + FirstSeveranceGridVolley.CadenceTicks, "no next volley truncation");
        }
        AssertEqual(true, changedOrder, "new volley changes scatter order");
    }

    [DomainTest("Beam ignition Final score and rotating sweep use animated not full-size hits")]
    private static void ScoreIgnitionCollision()
    {
        foreach (var state in new[] { FirstSeveranceSubstate.FinalSlicer, FirstSeveranceSubstate.RotatingBlade })
        {
            int fire = state == FirstSeveranceSubstate.FinalSlicer ? FirstSeveranceScoreGeometry.SlicerFire(5) : FirstSeveranceChoreography.BladeWindup;
            for (int age = 0; age <= 7; age++)
            {
                foreach (var item in FirstSeveranceScoreGeometry.Rays(state, 5, fire + age, 4000, 4000, 1))
                {
                    if (!item.Live) continue;
                    var body = item.BeamRay;
                    AssertEqual(FirstSeveranceBeamIgnition.At(item.Ray, age), body, "same envelope on both consumers");
                    float x = body.X + body.DirectionX * body.Length * .5f;
                    float y = body.Y + body.DirectionY * body.Length * .5f;
                    AssertEqual(age > 0, FirstSeveranceScoreGeometry.RayHits(state, item, fire + age, x, y, 0, 0), "live body, no launch-point phantom hit");
                }
            }
        }
    }
}
