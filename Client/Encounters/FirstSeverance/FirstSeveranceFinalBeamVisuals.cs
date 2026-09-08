using System;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Presentation only: never interpolate to another pulse's axis/offset while the
// current authority pulse can still hurt. Fractional time animates its material.
internal static class FirstSeveranceFinalBeamVisuals
{
    internal static void Draw(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        FirstSeveranceCombatProjection combat, double renderTick, ulong authorityTick, bool reduced)
    {
        if (Main.dedServ || authorityTick < combat.ActionStartedTick) return;
        double age = authorityTick - combat.ActionStartedTick;
        int cadence = FirstSeveranceScoreGeometry.SlicerCadence(combat.ActionIndex);
        int fire = FirstSeveranceScoreGeometry.SlicerFire(combat.ActionIndex);
        int end = FirstSeveranceScoreGeometry.SlicerEnd(combat.ActionIndex);
        double local = age % cadence;
        // Stay below the next tick so material cannot announce firing/end early.
        double clock = local + Math.Clamp(renderTick - authorityTick, 0, .999);
        foreach (var item in FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,
            combat.ActionIndex, age, combat.CoreX, combat.CoreY))
        {
            var ray = item.Ray;
            Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY);
            Vector2 normal = new(-direction.Y, direction.X), tip = origin + direction * ray.Length;
            Color color = item.Pulse % 2 == 0 ? new(65, 232, 255) : new(190, 143, 255);
            bool warning = local < fire;
            float charge = Window(clock, 0, fire);
            float fade = 1 - Window(clock, end, cadence);
            float power = warning ? .68f + .20f * charge : item.Live ? 1 : .09f * fade;
            // Same narrow plasma material as the opening pursuit lances, with
            // a readable full-width footprint. All width changes below are light,
            // never the 56px collision corridor or its 136px spacing.
            FirstSeveranceBeamMaterial.Draw(batch, accents, origin, direction, ray.Length, ray.HalfWidth,
                clock, charge, item.Live ? 1 : 0, power, color, reduced);
            Line(batch, origin, tip, FirstSeveranceAttackAccents.Neon(color,
                warning ? .13f + charge * .06f : item.Live ? .32f : 0), ray.HalfWidth * 2);
            float flare = item.Live ? 1 - Window(clock, fire, fire + 3) : 0;
            accents.Ribbon(batch, origin, direction, ray.Length, ray.HalfWidth * 2,
                Color.White, flare * (reduced ? .22f : .58f));
            // Pursuit-style converging filaments accelerate at release, without
            // side rails, arrows, or endpoint halos spilling across safe lanes.
            int strands = reduced ? 3 : 5;
            float speed = .10f + Window(clock, fire - 5, fire) * .65f;
            for (int fiber = 0; fiber < strands; fiber++)
            {
                Vector2 previous = origin;
                for (int segment = 1; segment <= 32; segment++)
                {
                    float t = segment / 32f;
                    float phase = t * 23 - (float)clock * speed + fiber * 2.4f;
                    float offset = (MathF.Sin(phase) * .28f + fiber / (float)strands - .5f)
                        * ray.HalfWidth * (.15f + .55f * MathF.Sin(MathF.PI * t));
                    Vector2 next = origin + direction * (ray.Length * t) + normal * offset;
                    Line(batch, previous, next, FirstSeveranceAttackAccents.Neon(
                        fiber == 0 ? Color.White : color, power * (item.Live ? .85f : .28f)),
                        fiber == 0 && item.Live ? 4.5f : 1.5f);
                    previous = next;
                }
            }
            // Compact aperture grows within this tooth, not a giant edge lens.
            accents.Halo(batch, origin + direction * 14, new Vector2(24 + charge * 20, ray.HalfWidth * 1.8f),
                color, power, direction.ToRotation());
        }
    }
}
