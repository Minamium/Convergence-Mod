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
        FirstSeveranceBeamMaterial.DrawVolume(batch, accents, origin, direction, length, halfWidth,
            age, charge, emission, opacity, color, reduced);
    }
}
