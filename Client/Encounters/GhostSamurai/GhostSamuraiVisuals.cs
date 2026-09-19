using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// Client-only articulated sprite rig. Authority clocks still own every attack beat.
internal sealed class GhostSamuraiVisuals : GlobalNPC
{
    // MagicPixel is a texture, not a promise of a 1x1 source image. Scaling its
    // whole surface multiplies every line/rectangle by the asset dimensions.
    internal static readonly Rectangle StrokePixel = new(0, 0, 1, 1);
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (!Main.dedServ && npc.ModNPC is GhostSamuraiBoss boss) GhostSamuraiPresentation.Hit(boss);
    }
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is GhostSamuraiBoss;
    public override bool PreDraw(NPC npc, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (Main.dedServ || npc.ModNPC is not GhostSamuraiBoss boss) return true;
        DrawBoss(batch, boss, screenPos);
        return false;
    }

    private void DrawBoss(SpriteBatch batch, GhostSamuraiBoss boss, Vector2 screen)
    {
        float clock = boss.VisualAge;
        Vector2 root = boss.NPC.Center - screen + new Vector2(0, MathF.Sin(clock * .035f) * 3);
        GhostSamuraiPresentation.Draw(batch, boss, screen);
        if (boss.Attack == SamuraiAttack.FrontalCleaveShockwave && boss.Combo.IsValid(boss.Attack))
        {
            int facing = boss.Combo.Facing;
            Color gaze = boss.Combo.Locked ? new(255, 242, 192) : new(83, 192, 255);
            Vector2 tip = root + new Vector2(facing * 100, -64);
            Stroke(batch, tip - new Vector2(facing * 27, 0), tip, 6, new Color(6, 13, 30));
            Stroke(batch, tip - new Vector2(facing * 27, 0), tip, 2, gaze);
            Stroke(batch, tip - new Vector2(facing * 10, 6), tip, 3, gaze);
            Stroke(batch, tip - new Vector2(facing * 10, -6), tip, 3, gaze);
        }
        if (boss.Attack == SamuraiAttack.Phase2DashSlash && boss.TransitionRemaining == 0)
        {
            float remaining = GhostSamuraiRules.DashApproach + GhostSamuraiRules.DashWarning
                - boss.VisualAttackTimer % GhostSamuraiRules.DashCadence;
            if (remaining > 0 && remaining <= GhostSamuraiRules.DashVisualCueTime)
            {
                // A local-to-body closing ring is the actual dodge cue. The
                // earlier shout only announces preparation; no full-screen flash.
                float progress = 1 - remaining / GhostSamuraiRules.DashVisualCueTime;
                Color cue = remaining <= GhostSamuraiRules.DashAimLockTime
                    ? new Color(255, 248, 201) : new Color(255, 178, 55);
                Ring(batch, root, 128 - progress * 48, 7, new Color(5, 12, 28));
                Ring(batch, root, 128 - progress * 48, 3, cue);
                Stroke(batch, root + new Vector2(-30, -75), root + new Vector2(-10, -75), 4, cue);
                Stroke(batch, root + new Vector2(10, -75), root + new Vector2(30, -75), 4, cue);
            }
        }
        if (boss.TransitionRemaining > 0)
        {
            float t = 1 - boss.TransitionRemaining / (float)GhostSamuraiRules.TransitionTime;
            Ring(batch, root, 75 + t * 145, 3, new Color(65, 135, 224) * (1 - t));
        }

    }

    internal static void Stroke(SpriteBatch batch, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 d = b - a;
        if (!float.IsFinite(d.X) || !float.IsFinite(d.Y) || !float.IsFinite(width) || width <= 0 || d.LengthSquared() < .001f) return;
        batch.Draw(TextureAssets.MagicPixel.Value, a, StrokePixel, color, d.ToRotation(), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }
    internal static void Ring(SpriteBatch batch, Vector2 center, float radius, float width, Color color)
    {
        for (int i = 0; i < 32; i++)
            Stroke(batch, center + (i * MathHelper.TwoPi / 32).ToRotationVector2() * radius,
                center + ((i + 1) * MathHelper.TwoPi / 32).ToRotationVector2() * radius, width, color);
    }
}

