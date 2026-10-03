#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Melee: Sable Scythe (REWARDS.md, "Melee - Sable Scythe"), ordinary Melee since 2026-10-03. Left click swings the figure
// eight (Over, Under, Over, Under, Whip) through one held SableStroke at a time; each Over and Under throws a homing
// SableCrescent at age 9 and the Whip's lash arc sheds a volley of five at age 20; every stroke whose blade, lash arc or
// crescents connect engraves one line of the staff carried by SableStaff; right click casts Staff Reap: a held
// SableRelease pose and up to six StaffCut children, all scheduled at the cast. Owner-client authority: only the owner
// samples input, keeps the combo, spawns projectiles, picks crescent targets and deals damage; everything another client
// draws is a pure function of native projectile state (netImportant, netUpdate). No packet. Client art enters through
// Client/Encounters/CrimsonFoundry/Rewards/Scythe*.cs.
public sealed class CrimsonSableScythe : ModItem
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Scythe);

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Melee);
        Item.width = Item.height = 56;
        Item.useStyle = ItemUseStyleID.Shoot;
        // useTime = useAnimation = 2 (the shared table): each stroke owns its own clock and the next one starts the
        // tick after the previous one ends.
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<SableStroke>();
        Item.shootSpeed = 1f;
    }

    public override bool MeleePrefix() => true;
    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        var state = player.GetModPlayer<SableScythePlayer>();
        // A cast release holds the scythe for its whole follow-through, through item swaps too (the lock lives on the
        // player, not on the held pose, which an item swap kills).
        if (!CrimsonRewardItems.CanAct(player) || state.Releasing || SableRelease.Any(player)) return false;
        if (player.altFunctionUse == 2)
        {
            if (SableStaff.Lines(player) <= 0) return false;
            // Pressed during a stroke: the release begins when that stroke's live window ends.
            if (SableStroke.Running(player)) { state.QueueRelease(); return false; }
            return true;
        }
        // Everything the scythe deals is ordinary Melee (owner decision 4): there is no class to switch.
        return !SableStroke.Busy(player);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
        int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var state = player.GetModPlayer<SableScythePlayer>();
        if (player.altFunctionUse == 2)
        {
            // A stroke that just ended (or none): the release blends from wherever its blade was.
            SableRelease.Cast(player, source, damage, knockback, SableStroke.Latest(player));
            return false;
        }
        float aim = CrimsonRewardItems.Aim(velocity, player.direction).ToRotation();
        int stroke = state.TakeStroke();
        int index = Projectile.NewProjectile(source, player.MountedCenter, state.FromFor(aim), type, damage, knockback,
            player.whoAmI, stroke, aim, 0);
        if (index >= 0 && index < Main.maxProjectiles)
        {
            if (Main.projectile[index].ModProjectile is SableStroke cast) cast.Serial = state.StrokeSerial;
            Main.projectile[index].netUpdate = true;
        }
        return false;
    }

    // The live weapon damage and knockback, for a release cast from a stroke and for the crescents a stroke throws.
    internal static int LiveDamage(Player player, Item item, out float knockback)
    {
        knockback = player.GetWeaponKnockback(item, item.knockBack);
        return player.GetWeaponDamage(item);
    }
}

// Shared pose helpers for the held projectiles: the shoulder (the native arm's centre, mirrored to the accepted facing,
// dedicated-server safe) and the aim frame to the world.
internal static class SableScytheFrame
{
    internal static Vector2 Shoulder(Player owner, int facing)
    {
        var basis = OboroHandAnchor.Capture(owner, facing);
        return owner.MountedCenter + new Vector2(basis.X, basis.Y);
    }
    internal static Vector2 World(Vector2 shoulder, NVector local, float aim, int facing)
    {
        NVector world = SableScytheMotion.ToWorld(local, aim, facing);
        return shoulder + new Vector2(world.X, world.Y);
    }
    internal static NVector N(Vector2 v) => new(v.X, v.Y);
    internal static bool ValidOwner(Projectile projectile) => projectile.owner >= 0 && projectile.owner < Main.maxPlayers;
    internal static Vector2 X(NVector v) => new(v.X, v.Y);
    // A remote held projectile tolerates HeldLagTicks of held-item lag; the owner cancels at once.
    internal static bool HoldingScythe(Projectile projectile, Player owner)
    {
        if (owner.HeldItem.type == ModContent.ItemType<CrimsonSableScythe>()) { projectile.localAI[1] = 0; return true; }
        return projectile.owner != Main.myPlayer && ++projectile.localAI[1] <= CrimsonRewardRules.HeldLagTicks;
    }
}

