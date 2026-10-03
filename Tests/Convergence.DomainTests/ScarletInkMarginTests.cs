using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

// ScarletInkStroke's quad margin (presentation only): the live pass's glow must have faded before the quad's edge, or the edge shows
// as a hard-edged lighter box (the 10 px margin did, 75% of the halo still left at the crossflow's edge). The model of the shader's halo
// term lives in ScarletInkMargin.Halo; tools/tests/test_scarlet_contracts.py pins that term in ScarletInk.fx as text.
internal static partial class Program
{
    [DomainTest("Scarlet ink quad margin lets the halo fade out before the quad's edge for every stroke radius, at zoom 1, zoom 2 and on a rotated quad")]
    private static void ScarletInkMarginFadesTheHalo()
    {
        float previous = 0;
        for (float radius = .5f; radius <= 250; radius += .5f)
        {
            float margin = ScarletInkMargin.Of(radius);
            AssertEqual(true, margin >= ScarletInkMargin.Floor && margin >= previous, $"the margin never shrinks as the radius grows (R={radius})");
            AssertEqual(true, MathF.Abs(margin - MathF.Max(10, MathF.Ceiling(.6f * radius + 9))) < .001f, $"0.6 r + 9, at least 10 (R={radius})");
            previous = margin;
            // One screen pixel per local pixel (zoom 1, axis-aligned quad), at the opening overshoot's peak (the shader's radius 1.14 r): the halo
            // keeps at most its limit at the edge; afterwards (radius r) it keeps far less.
            float edge = ScarletInkMargin.HaloAtEdge(radius, 1);
            AssertEqual(true, ScarletInkMargin.HaloAtEdge(radius, 1, 1) <= .03f, $"and in the steady state under 3% (R={radius})");
            AssertEqual(true, edge <= ScarletInkMargin.HaloAtEdgeLimit, $"the halo is cut at {edge:P1} of its peak at the overshoot, R={radius}, limit {ScarletInkMargin.HaloAtEdgeLimit:P0}");
            // Zoom 2 (aa = the 0.6 floor) fades further; a quad rotated 45 degrees at zoom 1 (aa = 1.41) keeps a bounded share for a body of any size.
            AssertEqual(true, ScarletInkMargin.HaloAtEdge(radius, .6f) <= edge + .001f, $"zoom 2 fades at least as far (R={radius})");
            if (radius >= 10) AssertEqual(true, ScarletInkMargin.HaloAtEdge(radius, 1.41f) <= .13f, $"a rotated quad keeps under 13% (R={radius})");
            if (radius >= 30) AssertEqual(true, ScarletInkMargin.HaloAtEdge(radius, 1.41f) <= .09f, $"and under 9% once the glow is wide (R={radius})");
        }
    }

    [DomainTest("Scarlet ink quad margin for the real strokes: the crossflow's 140 px band, the tracking beam and the signature moves; a flat 10 px would clip the halo")]
    private static void ScarletInkMarginOfTheRealStrokes()
    {
        float band = CrimsonChoreography.SideHalfWidth;
        AssertEqual(93f, ScarletInkMargin.Of(band), "the crossflow band (R=140) reaches 93 px beyond its radius");
        AssertEqual(31f, ScarletInkMargin.Of(CrimsonTrackingBeam.Radius), "the tracking beam (R=36) reaches 31 px beyond its radius");
        AssertEqual(31f, ScarletInkMargin.Of(CrimsonSignatureMoves.RopeRadius), "the Shroud rope (R=36) likewise");
        AssertEqual(45f, ScarletInkMargin.Of(CrimsonSignatureMoves.ClawRadius), "a Thorn claw (R=60) reaches 45 px");
        // The original margin: a hard edge, the detector for this test has teeth.
        float flat = ScarletInkMargin.Halo(band + 10, band, 1);
        AssertEqual(true, flat > .7f, $"a flat 10 px margin leaves {flat:P0} of the halo at the crossflow's edge");
        AssertEqual(true, ScarletInkMargin.Halo(CrimsonTrackingBeam.Radius + 10, CrimsonTrackingBeam.Radius, 1) > .4f, "and cuts the tracking beam's halo too");
        // The opening of the stream (F+1..F+10: half-width 3.9, 14.6, 30.2, 70, 109.8, 140), where the shader's body overshoots by 14%.
        foreach (float radius in new[] { 3.9f, 14.6f, 30.2f, 70f, 109.8f, 140f })
            AssertEqual(true, ScarletInkMargin.HaloAtEdge(radius, 1) <= ScarletInkMargin.HaloAtEdgeLimit, $"the opening stream's halo fades before the edge (R={radius})");
        // The quad only grows: the rule is never smaller than the original 10 px.
        AssertEqual(10f, ScarletInkMargin.Of(.5f), "thin strokes keep the original margin");
    }
}
