using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.RitualArmamentArt;

namespace Convergence.Client.Encounters.FirstSeverance;

// Existing metal/glass atlas + continuously connected energy; no new bitmap/VFX entities.
internal static class RitualKineticArt
{
    internal static void Seal(SpriteBatch batch, Vector2 center, float radius, float angle, Color color, float opacity)
    {
        if (opacity <= .001f || radius < 1) return;
        FirstSeveranceRaidVfx.WeaponSigil(batch, center, radius, angle, color, opacity, Reduced);
    }

    internal static void Vent(SpriteBatch batch, Vector2 at, Vector2 axis, float after, Color light, bool strong)
    {
        if (after < 0 || after > 16) return;
        float release = RitualKineticMotion.Arrive(after / 2), fade = 1 - RitualKineticMotion.Settle(after / 16);
        FirstSeveranceRaidVfx.Flare(batch, at, after, fade * .7f, light, Reduced, strong ? .85f : .35f);
        FirstSeveranceRaidVfx.Charge(batch, at, axis, after, 0, release * fade, fade * .55f,
            light, Reduced, strong ? .52f : .25f);
        Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 end = at - axis * (40 + release * 95) + normal * side * release * (strong ? 118 : 72);
            Line(batch, at + normal * side * 12, end, Light(light, fade * .7f), (strong ? 12 : 6) * fade);
            Line(batch, end, end - axis * 35 * fade, Light(Ivory, fade * .6f), 3);
        }
    }
}
