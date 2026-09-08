using System;
using System.Numerics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Ritual easing is bounded with resting endpoint velocities")]
    private static void RitualEasing()
    {
        AssertEqual(0f, RitualArmamentRules.Smooth(-1), "before windup");
        AssertEqual(1f, RitualArmamentRules.Smooth(2), "after recovery");
        float prior = 0;
        for (int i = 0; i <= 1000; i++)
        {
            float value = RitualArmamentRules.Smooth(i / 1000f);
            AssertEqual(true, value >= prior - .000002f && value is >= 0 and <= 1, "bounded monotone quintic");
            prior = value;
        }
        AssertEqual(true, RitualArmamentRules.Smooth(.001f) < .000001f, "zero initial velocity");
        AssertEqual(true, 1 - RitualArmamentRules.Smooth(.999f) < .000002f, "zero final velocity");
    }

    [DomainTest("Ritual homing caps turns and preserves finite speed")]
    private static void RitualHoming()
    {
        foreach (float dt in new[] { 1f, .5f, .25f })
        {
            Vector2 v = new(36, 0);
            for (int step = 0; step < 120; step++)
            {
                Vector2 next = RitualArmamentRules.Steer(v, new(-500, 200), 44, dt);
                float turn = MathF.Abs(MathF.IEEERemainder(MathF.Atan2(next.Y, next.X) - MathF.Atan2(v.Y, v.X), MathF.Tau));
                AssertEqual(true, turn <= RitualArmamentRules.TurnRate * dt + .00001f, "no frame snap");
                AssertEqual(true, RitualArmamentRules.Finite(next) && next.Length() <= 44.001f, "no runaway speed");
                v = next;
            }
            AssertEqual(true, Vector2.Dot(Vector2.Normalize(v), Vector2.Normalize(new Vector2(-500, 200))) > .999f, "converges to target");
        }
        AssertEqual(Vector2.Zero, RitualArmamentRules.Steer(new(float.NaN, 0), Vector2.One, 40, 1), "reject nonfinite velocity");
        AssertEqual(Vector2.Zero, RitualArmamentRules.Steer(Vector2.One, Vector2.One, 0, 1), "reject invalid speed");
        AssertEqual(true, RitualArmamentRules.Finite(RitualArmamentRules.Steer(Vector2.Zero, Vector2.Zero, 40, .5f)), "coincident target finite");
    }

    [DomainTest("Ritual homing response is independent of sub-update count")]
    private static void RitualHomingCadence()
    {
        Vector2 once = RitualArmamentRules.Steer(new(20, 0), new(300, 0), 44, 1);
        Vector2 twice = RitualArmamentRules.Steer(new(20, 0), new(300, 0), 44, .5f);
        twice = RitualArmamentRules.Steer(twice, new(300, 0), 44, .5f);
        AssertEqual(true, Vector2.Distance(once, twice) < .0001f, "extraUpdates cannot accelerate the response");
    }

    [DomainTest("Ritual damage budget includes sixth shot and return shares")]
    private static void RitualBudgets()
    {
        float volley = 0;
        for (int shot = 0; shot < 6; shot++) volley += RitualArmamentRules.RangedMultiplier(shot);
        AssertEqual(true, Math.Abs(volley - 7.1f) < .00001f, "finisher is not unbudgeted extra damage");
        AssertEqual(1f, RitualArmamentRules.RogueShare(false) + RitualArmamentRules.RogueShare(true), "return is a share, not a second full hit");
        AssertEqual(true, RitualArmamentRules.AcquisitionRange < RitualArmamentRules.RetainRange, "sticky-target hysteresis");
        AssertEqual(5, Enum.GetValues<RitualArmamentKind>().Length, "five class forms");
        foreach (var kind in Enum.GetValues<RitualArmamentKind>())
        {
            AssertEqual(true, RitualArmamentRules.Damage(kind) is > 0 and < 10000, "bounded initial tuning, not instant-kill damage");
            AssertEqual(true, RitualArmamentRules.UseTicks(kind) >= 12, "bounded nominal cadence");
        }
    }

    [DomainTest("Ritual melee remains continuous across its active boundaries")]
    private static void RitualMeleeContinuity()
    {
        foreach (int combo in new[] { 0, 1, 2 })
            foreach (int side in new[] { -1, 1 })
                foreach (float boundary in new[] { .24f, .76f })
                {
                    float a = NullRefrainMotion.Angle(combo, side, 1, boundary - .0001f);
                    float b = NullRefrainMotion.Angle(combo, side, 1, boundary + .0001f);
                    AssertEqual(true, Math.Abs(a - b) < .00001f, "windup/live/recovery pose continuity");
                }
        AssertEqual(false, NullRefrainMotion.Live(.23999f), "anticipation remains harmless");
        AssertEqual(false, NullRefrainMotion.Live(.76f), "recovery remains harmless");
    }
}
