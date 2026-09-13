using System;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Stillness uses half-width continuous bands with full warning and central sanctuary")]
    private static void CenterOutCurtain()
    {
        foreach (float x in new[] { 1000f, 70048f })
            foreach (byte step in new byte[] { 1, 3 })
            {
                var volley = FirstSeveranceAttackPatterns.Create(1, 100, FirstSeveranceSubstate.CoreExposure,
                    step, 0, x, 2000, 0, 0);
                AssertEqual(2, volley.Rays.Count, "only two bounded footprints on the wire");
                AssertEqual((ulong)FirstSeveranceAttackPatterns.StepTicks(FirstSeveranceSubstate.CoreExposure, step),
                    volley.EndTick - volley.StartTick, "scheduler reserves the complete band");
                AssertEqual(true, volley.EndTick - volley.StartTick < (ulong)FirstSeveranceAttackPatterns.StepCadence(FirstSeveranceSubstate.CoreExposure), "next charge never truncates the band");
                AssertEqual(35, volley.ActiveTicks, "overall attack window retained");
                AssertEqual(1, FirstSeveranceCurtainComb.LaneCount, "one unbroken material per side");
                foreach (var footprint in volley.Rays)
                {
                    AssertEqual(160f, footprint.HalfWidth, "band width halved from 640 to 320 pixels");
                    for (int lane = 0; lane < FirstSeveranceCurtainComb.LaneCount; lane++)
                    {
                        var ray = FirstSeveranceCurtainComb.Ray(footprint, lane);
                        ulong fire = FirstSeveranceCurtainComb.FireTick(volley, lane);
                        AssertEqual(footprint, ray, "material and collision share the full band");
                        AssertEqual((ulong)volley.TelegraphTicks, fire - FirstSeveranceCurtainComb.RevealTick(volley, lane), "every tooth gets the complete warning");
                        AssertEqual(false, FirstSeveranceCurtainComb.IsLive(volley, lane, fire - 1), "warning harmless");
                        AssertEqual(true, FirstSeveranceCurtainComb.IsLive(volley, lane, fire), "first live tick");
                        AssertEqual(false, FirstSeveranceCurtainComb.IsLive(volley, lane, FirstSeveranceCurtainComb.EndTick(volley, lane)), "cooling harmless");
                        AssertEqual(false, FirstSeveranceCurtainComb.Intersects(volley, fire - 1, ray.X, 2000, 0, 0), "not the old full curtain firing early");
                        AssertEqual(true, FirstSeveranceCurtainComb.Intersects(volley,
                            fire + FirstSeveranceBeamIgnition.FullWidthTicks, ray.X, 2000, 0, 0), "tooth reaches warned corridor after ignition");
                        AssertEqual(0, FirstSeveranceCurtainComb.Offset(lane), "no tooth staggering");
                    }
                    // Full-size player sampling includes joints and both curtain edges.
                    ulong full = volley.FireTick + FirstSeveranceCurtainComb.StaggerTicks + FirstSeveranceBeamIgnition.FullWidthTicks;
                    for (float dx = -footprint.HalfWidth; dx <= footprint.HalfWidth; dx += 2)
                        AssertEqual(true, FirstSeveranceCurtainComb.Intersects(volley, full, footprint.X + dx, 2000, 10, 21), "no survivable lane between teeth");
                }
                for (ulong tick = volley.StartTick; tick <= volley.EndTick; tick++)
                    AssertEqual(false, FirstSeveranceCurtainComb.Intersects(volley, tick, x, 2000, 10, 21), "central safe column retained through entire burst");
                AssertEqual(false, FirstSeveranceCurtainComb.Intersects(volley, volley.EndTick, volley.Rays[0].X, 2000, 10, 21), "no late damaging tail");
            }
        AssertThrows<ArgumentOutOfRangeException>(() => FirstSeveranceCurtainComb.Offset(-1), "negative lane rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => FirstSeveranceCurtainComb.Offset(1), "past-last lane rejected");
    }
}
