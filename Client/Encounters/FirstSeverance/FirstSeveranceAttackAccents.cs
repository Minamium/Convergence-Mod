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
        double start = (double)resolve - duration;
        float born = Arrive(tick - start, 12);
        float remaining = Math.Clamp((float)((resolve - tick) / Math.Max(1, duration)), 0, 1);
        FirstSeveranceRaidVfx.MechanicRing(batch, center, radius, tick, remaining, born, stack, reduced);
        if (!stack) return;
        // Inward guidance is outside the ONE true acceptance circle; no heavy
        // brackets, black disks, center diamonds or second timer circumference.
        Color color = new(137, 196, 211);
        for (int side = 0; side < 4; side++)
        {
            Vector2 unit = Unit(side * MathHelper.PiOver2), tangent = new(-unit.Y, unit.X);
            float travel = Cycle(tick - start, 64, side * .08);
            float alpha = MathF.Sin(MathF.PI * travel) * born * .8f;
            Vector2 tip = center + unit * (radius + 8 + (1 - Ease(travel)) * (reduced ? 34 : 60));
            Vector2 back = tip + unit * 14;
            Line(batch, back + tangent * 9, tip, Color.Black * alpha, 3.8f);
            Line(batch, back - tangent * 9, tip, Color.Black * alpha, 3.8f);
            Line(batch, back + tangent * 9, tip, color * alpha, 1.8f);
            Line(batch, back - tangent * 9, tip, color * alpha, 1.8f);
        }
    }

    // Material fracture/pressure, never a circular cast reticle. Player markers
    // above are the only Raid UI circles.
    internal void ChargeFracture(SpriteBatch batch, Vector2 center, double tick, double start, double fire,
        Color color, bool reduced, float scale = 1)
    {
        float born=Arrive(tick-start,6)*(1-Window(tick,fire+2,fire+20));
        FirstSeveranceRaidVfx.Charge(batch,center,new Vector2(1,-.18f).SafeNormalize(Vector2.UnitX),
            tick-start,CastTension(tick,start,fire),ReleaseImpulse(tick,fire),born,color,reduced,scale);
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
