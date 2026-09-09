#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeverancePreparationSettings
{
    public FirstSeverancePreparationSettings(int readyTimeoutTicks)
    {
        if (readyTimeoutTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(readyTimeoutTicks));
        }

        ReadyTimeoutTicks = readyTimeoutTicks;
    }

    // Provisional 60-second preparation window. It is tuning, not protocol identity.
    public static FirstSeverancePreparationSettings Default { get; } = new(60 * 60);

    public int ReadyTimeoutTicks { get; }
}

internal readonly record struct FirstSeveranceConnectionObservation(
    int ServerWhoAmI,
    ulong ConnectionEpoch,
    bool IsConnected);

internal readonly record struct FirstSeveranceSetReadyCommand(
    int SenderWhoAmI,
    ulong ConnectionEpoch,
    bool IsReady,
    uint RequestNonce,
    ulong AuthorityTick);

internal readonly record struct FirstSeveranceCancelPreparationCommand(
    int SenderWhoAmI,
    ulong ConnectionEpoch,
    uint RequestNonce,
    ulong AuthorityTick);

internal readonly record struct FirstSeverancePreparationMemberSnapshot(
    ParticipantId ParticipantId,
    int ServerWhoAmI,
    ulong ConnectionEpoch,
    bool IsReady);

internal sealed class FirstSeverancePreparationSnapshot
{
    public FirstSeverancePreparationSnapshot(
        FightId fightId,
        ulong enteredTick,
        ulong deadlineTick,
        bool combatGateClosed,
        bool isClosed,
        EncounterTerminationDescriptor termination,
        IReadOnlyList<FirstSeverancePreparationMemberSnapshot> members)
    {
        if (fightId.IsNone)
        {
            throw new ArgumentException("A preparation snapshot requires a Fight ID.", nameof(fightId));
        }

        FightId = fightId;
        EnteredTick = enteredTick;
        DeadlineTick = deadlineTick;
        CombatGateClosed = combatGateClosed;
        IsClosed = isClosed;
        Termination = termination;
        Members = FirstSeverancePlanCollections.Copy(members, nameof(members));
    }

    public FightId FightId { get; }

    public ulong EnteredTick { get; }

    public ulong DeadlineTick { get; }

    public bool CombatGateClosed { get; }

    public bool IsClosed { get; }

    public EncounterTerminationDescriptor Termination { get; }

    public IReadOnlyList<FirstSeverancePreparationMemberSnapshot> Members { get; }

    public bool AreAllReady
    {
        get
        {
            for (int index = 0; index < Members.Count; index++)
            {
                if (!Members[index].IsReady)
                {
                    return false;
                }
            }

            return Members.Count > 0;
        }
    }
}

internal readonly record struct FirstSeverancePreparationUpdate(
    bool IsAccepted,
    bool HasObservableChange,
    string FailureCode,
    EncounterTerminationDescriptor RequestedTermination)
{
    public bool RequestsEnd => !RequestedTermination.IsNone;

    public static FirstSeverancePreparationUpdate NoChange =>
        new(true, false, string.Empty, EncounterTerminationDescriptor.None);

    public static FirstSeverancePreparationUpdate Changed =>
        new(true, true, string.Empty, EncounterTerminationDescriptor.None);

    public static FirstSeverancePreparationUpdate Reject(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A rejection requires a failure code.", nameof(failureCode));
        }

        return new FirstSeverancePreparationUpdate(
            false,
            false,
            failureCode,
            EncounterTerminationDescriptor.None);
    }

    public static FirstSeverancePreparationUpdate End(
        string diagnosticCode,
        EncounterTerminationDescriptor termination)
    {
        if (string.IsNullOrWhiteSpace(diagnosticCode) || termination.IsNone)
        {
            throw new ArgumentException("A preparation ending requires diagnostics and termination.");
        }

        return new FirstSeverancePreparationUpdate(true, true, diagnosticCode, termination);
    }
}

