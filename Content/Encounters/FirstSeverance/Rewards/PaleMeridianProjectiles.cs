#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Pale Meridian (2026-10 refresh; docs/encounters/first-severance/WEAPONS.md "Pale Meridian"). Native weapon
// ownership: only the owning client reads the trigger and cursor, spends ammo (one PickAmmo per real round and one for
// the finisher) and spawns rounds and release lines through native projectile replication. Every count a peer sees
// (seated parts, the key, the tier) follows from the holdout's replicated ai. No packet. The three types are new; the
// 0.2.34 MeridianBastion/MeridianNeedle types stay in the code, unused.

// The held music-box siege rifle.
//  ai[0] = score age (frozen at the release), ai[1] = -1 while held, then the finisher tier that fired (0 = none),
//  ai[2] = 0 while held, -1 .. -StowTicks while it packs away. velocity = the aim (unit), locked at the release.
public sealed class MeridianHoldout : ModProjectile
{
    internal const float Held = -1;

    internal float Age => Projectile.ai[0];
    internal bool Packing => Projectile.ai[2] < 0;
    internal int Fired => Math.Max(0, (int)Projectile.ai[1]);
    internal float Stowed => -Projectile.ai[2];
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);

    public override string Texture => RitualArmamentItems.TexturePath;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    private bool Sane() => RitualTargeting.ValidState(Projectile) && Projectile.ai[0] >= 0
        && Projectile.ai[1] is >= Held and <= 3 && Projectile.ai[2] <= 0 && Projectile.ai[2] >= -PaleMeridianScore.StowTicks;

    public override void AI()
    {
        if (!Sane()) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // Item change, death, Down/elimination, noItems or crowd control: gone at once, no finisher, no more ammo.
        if (!RitualChannel.Valid(Projectile, owner, ModContent.ItemType<PaleMeridian>())) { Projectile.Kill(); return; }
        if (Packing)
        {
            Stow(owner);
            return;
        }
        if (Projectile.owner == Main.myPlayer && !owner.channel)
        {
            Release(owner);
            return;
        }
        RitualChannel.Hold(Projectile, owner, Age >= PaleMeridianScore.Ignite ? PaleMeridianScore.OverchargeTurn : PaleMeridianScore.BuildTurn);
        Pose(owner);
        Projectile.ai[0]++;
        int age = (int)Projectile.ai[0];
        MeridianShot shot = PaleMeridianScore.Shot(age);
        if (Projectile.owner != Main.myPlayer || shot == MeridianShot.None) return;
        if (!owner.PickAmmo(owner.HeldItem, out _, out _, out int damage, out float knockback, out int ammo))
        {
            // Out of ammo while holding: the gun packs itself away, with no finisher.
            PackAway(0);
            return;
        }
        Vector2 axis = Axis;
        Vector2 muzzle = MeridianVectors.X(PaleMeridianRig.MuzzleAt(MeridianVectors.N(Projectile.Center), MeridianVectors.N(axis)));
        Vector2 focus = owner.MountedCenter + axis * PaleMeridianScore.Focus;
        Vector2 direction = RitualArmamentItems.Aim(focus - muzzle, owner.direction);
        float speed = shot switch
        {
            MeridianShot.Note => PaleMeridianScore.NoteSpeed,
            MeridianShot.Heavy => PaleMeridianScore.HeavyLaunch,
            _ => PaleMeridianScore.RoundLaunch,
        };
        Projectile.NewProjectile(owner.GetSource_ItemUse_WithPotentialAmmo(owner.HeldItem, ammo), muzzle, direction * (speed / 2),
            ModContent.ProjectileType<MeridianRound>(), RitualArmamentRules.ScaledDamage(damage, PaleMeridianScore.Factor(shot)),
            knockback, Projectile.owner, 0, -1, (float)shot);
    }

    // Letting go: the finisher locks onto the line from the player through the cursor and costs one more PickAmmo.
    // Without a seated part, without ammo, unfocused or over the fullscreen map the gun just packs away.
    private void Release(Player owner)
    {
        int tier = PaleMeridianScore.Tier((int)Age);
        int fired = 0;
        Vector2 pivot = owner.RotatedRelativePoint(owner.MountedCenter, true);
        if (PaleMeridianScore.HasFinisher(tier) && Main.hasFocus && !Main.mapFullscreen
            && owner.PickAmmo(owner.HeldItem, out _, out _, out int damage, out float knockback, out int ammo))
        {
            Vector2 direction = RitualArmamentItems.Aim(Main.MouseWorld - pivot, owner.direction);
            Vector2 origin = MeridianVectors.X(PaleMeridianRig.MuzzleAt(MeridianVectors.N(pivot), MeridianVectors.N(direction)));
            float node = PaleMeridianLattice.Node(Vector2.Dot(Main.MouseWorld - origin, direction));
            var source = owner.GetSource_ItemUse_WithPotentialAmmo(owner.HeldItem, ammo);
            int type = ModContent.ProjectileType<MeridianLine>();
            Projectile.NewProjectile(source, origin, direction, type,
                RitualArmamentRules.ScaledDamage(damage, PaleMeridianScore.MeridianFactor(tier)), knockback, Projectile.owner, 0, node, tier);
            if (PaleMeridianScore.HasLattice(tier))
                Projectile.NewProjectile(source, origin, direction, type,
                    RitualArmamentRules.ScaledDamage(damage, PaleMeridianScore.LatticeFactor(tier)), knockback, Projectile.owner,
                    PaleMeridianLattice.LatticeStart(node), node, tier + MeridianLine.LatticeFlag);
            Projectile.velocity = direction;
            fired = tier;
        }
        Projectile.Center = pivot;
        PackAway(fired);
    }

    // Set inside AI: a netUpdate raised outside the projectile's own update is cleared before it is sent.
    private void PackAway(int fired)
    {
        Projectile.ai[1] = fired;
        Projectile.ai[2] = -1;
        Projectile.timeLeft = 2;
        Projectile.netUpdate = true;
    }

    private void Stow(Player owner)
    {
        Projectile.ai[2]--;
        Projectile.timeLeft = 2;
        Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter, true);
        if (Stowed < PaleMeridianScore.StowTicks / 2) Pose(owner);
        if (Projectile.ai[2] <= -PaleMeridianScore.StowTicks) Projectile.Kill();
    }

    // The front hand on the grip and the back hand toward the fore-end (the draw refines this per frame).
    private void Pose(Player owner)
    {
        NVector2 axis = MeridianVectors.N(Axis);
        bool mirrored = PaleMeridianRig.Mirrored(axis, owner.gravDir);
        NVector2 grip = PaleMeridianRig.World(MeridianVectors.N(Projectile.Center), axis, mirrored, PaleMeridianRig.Grip);
        Vector2 reach = new Vector2(grip.X, grip.Y) - owner.MountedCenter;
        float front = reach.LengthSquared() > 1 ? reach.ToRotation() : Projectile.rotation;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, front - MathHelper.PiOver2);
        owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation - MathHelper.PiOver2 + .12f * owner.direction);
    }
}

