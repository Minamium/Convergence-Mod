#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceTerminalCause : byte
{
    None = 0,
    BossLifeZero = 1,
    OverloadLimit = 2,
    AllParticipantsDowned = 3,
    RecoveryImpossible = 4,
    LoopCapExceeded = 5,
    UserCancelled = 6,
    FoundationCoreLost = 7,
    BossActorMissing = 8,
    RuntimeInvariantBroken = 9,
    AdministrativeAbort = 10,
    WorldUnload = 11,
    ProtocolFailure = 12,
    InternalFailure = 13,
}

internal sealed class FirstSeveranceTerminationContract : IEncounterTerminationContract
{
    public const ushort SchemaId = 1;
    public const byte SchemaVersion = 1;

    private static readonly IReadOnlyList<EncounterTerminationDescriptor> ExternalMapping =
        Array.AsReadOnly(
            new[]
            {
                CreateDescriptor(FirstSeveranceTerminalCause.WorldUnload),
                CreateDescriptor(FirstSeveranceTerminalCause.InternalFailure),
                CreateDescriptor(FirstSeveranceTerminalCause.ProtocolFailure),
            });

    public static FirstSeveranceTerminationContract Instance { get; } = new();

    private FirstSeveranceTerminationContract()
    {
    }

    public ushort FeatureSchemaId => SchemaId;

    public byte FeatureSchemaVersion => SchemaVersion;

    public IReadOnlyList<EncounterTerminationDescriptor> ExternalTerminations =>
        ExternalMapping;

    public EncounterTerminationDescriptor Create(FirstSeveranceTerminalCause cause)
    {
        return CreateDescriptor(cause);
    }

    public bool IsValid(in EncounterTerminationDescriptor descriptor)
    {
        if (descriptor.IsNone
            || descriptor.Feature.SchemaId != SchemaId
            || descriptor.Feature.SchemaVersion != SchemaVersion)
        {
            return false;
        }

        FirstSeveranceTerminalCause cause =
            (FirstSeveranceTerminalCause)descriptor.Feature.Cause;
        return cause != FirstSeveranceTerminalCause.None
            && Enum.IsDefined(cause)
            && descriptor.EndReason == GetEndReason(cause);
    }

    public FirstSeveranceTerminalCause GetCause(
        in EncounterTerminationDescriptor descriptor)
    {
        if (!IsValid(descriptor))
        {
            throw new ArgumentException(
                "The descriptor is not valid for First Severance.",
                nameof(descriptor));
        }

        return (FirstSeveranceTerminalCause)descriptor.Feature.Cause;
    }

    public FirstSeveranceTerminalCause SelectHigherPriority(
        FirstSeveranceTerminalCause first,
        FirstSeveranceTerminalCause second)
    {
        int firstPriority = GetFeaturePriority(first);
        int secondPriority = GetFeaturePriority(second);
        return firstPriority >= secondPriority ? first : second;
    }

    public static EncounterEndReason GetEndReason(FirstSeveranceTerminalCause cause)
    {
        return cause switch
        {
            FirstSeveranceTerminalCause.BossLifeZero => EncounterEndReason.Victory,
            FirstSeveranceTerminalCause.OverloadLimit
                or FirstSeveranceTerminalCause.AllParticipantsDowned
                or FirstSeveranceTerminalCause.RecoveryImpossible
                or FirstSeveranceTerminalCause.LoopCapExceeded => EncounterEndReason.Defeat,
            FirstSeveranceTerminalCause.UserCancelled => EncounterEndReason.Cancelled,
            FirstSeveranceTerminalCause.FoundationCoreLost =>
                EncounterEndReason.AnchorDestroyed,
            FirstSeveranceTerminalCause.BossActorMissing =>
                EncounterEndReason.EncounterActorMissing,
            FirstSeveranceTerminalCause.RuntimeInvariantBroken
                or FirstSeveranceTerminalCause.AdministrativeAbort =>
                EncounterEndReason.Invalidated,
            FirstSeveranceTerminalCause.WorldUnload => EncounterEndReason.WorldUnload,
            FirstSeveranceTerminalCause.ProtocolFailure => EncounterEndReason.ProtocolFailure,
            FirstSeveranceTerminalCause.InternalFailure => EncounterEndReason.InternalFailure,
            _ => throw new ArgumentOutOfRangeException(nameof(cause)),
        };
    }

    public static bool IsFeatureOwned(FirstSeveranceTerminalCause cause)
    {
        return cause is >= FirstSeveranceTerminalCause.BossLifeZero
            and <= FirstSeveranceTerminalCause.AdministrativeAbort;
    }

    private static EncounterTerminationDescriptor CreateDescriptor(
        FirstSeveranceTerminalCause cause)
    {
        EncounterFeatureTermination feature = EncounterFeatureTermination.Create(
            SchemaId,
            SchemaVersion,
            checked((byte)cause));
        return EncounterTerminationDescriptor.Create(GetEndReason(cause), feature);
    }

    private static int GetFeaturePriority(FirstSeveranceTerminalCause cause)
    {
        return cause switch
        {
            FirstSeveranceTerminalCause.BossActorMissing => 10,
            FirstSeveranceTerminalCause.FoundationCoreLost => 9,
            FirstSeveranceTerminalCause.RuntimeInvariantBroken => 8,
            FirstSeveranceTerminalCause.AdministrativeAbort => 7,
            FirstSeveranceTerminalCause.BossLifeZero => 6,
            FirstSeveranceTerminalCause.OverloadLimit => 5,
            FirstSeveranceTerminalCause.AllParticipantsDowned => 4,
            FirstSeveranceTerminalCause.RecoveryImpossible => 3,
            FirstSeveranceTerminalCause.LoopCapExceeded => 2,
            FirstSeveranceTerminalCause.UserCancelled => 1,
            _ => throw new ArgumentOutOfRangeException(
                nameof(cause),
                "Only feature-owned terminal causes participate in the reducer priority."),
        };
    }
}
