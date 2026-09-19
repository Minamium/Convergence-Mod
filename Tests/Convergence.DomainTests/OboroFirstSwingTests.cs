using System;
using Convergence.Client.Weapons;
using Convergence.Content.Items.Oboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Oboro first cut has five connected beats with fast release and quiet follow-through")]
    private static void OboroFirstBeats()
    {
        float Angle(float frame) => OboroRules.Offset(0, frame / 18) * 180 / MathF.PI;
        float[] frames = { 0, 4, 8, 12, 16, 18 }, angles = { 110, 135, 65, -35, -47, -50 };
        for (int i = 0; i < frames.Length; i++)
            AssertEqual(true, Math.Abs(Angle(frames[i]) - angles[i]) < .0001f, "authored knot");
        OboroMotionPhase[] phases = { OboroMotionPhase.Windup, OboroMotionPhase.Acceleration,
            OboroMotionPhase.Cut, OboroMotionPhase.FollowThrough, OboroMotionPhase.Transition };
        for (int i = 0; i < phases.Length; i++)
            AssertEqual(phases[i], OboroFirstSwingMotion.Phase((frames[i] + .01f) / 18), "half-open named phase");
        const float h = .001f;
        foreach (float at in new[] { 4f, 8, 12, 16 })
            AssertEqual(true, Math.Abs((Angle(at + h) - Angle(at)) / h - (Angle(at) - Angle(at - h)) / h) < .12f, "continuous angular velocity");
        float peak = 0, prep = 0, tail = 0;
        for (int i = 0; i < 1800; i++)
        {
            float f = i / 100f, speed = Math.Abs(Angle(f + .01f) - Angle(f));
            if (f < 4) prep = Math.Max(prep, speed);
            else if (f < 12) peak = Math.Max(peak, speed);
            else tail = Math.Max(tail, speed);
        }
        AssertEqual(true, peak > prep * 3 && peak > tail * 5, "sharp acceleration and lingering return");
        AssertEqual(18, OboroRules.Duration(0, 1), "unchanged duration");
    }

    [DomainTest("Oboro first cut hand motion and visible live blade mirror the authority exactly")]
    private static void OboroFirstHandAlignment()
    {
        var right = new OboroHandBasis(-4, -2, 10, 3, -3, 10);
        var left = new OboroHandBasis(4, -2, 10, -3, 3, 10);
        foreach (float speed in new[] { .5f, 1f, 2f, 3f })
        {
            int duration = OboroRules.Duration(0, speed);
            var rv = new OboroSwingPresentation(); var lv = new OboroSwingPresentation();
            for (int age = 0; age < duration; age++)
            {
                var view = OboroSample with { Step = 0, Duration = (ushort)duration, Aim = 0, Facing = 1 };
                rv.Update(view, age, true, true, 0, 0, 1, (ulong)age, right);
                lv.Update(view with { Aim = MathF.PI, Facing = -1 }, age, true, true, 0, 0, -1, (ulong)age, left);
                AssertEqual(true, Math.Abs(rv.Pose.X + lv.Pose.X) < .00002f && Math.Abs(rv.Pose.Y - lv.Pose.Y) < .00002f, "mirrored roots");
                AssertEqual(true, Math.Abs(OboroSwingPresentation.Wrap(lv.Pose.Angle - (MathF.PI - rv.Pose.Angle))) < .00002f, "mirrored blade");
                float p = age / (float)duration;
                if (!OboroRules.Live(0, p)) continue;
                var root = OboroRules.RootOffset(0, p, 0, OboroRules.Offset(0, p), right);
                AssertEqual(root.X, rv.Pose.X, "exact server root X"); AssertEqual(root.Y, rv.Pose.Y, "exact server root Y");
                AssertEqual(OboroRules.Offset(0, p), rv.Pose.Angle, "exact server angle");
            }
        }
        var back = OboroRules.RootOffset(0, 4f / 18, 0, OboroRules.Offset(0, 4f / 18), right);
        var front = OboroRules.RootOffset(0, 10f / 18, 0, OboroRules.Offset(0, 10f / 18), right);
        AssertEqual(true, front.X > back.X + 14, "small forward hand stroke after pullback");
        AssertEqual(0f, OboroFirstSwingMotion.HandWeight(1), "root meets existing second step");
    }
}
