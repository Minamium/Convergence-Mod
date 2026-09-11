using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeveranceAction(FirstSeveranceSubstate State, int Ticks, int ClockPoint = -1);

// Feature-local score. Sealed's score follows its FIRST full Core exposure.
// Adding an intermediate phase only changes the phase plan and its score here;
// Final remains an explicit HP-zero survival stage, never a normal death hook.
internal static class FirstSeveranceChoreography
{
    internal const float ClockRadiusX = 560, ClockRadiusY = 300;
    internal const int StackTicks = 150, SpreadTicks = 180;
    internal const int BladeWindup = 180, BladeSpin = 300, BladeRecovery = 60;
    internal const int BladeTurns = 2;
    internal const int BladeEnd = BladeWindup + BladeSpin;
    internal static readonly IReadOnlyList<FirstSeveranceAction> Sealed = Clock(4, false);
    internal static readonly IReadOnlyList<FirstSeveranceAction> Unbound = Array.AsReadOnly(new[]
    {
        new FirstSeveranceAction(FirstSeveranceSubstate.Lattice, 450),
        new FirstSeveranceAction(FirstSeveranceSubstate.Spread, SpreadTicks),
        new FirstSeveranceAction(FirstSeveranceSubstate.RotatingBlade, BladeWindup + BladeSpin + BladeRecovery),
        new FirstSeveranceAction(FirstSeveranceSubstate.Spread, SpreadTicks),
        new FirstSeveranceAction(FirstSeveranceSubstate.Lattice, 450),
        new FirstSeveranceAction(FirstSeveranceSubstate.Spread, SpreadTicks),
    });
    internal static readonly IReadOnlyList<FirstSeveranceAction> Distant = Array.AsReadOnly(new[]
    {
        new FirstSeveranceAction(FirstSeveranceSubstate.RemoteClaws, 600),
        new FirstSeveranceAction(FirstSeveranceSubstate.HalfField, 300),
        new FirstSeveranceAction(FirstSeveranceSubstate.Stack, 180, 0),
        new FirstSeveranceAction(FirstSeveranceSubstate.Spread, SpreadTicks),
        new FirstSeveranceAction(FirstSeveranceSubstate.HalfField, 300),
        new FirstSeveranceAction(FirstSeveranceSubstate.RemoteClaws, 600),
        new FirstSeveranceAction(FirstSeveranceSubstate.Stack, StackTicks, 2),
        new FirstSeveranceAction(FirstSeveranceSubstate.Spread, SpreadTicks),
        new FirstSeveranceAction(FirstSeveranceSubstate.RemoteCrush, 300),
    });
    internal static readonly IReadOnlyList<FirstSeveranceAction> Final = Clock(8, true);

    internal static IReadOnlyList<FirstSeveranceAction> For(FirstSeveranceBossPhase phase) => phase switch
    {
        FirstSeveranceBossPhase.Sealed => Sealed,
        FirstSeveranceBossPhase.Unbound => Unbound,
        FirstSeveranceBossPhase.Distant => Distant,
        FirstSeveranceBossPhase.Final => Final,
        _ => throw new ArgumentOutOfRangeException(nameof(phase)),
    };

    private static IReadOnlyList<FirstSeveranceAction> Clock(int points, bool final)
    {
        var actions = new List<FirstSeveranceAction>();
        for (int i = 0; i < points; i++)
        {
            actions.Add(new(FirstSeveranceSubstate.Stack, i == 0 ? 180 : StackTicks - (final ? i * 3 : 0), i));
            actions.Add(new(FirstSeveranceSubstate.Spread, SpreadTicks - (final ? i * 6 : 0)));
            if (final) actions.Add(new(i % 2 == 0 ? FirstSeveranceSubstate.FinalBullets : FirstSeveranceSubstate.FinalSlicer,
                FinalHazardTicks(i * 3 + 2)));
        }
        return actions.AsReadOnly();
    }

    internal static float FinalProgress(int step) => Math.Clamp(step / 3, 0, 7) / 7f;
    internal static int FinalHazardTicks(int step) => step / 3 % 2 == 1
        ? FirstSeveranceScoreGeometry.SlicerEnd(step) + (FirstSeveranceScoreGeometry.SlicerPulses - 1)
            * FirstSeveranceScoreGeometry.SlicerCadence(step) + 24
        : 240 - Math.Clamp(step / 3, 0, 7) * 8;

    internal static (float X, float Y) StackPosition(float groundX, float groundY, FirstSeveranceBossPhase phase, int step)
    {
        if (step < 0) return FirstSeveranceStackAnchor.FromGround(groundX, groundY);
        var score = For(phase);
        int point = score[step].ClockPoint;
        // During other actions retain the preceding marker for result effects.
        for (int i = step; point < 0 && i >= 0; i--) point = score[i].ClockPoint;
        if (point < 0) point = 0;
        float angle = -MathF.PI * .5f + point * MathF.Tau / (phase == FirstSeveranceBossPhase.Final ? 8 : 4);
        return (groundX + MathF.Cos(angle) * ClockRadiusX,
            groundY - FirstSeveranceLanceTuning.BossHeightAboveCore + MathF.Sin(angle) * ClockRadiusY);
    }

    internal static bool IsValidStep(FirstSeveranceBossPhase phase, FirstSeveranceSubstate state, int step)
        => step == -1 ? (phase == FirstSeveranceBossPhase.Sealed && state is >= FirstSeveranceSubstate.SpawnIntro and <= FirstSeveranceSubstate.Reset)
            || (phase != FirstSeveranceBossPhase.Sealed && state == FirstSeveranceSubstate.PhaseTransition)
            : step >= 0 && step < For(phase).Count && For(phase)[step].State == state;
}
