#nullable enable

using System;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly record struct EncounterFeatureTermination
{
    private EncounterFeatureTermination(
        ushort schemaId,
        byte schemaVersion,
        byte cause)
    {
        SchemaId = schemaId;
        SchemaVersion = schemaVersion;
        Cause = cause;
    }

    public static EncounterFeatureTermination None => default;

    public ushort SchemaId { get; }

    public byte SchemaVersion { get; }

    public byte Cause { get; }

    public bool IsNone => SchemaId == 0 && SchemaVersion == 0 && Cause == 0;

    public static EncounterFeatureTermination Create(
        ushort schemaId,
        byte schemaVersion,
        byte cause)
    {
        if (schemaId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaId));
        }

        if (schemaVersion == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (cause == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cause));
        }

        return new EncounterFeatureTermination(schemaId, schemaVersion, cause);
    }
}