// One figure-eight stroke held at the hand. ai = (stroke index 0-4, aim, age); velocity is data (ShouldUpdatePosition
// is false): X = what came before (-1 nothing, 0 the previous stroke, 1-5 a Staff Reap of that many lines),
// Y = the aim it was cast along, so the windup eases from the previous pose on every client. On the owner an Over or
// Under throws its crescent at age 9 and the Whip sheds the volley from its lash arc at age 20.
public sealed class SableStroke : ModProjectile
{
    internal const int LashPerTick = 4;
    private const int LashSamples = (CrimsonRewardRules.WhipLiveEnd - CrimsonRewardRules.WhipLiveStart) * LashPerTick + 1;
    private readonly CrimsonRootLedger bladeRoots = new(), lashRoots = new();
    private const byte BladePart = 1, LashPart = 2;
    private readonly byte[] parts = new byte[Main.maxNPCs];
    private readonly NVector[] lash = new NVector[LashSamples];
    private readonly NVector[] sweep = new NVector[CrimsonRewardRules.SweepSubsamples * 4];
    private Vector2 sweepShoulder;
    private int lashCount, sweepAge = -1;
    // The owner's serial for this stroke (SableThrowLedger): its blade, lash arc and crescents engrave one line at most.
    internal int Serial = -1;

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.ScytheHeld);
    internal int Stroke => Math.Clamp((int)Projectile.ai[0], 0, SableScytheMotion.Strokes - 1);
    internal float Aim => Projectile.ai[1];
    internal int Age => (int)Projectile.ai[2];
    internal int Facing => SableScytheMotion.Facing(Aim);
    internal int From => (int)Projectile.velocity.X;
    internal float FromAim => Projectile.velocity.Y;
    internal bool Finished => Age >= SableScytheMotion.Duration(Stroke);
    // The lash arc as written (world points, owner and peers each from their own accepted samples).
    internal ReadOnlySpan<NVector> Lash => lash.AsSpan(0, lashCount);

    // A finished stroke stays one tick so the next can begin from its end pose.
    internal static bool Busy(Player player) => Find(player, out var stroke) && !stroke!.Finished;
    internal static bool Running(Player player) => Busy(player);
    internal static SableStroke? Latest(Player player)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is SableStroke s) return s;
        return null;
    }
    internal static bool Find(Player player, out SableStroke? stroke)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is SableStroke s && !s.Finished) { stroke = s; return true; }
        stroke = null;
        return false;
    }

    // The pose at a (fractional) age in this stroke's own aim frame, eased in from what came before over the windup.
    internal SablePose PoseAt(float age)
    {
        SablePose pose = SableScytheMotion.Pose(Stroke, age);
        int ease = SableScytheMotion.LiveStart(Stroke);
        if (age >= ease || From < 0) return pose;
        float w = CrimsonRewardRules.Smooth(age / ease);
        int fromFacing = SableScytheMotion.Facing(FromAim);
        if (From == 0)
        {
            // The previous stroke ended in this pose along its own aim; turn the frame toward the new aim.
            if (fromFacing != Facing) return pose;
            return SableScytheMotion.Blend(SableScytheMotion.Reframe(pose, FromAim, fromFacing, Aim, Facing), pose, w);
        }
        // After Staff Reap: start from the release's held follow-through.
        SablePose held = SableScytheMotion.ReleasePose(SableScytheMotion.ReleaseTicks(From));
        return SableScytheMotion.Blend(SableScytheMotion.Reframe(held, FromAim, fromFacing, Aim, Facing), pose, w);
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = CrimsonRewardItems.DamageClassFor(CrimsonRewardKind.Melee);
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        // One tick of native immunity plus one root ledger per part (REWARDS.md, Melee, "Collision"): each part hits a
        // root once per stroke, and the Whip's blade and lash arc can both land on the same NPC.
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = SableScytheMotion.MaximumDuration + 4;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => SableScytheMotion.Live(Stroke, Age) || SableScytheMotion.LashLive(Stroke, Age) ? null : false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (!SableScytheFrame.ValidOwner(Projectile) || !SableScytheMotion.ValidStroke(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2],
                Projectile.velocity.X, Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!SableScytheMotion.HeldSurvives(owner.active && CrimsonRewardItems.CanAct(owner), SableScytheFrame.HoldingScythe(Projectile, owner)))
        { Projectile.Kill(); return; }
        if (owner.HeldItem.type != ModContent.ItemType<CrimsonSableScythe>()) return; // a peer waiting out held-item lag

        Projectile.ai[2]++;
        int age = Age, duration = SableScytheMotion.Duration(Stroke);
        if (age > duration) { Projectile.Kill(); return; }
        Vector2 shoulder = SableScytheFrame.Shoulder(owner, Facing);
        SablePose pose = PoseAt(age);
        Projectile.Center = SableScytheFrame.World(shoulder, pose.Hand, Aim, Facing);
        Projectile.rotation = Aim;
        Projectile.direction = Projectile.spriteDirection = Facing;
        RecordLash(shoulder, age);

        if (Projectile.owner != Main.myPlayer) return;
        if (SableScytheMotion.Live(Stroke, age)) Sweep(shoulder, age);
        if (age == 1 || age == SableScytheMotion.LiveStart(Stroke) || age % 6 == 0) Projectile.netUpdate = true;
        var state = owner.GetModPlayer<SableScythePlayer>();
        Throw(owner, state, shoulder, age);
        // A release pressed during this stroke begins when the stroke can no longer damage; the pose blends from here.
        if (state.ReleaseQueued && age >= SableScytheMotion.LastDamage(Stroke))
        {
            state.ClearQueue();
            Item item = owner.HeldItem;
            int damage = CrimsonSableScythe.LiveDamage(owner, item, out float knockback);
            if (SableRelease.Cast(owner, owner.GetSource_ItemUse(item), damage, knockback, this)) { Projectile.Kill(); return; }
        }
        if (age == duration) state.Ended(Aim, 0);
    }

    // Owner only, from a live stroke (so never while dead, Down, stunned or not holding the scythe): an Over or Under
    // throws one crescent at age 9 from the hook tip, at least 18 ticks after the last (counted on the player); the Whip's
    // lash arc sheds the volley at age 20 from five points along it. Damage is the live weapon damage at the throw.
    private void Throw(Player owner, SableScythePlayer state, Vector2 shoulder, int age)
    {
        int kind = SableScytheMotion.Kind(Stroke);
        bool whip = kind == SableScytheMotion.Whip;
        if (age != (whip ? CrimsonRewardRules.VolleyAge : CrimsonRewardRules.CrescentThrowAge)) return;
        if (!whip && !state.TryThrow(Main.GameUpdateCount)) return;
        Item item = owner.HeldItem;
        int damage = CrimsonSableScythe.LiveDamage(owner, item, out float knockback);
        var source = Projectile.GetSource_FromThis();
        if (!whip)
        {
            int crescent = kind == SableScytheMotion.Under ? SableCrescentFlight.Under : SableCrescentFlight.Over;
            Vector2 tip = SableScytheFrame.World(shoulder, SableScytheMotion.Tip(PoseAt(age)), Aim, Facing);
            SableCrescent.Throw(owner, source, crescent, tip, SableCrescentFlight.ThrowHeading(Aim, Facing, crescent), damage, knockback, Serial);
            return;
        }
        var arc = Lash;
        if (arc.Length < 2) return;
        for (int k = 0; k < CrimsonRewardRules.VolleyCrescents; k++)
        {
            NVector at = SableCrescentFlight.ArcPoint(arc, SableCrescentFlight.VolleyWritten(k), LashPerTick);
            SableCrescent.Throw(owner, source, SableCrescentFlight.FirstVolley + k, SableScytheFrame.X(at),
                SableCrescentFlight.VolleyHeading(Aim, k, arc[0], arc[^1]), damage, knockback, Serial);
        }
    }

    // The hook tip's path over the lash stays in the air as the lash arc: world points, four a tick, fixed where they
    // were swept. Sample i was written at WhipLiveStart + i / 4; a client that first sees the stroke late fills the
    // samples it missed from the pure pose at its own accepted shoulder.
    private void RecordLash(Vector2 shoulder, int age)
    {
        if (SableScytheMotion.Kind(Stroke) != SableScytheMotion.Whip || age <= CrimsonRewardRules.WhipLiveStart) return;
        int upto = Math.Min(lash.Length, (Math.Min(age, CrimsonRewardRules.WhipLiveEnd) - CrimsonRewardRules.WhipLiveStart) * LashPerTick + 1);
        for (; lashCount < upto; lashCount++)
        {
            NVector tip = SableScytheMotion.Tip(PoseAt(CrimsonRewardRules.WhipLiveStart + lashCount / (float)LashPerTick));
            lash[lashCount] = SableScytheFrame.N(SableScytheFrame.World(shoulder, tip, Aim, Facing));
        }
    }

    // Live ink radius of the lash arc at sample i (written at WhipLiveStart + i / 4), opened over 3 ticks.
    internal float LashRadius(int i, float age)
        => SableScytheMotion.LiveRadius(CrimsonRewardRules.LashRadius, age - (CrimsonRewardRules.WhipLiveStart + i / (float)LashPerTick));

    // Which parts touch this NPC: decided here with the NPC in hand (Colliding only sees boxes); ModifyHitNPC scales
    // the hit by the parts that landed and OnHitNPC records the root in each part's ledger.
    public override bool? CanHitNPC(NPC target)
    {
        int root = CrimsonRewardItems.Root(target);
        Player owner = Main.player[Projectile.owner];
        NVector min = new(target.Hitbox.Left, target.Hitbox.Top), max = new(target.Hitbox.Right, target.Hitbox.Bottom);
        byte touched = 0;
        if (SableScytheMotion.Live(Stroke, Age) && !bladeRoots.Contains(root) && BladeTouches(owner, target, min, max)) touched |= BladePart;
        if (SableScytheMotion.LashLive(Stroke, Age) && !lashRoots.Contains(root) && LashTouches(min, max)) touched |= LashPart;
        parts[target.whoAmI] = touched;
        return touched != 0 ? null : false;
    }

    // Soboro's swept blade: nine sub-samples of the accepted pose across the tick that just elapsed, in world space,
    // computed once per tick (the same capsules for every NPC) and checked against each hitbox.
    private void Sweep(Vector2 shoulder, int age)
    {
        Span<NVector> knots = stackalloc NVector[4];
        int start = SableScytheMotion.LiveStart(Stroke);
        for (int i = 0; i < CrimsonRewardRules.SweepSubsamples; i++)
        {
            SableScytheMotion.Blade(PoseAt(MathF.Max(start, age - 1f + i / (CrimsonRewardRules.SweepSubsamples - 1f))), knots);
            for (int k = 0; k < knots.Length; k++) sweep[i * 4 + k] = SableScytheFrame.N(SableScytheFrame.World(shoulder, knots[k], Aim, Facing));
        }
        sweepShoulder = shoulder;
        sweepAge = age;
    }

    private bool BladeTouches(Player owner, NPC target, NVector min, NVector max)
    {
        if (sweepAge != Age) return false;
        // Broadphase: nothing beyond the head of the haft can be cut.
        float reach = SableScytheMotion.Reach * 1.5f + SableScytheMotion.BladeRadius + 40;
        if (CrimsonRewardRules.BoxDistance(SableScytheFrame.N(sweepShoulder), min, max) > reach) return false;
        for (int i = 0; i < CrimsonRewardRules.SweepSubsamples; i++)
            if (SableScytheMotion.BladeTouches(sweep.AsSpan(i * 4, 4), min, max))
                return Collision.CanHitLine(sweepShoulder, 1, 1, target.position, target.width, target.height);
        return false;
    }

    private bool LashTouches(NVector min, NVector max)
    {
        for (int i = 0; i + 1 < lashCount; i++)
        {
            float r = MathF.Min(LashRadius(i, Age), LashRadius(i + 1, Age));
            if (r > 0 && CrimsonRewardRules.CapsuleTouchesBox(lash[i], lash[i + 1], r, min, max)) return true;
        }
        return false;
    }

    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox) => true;

    // The blade's multiplier and the lash arc's add when both land on the same tick; secondary hits scale the live
    // weapon damage the stroke was cast with.
    internal float PartMultiplier(byte touched)
        => ((touched & BladePart) != 0 ? SableScytheMotion.Multiplier(Stroke) : 0) + ((touched & LashPart) != 0 ? CrimsonRewardRules.LashMultiplier : 0);

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        float multiplier = PartMultiplier(parts[target.whoAmI]);
        if (multiplier > 0) modifiers.SourceDamage *= multiplier;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        int root = CrimsonRewardItems.Root(target);
        byte touched = parts[target.whoAmI];
        if ((touched & BladePart) != 0) bladeRoots.TryAdd(root);
        if ((touched & LashPart) != 0) lashRoots.TryAdd(root);
        parts[target.whoAmI] = 0;
        // Every stroke that connects, by its blade, lash arc or crescents, engraves one line of the staff.
        if (Projectile.owner != Main.myPlayer) return;
        Player owner = Main.player[Projectile.owner];
        if (owner.GetModPlayer<SableScythePlayer>().TryEngrave(Serial)) SableStaff.Engrave(owner, Projectile.GetSource_FromThis());
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
}

