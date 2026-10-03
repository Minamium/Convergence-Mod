#nullable enable
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Score = Convergence.Content.Encounters.FirstSeverance.Rewards.LacunaTestamentScore;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// A Lacuna Testament void pellet: leaves an open iris toward the cursor and seeks (RitualTargeting, 1800 px), one hit
// (penetrate 1), PelletLife ticks. ai[0] age, ai[1] the replicated target (-1 none), ai[2] the iris (0..6), ExtraAI
// whether it struck. Spawned by the owner's LacunaIrisChannel through native projectile replication; it stops once
// its owner is unusable. A standalone type (not a RitualBolt), so no legacy presentation draws it.
public sealed class LacunaPellet : ModProjectile
{
    internal float Age => Projectile.ai[0];
    internal int Iris => (int)Projectile.ai[2];
    // This pellet landed its hit, so its death is a bite and not a timeout. Set by the owner on the hit and sent to
    // every peer with one native projectile sync just before the kill, so peers never have to guess.
    internal bool Struck { get; private set; }

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
    public override bool? CanHitNPC(NPC target) => Struck ? false : null;

    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] < 0 || Projectile.ai[2] >= Score.Irises)
        {
            Projectile.Kill();
            return;
        }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner))
        {
            Projectile.Kill();
            return;
        }
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        NPC? target = RitualTargeting.Acquire(Projectile, owner);
        if (target is not null) RitualTargeting.Home(Projectile, target.Center, target.velocity, Score.PelletSpeed);
        Projectile.rotation = Projectile.velocity.ToRotation();
    }

    // A sweep covers the fast head; the drawn wake is harmless.
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        float collision = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, Projectile.width, ref collision);
    }

    // Owner side (the client that deals the hit): mark the hit and sync it before penetrate 1 kills the pellet, so the
    // native kill that follows reaches every peer after the flag. Standard SyncProjectile; protocol unchanged.
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Struck = true;
        if (Main.netMode != NetmodeID.SinglePlayer) NetMessage.SendData(MessageID.SyncProjectile, number: Projectile.whoAmI);
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(Struck);

    public override void ReceiveExtraAI(BinaryReader reader) => Struck = reader.ReadBoolean();
}
