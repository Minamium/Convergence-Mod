using System;
using System.IO;
using System.Linq;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;
internal static partial class Program
{
    [DomainTest("Video feedback: weapon presentation runs exactly once including zero extra updates")]
    private static void WeaponFinalSubupdate()
    {
        for (int extra = 0; extra < 9; extra++)
        {
            int remaining = extra, calls = 0;
            while (remaining >= 0)
            {
                remaining--; // Matches the installed Projectile.Update IL, before AI.
                if (RitualPresentationStep.IsFinal(remaining)) calls++;
            }
            AssertEqual(1, calls, "one audio/interpolation tick, including extraUpdates zero");
        }
        AssertEqual(true, FirstSeverancePresentationTiming.CueGain("StackSummon", 1)
            < FirstSeverancePresentationTiming.CueGain("SpreadSummon", 1), "Stack-only gain reduction");
    }

    [DomainTest("Video feedback: Final cannon locks P2 geometry, survives codec and rejects wrong stage")]
    private static void FinalCoreCannonContract()
    {
        var fight = CreateContext(2).FightId;
        var members = new[] { new FirstSeveranceCombatParticipantProjection(new ParticipantId(0), 0, true,
            RaidParticipantCombatState.Alive, false, 0, 0, 0, 500, 4200, 3500, 0, 0) };
        var ray = FirstSeveranceGridVolley.AimCoreBeam(4000, 4000, 4500, 3440);
        var cast = new FirstSeveranceCoreCannonVolley(9, 1024, 0, ray);
        FirstSeveranceCombatProjection Projection(FirstSeveranceCoreCannonVolley c,
            FirstSeveranceSubstate stage = FirstSeveranceSubstate.FinalBullets)
            => new(1, fight, stage, 1240, 0, 0, 0, 5000000, 0, 0,
                4000, 3000, 4000, 4000, FirstSeveranceMechanicResult.None, 0, members,
                bossPhase: FirstSeveranceBossPhase.Final, bossPhaseStartedTick: 800,
                actionStartedTick: 1000, actionIndex: 2, coreCannon: c);
        AssertEqual(false, cast.Intersects(cast.FireTick - 1, 4500, 3440, 10, 21), "warning harmless");
        AssertEqual(false, cast.Intersects(cast.FireTick, 6900, 3440, 10, 21), "far tip not teleported");
        AssertEqual(true, cast.Intersects(cast.FireTick + 16, 4500, 3440, 10, 21), "same P2 live beam");
        AssertEqual(false, cast.Intersects(cast.EndTick, 4500, 3440, 10, 21), "no lingering damage");
        AssertThrows<ArgumentException>(() => Projection(new(9, 1010, 0, ray)), "opening window");
        AssertThrows<ArgumentException>(() => Projection(cast, FirstSeveranceSubstate.FinalSlicer), "no cannon outside bullets");
        AssertThrows<ArgumentException>(() => Projection(new(9, 1220, 0, ray)), "cannot outlive bullets");
        AssertThrows<ArgumentException>(() => Projection(new(9, 1024, 8, ray)), "target must be participant");
        AssertThrows<ArgumentException>(() => Projection(new(9, 1024, 0, ray with { X = 4100 })), "origin cannot spoof");
        AssertThrows<ArgumentException>(() => new FirstSeveranceCoreCannonVolley(0, 1024, 0, ray), "zero serial");
        AssertThrows<ArgumentException>(() => new FirstSeveranceCoreCannonVolley(1, ulong.MaxValue, 0, ray), "overflow");
        var snapshot = new EncounterSnapshot(1, fight, FirstSeveranceIdentity.EncounterKey, EncounterLifecycle.Active,
            4, 1095, 1000, 95, EncounterTerminationDescriptor.None);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        FirstSeverancePacketCodec.WriteSnapshot(writer, snapshot, null, Projection(cast));
        int end = (int)stream.Position;
        writer.Write(1234);
        bool Read(byte[] bytes, out FirstSeveranceCombatProjection actual)
        {
            using var input = new MemoryStream(bytes); using var reader = new BinaryReader(input);
            EncounterPacketCodec.TryReadHeader(reader, out var header, out _);
            EncounterRouteCodec.TryRead(reader, out _);
            bool ok = FirstSeverancePacketCodec.TryReadSnapshot(reader, header, out _, out _, out var received, out _);
            actual = received!;
            if (ok) AssertEqual((long)end, input.Position, "exact payload consumption");
            return ok;
        }
        var bytes = stream.ToArray();
        AssertEqual(true, Read(bytes, out var actual), "late snapshot carries frozen aim");
        AssertEqual(ray, actual.CoreCannon!.Ray, "same origin, direction, dimensions");
        AssertEqual(cast.FireTick, actual.CoreCannon.FireTick, "same clock");
        var invalidFlag = (byte[])bytes.Clone(); invalidFlag[end - 23] = 2;
        AssertEqual(false, Read(invalidFlag, out _), "strict cannon flag");
        AssertThrows<EndOfStreamException>(() => Read(bytes.Take(end - 1).ToArray(), out _), "truncated cannon");
    }
}
