using System;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Doll core rupture starts at claw contact then cracks before the burst")]
    private static void DollCoreRuptureContact()
    {
        int impact = FirstSeveranceScoreGeometry.CrushImpactTick;
        FirstSeveranceCoreRupture Pose(double age) => FirstSeveranceCoreRupture.At(
            FirstSeveranceBossPhase.Distant, FirstSeveranceSubstate.RemoteCrush, 0, age);
        AssertEqual(false, Pose(impact - .001).Struck, "no pre-contact fracture");
        AssertEqual(true, Pose(impact).Struck, "same contact as authority geometry");
        var held = Pose(impact + 14);
        AssertEqual(1f, held.Cracks, "readable full cracks before release");
        AssertEqual(1f, held.MetalOpacity, "metal shell stays present during braked hold");
        AssertEqual(0f, held.Energy, "not a premature replacement flash");
        var burst = Pose(impact + FirstSeveranceCoreRupture.BurstDelay + 6);
        AssertEqual(true, burst.Energy > 0 && burst.MetalOpacity > 0 && burst.BurstAge > 0,
            "energy revealed beneath separating plates");
        AssertEqual(FirstSeveranceCoreRupture.Settled, Pose(300), "transformation complete before score advances");
    }

    [DomainTest("Doll core rupture persists across cycles Final and DPS check without a seen event")]
    private static void DollCoreRuptureSnapshots()
    {
        AssertEqual(FirstSeveranceSubstate.RemoteCrush, FirstSeveranceChoreography.Distant[^1].State,
            "cycle completion proves the first crush already finished");
        foreach (var action in FirstSeveranceChoreography.Distant)
            AssertEqual(FirstSeveranceCoreRupture.Settled, FirstSeveranceCoreRupture.At(
                FirstSeveranceBossPhase.Distant, action.State, 1, 0), "rejoined later cycle remains energy");
        foreach (var action in FirstSeveranceChoreography.Final)
            AssertEqual(FirstSeveranceCoreRupture.Settled, FirstSeveranceCoreRupture.At(
                FirstSeveranceBossPhase.Final, action.State, 0, 0), "Final including DPS check remains energy");
        AssertEqual(FirstSeveranceCoreRupture.Settled, FirstSeveranceCoreRupture.At(
            FirstSeveranceBossPhase.Final, FirstSeveranceSubstate.PhaseTransition, 0, 0), "transition does not revert");
        AssertEqual(default(FirstSeveranceCoreRupture), FirstSeveranceCoreRupture.At(
            FirstSeveranceBossPhase.Sealed, FirstSeveranceSubstate.SpawnIntro, 0, 30), "new fight is intact");
        AssertEqual(default(FirstSeveranceCoreRupture), FirstSeveranceCoreRupture.At(
            FirstSeveranceBossPhase.Unbound, FirstSeveranceSubstate.Lattice, 2, 200), "other phase cycles never rupture core");
    }

    [DomainTest("Doll core rupture fractional material envelopes stay bounded and connected")]
    private static void DollCoreRuptureCurves()
    {
        float lastEnergy = 0, lastMetal = 1;
        for (int i = 0; i <= 1200; i++)
        {
            var p = new FirstSeveranceCoreRupture(true, i / 10f);
            AssertEqual(true, p.Energy >= lastEnergy && p.MetalOpacity <= lastMetal,
                "no late metallic reconstitution");
            AssertEqual(true, p.Energy - lastEnergy < .014f && lastMetal - p.MetalOpacity < .004f,
                "fractional sampling has no replacement pop");
            AssertEqual(true, p.Energy is >= 0 and <= 1 && p.MetalOpacity is >= 0 and <= 1 &&
                p.Cracks is >= 0 and <= 1 && float.IsFinite(p.Flash), "finite bounded opacity");
            lastEnergy = p.Energy; lastMetal = p.MetalOpacity;
        }
    }
}
