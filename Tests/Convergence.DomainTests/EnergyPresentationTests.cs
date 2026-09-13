using System;
using Convergence.Client.Encounters.FirstSeverance;

namespace Convergence.DomainTests;
internal static partial class Program
{
    [DomainTest("Energy presentation: charge and kick respect accepted times")]
    private static void EnergyPulseBounds()
    {
        AssertEqual(0f, FirstSeveranceEnergyPulse.Charge(99, 100, 200), "before start");
        AssertEqual(0f, FirstSeveranceEnergyPulse.Charge(200, 100, 200), "charge stops at fire");
        AssertEqual(0f, FirstSeveranceEnergyPulse.Kick(199, 200, 250), "no early fire shake");
        AssertEqual(0f, FirstSeveranceEnergyPulse.Kick(262, 200, 250), "bounded release");
        for (double t = 100; t < 300; t += .1)
        {
            float charge = FirstSeveranceEnergyPulse.Charge(t, 100, 200);
            float kick = FirstSeveranceEnergyPulse.Kick(t, 200, 250);
            AssertEqual(true, float.IsFinite(charge) && charge >= 0 && charge <= 1, "bounded charge");
            AssertEqual(true, float.IsFinite(kick) && kick >= 0 && kick <= 18, "bounded shake");
        }
    }
    [DomainTest("Energy presentation: side crater remains continuous and off center")]
    private static void SideCraterProfile()
    {
        AssertEqual(0f, FirstSeveranceCoreCrater.SampleSide(0, 0, 1).Depth, "front face intact");
        AssertEqual(true, FirstSeveranceCoreCrater.SampleSide(.69f, 0, 1).Depth < -.4f, "opening at side");
        AssertEqual(0f, FirstSeveranceCoreCrater.SampleSide(.69f, 0, 0).Depth, "closed sphere");
        for (float x = -1; x <= 1; x += .001f)
        {
            var sample = FirstSeveranceCoreCrater.SampleSide(x, .1f, .8f);
            var next = FirstSeveranceCoreCrater.SampleSide(x + .0001f, .1f, .8f);
            AssertEqual(true, float.IsFinite(sample.AlongSlope) && MathF.Abs(next.Depth - sample.Depth) < .004f,
                "smooth edge at fractional coordinates");
        }
    }
}
