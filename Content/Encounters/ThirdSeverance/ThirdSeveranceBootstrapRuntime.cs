using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal sealed class ThirdSeveranceBootstrapRuntime : IEncounterRuntime
{
    private readonly FightId fightId;
    private bool isCleaned;

    public ThirdSeveranceBootstrapRuntime(FightId fightId, TilePoint requestedAnchor)
    {
        if (fightId.IsNone)
        {
            throw new ArgumentException("A runtime requires a Fight ID.", nameof(fightId));
        }

        this.fightId = fightId;
        RequestedAnchor = requestedAnchor;
    }

    // This is request data, not a validated arena anchor. Milestone 1 must resolve
    // the server-side Core Tile Entity before any world mutation is allowed.
    internal TilePoint RequestedAnchor { get; }

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

        // Milestone 1 adds Core ownership, arena validation, readiness, and barriers here.
        return EncounterRuntimeUpdate.None;
    }

    public void Cleanup(in EncounterCleanupContext context)
    {
        if (isCleaned)
        {
            return;
        }

        isCleaned = true;
        // The bootstrap runtime owns no world objects yet. Future owned resources must
        // be released here so cancellation, wipe, unload, and internal failures converge.
    }
}
