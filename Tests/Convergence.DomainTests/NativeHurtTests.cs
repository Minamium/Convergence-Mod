using System;
using System.IO;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Common.Networking.Protocol;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Native Hurt retains native defenses and distinguishes armor-ignoring mechanics")]
    private static void NativeHurtPolicies()
    {
        var normal = new FirstSeveranceHurtIntent(0, 120, FirstSeveranceHurtKind.Hazard);
        AssertEqual(true, normal.IsValid && normal.Dodgeable, "normal native hit");
        AssertEqual(0f, normal.ArmorPenetration, "normal defense works");
        var mechanic = normal with { Kind = FirstSeveranceHurtKind.Mechanic };
        AssertEqual(1f, mechanic.ArmorPenetration, "mechanic ignores armor only");
        AssertEqual(true, mechanic.Dodgeable, "standard dodges still work");
        AssertEqual(false, (normal with { Kind = FirstSeveranceHurtKind.Crush }).Dodgeable, "extreme crush");
        AssertEqual(false, (normal with { Kind = (FirstSeveranceHurtKind)255 }).IsValid, "bounded policy");
    }

    [DomainTest("Native Hurt ledger rejects stale revival generations, replay and unissued hit IDs")]
    private static void NativeHurtGenerationAndReplay()
    {
        var ledger = new FirstSeveranceHurtLedger();
        AssertEqual(true, ledger.TryIssue(new(2, 120, FirstSeveranceHurtKind.Hazard), 100, "Grid", out var hit), "issue");
        var result = new FirstSeveranceHurtResult(1, hit.Revision, 2, 90, 1, 89);
        AssertEqual(false, ledger.TryAccept(result with { HitRevision = 99 }, 2, 110, out _), "unissued");
        AssertEqual(false, ledger.TryAccept(result, 4, 110, out _), "late result cannot undo revival");
        AssertEqual(true, ledger.TryAccept(result, 2, 110, out var matched), "matching native result");
        AssertEqual(false, ledger.Awaiting(2, 110, out _), "settled hit permits terminal");
        AssertEqual("Grid", matched.Source, "authority retains source");
        AssertEqual(false, ledger.TryAccept(result, 2, 110, out _), "duplicate");
        AssertEqual(false, ledger.TryAccept(result with { Nonce = 2 }, 2, 111, out _), "consumed hit ID");
        AssertEqual(false, ledger.TryAccept(new(2, 0, 2, 90, 80, 10), 2, 111, out _), "unrequested hit zero only reports floor");
        AssertEqual(true, ledger.TryAccept(new(2, 0, 2, 2, 1, 1), 2, 111, out _), "ordinary native floor has no raid hit ID");
    }

    [DomainTest("Native Hurt queues are bounded and expired intents cannot apply delayed damage results")]
    private static void NativeHurtCapacityAndExpiry()
    {
        var ledger = new FirstSeveranceHurtLedger();
        for (int i = 0; i < FirstSeveranceHurtLedger.Capacity; i++)
            AssertEqual(true, ledger.TryIssue(new(0, 120, FirstSeveranceHurtKind.Hazard), 0, "Test", out _), "capacity");
        AssertEqual(false, ledger.TryIssue(new(0, 120, FirstSeveranceHurtKind.Hazard), 1, "Test", out _), "bounded");
        AssertEqual(false, ledger.TryAccept(new(1, 1, 0, 100, 50, 50), 0,
            FirstSeveranceHurtLedger.MaximumAgeTicks + 1, out _), "expired");
        AssertEqual(true, ledger.Awaiting(0, 601, out bool timedOut) && timedOut, "timeout cannot grant a false victory");
        AssertEqual(false, ledger.Awaiting(1, 601, out _), "authoritative recovery retires the old generation");
        AssertEqual(true, ledger.TryIssue(new(1, 120, FirstSeveranceHurtKind.Hazard), 700, "Test", out _), "new generation prunes");
    }

    [DomainTest("Pending Down survives in-flight Alive snapshots and clears only on authoritative recovery or Down")]
    private static void NativeHurtPendingDown()
    {
        FirstSeveranceDownLatch latch = default;
        latch.Request(2);
        latch.Observe(2, false);
        AssertEqual(true, latch.Pending, "old Alive snapshot cannot reopen damage/input");
        latch.Observe(1, false);
        AssertEqual(true, latch.Pending, "older snapshot");
        latch.Observe(3, true);
        AssertEqual(false, latch.Pending, "authority Down takes over");
        latch.Request(3);
        latch.Observe(4, false);
        AssertEqual(false, latch.Pending, "same-tick down/revive correction");
        latch = default;
        AssertEqual(false, latch.Pending, "Fight cleanup");
    }

    [DomainTest("Native Hurt result codec consumes a bounded complete body without swallowing the next packet")]
    private static void NativeHurtResultCodec()
    {
        var header = new EncounterPacketHeader(EncounterProtocol.CurrentVersion,
            EncounterPacketType.RequestRaidHurtResult, 7, CreateContext(2).FightId, 0);
        var expected = new FirstSeveranceHurtResult(4, 3, 2, 500, 1, 499);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        FirstSeverancePacketCodec.WriteHurtResult(writer, header, expected);
        long end = stream.Position;
        writer.Write(0x12345678);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        AssertEqual(true, EncounterPacketCodec.TryReadHeader(reader, out _, out _), "header");
        AssertEqual(true, EncounterRouteCodec.TryRead(reader, out _), "route");
        AssertEqual(true, FirstSeverancePacketCodec.TryReadHurtResult(reader, out var actual, out _), "body");
        AssertEqual(expected, actual, "round trip");
        AssertEqual(end, stream.Position, "bounded body");
        for (int length = 0; length < 24; length++)
        {
            using var truncated = new BinaryReader(new MemoryStream(new byte[length]));
            AssertThrows<EndOfStreamException>(() => FirstSeverancePacketCodec.TryReadHurtResult(truncated, out _, out _), "truncated");
        }
        AssertEqual(false, (expected with { Nonce = 0 }).IsValid, "nonce");
        AssertEqual(false, (expected with { LifeAfter = 0 }).IsValid, "no dead resurrection report");
        AssertEqual(false, (expected with { Damage = -1 }).IsValid, "negative damage");
        AssertEqual(false, (expected with { LifeBefore = int.MaxValue }).IsValid, "overflow bound");
    }
}
