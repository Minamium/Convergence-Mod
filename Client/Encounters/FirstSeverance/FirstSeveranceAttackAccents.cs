using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Original code-native light masks, owned by the client presentation. No assets,
// actors, target selection or damage. Premultiplied additive light in AlphaBlend
// lets us keep one batch and leave the safe lane completely untouched.
internal sealed class FirstSeveranceAttackAccents
{
    internal static readonly Color Cyan = new(45, 242, 255);
    internal static readonly Color Magenta = new(255, 58, 131);
    private Texture2D? radial, ribbon;

    internal static Color Neon(Color color, float strength)
        => new Color(color.R, color.G, color.B, 0) * Math.Clamp(strength, 0, 1);

    internal void Halo(SpriteBatch batch, Vector2 center, Vector2 size, Color color, float strength, float angle = 0)
    {
        if (strength <= .001f || Main.dedServ) return;
        EnsureMasks();
        batch.Draw(radial!, center - Main.screenPosition, null, Neon(color, strength), angle,
            new Vector2(64), size / 128f, SpriteEffects.None, 0);
    }

    internal void Ribbon(SpriteBatch batch, Vector2 origin, Vector2 direction, float length,
        float fullWidth, Color color, float strength)
    {
        if (strength <= .001f || fullWidth <= 0 || Main.dedServ) return;
        EnsureMasks();
        batch.Draw(ribbon!, origin - Main.screenPosition, null, Neon(color, strength),
            MathF.Atan2(direction.Y, direction.X), new Vector2(0, 32),
            new Vector2(length / 4f, fullWidth / 64f), SpriteEffects.None, 0);
    }

    internal static void Arc(SpriteBatch batch, Vector2 center, float radius, float start, float span,
        Color color, float width = 2)
    {
        int segments = Math.Clamp((int)(MathF.Abs(span) * radius / 9), 4, 96);
        Vector2 previous = center + Unit(start) * radius;
        for (int i = 1; i <= segments; i++)
        {
            Vector2 next = center + Unit(start + span * i / segments) * radius;
            Line(batch, previous, next, color, width);
            previous = next;
        }
    }

    internal void Marker(SpriteBatch batch, Vector2 center, float radius, bool stack,
        double tick, ulong resolve, int duration, bool reduced)
    {
        if (stack)
        {
            StackMarker(batch, center, radius, tick, resolve, duration, reduced);
            return;
        }
        double start = (double)resolve - duration;
        float born = Window(tick, start, start + 18);
        float progress = Math.Clamp((float)((resolve - tick) / duration), 0, 1);
        float imminent = PreRelease(tick, resolve, 36);
        Color color = stack ? Cyan : Magenta;
        // Fixed danger boundary + a clearly separate smooth countdown inside.
        Ring(batch, center, radius, Color.Black * .9f, 7);
        Ring(batch, center, radius, color * (.8f + imminent * .2f), 2.4f);
        Ring(batch, center, radius - 4, Neon(color, .26f + imminent * .35f), reduced ? 2 : 5);
        float contracting = radius * (.16f + .84f * progress);
        Ring(batch, center, contracting, Neon(color, born * (.5f + imminent * .4f)), 2.2f);
        Arc(batch, center, radius - 12, -MathHelper.PiOver2, MathHelper.TwoPi * progress,
            Color.White * born * .9f, 3);
        // Do not flood the body with bloom. Luminous arcs are distributed along
        // the perimeter; their approaching teeth stop exactly on that perimeter.
        for (int i = 0; i < (reduced ? 4 : 8); i++)
        {
            float angle = i * MathHelper.TwoPi / (reduced ? 4 : 8);
            Vector2 unit = Unit(angle), tangent = new(-unit.Y, unit.X);
            float travel = Cycle(tick - start, 64, i * .125);
            float fade = MathF.Sin(MathF.PI * travel) * born;
            float position = stack ? 1f - travel * .38f : .62f + travel * .38f;
            Vector2 tip = center + unit * (radius * position);
            Vector2 back = tip + unit * (stack ? 15 : -15);
            Line(batch, back - tangent * 8, tip, Neon(color, fade), 3);
            Line(batch, back + tangent * 8, tip, Neon(color, fade), 3);
            float rotation = angle + (float)(tick % 18000) * (stack ? -.009f : .007f);
            Arc(batch, center, radius - 22 - imminent * 6, rotation, .34f,
                Neon(color, born * .8f), 3.5f);
            if (!reduced)
                Halo(batch, center + unit * (radius - 5), new Vector2(75 + imminent * 65), color, born * (.24f + imminent * .34f));
            Vector2 latch = center + unit * (radius + (1 - imminent) * 25);
            Line(batch, latch - tangent * 9, latch + tangent * 9, Color.White * imminent, 3);
        }
        if (stack) Ring(batch, center, 24, color * born, 3);
        else
            for (int i = 0; i < 4; i++)
                Line(batch, center + Unit(i * MathHelper.PiOver2) * 27,
                    center + Unit((i + 1) * MathHelper.PiOver2) * 27, color * born, 3);
    }

