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
}

internal sealed class FirstSeveranceLoopStateMachine
{
    private const string InvalidTickFailure = "first_severance.loop_tick_invalid";
    private const string InvalidInputFailure = "first_severance.loop_input_invalid";

    private readonly FirstSeveranceEncounterPlan plan;

    public FirstSeveranceLoopStateMachine(
        FirstSeveranceEncounterPlan plan,
        int participantCount,
        int bossMaximumLife,
        ulong startTick)
    {
        this.plan = plan ?? throw new ArgumentNullException(nameof(plan));

        if (participantCount is < 2 or > 4)
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

        if (State.Substate == FirstSeveranceSubstate.CoreExposure && State.BossLife == 0)
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
            && State.Substate == FirstSeveranceSubstate.CoreExposure)
        {
            int acceptedDamage = Math.Min(input.AcceptedBossDamage, State.BossLife);
            State = State with { BossLife = State.BossLife - acceptedDamage };
        }

        return FirstSeveranceLoopUpdate.Applied;
    }

    private FirstSeveranceLoopUpdate ResolveDeadline(ulong authorityTick)
    {
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
                int completedExposures = checked(State.CompletedExposures + 1);
                State = State with { CompletedExposures = completedExposures };
                if (completedExposures >= plan.MaximumCompletedExposures)
                {
                    return End(FirstSeveranceTerminalCause.LoopCapExceeded);
                }

                EnterSubstate(FirstSeveranceSubstate.Reset, authorityTick);
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
