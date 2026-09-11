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
    internal static void Draw(SpriteBatch batch, FirstSeveranceEmissionVisuals emissions,
        FirstSeveranceCombatProjection combat, double renderTick, ulong authorityTick, bool reduced)
    {
        if (Main.dedServ || authorityTick < combat.ActionStartedTick) return;
        double age = authorityTick - combat.ActionStartedTick;
        // Stay below the next tick so material cannot announce firing/end early.
        double clock = age + Math.Clamp(renderTick - authorityTick, 0, .999);
        foreach (var item in FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,
            combat.ActionIndex, age, combat.CoreX, combat.CoreY, combat.ActionStartedTick))
        {
            var ray = item.Ray;
            Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY);
            int reveal = FirstSeveranceScoreGeometry.SlicerReveal(item.Pulse);
            int fire = FirstSeveranceScoreGeometry.SlicerFire(combat.ActionIndex)
                + item.Pulse * FirstSeveranceScoreGeometry.SlicerCadence(combat.ActionIndex);
            emissions.DrawPrismRay(batch, ray, clock, (ulong)reveal, (ulong)fire,
                (ulong)(fire + FirstSeveranceLanceTuning.PatternActiveTicks), item.Live,
                FirstSeveranceEmissionVisuals.PrismColor(item.Pulse), reduced);
        }
    }
}
