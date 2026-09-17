#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// FNA fields are verified by the pinned API harness. Preserve the caller's
// actual batch (including a sky/UI matrix), never assume a world-space batch.
internal readonly record struct ScarletBatchParameters(SpriteSortMode Sort, BlendState Blend,
    SamplerState Sampler, DepthStencilState Depth, RasterizerState Raster, Effect? Effect, Matrix Transform)
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sortMode")]
    private static extern ref SpriteSortMode SortOf(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "blendState")]
    private static extern ref BlendState BlendOf(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "samplerState")]
    private static extern ref SamplerState SamplerOf(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "depthStencilState")]
    private static extern ref DepthStencilState DepthOf(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "rasterizerState")]
    private static extern ref RasterizerState RasterOf(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "customEffect")]
    private static extern ref Effect? EffectOf(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "transformMatrix")]
    private static extern ref Matrix TransformOf(SpriteBatch batch);
    internal static ScarletBatchParameters Capture(SpriteBatch batch)
        => new(SortOf(batch), BlendOf(batch), SamplerOf(batch), DepthOf(batch), RasterOf(batch), EffectOf(batch), TransformOf(batch));
    internal void Restore(SpriteBatch batch) => batch.Begin(Sort, Blend, Sampler, Depth, Raster, Effect, Transform);
}

internal readonly struct ScarletGraphicsScope : IDisposable
{
    private readonly SpriteBatch batch;
    private readonly ScarletBatchParameters batchParameters;
    private readonly GraphicsDevice device;
    private readonly BlendState blend;
    private readonly DepthStencilState depth;
    private readonly RasterizerState raster;
    private readonly Rectangle scissor;
    private readonly bool cullScissor;
    private readonly VertexBufferBinding[] bindings;
    private readonly IndexBuffer? indices;
    private readonly Texture? t0, t1, t2, t3;
    private readonly SamplerState s0, s1, s2, s3;
    internal ScarletGraphicsScope(SpriteBatch batch)
    {
        this.batch = batch; batchParameters = ScarletBatchParameters.Capture(batch); batch.End();
        device = Main.instance.GraphicsDevice;
        blend = device.BlendState; depth = device.DepthStencilState; raster = device.RasterizerState;
        scissor = device.ScissorRectangle; cullScissor = RasterizerState.CullNone.ScissorTestEnable;
        bindings = device.GetVertexBuffers(); indices = device.Indices;
        t0 = device.Textures[0]; t1 = device.Textures[1]; t2 = device.Textures[2]; t3 = device.Textures[3];
        s0 = device.SamplerStates[0]; s1 = device.SamplerStates[1]; s2 = device.SamplerStates[2]; s3 = device.SamplerStates[3];
        device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
    }
    public void Dispose()
    {
        device.Textures[0] = t0; device.Textures[1] = t1; device.Textures[2] = t2; device.Textures[3] = t3;
        device.SamplerStates[0] = s0; device.SamplerStates[1] = s1; device.SamplerStates[2] = s2; device.SamplerStates[3] = s3;
        device.SetVertexBuffers(bindings); device.Indices = indices;
        RasterizerState.CullNone.ScissorTestEnable = cullScissor;
        device.RasterizerState = raster; device.ScissorRectangle = scissor;
        device.BlendState = blend; device.DepthStencilState = depth;
        batchParameters.Restore(batch);
    }
}

