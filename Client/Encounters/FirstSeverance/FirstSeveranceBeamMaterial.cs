using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Client materials for narrow lasers, combs and broad curtains. Flow changes
// radiance INSIDE the authoritative corridor, never the gameplay footprint.
internal static class FirstSeveranceBeamMaterial
{
    // Bright, compact optics for dense comb teeth and the fine lattice. The
    // entire footprint stays visible; the thin hot filament is not the hitbox.
    internal static void DrawTooth(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        Vector2 origin, Vector2 direction, float length, float halfWidth, double clock,
        float charge, float emission, float opacity, Color color, bool reduced)
    {
        if (Main.dedServ || opacity <= .001f || halfWidth <= 0 || length <= 0) return;
        Vector2 normal = new(-direction.Y, direction.X), end = origin + direction * length;
        float time = (float)(clock % 36000);
        Line(batch, origin, end, new Color(10, 13, 28) * opacity * .46f, halfWidth * 2);
        accents.Ribbon(batch, origin, direction, length, halfWidth * 2, color,
            opacity * (.48f + emission * .45f));
        // A fine pearl spine remains readable at zoom-out even before release.
        Line(batch, origin, end, FirstSeveranceAttackAccents.Neon(color,
            opacity * (.26f + charge * .25f + emission * .34f)), Math.Min(halfWidth, 2 + emission * 3));
        int segments = Math.Clamp((int)(length / (reduced ? 180 : 100)), 2, reduced ? 20 : 32);
        for (int strand = 0; strand < (reduced ? 1 : 2); strand++)
        {
            Vector2 last = origin;
            for (int n = 1; n <= segments; n++)
            {
                float t = n / (float)segments;
                float flow = MathF.Sin(t * 22 - FlowPhase(time, .28f, emission) + strand * MathF.PI);
                Vector2 next = origin + direction * (length * t)
                    + normal * (flow * halfWidth * .42f * MathF.Sin(MathF.PI * t));
                float pulse = .72f + .28f * MathF.Sin(t * 14 - time * .22f + strand);
                Line(batch, last, next, FirstSeveranceAttackAccents.Neon(Color.Lerp(color, Color.White, .65f),
                    opacity * pulse * (.32f + emission * .65f)), Math.Min(halfWidth * .32f, 1.3f + emission * 2.4f));
                last = next;
            }
        }
    }

    // Dense, laminar plasma: continuous occupied volume under fine wavering
    // threads, not a flat slab or a few large bands that resemble safe gaps.
    // Cell masks stop exactly at the rectangle's sides; no boundary rails/bloom.
    internal static void DrawVolume(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        Vector2 origin, Vector2 direction, float length, float halfWidth, double clock,
        float charge, float emission, float opacity, Color color, bool reduced)
    {
        if (Main.dedServ || opacity <= .001f || halfWidth <= 0 || length <= 0) return;
        Vector2 normal = new(-direction.Y, direction.X);
        float time = (float)(clock % 36000);
        int lanes = Math.Max(1, (int)MathF.Ceiling(halfWidth * 2 / 24));
        float pitch = halfWidth * 2 / lanes;
        // Texture draws first, then all pixel filaments, avoiding texture switches
        // for each small segment. ReducedEffects keeps the same visible footprint.
        Line(batch, origin, origin + direction * length, new Color(12, 16, 30) * opacity * .34f, halfWidth * 2);
        for (int lane = 0; lane < lanes; lane++)
        {
            float offset = -halfWidth + (lane + .5f) * pitch;
            float wave = .80f + .20f * MathF.Sin(lane * .73f - time * .055f);
            accents.Ribbon(batch, origin + normal * offset, direction, length, pitch, color,
                opacity * wave * (.30f + charge * .10f + emission * .30f));
        }
        int segments = Math.Clamp((int)(length / (reduced ? 240 : 120)), 2, reduced ? 14 : 28);
        for (int lane = 0; lane < lanes; lane++)
        {
            float center = -halfWidth + (lane + .5f) * pitch;
            Vector2 last = origin + normal * center;
            for (int n = 1; n <= segments; n++)
            {
                float t = n / (float)segments;
                float phase = t * 19 - FlowPhase(time, .17f, emission) + lane * 1.71f;
                float wave = MathF.Sin(phase) + MathF.Sin(phase * 1.37f + time * .031f) * .35f;
                float offset = center + wave * pitch * .16f * MathF.Sin(MathF.PI * t);
                Vector2 next = origin + direction * (length * t) + normal * offset;
                float shimmer = .78f + .22f * MathF.Sin(phase * .73f);
                Color tint = Color.Lerp(color, Color.White, .18f + emission * .40f);
                Line(batch, last, next, FirstSeveranceAttackAccents.Neon(color,
                    opacity * shimmer * (.18f + emission * .32f)), pitch * .38f);
                Line(batch, last, next, FirstSeveranceAttackAccents.Neon(tint,
                    opacity * shimmer * (.38f + charge * .10f + emission * .46f)),
                    Math.Min(pitch * .20f, 1.2f + emission * 2.1f));
                last = next;
            }
        }
    }

