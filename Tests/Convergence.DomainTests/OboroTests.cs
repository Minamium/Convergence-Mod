using System;
using System.IO;
using Convergence.Content.Items.Oboro;
using Convergence.Client.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Oboro cycles 1 2 3 exactly and rejects overlapping starts")]
    private static void OboroCycleSequence()
    {
        var clock = new OboroTiming(); ulong now = 200;
        for (int i = 0; i < 12; i++)
        {
            AssertEqual(true, clock.TryBegin(now, 1), "start"); AssertEqual(i % 3, clock.Step, "three-step loop");
            AssertEqual(false, clock.TryBegin(now, 1), "duplicate overlap");
            int total = clock.Duration;
            for (int tick = 1; tick <= total; tick++) AssertEqual(tick == total, clock.AdvanceSwing(++now), "one finish");
            AssertEqual(false, clock.AdvanceSwing(++now), "no duplicate finish");
        }
        clock.TryBegin(now + 91, 1); AssertEqual(0, clock.Step, "idle resets combo");
    }
    [DomainTest("Oboro manual and timed Zanshin detonate once then clear on death or exit")]
    private static void OboroZanshinLifecycle()
    {
        var clock = new OboroTiming();
        AssertEqual(OboroToggle.Started, clock.Toggle(100), "start");
        AssertEqual(OboroToggle.Rejected, clock.Toggle(101), "toggle debounce");
        for (int tick = 1; tick <= 300; tick++) AssertEqual(tick == 300, clock.TickZanshin(), "expiry exactly once");
        AssertEqual(false, clock.TickZanshin(), "expired");
        clock.Toggle(500); AssertEqual(OboroToggle.Detonate, clock.Toggle(520), "manual detonation");
        AssertEqual(false, clock.TickZanshin(), "no expiry burst after manual");
        clock.Toggle(600); clock.TryBegin(600, 1); clock.Clear(); clock.Clear();
        AssertEqual(0, clock.Zanshin, "buff cleared"); AssertEqual(0, clock.Duration, "swing cleared");
        AssertEqual(false, clock.TickZanshin(), "no death burst");
        clock.Initialize(); AssertEqual(OboroToggle.Started, clock.Toggle(0), "world tick reset");
    }
    [DomainTest("Oboro every attack speed retains a windup and bounded live window")]
    private static void OboroMotionWindows()
    {
        foreach (float speed in new[] { .1f, .5f, 1, 2, 3, 100 })
        for (int step = 0; step < 3; step++)
        {
            int duration = OboroRules.Duration(step, speed), live = 0;
            AssertEqual(true, duration is >= 8 and <= 84, "duration bound");
            AssertEqual(false, OboroRules.Live(step, 0), "windup harmless");
            AssertEqual(false, OboroRules.Live(step, 1), "recovery harmless");
            for (int i = 0; i < duration; i++) if (OboroRules.Live(step, i / (float)duration)) live++;
            AssertEqual(true, live > 0 && live < duration, "finite live ticks");
        }
        AssertEqual(true, OboroRules.Windup(2) > OboroRules.Windup(0), "heavy anticipation");
    }
    [DomainTest("Oboro return cut reverses direction and poses are continuous at boundaries")]
    private static void OboroMotionContinuity()
    {
        for (int i = 0; i <= 100; i++)
            AssertEqual(OboroRules.Offset(0, i / 100f), -OboroRules.Offset(1, i / 100f), "return direction");
        for (int step = 0; step < 3; step++)
        foreach (float at in new[] { OboroRules.Windup(step), .8f })
            AssertEqual(true, Math.Abs(OboroRules.Offset(step, at - .0001f) - OboroRules.Offset(step, at + .0001f)) < .005f, "connected pose");
    }
    [DomainTest("Oboro wounds cap at five without overwriting stored hit angles")]
    private static void OboroWoundCap()
    {
        var a = new OboroWounds(3, 71); var b = new OboroWounds(3, 72);
        for (int i = 0; i < 20; i++) AssertEqual(i < 5, a.Add(i * .2f, 8400 + i), "cap");
        AssertEqual(5, a.Marks.Count, "count"); AssertEqual(0, b.Marks.Count, "new NPC identity empty");
        AssertEqual(8400, a.Marks[0].Damage, "snapshot potency");
        AssertEqual(false, b.Add(float.NaN, 10), "NaN"); AssertEqual(false, b.Add(0, 0), "zero damage");
        for (int i = 0; i < 5; i++) AssertEqual(true, Math.Abs(OboroRules.PhantomAngle(a.Marks[i].Angle, i) - a.Marks[i].Angle) > 1, "different angle");
    }
    [DomainTest("Oboro Wraith Fire follows max-life formula and cannot overflow regen")]
    private static void OboroFireDamage()
    {
        AssertEqual(220, OboroRules.FireDps(1000), "small enemy");
        AssertEqual(48200, OboroRules.FireDps(2400000), "boss percent damage uncapped");
        AssertEqual(200, OboroRules.FireDps(0), "base");
        AssertEqual(true, (long)OboroRules.FireDps(int.MaxValue) * 2 < int.MaxValue, "native regen bound");
    }
    private static OboroSnapshot OboroSample => new(1, 30, 12, 6, 2, 20, 42, .75f, -1, 180);
    [DomainTest("Oboro snapshots reject stale revisions and replaced connections")]
    private static void OboroStaleState()
    {
        var v = OboroSample;
        AssertEqual(false, v.NewerThan(v), "duplicate");
        AssertEqual(false, (v with { Revision = 11 }).NewerThan(v), "old revision");
        AssertEqual(false, (v with { Generation = 29, Revision = 999 }).NewerThan(v), "old connection");
        AssertEqual(true, (v with { Generation = 31, Revision = 1 }).NewerThan(v), "new connection");
        AssertEqual(false, (v with { Aim = float.NaN }).Valid, "invalid aim");
        AssertEqual(false, (v with { Age = 43 }).Valid, "overrun");
        AssertEqual(false, (v with { Zanshin = 301 }).Valid, "unbounded buff");
    }
    [DomainTest("Oboro request snapshot and five-angle burst round-trip exactly")]
    private static void OboroCodecRoundTrip()
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); using var reader = new BinaryReader(stream);
        var request = new OboroRequest(OboroAction.Swing, 30, 17, -2.1f);
        request.Write(writer); OboroSample.Write(writer); new OboroBurst(100, 300, new[] { 0f, 1, 2, 3, 4 }).Write(writer);
        stream.Position = 0;
        AssertEqual(request, OboroRequest.Read(reader), "request"); AssertEqual(OboroSample, OboroSnapshot.Read(reader), "snapshot");
        var burst = OboroBurst.Read(reader); AssertEqual(5, burst.Angles.Length, "bounded angles");
        AssertEqual(4f, burst.Angles[4], "last angle"); AssertEqual(stream.Length, stream.Position, "exact consumption");
    }
    [DomainTest("Oboro wire rejects every truncation and unbounded burst allocation")]
    private static void OboroCodecTruncation()
    {
        Check(w => new OboroRequest(OboroAction.Zanshin, 3, 1, 0).Write(w), r => { OboroRequest.Read(r); });
        Check(w => OboroSample.Write(w), r => { OboroSnapshot.Read(r); });
        Check(w => new OboroBurst(1, 2, new[] { 0f, 1, 2, 3, 4 }).Write(w), r => { OboroBurst.Read(r); });
        using var invalid = new MemoryStream(); using var w = new BinaryWriter(invalid);
        w.Write(1f); w.Write(2f); w.Write((byte)255); invalid.Position = 0;
        using var r = new BinaryReader(invalid);
        bool rejected = false; try { OboroBurst.Read(r); } catch (IOException) { rejected = true; }
        AssertEqual(true, rejected, "count rejected before array/body");
        static void Check(Action<BinaryWriter> write, Action<BinaryReader> read)
        {
            using var full = new MemoryStream(); using var writer = new BinaryWriter(full); write(writer); byte[] bytes = full.ToArray();
            for (int size = 0; size < bytes.Length; size++)
            {
                using var fragment = new MemoryStream(bytes, 0, size); using var reader = new BinaryReader(fragment);
                bool rejected = false; try { read(reader); } catch (IOException) { rejected = true; }
                AssertEqual(true, rejected, "truncation " + size);
            }
        }
    }
    [DomainTest("Oboro shared-buffer decoding consumes only its declared fields")]
    private static void OboroSharedBuffer()
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); using var reader = new BinaryReader(stream);
        OboroSample.Write(writer); long boundary = stream.Position;
        writer.Write(new byte[128]); stream.Position = 0;
        AssertEqual(OboroSample, OboroSnapshot.Read(reader), "accepted within shared buffer");
        AssertEqual(boundary, stream.Position, "next packet untouched");
    }
    [DomainTest("Ghost Samurai violet atlas cells stay in bounds at nondivisible dimensions")]
    private static void SamuraiVioletFrames()
    {
        foreach (int size in new[] { 1254, 1280, 1536 })
        for (int i = 0; i < 12; i++)
        {
            var c = SamuraiSpriteFrames.Cell(i, size, size);
            AssertEqual(true, c.X >= 0 && c.Y >= 0 && c.X + c.Width <= size && c.Y + c.Height <= size, "source bounds");
        }
    }
}
