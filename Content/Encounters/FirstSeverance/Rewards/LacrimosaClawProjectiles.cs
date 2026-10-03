#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Lacrimosa's Claws gameplay (docs/encounters/first-severance/WEAPONS.md#lacrimosas-claws--refresh-2026-10).
// Ordinary owner-created projectiles under native replication, not a Raid protocol: the owner reads input, owns
// the bead meter and the combo, and processes its own hits; peers draw what the projectiles carry. Client art and
// sound enter through Client/Encounters/FirstSeverance/Weapons/LacrimosaClawVisuals.cs; Content never references it.

// The one held controller: both hands, spawned from HoldItem while the claws are held and usable, and kept for as
// long as they are. It runs the A/B/C kata (a stroke begins on the tick after the last one ends) and collides only
// in a live window. ai[0] = stroke (-1 none), ai[1] = the stroke's aim, ai[2] = ticks since it began (it keeps
// counting after the stroke, so peers see the hands return to rest). ExtraAI: duration, lit beads, stroke serial and
// the stroke's first impact. Item change (peers after 6 ticks), death, Down, crowd control or leaving the world end it.
public sealed class LacrimosaClawKata : ModProjectile
{
    internal const string IconTexture = "Convergence/Assets/Textures/Items/DollWeapons/NullRefrainIcon";
    private const int AgeCap = 1000;
    private readonly HashSet<int> struck = new();
    private int duration = LacrimosaClawMotion.BaseTicks(LacrimosaClawMotion.RakeDown);

    public override string Texture => IconTexture;

