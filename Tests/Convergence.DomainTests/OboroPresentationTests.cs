using System;
using Convergence.Client.Weapons;
using Convergence.Content.Items.Oboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Oboro live curve accelerates without overshoot or swept-hit gaps")]
    private static void OboroBurstCurve()
    {
        for (int step = 0; step < 3; step++)
        {
            var settings = OboroComboSettings.For(step);
            float start = settings.HitStart, finish = step switch { 0 => OboroFirstSwingMotion.CutEnd / settings.TotalFrames,
                1 => OboroSecondSwingMotion.CutEnd / settings.TotalFrames, _ => settings.HitEnd }, largest = 0;
            int sign = Math.Sign(settings.CutEndAngle - settings.WindupAngle);
            float previous = sign * settings.WindupAngle;
            for (int i = 0; i <= 1000; i++)
            {
                float angle = sign * OboroRules.Offset(step, start + (finish - start) * i / 1000);
                AssertEqual(true, angle >= previous - .00001f && angle <= sign * settings.CutEndAngle + .00001f, "monotone within defined arc");
                largest = Math.Max(largest, angle - previous); previous = angle;
            }
            AssertEqual(true, largest > Math.Abs(settings.CutEndAngle - settings.WindupAngle) / 1000 * 1.4f, "eased middle");
            for (int duration = OboroRules.Duration(step, 3); duration <= OboroRules.Duration(step, .5f); duration++)
            for (int age = 1; age < duration; age++)
            {
                float p = age / (float)duration;
                if (!OboroRules.Live(step, p)) continue;
                float delta = Math.Abs(OboroRules.Offset(step, p)
                    - OboroRules.Offset(step, Math.Max(start, (age - 1f) / duration)));
                AssertEqual(true, delta <= 64 * .035f, "within existing collision sampling budget");
            }
        }
    }
    [DomainTest("Oboro piecewise phases and three-to-one handoff have connected angular velocity")]
    private static void OboroConnectedPhases()
    {
        const float h = .0001f;
        for (int step = 0; step < 3; step++)
        {
            float windup = OboroRules.Windup(step);
            foreach (float at in new[] { windup, OboroComboSettings.For(step).HitEnd })
            {
                float center = OboroRules.Offset(step, at);
                float left = (center - OboroRules.Offset(step, at - h)) / h;
                float right = (OboroRules.Offset(step, at + h) - center) / h;
                AssertEqual(true, Math.Abs(left - right) < .15f, "no angular-velocity seam");
            }
            float end = OboroRules.Offset(step, 1), next = OboroRules.Offset((step + 1) % 3, 0);
            AssertEqual(true, Math.Abs(OboroSwingPresentation.Wrap(end - next)) < .00001f, "no combo pose jump");
        }
    }
    [DomainTest("Oboro visual blade exactly matches authoritative live aim and reach")]
    private static void OboroVisualAuthorityAlignment()
    {
        foreach (int facing in new[] { -1, 1 })
        foreach (float speed in new[] { .5f, 1f, 2f, 3f })
        {
            var visual = new OboroSwingPresentation(); ulong now = 100;
            for (byte step = 0; step < 3; step++)
            {
                int duration = OboroRules.Duration(step, speed);
                var view = OboroSample with { Step = step, Swing = (uint)(step + 1), Duration = (ushort)duration, Facing = (sbyte)facing, Aim = .7f + step };
                for (int age = 0; age < duration; age++)
                {
                    visual.Update(view, age, true, true, age * 5, 200, facing, now++);
                    float p = age / (float)duration;
                    if (!OboroRules.Live(step, p)) continue;
                    AssertEqual(view.Aim + facing * OboroRules.Offset(step, p), visual.Pose.Angle, "server angle");
                    AssertEqual(OboroRules.Reach, visual.Pose.Length, "server length");
                    AssertEqual(age * 5f, visual.Pose.X, "authoritative origin");
                }
            }
        }
    }
    [DomainTest("Oboro afterimages survive recovery then fade to zero with bounded storage")]
    private static void OboroEchoLifetime()
    {
        var visual = new OboroSwingPresentation();
        int duration = OboroRules.Duration(0, 1);
        var view = OboroSample with { Step = 0, Duration = (ushort)duration, Swing = 1 };
        for (int age = 0; age < duration; age++) visual.Update(view, age, true, true, age, 100, 1, (ulong)age + 100);
        AssertEqual(true, visual.Count > 0 && visual.Count <= OboroSwingPresentation.Capacity, "bounded echoes after live window");
        int count = visual.Count;
        visual.Update(view with { Duration = 0 }, 0, false, true, duration, 100, 1, (ulong)duration + 100);
        AssertEqual(true, visual.Count > 0 && visual.Count <= count && visual.Settling, "normal end preserves fading echoes");
        visual.Update(view with { Duration = 0 }, 0, false, true, duration, 100, 1, (ulong)duration + 122);
        AssertEqual(0, visual.Count, "expired"); AssertEqual(false, visual.Settling, "idle reached");
        AssertEqual(145f, visual.Pose.Length, "idle scale");
        float opacity = 1;
        for (ulong tick = 0; tick <= 15; tick++)
        {
            float next = OboroSwingPresentation.Opacity(100, 100 + tick);
            AssertEqual(true, next <= opacity && next >= 0, "monotone alpha"); opacity = next;
        }
        AssertEqual(0f, opacity, "no permanent ghost");
    }
    [DomainTest("Oboro history clears for replacement players teleport death and weapon changes")]
    private static void OboroEchoCleanup()
    {
        foreach (int cause in new[] { 0, 1, 2 })
        {
            var visual = new OboroSwingPresentation(); var view = OboroSample with { Step = 0, Duration = 24 };
            for (int age = 4; age <= 8; age++) visual.Update(view, age, true, true, 100, 200, 1, (ulong)age + 100);
            AssertEqual(true, visual.Count > 0, "seed echoes");
            visual.Update(cause == 0 ? view with { Generation = view.Generation + 1 } : view,
                0, false, cause != 2, cause == 1 ? 1000 : 100, 200, 1, 109);
            AssertEqual(0, visual.Count, "replacement teleport or ineligible clears immediately");
            visual.Clear(); visual.Clear(); AssertEqual(0, visual.Count, "idempotent world cleanup");
        }
    }
    [DomainTest("Oboro late snapshots cannot rewind or duplicate an echo and new cuts keep identity")]
    private static void OboroEchoSnapshots()
    {
        var visual = new OboroSwingPresentation(); var view = OboroSample with { Step = 0, Duration = 24, Swing = 7 };
        visual.Update(view, 10, true, true, 100, 200, 1, 100);
        float angle = visual.Pose.Angle;
        visual.Update(view, 9, true, true, 100, 200, 1, 101);
        AssertEqual(angle, visual.Pose.Angle, "late correction does not rewind");
        AssertEqual(1, visual.Count, "no duplicate echo for one pose");
        visual.Update(view with { Step = 1, Swing = 8 }, 10, true, true, 100, 200, 1, 102);
        AssertEqual(2, visual.Count, "old cut fades alongside new one");
        AssertEqual(7u, visual.Echo(0).Swing, "old identity"); AssertEqual(8u, visual.Echo(1).Swing, "new identity");
        view = view with { Step = 1, Swing = 8 };
        visual.Update(view, 24, false, true, 100, 200, 1, 103);
        visual.Update(view, 18, true, true, 100, 200, 1, 104);
        AssertEqual(false, visual.Swinging, "completed serial cannot replay on delayed clock");
        AssertEqual(2, visual.Count, "no revived afterimage");
        visual.Update(view with { Step = 2, Swing = 9 }, 0, true, true, 100, 200, 1, 105);
        AssertEqual(true, visual.Swinging, "next serial starts normally");
    }
}
