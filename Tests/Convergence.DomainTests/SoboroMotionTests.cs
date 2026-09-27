using System;
using Convergence.Content.Items.DXOboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Soboro pose knots join all three cuts and return to the same guard")]
    private static void SoboroMotionJoins()
    {
        AssertEqual(18, DXOboroMotion.Duration(0), "first duration");
        AssertEqual(16, DXOboroMotion.Duration(1), "return duration");
        AssertEqual(26, DXOboroMotion.Duration(2), "accent duration");
        AssertEqual(7, DXOboroMotion.Release(0), "first release");
        AssertEqual(6, DXOboroMotion.Release(1), "return release");
        AssertEqual(11, DXOboroMotion.Release(2), "accent release");
        AssertEqual(15, DXOboroMotion.LiveEnd(0), "first end");
        AssertEqual(13, DXOboroMotion.LiveEnd(1), "return end");
        AssertEqual(22, DXOboroMotion.LiveEnd(2), "accent end");

        for (int step = 0; step < 3; step++)
        {
            int next = (step + 1) % 3;
            float exit = DXOboroMotion.Offset(step, DXOboroMotion.Duration(step));
            float entry = DXOboroMotion.Offset(next, 0);
            AssertEqual(true, MathF.Abs(exit - entry) < .00001f, "adjacent guard pose");
            AssertEqual(true, MathF.Abs(DXOboroMotion.BladeLength(step, 0) - 140f) < .00001f,
                "compact arrival");
            AssertEqual(true, MathF.Abs(DXOboroMotion.BladeLength(step, DXOboroMotion.Duration(step)) - 140f) < .00001f,
                "compact handoff");
            float maximumJump = 0;
            for (int i = 1; i <= DXOboroMotion.Duration(step) * 20; i++)
            {
                float t = i / 20f;
                float jump = MathF.Abs(DXOboroMotion.Offset(step, t) - DXOboroMotion.Offset(step, t - .05f));
                maximumJump = MathF.Max(maximumJump, jump);
                float blade = DXOboroMotion.Angle(step, t, .2f, 1);
                float arm = DXOboroMotion.ArmAngle(step, t, .2f, 1);
                AssertEqual(true, float.IsFinite(blade) && float.IsFinite(arm)
                    && MathF.Abs(blade - arm) < .24f, "bounded connected wrist");
            }
            AssertEqual(true, maximumJump < .08f, "120fps trajectory has no pose jump");
            AssertEqual(true, DXOboroMotion.BladeLength(step, DXOboroMotion.Release(step) + 3f) > 180f,
                "physical blade grows after release");
        }
    }
}
