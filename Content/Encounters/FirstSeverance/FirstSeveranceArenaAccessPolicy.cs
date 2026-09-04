using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceBoundaryViolationKind : byte
{
    ParticipantOutsideBarrier = 1,
    OutsiderInsideBarrier = 2,
}

internal enum FirstSeveranceBoundaryResponse : byte
{
    ServerWarning = 1,
    MoveToNearestSafePoint = 2,
    EjectToArenaExterior = 3,
    ExcludeFromArena = 4,
    SuppressEncounterInteraction = 5,
}

internal readonly record struct FirstSeveranceArenaOccupant(
    int ServerWhoAmI,
    ulong ConnectionEpoch,
    ParticipantId ParticipantId)
{
    // Terraria dedicates byte.MaxValue (255) as the non-player/server sentinel;
    // valid Main.player slots are therefore 0 through 254.
    public const int TerrariaPlayerSlotCount = byte.MaxValue;

    public bool IsValid => ServerWhoAmI is >= 0 and < TerrariaPlayerSlotCount
        && ConnectionEpoch != 0;

    public bool IsParticipant => ParticipantId.IsValid;
}

internal readonly record struct FirstSeveranceBoundaryResponseStep(
    int MinimumContinuousTicks,
    int MinimumViolationCount,
    FirstSeveranceBoundaryResponse Response)
{
    public bool IsValid => MinimumContinuousTicks >= 0
        && MinimumViolationCount > 0
        && Enum.IsDefined(Response);

    public int DeterministicOrder => Response switch
    {
        FirstSeveranceBoundaryResponse.ServerWarning => 0,
        FirstSeveranceBoundaryResponse.SuppressEncounterInteraction => 1,
        FirstSeveranceBoundaryResponse.MoveToNearestSafePoint => 2,
        FirstSeveranceBoundaryResponse.EjectToArenaExterior => 3,
        FirstSeveranceBoundaryResponse.ExcludeFromArena => 4,
        _ => int.MaxValue,
    };

    public bool IsEligible(int continuousTicks, int violationCount)
    {
        return IsValid
            && continuousTicks >= MinimumContinuousTicks
            && violationCount >= MinimumViolationCount;
    }
}

internal sealed class FirstSeveranceBoundaryRule
{
    public FirstSeveranceBoundaryRule(
        FirstSeveranceBoundaryViolationKind violationKind,
        IReadOnlyList<FirstSeveranceBoundaryResponseStep> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);

        if (!Enum.IsDefined(violationKind))
        {
            throw new ArgumentOutOfRangeException(nameof(violationKind));
        }

        if (responses.Count == 0)
        {
            throw new ArgumentException("A boundary rule requires at least one response.", nameof(responses));
        }

        int previousTickThreshold = -1;
        int previousViolationThreshold = -1;
        int previousDeterministicOrder = -1;
        var responseKinds = new HashSet<FirstSeveranceBoundaryResponse>();
        for (int index = 0; index < responses.Count; index++)
        {
            FirstSeveranceBoundaryResponseStep response = responses[index];
            bool hasSameThresholds = response.MinimumContinuousTicks == previousTickThreshold
                && response.MinimumViolationCount == previousViolationThreshold;
            if (!response.IsValid
                || !responseKinds.Add(response.Response)
                || response.MinimumContinuousTicks < previousTickThreshold
                || response.MinimumViolationCount < previousViolationThreshold
                || (hasSameThresholds
                    && response.DeterministicOrder <= previousDeterministicOrder))
            {
                throw new ArgumentException(
                    "Boundary responses must be unique and monotonically ordered by "
                    + "continuous ticks, violation count, and fixed response precedence.",
                    nameof(responses));
            }

            previousTickThreshold = response.MinimumContinuousTicks;
            previousViolationThreshold = response.MinimumViolationCount;
            previousDeterministicOrder = response.DeterministicOrder;
        }

        ViolationKind = violationKind;
        Responses = FirstSeverancePlanCollections.Copy(responses, nameof(responses));
    }

    public FirstSeveranceBoundaryViolationKind ViolationKind { get; }

    public IReadOnlyList<FirstSeveranceBoundaryResponseStep> Responses { get; }

    // Results are returned in the constructor-validated deterministic order.
    // The authority executor records which response kinds were already applied
    // during the current violation episode to keep effects idempotent.
    public IReadOnlyList<FirstSeveranceBoundaryResponse> GetEligibleResponses(
        int continuousTicks,
        int violationCount)
    {
        if (continuousTicks < 0 || violationCount <= 0)
        {
            return Array.Empty<FirstSeveranceBoundaryResponse>();
        }

        var eligible = new List<FirstSeveranceBoundaryResponse>(Responses.Count);
        for (int index = 0; index < Responses.Count; index++)
        {
            FirstSeveranceBoundaryResponseStep step = Responses[index];
            if (step.IsEligible(continuousTicks, violationCount))
            {
                eligible.Add(step.Response);
            }
        }

        return eligible.AsReadOnly();
    }
}

internal sealed class FirstSeveranceArenaAccessPolicy
{
    private static readonly IReadOnlyList<FirstSeveranceBoundaryRule> DefaultRules =
        Array.AsReadOnly(
            new[]
            {
                new FirstSeveranceBoundaryRule(
                    FirstSeveranceBoundaryViolationKind.ParticipantOutsideBarrier,
                    Array.AsReadOnly(
                        new[]
                        {
                            new FirstSeveranceBoundaryResponseStep(
                                1,
                                1,
                                FirstSeveranceBoundaryResponse.ServerWarning),
                            new FirstSeveranceBoundaryResponseStep(
                                6,
                                1,
                                FirstSeveranceBoundaryResponse.MoveToNearestSafePoint),
                        })),
                new FirstSeveranceBoundaryRule(
                    FirstSeveranceBoundaryViolationKind.OutsiderInsideBarrier,
                    Array.AsReadOnly(
                        new[]
                        {
                            new FirstSeveranceBoundaryResponseStep(
                                1,
                                1,
                                FirstSeveranceBoundaryResponse.ServerWarning),
                            new FirstSeveranceBoundaryResponseStep(
                                1,
                                1,
                                FirstSeveranceBoundaryResponse.SuppressEncounterInteraction),
                            new FirstSeveranceBoundaryResponseStep(
                                120,
                                1,
                                FirstSeveranceBoundaryResponse.EjectToArenaExterior),
                            new FirstSeveranceBoundaryResponseStep(
                                120,
                                3,
                                FirstSeveranceBoundaryResponse.ExcludeFromArena),
                        })),
            });

    public static FirstSeveranceArenaAccessPolicy Instance { get; } = new();

    private FirstSeveranceArenaAccessPolicy()
    {
    }

    public IReadOnlyList<FirstSeveranceBoundaryRule> Rules => DefaultRules;

    // No response in this policy damages or kills a player. The authority adapter
    // will warn first, then resolve a safe correction/ejection destination.
    public bool UsesLethalExclusion => false;
}
