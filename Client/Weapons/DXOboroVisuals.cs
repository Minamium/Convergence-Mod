#nullable enable
using System;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Content.Items.DXOboro;
using Convergence.Content.Items.Oboro;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

[Autoload(Side = ModSide.Client)]
public sealed class DXOboroVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is DXOboroCut;

    private bool released;
    private ScreenShakeSystem.ShakeInfo? shake;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not DXOboroCut cut || !projectile.active) return;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.type != ModContent.ItemType<DXOboro>()) return;
        float angle = cut.BladeAngle;
        player.ChangeDir(cut.Facing);
        player.SetCompositeArmFront(true, OboroHandAnchor.Stretch(player, cut.Hand(player, angle), angle),
            angle - MathF.PI / 2);
        if (cut.Age != DXOboroMotion.ReleaseFrame || released) return;
        released = true;
        SoundEngine.PlaySound(SoundID.Item1 with
        {
            Volume = .72f,
            Pitch = cut.Step == 2 ? -.4f : .1f + cut.Step * .12f,
            MaxInstances = 4
        }, player.Center);
        if (cut.Step == 2)
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = .34f, Pitch = -.25f, MaxInstances = 3 }, player.Center);
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        if (projectile.owner == Main.myPlayer && !config.ReducedEffects && config.ScreenShake)
            shake = ScreenShakeSystem.StartShakeAtPoint(player.Center, cut.Step == 2 ? 2.2f : 1.1f,
                angularVariance: .22f, shakeDirection: angle.ToRotationVector2(),
                shakeStrengthDissipationIncrement: .45f);
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is not DXOboroCut cut) return false;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.type != ModContent.ItemType<DXOboro>()) return false;
        Vector2 grip = cut.Hand(player, cut.BladeAngle);
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        DXOboroMaterial.Draw(Main.spriteBatch, cut.Age, cut.Step, cut.Aim, cut.Facing, grip, reduced);
        if (cut.Age < DXOboroMotion.ReleaseFrame)
        {
            int motes = reduced ? 1 : 3;
            for (int i = 0; i < motes; i++)
            {
                float drift = cut.Age * .17f + i * MathHelper.TwoPi / motes;
                Vector2 along = cut.BladeAngle.ToRotationVector2();
                Vector2 side = new(-along.Y, along.X);
                Vector2 at = grip + along * (46f + 20f * i / motes)
                    + side * MathF.Sin(drift) * (9f + i * 3f);
                OboroArt.Flame(Main.spriteBatch, at, 8f + 2f * i,
                    .24f + .2f * cut.Age / DXOboroMotion.ReleaseFrame);
            }
        }
        DXOboroArt.Sword(Main.spriteBatch, grip, cut.BladeAngle, 136f,
            Color.White, cut.Facing < 0);
        return false;
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (shake is not null) shake.ShakeStrength = 0;
        shake = null;
        released = false;
    }
}
