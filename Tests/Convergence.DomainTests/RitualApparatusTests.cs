using System;
using System.Numerics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Claw display returns to the same parked hands at every attack boundary")]
    private static void ClawVisualSeams()
    {
        foreach (int facing in new[] { -1, 1 })
            foreach (int active in new[] { 0, 1 })
                foreach (int hand in new[] { 0, 1 })
                    foreach (float aim in new[] { -2.8f, -.2f, 0f, 1.4f, 2.8f })
                    {
                        var rest = RitualArmamentChoreography.ParkedHand(hand, 150, facing);
                        foreach (float p in new[] { 0f, 1f })
                        {
                            var pose = RitualArmamentChoreography.PresentedHand(p, active, hand, aim, facing, 150);
                            for (int finger = 0; finger < 5; finger++)
                                for (int joint = 0; joint < 4; joint++)
                                    AssertEqual(true, Vector2.Distance(NullCantorClawMotion.Joint(rest, finger, joint),
                                        NullCantorClawMotion.Joint(pose, finger, joint)) < .002f, "no pose teleport at spawn/death");
                        }
                    }
    }
    [DomainTest("Claw active presentation is identical to all damaging finger capsules")]
    private static void ClawLiveArt()
    {
        foreach (int facing in new[] { -1, 1 })
            foreach (int hand in new[] { 0, 1 })
                for (int step = 20; step <= 72; step++)
                {
                    float t = step / 100f;
                    var actual = RitualArmamentChoreography.WorldPose(NullCantorClawMotion.SwingPose(t, hand), 1.2f, facing);
                    var drawn = RitualArmamentChoreography.PresentedHand(t, hand, hand, 1.2f, facing, 50);
                    AssertEqual(true, Vector2.Distance(actual.Palm, drawn.Palm) < .001f, "no root lag during contact");
                    for (int finger = 0; finger < 5; finger++)
                        AssertEqual(true, Vector2.Distance(NullCantorClawMotion.Joint(actual, finger, 3),
                            NullCantorClawMotion.Joint(drawn, finger, 3)) < .001f, "visible tips follow collision");
                }
    }
    [DomainTest("Claw harmless blend joins retain position and velocity continuity")]
    private static void ClawBlendVelocity()
    {
        const float h = .0002f;
        foreach (int hand in new[] { 0, 1 })
            foreach (float cut in new[] { .20f, .72f })
            {
                var a = RitualArmamentChoreography.PresentedHand(cut - h, hand, hand, 0, 1, 0);
                var b = RitualArmamentChoreography.PresentedHand(cut, hand, hand, 0, 1, 0);
                var c = RitualArmamentChoreography.PresentedHand(cut + h, hand, hand, 0, 1, 0);
                AssertEqual(true, Vector2.Distance(a.Palm, c.Palm) < 1, "no boundary discontinuity");
                AssertEqual(true, Vector2.Distance((b.Palm-a.Palm)/h, (c.Palm-b.Palm)/h) < 40, "matched derivative within float tolerance");
            }
    }
    [DomainTest("Battery uses three distinct muzzles without multiplying its six-shot budget")]
    private static void BatteryBudget()
    {
        float sum = 0;
        for (int i = 0; i < 6; i++) sum += 3 * RitualArmamentChoreography.RangedRayShare(i);
        AssertEqual(true, Math.Abs(sum - 7.1f) < .0001f, "six shots include convergence, not extra damage");
        for (int lane = 0; lane < 3; lane++)
        {
            var root = RitualArmamentChoreography.CannonRoot(lane); var muzzle = RitualArmamentChoreography.CannonMuzzle(lane);
            AssertEqual(new Vector2(224, 0), muzzle - root, "material barrel and projectile emitter agree");
            AssertEqual(true, muzzle.Length() < 350, "bounded spawn");
        }
    }
    [DomainTest("Archive fourth-cast convergence redistributes the existing mana-cycle damage")]
    private static void ArchiveBudget()
    {
        float total = 0;
        for (int cast = 0; cast < 4; cast++) total += 3 * RitualArmamentChoreography.MagicRayShare(cast);
        AssertEqual(true, Math.Abs(total - 9) < .0001f, "same four-cast budget");
        AssertEqual(true, RitualArmamentChoreography.MagicRayShare(3) == 2 * RitualArmamentChoreography.MagicRayShare(0), "visible accent has real emphasis");
        AssertEqual(true, RitualArmamentChoreography.LensMuzzle(0) != RitualArmamentChoreography.LensMuzzle(2), "independent actual emitters");
    }
    [DomainTest("Choir has exactly three notes per slot and one shared third-beat accent")]
    private static void ChoirCadence()
    {
        for (int ordinal = 0; ordinal < 40; ordinal++)
        {
            int notes = 0; float budget = 0;
            for (int t = 0; t < 108; t++)
            {
                int shot = RitualArmamentChoreography.ChoirShotAt(t, ordinal);
                if (shot >= 0) { notes++; budget += RitualArmamentRules.ChoirMultiplier(shot); }
                float blend = RitualArmamentChoreography.ChoirAssembly(t);
                AssertEqual(true, blend is >= 0 and <= 1, "assembly envelope is bounded");
            }
            AssertEqual(3, notes, "three notes per108 real ticks");
            AssertEqual(true, Math.Abs(budget-3.4f) < .0001f, "per-slot damage budget preserved");
            AssertEqual(2, RitualArmamentChoreography.ChoirShotAt(88,ordinal), "every slot joins finale");
        }
    }
    [DomainTest("Choir assembly joins its cycle without a positional reset")]
    private static void ChoirSeatBounds()
    {
        AssertEqual(0f, RitualArmamentChoreography.ChoirAssembly(0), "starts dispersed");
        AssertEqual(0f, RitualArmamentChoreography.ChoirAssembly(108), "ends dispersed");
        for (int count=1; count<=40; count++)
            for (int i=0; i<count; i++)
                AssertEqual(true, RitualArmamentChoreography.ChoirSeat(i,count).Length() < 320, "visual formation remains local");
    }
    [DomainTest("Witness locks before execution and damages only a bounded triangular footprint")]
    private static void WitnessVerdictBounds()
    {
        AssertEqual(false, RitualArmamentChoreography.VerdictLive(27.99f), "harmless warning");
        AssertEqual(true, RitualArmamentChoreography.VerdictLive(28), "exact execution time");
        AssertEqual(false, RitualArmamentChoreography.VerdictLive(31), "residue harmless");
        AssertEqual(true, RitualArmamentChoreography.TriangleHits(Vector2.Zero,new(10),185), "center");
        AssertEqual(false, RitualArmamentChoreography.TriangleHits(new(182,182),new(1),185), "square corners are not triangle hits");
        AssertEqual(false, RitualArmamentChoreography.TriangleHits(new(500,0),new(10),185), "far decorative blade doesn't hit");
        AssertEqual(false, RitualArmamentChoreography.TriangleHits(new(float.NaN,0),new(10),185), "malformed geometry rejected");
    }
}
