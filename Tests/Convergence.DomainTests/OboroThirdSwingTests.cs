using System;
using Convergence.Client.Weapons;
using Convergence.Content.Items.Oboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Oboro finisher holds still then releases faster than both opening cuts")]
    private static void OboroFinisherBeats()
    {
        float Angle(float f) => OboroRules.Offset(2, f / 26) * 180 / MathF.PI;
        float[] frames = { 0, 8, 14, 18, 22, 26 }, angles = { 150, 170, 170, 60, -70, -95 };
        for (int i = 0; i < frames.Length; i++)
            AssertEqual(true, Math.Abs(Angle(frames[i]) - angles[i]) < .0001f, "authored finisher pose");
        foreach (float f in new[] { 8f, 14, 18, 22 })
        {
            const float h = .001f;
            AssertEqual(true, Math.Abs((Angle(f + h) - Angle(f)) / h - (Angle(f) - Angle(f - h)) / h) < .2f, "connected angular velocity");
        }
        for (float f = 8; f < 14; f += .125f)
        {
            AssertEqual(OboroRules.Offset(2, 8f / 26), OboroRules.Offset(2, f / 26), "held blade angle");
            AssertEqual(-4f, OboroThirdSwingMotion.Forward(f / 26), "held grip");
            AssertEqual(false, OboroRules.Live(2, f / 26), "charge cannot damage");
            AssertEqual(OboroMotionPhase.Charge, OboroThirdSwingMotion.Phase(f / 26), "explicit charge beat");
        }
        float Peak(int step)
        {
            float peak = 0, total = OboroComboSettings.For(step).TotalFrames;
            for (float f = 0; f < total; f += .01f)
                peak = Math.Max(peak, Math.Abs(OboroRules.Offset(step, (f + .01f) / total) - OboroRules.Offset(step, f / total)));
            return peak;
        }
        AssertEqual(true, Peak(2) > Math.Max(Peak(0), Peak(1)) * 1.4f, "explosive release, not just a longer cut");
        AssertEqual(8f, OboroThirdSwingMotion.Forward(18f / 26), "largest forward impulse");
        AssertEqual(0f, OboroThirdSwingMotion.Forward(1), "no accumulated displacement");
        AssertEqual(26, OboroRules.Duration(2, 1), "unchanged total frames");
    }

    [DomainTest("Oboro finisher entry settles before the hold and looping first cut inherits its pose")]
    private static void OboroFinisherHandoff()
    {
        foreach (int facing in new[] { -1, 1 })
        {
            var visual = new OboroSwingPresentation(); ulong now = 100;
            var view = OboroSample with { Step = 2, Swing = 3, Duration = 26, Aim = .9f, Facing = (sbyte)facing };
            for (int f = 0; f <= 26; f++)
            {
                visual.Update(view, f, true, true, 0, 0, facing, now++);
                if (f >= 8)
                    AssertEqual(view.Aim + facing * OboroRules.Offset(2, f / 26f), visual.Pose.Angle, "aim correction must finish before charge");
            }
            var end = visual.Pose;
            view = view with { Step = 0, Swing = 4, Duration = 18, Aim = -.4f };
            visual.Update(view, 0, true, true, 0, 0, facing, now++);
            AssertEqual(true, Math.Abs(OboroSwingPresentation.Wrap(end.Angle - visual.Pose.Angle)) < .000001f, "no angular pop on loop with changed aim");
            AssertEqual(end.X, visual.Pose.X, "same loop root");
            visual.Update(view, 4, true, true, 0, 0, facing, now++);
            AssertEqual(view.Aim + facing * OboroRules.Offset(0, 4f / 18), visual.Pose.Angle, "live first cut rejoined server");
        }
    }

    [DomainTest("Oboro finisher mirrored moving grip and live blade match authority at every supported speed")]
    private static void OboroFinisherAlignment()
    {
        var right = new OboroHandBasis(-4, -2, 10, 3, -3, 10);
        var left = new OboroHandBasis(4, -2, 10, -3, 3, 10);
        for (int duration = OboroRules.Duration(2, 3); duration <= OboroRules.Duration(2, .5f); duration++)
        {
            var rv = new OboroSwingPresentation(); var lv = new OboroSwingPresentation();
            for (int tick = 0; tick <= duration * 4; tick++)
            {
                float age = tick / 4f, p = age / duration;
                var view = OboroSample with { Step = 2, Swing = 3, Duration = (ushort)duration, Aim = 0, Facing = 1 };
                rv.Update(view, age, true, true, 0, 0, 1, (ulong)tick + 100, right);
                lv.Update(view with { Aim = MathF.PI, Facing = -1 }, age, true, true, 0, 0, -1, (ulong)tick + 100, left);
                // Fresh remote third-step pose may start at a different idle angle; entry absorbs it.
                if (p < OboroRules.EntryEnd(2)) continue;
                AssertEqual(true, Math.Abs(rv.Pose.X + lv.Pose.X) < .0001f && Math.Abs(rv.Pose.Y - lv.Pose.Y) < .0001f, "mirrored root");
                if (!OboroRules.Live(2, p)) continue;
                var root = OboroRules.RootOffset(2, p, 0, OboroRules.Offset(2, p), right);
                AssertEqual(root.X, rv.Pose.X, "authority X"); AssertEqual(root.Y, rv.Pose.Y, "authority Y");
                AssertEqual(OboroRules.Offset(2, p), rv.Pose.Angle, "authority angle");
                AssertEqual(OboroRules.Reach, rv.Pose.Length, "authority reach");
            }
        }
    }

    [DomainTest("Oboro explosive finisher sweeps stay overlapping within sixty-four samples")]
    private static void OboroFinisherCoverage()
    {
        var hand = new OboroHandBasis(-4, -2, 10, 3, -3, 10);
        float Distance((float X, float Y) a, (float X, float Y) b) => MathF.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
        (float X, float Y) Root(float p) => OboroRules.RootOffset(2, p, 0, OboroRules.Offset(2, p), hand);
        for (int duration = OboroRules.Duration(2, 3); duration <= OboroRules.Duration(2, .5f); duration++)
        for (int age = 1; age < duration; age++)
        {
            float end = age / (float)duration, start = Math.Max(OboroRules.Windup(2), (age - 1f) / duration);
            if (!OboroRules.Live(2, end)) continue;
            int count = OboroThirdSwingMotion.SweepSamples(start, end, Distance(Root(start), Root(end)));
            AssertEqual(true, count >= 1 && count <= 64, "bounded collision work");
            float previous = start;
            for (int sample = 1; sample <= count; sample++)
            {
                float p = start + (end - start) * sample / count;
                float gap = OboroRules.Reach * Math.Abs(OboroRules.Offset(2, p) - OboroRules.Offset(2, previous)) + Distance(Root(p), Root(previous));
                AssertEqual(true, gap < OboroRules.BladeWidth, "no gap between swept blade samples");
                previous = p;
            }
        }
    }
}