    internal int Stroke => float.IsFinite(Projectile.ai[0]) ? Math.Clamp((int)Projectile.ai[0], LacrimosaClawMotion.None, LacrimosaClawMotion.Clap) : LacrimosaClawMotion.None;
    internal float Aim => Projectile.ai[1];
    internal int Age => float.IsFinite(Projectile.ai[2]) ? Math.Clamp((int)Projectile.ai[2], 0, AgeCap) : 0;
    internal int Duration => duration;
    internal int Beads { get; private set; }
    internal ushort Serial { get; private set; }
    internal bool HasImpact { get; private set; }
    internal ushort ImpactSerial { get; private set; }
    internal Vector2 Impact { get; private set; }
    internal int Facing => LacrimosaClawMotion.Facing(Aim);
    internal bool Striking => Stroke != LacrimosaClawMotion.None && Age < duration;
    internal bool Finished => !Striking;
    internal Player Owner => Main.player[Projectile.owner];

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = CalamityTrueMelee.Damage;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.ownerHitCheck = true;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = 2;
        Projectile.ai[0] = LacrimosaClawMotion.None;
    }

    internal static bool OwnerUsable(Player owner)
        => owner.active && !owner.dead && RitualArmamentItems.Usable(owner) && !owner.CCed && !owner.noItems;

    // The owner's controller, if it exists.
    internal static LacrimosaClawKata? Find(Player player)
    {
        int index = player.GetModPlayer<LacrimosaClawPlayer>().KataIndex;
        if (index < 0 || index >= Main.maxProjectiles) return null;
        Projectile p = Main.projectile[index];
        return p.active && p.owner == player.whoAmI && p.ModProjectile is LacrimosaClawKata kata ? kata : null;
    }

    // Owner: start a stroke on this tick (its AI makes it age 1). Damage, crit, penetration and knockback are the
    // weapon's at this moment, as a freshly spawned stroke projectile would carry them.
    internal void Begin(int stroke, float aim, int ticks, int damage, float knockback, int crit, int armorPenetration)
    {
        stroke = LacrimosaClawMotion.Stroke(stroke);
        Projectile.ai[0] = stroke;
        Projectile.ai[1] = float.IsFinite(aim) ? aim : 0;
        Projectile.ai[2] = 0;
        duration = LacrimosaClawMotion.ValidDuration(stroke, ticks);
        Serial++;
        struck.Clear();
        Array.Clear(Projectile.localNPCImmunity);
        Projectile.damage = RitualArmamentRules.ScaledDamage(damage, LacrimosaClawMotion.Multiplier(stroke));
        Projectile.knockBack = knockback;
        Projectile.CritChance = crit;
        Projectile.ArmorPenetration = armorPenetration;
        Projectile.netUpdate = true;
    }

    // Owner: a grasp takes the hands; only this owner's stroke stops.
    internal void Cancel()
    {
        Projectile.ai[0] = LacrimosaClawMotion.None;
        Projectile.ai[2] = 0;
        Projectile.netUpdate = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override bool? CanDamage()
        => Striking && LacrimosaClawMotion.Live(Stroke, Age, duration) && !Owner.GetModPlayer<LacrimosaClawPlayer>().GraspBusy ? null : false;

    // One hit per logical NPC root per stroke: both clapping hands and a worm's segments share the ledger.
    public override bool? CanHitNPC(NPC target) => struck.Contains(Root(target)) ? false : null;

    internal static int Root(NPC npc) => npc.realLife >= 0 && npc.realLife < Main.maxNPCs ? npc.realLife : npc.whoAmI;

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) { Projectile.Kill(); return; }
        Player owner = Owner;
        if (!OwnerUsable(owner)) { Projectile.Kill(); return; }
        // Kept alive tick by tick, before the held-item check, so a peer's grace below really lasts 6 ticks.
        Projectile.timeLeft = 2;
        if (owner.HeldItem.type != ModContent.ItemType<NullRefrain>())
        {
            // Native held-item sync may trail the projectile on peers; the owner ends it at once.
            if (Projectile.owner == Main.myPlayer || ++Projectile.localAI[1] > 6) Projectile.Kill();
            return;
        }
        Projectile.localAI[1] = 0;
        if (!float.IsFinite(Projectile.ai[0]) || Projectile.ai[0] < LacrimosaClawMotion.None || Projectile.ai[0] > LacrimosaClawMotion.Clap
            || !float.IsFinite(Projectile.ai[1]) || !float.IsFinite(Projectile.ai[2]) || Projectile.ai[2] < 0)
        { Projectile.Kill(); return; }
        var state = owner.GetModPlayer<LacrimosaClawPlayer>();
        if (Projectile.owner == Main.myPlayer && state.KataIndex != Projectile.whoAmI && Find(owner) is not null)
        { Projectile.Kill(); return; }
        state.KataIndex = Projectile.whoAmI;

        Projectile.velocity = Vector2.Zero;
        Projectile.Center = owner.MountedCenter;
        if (Projectile.ai[2] < AgeCap) Projectile.ai[2]++;
        if (Projectile.owner == Main.myPlayer)
        {
            int beads = state.Heart.Lit;
            if (beads != Beads) { Beads = beads; Projectile.netUpdate = true; }
            // A periodic resync on the world clock (the stroke age stops at AgeCap), staggered per projectile.
            if ((Main.GameUpdateCount + (uint)Projectile.identity) % 120 == 0) Projectile.netUpdate = true;
        }
        if (!Striking || state.GraspBusy) return;
        // The hands are the weapon: the owner faces the stroke and both arms reach for their hands.
        owner.ChangeDir(Facing);
        owner.heldProj = Projectile.whoAmI;
        float baseAge = LacrimosaClawMotion.BaseAge(Stroke, Age, duration);
        Vector2 center = owner.MountedCenter;
        for (int hand = 0; hand < 2; hand++)
        {
            LacrimosaHand frame = LacrimosaClawMotion.Frame(Stroke, hand, baseAge);
            Vector2 wrist = ToWorld(frame.Wrist, owner.gravDir);
            float angle = (wrist - center).ToRotation() - MathHelper.PiOver2;
            if (hand == LacrimosaClawMotion.Right) owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, angle);
            else owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, angle);
        }
    }

    internal Vector2 ToWorld(NVector local, float gravDir)
    {
        NVector world = LacrimosaClawMotion.ToWorld(local, Aim, Facing, gravDir);
        return Owner.MountedCenter + new Vector2(world.X, world.Y);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        int stroke = Stroke;
        if (!Striking || !LacrimosaClawMotion.Live(stroke, Age, duration)) return false;
        float gravDir = Owner.gravDir;
        float first = LacrimosaClawMotion.FirstLiveAge(stroke, duration) - 1f;
        float start = (float)LacrimosaClawMotion.LiveStart(stroke) * duration / LacrimosaClawMotion.BaseTicks(stroke);
        NVector min = new(targetHitbox.Left, targetHitbox.Top), max = new(targetHitbox.Right, targetHitbox.Bottom);
        // Swept capsules: sub-samples across the last tick (from the live start on its first tick), so a fast
        // stroke cannot tunnel through a target. Each sample is a true capsule against the hitbox (round ends).
        for (int i = 0; i <= LacrimosaClawMotion.SubSamples; i++)
        {
            float age = MathF.Max(MathF.Max(start, first), Age - 1f + i / (float)LacrimosaClawMotion.SubSamples);
            float baseAge = LacrimosaClawMotion.BaseAge(stroke, age, duration);
            for (int hand = 0; hand < 2; hand++)
            {
                if (!LacrimosaClawMotion.TryCapsule(stroke, hand, baseAge, out NVector a, out NVector b, out float radius)) continue;
                Vector2 wa = ToWorld(a, gravDir), wb = ToWorld(b, gravDir);
                if (LacrimosaClawMotion.CapsuleHitsBox(new NVector(wa.X, wa.Y), new NVector(wb.X, wb.Y), radius, min, max)) return true;
            }
        }
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        struck.Add(Root(target));
        if (Projectile.owner != Main.myPlayer) return;
        if (!target.CountsAsACritter && !target.townNPC && !target.friendly || target.type == NPCID.TargetDummy)
            Owner.GetModPlayer<LacrimosaClawPlayer>().Heart.Land(((ulong)(uint)Projectile.identity << 16) | Serial, Stroke, duration, Main.GameUpdateCount);
        if (HasImpact && ImpactSerial == Serial) return;
        // The contact star sits where the striking capsule meets the target.
        Vector2 at = target.Center;
        float baseAge = LacrimosaClawMotion.BaseAge(Stroke, Age, duration);
        float best = float.MaxValue;
        for (int hand = 0; hand < 2; hand++)
        {
            if (!LacrimosaClawMotion.TryCapsule(Stroke, hand, baseAge, out NVector a, out NVector b, out _)) continue;
            Vector2 wa = ToWorld(a, Owner.gravDir), wb = ToWorld(b, Owner.gravDir);
            NVector near = LacrimosaClawMotion.Closest(new(wa.X, wa.Y), new(wb.X, wb.Y), new(target.Center.X, target.Center.Y));
            float distance = Vector2.DistanceSquared(new Vector2(near.X, near.Y), target.Center);
            if (distance >= best) continue;
            best = distance;
            at = new Vector2(Math.Clamp(near.X, target.Left.X, target.Right.X), Math.Clamp(near.Y, target.Top.Y, target.Bottom.Y));
        }
        HasImpact = true;
        ImpactSerial = Serial;
        Impact = at;
        Projectile.netUpdate = true;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((byte)duration);
        writer.Write((byte)Beads);
        writer.Write(Serial);
        writer.Write(HasImpact ? ImpactSerial : (ushort)0);
        writer.Write(HasImpact ? Impact.X : 0f);
        writer.Write(HasImpact ? Impact.Y : 0f);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        int ticks = reader.ReadByte(), beads = reader.ReadByte();
        ushort serial = reader.ReadUInt16(), impactSerial = reader.ReadUInt16();
        float x = reader.ReadSingle(), y = reader.ReadSingle();
        if (beads > LacrimosaHeart.Beads || !float.IsFinite(x) || !float.IsFinite(y) || Math.Abs(x) > 1_000_000 || Math.Abs(y) > 1_000_000)
        { Projectile.Kill(); return; }
        int stroke = Stroke;
        duration = stroke == LacrimosaClawMotion.None ? ticks : LacrimosaClawMotion.ValidDuration(stroke, ticks);
        Beads = beads;
        Serial = serial;
        if (impactSerial != 0 && (x != 0 || y != 0))
        {
            HasImpact = true;
            ImpactSerial = impactSerial;
            Impact = new Vector2(x, y);
        }
    }
}

