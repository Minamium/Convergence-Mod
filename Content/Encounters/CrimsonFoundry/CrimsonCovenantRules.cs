using System;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal readonly record struct CrimsonCovenantTarget(int Slot, int Life);

// Native NPC eligibility/range belongs to the adapter; selection is stable and bounded.
internal static class CrimsonCovenantRules
{
    internal const int MaximumTargets = 10, ChargeTicks = 38, LiveTicks = 52, Duration = ChargeTicks + LiveTicks;
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
