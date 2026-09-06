#nullable enable

using System;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeveranceLoopInput(
    ulong AuthorityTick,
    int DestroyedPylons,
    int AcceptedBossDamage);

internal enum FirstSeveranceLoopUpdateDisposition : byte
{
    Rejected = 0,
    NoChange = 1,
    Applied = 2,
    Ended = 3,
}

internal readonly record struct FirstSeveranceLoopUpdate(
    FirstSeveranceLoopUpdateDisposition Disposition,
    string FailureCode)
{
    public static FirstSeveranceLoopUpdate NoChange =>
        new(FirstSeveranceLoopUpdateDisposition.NoChange, string.Empty);

    public static FirstSeveranceLoopUpdate Applied =>
        new(FirstSeveranceLoopUpdateDisposition.Applied, string.Empty);

    public static FirstSeveranceLoopUpdate Ended =>
        new(FirstSeveranceLoopUpdateDisposition.Ended, string.Empty);

    public static FirstSeveranceLoopUpdate Reject(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A rejection requires a failure code.", nameof(failureCode));
        }

        return new FirstSeveranceLoopUpdate(
            FirstSeveranceLoopUpdateDisposition.Rejected,
            failureCode);
    }
}

internal readonly record struct FirstSeveranceLoopState(
    int ParticipantCount,
    FirstSeveranceSubstate Substate,
    ulong SubstateEnteredTick,
    ulong ResolveTick,
    ulong LastAuthorityTick,
    int ZeroBasedLoopIndex,
    int CompletedExposures,
    int Overload,
    int RemainingPylons,
    bool IsPenalizedExposure,
    int BossMaximumLife,
    int BossLife,
    EncounterTerminationDescriptor Termination)
{
    public bool IsTerminal => !Termination.IsNone;
    public FirstSeveranceBossPhase BossPhase { get; init; } = FirstSeveranceBossPhase.Sealed;
    public ulong BossPhaseStartedTick { get; init; }
    public int ActionIndex { get; init; } = -1;
    public int CompletedPhaseCycles { get; init; }
}

internal sealed class FirstSeveranceLoopStateMachine
{
    private const string InvalidTickFailure = "first_severance.loop_tick_invalid";
    private const string InvalidInputFailure = "first_severance.loop_input_invalid";

    private readonly FirstSeveranceEncounterPlan plan;
    private readonly FirstSeveranceBossPhasePlan? bossPhases;

