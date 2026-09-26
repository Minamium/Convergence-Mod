using System;
using System.IO;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal readonly record struct CrimsonCovenantTarget(int Slot, int Life);

// Native NPC eligibility/range belongs to the adapter; selection is stable and bounded.
internal static class CrimsonCovenantRules
{
    internal const int MaximumTargets = 20, ChargeTicks = 38, LiveTicks = 52, Duration = ChargeTicks + LiveTicks;
    internal const int Cycle = 60, Fire = 46;
    internal static byte ReadBatchCount(BinaryReader reader)
    {
        byte count = reader.ReadByte();
        if (count > MaximumTargets) throw new InvalidDataException("crimson.covenant_batch_invalid");
        return count; // Zero is a harmless native unowned spawn/despawn envelope.
    }
    internal static float Concentration(int count) => (1 / MathF.Sqrt(Math.Clamp(count, 1, MaximumTargets)) - 1 / MathF.Sqrt(MaximumTargets))
        / (1 - 1 / MathF.Sqrt(MaximumTargets));
    internal static float Scale(int count) => .82f + 1.78f * Concentration(count);
    internal static float DamageFactor(int count) => 1 + 3 * Concentration(count);
    internal static float HalfSpan(float enemyWidth, int count) => Math.Clamp(enemyWidth * .5f + 90 * Scale(count), 100, 2400);
    internal static int Select(Span<CrimsonCovenantTarget> candidates, Span<int> slots)
    {
        candidates.Sort((a, b) => a.Life != b.Life ? b.Life.CompareTo(a.Life) : a.Slot.CompareTo(b.Slot));
        int count = Math.Min(Math.Min(MaximumTargets, candidates.Length), slots.Length);
        for (int i = 0; i < count; i++) slots[i] = candidates[i].Slot;
        return count;
    }
    internal static float Opening(float age) => CrimsonInvocation.Ease((age - ChargeTicks) / 7)
        * (1 - CrimsonInvocation.Ease((age - (Duration - 12)) / 12));
}
