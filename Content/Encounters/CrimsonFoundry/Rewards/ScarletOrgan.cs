#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Ranged: Canticle Organ (REWARDS.md, "Ranged - Canticle Organ"). Four bone organ pipes in a ribcage frame: every
// bullet becomes a bone shard (ammo damage and conservation stay native through PickAmmo); each shard hit marks the
// NPC's root for the owner; the right click (Hymn of Hands) spends the marks at once, one bone hand per mark slamming
// onto its NPC in sixteenths from the cast, and a fully marked NPC's eighth hand is the Clasp.
// Ownership: the owner client samples the aim, spends ammo, keeps the mark ledger (CanticleOrganPlayer, owner only:
// other players never see marks) and spawns every shard and hand. Everything peers draw travels by native projectile
// replication (CanticleShard, BoneHand: netImportant); no packet. Presentation lives in Client/.../Rewards/Organ*.cs
// and enters through the static Marked hook and the replicated projectiles; Content never references Client.
public sealed class CrimsonCanticleOrgan : ModItem
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Organ);

    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Ranged);
        Item.width = 44;
        Item.height = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        // Drawn in code (Client/.../OrganHeld.cs): it follows the aim spring, kicks per pipe and lifts for the hymn.
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.useAmmo = AmmoID.Bullet;
        Item.shoot = ProjectileID.Bullet;
        Item.shootSpeed = CrimsonRewardRules.ShardSpeed;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        if (!CrimsonRewardItems.Usable(player)) return false;
        bool hymn = player.altFunctionUse == 2;
        // No marks in range, or a hymn still pending: the right click does nothing at all.
        if (hymn && !CanticleHymn.Ready(player)) return false;
        // The hymn is a plain 24-tick use that neither shoots nor touches ammo (the Moonloom Harp's pattern).
        Item.useTime = Item.useAnimation = hymn ? CrimsonRewardRules.HymnUseTicks : CrimsonRewardRules.UseTicks(CrimsonRewardKind.Ranged);
        Item.useAmmo = hymn ? AmmoID.None : AmmoID.Bullet;
        Item.shoot = hymn ? ProjectileID.None : ProjectileID.Bullet;
        return true;
    }

    public override bool? UseItem(Player player)
    {
        if (player.altFunctionUse == 2 && player.whoAmI == Main.myPlayer && CrimsonRewardItems.CanAct(player))
            CanticleHymn.Cast(player, Item);
        return true;
    }

    // Whatever bullet was picked, a shard leaves the next pipe's mouth along the aim spring at a fixed speed.
    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        type = ModContent.ProjectileType<CanticleShard>();
        var state = player.GetModPlayer<CanticleOrganPlayer>();
        float aim = state.ShotAim(velocity);
        velocity = aim.ToRotationVector2() * CrimsonRewardRules.ShardSpeed;
        position = CanticleOrganPlayer.Mouth(player, aim, state.Pipe);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer || !CrimsonRewardItems.Usable(player)) return false;
        // At most MaxShardsInFlight per owner: a faster use time retires the oldest shard instead of adding a fourth.
        CanticleShard.RetireOldest(player.whoAmI, CrimsonRewardRules.MaxShardsInFlight - 1);
        int pipe = player.GetModPlayer<CanticleOrganPlayer>().TakePipe();
        // ai[0] = the firing pipe (peers kick that pipe and breathe vapour from its mouth). NewProjectile sends it.
        Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, pipe);
        return false;
    }
}

// The hymn's schedule (REWARDS.md, "Right click - Hymn of Hands"): owner only, at the cast.
internal static class CanticleHymn
{
    private static int HandType => ModContent.ProjectileType<BoneHand>();

