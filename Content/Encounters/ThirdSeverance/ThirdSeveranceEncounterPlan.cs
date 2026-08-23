#nullable enable

using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.ThirdSeverance;

// Immutable feature-owned configuration. A future authority-only executor consumes
// this schedule and replicates assignments/results; clients never advance it.
internal sealed class ThirdSeveranceEncounterPlan
{
    public static ThirdSeveranceEncounterPlan Instance { get; } = CreateDefault();

    private ThirdSeveranceEncounterPlan(
        ThirdSeveranceArenaBlueprint arena,
        ThirdSeveranceArenaAccessPolicy accessPolicy,
        ThirdSeveranceBossPlan boss,
        IReadOnlyList<ThirdSeverancePhaseDefinition> phases)
    {
        Arena = arena ?? throw new ArgumentNullException(nameof(arena));
        AccessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
        Boss = boss ?? throw new ArgumentNullException(nameof(boss));
        Phases = ThirdSeverancePlanCollections.Copy(phases, nameof(phases));

        ValidatePlan();
    }

    public ThirdSeveranceArenaBlueprint Arena { get; }

    public ThirdSeveranceArenaAccessPolicy AccessPolicy { get; }

    public ThirdSeveranceBossPlan Boss { get; }

    public IReadOnlyList<ThirdSeverancePhaseDefinition> Phases { get; }

    public ThirdSeverancePhaseId FirstPhase => ThirdSeverancePhaseId.BaseActivation;

    public int HardEnrageOverloadThreshold => 3;

    public ThirdSeveranceFailureOutcome LoopExhaustionOutcome =>
        ThirdSeveranceFailureOutcome.AdvanceHardEnrage;

