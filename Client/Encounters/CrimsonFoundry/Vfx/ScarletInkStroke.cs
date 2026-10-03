#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// Scarlet's strike material (ScarletInk.fx) on the authoritative CrimsonStroke capsules: a
// river of black blood. Live: a black body whose rim burns and melts, red threads streaming
// along it, a twisting core, and a blaze in the first ticks after impact. Residue: the ink
// dries into a narrow scar. The body never leaves its capsule except the few pixels of
// anti-aliased rim; only the soft glow around it reaches out, as far as the quad does
// (ScarletInkMargin.Of: 0.6 r + 9 px, so the glow has faded to under 5% where the quad ends, even at the opening overshoot, and is not cut into a box).
// The seal crossflow's stream (SideBeams) is drawn as the
// band between its two ends, cut square at both (and at its growing front) like the original
// stream, because the capsule's round ends stuck out past the seals' narrow ellipses. The
// band covers the capsule's whole span and its half-width is the capsule's radius, so its body lies
// inside the forecast band (only the glow reaches past it, across the band, never past a cut end), and the capsule (the collision)
// is not touched; only the four corners the round ends cut off are inked without hurting. The seals are drawn over a live
// stream (CrimsonGestureVisuals.DrawTrackingBeams), so each flat cut, which falls inside its seal's
// ring, sinks into it; at a wall the held seal lies over the stream, which runs on under it to the wall.
//
// It owns only the live and residue time of a field beam (TrackingBeam, SideBeams) and of an
// Act signature move (CinderCurtain, ShroudRope, FourHands). The forecast (CrimsonEnergy's
// PortalForecastPass + ForecastDustPass + mouth) and the two seals that enclose the crossflow
// stay with their original renderers, so ScarletInk's ForecastPass is never used here. A
// signature move keeps its own residue length (CrimsonSignatureMoves.ResidueTicks) and, while
// ScarletResidueYield is enabled, its residue lies under the forecasts and dries away early
// wherever the move's next note leaves the ground safe.
internal sealed class ScarletInkStroke
{
    internal const int CloseTicks = 8, ResidueTicks = 24;
    private readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private readonly CrimsonStroke[] buffer = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
    private readonly CrimsonStroke[] successor = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];

    // Ember Crown burns, Sable Mantle is shroud, Thorn Choir is bone; Final mixes them.
    internal static Vector4 Flavor(int source) => source switch
    {
        0 => new(1, 0, 0, 0), 1 => new(0, 1, 0, 0), 2 => new(0, 0, 1, 0), _ => new(.5f, .3f, .3f, 1)
    };

    // The field beams (the thin tracking line and the stream between the crossflow seals) and the signature moves.
    internal static bool Applies(in CrimsonGesturePlan plan)
        => plan.Technique is CrimsonTechnique.TrackingBeam or CrimsonTechnique.SideBeams || plan.IsSignature;

    // Residue length after End: the signature move's own, otherwise the approved 24 ticks.
    internal static int ResidueTicksOf(in CrimsonGesturePlan plan)
        => plan.IsSignature ? CrimsonSignatureMoves.ResidueTicks(plan.Technique) : ResidueTicks;

    // Live strike and its residue; the warning before Fire belongs to the owner's original forecast.
    internal static bool Owns(in CrimsonGesturePlan plan, float age)
        => Applies(plan) && age >= plan.Fire && age < plan.End + ResidueTicksOf(plan);

    // A signature residue drawn under the forecasts (ScarletResidueYield); everything else Owns draws over them.
    internal static bool Underlies(in CrimsonGesturePlan plan, float age)
        => Owns(plan, age) && age >= plan.End && ScarletResidueYield.Applies(plan);

    // phrase: the boss's other plans, where a yielding signature residue finds its move's next note.
    internal void Draw(in ScarletView view, IScarletAssets assets, in CrimsonGesturePlan plan, ReadOnlySpan<CrimsonGesturePlan> phrase = default)
    {
        float age = view.Clock;
        if (!Owns(plan, age)) return;
        var shader = assets.GetShader("ScarletInk");
        var device = view.Device;
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        device.Textures[1] = assets.GetTexture("Noise/TurbulentNoise"); device.SamplerStates[1] = SamplerState.LinearWrap;
        device.Textures[2] = assets.GetTexture("Noise/WavyBlotchNoise"); device.SamplerStates[2] = SamplerState.LinearWrap;
        shader.Set("uWorldViewProjection", view.WorldClip);
        shader.Set("flavor", Flavor(plan.Source));
        shader.Set("clock", age / 60f);
        float reduced = view.Reduced ? 1 : 0;
        string pass; Vector4 signal; int count;
        if (age < plan.End)
        {
            float close = 1 - CrimsonInvocation.Ease((age - (plan.End - CloseTicks)) / CloseTicks);
            signal = new(age - plan.Fire, close, 1, reduced);
            pass = "AutoloadPass"; count = CrimsonTechniqueGeometry.Write(plan, age, buffer, false);
        }
        else
        {
            float fade = 1 - (age - plan.End) / ResidueTicksOf(plan);
            signal = new(age - plan.End, fade * fade, 1, reduced);
            pass = "ResiduePass"; count = CrimsonTechniqueGeometry.Write(plan, plan.End - 1, buffer, false);
        }
        // Yield: each stroke the next note's forecast does not cover dries in ScarletResidueYield.YieldTicks.
        bool yields = age >= plan.End && ScarletResidueYield.Applies(plan);
        int follower = yields ? ScarletResidueYield.Successor(plan, phrase) : -1;
        int next = follower >= 0 ? ScarletResidueYield.Forecast(phrase[follower], successor) : 0;
        shader.Set("signal", signal);
        // The noise seed comes from a point that holds still for the whole strike. The crossflow's capsule start moves
        // with its width while the stream opens and closes (its round end stays on the right stream end), so it seeds
        // from that fixed end instead; seeding from the moving start would re-roll the black blood every tick.
        bool band = plan.Technique == CrimsonTechnique.SideBeams;
        CrimsonPoint? anchor = band ? CrimsonChoreography.Reach(plan).Right : null;
        for (int i = 0; i < count; i++)
        {
            var s = buffer[i];
            Vector2 a = new(s.A.X, s.A.Y), delta = new(s.B.X - s.A.X, s.B.Y - s.A.Y);
            float length = delta.Length();
            if (s.Radius < .5f) continue;
            if (yields)
            {
                float strength = signal.Y * ScarletResidueYield.Factor(ScarletResidueYield.Holds(s, successor.AsSpan(0, next)), age - plan.End);
                if (strength <= .001f) continue;
                shader.Set("signal", new Vector4(signal.X, strength, signal.Z, signal.W));
            }
            Vector2 along = length > .01f ? delta / length : Vector2.UnitX, normal = new(-along.Y, along.X);
            // The quad reaches beyond the radius by the margin rule (ScarletInkMargin), so the glow fades out before its edge.
            float margin = ScarletInkMargin.Of(s.Radius), extent = s.Radius + margin;
            var seed = anchor ?? s.A;
            if (band)
            {
                // The stream is horizontal: its band runs from the right end (hi) leftward to its front (lo), the extent of the
                // capsule with its round ends. The shader's own segment is the whole band (its x = 0 on the right end, which
                // holds still, so the flow does not slide while the stream opens), the quad stops at both ends and no round
                // end is drawn: the ink is cut square exactly on the two stream ends, where the seals are.
                float hi = MathF.Max(s.A.X, s.B.X) + s.Radius, lo = MathF.Min(s.A.X, s.B.X) - s.Radius, cut = hi - lo;
                if (cut < .5f) continue;
                along = -Vector2.UnitX; normal = new(0, -1);
                shader.Set("shape", new Vector4(cut, s.Radius, (seed.X * .37f + seed.Y * .61f + i * 3.1f) % 17f * .1f, margin));
                shader.Apply(pass);
                float span = cut + extent * 2;
                Quad(new Vector2(hi, s.A.Y) - normal * extent, normal * extent * 2, along * cut, extent / span, (cut + extent) / span);
                device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
                continue;
            }
            shader.Set("shape", new Vector4(length, s.Radius, (seed.X * .37f + seed.Y * .61f + i * 3.1f) % 17f * .1f, margin));
            shader.Apply(pass);
            Quad(a - along * extent - normal * extent, normal * extent * 2, along * (length + extent * 2));
            device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
        }
    }

    // The shader reads its position along the quad from the u coordinate: [u0, u1] of the whole span (margins included).
    private void Quad(Vector2 start, Vector2 across, Vector2 along, float u0 = 0, float u1 = 1)
    {
        quad[0] = new(new Vector3(start, 0), Color.White, new(u0, 0));
        quad[1] = new(new Vector3(start + across, 0), Color.White, new(u0, 1));
        quad[2] = new(new Vector3(start + along, 0), Color.White, new(u1, 0));
        quad[3] = quad[2]; quad[4] = quad[1];
        quad[5] = new(new Vector3(start + across + along, 0), Color.White, new(u1, 1));
    }
}