// One round: a build note (straight), an overcharge round (homing) or a heavy round (homing, pierces 3).
//  ai[0] = age in ticks, ai[1] = homing target (-1 none), ai[2] = MeridianShot.
public sealed class MeridianRound : ModProjectile
{
    // Logical NPC roots (realLife or whoAmI) already hit; null in the type's template.
    private HashSet<int>? hitRoots;

    internal MeridianShot Kind => (MeridianShot)(int)Projectile.ai[2];
    internal float Age => Projectile.ai[0];

    public override string Texture => RitualArmamentItems.TexturePath;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 12;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1;
        Projectile.timeLeft = PaleMeridianScore.RoundLife * 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool PreDraw(ref Color lightColor) => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanHitNPC(NPC target) => hitRoots?.Contains(RitualTargeting.Root(target)) == true ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => (hitRoots ??= new HashSet<int>()).Add(RitualTargeting.Root(target));

    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Kind is < MeridianShot.Note or > MeridianShot.Heavy) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // Launched rounds outlive an item change, never their owner's death, Down or elimination.
        if (!RitualArmamentItems.Usable(owner)) { Projectile.Kill(); return; }
        if (Age <= 0 && Kind == MeridianShot.Heavy) Projectile.penetrate = PaleMeridianScore.HeavyPierce;
        Projectile.ai[0] += 1f / Projectile.MaxUpdates;
        if (Kind != MeridianShot.Note)
        {
            NPC? target = RitualTargeting.Acquire(Projectile, owner, excluded: hitRoots);
            if (target is not null)
                RitualTargeting.Home(Projectile, target.Center, target.velocity,
                    Kind == MeridianShot.Heavy ? PaleMeridianScore.HeavySeek : PaleMeridianScore.RoundSeek);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
    }

    // A sweep from the last update covers the fast head; the drawn wake is harmless.
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collision = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, Projectile.width, ref collision);
    }
}