internal sealed class FirstSeverancePreparationStateMachine
{
    private readonly FirstSeveranceRoster roster;
    private readonly bool[] ready;
    private readonly uint[] lastRequestNonces;
    private ulong lastAuthorityTick;
    private bool isCleaned;

    public FirstSeverancePreparationStateMachine(
        FightId fightId,
        FirstSeveranceRoster roster,
        ulong enteredTick,
        in FirstSeverancePreparationSettings settings,
        int deploymentTicks = 0)
    {
        if (fightId.IsNone)
        {
            throw new ArgumentException("Preparation requires a Fight ID.", nameof(fightId));
        }

        this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
        if (roster.Count is < FirstSeveranceRoster.MinimumCount
            or > FirstSeveranceRoster.MaximumCount)
        {
            throw new ArgumentOutOfRangeException(nameof(roster));
        }

        if (settings.ReadyTimeoutTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                "Preparation requires a positive Ready timeout.");
        }

        FightId = fightId;
        EnteredTick = enteredTick;
        if (deploymentTicks < 0) throw new ArgumentOutOfRangeException(nameof(deploymentTicks));
        ReadyOpensTick = checked(enteredTick + (ulong)deploymentTicks);
        DeadlineTick = checked(ReadyOpensTick + (ulong)settings.ReadyTimeoutTicks);
        lastAuthorityTick = enteredTick;
        ready = new bool[roster.Count];
        lastRequestNonces = new uint[roster.Count];
    }

    public FightId FightId { get; }

    public ulong EnteredTick { get; }

    public ulong DeadlineTick { get; }

    public ulong ReadyOpensTick { get; }

    public bool CombatGateClosed => true;

    public bool IsClosed => !RequestedTermination.IsNone;

    public EncounterTerminationDescriptor RequestedTermination { get; private set; }

    public FirstSeverancePreparationUpdate Advance(
        ulong authorityTick,
        bool coreIsPresent,
        IReadOnlyList<FirstSeveranceConnectionObservation> connections)
    {
        ArgumentNullException.ThrowIfNull(connections);

        if (isCleaned)
        {
            return FirstSeverancePreparationUpdate.Reject(
                "first_severance.preparation_cleaned");
        }

        if (authorityTick < lastAuthorityTick)
        {
            return FirstSeverancePreparationUpdate.Reject(
                "first_severance.preparation_stale_tick");
        }

        lastAuthorityTick = authorityTick;
        if (IsClosed)
        {
            return FirstSeverancePreparationUpdate.NoChange;
        }

        if (!coreIsPresent)
        {
            return Close("first_severance.preparation_core_removed");
        }

        if (!AllBindingsAreCurrent(connections))
        {
            return Close("first_severance.preparation_participant_lost");
        }

        if (authorityTick >= DeadlineTick)
        {
            return Close("first_severance.preparation_timeout");
        }

        return FirstSeverancePreparationUpdate.NoChange;
    }

    public FirstSeverancePreparationUpdate ApplySetReady(
        in FirstSeveranceSetReadyCommand command)
    {
        if (command.AuthorityTick < ReadyOpensTick)
            return FirstSeverancePreparationUpdate.Reject("first_severance.preparation_field_deploying");
        if (!CanApplyCommand(command.AuthorityTick, out string failureCode))
        {
            return FirstSeverancePreparationUpdate.Reject(failureCode);
        }

        if (!roster.TryResolveCurrentBinding(
                command.SenderWhoAmI,
                command.ConnectionEpoch,
                out FirstSeveranceRosterMember member))
        {
            return FirstSeverancePreparationUpdate.Reject(
                "first_severance.preparation_sender_not_bound");
        }

        int participantIndex = member.ParticipantId.Value;
        if (!TryAcceptNonce(participantIndex, command.RequestNonce))
        {
            return FirstSeverancePreparationUpdate.Reject(
                "first_severance.preparation_stale_nonce");
        }

        lastAuthorityTick = command.AuthorityTick;
        bool changed = ready[participantIndex] != command.IsReady;
        ready[participantIndex] = command.IsReady;
        return changed
            ? FirstSeverancePreparationUpdate.Changed
            : FirstSeverancePreparationUpdate.NoChange;
    }

    public FirstSeverancePreparationUpdate ApplyCancel(
        in FirstSeveranceCancelPreparationCommand command)
    {
        if (!CanApplyCommand(command.AuthorityTick, out string failureCode))
        {
            return FirstSeverancePreparationUpdate.Reject(failureCode);
        }

        if (!roster.TryResolveCurrentBinding(
                command.SenderWhoAmI,
                command.ConnectionEpoch,
                out FirstSeveranceRosterMember member))
        {
            return FirstSeverancePreparationUpdate.Reject(
                "first_severance.preparation_sender_not_bound");
        }

        if (member.ParticipantId != roster.InitiatorParticipantId)
        {
            return FirstSeverancePreparationUpdate.Reject(
                "first_severance.preparation_cancel_not_initiator");
        }

        int participantIndex = member.ParticipantId.Value;
        if (!TryAcceptNonce(participantIndex, command.RequestNonce))
        {
            return FirstSeverancePreparationUpdate.Reject(
                "first_severance.preparation_stale_nonce");
        }

        lastAuthorityTick = command.AuthorityTick;
        return Close("first_severance.preparation_cancelled");
    }

    public FirstSeverancePreparationSnapshot CreateSnapshot()
    {
        var members = new FirstSeverancePreparationMemberSnapshot[roster.Count];
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            members[index] = new FirstSeverancePreparationMemberSnapshot(
                member.ParticipantId,
                member.ServerWhoAmI,
                member.ConnectionEpoch,
                ready[index]);
        }

        return new FirstSeverancePreparationSnapshot(
            FightId,
            EnteredTick,
            DeadlineTick,
            CombatGateClosed,
            IsClosed,
            RequestedTermination,
            Array.AsReadOnly(members));
    }

    public bool Cleanup(FightId expectedFightId)
    {
        if (expectedFightId != FightId)
        {
            return false;
        }

        if (isCleaned)
        {
            return true;
        }

        Array.Clear(ready);
        Array.Clear(lastRequestNonces);
        isCleaned = true;
        return true;
    }

    private bool CanApplyCommand(ulong authorityTick, out string failureCode)
    {
        if (isCleaned)
        {
            failureCode = "first_severance.preparation_cleaned";
            return false;
        }

        if (IsClosed)
        {
            failureCode = "first_severance.preparation_closed";
            return false;
        }

        if (authorityTick < lastAuthorityTick)
        {
            failureCode = "first_severance.preparation_stale_tick";
            return false;
        }

        if (authorityTick >= DeadlineTick)
        {
            failureCode = "first_severance.preparation_deadline_reached";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    private bool TryAcceptNonce(int participantIndex, uint requestNonce)
    {
        if (requestNonce == 0 || requestNonce <= lastRequestNonces[participantIndex])
        {
            return false;
        }

        lastRequestNonces[participantIndex] = requestNonce;
        return true;
    }

    private bool AllBindingsAreCurrent(
        IReadOnlyList<FirstSeveranceConnectionObservation> connections)
    {
        for (int memberIndex = 0; memberIndex < roster.Members.Count; memberIndex++)
        {
            FirstSeveranceRosterMember member = roster.Members[memberIndex];
            bool found = false;
            for (int observationIndex = 0; observationIndex < connections.Count; observationIndex++)
            {
                FirstSeveranceConnectionObservation observation = connections[observationIndex];
                if (observation.ServerWhoAmI != member.ServerWhoAmI)
                {
                    continue;
                }

                found = observation.IsConnected
                    && observation.ConnectionEpoch == member.ConnectionEpoch;
                break;
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private FirstSeverancePreparationUpdate Close(string diagnosticCode)
    {
        RequestedTermination = FirstSeveranceTerminationContract.Instance.Create(
            FirstSeveranceTerminalCause.UserCancelled);
        return FirstSeverancePreparationUpdate.End(
            diagnosticCode,
            RequestedTermination);
    }
}
