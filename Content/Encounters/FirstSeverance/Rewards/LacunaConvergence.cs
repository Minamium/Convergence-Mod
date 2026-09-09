using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// One paid cast, one budget, one root hit. Native projectile ownership/replication;
// no Raid request, homing after release, or client-generated damage/VFX projectiles.
public sealed class LacunaConvergence : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    internal float Age => Projectile.ai[0];
    internal bool Empowered => Projectile.ai[2] == 1;
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);
    internal Vector2 Muzzle => Projectile.Center + Axis * 205;
    internal float Reach => RitualKineticMotion.MagicReach(Age);
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.extraUpdates = 1; Projectile.timeLeft = 90;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => RitualKineticMotion.MagicLive(Age) && Reach > 0 ? null : false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Count > 0 ? false : null;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] is < 0 or > 1)
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner) || owner.noItems || owner.CCed
            || owner.HeldItem.type != ModContent.ItemType<LacunaTestament>() || Age >= RitualKineticMotion.MagicDuration)
        { Projectile.Kill(); return; }
        float before = Age;
        if (before < RitualKineticMotion.MagicFire) Projectile.Center = owner.MountedCenter;
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        Projectile.rotation = Axis.ToRotation();
        if (before < RitualKineticMotion.MagicFire && Age >= RitualKineticMotion.MagicFire && Projectile.owner == Main.myPlayer)
            Projectile.netUpdate = true; // Freeze the firing origin for all peers.
    }
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        if (!RitualKineticMotion.MagicLive(Age) || Reach <= 0) return false;
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Muzzle, Muzzle + Axis * Reach, RitualKineticMotion.MagicWidth(Empowered), ref distance);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => hitRoots.Add(RitualTargeting.Root(target));
}
