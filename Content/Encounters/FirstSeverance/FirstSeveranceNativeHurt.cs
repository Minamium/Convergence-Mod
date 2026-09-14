#nullable enable
using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceHurtKind : byte
{
    Hazard = 0,
    Mechanic = 1,
    Crush = 2,
}

internal readonly record struct FirstSeveranceHurtIntent(uint HealthRevision, int Damage, FirstSeveranceHurtKind Kind)
{
    internal const int MaximumDamage = 1_000_000;
    internal bool IsValid => Damage is > 0 and <= MaximumDamage
        && Kind is >= FirstSeveranceHurtKind.Hazard and <= FirstSeveranceHurtKind.Crush;
    internal bool Dodgeable => Kind != FirstSeveranceHurtKind.Crush;
    internal float ArmorPenetration => Kind == FirstSeveranceHurtKind.Hazard ? 0f : 1f;
}

// Owner-reported native result, NOT a client geometry/success verdict. HP still
// travels through Terraria's native synchronization; this message never subtracts HP.
internal readonly record struct FirstSeveranceHurtResult(
    uint Nonce, uint HitRevision, uint HealthRevision, int LifeBefore, int LifeAfter, int Damage)
{
    internal bool ReachedFloor => LifeAfter == 1;
    internal bool IsValid => Nonce > 0
        && LifeBefore is > 0 and <= FirstSeveranceHurtIntent.MaximumDamage
        && LifeAfter is > 0 and <= FirstSeveranceHurtIntent.MaximumDamage
        && Damage is >= 0 and <= FirstSeveranceHurtIntent.MaximumDamage
        && (HitRevision > 0 || ReachedFloor);
}

internal readonly record struct FirstSeverancePendingHurt(
    uint Revision, FirstSeveranceHurtIntent Intent, ulong IssuedTick, string Source);

// One bounded ledger per frozen participant, owned and discarded by its Fight.
internal sealed class FirstSeveranceHurtLedger
{
    internal const int Capacity = 16;
    internal const ulong MaximumAgeTicks = 600;
    private readonly Dictionary<uint, FirstSeverancePendingHurt> pending = new();
    private uint nextHit, lastResult;
    private uint? expiredGeneration;

    internal bool TryIssue(FirstSeveranceHurtIntent intent, ulong tick, string source,
        out FirstSeverancePendingHurt hit)
    {
        Prune(intent.HealthRevision, tick);
        hit = default;
        if (!intent.IsValid || pending.Count >= Capacity || nextHit == uint.MaxValue) return false;
        hit = new(++nextHit, intent, tick, source);
        pending.Add(hit.Revision, hit);
        return true;
    }

    internal bool TryAccept(in FirstSeveranceHurtResult result, uint healthRevision, ulong tick,
        out FirstSeverancePendingHurt hit)
    {
        hit = default;
        if (!result.IsValid || result.Nonce <= lastResult || result.HealthRevision != healthRevision) return false;
        Prune(healthRevision, tick);
        if (result.HitRevision != 0)
        {
            if (!pending.TryGetValue(result.HitRevision, out hit)
                || hit.Intent.HealthRevision != healthRevision) return false;
            pending.Remove(result.HitRevision);
        }
        lastResult = result.Nonce;
        return true;
    }

    private void Prune(uint generation, ulong tick)
    {
        // At most sixteen entries, no per-tick scan/allocation outside hit ingress.
        List<uint>? expired = null;
        foreach (var entry in pending)
            if (entry.Value.Intent.HealthRevision != generation || tick < entry.Value.IssuedTick
                || tick - entry.Value.IssuedTick > MaximumAgeTicks)
            {
                if (entry.Value.Intent.HealthRevision == generation) expiredGeneration = generation;
                (expired ??= new()).Add(entry.Key);
            }
        if (expired is not null) foreach (uint key in expired) pending.Remove(key);
    }

    internal bool Awaiting(uint generation, ulong tick, out bool timedOut)
    {
        Prune(generation, tick);
        timedOut = expiredGeneration == generation;
        return timedOut || pending.Count > 0;
    }
}

internal struct FirstSeveranceDownLatch
{
    internal bool Pending { get; private set; }
    private uint generation;
    internal void Request(uint healthRevision) { Pending = true; generation = healthRevision; }
    internal void Observe(uint healthRevision, bool authorityDown)
    {
        if (authorityDown || healthRevision > generation) Pending = false;
    }
}
