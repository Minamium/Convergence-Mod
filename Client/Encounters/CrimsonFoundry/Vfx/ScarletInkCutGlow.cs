#nullable enable
using System;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// How the seal crossflow's glow meets its flat cuts. ScarletInkStroke draws the stream as a band that stops exactly on the two
// stream ends (the seal centres, or the wall), so the glow ScarletInk.fx keeps round the body (ScarletInkMargin: up to 93 px beyond
// the 140 px half-width) is cut there by a vertical line. The seal's ring only reaches about 165 px above the stream's axis, so over
// the last ~65 px of the glow that line stood in the open as a straight vertical step (17-22/255 on the night ground, reviewer's
// frames of 2026-10-04, measured from the ink-only frames by G11 as `cutGlowMax`).
//
// The glow cannot be shortened in the shader (ScarletInk.fx is the approved material and is not touched), but the quad's texture
// coordinates are ours: across the band, beyond the solid (the body and the original 10 px), the position the shader is asked about
// is pushed away from the axis in proportion to how near the cut is, so the glow fades over a fraction of the distance there
// and has under 12% of its peak left above the seal's apex. The stream's own body (|across| <= Solid) is drawn 1:1, the quad's outline and the cut lines
// do not move, and what changes is only the soft light outside the body, narrowing as it nears each seal.
//
//   sampled(a, f) = a                                a <= Solid
//                 = Solid + the integral of Slope    a > Solid        (the slope rises from 1 to f = Factor(distance from the nearest cut) >= 1
//                                                                     over Ramp px, so the falloff has no crease where the body ends)
//
// XNA-free so the domain tests link this file; tools/tests/test_scarlet_contracts.py pins the shader terms it models as text.
internal static class ScarletInkCutGlow
{
    // The distance from a cut over which the glow narrows, and the factor at the cut itself (the glow is sampled this many times
    // further out per pixel there, so it falls off over 1/Pinch of its usual distance).
    internal const float Reach = 160;
    internal const float Pinch = 4;

    // The shader's body feathers out to 1.04 R (+ aa), R <= r * open * over, over = 1 + .14 exp2(-((t - 3.2) / 2.4)^2) (ScarletInk.fx, Live).
    private const float FeatherScale = 1.04f;

    // The factor `distance` px inside the nearest cut: Pinch at the cut, 1 from Reach on, smooth between.
    internal static float Factor(float distance)
    {
        float t = Math.Clamp(distance / Reach, 0, 1);
        return 1 + (Pinch - 1) * (1 - t * t * (3 - 2 * t));
    }

    // The shader's opening overshoot of the body radius at `ticks` after the strike (ScarletInk.fx Live: `over`).
    internal static float Overshoot(float ticks)
    {
        float x = (ticks - 3.2f) / 2.4f;
        return 1 + .14f * MathF.Pow(2, -x * x);
    }

    // The distance from the stream's axis up to which the picture is drawn 1:1: the original 10 px margin, and the whole body
    // (its feathered edge, 1.04 R + 2 px of anti-aliasing) while it snaps open with the overshoot, so the body is never narrowed.
    internal static float Solid(float radius, float ticks)
        => MathF.Max(radius + ScarletInkMargin.Floor, FeatherScale * radius * Overshoot(ticks) + 2);

    // Over its first Ramp px past the solid the pinch builds up smoothly (the slope goes from 1 to the factor with zero slope at both
    // ends, a smoothstep), so the glow's falloff has no crease where the body ends.
    internal const float Ramp = 10;

    // The slope of Sampled at `across`: 1 up to the solid, then the factor, reached smoothly over Ramp.
    internal static float Slope(float across, float solid, float factor)
    {
        float u = Math.Clamp((across - solid) / Ramp, 0, 1);
        return 1 + (factor - 1) * u * u * (3 - 2 * u);
    }

    // The shader-local distance across the band to ask for at screen distance `across` from the axis, with the cut factor f:
    // the integral of Slope, a = across - solid, solid + a + (f - 1) Ramp G(a / Ramp), G(u) = u^3 - u^4 / 2 below 1 and u - 1/2 above.
    internal static float Sampled(float across, float solid, float factor)
    {
        if (across <= solid) return across;
        float a = across - solid, u = a / Ramp;
        float g = u >= 1 ? u - .5f : u * u * u - u * u * u * u * .5f;
        return solid + a + (factor - 1) * Ramp * g;
    }

    // The share of the glow's peak that is left at screen distance `across` at a cut, for a stroke of this radius at rest
    // (shader radius R = r, one screen pixel per local pixel before the pinch): the model the domain test pins.
    internal static float GlowAtCut(float radius, float across, float ticks = 99)
    {
        float solid = Solid(radius, ticks), asked = Sampled(across, solid, Pinch);
        // Anti-aliasing width grows with the pinch: ddy(p.y) is the slope.
        float aa = Slope(across, solid, Pinch);
        return ScarletInkMargin.Halo(asked, radius * Overshoot(ticks), aa);
    }
}