// A crescent (刈り月; REWARDS.md, "Melee - Sable Scythe", Crescents): an arc of live black blood thrown by a stroke that
// seeks the enemy nearest the cursor. ai = (target slot or -1, kind + 8 x break age, age in ticks); position and velocity
// are native with one extra update (velocity per update). The owner picks targets (at the throw, then every 4 ticks while
// it has none; a hit on its target chains to the nearest unhit NPC within 320 px or breaks it), deals the damage (x0.4,
// the volley's x0.2, of the live weapon damage at the throw; once per root, at most 3 roots) and engraves its stroke's
// line once. Every client steers with SableCrescentFlight toward the replicated slot (a slot empty on a peer: straight),
// so the owner's netUpdate (throw, target change, break, every 12 ticks) only corrects drift. Thrown, it flies through
// its owner's death, Down and item swaps (commitment); leaving the world ends it. Drawn in the shared reward ink layer
// (Client/Encounters/CrimsonFoundry/Rewards/ScytheVisuals.cs); the client keeps its scar after it ends.
public sealed class SableCrescent : ModProjectile
{
    private readonly CrimsonRootLedger roots = new();
    private readonly NVector[] body = new NVector[CrimsonRewardRules.CrescentSamples];
    private readonly float[] radius = new float[CrimsonRewardRules.CrescentSamples];
    private NVector heading;
    private ulong incarnation;
    private int stroke = -1;
    private bool engraved;
    private float nextAcquire;

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Scythe);
    internal int Target => (int)Projectile.ai[0];
    internal int Kind => SableCrescentFlight.KindOf(Projectile.ai[1]);
    internal int BreakAge => SableCrescentFlight.BreakAgeOf(Projectile.ai[1]);
    internal bool Broken => BreakAge > 0;
    internal float Age => Projectile.ai[2];
    // Live ticks left for the whole body: the material closes over the last 6 (the break or ticks 64-70).
    internal float Remaining => SableCrescentFlight.Remaining(Age, BreakAge);
    // The unit heading the body faces (the velocity's, held through a stop).
    internal NVector Heading
    {
        get
        {
            NVector v = SableScytheFrame.N(Projectile.velocity);
            if (v.LengthSquared() > 1e-6f) heading = NVector.Normalize(v);
            else if (heading.LengthSquared() < .5f) heading = NVector.UnitX;
            return heading;
        }
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        // Once per NPC natively, once per realLife root through the ledger, at most 3 roots; no ID-static immunity, so
        // every crescent of a volley lands on a boss.
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.extraUpdates = CrimsonRewardRules.CrescentExtraUpdates;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = (CrimsonRewardRules.CrescentLife + 4) * (1 + CrimsonRewardRules.CrescentExtraUpdates);
    }

    public override bool? CanDamage() => SableCrescentFlight.Done(Age, BreakAge) ? false : null;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (!SableScytheFrame.ValidOwner(Projectile)
            || !SableCrescentFlight.Valid(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2], SableScytheFrame.N(Projectile.velocity)))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; } // leaving the world clears everything
        float dt = SableCrescentFlight.Dt;
        Projectile.ai[2] = MathF.Min(Age + dt, CrimsonRewardRules.CrescentLife + 1);
        float age = Age;
        if (SableCrescentFlight.Done(age, BreakAge)) { Projectile.Kill(); return; }
        bool mine = Projectile.owner == Main.myPlayer;
        int updates = Projectile.MaxUpdates;
        NVector velocity = SableScytheFrame.N(Projectile.velocity) * updates, at = SableScytheFrame.N(Projectile.Center);
        if (Broken) velocity = SableCrescentFlight.Brake(velocity, dt);
        else
        {
            if (mine) KeepTarget(owner, age);
            NVector? aim = null;
            if (CrimsonRewardItems.TryNpc(Target, mine ? incarnation : 0, out NPC npc))
                aim = SableCrescentFlight.AimPoint(at, SableScytheFrame.N(npc.Center), SableScytheFrame.N(npc.velocity), velocity.Length());
            velocity = SableCrescentFlight.Steer(velocity, at, age, Kind, aim, dt);
        }
        Projectile.velocity = SableScytheFrame.X(velocity) / updates;
        Projectile.rotation = MathF.Atan2(Heading.Y, Heading.X);
        Projectile.direction = Projectile.spriteDirection = Projectile.velocity.X < 0 ? -1 : 1;
        if (mine && age == MathF.Floor(age) && (int)age % CrimsonRewardRules.CrescentSync == 0) Projectile.netUpdate = true;
    }

    // Owner: a target is kept until it is hit (OnHitNPC), dies, stops being chaseable, moves beyond 1,400 px or its slot
    // is reused (incarnation); without one the crescent acquires every 4 ticks. Moving the cursor never redirects it.
    private void KeepTarget(Player owner, float age)
    {
        if (Target >= 0 && !(CrimsonRewardItems.TryNpc(Target, incarnation, out NPC npc) && npc.CanBeChasedBy(Projectile)
                && !roots.Contains(CrimsonRewardItems.Root(npc)) && SableCrescentFlight.InKeepRange(SableScytheFrame.N(Projectile.Center),
                    new NVector(npc.Hitbox.Left, npc.Hitbox.Top), new NVector(npc.Hitbox.Right, npc.Hitbox.Bottom))))
            SetTarget(-1);
        if (Target >= 0 || age < nextAcquire) return;
        nextAcquire = age + CrimsonRewardRules.CrescentAcquireInterval;
        SetTarget(Find(owner, false));
    }

    // Owner: the acquisition (or, after a hit, the chain) over chaseable NPCs whose root this crescent has not hit, in
    // line of sight from the crescent (it passes through tiles but sees only what it can reach). Cheap range checks run
    // first; line of sight only for the best remaining candidate, until one passes.
    private int Find(Player owner, bool chain)
    {
        Span<NVector> mins = stackalloc NVector[Main.maxNPCs], maxs = stackalloc NVector[Main.maxNPCs];
        Span<int> slots = stackalloc int[Main.maxNPCs];
        Span<bool> skip = stackalloc bool[Main.maxNPCs];
        NVector at = SableScytheFrame.N(Projectile.Center);
        NVector cursor = SableCrescentFlight.ClampCursor(SableScytheFrame.N(owner.Center), SableScytheFrame.N(Main.MouseWorld));
        int count = 0;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy(Projectile) || roots.Contains(CrimsonRewardItems.Root(npc))) continue;
            NVector min = new(npc.Hitbox.Left, npc.Hitbox.Top), max = new(npc.Hitbox.Right, npc.Hitbox.Bottom);
            if (chain ? CrimsonRewardRules.BoxDistance(at, min, max) > CrimsonRewardRules.CrescentChainRange
                : !SableCrescentFlight.InAcquireRange(cursor, at, min, max)) continue;
            mins[count] = min; maxs[count] = max; slots[count] = npc.whoAmI; skip[count] = false; count++;
        }
        for (int tries = 0; tries < count; tries++)
        {
            int i = chain ? SableCrescentFlight.Chain(at, mins[..count], maxs[..count], skip[..count])
                : SableCrescentFlight.Acquire(cursor, at, Heading, mins[..count], maxs[..count], skip[..count]);
            if (i < 0) return -1;
            NPC npc = Main.npc[slots[i]];
            if (Collision.CanHitLine(Projectile.Center, 1, 1, npc.position, npc.width, npc.height)) return slots[i];
            skip[i] = true;
        }
        return -1;
    }

    private void SetTarget(int slot)
    {
        if (Target == slot) return;
        Projectile.ai[0] = slot;
        incarnation = slot >= 0 && slot < Main.maxNPCs ? CrimsonRewardItems.Incarnation(Main.npc[slot]) : 0;
        Projectile.netUpdate = true;
    }

    // The crescent's live window ends 6 ticks from here, through the material's close, while it slows into the wound.
    internal void Break()
    {
        if (Broken) return;
        Projectile.ai[1] = SableCrescentFlight.State(Kind, SableCrescentFlight.BreakAgeAt(Age));
        Projectile.netUpdate = true;
    }

    // Owner only, from a live stroke. A ninth crescent in flight breaks the oldest one still flying.
    internal static void Throw(Player owner, IEntitySource source, int kind, Vector2 apex, float heading, int damage, float knockback, int stroke)
    {
        if (owner.whoAmI != Main.myPlayer) return;
        int flying = 0;
        SableCrescent? oldest = null;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner.whoAmI && p.ModProjectile is SableCrescent c && !c.Broken)
            {
                flying++;
                if (oldest is null || c.Age > oldest.Age) oldest = c;
            }
        if (SableCrescentFlight.MustBreakOldest(flying)) oldest?.Break();

        NVector f = SableCrescentFlight.Unit(heading);
        Vector2 center = SableScytheFrame.X(SableCrescentFlight.CenterFromApex(SableScytheFrame.N(apex), f, kind));
        Vector2 velocity = SableScytheFrame.X(f * SableCrescentFlight.LaunchSpeed(kind)) / (1 + CrimsonRewardRules.CrescentExtraUpdates);
        int index = Projectile.NewProjectile(source, center, velocity, ModContent.ProjectileType<SableCrescent>(),
            CrimsonRewardItems.Hit(damage, SableCrescentFlight.Multiplier(kind)), knockback * CrimsonRewardRules.CrescentKnockback,
            owner.whoAmI, -1, SableCrescentFlight.State(kind, 0), 0);
        if (index < 0 || index >= Main.maxProjectiles || Main.projectile[index].ModProjectile is not SableCrescent crescent) return;
        crescent.stroke = stroke;
        crescent.heading = f;
        crescent.SetTarget(crescent.Find(owner, false));
        Main.projectile[index].netUpdate = true;
    }

    // The body as it collides now (the drawn arc, opened over 3 ticks since the throw).
    private void Body() => SableCrescentFlight.Body(SableScytheFrame.N(Projectile.Center), Heading, Kind,
        CrimsonRewardRules.InkOpen(Age), body, radius);

    public override bool? CanHitNPC(NPC target)
    {
        if (roots.Count >= CrimsonRewardRules.CrescentRoots || roots.Contains(CrimsonRewardItems.Root(target))) return false;
        NVector min = new(target.Hitbox.Left, target.Hitbox.Top), max = new(target.Hitbox.Right, target.Hitbox.Bottom);
        if (CrimsonRewardRules.BoxDistance(SableScytheFrame.N(Projectile.Center), min, max) > SableCrescentFlight.Broadphase(Kind)) return false;
        Body();
        return SableCrescentFlight.Touches(SableScytheFrame.N(Projectile.Center), Kind, body, radius, min, max) ? null : false;
    }

    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox) => true;

    // Owner: the first hit engraves this crescent's stroke (once per stroke); a hit on its target (or with none) chains to
    // the nearest unhit NPC within 320 px or breaks it there; the third root breaks it.
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        int root = CrimsonRewardItems.Root(target);
        roots.TryAdd(root);
        if (Projectile.owner != Main.myPlayer) return;
        Player owner = Main.player[Projectile.owner];
        if (!engraved)
        {
            engraved = true;
            if (owner.GetModPlayer<SableScythePlayer>().TryEngrave(stroke)) SableStaff.Engrave(owner, Projectile.GetSource_FromThis());
        }
        if (Broken) return;
        if (roots.Count >= CrimsonRewardRules.CrescentRoots) { Break(); return; }
        bool onTarget = Target < 0 || (Target < Main.maxNPCs && CrimsonRewardItems.Root(Main.npc[Target]) == root);
        if (!onTarget) return;
        int next = Find(owner, true);
        if (next >= 0) SetTarget(next);
        else Break();
    }
}

