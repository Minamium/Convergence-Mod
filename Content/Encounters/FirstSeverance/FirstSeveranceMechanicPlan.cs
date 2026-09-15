#nullable enable

using System;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceSubstate : byte
{
    None = 0,
    SpawnIntro = 1,
    PylonCheck = 2,
    Stack = 3,
    Spread = 4,
    CoreExposure = 5,
    Reset = 6,
    PhaseTransition = 7,
    Lattice = 8,
    RotatingBlade = 9,
    RemoteClaws = 10,
    HalfField = 11,
    FinalBullets = 12,
    FinalSlicer = 13,
    RemoteCrush = 14,
    FinalCoreCheck = 15,
}

internal enum FirstSeveranceMechanicKind : byte
{
    None = 0,
    PylonCheck = 1,
    Stack = 2,
    Spread = 3,
    CoreExposure = 4,
}

internal readonly record struct FirstSeveranceParticipantScaledInt(
    int TwoParticipants,
    int ThreeParticipants,
    int FourParticipants)
{
    public bool IsPositive => TwoParticipants > 0
        && ThreeParticipants > 0
        && FourParticipants > 0;

    public bool FitsParticipantCount => IsPositive
        && TwoParticipants <= 2
        && ThreeParticipants <= 3
        && FourParticipants <= 4;

    public int GetValue(int participantCount)
    {
        return participantCount switch
        {
            1 => TwoParticipants, // Keep the two-player mechanic workload for debug admission.
            2 => TwoParticipants,
            3 => ThreeParticipants,
            4 => FourParticipants,
            _ => throw new ArgumentOutOfRangeException(nameof(participantCount)),
        };
    }
}

internal sealed class FirstSeveranceTimingPlan
{
    public const int MaximumSubstateDurationTicks = 36_000;

    public FirstSeveranceTimingPlan(
        int spawnIntroTicks,
        int pylonTelegraphTicks,
        int pylonActiveTicks,
        int stackTelegraphTicks,
        int spreadTelegraphTicks,
        int normalExposureTicks,
        int penalizedExposureTicks,
        int resetTicks)
    {
        if (spawnIntroTicks <= 0
            || pylonTelegraphTicks <= 0
            || pylonActiveTicks <= 0
            || stackTelegraphTicks <= 0
            || spreadTelegraphTicks <= 0
            || normalExposureTicks <= 0
            || penalizedExposureTicks <= 0
            || resetTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(spawnIntroTicks),
                "Every First Severance duration must be positive.");
        }

        if (spawnIntroTicks > MaximumSubstateDurationTicks
            || pylonTelegraphTicks > MaximumSubstateDurationTicks
            || pylonActiveTicks > MaximumSubstateDurationTicks
            || stackTelegraphTicks > MaximumSubstateDurationTicks
            || spreadTelegraphTicks > MaximumSubstateDurationTicks
            || normalExposureTicks > MaximumSubstateDurationTicks
            || penalizedExposureTicks > MaximumSubstateDurationTicks
            || resetTicks > MaximumSubstateDurationTicks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(spawnIntroTicks),
                "Every First Severance duration must be bounded to ten minutes.");
        }

        if (penalizedExposureTicks >= normalExposureTicks)
        {
            throw new ArgumentException(
                "The penalized exposure must be shorter than the normal exposure.",
                nameof(penalizedExposureTicks));
        }

        SpawnIntroTicks = spawnIntroTicks;
        PylonTelegraphTicks = pylonTelegraphTicks;
        PylonActiveTicks = pylonActiveTicks;
        StackTelegraphTicks = stackTelegraphTicks;
        SpreadTelegraphTicks = spreadTelegraphTicks;
        NormalExposureTicks = normalExposureTicks;
        PenalizedExposureTicks = penalizedExposureTicks;
        ResetTicks = resetTicks;

        int pylonCheckTicks = checked(pylonTelegraphTicks + pylonActiveTicks);
        if (pylonCheckTicks > MaximumSubstateDurationTicks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pylonActiveTicks),
                "The complete Pylon check must be bounded to ten minutes.");
        }
    }

    public int SpawnIntroTicks { get; }

    public int PylonTelegraphTicks { get; }

    public int PylonActiveTicks { get; }

    public int PylonCheckTicks => checked(PylonTelegraphTicks + PylonActiveTicks);

    public int StackTelegraphTicks { get; }

    public int SpreadTelegraphTicks { get; }

    public int NormalExposureTicks { get; }

    public int PenalizedExposureTicks { get; }

    public int ResetTicks { get; }
}

internal readonly record struct FirstSeveranceStateEdge
{
    private FirstSeveranceStateEdge(
        FirstSeveranceSubstate nextSubstate,
        FirstSeveranceTerminalCause terminalCause)
    {
        NextSubstate = nextSubstate;
        TerminalCause = terminalCause;
    }

    public FirstSeveranceSubstate NextSubstate { get; }

    public FirstSeveranceTerminalCause TerminalCause { get; }

    public bool IsTerminal => TerminalCause != FirstSeveranceTerminalCause.None;

    public static FirstSeveranceStateEdge TransitionTo(FirstSeveranceSubstate next)
    {
        if (next == FirstSeveranceSubstate.None || !Enum.IsDefined(next))
        {
            throw new ArgumentOutOfRangeException(nameof(next));
        }

        return new FirstSeveranceStateEdge(next, FirstSeveranceTerminalCause.None);
    }

    public static FirstSeveranceStateEdge End(FirstSeveranceTerminalCause cause)
    {
        if (!FirstSeveranceTerminationContract.IsFeatureOwned(cause))
        {
            throw new ArgumentOutOfRangeException(nameof(cause));
        }

        return new FirstSeveranceStateEdge(FirstSeveranceSubstate.None, cause);
    }
}

internal sealed class FirstSeveranceSubstateDefinition
{
    public FirstSeveranceSubstateDefinition(
        FirstSeveranceSubstate substate,
        FirstSeveranceMechanicKind mechanic,
        int durationTicks,
        FirstSeveranceStateEdge successEdge,
        FirstSeveranceStateEdge failureEdge)
    {
        if (substate == FirstSeveranceSubstate.None || !Enum.IsDefined(substate))
        {
            throw new ArgumentOutOfRangeException(nameof(substate));
        }

        if (!Enum.IsDefined(mechanic))
        {
            throw new ArgumentOutOfRangeException(nameof(mechanic));
        }

        if (durationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationTicks));
        }

        if (!successEdge.IsTerminal && successEdge.NextSubstate == FirstSeveranceSubstate.None)
        {
            throw new ArgumentException("A success edge requires a destination.", nameof(successEdge));
        }

        if (!failureEdge.IsTerminal && failureEdge.NextSubstate == FirstSeveranceSubstate.None)
        {
            throw new ArgumentException("A failure edge requires a destination.", nameof(failureEdge));
        }

        Substate = substate;
        Mechanic = mechanic;
        DurationTicks = durationTicks;
        SuccessEdge = successEdge;
        FailureEdge = failureEdge;
    }

    public FirstSeveranceSubstate Substate { get; }

    public FirstSeveranceMechanicKind Mechanic { get; }

    public int DurationTicks { get; }

    public FirstSeveranceStateEdge SuccessEdge { get; }

    public FirstSeveranceStateEdge FailureEdge { get; }
}
