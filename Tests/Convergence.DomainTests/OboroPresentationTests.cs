using System;
using Convergence.Client.Weapons;
using Convergence.Content.Items.Oboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Oboro sword exists only through an accepted attack and never reappears from an old replica")]
    private static void OboroAttackOnlySword()
    {
        foreach (int facing in new[] { -1, 1 })
        foreach (float speed in new[] { .5f, 1f, 3f })
        for (byte step = 0; step < 3; step++)
        {
            var visual = new OboroSwingPresentation();
            int duration = OboroRules.Duration(step, speed);
            var view = OboroSample with { Facing = (sbyte)facing, Step = step, Duration = (ushort)duration, Swing = 11 };
            visual.Update(view, 0, false, true, 0, 0, facing, 100);
            AssertEqual(false, visual.Swinging, "holding the item does not show a sword");
            AssertEqual(0f, visual.SwordPose.Length, "initial hidden pose");
            for (int frame = 0; frame < duration; frame++)
            {
                visual.Update(view, frame, true, true, 0, 0, facing, (ulong)frame + 101);
                AssertEqual(true, visual.Swinging, "windup cut and follow-through remain visible");
                AssertEqual(132f, visual.SwordPose.Length, "constant hand-sized metal blade");
            }
            visual.Update(view, duration, false, true, 0, 0, facing, (ulong)duration + 101);
            AssertEqual(false, visual.Swinging, "hide immediately when the accepted cut ends");
            AssertEqual(0f, visual.SwordPose.Length, "no lingering equipped sprite");
            visual.Update(view, duration - 3, true, true, 0, 0, facing, (ulong)duration + 102);
            AssertEqual(false, visual.Swinging, "late snapshot cannot resurrect completed weapon");
            visual.Update(view with { Swing = 12 }, 0, true, true, 0, 0, facing, (ulong)duration + 103);
            AssertEqual(true, visual.Swinging, "new accepted click materializes the blade");
            visual.Update(view, 1, true, false, 0, 0, facing, (ulong)duration + 104);
            AssertEqual(false, visual.Swinging, "death swap or invalid binding hides immediately");
            AssertEqual(0, visual.Count, "invalid owner cannot leave echoes");
        }
    }
    [DomainTest("Oboro metal blade stays hand sized with mirrored wrist articulation and unchanged spectral reach")]
    private static void OboroHumanSword()
    {
        var right = new OboroHandBasis(-4, -2, 10, 3, -3, 10);
        var left = new OboroHandBasis(4, -2, 10, -3, 3, 10);
        for (byte step = 0; step < 3; step++)
        {
            var rv = new OboroSwingPresentation(); var lv = new OboroSwingPresentation();
            int duration = OboroComboSettings.For(step).TotalFrames;
            for (int frame = 0; frame < duration * 4; frame++)
            {
                float age = frame / 4f;
                var view = OboroSample with { Step = step, Duration = (ushort)duration, Aim = 0, Facing = 1 };
                rv.Update(view, age, true, true, 0, 0, 1, (ulong)frame, right);
                lv.Update(view with { Aim = MathF.PI, Facing = -1 }, age, true, true, 0, 0, -1, (ulong)frame, left);
                AssertEqual(OboroSwingPresentation.SwordLength, rv.SwordPose.Length, "no inflated PNG at any beat");
                AssertEqual(true, Math.Abs(rv.SwordPose.X + lv.SwordPose.X) < .0001f
                    && Math.Abs(rv.SwordPose.Y - lv.SwordPose.Y) < .0001f, "mirrored hands");
                var grip = right.At(rv.ArmAngle);
                AssertEqual(grip.X, rv.SwordPose.X, "grip on articulated hand x");
                AssertEqual(grip.Y, rv.SwordPose.Y, "grip on articulated hand y");
                AssertEqual(OboroRules.Reach, rv.Pose.Length, "spectral reach stays authoritative");
                AssertEqual(true, Math.Abs(rv.ArmAngle - rv.Pose.Angle) <= .421f, "bounded wrist bend");
            }
            rv.Clear(); AssertEqual(default(OboroBladePose), rv.SwordPose, "no stale hand after cancellation");
        }
    }
    [DomainTest("Oboro live curve accelerates without overshoot or swept-hit gaps")]
    private static void OboroBurstCurve()
    {
        for (int step = 0; step < 3; step++)
        {
            var settings = OboroComboSettings.For(step);
            float start = settings.HitStart, finish = step switch { 0 => OboroFirstSwingMotion.CutEnd / settings.TotalFrames,
                1 => OboroSecondSwingMotion.CutEnd / settings.TotalFrames, _ => OboroThirdSwingMotion.CutEnd / settings.TotalFrames }, largest = 0;
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
                AssertEqual(true, delta <= 64 * OboroRules.BladeWidth / OboroRules.Reach, "within existing collision sampling budget");
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
            if (step < 2) AssertEqual(true, Math.Abs(OboroSwingPresentation.Wrap(end - next)) < .00001f, "no combo pose jump");
            // Finisher retains its follow-through; the next harmless entry bridges it (tested separately).
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
                    var root = OboroRules.RootOffset(step, p, view.Aim, visual.Pose.Angle, default);
                    AssertEqual(age * 5f + root.X, visual.Pose.X, "authoritative origin");
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
        // At release/cancellation the blade disappears immediately, while a
        // still-young harmless spectral residue can finish its bounded fade.
        int lastLive = OboroComboSettings.For(0).HitEndFrame - 1;
        for (int age = 0; age <= lastLive; age++) visual.Update(view, age, true, true, age, 100, 1, (ulong)age + 100);
        AssertEqual(true, visual.Count > 0 && visual.Count <= OboroSwingPresentation.Capacity, "bounded echoes after live window");
        int count = visual.Count;
        visual.Update(view with { Duration = 0 }, 0, false, true, duration, 100, 1, (ulong)lastLive + 101);
        AssertEqual(true, visual.Count > 0 && visual.Count <= count && !visual.Swinging, "end preserves only fading spectral echoes");
        AssertEqual(0f, visual.SwordPose.Length, "no idle or settling weapon");
        visual.Update(view with { Duration = 0 }, 0, false, true, duration, 100, 1, (ulong)duration + 122);
        AssertEqual(0, visual.Count, "expired"); AssertEqual(false, visual.Swinging, "idle reached");
        AssertEqual(0f, visual.SwordPose.Length, "idle weapon stays hidden");
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
