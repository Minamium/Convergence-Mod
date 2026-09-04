#nullable enable

using System;
using System.Collections.Generic;

namespace Convergence.Common.Encounters.Abstractions;

internal sealed class EncounterExternalTerminationMap
{
    private static readonly EncounterEndReason[] RequiredReasons =
    {
        EncounterEndReason.WorldUnload,
        EncounterEndReason.InternalFailure,
        EncounterEndReason.ProtocolFailure,
    };

    private readonly IReadOnlyDictionary<EncounterEndReason, EncounterTerminationDescriptor> entries;

    public EncounterExternalTerminationMap(
        IEncounterTerminationContract contract,
        IReadOnlyList<EncounterTerminationDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(descriptors);

        var mutable = new Dictionary<EncounterEndReason, EncounterTerminationDescriptor>();
        for (int index = 0; index < descriptors.Count; index++)
        {
            EncounterTerminationDescriptor descriptor = descriptors[index];
            if (!IsExternalReason(descriptor.EndReason))
            {
                throw new ArgumentException(
                    $"'{descriptor.EndReason}' is not a coordinator-owned terminal reason.",
                    nameof(descriptors));
            }

            if (!contract.IsValid(descriptor))
            {
                throw new ArgumentException(
                    $"The terminal mapping for '{descriptor.EndReason}' is incompatible with its feature contract.",
                    nameof(descriptors));
            }

            if (!mutable.TryAdd(descriptor.EndReason, descriptor))
            {
                throw new ArgumentException(
                    $"The terminal mapping for '{descriptor.EndReason}' is duplicated.",
                    nameof(descriptors));
            }
        }

        for (int index = 0; index < RequiredReasons.Length; index++)
        {
            if (!mutable.ContainsKey(RequiredReasons[index]))
            {
                throw new ArgumentException(
                    $"The terminal mapping for '{RequiredReasons[index]}' is required.",
                    nameof(descriptors));
            }
        }

        if (mutable.Count != RequiredReasons.Length)
        {
            throw new ArgumentException(
                "Only the complete coordinator-owned terminal mapping is allowed.",
                nameof(descriptors));
        }

        entries = new System.Collections.ObjectModel.ReadOnlyDictionary<
            EncounterEndReason,
            EncounterTerminationDescriptor>(mutable);
    }

    public EncounterTerminationDescriptor Get(EncounterEndReason reason)
    {
        if (!entries.TryGetValue(reason, out EncounterTerminationDescriptor descriptor))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                "The reason is not a mapped coordinator-owned termination.");
        }

        return descriptor;
    }

    public static bool IsExternalReason(EncounterEndReason reason)
    {
        return reason is EncounterEndReason.WorldUnload
            or EncounterEndReason.InternalFailure
            or EncounterEndReason.ProtocolFailure;
    }

    public static int GetPriority(EncounterEndReason reason)
    {
        return reason switch
        {
            EncounterEndReason.WorldUnload => 3,
            EncounterEndReason.InternalFailure => 2,
            EncounterEndReason.ProtocolFailure => 1,
            _ => 0,
        };
    }
}
