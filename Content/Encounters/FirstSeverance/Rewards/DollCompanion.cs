#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Ordinary owner-replicated minion. Never a Raid participant, NPC proxy, or Ready vote.
public sealed class DollCompanion : ModProjectile
{
    private float walk;
    private int idle, stuck;
    public override string Texture => "Convergence/Assets/Textures/NPCs/DollTheater/DollCompanion";
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true; Main.projFrames[Type] = DollCompanionRules.FrameCount;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 22; Projectile.height = 44;
        Projectile.minion = true; Projectile.minionSlots = DollCompanionRules.Slots;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.ignoreWater = true;
        Projectile.tileCollide = false; Projectile.netImportant = true; Projectile.timeLeft = 18000;
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[0] < 0 || Projectile.ai[0] >= DollCompanionRules.Cycle
            || Projectile.ai[2] is not (0 or 1)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        int buff = ModContent.BuffType<DollCovenantBuff>();
        if (!owner.active || owner.dead) { owner.ClearBuff(buff); Projectile.Kill(); return; }
        if (!owner.HasBuff(buff)) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        bool usable = RitualArmamentItems.Usable(owner) && !owner.noItems && !owner.CCed;
        NPC? target = usable ? RitualTargeting.Acquire(Projectile, owner, manual: true) : null;
        bool authority = Projectile.owner == Main.myPlayer;
        float before = Projectile.ai[0];
        Projectile.ai[0] = target is not null ? (before + 1) % DollCompanionRules.Cycle : 0;
        if (authority && (before > 0 && Projectile.ai[0] == 0 || Projectile.ai[0] > 0 && (int)Projectile.ai[0] % 45 == 0)) Projectile.netUpdate = true;

        Vector2 footHome = owner.Bottom - new Vector2(owner.direction * 70, 0);
        bool ownerGrounded = Math.Abs(owner.velocity.Y) < .2f;
        bool landed = Math.Abs(Projectile.velocity.Y) < .1f;
        if (authority)
        {
            bool fly = Projectile.ai[2] == 1;
            if (!ownerGrounded || Vector2.DistanceSquared(Projectile.Bottom, footHome) > 360 * 360
                || Math.Abs(Projectile.Bottom.Y - owner.Bottom.Y) > 140 || stuck > 36) fly = true;
            // Settle only near the ground; do not switch collision on inside blocks.
            else if (fly && Math.Abs(Projectile.Bottom.X - footHome.X) < 80
                && Projectile.Bottom.Y >= owner.Bottom.Y - 18 && Projectile.Bottom.Y <= owner.Bottom.Y + 12
                && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height)) fly = false;
            if (Projectile.ai[2] != (fly ? 1 : 0)) { Projectile.ai[2] = fly ? 1 : 0; Projectile.netUpdate = true; }
        }
        bool floating = Projectile.ai[2] == 1;
        Projectile.tileCollide = !floating;
        if (floating)
        {
            Vector2 destination = ownerGrounded ? footHome - new Vector2(0, Projectile.height * .5f + 4)
                : owner.Center + new Vector2(-owner.direction * 92, -52);
            Vector2 desired = (destination - Projectile.Center) * .11f;
            float limit = Math.Max(15, owner.velocity.Length() + 5);
            if (desired.LengthSquared() > limit * limit) desired = Vector2.Normalize(desired) * limit;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .19f);
            stuck = 0;
        }
        else
        {
            float delta = footHome.X - Projectile.Bottom.X;
            float wanted = Math.Abs(delta) < 12 ? 0 : Math.Clamp(delta * .10f, -8, 8);
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, wanted, .18f);
            Projectile.velocity.Y = Math.Min(11, Projectile.velocity.Y + .42f);
            bool wall = Collision.SolidCollision(Projectile.position + new Vector2(Math.Sign(wanted) * 12, 0), Projectile.width, Projectile.height - 5);
            if (landed && (wall || owner.Bottom.Y < Projectile.Bottom.Y - 28)) Projectile.velocity.Y = -8;
            stuck = wall && Math.Abs(delta) > 24 ? stuck + 1 : 0;
        }
        if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > 2200 * 2200 && authority)
        {
            Projectile.Center = owner.Center - new Vector2(owner.direction * 48, 48);
            Projectile.velocity = Vector2.Zero; Projectile.ai[2] = 1; Projectile.tileCollide = false; Projectile.netUpdate = true;
        }
        float facing = target is not null ? target.Center.X - Projectile.Center.X : Projectile.velocity.X;
        if (Math.Abs(facing) > .3f) Projectile.spriteDirection = facing >= 0 ? 1 : -1;
        Projectile.rotation = MathHelper.Lerp(Projectile.rotation, floating ? Projectile.velocity.X * .012f : 0, .15f);
        idle = (idle + 1) % 2520; walk = (walk + Math.Abs(Projectile.velocity.X)) % 40;
        Projectile.frame = DollCompanionRules.Frame(Projectile.ai[0], floating, Math.Abs(Projectile.velocity.X) > .4f ? walk : -1, idle);
        if (!authority || target is null || !usable) return;
        int age = (int)Projectile.ai[0];
        Vector2 muzzle = Projectile.Center + new Vector2(Projectile.spriteDirection * 17, -8);
        if (DollCompanionRules.NeedleAt(age))
        {
            Vector2 aim = RitualArmamentItems.Aim(target.Center - muzzle, Projectile.spriteDirection);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), muzzle, aim * 28,
                ModContent.ProjectileType<DollNeedle>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
                0, target.whoAmI, Projectile.identity);
        }
        if (age == DollCompanionRules.Verdict)
            for (int side = -1; side <= 1; side += 2)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, new Vector2(1, side).SafeNormalize(Vector2.UnitX),
                    ModContent.ProjectileType<DollSeam>(), RitualArmamentRules.ScaledDamage(Projectile.damage, 1.5f),
                    Projectile.knockBack, Projectile.owner, 0, -1, Projectile.identity);
    }
    public override bool OnTileCollide(Vector2 oldVelocity) => false;
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    { fallThrough = Main.player[Projectile.owner].Bottom.Y > Projectile.Bottom.Y + 48; return true; }

    internal static bool ParentAlive(Projectile child)
    {
        if (!RitualTargeting.ValidState(child)) return false;
        Player owner = Main.player[child.owner];
        if (!RitualArmamentItems.Usable(owner) || owner.noItems || owner.CCed || !owner.HasBuff(ModContent.BuffType<DollCovenantBuff>())) return false;
        foreach (Projectile parent in Main.ActiveProjectiles)
            if (parent.owner == child.owner && parent.identity == (int)child.ai[2] && parent.ModProjectile is DollCompanion) return true;
        return false;
    }
}

public sealed class DollNeedle : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.timeLeft = 65; Projectile.penetrate = 1;
    }
    public override void AI()
    {
        if (!DollCompanion.ParentAlive(Projectile)) { Projectile.Kill(); return; }
        Projectile.ai[0]++;
        if (RitualTargeting.Current(Projectile) is { } target)
            RitualTargeting.Home(Projectile, target.Center, target.velocity, 48);
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
}

public sealed class DollSeam : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 24; Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!DollCompanion.ParentAlive(Projectile)) { Projectile.Kill(); return; }
        Projectile.ai[0]++; Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override bool? CanDamage() => Projectile.ai[0] is >= 5 and <= 9 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        Vector2 axis = RitualArmamentItems.Aim(Projectile.velocity, 1) * 132;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - axis, Projectile.Center + axis, 22, ref point);
    }
}
