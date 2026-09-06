#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Compatibility.Calamity;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Replication;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Default First Severance plans validate")]
    private static void DefaultFirstSeverancePlansValidate()
    {
        FirstSeveranceEncounterPlan plan = FirstSeveranceEncounterPlan.Instance;
        AssertEqual(FirstSeveranceSubstate.SpawnIntro, plan.FirstSubstate, "first substate");
        AssertEqual(6, plan.Substates.Count, "substate count");
        AssertEqual(160, plan.Arena.Profile.WidthInTiles, "Arena width profile");
        AssertEqual(70, plan.Arena.Profile.HeightInTiles, "Arena height profile");
        AssertEqual(3, plan.OverloadThreshold, "Overload threshold");
        AssertEqual(8, plan.MaximumCompletedExposures, "exposure cap");
        AssertEqual(true, plan.Boss.UsesPersistentLifePool, "persistent Boss life");

        int[] expectedPylons = { 2, 3, 4 };
        int[] expectedShares = { 2, 3, 4 };
        for (int participantCount = 2; participantCount <= 4; participantCount++)
        {
            AssertEqual(
                expectedPylons[participantCount - 2],
                plan.PylonCount.GetValue(participantCount),
                $"{participantCount}-player Pylon count");
            AssertEqual(
                expectedShares[participantCount - 2],
                plan.StackRequiredShares.GetValue(participantCount),
                $"{participantCount}-player Stack shares");
        }

        AssertEqual(360, plan.Timing.SpawnIntroTicks, "SpawnIntro duration");
        AssertEqual(930, plan.Timing.PylonCheckTicks, "Pylon duration");
        AssertEqual(240, plan.Timing.StackTelegraphTicks, "Stack duration");
        AssertEqual(240, plan.Timing.SpreadTelegraphTicks, "Spread duration");
        AssertEqual(1080, plan.Timing.NormalExposureTicks, "normal exposure duration");
        AssertEqual(720, plan.Timing.PenalizedExposureTicks, "penalized exposure duration");
        AssertEqual(120, plan.Timing.ResetTicks, "Reset duration");

        FirstSeveranceSubstate[] expectedSubstates =
        {
            FirstSeveranceSubstate.None,
            FirstSeveranceSubstate.SpawnIntro,
            FirstSeveranceSubstate.PylonCheck,
            FirstSeveranceSubstate.Stack,
            FirstSeveranceSubstate.Spread,
            FirstSeveranceSubstate.CoreExposure,
            FirstSeveranceSubstate.Reset,
            FirstSeveranceSubstate.PhaseTransition,
            FirstSeveranceSubstate.Lattice,
            FirstSeveranceSubstate.RotatingBlade,
            FirstSeveranceSubstate.RemoteClaws,
            FirstSeveranceSubstate.HalfField,
            FirstSeveranceSubstate.FinalBullets,
            FirstSeveranceSubstate.FinalSlicer,
            FirstSeveranceSubstate.RemoteCrush,
        };
        FirstSeveranceMechanicKind[] expectedMechanics =
        {
            FirstSeveranceMechanicKind.None,
            FirstSeveranceMechanicKind.PylonCheck,
            FirstSeveranceMechanicKind.Stack,
            FirstSeveranceMechanicKind.Spread,
            FirstSeveranceMechanicKind.CoreExposure,
        };
        AssertEnumValues(expectedSubstates, "active substates");
        AssertEnumValues(expectedMechanics, "active mechanics");
    }

    [DomainTest("Invalid First Severance plans are rejected")]
    private static void InvalidFirstSeverancePlansAreRejected()
    {
        FirstSeveranceEncounterPlan baseline = FirstSeveranceEncounterPlan.Instance;
        AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new FirstSeveranceTimingPlan(
                0,
                60,
                600,
                180,
                180,
                720,
                360,
                90),
            "zero duration");

        AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new FirstSeveranceEncounterPlan(
                baseline.Arena,
                baseline.AccessPolicy,
                baseline.Boss,
                baseline.Timing,
                new FirstSeveranceParticipantScaledInt(2, 3, 5),
                baseline.StackRequiredShares,
                baseline.OverloadThreshold,
                baseline.MaximumCompletedExposures,
                baseline.Substates,
                baseline.TerminationContract),
            "Pylon count");

        AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new FirstSeveranceEncounterPlan(
                baseline.Arena,
                baseline.AccessPolicy,
                baseline.Boss,
                baseline.Timing,
                baseline.PylonCount,
                baseline.StackRequiredShares,
                overloadThreshold: 0,
                baseline.MaximumCompletedExposures,
                baseline.Substates,
                baseline.TerminationContract),
            "Overload threshold");

        var duplicateMechanic = new List<FirstSeveranceSubstateDefinition>(baseline.Substates);
        FirstSeveranceSubstateDefinition spread = baseline.GetSubstate(
            FirstSeveranceSubstate.Spread);
        for (int index = 0; index < duplicateMechanic.Count; index++)
        {
            if (duplicateMechanic[index].Substate == FirstSeveranceSubstate.Spread)
            {
                duplicateMechanic[index] = new FirstSeveranceSubstateDefinition(
                    spread.Substate,
                    FirstSeveranceMechanicKind.Stack,
                    spread.DurationTicks,
                    spread.SuccessEdge,
                    spread.FailureEdge);
                break;
            }
        }

        AssertThrows<ArgumentException>(
            () => _ = new FirstSeveranceEncounterPlan(
                baseline.Arena,
                baseline.AccessPolicy,
                baseline.Boss,
                baseline.Timing,
                baseline.PylonCount,
                baseline.StackRequiredShares,
                baseline.OverloadThreshold,
                baseline.MaximumCompletedExposures,
                duplicateMechanic,
                baseline.TerminationContract),
            "duplicate mechanic owner");
    }

    [DomainTest("Pylons complete early and at the deadline")]
    private static void PylonsCompleteWithoutOverload()
    {
        FirstSeveranceLoopStateMachine early = CreateLoopMachine(participantCount: 4);
        EnterPylonCheck(early);
        ulong firstDamageableTick = early.State.SubstateEnteredTick
            + (ulong)FirstSeveranceEncounterPlan.Instance.Timing.PylonTelegraphTicks;
        AssertApplied(early.Advance(new FirstSeveranceLoopInput(
            firstDamageableTick,
            early.State.RemainingPylons,
            AcceptedBossDamage: 0)));
        AssertEqual(FirstSeveranceSubstate.Stack, early.State.Substate, "early Pylon edge");
        AssertEqual(0, early.State.Overload, "early Pylon Overload");

        FirstSeveranceLoopStateMachine closingTick = CreateLoopMachine(participantCount: 3);
        EnterPylonCheck(closingTick);
        AssertApplied(closingTick.Advance(new FirstSeveranceLoopInput(
            closingTick.State.ResolveTick,
            closingTick.State.RemainingPylons,
            AcceptedBossDamage: 0)));
        AssertEqual(
            FirstSeveranceSubstate.Stack,
            closingTick.State.Substate,
            "closing-tick Pylon edge");
        AssertEqual(0, closingTick.State.Overload, "closing-tick Pylon Overload");
    }

    [DomainTest("Pylon failures reach the third-Overload defeat")]
    private static void PylonFailuresReachOverloadDefeat()
    {
        FirstSeveranceLoopStateMachine machine = CreateLoopMachine(participantCount: 2);
        EnterPylonCheck(machine);

        for (int failure = 1; failure <= 3; failure++)
        {
            FirstSeveranceLoopUpdate result = machine.Advance(new FirstSeveranceLoopInput(
                machine.State.ResolveTick,
                DestroyedPylons: 0,
                AcceptedBossDamage: 0));
            AssertEqual(failure, machine.State.Overload, $"Overload after failure {failure}");

            if (failure == 3)
            {
                AssertEnded(result);
                break;
            }

            AssertApplied(result);
            AssertEqual(FirstSeveranceSubstate.Stack, machine.State.Substate, "post-failure Stack");
            AssertEqual(true, machine.State.IsPenalizedExposure, "penalized loop flag");
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.Spread);
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.CoreExposure);
            AssertEqual(
                (ulong)FirstSeveranceEncounterPlan.Instance.Timing.PenalizedExposureTicks,
                machine.State.ResolveTick - machine.State.SubstateEnteredTick,
                "penalized exposure duration");
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.Reset);
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.PylonCheck);
        }

        AssertTermination(
            machine.State.Termination,
            EncounterEndReason.Defeat,
            FirstSeveranceTerminalCause.OverloadLimit);
        AssertEqual(FirstSeveranceSubstate.PylonCheck, machine.State.Substate, "terminal source state");
    }

    [DomainTest("The full loop preserves Boss life")]
    private static void FullLoopPreservesBossLife()
    {
        FirstSeveranceLoopStateMachine machine = CreateLoopMachine(
            participantCount: 4,
            bossMaximumLife: 100);

        AssertApplied(machine.Advance(new FirstSeveranceLoopInput(
            AuthorityTick: 1,
            DestroyedPylons: 0,
            AcceptedBossDamage: 40)));
        AssertEqual(100, machine.State.BossLife, "shielded Boss life");

        EnterPylonCheck(machine);
        ClearCurrentPylons(machine);
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.Spread);
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.CoreExposure);
        AssertApplied(machine.Advance(new FirstSeveranceLoopInput(
            machine.State.SubstateEnteredTick + 1,
            DestroyedPylons: 0,
            AcceptedBossDamage: 30)));
        AssertEqual(70, machine.State.BossLife, "exposure Boss life");
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.Reset);
        AssertEqual(1, machine.State.CompletedExposures, "completed exposures");
        AssertEqual(70, machine.State.BossLife, "Boss life after exposure");
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.PylonCheck);
        AssertEqual(1, machine.State.ZeroBasedLoopIndex, "second loop index");
        AssertEqual(70, machine.State.BossLife, "Boss life entering second loop");
        AssertEqual(false, machine.State.IsPenalizedExposure, "new clean loop flag");
    }

    [DomainTest("Exposure victory wins on its deadline tick")]
    private static void ExposureVictoryWinsDeadlineCollision()
    {
        FirstSeveranceLoopStateMachine machine = CreateLoopMachine(
            participantCount: 3,
            bossMaximumLife: 100);
        EnterPylonCheck(machine);
        ClearCurrentPylons(machine);
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.Spread);
        AdvanceAtDeadline(machine, FirstSeveranceSubstate.CoreExposure);

        FirstSeveranceLoopUpdate result = machine.Advance(new FirstSeveranceLoopInput(
            machine.State.ResolveTick,
            DestroyedPylons: 0,
            AcceptedBossDamage: 100));
        AssertEnded(result);
        AssertTermination(
            machine.State.Termination,
            EncounterEndReason.Victory,
            FirstSeveranceTerminalCause.BossLifeZero);
        AssertEqual(0, machine.State.CompletedExposures, "closing-tick exposure count");
    }

    [DomainTest("The eighth exposure ends at the loop cap")]
    private static void EighthExposureEndsAtLoopCap()
    {
        FirstSeveranceLoopStateMachine machine = CreateLoopMachine(participantCount: 4);
        EnterPylonCheck(machine);

        for (int exposure = 1; exposure <= 8; exposure++)
        {
            ClearCurrentPylons(machine);
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.Spread);
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.CoreExposure);
            FirstSeveranceLoopUpdate result = machine.Advance(new FirstSeveranceLoopInput(
                machine.State.ResolveTick,
                DestroyedPylons: 0,
                AcceptedBossDamage: 0));

            if (exposure == 8)
            {
                AssertEnded(result);
                break;
            }

            AssertApplied(result);
            AssertEqual(FirstSeveranceSubstate.Reset, machine.State.Substate, "Reset edge");
            AdvanceAtDeadline(machine, FirstSeveranceSubstate.PylonCheck);
        }

        AssertEqual(8, machine.State.CompletedExposures, "completed exposure cap");
        AssertTermination(
            machine.State.Termination,
            EncounterEndReason.Defeat,
            FirstSeveranceTerminalCause.LoopCapExceeded);

        FirstSeveranceLoopState frozen = machine.State;
        AssertEqual(
            FirstSeveranceLoopUpdateDisposition.NoChange,
            machine.Advance(new FirstSeveranceLoopInput(
                frozen.LastAuthorityTick + 1,
                0,
                0)).Disposition,
            "post-terminal update");
        AssertEqual(frozen, machine.State, "post-terminal state");
    }
}
