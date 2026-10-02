#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Score = Convergence.Content.Encounters.FirstSeverance.Rewards.LacunaTestamentScore;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// A Lacuna Testament void pellet: leaves an open iris toward the cursor and seeks (RitualTargeting, 1800 px), one hit
// per logical NPC root, PelletLife ticks. ai[0] age, ai[1] the replicated target (-1 none), ai[2] the iris (0..6).
// Spawned by the owner's LacunaIrisChannel through native projectile replication; it stops once its owner is unusable.
// A standalone type (not a RitualBolt), so no legacy presentation draws it.
public sealed class LacunaPellet : ModProjectile
{
    // Roots already struck (owner side). Created on first hit; the template never allocates it.
    private HashSet<int>? hitRoots;

    internal float Age => Projectile.ai[0];
    internal int Iris => (int)Projectile.ai[2];
    // Owner side: this pellet landed a hit (its death is a bite, not a timeout).
    internal bool Struck { get; private set; }
    // This client's own AI ended it (an unusable owner, an invalid state): a quiet end, never a bite.
    internal bool Quiet { get; private set; }

    public override string Texture => RitualArmamentItems.TexturePath;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 14;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1800;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = (int)Score.PelletWidth;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1;
        Projectile.timeLeft = Score.PelletLife * 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool CanHitPvp(Player target) => false;
    // Drawn by the Doll weapon layer (Client/Encounters/FirstSeverance/Weapons/LacunaVisuals).
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool? CanHitNPC(NPC target) => hitRoots is not null && hitRoots.Contains(RitualTargeting.Root(target)) ? false : null;

    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] < 0 || Projectile.ai[2] >= Score.Irises)
        {
            End();
            return;
        }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner))
        {
            End();
            return;
        }
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        NPC? target = RitualTargeting.Acquire(Projectile, owner, excluded: hitRoots);
        if (target is not null) RitualTargeting.Home(Projectile, target.Center, target.velocity, Score.PelletSpeed);
        Projectile.rotation = Projectile.velocity.ToRotation();
    }

    private void End()
    {
        Quiet = true;
        Projectile.Kill();
    }

    // A sweep covers the fast head; the drawn wake is harmless.
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        float collision = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, Projectile.width, ref collision);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        (hitRoots ??= new HashSet<int>()).Add(RitualTargeting.Root(target));
        Struck = true;
    }
}
