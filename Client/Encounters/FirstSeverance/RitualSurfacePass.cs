#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
    private static BasicEffect? effect;
    private static Asset<Texture2D>? feather;
    internal static void Begin() { used = 0; }
    internal static void Ribbon(ReadOnlySpan<Vector2> points, float width, Color color)
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
            float radius = width * .5f * MathF.Pow(Math.Max(0, MathF.Sin(t * MathF.PI)), .65f);
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
    internal static void Flame(ReadOnlySpan<Vector2> points, float width, Color tint, float opacity)
    {
        Ribbon(points, width * 1.16f, new Color(10, 3, 23, 215) * opacity);
        Ribbon(points, width, new Color(tint.R, tint.G, tint.B, 0) * (opacity * .9f));
        Ribbon(points, width * .36f, new Color(228, 217, 255, 0) * opacity);
        Ribbon(points, width * .10f, new Color(255, 252, 246, 0) * opacity);
    }
    internal static void Flush()
    {
        if (Main.dedServ || used == 0) return;
        var device = Main.graphics.GraphicsDevice;
        var blend = device.BlendState; var depth = device.DepthStencilState;
        var raster = device.RasterizerState; var sampler = device.SamplerStates[0];
        try
        {
            effect ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true, LightingEnabled = false };
            feather ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/Items/RitualArmaments/V2/RibbonFeather");
            effect.Texture = feather.Value;
            effect.World = Matrix.Identity; effect.View = Main.GameViewMatrix.TransformationMatrix;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1);
            device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone; device.SamplerStates[0] = SamplerState.LinearClamp;
            foreach (var pass in effect.CurrentTechnique.Passes)
            { pass.Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices!, 0, used / 3); }
        }
        finally
        { device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster; device.SamplerStates[0] = sampler; used = 0; }
    }
    internal static void Dispose() { effect?.Dispose(); effect = null; feather = null; vertices = null; used = 0; }
}
