#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceEncounterPlan
{
    public const int MaximumSupportedExposures = 16;

    private readonly IReadOnlyDictionary<
        FirstSeveranceSubstate,
        FirstSeveranceSubstateDefinition> definitionsBySubstate;

    public FirstSeveranceEncounterPlan(
        FirstSeveranceArenaBlueprint arena,
        FirstSeveranceArenaAccessPolicy accessPolicy,
        FirstSeveranceBossPlan boss,
        FirstSeveranceTimingPlan timing,
        in FirstSeveranceParticipantScaledInt pylonCount,
        in FirstSeveranceParticipantScaledInt stackRequiredShares,
        int overloadThreshold,
        int maximumCompletedExposures,
        IReadOnlyList<FirstSeveranceSubstateDefinition> substates,
        FirstSeveranceTerminationContract terminationContract)
    {
        Arena = arena ?? throw new ArgumentNullException(nameof(arena));
        AccessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
        Boss = boss ?? throw new ArgumentNullException(nameof(boss));
        Timing = timing ?? throw new ArgumentNullException(nameof(timing));
        TerminationContract = terminationContract
            ?? throw new ArgumentNullException(nameof(terminationContract));

        if (!pylonCount.FitsParticipantCount
            || pylonCount.FourParticipants > FirstSeveranceArenaBlueprint.RequiredPylonSlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pylonCount));
        }

        if (!stackRequiredShares.FitsParticipantCount)
        {
            throw new ArgumentOutOfRangeException(nameof(stackRequiredShares));
        }

        if (overloadThreshold is <= 0 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(overloadThreshold));
        }

        if (maximumCompletedExposures is <= 0 or > MaximumSupportedExposures)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCompletedExposures));
        }

        PylonCount = pylonCount;
        StackRequiredShares = stackRequiredShares;
        OverloadThreshold = overloadThreshold;
        MaximumCompletedExposures = maximumCompletedExposures;
        Substates = FirstSeverancePlanCollections.Copy(substates, nameof(substates));
        definitionsBySubstate = ValidatePlan(Substates);
    }

    public static FirstSeveranceEncounterPlan Instance { get; } = CreateDefault();

    public FirstSeveranceArenaBlueprint Arena { get; }

    public FirstSeveranceArenaAccessPolicy AccessPolicy { get; }

    public FirstSeveranceBossPlan Boss { get; }

    public FirstSeveranceTimingPlan Timing { get; }

    public FirstSeveranceParticipantScaledInt PylonCount { get; }

    public FirstSeveranceParticipantScaledInt StackRequiredShares { get; }

    public int OverloadThreshold { get; }

    public int MaximumCompletedExposures { get; }

    public FirstSeveranceSubstate FirstSubstate => FirstSeveranceSubstate.SpawnIntro;

    public IReadOnlyList<FirstSeveranceSubstateDefinition> Substates { get; }

    public FirstSeveranceTerminationContract TerminationContract { get; }

    public FirstSeveranceSubstateDefinition GetSubstate(FirstSeveranceSubstate substate)
    {
        if (!definitionsBySubstate.TryGetValue(
            substate,
            out FirstSeveranceSubstateDefinition? definition))
        {
            throw new ArgumentOutOfRangeException(nameof(substate));
        }

        return definition;
    }

    public int GetDurationTicks(
        FirstSeveranceSubstate substate,
        bool isPenalizedExposure)
    {
        if (substate == FirstSeveranceSubstate.CoreExposure && isPenalizedExposure)
        {
            return Timing.PenalizedExposureTicks;
        }

        return GetSubstate(substate).DurationTicks;
    }

    private static FirstSeveranceEncounterPlan CreateDefault()
    {
        var timing = new FirstSeveranceTimingPlan(
            spawnIntroTicks: 180,
            pylonTelegraphTicks: 60,
            pylonActiveTicks: 600,
            stackTelegraphTicks: 180,
            spreadTelegraphTicks: 180,
            normalExposureTicks: 720,
            penalizedExposureTicks: 360,
            resetTicks: 90);

        IReadOnlyList<FirstSeveranceSubstateDefinition> substates = Array.AsReadOnly(
            new[]
            {
                new FirstSeveranceSubstateDefinition(
                    FirstSeveranceSubstate.SpawnIntro,
                    FirstSeveranceMechanicKind.None,
                    timing.SpawnIntroTicks,
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.PylonCheck),
                    FirstSeveranceStateEdge.End(
                        FirstSeveranceTerminalCause.RuntimeInvariantBroken)),
                new FirstSeveranceSubstateDefinition(
                    FirstSeveranceSubstate.PylonCheck,
                    FirstSeveranceMechanicKind.PylonCheck,
                    timing.PylonCheckTicks,
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Stack),
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Stack)),
                new FirstSeveranceSubstateDefinition(
                    FirstSeveranceSubstate.Stack,
                    FirstSeveranceMechanicKind.Stack,
                    timing.StackTelegraphTicks,
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Spread),
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Spread)),
                new FirstSeveranceSubstateDefinition(
                    FirstSeveranceSubstate.Spread,
                    FirstSeveranceMechanicKind.Spread,
                    timing.SpreadTelegraphTicks,
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.CoreExposure),
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.CoreExposure)),
                new FirstSeveranceSubstateDefinition(
                    FirstSeveranceSubstate.CoreExposure,
                    FirstSeveranceMechanicKind.CoreExposure,
                    timing.NormalExposureTicks,
                    FirstSeveranceStateEdge.End(FirstSeveranceTerminalCause.BossLifeZero),
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Reset)),
                new FirstSeveranceSubstateDefinition(
                    FirstSeveranceSubstate.Reset,
                    FirstSeveranceMechanicKind.None,
                    timing.ResetTicks,
                    FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.PylonCheck),
                    FirstSeveranceStateEdge.End(
                        FirstSeveranceTerminalCause.RuntimeInvariantBroken)),
            });

        return new FirstSeveranceEncounterPlan(
            FirstSeveranceArenaBlueprint.Instance,
            FirstSeveranceArenaAccessPolicy.Instance,
            FirstSeveranceBossPlan.CreateDefault(),
            timing,
            new FirstSeveranceParticipantScaledInt(2, 3, 4),
            new FirstSeveranceParticipantScaledInt(2, 2, 3),
            overloadThreshold: 3,
            maximumCompletedExposures: 8,
            substates,
            FirstSeveranceTerminationContract.Instance);
    }

    private IReadOnlyDictionary<
        FirstSeveranceSubstate,
        FirstSeveranceSubstateDefinition> ValidatePlan(
            IReadOnlyList<FirstSeveranceSubstateDefinition> substates)
    {
        int expectedStateCount = Enum.GetValues<FirstSeveranceSubstate>().Length - 1;
        if (substates.Count != expectedStateCount)
        {
            throw new ArgumentException(
                "The plan must define every active substate exactly once.",
                nameof(substates));
        }

        var definitions = new Dictionary<
            FirstSeveranceSubstate,
            FirstSeveranceSubstateDefinition>();
        var mechanicOwners = new Dictionary<
            FirstSeveranceMechanicKind,
            FirstSeveranceSubstate>();

        for (int index = 0; index < substates.Count; index++)
        {
            FirstSeveranceSubstateDefinition definition = substates[index]
                ?? throw new ArgumentException("A substate definition cannot be null.", nameof(substates));
            if (!definitions.TryAdd(definition.Substate, definition))
            {
                throw new ArgumentException(
                    $"Substate '{definition.Substate}' is defined more than once.",
                    nameof(substates));
            }

            if (definition.Mechanic != FirstSeveranceMechanicKind.None
                && !mechanicOwners.TryAdd(definition.Mechanic, definition.Substate))
            {
                throw new ArgumentException(
                    $"Mechanic '{definition.Mechanic}' has more than one owner.",
                    nameof(substates));
            }
        }

        ValidateMechanicOwnership(definitions, mechanicOwners);
        ValidateDurations(definitions);
        ValidateAcceptedEdges(definitions);
        ValidateReachabilityAndBoundedCycle(definitions);
        ValidateTerminationContract();

        return new System.Collections.ObjectModel.ReadOnlyDictionary<
            FirstSeveranceSubstate,
            FirstSeveranceSubstateDefinition>(definitions);
    }

    private static void ValidateMechanicOwnership(
        IReadOnlyDictionary<FirstSeveranceSubstate, FirstSeveranceSubstateDefinition> definitions,
        IReadOnlyDictionary<FirstSeveranceMechanicKind, FirstSeveranceSubstate> mechanicOwners)
    {
        var expectedOwners = new Dictionary<FirstSeveranceMechanicKind, FirstSeveranceSubstate>
        {
            [FirstSeveranceMechanicKind.PylonCheck] = FirstSeveranceSubstate.PylonCheck,
            [FirstSeveranceMechanicKind.Stack] = FirstSeveranceSubstate.Stack,
            [FirstSeveranceMechanicKind.Spread] = FirstSeveranceSubstate.Spread,
            [FirstSeveranceMechanicKind.CoreExposure] = FirstSeveranceSubstate.CoreExposure,
        };

        if (mechanicOwners.Count != expectedOwners.Count)
        {
            throw new ArgumentException("Every mechanic requires one exact substate owner.");
        }

        foreach (KeyValuePair<FirstSeveranceMechanicKind, FirstSeveranceSubstate> expected in
            expectedOwners)
        {
            if (!mechanicOwners.TryGetValue(expected.Key, out FirstSeveranceSubstate owner)
                || owner != expected.Value)
            {
                throw new ArgumentException(
                    $"Mechanic '{expected.Key}' must be owned by '{expected.Value}'.");
            }
        }

        if (definitions[FirstSeveranceSubstate.SpawnIntro].Mechanic
                != FirstSeveranceMechanicKind.None
            || definitions[FirstSeveranceSubstate.Reset].Mechanic
                != FirstSeveranceMechanicKind.None)
        {
            throw new ArgumentException("SpawnIntro and Reset cannot own a mechanic.");
        }
    }

    private void ValidateDurations(
        IReadOnlyDictionary<FirstSeveranceSubstate, FirstSeveranceSubstateDefinition> definitions)
    {
        var expected = new Dictionary<FirstSeveranceSubstate, int>
        {
            [FirstSeveranceSubstate.SpawnIntro] = Timing.SpawnIntroTicks,
            [FirstSeveranceSubstate.PylonCheck] = Timing.PylonCheckTicks,
            [FirstSeveranceSubstate.Stack] = Timing.StackTelegraphTicks,
            [FirstSeveranceSubstate.Spread] = Timing.SpreadTelegraphTicks,
            [FirstSeveranceSubstate.CoreExposure] = Timing.NormalExposureTicks,
            [FirstSeveranceSubstate.Reset] = Timing.ResetTicks,
        };

        foreach (KeyValuePair<FirstSeveranceSubstate, int> item in expected)
        {
            if (definitions[item.Key].DurationTicks != item.Value)
            {
                throw new ArgumentException(
                    $"Substate '{item.Key}' does not own its configured duration.");
            }
        }
    }

    private static void ValidateAcceptedEdges(
        IReadOnlyDictionary<FirstSeveranceSubstate, FirstSeveranceSubstateDefinition> definitions)
    {
        ValidateEdges(
            definitions[FirstSeveranceSubstate.SpawnIntro],
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.PylonCheck),
            FirstSeveranceStateEdge.End(FirstSeveranceTerminalCause.RuntimeInvariantBroken));
        ValidateEdges(
            definitions[FirstSeveranceSubstate.PylonCheck],
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Stack),
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Stack));
        ValidateEdges(
            definitions[FirstSeveranceSubstate.Stack],
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Spread),
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Spread));
        ValidateEdges(
            definitions[FirstSeveranceSubstate.Spread],
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.CoreExposure),
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.CoreExposure));
        ValidateEdges(
            definitions[FirstSeveranceSubstate.CoreExposure],
            FirstSeveranceStateEdge.End(FirstSeveranceTerminalCause.BossLifeZero),
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.Reset));
        ValidateEdges(
            definitions[FirstSeveranceSubstate.Reset],
            FirstSeveranceStateEdge.TransitionTo(FirstSeveranceSubstate.PylonCheck),
            FirstSeveranceStateEdge.End(FirstSeveranceTerminalCause.RuntimeInvariantBroken));
    }

    private static void ValidateEdges(
        FirstSeveranceSubstateDefinition definition,
        FirstSeveranceStateEdge expectedSuccess,
        FirstSeveranceStateEdge expectedFailure)
    {
        if (definition.SuccessEdge != expectedSuccess
            || definition.FailureEdge != expectedFailure)
        {
            throw new ArgumentException(
                $"Substate '{definition.Substate}' has an unsupported edge.");
        }
    }

    private void ValidateReachabilityAndBoundedCycle(
        IReadOnlyDictionary<FirstSeveranceSubstate, FirstSeveranceSubstateDefinition> definitions)
    {
        var reachable = new HashSet<FirstSeveranceSubstate>();
        var pending = new Queue<FirstSeveranceSubstate>();
        pending.Enqueue(FirstSubstate);

        while (pending.Count > 0)
        {
            FirstSeveranceSubstate current = pending.Dequeue();
            if (!reachable.Add(current))
            {
                continue;
            }

            FirstSeveranceSubstateDefinition definition = definitions[current];
            EnqueueDestination(definition.SuccessEdge, definitions, pending);
            EnqueueDestination(definition.FailureEdge, definitions, pending);
        }

        if (reachable.Count != definitions.Count)
        {
            throw new ArgumentException("Every active substate must be reachable.");
        }

        FirstSeveranceSubstateDefinition reset = definitions[FirstSeveranceSubstate.Reset];
        if (reset.SuccessEdge.NextSubstate != FirstSeveranceSubstate.PylonCheck
            || MaximumCompletedExposures <= 0)
        {
            throw new ArgumentException("The repeated loop must have a bounded Reset edge.");
        }
    }

    private void ValidateTerminationContract()
    {
        var seenDescriptors = new HashSet<EncounterTerminationDescriptor>();
        foreach (FirstSeveranceTerminalCause cause in Enum.GetValues<FirstSeveranceTerminalCause>())
        {
            if (cause == FirstSeveranceTerminalCause.None)
            {
                continue;
            }

            EncounterTerminationDescriptor descriptor = TerminationContract.Create(cause);
            if (!TerminationContract.IsValid(descriptor) || !seenDescriptors.Add(descriptor))
            {
                throw new ArgumentException(
                    $"Terminal cause '{cause}' does not have one valid mapping.");
            }
        }

        _ = new EncounterExternalTerminationMap(
            TerminationContract,
            TerminationContract.ExternalTerminations);
    }

    private static void EnqueueDestination(
        FirstSeveranceStateEdge edge,
        IReadOnlyDictionary<FirstSeveranceSubstate, FirstSeveranceSubstateDefinition> definitions,
        Queue<FirstSeveranceSubstate> pending)
    {
        if (edge.IsTerminal)
        {
            return;
        }

        if (!definitions.ContainsKey(edge.NextSubstate))
        {
            throw new ArgumentException(
                $"Edge references undefined substate '{edge.NextSubstate}'.");
        }

        pending.Enqueue(edge.NextSubstate);
    }
}
