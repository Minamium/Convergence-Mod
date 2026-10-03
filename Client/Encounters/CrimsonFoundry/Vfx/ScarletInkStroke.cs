#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// Scarlet's strike material (ScarletInk.fx) on the authoritative CrimsonStroke capsules: a
// river of black blood. Live: a black body whose rim burns and melts, red threads streaming
// along it, a twisting core, and a blaze in the first ticks after impact. Residue: the ink
// dries into a narrow scar. Nothing draws outside a capsule except a few pixels of
// anti-aliased rim.
//
// It owns only the live and residue time of a field beam (TrackingBeam, SideBeams). The
// forecast (CrimsonEnergy's PortalForecastPass + ForecastDustPass + mouth) and the two
// seals that enclose the crossflow stay with their original renderers, so ScarletInk's
// ForecastPass is never used here.
internal sealed class ScarletInkStroke
{
    internal const float Margin = 10;
    internal const int CloseTicks = 8, ResidueTicks = 24;
    private readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private readonly CrimsonStroke[] buffer = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];

    // Ember Crown burns, Sable Mantle is silk, Thorn Choir is bone; Final mixes them.
    internal static Vector4 Flavor(int source) => source switch
    {
        0 => new(1, 0, 0, 0), 1 => new(0, 1, 0, 0), 2 => new(0, 0, 1, 0), _ => new(.5f, .3f, .3f, 1)
    };

    // The techniques drawn as field beams: the thin tracking line and the stream between the crossflow seals.
    internal static bool Applies(in CrimsonGesturePlan plan)
        => plan.Technique is CrimsonTechnique.TrackingBeam or CrimsonTechnique.SideBeams;

    // Live strike and its residue; the warning before Fire belongs to the owner's original forecast.
    internal static bool Owns(in CrimsonGesturePlan plan, float age)
        => Applies(plan) && age >= plan.Fire && age < plan.End + ResidueTicks;

    internal void Draw(in ScarletView view, IScarletAssets assets, in CrimsonGesturePlan plan)
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
            float fade = 1 - (age - plan.End) / ResidueTicks;
            signal = new(age - plan.End, fade * fade, 1, reduced);
            pass = "ResiduePass"; count = CrimsonTechniqueGeometry.Write(plan, plan.End - 1, buffer, false);
        }
        shader.Set("signal", signal);
        // The noise seed comes from a point that holds still for the whole strike. The crossflow's capsule start moves
        // with its width while the stream opens and closes (its round end stays on the right stream end), so it seeds
        // from that fixed end instead; seeding from the moving start would re-roll the black blood every tick.
        CrimsonPoint? anchor = plan.Technique == CrimsonTechnique.SideBeams ? CrimsonChoreography.Reach(plan).Right : null;
        for (int i = 0; i < count; i++)
        {
            var s = buffer[i];
            Vector2 a = new(s.A.X, s.A.Y), delta = new(s.B.X - s.A.X, s.B.Y - s.A.Y);
            float length = delta.Length();
            if (s.Radius < .5f) continue;
            Vector2 along = length > .01f ? delta / length : Vector2.UnitX, normal = new(-along.Y, along.X);
            float extent = s.Radius + Margin;
            var seed = anchor ?? s.A;
            shader.Set("shape", new Vector4(length, s.Radius, (seed.X * .37f + seed.Y * .61f + i * 3.1f) % 17f * .1f, Margin));
            shader.Apply(pass);
            Quad(a - along * extent - normal * extent, normal * extent * 2, along * (length + extent * 2));
            device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
        }
    }

    private void Quad(Vector2 start, Vector2 across, Vector2 along)
    {
        quad[0] = new(new Vector3(start, 0), Color.White, new(0, 0));
        quad[1] = new(new Vector3(start + across, 0), Color.White, new(0, 1));
        quad[2] = new(new Vector3(start + along, 0), Color.White, new(1, 0));
        quad[3] = quad[2]; quad[4] = quad[1];
        quad[5] = new(new Vector3(start + across + along, 0), Color.White, new(1, 1));
    }
}