    // A hymn from an earlier cast is pending while any of its hands has not landed.
    internal static bool Pending(int owner)
    {
        int type = HandType;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.type == type && CanticleRules.Pending((int)p.ai[2])) return true;
        return false;
    }

    internal static bool Ready(Player player)
    {
        if (player.whoAmI != Main.myPlayer || Pending(player.whoAmI)) return false;
        Span<CanticleHand> hands = stackalloc CanticleHand[CrimsonRewardRules.MaxHands];
        return Plan(player, hands) > 0;
    }

    private static int Plan(Player player, Span<CanticleHand> hands)
    {
        var state = player.GetModPlayer<CanticleOrganPlayer>();
        state.Prune();
        CanticleMarks marks = state.Marks;
        Span<bool> inRange = stackalloc bool[CrimsonRewardRules.MaxMarkedNpcs];
        float range = CrimsonRewardRules.HymnRange * CrimsonRewardRules.HymnRange;
        for (int i = 0; i < marks.Count; i++)
        {
            NPC npc = Main.npc[marks[i].Root];
            inRange[i] = npc.active && Vector2.DistanceSquared(player.Center, npc.Center) <= range;
        }
        return marks.Plan(inRange, hands);
    }

    // The marks are spent and every hand is spawned at once; each hand runs its own countdown to its slam, so an item
    // swap does not stop a cast hymn. Damage derives from the live weapon damage (no ammo).
    internal static void Cast(Player player, Item item)
    {
        if (Pending(player.whoAmI)) return;
        Span<CanticleHand> hands = stackalloc CanticleHand[CrimsonRewardRules.MaxHands];
        int count = Plan(player, hands);
        if (count == 0) return;
        int damage = player.GetWeaponDamage(item);
        float knockback = player.GetWeaponKnockback(item);
        IEntitySource source = player.GetSource_ItemUse(item);
        int type = HandType;
        for (int i = 0; i < count; i++)
        {
            CanticleHand hand = hands[i];
            NPC npc = Main.npc[hand.Root];
            int age = CanticleRules.SpawnAge(hand.Ordinal);
            bool clasp = hand.Role == CanticleRules.HandRole.Clasp;
            Vector2 at = BoneHand.Palm(npc, hand.Ordinal, hand.Role, age);
            int index = Projectile.NewProjectile(source, at, Vector2.Zero, type,
                CrimsonRewardItems.Hit(damage, clasp ? CrimsonRewardRules.ClaspMultiplier : CrimsonRewardRules.HandMultiplier), knockback,
                player.whoAmI, hand.Root, CanticleRules.HandCode(hand.Ordinal, hand.Role), age);
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is BoneHand bone)
                bone.Bind(CrimsonRewardItems.Incarnation(npc));
        }
        player.GetModPlayer<CanticleOrganPlayer>().Marks.Spend(hands[..count]);
    }
}

// A bone shard: speed 20 with one extra update for 40 ticks (1,600 px). It pierces nothing (x1.0 to the first NPC it
// hits, which marks that NPC's root for the owner) and shatters on tiles. ai[0] = its pipe (0-3). Drawn by
// Client/.../OrganVisuals.cs (sprite and wake); netImportant so the owner's kill reaches peers.
public sealed class CanticleShard : ModProjectile
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.OrganShard);

    internal int Pipe => Math.Clamp((int)Projectile.ai[0], 0, CanticleRules.Pipes - 1);

    public override void SetStaticDefaults()
    {
        // The last three updates' positions, for the wake (it lies inside the path the shard really flew).
        ProjectileID.Sets.TrailCacheLength[Type] = 3;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = CrimsonRewardRules.ShardExtraUpdates;
        Projectile.timeLeft = CanticleRules.ShardTimeLeft;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }

    public override bool PreDraw(ref Color lightColor) => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;

    public override void AI()
    {
        if (!CrimsonRewardItems.ValidAi(Projectile, 0, CanticleRules.Pipes - 1, 0, 0, 0, 0)
            || !CrimsonRewardRules.ValidInteger(Projectile.ai[0], 0, CanticleRules.Pipes - 1)
            || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        if (Projectile.velocity.LengthSquared() > .01f) Projectile.rotation = Projectile.velocity.ToRotation();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.owner == Main.myPlayer) Main.player[Projectile.owner].GetModPlayer<CanticleOrganPlayer>().Mark(target);
    }

    // Keep at most `keep` of the owner's shards, retiring the oldest (least time left) first.
    internal static void RetireOldest(int owner, int keep)
    {
        int type = ModContent.ProjectileType<CanticleShard>();
        while (true)
        {
            Projectile? oldest = null;
            int count = 0;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner != owner || p.type != type) continue;
                count++;
                if (oldest is null || p.timeLeft < oldest.timeLeft) oldest = p;
            }
            if (count <= keep || oldest is null) return;
            oldest.Kill();
        }
    }
}

