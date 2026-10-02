#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Summon: Ember Censer (REWARDS.md, "Summon - Ember Censer"). Each use calls one small crown censer that hangs from its
// own floating ring behind the owner, glides to a station above a target, swings on its own pendulum clock and pours
// burning black blood at every apex from the second on; every fourth pour is the braced Grand Pour. The pendulum, the
// pours and the column are pure functions of the replicated ai (CenserRules). Presentation lives in Client
// (CenserVisuals, CenserInk); nothing here touches graphics or audio.
public sealed class CrimsonEmberCenser : ModItem
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Censer);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.StaffMinionSlotsRequired[Type] = CrimsonRewardRules.CenserSlots;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
    }

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Summon);
        Item.width = 24;
        Item.height = 32;
        Item.mana = CrimsonRewardRules.CenserMana;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.shootSpeed = 1;
        Item.buffType = ModContent.BuffType<CrimsonEmberCenserBuff>();
        Item.shoot = ModContent.ProjectileType<EmberCenserMinion>();
    }

    public override bool CanUseItem(Player player) => CrimsonRewardItems.Usable(player) && player.maxMinions >= CrimsonRewardRules.CenserSlots;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        player.AddBuff(Item.buffType, 2);
        // Spawn at the ring's place in the row behind the owner, never at an unchecked distant cursor coordinate.
        var offset = CrimsonRewardRules.CenserIdleOffset(player.ownedProjectileCounts[type], player.direction);
        int index = Projectile.NewProjectile(source, player.MountedCenter + new Vector2(offset.X, offset.Y), Vector2.Zero, type, damage, knockback,
            player.whoAmI, (float)CenserState.Idle, -1, 0);
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
}

public sealed class CrimsonEmberCenserBuff : ModBuff
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.CenserBuff);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    // Native minion upkeep: the buff lasts while a censer does.
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<EmberCenserMinion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

// ai[0] = CenserState (Idle, Seek, Swing), ai[1] = target NPC slot (-1 none), ai[2] = swing clock (0 outside Swing).
// The owner decides every transition, the target and each censer's phase, with a netUpdate on each; the summon-order
// stamp rides ExtraAI. Every client then runs the same clock (CenserRules.Advance) and derives the pose, the pours and
// the column from it; a peer keeps its own clock while it merely leads a stale packet. There is no child projectile:
// the censer collides only while a pour is live, with the column capsules, and its root ledger clears at each pour.
public sealed class EmberCenserMinion : ModProjectile
{
    private readonly record struct Entry(int Owner, long Key, int Target, CenserState State);

    // One pass over the projectile array per update serves every censer of every owner.
    private static readonly List<Entry> roll = new();
    private static ulong rollTick = ulong.MaxValue;
    private static readonly float[] open = CreateOpen();

    private readonly CrimsonRootLedger ledger = new();
    private readonly float[] depths = new float[CenserRules.MaxColumnPoints];
    private readonly NVector2[] points = new NVector2[CenserRules.MaxColumnPoints];
    private readonly float[] radii = new float[CenserRules.MaxColumnPoints];
    private int order, clock, written = -1, life, seeking, ledgerPour = -1, depthPour = -1, columnCount;
    private CenserState ran;
    private ulong targetIncarnation, columnTick = ulong.MaxValue;
    private Vector2 station, columnAt;
    private bool hasStation;

    internal CenserState State => (CenserState)Math.Clamp((int)Projectile.ai[0], 0, (int)CenserState.Swing);
    // The swing clock this client runs (equal to ai[2] after every update).
    internal int Clock => clock;
    internal int Target => (int)Projectile.ai[1];
    // Updates since this client first ran the censer.
    internal int Life => life;
    // Rank and count among the owner's censers by summon order (the place in the row behind the owner).
    internal int Ordinal { get; private set; }
    internal int Count { get; private set; } = 1;
    // The owner has stamped the summon order; a peer learns it from the first packet after the spawn.
    internal bool Stamped => order > 0;
    // Floor depth below each column point of this pour (refreshed every update while it is live; PourMaxDepth: none).
    internal ReadOnlySpan<float> Floors(in CenserPour pour) => depthPour == pour.Start ? depths : open;