    private static ThirdSeveranceEncounterPlan CreateDefault()
    {
        var strategicPartKeys = Array.AsReadOnly(
            new[]
            {
                ThirdSeveranceBossKeys.CrownPart,
                ThirdSeveranceBossKeys.WingsPart,
                ThirdSeveranceBossKeys.HeartCasingPart,
            });

        var phases = Array.AsReadOnly(
            new[]
            {
                new ThirdSeverancePhaseDefinition(
                    ThirdSeverancePhaseId.BaseActivation,
                    ThirdSeveranceBossKeys.SealedForm,
                    360,
                    ThirdSeverancePhaseExitRule.PreparationComplete,
                    ThirdSeverancePhaseId.SealRelease,
                    ThirdSeverancePhaseId.SealRelease,
                    1,
                    false,
                    Array.AsReadOnly<ThirdSeveranceMechanicDefinition>(
                        new ThirdSeveranceMechanicDefinition[]
                        {
                            new ThirdSeveranceBossAttackPatternDefinition(
                                "base_activation_warning",
                                0,
                                300,
                                "sealed_observation",
                                ThirdSeveranceTargetRule.None,
                                ThirdSeveranceFailureOutcome.None),
                        })),
                new ThirdSeverancePhaseDefinition(
                    ThirdSeverancePhaseId.SealRelease,
                    ThirdSeveranceBossKeys.SealedForm,
                    1800,
                    ThirdSeverancePhaseExitRule.MechanicResolvedOrDeadline,
                    ThirdSeverancePhaseId.PartBreak,
                    ThirdSeverancePhaseId.PartBreak,
                    1,
                    false,
                    Array.AsReadOnly<ThirdSeveranceMechanicDefinition>(
                        new ThirdSeveranceMechanicDefinition[]
                        {
                            new ThirdSeverancePylonDpsCheckDefinition(
                                "seal_release_pylon_dps_check",
                                180,
                                1200,
                                new ThirdSeveranceParticipantScaledInt(2, 3, 4),
                                ThirdSeverancePylonSelectionRule.RosterMappedSymmetricSlots,
                                "balance_pylon_damage_per_participant",
                                true,
                                ThirdSeveranceFailureOutcome.ApplyOverload),
                            new ThirdSeveranceBossAttackPatternDefinition(
                                "seal_release_pylon_crossfire",
                                300,
                                1200,
                                "pylon_crossfire",
                                ThirdSeveranceTargetRule.AllParticipants,
                                ThirdSeveranceFailureOutcome.ApplyParticipantDebuff),
                        })),
                new ThirdSeverancePhaseDefinition(
                    ThirdSeverancePhaseId.PartBreak,
                    ThirdSeveranceBossKeys.ManifestForm,
                    1800,
                    ThirdSeverancePhaseExitRule.MechanicResolvedOrDeadline,
                    ThirdSeverancePhaseId.Coordination,
                    ThirdSeverancePhaseId.Coordination,
                    3,
                    true,
                    Array.AsReadOnly<ThirdSeveranceMechanicDefinition>(
                        new ThirdSeveranceMechanicDefinition[]
                        {
                            new ThirdSeverancePartBreakDefinition(
                                "part_break_strategic_choice",
                                120,
                                1320,
                                strategicPartKeys,
                                1),
                            new ThirdSeveranceBossAttackPatternDefinition(
                                "part_break_wing_crossing",
                                300,
                                1200,
                                "wing_crossing",
                                ThirdSeveranceTargetRule.FarthestParticipant,
                                ThirdSeveranceFailureOutcome.ApplyParticipantDebuff),
                        })),
                new ThirdSeverancePhaseDefinition(
                    ThirdSeverancePhaseId.Coordination,
                    ThirdSeveranceBossKeys.ManifestForm,
                    1800,
                    ThirdSeverancePhaseExitRule.AllScheduledMechanicsResolved,
                    ThirdSeverancePhaseId.PersonalEffigies,
                    ThirdSeverancePhaseId.PersonalEffigies,
                    3,
                    true,
                    Array.AsReadOnly<ThirdSeveranceMechanicDefinition>(
                        new ThirdSeveranceMechanicDefinition[]
                        {
                            new ThirdSeveranceStackDefinition(
                                "coordination_stack",
                                120,
                                300,
                                10,
                                new ThirdSeveranceParticipantScaledInt(2, 2, 3),
                                1),
                            new ThirdSeveranceSpreadDefinition(
                                "coordination_spread",
                                600,
                                300,
                                16,
                                7),
                            new ThirdSeveranceTargetedLineDefinition(
                                "coordination_targeted_line",
                                1080,
                                360,
                                120,
                                9,
                                ThirdSeveranceTargetRule.HighestRecentServerDamageParticipant),
                            new ThirdSeveranceBossAttackPatternDefinition(
                                "coordination_choir_sweep",
                                60,
                                1560,
                                "choir_sweep",
                                ThirdSeveranceTargetRule.AllParticipants,
                                ThirdSeveranceFailureOutcome.ApplyParticipantDebuff),
                        })),
                new ThirdSeverancePhaseDefinition(
                    ThirdSeverancePhaseId.PersonalEffigies,
                    ThirdSeveranceBossKeys.ConvergenceForm,
                    1800,
                    ThirdSeverancePhaseExitRule.MechanicResolvedOrDeadline,
                    ThirdSeverancePhaseId.WeakPointExposure,
                    ThirdSeverancePhaseId.WeakPointExposure,
                    3,
                    true,
                    Array.AsReadOnly<ThirdSeveranceMechanicDefinition>(
                        new ThirdSeveranceMechanicDefinition[]
                        {
                            new ThirdSeverancePersonalEffigyDefinition(
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
                            new ThirdSeveranceBossAttackPatternDefinition(
                                "personal_effigies_identity_pressure",
                                300,
                                1200,
                                "identity_pressure",
                                ThirdSeveranceTargetRule.AllParticipants,
                                ThirdSeveranceFailureOutcome.StrengthenBoss),
                        })),
                new ThirdSeverancePhaseDefinition(
                    ThirdSeverancePhaseId.WeakPointExposure,
                    ThirdSeveranceBossKeys.ExposedForm,
                    900,
                    ThirdSeverancePhaseExitRule.BossLifeOrDeadline,
                    ThirdSeverancePhaseId.PartBreak,
                    ThirdSeverancePhaseId.PartBreak,
                    3,
                    true,
                    Array.AsReadOnly<ThirdSeveranceMechanicDefinition>(
                        new ThirdSeveranceMechanicDefinition[]
                        {
                            new ThirdSeveranceWeakPointExposureDefinition(
                                "weak_point_core_burst",
                                120,
                                780,
                                ThirdSeveranceBossKeys.CoreWeakPoint,
                                "balance_core_burst_damage_per_participant",
                                ThirdSeveranceFailureOutcome.ApplyOverload),
                            new ThirdSeveranceBossAttackPatternDefinition(
                                "weak_point_sustained_beam",
                                120,
                                780,
                                "sustained_exposure_beam",
                                ThirdSeveranceTargetRule.AllParticipants,
                                ThirdSeveranceFailureOutcome.ApplyParticipantDebuff),
                        })),
                new ThirdSeverancePhaseDefinition(
                    ThirdSeverancePhaseId.LastStand,
                    ThirdSeveranceBossKeys.LastStandForm,
                    2400,
                    ThirdSeverancePhaseExitRule.FixedSequenceComplete,
                    ThirdSeverancePhaseId.None,
                    ThirdSeverancePhaseId.None,
                    1,
                    false,
                    Array.AsReadOnly<ThirdSeveranceMechanicDefinition>(
                        new ThirdSeveranceMechanicDefinition[]
                        {
                            new ThirdSeveranceBossAttackPatternDefinition(
                                "last_stand_individual_judgment",
                                0,
                                1800,
                                "final_individual_judgment",
                                ThirdSeveranceTargetRule.AllParticipants,
                                ThirdSeveranceFailureOutcome.ApplyParticipantDebuff),
                            new ThirdSeveranceTargetedLineDefinition(
                                "last_stand_targeted_line",
                                120,
                                420,
                                150,
                                8,
                                ThirdSeveranceTargetRule.DeterministicRandomParticipant),
                            new ThirdSeveranceSpreadDefinition(
                                "last_stand_spread",
                                720,
                                360,
                                18,
                                7),
                            new ThirdSeveranceStackDefinition(
                                "last_stand_stack",
                                1260,
                                360,
                                9,
                                new ThirdSeveranceParticipantScaledInt(2, 3, 4),
                                1),
                            new ThirdSeveranceWeakPointExposureDefinition(
                                "last_stand_final_core",
                                1800,
                                600,
                                ThirdSeveranceBossKeys.CoreWeakPoint,
                                "balance_last_stand_core_damage",
                                ThirdSeveranceFailureOutcome.AdvanceHardEnrage),
                        })),
            });

        return new ThirdSeveranceEncounterPlan(
            ThirdSeveranceArenaBlueprint.Instance,
            ThirdSeveranceArenaAccessPolicy.Instance,
            ThirdSeveranceBossPlan.CreateDefault(),
            phases);
    }

    private void ValidatePlan()
    {
        if (Arena.Profile.WidthInTiles != ThirdSeveranceArenaBlueprint.WidthInTiles
            || Arena.Profile.HeightInTiles != ThirdSeveranceArenaBlueprint.HeightInTiles)
        {
            throw new InvalidOperationException("Third Severance requires the 320x140 arena profile.");
        }

        var formsByKey = new Dictionary<string, ThirdSeveranceBossFormDefinition>(StringComparer.Ordinal);
        for (int formIndex = 0; formIndex < Boss.Forms.Count; formIndex++)
        {
            ThirdSeveranceBossFormDefinition form = Boss.Forms[formIndex];
            formsByKey.Add(form.Key, form);
        }

        var partKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int partIndex = 0; partIndex < Boss.Parts.Count; partIndex++)
        {
            partKeys.Add(Boss.Parts[partIndex].Key);
        }

        var phasesById = new Dictionary<ThirdSeverancePhaseId, ThirdSeverancePhaseDefinition>();
        var mechanicKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int phaseIndex = 0; phaseIndex < Phases.Count; phaseIndex++)
        {
            ThirdSeverancePhaseDefinition phase = Phases[phaseIndex];
            if (!phasesById.TryAdd(phase.Id, phase))
            {
                throw new InvalidOperationException($"Duplicate phase '{phase.Id}'.");
            }

            if (!formsByKey.TryGetValue(phase.BossFormKey, out ThirdSeveranceBossFormDefinition form))
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
            || !phasesById.ContainsKey(ThirdSeverancePhaseId.LastStand))
        {
            throw new InvalidOperationException("The plan requires activation and Last Stand phases.");
        }

