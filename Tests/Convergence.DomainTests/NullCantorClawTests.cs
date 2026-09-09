using System;
using System.Numerics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Claws recharge once in real ticks and item swapping cannot duplicate a charge")]
    private static void ClawChargeContract()
    {
        var charge = new NullCantorClawCharge();
        AssertEqual(false, charge.TrySpend(), "empty charge cannot execute");
        for (int i = 0; i < 359; i++) charge.Tick(true, true, false);
        AssertEqual(false, charge.Ready, "no early completion");
        for (int i = 0; i < 100; i++) { charge.Tick(false, true, false); charge.Tick(true, false, false); charge.Tick(true, true, true); }
        AssertEqual(359, charge.Ticks, "swapped, Downed or crushing cannot advance the timer");
        charge.Tick(true, true, false);
        AssertEqual(true, charge.Ready, "exact six-second deadline");
        AssertEqual(true, charge.TrySpend(), "one execution");
        AssertEqual(false, charge.TrySpend(), "same tick cannot execute twice");
        for (int i = 0; i < 1000; i++) charge.Tick(true, true, false);
        AssertEqual(360, charge.Ticks, "saturating counter");
        charge.Reset(); AssertEqual(0, charge.Ticks, "death/world reset");
    }
    [DomainTest("Claw growth and five articulated finger chains are mirrored and bounded")]
    private static void ClawRigContract()
    {
        for (int tick = 0; tick <= 1000; tick++)
        {
            float p = tick / 1000f;
            var a = NullCantorClawMotion.SwingPose(p, 0);
            var b = NullCantorClawMotion.SwingPose(p, 1);
            AssertEqual(true, a.Scale is >= .67f and <= 2.301f, "bounded large in-swing expansion");
            for (int finger = 0; finger < 5; finger++)
                for (int joint = 0; joint <= 3; joint++)
                {
                    Vector2 x = NullCantorClawMotion.Joint(a, finger, joint), y = NullCantorClawMotion.Joint(b, finger, joint);
                    AssertEqual(true, NullCantorClawMotion.Finite(x), "finite pose");
                    AssertEqual(true, x.Length() < NullCantorClawMotion.MaximumReach, "no invisible unbounded reach");
                    AssertEqual(true, Vector2.Distance(x, new Vector2(y.X, -y.Y)) < .001f, "true left/right hand reflection");
                }
        }
        AssertEqual(true, NullCantorClawMotion.SwingPose(.4f, 0).Scale > 3 * NullCantorClawMotion.SwingPose(0, 0).Scale,
            "a giant claw, not a tiny inventory sprite with thin lines");
    }
    [DomainTest("Claw swing is continuous with harmless windup and recovery")]
    private static void ClawSwingContract()
    {
        foreach (float boundary in new[] { .20f, .72f })
            foreach (int hand in new[] { 0, 1 })
            {
                float x = NullCantorClawMotion.SwingAngle(boundary - .0001f, hand);
                float y = NullCantorClawMotion.SwingAngle(boundary + .0001f, hand);
                AssertEqual(true, Math.Abs(x - y) < .00001f, "no pose snap at live boundaries");
            }
        AssertEqual(false, NullCantorClawMotion.SwingLive(.19999f), "forecast does not hit");
        AssertEqual(true, NullCantorClawMotion.SwingLive(.20f), "inclusive start");
        AssertEqual(false, NullCantorClawMotion.SwingLive(.72f), "exclusive end");
        AssertEqual(false, NullCantorClawMotion.SwingLive(float.NaN), "malformed progress harmless");
        AssertEqual(28, NullCantorClawMotion.Duration(float.NaN), "invalid speed defaults safely");
        AssertEqual(10, NullCantorClawMotion.Duration(1000), "speed has a lower duration bound");
    }
    [DomainTest("Remote crush has one bounded impact window and harmless arrival and departure")]
    private static void ClawCrushClock()
    {
        AssertEqual(false, NullCantorClawMotion.CrushLive(15.99f), "approach is harmless");
        AssertEqual(true, NullCantorClawMotion.CrushLive(16), "impact");
        AssertEqual(false, NullCantorClawMotion.CrushLive(20), "residual flash is harmless");
        for (int hand = 0; hand < 2; hand++)
            for (int tick = 0; tick <= NullCantorClawMotion.CrushTicks; tick++)
            {
                var pose = NullCantorClawMotion.CrushPose(tick, hand);
                AssertEqual(true, NullCantorClawMotion.Finite(pose.Palm) && pose.Scale > 0 && pose.Scale <= 3.101f, "bounded remote rig");
            }
        AssertEqual(true, NullCantorClawMotion.CrushMultiplier < 5, "execution motif, not a literal NPC deletion");
    }
    [DomainTest("Claw execution snaps in, brakes briefly and accelerates into one diagonal impact")]
    private static void ClawExecutionContrast()
    {
        float Distance(float a, float b) => Vector2.Distance(NullCantorClawMotion.CrushPose(a, 0).Palm,
            NullCantorClawMotion.CrushPose(b, 0).Palm);
        AssertEqual(true, Distance(0, 1) > Distance(7, 8) * 12, "arrival faster than held tension");
        AssertEqual(true, Distance(15, 16) > Distance(11, 12) * 10, "terminal closure accelerates");
        foreach (float seam in new[] { 5f, 11f, 16f, 22f, 42f })
            AssertEqual(true, Distance(seam - .0001f, seam + .0001f) < .1f, "position continuous at beat boundaries");
        var left = NullCantorClawMotion.CrushPose(8, 0);
        var right = NullCantorClawMotion.CrushPose(8, 1);
        AssertEqual(true, left.Palm.Y > 80 && right.Palm.Y < -80, "opposing hands use a diagonal axis");
        AssertEqual(true, (left.Palm + right.Palm).Length() < .001f, "target stays between hands");
        int live = 0;
        for (int i = 0; i < NullCantorClawMotion.CrushTicks; i++) if (NullCantorClawMotion.CrushLive(i)) live++;
        AssertEqual(4, live, "duration changed without adding more hits");
    }
    [DomainTest("Crush target is finite range-limited and independent of later pointer movement")]
    private static void ClawTargetContract()
    {
        Vector2 origin = new(4000, 5000), mouse = new(9000, 6000);
        Vector2 locked = NullCantorClawMotion.ClampTarget(origin, mouse);
        AssertEqual(true, Math.Abs(Vector2.Distance(origin, locked) - 1120) < .001f, "range cap");
        AssertEqual(origin, NullCantorClawMotion.ClampTarget(origin, new(float.NaN, 0)), "invalid target does not escape");
        AssertEqual(origin, NullCantorClawMotion.ClampTarget(origin, origin), "zero distance is valid");
        AssertEqual(origin + Vector2.UnitX * 100, NullCantorClawMotion.ClampTarget(origin, origin + Vector2.UnitX * 100), "near target exact");
    }
    [DomainTest("Crush damage matches its warned ellipse rather than the screen-wide flash")]
    private static void ClawCrushGeometry()
    {
        AssertEqual(true, NullCantorClawMotion.CrushHits(Vector2.Zero, new(10, 20)), "inside center");
        AssertEqual(true, NullCantorClawMotion.CrushHits(new(175, 0), new(10, 20)), "body touches ellipse");
        AssertEqual(false, NullCantorClawMotion.CrushHits(new(190, 0), new(10, 20)), "outside warned region");
        AssertEqual(false, NullCantorClawMotion.CrushHits(new(166, 132), Vector2.Zero), "square corner is not in ellipse");
        AssertEqual(false, NullCantorClawMotion.CrushHits(new(0, 600), new(10, 20)), "long vertical flash never damages");
    }
}
