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
    private int idle, stuck, ownerGroundedTicks;
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
        // Zero vertical speed also occurs while hovering or riding horizontally.
        // The owner selects locomotion; peers render its existing replicated ai[2].
        bool ownerRequiresBroom = DollCompanionRules.OwnerRequiresBroom(owner.mount.Active,
            HasFootSupport(owner), owner.velocity.Y, owner.gravDir);
        bool landed = Math.Abs(Projectile.velocity.Y) < .1f;
        if (authority)
        {
            ownerGroundedTicks = DollCompanionRules.GroundedTicks(ownerRequiresBroom, ownerGroundedTicks);
            bool catchup = Vector2.DistanceSquared(Projectile.Bottom, footHome) > 360 * 360
                || Math.Abs(Projectile.Bottom.Y - owner.Bottom.Y) > 140 || stuck > 36;
            // Settle only after a stable dismounted landing and a clear body.
            bool canLand = Math.Abs(Projectile.Bottom.X - footHome.X) < 80
                && Projectile.Bottom.Y >= owner.Bottom.Y - 18 && Projectile.Bottom.Y <= owner.Bottom.Y + 12
                && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
            bool fly = DollCompanionRules.UseBroom(Projectile.ai[2] == 1, ownerRequiresBroom,
                ownerGroundedTicks, catchup, canLand);
            if (Projectile.ai[2] != (fly ? 1 : 0)) { Projectile.ai[2] = fly ? 1 : 0; Projectile.netUpdate = true; }
        }
        bool floating = Projectile.ai[2] == 1;
        Projectile.tileCollide = !floating;
        if (floating)
        {
            Vector2 destination = ownerRequiresBroom ? owner.Center + new Vector2(-owner.direction * 92, -52 * owner.gravDir)
                : footHome - new Vector2(0, Projectile.height * .5f + 4);
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
        Vector2 hand = Hand(Projectile);
        Vector2 axis = RitualArmamentItems.Aim(target.Center - hand, Projectile.spriteDirection);
        Vector2 muzzle = hand + axis * 35;
        if (DollCompanionRules.NeedleAt(age))
        {
            int lane = age == DollCompanionRules.NeedleTick(0) ? 0 : age == DollCompanionRules.NeedleTick(1) ? 1 : 2;
            Vector2 at = Sigil(hand, axis, lane, age);
            Vector2 aim = RitualArmamentItems.Aim(target.Center - at, Projectile.spriteDirection);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, aim * 28,
                ModContent.ProjectileType<DollNeedle>(), RitualArmamentRules.ScaledDamage(Projectile.damage, DollCompanionRules.NeedleDamageScale),
                Projectile.knockBack, Projectile.owner,
                0, target.whoAmI, Projectile.identity);
        }
        if (age == DollCompanionRules.Verdict)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), muzzle, axis,
                ModContent.ProjectileType<DollLacunaBeam>(), RitualArmamentRules.ScaledDamage(Projectile.damage, DollCompanionRules.BeamDamageScale),
                Projectile.knockBack, Projectile.owner, 0, target.whoAmI, Projectile.identity);
    }
    public override bool OnTileCollide(Vector2 oldVelocity) => false;

    private static bool HasFootSupport(Player owner)
    {
        if (owner.mount.Active || owner.gravDir < 0) return false;
        // Native shape-aware point tests include platforms, slopes and half
        // blocks. Unlike running player collision, this does not move anything.
        for (int sample = 0; sample < 3; sample++)
        {
            Vector2 point = new(owner.position.X + 2 + sample * (owner.width - 4) * .5f, owner.Bottom.Y + 2);
            if (WorldGen.InWorld((int)(point.X / 16), (int)(point.Y / 16), 1)
                && Collision.IsWorldPointSolid(point, treatPlatformsAsNonSolid: false)) return true;
        }
        return false;
    }
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    { fallThrough = Main.player[Projectile.owner].Bottom.Y > Projectile.Bottom.Y + 48; return true; }

    internal static Vector2 Hand(Projectile parent) => parent.Center
        + new Vector2(parent.spriteDirection * 17, -8).RotatedBy(parent.rotation);

    internal static Vector2 Sigil(Vector2 hand, Vector2 axis, int index, float age)
    {
        Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
        Vector2 muzzle = hand + axis * 35;
        Vector2 offset = index == 0 ? axis * 8 : -axis * 7 + normal * (index == 1 ? -34 : 34);
        float birth = 1 - MathF.Pow(1 - Math.Clamp((age - DollCompanionRules.SigilBirth(index)) / 6, 0, 1), 4);
        Vector2 at = muzzle + offset + normal * (index == 2 ? 1 : -1) * (1 - birth) * 28;
        return Vector2.Lerp(at, muzzle, DollCompanionRules.MergeAmount(age));
    }

    internal static bool ParentAlive(Projectile child) => TryGetParent(child, out _);

    internal static bool TryGetParent(Projectile child, out Projectile parent)
    {
        parent = null!;
        if (!RitualTargeting.ValidState(child) || child.ai[2] < 0 || child.ai[2] != (int)child.ai[2]) return false;
        Player owner = Main.player[child.owner];
        if (!RitualArmamentItems.Usable(owner) || owner.noItems || owner.CCed || !owner.HasBuff(ModContent.BuffType<DollCovenantBuff>())) return false;
        foreach (Projectile candidate in Main.ActiveProjectiles)
            if (candidate.owner == child.owner && candidate.identity == (int)child.ai[2] && candidate.ModProjectile is DollCompanion)
            { parent = candidate; return true; }
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
    public override bool CanHitPvp(Player target) => false;
}

