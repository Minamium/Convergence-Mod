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

    internal static void QueueMagic(LacunaConvergence beam, float age)
    {
        float length = RitualKineticMotion.MagicReach(age);
        if (length <= 0) return;
        Vector2 axis = beam.Axis, normal = axis.RotatedBy(MathHelper.PiOver2);
        float fade = RitualKineticMotion.MagicFade(age), width = RitualKineticMotion.MagicWidth(beam.Empowered);
        Color light = ColorFor(RitualArmamentKind.Magic);
        Span<Vector2> points = stackalloc Vector2[65];
        int layers = Reduced ? 3 : 6;
        for (int layer = 0; layer < layers; layer++)
        {
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1), wave = MathF.Sin(t * 27 - age * .83f + layer * 1.8f);
                points[i] = beam.Muzzle + axis * (length * t) + normal * (wave * (layer == 0 ? 2 : width * .13f)
                    * MathF.Sin(t * MathF.PI));
            }
            if (layer == 0) RitualSurfacePass.Flame(points, width * 1.6f, light, fade * (Reduced ? .7f : .95f));
            else RitualSurfacePass.Ribbon(points, width * (.11f + layer * .013f), Light(layer % 2 == 0 ? Ivory : light, fade * .65f));
        }
    }

    internal static void DrawMagic(SpriteBatch batch, LacunaConvergence beam, float age)
    {
        Vector2 root = beam.Projectile.Center, axis = beam.Axis, muzzle = beam.Muzzle;
        float aim = axis.ToRotation(), charge = RitualKineticMotion.MagicCharge(age);
        float after = age - RitualKineticMotion.MagicFire, fade = RitualKineticMotion.MagicFade(age);
        float collapse = RitualKineticMotion.Strike(after / 4), flash = RitualKineticMotion.Recoil(after);
        Color light = ColorFor(RitualArmamentKind.Magic);
        for (int i = 0; i < 5; i++)
        {
            float arrived = RitualKineticMotion.SigilArrival(age, i);
            var offset = RitualKineticMotion.SigilOffset(age, i);
            Vector2 at = root + new Vector2(offset.X, offset.Y).RotatedBy(aim);
            at = Vector2.Lerp(at, muzzle - axis * 24, collapse);
            float opacity = arrived * (1 - RitualKineticMotion.Settle(after / 9));
            float radius = (beam.Empowered ? 94 : 76) * arrived * (1 - collapse * .70f);
            Seal(batch, at, radius, aim + .2f * MathF.Sin(age * .04f + i) + (1 - arrived) * .9f,
                light, opacity * (Reduced ? .7f : 1));
            Part(batch, 3, at, aim + (1 - arrived) * .9f, radius * .8f, Color.White * opacity * .7f);
            // Flow follows the moving emitter and converges before the release.
            if (arrived > .1f)
            {
                Vector2 control = Vector2.Lerp(at, muzzle, .45f) - axis.RotatedBy(MathHelper.PiOver2) * 44;
                Vector2 prev = at;
                for (int j = 1; j <= 16; j++)
                {
                    float t = j / 16f;
                    Vector2 next = Vector2.Lerp(Vector2.Lerp(at, control, t), Vector2.Lerp(control, muzzle, t), t);
                    Line(batch, prev, next, Light(light, opacity * (.12f + charge * .35f)), 1.5f + charge * 2);
                    prev = next;
                }
            }
        }
        float born = RitualKineticMotion.Arrive(age / 5);
        Seal(batch, muzzle, (100 - charge * 48 + flash * 50) * born, aim + MathHelper.PiOver2,
            light, born * fade * .8f);
        Glow(batch, muzzle, new Vector2(22 + charge * 68 + flash * (beam.Empowered ? 200 : 135)),
            Light(light, born * fade * (.25f + charge * .5f + flash * .7f) * (Reduced ? .6f : 1)));
        if (after < 0) return;
        float length = RitualKineticMotion.MagicReach(age);
        Line(batch, muzzle, muzzle + axis * length, Light(Ivory, fade * .85f), beam.Empowered ? 12 : 7);
        Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
        Line(batch, muzzle - normal * 180 * flash, muzzle + normal * 180 * flash, Light(Ivory, flash), 7 * flash);
        Ring(batch, muzzle + axis * (24 + after * 22), new(12 + after * 3, 75 + after * 9), aim,
            Light(light, flash * .85f), 7 * flash, true);
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
