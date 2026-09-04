using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeveranceWorldMutationDecision(
    bool IsAccepted,
    string FailureCode)
{
    public static FirstSeveranceWorldMutationDecision Accept => new(true, string.Empty);

    public static FirstSeveranceWorldMutationDecision Reject(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A rejected mutation requires a failure code.", nameof(failureCode));
        }

        return new FirstSeveranceWorldMutationDecision(false, failureCode);
    }
}

// This port is the only planned route from the feature plan to Terraria world
// mutation. Its production implementation belongs to later milestones and must
// run on server/Single Player authority only.
internal interface IFirstSeveranceWorldAdapter : IEncounterCleanupParticipant
{
    bool IsOperational { get; }

    FirstSeveranceWorldMutationDecision TryDeployArena(
        FightId fightId,
        FirstSeveranceArenaLayout layout);

    FirstSeveranceWorldMutationDecision TrySpawnEncounterActors(
        FightId fightId,
        string actorKey,
        int count);

    // Implementations re-resolve ServerWhoAmI and require the same connection
    // epoch immediately before applying a response; a slot is never identity.
    FirstSeveranceWorldMutationDecision TryApplyBoundaryResponse(
        FightId fightId,
        in FirstSeveranceArenaOccupant occupant,
        FirstSeveranceBoundaryResponse response);

    FirstSeveranceWorldMutationDecision TrySetBossDamageGate(
        FightId fightId,
        bool canTakeDamage);
}

internal sealed class InertFirstSeveranceWorldAdapter : IFirstSeveranceWorldAdapter
{
    private const string InertFailureCode = "first_severance.world_adapter_inert";

    public static InertFirstSeveranceWorldAdapter Instance { get; } = new();

    private InertFirstSeveranceWorldAdapter()
    {
    }

    public bool IsOperational => false;

    public FirstSeveranceWorldMutationDecision TryDeployArena(
        FightId fightId,
        FirstSeveranceArenaLayout layout)
    {
        _ = fightId;
        _ = layout;
        return FirstSeveranceWorldMutationDecision.Reject(InertFailureCode);
    }

    public FirstSeveranceWorldMutationDecision TrySpawnEncounterActors(
        FightId fightId,
        string actorKey,
        int count)
    {
        _ = fightId;
        _ = actorKey;
        _ = count;
        return FirstSeveranceWorldMutationDecision.Reject(InertFailureCode);
    }

    public FirstSeveranceWorldMutationDecision TryApplyBoundaryResponse(
        FightId fightId,
        in FirstSeveranceArenaOccupant occupant,
        FirstSeveranceBoundaryResponse response)
    {
        _ = fightId;
        _ = occupant;
        _ = response;
        return FirstSeveranceWorldMutationDecision.Reject(InertFailureCode);
    }

    public FirstSeveranceWorldMutationDecision TrySetBossDamageGate(
        FightId fightId,
        bool canTakeDamage)
    {
        _ = fightId;
        _ = canTakeDamage;
        return FirstSeveranceWorldMutationDecision.Reject(InertFailureCode);
    }

    public void Cleanup(in EncounterCleanupContext context)
    {
        _ = context;
        // The inert adapter never creates Tiles, NPCs, Projectiles, or player state.
    }
}