// One bone hand of a hymn. ai[0] = its NPC slot (a mark root); ai[1] = CanticleRules.HandCode(ordinal, role);
// ai[2] = age relative to its slam (negative: the wait). It appears HandLead ticks before the slam 240 px above its
// NPC, falls onto the NPC's centre (offset by its arm, tracking the NPC's x) and is live for 3 ticks in a 48 px disc
// (x2.25 once per root it touches). The Clasp is one projectile drawn as four hands: x4.5 to its target's root in a
// 64 px disc plus a splash crown of six capsules (64-140 px, radius 18), x1.0 once per other root.
// The owner retires a hand whose NPC died or whose slot was reused (it keeps that NPC's incarnation), and every hand
// that has not landed when the owner dies or goes Down. Peers rebuild the motion from ai and their copy of the NPC;
// the owner sends the exact slam point once, on the slam. Drawn by Client/.../OrganVisuals.cs.
public sealed class BoneHand : ModProjectile
{
    // Owner only; null in the type's template (clones share references).
    private CrimsonRootLedger? struck;
    private ulong incarnation;

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.BoneHand);

    internal int Slot => (int)Projectile.ai[0];
    internal int Age => (int)Projectile.ai[2];
    internal int Ordinal => CanticleRules.TryHand(Projectile.ai[1], out int ordinal, out _) ? ordinal : 0;
    internal CanticleRules.HandRole Role => CanticleRules.TryHand(Projectile.ai[1], out _, out var role) ? role : CanticleRules.HandRole.Hand;
    internal bool Clasp => Role == CanticleRules.HandRole.Clasp;

    internal void Bind(ulong npcIncarnation) => incarnation = npcIncarnation;

    // The palm at `age`: above the landing point by the fall height, tracking the NPC's x.
    internal static Vector2 Palm(NPC npc, int ordinal, CanticleRules.HandRole role, float age)
        => npc.Center + new Vector2(CanticleRules.LandingOffset(ordinal, role, npc.width), -CanticleRules.FallHeight(age));

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        // Once per NPC for the whole hand; the root ledger below makes segmented NPCs count once.
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        // The age (replicated in ai[2]) ends the hand; this only backs it up.
        Projectile.timeLeft = CrimsonRewardRules.HandMaxLife + 4;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanDamage() => CanticleRules.IsLive(Age) ? null : false;

    private bool Sane()
        => CrimsonRewardItems.ValidAi(Projectile, 0, Main.maxNPCs - 1, 0, CanticleRules.MaxHandCode, CanticleRules.SpawnAge(CrimsonRewardRules.MaxHands - 1), CanticleRules.HandTail)
            && CrimsonRewardRules.ValidInteger(Projectile.ai[0], 0, Main.maxNPCs - 1)
            && CanticleRules.TryHand(Projectile.ai[1], out _, out _) && CanticleRules.ValidAge(Projectile.ai[2]);

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        if (!Sane()) { Projectile.Kill(); return; }
        int age = Age + 1;
        if (CanticleRules.Expired(age)) { Projectile.Kill(); return; }
        Projectile.ai[2] = age;
        Player owner = Main.player[Projectile.owner];
        bool mine = Projectile.owner == Main.myPlayer;
        if (!owner.active) { Projectile.Kill(); return; }
        // Commitment: the owner's death or Down retires the hands that have not landed; landed hands finish.
        if (CanticleRules.RetireOnOwnerLoss(age) && (owner.dead || mine && !CrimsonRewardItems.Usable(owner))) { Projectile.Kill(); return; }
        if (age > 0) return; // landed: the disc stays where it slammed
        // Until it lands the hand tracks its NPC; a dead NPC or a reused slot retires it (peers accept the slot's NPC).
        if (!CrimsonRewardItems.TryNpc(Slot, mine ? incarnation : 0, out NPC npc)) { Projectile.Kill(); return; }
        Projectile.Center = Palm(npc, Ordinal, Role, age);
        // The slam point, exactly, for every peer (the native update clears netUpdate before AI, so set it here).
        if (age == 0 && mine) Projectile.netUpdate = true;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => CanticleRules.SlamTouches(new NVector2(Projectile.Center.X, Projectile.Center.Y), Clasp, Age,
            new NVector2(targetHitbox.Left, targetHitbox.Top), new NVector2(targetHitbox.Right, targetHitbox.Bottom));

    public override bool? CanHitNPC(NPC target) => struck?.Contains(CrimsonRewardItems.Root(target)) == true ? false : null;

    // The projectile carries the Clasp's x4.5; other roots its splash touches take x1.0 of the same live damage.
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Clasp && CrimsonRewardItems.Root(target) != Slot)
            modifiers.SourceDamage *= CanticleRules.SlamMultiplier(CanticleRules.HandRole.Clasp, false) / CanticleRules.SlamMultiplier(CanticleRules.HandRole.Clasp, true);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => (struck ??= new CrimsonRootLedger()).TryAdd(CrimsonRewardItems.Root(target));
}

