using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceBootstrapRuntime : IEncounterRuntime
{
    private readonly FightId fightId;
    private readonly FirstSeveranceEncounterPlan plan;
    private readonly IFirstSeveranceWorldAdapter worldAdapter;
    private bool isCleaned;

    public FirstSeveranceBootstrapRuntime(
        FightId fightId,
        FirstSeveranceEncounterPlan plan,
        IFirstSeveranceWorldAdapter worldAdapter)
    {
        if (fightId.IsNone)
        {
            throw new ArgumentException("A runtime requires a Fight ID.", nameof(fightId));
        }

        this.fightId = fightId;
        this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
        this.worldAdapter = worldAdapter ?? throw new ArgumentNullException(nameof(worldAdapter));
    }

    internal FirstSeveranceEncounterPlan Plan => plan;

    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (isCleaned)
        {
            throw new InvalidOperationException("A cleaned runtime cannot be ticked.");
        }

        if (context.FightId != fightId)
        {
            throw new InvalidOperationException("Encounter runtime context has the wrong Fight ID.");
        }

        // The availability policy currently prevents construction. If another code
        // path bypasses it, fail closed rather than calculating from RequestedAnchor
        // or mutating the World through an unimplemented adapter.
        if (!worldAdapter.IsOperational)
        {
            return EncounterRuntimeUpdate.End(EncounterEndReason.Invalidated);
        }

        // Milestone 1 replaces this bootstrap with server-side Core resolution,
        // validation, readiness, and a typed schedule executor.
        return EncounterRuntimeUpdate.None;
    }

    public void Cleanup(in EncounterCleanupContext context)
    {
        if (isCleaned)
        {
            return;
        }

        if (context.FightId != fightId)
        {
            throw new InvalidOperationException("Cleanup context has the wrong Fight ID.");
        }

        worldAdapter.Cleanup(context);
        isCleaned = true;
    }
}
