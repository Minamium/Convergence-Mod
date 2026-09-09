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
        Ring(batch, center, new(radius, radius * .72f), angle, Light(color, opacity), 2.5f, true);
        Ring(batch, center, new(radius * .78f, radius * .56f), angle + .08f, Light(Ivory, opacity * .6f), 1.5f);
        int marks = Reduced ? 8 : 16;
        for (int i = 0; i < marks; i++)
        {
            float a = i * MathF.Tau / marks;
            Vector2 Local(float r, float offset = 0) => new Vector2(MathF.Cos(a + offset) * r,
                MathF.Sin(a + offset) * r * .72f).RotatedBy(angle) + center;
            Line(batch, Local(radius * .81f), Local(radius * .96f), Light(color, opacity * .75f), 2);
            if (!Reduced && i % 2 == 0)
            {
                Line(batch, Local(radius * .90f, -.045f), Local(radius * .90f, .045f), Light(Ivory, opacity * .6f), 2);
                Line(batch, Local(radius * .54f), Local(radius * .72f, .07f), Light(color, opacity * .38f), 1.5f);
            }
        }
    }

    internal static void Vent(SpriteBatch batch, Vector2 at, Vector2 axis, float after, Color light, bool strong)
    {
        if (after < 0 || after > 16) return;
        float release = RitualKineticMotion.Arrive(after / 2), fade = 1 - RitualKineticMotion.Settle(after / 16);
        Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 end = at - axis * (40 + release * 95) + normal * side * release * (strong ? 118 : 72);
            Line(batch, at + normal * side * 12, end, Light(light, fade * .7f), (strong ? 12 : 6) * fade);
            Line(batch, end, end - axis * 35 * fade, Light(Ivory, fade * .6f), 3);
        }
    }
}
