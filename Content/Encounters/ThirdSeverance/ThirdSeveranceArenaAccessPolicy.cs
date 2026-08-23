using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal enum ThirdSeveranceBoundaryViolationKind : byte
{
    ParticipantOutsideBarrier = 1,
    OutsiderInsideBarrier = 2,
}

internal enum ThirdSeveranceBoundaryResponse : byte
{
    ServerWarning = 1,
    MoveToNearestSafePoint = 2,
    EjectToArenaExterior = 3,
    ExcludeFromArena = 4,
    SuppressEncounterInteraction = 5,
}

internal readonly record struct ThirdSeveranceArenaOccupant(
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

internal readonly record struct ThirdSeveranceBoundaryResponseStep(
    int MinimumContinuousTicks,
    int MinimumViolationCount,
    ThirdSeveranceBoundaryResponse Response)
{
    public bool IsValid => MinimumContinuousTicks >= 0
        && MinimumViolationCount > 0
        && Enum.IsDefined(Response);

    public int DeterministicOrder => Response switch
    {
        ThirdSeveranceBoundaryResponse.ServerWarning => 0,
        ThirdSeveranceBoundaryResponse.SuppressEncounterInteraction => 1,
        ThirdSeveranceBoundaryResponse.MoveToNearestSafePoint => 2,
        ThirdSeveranceBoundaryResponse.EjectToArenaExterior => 3,
        ThirdSeveranceBoundaryResponse.ExcludeFromArena => 4,
        _ => int.MaxValue,
    };

    public bool IsEligible(int continuousTicks, int violationCount)
    {
        return IsValid
            && continuousTicks >= MinimumContinuousTicks
            && violationCount >= MinimumViolationCount;
    }
}

internal sealed class ThirdSeveranceBoundaryRule
{
    public ThirdSeveranceBoundaryRule(
        ThirdSeveranceBoundaryViolationKind violationKind,
        IReadOnlyList<ThirdSeveranceBoundaryResponseStep> responses)
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
        var responseKinds = new HashSet<ThirdSeveranceBoundaryResponse>();
        for (int index = 0; index < responses.Count; index++)
        {
            ThirdSeveranceBoundaryResponseStep response = responses[index];
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
        Responses = ThirdSeverancePlanCollections.Copy(responses, nameof(responses));
    }

    public ThirdSeveranceBoundaryViolationKind ViolationKind { get; }

    public IReadOnlyList<ThirdSeveranceBoundaryResponseStep> Responses { get; }

    // Results are returned in the constructor-validated deterministic order.
    // The authority executor records which response kinds were already applied
    // during the current violation episode to keep effects idempotent.
    public IReadOnlyList<ThirdSeveranceBoundaryResponse> GetEligibleResponses(
        int continuousTicks,
        int violationCount)
    {
        if (continuousTicks < 0 || violationCount <= 0)
        {
            return Array.Empty<ThirdSeveranceBoundaryResponse>();
        }

        var eligible = new List<ThirdSeveranceBoundaryResponse>(Responses.Count);
        for (int index = 0; index < Responses.Count; index++)
        {
            ThirdSeveranceBoundaryResponseStep step = Responses[index];
            if (step.IsEligible(continuousTicks, violationCount))
            {
                eligible.Add(step.Response);
            }
        }

        return eligible.AsReadOnly();
    }
}

internal sealed class ThirdSeveranceArenaAccessPolicy
{
    private static readonly IReadOnlyList<ThirdSeveranceBoundaryRule> DefaultRules =
        Array.AsReadOnly(
            new[]
            {
                new ThirdSeveranceBoundaryRule(
                    ThirdSeveranceBoundaryViolationKind.ParticipantOutsideBarrier,
                    Array.AsReadOnly(
                        new[]
                        {
                            new ThirdSeveranceBoundaryResponseStep(
                                1,
                                1,
                                ThirdSeveranceBoundaryResponse.ServerWarning),
                            new ThirdSeveranceBoundaryResponseStep(
                                6,
                                1,
                                ThirdSeveranceBoundaryResponse.MoveToNearestSafePoint),
                        })),
                new ThirdSeveranceBoundaryRule(
                    ThirdSeveranceBoundaryViolationKind.OutsiderInsideBarrier,
                    Array.AsReadOnly(
                        new[]
                        {
                            new ThirdSeveranceBoundaryResponseStep(
                                1,
                                1,
                                ThirdSeveranceBoundaryResponse.ServerWarning),
                            new ThirdSeveranceBoundaryResponseStep(
                                1,
                                1,
                                ThirdSeveranceBoundaryResponse.SuppressEncounterInteraction),
                            new ThirdSeveranceBoundaryResponseStep(
                                120,
                                1,
                                ThirdSeveranceBoundaryResponse.EjectToArenaExterior),
                            new ThirdSeveranceBoundaryResponseStep(
                                120,
                                3,
                                ThirdSeveranceBoundaryResponse.ExcludeFromArena),
                        })),
            });

    public static ThirdSeveranceArenaAccessPolicy Instance { get; } = new();

    private ThirdSeveranceArenaAccessPolicy()
    {
    }

    public IReadOnlyList<ThirdSeveranceBoundaryRule> Rules => DefaultRules;

    // No response in this policy damages or kills a player. The authority adapter
    // will warn first, then resolve a safe correction/ejection destination.
    public bool UsesLethalExclusion => false;
}