// Ordinary native minion shot, exact owner + parent identity. A sustained
// connected beam is one projectile, never dozens of per-tick laser spawns.
public sealed class DollLacunaBeam : ModProjectile
{
    private readonly ulong[] nextRootHit = new ulong[Main.maxNPCs];
    internal float Age => Projectile.ai[0];
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);
    internal float Opening => DollCompanionRules.BeamScale(Age);
    internal float Reach => DollCompanionRules.BeamLength * Opening;
    internal float Width => DollCompanionRules.BeamWidth * Opening;
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2200;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = DollCompanionRules.BeamTicks + DollCompanionRules.BeamAfterglow; Projectile.penetrate = -1;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = DollCompanionRules.BeamHitCadence;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!DollCompanion.TryGetParent(Projectile, out Projectile parent)
            || parent.ai[0] < DollCompanionRules.Verdict || parent.ai[0] >= DollCompanionRules.Cycle
            || Age < 0 || Age >= DollCompanionRules.BeamTicks + DollCompanionRules.BeamAfterglow)
        { Projectile.Kill(); return; }
        Projectile.ai[0]++;
        Vector2 hand = DollCompanion.Hand(parent);
        if (Projectile.owner == Main.myPlayer)
        {
            NPC? target = RitualTargeting.Current(Projectile);
            if (target is not null)
            {
                float desired = (target.Center - hand).ToRotation();
                Projectile.velocity = Axis.ToRotation().AngleTowards(desired, .052f).ToRotationVector2();
            }
            if ((int)Age % 6 == 0) Projectile.netUpdate = true;
        }
        Projectile.Center = hand + Axis * 35;
        Projectile.rotation = Axis.ToRotation();
        Projectile.damage = RitualArmamentRules.ScaledDamage(parent.damage, DollCompanionRules.BeamDamageScale);
    }
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => DollCompanionRules.BeamLive(Age) && Opening > 0 ? null : false;
    public override bool? CanHitNPC(NPC target) => Main.GameUpdateCount < nextRootHit[RitualTargeting.Root(target)] ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        if (!DollCompanionRules.BeamLive(Age) || Opening <= 0) return false;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Axis * Reach, Width, ref point);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => nextRootHit[RitualTargeting.Root(target)] = Main.GameUpdateCount + DollCompanionRules.BeamHitCadence;
}
