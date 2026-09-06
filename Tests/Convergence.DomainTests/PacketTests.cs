using System;
using System.IO;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking;
using Convergence.Common.Networking.Protocol;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static void AssertTrue(bool condition, string message) => AssertEqual(true, condition, message);

    [DomainTest("Packet routes are bounded, definition-scoped and reject stale sessions")]
    private static void PacketRouteContracts()
    {
        var registry = new EncounterPacketRoutes();
        var first = new PacketProbe();
        var second = new PacketProbe(); // a handler stub, not a second Raid implementation
        registry.Register("first", first);
        registry.Register("second", second);
        AssertTrue(registry.TryGet("first", out var one) && ReferenceEquals(one, first), "first route");
        AssertTrue(registry.TryGet("second", out var two) && ReferenceEquals(two, second), "second route shares operation IDs");
        AssertThrows<InvalidOperationException>(() => registry.Register("first", second), "duplicate definition rejected");
        AssertThrows<InvalidOperationException>(() => registry.Register("", second), "empty definition rejected");
        AssertTrue(!registry.TryGet("unknown", out _), "unknown route fails closed");

        var fight = FightId.FromWire(Guid.NewGuid());
        var header = new EncounterPacketHeader(EncounterProtocol.CurrentVersion, EncounterPacketType.RequestSetReady, 1, fight, 3);
        var snapshot = new EncounterSnapshot(1, fight, "first", EncounterLifecycle.Preparing, 3, 20, 1, 0, EncounterTerminationDescriptor.None);
        AssertTrue(EncounterPacketRoutes.MatchesSession("first", header, snapshot), "exact session");
        AssertTrue(!EncounterPacketRoutes.MatchesSession("second", header, snapshot), "cross-feature denied");
        AssertTrue(!EncounterPacketRoutes.MatchesSession("first", header with { EncounterSequence = 2 }, snapshot), "stale sequence denied");
        AssertTrue(!EncounterPacketRoutes.MatchesSession("first", header with { FightId = FightId.None }, snapshot), "missing fight denied");
        foreach (string key in new[] { "", "first", new string('a', EncounterRouteCodec.MaximumKeyBytes) })
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            EncounterRouteCodec.Write(writer, key);
            writer.Write((byte)199); // shared stream suffix must be untouched
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            AssertTrue(EncounterRouteCodec.TryRead(reader, out string decoded), "bounded route round trip");
            AssertEqual(key, decoded, "route identity");
            AssertEqual((byte)199, reader.ReadByte(), "do not drain shared receive buffer");
        }
        foreach (byte[] bytes in new[] { Array.Empty<byte>(), new byte[] { 65 }, new byte[] { 2, 97 }, new byte[] { 1, 255 }, new byte[] { 1, 47 } })
        {
            using var reader = new BinaryReader(new MemoryStream(bytes));
            AssertTrue(!EncounterRouteCodec.TryRead(reader, out _), "malformed/truncated route");
        }
        using (var stream = new MemoryStream())
        {
            using var writer = new BinaryWriter(stream);
            FirstSeverancePacketCodec.WriteReadyRequest(writer, header, true, 7);
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            AssertTrue(EncounterPacketCodec.TryReadHeader(reader, out var decoded, out _), "real header");
            AssertEqual(header, decoded, "stable envelope");
            AssertTrue(EncounterRouteCodec.TryRead(reader, out string key), "real feature route");
            AssertEqual(FirstSeveranceIdentity.EncounterKey, key, "feature writer binds its definition");
            AssertTrue(FirstSeverancePacketCodec.TryReadReadyRequest(reader, out bool ready, out uint nonce, out _), "real bounded payload");
            AssertTrue(ready && nonce == 7 && stream.Position == stream.Length, "payload complete");
        }
        registry.Clear();
        registry.Clear();
        AssertTrue(!registry.TryGet("first", out _), "unload clears routes idempotently");
    }

    private sealed class PacketProbe : IEncounterPacketHandler
    {
        public bool TryHandle(BinaryReader reader, int sender, in EncounterPacketHeader header, out string failure)
        { failure = string.Empty; return true; }
        public void PublishSnapshot(in EncounterSnapshot snapshot, int toClient) { }
        public void ApplyIdleSnapshot(in EncounterSnapshot snapshot) { }
    }
}
