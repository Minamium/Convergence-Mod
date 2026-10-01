using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

internal sealed class GhostSamuraiVisuals : GlobalNPC
{
    internal static readonly Rectangle StrokePixel = new(0, 0, 1, 1);
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    { if (!Main.dedServ && npc.ModNPC is GhostSamuraiBoss boss) GhostSamuraiPresentation.Hit(boss); }
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is GhostSamuraiBoss;
    public override bool PreDraw(NPC npc, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (Main.dedServ || npc.ModNPC is not GhostSamuraiBoss boss) return true;
        GhostSamuraiPresentation.Draw(batch, boss, screenPos);
        // Raised blades and their emitted veil replace detached UI target rings.
        return false;
    }
    internal static void Stroke(SpriteBatch batch, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 d = b - a;
        if (!float.IsFinite(d.X) || !float.IsFinite(d.Y) || !float.IsFinite(width) || width <= 0 || d.LengthSquared() < .001f) return;
        batch.Draw(TextureAssets.MagicPixel.Value, a, StrokePixel, color, d.ToRotation(), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }
    internal static void Ring(SpriteBatch batch, Vector2 center, float radius, float width, Color color)
    {
        for (int i = 0; i < 64; i++) Stroke(batch, center + (i * MathHelper.TwoPi / 64).ToRotationVector2() * radius,
            center + ((i + 1) * MathHelper.TwoPi / 64).ToRotationVector2() * radius, width, color);
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
        if (h.HasAim && age >= h.Fire && !p.SlashAim.Locked) return false;
        var batch = Main.spriteBatch;
        Vector2 position = p.VisualCenter(age) - Main.screenPosition;
        bool reduced = GhostSamuraiRigArt.Reduced;
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        int width = (int)MathF.Ceiling(Main.screenWidth / Math.Max(.1f, zoom.X)) + 64;
        int height = (int)MathF.Ceiling(Main.screenHeight / Math.Max(.1f, zoom.Y)) + 64;
        Rectangle viewport = new((Main.screenWidth - width) / 2, (Main.screenHeight - height) / 2, width, height);
        if ((h.IsCircle || h.Shape == SamuraiShape.FrontalCleave) && Main.npc[p.BossSlot].ModNPC is GhostSamuraiBoss owner)
        {
            var field = owner.Arena;
            viewport = Rectangle.Intersect(viewport, new((int)(field.Left - Main.screenPosition.X), (int)(field.Top - Main.screenPosition.Y),
                (int)(field.HalfWidth * 2), (int)(field.HalfHeight * 2)));
        }
        if (h.Shape == SamuraiShape.FrontalCleave) GhostSamuraiComboVisuals.DrawCleave(batch, h, position, age, p.SlashAim.Locked, viewport, reduced);
        else if (h.Shape == SamuraiShape.GroundShockwave) GhostSamuraiComboVisuals.DrawShock(batch, h, position, age, reduced);
        else if (h.IsCircle) GhostSamuraiCircleVisuals.Draw(batch, h, position, age, viewport, reduced);
        else if (h.Shape == SamuraiShape.SlashWave) GhostSamuraiWaveVisuals.Draw(batch, h, position, age, p.SlashAim.Locked, reduced);
        else if (h.Shape == SamuraiShape.RushVisual) DrawRush(batch, p, position, age, reduced);
        else if (h.Shape == SamuraiShape.Wisp)
        {
            Vector2 heading = new Vector2(p.WispMotion.VX, p.WispMotion.VY).SafeNormalize(new(h.DX, h.DY));
            // The flame's tail is decoration, not a second collision shape.
            GhostSamuraiCuts.Wisp(batch, position, h.Radius, age, heading, GhostSamuraiCuts.Seed(h), reduced);
        }
        else GhostSamuraiCuts.Slash(batch, h, position, age, reduced);
        return false;
    }
    private static void DrawRush(SpriteBatch batch, GhostSamuraiAttackProjectile p, Vector2 start, float age, bool reduced)
    {
        var h = p.DisplayHazard;
        Vector2 d = new(h.DX, h.DY), n = new(-h.DY, h.DX), end = start + d * h.Length;
        if (!h.Live(age))
        {
            float radius = Math.Abs(n.X) * GhostSamuraiRules.BodyWidth / 2 + Math.Abs(n.Y) * GhostSamuraiRules.BodyHeight / 2;
            GhostSamuraiCuts.Stroke(batch, start, end, radius, age, h.Born, h.Fire, h.End, reduced,
                GhostSamuraiCuts.Seed(h), route: true);
        }
        else
        {
            float t = (age - h.Fire) / (h.End - h.Fire);
            Vector2 body = GhostSamuraiAttackProjectile.RushCenter(h, age) - Main.screenPosition;
            // No filled route: only the moving boss owns rush damage.
            GhostSamuraiSlashArt.Strip(batch, SamuraiSlashArt.Dash, body - d * 260, body, 70, (1 - t) * (reduced ? .35f : .72f));
        }
    }
}
