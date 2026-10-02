#nullable enable
using System;
using Convergence.Content.Encounters.GhostSamurai;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.GhostSamurai;

// One frame of the sealed field, produced by GhostSamuraiBattlefield from the
// accepted fight and the local view. Times are seconds; fractions are 0..1.
internal struct SamuraiFieldLook
{
    internal float Clock, Reduced, Presence, Phase, Surge, Deploy, Ending;
    internal bool Outsider;
    internal Vector2 Camera;                  // view centre minus field centre, world px
    internal Vector4 Ripple0, Ripple1;        // field-local x, y, age seconds, strength
}

// Terraria-free drawing, so the offline preview links this exact code.
internal static class SamuraiFieldRenderer
{
    internal const string ShaderName = "Convergence.SamuraiBattlefield";
    private const float Bleed = 4f;
    private static readonly Vector2 Size = new(SamuraiArenaBounds.Width, SamuraiArenaBounds.Height);
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];

    // World quad over the field plus a small bleed hidden under the seal, behind terrain.
    // `view` is the world view matrix; `fieldTopLeft` is the field corner minus the screen position.
    internal static void DrawBackdrop(GraphicsDevice device, Matrix view, Vector2 fieldTopLeft, in SamuraiFieldLook look)
    {
        var shader = Bind(look);
        shader.TrySetParameter("uWorldViewProjection", view
            * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        Vector2 size = Size + new Vector2(Bleed * 2);
        Fill(fieldTopLeft - new Vector2(Bleed), size, new Vector2(-Bleed) / Size, size / Size);
        shader.Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    // Physical-viewport quad above the world: the opaque abyss outside and everything
    // on the seal's outer face. `field` is the field's left, top, right and bottom in
    // physical viewport pixels, unclamped; `flipY` when reversed gravity mirrors the view.
    internal static void DrawSeal(GraphicsDevice device, Vector4 field, bool flipY, in SamuraiFieldLook look)
        => Overlay(device, field, flipY, look, "SealPass");

    // The thin light just inside the seal, drawn in the world pass behind players,
    // bodies and forecasts so it never lies over combat information.
    internal static void DrawRim(GraphicsDevice device, Vector4 field, bool flipY, in SamuraiFieldLook look)
        => Overlay(device, field, flipY, look, "RimPass");

    // Corners of the field in physical viewport pixels (min/max of all four, like the
    // tested mask capture, but unclamped so the shader knows where the real edge is).
    internal static Vector4 ScreenField(float left, float top, float right, float bottom,
        System.Numerics.Matrix3x2 worldToScreen, out bool flipY)
    {
        var a = System.Numerics.Vector2.Transform(new(left, top), worldToScreen);
        var b = System.Numerics.Vector2.Transform(new(right, top), worldToScreen);
        var c = System.Numerics.Vector2.Transform(new(left, bottom), worldToScreen);
        var d = System.Numerics.Vector2.Transform(new(right, bottom), worldToScreen);
        var min = System.Numerics.Vector2.Min(System.Numerics.Vector2.Min(a, b), System.Numerics.Vector2.Min(c, d));
        var max = System.Numerics.Vector2.Max(System.Numerics.Vector2.Max(a, b), System.Numerics.Vector2.Max(c, d));
        flipY = a.Y > c.Y;
        return new Vector4(min.X, min.Y, max.X, max.Y);
    }

    private static void Overlay(GraphicsDevice device, Vector4 field, bool flipY, in SamuraiFieldLook look, string pass)
    {
        var shader = Bind(look);
        int width = device.Viewport.Width, height = device.Viewport.Height;
        shader.TrySetParameter("uWorldViewProjection", Matrix.CreateOrthographicOffCenter(0, width, height, 0, -1, 1));
        shader.TrySetParameter("rect", field);
        shader.TrySetParameter("worldPerPx", Size.X / Math.Max(1e-3f, field.Z - field.X));
        shader.TrySetParameter("flipY", flipY ? 1f : 0f);
        Vector2 screen = new(width, height);
        Fill(Vector2.Zero, screen, Vector2.Zero, screen);
        shader.Apply(pass);
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    private static ManagedShader Bind(in SamuraiFieldLook look)
    {
        var shader = ShaderManager.GetShader(ShaderName);
        shader.TrySetParameter("clock", look.Clock);
        shader.TrySetParameter("reduced", look.Reduced);
        shader.TrySetParameter("presence", look.Presence);
        shader.TrySetParameter("phase", look.Phase);
        shader.TrySetParameter("surge", look.Surge);
        shader.TrySetParameter("deploy", look.Deploy);
        shader.TrySetParameter("ending", look.Ending);
        shader.TrySetParameter("outsider", look.Outsider ? 1f : 0f);
        shader.TrySetParameter("arenaSize", Size);
        shader.TrySetParameter("camera", look.Camera);
        shader.TrySetParameter("ripple0", look.Ripple0);
        shader.TrySetParameter("ripple1", look.Ripple1);
        shader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 3, SamplerState.LinearWrap);
        return shader;
    }

    private static void Fill(Vector2 at, Vector2 size, Vector2 uvAt, Vector2 uvSize)
    {
        for (int i = 0; i < 6; i++)
        {
            int corner = i switch { 0 => 0, 1 => 2, 2 => 1, 3 => 1, 4 => 2, _ => 3 };
            Vector2 c = new(corner % 2, corner / 2);
            quad[i] = new(new(at + c * size, 0), Color.White, uvAt + c * uvSize);
        }
    }
}
