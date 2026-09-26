using System;
using System.IO;
using Convergence.Content.Encounters.AzureCathedral;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Azure recovery snapshot preserves HP generation and rejects stale resurrect slot reuse and malformed payloads")]
    private static void AzureRecoveryCodec()
    {
        var initial = AzureExample();
        var down = new AzureRecoveryState(2, true, 1, 8000, 5900, 0, 4200);
        var state = initial with { Members = new[] { initial.Members[0] with { Recovery = down } } };
        AssertEqual(down, AzureRead(state)!.Value.Members[0].Recovery, "complete recovery snapshot");
        AssertEqual(false, initial.CanReplace(state), "default Alive cannot clear Down");
        var restored = state with { Age = state.Age + 1, Members = new[] { state.Members[0] with
            { Recovery = down with { Revision = 3, Downed = false, Life = 350, ImmunityUntil = 700, LockoutUntil = 4120 } } } };
        AssertEqual(true, restored.CanReplace(state), "authority revive generation");
        AssertEqual(false, state.CanReplace(restored), "late Down cannot undo revive");
        var differentConnection = state with { Members = new[] { state.Members[0] with { Connection = Guid.NewGuid() } } };
        AssertEqual(false, differentConnection.CanReplace(state), "frozen slot cannot rebind");
        foreach (var invalid in new[] { down with { Revision = 0 }, down with { Life = 200 }, down with { X = float.NaN },
            down with { Y = float.PositiveInfinity }, down with { ImmunityUntil = -1 }, down with { LockoutUntil = 76001 } })
        {
            bool rejected = false;
            try { AzureRead(state with { Members = new[] { state.Members[0] with { Recovery = invalid } } }); }
            catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "bounded recovery state");
        }
        AssertEqual(false, state.CanFight(state.Members[0].Slot), "Down cannot hit or be targeted");
        AssertEqual(true, state.Contains(state.Members[0].Slot), "Down retains arena ownership");
    }

    [DomainTest("Azure recovery request is bounded and a pending lethal latch survives stale Alive")]
    private static void AzureRecoveryRequestCodec()
    {
        var request = new AzureRecoveryRequest(Guid.NewGuid(), 5, 2);
        using var memory = new MemoryStream();
        using (var writer = new BinaryWriter(memory, System.Text.Encoding.UTF8, true)) request.Write(writer);
        byte[] bytes = memory.ToArray();
        using (var reader = new BinaryReader(new MemoryStream(bytes)))
            AssertEqual(request, AzureRecoveryRequest.Read(reader), "request roundtrip");
        for (int n = 0; n < bytes.Length; n++)
        {
            bool rejected = false;
            try { using var reader = new BinaryReader(new MemoryStream(bytes, 0, n)); AzureRecoveryRequest.Read(reader); }
            catch (Exception e) when (e is EndOfStreamException or InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "every truncated request rejected");
        }
        foreach (var invalid in new[] { request with { Connection = Guid.Empty }, request with { Nonce = 0 }, request with { HealthRevision = 0 }, request with { HealthRevision = 72001 } })
        {
            using var m = new MemoryStream(); using (var writer = new BinaryWriter(m, System.Text.Encoding.UTF8, true)) invalid.Write(writer);
            m.Position = 0; bool rejected = false;
            try { using var reader = new BinaryReader(m); AzureRecoveryRequest.Read(reader); }
            catch (InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "invalid request rejected");
        }
        var latch = new AzureDownLatch(); latch.Request(1);
        latch.Observe(new(1, false, 100, 100, 100, 0, 0)); AssertEqual(true, latch.Pending, "older Alive not an acknowledgement");
        latch.Observe(new(2, true, 1, 100, 100, 0, 0)); AssertEqual(false, latch.Pending, "accepted Down acknowledged");
        latch.Request(2); latch.Observe(new(3, false, 350, 100, 100, 800, 4220)); AssertEqual(false, latch.Pending, "new generation clears local latch");
    }

    [DomainTest("Azure one damage playtest retains zero-success verdict and restores budgets in one switch")]
    private static void AzureOneDamageBudget()
    {
        AssertEqual(0, AzureRules.NativeSourceDamage(0), "successful chorus does not hurt");
        foreach (int amount in new[] { 1, 300, 330, 340, 360, AzureChorusRules.Damage })
        {
            AssertEqual(AzureRules.DebugOneDamagePlaytest ? 1 : amount, AzureRules.NativeSourceDamage(amount), "native source");
            AssertEqual(AzureRules.DebugOneDamagePlaytest ? 1 : amount, AzureRules.NativeFinalDamageLimit(amount), "native cap");
        }
    }

    [DomainTest("Azure one HP admission is not itself a lethal hit but a later native floor is")]
    private static void AzureAdmissionFloor()
    {
        var floor = new AzureNativeFloor(); floor.Reset(1);
        AssertEqual(false, floor.Observe(1), "one HP on admission alone does not wipe solo");
        AssertEqual(true, floor.Observe(1, nativeLethalReceipt: true), "actual hit at one HP downs");
        floor.Reset(1);
        AssertEqual(false, floor.Observe(10), "healed without damage");
        AssertEqual(true, floor.Observe(1), "post-admission floor downs");
        floor.Reset(350); AssertEqual(false, floor.Observe(350), "revive resets the edge");
    }
}
