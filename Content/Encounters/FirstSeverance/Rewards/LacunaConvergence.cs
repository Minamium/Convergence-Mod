using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// The holdout IS the sustained beam, not a sequence of short laser bursts.
// Root cooldown prevents segmented NPC damage multiplication.
public sealed class LacunaConvergence : ModProjectile
{
    private readonly ulong[] nextRootHit = new ulong[Main.maxNPCs];
    internal float Age => Projectile.ai[0];
    internal bool Empowered => Age >= RitualGrandScore.MagicFire;
    internal float Fade => Projectile.ai[2] < 0 ? Math.Clamp(1 + Projectile.ai[2] / 20, 0, 1) : 1;
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);
    internal Vector2 Muzzle => Projectile.Center + Axis * 180;
    internal float Reach => RitualGrandScore.MagicLength * RitualGrandScore.BeamOpening(Age, RitualGrandScore.MagicFire);
    internal Vector2 Sigil(int index, float age)
    {
        var offset = RitualGrandScore.SigilOffset(index, Projectile.ai[1] >= 0 ? 1 : -1);
        float arrived = RitualGrandScore.Arrival(age, RitualGrandScore.SigilBirth(index));
        Vector2 at = Projectile.Center + new Vector2(offset.X, offset.Y)
            + new Vector2(0, (index % 2 == 0 ? 1 : -1) * (1 - arrived) * 95);
        return Vector2.Lerp(at, Muzzle, RitualGrandScore.Merge(age));
    }
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = RitualGrandScore.MagicHitCadence;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => Empowered && Projectile.ai[2] >= 0 && Reach > 0 ? null : false;
    public override bool? CanHitNPC(NPC target) => Main.GameUpdateCount < nextRootHit[RitualTargeting.Root(target)] ? false : null;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] is < -20 or > 0)
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualChannel.Valid(Projectile, owner, ModContent.ItemType<LacunaTestament>()))
        { Projectile.Kill(); return; }
        if (RitualChannel.Fade(Projectile)) return;
        if (Projectile.owner == Main.myPlayer && !owner.channel) { RitualChannel.Stop(Projectile); return; }
        RitualChannel.Hold(Projectile, owner, Empowered ? .033f : .075f);
        Projectile.ai[0]++;
        owner.manaRegenDelay = Math.Max(owner.manaRegenDelay, 60);
        // Ongoing equipment / Mana Sickness modifiers apply, not spawn-time stats.
        Projectile.damage = RitualArmamentRules.ScaledDamage(owner.GetWeaponDamage(owner.HeldItem), 2.0f);
        if (Projectile.owner != Main.myPlayer) return;
        int tick = (int)Age;
        if (RitualGrandScore.MagicPays(tick))
        {
            int baseCost = owner.GetManaCost(owner.HeldItem);
            int cost = Empowered ? baseCost : (int)MathF.Ceiling(baseCost * .35f);
            if (!owner.CheckMana(owner.HeldItem, cost, pay: true, blockQuickMana: true))
            { RitualChannel.Stop(Projectile); return; }
        }
        for (int i = 0; i < 7; i++)
        {
            if (!RitualGrandScore.MagicBoltAt(tick, i)) continue;
            Vector2 at = Sigil(i, Age), aim = RitualArmamentItems.Aim(Main.MouseWorld - at, owner.direction);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, aim * 18,
                ModContent.ProjectileType<LacunaRay>(), RitualArmamentRules.ScaledDamage(owner.GetWeaponDamage(owner.HeldItem), .55f),
                Projectile.knockBack, Projectile.owner, 0, -1, 0);
        }
    }
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        if (!Empowered || Projectile.ai[2] < 0 || Reach <= 0) return false;
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Muzzle, Muzzle + Axis * Reach, RitualGrandScore.MagicWidth * RitualGrandScore.BeamOpening(Age, RitualGrandScore.MagicFire), ref distance);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => nextRootHit[RitualTargeting.Root(target)] = Main.GameUpdateCount + RitualGrandScore.MagicHitCadence;
}
