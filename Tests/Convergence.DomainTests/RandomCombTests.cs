using System;
using System.Linq;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Final four-color prism score reconstructs full warning with pursuit collision")]
    private static void RandomFinalTriples()
    {
        var seen = new bool[4];
        foreach (int step in new[] {5,11,17,23})
        for (ulong seed = 1; seed <= 128; seed++)
        for (int pulse = 0; pulse < FirstSeveranceScoreGeometry.SlicerPulses; pulse++)
        {
            int axis = FirstSeveranceRandomComb.Axis(seed, step, pulse);
            seen[axis] = true;
            int cadence = FirstSeveranceScoreGeometry.SlicerCadence(step);
            int fire = pulse * cadence + FirstSeveranceScoreGeometry.SlicerFire(step);
            var warning = FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,step,fire-1,4000,4000,seed).Where(r => r.Pulse == pulse).ToArray();
            var live = FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,step,fire,4000,4000,seed).Where(r => r.Pulse == pulse).ToArray();
            var replica = FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,step,FirstSeveranceScoreGeometry.SlicerReveal(pulse),4000,4000,seed).Where(r => r.Pulse == pulse).ToArray();
            AssertEqual(true,live.Length > 0 && live.Length <= 40,"bounded field-wide prism groups");
            AssertEqual(live.Length,warning.Length,"warning includes every tooth");
            AssertEqual(live.Length,replica.Length,"full shape already shown at initial reveal");
            for (int i=0;i<live.Length;i++)
            {
                var ray=live[i].Ray;
                AssertEqual(true,ray.IsValid,"finite clipped ray");
                AssertEqual(ray,replica[i].Ray,"peer reconstructs identical seeded ray");
                AssertEqual(ray,warning[i].Ray,"no retarget on release");
                AssertEqual(false,warning[i].Live,"warning harmless");
                AssertEqual(true,live[i].Live,"first live tick");
                AssertEqual(FirstSeveranceLanceTuning.HalfWidth,ray.HalfWidth,"initial pursuit collision width");
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
        AssertEqual(4,FirstSeveranceScoreGeometry.SlicerPulses,"four ordered colors");
    }

    [DomainTest("Final four-color prism score has reading time, safe bands and no permanent hiding point")]
    private static void FinalColorCoverage()
    {
        foreach(int step in new[]{5,11,17,23})
        {
            AssertEqual(84,FirstSeveranceScoreGeometry.SlicerFire(step)-FirstSeveranceScoreGeometry.SlicerReveal(3),"last color reading time never accelerates");
            AssertEqual(true,FirstSeveranceScoreGeometry.SlicerCadence(step)>=48,"at least 0.6 seconds fully harmless movement between 0.2 second strikes");
            for(ulong seed=1;seed<=16;seed++)
            {
                var groups=Enumerable.Range(0,4).Select(p=>FirstSeveranceRandomComb.Rays(seed,step,p,0,0)).ToArray();
                int[] safe=new int[4];int samples=0;
                for(int x=-1248;x<=1248;x+=48) for(int y=-528;y<=528;y+=48)
                {
                    samples++;bool any=false;
                    for(int p=0;p<4;p++) {
                        bool hit=groups[p].Any(r=>r.Intersects(x,y,10,21));
                        any|=hit;if(!hit)safe[p]++;
                    }
                    AssertEqual(true,any,"four colors collectively fill the arena; no static exploit pocket");
                }
                foreach(int count in safe) AssertEqual(true,count>samples*.2f,"each color has broad full-player safe bands");
                AssertEqual(false,FirstSeveranceRandomComb.Axis(seed,step,0)==FirstSeveranceRandomComb.Axis(seed,step,2),"two different orientations per score");
            }
        }
    }
}
