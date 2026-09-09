using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Authored atlas fragments only: no entities, random state or retained frame buffers.
internal static class FirstSeveranceDissolutionVisuals
{
    internal static readonly Vector2 RiftAxis = new Vector2(1, -.62f).SafeNormalize(Vector2.UnitX);

    internal static void Bone(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 position,
        Vector2 pivot, Vector2 scale, float rotation, Color tint, float breakup, float consume,
        Vector2 sink, float time, bool reduced)
    {
        // Unequal cuts preserve the material at rest; each piece releases once,
        // bends away, briefly hangs, then accelerates toward the same rift plane.
        int columns = reduced ? 2 : 3, rows = reduced ? 3 : 4;
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int x = col * source.Width / columns, y = row * source.Height / rows;
                int w = (col + 1) * source.Width / columns - x;
                int h = (row + 1) * source.Height / rows - y;
                float seed = source.X * .013f + source.Y * .007f + col * 2.13f + row * 1.79f;
                float order = .5f + .5f * MathF.Sin(seed * 5.7f);
                float loosen = Window(breakup, order * .42, order * .42 + .40);
                Vector2 local = new Vector2(x + w * .5f, y + h * .5f) - pivot;
                Vector2 basePoint = position + (local * scale).RotatedBy(rotation);
                Vector2 drift = new Vector2(MathF.Sin(seed), MathF.Cos(seed * 1.3f));
                float breath = MathF.Sin(time * (.018f + order * .024f) + seed);
                Vector2 point = basePoint + drift * (12 + 80 * order + breath * 14) * loosen * (reduced ? .35f : 1);
                float pull = MathF.Pow(Window(consume, order * .18, .74 + order * .25), 3);
                Vector2 target = sink + RiftAxis * MathF.Sin(seed * 4) * (210 * (1 - pull));
                point = Vector2.Lerp(point, target, pull);
                float vanish = 1 - Window(pull, .78, 1);
                float twist = rotation + loosen * MathF.Sin(seed + time * .008f) * .38f + pull * MathF.Sin(seed) * 1.6f;
                var patch = new Rectangle(source.X + x, source.Y + y, w, h);
                Vector2 shrinking = scale * (1 - pull * .93f);
                batch.Draw(texture, point - Main.screenPosition, patch, tint * vanish,
                    twist, new Vector2(w, h) * .5f, shrinking, SpriteEffects.None, 0);
                if (!reduced && loosen > .1f && (row + col) % 3 == 0)
                {
                    Vector2 tail = point - drift * (14 + 65 * pull);
                    Line(batch, tail, point, FirstSeveranceAttackAccents.Neon(new Color(187, 159, 234),
                        loosen * vanish * (.12f + pull * .6f)), .7f + pull * 2);
                }
            }
    }

    internal static void Rift(SpriteBatch batch, FirstSeveranceAttackAccents accents, Vector2 center,
        float age, bool reduced)
    {
        Vector2 axis = RiftAxis, normal = new(-axis.Y, axis.X);
        float opening = Window(age, .025, .075);
        float drawIn = Window(age, .28, .78);
        float shut = 1 - Window(age, .785, .81);
        float life = opening * shut;
        float length = (880 + drawIn * 480) * life;
        Color ion = new(202, 154, 255);
        accents.Halo(batch, center, new Vector2(length * .68f, 35 + drawIn * 90), ion,
            life * (reduced ? .16f : .7f), axis.ToRotation());
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float u = i / (float)segments, v = (i + 1f) / segments;
            float taper = MathF.Pow(MathF.Sin((u + v) * .5f * MathF.PI), 1.7f);
            float bend = MathF.Sin(u * 19 + age * 24) * 6 * taper * life;
            Vector2 a = center + axis * ((u - .5f) * length) + normal * bend;
            Vector2 b = center + axis * ((v - .5f) * length) + normal * (MathF.Sin(v * 19 + age * 24) * 6 * taper * life);
            float width = taper * (12 + drawIn * 52) * life;
            Line(batch, a, b, new Color(1, 0, 5) * life, width + 2);
            for (int side = -1; side <= 1; side += 2)
                Line(batch, a + normal * side * width * .5f, b + normal * side * width * .5f,
                    FirstSeveranceAttackAccents.Neon(side < 0 ? ion : Color.White, life * (reduced ? .2f : .85f)), 1.2f + drawIn * 2);
        }
        float flash = Window(age, .785, .797) * (1 - Window(age, .797, .88));
        accents.Halo(batch, center, new Vector2(1300, 130), Color.White, flash * (reduced ? .10f : 1), axis.ToRotation());
        accents.Halo(batch, center, new Vector2(820), ion, flash * (reduced ? .06f : .9f));
    }
}