    private static float[] CreateOpen()
    {
        var all = new float[CenserRules.MaxColumnPoints];
        Array.Fill(all, CrimsonRewardRules.PourMaxDepth);
        return all;
    }

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.CenserMinion);

    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.minion = true; Projectile.minionSlots = CrimsonRewardRules.CenserSlots;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 18000;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 10;
    }

    // ---- Damage: the live column only ----------------------------------------------------------------------
    // Pets skip native damage unless they opt in; the censer opts in and then collides only with its column, never by
    // contact, and only during a pour's live window.
    public override bool MinionContactDamage() => true;
    public override bool? CanDamage() => LivePour(out _);
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool OnTileCollide(Vector2 oldVelocity) => false;

    public override bool? CanHitNPC(NPC target)
        => !LivePour(out _) || ledger.Contains(CrimsonRewardItems.Root(target)) ? false : null;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!LivePour(out var pour)) return false;
        int n = Column(pour);
        return CenserRules.ColumnTouches(points.AsSpan(0, n), radii.AsSpan(0, n),
            new NVector2(targetHitbox.Left, targetHitbox.Top), new NVector2(targetHitbox.Right, targetHitbox.Bottom));
    }

    // x1.0 per pour, x2.2 for the Grand Pour, always from the live minion damage.
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (LivePour(out var pour)) modifiers.SourceDamage *= pour.Multiplier;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => ledger.TryAdd(CrimsonRewardItems.Root(target));

    internal bool LivePour(out CenserPour pour)
    {
        pour = default;
        return State == CenserState.Swing && CenserRules.TryPour(clock, out pour);
    }

    // The column at this update's clock and the censer's current ring (CenserRules.Column), cached for the update.
    private int Column(in CenserPour pour)
    {
        if (columnTick == Main.GameUpdateCount && columnAt == Projectile.Center) return columnCount;
        columnTick = Main.GameUpdateCount; columnAt = Projectile.Center;
        Span<float> times = stackalloc float[CenserRules.MaxColumnPoints];
        int n = CenserRules.Column(pour, clock, Floors(pour), points, radii, times, out _);
        var ring = new NVector2(Projectile.Center.X, Projectile.Center.Y);
        for (int i = 0; i < n; i++) points[i] += ring;
        return columnCount = n;
    }

    // ---- Update ----------------------------------------------------------------------------------------------
    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers
            || !CenserRules.ValidAi(Projectile.ai[0], Projectile.ai[1], Projectile.ai[2])
            || !float.IsFinite(Projectile.position.X) || !float.IsFinite(Projectile.position.Y)
            || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; }
        bool authority = Projectile.owner == Main.myPlayer;
        Reconcile(authority);
        if (State == CenserState.Swing) clock = CenserRules.Advance(clock);
        int buff = ModContent.BuffType<CrimsonEmberCenserBuff>();
        bool usable = CrimsonRewardItems.CanAct(owner);
        if (owner.dead)
        {
            // Death clears the build. A pour already live finishes its window; nothing else stays.
            owner.ClearBuff(buff);
            if (State != CenserState.Swing || !CenserRules.Continues(clock, false)) { Projectile.Kill(); return; }
            usable = false;
        }
        else if (authority && !owner.HasBuff(buff)) { Projectile.Kill(); return; } // remote buffs are no lifetime authority
        Projectile.timeLeft = 2;
        life++;
        Roster();
        if (authority && order == 0)
        {
            // First update on the owner: fix the place in the summon order once; peers learn it from the packet.
            order = CenserRules.SummonOrder(Main.GameUpdateCount);
            Projectile.netUpdate = true;
        }
        NPC? target = Acquire(owner, usable);
        bool entered = false;

        if (authority && Vector2.DistanceSquared(Projectile.Center, owner.Center) > 2400f * 2400f)
        {
            Projectile.Center = IdleHome(owner);
            Projectile.velocity = Vector2.Zero;
            entered = Enter(CenserState.Idle);
        }
        else switch (State)
        {
            case CenserState.Idle:
                Glide(IdleHome(owner), .10f, 30, .16f, owner.velocity * .85f);
                if (authority && target is not null) entered = Enter(CenserState.Seek);
                break;

            case CenserState.Seek:
                if (target is null)
                {
                    Glide(IdleHome(owner), .10f, 30, .16f, owner.velocity * .85f);
                    if (authority) entered = Enter(CenserState.Idle);
                    break;
                }
                Vector2 goal = Station(target);
                Glide(goal, .12f, 38, .18f, target.velocity);
                seeking++;
                float miss = Vector2.DistanceSquared(Projectile.Center, goal);
                if (authority && (miss < 24f * 24f || seeking > 90 && miss < 96f * 96f)) entered = StartSwing(target);
                break;

            case CenserState.Swing:
                // An unusable owner or a lost target: finish a live pour, never start one, then go back to Idle.
                if (!CenserRules.Continues(clock, usable && target is not null))
                {
                    Glide(IdleHome(owner), .10f, 30, .16f, owner.velocity * .85f);
                    entered = Enter(CenserState.Idle); // peers predict it too; the owner's packet confirms
                    break;
                }
                // A new target: the station glides to it while the swing goes on, never re-phased.
                Vector2 hold = target is not null ? Station(target) : hasStation ? station : Projectile.Center;
                Glide(hold, .2f, 30, .3f, target?.velocity ?? Vector2.Zero);
                if (LivePour(out var pour))
                {
                    if (authority && ledgerPour != pour.Start) { ledgerPour = pour.Start; ledger.Clear(); }
                    if (!Main.dedServ) Floor(pour); // the owner collides with it and every client draws it
                }
                break;
        }

        Projectile.ai[2] = State == CenserState.Swing ? clock : 0;
        written = (int)Projectile.ai[2];
        ran = State;
        // Peers extrapolate between corrections with the same deterministic motion and clock.
        if (authority && !entered && (Main.GameUpdateCount + (uint)Projectile.identity) % (State == CenserState.Idle ? 60u : 20u) == 0)
            Projectile.netUpdate = true;
    }

    // A peer runs its own clock: a transition or a packet it does not merely lead is adopted, a stale one is kept.
    private void Reconcile(bool authority)
    {
        if (authority) return;
        int received = (int)Projectile.ai[2];
        if (written < 0 || State != ran) clock = received;
        else if (received != written && !CenserRules.KeepLocal(clock, received)) clock = received;
    }

    private bool Enter(CenserState next)
    {
        if (State == next) return false;
        Projectile.ai[0] = (float)next;
        clock = 0; seeking = 0; ledgerPour = -1;
        if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        return true;
    }

    // The owner starts the clock so this censer's pours fall in the largest gap between the pours of the censers
    // already swinging over the same target (CenserRules.ChooseStart). Censers later in the array have not advanced
    // this update yet, so their clock for this tick is one ahead.
    private bool StartSwing(NPC target)
    {
        Span<int> others = stackalloc int[CenserRules.MaxPhaseOthers];
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (count >= others.Length) break;
            if (p.whoAmI == Projectile.whoAmI || p.owner != Projectile.owner || p.ModProjectile is not EmberCenserMinion other) continue;
            if (other.State != CenserState.Swing || other.Target != target.whoAmI) continue;
            others[count++] = p.whoAmI > Projectile.whoAmI ? CenserRules.Advance(other.clock) : other.clock;
        }
        Enter(CenserState.Swing);
        clock = CenserRules.ChooseStart(others[..count]);
        return true;
    }

    // ---- Floor -------------------------------------------------------------------------------------------------
    // Each column point falls to the first solid sample below where it left the mouth (every 8 px, at most 600 px,
    // platforms let it through); every client samples the same tiles. Only as far as the point can fall by the next
    // update is read.
    private void Floor(in CenserPour pour)
    {
        if (depthPour != pour.Start) { depthPour = pour.Start; Array.Fill(depths, CrimsonRewardRules.PourMaxDepth); }
        Span<float> emitted = stackalloc float[CenserRules.MaxColumnPoints];
        int n = CenserRules.Emissions(pour, clock, emitted);
        for (int k = 0; k < n; k++)
        {
            var mouth = CenserRules.Mouth(emitted[k]);
            var probe = new TileFloor(Projectile.Center + new Vector2(mouth.X, mouth.Y));
            depths[k] = CenserRules.FloorDepth(probe, CenserRules.ScanDepth(pour, emitted[k], clock));
        }
        columnTick = ulong.MaxValue;
    }

    private readonly struct TileFloor : ICenserFloor
    {
        private readonly float x, y;
        internal TileFloor(Vector2 top) { x = top.X; y = top.Y; }
        public bool Solid(int sample)
        {
            var at = new Vector2(x, y + sample * CrimsonRewardRules.PourSample);
            if (!WorldGen.InWorld((int)(at.X / 16f), (int)(at.Y / 16f), 1)) return true;
            return Collision.IsWorldPointSolid(at, treatPlatformsAsNonSolid: true);
        }
    }

    // ---- Targeting and motion ----------------------------------------------------------------------------------
    private bool Valid(NPC npc, Player owner, float range)
        => npc.CanBeChasedBy(Projectile) && Vector2.DistanceSquared(npc.Center, owner.Center) <= range * range;

    // The owner chooses: the minion target if one is set (within the keep range), otherwise the nearest chaseable NPC
    // within 1,200 px of the owner, kept out to 1,600 px; a reused slot is a different NPC. Peers read ai[1].
    private NPC? Acquire(Player owner, bool usable)
    {
        bool authority = Projectile.owner == Main.myPlayer;
        int id = (int)Projectile.ai[1];
        NPC? current = usable && CrimsonRewardItems.TryNpc(id, authority ? targetIncarnation : 0, out NPC held)
            && Valid(held, owner, CrimsonRewardRules.CenserKeep) ? held : null;
        if (!authority) return current;
        NPC? chosen = current;
        if (usable && owner.HasMinionAttackTargetNPC)
        {
            int manual = owner.MinionAttackTargetNPC;
            if (manual >= 0 && manual < Main.maxNPCs && Valid(Main.npc[manual], owner, CrimsonRewardRules.CenserKeep)) chosen = Main.npc[manual];
        }
        if (chosen is null && usable)
        {
            float best = CrimsonRewardRules.CenserSeek * CrimsonRewardRules.CenserSeek;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float distance = Vector2.DistanceSquared(npc.Center, owner.Center);
                if (distance < best && Valid(npc, owner, CrimsonRewardRules.CenserSeek)) { chosen = npc; best = distance; }
            }
        }
        int next = chosen?.whoAmI ?? -1;
        ulong incarnation = chosen is null ? 0 : CrimsonRewardItems.Incarnation(chosen);
        if (id != next || incarnation != targetIncarnation)
        {
            Projectile.ai[1] = next; targetIncarnation = incarnation;
            Projectile.netUpdate = true;
        }
        return chosen;
    }

    // Rank and count among the owner's censers. The projectile array is scanned once per update for all of them.
    private void Roster()
    {
        ulong now = Main.GameUpdateCount;
        if (rollTick != now)
        {
            rollTick = now;
            roll.Clear();
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.ModProjectile is EmberCenserMinion other)
                    roll.Add(new Entry(p.owner, CenserRules.RosterKey(other.order, p.identity), other.Target, other.State));
        }
        long own = CenserRules.RosterKey(order, Projectile.identity);
        int rank = 0, count = 0;
        foreach (var entry in roll)
            if (entry.Owner == Projectile.owner)
            {
                count++;
                if (entry.Key < own) rank++;
            }
        Ordinal = rank; Count = Math.Max(count, 1);
    }

    // Censers sharing a target spread their rings across it: rank r of n by summon order.
    private (int Rank, int Count) TargetRank(int target)
    {
        long own = CenserRules.RosterKey(order, Projectile.identity);
        int rank = 0, count = 1;
        foreach (var entry in roll)
        {
            if (entry.Owner != Projectile.owner || entry.Key == own || entry.Target != target
                || entry.State == CenserState.Idle) continue;
            count++;
            if (entry.Key < own) rank++;
        }
        return (rank, count);
    }

    private Vector2 IdleHome(Player owner)
    {
        var offset = CrimsonRewardRules.CenserIdleOffset(Ordinal, owner.direction);
        return owner.MountedCenter + new Vector2(offset.X, offset.Y);
    }

    // CenserStation above the target's top, lowered under a ceiling so the bowl swings in free air.
    private Vector2 Station(NPC target)
    {
        var (rank, count) = TargetRank(target.whoAmI);
        float x = target.Center.X + CrimsonRewardRules.CenserSpread(rank, count, target.width);
        float lift = Math.Clamp(Clearance(new Vector2(x, target.Top.Y), CrimsonRewardRules.CenserStation + 16) - 16,
            CrimsonRewardRules.BowlDrop + 36, CrimsonRewardRules.CenserStation);
        station = new Vector2(x, target.Top.Y - lift);
        hasStation = true;
        return station;
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

    private void Glide(Vector2 destination, float spring, float limit, float blend, Vector2 feed)
    {
        Vector2 desired = (destination - Projectile.Center) * spring;
        if (desired.LengthSquared() > limit * limit) desired = Vector2.Normalize(desired) * limit;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired + feed, blend);
    }

    // The summon-order stamp rides the ordinary projectile sync (netImportant + netUpdate).
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(order);

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        int stamp = reader.ReadInt32();
        // Written once by the owner (0 = not stamped yet); it never changes after that.
        if (order == 0 && stamp > 0 && stamp <= CenserRules.SummonOrderWrap) order = stamp;
    }
}
