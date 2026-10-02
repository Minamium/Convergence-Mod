#nullable enable
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ScarletSpriteVertex : IVertexType
{
    internal Vector3 Position;
    internal Color Color;
    internal Vector2 Uv;   // texture coordinates
    internal Vector2 Part; // 0..1 across the drawn part (where its edges are)
    internal static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1));
    VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}

// A sprite part drawn through ScarletInk's SpriteBurnPass: burn 0 draws it intact; burn 0..1 erodes it from its
// edges by noise behind a burning crimson lip (the reliquary's exit), never an alpha fade. Final pixel art uses
// point sampling, painted placeholders linear. Sets textures 0-2 and their samplers; restores nothing.
internal sealed class ScarletSpriteBurn
{
    private readonly ScarletSpriteVertex[] quad = new ScarletSpriteVertex[6];

    // world: where the origin lands (world px). origin: in source pixels. scale: world px per source pixel.
    internal void Draw(in ScarletView view, IScarletAssets assets, Texture2D texture, Rectangle source, Vector2 world, Vector2 origin,
        float rotation, Vector2 scale, Color color, float burn, float seed, bool pixelArt, bool flipX = false)
    {
        var device = view.Device;
        var shader = assets.GetShader("ScarletInk");
        float cos = System.MathF.Cos(rotation), sin = System.MathF.Sin(rotation);
        Vector2 Corner(float x, float y)
        {
            Vector2 local = new((x - origin.X) * scale.X, (y - origin.Y) * scale.Y);
            return world + new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos);
        }
        float u0 = source.Left / (float)texture.Width, u1 = source.Right / (float)texture.Width;
        float v0 = source.Top / (float)texture.Height, v1 = source.Bottom / (float)texture.Height;
        if (flipX) (u0, u1) = (u1, u0);
        var a = Vertex(Corner(0, 0), color, new(u0, v0), new(0, 0));
        var b = Vertex(Corner(source.Width, 0), color, new(u1, v0), new(1, 0));
        var c = Vertex(Corner(0, source.Height), color, new(u0, v1), new(0, 1));
        var d = Vertex(Corner(source.Width, source.Height), color, new(u1, v1), new(1, 1));
        quad[0] = a; quad[1] = b; quad[2] = c; quad[3] = c; quad[4] = b; quad[5] = d;
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        shader.Set("uWorldViewProjection", view.WorldClip);
        shader.Set("signal", new Vector4(burn, 0, 0, view.Reduced ? 1 : 0));
        shader.Set("shape", new Vector4(0, 0, seed, 0));
        shader.Set("clock", view.Clock / 60f);
        shader.Apply("SpriteBurnPass");
        device.Textures[0] = texture; device.SamplerStates[0] = pixelArt ? SamplerState.PointClamp : SamplerState.LinearClamp;
        device.Textures[1] = assets.GetTexture("Noise/TurbulentNoise"); device.SamplerStates[1] = SamplerState.LinearWrap;
        device.Textures[2] = assets.GetTexture("Noise/WavyBlotchNoise"); device.SamplerStates[2] = SamplerState.LinearWrap;
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    private static ScarletSpriteVertex Vertex(Vector2 at, Color color, Vector2 uv, Vector2 part)
        => new() { Position = new Vector3(at, 0), Color = color, Uv = uv, Part = part };
}
