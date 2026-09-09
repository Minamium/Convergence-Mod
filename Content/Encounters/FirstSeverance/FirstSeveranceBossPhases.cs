using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceBossPhase : byte { Sealed = 1, Unbound = 2, Distant = 3, Final = 4 }

internal readonly record struct FirstSeveranceBossPhaseDefinition(
    FirstSeveranceBossPhase Id, int EntryLifePermille, int TransitionTicks,
    FirstSeveranceSubstate ActiveState, int WindowTicks);

// Ordered feature-local stages. Future stages extend this plan and their own
// attack adapter, not the global coordinator, packet router or revive service.
internal sealed class FirstSeveranceBossPhasePlan
{
    internal const int RuptureTicks = 360;
    internal const int GridWindowTicks = 1080;
    internal static FirstSeveranceBossPhasePlan Instance { get; } = new();
    internal IReadOnlyList<FirstSeveranceBossPhaseDefinition> Stages { get; } = Array.AsReadOnly(new[]
    {
        new FirstSeveranceBossPhaseDefinition(FirstSeveranceBossPhase.Sealed, 1000, 0, FirstSeveranceSubstate.CoreExposure, 1080),
        new FirstSeveranceBossPhaseDefinition(FirstSeveranceBossPhase.Unbound, 800, RuptureTicks, FirstSeveranceSubstate.Lattice, GridWindowTicks),
        new FirstSeveranceBossPhaseDefinition(FirstSeveranceBossPhase.Distant, 400, 300, FirstSeveranceSubstate.RemoteClaws, 600),
        new FirstSeveranceBossPhaseDefinition(FirstSeveranceBossPhase.Final, 0, 240, FirstSeveranceSubstate.Stack, 180),
    });

    internal bool TryGetNext(FirstSeveranceBossPhase current, out FirstSeveranceBossPhaseDefinition next)
    {
        for (int index = 0; index + 1 < Stages.Count; index++)
            if (Stages[index].Id == current) { next = Stages[index + 1]; return true; }
        next = default;
        return false;
    }

    internal FirstSeveranceBossPhaseDefinition Get(FirstSeveranceBossPhase id)
    {
        foreach (var stage in Stages) if (stage.Id == id) return stage;
        throw new ArgumentOutOfRangeException(nameof(id));
    }

    internal static int LifeThreshold(int maximumLife, in FirstSeveranceBossPhaseDefinition stage)
        => Math.Max(0, (int)((long)maximumLife * stage.EntryLifePermille / 1000));

    internal static bool IsDamageState(FirstSeveranceSubstate state)
        => state is FirstSeveranceSubstate.CoreExposure or FirstSeveranceSubstate.Lattice
            or FirstSeveranceSubstate.RotatingBlade or FirstSeveranceSubstate.RemoteClaws or FirstSeveranceSubstate.HalfField
            or FirstSeveranceSubstate.RemoteCrush;
}

internal static class FirstSeveranceStackAnchor
{
    internal const float HeightAboveGround = 880f;
    internal static (float X, float Y) FromGround(float x, float groundY) => (x, groundY - HeightAboveGround);
    internal static bool Contains(float x, float y, float centerX, float centerY)
        => (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY)
            <= FirstSeveranceLanceTuning.StackRadius * FirstSeveranceLanceTuning.StackRadius;
}