    private static void StackMarker(SpriteBatch batch, Vector2 center, float radius,
        double tick, ulong resolve, int duration, bool reduced)
    {
        double start = (double)resolve - duration;
        float born = Window(tick, start, start + 15);
        float imminent = PreRelease(tick, resolve, 36);
        // Exactly one spatial circle: the authority's fixed acceptance radius.
        // No contracting timer circle, rotating outer arcs or circular bloom.
        Ring(batch, center, radius, Color.Black * .95f, 11);
        Ring(batch, center, radius, Cyan * (.86f + .14f * imminent), 5);
        for (int side = 0; side < 4; side++)
        {
            Vector2 unit = Unit(side * MathHelper.PiOver2), tangent = new(-unit.Y, unit.X);
            for (int tooth = 0; tooth < (reduced ? 1 : 2); tooth++)
            {
                float travel = Cycle(tick - start, 64, tooth * .5);
                float alpha = MathF.Sin(MathF.PI * travel) * born;
                Vector2 tip = center + unit * (radius + 12 + (1 - Ease(travel)) * 105);
                Vector2 back = tip + unit * 29;
                Line(batch, back + tangent * 21, tip, Color.Black * alpha, 11);
                Line(batch, back - tangent * 21, tip, Color.Black * alpha, 11);
                Line(batch, back + tangent * 21, tip, Cyan * alpha, 6);
                Line(batch, back - tangent * 21, tip, Cyan * alpha, 6);
                Line(batch, back + tangent * 21, tip, Color.White * alpha * .65f, 1.5f);
                Line(batch, back - tangent * 21, tip, Color.White * alpha * .65f, 1.5f);
            }
        }
        // Countdown becomes a straight readout, never a second spatial boundary.
        float remaining = Math.Clamp((float)((resolve - tick) / duration), 0, 1);
        Vector2 bar = center + new Vector2(-40, 39);
        Line(batch, bar, bar + new Vector2(80, 0), Color.Black * born, 8);
        Line(batch, bar, bar + new Vector2(80 * remaining, 0), Color.White * born, 3);
        for (int i = 0; i < 4; i++)
            Line(batch, center + Unit(i * MathHelper.PiOver2) * 17,
                center + Unit((i + 1) * MathHelper.PiOver2) * 17, Cyan * born, 3);
    }

    // Material fracture/pressure, never a circular cast reticle. Player markers
    // above are the only Raid UI circles.
    internal void ChargeFracture(SpriteBatch batch, Vector2 center, double tick, double start, double fire,
        Color color, bool reduced, float scale = 1)
    {
        float tension = CastTension(tick, start, fire);
        float release = ReleaseImpulse(tick, fire);
        float born = Arrive(tick - start, Math.Min(7, (fire - start) * .2))
            * (1 - Window(tick, fire + 2, fire + 20));
        Halo(batch, center, new Vector2(160 + tension * 90, 60 + tension * 40) * scale,
            color, born * (reduced ? .18f : .34f), -.32f);
        int count = reduced ? 3 : 7;
        for (int i = 0; i < count; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            float arrival = Arrive(tick - start - i * .6, 6);
            float extent = (26 + i * 14) * (1 - tension * .65f) + release * (45 + i * 10);
            Vector2 tip = center + new Vector2(side * extent, (i - count * .5f) * 8 * (1 - tension * .5f)) * scale;
            Vector2 tail = tip + new Vector2(side * (10 + tension * 17), -6 - i * 2) * scale;
            Line(batch, tail, tip, Neon(color, born * arrival * .7f), (1.2f + tension) * scale);
        }
        Halo(batch, center, new Vector2(240, 27) * scale, Color.White,
            release * (reduced ? .12f : .52f), -.32f);
    }

    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

    private void EnsureMasks()
    {
        if (radial is not null) return;
        var pixels = new Color[128 * 128];
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float distance = new Vector2(x - 63.5f, y - 63.5f).Length() / 63.5f;
                pixels[y * 128 + x] = Color.White * MathF.Pow(Math.Max(0, 1 - distance), 2.2f);
            }
        radial = new Texture2D(Main.instance.GraphicsDevice, 128, 128);
        radial.SetData(pixels);
        pixels = new Color[4 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 4; x++)
                pixels[y * 4 + x] = Color.White * MathF.Pow(Math.Max(0, 1 - MathF.Abs(y - 31.5f) / 31.5f), 1.6f);
        ribbon = new Texture2D(Main.instance.GraphicsDevice, 4, 64);
        ribbon.SetData(pixels);
    }

    internal void Unload()
    {
        Texture2D? oldRadial = radial, oldRibbon = ribbon;
        radial = ribbon = null;
        if (oldRadial is not null) Main.QueueMainThreadAction(oldRadial.Dispose);
        if (oldRibbon is not null) Main.QueueMainThreadAction(oldRibbon.Dispose);
    }
}