// The staff (the build): a harmless carrier while lines > 0, centred on its owner. ai = (lines at the last engraving,
// ticks since it, -). Lines last 360 ticks after the last engraving, then drain one per 30. It survives item swaps and
// Down (it only cannot be released then); death clears it. The owner sends netUpdate on each change of the line count.
public sealed class SableStaff : ModProjectile
{
    // Engrave runs inside a stroke's update, and Projectile.Update clears netUpdate before this carrier's own AI, so
    // the owner republishes the engraving from the carrier's next AI instead (BatonStroke and BloodinkTrail do the same).
    private bool resync;
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Scythe);
    internal int Engraved => Math.Clamp((int)Projectile.ai[0], 0, CrimsonRewardRules.StaffLines);
    internal float Since => Projectile.ai[1];
    internal int Current => SableScytheMotion.LinesAt(Engraved, Since);

    internal static Projectile? Find(Player player)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is SableStaff) return p;
        return null;
    }
    internal static int Lines(Player player) => Find(player)?.ModProjectile is SableStaff staff ? staff.Current : 0;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 200;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = SableScytheMotion.StaffLife(CrimsonRewardRules.StaffLines) + 30;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (!SableScytheFrame.ValidOwner(Projectile) || !SableScytheMotion.ValidStaff(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2]))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // Death clears the build; Down and item swaps do not.
        if (!SableScytheMotion.StaffSurvives(owner.active, owner.dead)) { Projectile.Kill(); return; }
        if (resync && Projectile.owner == Main.myPlayer) { resync = false; Projectile.netUpdate = true; }
        int before = Current;
        Projectile.ai[1]++;
        Projectile.Center = owner.MountedCenter;
        Projectile.velocity = Vector2.Zero;
        Projectile.timeLeft = 2 + SableScytheMotion.StaffLife(Engraved) - (int)Since;
        int now = Current;
        if (now <= 0) { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer && now != before) Projectile.netUpdate = true;
    }

    // The owner engraves (adds a line up to five; every connecting stroke restarts the 360-tick clock).
    internal static void Engrave(Player owner, IEntitySource source)
    {
        Projectile? found = Find(owner);
        if (found?.ModProjectile is SableStaff staff)
        {
            found.ai[0] = Math.Min(CrimsonRewardRules.StaffLines, staff.Current + 1);
            found.ai[1] = 0;
            found.timeLeft = 2 + SableScytheMotion.StaffLife(staff.Engraved);
            found.netUpdate = staff.resync = true;
            return;
        }
        int index = Projectile.NewProjectile(source, owner.MountedCenter, Vector2.Zero,
            ModContent.ProjectileType<SableStaff>(), 0, 0, owner.whoAmI, 1, 0, 0);
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].netUpdate = true;
    }
}

