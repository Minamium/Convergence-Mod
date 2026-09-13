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
    // This GlobalNPC is shared (InstancePerEntity=false). Its shared texture
    // cache must be static or tML rejects the entire Mod during ValidateType.
    private static readonly GhostSamuraiArt art = new();
    public override void Unload() => art.Unload();
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is GhostSamuraiBoss;
    public override bool PreDraw(NPC npc, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (Main.dedServ || npc.ModNPC is not GhostSamuraiBoss boss) return true;
        DrawBoss(batch, boss, screenPos);
        return false;
    }

    private void DrawBoss(SpriteBatch batch, GhostSamuraiBoss boss, Vector2 screen)
    {
        Texture2D texture = art.Texture;
        float clock = boss.VisualAge;
        Vector2 root = boss.NPC.Center - screen + new Vector2(0, MathF.Sin(clock * .035f) * 3);
        Color tint = boss.Phase == SamuraiPhase.Phase1 ? Color.White : new Color(211, 236, 255);
        float pose = AttackPose(boss);
        float motion = MathF.Sin(clock * .028f) * .04f;
        // Both arms are articulated from their shoulder bone, never from the
        // texture corner; mirroring also mirrors the pivot within the source rect.
        DrawArm(-1);
        batch.Draw(texture, root, GhostSamuraiArt.Body, tint, 0, GhostSamuraiArt.BodyPivot,
            GhostSamuraiArt.Scale, SpriteEffects.None, 0);
        DrawArm(1);
        if (boss.TransitionRemaining > 0)
        {
            float t = 1 - boss.TransitionRemaining / (float)GhostSamuraiRules.TransitionTime;
            Ring(batch, root, 75 + t * 145, 3, new Color(65, 135, 224) * (1 - t));
        }

        void DrawArm(int side)
        {
            Vector2 shoulder = root + new Vector2(side * 56, -35);
            float angle = side * (.1f + motion + pose * (pose < 0 ? .65f : 1.2f));
            Vector2 pivot = GhostSamuraiArt.ShoulderPivot;
            if (side < 0) pivot.X = GhostSamuraiArt.SwordArm.Width - pivot.X;
            batch.Draw(texture, shoulder, GhostSamuraiArt.SwordArm, tint, angle, pivot,
                GhostSamuraiArt.Scale, side < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }
    }

    private static float AttackPose(GhostSamuraiBoss boss)
    {
        float t = boss.VisualAttackTimer;
        float warning, live;
        switch (boss.Attack)
        {
            case SamuraiAttack.DirectionalSlash:
                return GhostSamuraiRules.DirectionalPose(t);
            case SamuraiAttack.ChargedSlash:
                return GhostSamuraiRules.ChargePose(t - GhostSamuraiRules.ChargeAimTime, boss.Phase, false);
            case SamuraiAttack.GridSlash:
                return GhostSamuraiRules.ChargePose(t, boss.Phase, true);
            case SamuraiAttack.Phase2DashSlash:
                t = t % GhostSamuraiRules.DashCadence - GhostSamuraiRules.DashApproach;
                warning = GhostSamuraiRules.DashWarning; live = GhostSamuraiRules.DashLive; break;
            case SamuraiAttack.Phase3CircleAttack:
                int step = Math.Min((int)t / GhostSamuraiRules.Phase3CircleStepInterval, 3);
                t -= step * GhostSamuraiRules.Phase3CircleStepInterval;
                warning = GhostSamuraiRules.Phase3CircleTelegraphTime;
                live = step < 2 ? GhostSamuraiRules.Phase3CircleSlashLive : GhostSamuraiRules.Phase3KamaitachiDuration;
                break;
            default: return 0;
        }
        if (t < 0) return 0;
        if (t < warning) return -MathF.Sin(Math.Clamp(t / 16, 0, 1) * MathHelper.PiOver2);
        if (t < warning + 5)
        {
            float release = (t - warning) / 5;
            return -1 + 2 * release * release;
        }
        if (t < warning + live) return 1;
        float recoil = Math.Clamp((t - warning - live) / GhostSamuraiRules.RecoveryTime, 0, 1);
        return 1 - recoil * recoil * (3 - 2 * recoil);
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
        // A delayed client must not render a stale aimed line as the live strike.
        if (h.HasAim && age >= h.Fire && !p.SlashAim.Locked) return false;
        SpriteBatch batch = Main.spriteBatch;
        Vector2 position = p.VisualCenter(age) - Main.screenPosition;
        bool live = h.Live(age);
        Color color = live ? new Color(186, 247, 255) : new Color(69, 182, 240);
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        int viewWidth = (int)MathF.Ceiling(Main.screenWidth / Math.Max(.1f, zoom.X)) + 64;
        int viewHeight = (int)MathF.Ceiling(Main.screenHeight / Math.Max(.1f, zoom.Y)) + 64;
        Rectangle viewport = new((Main.screenWidth - viewWidth) / 2, (Main.screenHeight - viewHeight) / 2, viewWidth, viewHeight);
        if (h.IsCircle && Main.npc[p.BossSlot].ModNPC is GhostSamuraiBoss owner)
        {
            var field = owner.Arena;
            viewport = Rectangle.Intersect(viewport, new Rectangle((int)(field.Left - Main.screenPosition.X),
                (int)(field.Top - Main.screenPosition.Y), (int)(field.HalfWidth * 2), (int)(field.HalfHeight * 2)));
        }
        if (h.IsCircle) GhostSamuraiCircleVisuals.Draw(batch, h, position, age, viewport,
            ModContent.GetInstance<Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualConfig>().ReducedEffects);
        else if (h.Shape == SamuraiShape.SlashWave) GhostSamuraiWaveVisuals.Draw(batch, h, position, age, p.SlashAim.Locked);
        else if (h.Shape == SamuraiShape.RushVisual) DrawRush(batch, p, position, age);
        else if (h.Shape == SamuraiShape.Wisp)
        {
            // Core radius matches collision. Tail is translucent decoration.
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
            // Warm warnings remain distinct from the blue boss/wisps and bright sky.
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

    private static void DrawRush(SpriteBatch batch, GhostSamuraiAttackProjectile p, Vector2 start, float age)
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
            Color flash = new Color(186, 247, 255) * (1 - t);
            // Large single blade flash stays translucent and unbordered: unlike
            // the solid outlined grid, this art is entirely harmless.
            GhostSamuraiVisuals.Stroke(batch, start, end, h.Radius * .6f, flash * .15f);
            GhostSamuraiVisuals.Stroke(batch, start, end, 7, flash * .65f);
            Vector2 body = GhostSamuraiAttackProjectile.RushCenter(h, age) - Main.screenPosition;
            GhostSamuraiVisuals.Stroke(batch, body - d * 180, body, 18, flash * .3f);
        }
    }
}
