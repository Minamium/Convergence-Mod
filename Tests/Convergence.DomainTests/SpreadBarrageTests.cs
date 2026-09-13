using System;
using System.IO;
using System.Linq;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Standalone Spread fits three or four spaced pursuits; embedded mechanics suppress them")]
    private static void SpreadBarrageSchedule()
    {
        foreach (int duration in new[] { 138, 144, 150, 156, 162, 168, 170, 174, 180, 240 })
        {
            var w = new FirstSeveranceSafeWindow(FirstSeveranceSafeMechanic.Spread, 1000, (ulong)(1000 + duration), 0, 0);
            ulong previous = 0;
            int shots = FirstSeveranceSpreadBarrage.ShotCount(w);
            AssertEqual(duration >= 180 ? 4 : 3, shots, "short windows use fewer casts");
            AssertThrows<ArgumentException>(() => FirstSeveranceSpreadBarrage.Start(w, shots), "no extra shot squeezed into the window");
            for (byte i = 0; i < shots; i++)
            {
                ulong start = FirstSeveranceSpreadBarrage.Start(w, i);
                var cast = FirstSeveranceAttackPatterns.CreatePrism((uint)i + 1, start, i, new[] { new FirstSeverancePrismTarget(0, 4000, 3000, 12, -4) });
                AssertEqual(true, start >= previous + 34, "minimum34-tick start spacing");
                AssertEqual(28ul, cast.FireTick - start, "P1 reading window unchanged");
                AssertEqual(true, cast.EndTick <= w.ResolveTick - 24, "settle before Spread");
                AssertEqual(false, cast.IsFiring(cast.FireTick - 1), "warning harmless");
                AssertEqual(true, cast.IsFiring(cast.FireTick), "fire exact");
                AssertEqual(false, cast.IsFiring(cast.EndTick), "end exact");
                previous = start;
            }
        }
        foreach (var phase in Enum.GetValues<FirstSeveranceBossPhase>())
        {
            var actions = FirstSeveranceChoreography.For(phase);
            for (int i = 0; i < actions.Count; i++)
            for (ulong age = 0; age < (ulong)actions[i].Ticks; age++)
            {
                var a = actions[i];
                bool expected = a.State == FirstSeveranceSubstate.Spread;
                var actual = FirstSeveranceSpreadBarrage.Window(a.State, i, 1000, (ulong)(1000 + a.Ticks), 1000 + age);
                AssertEqual(expected, actual.HasValue, "standalone only; no pursuit during lattice/flood/other attacks");
                if (actual is { } w) _ = FirstSeveranceSpreadBarrage.Start(w, FirstSeveranceSpreadBarrage.ShotCount(w) - 1);
            }
        }
    }

    [DomainTest("Spread pursuit snapshot round-trips 1–4 players and rejects malformed bounded casts")]
    private static void SpreadBarrageCodec()
    {
        var fight = CreateContext(2).FightId;
        for (int count = 1; count <= 4; count++)
        {
            var members = Enumerable.Range(0, count).Select(i => new FirstSeveranceCombatParticipantProjection(
                new ParticipantId((byte)i), i, true, RaidParticipantCombatState.Alive, false, 0, 0, 0, 500, 4000, 3000, 0, 0)).ToArray();
            var targets = Enumerable.Range(0, count).Select(i => new FirstSeverancePrismTarget(i, 4000 + i * 300, 3000, 10, -3)).ToArray();
            var w = new FirstSeveranceSafeWindow(FirstSeveranceSafeMechanic.Spread, 1000, 1180, 0, 0);
            var casts = Enumerable.Range(0, 4).Select(i => FirstSeveranceAttackPatterns.CreatePrism((uint)i + 1,
                FirstSeveranceSpreadBarrage.Start(w, i), (byte)i, targets)).ToArray();
            FirstSeveranceCombatProjection Project(FirstSeveranceLanceVolley[] list) => new(1, fight, FirstSeveranceSubstate.Spread,
                1180, 0, 0, 5000, 5000, 0, 0, 4000, 3000, 4000, 4000, FirstSeveranceMechanicResult.None, 0,
                members, actionStartedTick: 1000, spreadLances: list);
            var projection = Project(casts);
            AssertThrows<ArgumentException>(() => Project(casts.Concat(new[] { casts[0] }).ToArray()), "five casts");
            AssertThrows<ArgumentException>(() => Project(new[] { casts[0], casts[0] }), "duplicate serial/step");
            AssertThrows<ArgumentException>(() => Project(new[] { FirstSeveranceAttackPatterns.CreatePrism(99, 1150, 0, targets) }), "no truncated final warning");
            AssertThrows<ArgumentException>(() => Project(new[] { FirstSeveranceAttackPatterns.CreatePrism(99, 1001, 0, targets) }), "not before scheduled reveal");
            var snapshot = new EncounterSnapshot(1, fight, FirstSeveranceIdentity.EncounterKey, EncounterLifecycle.Active,
                4, 1140, 1000, 140, EncounterTerminationDescriptor.None);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            FirstSeverancePacketCodec.WriteSnapshot(writer, snapshot, null, projection);
            int end = (int)stream.Position;
            writer.Write(0x12345678);
            byte[] bytes = stream.ToArray();
            bool Read(byte[] packet, out FirstSeveranceCombatProjection actual)
            {
                using var input = new MemoryStream(packet);
                using var reader = new BinaryReader(input);
                EncounterPacketCodec.TryReadHeader(reader, out var header, out _);
                EncounterRouteCodec.TryRead(reader, out _);
                bool ok = FirstSeverancePacketCodec.TryReadSnapshot(reader, header, out _, out _, out var received, out _);
                actual = received!;
                if (ok) AssertEqual((long)end, input.Position, "leaves shared buffer alone");
                return ok;
            }
            AssertEqual(true, Read(bytes, out var decoded), "full pursuit history");
            for (int i = 0; i < 4; i++)
            {
                AssertEqual(casts[i].FireTick, decoded.SpreadLances[i].FireTick, "same release clock");
                for (int j = 0; j < count; j++) AssertEqual(casts[i].Rays[j], decoded.SpreadLances[i].Rays[j], "locked peer geometry");
            }
            int section = end - (1 + 4 * (16 + 24 * count)) - 2; // v35 main/carry flags follow Spread
            var corrupt = (byte[])bytes.Clone(); corrupt[section] = 5;
            AssertEqual(false, Read(corrupt, out _), "reject count before allocation");
            corrupt = (byte[])bytes.Clone(); corrupt[section + 16] = (byte)(count + 1);
            AssertEqual(false, Read(corrupt, out _), "reject ray count before allocation");
            foreach (int length in new[] { section, section + 5, end - 1 })
                AssertThrows<EndOfStreamException>(() => Read(bytes.Take(length).ToArray(), out _), "truncated cast; router owns rejection");
        }
    }
}
