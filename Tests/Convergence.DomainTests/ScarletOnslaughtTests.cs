using System;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet sixth accent is bounded and seventh accent is rejected")]
    private static void ScarletSixthAccent()
    {
        var p = TechniqueExample(CrimsonTechnique.CrownRain) with { Pulse = 5, Step = 5, Steps = 6 };
        p.Validate();
        bool failed = false;
        try { (p with { Pulse = 6 }).Validate(); } catch (System.IO.InvalidDataException) { failed = true; }
        AssertEqual(true, failed, "bounded note count");
    }

    [DomainTest("Scarlet release accelerates brakes and bites without discontinuities")]
    private static void ScarletReleaseCurve()
    {
        float old = 0;
        for (int i = 0; i <= 1000; i++)
        {
            float value = CrimsonTechniqueGeometry.Strike(i / 1000f);
            AssertEqual(true, value >= old && value <= 1.001f, "monotone bounded attack path");
            AssertEqual(true, value - old < .007f, "no discontinuous extension");
            old = value;
        }
        AssertEqual(true, Math.Abs(old - 1) < .00001, "full extension");
    }

    [DomainTest("Scarlet broad attacks occupy the field while preserving dodge space across six notes")]
    private static void ScarletBroadFootprints()
    {
        Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var technique in new[] { CrimsonTechnique.CrownRain, CrimsonTechnique.CrownCinders,
            CrimsonTechnique.MantleFan, CrimsonTechnique.MantleScissors, CrimsonTechnique.ChoirThrust,
            CrimsonTechnique.ChoirRend, CrimsonTechnique.VesperaPetals })
        {
            var basePlan = TechniqueExample(technique);
            float low = float.MaxValue, high = float.MinValue;
            int count = CrimsonTechniqueGeometry.Write(basePlan, basePlan.Fire, strokes, true);
            foreach (var s in strokes[..count])
            { low = Math.Min(low, Math.Min(s.A.X, s.B.X)); high = Math.Max(high, Math.Max(s.A.X, s.B.X)); }
            AssertEqual(true, high - low > 1500, $"{technique} reaches beyond a local target cluster");
            bool found = false;
            var f = basePlan.Field;
            for (float x = f.Left + 70; x < f.Right - 70 && !found; x += 32)
                for (float y = f.Top + 70; y < f.Bottom - 90 && !found; y += 32)
                {
                    bool safe = true;
                    for (int pulse = 0; pulse < 6 && safe; pulse++)
                    {
                        var p = basePlan with { Pulse = (byte)pulse, Step = (byte)pulse, Steps = 6 };
                        count = CrimsonTechniqueGeometry.Write(p, p.Fire, strokes, true);
                        foreach (var s in strokes[..count])
                            if (CrimsonTechniqueGeometry.Intersects(s, x, y, 20, 42)) { safe = false; break; }
                    }
                    found = safe;
                }
            AssertEqual(true, found, $"{technique} has a full-body refuge across the fast phrase");
        }
    }
}
