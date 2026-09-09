using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Kinetic magic sigils arrive in sequence before charge and release")]
    private static void KineticMagicSequence()
    {
        for (int i = 0; i < 5; i++)
        {
            AssertEqual(0f, RitualKineticMotion.SigilArrival(i * 2, i), "not yet arrived");
            AssertEqual(1f, RitualKineticMotion.SigilArrival(i * 2 + 3, i), "snapped into place");
            var prev = RitualKineticMotion.SigilOffset(0, i);
            for (int t = 1; t <= 1600; t++)
            {
                var next = RitualKineticMotion.SigilOffset(t / 100f, i);
                AssertEqual(true, RitualArmamentRules.Finite(next) && (next - prev).Length() < 3,
                    "continuous finite sequential materialization/charge path");
                prev = next;
            }
        }
        AssertEqual(0f, RitualKineticMotion.MagicCharge(10), "short brace begins after deployment");
        AssertEqual(1f, RitualKineticMotion.MagicCharge(16), "charge completes on the fire beat");
    }

    [DomainTest("Kinetic magic beam preserves cast budget and only damages during its release")]
    private static void KineticMagicDamageWindow()
    {
        float total = 0;
        for (int cast = 0; cast < 4; cast++)
        {
            AssertEqual(3 * RitualArmamentChoreography.MagicRayShare(cast), RitualKineticMotion.MagicShare(cast),
                "one beam replaces three shares, not three full-budget beams");
            total += RitualKineticMotion.MagicShare(cast);
        }
        AssertEqual(true, Math.Abs(total - 9) < .001f, "four casts retain9x total base budget");
        AssertEqual(false, RitualKineticMotion.MagicLive(15.999f), "sigils and charge cannot hurt");
        AssertEqual(0f, RitualKineticMotion.MagicReach(16), "first frame begins at the muzzle");
        AssertEqual(true, RitualKineticMotion.MagicLive(16.5f), "release grows in two real ticks");
        AssertEqual(1800f, RitualKineticMotion.MagicReach(18), "finite full length");
        AssertEqual(false, RitualKineticMotion.MagicLive(22), "residue cannot hurt");
        AssertEqual(0f, RitualKineticMotion.MagicFade(34), "all residue expires");
        AssertEqual(true, RitualKineticMotion.MagicWidth(true) > RitualKineticMotion.MagicWidth(false), "fourth beam has larger real width");
    }

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
