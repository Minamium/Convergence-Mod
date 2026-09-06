using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Read-only material for broad attack volumes. The whole marked rectangle is
// dangerous, including its ends; ornament stays INSIDE that exact footprint.
internal static class FirstSeveranceHazardSurface
{
    internal static void Draw(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        Vector2 origin, Vector2 direction, float length, float halfWidth, double age,
        float charge, float emission, float opacity, Color color, bool reduced)
    {
        Vector2 normal = new(-direction.Y, direction.X), end = origin + direction * length;
        float imminent = Window(charge, .70, 1);
        // A dark material underlay gives the same contrast against noon, weather and summons.
        Line(batch, origin, end, new Color(10, 8, 24) * (.24f + charge * .16f) * opacity, halfWidth * 2);
        Line(batch, origin, end, color * (.10f + charge * .09f + emission * .18f) * opacity, halfWidth * 2);
        accents.Ribbon(batch, origin, direction, length, halfWidth * 2, color,
            opacity * (.18f + charge * .20f + emission * .40f));
        int strands = Math.Clamp((int)(halfWidth / (reduced ? 100 : 55)), 3, reduced ? 10 : 24);
        for (int i = 0; i < strands; i++)
        {
            float lateral = (i + .5f) / strands * halfWidth * 2 - halfWidth;
            float flow = Cycle(age, 50 - charge * 20, i * .173);
            float run = length * (.12f + emission * .48f);
            float at = flow * Math.Max(0, length - run);
            Vector2 start = origin + normal * lateral + direction * at;
            float alpha = MathF.Sin(flow * MathF.PI) * opacity;
            accents.Ribbon(batch, start, direction, run, 6 + emission * 13, color, alpha * (.45f + emission * .45f));
            Line(batch, start, start + direction * run, Color.White * alpha * (.20f + emission * .6f), 1.5f + emission * 2);
        }
        // Sliding shutters converge across the face, never misleadingly shrinking the hitbox.
        for (int i = 0; i < (reduced ? 3 : 7); i++)
        {
            float p = Cycle(age, 65, i / (reduced ? 3d : 7d));
            Vector2 point = origin + direction * (p * length);
            float light = MathF.Sin(p * MathF.PI) * opacity * (.3f + imminent * .45f);
            Line(batch, point - normal * halfWidth, point + normal * halfWidth, color * light, 2 + imminent * 2);
        }
        // The continuous filled volume, interior flow and feathered radiance carry
        // the footprint. No black/white rectangle or heavy edge rails.
    }
}