internal sealed class GhostSamuraiHazardVisuals : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is GhostSamuraiAttackProjectile;

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (Main.dedServ || projectile.ModProjectile is not GhostSamuraiAttackProjectile p || !p.TryGetAge(out float age)) return true;
        var h = p.DisplayHazard;
        if (age < h.Born || age >= h.End) return false;
        if (h.Shape == SamuraiShape.VerticalSlash && age < h.Fire - SamuraiComboRules.VerticalForecast) return false;
        // A delayed client must not render a stale aimed line as the live strike.
        if (h.HasAim && age >= h.Fire && !p.SlashAim.Locked) return false;
        SpriteBatch batch = Main.spriteBatch;
        Vector2 position = p.VisualCenter(age) - Main.screenPosition;
        bool live = h.Live(age);
        bool reduced = ModContent.GetInstance<Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualConfig>().ReducedEffects;
        Color color = live ? new Color(230, 207, 255) : new Color(171, 108, 240);
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        int viewWidth = (int)MathF.Ceiling(Main.screenWidth / Math.Max(.1f, zoom.X)) + 64;
        int viewHeight = (int)MathF.Ceiling(Main.screenHeight / Math.Max(.1f, zoom.Y)) + 64;
        Rectangle viewport = new((Main.screenWidth - viewWidth) / 2, (Main.screenHeight - viewHeight) / 2, viewWidth, viewHeight);
        if ((h.IsCircle || h.Shape == SamuraiShape.FrontalCleave) && Main.npc[p.BossSlot].ModNPC is GhostSamuraiBoss owner)
        {
            var field = owner.Arena;
            viewport = Rectangle.Intersect(viewport, new Rectangle((int)(field.Left - Main.screenPosition.X),
                (int)(field.Top - Main.screenPosition.Y), (int)(field.HalfWidth * 2), (int)(field.HalfHeight * 2)));
        }
        if (h.Shape == SamuraiShape.FrontalCleave) GhostSamuraiComboVisuals.DrawCleave(batch, h, position, age, p.SlashAim.Locked, viewport, reduced);
        else if (h.Shape == SamuraiShape.GroundShockwave) GhostSamuraiComboVisuals.DrawShock(batch, h, position, age, reduced);
        else if (h.IsCircle) GhostSamuraiCircleVisuals.Draw(batch, h, position, age, viewport, reduced);
        else if (h.Shape == SamuraiShape.SlashWave) GhostSamuraiWaveVisuals.Draw(batch, h, position, age, p.SlashAim.Locked, reduced);
        else if (h.Shape == SamuraiShape.RushVisual) DrawRush(batch, p, position, age, reduced);
        else if (h.Shape == SamuraiShape.Wisp)
        {
            // The outlined core retains the collision radius; the violet flame is decoration.
            GhostSamuraiSpriteArt.DrawSpirit(batch, position, h.Radius);
            GhostSamuraiVisuals.Ring(batch, position, h.Radius, 6, new Color(5, 14, 32) * .9f);
            GhostSamuraiVisuals.Ring(batch, position, h.Radius, 3, color);
            Vector2 heading = new Vector2(p.WispMotion.VX, p.WispMotion.VY).SafeNormalize(new Vector2(h.DX, h.DY));
            GhostSamuraiVisuals.Stroke(batch, position - heading * 30, position, live ? 10 : 5, color * .6f);
            if (!live) GhostSamuraiVisuals.Stroke(batch, position, position + heading * 75, 2, color * .5f);
        }
        else
        {
            Vector2 direction = new(h.DX, h.DY), normal = new(-h.DY, h.DX), end = position + direction * h.Length;
            float progress = Math.Clamp((age - h.Born) / (h.Fire - h.Born), 0, 1);
            // Warm warnings remain distinct from the violet boss/wisps and bright sky.
            // Dark backing provides contrast without flooding the safe cells.
            Color ink = new(6, 13, 30);
            Color edge = live ? new Color(218, 253, 255)
                : Color.Lerp(new Color(255, 180, 58), new Color(255, 235, 160), progress);
            // The stable pale-gold edge identifies the final dodge window without
            // adding a flash or changing the authoritative width.
            if (!live && p.SlashAim.LockTick > h.Born && p.SlashAim.Locked) edge = new Color(255, 245, 198);
            GhostSamuraiVisuals.Stroke(batch, position, end, h.Radius * 2, ink * (live ? .35f : .42f));
            GhostSamuraiVisuals.Stroke(batch, position, end, h.Radius * 2 - 4, (live ? color : edge) * (live ? .38f : .14f + .08f * progress));
            GhostSamuraiVisuals.Stroke(batch, position, end, live ? 7 : 4, ink * .85f);
            GhostSamuraiVisuals.Stroke(batch, position, end, live ? 3 : 2, edge * (live ? 1 : .7f + .3f * progress));
            if (live)
            {
                // Grid hazards lock at birth; the directional slashes track their
                // aim. Use the immutable hazard metadata, never the boss's next state.
                bool grid = h.Shape == SamuraiShape.Slash && p.SlashAim.LockTick == h.Born
                    && (h.Length == GhostSamuraiRules.GridWidth || h.Length == GhostSamuraiRules.GridHeight)
                    && h.Radius == GhostSamuraiRules.GridHalfWidth;
                GhostSamuraiSlashArt.Strip(batch, grid ? SamuraiSlashArt.Grid : SamuraiSlashArt.Normal,
                    position, end, h.Radius * 2 - 8,
                    GhostSamuraiSlashArt.Energy(age, h.Fire, h.End) * (reduced ? .55f : grid ? .82f : 1));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                // Borders are inset: their outer edge is the authoritative half-width.
                Vector2 offset = normal * (h.Radius - 2) * side;
                GhostSamuraiVisuals.Stroke(batch, position + offset, end + offset, 4, ink * .95f);
                GhostSamuraiVisuals.Stroke(batch, position + offset, end + offset, 2, edge);
            }
            GhostSamuraiVisuals.Stroke(batch, position - normal * h.Radius, position + normal * h.Radius, 2, edge);
            GhostSamuraiVisuals.Stroke(batch, end - normal * h.Radius, end + normal * h.Radius, 2, edge);
        }
        return false;
    }

    private static void DrawRush(SpriteBatch batch, GhostSamuraiAttackProjectile p, Vector2 start, float age, bool reduced)
    {
        var h = p.DisplayHazard;
        Vector2 d = new(h.DX, h.DY), n = new(-h.DY, h.DX), end = start + d * h.Length;
        bool live = h.Live(age);
        Color ink = new(6, 13, 30), edge = p.SlashAim.Locked ? new(255, 245, 198) : new(255, 180, 58);
        if (!live)
        {
            // Only the route is forecast. No filled slash-shaped damage field.
            float width = Math.Abs(n.X) * GhostSamuraiRules.BodyWidth / 2 + Math.Abs(n.Y) * GhostSamuraiRules.BodyHeight / 2;
            GhostSamuraiVisuals.Stroke(batch, start, end, 6, ink);
            GhostSamuraiVisuals.Stroke(batch, start, end, 2, edge);
            for (int side = -1; side <= 1; side += 2)
            for (float along = 0; along < h.Length; along += 60)
            {
                Vector2 a = start + d * along + n * width * side;
                Vector2 b = a + d * Math.Min(30, h.Length - along);
                GhostSamuraiVisuals.Stroke(batch, a, b, 5, ink * .8f);
                GhostSamuraiVisuals.Stroke(batch, a, b, 2, edge * .8f);
            }
        }
        else
        {
            float t = (age - h.Fire) / (h.End - h.Fire);
            Color flash = new Color(213, 241, 255) * (1 - t);
            // Large single blade flash stays translucent and unbordered: unlike
            // the solid outlined grid, this art is entirely harmless.
            GhostSamuraiVisuals.Stroke(batch, start, end, h.Radius * .6f, flash * .15f);
            GhostSamuraiVisuals.Stroke(batch, start, end, 7, flash * .65f);
            GhostSamuraiSlashArt.Strip(batch, SamuraiSlashArt.Dash, start, end, h.Radius,
                (1 - t) * (reduced ? .25f : .55f));
            Vector2 body = GhostSamuraiAttackProjectile.RushCenter(h, age) - Main.screenPosition;
            // The brighter wake follows the authoritative rush center. The full
            // route remains dim decoration; only the boss body owns rush damage.
            GhostSamuraiSlashArt.Strip(batch, SamuraiSlashArt.Dash, body - d * 240, body,
                80, (1 - t) * (reduced ? .45f : .85f));
        }
    }
}
