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
        SoboroSlashLayer.Track(projectile, cut, player);
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
        Vector2 impactPosition = new(Math.Clamp(tip.X, target.Left.X, target.Right.X),
            Math.Clamp(tip.Y, target.Top.Y, target.Bottom.Y));
        SoboroSlashLayer.Impact(projectile, impactPosition);
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
        // The crescent, lightning, sparks and hit star live in SoboroSlashLayer's
        // pixel layer; only the physical blade is drawn over the player here.
        DXOboroArt.Sword(Main.spriteBatch, grip, bladeAngle,
            DXOboroMotion.BladeLength(cut.Step, age),
            Color.White, cut.Facing < 0);
        return false;
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        SoboroSlashLayer.Release(projectile);
        if (shake is not null) shake.ShakeStrength = 0;
        shake = null;
        released = false;
        lastObservedAge = -1;
        impactCount = 0;
    }
}
