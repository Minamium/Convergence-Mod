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
    private int lastObservedAge = -1;
    private ScreenShakeSystem.ShakeInfo? shake;
    private ulong impactTick;
    private Vector2 impactPosition;
    private float impactAngle;
    private int impactCount;

    private static float DrawAge(DXOboroCut cut)
    {
        float age = Math.Clamp(cut.Age - 1f + WeaponDrawClock.Fraction, 0f,
            DXOboroMotion.Duration(cut.Step));
        // The simulation owns the hit window. Never show a harmless windup on
        // an already live tick, or a live white flare on a recovery tick.
        if (cut.Age >= DXOboroMotion.Release(cut.Step))
            age = MathF.Max(age, DXOboroMotion.Release(cut.Step));
        if (cut.Age >= DXOboroMotion.LiveEnd(cut.Step))
            age = MathF.Max(age, DXOboroMotion.LiveEnd(cut.Step));
        return age;
    }

    internal bool TryGetArmPose(Projectile projectile, out float armRotation)
    {
        armRotation = 0f;
        if (!projectile.active || projectile.ModProjectile is not DXOboroCut cut) return false;
        float age = DrawAge(cut);
        armRotation = DXOboroMotion.ArmAngle(cut.Step, age, cut.Aim, cut.Facing) - MathF.PI / 2f;
        return true;
    }

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not DXOboroCut cut || !projectile.active) return;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.type != ModContent.ItemType<DXOboro>()) return;
        float angle = cut.BladeAngle;
        float arm = DXOboroMotion.ArmAngle(cut.Step, cut.Age, cut.Aim, cut.Facing);
        player.ChangeDir(cut.Facing);
        player.SetCompositeArmFront(true, OboroHandAnchor.Stretch(player, cut.Hand(player, cut.Age), arm),
            arm - MathF.PI / 2);
        int previousAge = lastObservedAge;
        lastObservedAge = cut.Age;
        if (released || previousAge >= DXOboroMotion.Release(cut.Step)
            || cut.Age < DXOboroMotion.Release(cut.Step)
            || cut.Age > DXOboroMotion.Release(cut.Step) + 2) return;
        released = true;
        DXOboroAudio.Swing(cut.Step, player.Center);
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        if (projectile.owner == Main.myPlayer && !config.ReducedEffects && config.ScreenShake)
            shake = ScreenShakeSystem.StartShakeAtPoint(player.Center, cut.Step == 2 ? 3.5f : 1.35f,
                angularVariance: .22f, shakeDirection: angle.ToRotationVector2(),
                shakeStrengthDissipationIncrement: cut.Step == 2 ? .65f : .45f);
    }

    // Pure local feedback for tML's native hit callback; no new damage path or
    // network event. A crowd cannot stack a sound/flash for every target.
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.dedServ || projectile.ModProjectile is not DXOboroCut cut || damageDone <= 0
            || impactCount >= (cut.Step == 2 ? 2 : 1)
            || (impactCount > 0 && Main.GameUpdateCount - impactTick < 4)) return;
        impactCount++;
        impactTick = Main.GameUpdateCount;
        Vector2 grip = cut.Hand(Main.player[projectile.owner], cut.Age);
        Vector2 tip = grip + cut.BladeAngle.ToRotationVector2() * DXOboroMotion.Reach;
        impactPosition = new Vector2(Math.Clamp(tip.X, target.Left.X, target.Right.X),
            Math.Clamp(tip.Y, target.Top.Y, target.Bottom.Y));
        impactAngle = cut.BladeAngle;
        bool metallic = target.HitSound is SoundStyle sound && sound.Equals(SoundID.NPCHit4);
        DXOboroAudio.Hit(impactPosition, cut.Step == 2, metallic);
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is not DXOboroCut cut) return false;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.type != ModContent.ItemType<DXOboro>()) return false;
        float age = DrawAge(cut);
        float bladeAngle = DXOboroMotion.Angle(cut.Step, age, cut.Aim, cut.Facing);
        var basis = OboroHandAnchor.Capture(player, cut.Facing);
        var point = basis.At(DXOboroMotion.ArmAngle(cut.Step, age, cut.Aim, cut.Facing));
        Vector2 grip = player.MountedCenter + new Vector2(point.X, point.Y);
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        DXOboroMaterial.Draw(Main.spriteBatch, age, cut.Step, cut.Aim, cut.Facing,
            player.MountedCenter + new Vector2(basis.X, basis.Y),
            new Vector2(basis.AlongX, basis.AlongY),
            new Vector2(basis.AcrossX, basis.AcrossY), reduced);
        if (age < DXOboroMotion.Release(cut.Step))
        {
            int motes = reduced ? 1 : 3;
            for (int i = 0; i < motes; i++)
            {
                float drift = age * .17f + i * MathHelper.TwoPi / motes;
                Vector2 along = bladeAngle.ToRotationVector2();
                Vector2 side = new(-along.Y, along.X);
                Vector2 at = grip + along * (46f + 20f * i / motes)
                    + side * MathF.Sin(drift) * (9f + i * 3f);
                OboroArt.Flame(Main.spriteBatch, at, 8f + 2f * i,
                    .24f + .2f * age / DXOboroMotion.Release(cut.Step));
            }
        }
        DXOboroArt.Sword(Main.spriteBatch, grip, bladeAngle,
            DXOboroMotion.BladeLength(cut.Step, age),
            Color.White, cut.Facing < 0);
        DXOboroMaterial.BladeAndFracture(Main.spriteBatch, age, cut.Step, cut.Aim, cut.Facing, grip, reduced);
        if (impactCount > 0)
            DXOboroMaterial.Impact(Main.spriteBatch, impactPosition, impactAngle,
                (float)(Main.GameUpdateCount - impactTick) + WeaponDrawClock.Fraction, cut.Step, reduced);
        return false;
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (shake is not null) shake.ShakeStrength = 0;
        shake = null;
        released = false;
        lastObservedAge = -1;
        impactCount = 0;
    }
}
