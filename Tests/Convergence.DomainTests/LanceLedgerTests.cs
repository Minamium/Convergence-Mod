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
    private static FirstSeveranceLanceVolley HeldPrism(uint serial, ulong start, byte step = 0)
        => FirstSeveranceAttackPatterns.Create(serial, start, FirstSeveranceSubstate.PylonCheck, step, 0, 1000, 2000, 0, 0);

    [DomainTest("Sustained pursuit ledger preserves old hits across a new forecast and exact cleanup")]
    private static void SustainedPursuitLedger()
    {
        var ledger = new FirstSeveranceLanceLedger();
        var member = new ParticipantId(0);
        var first = HeldPrism(1, 100);
        var second = HeldPrism(2, 142, 1);
        AssertEqual(54, first.ActiveTicks, "main beam holds for 0.9 seconds");
        AssertEqual(28, first.TelegraphTicks, "forecast unchanged");
        ledger.Start(first, 100);
        ledger.MarkHit(1, member);
        ledger.Start(second, 142);
        AssertEqual(first, ledger.Carried!, "previous firing beam survives next forecast");
        AssertEqual(true, ledger.HasHit(1, member), "old ray cannot damage again");
        AssertEqual(false, ledger.HasHit(2, member), "new cast owns a separate hit cap");
        ledger.MarkHit(2, member);
        AssertEqual(false, ledger.CanStart, "only two simultaneous casts");
        AssertEqual(false, ledger.Retire(181), "old cast kept through last live tick");
        AssertEqual(true, ledger.Retire(182), "old cast retired exactly at end");
        AssertEqual(true, ledger.HasHit(2, member), "retirement does not clear current cap");
        AssertThrows<InvalidOperationException>(() => ledger.HasHit(1, member), "expired hit identity rejected");
        ledger.Start(HeldPrism(3, 184, 2), 184);
        AssertEqual(true, ledger.HasHit(2, member), "second cap survives role swap");
        ledger.Clear(); ledger.Clear();
        AssertEqual(true, ledger.Current is null && ledger.Carried is null && ledger.CanStart, "phase/fight cleanup idempotent");
        AssertThrows<InvalidOperationException>(() => ledger.MarkHit(2, member), "cleanup revokes old cast");
    }

    [DomainTest("Sustained pursuit cadence rejects overflow and leaves Spread duration unchanged")]
    private static void SustainedPursuitBounds()
    {
        var ledger = new FirstSeveranceLanceLedger();
        ledger.Start(HeldPrism(1, 100), 100);
        AssertThrows<InvalidOperationException>(() => ledger.Start(HeldPrism(2, 141), 141), "cannot add an early cast");
        ledger.Start(HeldPrism(2, 142), 142);
        AssertThrows<InvalidOperationException>(() => ledger.Start(HeldPrism(3, 143), 143), "bounded storage cannot be overwritten");
        var targets = new[] { new FirstSeverancePrismTarget(0, 1000, 2000, 0, 0) };
        var spread = FirstSeveranceAttackPatterns.CreatePrism(4, 200, 0, targets);
        AssertEqual(12, spread.ActiveTicks, "standalone Spread keeps its existing duration");
        AssertEqual(false, spread.SustainedPrism, "no global duration change");
        ledger.Clear();
        ledger.Start(spread, 200);
        AssertEqual(false, ledger.CanStart, "only the main sequence permits overlap");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(5, 200, spread.Rays,
            FirstSeveranceAttackKind.SweepRight, 0, 0, 200, sustainedPrism: true), "charge cannot claim main hold policy");
    }

    [DomainTest("Sustained pursuit snapshot repairs overlapping rays for one to four peers and rejects malformed carry")]
    private static void SustainedPursuitCodec()
    {
        var fight = CreateContext(2).FightId;
        for (int count = 1; count <= 4; count++)
        {
            var members = Enumerable.Range(0, count).Select(i => new FirstSeveranceCombatParticipantProjection(
                new ParticipantId((byte)i), i, true, RaidParticipantCombatState.Alive, false, 0, 0, 0, 500, 4000, 3000, 0, 0)).ToArray();
            var targets = Enumerable.Range(0, count).Select(i => new FirstSeverancePrismTarget(i, 4000+i*300,3000,12,-3)).ToArray();
            var prior = FirstSeveranceAttackPatterns.CreatePrism(10,100,0,targets,sustainedPrism:true);
            var current = FirstSeveranceAttackPatterns.CreatePrism(11,142,1,targets,sustainedPrism:true);
            FirstSeveranceCombatProjection Project(FirstSeveranceLanceVolley latest, FirstSeveranceLanceVolley old)
                => new(1,fight,FirstSeveranceSubstate.PylonCheck,1000,0,0,5000,5000,0,0,4000,3000,4000,4000,
                    FirstSeveranceMechanicResult.None,0,members,latest,carriedLance:old);
            AssertThrows<ArgumentException>(()=>Project(prior,current),"reversed timestamps");
            AssertThrows<ArgumentException>(()=>Project(current,current),"duplicate serial");
            AssertThrows<ArgumentException>(()=>Project(current,FirstSeveranceAttackPatterns.CreatePrism(12,100,0,targets)),"no short cast as carried");
            var snapshot = new EncounterSnapshot(1,fight,FirstSeveranceIdentity.EncounterKey,EncounterLifecycle.Active,
                4,172,100,72,EncounterTerminationDescriptor.None);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            FirstSeverancePacketCodec.WriteSnapshot(writer,snapshot,null,Project(current,prior));
            int end = (int)stream.Position;
            writer.Write(0x12345678);
            byte[] bytes = stream.ToArray();
            bool Read(byte[] packet,out FirstSeveranceCombatProjection result)
            {
                using var input = new MemoryStream(packet);
                using var reader = new BinaryReader(input);
                EncounterPacketCodec.TryReadHeader(reader,out var header,out _);
                EncounterRouteCodec.TryRead(reader,out _);
                bool ok = FirstSeverancePacketCodec.TryReadSnapshot(reader,header,out _,out _,out var received,out _);
                result = received!;
                if(ok) AssertEqual((long)end,input.Position,"consume exactly one shared-buffer payload");
                return ok;
            }
            AssertEqual(true,Read(bytes,out var decoded),"both casts recovered without local history");
            AssertEqual(224ul,decoded.LanceVolley!.EndTick,"latest full firing window");
            AssertEqual(182ul,decoded.CarriedLance!.EndTick,"predecessor keeps independent end");
            for(int i=0;i<count;i++)
            {
                AssertEqual(prior.Rays[i],decoded.CarriedLance.Rays[i],"older locked aim unmodified");
                AssertEqual(current.Rays[i],decoded.LanceVolley.Rays[i],"current locked aim unmodified");
            }
            int tail = end-(3+16+24*count); // v37 adds a trailing cannon-present flag.
            foreach(int flag in new[]{tail,tail+1})
            {
                var bad=(byte[])bytes.Clone(); bad[flag]=2;
                AssertEqual(false,Read(bad,out _),"strict booleans before carry allocation");
            }
            var oversized=(byte[])bytes.Clone(); oversized[tail+17]=(byte)(count+1);
            AssertEqual(false,Read(oversized,out _),"carry rays bounded to roster");
            var missingPolicy=(byte[])bytes.Clone(); missingPolicy[tail]=0;
            AssertEqual(false,Read(missingPolicy,out _),"cannot carry without sustained main policy");
            AssertThrows<EndOfStreamException>(()=>Read(bytes.Take(end-1).ToArray(),out _),"truncation rejected by router");
        }
    }
}
