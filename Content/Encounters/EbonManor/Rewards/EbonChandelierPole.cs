#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Summon — Ballroom Chandelier (docs/encounters/ebon-manor/REWARDS.md#summon--ballroom-chandelier).
// One-slot chandeliers hang behind the owner; with a target each glides above it, settles, drops on its
// own beat of the 4-beat cycle, shatters and is reeled back up and rewoven. Presentation lives in Client
// (EbonChandelierVisuals); nothing here touches graphics or audio.
public sealed class EbonChandelierPole : ModItem
{
    public override string Texture => EbonRewardItems.Icon(nameof(EbonChandelierPole), ItemID.StardustDragonStaff);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
    }

    public override void SetDefaults()
    {
        EbonRewardItems.Defaults(Item, EbonRewardKind.Summon);
        Item.width = Item.height = 32; // the 62x64 icon is a 2x drawing of a 31x32 pole
        Item.mana = 10;
        Item.shootSpeed = 1;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.UseSound = SoundID.Item44;
        Item.buffType = ModContent.BuffType<EbonChandelierBuff>();
        Item.shoot = ModContent.ProjectileType<EbonChandelierMinion>();
    }

    public override bool CanUseItem(Player player) => EbonRewardItems.Usable(player) && player.maxMinions >= 1;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        player.AddBuff(Item.buffType, 2);
        // Spawn at the owner, never at an unchecked distant cursor coordinate.
        int index = Projectile.NewProjectile(source, player.MountedCenter + new Vector2(0, -130), Vector2.Zero,
            Item.shoot, damage, knockback, player.whoAmI, (float)ChandelierState.Idle, -1, 0);
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
}

public sealed class EbonChandelierBuff : ModBuff
{
    // The ER07 buff cell (EbonChandelierBuff.png, 16x16 drawn at 2x); a vanilla minion buff icon stands in if it is missing.
    public override string Texture => ModContent.HasAsset(EbonRewardItems.TextureRoot + nameof(EbonChandelierBuff))
        ? EbonRewardItems.TextureRoot + nameof(EbonChandelierBuff)
        : "Terraria/Images/Buff_" + BuffID.StardustDragonMinion;

    public override void SetStaticDefaults()
    { Main.buffNoSave[Type] = true; Main.buffNoTimeDisplay[Type] = true; }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<EbonChandelierMinion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

// ai[0] = ChandelierState, ai[1] = target NPC (-1 none), ai[2] = ticks since the state began.
// The owner decides every transition except the two that are deterministic from local data and are also
// run by peers so a falling chandelier never overshoots its target while a packet is in flight:
// Fall -> Reweave (impact) and Reweave -> Seek/Idle (timer). Only the owner spawns the damage child.
// The owner also stamps each chandelier once with its summon order and size; both ride the extra AI.
public sealed class EbonChandelierMinion : ModProjectile
{
    // One pass over the projectile array per update serves every chandelier of every owner.
    private static readonly List<(int Owner, long Key)> roll = new();
    private static ulong rollTick = ulong.MaxValue;

    private int wait = -1, dropIndex = -1, serial, fallSerial = -1, order;
    private long dropAt = -1;
    private bool ignoreTiles, entered, small;
    private Vector2 impact;

    internal ChandelierState State => (ChandelierState)Math.Clamp((int)Projectile.ai[0], 0, (int)ChandelierState.Reweave);
    internal int Age => Math.Max(0, (int)Projectile.ai[2]);
    // Counts drops; peers use it to fire each cue once per drop.
    internal int Serial => serial;
    // Ticks until this chandelier's beat once scheduled (counts down on every client); -1 when not scheduled.
    internal int Wait => wait;
    // Lowest point of the last fall: the shatter centre.
    internal Vector2 Impact => impact;
    // Rank among the owner's chandeliers by summon order (the beat it owns, the slot in the row), and how many
    // there are. Only a chandelier leaving the row renumbers the ones summoned after it.
    internal int Ordinal { get; private set; }
    internal int Count { get; private set; } = 1;
    // The small body, chosen once when it is summoned (every second chandelier) so it never changes shape.
    internal bool Small => small;
    // The owner has stamped the summon order and size; a peer only learns them from the packet after the spawn.
    internal bool Stamped => order > 0;

