#nullable enable

using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

// Immutable feature-owned configuration. A future authority-only executor consumes
// this schedule and replicates assignments/results; clients never advance it.
internal sealed class FirstSeveranceEncounterPlan
{
    public static FirstSeveranceEncounterPlan Instance { get; } = CreateDefault();

    private FirstSeveranceEncounterPlan(
        FirstSeveranceArenaBlueprint arena,
        FirstSeveranceArenaAccessPolicy accessPolicy,
        FirstSeveranceBossPlan boss,
        IReadOnlyList<FirstSeverancePhaseDefinition> phases)
    {
        Arena = arena ?? throw new ArgumentNullException(nameof(arena));
        AccessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
        Boss = boss ?? throw new ArgumentNullException(nameof(boss));
        Phases = FirstSeverancePlanCollections.Copy(phases, nameof(phases));

        ValidatePlan();
    }

    public FirstSeveranceArenaBlueprint Arena { get; }

    public FirstSeveranceArenaAccessPolicy AccessPolicy { get; }

    public FirstSeveranceBossPlan Boss { get; }

    public IReadOnlyList<FirstSeverancePhaseDefinition> Phases { get; }

    public FirstSeverancePhaseId FirstPhase => FirstSeverancePhaseId.BaseActivation;

    public int HardEnrageOverloadThreshold => 3;

    public FirstSeveranceFailureOutcome LoopExhaustionOutcome =>
        FirstSeveranceFailureOutcome.AdvanceHardEnrage;