// Owner-side bookkeeping: the mark ledger, the aim spring and the next pipe. Marks are visual for the owner only and
// never replicated (REWARDS.md, "Ownership and replication"). Builds keep their own lifetimes across item swaps and
// Down; death clears them; entering a world starts clean.
public sealed class CanticleOrganPlayer : ModPlayer
{
    // Client presentation hook (the toll and the note-head flare); Content never references Client.
    internal static Action<Player, NPC, CanticleMarkResult>? Marked;

    private readonly CanticleMarks marks = new();
    private float aim, previousAim, aimVelocity;
    private bool aimed;
    private int pipe;

    internal CanticleMarks Marks => marks;
    internal int Pipe => pipe;
    internal bool Aimed => aimed;
    internal float Aim => aim;
    internal float PreviousAim => previousAim;

    internal int TakePipe()
    {
        int fired = pipe;
        pipe = CanticleRules.NextPipe(pipe);
        return fired;
    }

    // The spring's aim, or the shot's own direction until the spring has run (the first tick after equipping).
    internal float ShotAim(Vector2 velocity) => aimed ? aim : CrimsonRewardItems.Aim(velocity, Player.direction).ToRotation();

    // The firing pipe's mouth: the front hand at that aim plus the mouth offset, never through a wall.
    internal static Vector2 Mouth(Player player, float aim, int pipe)
    {
        Vector2 hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
        NVector2 offset = CanticleRules.MouthOffset(pipe, aim);
        Vector2 mouth = hand + new Vector2(offset.X, offset.Y);
        return Collision.CanHitLine(player.MountedCenter, 1, 1, mouth, 1, 1) ? mouth : player.MountedCenter;
    }

    internal void Mark(NPC target)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        NPC root = Main.npc[CrimsonRewardItems.Root(target)];
        if (!root.active) return;
        CanticleMarkResult result = marks.Mark(root.whoAmI, root.type, CrimsonRewardItems.Incarnation(root), Main.GameUpdateCount);
        if (!Main.dedServ) Marked?.Invoke(Player, root, result);
    }

    // Expired marks, dead NPCs and reused slots leave the ledger.
    internal void Prune()
    {
        marks.Expire(Main.GameUpdateCount);
        for (int i = marks.Count - 1; i >= 0; i--)
        {
            CanticleMark mark = marks[i];
            NPC npc = Main.npc[mark.Root];
            if (!npc.active || npc.life <= 0 || npc.type != mark.Type || CrimsonRewardItems.Incarnation(npc) != mark.Incarnation) marks.RemoveAt(i);
        }
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        Prune();
        previousAim = aim;
        if (Player.HeldItem.type != ModContent.ItemType<CrimsonCanticleOrgan>() || !Player.active || Player.dead) { aimed = false; return; }
        float target = (Main.MouseWorld - Player.MountedCenter).ToRotation();
        if (!aimed) { previousAim = aim = target; aimVelocity = 0; aimed = true; return; }
        (aim, aimVelocity) = CanticleRules.SpringAim(aim, aimVelocity, target);
    }

    public override void UpdateDead() => marks.Clear();

    public override void OnEnterWorld()
    {
        marks.Clear();
        aimed = false;
        pipe = 0;
    }
}