// The grasp: both hands fly to a point, close on the NPC there (0.3x contact to that NPC only), hold and
// squeeze, and crush everything touching the 166 x 132 px ellipse (4.0x per logical root). Ordinary melee, not
// true melee. ai[0] = grasped NPC slot + 1 (0: empty grasp), ai[1] = its type, ai[2] = age (real ticks).
// ExtraAI: the anchor (the last centre it held) and the approach side. It follows a valid target but never moves,
// slows or stuns it; a dead, despawned, replaced or teleported target leaves the hands crushing its last centre.
public sealed class LacrimosaClawGrasp : ModProjectile
{
    private readonly HashSet<int> contacted = new(), crushed = new();
    private bool lost;

    public override string Texture => LacrimosaClawKata.IconTexture;

    internal int Age => float.IsFinite(Projectile.ai[2]) ? Math.Clamp((int)Projectile.ai[2], 0, LacrimosaClawMotion.GraspEnd + 1) : 0;
    internal bool HasTarget => Projectile.ai[0] >= 1;
    internal int TargetSlot => (int)Projectile.ai[0] - 1;
    internal Vector2 Anchor { get; private set; }
    internal int Side { get; private set; } = 1;
    internal bool Lost => lost;
    // The grasped body's half size along the tilt axis (the fists' seat), from the replicated NPC.
    internal float HalfExtent { get; private set; } = LacrimosaClawMotion.EmptyHalfExtent;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = LacrimosaClawMotion.GraspEnd + 4;
    }

    internal void Seat(Vector2 anchor, int side)
    {
        Anchor = anchor;
        Side = side < 0 ? -1 : 1;
        Projectile.netUpdate = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override bool? CanDamage()
        => LacrimosaClawMotion.ContactLive(Age) && HasTarget && !lost || LacrimosaClawMotion.CrushLive(Age) ? null : false;

    public override bool? CanHitNPC(NPC target)
    {
        int root = LacrimosaClawKata.Root(target);
        if (LacrimosaClawMotion.ContactLive(Age))
            return HasTarget && !lost && TargetSlot >= 0 && TargetSlot < Main.maxNPCs
                && root == LacrimosaClawKata.Root(Main.npc[TargetSlot]) && !contacted.Contains(root) ? null : false;
        return LacrimosaClawMotion.CrushLive(Age) && !crushed.Contains(root) ? null : false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!LacrimosaClawMotion.ContactLive(Age) && !LacrimosaClawMotion.CrushLive(Age)) return false;
        return LacrimosaClawMotion.CrushHits(new NVector(targetHitbox.Center.X - Projectile.Center.X, targetHitbox.Center.Y - Projectile.Center.Y),
            new NVector(targetHitbox.Width * .5f, targetHitbox.Height * .5f));
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (LacrimosaClawMotion.ContactLive(Age)) modifiers.Knockback *= 0;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        int root = LacrimosaClawKata.Root(target);
        if (LacrimosaClawMotion.ContactLive(Age)) contacted.Add(root);
        else crushed.Add(root);
    }

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!LacrimosaClawKata.OwnerUsable(owner)) { Projectile.Kill(); return; }
        if (owner.HeldItem.type != ModContent.ItemType<NullRefrain>())
        {
            if (Projectile.owner == Main.myPlayer || ++Projectile.localAI[1] > 6) Projectile.Kill();
            return;
        }
        Projectile.localAI[1] = 0;
        if (!float.IsFinite(Projectile.ai[0]) || !float.IsFinite(Projectile.ai[1]) || !float.IsFinite(Projectile.ai[2])
            || Projectile.ai[2] < 0 || Projectile.ai[0] < 0 || Projectile.ai[0] > Main.maxNPCs
            || !float.IsFinite(Projectile.Center.X) || !float.IsFinite(Projectile.Center.Y)
            || Projectile.Center.X < 16 || Projectile.Center.Y < 16
            || Projectile.Center.X > Main.maxTilesX * 16 - 16 || Projectile.Center.Y > Main.maxTilesY * 16 - 16)
        { Projectile.Kill(); return; }
        Projectile.ai[2]++;
        int age = Age;
        if (age > LacrimosaClawMotion.GraspEnd) { Projectile.Kill(); return; }
        Projectile.velocity = Vector2.Zero;
        if (age == 1)
        {
            if (Anchor == Vector2.Zero) Anchor = Projectile.Center;
            if (Projectile.owner != Main.myPlayer) Side = Projectile.Center.X >= owner.Center.X ? 1 : -1;
        }

        // Follow the grasped NPC from replicated NPC state on every side; never move it.
        if (HasTarget && !lost)
        {
            NPC npc = Main.npc[TargetSlot];
            if (npc.active && npc.life > 0 && npc.type == (int)Projectile.ai[1]
                && Vector2.DistanceSquared(npc.Center, Anchor) <= MathF.Pow(LacrimosaClawMotion.TeleportJump + npc.velocity.Length(), 2))
            {
                Anchor = npc.Center;
                NVector axis = LacrimosaClawMotion.GraspAxis(Side);
                HalfExtent = Math.Clamp(MathF.Abs(axis.X) * npc.width * .5f + MathF.Abs(axis.Y) * npc.height * .5f, 24, 140);
            }
            else lost = true;
        }
        Projectile.Center = Anchor;

        var state = owner.GetModPlayer<LacrimosaClawPlayer>();
        state.SeeGrasp(Projectile.whoAmI, age);
        if (Projectile.owner == Main.myPlayer)
        {
            int baseDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
            if (age < LacrimosaClawMotion.GraspCrush)
            {
                Projectile.damage = RitualArmamentRules.ScaledDamage(baseDamage, LacrimosaClawMotion.ContactMultiplier);
                Projectile.knockBack = 0;
            }
            else
            {
                Projectile.damage = RitualArmamentRules.ScaledDamage(baseDamage, LacrimosaClawMotion.CrushMultiplier);
                Projectile.knockBack = state.GraspKnockback * 1.4f;
            }
            if (age == LacrimosaClawMotion.GraspContact || age == LacrimosaClawMotion.GraspCrush) Projectile.netUpdate = true;
        }
        if (!LacrimosaClawMotion.GraspBusy(age)) return;
        // Both arms reach toward the grasp, as the v1 crush did.
        owner.heldProj = Projectile.whoAmI;
        owner.ChangeDir(Side);
        float direction = (Projectile.Center - owner.Center).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction - MathHelper.PiOver2 - .34f);
        owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, direction - MathHelper.PiOver2 + .34f);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Anchor.X);
        writer.Write(Anchor.Y);
        writer.Write((sbyte)Side);
        writer.Write(lost);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        float x = reader.ReadSingle(), y = reader.ReadSingle();
        int side = reader.ReadSByte();
        bool dropped = reader.ReadBoolean();
        if (!float.IsFinite(x) || !float.IsFinite(y) || Math.Abs(x) > 1_000_000 || Math.Abs(y) > 1_000_000)
        { Projectile.Kill(); return; }
        if (x != 0 || y != 0) Anchor = new Vector2(x, y);
        Side = side < 0 ? -1 : 1;
        lost |= dropped;
    }
}

