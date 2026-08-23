#nullable enable

using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterSession
{
    private readonly IEncounterRuntime runtime;
    private readonly EncounterCleanupScope cleanupScope;

    public EncounterSession(
        ulong encounterSequence,
        FightId fightId,
        EncounterDefinition definition,
        IEncounterRuntime runtime,
        EncounterCleanupScope cleanupScope,
        ulong authorityTick)
    {
        if (encounterSequence == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(encounterSequence));
        }

        if (fightId.IsNone)
        {
            throw new ArgumentException("An active Encounter requires a Fight ID.", nameof(fightId));
        }

        EncounterSequence = encounterSequence;
        FightId = fightId;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.cleanupScope = cleanupScope ?? throw new ArgumentNullException(nameof(cleanupScope));
        Lifecycle = EncounterLifecycle.Validating;
        LifecycleEnteredTick = authorityTick;
        Revision = 1;
    }

    public ulong EncounterSequence { get; }

    public FightId FightId { get; }

    public EncounterDefinition Definition { get; }

    public EncounterLifecycle Lifecycle { get; private set; }

    public uint Revision { get; private set; }

    public ulong LifecycleEnteredTick { get; private set; }

    public ulong ActiveFightTick { get; private set; }

    public EncounterEndReason EndReason { get; private set; }

    public EncounterRuntimeUpdate Tick(ulong authorityTick)
    {
        if (Lifecycle == EncounterLifecycle.Active)
        {
            ActiveFightTick++;
        }

        EncounterRuntimeContext context = new(
            EncounterSequence,
            FightId,
            Lifecycle,
            authorityTick,
            LifecycleEnteredTick,
            ActiveFightTick);
        return runtime.Tick(context);
    }

    public bool TryTransition(EncounterLifecycle next, ulong authorityTick)
    {
        if (!EncounterLifecycleMachine.CanTransition(Lifecycle, next))
        {
            return false;
        }

        Lifecycle = next;
        LifecycleEnteredTick = authorityTick;
        MarkObservableChange();
        return true;
    }

    public bool TryEnterCleanup(EncounterEndReason reason, ulong authorityTick)
    {
        if (reason == EncounterEndReason.None || Lifecycle == EncounterLifecycle.Cleanup)
        {
            return false;
        }

        Lifecycle = EncounterLifecycle.Cleanup;
        LifecycleEnteredTick = authorityTick;
        EndReason = reason;
        MarkObservableChange();
        return true;
    }

    public void MarkObservableChange()
    {
        if (Revision < uint.MaxValue)
        {
            Revision++;
        }
    }

    public void Cleanup(Action<Exception> onFailure)
    {
        EncounterCleanupContext context = new(EncounterSequence, FightId, EndReason);
        cleanupScope.CleanupPending(context, onFailure);
    }

    public EncounterCleanupWork? CreatePendingCleanupWork(ulong nextAttemptTick)
    {
        if (cleanupScope.IsComplete)
        {
            return null;
        }

        EncounterCleanupContext context = new(EncounterSequence, FightId, EndReason);
        return new EncounterCleanupWork(cleanupScope, context, nextAttemptTick);
    }

    public EncounterSnapshot CreateSnapshot(ulong authorityTick)
    {
        return new EncounterSnapshot(
            EncounterSequence,
            FightId,
            Definition.Key,
            Lifecycle,
            Revision,
            authorityTick,
            LifecycleEnteredTick,
            ActiveFightTick,
            EndReason);
    }
}
