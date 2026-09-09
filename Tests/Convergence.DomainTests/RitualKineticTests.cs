using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Kinetic recoil and verdict use connected fast brake accelerating beats")]
    private static void KineticBeatContinuity()
    {
        AssertEqual(0f, RitualKineticMotion.Recoil(0), "no one-frame flash step");
        AssertEqual(true, RitualKineticMotion.Recoil(1) > .9f, "fast firing kick");
        AssertEqual(0f, RitualKineticMotion.Recoil(12), "recovered");
        AssertEqual(265f, RitualKineticMotion.VerdictRadius(5), "arrival brakes at envelope");
        AssertEqual(265f, RitualKineticMotion.VerdictRadius(23), "holds while locking");
        AssertEqual(RitualArmamentChoreography.VerdictRadius, RitualKineticMotion.VerdictRadius(28), "exact real footprint at contact");
        float early = RitualKineticMotion.VerdictClosure(24) - RitualKineticMotion.VerdictClosure(23);
        float late = RitualKineticMotion.VerdictClosure(28) - RitualKineticMotion.VerdictClosure(27);
        AssertEqual(true, late > early * 100, "explosive final convergence, no uniformly slow interpolation");
        for (int i = 0; i <= 10800; i++)
        {
            float phase = i / 100f, flare = RitualKineticMotion.ChoirFlare(phase);
            AssertEqual(true, float.IsFinite(flare) && flare is >= 0 and <= 1, "bounded choir pressure");
        }
        AssertEqual(0f, RitualKineticMotion.ChoirFlare(88), "choir release starts continuously");
        AssertEqual(0f, RitualKineticMotion.ChoirFlare(108), "cycle seam has no residual flash");
    }
}
