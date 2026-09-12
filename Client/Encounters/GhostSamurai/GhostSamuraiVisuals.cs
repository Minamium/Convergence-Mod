using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// Original procedural placeholder derived from the user's skull/oni/skeleton sketch.
// Replace these drawing methods with an atlas later; no gameplay depends on them.
internal sealed class GhostSamuraiVisuals : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is GhostSamuraiBoss;
    public override bool PreDraw(NPC npc, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (Main.dedServ || npc.ModNPC is not GhostSamuraiBoss boss) return true;
        DrawBoss(batch, boss, screenPos);
        return false;
    }

    private static void DrawBoss(SpriteBatch batch, GhostSamuraiBoss boss, Vector2 screen)
    {
        float clock = boss.VisualAge;
        Vector2 root = boss.NPC.Center - screen + new Vector2(0, MathF.Sin(clock * .035f) * 5);
        Color bone = boss.Phase == SamuraiPhase.Phase1 ? new(174, 229, 244) : new(193, 225, 255);
        Color aura = boss.Phase == SamuraiPhase.Phase1 ? new(27, 135, 205) : new(65, 95, 224);
        // The torso/rib cage is also the damageable hitbox, with a visible rim.
        for (int i = 0; i < 7; i++)
        {
            float y = 8 + i * 16;
            float width = 49 - i * 3;
            Vector2 a = root + new Vector2(-width, y), b = root + new Vector2(width, y);
            Stroke(batch, a, root + new Vector2(-width - 6, y + 7), 5, bone * .75f);
            Stroke(batch, root + new Vector2(-width - 6, y + 7), root + new Vector2(-8, y + 12), 5, bone * .75f);
            Stroke(batch, b, root + new Vector2(width + 6, y + 7), 5, bone * .75f);
            Stroke(batch, root + new Vector2(width + 6, y + 7), root + new Vector2(8, y + 12), 5, bone * .75f);
        }
        Stroke(batch, root + new Vector2(0, -2), root + new Vector2(0, 105), 6, bone);
        Vector2 skull = root + new Vector2(0, -53);
        Vector2[] outline = { new(-40,-25), new(-26,-43), new(26,-43), new(40,-25), new(38,9), new(20,31), new(-20,31), new(-38,9) };
        for (int i = 0; i < outline.Length; i++) Stroke(batch, skull + outline[i], skull + outline[(i + 1) % outline.Length], 7, bone);
        for (int side = -1; side <= 1; side += 2)
        {
            // Swept horns, slanted eye sockets and fang-like teeth.
            Stroke(batch, skull + new Vector2(side * 30, -32), skull + new Vector2(side * 62, -62), 9, bone);
            Stroke(batch, skull + new Vector2(side * 62, -62), skull + new Vector2(side * 55, -34), 4, bone);
            Stroke(batch, skull + new Vector2(side * 9, -7), skull + new Vector2(side * 29, -14), 11, new Color(10, 29, 52));
            Stroke(batch, skull + new Vector2(side * 12, -8), skull + new Vector2(side * 27, -12), 4, new Color(117, 238, 255));
            Stroke(batch, skull + new Vector2(side * 26, 20), skull + new Vector2(side * 20, 33), 4, bone);
        }
        Stroke(batch, skull + new Vector2(-25, 15), skull + new Vector2(25, 15), 5, bone);
        for (int i = -2; i <= 2; i++) Stroke(batch, skull + new Vector2(i * 9, 15), skull + new Vector2(i * 9, 23), 3, bone);

        float motion = MathF.Sin(clock * .028f) * .10f;
        float pose = AttackPose(boss);
        float strike = Math.Max(0, pose), tension = Math.Max(0, -pose) * .65f;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 shoulder = root + new Vector2(side * 58, 2);
            float angle = side * (1.2f + motion - tension - strike * 1.7f);
            // Arm and sword share the same hand anchor, including attack/recoil.
            Vector2 hand = root + new Vector2(side * (95 - tension * 30), 62 - tension * 105 - strike * 50);
            Vector2 elbow = Vector2.Lerp(shoulder, hand, .5f) + new Vector2(side * 35, 5);
            Stroke(batch, shoulder, elbow, 8, bone); Stroke(batch, elbow, hand, 6, bone);
            Vector2 direction = new(MathF.Sin(angle), -MathF.Cos(angle));
            Vector2 normal = new(-direction.Y, direction.X);
            Stroke(batch, hand - direction * 23, hand + direction * 12, 10, new Color(46, 89, 117));
            Stroke(batch, hand + direction * 9 - normal * 20, hand + direction * 9 + normal * 20, 6, bone);
            Vector2 blade = hand + direction * 14;
            for (int j = 0; j < 8; j++)
            {
                Vector2 start = blade + direction * (j * 20) + normal * (j * j * .36f);
                Vector2 end = blade + direction * ((j + 1) * 20) + normal * ((j + 1) * (j + 1) * .36f);
                Stroke(batch, start, end, 10 - j, bone);
            }
            for (int j = 0; j < 9; j++)
            {
                Vector2 a = root + new Vector2(side * (35 + j * 3 + MathF.Sin(clock * .04f + j) * 7), 100 + j * 11);
                Stroke(batch, a, a + new Vector2(side * 3, 18), 6 - j * .5f, aura * (1 - j / 9f) * .5f);
            }
        }
        if (boss.TransitionRemaining > 0)
        {
            float t = 1 - boss.TransitionRemaining / (float)GhostSamuraiRules.TransitionTime;
            Ring(batch, root, 75 + t * 145, 3, aura * (1 - t));
        }
    }

    private static float AttackPose(GhostSamuraiBoss boss)
    {
        float t = boss.VisualAttackTimer;
        float warning, live;
        switch (boss.Attack)
        {
            case SamuraiAttack.DirectionalSlash:
                t %= GhostSamuraiRules.SlashCadence;
                warning = GhostSamuraiRules.SlashWarning; live = GhostSamuraiRules.SlashLive; break;
            case SamuraiAttack.ChargedSlash:
                t -= GhostSamuraiRules.ChargeAimTime;
                warning = GhostSamuraiRules.ChargeWarning; live = GhostSamuraiRules.ChargeLive; break;
            case SamuraiAttack.GridSlash:
                // Quick double unsheathing in the prelude, then a held pose.
                if (t < GhostSamuraiRules.GridPrelude) return MathF.Sin(t / GhostSamuraiRules.GridPrelude * MathHelper.TwoPi * 2) * .7f;
                t -= GhostSamuraiRules.GridPrelude;
                warning = GhostSamuraiRules.GridWarning; live = GhostSamuraiRules.GridLive; break;
            case SamuraiAttack.Phase2DashSlash:
                t = t % GhostSamuraiRules.DashCadence - GhostSamuraiRules.DashApproach;
                warning = GhostSamuraiRules.DashWarning; live = GhostSamuraiRules.DashLive; break;
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
        float recoil = Math.Clamp((t - warning - live) / 24, 0, 1);
        return 1 - recoil * recoil * (3 - 2 * recoil);
    }

    internal static void Stroke(SpriteBatch batch, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 d = b - a;
        if (d.LengthSquared() < .001f) return;
        // Scale one texel, not the full texture: rendered endpoints/width must
        // match SamuraiHazard geometry for both forecasts and live slashes.
        batch.Draw(TextureAssets.MagicPixel.Value, a, new Rectangle(0, 0, 1, 1), color, d.ToRotation(), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
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
    public override bool InstancePerEntity => true;
    private int lastAge = -1;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is GhostSamuraiAttackProjectile;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || projectile.ModProjectile is not GhostSamuraiAttackProjectile p || !p.TryGetAge(out float age)) return;
        int tick = (int)age;
        var h = p.Hazard;
        // Do not replay missed historical cues after joining/snapshot catch-up.
        if (lastAge >= 0 && tick - lastAge <= 4)
        {
            if (h.Shape == SamuraiShape.Slash && h.Radius == GhostSamuraiRules.ChargeHalfWidth)
                for (int warning = 0; warning < 3; warning++)
                {
                    int at = h.Born + warning * 24;
                    if (lastAge < at && tick >= at) SoundEngine.PlaySound(SoundID.Item4 with { Volume = .7f, Pitch = warning * .15f }, projectile.Center);
                }
            if (lastAge < h.Fire && tick >= h.Fire && h.Shape == SamuraiShape.Slash)
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = .6f, MaxInstances = 2 }, projectile.Center);
        }
        // First warning is the current event, not history, on a fresh charge spawn.
        if (lastAge < 0 && tick >= h.Born && tick <= h.Born + 3 && h.Radius == GhostSamuraiRules.ChargeHalfWidth)
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = .7f }, projectile.Center);
        lastAge = tick;
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (Main.dedServ || projectile.ModProjectile is not GhostSamuraiAttackProjectile p || !p.TryGetAge(out float age)) return true;
        var h = p.Hazard;
        if (age < h.Born || age >= h.End) return false;
        SpriteBatch batch = Main.spriteBatch;
        Vector2 position = new Vector2(h.CenterX(age), h.CenterY(age)) - Main.screenPosition;
        bool live = h.Live(age);
        Color color = live ? new Color(186, 247, 255) : new Color(69, 182, 240);
        if (h.Shape == SamuraiShape.Wisp)
        {
            // Core radius matches collision. Tail is translucent decoration.
            GhostSamuraiVisuals.Ring(batch, position, h.Radius, 3, color);
            GhostSamuraiVisuals.Stroke(batch, position - new Vector2(h.DX, h.DY) * 30, position, live ? 10 : 5, color * .6f);
            if (!live) GhostSamuraiVisuals.Stroke(batch, position, position + new Vector2(h.DX, h.DY) * 75, 2, color * .5f);
        }
        else
        {
            Vector2 direction = new(h.DX, h.DY), normal = new(-h.DY, h.DX), end = position + direction * h.Length;
            // Continuous full-width fill + two exact edges, including the broad cut.
            GhostSamuraiVisuals.Stroke(batch, position, end, h.Radius * 2, color * (live ? .75f : .16f));
            GhostSamuraiVisuals.Stroke(batch, position, end, live ? 7 : 2, color * (live ? 1 : .65f));
            for (int side = -1; side <= 1; side += 2)
                GhostSamuraiVisuals.Stroke(batch, position + normal * h.Radius * side, end + normal * h.Radius * side, 2, color * .8f);
            GhostSamuraiVisuals.Stroke(batch, position - normal * h.Radius, position + normal * h.Radius, 2, color);
            GhostSamuraiVisuals.Stroke(batch, end - normal * h.Radius, end + normal * h.Radius, 2, color);
        }
        return false;
    }
}