// Staff Reap's held pose (harmless). ai = (aim at C, lines spent, age); velocity is data: X = the interrupted stroke
// packed as index * 32 + age (-1 when cast from rest), Y = that stroke's aim. Windup 0-16, the whip forward at 16,
// then the follow-through held until 56 (full staff) or 8 ticks after the last line fires.
public sealed class SableRelease : ModProjectile
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.ScytheHeld);
    internal float Aim => Projectile.ai[0];
    internal int LinesSpent => Math.Clamp((int)Projectile.ai[1], 1, CrimsonRewardRules.StaffLines);
    internal int Age => (int)Projectile.ai[2];
    internal int Facing => SableScytheMotion.Facing(Aim);
    internal int Duration => SableScytheMotion.ReleaseTicks(LinesSpent);

    // A release on its last tick no longer blocks: the next Over starts the tick after and eases in from its pose.
    internal static bool Any(Player player)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is SableRelease release && release.Age < release.Duration) return true;
        return false;
    }

    // The pose at a (fractional) age, blended over the windup from wherever the interrupted stroke's blade was.
    internal SablePose PoseAt(float age)
    {
        int packed = (int)Projectile.velocity.X;
        if (packed < 0) return SableScytheMotion.ReleasePose(age, SableScytheMotion.ReleasePose(0));
        int stroke = Math.Clamp(packed / SableScytheMotion.FromStride, 0, SableScytheMotion.Strokes - 1), strokeAge = packed % SableScytheMotion.FromStride;
        float fromAim = Projectile.velocity.Y;
        SablePose from = SableScytheMotion.Reframe(SableScytheMotion.Pose(stroke, strokeAge), fromAim, SableScytheMotion.Facing(fromAim), Aim, Facing);
        return SableScytheMotion.ReleasePose(age, from);
    }

    // Owner only: spend the staff, lay it on the target and schedule every cut now (REWARDS.md, "Commitment").
    internal static bool Cast(Player owner, IEntitySource source, int damage, float knockback, SableStroke? interrupted)
    {
        if (owner.whoAmI != Main.myPlayer || !CrimsonRewardItems.CanAct(owner) || Any(owner)) return false;
        Projectile? staffCarrier = SableStaff.Find(owner);
        int lines = staffCarrier?.ModProjectile is SableStaff staff ? staff.Current : 0;
        if (lines <= 0) return false;

        Span<NVector> mins = stackalloc NVector[Main.maxNPCs], maxs = stackalloc NVector[Main.maxNPCs];
        int count = 0;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy()) continue;
            mins[count] = new NVector(npc.Hitbox.Left, npc.Hitbox.Top);
            maxs[count++] = new NVector(npc.Hitbox.Right, npc.Hitbox.Bottom);
        }
        Vector2 player = owner.Center, cursor = Main.MouseWorld;
        var placement = SableScytheMotion.Place(SableScytheFrame.N(player), SableScytheFrame.N(cursor), mins[..count], maxs[..count]);
        Vector2 toward = SableScytheFrame.X(placement.Center) - player;
        float aim = toward.LengthSquared() > 1 ? toward.ToRotation() : (owner.direction < 0 ? MathF.PI : 0);

        Vector2 from = interrupted is null ? new Vector2(-1, 0)
            : new Vector2(interrupted.Stroke * SableScytheMotion.FromStride + Math.Clamp(interrupted.Age, 0, SableScytheMotion.FromStride - 1), interrupted.Aim);
        int index = Projectile.NewProjectile(source, owner.MountedCenter, from, ModContent.ProjectileType<SableRelease>(), 0, 0,
            owner.whoAmI, aim, lines, 0);
        if (index < 0 || index >= Main.maxProjectiles) return false;
        Main.projectile[index].netUpdate = true;

        for (int k = 0; k < SableScytheMotion.Cuts(lines); k++)
        {
            SableCutPlan cut = SableScytheMotion.Cut(k, placement, player.X);
            bool barline = cut.Index == CrimsonRewardRules.StaffLines;
            int child = Projectile.NewProjectile(source, SableScytheFrame.X(cut.Start), Vector2.Zero, ModContent.ProjectileType<StaffCut>(),
                CrimsonRewardItems.Hit(damage, barline ? CrimsonRewardRules.BarlineMultiplier : CrimsonRewardRules.StaffLineMultiplier),
                knockback, owner.whoAmI, cut.Length, cut.Index + StaffCut.LinesStride * lines, -cut.Fire);
            if (child >= 0 && child < Main.maxProjectiles) Main.projectile[child].netUpdate = true;
        }
        staffCarrier!.Kill();
        owner.GetModPlayer<SableScythePlayer>().Released(SableScytheMotion.ReleaseTicks(lines));
        return true;
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = CrimsonRewardRules.BarlineHold + 4;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (!SableScytheFrame.ValidOwner(Projectile) || !SableScytheMotion.ValidRelease(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2],
                Projectile.velocity.X, Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // The pose is a held projectile: it stops with its owner. The cuts already cast keep their own schedule.
        if (!SableScytheMotion.HeldSurvives(owner.active && CrimsonRewardItems.CanAct(owner), SableScytheFrame.HoldingScythe(Projectile, owner)))
        { Projectile.Kill(); return; }
        if (owner.HeldItem.type != ModContent.ItemType<CrimsonSableScythe>()) return;
        Projectile.ai[2]++;
        if (Age > Duration) { Projectile.Kill(); return; }
        Vector2 shoulder = SableScytheFrame.Shoulder(owner, Facing);
        Projectile.Center = SableScytheFrame.World(shoulder, PoseAt(Age).Hand, Aim, Facing);
        Projectile.direction = Projectile.spriteDirection = Facing;
        if (Projectile.owner == Main.myPlayer && Age == Duration) owner.GetModPlayer<SableScythePlayer>().Ended(Aim, LinesSpent);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
}

