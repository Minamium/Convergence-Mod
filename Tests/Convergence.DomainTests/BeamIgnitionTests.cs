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
                AssertEqual(true, grid.PulseAt(line, fire + 10).Length < grid.Rays[line].Length, "finite packet, not a lit corridor");
                AssertEqual(0f, grid.PulseAt(line, fire).Length, "first instant starts at origin");
                for (double age = 0; age <= 20; age += .25)
                    AssertEqual(peer.PulseAt(line, fire + age), grid.PulseAt(line, fire + age), "fractional shared geometry");
            }
            AssertEqual(grid.FireTick, first, "no global delay added");
            AssertEqual(30ul, last - first, "readable half-second scattered deployment");
            AssertEqual(true, grid.EndTick < grid.StartTick + FirstSeveranceGridVolley.CadenceTicks, "no next volley truncation");
        }
        AssertEqual(true, changedOrder, "new volley changes scatter order");
    }

    [DomainTest("Lattice ribbon head and tail bound visible collision continuously")]
    private static void LatticeRibbonBounds()
    {
        foreach (bool vertical in new[] { false, true })
        {
            var ray = new FirstSeveranceLanceRay(4000, 3000, vertical ? 0 : 1, vertical ? 1 : 0, vertical ? 1120 : 2560, 12);
            float priorHead = 0, priorTail = -ray.Length;
            for (int sample = 0; sample <= 2000; sample++)
            {
                var pulse = new FirstSeveranceGridPulse(ray, ray, sample / 100d);
                AssertEqual(true, pulse.HeadDistance >= priorHead && pulse.TailDistance >= priorTail, "packet moves only forward");
                if (sample > 0) AssertEqual(true, pulse.HeadDistance - priorHead < 1.84f, "continuous fractional front");
                priorHead = pulse.HeadDistance; priorTail = pulse.TailDistance;
                float ahead = pulse.EndDistance + 1, behind = pulse.StartDistance - 1;
                AssertEqual(false, pulse.Intersects(ray.X + ray.DirectionX * ahead, ray.Y + ray.DirectionY * ahead, 0, 0), "no invisible damage ahead");
                AssertEqual(false, pulse.Intersects(ray.X + ray.DirectionX * behind, ray.Y + ray.DirectionY * behind, 0, 0), "no invisible damage behind tail");
                if (pulse.Length <= 0) continue;
                foreach (float u in new[] { .05f, .2f, .6f, .93f })
                {
                    float distance = pulse.TailDistance + pulse.PacketLength * u;
                    if (distance < pulse.StartDistance || distance > pulse.EndDistance) continue;
                    float width = pulse.HalfWidth * FirstSeveranceGridPulse.Envelope(u);
                    float x = ray.X + ray.DirectionX * distance, y = ray.Y + ray.DirectionY * distance;
                    AssertEqual(true, pulse.Intersects(x, y, 0, 0), "visible live spine damages");
                    AssertEqual(false, pulse.Intersects(x - ray.DirectionY * (width + .02f), y + ray.DirectionX * (width + .02f), 0, 0), "taper is geometry not alpha-only rectangle");
                    AssertEqual(true, pulse.Intersects(x - ray.DirectionY * width * .7f, y + ray.DirectionX * width * .7f, 0, 0), "inside luminous tapered body");
                }
            }
            AssertEqual(0f, new FirstSeveranceGridPulse(ray, ray, 20).Length, "last tail exits without a full-width switch-off");
            var late = new FirstSeveranceGridPulse(ray, ray, 10);
            AssertEqual(false, late.Intersects(ray.X, ray.Y, 10, 21), "source clear once tail passes");
        }
    }

    [DomainTest("Lattice sanctuary lanes remain unbroken and preserve the complete volley budget")]
    private static void LatticeRibbonCuts()
    {
        var grid = new FirstSeveranceGridVolley(3, 100, 8, 4000, 4000);
        for (int a = 0; a < grid.Rays.Count; a++)
            for (int b = a + 1; b < grid.Rays.Count; b++)
            {
                var first = grid.PulseAt(a, 180);
                var second = grid.PulseAt(b, 180);
                AssertEqual(false, first.Track == second.Track, "each retained lane has one continuous moving body");
            }
        AssertEqual(true, FirstSeveranceSafeWindows.SpreadVolleyAge + FirstSeveranceGridVolley.DurationTicks <= 450, "third volley completes before action deadline");
        AssertEqual(true, FirstSeveranceGridVolley.OpeningTicks + 3 * FirstSeveranceGridVolley.CadenceTicks
            + FirstSeveranceGridVolley.DurationTicks > 450, "no truncated fourth volley");
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
