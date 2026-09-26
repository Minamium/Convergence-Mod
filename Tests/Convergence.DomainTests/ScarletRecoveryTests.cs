using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet recovery preserves frozen identities generations and arena membership")]
    private static void ScarletRecoveryProjection()
    {
        var old = ScarletRehearsalState();
        var health = new CrimsonRecoveryState(2, true, 1, 8000, 5900, 0, 4200);
        var down = old with { Members = new[] { old.Members[0] with { Recovery = health } } };
        using var memory = new MemoryStream();
        down.Write(new BinaryWriter(memory)); memory.Position = 0;
        AssertEqual(health, CrimsonState.Read(new BinaryReader(memory)).Members[0].Recovery, "native actor roundtrip");
        AssertEqual(false, old.CanReplace(down), "older Alive cannot undo Down");
        AssertEqual(false, down.CanFight(0), "Down cannot attack or be targeted");
        AssertEqual(true, down.Contains(0), "Down keeps field ownership");
        var revive = health with { Revision = 3, Downed = false, Life = 350, ImmunityUntil = 1400, LockoutUntil = 4820 };
        var resumed = down with { Age = down.Age + 1, Members = new[] { down.Members[0] with { Recovery = revive } } };
        AssertEqual(true, resumed.CanReplace(down), "authoritative revive generation accepted");
        AssertEqual(false, down.CanReplace(resumed), "late Down cannot undo revive");
        AssertEqual(false, (resumed with { Members = new[] { resumed.Members[0] with { Connection = Guid.NewGuid() } } }).CanReplace(resumed), "slot reuse rejected");
        AssertEqual(false, revive.AcceptsFloor(2, 6, 5, 1), "late pre-revive lethal receipt rejected");
        AssertEqual(false, revive.AcceptsFloor(3, 5, 5, 1), "duplicate receipt rejected");
        AssertEqual(false, revive.AcceptsFloor(3, 6, 5, 350), "receipt cannot invent native HP loss");
        AssertEqual(true, revive.AcceptsFloor(3, 6, 5, 1), "current HP floor received");
        foreach (var invalid in new[] { health with { Revision = 0 }, health with { Life = 2 },
            health with { X = float.NaN }, health with { LockoutUntil = 76001 } })
        {
            using var data = new MemoryStream(); invalid.Write(new BinaryWriter(data)); data.Position = 0;
            AssertThrows<InvalidDataException>(() => CrimsonRecoveryState.Read(new BinaryReader(data)), "invalid health rejected");
        }
    }

    [DomainTest("Scarlet recovery request bounds and local native floor cannot pre-empt an actual hit")]
    private static void ScarletRecoveryIngress()
    {
        var request = new CrimsonRecoveryRequest(Guid.NewGuid(), 5, 2);
        using var memory = new MemoryStream(); request.Write(new BinaryWriter(memory)); byte[] bytes = memory.ToArray();
        using (var reader = new BinaryReader(new MemoryStream(bytes)))
            AssertEqual(request, CrimsonRecoveryRequest.Read(reader), "bounded request roundtrip");
        for (int n = 0; n < bytes.Length; n++)
        {
            bool bad = false;
            try { CrimsonRecoveryRequest.Read(new BinaryReader(new MemoryStream(bytes, 0, n))); }
            catch (Exception e) when (e is EndOfStreamException or InvalidDataException) { bad = true; }
            AssertEqual(true, bad, "every truncated request rejected");
        }
        foreach (var invalid in new[] { request with { Connection = Guid.Empty }, request with { Nonce = 0 },
            request with { HealthRevision = 0 }, request with { HealthRevision = 72001 } })
        {
            using var data = new MemoryStream(); invalid.Write(new BinaryWriter(data)); data.Position = 0;
            AssertThrows<InvalidDataException>(() => CrimsonRecoveryRequest.Read(new BinaryReader(data)), "invalid request rejected");
        }
        var latch = new CrimsonDownLatch(); latch.Request(1);
        latch.Observe(new(1, false, 100, 100, 100, 0, 0));
        AssertEqual(true, latch.Pending, "old Alive cannot unlock pending Down");
        latch.Observe(new(2, true, 1, 100, 100, 0, 0));
        AssertEqual(false, latch.Pending, "authority acknowledges Down");
        var floor = new CrimsonNativeFloor(); floor.Reset(1);
        AssertEqual(false, floor.Observe(1), "joining at one HP alone never Downs");
        AssertEqual(true, floor.Observe(1, nativeLethalReceipt: true), "native lethal at one HP Downs");
        floor.Reset(350); AssertEqual(false, floor.Observe(350), "revive rearms floor");
        AssertEqual(true, floor.Observe(1), "subsequent native floor");
    }

    [DomainTest("Scarlet one-damage mode retains Stack Spread outcomes and native zero-success")]
    private static void ScarletRehearsalVerdicts()
    {
        var center = new CrimsonPoint(8000, 5500);
        var positions = new[] { center, center };
        foreach (var kind in new[] { CrimsonChorusKind.Stack, CrimsonChorusKind.Spread })
        {
            var intended = CrimsonChorusRules.Resolve(kind, center, positions, 3, 3);
            for (int i = 0; i < intended.Length; i++)
            {
                int expected = kind == CrimsonChorusKind.Stack ? 0 : 1;
                AssertEqual(expected, CrimsonPlaytestTuning.NativeSourceDamage(intended[i]), "success zero / failure one");
                AssertEqual(expected, CrimsonPlaytestTuning.NativeFinalDamageLimit(intended[i]), "native cap agrees");
                AssertEqual(kind == CrimsonChorusKind.Spread, intended[i] > 0, "failure flag retained for VFX despite God Mode or cap");
            }
        }
        var missing = CrimsonChorusRules.Resolve(CrimsonChorusKind.Stack, center,
            new[] { center, center + new CrimsonPoint(700, 0) }, 3, 3);
        AssertEqual(450, missing[0], "unmodified missing-fraction rule");
        AssertEqual(1, CrimsonPlaytestTuning.NativeSourceDamage(missing[0]), "partial failure also capped");
        AssertEqual(0, CrimsonPlaytestTuning.NativeSourceDamage(0), "no strike created on success");
    }

    [DomainTest("Scarlet failed chorus has a readable cosmetic tail without extending the hit window")]
    private static void ScarletVerdictTail()
    {
        var plan = new CrimsonChorusPlan(Guid.NewGuid(), 1, 0, 1, 3, CrimsonChorusKind.Spread,
            3, 0, 240, 264, 8000, 6000, new(8000, 5500));
        AssertEqual(288, CrimsonChorusImpactPositions.LeaseEnd(plan), "even shortest recovery preserves the full tail");
        AssertEqual(12, CrimsonChorusRules.ImpactTicks, "native impact window unchanged");
        AssertEqual(0f, CrimsonChorusImpactPositions.FailureAlpha(-1), "no failure before verdict");
        AssertEqual(true, CrimsonChorusImpactPositions.FailureAlpha(24) > .7f, "visible after delayed verdict");
        AssertEqual(0f, CrimsonChorusImpactPositions.FailureAlpha(48), "bounded cosmetic cleanup");
    }
}