// One Staff Reap part, cast with its release (at most six). Position = the starting end; ai = (signed length, index
// (0-4 a staff line, 5 the barline pair) + 8 x the lines spent, age (negative while it waits)). A line's head crosses
// 840 px in 4 ticks and each point stays live 10 ticks (x0.8 once per root); the barline pair falls in 3 ticks and
// stays live 12 (x2.0 once per root). A full staff's lines hold their scars until the barline has dried, so the whole
// staff dries together. Unstarted parts die with their owner's death or Down; started parts finish their window.
public sealed class StaffCut : ModProjectile
{
    internal const int LinesStride = SableScytheMotion.CutStride;
    private readonly CrimsonRootLedger roots = new();

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Scythe);
    internal int Index => (int)Projectile.ai[1] % LinesStride;
    internal int Lines => (int)Projectile.ai[1] / LinesStride;
    internal bool Barline => Index == CrimsonRewardRules.StaffLines;
    internal bool FullStaff => Lines >= CrimsonRewardRules.StaffLines;
    internal float T => Projectile.ai[2];
    internal SableCutPlan Plan => new(SableScytheFrame.N(Projectile.Center), Projectile.ai[0], Index, 0);
    internal int End => SableScytheMotion.CutEnd(Index, Lines);

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)(CrimsonRewardRules.StaffHalfLength * 2 + 200);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = CrimsonRewardRules.StaffCutLife + 2;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => (Barline ? SableScytheMotion.BarlineLive(T) : SableScytheMotion.LineLive(T)) ? null : false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (!SableScytheFrame.ValidOwner(Projectile) || !SableScytheMotion.ValidCut(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2]))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        // Commitment: the owner's death or Down kills the parts that have not started; live parts finish.
        if (!SableScytheMotion.CutSurvives(T, owner.active && CrimsonRewardItems.Usable(owner))) { Projectile.Kill(); return; }
        Projectile.ai[2]++;
        Projectile.velocity = Vector2.Zero;
        if (T > End) Projectile.Kill();
    }

    public override bool? CanHitNPC(NPC target)
    {
        if (T < 0 || roots.Contains(CrimsonRewardItems.Root(target))) return false;
        NVector min = new(target.Hitbox.Left, target.Hitbox.Top), max = new(target.Hitbox.Right, target.Hitbox.Bottom);
        return SableScytheMotion.CutTouches(Plan, T, min, max) ? null : false;
    }

    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox) => true;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => roots.TryAdd(CrimsonRewardItems.Root(target));
}

