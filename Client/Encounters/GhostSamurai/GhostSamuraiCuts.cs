#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.GhostSamurai;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.GhostSamurai;

// Pixel-art sword incisions, not beam emitters. Every pass uses the accepted
// rectangle, disk/annulus or travelling crescent and its fractional Fire/End
// clock; the shader lights only world-aligned art pixels inside that footprint.
internal static class GhostSamuraiCuts
{
    internal const int ResidueTicks = 16;
    private static ManagedShader? material;
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    internal static void Reset() => material = null;

    internal static void Slash(SpriteBatch batch, SamuraiHazard h, Vector2 at, float age, bool reduced)
        => Stroke(batch, at, at + new Vector2(h.DX, h.DY) * h.Length, h.Radius, age,
            h.Shape == SamuraiShape.VerticalSlash ? h.Fire - SamuraiComboRules.VerticalForecast : h.Born,
            h.Fire, h.End, reduced, Seed(h));

    internal static float Seed(SamuraiHazard h) => h.X * .0137f + h.Y * .0191f + h.Born * .173f;

    internal static void Stroke(SpriteBatch batch, Vector2 a, Vector2 b, float radius,
        float age, float born, float fire, float end, bool reduced, float seed, bool route = false)
    {
        Vector2 d = b - a;
        float length = d.Length();
        if (Main.dedServ || !float.IsFinite(length) || length < 1 || radius <= 0 || age < born || age >= end + ResidueTicks) return;
        using var scope = new WorldGraphicsScope(batch);
        Vector2 along = d / length, across = new(-along.Y, along.X);
        Prepare(age, born, fire, end, reduced, new(length, radius, seed, route ? 1 : 0), a, along, across);
        Vector2 n = across * radius;
        Draw(a - n, b - n, a + n, b + n, Vector2.Zero, Vector2.One, "AutoloadPass");
    }

    internal static void Field(SpriteBatch batch, SamuraiHazard h, Vector2 at, float age, Rectangle viewport, bool reduced)
    {
        if (Main.dedServ || h.Radius <= 0 || age < h.Born || age >= h.End + ResidueTicks) return;
        var bounds = new Rectangle((int)MathF.Floor(at.X - h.Radius), (int)MathF.Floor(at.Y - h.Radius),
            (int)MathF.Ceiling(h.Radius * 2 + 2), (int)MathF.Ceiling(h.Radius * 2 + 2));
        var clip = Rectangle.Intersect(bounds, viewport);
        if (clip.Width <= 0 || clip.Height <= 0) return;
        Vector2 a = new(clip.Left, clip.Top), b = new(clip.Right, clip.Bottom), origin = at - new Vector2(h.Radius);
        using var scope = new WorldGraphicsScope(batch);
        Prepare(age, h.Born, h.Fire, h.End, reduced,
            new(h.Radius, h.IsOuter ? h.Length : 0, h.Shape == SamuraiShape.FrontalCleave ? h.DX : 0, h.IsWind ? 1 : 0),
            at, Vector2.UnitX, Vector2.UnitY);
        Draw(a, new(b.X, a.Y), new(a.X, b.Y), b, (a - origin) / (h.Radius * 2),
            (b - origin) / (h.Radius * 2), "FieldPass");
    }

    internal static void Wave(SpriteBatch batch, SamuraiHazard h, Vector2 at, float age, bool reduced)
    {
        Vector2 d = new(h.DX, h.DY), n = new(-h.DY, h.DX);
        if (age < h.Fire)
        {
            float reach = (h.End - h.Fire - 1) * SamuraiWaveRules.ChargedSlashWaveSpeed + h.Length / 2;
            Stroke(batch, at, at + d * reach, h.Radius, age, h.Born, h.Fire, h.End, reduced, Seed(h), route: true);
            return;
        }
        if (Main.dedServ || age >= h.End + ResidueTicks) return;
        using var scope = new WorldGraphicsScope(batch);
        Prepare(age, h.Born, h.Fire, h.End, reduced, new(h.Length, h.Radius, Seed(h), 0), at, d, n);
        d *= h.Length * .5f; n *= h.Radius;
        Draw(at - d - n, at + d - n, at - d + n, at + d + n, Vector2.Zero, Vector2.One, "WavePass");
    }

    internal static void Wisp(SpriteBatch batch, Vector2 at, float radius, float age, Vector2 heading, float seed, bool reduced)
    {
        if (Main.dedServ || radius <= 0) return;
        Vector2 d = heading.SafeNormalize(Vector2.UnitX), n = new(-d.Y, d.X);
        using var scope = new WorldGraphicsScope(batch);
        Prepare(age, 0, 0, 1, reduced, new(radius, 0, seed, 0), at, d, n);
        Vector2 front = at + d * radius * 1.3f, back = at - d * radius * 3.4f, side = n * radius * 1.3f;
        Draw(back - side, front - side, back + side, front + side, Vector2.Zero, Vector2.One, "WispPass");
    }

    private static void Prepare(float age, float born, float fire, float end, bool reduced, Vector4 shape,
        Vector2 origin, Vector2 axisX, Vector2 axisY)
    {
        material ??= ShaderManager.GetShader("Convergence.SamuraiCut");
        material.TrySetParameter("clock", age / 60);
        material.TrySetParameter("uScreenPosition", Main.screenPosition);
        material.TrySetParameter("frameOrigin", origin);
        material.TrySetParameter("frameX", axisX);
        material.TrySetParameter("frameY", axisY);
        material.TrySetParameter("phase", new Vector4(age - fire, Math.Max(1, end - fire),
            Math.Clamp((age - born) / Math.Max(1, fire - born), 0, 1), reduced ? 1 : 0));
        material.TrySetParameter("shape", shape);
        material.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, Main.instance.GraphicsDevice.Viewport.Width,
                Main.instance.GraphicsDevice.Viewport.Height, 0, -1, 1));
        material.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        material.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
    }
    private static void Draw(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 uvMin, Vector2 uvMax, string pass)
    {
        quad[0] = V(a, uvMin.X, uvMin.Y); quad[1] = V(c, uvMin.X, uvMax.Y); quad[2] = V(b, uvMax.X, uvMin.Y);
        quad[3] = quad[2]; quad[4] = quad[1]; quad[5] = V(d, uvMax.X, uvMax.Y);
        material!.Apply(pass); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
        static VertexPositionColorTexture V(Vector2 p, float u, float v) => new(new(p, 0), Color.White, new(u, v));
    }
}
