#nullable enable
using System;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// How far ScarletInkStroke's quad reaches beyond a stroke's radius. ScarletInk.fx's live pass keeps a soft glow outside the
// body, and the quad is the only thing that cuts it. The halo term (ScarletInk.fx, Live)
//
//     halo = exp2(-pow(max(0, d - .9R) / (.24R + 4aa), 2))
//
// is still about 75% at d = R + 10 for the crossflow's R = 140, so the original flat 10 px margin showed the quad's edge as a
// hard-edged lighter box 10 px above and below the band (owner-side review, 2026-10-04). The margin now grows with the radius,
// 0.6 r + 9 px (at least the original 10), where r is the stroke's capsule radius:
//   - The shader's own body radius R is not r. It snaps open with an overshoot, R = r * saturate(t/3) * (1 + .14 exp2(-((t - 3.2)/2.4)^2))
//     * (.9 + .1 rimN), at most 1.14 r (PeakRadius), in the first ticks after impact, when the whole stroke blazes. The rule is set for that peak.
//   - At R = 1.14 r, one screen pixel per local pixel (aa = 1) the halo is at most 4.6% of its peak at the quad's edge for every radius (1.3% or less
//     in the steady state), at zoom 2 (aa = 0.6) at most 3.9%, and for R >= 30 on a quad rotated 45 degrees (aa = 1.41) at most 8.3%.
//     The lip (.86 R, scale .11 R + 2aa) has fallen to nothing there.
// Why not the 12% of 0.32 r + 7 that fits the halo at R = r: measured on the rendered night frames it left a straight step of about 3/255
// (up to 10/255 while the stream opens and closes, 22/255 at F+3 where the overshoot is), visible when the frame is amplified; and why not
// less: the shader's glow is what it is, a quad can only show it. ScarletInk.fx is not touched. The margin only widens the quad across and
// beyond a stroke: the body, the collision capsule, the flat cut's horizontal extent (ScarletInkStroke's band is cut exactly on its stream
// ends) and every other shader input are unchanged, and what the quad newly shows is the glow the shader already computes.
//
// XNA-free so the domain tests link this file and pin the rule against the shader's halo term; tools/tests/test_scarlet_contracts.py
// pins that term, the overshoot and this rule's constants in the sources as text.
internal static class ScarletInkMargin
{
    // The original margin, which every stroke keeps at least.
    internal const float Floor = 10;

    // The largest the shader's body radius gets relative to the capsule's radius (ScarletInk.fx Live: the opening overshoot).
    internal const float PeakRadius = 1.14f;

    // The share of its peak the halo may still have at the quad's edge (aa = 1, at the overshoot's peak; the rule gives 4.6% at most).
    internal const float HaloAtEdgeLimit = .05f;

    // Pixels the quad reaches beyond the capsule's radius for a stroke of this radius.
    internal static float Of(float radius) => MathF.Max(Floor, MathF.Ceiling(.6f * radius + 9));

    // ScarletInk.fx Live: the halo's falloff (1 at the body's rim, 0 far away) at distance d from the stroke's axis, for the
    // shader's radius R and the anti-aliasing width aa (local pixels per screen pixel, at least 0.6).
    internal static float Halo(float distance, float shaderRadius, float aa)
    {
        float x = MathF.Max(0, distance - .9f * shaderRadius) / (.24f * shaderRadius + 4 * aa);
        return MathF.Pow(2, -x * x);
    }

    // The same falloff at the quad's edge, d = radius + Of(radius), for a shader radius of `scale` x the capsule's (PeakRadius at the
    // blaze after impact, 1 afterwards): what the quad still cuts.
    internal static float HaloAtEdge(float radius, float aa = 1, float scale = PeakRadius) => Halo(radius + Of(radius), radius * scale, aa);
}