// Owner bookkeeping: the measure counter, a queued release, the release lock, what the next stroke eases in from, and
// the crescent ledger (stroke serials with their engraved bit, the 18-tick throw spacing). Nothing here is replicated;
// peers rebuild everything from the projectiles.
public sealed class SableScythePlayer : ModPlayer
{
    private readonly SableCombo combo = new();
    private readonly SableThrowLedger crescents = new();
    private bool queued;
    private ulong lastEnd = ulong.MaxValue, releaseUntil;
    private float lastAim;
    private int lastLines;

    internal bool ReleaseQueued => queued;
    internal void QueueRelease() => queued = true;
    internal void ClearQueue() => queued = false;
    internal int TakeStroke() { queued = false; crescents.BeginStroke(); return combo.Take(Main.GameUpdateCount); }
    internal int StrokeSerial => crescents.Serial;
    internal bool TryThrow(ulong now) => crescents.TryThrow(now);
    internal bool TryEngrave(int serial) => crescents.TryEngrave(serial);
    // After Staff Reap the measure restarts on the Over, which starts from the release's held pose. The scythe stays
    // locked until the follow-through ends (`ticks` from the cast), even if the held pose dies with an item swap.
    internal void Released(int ticks) { combo.Reset(); queued = false; releaseUntil = Main.GameUpdateCount + (ulong)Math.Max(0, ticks); }
    internal bool Releasing => Main.GameUpdateCount < releaseUntil;

    // A stroke or a release reached its last tick; the stroke spawned on the next tick eases in from it.
    internal void Ended(float aim, int lines) { lastEnd = Main.GameUpdateCount; lastAim = aim; lastLines = lines; }
    internal Vector2 FromFor(float aim)
    {
        ulong now = Main.GameUpdateCount;
        bool chained = lastEnd != ulong.MaxValue && now >= lastEnd && now - lastEnd <= 1;
        return chained ? new Vector2(lastLines, lastAim) : new Vector2(-1, aim);
    }

    // An item change resets the measure but never the release lock.
    public override void PostUpdate()
    {
        if (Player.HeldItem.type != ModContent.ItemType<CrimsonSableScythe>()) { combo.Reset(); queued = false; lastEnd = ulong.MaxValue; }
    }

    // Death clears the owner's bookkeeping (the staff carrier kills itself on every client). Crescents already thrown
    // finish their flight; with their strokes forgotten they engrave nothing.
    public override void UpdateDead() { combo.Reset(); crescents.Reset(); queued = false; lastEnd = ulong.MaxValue; releaseUntil = 0; }
    public override void OnEnterWorld() { combo.Reset(); crescents.Reset(); queued = false; lastEnd = ulong.MaxValue; releaseUntil = 0; }
}
