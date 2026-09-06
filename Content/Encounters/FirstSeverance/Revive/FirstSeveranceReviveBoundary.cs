#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;

namespace Convergence.Content.Encounters.FirstSeverance.Revive;

// Feature composition boundary. The future validated-roster phase creates this
// on the authority and forwards only server-resolved commands. Until that adapter
// exists, First Severance remains disabled by its availability policy.
internal sealed class FirstSeveranceReviveBoundary : IEncounterCleanupParticipant
{
    private readonly FightId fightId;
    private readonly Action<RaidReviveEvent>? observeEvent;
    private RaidReviveService? service;
    private bool hasPendingObservableChange;
    private bool isCleaned;

    public FirstSeveranceReviveBoundary(FightId fightId, Action<RaidReviveEvent>? observeEvent = null)
    {
        if (fightId.IsNone)
        {
            throw new ArgumentException("The revive boundary requires a Fight ID.", nameof(fightId));
        }

        this.fightId = fightId;
        this.observeEvent = observeEvent;
    }

    public bool IsInitialized => service is not null;

    public bool TryInitialize(
        IReadOnlyList<RaidParticipantBinding> authoritativeRoster,
        out string failureCode)
    {
        if (isCleaned)
        {
            failureCode = "first_severance.revive_boundary_cleaned";
            return false;
        }

        if (service is not null)
        {
            failureCode = "first_severance.revive_already_initialized";
            return false;
        }

        ArgumentNullException.ThrowIfNull(authoritativeRoster);
        if (authoritativeRoster.Count is < FirstSeveranceRoster.MinimumCount or > FirstSeveranceRoster.MaximumCount)
        {
            failureCode = "first_severance.revive_roster_size_invalid";
            return false;
        }

        try
        {
            service = new RaidReviveService(
                fightId,
                RaidReviveSettings.CreateInstantUnlimited(authoritativeRoster.Count),
                authoritativeRoster);
            failureCode = string.Empty;
            return true;
        }
        catch (ArgumentException)
        {
            failureCode = "first_severance.revive_roster_invalid";
            return false;
        }
    }

    public RaidReviveCommandResult Apply(in AuthoritativeParticipantDownedCommand command)
    {
        return TrackObservableChange(
            service?.Apply(command)
                ?? RaidReviveCommandResult.Rejected("first_severance.revive_not_initialized"));
    }

    // The future authority adapter calls this once with the complete start-request
    // ingress set for a tick; it must not split packet arrival groups into calls.
    public RaidReviveStartBatchResult ApplyStartBatch(
        IReadOnlyList<RaidReviveStartCommand> commands)
    {
        return TrackObservableChange(
            service?.ApplyStartBatch(commands)
                ?? RaidReviveStartBatchResult.Rejected(
                    "first_severance.revive_not_initialized"));
    }

    public RaidReviveCommandResult Apply(in RaidReviveInterruptCommand command)
    {
        return TrackObservableChange(
            service?.Apply(command)
                ?? RaidReviveCommandResult.Rejected("first_severance.revive_not_initialized"));
    }

    public RaidReviveCommandResult Apply(in RaidParticipantDisconnectedCommand command)
    {
        return TrackObservableChange(
            service?.Apply(command)
                ?? RaidReviveCommandResult.Rejected("first_severance.revive_not_initialized"));
    }

    public RaidReviveCommandResult Apply(in RaidParticipantRejoinedCommand command)
    {
        return TrackObservableChange(
            service?.Apply(command)
                ?? RaidReviveCommandResult.Rejected("first_severance.revive_not_initialized"));
    }

    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (service is null || context.Lifecycle != EncounterLifecycle.Active)
        {
            return EncounterRuntimeUpdate.None;
        }

        RaidReviveCommandResult result = service.CommitTick(
            context.FightId,
            context.AuthorityTick);
        ObserveEvents(result);
        bool hasObservableChange = hasPendingObservableChange || result.HasObservableChange;
        hasPendingObservableChange = false;
        if (service.FailureReason != RaidReviveFailureReason.None)
        {
            FirstSeveranceTerminalCause cause = service.FailureReason
                == RaidReviveFailureReason.AllParticipantsDowned
                    ? FirstSeveranceTerminalCause.AllParticipantsDowned
                    : FirstSeveranceTerminalCause.RecoveryImpossible;
            return EncounterRuntimeUpdate.End(
                FirstSeveranceTerminationContract.Instance.Create(cause));
        }

        if (!result.IsAccepted)
        {
            return EncounterRuntimeUpdate.End(
                FirstSeveranceTerminationContract.Instance.Create(
                    FirstSeveranceTerminalCause.RuntimeInvariantBroken));
        }

        return hasObservableChange
            ? EncounterRuntimeUpdate.ObservableChange()
            : EncounterRuntimeUpdate.None;
    }

    public bool TryGetControlProjection(
        in RaidParticipantBinding binding,
        ulong authorityTick,
        out RaidParticipantControlProjection projection)
    {
        if (service is null)
        {
            projection = default;
            return false;
        }

        return service.TryGetControlProjection(binding, authorityTick, out projection);
    }

    public RaidReviveSnapshot? CreateSnapshot()
    {
        return service?.CreateSnapshot();
    }

    public void Cleanup(in EncounterCleanupContext context)
    {
        if (isCleaned)
        {
            return;
        }

        if (context.FightId != fightId)
        {
            throw new InvalidOperationException("Revive cleanup received a stale Fight ID.");
        }

        if (service is not null && !service.TryCleanup(context.FightId))
        {
            throw new InvalidOperationException("Revive service rejected its owning cleanup request.");
        }

        service = null;
        hasPendingObservableChange = false;
        isCleaned = true;
    }

    private RaidReviveCommandResult TrackObservableChange(RaidReviveCommandResult result)
    {
        hasPendingObservableChange |= result.HasObservableChange;
        ObserveEvents(result);
        return result;
    }

    private RaidReviveStartBatchResult TrackObservableChange(
        RaidReviveStartBatchResult result)
    {
        hasPendingObservableChange |= result.HasObservableChange;
        foreach (RaidReviveCommandResult command in result.CommandResults)
            ObserveEvents(command);
        return result;
    }

    private void ObserveEvents(RaidReviveCommandResult result)
    {
        if (observeEvent is null)
            return;
        foreach (RaidReviveEvent entry in result.Events)
        {
            // Diagnostics must never affect a committed gameplay transition.
            try { observeEvent(entry); }
            catch (Exception) { }
        }
    }
}
