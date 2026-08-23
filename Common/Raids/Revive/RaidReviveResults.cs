#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Raids.Revive;

internal enum RaidParticipantCombatState : byte
{
    Alive = 0,
    Downed = 1,
    Eliminated = 2,
}

internal enum RaidReviveCancelReason : byte
{
    None = 0,
    Manual = 1,
    ReviverMoved = 2,
    ReviverDamaged = 3,
    TargetInvalid = 4,
    ReviverInvalid = 5,
    ParticipantDisconnected = 6,
    EncounterEnded = 7,
}

internal enum RaidReviveEventKind : byte
{
    ParticipantDowned = 1,
    ChannelStarted = 2,
    ChannelCancelled = 3,
    ParticipantRevived = 4,
    ParticipantEliminated = 5,
    ParticipantDisconnected = 6,
    ParticipantRejoined = 7,
    RaidFailed = 8,
}

internal enum RaidReviveFailureReason : byte
{
    None = 0,
    AllParticipantsDowned = 1,
    NoAvailableParticipants = 2,
    DownedTimeoutWithoutToken = 3,
}

internal enum RaidParticipantEliminationReason : byte
{
    None = 0,
    DownedTimeout = 1,
    DisconnectGraceExpired = 2,
}

internal enum RaidReviveCommandDisposition : byte
{
    Rejected = 0,
    NoChange = 1,
    Applied = 2,
}

internal readonly record struct RaidReviveEvent(
    FightId FightId,
    uint Revision,
    ulong AuthorityTick,
    RaidReviveEventKind Kind,
    ParticipantId Subject,
    ParticipantId Actor,
    uint ChannelNonce,
    RaidReviveCancelReason CancelReason,
    RaidReviveFailureReason FailureReason,
    RaidParticipantEliminationReason EliminationReason,
    float RestoredLifeRatio,
    ulong InvulnerabilityUntilTick,
    ulong WeaknessUntilTick);

internal readonly record struct RaidReviveCommandResult(
    RaidReviveCommandDisposition Disposition,
    string FailureCode,
    IReadOnlyList<RaidReviveEvent> Events)
{
    public bool IsAccepted => Disposition != RaidReviveCommandDisposition.Rejected;

    public bool HasObservableChange => Events.Count > 0;

    public static RaidReviveCommandResult Rejected(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A rejection requires a failure code.", nameof(failureCode));
        }

        return new RaidReviveCommandResult(
            RaidReviveCommandDisposition.Rejected,
            failureCode,
            Array.Empty<RaidReviveEvent>());
    }

    public static RaidReviveCommandResult NoChange(string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("A no-change result requires a reason code.", nameof(reasonCode));
        }

        return new RaidReviveCommandResult(
            RaidReviveCommandDisposition.NoChange,
            reasonCode,
            Array.Empty<RaidReviveEvent>());
    }

    public static RaidReviveCommandResult Applied(IReadOnlyList<RaidReviveEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return new RaidReviveCommandResult(
            RaidReviveCommandDisposition.Applied,
            string.Empty,
            events);
    }
}

// A start batch is the authority adapter's complete set of revive-start requests
// for one ingress tick. Results are returned in the same deterministic order in
// which the service adjudicated the requests, rather than packet arrival order.
internal sealed class RaidReviveStartBatchResult
{
    private RaidReviveStartBatchResult(
        bool isAccepted,
        string failureCode,
        IReadOnlyList<RaidReviveCommandResult> commandResults)
    {
        IsAccepted = isAccepted;
        FailureCode = failureCode;
        CommandResults = commandResults;
    }

    public bool IsAccepted { get; }

    public string FailureCode { get; }

    public IReadOnlyList<RaidReviveCommandResult> CommandResults { get; }

    public bool HasObservableChange
    {
        get
        {
            for (int index = 0; index < CommandResults.Count; index++)
            {
                if (CommandResults[index].HasObservableChange)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static RaidReviveStartBatchResult Rejected(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A batch rejection requires a failure code.", nameof(failureCode));
        }

        return new RaidReviveStartBatchResult(
            isAccepted: false,
            failureCode,
            Array.Empty<RaidReviveCommandResult>());
    }

    public static RaidReviveStartBatchResult Accepted(
        IReadOnlyList<RaidReviveCommandResult> commandResults)
    {
        ArgumentNullException.ThrowIfNull(commandResults);
        return new RaidReviveStartBatchResult(
            isAccepted: true,
            string.Empty,
            commandResults);
    }
}
