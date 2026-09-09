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
        "Convergence/Assets/Textures/Items/RitualArmaments/" + (kind == RitualArmamentKind.Melee ? "" : "V2/") + names[(int)kind])).Value;
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
        float open = p.Open;
        if (open <= 0) return;
        Color light = ColorFor(p.Kind);
        if (p.Kind == RitualArmamentKind.Ranged) DrawBattery(b, p, age, aim, root, open, light);
        else if (p.Kind == RitualArmamentKind.Magic) DrawArchive(b, p, age, aim, root, open, light);
        else if (p.Kind == RitualArmamentKind.Summon)
        {
            Part(b, 7, root, aim + MathHelper.PiOver2, 138, Color.White * open, origin: new(256, 412));
            Ring(b, root - new Vector2(0, 85), new(70, 17), -.2f, Light(light, open), 5);
        }
        else
        {
            float release = 1 - Q(age / 14);
            Part(b, 6, root + aim.ToRotationVector2() * (35 + age * 5), aim + age * .07f,
                130 + age * 4, Light(light, release * .8f));
        }
    }
    private static void DrawBattery(SpriteBatch b, RitualArmamentPose p, float age, float aim, Vector2 root, float open, Color light)
    {
        Vector2 axis = aim.ToRotationVector2(), normal = axis.RotatedBy(MathHelper.PiOver2);
        Ring(b, root - axis * 135, new Vector2(70, 203) * open, aim, Light(light, open * .55f), 5, true);
        Part(b, 7, root + axis * 12, aim + MathHelper.PiOver2, 110, Color.White * open, origin: new(256, 410));
        for (int barrel = 0; barrel < 3; barrel++)
        {
            float sinceFire = age - (4 + barrel * 2);
            float recoil = sinceFire < 0 ? 0 : MathF.Sin(Math.Min(MathF.PI, sinceFire * .24f)) * MathF.Exp(-sinceFire * .13f);
            Vector2 anchor = root + Rotate(RitualArmamentChoreography.CannonRoot(barrel), aim) * (.38f + open * .62f);
            Vector2 body = anchor - axis * recoil * (p.Empowered ? 28 : 15);
            float hatch = .28f + .42f * Q((age + 3) / 7);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 hinge = body + axis * 45 + normal * side * (18 + open * 21);
                Part(b, 1, hinge, aim + side * hatch - MathHelper.PiOver2, 130 * open, Color.White * open, side < 0, new(256, 372));
            }
            Part(b, 0, body, aim, 284 * open, Color.White * open, origin: new(79, 256));
            Vector2 muzzle = anchor + axis * 224;
            Ring(b, muzzle - axis * 6, new(11, 31 + (p.Empowered ? 12 : 0)), aim,
                Light(light, .8f * open), 5);
            float flash = sinceFire < 0 ? .2f * Q((sinceFire + 4) / 4) : 1 - Q(sinceFire / (p.Empowered ? 11 : 7));
            Glow(b, muzzle, new Vector2(p.Empowered ? 116 : 69), Light(light, flash * open));
            Line(b, muzzle, muzzle + axis * flash * (p.Empowered ? 480 : 185), Light(Ivory, flash), p.Empowered ? 19 : 8);
            Line(b, muzzle - normal * 66 * flash, muzzle + normal * 66 * flash, Light(light, flash), 5);
        }
        if (p.Empowered)
            Ring(b, root + axis * 96, new Vector2(27, 215) * (1 + Q(age / 18) * .6f), aim,
                Light(light, open * (1 - Q(age / 22))), 9, true);
    }
    private static void DrawArchive(SpriteBatch b, RitualArmamentPose p, float age, float aim, Vector2 root, float open, Color light)
    {
        Vector2 axis = aim.ToRotationVector2();
        Vector2 book = root + new Vector2(-24, -66);
        float clock = RitualRenderClock.Time;
        int pages = Reduced ? 6 : 10;
        float spread = open * (p.Empowered ? 212 : 170);
        Ring(b, book, new(spread + 18, spread * .53f), -.23f, Light(light, .55f * open), 4, true);
        for (int i = 0; i < pages; i++)
        {
            float orbit = clock * .014f + MathF.Tau * i / pages;
            Vector2 at = book + new Vector2(MathF.Cos(orbit) * spread, MathF.Sin(orbit) * spread * .55f);
            float depth = .72f + .18f * MathF.Sin(orbit);
            Part(b, 3, at, .14f * MathF.Sin(orbit), 76 * depth * open, Color.White * (open * depth));
            Line(b, at, book, Light(light, open * .12f), 2);
        }
        for (int side = -1; side <= 1; side += 2)
            Part(b, 2, book + new Vector2(side * 20 * open, 0), side * (.08f + open * .36f),
                155 * open, Color.White * open, side < 0, new(150, 256));
        Glow(b, book, new(90), Light(light, open * .7f));
        for (int lens = 0; lens < 3; lens++)
        {
            Vector2 at = root + Rotate(RitualArmamentChoreography.LensMuzzle(lens), aim);
            float after = age - 6 - lens * 3;
            float flash = after < 0 ? Q((after + 6) / 6) * .4f : 1 - Q(after / 12);
            float size = (p.Empowered ? 149 : 114) * open;
            Part(b, 3, at, aim, size, Color.White * open);
            Ring(b, at, new(13, size * .43f), aim, Light(light, open * (.6f + flash)), 6);
            Glow(b, at, new Vector2(95 + flash * 70), Light(light, flash));
            if (after >= 0)
                Line(b, at, at + axis * flash * (p.Empowered ? 350 : 135), Light(Ivory, flash), p.Empowered ? 22 : 8);
        }
    }
    internal static void DrawChoir(SpriteBatch b, ChoirSentinel s, Vector2 center, float age)
    {
        float assemble = RitualArmamentChoreography.ChoirAssembly(age);
        float phase = RitualArmamentChoreography.Mod(age, RitualArmamentChoreography.ChoirCycle);
        float release = phase >= 88 ? 1 - Q((phase - 88) / 16) : 0;
        if (s.IsLeader && assemble > .001f)
        {
            Vector2 organ = s.ConcertCenter;
            float size = Math.Min(660, 340 + s.ChoirCount * 29);
            Part(b, 5, organ + new Vector2(0, -37), 0, size * (.82f + assemble * .18f), Color.White * assemble);
            Ring(b, organ, new(size * .48f, size * .20f), -.1f, Light(Gold, assemble * .6f), 6, true);
            Glow(b, organ, new(170, 90), Light(Gold, assemble * .45f));
            if (release > 0)
            {
                float radius = 65 + (1 - release) * 430;
                Ring(b, organ + new Vector2(0, 105), new(radius, radius * .50f), 0, Light(Ivory, release), 16 * release, true);
                Line(b, organ - new Vector2(0, 190), organ + new Vector2(0, 390 * release), Light(Gold, release), 12 * release);
            }
        }
        float breathe = MathF.Sin(age * .036f + s.Ordinal) * .05f;
        Part(b, 4, center, s.Projectile.rotation, 146 + assemble * 46, Color.White);
        for (int side = -1; side <= 1; side += 2)
            Part(b, 1, center + new Vector2(side * (27 + assemble * 20), 8), side * (.15f + assemble * .4f + breathe),
                95, Color.White * .95f, side < 0, new(256, 370));
        Vector2 mouth = center + new Vector2(0, -22);
        Ring(b, mouth, new(25 + assemble * 10, 12 + assemble * 3), 0, Light(Gold, .8f), 4);
        Glow(b, mouth, new(40 + release * 30), Light(Ivory, .3f + release * .6f));
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
            Part(b, 6, center, angle, size, Color.White);
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
        float close = Q((age - RitualArmamentChoreography.VerdictLock) / 12);
        float radius = MathHelper.Lerp(265, RitualArmamentChoreography.VerdictRadius, close);
        float fade = Q(age / 8) * (1 - Q((age - 40) / 16));
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
            RitualSurfacePass.Flame(path, age < RitualArmamentChoreography.VerdictHit ? 26 : 105, ColorFor(RitualArmamentKind.Rogue), fade);
        }
    }
    internal static void DrawVerdict(SpriteBatch b, WitnessVerdict v, float age)
    {
        Vector2 center = v.Projectile.Center;
        float close = Q((age - RitualArmamentChoreography.VerdictLock) / 12);
        float fade = Q(age / 8) * (1 - Q((age - 40) / 16));
        float radius = MathHelper.Lerp(265, RitualArmamentChoreography.VerdictRadius, close);
        Color light = ColorFor(RitualArmamentKind.Rogue);
        for (int i = 0; i < 3; i++)
        {
            var corner = RitualArmamentChoreography.Triangle(i, radius);
            Vector2 at = center + new Vector2(corner.X, corner.Y);
            Part(b, 6, at, MathF.Tau * i / 3 + (1 - close) * .6f, 186, Color.White * fade);
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
    internal static void Unload() { assembly = null; Array.Clear(icons); }
}
