#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Stealth's former 3 x .20 echo budget becomes ONE .60 triangular execution.
// Warning tracks briefly, then freezes. All three blade props share one hit ledger.
public sealed class WitnessVerdict : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    internal float Age => Projectile.ai[0];
    public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/V2/LastWitness";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.friendly = true; Projectile.DamageType = RitualArmamentItems.DamageClassFor(RitualArmamentKind.Rogue);
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.extraUpdates = 1; Projectile.timeLeft = 150;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => RitualArmamentChoreography.VerdictLive(Age) ? null : false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(RitualTargeting.Root(target)) ? false : null;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || !RitualArmamentItems.Usable(Main.player[Projectile.owner])
            || Age >= RitualArmamentChoreography.VerdictDuration)
        { Projectile.Kill(); return; }
        float before = Age;
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        if (Age < RitualArmamentChoreography.VerdictLock)
        {
            NPC? target = RitualTargeting.Current(Projectile);
            if (target is not null)
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center,
                    RitualArmamentChoreography.Response(.23f, 1f / Projectile.MaxUpdates));
        }
        if (before < RitualArmamentChoreography.VerdictLock && Age >= RitualArmamentChoreography.VerdictLock
            && Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => RitualArmamentChoreography.VerdictLive(Age) && RitualArmamentChoreography.TriangleHits(
            new(targetHitbox.Center.X - Projectile.Center.X, targetHitbox.Center.Y - Projectile.Center.Y),
            new(targetHitbox.Width * .5f, targetHitbox.Height * .5f), RitualArmamentChoreography.VerdictRadius);
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => hitRoots.Add(RitualTargeting.Root(target));
}
