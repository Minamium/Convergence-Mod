using System;
using System.IO;
using System.Text;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Mechanic presentation preserves partial Spread recipients and terminal-only effects")]
    private static void MechanicPresentationRoundTrip()
    {
        var fight = CreateContext(2).FightId;
        var members = new FirstSeveranceCombatParticipantProjection[3];
        var impacts = new FirstSeveranceMechanicImpact[3];
        for (byte i = 0; i < 3; i++)
        {
            members[i] = new(new ParticipantId(i), i, true, RaidParticipantCombatState.Alive,
                false, 0, 0, 0, 500, 4000, 3200, 0, 0);
            impacts[i] = new(new ParticipantId(i), 4000 + i * 700, 3200, i < 2);
        }
        var projection = new FirstSeveranceCombatProjection(1, fight, FirstSeveranceSubstate.Spread, 2000,
            0, 0, 5_000_000, 5_000_000, 0, 0, 4000, 3200, 4000, 4000,
            FirstSeveranceMechanicResult.SpreadFailed, 1, members, mechanicTick: 1000, mechanicImpacts: impacts);
        impacts[0] = impacts[0] with { Failed = false };
        AssertEqual(true, projection.MechanicImpacts[0].Failed, "copied immutable result, not caller-owned array");
        foreach (var lifecycle in new[] { EncounterLifecycle.Active, EncounterLifecycle.Cleanup })
        {
            var termination = lifecycle == EncounterLifecycle.Cleanup
                ? FirstSeveranceTerminationContract.Instance.Create(FirstSeveranceTerminalCause.AllParticipantsDowned)
                : EncounterTerminationDescriptor.None;
            var snapshot = new EncounterSnapshot(1, fight, FirstSeveranceIdentity.EncounterKey, lifecycle,
                4, 1000, 100, 900, termination);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            FirstSeverancePacketCodec.WriteSnapshot(writer, snapshot, null, projection);
            long payloadEnd = stream.Position;
            writer.Write(0x12345678); // A shared receive buffer is not packet-sized.
            stream.Position = 0;
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            AssertEqual(true, EncounterPacketCodec.TryReadHeader(reader, out var header, out _), "header");
            AssertEqual(true, EncounterRouteCodec.TryRead(reader, out _), "route");
            AssertEqual(true, FirstSeverancePacketCodec.TryReadSnapshot(reader, header,
                out var accepted, out _, out var actual, out _), "bounded full snapshot with cosmetic targets");
            AssertEqual(lifecycle, accepted.Lifecycle, "a terminal does not become Active");
            AssertEqual(3, actual!.MechanicImpacts.Count, "all three outcomes replicated");
            AssertEqual(true, actual.MechanicImpacts[0].Failed && actual.MechanicImpacts[1].Failed, "both overlapping members hit");
            AssertEqual(false, actual.MechanicImpacts[2].Failed, "separated member's ray dissipates");
            AssertEqual(5400f, actual.MechanicImpacts[2].X, "frozen pre-damage endpoint");
            AssertEqual(payloadEnd, stream.Position, "no trailing shared-buffer consumption");
        }
    }

    [DomainTest("Mechanic presentation rejects foreign duplicate and nonfinite recipients")]
    private static void MechanicPresentationBounds()
    {
        var member = new FirstSeveranceCombatParticipantProjection(new ParticipantId(0), 0, true,
            RaidParticipantCombatState.Alive, false, 0, 0, 0, 500, 4000, 3200, 0, 0);
        FirstSeveranceCombatProjection Project(FirstSeveranceMechanicResult result, ulong tick,
            params FirstSeveranceMechanicImpact[] impacts) => new(1, CreateContext(2).FightId,
                FirstSeveranceSubstate.Stack, 2000, 0, 0, 5000, 5000, 0, 0, 4000, 3200, 4000, 4000,
                result, 1, new[] { member }, mechanicTick: tick, mechanicImpacts: impacts);
        var hit = new FirstSeveranceMechanicImpact(new ParticipantId(0), 4000, 3200, true);
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceMechanicResult.StackFailed, 0, hit), "zero cue tick");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceMechanicResult.StackFailed, 1000, hit with { ParticipantId = new(1) }), "outsider");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceMechanicResult.StackFailed, 1000, hit with { X = float.NaN }), "nonfinite endpoint");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceMechanicResult.StackFailed, 1000, hit, hit), "overbounded/duplicate");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceMechanicResult.SpreadPassed, 1000, hit), "success cannot show a damaging verdict");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceMechanicResult.StackFailed, 1000, hit with { Failed = false }), "failed Stack affects all standing targets");
    }
}
