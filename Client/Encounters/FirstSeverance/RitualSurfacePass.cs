#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// Connected triangle strips, not hundreds of overlapping line-sprite end caps.
// Both presentation systems flush before drawing their opaque physical parts.
internal static class RitualSurfacePass
{
    private const int Capacity = 196608;
    private static VertexPositionColorTexture[]? vertices;
    private static int used;
    internal static void Begin() { used = 0; }
    internal static void Ribbon(ReadOnlySpan<Vector2> points, float width, Color color, bool taper = true,
        float startWidth = -1, float openingFraction = 0)
    {
        if (Main.dedServ || width <= 0 || points.Length < 2 || points.Length > 256) return;
        int required = (points.Length - 1) * 6;
        if (used + required > Capacity) return; // Bounded decoration only; no combat consequence.
        vertices ??= new VertexPositionColorTexture[Capacity];
        Vector2 previousLeft = default, previousRight = default;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 tangent = points[Math.Min(i + 1, points.Length - 1)] - points[Math.Max(0, i - 1)];
            if (!float.IsFinite(points[i].X) || !float.IsFinite(points[i].Y)) return;
            Vector2 normal = new Vector2(-tangent.Y, tangent.X).SafeNormalize(Vector2.UnitY);
            float t = i / (float)(points.Length - 1);
            float materialWidth = startWidth >= 0 && openingFraction > 0
                ? MathHelper.Lerp(startWidth, width, FirstSeveranceVisualCurves.Window(t, 0, openingFraction)) : width;
            float radius = materialWidth * .5f * (taper ? MathF.Pow(Math.Max(0, MathF.Sin(t * MathF.PI)), .65f) : 1);
            Vector2 left = points[i] - Main.screenPosition + normal * radius;
            Vector2 right = points[i] - Main.screenPosition - normal * radius;
            if (i > 0)
            {
                float u0 = (i - 1f) / (points.Length - 1);
                Add(previousLeft, new(u0, 0), color); Add(previousRight, new(u0, 1), color); Add(left, new(t, 0), color);
                Add(left, new(t, 0), color); Add(previousRight, new(u0, 1), color); Add(right, new(t, 1), color);
            }
            previousLeft = left; previousRight = right;
        }
    }
    private static void Add(Vector2 p, Vector2 uv, Color c) => vertices![used++] = new(new Vector3(p, 0), c, uv);
    internal static void Flame(ReadOnlySpan<Vector2> points, float width, Color tint, float opacity, bool darkUnderlay = true)
    {
        // One flowing material, without the old black slab and stacked flat cores.
        Ribbon(points, width, new Color(tint.R, tint.G, tint.B, 0) * (opacity * .9f));
    }
    internal static void Flush()
    {
        if (Main.dedServ || used == 0) return;
        var device = Main.graphics.GraphicsDevice;
        var blend = device.BlendState; var depth = device.DepthStencilState;
        var raster = device.RasterizerState;
        var t1 = device.Textures[1]; var t2 = device.Textures[2];
        var s1 = device.SamplerStates[1]; var s2 = device.SamplerStates[2];
        try
        {
            var effect = ShaderManager.GetShader("Convergence.ArmamentEnergy");
            effect.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
                Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
            effect.TrySetParameter("clock", RitualRenderClock.Time / 60);
            effect.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
            effect.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
            device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.Apply("AutoloadPass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices!, 0, used / 3);
        }
        finally
        {
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster;
            device.Textures[1] = t1; device.Textures[2] = t2;
            device.SamplerStates[1] = s1; device.SamplerStates[2] = s2; used = 0;
        }
    }
    internal static void Dispose() { vertices = null; used = 0; }
}
