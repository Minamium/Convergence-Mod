#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance;

// Exact-combat owner: a current assignment and at most one surviving Prism.
// Separate hit sets prevent the next telegraph from rearming the previous ray.
internal sealed class FirstSeveranceLanceLedger
{
    internal FirstSeveranceLanceVolley? Current { get; private set; }
    internal FirstSeveranceLanceVolley? Carried { get; private set; }
    private HashSet<ParticipantId> currentHits = new(), carriedHits = new();

    internal bool CanStart => Current is null || (Current.SustainedPrism && Carried is null);

    internal bool Retire(ulong tick)
    {
        bool changed = false;
        if (Current is { } current && tick >= current.EndTick)
        { Current = null; currentHits.Clear(); changed = true; }
        if (Carried is { } old && tick >= old.EndTick)
        { Carried = null; carriedHits.Clear(); changed = true; }
        return changed;
    }

    internal void Start(FirstSeveranceLanceVolley next, ulong tick)
    {
        Retire(tick);
        if (next.StartTick != tick || !CanStart || (Current is not null
            && (!next.SustainedPrism || next.Serial == Current.Serial || next.StartTick <= Current.StartTick
                || next.StartTick - Current.StartTick < (ulong)FirstSeveranceAttackPatterns.StepCadence(FirstSeveranceSubstate.PylonCheck))))
            throw new InvalidOperationException("Overlapping lance assignments violate the bounded cadence.");
        if (Current is not null)
        {
            Carried = Current;
            (currentHits, carriedHits) = (carriedHits, currentHits);
        }
        Current = next;
        currentHits.Clear();
    }

    internal void UpdateMotion(FirstSeveranceLanceVolley next)
    {
        if (Current is not { IsCharge:true } current || next.Serial != current.Serial)
            throw new InvalidOperationException("Only the current charge can accept motion.");
        Current = next;
    }

    internal bool HasHit(uint serial, ParticipantId participant)
        => Hits(serial).Contains(participant);
    internal void MarkHit(uint serial, ParticipantId participant) => Hits(serial).Add(participant);
    private HashSet<ParticipantId> Hits(uint serial)
        => Current?.Serial == serial ? currentHits : Carried?.Serial == serial ? carriedHits
            : throw new InvalidOperationException("Unknown or expired lance hit ledger.");

    internal void Clear()
    { Current = Carried = null; currentHits.Clear(); carriedHits.Clear(); }
}
