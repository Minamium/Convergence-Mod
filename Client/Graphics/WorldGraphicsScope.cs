#nullable enable
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Graphics;

// FNA fields are verified by the pinned API harness. Preserve the caller's
// actual batch (including a sky/UI matrix), never assume a world-space batch.
internal readonly record struct WorldBatchParameters(SpriteSortMode Sort, BlendState Blend,
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
    internal static WorldBatchParameters Capture(SpriteBatch batch)
        => new(SortOf(batch), BlendOf(batch), SamplerOf(batch), DepthOf(batch), RasterOf(batch), EffectOf(batch), TransformOf(batch));
    internal void Restore(SpriteBatch batch) => batch.Begin(Sort, Blend, Sampler, Depth, Raster, Effect, Transform);
}

internal readonly struct WorldGraphicsScope : IDisposable
{
    private readonly SpriteBatch batch;
    private readonly WorldBatchParameters batchParameters;
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
    internal WorldGraphicsScope(SpriteBatch batch)
    {
        this.batch = batch; batchParameters = WorldBatchParameters.Capture(batch); batch.End();
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