        for (int phaseIndex = 0; phaseIndex < Phases.Count; phaseIndex++)
        {
            ThirdSeverancePhaseDefinition phase = Phases[phaseIndex];
            ValidateDestination(phase.Id, phase.NextPhaseOnResolution, phasesById);
            ValidateDestination(phase.Id, phase.NextPhaseOnSoftFailure, phasesById);
            ValidateTerminalContract(phase);
        }

        ValidateReachabilityAndFiniteCycles(phasesById);
    }

    private static void ValidateTerminalContract(ThirdSeverancePhaseDefinition phase)
    {
        if (phase.Id == ThirdSeverancePhaseId.LastStand)
        {
            if (phase.NextPhaseOnResolution != ThirdSeverancePhaseId.None
                || phase.NextPhaseOnSoftFailure != ThirdSeverancePhaseId.None
                || phase.MaximumVisits != 1
                || phase.CanBeInterruptedByLastStand
                || phase.ExitRule != ThirdSeverancePhaseExitRule.FixedSequenceComplete)
            {
                throw new InvalidOperationException(
                    "Last Stand must be a single-visit terminal fixed sequence.");
            }

            return;
        }

        if (phase.NextPhaseOnResolution == ThirdSeverancePhaseId.None
            || phase.NextPhaseOnSoftFailure == ThirdSeverancePhaseId.None)
        {
            throw new InvalidOperationException(
                $"Non-terminal phase '{phase.Id}' requires resolution and soft-failure targets.");
        }
    }

    private static void ValidateDestination(
        ThirdSeverancePhaseId source,
        ThirdSeverancePhaseId destination,
        IReadOnlyDictionary<ThirdSeverancePhaseId, ThirdSeverancePhaseDefinition> knownPhases)
    {
        if (destination != ThirdSeverancePhaseId.None && !knownPhases.Contains(destination))
        {
            throw new InvalidOperationException(
                $"Phase '{source}' references unknown destination '{destination}'.");
        }
    }

    private void ValidateReachabilityAndFiniteCycles(
        IReadOnlyDictionary<ThirdSeverancePhaseId, ThirdSeverancePhaseDefinition> phasesById)
    {
        var reachable = new HashSet<ThirdSeverancePhaseId>();
        var pending = new Queue<ThirdSeverancePhaseId>();
        reachable.Add(FirstPhase);
        pending.Enqueue(FirstPhase);

        while (pending.Count > 0)
        {
            ThirdSeverancePhaseId current = pending.Dequeue();
            ThirdSeverancePhaseDefinition phase = phasesById[current];
            EnqueueReachable(phase.NextPhaseOnResolution, reachable, pending);
            EnqueueReachable(phase.NextPhaseOnSoftFailure, reachable, pending);

            // Boss life crossing the threshold is an implicit authority edge from
            // interruptible phases into Last Stand; it is part of graph validity.
            if (phase.CanBeInterruptedByLastStand)
            {
                EnqueueReachable(ThirdSeverancePhaseId.LastStand, reachable, pending);
            }
        }

        foreach (ThirdSeverancePhaseId phaseId in phasesById.Keys)
        {
            if (!reachable.Contains(phaseId))
            {
                throw new InvalidOperationException($"Phase '{phaseId}' is unreachable.");
            }
        }

        var visiting = new HashSet<ThirdSeverancePhaseId>();
        var visited = new HashSet<ThirdSeverancePhaseId>();
        bool hasCycle = HasCycleFrom(FirstPhase, phasesById, visiting, visited);
        if (hasCycle && LoopExhaustionOutcome == ThirdSeveranceFailureOutcome.None)
        {
            throw new InvalidOperationException(
                "A cyclic phase graph requires a non-empty loop exhaustion outcome.");
        }
    }

    private static void EnqueueReachable(
        ThirdSeverancePhaseId destination,
        HashSet<ThirdSeverancePhaseId> reachable,
        Queue<ThirdSeverancePhaseId> pending)
    {
        if (destination != ThirdSeverancePhaseId.None && reachable.Add(destination))
        {
            pending.Enqueue(destination);
        }
    }

    private static bool HasCycleFrom(
        ThirdSeverancePhaseId phaseId,
        IReadOnlyDictionary<ThirdSeverancePhaseId, ThirdSeverancePhaseDefinition> phasesById,
        HashSet<ThirdSeverancePhaseId> visiting,
        HashSet<ThirdSeverancePhaseId> visited)
    {
        if (visited.Contains(phaseId))
        {
            return false;
        }

        if (!visiting.Add(phaseId))
        {
            return true;
        }

        ThirdSeverancePhaseDefinition phase = phasesById[phaseId];
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
                    ThirdSeverancePhaseId.LastStand,
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
        ThirdSeverancePhaseId destination,
        IReadOnlyDictionary<ThirdSeverancePhaseId, ThirdSeverancePhaseDefinition> phasesById,
        HashSet<ThirdSeverancePhaseId> visiting,
        HashSet<ThirdSeverancePhaseId> visited)
    {
        return destination != ThirdSeverancePhaseId.None
            && HasCycleFrom(destination, phasesById, visiting, visited);
    }

    private void ValidateMechanicReferences(
        ThirdSeverancePhaseDefinition phase,
        ThirdSeveranceBossFormDefinition form,
        ThirdSeveranceMechanicDefinition mechanic,
        HashSet<string> knownPartKeys)
    {
        if (mechanic is ThirdSeverancePartBreakDefinition partBreak)
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

        if (mechanic is ThirdSeveranceWeakPointExposureDefinition exposure
            && !string.Equals(exposure.WeakPointKey, Boss.WeakPoint.Key, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Mechanic '{mechanic.Key}' references unknown weak point '{exposure.WeakPointKey}'.");
        }

        if (mechanic is ThirdSeveranceBossAttackPatternDefinition attack
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
