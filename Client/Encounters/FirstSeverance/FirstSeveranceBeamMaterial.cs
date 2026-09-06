using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;

namespace Convergence.Client.Encounters.FirstSeverance;

// One client material for narrow lasers, combs and broad curtains. Flow changes
// radiance INSIDE the authoritative corridor, never the gameplay footprint.
internal static class FirstSeveranceBeamMaterial
{
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
                float phase = t * 17 + band * 2.31f - time * (.072f + emission * .11f);
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