// A release line: the meridian, or (ai[2] >= LatticeFlag) the lattice around its node. position = the muzzle at
// the release, velocity = the unit direction; ai[0] = age (the lattice starts negative), ai[1] = node distance,
// ai[2] = tier (+ LatticeFlag). Damaging only while PaleMeridianLattice.Live lights a stretch; one hit per NPC root.
// Lines pass through tiles.
public sealed class MeridianLine : ModProjectile
{
    internal const int LatticeFlag = 4;

    private List<int>? struck;

    internal float Age => Projectile.ai[0];
    internal float Node => Projectile.ai[1];
    internal int Tier => (int)Projectile.ai[2] % LatticeFlag;
    internal bool Lattice => Projectile.ai[2] >= LatticeFlag;
    internal NVector2 Origin => MeridianVectors.N(Projectile.Center);
    internal NVector2 Direction => MeridianVectors.N(Projectile.velocity);
    internal int End => Lattice ? PaleMeridianLattice.LatticeEnd : PaleMeridianLattice.MeridianEnd;

    public override string Texture => RitualArmamentItems.TexturePath;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = 2;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;

    private bool Sane() => Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && float.IsFinite(Projectile.ai[0])
        && Projectile.ai[2] == MathF.Floor(Projectile.ai[2]) && Projectile.ai[2] >= 0 && Projectile.ai[2] < 2 * LatticeFlag
        && PaleMeridianLattice.Valid(Origin, Direction, Node, Tier, Lattice) && Age > -64;

    public override void AI()
    {
        if (!Sane() || !RitualArmamentItems.Usable(Main.player[Projectile.owner])) { Projectile.Kill(); return; }
        Projectile.ai[0]++;
        Projectile.timeLeft = 2;
        if (Age >= End) Projectile.Kill();
    }

    private int Live(Span<MeridianSegment> segments)
        => PaleMeridianLattice.Live(Origin, Direction, Node, Tier, Lattice, Age, segments);

    public override bool? CanDamage()
    {
        Span<MeridianSegment> segments = stackalloc MeridianSegment[PaleMeridianLattice.MaxSegments];
        return Live(segments) > 0 ? null : false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Span<MeridianSegment> segments = stackalloc MeridianSegment[PaleMeridianLattice.MaxSegments];
        int count = Live(segments);
        NVector2 centre = new(targetHitbox.Center.X, targetHitbox.Center.Y), half = new(targetHitbox.Width * .5f, targetHitbox.Height * .5f);
        for (int i = 0; i < count; i++)
            if (PaleMeridianLattice.Hits(segments[i], centre, half)) return true;
        return false;
    }

    public override bool? CanHitNPC(NPC target) => struck?.Contains(RitualTargeting.Root(target)) == true ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => (struck ??= new List<int>(4)).Add(RitualTargeting.Root(target));
}

// XNA <-> System.Numerics vectors for the pure Pale Meridian rules.
internal static class MeridianVectors
{
    internal static NVector2 N(Vector2 v) => new(v.X, v.Y);
    internal static Vector2 X(NVector2 v) => new(v.X, v.Y);
}
