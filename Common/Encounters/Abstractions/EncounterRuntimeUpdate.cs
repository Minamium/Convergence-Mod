#nullable enable

using System;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly struct EncounterRuntimeUpdate
{
    private EncounterRuntimeUpdate(
        bool hasObservableChange,
        EncounterLifecycle? requestedLifecycle,
        EncounterEndReason requestedEndReason)
    {
        HasObservableChange = hasObservableChange;
        RequestedLifecycle = requestedLifecycle;
        RequestedEndReason = requestedEndReason;
    }

    public static EncounterRuntimeUpdate None => default;

    public bool HasObservableChange { get; }

    public EncounterLifecycle? RequestedLifecycle { get; }

    public EncounterEndReason RequestedEndReason { get; }

    public static EncounterRuntimeUpdate ObservableChange()
    {
        return new EncounterRuntimeUpdate(true, null, EncounterEndReason.None);
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

        return new EncounterRuntimeUpdate(hasObservableChange, next, EncounterEndReason.None);
    }

    public static EncounterRuntimeUpdate End(EncounterEndReason reason)
    {
        if (reason == EncounterEndReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), "Ending an encounter requires a reason.");
        }

        return new EncounterRuntimeUpdate(true, null, reason);
    }
}
