using System;
using Convergence.Content.Items.DXOboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Soboro flowing kata keeps each live sweep and never brakes abruptly")]
    private static void SoboroFlowingKata()
    {
        // Gameplay contract: the swept arc during each hit window is unchanged.
        float[] releaseAngle = { -1.58f, 1.32f, -1.72f };
        float[] endAngle = { 1.17f, -1.13f, 1.75f };
        for (int step = 0; step < 3; step++)
        {
            AssertEqual(true, MathF.Abs(DXOboroMotion.Offset(step, DXOboroMotion.Release(step)) - releaseAngle[step]) < .0001f,
                "live sweep starts at the accepted angle");
            AssertEqual(true, MathF.Abs(DXOboroMotion.Offset(step, DXOboroMotion.LiveEnd(step)) - endAngle[step]) < .0001f,
                "live sweep ends at the accepted angle");
            int next = (step + 1) % 3;
            float exitSpeed = Speed(step, DXOboroMotion.Duration(step) - .001f);
            float entrySpeed = Speed(next, .001f);
            AssertEqual(true, MathF.Abs(exitSpeed - entrySpeed) < .01f, "momentum carries across the join");
            float peakAcceleration = 0;
            for (int i = 1; i < DXOboroMotion.Duration(step) * 20; i++)
            {
                float t = i / 20f;
                float acceleration = (DXOboroMotion.Offset(step, t + .05f) - 2 * DXOboroMotion.Offset(step, t)
                    + DXOboroMotion.Offset(step, t - .05f)) / (.05f * .05f);
                peakAcceleration = MathF.Max(peakAcceleration, MathF.Abs(acceleration));
            }
            AssertEqual(true, peakAcceleration < .3f, "no dead stop or snap at the swing ends");
        }

        static float Speed(int step, float t) => (DXOboroMotion.Offset(step, t + .001f)
            - DXOboroMotion.Offset(step, t - .001f)) / .002f;
    }

    [DomainTest("Soboro pose knots join all three cuts with the same pose")]
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
            // The finisher loops around the back, so compare the pose, not the raw angle.
            float gap = MathF.IEEERemainder(exit - entry, MathF.Tau);
            AssertEqual(true, MathF.Abs(gap) < .0001f, "adjacent pose");
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
