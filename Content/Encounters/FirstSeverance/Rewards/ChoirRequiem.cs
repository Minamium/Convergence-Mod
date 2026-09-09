#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Exactly one leader-owned three-second chorus. Identity, not a reusable array
// slot, binds it to the conductor; removed voices stop contributing immediately.
public sealed class ChoirRequiem : ModProjectile
{
    private readonly ulong[] nextRootHit = new ulong[Main.maxNPCs];
    internal float Age => Projectile.ai[0];
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);
    internal float Opening => RitualGrandScore.BeamOpening(Age, 0);
    internal float Fade => Math.Clamp((180 - Age) / 18, 0, 1);
    internal float Reach => 2000 * Opening;
    internal float Width => 92 * Opening;
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults()
    { ProjectileID.Sets.MinionShot[Type] = true; ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2600; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 182;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 12;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => Main.GameUpdateCount < nextRootHit[RitualTargeting.Root(target)] ? false : null;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] < 0) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner) || owner.noItems || owner.CCed) { Projectile.Kill(); return; }
        ChoirSentinel? conductor = null;
        double total = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.ModProjectile is ChoirSentinel s)
            {
                total += Math.Max(0, p.damage);
                if (p.identity == (int)Projectile.ai[2]) conductor = s;
            }
        if (conductor is null || !RitualGrandScore.ChoirLive(conductor.Age) || Age >= 180)
        { Projectile.Kill(); return; }
        NPC? target = RitualTargeting.Acquire(Projectile, owner, manual: true);
        if (target is null) { Projectile.Kill(); return; }
        Projectile.Center = conductor.ConcertCenter + new Vector2(0, 25);
        if (Projectile.owner == Main.myPlayer)
        {
            Vector2 desired = RitualArmamentItems.Aim(target.Center - Projectile.Center, owner.direction);
            float turn = MathHelper.WrapAngle(desired.ToRotation() - Axis.ToRotation());
            Projectile.velocity = Age < 1 ? desired : Axis.RotatedBy(Math.Clamp(turn, -.045f, .045f));
            if ((int)Age % 6 == 0) Projectile.netUpdate = true;
        }
        Projectile.damage = (int)Math.Clamp(total * 1.05, 1, int.MaxValue / 4.0);
        Projectile.rotation = Axis.ToRotation(); Projectile.ai[0]++;
    }
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        if (Opening <= 0) return false;
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center, Projectile.Center + Axis * Reach, Width, ref distance);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => nextRootHit[RitualTargeting.Root(target)] = Main.GameUpdateCount + 12;
}
