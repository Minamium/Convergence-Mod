#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// One original shader, two bounded quads: the exact locked corridor and a small
// directional pressure mouth. No render targets, particles, actors or timers to
// leak into the next Fight. Luminance owns the shader's load/reload/disposal.
internal sealed class FirstSeverancePursuitBeamVisuals
{
    private readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[6];

    internal void Draw(SpriteBatch batch, FirstSeveranceLanceRay ray, double now,
        ulong start, ulong fire, ulong end, bool active, ulong? cancelledAt, Color color, bool reduced)
    {
        if (Main.dedServ || now < start || ray.Length <= 0 || ray.HalfWidth <= 0) return;
        double clock = cancelledAt is { } cancelled ? Math.Min(now, cancelled) : now;
        float fade = cancelledAt is { } stop ? 1 - Window(now, stop, stop + 12d) : 1;
        float born = Arrive(clock - start, 4);
        float tension = .68f * Arrive(clock - start, 8)
            + .32f * Window(clock, Math.Max(start, (double)fire - 12), fire);
        float after = 1 - Window(clock, end, end + 16d);
        if (fade * after < .001f) return;
        bool warning = clock < fire;
        // Authority decides danger. Fractional material time can never turn a
        // warning live or leave a bright damaging-looking tail after retirement.
        float live = active ? 1 : 0;
        float tail = !active && !warning ? after * .085f : 0;
        float kick = active ? 1 - Window(clock, fire, fire + 7d) : 0;
        float opacity = fade * (warning ? born : active ? 1 : tail);
        if (opacity < .001f) return;

        ManagedShader shader = ShaderManager.GetShader("Convergence.PursuitPrismFlow");
        Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY);
        var device = Main.instance.GraphicsDevice;
        batch.End();
        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var raster = device.RasterizerState;
        var sampler = device.SamplerStates[0];
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            // Same world transform as the surrounding batch, not a UI callback
            // or Luminance's trail projection. Subtract screenPosition only once.
            shader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
                Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
            shader.TrySetParameter("beamColor", color.ToVector3());
            shader.TrySetParameter("envelope", new Vector4(tension, live, opacity, kick));
            // Integrate the speed change; multiplying time by a varying speed
            // causes a discontinuous phase exactly when the shot fires.
            shader.TrySetParameter("flowTime", (float)((clock - start) * .055
                + Math.Clamp(clock - fire, 0, (double)end - fire) * .27));
            shader.TrySetParameter("detail", reduced ? .35f : 1f);

            shader.TrySetParameter("dimensions", new Vector2(ray.Length, ray.HalfWidth));
            Quad(origin, direction, ray.Length, ray.HalfWidth);
            shader.Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);

            // Elongated slit and inward fibers, never an iris/circular HUD seal.
            float radius = 24 + tension * 40 + kick * 18;
            shader.TrySetParameter("dimensions", new Vector2(220, radius));
            Quad(origin - direction * 150, direction, 220, radius);
            shader.Apply("MouthPass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
        }
        finally
        {
            device.BlendState = blend; device.DepthStencilState = depth;
            device.RasterizerState = raster; device.SamplerStates[0] = sampler;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    private void Quad(Vector2 origin, Vector2 direction, float length, float halfWidth)
    {
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 a = origin - Main.screenPosition - normal * halfWidth;
        Vector2 b = origin - Main.screenPosition + normal * halfWidth;
        Vector2 c = a + direction * length, d = b + direction * length;
        vertices[0] = new(new Vector3(a, 0), Color.White, new(0, 0));
        vertices[1] = new(new Vector3(b, 0), Color.White, new(0, 1));
        vertices[2] = new(new Vector3(c, 0), Color.White, new(1, 0));
        vertices[3] = vertices[2]; vertices[4] = vertices[1];
        vertices[5] = new(new Vector3(d, 0), Color.White, new(1, 1));
    }
}
