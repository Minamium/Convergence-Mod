#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterCoordinator : IEncounterCommandSink
{
    private const ulong CleanupRetryIntervalTicks = 60;
    private readonly EncounterRegistry registry;
    private readonly IReadOnlyList<IEncounterActivationPolicy> activationPolicies;
    private readonly Action<Exception> exceptionSink;
    private readonly EncounterSnapshotOutbox snapshotOutbox = new();
    private readonly List<EncounterCleanupWork> cleanupBacklog = new();
    private EncounterSession? activeSession;
    private ulong authorityTick;
    private ulong encounterSequence;
    private uint idleRevision;
    private EncounterEndReason pendingExternalTermination;

    public EncounterCoordinator(
        EncounterRegistry registry,
        IReadOnlyList<IEncounterActivationPolicy> activationPolicies,
        Action<Exception> exceptionSink)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.activationPolicies = activationPolicies
            ?? throw new ArgumentNullException(nameof(activationPolicies));
        this.exceptionSink = exceptionSink ?? throw new ArgumentNullException(nameof(exceptionSink));
    }

    public bool HasActiveSession => activeSession is not null;

    public bool HasPendingCleanup => cleanupBacklog.Count > 0;

    public EncounterSnapshot Snapshot => activeSession?.CreateSnapshot(authorityTick)
        ?? EncounterSnapshot.CreateIdle(encounterSequence, idleRevision, authorityTick);

    public EncounterSnapshot? LastTerminalSnapshot { get; private set; }

    public bool TryStart(
        in EncounterStartCommand command,
        out EncounterSnapshot snapshot,
        out string failureCode)
    {
        snapshot = Snapshot;

        if (activeSession is not null)
        {
            failureCode = "encounter.already_active";
            return false;
        }

        if (cleanupBacklog.Count > 0)
        {
            failureCode = "encounter.cleanup_pending";
            return false;
        }

        if (string.IsNullOrWhiteSpace(command.DefinitionKey))
        {
            failureCode = "encounter.invalid_definition";
            return false;
        }

        if (!registry.TryGet(command.DefinitionKey, out EncounterDefinition definition))
        {
            failureCode = "encounter.unknown_definition";
            return false;
        }

        if (!TryEvaluatePolicies(activationPolicies, command, definition, out failureCode)
            || !TryEvaluatePolicies(definition.ActivationPolicies, command, definition, out failureCode))
        {
            return false;
        }

        if (encounterSequence == ulong.MaxValue)
        {
            failureCode = "encounter.sequence_exhausted";
            return false;
        }

        ulong nextSequence = encounterSequence + 1;
        FightId fightId = FightId.CreateForAuthority();
        EncounterCleanupScope cleanupScope = new();
        AcceptedEncounterStart start = new(
            command.SenderWhoAmI,
            command.RequestedAnchor,
            command.RequestNonce);
        EncounterRuntimeCreationContext creationContext = new(
            nextSequence,
            fightId,
            start,
            cleanupScope);
        EncounterSession? candidateSession = null;

        try
        {
            IEncounterRuntime runtime = definition.RuntimeFactory.Create(
                definition,
                creationContext) ?? throw new InvalidOperationException(
                    "An encounter runtime factory returned null.");
            cleanupScope.Register(runtime);
            candidateSession = new EncounterSession(
                nextSequence,
                fightId,
                definition,
                runtime,
                cleanupScope,
                authorityTick);
            snapshot = candidateSession.CreateSnapshot(authorityTick);

            activeSession = candidateSession;
            encounterSequence = nextSequence;
            idleRevision = 0;
            pendingExternalTermination = EncounterEndReason.None;
            LastTerminalSnapshot = null;
            snapshotOutbox.Publish(snapshot);
            failureCode = string.Empty;
            return true;
        }
        catch (EncounterStartRejectedException rejection)
        {
            activeSession = null;
            failureCode = rejection.FailureCode;
            return false;
        }
        catch (Exception exception)
        {
            SafeReport(exception);
            activeSession = null;
            EncounterTerminationDescriptor termination = definition.ExternalTerminations.Get(
                EncounterEndReason.InternalFailure);
            EncounterCleanupContext cleanupContext = new(
                nextSequence,
                fightId,
                termination);
            cleanupScope.CleanupPending(cleanupContext, SafeReport);
            AddCleanupBacklog(cleanupScope, cleanupContext);
            failureCode = "encounter.runtime_creation_failure";
            return false;
        }
    }

    public void Tick()
    {
        authorityTick++;
        ProcessCleanupBacklog(force: false);

        if (activeSession is null)
        {
            return;
        }

        if (pendingExternalTermination != EncounterEndReason.None)
        {
            EncounterEndReason reason = pendingExternalTermination;
            pendingExternalTermination = EncounterEndReason.None;
            TryEndFromExternal(activeSession.FightId, reason, out _);
            return;
        }

        EncounterRuntimeUpdate update;
        try
        {
            update = activeSession.Tick(authorityTick);
        }
        catch (Exception exception)
        {
            SafeReport(exception);
            TryEndFromExternal(
                activeSession.FightId,
                EncounterEndReason.InternalFailure,
                out _);
            return;
        }

        if (!update.RequestedTermination.IsNone)
        {
            if (!activeSession.Definition.TerminationContract.IsValid(
                update.RequestedTermination))
            {
                SafeReport(new InvalidOperationException(
                    "Runtime returned an incompatible termination descriptor."));
                TryEndFromExternal(
                    activeSession.FightId,
                    EncounterEndReason.InternalFailure,
                    out _);
                return;
            }

            TryEndCore(activeSession.FightId, update.RequestedTermination, out _);
            return;
        }

        if (update.RequestedLifecycle.HasValue)
        {
            if (!TryTransition(activeSession.FightId, update.RequestedLifecycle.Value, out _))
            {
                SafeReport(new InvalidOperationException(
                    $"Runtime requested an invalid transition from {activeSession.Lifecycle} "
                    + $"to {update.RequestedLifecycle.Value}."));
                TryEndFromExternal(
                    activeSession.FightId,
                    EncounterEndReason.InternalFailure,
                    out _);
            }

            return;
        }

        if (update.HasObservableChange)
        {
            activeSession.MarkObservableChange();
            snapshotOutbox.Publish(activeSession.CreateSnapshot(authorityTick));
        }
    }

    public bool TryTakePublishedSnapshot(out EncounterSnapshot snapshot)
    {
        return snapshotOutbox.TryTake(out snapshot);
    }

    public void Reset(EncounterEndReason reason)
    {
        if (!EncounterExternalTerminationMap.IsExternalReason(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                "Coordinator reset requires an external terminal reason.");
        }

        if (activeSession is not null)
        {
            QueueExternalTermination(reason);
            EncounterEndReason selected = pendingExternalTermination;
            pendingExternalTermination = EncounterEndReason.None;
            TryEndFromExternal(activeSession.FightId, selected, out _);
        }

        ProcessCleanupBacklog(force: true);
        if (cleanupBacklog.Count > 0)
        {
            SafeReport(new InvalidOperationException(
                $"{cleanupBacklog.Count} cleanup scope(s) remained incomplete during reset."));
        }

        cleanupBacklog.Clear();
        snapshotOutbox.Clear();
        pendingExternalTermination = EncounterEndReason.None;
    }

    internal bool RequestExternalTermination(EncounterEndReason reason)
    {
        if (activeSession is null || !EncounterExternalTerminationMap.IsExternalReason(reason))
        {
            return false;
        }

        QueueExternalTermination(reason);
        return true;
    }

    private bool TryEndFromExternal(
        FightId expectedFightId,
        EncounterEndReason reason,
        out EncounterSnapshot terminalSnapshot)
    {
        if (activeSession is null || activeSession.FightId != expectedFightId)
        {
            terminalSnapshot = LastTerminalSnapshot ?? Snapshot;
            return false;
        }

        EncounterTerminationDescriptor termination = activeSession.Definition
            .ExternalTerminations
            .Get(reason);
        return TryEndCore(expectedFightId, termination, out terminalSnapshot);
    }

    private bool TryEndCore(
        FightId expectedFightId,
        EncounterTerminationDescriptor termination,
        out EncounterSnapshot terminalSnapshot)
    {
        if (termination.IsNone
            || activeSession is null
            || activeSession.FightId != expectedFightId)
        {
            terminalSnapshot = LastTerminalSnapshot ?? Snapshot;
            return false;
        }

        EncounterSession endingSession = activeSession;
        if (!endingSession.TryEnterCleanup(termination, authorityTick))
        {
            terminalSnapshot = endingSession.CreateSnapshot(authorityTick);
            return false;
        }

        terminalSnapshot = endingSession.CreateSnapshot(authorityTick);
        LastTerminalSnapshot = terminalSnapshot;
        snapshotOutbox.Publish(terminalSnapshot);

        try
        {
            endingSession.Cleanup(SafeReport);
        }
        catch (Exception exception)
        {
            SafeReport(exception);
        }
        finally
        {
            EncounterCleanupWork? pending = endingSession.CreatePendingCleanupWork(
                authorityTick + CleanupRetryIntervalTicks);
            if (pending is not null)
            {
                cleanupBacklog.Add(pending);
            }

            activeSession = null;
            idleRevision = terminalSnapshot.Revision == uint.MaxValue
                ? uint.MaxValue
                : terminalSnapshot.Revision + 1;
            // The outbox sends the retained terminal first, then this newer Idle.
            // Without it, peers remain in Cleanup and Idle-gated summons never send.
            // TryStart still rejects while any cleanup work remains pending.
            snapshotOutbox.Publish(Snapshot);
        }

        return true;
    }

    private void QueueExternalTermination(EncounterEndReason reason)
    {
        if (!EncounterExternalTerminationMap.IsExternalReason(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        if (EncounterExternalTerminationMap.GetPriority(reason)
            > EncounterExternalTerminationMap.GetPriority(pendingExternalTermination))
        {
            pendingExternalTermination = reason;
        }
    }

    private bool TryTransition(
        FightId expectedFightId,
        EncounterLifecycle next,
        out EncounterSnapshot snapshot)
    {
        if (activeSession is null || activeSession.FightId != expectedFightId)
        {
            snapshot = Snapshot;
            return false;
        }

        if (!activeSession.TryTransition(next, authorityTick))
        {
            snapshot = activeSession.CreateSnapshot(authorityTick);
            return false;
        }

        snapshot = activeSession.CreateSnapshot(authorityTick);
        snapshotOutbox.Publish(snapshot);
        return true;
    }

    private void AddCleanupBacklog(
        EncounterCleanupScope cleanupScope,
        in EncounterCleanupContext cleanupContext)
    {
        if (!cleanupScope.IsComplete)
        {
            cleanupBacklog.Add(new EncounterCleanupWork(
                cleanupScope,
                cleanupContext,
                authorityTick + CleanupRetryIntervalTicks));
        }
    }

    private void ProcessCleanupBacklog(bool force)
    {
        for (int index = cleanupBacklog.Count - 1; index >= 0; index--)
        {
            EncounterCleanupWork work = cleanupBacklog[index];
            if (!force && authorityTick < work.NextAttemptTick)
            {
                continue;
            }

            work.Scope.CleanupPending(work.Context, SafeReport);
            if (work.Scope.IsComplete)
            {
                cleanupBacklog.RemoveAt(index);
            }
            else
            {
                work.NextAttemptTick = authorityTick + CleanupRetryIntervalTicks;
            }
        }
    }

    private void SafeReport(Exception exception)
    {
        try
        {
            exceptionSink(exception);
        }
        catch
        {
            // Diagnostics are never allowed to block cleanup or authoritative state release.
        }
    }

    private bool TryEvaluatePolicies(
        IReadOnlyList<IEncounterActivationPolicy> policies,
        in EncounterStartCommand command,
        EncounterDefinition definition,
        out string failureCode)
    {
        foreach (IEncounterActivationPolicy policy in policies)
        {
            EncounterActivationDecision decision;
            try
            {
                decision = policy.Evaluate(command, definition);
            }
            catch (Exception exception)
            {
                SafeReport(exception);
                failureCode = "encounter.activation_policy_failure";
                return false;
            }

            if (!decision.IsAllowed)
            {
                failureCode = decision.FailureCode;
                return false;
            }
        }

        failureCode = string.Empty;
        return true;
    }
}
