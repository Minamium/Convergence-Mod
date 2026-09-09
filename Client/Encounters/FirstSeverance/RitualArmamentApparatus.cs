#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// Four distinct assemblies share material, not a single recolored weapon sprite.
internal static class RitualArmamentArt
{
    private static Asset<Texture2D>? assembly;
    private static readonly Asset<Texture2D>?[] icons = new Asset<Texture2D>?[5];
    private static readonly Asset<Texture2D>?[] grandArt = new Asset<Texture2D>?[5];
    private static readonly string[] names = { "NullCantorClaws", "PaleMeridian", "LacunaTestament", "ChoirOfTheUnmade", "LastWitness" };
    internal static readonly Color Gold = new(207, 167, 104);
    internal static readonly Color Ivory = new(232, 225, 218);
    internal static bool Reduced => ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
    internal static Color ColorFor(RitualArmamentKind kind) => kind switch
    {
        RitualArmamentKind.Ranged => new(90, 198, 255), RitualArmamentKind.Magic => new(166, 101, 255),
        RitualArmamentKind.Rogue => new(181, 110, 247), RitualArmamentKind.Summon => new(231, 187, 116),
        _ => new(148, 88, 255),
    };
    internal static Color Light(Color c, float power) => NullCantorClawArt.Light(c, power);
    internal static Texture2D Icon(RitualArmamentKind kind) => (icons[(int)kind] ??= ModContent.Request<Texture2D>(
        "Convergence/Assets/Textures/Items/RitualArmaments/" + (kind == RitualArmamentKind.Melee ? "" : "V3/") + names[(int)kind])).Value;
    internal static void Relic(SpriteBatch batch, RitualArmamentKind kind, Vector2 center, float angle, float size, Color tint)
    {
        var texture = (grandArt[(int)kind] ??= ModContent.Request<Texture2D>(
            "Convergence/Assets/Textures/Items/RitualArmaments/V3/" + names[(int)kind] + "_Apparatus")).Value;
        batch.Draw(texture, center - Main.screenPosition, null, tint, angle, texture.Size() * .5f,
            size / texture.Width, SpriteEffects.None, 0);
    }
    internal static void Part(SpriteBatch b, int part, Vector2 center, float angle, float size, Color tint,
        bool mirror = false, Vector2? origin = null)
    {
        assembly ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/Items/RitualArmaments/V2/ReliquaryAssemblies");
        Rectangle src = new(part % 4 * 512, part / 4 * 512, 512, 512);
        b.Draw(assembly.Value, center - Main.screenPosition, src, tint, angle, origin ?? new Vector2(256),
            size / 512, mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
    }
    internal static void Line(SpriteBatch b, Vector2 a, Vector2 z, Color c, float width) => NullCantorClawArt.Beam(b, a, z, width, c);
    internal static void Ring(SpriteBatch b, Vector2 c, Vector2 r, float rotation, Color tint, float width, bool broken = false)
        => NullCantorClawArt.Ring(b, c, r, rotation, width, tint, broken);
    internal static void Glow(SpriteBatch b, Vector2 c, Vector2 radius, Color tint) => NullCantorClawArt.Glow(b, c, radius, tint);
    private static Vector2 Rotate(System.Numerics.Vector2 p, float angle) => new Vector2(p.X, p.Y).RotatedBy(angle);
    private static float Q(float t) => RitualArmamentChoreography.Smooth(t);
    internal static void QueueFlight(Projectile p, Vector2 head)
    {
        if (p.ModProjectile is not (RitualBolt or WitnessBlade) || p.ModProjectile is RitualBolt { Age: < 0 }) return;
        Span<Vector2> path = stackalloc Vector2[64]; int n = 0;
        for (int i = Math.Min(60, p.oldPos.Length - 1); i >= p.MaxUpdates; i--)
        {
            if (p.oldPos[i] == Vector2.Zero) continue;
            Vector2 point = p.oldPos[i] + p.Size * .5f;
            if (n > 0 && Vector2.DistanceSquared(path[n - 1], point) > 700 * 700) { n = 0; continue; }
            if (n == 0 || Vector2.DistanceSquared(path[n - 1], point) > .1f) path[n++] = point;
        }
        if (n < 1) return;
        path[n++] = head;
        var kind = p.ModProjectile is RitualBolt bolt ? bolt.Kind : RitualArmamentKind.Rogue;
        bool strong = p.ModProjectile is RitualBolt { Empowered: true } or WitnessBlade { Stealth: true };
        float width = p.ModProjectile is WitnessBlade ? (strong ? 136 : 90) : strong ? 65 : kind == RitualArmamentKind.Magic ? 42 : 28;
        float fade = Math.Min(1, p.timeLeft / (12f * p.MaxUpdates));
        RitualSurfacePass.Flame(path[..n], width, ColorFor(kind), fade * (Reduced ? .68f : .94f));
    }
    internal static void DrawApparatus(SpriteBatch b, RitualArmamentPose p, float age, float aim, Vector2 root)
    {
        float open = RitualKineticMotion.Arrive(RitualRenderClock.Sample(p.LifeAge) / 3)
            * (1 - Q((age - p.Duration - 18) / 24));
        if (open <= 0) return;
        Color light = ColorFor(p.Kind);
        if (p.Kind == RitualArmamentKind.Summon)
        {
            Relic(b, RitualArmamentKind.Summon, root - new Vector2(0, 25), -.10f, 138, Color.White * open);
            Ring(b, root - new Vector2(0, 85), new(70, 17), -.2f, Light(light, open), 5);
        }
        else
        {
            float release = 1 - Q(age / 14), snap = RitualKineticMotion.Arrive(age / 3);
            Part(b, 6, root + aim.ToRotationVector2() * (35 + snap * 120), aim + snap * .8f,
                75 + snap * 145, Light(light, release * .8f));
            RitualKineticArt.Vent(b, root, aim.ToRotationVector2(), age, light, p.Empowered);
        }
    }
    internal static void DrawFlight(SpriteBatch b, Projectile p, Vector2 center, float age)
    {
        RitualArmamentKind kind = p.ModProjectile is RitualBolt bolt ? bolt.Kind : RitualArmamentKind.Rogue;
        Color light = ColorFor(kind);
        bool strong = p.ModProjectile is RitualBolt { Empowered: true } or WitnessBlade { Stealth: true };
        if (p.ModProjectile is WitnessBlade witness)
        {
            float size = (witness.Stealth ? 225 : 158) * (.64f + .36f * Q(age / 9));
            float angle = p.rotation + RitualRenderClock.Fraction * .22f;
            Relic(b, RitualArmamentKind.Rogue, center, angle, size, Color.White);
            Ring(b, center, new(size * .43f), -angle, Light(light, .6f), 4, true);
            Glow(b, center, new(size * .33f), Light(light, .5f));
            return;
        }
        if (p.ModProjectile is RitualBolt { Age: < 0 })
        { Ring(b, center, new(10, 33), p.rotation, Light(light, .8f), 4); return; }
        Vector2 axis = RitualArmamentItems.Aim(p.velocity, 1), normal = axis.RotatedBy(MathHelper.PiOver2);
        float length = kind == RitualArmamentKind.Ranged ? (strong ? 156 : 90) : (strong ? 116 : 75);
        float life = Math.Min(1, p.timeLeft / (12f * p.MaxUpdates));
        Glow(b, center, new(strong ? 52 : 30), Light(light, life * .9f));
        Line(b, center - axis * length, center, new Color(10, 3, 20, 230) * life, strong ? 24 : 13);
        Line(b, center - axis * length, center + axis * 12, Light(light, life), strong ? 15 : 8);
        Line(b, center - axis * length * .83f, center + axis * 7, Light(Ivory, life), strong ? 5 : 3);
        if (strong)
            for (int side = -1; side <= 1; side += 2)
                Line(b, center - axis * 54 + normal * side * 19, center - axis * 8, Light(light, life), 4);
    }
    internal static void QueueVerdict(WitnessVerdict v, float age)
    {
        float close = RitualKineticMotion.VerdictClosure(age);
        float radius = RitualKineticMotion.VerdictRadius(age);
        float fade = RitualKineticMotion.Arrive(age / 5) * (1 - Q((age - 40) / 16));
        Span<Vector2> path = stackalloc Vector2[49];
        for (int edge = 0; edge < 3; edge++)
        {
            Vector2 a = v.Projectile.Center + Rotate(RitualArmamentChoreography.Triangle(edge, radius), 0);
            Vector2 z = v.Projectile.Center + Rotate(RitualArmamentChoreography.Triangle(edge + 1, radius), 0);
            Vector2 n = (z - a).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < path.Length; i++)
            {
                float t = i / (float)(path.Length - 1);
                path[i] = Vector2.Lerp(a, z, t) + n * MathF.Sin(t * MathF.PI) * (1 - close) * MathF.Sin(age * .1f + edge) * 32;
            }
            float impact = RitualKineticMotion.Recoil(age - RitualArmamentChoreography.VerdictHit);
            RitualSurfacePass.Flame(path, 26 + impact * 110, ColorFor(RitualArmamentKind.Rogue), fade * (Reduced ? .65f : 1));
        }
    }
    internal static void DrawVerdict(SpriteBatch b, WitnessVerdict v, float age)
    {
        Vector2 center = v.Projectile.Center;
        float close = RitualKineticMotion.VerdictClosure(age);
        float fade = RitualKineticMotion.Arrive(age / 5) * (1 - Q((age - 40) / 16));
        float radius = RitualKineticMotion.VerdictRadius(age);
        Color light = ColorFor(RitualArmamentKind.Rogue);
        for (int i = 0; i < 3; i++)
        {
            var corner = RitualArmamentChoreography.Triangle(i, radius);
            Vector2 at = center + new Vector2(corner.X, corner.Y);
            float jitter = MathF.Sin(age * 3.7f + i * 2) * 1.8f
                * RitualKineticMotion.Arrive((age - 16) / 1.5f) * (1 - Q((age - 21) / 2));
            at += new Vector2(jitter, -jitter);
            Part(b, 6, at, MathF.Tau * i / 3 + (1 - close) * .6f, 186, Color.White * fade);
            RitualKineticArt.Seal(b, at, 80, -i * .32f + (1 - close) * .2f, light, fade * .55f);
            Glow(b, at, new(64), Light(light, fade));
        }
        Ring(b, center, new(radius * .74f), age * .012f, Light(light, fade * .40f), 4, true);
        if (age >= RitualArmamentChoreography.VerdictHit)
            DrawImpact(b, center, age - RitualArmamentChoreography.VerdictHit, RitualArmamentKind.Rogue, true);
    }
    internal static void DrawImpact(SpriteBatch b, Vector2 center, float age, RitualArmamentKind kind, bool strong)
    {
        if (age < 0 || age > 32) return;
        Color color = ColorFor(kind);
        float fade = MathF.Pow(1 - age / 32, 2), flash = 1 - Q(age / 8), expand = Q(age / 23);
        float reach = strong ? 235 : 91;
        Glow(b, center, new(reach * .66f * flash), Light(color, flash));
        Ring(b, center, new Vector2(18 + reach * expand, 12 + reach * expand * .55f), -.25f,
            Light(color, fade), (strong ? 16 : 7) * fade, true);
        Line(b, center - new Vector2(reach * flash, 0), center + new Vector2(reach * flash, 0), Light(Ivory, flash), strong ? 12 : 5);
        if (strong) Line(b, center - new Vector2(0, reach * flash * 1.6f), center + new Vector2(0, reach * flash * 1.6f), Light(color, flash), 8);
        int count = Reduced ? 7 : strong ? 27 : 12;
        for (int i = 0; i < count; i++)
        {
            Vector2 axis = (i * 2.399963f).ToRotationVector2();
            Vector2 at = center + axis * reach * expand * (.45f + i % 4 * .17f);
            Line(b, at, at + axis * (24 * fade), Light(i % 3 == 0 ? Ivory : color, fade), i % 3 == 0 ? 4 : 2);
        }
    }
    internal static void Unload() { assembly = null; Array.Clear(icons); Array.Clear(grandArt); }
}
