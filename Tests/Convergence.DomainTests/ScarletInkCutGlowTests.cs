using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

// The seal crossflow's glow at its flat cuts (presentation only): ScarletInkStroke.Band pushes the position the shader is asked about
// away from the stream's axis beyond the body, growing toward each cut, so the glow does not end in a straight vertical step above the
// seal's apex. The model lives in ScarletInkCutGlow; ScarletRigGates G11 measures the rendered pixels (cutGlowMax).
internal static partial class Program
{
    [DomainTest("Scarlet ink cut glow: the pinch is smooth, 1 away from the cuts and the identity over the body, so the body and the middle of the stream are untouched")]
    private static void ScarletInkCutGlowIsSmoothAndLeavesTheBodyAlone()
    {
        AssertEqual(ScarletInkCutGlow.Pinch, ScarletInkCutGlow.Factor(0), "the full pinch on the cut");
        AssertEqual(1f, ScarletInkCutGlow.Factor(ScarletInkCutGlow.Reach), "none from the reach on");
        AssertEqual(1f, ScarletInkCutGlow.Factor(ScarletInkCutGlow.Reach * 3), "and none in the middle of the stream");
        float previous = float.MaxValue;
        for (float d = 0; d <= ScarletInkCutGlow.Reach + 1; d += .5f)
        {
            float f = ScarletInkCutGlow.Factor(d);
            AssertEqual(true, f <= previous + 1e-6f && f >= 1, $"the factor falls monotonically toward 1 ({d})");
            // Smooth: the grid of ScarletInkStroke.Band moves it by at most a few per cent between neighbouring columns.
            AssertEqual(true, MathF.Abs(f - ScarletInkCutGlow.Factor(d + ScarletInkCutGlow.Reach / 20)) <= (ScarletInkCutGlow.Pinch - 1) * .16f, $"neighbouring columns differ little ({d})");
            previous = f;
        }
        foreach (float solid in new[] { 20f, 150f, 170f })
            foreach (float factor in new[] { 1f, 2f, ScarletInkCutGlow.Pinch })
            {
                float last = 0;
                for (float a = 0; a <= solid + 120; a += .25f)
                {
                    float asked = ScarletInkCutGlow.Sampled(a, solid, factor);
                    if (a <= solid) AssertEqual(a, asked, $"1:1 over the body (solid {solid}, across {a})");
                    AssertEqual(true, asked >= last, $"the sampled distance never runs backward ({a})");
                    if (factor == 1) AssertEqual(true, MathF.Abs(asked - a) < 1e-3f, "without the pinch it is the identity");
                    // The closed form is the integral of Slope (finite difference), and the slope starts at 1 and ends at the factor.
                    if (a > 0.25f) AssertEqual(true, MathF.Abs((asked - last) / .25f - ScarletInkCutGlow.Slope(a - .125f, solid, factor)) < .02f, $"Sampled integrates Slope ({a})");
                    last = asked;
                }
                AssertEqual(1f, ScarletInkCutGlow.Slope(solid, solid, factor), "the slope leaves the body at 1 (no crease)");
                AssertEqual(factor, ScarletInkCutGlow.Slope(solid + ScarletInkCutGlow.Ramp, solid, factor), "and reaches the factor after the ramp");
            }
    }

    [DomainTest("Scarlet ink cut glow: the body, including the opening overshoot, is never pinched; the glow above a seal's apex is")]
    private static void ScarletInkCutGlowPinchesOnlyTheGlow()
    {
        AssertEqual(true, MathF.Abs(ScarletInkCutGlow.Overshoot(3.2f) - ScarletInkMargin.PeakRadius) < 1e-4f, "the shader's overshoot peaks at the margin rule's 1.14");
        AssertEqual(true, ScarletInkCutGlow.Overshoot(60) < 1.0001f, "and is gone after the opening");
        for (float radius = .5f; radius <= 250; radius += .5f)
            for (float t = 0; t <= 60; t += .25f)
            {
                float solid = ScarletInkCutGlow.Solid(radius, t);
                AssertEqual(true, solid >= radius + ScarletInkMargin.Floor, $"at least the original margin (R={radius})");
                // ScarletInk.fx Live: the body feathers out to 1.04 R + aa, R <= r * open * over.
                AssertEqual(true, solid >= 1.04f * radius * ScarletInkCutGlow.Overshoot(t) + 1, $"the whole feathered body is inside the solid (R={radius}, t={t})");
            }
        // The crossflow at rest: the 140 px band's glow above the seals' apex (165 px from the axis, ScarletRigGates.SealTop 170 px).
        float band = CrimsonChoreography.SideHalfWidth, extent = band + ScarletInkMargin.Of(band);
        AssertEqual(150f, ScarletInkCutGlow.Solid(band, 99), "the crossflow's solid is the band and the original 10 px");
        float unpinched = ScarletInkMargin.Halo(170, band, 1);
        AssertEqual(true, unpinched > .3f, $"unpinched, {unpinched:P0} of the halo stands in a straight vertical line above the seals' apex");
        float lastGlow = 1;
        for (float across = 170; across <= extent; across += 1)
        {
            float glow = ScarletInkCutGlow.GlowAtCut(band, across);
            AssertEqual(true, glow <= .12f, $"pinched, at most 12% of the halo is left at the cut above the apex ({across}: {glow:P1})");
            AssertEqual(true, glow <= lastGlow + 1e-6f, $"and it keeps falling ({across})");
            lastGlow = glow;
        }
        // The quad's edge across the band is dimmer at the cut than the margin rule leaves it elsewhere.
        AssertEqual(true, ScarletInkCutGlow.GlowAtCut(band, extent) <= ScarletInkMargin.HaloAtEdge(band, 1, 1) + 1e-6f, "the quad's edge at the cut is no brighter than the margin rule's");
        // Over the body nothing moves at the cut either.
        AssertEqual(band, ScarletInkCutGlow.Sampled(band, ScarletInkCutGlow.Solid(band, 99), ScarletInkCutGlow.Pinch), "the stream's own edge is where it was");
    }
}