// Owner bookkeeping for the claws: the bead meter (one per player, so extra copies cannot duplicate it), the kata
// combo and the right-click grasp. Every client also records where the owner's controller and grasp are.
public sealed class LacrimosaClawPlayer : ModPlayer
{
    internal readonly LacrimosaHeart Heart = new();
    internal readonly LacrimosaCombo Combo = new();
    internal int KataIndex = -1;
    // GameUpdateCount of the owner's last right click without six beads (the client flickers and ticks).
    internal ulong DryClick;
    internal float GraspKnockback;
    private int graspIndex = -1, graspAge = int.MaxValue;
    private ulong graspSeen;
    private bool rightHeld, requested;
    private Vector2 requestedPoint;

    internal int GraspIndex => GraspActive ? graspIndex : -1;
    internal bool GraspActive => graspIndex >= 0 && graspSeen + 1 >= Main.GameUpdateCount;
    // A grasp holds the hands (no strokes, no meter) until its release tick.
    internal bool GraspBusy => GraspActive && LacrimosaClawMotion.GraspBusy(graspAge);

    internal void SeeGrasp(int index, int age)
    {
        graspIndex = index;
        graspAge = age;
        graspSeen = Main.GameUpdateCount;
    }

    private bool Holding => Player.HeldItem.ModItem is NullRefrain;

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        bool pressed = Main.mouseRight && !rightHeld;
        rightHeld = Main.mouseRight;
        if (!pressed || Main.gameMenu || Main.gamePaused || Main.mapFullscreen || !Main.hasFocus || Player.mouseInterface
            || !Holding || !LacrimosaClawKata.OwnerUsable(Player)) return;
        // A fresh right click while left-clicking still grasps: it is taken here, not through item use.
        if (Heart.Ready && !GraspBusy) { requested = true; requestedPoint = Main.MouseWorld; }
        else if (!Heart.Ready) DryClick = Main.GameUpdateCount;
    }

    // Owner: spend the six beads, take the hands from this owner's stroke and send them to the point.
    internal bool TryGrasp(IEntitySource source, Vector2 point, int damage, float knockback)
    {
        if (Player.whoAmI != Main.myPlayer || !LacrimosaClawKata.OwnerUsable(Player) || !Holding || GraspBusy
            || !float.IsFinite(point.X) || !float.IsFinite(point.Y) || !Heart.Ready) return false;
        NVector bounded = LacrimosaClawMotion.ClampTarget(new NVector(Player.Center.X, Player.Center.Y), new NVector(point.X, point.Y));
        point = new Vector2(Math.Clamp(bounded.X, 32, Main.maxTilesX * 16 - 32), Math.Clamp(bounded.Y, 32, Main.maxTilesY * 16 - 32));
        if (!Heart.Spend()) return false;
        int target = Acquire(point);
        Vector2 at = target >= 0 ? Main.npc[target].Center : point;
        LacrimosaClawKata.Find(Player)?.Cancel();
        Combo.Restart();
        GraspKnockback = knockback;
        int index = Projectile.NewProjectile(source, at, Vector2.Zero, ModContent.ProjectileType<LacrimosaClawGrasp>(), damage, 0,
            Player.whoAmI, target + 1, target >= 0 ? Main.npc[target].type : 0, 0);
        if (index < 0 || index >= Main.maxProjectiles) return true;
        Projectile grasp = Main.projectile[index];
        grasp.originalDamage = damage;
        if (grasp.ModProjectile is LacrimosaClawGrasp claw) claw.Seat(at, at.X >= Player.Center.X ? 1 : -1);
        SeeGrasp(index, 0);
        return true;
    }

    // Owner presentation aid: the NPC a grasp at `point` would take (the same clamp and pick as TryGrasp), if any.
    internal static int? AimTarget(Vector2 point, Player player)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) return null;
        NVector bounded = LacrimosaClawMotion.ClampTarget(new NVector(player.Center.X, player.Center.Y), new NVector(point.X, point.Y));
        int target = Acquire(new Vector2(bounded.X, bounded.Y));
        return target >= 0 ? target : null;
    }

    // The nearest hostile NPC whose hitbox lies within AcquireRadius of the point (ties: the lower slot).
    private static int Acquire(Vector2 point)
    {
        int best = -1;
        float bestDistance = LacrimosaClawMotion.AcquireRadius * LacrimosaClawMotion.AcquireRadius;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!npc.active || npc.life <= 0 || npc.friendly || !(npc.CanBeChasedBy() || npc.type == NPCID.TargetDummy)) continue;
            Rectangle box = npc.Hitbox;
            Vector2 near = new(Math.Clamp(point.X, box.Left, box.Right), Math.Clamp(point.Y, box.Top, box.Bottom));
            float distance = Vector2.DistanceSquared(near, point);
            if (distance <= bestDistance && (best < 0 || distance < bestDistance)) { best = i; bestDistance = distance; }
        }
        return best;
    }

    public override void PostUpdate()
    {
        if (!Player.active || Player.dead) { Clear(); return; }
        if (requested && Player.whoAmI == Main.myPlayer)
        {
            requested = false;
            Item item = Player.HeldItem;
            if (item.ModItem is NullRefrain)
            {
                // The grasp is ordinary melee: its damage takes melee bonuses, not true-melee ones.
                DamageClass previous = item.DamageType;
                item.DamageType = DamageClass.Melee;
                try { TryGrasp(Player.GetSource_ItemUse(item), requestedPoint, Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack)); }
                finally { item.DamageType = previous; }
            }
        }
        if (KataIndex >= 0 && (KataIndex >= Main.maxProjectiles || !Main.projectile[KataIndex].active
            || Main.projectile[KataIndex].ModProjectile is not LacrimosaClawKata)) KataIndex = -1;
        Heart.Tick(Holding, LacrimosaClawKata.OwnerUsable(Player), GraspBusy);
    }

    private void Clear()
    {
        Heart.Reset();
        Combo.Reset();
        requested = false;
        graspIndex = -1;
        KataIndex = -1;
    }

    public override void OnEnterWorld() { Clear(); rightHeld = false; DryClick = 0; }
    public override void UpdateDead() => Clear();
}