    private static FirstSeveranceEncounterPlan CreateDefault()
    {
        var strategicPartKeys = Array.AsReadOnly(
            new[]
            {
                FirstSeveranceBossKeys.CrownPart,
                FirstSeveranceBossKeys.WingsPart,
                FirstSeveranceBossKeys.HeartCasingPart,
            });

        var phases = Array.AsReadOnly(
            new[]
            {
                new FirstSeverancePhaseDefinition(
                    FirstSeverancePhaseId.BaseActivation,
                    FirstSeveranceBossKeys.SealedForm,
                    360,
                    FirstSeverancePhaseExitRule.PreparationComplete,
                    FirstSeverancePhaseId.SealRelease,
                    FirstSeverancePhaseId.SealRelease,
                    1,
                    false,
                    Array.AsReadOnly<FirstSeveranceMechanicDefinition>(
                        new FirstSeveranceMechanicDefinition[]
                        {
                            new FirstSeveranceBossAttackPatternDefinition(
                                "base_activation_warning",
                                0,
                                300,
                                "sealed_observation",
                                FirstSeveranceTargetRule.None,
                                FirstSeveranceFailureOutcome.None),
                        })),
                new FirstSeverancePhaseDefinition(
                    FirstSeverancePhaseId.SealRelease,
                    FirstSeveranceBossKeys.SealedForm,
                    1800,
                    FirstSeverancePhaseExitRule.MechanicResolvedOrDeadline,
                    FirstSeverancePhaseId.PartBreak,
                    FirstSeverancePhaseId.PartBreak,
                    1,
                    false,
                    Array.AsReadOnly<FirstSeveranceMechanicDefinition>(
                        new FirstSeveranceMechanicDefinition[]
                        {
                            new FirstSeverancePylonDpsCheckDefinition(
                                "seal_release_pylon_dps_check",
                                180,
                                1200,
                                new FirstSeveranceParticipantScaledInt(2, 3, 4),
                                FirstSeverancePylonSelectionRule.RosterMappedSymmetricSlots,
                                "balance_pylon_damage_per_participant",
                                true,
                                FirstSeveranceFailureOutcome.ApplyOverload),
                            new FirstSeveranceBossAttackPatternDefinition(
                                "seal_release_pylon_crossfire",
                                300,
                                1200,
                                "pylon_crossfire",
                                FirstSeveranceTargetRule.AllParticipants,
                                FirstSeveranceFailureOutcome.ApplyParticipantDebuff),
                        })),
                new FirstSeverancePhaseDefinition(
                    FirstSeverancePhaseId.PartBreak,
                    FirstSeveranceBossKeys.ManifestForm,
                    1800,
                    FirstSeverancePhaseExitRule.MechanicResolvedOrDeadline,
                    FirstSeverancePhaseId.Coordination,
                    FirstSeverancePhaseId.Coordination,
                    3,
                    true,
                    Array.AsReadOnly<FirstSeveranceMechanicDefinition>(
                        new FirstSeveranceMechanicDefinition[]
                        {
                            new FirstSeverancePartBreakDefinition(
                                "part_break_strategic_choice",
                                120,
                                1320,
                                strategicPartKeys,
                                1),
                            new FirstSeveranceBossAttackPatternDefinition(
                                "part_break_wing_crossing",
                                300,
                                1200,
                                "wing_crossing",
                                FirstSeveranceTargetRule.FarthestParticipant,
                                FirstSeveranceFailureOutcome.ApplyParticipantDebuff),
                        })),
                new FirstSeverancePhaseDefinition(
                    FirstSeverancePhaseId.Coordination,
                    FirstSeveranceBossKeys.ManifestForm,
                    1800,
                    FirstSeverancePhaseExitRule.AllScheduledMechanicsResolved,
                    FirstSeverancePhaseId.PersonalEffigies,
                    FirstSeverancePhaseId.PersonalEffigies,
                    3,
                    true,
                    Array.AsReadOnly<FirstSeveranceMechanicDefinition>(
                        new FirstSeveranceMechanicDefinition[]
                        {
                            new FirstSeveranceStackDefinition(
                                "coordination_stack",
                                120,
                                300,
                                10,
                                new FirstSeveranceParticipantScaledInt(2, 2, 3),
                                1),
                            new FirstSeveranceSpreadDefinition(
                                "coordination_spread",
                                600,
                                300,
                                16,
                                7),
                            new FirstSeveranceTargetedLineDefinition(
                                "coordination_targeted_line",
                                1080,
                                360,
                                120,
                                9,
                                FirstSeveranceTargetRule.HighestRecentServerDamageParticipant),
                            new FirstSeveranceBossAttackPatternDefinition(
                                "coordination_choir_sweep",
                                60,
                                1560,
                                "choir_sweep",
                                FirstSeveranceTargetRule.AllParticipants,
                                FirstSeveranceFailureOutcome.ApplyParticipantDebuff),
                        })),
                new FirstSeverancePhaseDefinition(
                    FirstSeverancePhaseId.PersonalEffigies,
                    FirstSeveranceBossKeys.ConvergenceForm,
                    1800,
                    FirstSeverancePhaseExitRule.MechanicResolvedOrDeadline,
                    FirstSeverancePhaseId.WeakPointExposure,
                    FirstSeverancePhaseId.WeakPointExposure,
                    3,
                    true,
                    Array.AsReadOnly<FirstSeveranceMechanicDefinition>(
                        new FirstSeveranceMechanicDefinition[]
                        {
                            new FirstSeverancePersonalEffigyDefinition(
                                "personal_effigies_owner_trial",
                                120,
                                1320,
                                1000,
                                100,
                                1,
                                "combat_resolve_primary_damage_class",
                                Array.AsReadOnly(
                                    new[]
                                    {
                                        "melee_pursuit",
                                        "ranged_sightline",
                                        "magic_delayed_burst",
                                        "summoner_hostile_minions",
                                        "rogue_false_telegraph",
                                        "classless_fallback",
                                    })),
                            new FirstSeveranceBossAttackPatternDefinition(
                                "personal_effigies_identity_pressure",
                                300,
                                1200,
                                "identity_pressure",
                                FirstSeveranceTargetRule.AllParticipants,
                                FirstSeveranceFailureOutcome.StrengthenBoss),
                        })),
                new FirstSeverancePhaseDefinition(
                    FirstSeverancePhaseId.WeakPointExposure,
                    FirstSeveranceBossKeys.ExposedForm,
                    900,
                    FirstSeverancePhaseExitRule.BossLifeOrDeadline,
                    FirstSeverancePhaseId.PartBreak,
                    FirstSeverancePhaseId.PartBreak,
                    3,
                    true,
                    Array.AsReadOnly<FirstSeveranceMechanicDefinition>(
                        new FirstSeveranceMechanicDefinition[]
                        {
                            new FirstSeveranceWeakPointExposureDefinition(
                                "weak_point_core_burst",
                                120,
                                780,
                                FirstSeveranceBossKeys.CoreWeakPoint,
                                "balance_core_burst_damage_per_participant",
                                FirstSeveranceFailureOutcome.ApplyOverload),
                            new FirstSeveranceBossAttackPatternDefinition(
                                "weak_point_sustained_beam",
                                120,
                                780,
                                "sustained_exposure_beam",
                                FirstSeveranceTargetRule.AllParticipants,
                                FirstSeveranceFailureOutcome.ApplyParticipantDebuff),
                        })),
                new FirstSeverancePhaseDefinition(
                    FirstSeverancePhaseId.LastStand,
                    FirstSeveranceBossKeys.LastStandForm,
                    2400,
                    FirstSeverancePhaseExitRule.FixedSequenceComplete,
                    FirstSeverancePhaseId.None,
                    FirstSeverancePhaseId.None,
                    1,
                    false,
                    Array.AsReadOnly<FirstSeveranceMechanicDefinition>(
                        new FirstSeveranceMechanicDefinition[]
                        {
                            new FirstSeveranceBossAttackPatternDefinition(
                                "last_stand_individual_judgment",
                                0,
                                1800,
                                "final_individual_judgment",
                                FirstSeveranceTargetRule.AllParticipants,
                                FirstSeveranceFailureOutcome.ApplyParticipantDebuff),
                            new FirstSeveranceTargetedLineDefinition(
                                "last_stand_targeted_line",
                                120,
                                420,
                                150,
                                8,
                                FirstSeveranceTargetRule.DeterministicRandomParticipant),
                            new FirstSeveranceSpreadDefinition(
                                "last_stand_spread",
                                720,
                                360,
                                18,
                                7),
                            new FirstSeveranceStackDefinition(
                                "last_stand_stack",
                                1260,
                                360,
                                9,
                                new FirstSeveranceParticipantScaledInt(2, 3, 4),
                                1),
                            new FirstSeveranceWeakPointExposureDefinition(
                                "last_stand_final_core",
                                1800,
                                600,
                                FirstSeveranceBossKeys.CoreWeakPoint,
                                "balance_last_stand_core_damage",
                                FirstSeveranceFailureOutcome.AdvanceHardEnrage),
                        })),
            });

        return new FirstSeveranceEncounterPlan(
            FirstSeveranceArenaBlueprint.Instance,
            FirstSeveranceArenaAccessPolicy.Instance,
            FirstSeveranceBossPlan.CreateDefault(),
            phases);
    }