    public override string Texture => EbonRewardItems.Icon(nameof(EbonChandelierMinion), ItemID.StardustDragonStaff);

    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 44; Projectile.height = (int)(EbonChandelierRules.BodyHalfHeight * 2);
        Projectile.minion = true; Projectile.minionSlots = 1;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 18000;
    }

    // Never hits by contact: the shatter is a short child with its own once-per-root ledger.
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool OnTileCollide(Vector2 oldVelocity) => false;

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers
            || !float.IsFinite(Projectile.position.X) || !float.IsFinite(Projectile.position.Y)
            || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        int buff = ModContent.BuffType<EbonChandelierBuff>();
        if (!owner.active || owner.dead) { owner.ClearBuff(buff); Projectile.Kill(); return; }
        bool authority = Projectile.owner == Main.myPlayer;
        // Player buffs can arrive after their projectile; only the owner reads a missing buff as dismissal.
        if (authority && !owner.HasBuff(buff)) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        Sanitize();
        entered = false;
        Projectile.ai[2]++;
        if (wait > 0) wait--;
        Roster();
        if (authority && order == 0)
        {
            // First update on the owner: fix the place in the summon order and the size once; peers learn both from the packet.
            order = EbonChandelierRules.SummonOrder(Main.GameUpdateCount);
            small = Count % 2 == 0; // Count includes this one, so the second, fourth... are small
            Projectile.netUpdate = true;
        }
        bool usable = EbonRewardItems.Usable(owner) && !owner.noItems && !owner.CCed;
        NPC? target = Acquire(owner, usable);
        ChandelierState state = State;
        int age = Age;

        if (authority && Vector2.DistanceSquared(Projectile.Center, owner.Center) > 2400f * 2400f)
        {
            Projectile.Center = IdleHome(owner);
            Projectile.velocity = Vector2.Zero;
            Enter(ChandelierState.Idle);
            return;
        }

        Vector2 hover = default;
        switch (state)
        {
            case ChandelierState.Idle:
                Glide(IdleHome(owner), .10f, 30, .16f, owner.velocity * .85f);
                if (authority && target is not null) Enter(ChandelierState.Seek);
                break;

            case ChandelierState.Seek:
                if (target is null)
                {
                    Glide(IdleHome(owner), .10f, 30, .16f, owner.velocity * .85f);
                    if (authority) Enter(ChandelierState.Idle);
                    break;
                }
                hover = Hover(target);
                // Feeding the target's velocity forward lets the glide keep up with a moving enemy.
                Glide(hover, .12f, 38, .18f, target.velocity);
                if (authority && Vector2.DistanceSquared(Projectile.Center, hover) < 64f * 64f) Enter(ChandelierState.Settle);
                break;

            case ChandelierState.Settle:
                if (target is null)
                {
                    Glide(IdleHome(owner), .10f, 30, .16f, owner.velocity * .85f);
                    if (authority) Enter(ChandelierState.Idle);
                    break;
                }
                hover = Hover(target);
                Glide(hover, .20f, 30, .30f, target.velocity);
                if (!authority) break;
                float miss = Vector2.DistanceSquared(Projectile.Center, hover);
                if (miss > 150f * 150f) { Enter(ChandelierState.Seek); break; }
                // Only a chandelier that is really over its target cuts its thread.
                if (age >= EbonRewardRules.SettleTicks) ScheduleOrDrop(miss < 110f * 110f);
                break;

            case ChandelierState.Fall:
                Fall(usable);
                break;

            case ChandelierState.Reweave:
                if (age > EbonRewardRules.ReweaveTicks)
                {
                    Projectile.velocity = Vector2.Zero;
                    Enter(target is not null ? ChandelierState.Seek : ChandelierState.Idle);
                    break;
                }
                Projectile.velocity = new Vector2(0, -EbonChandelierRules.ReelStep(age));
                break;
        }

