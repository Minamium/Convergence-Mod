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

// Bounded, original Luminance material shared by this encounter's presentation.
// Screen-relative world coordinates only. No UI matrix or simulation state.
internal static class GhostSamuraiEnergy
{
    private static ManagedShader? material;
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    internal static void Reset() => material = null;

    internal static void Line(SpriteBatch batch, Vector2 a, Vector2 b, float radius,
        float age, float progress, bool live, float since, float remaining, bool reduced, bool route = false)
    {
        Vector2 d = b - a;
        if (Main.dedServ || !float.IsFinite(d.LengthSquared()) || d.LengthSquared() < 1 || radius <= 0) return;
        using var scope = new WorldGraphicsScope(batch);
        Prepare(age, route ? 4 : 0, new(d.Length(), radius, 0, 0),
            new(progress, live ? 1 : 0, since, remaining), reduced, Vector4.One);
        Vector2 normal = new Vector2(-d.Y, d.X) / d.Length() * radius;
        Draw(a - normal, b - normal, a + normal, b + normal, Vector2.Zero, Vector2.One);
    }
    internal static void Slash(SpriteBatch batch, SamuraiHazard h, Vector2 at, float age, bool reduced)
        => Line(batch, at, at + new Vector2(h.DX, h.DY) * h.Length, h.Radius, age,
            Math.Clamp((age - h.Born) / Math.Max(1, h.Fire - h.Born), 0, 1), h.Live(age), age - h.Fire, h.End - age, reduced);

    internal static void Field(SpriteBatch batch, SamuraiHazard h, Vector2 at, float age, Rectangle viewport, bool reduced)
    {
        if (Main.dedServ || h.Radius <= 0) return;
        var bounds = new Rectangle((int)MathF.Floor(at.X - h.Radius), (int)MathF.Floor(at.Y - h.Radius),
            (int)MathF.Ceiling(h.Radius * 2 + 2), (int)MathF.Ceiling(h.Radius * 2 + 2));
        var clip = Rectangle.Intersect(bounds, viewport);
        if (clip.Width <= 0 || clip.Height <= 0) return;
        Vector2 minimum = new(clip.Left, clip.Top), maximum = new(clip.Right, clip.Bottom);
        Vector2 origin = at - new Vector2(h.Radius);
        using var scope = new WorldGraphicsScope(batch);
        Prepare(age, 1, new(h.Radius, h.IsOuter ? h.Length : 0, h.Shape == SamuraiShape.FrontalCleave ? h.DX : 0, h.IsWind ? 1 : 0),
            new(Math.Clamp((age - h.Born) / Math.Max(1, h.Fire - h.Born), 0, 1), h.Live(age) ? 1 : 0, age - h.Fire, h.End - age), reduced, Vector4.One);
        Draw(minimum, new(maximum.X, minimum.Y), new(minimum.X, maximum.Y), maximum,
            (minimum - origin) / (h.Radius * 2), (maximum - origin) / (h.Radius * 2));
    }
    internal static void Wisp(SpriteBatch batch, Vector2 at, float radius, float age, Vector2 direction, bool reduced)
    {
        if (Main.dedServ) return;
        using var scope = new WorldGraphicsScope(batch);
        Prepare(age, 2, Vector4.Zero, Vector4.Zero, reduced, Vector4.One);
        var d = direction.SafeNormalize(Vector2.UnitX) * radius;
        var n = new Vector2(-d.Y, d.X);
        Draw(at - d - n, at + d - n, at - d + n, at + d + n, Vector2.Zero, Vector2.One);
    }

    internal static void Body(SpriteBatch batch, in SamuraiRigPose pose, Vector2 screen, bool front, float opacity = 1)
    {
        if (Main.dedServ) return;
        bool reduced = GhostSamuraiRigArt.Reduced;
        float poseAge = pose.Age;
        var root = new Vector2(pose.X, pose.Y) - screen;
        float pulse = .90f + MathF.Sin(pose.Age * .13f) * .1f;
        using var scope = new WorldGraphicsScope(batch);
        for (int side = -1; side <= 1; side += 2)
        {
            var blade = side < 0 ? pose.Left : pose.Right;
            Vector2 hand = GhostSamuraiRigArt.Hand(pose, side) - screen;
            if (front)
            {
                // Every arm carries its own travelling energy, rooted at its hand.
                Vector2 tip = GhostSamuraiRigArt.Tip(pose, side) - screen;
                Flame((hand + tip) * .5f, new Vector2(30 + blade.Charge * 15, Vector2.Distance(hand, tip) * .64f),
                    (tip - hand).ToRotation() - MathHelper.PiOver2, blade.Charge, .46f + blade.Charge * .38f, side);
                Flame(hand, new Vector2(24, 45) * (1 + blade.Charge * .48f), pose.Lean + side * .4f, blade.Charge, .70f, side + 2);
            }
            else
            {
                float lag = MathF.Sin(pose.Age * .057f + side * 2) * 14;
                Flame(root + new Vector2(side * (96 + blade.Charge * 24), 25 + lag),
                    new Vector2(132, 218) * pose.Scale, side * -.26f + pose.Lean, blade.Charge, reduced ? .36f : .74f, side);
                if (!reduced) Flame(root + new Vector2(side * 111, -42 + lag), new Vector2(65, 147), side * -.48f,
                    blade.Charge, .48f, side + 4);
            }
        }
        if (!front) Flame(root + new Vector2(-pose.Lag * 40, 136), new Vector2(123, 191) * pose.Scale,
            pose.Lean, Math.Max(pose.Left.Charge, pose.Right.Charge), .66f, 7);

        // Captures value parameters, not the in-parameter, for allocation-free draw calls.
        void Flame(Vector2 at, Vector2 size, float angle, float charge, float strength, int seed)
        {
            Prepare(poseAge + seed * 31, 3, Vector4.Zero, new(charge, 0, 0, 0), reduced,
                new(1, 1, 1, strength * opacity * pulse));
            Vector2 x = new Vector2(size.X, 0).RotatedBy(angle), y = new Vector2(0, size.Y).RotatedBy(angle);
            Draw(at - x - y, at + x - y, at - x + y, at + x + y, Vector2.Zero, Vector2.One);
        }
    }
    private static void Prepare(float age, float mode, Vector4 shape, Vector4 beat, bool reduced, Vector4 tint)
    {
        material ??= ShaderManager.GetShader("Convergence.SamuraiEnergy");
        material.TrySetParameter("clock", age / 60);
        material.TrySetParameter("mode", mode); material.TrySetParameter("shape", shape);
        material.TrySetParameter("beat", beat); material.TrySetParameter("tint", tint);
        material.TrySetParameter("reduced", reduced ? 1f : 0f);
        material.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height, 0, -1, 1));
        material.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.LinearWrap);
        material.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
    }
    private static void Draw(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 uvMin, Vector2 uvMax)
    {
        for (int i = 0; i < 6; i++)
        {
            int corner = i switch { 0 => 0, 1 => 2, 2 => 1, 3 => 1, 4 => 2, _ => 3 };
            Vector2 p = corner switch { 0 => a, 1 => b, 2 => c, _ => d };
            Vector2 uv = new(corner % 2 == 0 ? uvMin.X : uvMax.X, corner < 2 ? uvMin.Y : uvMax.Y);
            quad[i] = new(new(p, 0), Color.White, uv);
        }
        material!.Apply(); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }
}