    public FirstSeveranceLoopStateMachine(
        FirstSeveranceEncounterPlan plan,
        int participantCount,
        int bossMaximumLife,
        ulong startTick,
        FirstSeveranceBossPhasePlan? bossPhases = null)
    {
        this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
        this.bossPhases = bossPhases;

        if (participantCount is < FirstSeveranceRoster.MinimumCount or > FirstSeveranceRoster.MaximumCount)
        {
            throw new ArgumentOutOfRangeException(nameof(participantCount));
        }

        if (bossMaximumLife <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bossMaximumLife));
        }

        State = new FirstSeveranceLoopState(
            participantCount,
            plan.FirstSubstate,
            startTick,
            AddDuration(startTick, plan.Timing.SpawnIntroTicks),
            startTick,
            ZeroBasedLoopIndex: 0,
            CompletedExposures: 0,
            Overload: 0,
            RemainingPylons: 0,
            IsPenalizedExposure: false,
            bossMaximumLife,
            BossLife: bossMaximumLife,
            EncounterTerminationDescriptor.None);
    }

    public FirstSeveranceLoopState State { get; private set; }

    internal bool WillChangeStage(int damage) => bossPhases is not null
        && State.CompletedPhaseCycles > 0
        && FirstSeveranceBossPhasePlan.IsDamageState(State.Substate)
        && bossPhases.TryGetNext(State.BossPhase, out var next)
        && (long)State.BossLife - damage <= FirstSeveranceBossPhasePlan.LifeThreshold(State.BossMaximumLife, next);

    public FirstSeveranceLoopUpdate Advance(in FirstSeveranceLoopInput input)
    {
        if (State.IsTerminal)
        {
            return FirstSeveranceLoopUpdate.NoChange;
        }

        if (input.AuthorityTick <= State.LastAuthorityTick)
        {
            return FirstSeveranceLoopUpdate.Reject(InvalidTickFailure);
        }

        if (input.DestroyedPylons < 0 || input.AcceptedBossDamage < 0)
        {
            return FirstSeveranceLoopUpdate.Reject(InvalidInputFailure);
        }

        FirstSeveranceLoopState before = State;
        State = State with { LastAuthorityTick = input.AuthorityTick };

        FirstSeveranceLoopUpdate inputResult = ApplyObservedInput(input);
        if (inputResult.Disposition == FirstSeveranceLoopUpdateDisposition.Rejected)
        {
            State = before;
            return inputResult;
        }

        if (bossPhases is not null && State.CompletedPhaseCycles > 0 && input.AuthorityTick >= State.ResolveTick
            && FirstSeveranceBossPhasePlan.IsDamageState(State.Substate)
            && bossPhases.TryGetNext(State.BossPhase, out var next)
            && State.BossLife <= FirstSeveranceBossPhasePlan.LifeThreshold(State.BossMaximumLife, next))
        {
            State = State with { BossPhase = next.Id, BossPhaseStartedTick = input.AuthorityTick, ActionIndex = -1, CompletedPhaseCycles = 0 };
            EnterSubstate(FirstSeveranceSubstate.PhaseTransition, input.AuthorityTick);
            return FirstSeveranceLoopUpdate.Applied;
        }
        if (bossPhases is null && FirstSeveranceBossPhasePlan.IsDamageState(State.Substate) && State.BossLife == 0)
        {
            return End(FirstSeveranceTerminalCause.BossLifeZero);
        }

        if (State.Substate == FirstSeveranceSubstate.PylonCheck
            && State.RemainingPylons == 0)
        {
            EnterSubstate(FirstSeveranceSubstate.Stack, input.AuthorityTick);
            return FirstSeveranceLoopUpdate.Applied;
        }

        if (input.AuthorityTick < State.ResolveTick)
        {
            return State == before
                ? FirstSeveranceLoopUpdate.NoChange
                : FirstSeveranceLoopUpdate.Applied;
        }

        return ResolveDeadline(input.AuthorityTick);
    }

    private FirstSeveranceLoopUpdate ApplyObservedInput(in FirstSeveranceLoopInput input)
    {
        if (input.DestroyedPylons > 0
            && State.Substate == FirstSeveranceSubstate.PylonCheck
            && input.AuthorityTick >= AddDuration(
                State.SubstateEnteredTick,
                plan.Timing.PylonTelegraphTicks))
        {
            if (input.DestroyedPylons > State.RemainingPylons)
            {
                return FirstSeveranceLoopUpdate.Reject(InvalidInputFailure);
            }

            State = State with
            {
                RemainingPylons = State.RemainingPylons - input.DestroyedPylons,
            };
        }

        if (input.AcceptedBossDamage > 0
            && FirstSeveranceBossPhasePlan.IsDamageState(State.Substate))
        {
            int floor = bossPhases is not null && bossPhases.TryGetNext(State.BossPhase, out var next)
                ? FirstSeveranceBossPhasePlan.LifeThreshold(State.BossMaximumLife, next) : 0;
            // A single overpowered hit cannot skip the requested transformation.
            int acceptedDamage = Math.Min(input.AcceptedBossDamage, Math.Max(0, State.BossLife - floor));
            State = State with { BossLife = State.BossLife - acceptedDamage };
        }

        return FirstSeveranceLoopUpdate.Applied;
    }

    private FirstSeveranceLoopUpdate ResolveDeadline(ulong authorityTick)
    {
        if (bossPhases is not null && State.ActionIndex >= 0)
            return AdvanceScore(authorityTick);
        switch (State.Substate)
        {
            case FirstSeveranceSubstate.SpawnIntro:
                EnterSubstate(FirstSeveranceSubstate.PylonCheck, authorityTick);
                return FirstSeveranceLoopUpdate.Applied;

            case FirstSeveranceSubstate.PylonCheck:
                int nextOverload = checked(State.Overload + 1);
                State = State with
                {
                    Overload = nextOverload,
                    IsPenalizedExposure = true,
                };
                if (nextOverload >= plan.OverloadThreshold)
                {
                    return End(FirstSeveranceTerminalCause.OverloadLimit);
                }

                EnterSubstate(FirstSeveranceSubstate.Stack, authorityTick);
                return FirstSeveranceLoopUpdate.Applied;

            case FirstSeveranceSubstate.Stack:
                EnterSubstate(FirstSeveranceSubstate.Spread, authorityTick);
                return FirstSeveranceLoopUpdate.Applied;

            case FirstSeveranceSubstate.Spread:
                EnterSubstate(FirstSeveranceSubstate.CoreExposure, authorityTick);
                return FirstSeveranceLoopUpdate.Applied;

            case FirstSeveranceSubstate.CoreExposure:
            case FirstSeveranceSubstate.Lattice:
                int completedExposures = checked(State.CompletedExposures + 1);
                State = State with { CompletedExposures = completedExposures };
                if (bossPhases is not null)
                {
                    EnterAction(0, authorityTick);
                    return FirstSeveranceLoopUpdate.Applied;
                }
                if (completedExposures >= plan.MaximumCompletedExposures)
                {
                    return End(FirstSeveranceTerminalCause.LoopCapExceeded);
                }

                EnterSubstate(State.Substate == FirstSeveranceSubstate.Lattice
                    ? FirstSeveranceSubstate.Lattice : FirstSeveranceSubstate.Reset, authorityTick);
                return FirstSeveranceLoopUpdate.Applied;

            case FirstSeveranceSubstate.PhaseTransition:
                // The active stage is a distinct attack module, not a resumed
                // Pylon cycle. Existing recovery and the exact Fight persist.
                EnterAction(0, authorityTick);
                return FirstSeveranceLoopUpdate.Applied;

            case FirstSeveranceSubstate.Reset:
                State = State with
                {
                    ZeroBasedLoopIndex = checked(State.ZeroBasedLoopIndex + 1),
                };
                EnterSubstate(FirstSeveranceSubstate.PylonCheck, authorityTick);
                return FirstSeveranceLoopUpdate.Applied;

            default:
                return End(FirstSeveranceTerminalCause.RuntimeInvariantBroken);
        }
    }

    private void EnterSubstate(FirstSeveranceSubstate substate, ulong authorityTick)
    {
        bool penalizedExposure = substate == FirstSeveranceSubstate.CoreExposure
            && State.IsPenalizedExposure;
        int durationTicks = plan.GetDurationTicks(substate, penalizedExposure);
        if (bossPhases is not null && State.BossPhase != FirstSeveranceBossPhase.Sealed)
        {
            var stage = bossPhases.Get(State.BossPhase);
            if (substate == FirstSeveranceSubstate.PhaseTransition) durationTicks = stage.TransitionTicks;
            else if (substate == stage.ActiveState) durationTicks = stage.WindowTicks;
        }
        int remainingPylons = substate == FirstSeveranceSubstate.PylonCheck
            ? plan.PylonCount.GetValue(State.ParticipantCount)
            : 0;
        bool keepsPenalty = substate is FirstSeveranceSubstate.Stack
            or FirstSeveranceSubstate.Spread
            or FirstSeveranceSubstate.CoreExposure;

        State = State with
        {
            Substate = substate,
            SubstateEnteredTick = authorityTick,
            ResolveTick = AddDuration(authorityTick, durationTicks),
            RemainingPylons = remainingPylons,
            IsPenalizedExposure = keepsPenalty && State.IsPenalizedExposure,
        };
    }

    internal int DamageFloor => bossPhases is not null && bossPhases.TryGetNext(State.BossPhase, out var next)
        ? FirstSeveranceBossPhasePlan.LifeThreshold(State.BossMaximumLife, next) : 0;

    private FirstSeveranceLoopUpdate AdvanceScore(ulong tick)
    {
        var score = FirstSeveranceChoreography.For(State.BossPhase);
        if (State.ActionIndex + 1 < score.Count)
        {
            EnterAction(State.ActionIndex + 1, tick);
            return FirstSeveranceLoopUpdate.Applied;
        }
        State = State with { CompletedPhaseCycles = Math.Min(255, State.CompletedPhaseCycles + 1) };
        if (State.BossPhase == FirstSeveranceBossPhase.Final)
            return End(FirstSeveranceTerminalCause.BossLifeZero);
        if (bossPhases!.TryGetNext(State.BossPhase, out var next) && State.BossLife <= DamageFloor)
        {
            State = State with { BossPhase = next.Id, BossPhaseStartedTick = tick, ActionIndex = -1, CompletedPhaseCycles = 0 };
            EnterSubstate(FirstSeveranceSubstate.PhaseTransition, tick);
        }
        else if (State.BossPhase == FirstSeveranceBossPhase.Sealed)
        {
            State = State with { ActionIndex = -1 };
            EnterSubstate(FirstSeveranceSubstate.Reset, tick);
        }
        else EnterAction(0, tick);
        return FirstSeveranceLoopUpdate.Applied;
    }

    private void EnterAction(int index, ulong tick)
    {
        var action = FirstSeveranceChoreography.For(State.BossPhase)[index];
        State = State with { ActionIndex = index, Substate = action.State, SubstateEnteredTick = tick,
            ResolveTick = AddDuration(tick, action.Ticks), RemainingPylons = 0, IsPenalizedExposure = false };
    }

    private FirstSeveranceLoopUpdate End(FirstSeveranceTerminalCause cause)
    {
        State = State with
        {
            Termination = plan.TerminationContract.Create(cause),
            RemainingPylons = 0,
        };
        return FirstSeveranceLoopUpdate.Ended;
    }

    private static ulong AddDuration(ulong tick, int durationTicks)
    {
        return checked(tick + (ulong)durationTicks);
    }
}
