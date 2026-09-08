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
            combat.ActionIndex, age, combat.CoreX, combat.CoreY, combat.ActionStartedTick))
        {
            var ray = item.Ray;
            Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY);
            Color color = item.Pulse % 2 == 0 ? new(65, 232, 255) : new(190, 143, 255);
            bool warning = local < fire;
            float charge = Window(clock, 0, fire);
            float fade = 1 - Window(clock, end, cadence);
            float power = warning ? .75f + .25f * Window(clock, 0, 6) : item.Live ? 1 : .10f * fade;
            // Exact lattice tooth material and width; no extra wide plasma overlay.
            FirstSeveranceBeamMaterial.DrawTooth(batch, accents, origin, direction, ray.Length, ray.HalfWidth,
                clock, charge, item.Live ? 1 : 0, power, color, reduced);
        }
    }
}
