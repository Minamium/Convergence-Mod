#nullable enable
using System;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.FirstSeverance;

// The projected Fight clock and physical Core position are supplied by the caller.
// This pass owns no gameplay state, textures, render targets, or device resources.
internal static class FirstSeveranceEnergyCore
{
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];

    internal static void Draw(SpriteBatch batch, Vector2 center, float radius, float seconds,
        float opacity, bool reduced, float intensity = 0, float bore = 0,
        Vector2 boreAxis = default, bool twinBore = false)
    {
        if (Main.dedServ || radius < 1 || opacity <= .001f)
            return;

        ManagedShader shader = ShaderManager.GetShader("Convergence.DollCoreEnergy");
        GraphicsDevice device = Main.instance.GraphicsDevice;
        Vector2 axis = boreAxis.LengthSquared() > .001f
            ? Vector2.Normalize(boreAxis) : Vector2.UnitY;
        shader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        shader.TrySetParameter("clock", seconds);
        shader.TrySetParameter("signal", new Vector4(MathHelper.Clamp(opacity, 0, 1),
            MathHelper.Clamp(intensity, 0, 1), reduced ? 1 : 0, MathHelper.Clamp(bore, 0, 1)));
        shader.TrySetParameter("boreAxis", axis);
        shader.TrySetParameter("twinBore", twinBore ? 1f : 0f);

        batch.End();
        BlendState blend = device.BlendState;
        DepthStencilState depth = device.DepthStencilState;
        RasterizerState raster = device.RasterizerState;
        Texture? texture1 = device.Textures[1], texture2 = device.Textures[2], texture3 = device.Textures[3];
        SamplerState sampler1 = device.SamplerStates[1], sampler2 = device.SamplerStates[2], sampler3 = device.SamplerStates[3];
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
            shader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
            shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 3, SamplerState.LinearWrap);

            Vector2 screenCenter = center - Main.screenPosition;
            FillQuad(screenCenter, radius * 1.36f);
            shader.Apply("CoronaPass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);

            FillQuad(screenCenter, radius);
            shader.Apply("AutoloadPass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
        }
        finally
        {
            device.Textures[1] = texture1; device.Textures[2] = texture2; device.Textures[3] = texture3;
            device.SamplerStates[1] = sampler1; device.SamplerStates[2] = sampler2; device.SamplerStates[3] = sampler3;
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    private static void FillQuad(Vector2 center, float radius)
    {
        Vector2 lo = center - new Vector2(radius), hi = center + new Vector2(radius);
        quad[0] = new(new Vector3(lo.X, lo.Y, 0), Color.White, Vector2.Zero);
        quad[1] = new(new Vector3(lo.X, hi.Y, 0), Color.White, Vector2.UnitY);
        quad[2] = new(new Vector3(hi.X, lo.Y, 0), Color.White, Vector2.UnitX);
        quad[3] = quad[2]; quad[4] = quad[1];
        quad[5] = new(new Vector3(hi.X, hi.Y, 0), Color.White, Vector2.One);
    }
}
