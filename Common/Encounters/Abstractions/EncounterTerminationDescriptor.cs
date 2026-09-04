#nullable enable

using System;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly record struct EncounterTerminationDescriptor
{
    private EncounterTerminationDescriptor(
        EncounterEndReason endReason,
        EncounterFeatureTermination feature)
    {
        EndReason = endReason;
        Feature = feature;
    }

    public static EncounterTerminationDescriptor None => default;

    public EncounterEndReason EndReason { get; }

    public EncounterFeatureTermination Feature { get; }

    public bool IsNone => EndReason == EncounterEndReason.None && Feature.IsNone;

    public static EncounterTerminationDescriptor Create(
        EncounterEndReason endReason,
        EncounterFeatureTermination feature)
    {
        if (endReason == EncounterEndReason.None || !Enum.IsDefined(endReason))
        {
            throw new ArgumentOutOfRangeException(nameof(endReason));
        }

        if (feature.IsNone)
        {
            throw new ArgumentException(
                "A terminal descriptor requires bounded feature metadata.",
                nameof(feature));
        }

        return new EncounterTerminationDescriptor(endReason, feature);
    }
}
