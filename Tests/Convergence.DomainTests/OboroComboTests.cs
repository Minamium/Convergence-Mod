using System;
using System.IO;
using Convergence.Content.Items.Oboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Oboro release on every frame finishes only the current step and restarts at one")]
    private static void OboroReleaseBoundaries()
    {
        for (int releaseStep = 0; releaseStep < 3; releaseStep++)
        for (int releaseFrame = 0; releaseFrame < new[] { 18, 16, 26 }[releaseStep]; releaseFrame++)
        {
            var clock = new OboroTiming(); ulong now = 100;
            clock.SetHeld(true, now); clock.TryBegin(now, 1);
            while (clock.Step < releaseStep)
            { clock.SetHeld(true, now); clock.AdvanceSwing(++now, 1); }
            int total = clock.Duration;
            for (int age = 0; age < total; age++)
            {
                if (age <= releaseFrame) clock.SetHeld(age != releaseFrame, now);
                AssertEqual(age + 1 == total ? OboroAdvance.Finished : OboroAdvance.None,
                    clock.AdvanceSwing(++now, 1), "release must not cut off a live swing or start another");
            }
            AssertEqual(0, clock.Duration, "stopped");
            uint serial = clock.Serial;
            for (int i = 0; i < 100; i++) AssertEqual(OboroAdvance.None, clock.AdvanceSwing(++now, 1), "idle cannot attack");
            AssertEqual(serial, clock.Serial, "idle serial stable");
            clock.SetHeld(true, now); clock.TryBegin(now, 1);
            AssertEqual(0, clock.Step, "new press starts step one");
        }
    }

    [DomainTest("Oboro missing hold refresh expires and speed changes apply only at step boundaries")]
    private static void OboroInputLeaseAndSpeed()
    {
        var clock = new OboroTiming(); ulong now = 100;
        clock.SetHeld(true, now); clock.TryBegin(now, 1);
        for (int i = 0; i < 17; i++) AssertEqual(OboroAdvance.None, clock.AdvanceSwing(++now, 3), "current duration fixed");
        AssertEqual(18, clock.Duration, "speed does not shorten running step");
        AssertEqual(OboroAdvance.NextStep, clock.AdvanceSwing(++now, .5f), "next step");
        AssertEqual(32, clock.Duration, "new speed at boundary");
        int remaining = clock.Duration;
        for (int i = 0; i < remaining; i++)
            AssertEqual(i + 1 == remaining ? OboroAdvance.Finished : OboroAdvance.None,
                clock.AdvanceSwing(++now, 1), "expired input finishes current step only");
        clock.SetHeld(true, now); clock.CancelSwing();
        AssertEqual(false, clock.HeldAt(now), "control loss clears input");
        clock.SetHeld(true, now); clock.Clear();
        AssertEqual(false, clock.HeldAt(now), "death and disconnect clear input");
    }

    [DomainTest("Oboro definitions match requested degrees and exact half-open frame windows")]
    private static void OboroFrameDefinitions()
    {
        int[] total = { 18, 16, 26 }, begin = { 4, 3, 14 }, end = { 13, 12, 23 };
        float[] starts = { 110, -50, 150 }, winds = { 135, -80, 170 }, ends = { -35, 120, -70 };
        for (int step = 0; step < 3; step++)
        {
            var settings = OboroComboSettings.For(step);
            AssertEqual(total[step], settings.TotalFrames, "total frames");
            AssertEqual(starts[step], settings.StartDegrees, "start degrees");
            AssertEqual(winds[step], settings.WindupDegrees, "windup degrees");
            AssertEqual(ends[step], settings.EndDegrees, "end degrees");
            AssertEqual(step == 2 ? 8f : 0f, settings.ForwardDistance, "finisher thrust distance");
            for (int timer = 0; timer <= total[step]; timer++)
                AssertEqual(timer >= begin[step] && timer < end[step],
                    OboroRules.Live(step, timer / (float)total[step]), "base frame window");
            float cutEnd = step switch { 0 => OboroFirstSwingMotion.CutEnd / settings.TotalFrames,
                1 => OboroSecondSwingMotion.CutEnd / settings.TotalFrames, _ => OboroThirdSwingMotion.CutEnd / settings.TotalFrames };
            AssertEqual(true, Math.Abs(OboroRules.Offset(step, cutEnd) - settings.CutEndAngle) < .00001f, "slash end");
        }
    }

    [DomainTest("Oboro held and release requests round-trip and unknown input actions are rejected")]
    private static void OboroHeldRequestCodec()
    {
        foreach (var action in new[] { OboroAction.Hold, OboroAction.Release })
        {
            using var stream = new MemoryStream(); using var w = new BinaryWriter(stream); using var r = new BinaryReader(stream);
            var request = new OboroRequest(action, 5, 17, -.3f);
            request.Write(w); w.Write(123); stream.Position = 0;
            AssertEqual(request, OboroRequest.Read(r), "input request");
            AssertEqual(123, r.ReadInt32(), "next packet untouched");
        }
        using var invalid = new MemoryStream(); using var writer = new BinaryWriter(invalid); using var reader = new BinaryReader(invalid);
        new OboroRequest((OboroAction)5, 5, 17, 0).Write(writer); invalid.Position = 0;
        bool rejected = false; try { OboroRequest.Read(reader); } catch (IOException) { rejected = true; }
        AssertEqual(true, rejected, "unknown action");
    }
}
