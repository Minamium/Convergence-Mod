using System;
using Convergence.Content.Encounters.FirstSeverance;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;
namespace Convergence.DomainTests;
internal static partial class Program
{
    [DomainTest("Beams use fixed damage while charges and raid shares retain their rules")]
    private static void FixedBeamDamage()
    {
        foreach (int life in new[] { 1, 100, 500, 625, 1000, int.MaxValue })
            foreach (var kind in new[] { FirstSeveranceAttackKind.ObservationLance,
                FirstSeveranceAttackKind.PursuitPrism, FirstSeveranceAttackKind.Stillness })
                AssertEqual(500, FirstSeveranceCombatRules.AttackDamage(kind, life), "HP-independent native beam source damage");
        AssertEqual(FirstSeveranceCombatRules.BeamDamage, FirstSeveranceScoreGeometry.FixedDamage, "later beams and bullets share the native source budget");
        AssertEqual(300, FirstSeveranceCombatRules.AttackDamage(FirstSeveranceAttackKind.SweepRight, 500), "right charge unchanged");
        AssertEqual(375, FirstSeveranceCombatRules.AttackDamage(FirstSeveranceAttackKind.SweepLeft, 625), "left charge unchanged");
        AssertEqual(0, FirstSeveranceCombatRules.StackDamage(625, 2, 2), "full attendance unchanged");
        AssertEqual(313, FirstSeveranceCombatRules.StackDamage(625, 2, 1), "missing share unchanged");
    }

    [DomainTest("Anticipation is bounded and continuous without loop-reset flashes")]
    private static void SmoothAnticipation()
    {
        const double fire = 128;
        foreach (double lead in new[] { 12d, 18d, 24d, 36d })
        {
            AssertEqual(0f, PreRelease(fire - lead, fire, lead), "pre-release starts dark");
            AssertEqual(1f, PreRelease(fire, fire, lead), "one luminous release crest");
            AssertEqual(0f, PreRelease(fire + 10, fire, lead), "release ends dark");
            for (double tick = fire - lead - 1; tick <= fire + 11; tick += .125)
            {
                float value = PreRelease(tick, fire, lead);
                AssertEqual(true, float.IsFinite(value) && value is >= 0 and <= 1, "bounded pre-release");
                AssertEqual(true, Math.Abs(PreRelease(tick + .001, fire, lead) - value) < .001, "no transition snap");
            }
        }
        foreach (double tick in new[] { 0d, 63.99, 64, 64.01, 4294967296d })
            AssertEqual(true, Cycle(tick, 64) is >= 0 and < 1, "bounded render-cycle phase");
    }

    [DomainTest("Emission envelopes remain continuous through firing and cooling")]
    private static void ContinuousEmissionEnvelopes()
    {
        const ulong start = 100, fire = 128, end = 140;
        foreach (double tick in new double[] { start, fire - 2, fire, fire + 2, end - 2, end, end + 18, end + 24 })
        {
            AssertEqual(true, Math.Abs(Emission(tick + .001, fire, end) - Emission(tick - .001, fire, end)) < .01f, "continuous emission");
            AssertEqual(true, Math.Abs(Aperture(tick + .001, start, fire, end) - Aperture(tick - .001, start, fire, end)) < .01f, "continuous mouth");
            AssertEqual(true, Math.Abs(CastPose(tick + .001, start, fire, end) - CastPose(tick - .001, start, fire, end)) < .01f, "continuous pose");
        }
        AssertEqual(0f, Emission(start, fire, end), "no early jet");
        AssertEqual(true, Emission(end + 1d, fire, end) > 0, "cooling survives authority end");
        AssertEqual(0f, Emission(end + 24d, fire, end), "bounded cooling");
        AssertEqual(0f, Aperture(end + 24d, start, fire, end), "bounded aperture");
        var to = new System.Numerics.Vector2(90, 30);
        AssertEqual(to, Hermite(System.Numerics.Vector2.Zero, new(10, 5), to, 4, 1), "settled position");
    }
    [DomainTest("Containment is ground anchored and clamps the complete body")]
    private static void GroundContainmentBounds()
    {
        var b = FirstSeveranceContainmentBounds.FromGround(4000, 4000);
        AssertEqual(3440f, b.CenterY, "center above floor");
        AssertEqual(4000f, b.Bottom, "floor is ground");
        AssertEqual((3000f, 3000f), b.ClampBody(3000, 3000, 20, 42), "interior untouched");
        AssertEqual((2720f, 2880f), b.ClampBody(-1000, -1000, 20, 42), "top left teleport");
        AssertEqual((5260f, 3958f), b.ClampBody(10000, 10000, 20, 42), "bottom right complete body");
        AssertEqual((2722f, 2882f), b.ClampBody(-1000, -1000, 20, 42, 2), "inward hysteresis");
        AssertEqual(true, float.IsFinite(b.ClampBody(float.NaN, 0, 20, 42).X), "invalid movement recovers");
    }
}