    private void ValidatePlan()
    {
        if (Arena.Profile.WidthInTiles != FirstSeveranceArenaBlueprint.WidthInTiles
            || Arena.Profile.HeightInTiles != FirstSeveranceArenaBlueprint.HeightInTiles)
        {
            throw new InvalidOperationException("First Severance requires the 320x140 arena profile.");
        }

        var formsByKey = new Dictionary<string, FirstSeveranceBossFormDefinition>(StringComparer.Ordinal);
        for (int formIndex = 0; formIndex < Boss.Forms.Count; formIndex++)
        {
            FirstSeveranceBossFormDefinition form = Boss.Forms[formIndex];
            formsByKey.Add(form.Key, form);
        }

        var partKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int partIndex = 0; partIndex < Boss.Parts.Count; partIndex++)
        {
            partKeys.Add(Boss.Parts[partIndex].Key);
        }

        var phasesById = new Dictionary<FirstSeverancePhaseId, FirstSeverancePhaseDefinition>();
        var mechanicKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int phaseIndex = 0; phaseIndex < Phases.Count; phaseIndex++)
        {
            FirstSeverancePhaseDefinition phase = Phases[phaseIndex];
            if (!phasesById.TryAdd(phase.Id, phase))
            {
                throw new InvalidOperationException($"Duplicate phase '{phase.Id}'.");
            }

            if (!formsByKey.TryGetValue(phase.BossFormKey, out FirstSeveranceBossFormDefinition? form))
            {
                throw new InvalidOperationException(
                    $"Phase '{phase.Id}' references unknown form '{phase.BossFormKey}'.");
            }

            for (int mechanicIndex = 0; mechanicIndex < phase.Mechanics.Count; mechanicIndex++)
            {
                if (!mechanicKeys.Add(phase.Mechanics[mechanicIndex].Key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate mechanic key '{phase.Mechanics[mechanicIndex].Key}'.");
                }

                ValidateMechanicReferences(phase, form, phase.Mechanics[mechanicIndex], partKeys);
            }
        }

        if (!phasesById.ContainsKey(FirstPhase)
            || !phasesById.ContainsKey(FirstSeverancePhaseId.LastStand))
        {
            throw new InvalidOperationException("The plan requires activation and Last Stand phases.");
        }

        for (int phaseIndex = 0; phaseIndex < Phases.Count; phaseIndex++)
        {
            FirstSeverancePhaseDefinition phase = Phases[phaseIndex];
            ValidateDestination(phase.Id, phase.NextPhaseOnResolution, phasesById);
            ValidateDestination(phase.Id, phase.NextPhaseOnSoftFailure, phasesById);
            ValidateTerminalContract(phase);
        }

        ValidateReachabilityAndFiniteCycles(phasesById);
    }

    private static void ValidateTerminalContract(FirstSeverancePhaseDefinition phase)
    {
        if (phase.Id == FirstSeverancePhaseId.LastStand)
        {
            if (phase.NextPhaseOnResolution != FirstSeverancePhaseId.None
                || phase.NextPhaseOnSoftFailure != FirstSeverancePhaseId.None
                || phase.MaximumVisits != 1
                || phase.CanBeInterruptedByLastStand
                || phase.ExitRule != FirstSeverancePhaseExitRule.FixedSequenceComplete)
            {
                throw new InvalidOperationException(
                    "Last Stand must be a single-visit terminal fixed sequence.");
            }

            return;
        }

        if (phase.NextPhaseOnResolution == FirstSeverancePhaseId.None
            || phase.NextPhaseOnSoftFailure == FirstSeverancePhaseId.None)
        {
            throw new InvalidOperationException(
                $"Non-terminal phase '{phase.Id}' requires resolution and soft-failure targets.");
        }
    }

    private static void ValidateDestination(
        FirstSeverancePhaseId source,
        FirstSeverancePhaseId destination,
        IReadOnlyDictionary<FirstSeverancePhaseId, FirstSeverancePhaseDefinition> knownPhases)
    {
        if (destination != FirstSeverancePhaseId.None && !knownPhases.ContainsKey(destination))
        {
            throw new InvalidOperationException(
                $"Phase '{source}' references unknown destination '{destination}'.");
        }
    }

    private void ValidateReachabilityAndFiniteCycles(
        IReadOnlyDictionary<FirstSeverancePhaseId, FirstSeverancePhaseDefinition> phasesById)
    {
        var reachable = new HashSet<FirstSeverancePhaseId>();
        var pending = new Queue<FirstSeverancePhaseId>();
        reachable.Add(FirstPhase);
        pending.Enqueue(FirstPhase);

        while (pending.Count > 0)
        {
            FirstSeverancePhaseId current = pending.Dequeue();
            FirstSeverancePhaseDefinition phase = phasesById[current];
            EnqueueReachable(phase.NextPhaseOnResolution, reachable, pending);
            EnqueueReachable(phase.NextPhaseOnSoftFailure, reachable, pending);

            // Boss life crossing the threshold is an implicit authority edge from
            // interruptible phases into Last Stand; it is part of graph validity.
            if (phase.CanBeInterruptedByLastStand)
            {
                EnqueueReachable(FirstSeverancePhaseId.LastStand, reachable, pending);
            }
        }

        foreach (FirstSeverancePhaseId phaseId in phasesById.Keys)
        {
            if (!reachable.Contains(phaseId))
            {
                throw new InvalidOperationException($"Phase '{phaseId}' is unreachable.");
            }
        }

        var visiting = new HashSet<FirstSeverancePhaseId>();
        var visited = new HashSet<FirstSeverancePhaseId>();
        bool hasCycle = HasCycleFrom(FirstPhase, phasesById, visiting, visited);
        if (hasCycle && LoopExhaustionOutcome == FirstSeveranceFailureOutcome.None)
        {
            throw new InvalidOperationException(
                "A cyclic phase graph requires a non-empty loop exhaustion outcome.");
        }
    }

    private static void EnqueueReachable(
        FirstSeverancePhaseId destination,
        HashSet<FirstSeverancePhaseId> reachable,
        Queue<FirstSeverancePhaseId> pending)
    {
        if (destination != FirstSeverancePhaseId.None && reachable.Add(destination))
        {
            pending.Enqueue(destination);
        }
    }

    private static bool HasCycleFrom(
        FirstSeverancePhaseId phaseId,
        IReadOnlyDictionary<FirstSeverancePhaseId, FirstSeverancePhaseDefinition> phasesById,
        HashSet<FirstSeverancePhaseId> visiting,
        HashSet<FirstSeverancePhaseId> visited)
    {
        if (visited.Contains(phaseId))
        {
            return false;
        }

        if (!visiting.Add(phaseId))
        {
            return true;
        }

        FirstSeverancePhaseDefinition phase = phasesById[phaseId];
        if (HasCycleAtDestination(
                phase.NextPhaseOnResolution,
                phasesById,
                visiting,
                visited)
            || HasCycleAtDestination(
                phase.NextPhaseOnSoftFailure,
                phasesById,
                visiting,
                visited)
            || (phase.CanBeInterruptedByLastStand
                && HasCycleAtDestination(
                    FirstSeverancePhaseId.LastStand,
                    phasesById,
                    visiting,
                    visited)))
        {
            return true;
        }

        visiting.Remove(phaseId);
        visited.Add(phaseId);
        return false;
    }

    private static bool HasCycleAtDestination(
        FirstSeverancePhaseId destination,
        IReadOnlyDictionary<FirstSeverancePhaseId, FirstSeverancePhaseDefinition> phasesById,
        HashSet<FirstSeverancePhaseId> visiting,
        HashSet<FirstSeverancePhaseId> visited)
    {
        return destination != FirstSeverancePhaseId.None
            && HasCycleFrom(destination, phasesById, visiting, visited);
    }

    private void ValidateMechanicReferences(
        FirstSeverancePhaseDefinition phase,
        FirstSeveranceBossFormDefinition form,
        FirstSeveranceMechanicDefinition mechanic,
        HashSet<string> knownPartKeys)
    {
        if (mechanic is FirstSeverancePartBreakDefinition partBreak)
        {
            for (int partIndex = 0; partIndex < partBreak.EligiblePartKeys.Count; partIndex++)
            {
                if (!knownPartKeys.Contains(partBreak.EligiblePartKeys[partIndex]))
                {
                    throw new InvalidOperationException(
                        $"Mechanic '{mechanic.Key}' references unknown part "
                        + $"'{partBreak.EligiblePartKeys[partIndex]}'.");
                }

                if (!ContainsOrdinal(form.EnabledPartKeys, partBreak.EligiblePartKeys[partIndex]))
                {
                    throw new InvalidOperationException(
                        $"Mechanic '{mechanic.Key}' references part "
                        + $"'{partBreak.EligiblePartKeys[partIndex]}' disabled in form '{form.Key}'.");
                }
            }
        }

        if (mechanic is FirstSeveranceWeakPointExposureDefinition exposure
            && !string.Equals(exposure.WeakPointKey, Boss.WeakPoint.Key, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Mechanic '{mechanic.Key}' references unknown weak point '{exposure.WeakPointKey}'.");
        }

        if (mechanic is FirstSeveranceBossAttackPatternDefinition attack
            && !ContainsOrdinal(form.AttackPatternKeys, attack.PatternKey))
        {
            throw new InvalidOperationException(
                $"Phase '{phase.Id}' form '{form.Key}' does not allow pattern '{attack.PatternKey}'.");
        }
    }

    private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (string.Equals(values[index], expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