        // Peers extrapolate between corrections with the same deterministic motion.
        if (authority && !entered && (State is ChandelierState.Idle or ChandelierState.Seek or ChandelierState.Settle)
            && (Main.GameUpdateCount + (uint)Projectile.identity) % (State == ChandelierState.Idle ? 60u : 20u) == 0)
            Projectile.netUpdate = true;
    }

    // --- State changes --------------------------------------------------------------------------
    private void Enter(ChandelierState next)
    {
        Projectile.ai[0] = (float)next;
        Projectile.ai[2] = 0;
        wait = -1; dropAt = -1; dropIndex = -1;
        entered = true;
        Projectile.netUpdate = true;
    }

    // The owner picks this chandelier's beat once it has settled; peers learn the countdown from the packet.
    private void ScheduleOrDrop(bool aligned)
    {
        long now = Main.GameUpdateCount;
        if (dropAt < 0 || dropIndex != Ordinal)
        {
            dropIndex = Ordinal;
            dropAt = EbonChandelierRules.DropAt(Ordinal, now);
            wait = (int)Math.Min(dropAt - now, 300);
            Projectile.netUpdate = true;
        }
        if (now < dropAt || !aligned) return;
        // The thread is snipped exactly on the beat.
        serial++;
        Enter(ChandelierState.Fall);
        Projectile.velocity = new Vector2(0, EbonChandelierRules.FallSpeed(0));
    }

    private void Fall(bool usable)
    {
        int age = Age;
        if (fallSerial != serial) { fallSerial = serial; ignoreTiles = SolidAt(Foot(Projectile.Center)); }
        float speed = EbonChandelierRules.FallSpeed(age);
        Projectile.velocity = new Vector2(0, speed);
        // Sub-step the path so a 30 px/tick fall cannot tunnel through a thin floor or a small enemy.
        int steps = Math.Max(1, (int)MathF.Ceiling(speed / 8f));
        Vector2 start = Projectile.Center;
        for (int i = 1; i <= steps; i++)
        {
            Vector2 at = start + new Vector2(0, speed * i / steps);
            if (ignoreTiles && !SolidAt(Foot(at))) ignoreTiles = false;
            if (!Contact(at)) continue;
            Projectile.Center = at;
            Land(usable);
            return;
        }
        if (age > 120 || Projectile.Center.Y + speed > (Main.maxTilesY - 20) * 16f) Land(usable);
    }

    // Shatter on the target's top or a tile. Peers run the same landing so the visual never overshoots;
    // the damage child exists only on the owner, and only while the owner is allowed to attack.
    private void Land(bool usable)
    {
        impact = new Vector2(Projectile.Center.X, Projectile.Center.Y + EbonChandelierRules.BodyHalfHeight);
        Projectile.velocity = Vector2.Zero;
        Enter(ChandelierState.Reweave);
        if (Projectile.owner != Main.myPlayer || !usable) return;
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), impact, Vector2.Zero,
            ModContent.ProjectileType<EbonChandelierShatter>(), EbonRewardItems.Hit(Projectile.damage, EbonRewardRules.ShatterMultiplier),
            Projectile.knockBack, Projectile.owner);
    }

    // --- Targeting and motion ---------------------------------------------------------------------
    private bool Valid(NPC npc, Player owner, float range)
        => npc.CanBeChasedBy(Projectile) && Vector2.DistanceSquared(npc.Center, owner.Center) <= range * range;

    // The owner chooses (manual command first, then the nearest enemy to itself) and keeps the target until
    // it is invalid; peers read the replicated ai[1] so every replica follows the same enemy.
    private NPC? Acquire(Player owner, bool usable)
    {
        int id = (int)Projectile.ai[1];
        NPC? current = usable && id >= 0 && id < Main.maxNPCs && Valid(Main.npc[id], owner, EbonChandelierRules.RetainRange)
            ? Main.npc[id] : null;
        if (Projectile.owner != Main.myPlayer) return current;
        NPC? chosen = current;
        if (usable && owner.HasMinionAttackTargetNPC)
        {
            int manual = owner.MinionAttackTargetNPC;
            if (manual >= 0 && manual < Main.maxNPCs && Valid(Main.npc[manual], owner, EbonChandelierRules.AcquireRange))
                chosen = Main.npc[manual];
        }
        if (chosen is null && usable)
        {
            float best = EbonChandelierRules.AcquireRange * EbonChandelierRules.AcquireRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float distance = Vector2.DistanceSquared(npc.Center, owner.Center);
                if (distance < best && Valid(npc, owner, EbonChandelierRules.AcquireRange)) { chosen = npc; best = distance; }
            }
        }
        int next = chosen?.whoAmI ?? -1;
        if (id != next) { Projectile.ai[1] = next; Projectile.netUpdate = true; }
        return chosen;
    }

    // Rank and count among this owner's chandeliers. The projectile array is scanned once per update for all of
    // them; each chandelier then ranks itself in the (short) list by summon order, ties broken by slot.
    private void Roster()
    {
        ulong now = Main.GameUpdateCount;
        if (rollTick != now)
        {
            rollTick = now;
            roll.Clear();
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.type == Type && p.ModProjectile is EbonChandelierMinion other)
                    roll.Add((p.owner, EbonChandelierRules.RosterKey(other.order, p.identity)));
        }
        long own = EbonChandelierRules.RosterKey(order, Projectile.identity);
        Ordinal = 0; Count = 0;
        foreach ((int owner, long key) in roll)
            if (owner == Projectile.owner)
            {
                Count++;
                if (key < own) Ordinal++;
            }
        Count = Math.Max(Count, 1);
    }

    private Vector2 IdleHome(Player owner)
    {
        var slot = EbonChandelierRules.IdleSlot(Ordinal, Count, owner.direction);
        return owner.MountedCenter + new Vector2(slot.X, slot.Y);
    }

    // EbonRewardRules.HoverHeight above the target, leading its velocity by one fall; lowered under a ceiling
    // so the body never hangs inside a tile. With several chandeliers each hangs at its own slot of a row
    // across the target (and odd slots a little higher), so the cascade is a sweep of separate bodies, not
    // one stack; the row stays within what a straight fall still lands on.
    private Vector2 Hover(NPC target)
    {
        int slot = EbonChandelierRules.HoverSlot(Ordinal, Count);
        float raise = EbonChandelierRules.HoverRaise(slot, Count);
        float room = Clearance(new Vector2(target.Center.X, target.Top.Y),
                EbonRewardRules.HoverHeight + EbonChandelierRules.HoverStagger + EbonChandelierRules.BodyRise + 40)
            + target.height * .5f - EbonChandelierRules.BodyRise;
        float lift = Math.Clamp(room - (Count > 1 ? EbonChandelierRules.HoverStagger : 0f), EbonChandelierRules.MinHover, EbonRewardRules.HoverHeight)
            + raise;
        int fall = EbonChandelierRules.FallTicks(lift - EbonChandelierRules.BodyHalfHeight - target.height * .5f);
        var lead = EbonChandelierRules.Lead(new System.Numerics.Vector2(target.velocity.X, target.velocity.Y), fall);
        float spread = EbonChandelierRules.HoverSpread(slot, Count, EbonChandelierRules.HoverReach(target.width));
        return target.Center + new Vector2(lead.X + spread, lead.Y) - new Vector2(0, lift);
    }

    // Free air above `from`, in 16 px steps up to `limit`. Platforms do not count as a ceiling.
    private static float Clearance(Vector2 from, float limit)
    {
        for (float d = 16; d <= limit; d += 16)
        {
            Vector2 at = from - new Vector2(0, d);
            if (!WorldGen.InWorld((int)(at.X / 16f), (int)(at.Y / 16f), 1)
                || Collision.IsWorldPointSolid(at, treatPlatformsAsNonSolid: true)) return d - 16;
        }
        return limit;
    }

    private void Glide(Vector2 destination, float spring, float limit, float blend, Vector2 feed = default)
    {
        Vector2 desired = (destination - Projectile.Center) * spring;
        if (desired.LengthSquared() > limit * limit) desired = Vector2.Normalize(desired) * limit;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired + feed, blend);
    }

    private static Rectangle Foot(Vector2 centre)
        => new((int)(centre.X - 20), (int)(centre.Y + EbonChandelierRules.BodyHalfHeight - 8), 40, 10);

    private static bool SolidAt(Rectangle area) => Collision.SolidCollision(new Vector2(area.X, area.Y), area.Width, area.Height);

    private bool Contact(Vector2 centre)
    {
        Rectangle foot = Foot(centre);
        if (!ignoreTiles && SolidAt(foot)) return true;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.CanBeChasedBy(Projectile) && foot.Intersects(npc.Hitbox)) return true;
        return false;
    }

    private void Sanitize()
    {
        if (!float.IsFinite(Projectile.ai[0]) || Projectile.ai[0] < 0 || Projectile.ai[0] > (int)ChandelierState.Reweave) Projectile.ai[0] = 0;
        if (!float.IsFinite(Projectile.ai[1]) || Projectile.ai[1] < -1 || Projectile.ai[1] >= Main.maxNPCs) Projectile.ai[1] = -1;
        if (!float.IsFinite(Projectile.ai[2]) || Projectile.ai[2] < 0 || Projectile.ai[2] > 100000) Projectile.ai[2] = 0;
    }

    // Drop count, countdown, shatter centre, summon order and size ride the ordinary projectile sync (netImportant + netUpdate).
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(serial);
        writer.Write((short)Math.Clamp(wait, -1, 300));
        writer.Write(impact.X); writer.Write(impact.Y);
        writer.Write(order); writer.Write(small);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        int count = reader.ReadInt32(), countdown = reader.ReadInt16();
        float x = reader.ReadSingle(), y = reader.ReadSingle();
        int stamp = reader.ReadInt32();
        bool tiny = reader.ReadBoolean();
        if (count < serial || countdown < -1 || countdown > 300
            || !float.IsFinite(x) || !float.IsFinite(y) || Math.Abs(x) > 1_000_000 || Math.Abs(y) > 1_000_000) return;
        serial = count; wait = countdown; impact = new Vector2(x, y);
        // The stamp is written once by the owner (0 = not stamped yet) and never changes after that.
        if (stamp > 0 && stamp <= EbonChandelierRules.SummonOrderWrap) { order = stamp; small = tiny; }
    }
}

// The 2-tick damage window of one shatter: x ShatterMultiplier to every NPC within ShatterRadius, once per
// root NPC (segmented bosses take one hit, as the other Convergence reward weapons ledger them). Spawned by
// the owner's chandelier at the moment of impact; the multiplier is applied to the live minion damage.
public sealed class EbonChandelierShatter : ModProjectile
{
    private readonly HashSet<int> hitRoots = new();

    public override string Texture => EbonRewardItems.Icon(nameof(EbonChandelierMinion), ItemID.StardustDragonStaff);

    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 3;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    // The shards are drawn by the chandelier's own pixel source.
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool? CanHitNPC(NPC target) => hitRoots.Contains(RitualTargeting.Root(target)) ? false : null;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => EbonChandelierRules.CircleTouchesBox(new(Projectile.Center.X, Projectile.Center.Y), EbonRewardRules.ShatterRadius,
            new(targetHitbox.Left, targetHitbox.Top), new(targetHitbox.Right, targetHitbox.Bottom));

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => hitRoots.Add(RitualTargeting.Root(target));
}
