#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public abstract class RitualBolt : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();
    internal abstract RitualArmamentKind Kind { get; }
    internal virtual float Speed => 36;
    internal float Age => Projectile.ai[0];
    internal bool Empowered => Projectile.ai[2] > .5f;
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 20;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true; Projectile.DamageType = RitualArmamentItems.DamageClassFor(Kind);
        Projectile.penetrate = 1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1; Projectile.timeLeft = 300;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(RitualTargeting.Root(target)) ? false : null;
    public override bool? CanDamage() => Age >= 0 ? null : false;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner)) { Projectile.Kill(); return; }
        float previous = Age;
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        if (previous < 0)
        {
            // Lens opening is real windup, with no damaging stationary projectile.
            Projectile.Center = owner.MountedCenter + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 64;
            Projectile.rotation = Projectile.velocity.ToRotation();
            return;
        }
        if (Age < 1 && Kind == RitualArmamentKind.Ranged && Empowered) Projectile.penetrate = 3;
        NPC? target = RitualTargeting.Acquire(Projectile, owner, excluded: hitRoots);
        if (target is not null) RitualTargeting.Home(Projectile, target.Center, target.velocity, Speed);
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => hitRoots.Add(RitualTargeting.Root(target));
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        // A sweep covers the fast head, while the long visual wake is harmless.
        float collision = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, Projectile.width, ref collision);
    }
}
public sealed class RefrainEcho : RitualBolt { internal override RitualArmamentKind Kind => RitualArmamentKind.Melee; }
public sealed class MeridianNeedle : RitualBolt
{
    internal override RitualArmamentKind Kind => RitualArmamentKind.Ranged;
    internal override float Speed => Empowered ? 52 : 44;
}
public sealed class LacunaRay : RitualBolt { internal override RitualArmamentKind Kind => RitualArmamentKind.Magic; }
public sealed class ChoirNote : RitualBolt
{
    internal override RitualArmamentKind Kind => RitualArmamentKind.Summon;
    public override void SetStaticDefaults()
    { base.SetStaticDefaults(); ProjectileID.Sets.MinionShot[Type] = true; }
}
public sealed class WitnessEcho : RitualBolt { internal override RitualArmamentKind Kind => RitualArmamentKind.Rogue; }

public sealed class WitnessBlade : ModProjectile
{
    private readonly HashSet<int> hits = new();
    private bool returning, echoesReleased;
    internal bool Returning => returning;
    internal bool Stealth => ((int)Projectile.ai[2] & 1) != 0;
    internal float Age => Projectile.ai[0];
    public override string Texture => RitualArmamentItems.TexturePath;
    public override void SetStaticDefaults()
    { ProjectileID.Sets.TrailCacheLength[Type] = 24; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 38;
        Projectile.friendly = true; Projectile.DamageType = RitualArmamentItems.DamageClassFor(RitualArmamentKind.Rogue);
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1; Projectile.timeLeft = 280;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => hits.Contains(RitualTargeting.Root(target)) ? false : null;
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        => modifiers.SourceDamage *= RitualArmamentRules.RogueShare(returning);
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] < 0 || Projectile.ai[2] > 3)
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!RitualArmamentItems.Usable(owner)) { Projectile.Kill(); return; }
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        Projectile.rotation += .22f / Projectile.MaxUpdates;
        if (!echoesReleased && Age >= 12)
        {
            echoesReleased = true;
            if (Stealth && Projectile.owner == Main.myPlayer)
                for (int i = 0; i < 3; i++)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                        Projectile.velocity.RotatedBy((i - 1) * .65f), ModContent.ProjectileType<WitnessEcho>(),
                        RitualArmamentRules.ScaledDamage(Projectile.damage, .20f), Projectile.knockBack * .4f,
                        Projectile.owner, 0, -1, 1);
        }
        bool returnRequested = Age >= 42 || ((int)Projectile.ai[2] & 2) != 0;
        if (!returning && returnRequested)
        {
            returning = true; hits.Clear(); Array.Clear(Projectile.localNPCImmunity);
            if (Projectile.owner == Main.myPlayer) { Projectile.ai[2] = Stealth ? 3 : 2; Projectile.netUpdate = true; }
        }
        if (returning)
        {
            RitualTargeting.Home(Projectile, owner.MountedCenter, owner.velocity, 46);
            if (Vector2.DistanceSquared(Projectile.Center, owner.MountedCenter) < 32 * 32) Projectile.Kill();
        }
        else
        {
            NPC? target = RitualTargeting.Acquire(Projectile, owner);
            if (target is not null) RitualTargeting.Home(Projectile, target.Center, target.velocity, 34);
        }
    }
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        float collision = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, 32, ref collision);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hits.Add(RitualTargeting.Root(target));
        // A hit opens a short outbound flourish; recall is smooth, not a teleport.
        if (!returning && Projectile.owner == Main.myPlayer)
        { Projectile.ai[0] = Math.Max(Projectile.ai[0], 34); Projectile.netUpdate = true; }
    }
}
