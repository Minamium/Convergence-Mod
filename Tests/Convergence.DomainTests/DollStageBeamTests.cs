using System;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Doll stage key requires a living sender's selected Doll and returns to an empty plinth")]
    private static void DollStageKey()
    {
        for (int mask = 0; mask < 16; mask++)
        {
            bool active = (mask & 1) != 0, dead = (mask & 2) != 0,
                ghost = (mask & 4) != 0, doll = (mask & 8) != 0;
            AssertEqual(mask == 9, FirstSeveranceDollActivation.CanActivate(active, dead, ghost, doll),
                "no forged held-item/alive claims or consumption path");
        }
        AssertEqual(false, FirstSeveranceDollActivation.ShowAttendant(FirstSeveranceCoreProtectionState.Idle), "empty before and after a fight");
        AssertEqual(true, FirstSeveranceDollActivation.ShowAttendant(FirstSeveranceCoreProtectionState.Preparing), "Doll on stage while Ready");
        AssertEqual(false, FirstSeveranceDollActivation.ShowAttendant(FirstSeveranceCoreProtectionState.Active), "intro owns capture art");
    }

    [DomainTest("Doll sphere crater has continuous depth wall normals and an unchanged outside silhouette")]
    private static void DollCoreCrater()
    {
        var closed = FirstSeveranceCoreCrater.Sample(0, 0, 0);
        AssertEqual(0f, closed.Depth, "closed metal is unchanged");
        AssertEqual(-.64f, FirstSeveranceCoreCrater.Sample(0, 0, 1).Depth, "real depression, not just dark ink");
        for (int frame = 0; frame <= 400; frame++)
        {
            float opening = frame / 400f;
            for (int pixel = 0; pixel <= 150; pixel++)
            {
                float x = pixel / 100f;
                var sample = FirstSeveranceCoreCrater.Sample(x, .07f, opening);
                AssertEqual(true, float.IsFinite(sample.Depth + sample.AlongSlope + sample.AcrossSlope), "finite mesh/normal");
                AssertEqual(true, sample.Depth >= -.64f && sample.Depth <= .1001f, "bounded cavity and lip");
                AssertEqual(true, sample.Interior >= 0 && sample.Interior <= 1, "bounded occlusion");
                var nearby = FirstSeveranceCoreCrater.Sample(x + .0001f, .07f, opening);
                AssertEqual(true, MathF.Abs(nearby.Depth - sample.Depth) < .004f, "continuous wall");
            }
            AssertEqual(0f, FirstSeveranceCoreCrater.Sample(1, 0, opening).Depth, "sphere rim never moves");
        }
        const float at = .42f, step = .0001f;
        float derivative = (FirstSeveranceCoreCrater.Sample(at + step, .1f, 1).Depth
            - FirstSeveranceCoreCrater.Sample(at - step, .1f, 1).Depth) / (2 * step);
        AssertEqual(true, MathF.Abs(derivative - FirstSeveranceCoreCrater.Sample(at, .1f, 1).AlongSlope) < .01f,
            "wall lighting is derived from displaced geometry");
    }
}
