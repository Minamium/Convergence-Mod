#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.FirstSeverance;

// Actual P3 material, at native atlas resolution, not a quantized concept thumbnail.
// Sources are borrowed; only the small procedural light textures are owned here.
internal static class NullCantorClawArt
{
    private static Asset<Texture2D>? rig, icon;
    private static Texture2D? feather, glow;
    private static readonly Rectangle PalmSource = new(960, 792, 294, 301);
    private static readonly Rectangle BoneSource = new(786, 300, 98, 226);
    private static readonly Rectangle TalonSource = new(960, 195, 110, 388);
    private static readonly Rectangle ArmSource = new(488, 30, 174, 604);
    internal static readonly Color Violet = new(148, 88, 255);
    internal static readonly Color Pale = new(231, 211, 255);
    internal static readonly Color Gold = new(213, 178, 121);
    internal static bool Reduced => ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
    internal static Texture2D Icon => (icon ??= ModContent.Request<Texture2D>(
        "Convergence/Assets/Textures/Items/RitualArmaments/NullCantorClaws")).Value;
    private static Texture2D Rig => (rig ??= ModContent.Request<Texture2D>(
        "Convergence/Assets/Textures/NPCs/NullCantorRigAtlas")).Value;
    internal static Color Light(Color c, float strength) => new Color(c.R, c.G, c.B, 0) * Math.Max(0, strength);
    private static void EnsureLight()
    {
        if (feather is not null || Main.dedServ) return;
        feather = new Texture2D(Main.graphics.GraphicsDevice, 8, 64);
        Color[] pixels = new Color[8 * 64];
        for (int y = 0; y < 64; y++)
        {
            float v = MathF.Pow(Math.Max(0, 1 - Math.Abs((y - 31.5f) / 31.5f)), 1.4f);
            for (int x = 0; x < 8; x++) pixels[y * 8 + x] = Color.White * v;
        }
        feather.SetData(pixels);
        glow = new Texture2D(Main.graphics.GraphicsDevice, 96, 96);
        pixels = new Color[96 * 96];
        for (int y = 0; y < 96; y++)
            for (int x = 0; x < 96; x++)
            {
                float r = new Vector2(x - 47.5f, y - 47.5f).Length() / 47.5f;
                pixels[y * 96 + x] = Color.White * MathF.Pow(Math.Max(0, 1 - r), 2);
            }
        glow.SetData(pixels);
    }
    internal static void Beam(SpriteBatch b, Vector2 a, Vector2 z, float width, Color c)
    {
        Vector2 d = z - a;
        if (d.LengthSquared() < .01f || width <= 0) return;
        EnsureLight();
        b.Draw(feather!, (a + z) * .5f - Main.screenPosition, null, c, d.ToRotation(),
            new Vector2(4, 32), new Vector2((d.Length() + 1) / 8, width / 64), SpriteEffects.None, 0);
    }
    internal static void Glow(SpriteBatch b, Vector2 p, Vector2 size, Color c)
    {
        EnsureLight();
        b.Draw(glow!, p - Main.screenPosition, null, c, 0, new Vector2(48), size / 48, SpriteEffects.None, 0);
    }
    internal static void Ring(SpriteBatch b, Vector2 p, Vector2 radius, float rotation, float width, Color color, bool broken = false)
    {
        int n = Reduced ? 48 : 96;
        Vector2 last = p + new Vector2(radius.X, 0).RotatedBy(rotation);
        for (int i = 1; i <= n; i++)
        {
            float a = MathF.Tau * i / n;
            Vector2 next = p + new Vector2(MathF.Cos(a) * radius.X, MathF.Sin(a) * radius.Y).RotatedBy(rotation);
            if (!broken || i % 12 < 9) Beam(b, last, next, width, color);
            last = next;
        }
    }
    internal static Vector2 Transform(NVector p, Vector2 root, float angle, int facing)
    {
        p.Y *= facing; p = NullCantorClawMotion.Rotate(p, angle);
        return root + new Vector2(p.X, p.Y);
    }
    private static void Material(SpriteBatch b, Rectangle source, Vector2 a, Vector2 z, float width, Color color, bool mirror)
    {
        Vector2 d = z - a;
        b.Draw(Rig, a - Main.screenPosition, source, color, d.ToRotation() - MathHelper.PiOver2,
            new Vector2(source.Width * .5f, 0), new Vector2(width / source.Width, d.Length() / source.Height),
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
    }
    internal static void Hand(SpriteBatch b, in CantorClawPose pose, Vector2 root, float aim, int facing, float opacity, bool arm = true, float armOpacity = 1)
    {
        Vector2 palm = Transform(pose.Palm, root, aim, facing);
        Color material = new Color(244, 235, 224) * opacity;
        if (arm)
        {
            Vector2 elbow = Vector2.Lerp(root, palm, .50f) + (palm - root).SafeNormalize(Vector2.UnitX)
                .RotatedBy(MathHelper.PiOver2) * (22 * pose.Scale * pose.Mirror * facing);
            Material(b, ArmSource, root, elbow, 27 * pose.Scale, material * armOpacity, pose.Mirror < 0);
            Material(b, BoneSource, elbow, palm, 34 * pose.Scale, material * armOpacity, pose.Mirror < 0);
        }
        Glow(b, palm, new Vector2(90, 73) * pose.Scale, Light(Violet, .85f * opacity));
        for (int finger = 4; finger >= 0; finger--)
            for (int part = 0; part < 3; part++)
            {
                var s = NullCantorClawMotion.Segment(pose, finger, part);
                Vector2 a = Transform(s.Start, root, aim, facing), z = Transform(s.End, root, aim, facing);
                Material(b, part == 2 ? TalonSource : BoneSource, a, z, s.Radius * 2.7f, material, pose.Mirror * facing < 0);
                Beam(b, a, z, 2.3f * pose.Scale, Light(Pale, .45f * opacity));
                if (part < 2) Glow(b, z, new Vector2(8 * pose.Scale), Light(Gold, .8f * opacity));
                else Glow(b, z, new Vector2(13, 8) * pose.Scale, Light(Color.White, opacity));
            }
        b.Draw(Rig, palm - Main.screenPosition, PalmSource, material, pose.Angle * facing + aim,
            PalmSource.Size() * .5f, 78 * pose.Scale / PalmSource.Width, SpriteEffects.None, 0);
        // A negative-space aperture remains readable even through bright crescents.
        Glow(b, palm, new Vector2(32 * pose.Scale), new Color(0, 0, 4, 255) * opacity);
        Ring(b, palm, new Vector2(19 * pose.Scale), 0, 4 * pose.Scale, Light(Pale, opacity));
    }
    internal static void QueueSwipeTrail(NullCantorClawSwipe p, float age)
    {
        float progress = Math.Clamp(age / p.Duration, 0, 1);
        if (progress < NullCantorClawMotion.SweepStart) return;
        float fade = 1 - NullCantorClawMotion.Smooth((progress - .74f) / .22f);
        Vector2 root = Main.player[p.Projectile.owner].MountedCenter;
        int count = Reduced ? 40 : 96;
        Span<Vector2> points = stackalloc Vector2[96];
        // No clamped run of coincident points: resample only the actual swept arc.
        float end = Math.Min(progress, NullCantorClawMotion.SweepEnd);
        float begin = Math.Max(NullCantorClawMotion.SweepStart, end - .32f);
        if (end <= begin) return;
        for (int finger = 0; finger < 5; finger++)
        {
            for (int i = 0; i < count; i++)
            {
                float sample = MathHelper.Lerp(begin, end, i / (float)(count - 1));
                var pose = NullCantorClawMotion.SwingPose(sample, p.Hand);
                points[i] = Transform(NullCantorClawMotion.Joint(pose, finger, 3), root, p.Aim, p.Facing);
            }
            RitualSurfacePass.Flame(points[..count], (Reduced ? 74 : 108) * NullCantorClawMotion.Smooth((end - begin) / .14f), Violet, fade);
        }
    }
    internal static void DrawSwipe(SpriteBatch b, NullCantorClawSwipe p, float age)
    {
        float progress = Math.Clamp(age / p.Duration, 0, 1);
        Vector2 root = Main.player[p.Projectile.owner].MountedCenter;
        for (int pass = 0; pass < 2; pass++)
        {
            int hand = pass == 0 ? 1 - p.Hand : p.Hand;
            var pose = RitualArmamentChoreography.PresentedHand(progress, p.Hand, hand, p.Aim, p.Facing, RitualRenderClock.Time);
            Hand(b, pose, root, 0, 1, 1, hand == p.Hand, NullCantorClawMotion.Envelope(progress, .12f, .82f, 1));
        }
        float flash = NullCantorClawMotion.Envelope(progress, .20f, .70f, .98f);
        if (!Reduced && progress > .20f) Shards(b, root, age, p.Projectile.identity, p.Aim, flash, 28, 370);
        if (p.HasImpact) Impact(b, p.Impact, age - p.ImpactAge, p.Projectile.identity, false);
    }
    internal static void DrawCrush(SpriteBatch b, NullCantorClawCrush p, float age)
    {
        Vector2 center = p.Projectile.Center;
        float emerge = NullCantorClawMotion.CrushArrival(age);
        float close = NullCantorClawMotion.CrushClosure(age);
        float fade = 1 - NullCantorClawMotion.Smooth((age - 22) / 20);
        // Clearly locate the actual strike ellipse before the remote hands close.
        if (age < NullCantorClawMotion.CrushImpactTick)
        {
            Ring(b, center, new(NullCantorClawMotion.CrushRadiusX, NullCantorClawMotion.CrushRadiusY), 0,
                6, Light(Pale, .65f * emerge), true);
            Glow(b, center, new Vector2(200 - 145 * close), new Color(2, 0, 8, 230) * emerge);
            for (int layer = 0; layer < (Reduced ? 2 : 4); layer++)
            {
                float a = layer * .65f + age * .015f;
                Ring(b, center, new Vector2(270 - close * 220, 82 - close * 58), a,
                    6 + layer, Light(Violet, emerge * .8f), true);
            }
            for (int i = 0; i < (Reduced ? 12 : 36); i++)
            {
                float a = i * 2.399963f + age * .025f;
                float travel = (age * .048f + i * .13f) % 1;
                Vector2 axis = a.ToRotationVector2();
                Vector2 at = center + axis * (55 + (1 - travel) * 460);
                Beam(b, at, at - axis * (16 + 75 * travel), 4 * (1 - travel), Light(Pale, emerge * travel));
            }
        }
        for (int hand = 0; hand < 2; hand++)
        {
            var pose = NullCantorClawMotion.CrushPose(age, hand);
            Vector2 wrist = center + new Vector2(hand == 0 ? -600 : 600, 10).RotatedBy(NullCantorClawMotion.CrushTilt);
            Ring(b, wrist, new(26, 160 * emerge * fade), NullCantorClawMotion.CrushTilt, 12, Light(Violet, .9f * fade));
            // The wrist comes from a slit; fingers remain independent physical parts.
            Vector2 palm = center + new Vector2(pose.Palm.X, pose.Palm.Y);
            Material(b, ArmSource, wrist, palm, 75 * emerge, new Color(237, 226, 211) * fade, hand != 0);
            if (age >= NullCantorClawMotion.CrushCloseTick && age < NullCantorClawMotion.CrushEndHitTick + 2)
            {
                for (int echo = 4; echo > 0; echo--)
                {
                    var before = NullCantorClawMotion.CrushPose(Math.Max(0, age - echo * 1.2f), hand);
                    for (int finger = 0; finger < 5; finger++)
                    {
                        Vector2 a = Transform(NullCantorClawMotion.Joint(before, finger, 3), center, 0, 1);
                        Vector2 z = Transform(NullCantorClawMotion.Joint(pose, finger, 3), center, 0, 1);
                        Beam(b, a, z, 38 * fade, Light(Violet, .7f * fade / echo));
                        Beam(b, a, z, 6 * fade, Light(Color.White, .85f * fade / echo));
                    }
                }
            }
            Hand(b, pose, center, 0, 1, emerge * fade, false);
        }
        if (age >= NullCantorClawMotion.CrushImpactTick)
            Impact(b, center, age - NullCantorClawMotion.CrushImpactTick, p.Projectile.identity, true);
    }
    internal static void Impact(SpriteBatch b, Vector2 center, float age, int seed, bool crush)
    {
        float duration = crush ? 32 : 18;
        if (age < 0 || age >= duration) return;
        float t = age / duration, fade = (1 - t) * (1 - t);
        float release = 1 - MathF.Exp(-age * .23f);
        float snap = 1 - NullCantorClawMotion.Smooth(age / (crush ? 9 : 5));
        float extent = crush ? 610 : 150;
        Glow(b, center, new Vector2((crush ? 310 : 125) * snap), Light(Violet, snap * 1.5f));
        Beam(b, center - Vector2.UnitY * extent * snap, center + Vector2.UnitY * extent * snap,
            (crush ? 62 : 24) * snap, Light(Pale, snap));
        Beam(b, center - Vector2.UnitX * extent * snap, center + Vector2.UnitX * extent * snap,
            13 * snap, Light(Color.White, snap));
        Ring(b, center, new Vector2(extent * release, extent * .55f * release), -.23f,
            (crush ? 24 : 9) * fade, Light(Violet, fade), true);
        Ring(b, center, new Vector2(extent * .82f * release, extent * .36f * release), .28f,
            7 * fade, Light(Color.White, fade), true);
        Shards(b, center, age, seed, 0, fade, Reduced ? 8 : crush ? 44 : 18, extent * release);
        if (crush)
        {
            Glow(b, center, new Vector2(75 * fade), new Color(0, 0, 4, 255) * fade);
            Ring(b, center, new Vector2(29 * fade), 0, 6 * fade, Light(Color.White, fade));
        }
    }
    private static void Shards(SpriteBatch b, Vector2 center, float age, int seed, float rotation, float opacity, int count, float extent)
    {
        for (int i = 0; i < count; i++)
        {
            float a = i * 2.399963f + seed * .37f + rotation;
            float f = .30f + (i * .618034f % 1) * .70f;
            Vector2 axis = a.ToRotationVector2();
            Vector2 at = center + axis * extent * f;
            Beam(b, at - axis * 24 * opacity, at, 5 * opacity, Light(i % 3 == 0 ? Gold : Pale, opacity));
            if (i % 2 == 0)
                b.Draw(Rig, at - Main.screenPosition, TalonSource, new Color(50, 38, 66) * opacity,
                    a + age * .10f, TalonSource.Size() * .5f, new Vector2(.12f, .05f) * (1 + f), SpriteEffects.None, 0);
        }
    }
    internal static void Dispose()
    {
        feather?.Dispose(); glow?.Dispose(); feather = glow = null; rig = icon = null;
    }
}
