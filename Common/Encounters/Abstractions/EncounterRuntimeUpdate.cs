#nullable enable

using System;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly struct EncounterRuntimeUpdate
{
    private EncounterRuntimeUpdate(
        bool hasObservableChange,
        EncounterLifecycle? requestedLifecycle,
        EncounterTerminationDescriptor requestedTermination)
    {
        HasObservableChange = hasObservableChange;
        RequestedLifecycle = requestedLifecycle;
        RequestedTermination = requestedTermination;
    }

    public static EncounterRuntimeUpdate None => default;

    public bool HasObservableChange { get; }

    public EncounterLifecycle? RequestedLifecycle { get; }

    public EncounterTerminationDescriptor RequestedTermination { get; }

    public static EncounterRuntimeUpdate ObservableChange()
    {
        return new EncounterRuntimeUpdate(true, null, EncounterTerminationDescriptor.None);
    }

    public static EncounterRuntimeUpdate TransitionTo(
        EncounterLifecycle next,
        bool hasObservableChange = true)
    {
        if (next is EncounterLifecycle.Idle or EncounterLifecycle.Cleanup)
        {
            throw new ArgumentOutOfRangeException(
                nameof(next),
                "Runtimes request a normal lifecycle or End; they do not enter Idle/Cleanup directly.");
        }

        return new EncounterRuntimeUpdate(
            hasObservableChange,
            next,
            EncounterTerminationDescriptor.None);
    }

    public static EncounterRuntimeUpdate End(EncounterTerminationDescriptor termination)
    {
        if (termination.IsNone)
        {
            throw new ArgumentException(
                "Ending an encounter requires a termination descriptor.",
                nameof(termination));
        }

        return new EncounterRuntimeUpdate(true, null, termination);
    }
}
