using System;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Random final triples reconstruct all axes with lattice collision and bounded clipping")]
    private static void RandomFinalTriples()
    {
        var seen = new bool[4];
        foreach (int step in new[] {5,11,17,23})
        for (ulong seed = 1; seed <= 128; seed++)
        for (int pulse = 0; pulse < 3; pulse++)
        {
            int axis = FirstSeveranceRandomComb.Axis(seed, step, pulse);
            seen[axis] = true;
            int cadence = FirstSeveranceScoreGeometry.SlicerCadence(step);
            int fire = pulse * cadence + FirstSeveranceScoreGeometry.SlicerFire(step);
            var warning = FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,step,fire-1,4000,4000,seed);
            var live = FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,step,fire,4000,4000,seed);
            var replica = FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,step,fire,4000,4000,seed);
            AssertEqual(true,live.Count > 0 && live.Count <= 32,"bounded field-wide comb");
            AssertEqual(live.Count,warning.Count,"warning includes every tooth");
            for (int i=0;i<live.Count;i++)
            {
                var ray=live[i].Ray;
                AssertEqual(true,ray.IsValid,"finite clipped ray");
                AssertEqual(ray,replica[i].Ray,"peer reconstructs identical seeded ray");
                AssertEqual(ray,warning[i].Ray,"no retarget on release");
                AssertEqual(false,warning[i].Live,"warning harmless");
                AssertEqual(true,live[i].Live,"first live tick");
                AssertEqual(FirstSeveranceGridVolley.HalfWidth,ray.HalfWidth,"grid collision width");
                float mx=ray.X+ray.DirectionX*ray.Length*.5f, my=ray.Y+ray.DirectionY*ray.Length*.5f;
                AssertEqual(true,ray.Intersects(mx,my,10,21),"beam center hits");
                float gap = FirstSeveranceScoreGeometry.SlicerPitch * .5f;
                AssertEqual(false,ray.Intersects(mx-ray.DirectionY*gap,my+ray.DirectionX*gap,10,21),"half-pitch gap fits full player even diagonally");
            }
            foreach(var ray in FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,step,
                pulse*cadence+FirstSeveranceScoreGeometry.SlicerEnd(step),4000,4000,seed))
                AssertEqual(false,ray.Live,"recovery harmless");
        }
        foreach(bool axis in seen) AssertEqual(true,axis,"vertical horizontal and both diagonals appear");
        AssertEqual(3,FirstSeveranceScoreGeometry.SlicerPulses,"three pulses only");
    }
}
