using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// One accepted phase clock drives gates, bindings, native roots and presentation.
internal static class CrimsonEnsemble
{
    internal const int GateOpen = 42, ActRelease = 132, FinalRelease = 420, SacrificeComplete = 390, FinalTransition = 540;
    internal const int SacrificeStart = 270;
    internal const int BodyWidth = 420, BodyHeight = 360;
    internal static CrimsonPoint Binding(RaidFieldGeometry field, int index)
    {
        if (index is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(index));
        return new CrimsonPoint(field.CenterX, field.CenterY) + CrimsonPoint.Polar(-MathF.PI / 2 + index * MathF.Tau / 3, 360);
    }
    internal static int Transition(int phase) => phase == 3 ? FinalTransition : CrimsonPhaseRules.TransitionTicks;
    internal static float Emergence(float elapsed, bool final) => CrimsonInvocation.Ease((elapsed - (final ? FinalRelease : ActRelease)) / (final ? 90 : 80));
    internal static float Absorption(float elapsed) => CrimsonInvocation.Ease((elapsed - SacrificeStart) / (SacrificeComplete - SacrificeStart));
    internal static float RetreatDissolve(float elapsed) => CrimsonInvocation.Ease((elapsed - 62) / 48);
    internal static float ConductorAbsorption(float elapsed) => CrimsonInvocation.Ease((elapsed - 65) / 90);
    internal static float ChildReveal(float elapsed, int index) => CrimsonInvocation.Ease((elapsed - 18 - index * 16) / 56);
    internal static float BloodPressure(float elapsed) => CrimsonInvocation.Ease((elapsed - 155) / 100)
        * (1 - CrimsonInvocation.Ease((elapsed - SacrificeComplete) / 32));
    internal static float VictoryMelt(float elapsed) => CrimsonInvocation.Ease((elapsed - 22) / 112);
    internal static (CrimsonTechnique First, CrimsonTechnique Second) Pair(int phrase) => (phrase % 3) switch
    {
        0 => (CrimsonTechnique.TrackingBeam, CrimsonTechnique.SpatialRift),
        1 => (CrimsonTechnique.SpatialRift, CrimsonTechnique.SpatialGrid),
        _ => (CrimsonTechnique.SpatialGrid, CrimsonTechnique.TrackingBeam)
    };
    internal static CrimsonTechnique Technique(int phase, int phrase, int note, bool second)
    {
        if (phase != 3) return CrimsonChoreography.Technique(phase, phrase, note);
        if (note == CrimsonChoreography.BasicNotes) return CrimsonTechnique.ClusterVolley;
        var pair = Pair(phrase);
        return second ? pair.Second : pair.First;
    }
    internal static int NoteEnd(CrimsonTechnique technique, CrimsonRhythmHit hit) => technique switch
    {
        CrimsonTechnique.ClusterVolley => hit.Fire + CrimsonClusters.FlightTicks,
        CrimsonTechnique.SpatialRift or CrimsonTechnique.SpatialGrid => hit.Fire + CrimsonSpatialCuts.LiveTicks,
        _ => hit.End
    };
}
