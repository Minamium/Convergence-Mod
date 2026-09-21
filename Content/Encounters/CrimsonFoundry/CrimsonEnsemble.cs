using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// One accepted phase clock drives gates, bindings, native roots and presentation.
internal static class CrimsonEnsemble
{
    internal const int GateOpen = 42, ActRelease = 74, FinalRelease = 120, SacrificeComplete = 192, FinalTransition = 240;
    internal const int BodyWidth = 420, BodyHeight = 360;
    internal static CrimsonPoint Binding(RaidFieldGeometry field, int index)
    {
        if (index is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(index));
        return new CrimsonPoint(field.CenterX, field.CenterY) + CrimsonPoint.Polar(-MathF.PI / 2 + index * MathF.Tau / 3, 360);
    }
    internal static int Transition(int phase) => phase == 3 ? FinalTransition : CrimsonPhaseRules.TransitionTicks;
    internal static float Emergence(float elapsed, bool final) => CrimsonInvocation.Ease((elapsed - (final ? FinalRelease : ActRelease)) / (final ? 72 : 52));
    internal static float Absorption(float elapsed) => CrimsonInvocation.Ease((elapsed - FinalRelease) / (SacrificeComplete - FinalRelease));
    internal static (CrimsonTechnique First, CrimsonTechnique Second) Pair(int phrase) => (phrase % 3) switch
    {
        0 => (CrimsonTechnique.TrackingBeam, CrimsonTechnique.SpatialRift),
        1 => (CrimsonTechnique.SpatialRift, CrimsonTechnique.SpatialGrid),
        _ => (CrimsonTechnique.SpatialGrid, CrimsonTechnique.TrackingBeam)
    };
    internal static CrimsonTechnique Technique(int phase, int phrase, int note, bool second)
    {
        if (phase != 3) return CrimsonChoreography.Technique(phase, phrase, note);
        if (note == CrimsonChoreography.BasicNotes) return CrimsonTechnique.SideBeams;
        var pair = Pair(phrase);
        return second ? pair.Second : pair.First;
    }
}
