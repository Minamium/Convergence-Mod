#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// Borrowed textures belong to ReLogic. All motion/light is disposable;
// neither a trailing blade nor a lens decides whether a target is hit.
internal static class RitualArmamentArt
{
    private static readonly Asset<Texture2D>?[] textures = new Asset<Texture2D>?[6];
    internal static readonly Color Ivory = new(232, 224, 208);
    internal static readonly Color Gold = new(177, 145, 97);
    internal static readonly Color Ice = new(134, 208, 227);
    internal static bool Reduced => ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
    private static readonly string[] Names = { "NullRefrain", "PaleMeridian", "LacunaTestament", "ChoirOfTheUnmade", "LastWitness", "ChoirSentinel" };
    // Pivots were measured in the approved concept crops, independently of export resolution.
    private static readonly Vector2[] DesignSizes = { new(279, 49), new(274, 57), new(110, 116), new(175, 43), new(100, 109), new(54, 100) };
    internal static Texture2D Texture(int row) => (textures[row] ??= ModContent.Request<Texture2D>(
        "Convergence/Assets/Textures/Items/RitualArmaments/" + Names[row])).Value;
    internal static void Unload() => Array.Clear(textures);
    internal static Rectangle Source(int row) => Texture(row).Bounds;
    internal static Color ColorFor(RitualArmamentKind kind) => kind switch
    {
        RitualArmamentKind.Ranged => Ice,
        RitualArmamentKind.Magic => new(196, 181, 229),
        RitualArmamentKind.Rogue => new(219, 191, 152),
        _ => Ivory,
    };
    internal static void Sprite(SpriteBatch batch, int row, Vector2 world, float angle, float length,
        Color tint, bool flip = false, Vector2? pivot = null)
    {
        Rectangle source = Source(row);
        batch.Draw(Texture(row), world - Main.screenPosition, source, tint, angle,
            pivot is { } measured ? measured * source.Size() / DesignSizes[row] : source.Size() * .5f,
            length / source.Width, flip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
    }
    internal static void Blade(SpriteBatch batch, Vector2 world, float angle, float length, Color tint, float open)
    {
        Texture2D image = Texture(0);
        float scale = length / image.Width;
        Vector2 origin = new(42, 26), hinge = new(68, image.Height * .5f);
        Rectangle root = new(0, 0, 68, image.Height);
        batch.Draw(image, world - Main.screenPosition, root, tint, angle, origin, scale, SpriteEffects.None, 0);
        Vector2 pivot = world + ((hinge - origin) * scale).RotatedBy(angle);
        for (int side = -1; side <= 1; side += 2)
        {
            int y = side < 0 ? 0 : image.Height / 2;
            int height = side < 0 ? image.Height / 2 : image.Height - y;
            Rectangle plate = new(68, y, image.Width - 68, height);
            batch.Draw(image, pivot - Main.screenPosition, plate, tint, angle + side * open * .045f,
                hinge - new Vector2(plate.X, plate.Y), scale, SpriteEffects.None, 0);
        }
    }
    internal static void Line(SpriteBatch batch, Vector2 a, Vector2 b, Color color, float width)
        => FirstSeveranceBossVisuals.Line(batch, a, b, color, Math.Max(.2f, width));
    internal static Color Light(Color color, float amount) => FirstSeveranceAttackAccents.Neon(color, amount);
    internal static void Ellipse(SpriteBatch batch, Vector2 center, Vector2 radii, float rotation, Color color, float width)
    {
        Vector2 last = center + new Vector2(radii.X, 0).RotatedBy(rotation);
        for (int i = 1; i <= 32; i++)
        {
            float angle = MathF.Tau * i / 32;
            Vector2 next = center + new Vector2(MathF.Cos(angle) * radii.X, MathF.Sin(angle) * radii.Y).RotatedBy(rotation);
            Line(batch, last, next, color, width); last = next;
        }
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class RitualArmamentItemVisuals : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is IRitualArmament;
    public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position,
        Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        int row = (int)((IRitualArmament)item.ModItem).Kind;
        Rectangle source = RitualArmamentArt.Source(row);
        spriteBatch.Draw(RitualArmamentArt.Texture(row), position, source, Color.White, row < 2 ? -.20f : 0,
            source.Size() * .5f, Math.Min(1, scale) * 54 / Math.Max(source.Width, source.Height), SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor,
        Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        int row = (int)((IRitualArmament)item.ModItem).Kind;
        float time = Main.GlobalTimeWrappedHourly;
        Vector2 center = item.Center + new Vector2(0, MathF.Sin(time * 1.7f + whoAmI) * 4);
        RitualArmamentArt.Sprite(spriteBatch, row, center, row < 2 ? -.18f : .04f * MathF.Sin(time),
            row < 2 ? 110 : 58, Color.Lerp(lightColor, Color.White, .75f));
        RitualArmamentArt.Ellipse(spriteBatch, center + new Vector2(0, 40), new(33, 6), 0,
            RitualArmamentArt.Light(RitualArmamentArt.Gold, .24f), 1);
        return false;
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class RitualArmamentProjectileVisuals : GlobalProjectile
{
    private long frameStamp;
    private bool sounded, impactSeen;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        => projectile.ModProjectile is NullRefrainSlash or RitualBolt or WitnessBlade or ChoirSentinel or RitualArmamentPose;
    private float Fraction => Main.gamePaused ? 0 : (float)Math.Clamp(
        (Stopwatch.GetTimestamp() - frameStamp) * 60d / Stopwatch.Frequency, 0, 1);
    public override void PostAI(Projectile projectile)
    {
        if (projectile.numUpdates == 0) frameStamp = Stopwatch.GetTimestamp();
        if (projectile.ModProjectile is NullRefrainSlash slash && !sounded && slash.Progress >= .24f)
        {
            sounded = true;
            RitualWeaponFeedback.Sound(slash.Combo == 2 ? "BladeSweep" : "BladeUnsheathe", projectile.Center, slash.Combo == 2 ? .48f : .32f);
        }
        if (projectile.ModProjectile is RitualArmamentPose pose && !sounded)
        {
            sounded = true;
            string cue = pose.Kind switch
            {
                RitualArmamentKind.Ranged => pose.Empowered ? "CoreSalvoFire" : "LanceFire",
                RitualArmamentKind.Magic => "SpreadExecution",
                RitualArmamentKind.Summon => "CoreExposure",
                _ => "BladeUnsheathe",
            };
            RitualWeaponFeedback.Sound(cue, projectile.Center, pose.Empowered ? .40f : .24f);
        }
        if (projectile.ModProjectile is ChoirNote && !sounded)
        { sounded = true; RitualWeaponFeedback.Sound("SpreadExecution", projectile.Center, .13f); }
    }
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (impactSeen) return;
        impactSeen = true;
        RitualArmamentKind kind = Kind(projectile);
        ModContent.GetInstance<RitualWeaponFeedback>().Burst(target.Center, kind, projectile.owner == Main.myPlayer,
            projectile.ModProjectile is NullRefrainSlash { Combo: 2 } || projectile.ai[2] > .5f);
    }
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        // Ordinary projectile death supplies a harmless remote endpoint cue.
        if (!impactSeen && projectile.ModProjectile is RitualBolt)
            ModContent.GetInstance<RitualWeaponFeedback>().Burst(projectile.Center, Kind(projectile), false, false);
    }
    private static RitualArmamentKind Kind(Projectile p) => p.ModProjectile switch
    {
        RitualBolt bolt => bolt.Kind,
        WitnessBlade => RitualArmamentKind.Rogue,
        ChoirSentinel => RitualArmamentKind.Summon,
        RitualArmamentPose pose => pose.Kind,
        _ => RitualArmamentKind.Melee,
    };
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        SpriteBatch batch = Main.spriteBatch;
        float fraction = Fraction;
        switch (projectile.ModProjectile)
        {
            case NullRefrainSlash slash: DrawSlash(batch, projectile, slash, fraction); break;
            case RitualArmamentPose pose: DrawPose(batch, projectile, pose, fraction); break;
            case ChoirSentinel sentinel: DrawSentinel(batch, projectile, sentinel, fraction); break;
            default: DrawFlight(batch, projectile, fraction); break;
        }
        return false;
    }
    private static void DrawSlash(SpriteBatch batch, Projectile p, NullRefrainSlash slash, float fraction)
    {
        float progress = Math.Clamp(slash.Progress + fraction / slash.Duration, 0, 1);
        float angle = slash.AngleAt(progress), reach = slash.Reach;
        float fade = 1 - RitualArmamentRules.Smooth((progress - .83f) / .17f);
        Color color = slash.Combo == 2 ? RitualArmamentArt.Gold : RitualArmamentArt.Ivory;
        Vector2 center = Main.player[p.owner].MountedCenter;
        int echoes = RitualArmamentArt.Reduced ? 3 : 9;
        for (int i = echoes; i > 0 && progress >= .24f; i--)
        {
            float sample = Math.Max(.24f, progress - i * .014f);
            RitualArmamentArt.Sprite(batch, 0, center, slash.AngleAt(sample), reach + 50,
                RitualArmamentArt.Light(color, fade * (1 - i / (float)(echoes + 1)) * .24f),
                slash.Facing < 0, new Vector2(42, 26));
        }
        // Analytic curved ribbons connect the fractional trailing blade poses.
        if (progress >= .24f)
            for (int lane = 0; lane < (RitualArmamentArt.Reduced ? 2 : 5); lane++)
            {
                Vector2 previous = center;
                for (int n = 0; n <= 28; n++)
                {
                    float t = n / 28f;
                    float sample = Math.Clamp(progress - .24f + .24f * t, .24f, .76f);
                    float radius = reach * (.60f + lane * .075f);
                    Vector2 next = center + slash.AngleAt(sample).ToRotationVector2() * radius;
                    if (n > 0) RitualArmamentArt.Line(batch, previous, next,
                        RitualArmamentArt.Light(Color.Lerp(color, Color.White, t), fade * t * .8f), (slash.Combo == 2 ? 5 : 3) * t);
                    previous = next;
                }
            }
        float open = RitualArmamentRules.Envelope(progress, .22f, .72f, .95f);
        RitualArmamentArt.Blade(batch, center, angle, reach + 50, Color.White * fade,
            open * (slash.Combo == 2 ? 1 : .28f));
        Vector2 along = angle.ToRotationVector2();
        RitualArmamentArt.Line(batch, center + along * 62, center + along * (reach - 15),
            RitualArmamentArt.Light(color, open * .65f), 1.5f);
    }
    private static void DrawPose(SpriteBatch batch, Projectile p, RitualArmamentPose pose, float fraction)
    {
        float age = pose.Age + fraction, progress = age / pose.Duration;
        float recoil = MathF.Exp(-age * .30f) * MathF.Sin(Math.Min(MathF.PI, age * .6f));
        float open = RitualArmamentRules.Envelope(age, 3, pose.Duration * .50f, pose.Duration);
        float fade = 1 - RitualArmamentRules.Smooth((progress - .83f) / .17f);
        Vector2 axis = p.rotation.ToRotationVector2(), normal = new(-axis.Y, axis.X);
        Vector2 hand = Main.player[p.owner].MountedCenter - axis * recoil * (pose.Empowered ? 12 : 6);
        bool flip = axis.X < 0;
        Color light = RitualArmamentArt.ColorFor(pose.Kind);
        if (pose.Kind == RitualArmamentKind.Ranged)
        {
            RitualArmamentArt.Sprite(batch, 1, hand, p.rotation, 176, Color.White * fade, flip, new(56, 30));
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = hand + axis * 30 + normal * side * (7 + open * 8);
                RitualArmamentArt.Line(batch, root, root + axis * 90, RitualArmamentArt.Light(light, open * .6f), 1.2f);
            }
            float flash = 1 - RitualArmamentRules.Smooth(age / 5);
            Vector2 muzzle = hand + axis * 136;
            RitualArmamentArt.Line(batch, muzzle - normal * flash * 18, muzzle + normal * flash * 18,
                RitualArmamentArt.Light(Color.White, flash), 2);
            RitualArmamentArt.Line(batch, muzzle, muzzle + axis * flash * (pose.Empowered ? 155 : 65),
                RitualArmamentArt.Light(light, flash), pose.Empowered ? 5 : 2);
        }
        else if (pose.Kind == RitualArmamentKind.Magic)
        {
            Vector2 book = hand + axis * 44 + new Vector2(0, -8 * open);
            RitualArmamentArt.Sprite(batch, 2, book, p.rotation * .18f, 55, Color.White * fade);
            for (int i = 0; i < 3; i++)
            {
                Vector2 lens = book + axis * (30 + i * (19 + open * 11));
                float radius = 21 - i * 3 + MathF.Sin(age * .20f - i) * 2;
                RitualArmamentArt.Ellipse(batch, lens, new(5 + open * 5, radius), p.rotation,
                    RitualArmamentArt.Light(light, open * (.8f - i * .13f)), 1.4f);
                RitualArmamentArt.Line(batch, lens - normal * radius, lens + normal * radius,
                    RitualArmamentArt.Light(Color.White, open * .20f), 3);
            }
        }
        else if (pose.Kind == RitualArmamentKind.Summon)
        {
            RitualArmamentArt.Sprite(batch, 3, hand, p.rotation - recoil * .1f, 135, Color.White * fade, flip, new(12, 22));
            RitualArmamentArt.Ellipse(batch, hand + axis * 110, new(12 + open * 13, 24 + open * 12), p.rotation,
                RitualArmamentArt.Light(light, open * .8f), 1.5f);
        }
        else
        {
            float release = 1 - RitualArmamentRules.Smooth(age / 10);
            RitualArmamentArt.Sprite(batch, 4, hand + axis * (20 + age * 2), p.rotation + age * .20f, 54,
                RitualArmamentArt.Light(light, release * .48f));
        }
    }
    private static void DrawSentinel(SpriteBatch batch, Projectile p, ChoirSentinel sentinel, float fraction)
    {
        float age = sentinel.Age + fraction;
        Vector2 center = p.Center + p.velocity * fraction;
        float clock = (age - p.minionPos * 7) % RitualArmamentRules.ChoirPeriod;
        if (clock < 0) clock += RitualArmamentRules.ChoirPeriod;
        float gather = RitualArmamentRules.Smooth(Math.Clamp((clock - 23) / 13, 0, 1));
        float flash = 1 - RitualArmamentRules.Smooth(clock / 7);
        Color tint = RitualArmamentArt.Ivory;
        RitualArmamentArt.Sprite(batch, 5, center, p.rotation, 46, Color.White);
        float width = 18 + gather * 12 - flash * 5;
        RitualArmamentArt.Ellipse(batch, center + new Vector2(0, -6), new(width, 8), -.18f,
            RitualArmamentArt.Light(tint, .3f + gather * .45f), 1.4f);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 a = center + new Vector2(side * (20 + gather * 8), -21);
            Vector2 b = center + new Vector2(side * (27 + gather * 12), 13);
            RitualArmamentArt.Line(batch, a, b, RitualArmamentArt.Light(RitualArmamentArt.Gold, .4f + gather * .4f), 1.4f);
        }
    }
    private static void DrawFlight(SpriteBatch batch, Projectile p, float fraction)
    {
        RitualArmamentKind kind = Kind(p);
        Color color = RitualArmamentArt.ColorFor(kind);
        Vector2 center = p.Center + p.velocity * (fraction * p.MaxUpdates);
        float opacity = Math.Min(1, p.timeLeft / (16f * p.MaxUpdates));
        Vector2 last = center;
        int step = RitualArmamentArt.Reduced ? 2 : 1;
        for (int i = 0; i < p.oldPos.Length; i += step)
        {
            if (p.oldPos[i] == Vector2.Zero) break;
            Vector2 at = p.oldPos[i] + p.Size * .5f;
            if (Vector2.DistanceSquared(last, at) > 400 * 400) break;
            float t = 1 - i / (float)p.oldPos.Length;
            RitualArmamentArt.Line(batch, last, at, RitualArmamentArt.Light(color, opacity * t * .17f), 18 * t);
            RitualArmamentArt.Line(batch, last, at, RitualArmamentArt.Light(color, opacity * t * .72f), 2.5f * t);
            last = at;
        }
        if (p.ModProjectile is WitnessBlade witness)
        {
            color = witness.Returning ? RitualArmamentArt.Gold : RitualArmamentArt.Ivory;
            int echoes = RitualArmamentArt.Reduced ? 2 : 5;
            for (int i = echoes; i > 0 && i < p.oldPos.Length; i--)
                if (p.oldPos[i] != Vector2.Zero)
                    RitualArmamentArt.Sprite(batch, 4, p.oldPos[i] + p.Size * .5f, p.oldRot[i], witness.Stealth ? 90 : 68,
                        RitualArmamentArt.Light(color, opacity * (1 - i / (float)(echoes + 1)) * .20f));
            RitualArmamentArt.Sprite(batch, 4, center, p.rotation + fraction * .22f, witness.Stealth ? 90 : 68, Color.White * opacity);
            return;
        }
        if (p.ModProjectile is RitualBolt bolt && bolt.Age < 0)
        {
            RitualArmamentArt.Ellipse(batch, center, new(5, 19), p.rotation,
                RitualArmamentArt.Light(color, .60f), 1.5f);
            return;
        }
        Vector2 direction = RitualArmamentItems.Aim(p.velocity, 1), normal = new(-direction.Y, direction.X);
        float length = kind == RitualArmamentKind.Ranged ? (p.ai[2] > .5f ? 108 : 70) : 46;
        RitualArmamentArt.Line(batch, center - direction * length, center, new Color(13, 13, 18) * opacity, 10);
        RitualArmamentArt.Line(batch, center - direction * length, center, RitualArmamentArt.Light(color, opacity), 4);
        RitualArmamentArt.Line(batch, center - direction * length * .8f, center, RitualArmamentArt.Light(Color.White, opacity), 1.4f);
        for (int side = -1; side <= 1; side += 2)
            RitualArmamentArt.Line(batch, center - direction * 22 + normal * side * 7, center,
                RitualArmamentArt.Light(color, opacity * .85f), 1.2f);
        if (kind is RitualArmamentKind.Melee or RitualArmamentKind.Rogue)
            RitualArmamentArt.Sprite(batch, kind == RitualArmamentKind.Melee ? 0 : 4, center,
                p.rotation, kind == RitualArmamentKind.Melee ? 86 : 30, Color.White * opacity);
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class RitualWeaponFeedback : ModSystem
{
    private sealed class Impact(Vector2 position, Color color, bool strong)
    { internal readonly Vector2 Position = position; internal readonly Color Color = color; internal readonly bool Strong = strong; internal int Age; }
    private readonly List<Impact> impacts = new(32);
    private readonly List<ReLogic.Utilities.SlotId> voices = new(24);
    private long stamp;
    private float shake;
    internal static void Sound(string name, Vector2 at, float volume)
    {
        if (Main.dedServ || Main.gameMenu) return;
        var voice = SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + name)
        {
            Identifier = "Convergence:RitualWeapon:" + name,
            Volume = volume, PitchVariance = .06f, MaxInstances = 2,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        }, at);
        var system = ModContent.GetInstance<RitualWeaponFeedback>();
        system.voices.RemoveAll(id => !SoundEngine.TryGetActiveSound(id, out var sound) || !sound.IsPlaying);
        system.voices.Add(voice);
    }
    internal void Burst(Vector2 at, RitualArmamentKind kind, bool local, bool strong)
    {
        if (Main.dedServ || Main.gameMenu) return;
        // Same-frame hits coalesce spatially rather than covering one target in flashes.
        foreach (var existing in impacts)
            if (existing.Age <= 1 && Vector2.DistanceSquared(existing.Position, at) < 72 * 72) return;
        if (impacts.Count == 32) impacts.RemoveAt(0);
        impacts.Add(new Impact(at, RitualArmamentArt.ColorFor(kind), strong));
        Sound(strong ? "CoreHit" : "PylonHit", at, strong ? .30f : .18f);
        if (local && strong) shake = Math.Max(shake, 3.5f);
    }
    public override void PostUpdateEverything()
    {
        stamp = Stopwatch.GetTimestamp(); shake *= .72f;
        for (int i = impacts.Count - 1; i >= 0; i--) if (++impacts[i].Age >= 24) impacts.RemoveAt(i);
    }
    public override void ModifyScreenPosition()
    {
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        if (!Main.gameMenu && config.ScreenShake && !config.ReducedEffects)
            Main.screenPosition += new Vector2(MathF.Sin((float)Main.GameUpdateCount * 2.1f), MathF.Cos((float)Main.GameUpdateCount * 2.7f)) * shake;
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || impacts.Count == 0) return;
        var batch = Main.spriteBatch;
        float fraction = Main.gamePaused ? 0 : (float)Math.Clamp((Stopwatch.GetTimestamp() - stamp) * 60d / Stopwatch.Frequency, 0, 1);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (var hit in impacts)
            {
                float age = hit.Age + fraction, t = age / 24, fade = (1 - t) * (1 - t);
                float spread = 1 - MathF.Exp(-age * .19f);
                float extent = hit.Strong ? 105 : 57;
                int count = RitualArmamentArt.Reduced ? 4 : 9;
                for (int i = 0; i < count; i++)
                {
                    Vector2 axis = (i * 2.399963f).ToRotationVector2();
                    Vector2 point = hit.Position + axis * (extent * spread);
                    RitualArmamentArt.Line(batch, point, point + axis * (18 * fade),
                        RitualArmamentArt.Light(hit.Color, fade * .8f), 1.4f);
                }
                float snap = 1 - RitualArmamentRules.Smooth(age / 7);
                RitualArmamentArt.Line(batch, hit.Position - Vector2.UnitX * extent * snap,
                    hit.Position + Vector2.UnitX * extent * snap, RitualArmamentArt.Light(Color.White, snap * .8f), 2);
            }
        }
        finally { batch.End(); }
    }
    public override void OnWorldUnload()
    {
        impacts.Clear(); shake = 0;
        foreach (var id in voices) if (SoundEngine.TryGetActiveSound(id, out var sound)) sound.Stop();
        voices.Clear();
    }
    public override void Unload() { OnWorldUnload(); RitualArmamentArt.Unload(); }
}
