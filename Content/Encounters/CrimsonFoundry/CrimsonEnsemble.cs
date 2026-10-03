using System;
using System.Collections.Generic;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// One accepted phase clock drives gates, bindings, native roots and presentation.
// Transitions are whole bars: an Act change is the old music's last bar plus the
// stop (ActRelease lands on its downbeat); Final adds a two-bar riser and the
// pre-drop (FinalRelease) before the climax unlocks combat.
internal static class CrimsonEnsemble
{
    internal const int GateOpen = 42, ActRelease = 113, FinalRelease = 450, SacrificeComplete = 420, FinalTransition = 562;
    internal const int SacrificeStart = 280;
    internal const int BodyWidth = 420, BodyHeight = 360;
    internal static CrimsonPoint Binding(RaidFieldGeometry field, int index)
    {
        if (index is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(index));
        return new CrimsonPoint(field.CenterX, field.CenterY) + CrimsonPoint.Polar(-MathF.PI / 2 + index * MathF.Tau / 3, 360);
    }
    internal static int Transition(int phase) => phase == 3 ? FinalTransition : CrimsonPhaseRules.TransitionTicks;
    internal static float Emergence(float elapsed, bool final) => CrimsonInvocation.Ease((elapsed - (final ? FinalRelease : ActRelease)) / (final ? 90 : 80));
    internal static float Absorption(float elapsed) => CrimsonInvocation.Ease((elapsed - SacrificeStart) / (SacrificeComplete - SacrificeStart));
    internal static float RetreatDissolve(float elapsed) => CrimsonInvocation.Ease((elapsed - 50) / 62);
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
        if (CrimsonChoreography.IsCrossflow(note)) return CrimsonTechnique.ClusterVolley;
        var pair = Pair(phrase);
        return second ? pair.Second : pair.First;
    }
    // The notes of a phrase in issue order: every hit, then in Final each ordinary hit again as the second family
    // (plan Pulse + PairOffset). Pickups and closers stay single.
    internal static List<(CrimsonRhythmHit Hit, bool Second)> Notes(CrimsonRhythmPhrase rhythm, int phase)
    {
        var notes = new List<(CrimsonRhythmHit Hit, bool Second)>(rhythm.Hits.Count * 2);
        foreach (var hit in rhythm.Hits) notes.Add((hit, false));
        if (phase == CrimsonPhaseRules.FinalPhase)
            foreach (var hit in rhythm.Hits) if (hit.Pulse < CrimsonChoreography.BasicNotes) notes.Add((hit, true));
        return notes;
    }
    internal static byte PlanPulse(CrimsonRhythmHit hit, bool second) => (byte)(second ? hit.Pulse + CrimsonChoreography.PairOffset : hit.Pulse);
    internal static int NoteEnd(CrimsonTechnique technique, CrimsonRhythmHit hit) => technique switch
    {
        CrimsonTechnique.ClusterVolley => hit.Fire + CrimsonClusters.FlightTicks,
        CrimsonTechnique.SpatialRift or CrimsonTechnique.SpatialGrid or CrimsonTechnique.ChoirRakes => hit.Fire + CrimsonSpatialCuts.LiveTicks,
        CrimsonTechnique.CinderCurtain or CrimsonTechnique.ShroudRope or CrimsonTechnique.FourHands
            => hit.Fire + CrimsonSignatureMoves.LiveTicks(technique),
        _ => hit.End
    };
}
