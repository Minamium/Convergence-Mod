#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Encounters.FirstSeverance.Development;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeveranceRosterCandidate(
    int ServerWhoAmI,
    ulong ConnectionEpoch,
    bool IsConnected,
    bool IsEligible,
    bool IsWithinParticipationRegion)
{
    public bool HasValidBinding => ServerWhoAmI is >= 0
        and < FirstSeveranceArenaOccupant.TerrariaPlayerSlotCount
        && ConnectionEpoch != 0;

    public bool IsSelectable => HasValidBinding
        && IsConnected
        && IsEligible
        && IsWithinParticipationRegion;
}

internal readonly record struct FirstSeveranceRosterMember(
    ParticipantId ParticipantId,
    int ServerWhoAmI,
    ulong ConnectionEpoch)
{
    public bool IsValid => ParticipantId.IsValid
        && ServerWhoAmI is >= 0 and < FirstSeveranceArenaOccupant.TerrariaPlayerSlotCount
        && ConnectionEpoch != 0;
}

internal sealed class FirstSeveranceRoster
{
    // Wire/runtime capacity is separate from the authority's admission policy.
    public const int MinimumCount = 1;
    public const int ProductionMinimumCount = 2;
    public const int MaximumCount = 4;

    private FirstSeveranceRoster(
        IReadOnlyList<FirstSeveranceRosterMember> members,
        ParticipantId initiatorParticipantId)
    {
        Members = FirstSeverancePlanCollections.Copy(members, nameof(members));
        InitiatorParticipantId = initiatorParticipantId;
    }

    public IReadOnlyList<FirstSeveranceRosterMember> Members { get; }

    public ParticipantId InitiatorParticipantId { get; }

    public int Count => Members.Count;

    internal bool MatchesConnected(IReadOnlyList<FirstSeveranceConnectionObservation> connections)
    {
        int count = 0;
        var seen = new HashSet<int>();
        foreach (var connection in connections)
        {
            if (!connection.IsConnected) continue;
            if (!seen.Add(connection.ServerWhoAmI)
                || !TryResolveCurrentBinding(connection.ServerWhoAmI, connection.ConnectionEpoch, out _)) return false;
            count++;
        }
        return count == Count;
    }

    public static bool TryCreate(
        IReadOnlyList<FirstSeveranceRosterCandidate> candidates,
        int initiatorWhoAmI,
        ulong initiatorConnectionEpoch,
        out FirstSeveranceRoster? roster,
        out string failureCode,
        bool allowSoloDebug = false,
        bool requireAllConnected = false)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        roster = null;
        var selectable = new List<FirstSeveranceRosterCandidate>(candidates.Count);
        var seenSlots = new HashSet<int>();
        for (int index = 0; index < candidates.Count; index++)
        {
            FirstSeveranceRosterCandidate candidate = candidates[index];
            if (!candidate.HasValidBinding || !seenSlots.Add(candidate.ServerWhoAmI))
            {
                failureCode = "first_severance.roster_candidate_invalid";
                return false;
            }

            if (requireAllConnected && candidate.IsConnected && !candidate.IsEligible)
            {
                failureCode = "first_severance.roster_player_not_alive";
                return false;
            }
            if (requireAllConnected ? candidate.IsConnected && candidate.IsEligible : candidate.IsSelectable)
            {
                selectable.Add(candidate);
            }
        }

        selectable.Sort(static (left, right) =>
        {
            int slotOrder = left.ServerWhoAmI.CompareTo(right.ServerWhoAmI);
            return slotOrder != 0
                ? slotOrder
                : left.ConnectionEpoch.CompareTo(right.ConnectionEpoch);
        });

        if (selectable.Count < FirstSeveranceDevelopmentPolicy.MinimumFor(allowSoloDebug))
        {
            failureCode = FirstSeveranceArenaIssueCodes.TooFewParticipants;
            return false;
        }

        if (selectable.Count > MaximumCount)
        {
            failureCode = FirstSeveranceArenaIssueCodes.SelectionRequired;
            return false;
        }

        int initiatorIndex = -1;
        var members = new FirstSeveranceRosterMember[selectable.Count];
        for (int index = 0; index < selectable.Count; index++)
        {
            FirstSeveranceRosterCandidate candidate = selectable[index];
            var participantId = new ParticipantId(checked((byte)index));
            members[index] = new FirstSeveranceRosterMember(
                participantId,
                candidate.ServerWhoAmI,
                candidate.ConnectionEpoch);
            if (candidate.ServerWhoAmI == initiatorWhoAmI
                && candidate.ConnectionEpoch == initiatorConnectionEpoch)
            {
                initiatorIndex = index;
            }
        }

        if (initiatorIndex < 0)
        {
            failureCode = "first_severance.roster_initiator_not_eligible";
            return false;
        }

        roster = new FirstSeveranceRoster(
            Array.AsReadOnly(members),
            members[initiatorIndex].ParticipantId);
        failureCode = string.Empty;
        return true;
    }

    public bool TryResolveCurrentBinding(
        int serverWhoAmI,
        ulong connectionEpoch,
        out FirstSeveranceRosterMember member)
    {
        for (int index = 0; index < Members.Count; index++)
        {
            FirstSeveranceRosterMember candidate = Members[index];
            if (candidate.ServerWhoAmI == serverWhoAmI
                && candidate.ConnectionEpoch == connectionEpoch)
            {
                member = candidate;
                return true;
            }
        }

        member = default;
        return false;
    }

    public bool TryGet(ParticipantId participantId, out FirstSeveranceRosterMember member)
    {
        if (participantId.IsValid && participantId.Value < Members.Count)
        {
            member = Members[participantId.Value];
            return member.ParticipantId == participantId;
        }

        member = default;
        return false;
    }
}
