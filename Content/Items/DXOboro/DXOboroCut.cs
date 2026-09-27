using System;
using System.Collections.Generic;
using Convergence.Common.Compatibility.Calamity;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Items.DXOboro;

public sealed class DXOboroCut : ModProjectile
{
    public override string Texture => "Convergence/Assets/Textures/Items/DXOboro/Blade";
    internal int Step => Math.Clamp((int)Projectile.ai[0], 0, 2);
    internal float Aim => Projectile.ai[1];
    // Native ai[2] is included in projectile snapshots, including late joins.
    internal int Age => (int)Projectile.ai[2];
    internal int Facing => MathF.Cos(Aim) < 0 ? -1 : 1;
    internal float BladeAngle => DXOboroMotion.Angle(Step, Age, Aim, Facing);
    internal Vector2 Hand(Player owner, float age)
    {
        var point = OboroHandAnchor.Capture(owner, Facing).At(
            DXOboroMotion.ArmAngle(Step, age, Aim, Facing));
        return owner.MountedCenter + new Vector2(point.X, point.Y);
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = CalamityTrueMelee.Damage;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 24;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = DXOboroMotion.MaximumDuration + 2;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => DXOboroMotion.Live(Step, Age);
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        if (owner.HeldItem.type != ModContent.ItemType<DXOboro>())
        {
            // Native held-item sync may arrive a few ticks after the projectile
            // on other peers. The owner still cancels instantly on switching.
            if (Projectile.owner == Main.myPlayer || ++Projectile.localAI[1] > 6)
                Projectile.Kill();
            return;
        }
        Projectile.localAI[1] = 0;
        if (!float.IsFinite(Aim) || Projectile.ai[0] < 0 || Projectile.ai[0] > 2
            || !float.IsFinite(Projectile.ai[2]) || Projectile.ai[2] < 0
            || Projectile.ai[2] > DXOboroMotion.MaximumDuration + 2)
        { Projectile.Kill(); return; }

        Projectile.ai[2]++;
        if (Projectile.owner == Main.myPlayer &&
            (Age == 1 || Age == DXOboroMotion.Release(Step) || Age % 6 == 0))
            Projectile.netUpdate = true;
        Projectile.Center = Hand(owner, Age);
        Projectile.rotation = BladeAngle;
        Projectile.direction = Projectile.spriteDirection = Facing;
        Projectile.velocity = Vector2.Zero;
        if (Age >= DXOboroMotion.Duration(Step)) Projectile.Kill();
    }

    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        if (!DXOboroMotion.Live(Step, Age)) return false;
        Player owner = Main.player[Projectile.owner];
        var hand = OboroHandAnchor.Capture(owner, Facing);
        Vector2 topLeft = new(targetHitbox.X, targetHitbox.Y);
        Vector2 size = new(targetHitbox.Width, targetHitbox.Height);
        // Sweep the same advancing curve rendered by the ribbon. Fast release
        // cannot tunnel past narrow enemies between game ticks.
        for (int i = 0; i <= 8; i++)
        {
            float sampleAge = Math.Max(DXOboroMotion.Release(Step), Age - 1f + i / 8f);
            float angle = DXOboroMotion.Angle(Step, sampleAge, Aim, Facing);
            var point = hand.At(DXOboroMotion.ArmAngle(Step, sampleAge, Aim, Facing));
            Vector2 root = owner.MountedCenter + new Vector2(point.X, point.Y);
            float collision = 0;
            if (Collision.CheckAABBvLineCollision(topLeft, size, root,
                    root + angle.ToRotationVector2() * DXOboroMotion.Reach,
                    DXOboroMotion.Width, ref collision)
                && Collision.CanHitLine(root, 1, 1, topLeft, targetHitbox.Width, targetHitbox.Height)) return true;
        }
        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
        List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI) => overPlayers.Add(index);
}
