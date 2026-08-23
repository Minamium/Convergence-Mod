using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal readonly record struct ThirdSeveranceWorldMutationDecision(
    bool IsAccepted,
    string FailureCode)
{
    public static ThirdSeveranceWorldMutationDecision Accept => new(true, string.Empty);

    public static ThirdSeveranceWorldMutationDecision Reject(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A rejected mutation requires a failure code.", nameof(failureCode));
        }

        return new ThirdSeveranceWorldMutationDecision(false, failureCode);
    }
}

// This port is the only planned route from the feature plan to Terraria world
// mutation. Its production implementation belongs to later milestones and must
// run on server/Single Player authority only.
internal interface IThirdSeveranceWorldAdapter : IEncounterCleanupParticipant
{
    bool IsOperational { get; }

    ThirdSeveranceWorldMutationDecision TryDeployArena(
        FightId fightId,
        ThirdSeveranceArenaLayout layout);

    ThirdSeveranceWorldMutationDecision TrySpawnEncounterActors(
        FightId fightId,
        string actorKey,
        int count);

    // Implementations re-resolve ServerWhoAmI and require the same connection
    // epoch immediately before applying a response; a slot is never identity.
    ThirdSeveranceWorldMutationDecision TryApplyBoundaryResponse(
        FightId fightId,
        in ThirdSeveranceArenaOccupant occupant,
        ThirdSeveranceBoundaryResponse response);

    ThirdSeveranceWorldMutationDecision TrySetWeakPointExposure(
        FightId fightId,
        string weakPointKey,
        bool isExposed);
}

internal sealed class InertThirdSeveranceWorldAdapter : IThirdSeveranceWorldAdapter
{
    private const string InertFailureCode = "third_severance.world_adapter_inert";

    public static InertThirdSeveranceWorldAdapter Instance { get; } = new();

    private InertThirdSeveranceWorldAdapter()
    {
    }

    public bool IsOperational => false;

    public ThirdSeveranceWorldMutationDecision TryDeployArena(
        FightId fightId,
        ThirdSeveranceArenaLayout layout)
    {
        _ = fightId;
        _ = layout;
        return ThirdSeveranceWorldMutationDecision.Reject(InertFailureCode);
    }

    public ThirdSeveranceWorldMutationDecision TrySpawnEncounterActors(
        FightId fightId,
        string actorKey,
        int count)
    {
        _ = fightId;
        _ = actorKey;
        _ = count;
        return ThirdSeveranceWorldMutationDecision.Reject(InertFailureCode);
    }

    public ThirdSeveranceWorldMutationDecision TryApplyBoundaryResponse(
        FightId fightId,
        in ThirdSeveranceArenaOccupant occupant,
        ThirdSeveranceBoundaryResponse response)
    {
        _ = fightId;
        _ = occupant;
        _ = response;
        return ThirdSeveranceWorldMutationDecision.Reject(InertFailureCode);
    }

    public ThirdSeveranceWorldMutationDecision TrySetWeakPointExposure(
        FightId fightId,
        string weakPointKey,
        bool isExposed)
    {
        _ = fightId;
        _ = weakPointKey;
        _ = isExposed;
        return ThirdSeveranceWorldMutationDecision.Reject(InertFailureCode);
    }

    public void Cleanup(in EncounterCleanupContext context)
    {
        _ = context;
        // The inert adapter never creates Tiles, NPCs, Projectiles, or player state.
    }
}