// Continuous materials on the accepted geometry, not solid debug capsules.
internal static class ScarletMaterials
{
    private static readonly List<Vector2> points = new(128);
    private static readonly List<float> radii = new(128);
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private static PrimitiveSettings? settings;
    private static ManagedShader? ribbon;
    internal static Color Palette(int source) => source switch
    {
        0 => new(255, 104, 62), 1 => new(245, 78, 116),
        2 => new(222, 55, 137), _ => new(255, 84, 120)
    };
    internal static void Reset() { ribbon = null; settings = null; points.Clear(); radii.Clear(); }
    private static void Configure(int source, float clock, float alpha, bool warning, bool rift, float accent,
        bool physical = false, float charge = 0, float release = -1)
    {
        ribbon ??= ShaderManager.GetShader("Convergence.ScarletRibbon");
        settings ??= new PrimitiveSettings(Width, _ => Color.White, Smoothen: false, Shader: ribbon);
        ribbon.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        ribbon.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
        ribbon.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 3, SamplerState.LinearWrap);
        ribbon.TrySetParameter("clock", clock / 60f);
        ribbon.TrySetParameter("species", (float)source);
        ribbon.TrySetParameter("tint", Palette(source).ToVector3());
        ribbon.TrySetParameter("signal", new Vector4(alpha, warning ? 1 : 0, rift ? 1 : 0, accent));
        ribbon.TrySetParameter("reduced", CrimsonVisuals.Reduced ? 1f : 0f);
        ribbon.TrySetParameter("roundShape", 0f);
        ribbon.TrySetParameter("capAxis", Vector2.Zero);
        ribbon.TrySetParameter("motion", new Vector3(physical ? 1 : 0, charge, release));
    }
    private static float Width(float u)
    {
        float index = Math.Clamp(u, 0, 1) * (radii.Count - 1); int i = (int)index;
        return MathHelper.Lerp(radii[i], radii[Math.Min(i + 1, radii.Count - 1)], index - i);
    }
    // Caller has ended SpriteBatch with ScarletGraphicsScope.
    internal static void Strokes(ReadOnlySpan<CrimsonStroke> strokes, int source, float age, float alpha,
        bool warning, bool rift, float accent, float charge = 0, float release = -1)
    {
        if (Main.dedServ || strokes.IsEmpty || alpha <= .001f) return;
        Configure(source, age, alpha, warning, rift, accent, true, charge, release);
        points.Clear(); radii.Clear();
        for (int i = 0; i < strokes.Length; i++)
        {
            ref readonly var s = ref strokes[i];
            Vector2 a = CrimsonGestureVisuals.V(s.A), b = CrimsonGestureVisuals.V(s.B);
            if (Vector2.DistanceSquared(a, b) < .01f)
            { Flush(); Disk(a, s.Radius); continue; }
            if (points.Count > 0 && Vector2.DistanceSquared(points[^1], a) > .04f) Flush();
            if (points.Count == 0) { points.Add(a); radii.Add(s.Radius); }
            else radii[^1] = Math.Max(radii[^1], s.Radius);
            points.Add(b); radii.Add(s.Radius);
            if (points.Count >= 96) Flush();
        }
        Flush();
    }
    internal static void Path(IReadOnlyList<Vector2> path, float radius, int source, float age, float alpha,
        bool warning = false, bool rift = false, float accent = 0)
    {
        if (Main.dedServ || path.Count < 2 || alpha <= .001f) return;
        Configure(source, age, alpha, warning, rift, accent);
        points.Clear(); radii.Clear();
        for (int i = 0; i < path.Count && i < 120; i++) { points.Add(path[i]); radii.Add(radius); }
        Flush();
    }
    private static void Flush()
    {
        if (points.Count == 0) return;
        if (points.Count == 2)
        { points.Insert(1, (points[0] + points[1]) * .5f); radii.Insert(1, (radii[0] + radii[1]) * .5f); }
        if (points.Count >= 3)
        {
            float length = 0, radius = 0;
            for (int i = 0; i < points.Count; i++)
            {
                radius += radii[i];
                if (i > 0) length += Vector2.Distance(points[i - 1], points[i]);
            }
            ribbon!.TrySetParameter("footprint", new Vector2(Math.Max(1, length), Math.Max(1, radius / points.Count)));
            ribbon!.TrySetParameter("roundShape", 0f);
            ribbon.TrySetParameter("capAxis", Vector2.Zero);
            PrimitiveRenderer.RenderTrail(points, settings!, points.Count);
            Disk(points[0], radii[0], EndDirection(points[0] - points[1]));
            Disk(points[^1], radii[^1], EndDirection(points[^1] - points[^2]));
        }
        points.Clear(); radii.Clear();
    }
    private static Vector2 EndDirection(Vector2 delta) => delta.LengthSquared() > .001f ? Vector2.Normalize(delta) : Vector2.Zero;
    private static void Disk(Vector2 center, float radius, Vector2 capAxis = default)
    {
        if (radius <= 0) return;
        var shader = ribbon!;
        shader.TrySetParameter("footprint", new Vector2(radius * 2, radius));
        shader.TrySetParameter("capAxis", capAxis);
        shader.TrySetParameter("roundShape", 1f);
        shader.TrySetParameter("uWorldViewProjection", WorldMatrix);
        Quad(center - Main.screenPosition - new Vector2(radius), new Vector2(radius * 2));
        shader.Apply("CapPass");
        Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }
    internal static Matrix WorldMatrix => Main.GameViewMatrix.TransformationMatrix *
        Matrix.CreateOrthographicOffCenter(0, Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height, 0, -1, 1);
    internal static void Quad(Vector2 topLeft, Vector2 size)
    {
        quad[0] = new(new(topLeft, 0), Color.White, new(0, 0));
        quad[1] = new(new(topLeft + new Vector2(0, size.Y), 0), Color.White, new(0, 1));
        quad[2] = new(new(topLeft + new Vector2(size.X, 0), 0), Color.White, new(1, 0));
        quad[3] = quad[2]; quad[4] = quad[1]; quad[5] = new(new(topLeft + size, 0), Color.White, new(1, 1));
    }
    internal static void DrawQuad(ManagedShader shader, Vector2 topLeft, Vector2 size, string pass = "AutoloadPass")
    {
        Quad(topLeft, size); shader.Apply(pass);
        Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }
}
