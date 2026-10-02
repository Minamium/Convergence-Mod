#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// One vertex of a Doll layer primitive; DollPixel.fx documents L/S/T per pass. Weapon energy materials
// share this layout.
internal struct DollPixelVertex : IVertexType
{
    public Vector3 Position;
    public Vector4 Local, Shape, Style;

    internal static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(28, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(44, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2));

    VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}

// Palette, composites, the built-in ramp material and device-state helpers shared by the game layer
// and the offline preview. Depends only on FNA.
internal static class DollPixelArt
{
    internal const string ShaderName = "Convergence.DollPixel";
    internal const float GlowStrength = .55f;

    // BEGIN DOLL PALETTE (DollTone order; identical to the block in every Doll*.fx)
    internal static readonly Color[] Palette =
    {
        Hex(0x121017), // Ink
        Hex(0x1d1a22), // IronDark
        Hex(0x302a29), // Iron
        Hex(0x49404a), // IronLight
        Hex(0x9c8070), // PorcelainShade
        Hex(0xd0b69e), // Porcelain
        Hex(0xf4e3ce), // PorcelainLight
        Hex(0xfcf4e6), // Bone
        Hex(0xc9c4c9), // PearlGrey
        Hex(0xe1dce0), // Pearl
        Hex(0x684828), // BrassShade
        Hex(0xa07b48), // Brass
        Hex(0xd5b279), // BrassLight
        Hex(0x301840), // Plum
        Hex(0x5a2c9c), // PlumLight
        Hex(0x9458ff), // Violet
        Hex(0xb99cff), // Lilac
        Hex(0xddd8f8), // PearlViolet
        Hex(0xffffff), // White
        Hex(0x8c141c), // Ruby
    };
    // END DOLL PALETTE

    // The built-in energy material: DollPixel.fx RampPass (T.x intensity 0..1 or negative for void, T.y alpha).
    internal static readonly IDollEnergyMaterial Ramp = new RampMaterial();

    private static readonly VertexPositionTexture[] quad = new VertexPositionTexture[6];

    internal static Color Tone(DollTone tone) => Palette[Math.Min((int)tone, Palette.Length - 1)];

    private static Color Hex(uint rgb) => new((int)(rgb >> 16 & 0xFF), (int)(rgb >> 8 & 0xFF), (int)(rgb & 0xFF));

