using System;
using System.IO;
using Convergence.Content.Encounters.GhostSamurai;
using Convergence.Content.Items.Oboro;
using Convergence.Content.Items.DXOboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Samurai native actor envelope handles absence exact boundaries and malformed payloads")]
    private static void SamuraiNativeEnvelope()
    {
        using var absent = new MemoryStream(); using var absentWriter = new BinaryWriter(absent);
        SamuraiNativeFrame.Write(absentWriter, default, default); absentWriter.Write(9876); absent.Position = 0;
        using var absentReader = new BinaryReader(absent);
        AssertEqual(false, SamuraiNativeFrame.TryRead(absentReader, out _, out _), "unowned NPC is absent, not arena_invalid");
        AssertEqual(1L, absent.Position, "single absent byte");
        var state = new SamuraiActorSnapshot(Guid.NewGuid(), 123, SamuraiPhase.Phase1, SamuraiAttack.DirectionalSlash,
            SamuraiBeat.Telegraph, 2, 0, 2400000, new(6000, 4000, 1280, 560));
        var motion = new SamuraiMotionSample(6000, 4000, 5, -1);
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        SamuraiNativeFrame.Write(writer, state, motion); byte[] bytes = stream.ToArray(); writer.Write(12345); stream.Position = 0;
        using var reader = new BinaryReader(stream);
        AssertEqual(true, SamuraiNativeFrame.TryRead(reader, out var decoded, out var decodedMotion), "present");
        AssertEqual(state, decoded, "actor roundtrip"); AssertEqual(motion, decodedMotion, "motion roundtrip");
        AssertEqual((long)bytes.Length, stream.Position, "no following native message consumed");
        for (int size = 0; size < bytes.Length; size++)
        {
            using var shortStream = new MemoryStream(bytes, 0, size); using var r = new BinaryReader(shortStream);
            bool rejected = false;
            try { SamuraiNativeFrame.TryRead(r, out _, out _); } catch (IOException) { rejected = true; }
            AssertEqual(true, rejected, "native truncation " + size);
        }
        using var invalid = new MemoryStream(new byte[] { 2 }); using var invalidReader = new BinaryReader(invalid);
        bool invalidRejected = false;
        try { SamuraiNativeFrame.TryRead(invalidReader, out _, out _); } catch (InvalidDataException) { invalidRejected = true; }
        AssertEqual(true, invalidRejected, "invalid presence byte");
    }
    [DomainTest("Samurai motion duplicate packets cannot rewind or restart correction")]
    private static void SamuraiMotionDuplicates()
    {
        var motion = new SamuraiClientMotion();
        AssertEqual(true, motion.Accept(100, new(100, 200, 3, -1), 1000), "first");
        AssertEqual((118f, 194f), motion.At(1006), "predict six ticks");
        AssertEqual(true, motion.Accept(106, new(116, 195, 2, 0), 1006), "fresh");
        AssertEqual((118f, 194f), motion.At(1006), "no receive-position snap");
        var midway = motion.At(1009);
        AssertEqual(false, motion.Accept(106, new(400, 400, 20, 20), 1009), "duplicate");
        AssertEqual(false, motion.Accept(99, new(400, 400, 20, 20), 1009), "stale");
        AssertEqual(midway, motion.At(1009), "clock remains intact");
        AssertEqual((128f, 195f), motion.At(1012), "correction converges");
        AssertEqual(motion.At(1024), motion.At(1200), "packet loss freezes bounded prediction");
    }
    [DomainTest("Samurai motion rejects poison and snaps real discontinuities only")]
    private static void SamuraiMotionDiscontinuities()
    {
        var motion = new SamuraiClientMotion();
        motion.Accept(1, new(100, 200, 0, 0), 100);
        AssertEqual(false, motion.Accept(2, new(float.NaN, 0, 0, 0), 101), "poison");
        AssertEqual(1, motion.Age, "no partial mutation");
        motion.Accept(2, new(1000, 2000, 0, 0), 102);
        AssertEqual((1000f, 2000f), motion.At(102), "teleport is not interpolated");
        motion.Accept(3, new(1010, 2020, 0, 0), 200);
        AssertEqual((1010f, 2020f), motion.At(200), "long gap resets");
        motion.Accept(4, new(1015, 2020, 0, 0), 201, discontinuity: true);
        AssertEqual((1015f, 2020f), motion.At(201), "explicit trajectory switch");
    }
    [DomainTest("Samurai motion codec roundtrip shared buffer truncation and finite bounds")]
    private static void SamuraiMotionCodec()
    {
        var sample = new SamuraiMotionSample(1337, 800, -12.5f, 8);
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        sample.Write(writer); byte[] bytes = stream.ToArray(); writer.Write(12345); stream.Position = 0;
        using var reader = new BinaryReader(stream);
        AssertEqual(sample, SamuraiMotionSample.Read(reader), "roundtrip");
        AssertEqual(16L, stream.Position, "bounded native body");
        for (int size = 0; size < 16; size++)
        {
            using var truncated = new MemoryStream(bytes, 0, size); using var r = new BinaryReader(truncated);
            bool rejected = false;
            try { SamuraiMotionSample.Read(r); } catch (EndOfStreamException) { rejected = true; }
            AssertEqual(true, rejected, "truncation");
        }
        foreach (var bad in new[] { new SamuraiMotionSample(float.PositiveInfinity, 0, 0, 0), new(0, 0, 201, 0), new(0, -1000001, 0, 0) })
        {
            using var invalid = new MemoryStream(); using var w = new BinaryWriter(invalid); bad.Write(w); invalid.Position = 0;
            using var r = new BinaryReader(invalid); bool rejected = false;
            try { SamuraiMotionSample.Read(r); } catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "invalid motion");
        }
    }
    [DomainTest("Oboro lost handshake and terminal snapshot recover without permanent input lock")]
    private static void OboroNetworkLease()
    {
        AssertEqual(true, OboroNetworkRules.NeedRepair(0, 120, 0, 120), "missing hello retry");
        AssertEqual(false, OboroNetworkRules.NeedRepair(0, 119, 0, 120), "bounded retry cadence");
        AssertEqual(true, OboroNetworkRules.Fresh(190, 100), "lease inclusive");
        AssertEqual(false, OboroNetworkRules.Fresh(191, 100), "lost terminal cannot lock item indefinitely");
        AssertEqual(true, OboroNetworkRules.NeedRepair(4, 191, 100, 0), "resync stale state");
        AssertEqual(false, OboroNetworkRules.NeedRepair(4, 200, 195, 0), "healthy state no request");
        AssertEqual(false, OboroNetworkRules.Fresh(1, 999), "world-clock reset");
    }
    [DomainTest("DXOboro shared motion is finite mirrored and live only during its release")]
    private static void DXOboroSharedMotion()
    {
        for (int step = 0; step < 3; step++)
        for (int i = 0; i <= DXOboroMotion.Duration * 10; i++)
        {
            float age = i / 10f, aim = .2f;
            float right = DXOboroMotion.Angle(step, age, aim, 1), left = DXOboroMotion.Angle(step, age, aim, -1);
            AssertEqual(true, float.IsFinite(right) && Math.Abs(right + left - aim * 2) < .00001f, "mirrored real curve");
            if (i > 0) AssertEqual(true, Math.Abs(right - DXOboroMotion.Angle(step, age - .1f, aim, 1)) < .16f, "continuous sampled release");
        }
        AssertEqual(false, DXOboroMotion.Live(DXOboroMotion.ReleaseFrame - 1), "warning harmless");
        AssertEqual(true, DXOboroMotion.Live(DXOboroMotion.ReleaseFrame), "release active");
        AssertEqual(true, DXOboroMotion.Live(DXOboroMotion.LiveEndFrame - 1), "last live tick");
        AssertEqual(false, DXOboroMotion.Live(DXOboroMotion.LiveEndFrame), "aftermath harmless");
    }
}
