using System;
using System.IO;
using System.Text;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Client.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Raid hit receipt binds the owner Fight and rejects replay/stale/zero damage")]
    private static void RaidHitReceiptIdentity()
    {
        FightId fight = CreateContext(2).FightId;
        var header = new EncounterPacketHeader(EncounterProtocol.CurrentVersion, EncounterPacketType.RaidHit, 7, fight, 1);
        FirstSeveranceHitReceipt receipt = default;
        AssertEqual(false, receipt.TryAccept(header, FightId.None, 7, 120), "inactive/cleared player");
        AssertEqual(false, receipt.TryAccept(header, fight, 8, 120), "different session");
        AssertEqual(false, receipt.TryAccept(header with { FightId = FightId.None }, fight, 7, 120), "foreign Fight");
        AssertEqual(false, receipt.TryAccept(header with { Revision = 0 }, fight, 7, 120), "zero serial");
        AssertEqual(false, receipt.TryAccept(header, fight, 7, 0), "successful mechanic is not a hit");
        AssertEqual(false, receipt.TryAccept(header with { ProtocolVersion = 23 }, fight, 7, 120), "old version");
        AssertEqual(true, receipt.TryAccept(header, fight, 7, 120), "first accepted hit");
        AssertEqual(false, receipt.TryAccept(header, fight, 7, 120), "duplicate HP snapshot/event cannot reset again");
        AssertEqual(true, receipt.TryAccept(header with { Revision = 2 }, fight, 7, 700), "lethal-to-Down hit");
        AssertEqual(false, receipt.TryAccept(header, fight, 7, 120), "stale hit");
        receipt = default; // ClearRaidState, including world/terminal cleanup.
        AssertEqual(true, receipt.TryAccept(header, fight, 7, 120), "fresh owner cursor");
    }

    [DomainTest("Raid hit codec is bounded and positive-only, including truncated payloads")]
    private static void RaidHitCodecBounds()
    {
        var header = new EncounterPacketHeader(EncounterProtocol.CurrentVersion, EncounterPacketType.RaidHit,
            7, CreateContext(2).FightId, 3);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        var intent = new FirstSeveranceHurtIntent(4, 120, FirstSeveranceHurtKind.Hazard);
        FirstSeverancePacketCodec.WriteRaidHit(writer, header, intent);
        long end = stream.Position;
        writer.Write(0x12345678);
        stream.Position = 0;
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        AssertEqual(true, EncounterPacketCodec.TryReadHeader(reader, out var actual, out _), "header");
        AssertEqual(header, actual, "Fight and hit serial");
        AssertEqual(true, EncounterRouteCodec.TryRead(reader, out string route), "route");
        AssertEqual(FirstSeveranceIdentity.EncounterKey, route, "definition route");
        AssertEqual(true, FirstSeverancePacketCodec.TryReadRaidHit(reader, out var actualIntent, out _), "hit");
        AssertEqual(intent, actualIntent, "source damage and recovery generation");
        AssertEqual(end, stream.Position, "shared stream remains intact");
        for (int length = 0; length < 9; length++)
        {
            using var shortStream = new MemoryStream(new byte[length]);
            using var shortReader = new BinaryReader(shortStream);
            AssertThrows<EndOfStreamException>(() => FirstSeverancePacketCodec.TryReadRaidHit(shortReader, out _, out _), "truncated hit");
        }
        foreach (int invalid in new[] { 0, -1, int.MinValue, FirstSeveranceHurtIntent.MaximumDamage + 1 })
        {
            using var invalidStream = new MemoryStream();
            using var invalidWriter = new BinaryWriter(invalidStream, Encoding.UTF8, true);
            invalidWriter.Write(0u); invalidWriter.Write(invalid); invalidWriter.Write((byte)0);
            invalidStream.Position = 0;
            using var invalidReader = new BinaryReader(invalidStream);
            AssertEqual(false, FirstSeverancePacketCodec.TryReadRaidHit(invalidReader, out _, out _), "non-hit");
        }
    }

    [DomainTest("Raid casting has fast arrival, brake and final load without moving fire")]
    private static void RaidCastingContrast()
    {
        foreach (double span in new[] { 24d, 48d, 108d, 180d })
        {
            AssertEqual(0f, FirstSeveranceVisualCurves.CastTension(-1, 0, span), "before start");
            float early = FirstSeveranceVisualCurves.CastTension(span * .22, 0, span);
            float settled = FirstSeveranceVisualCurves.CastTension(span * .58, 0, span);
            AssertEqual(true, early > settled + .10f, "clear settling beat");
            AssertEqual(true, FirstSeveranceVisualCurves.CastTension(span - .01, 0, span) > .999f, "continuous loaded endpoint");
            for (double tick = 0; tick < span + 1; tick += .125)
            {
                float value = FirstSeveranceVisualCurves.CastTension(tick, 0, span);
                AssertEqual(true, float.IsFinite(value) && value >= 0 && value <= 1.00001f, "bounded pose");
            }
        }
        AssertEqual(0f, FirstSeveranceVisualCurves.ReleaseImpulse(99, 100), "no prefire kick");
        AssertEqual(true, FirstSeveranceVisualCurves.ReleaseImpulse(102, 100) > .99f, "fast impact");
        AssertEqual(0f, FirstSeveranceVisualCurves.ReleaseImpulse(120, 100), "shaped recovery ends");
    }

    [DomainTest("Raid flow phase does not jump with accumulated warning time")]
    private static void RaidFlowContinuity()
    {
        foreach (double age in new[] { 24d, 180d, 3600d })
        {
            float before = FirstSeveranceVisualCurves.FlowPhase(age, .28f, 0);
            float after = FirstSeveranceVisualCurves.FlowPhase(age, .28f, 1);
            AssertEqual(true, Math.Abs((after - before) - 1.4f) < .001f, "bounded release-phase shift");
        }
        foreach (Func<float, float> curve in new Func<float, float>[] {
            FirstSeveranceVisualCurves.EclosionPry, FirstSeveranceVisualCurves.EclosionPeel,
            FirstSeveranceVisualCurves.EclosionEmerge, FirstSeveranceVisualCurves.EclosionUnfurl })
        {
            AssertEqual(0f, curve(0), "sealed endpoint");
            AssertEqual(true, Math.Abs(curve(1) - 1) < .00001f, "open endpoint");
            float prior = 0;
            for (int i = 1; i <= 1000; i++)
            {
                float value = curve(i / 1000f);
                AssertEqual(true, value >= prior && value <= 1.00001f, "no seam or reverse tearing");
                prior = value;
            }
        }
    }
}
