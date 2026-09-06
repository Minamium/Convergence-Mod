using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;

namespace Convergence.Client.Encounters.FirstSeverance;

[Autoload(Side = ModSide.Client)]
public sealed class NullRefrainItemVisuals : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == ModContent.ItemType<NullRefrain>();
    public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position,
        Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        var texture = TextureAssets.Item[item.type].Value;
        // The source is high-resolution, but the inventory footprint is not.
        float size = Math.Min(scale, 64f / texture.Width);
        spriteBatch.Draw(texture, position, null,
            Color.White, -.35f, texture.Size() * .5f, size, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor,
        Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        var texture = TextureAssets.Item[item.type].Value;
        spriteBatch.Draw(texture, item.Center - Main.screenPosition + new Vector2(0, MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 3),
            null, Color.Lerp(lightColor, Color.White, .65f), -.38f, texture.Size() * .5f,
            108f / texture.Width, SpriteEffects.None, 0);
        return false;
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class NullRefrainProjectileVisuals : GlobalProjectile
{
    private bool sounded, impacted;
    private int impactAge = -1;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ModContent.ProjectileType<NullRefrainSlash>();
    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not NullRefrainSlash slash) return;
        if (!sounded && slash.Progress >= .24f)
        {
            sounded = true;
            Play(slash.Combo == 2 ? "BladeSweep" : "BladeUnsheathe", projectile.Center, slash.Combo == 2 ? .62f : .44f,
                slash.Combo == 1 ? .12f : -.08f);
        }
        if (!impacted && slash.HasImpact)
        {
            impacted = true; impactAge = 0;
            Play(slash.Combo == 2 ? "CoreSalvoFire" : "PylonBreak", slash.Impact, slash.Combo == 2 ? .65f : .35f, .10f);
            if (projectile.owner == Main.myPlayer) ModContent.GetInstance<NullRefrainCamera>().Kick(slash.Combo == 2 ? 7 : 3);
        }
        if (impactAge >= 0) impactAge++;
        Lighting.AddLight(projectile.Center + projectile.rotation.ToRotationVector2() * slash.Reach * .7f,
            .3f, .38f, .42f);
    }
    private static void Play(string name, Vector2 at, float volume, float pitch) => SoundEngine.PlaySound(
        new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + name)
        { Volume = volume, Pitch = pitch, MaxInstances = 2, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest }, at);

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is not NullRefrainSlash slash) return true;
        var batch = Main.spriteBatch;
        var texture = TextureAssets.Projectile[projectile.type].Value;
        float p = slash.Progress, reach = slash.Reach;
        float fade = Math.Clamp((1 - p) / .24f, 0, 1);
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        Color tint = slash.Combo == 2 ? new(255, 221, 165) : new(142, 235, 255);
        var origin = new Vector2(texture.Width * .16f, texture.Height * .5f);
        float scale = reach / (texture.Width * .82f);
        int echoes = config.ReducedEffects ? 3 : 8;
        if (p >= .24f)
            for (int i = echoes; i > 0; i--)
            {
                float previous = Math.Max(.24f, p - i * .018f);
                float angle = slash.AngleAt(previous);
                batch.Draw(texture, projectile.Center - Main.screenPosition, null,
                    FirstSeveranceAttackAccents.Neon(tint, fade * (1 - i / (float)(echoes + 1)) * .28f),
                    angle, origin, scale, SpriteEffects.None, 0);
            }
        // Flowing filaments trace the actual swept blade instead of a filled pie.
        if (p >= .24f && p < .93f)
            for (int band = 0; band < (config.ReducedEffects ? 2 : 5); band++)
            {
                Vector2 previous = default;
                for (int n = 0; n <= 32; n++)
                {
                    float t = n / 32f, sample = Math.Max(.24f, p - .23f + t * .23f);
                    float angle = slash.AngleAt(sample);
                    float radius = reach * (.55f + band * .085f) + MathF.Sin(t * 14 - p * 26 + band) * 6;
                    Vector2 next = projectile.Center + angle.ToRotationVector2() * radius;
                    if (n > 0) Line(batch, previous, next,
                        FirstSeveranceAttackAccents.Neon(Color.Lerp(tint, Color.White, t * .7f), fade * t * .8f),
                        (slash.Combo == 2 ? 5 : 3) * t + .6f);
                    previous = next;
                }
            }
        batch.Draw(texture, projectile.Center - Main.screenPosition, null, Color.White * fade,
            projectile.rotation, origin, scale, SpriteEffects.None, 0);
        if (impactAge is >= 0 and < 18)
        {
            float q = impactAge / 18f;
            FirstSeveranceAttackAccents.Arc(batch, slash.Impact, 12 + q * (slash.Combo == 2 ? 170 : 95),
                -q, MathHelper.TwoPi, FirstSeveranceAttackAccents.Neon(tint, (1 - q) * .9f), (1 - q) * 4);
            for (int i = 0; i < 5; i++)
            {
                Vector2 axis = (projectile.rotation + i * MathHelper.TwoPi / 5).ToRotationVector2();
                Line(batch, slash.Impact + axis * (q * 55), slash.Impact + axis * (q * 155 + 25),
                    FirstSeveranceAttackAccents.Neon(Color.White, (1 - q) * .75f), (1 - q) * 3);
            }
        }
        return false;
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class NullRefrainCamera : ModSystem
{
    private float kick;
    internal void Kick(float amount) => kick = Math.Max(kick, amount);
    public override void PostUpdateEverything() => kick *= .78f;
    public override void ModifyScreenPosition()
    {
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        if (!Main.gameMenu && config.ScreenShake && !config.ReducedEffects)
            Main.screenPosition += new Vector2(MathF.Sin((float)Main.GameUpdateCount * 2.9f),
                MathF.Cos((float)Main.GameUpdateCount * 2.3f)) * kick;
    }
    public override void OnWorldUnload() => kick = 0;
}