    // Point-upscales target dots `dots` to screen px screenOffset + 2 * dot (before `view`).
    internal static void CompositeArt(GraphicsDevice device, Effect effect, Texture2D art, Rectangle dots, Vector2 screenOffset, Matrix view)
    {
        Quad(device, effect, art, dots, screenOffset, view);
        device.Textures[0] = art;
        device.SamplerStates[0] = SamplerState.PointClamp;
        Apply(effect, "CompositeArtPass");
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    // The Light composite adds the ink outline (never over the art) and, unless reduced, the glow.
    internal static void CompositeLight(GraphicsDevice device, Effect effect, Texture2D light, Texture2D? art, Rectangle dots,
        Vector2 screenOffset, Matrix view, bool reduced)
    {
        Quad(device, effect, light, dots, screenOffset, view);
        Set(effect, "texel", new Vector2(1f / light.Width, 1f / light.Height));
        Set(effect, "glowStrength", reduced ? 0f : GlowStrength);
        device.Textures[0] = light;
        device.SamplerStates[0] = SamplerState.PointClamp;
        // Without art this frame the light itself stands in: any lit dot is skipped before the art test.
        device.Textures[1] = art ?? light;
        device.SamplerStates[1] = SamplerState.PointClamp;
        Apply(effect, reduced ? "CompositeLightPlainPass" : "CompositeLightPass");
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    private static void Quad(GraphicsDevice device, Effect effect, Texture2D scene, Rectangle dots, Vector2 screenOffset, Matrix view)
    {
        float width = scene.Width, height = scene.Height;
        VertexPositionTexture Corner(int x, int y)
            => new(new Vector3(screenOffset.X + x * 2, screenOffset.Y + y * 2, 0), new Vector2(x / width, y / height));
        quad[0] = Corner(dots.Left, dots.Top);
        quad[1] = Corner(dots.Right, dots.Top);
        quad[2] = Corner(dots.Left, dots.Bottom);
        quad[3] = quad[2];
        quad[4] = quad[1];
        quad[5] = Corner(dots.Right, dots.Bottom);
        Set(effect, "uWorldViewProjection", view * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
    }

    internal static void Set(Effect effect, string name, float value) => effect.Parameters[name]?.SetValue(value);
    internal static void Set(Effect effect, string name, Vector2 value) => effect.Parameters[name]?.SetValue(value);
    internal static void Set(Effect effect, string name, Vector4 value) => effect.Parameters[name]?.SetValue(value);
    internal static void Set(Effect effect, string name, Matrix value) => effect.Parameters[name]?.SetValue(value);
    internal static void Apply(Effect effect, string pass) => effect.CurrentTechnique.Passes[pass].Apply();

    private sealed class RampMaterial : IDollEnergyMaterial
    {
        public bool Apply(GraphicsDevice device, in DollEnergyContext context, int pass)
        {
            Set(context.Pixel, "uWorldViewProjection", context.Projection);
            Set(context.Pixel, "dotOrigin", context.DotOrigin);
            DollPixelArt.Apply(context.Pixel, "RampPass");
            return true;
        }
    }
}

// Caller's device state around the layer's targets and composites: render targets (read through FNA's
// allocation-free accessor into fixed arrays; FNA binds at most four), viewport, blend, depth, raster,
// scissor, index buffer and textures/samplers 0-2. Vertex buffers are not captured: GetVertexBuffers
// allocates per call, the layer only uses DrawUserPrimitives (which leaves the managed bindings alone and
// has FNA re-apply them on the next buffered draw), and SpriteBatch binds its own buffers on every flush.
internal readonly struct DollDeviceState
{
    private static readonly RenderTargetBinding[][] targets =
    {
        Array.Empty<RenderTargetBinding>(), new RenderTargetBinding[1], new RenderTargetBinding[2],
        new RenderTargetBinding[3], new RenderTargetBinding[4],
    };

    private readonly BlendState blend;
    private readonly DepthStencilState depth;
    private readonly RasterizerState raster;
    private readonly Rectangle scissor;
    private readonly Viewport viewport;
    private readonly SamplerState sampler0, sampler1, sampler2;
    private readonly Texture? texture0, texture1, texture2;
    private readonly IndexBuffer? indices;
    private readonly int targetCount;

    private DollDeviceState(GraphicsDevice device, bool withTargets)
    {
        blend = device.BlendState;
        depth = device.DepthStencilState;
        raster = device.RasterizerState;
        scissor = device.ScissorRectangle;
        viewport = device.Viewport;
        sampler0 = device.SamplerStates[0];
        sampler1 = device.SamplerStates[1];
        sampler2 = device.SamplerStates[2];
        texture0 = device.Textures[0];
        texture1 = device.Textures[1];
        texture2 = device.Textures[2];
        indices = device.Indices;
        targetCount = -1;
        if (!withTargets) return;
        targetCount = Math.Min(device.GetRenderTargetsNoAllocEXT(null), targets.Length - 1);
        device.GetRenderTargetsNoAllocEXT(targets[targetCount]);
    }

    internal static DollDeviceState Capture(GraphicsDevice device, bool withTargets) => new(device, withTargets);

    internal void Restore(GraphicsDevice device)
    {
        if (targetCount == 0) device.SetRenderTarget(null);
        else if (targetCount > 0) device.SetRenderTargets(targets[targetCount]);
        if (targetCount >= 0) Array.Clear(targets[targetCount]);
        device.Viewport = viewport;
        device.Textures[0] = texture0;
        device.Textures[1] = texture1;
        device.Textures[2] = texture2;
        device.SamplerStates[0] = sampler0;
        device.SamplerStates[1] = sampler1;
        device.SamplerStates[2] = sampler2;
        device.Indices = indices;
        device.BlendState = blend;
        device.DepthStencilState = depth;
        device.RasterizerState = raster;
        device.ScissorRectangle = scissor;
    }
}
