using System;
using Convergence.Client.Weapons;
using Convergence.Content.Items.Oboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Oboro return cut turns below then returns on a smaller arc with five continuous beats")]
    private static void OboroReturnBeats()
    {
        float Angle(float frame) => OboroRules.Offset(1, frame / 18) * 180 / MathF.PI;
        float[] frames = { 0, 3, 6, 11, 15, 18 }, angles = { 90, 102, 60, -40, -55, -65 };
        OboroMotionPhase[] phases = { OboroMotionPhase.Windup, OboroMotionPhase.Acceleration,
            OboroMotionPhase.Cut, OboroMotionPhase.FollowThrough, OboroMotionPhase.Transition };
        for (int i = 0; i < frames.Length; i++)
        {
            AssertEqual(true, Math.Abs(Angle(frames[i]) - angles[i]) < .0001f, "authored return pose");
            if (i < phases.Length) AssertEqual(phases[i], OboroSecondSwingMotion.Phase((frames[i] + .01f) / 18), "named beat");
        }
        const float h = .001f;
        foreach (float f in new[] { 3f, 6, 11, 15 })
            AssertEqual(true, Math.Abs((Angle(f + h) - Angle(f)) / h - (Angle(f) - Angle(f - h)) / h) < .15f, "continuous angular speed");
        AssertEqual(true, Math.Abs(Angle(3.1f) - Angle(2.9f)) < .2f, "brief lower reversal beat");
        float firstPeak = 0, secondPeak = 0;
        for (int i = 0; i < 2200; i++)
        {
            float f = i / 100f;
            firstPeak = Math.Max(firstPeak, Math.Abs(OboroRules.Offset(0, (f + .01f) / 22) - OboroRules.Offset(0, f / 22)));
            if (f >= 3 && f < 18) AssertEqual(true, Angle(f + .01f) <= Angle(f) + .0001f, "single decisive return stroke without recoil reversal");
            secondPeak = Math.Max(secondPeak, Math.Abs(OboroRules.Offset(1, (f + .01f) / 18) - OboroRules.Offset(1, f / 18)));
        }
        AssertEqual(true, secondPeak > firstPeak * .9f && secondPeak < firstPeak * 1.1f, "compact cut retains a sharp release");
        AssertEqual(18, OboroRules.Duration(1, 1), "slightly slower compact return");
    }

    [DomainTest("Oboro return hand and visible live blade agree through mirroring speed and combo joins")]
    private static void OboroReturnAlignment()
    {
        var right = new OboroHandBasis(-4, -2, 10, 3, -3, 10);
        var left = new OboroHandBasis(4, -2, 10, -3, 3, 10);
        foreach (float speed in new[] { .5f, 1f, 2f, 3f })
        {
            var rv = new OboroSwingPresentation(); var lv = new OboroSwingPresentation(); ulong now = 100;
            for (byte step = 0; step < 3; step++)
            {
                int duration = OboroRules.Duration(step, speed);
                for (int tick = 0; tick <= duration * 4; tick++)
                {
                    float age = tick / 4f, p = age / duration;
                    var view = OboroSample with { Step = step, Swing = (uint)step + 1, Duration = (ushort)duration, Aim = 0, Facing = 1 };
                    rv.Update(view, age, true, true, 0, 0, 1, now, right);
                    lv.Update(view with { Aim = MathF.PI, Facing = -1 }, age, true, true, 0, 0, -1, now++, left);
                    AssertEqual(true, Math.Abs(rv.Pose.X + lv.Pose.X) < .00003f && Math.Abs(rv.Pose.Y - lv.Pose.Y) < .00003f, "mirrored hand path");
                    AssertEqual(true, Math.Abs(OboroSwingPresentation.Wrap(lv.Pose.Angle - (MathF.PI - rv.Pose.Angle))) < .00003f, "mirrored blade");
                    if (step != 1 || !OboroRules.Live(step, p)) continue;
                    var root = OboroRules.RootOffset(step, p, 0, OboroRules.Offset(step, p), right);
                    AssertEqual(root.X, rv.Pose.X, "authoritative grip X"); AssertEqual(root.Y, rv.Pose.Y, "authoritative grip Y");
                    AssertEqual(OboroRules.Offset(step, p), rv.Pose.Angle, "authoritative blade rotation");
                }
            }
        }
        foreach (int before in new[] { 0, 1 })
        {
            float end = OboroRules.Offset(before, 1), start = OboroRules.Offset(before + 1, 0);
            AssertEqual(true, Math.Abs(end - start) < .00001f, "identical angle at step boundary");
            AssertEqual(OboroRules.RootOffset(before, 1, 0, end, right), OboroRules.RootOffset(before + 1, 0, 0, start, right), "identical root at step boundary");
        }
    }

    [DomainTest("Oboro return echoes fade sooner than overhead cut and finisher without leaking on cancellation")]
    private static void OboroReturnEchoes()
    {
        AssertEqual(0f, OboroSwingPresentation.Opacity(100, 106, 1), "return afterimage expires at six ticks");
        AssertEqual(true, OboroSwingPresentation.Opacity(100, 106, 0) > 0 && OboroSwingPresentation.Opacity(100, 106, 2) > 0, "other echoes preserved after return fades");
        var visual = new OboroSwingPresentation();
        var view = OboroSample with { Step = 1, Swing = 2, Duration = 18 };
        for (int age = 0; age <= 12; age++) visual.Update(view, age, true, true, 0, 0, 1, (ulong)(100 + age));
        AssertEqual(true, visual.Count > 0 && visual.Count <= OboroSwingPresentation.Capacity, "bounded return history");
        visual.Update(view with { Duration = 0 }, 0, false, true, 0, 0, 1, 125);
        AssertEqual(0, visual.Count, "all return echoes expire");
        visual.Clear(); visual.Clear(); AssertEqual(0, visual.Count, "cancel is idempotent");
    }

    [DomainTest("Oboro faster return stays inside the existing moving-blade sweep budget")]
    private static void OboroReturnSweepCoverage()
    {
        var hand = new OboroHandBasis(-4, -2, 10, 3, -3, 10);
        float Distance((float X, float Y) a, (float X, float Y) b) => MathF.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
        (float X, float Y) Root(float p) => OboroRules.RootOffset(1, p, 0, OboroRules.Offset(1, p), hand);
        for (int duration = OboroRules.Duration(1, 3); duration <= OboroRules.Duration(1, .5f); duration++)
        for (int age = 1; age < duration; age++)
        {
            float end = age / (float)duration, start = Math.Max(OboroRules.Windup(1), (age - 1f) / duration);
            if (!OboroRules.Live(1, end)) continue;
            int samples = OboroSecondSwingMotion.SweepSamples(start, end, Distance(Root(start), Root(end)));
            AssertEqual(true, samples >= 1 && samples <= 64, "unchanged maximum collision budget");
            float previous = start;
            for (int sample = 1; sample <= samples; sample++)
            {
                float p = start + (end-start)*sample/samples;
                float tipTravel = OboroRules.Reach * Math.Abs(OboroRules.Offset(1, p)-OboroRules.Offset(1, previous)) + Distance(Root(p), Root(previous));
                AssertEqual(true, tipTravel < OboroRules.BladeWidth, "sweep slices overlap even at maximum speed");
                previous = p;
            }
        }
    }
}
