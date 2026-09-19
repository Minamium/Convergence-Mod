using System;
using System.IO;
using Convergence.Client.Encounters.CrimsonFoundry;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet long flight codec preserves the tail and rejects an oversized lifetime")]
    private static void ScarletLongFlightCodec()
    {
        var p = TechniqueExample(CrimsonTechnique.CrownRain) with { End = 632, LastEnd = 632 };
        using var stream = new MemoryStream();
        using (var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) p.Write(w);
        using var reader = new BinaryReader(new MemoryStream(stream.ToArray()));
        AssertEqual(p, CrimsonGesturePlan.Read(reader), "receiving peer accepts extended flight");
        bool rejected = false;
        try { (p with { End = 633, LastEnd = 633 }).Validate(); } catch (InvalidDataException) { rejected = true; }
        AssertEqual(true, rejected, "bounded lifetime");
    }
    [DomainTest("Scarlet rain head and tail traverse the entire forecast continuously")]
    private static void ScarletRainContinuity()
    {
        var p = TechniqueExample(CrimsonTechnique.CrownRain) with { End = 632, LastEnd = 632 };
        Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        CrimsonTechniqueGeometry.Write(p, p.Fire, strokes, true);
        var forecast = strokes[0]; float oldHead = forecast.A.Y, oldTail = oldHead, maxLength = 0;
        for (float age = p.Fire; age < p.End; age += .125f)
        {
            CrimsonTechniqueGeometry.Write(p, age, strokes);
            var s = strokes[0];
            AssertEqual(true, s.A.Y >= oldTail && s.B.Y >= oldHead, "monotone falling front/back");
            AssertEqual(true, s.B.Y - oldHead < 12 && s.A.Y - oldTail < 12, "continuous fractional movement");
            AssertEqual(true, s.A.Y >= forecast.A.Y && s.B.Y <= forecast.B.Y, "whole flight stays in its full-height forecast");
            maxLength = Math.Max(maxLength, s.B.Y - s.A.Y); oldHead = s.B.Y; oldTail = s.A.Y;
        }
        AssertEqual(true, maxLength > 400, "visible flowing body, not a tiny flashing pill");
        AssertEqual(true, Math.Abs(oldHead - forecast.B.Y) < .01 && Math.Abs(oldTail - forecast.B.Y) < .1, "head and tail both arrive at the bottom");
        AssertEqual(0, CrimsonTechniqueGeometry.Write(p, p.End, strokes), "residue never damages");
    }
    [DomainTest("Scarlet visual start fire and residue join continuously")]
    private static void ScarletFlowEnvelopes()
    {
        var p = TechniqueExample(CrimsonTechnique.CrownRain) with { End = 632, LastEnd = 632 };
        float oldGuide = 0, oldLive = 0;
        for (float age = p.Born - 1; age <= p.End + CrimsonRhythm.ResidueTicks + 1; age += .125f)
        {
            float guide = ScarletGesturePresentation.WarningOpacity(p, age), live = ScarletGesturePresentation.LiveOpacity(p, age);
            AssertEqual(true, guide is >= 0 and <= 1 && live is >= 0 and <= 1, "bounded opacity");
            AssertEqual(true, Math.Abs(guide - oldGuide) < .04 && Math.Abs(live - oldLive) < .07, "no flash/pop at state changes");
            oldGuide = guide; oldLive = live;
        }
        AssertEqual(true, ScarletGesturePresentation.WarningOpacity(p, p.Fire) > .8f, "forecast remains during ignition");
        AssertEqual(true, ScarletGesturePresentation.LiveOpacity(p, p.End + 12) > .4f, "shaped aftermath not immediate cutoff");
    }
    [DomainTest("Scarlet padded primitive endpoint and UV cover the complete path under the Luminance contract")]
    private static void ScarletTrailEndpoint()
    {
        for (int visible = 3; visible <= 96; visible++)
        {
            int submitted = visible + 1, drawnSegments = submitted - 2;
            AssertEqual(visible - 1, drawnSegments, "support point restores the omitted segment");
            float lastUv = drawnSegments / (float)(submitted - 1);
            AssertEqual(true, Math.Abs(lastUv * ScarletGesturePresentation.TrailCompletionScale(submitted) - 1) < .00001, "last visible endpoint reaches UV1");
        }
    }
}