    internal static void Draw(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        Vector2 origin, Vector2 direction, float length, float halfWidth, double clock,
        float charge, float emission, float opacity, Color color, bool reduced)
    {
        if (Main.dedServ || opacity <= .001f || halfWidth <= 0 || length <= 0) return;
        Vector2 normal = new(-direction.Y, direction.X);
        float time = (float)(clock % 36000);
        float power = opacity * (.35f + .22f * charge + .43f * emission);
        // Low-density continuous footprint beneath separated, moving plasma bands.
        Line(batch, origin, origin + direction * length, new Color(7, 9, 20) * opacity * .18f, halfWidth * 2);
        accents.Ribbon(batch, origin, direction, length, halfWidth * 2, color, power * .20f);
        int bands = Math.Clamp(3 + (int)(halfWidth / 85), 3, reduced ? 4 : 9);
        int segments = Math.Clamp((int)(length / (reduced ? 180 : 95)), 8, reduced ? 18 : 40);
        for (int band = 0; band < bands; band++)
        {
            float lane = (band + .5f) / bands * 1.68f - .84f;
            Vector2 previous = default;
            for (int n = 0; n <= segments; n++)
            {
                float t = n / (float)segments;
                float phase = t * 17 + band * 2.31f - FlowPhase(time, .15f, emission);
                float turbulence = MathF.Sin(phase) * .105f + MathF.Sin(phase * 1.71f + time * .043f) * .045f;
                float envelope = .28f + .72f * MathF.Sin(MathF.PI * t);
                float offset = Math.Clamp(halfWidth * (lane + turbulence * envelope), -halfWidth * .84f, halfWidth * .84f);
                Vector2 next = origin + direction * (length * t) + normal * offset;
                if (n > 0)
                {
                    Vector2 delta = next - previous;
                    float distance = delta.Length();
                    float breadth = Math.Min(halfWidth * .30f, Math.Max(2, halfWidth * (.14f + .07f * MathF.Sin(phase + .8f))));
                    float shimmer = .67f + .33f * MathF.Sin(phase * .61f + band);
                    Color tint = Color.Lerp(color, Color.White, (band == bands / 2 ? .48f : .12f) * emission);
                    if (distance > .001f)
                    {
                        // Keep all flow segments on the same pixel texture. Alternating
                        // gradient/pixel sprites per segment would split thousands of batches.
                        Line(batch, previous, next, FirstSeveranceAttackAccents.Neon(tint, power * shimmer * .16f), breadth);
                        Line(batch, previous, next, FirstSeveranceAttackAccents.Neon(tint,
                            power * shimmer * (.42f + emission * .33f)), breadth * .56f);
                        Line(batch, previous, next, FirstSeveranceAttackAccents.Neon(tint, power * shimmer * .52f),
                            Math.Min(breadth * .18f, 1.2f + emission * 2.0f));
                    }
                }
                previous = next;
            }
        }
    }

    internal static void CrushMembrane(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        Vector2 center, float halfWidth, float halfHeight, double age, float charge,
        float live, float opacity, Color color, bool reduced)
    {
        Draw(batch, accents, center - Vector2.UnitX * halfWidth, Vector2.UnitX, halfWidth * 2,
            halfHeight, age, charge, live, opacity * .65f, color, reduced);
        int threads = reduced ? 4 : 10;
        for (int i = 0; i < threads; i++)
        {
            float lane = (i + .5f) / threads * 2 - 1;
            Vector2 last = default;
            for (int n = 0; n <= 24; n++)
            {
                float t = n / 24f, bow = MathF.Sin(t * MathF.PI);
                float drift = MathF.Sin(t * 13 + (float)age * .052f + i) * .035f;
                float x = halfWidth * (lane * (1 - charge * .42f * bow) + drift * bow);
                Vector2 next = center + new Vector2(x, (t * 2 - 1) * halfHeight);
                if (n > 0) Line(batch, last, next, FirstSeveranceAttackAccents.Neon(
                    Color.Lerp(color, Color.White, charge * .6f), opacity * (.24f + charge * .45f)),
                    1.2f + charge * 1.3f);
                last = next;
            }
        }
        accents.Halo(batch, center, new Vector2(halfWidth * (1 - live * .7f), halfHeight * 1.4f),
            Color.White, opacity * live * .8f);
    }
}
