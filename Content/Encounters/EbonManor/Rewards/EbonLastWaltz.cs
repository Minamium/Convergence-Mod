#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// The Last Waltz: Noirette as a 10-slot companion (docs/encounters/ebon-manor/REWARDS.md#the-last-waltz).
// Follows the Doll companion contract: one per owner, owner-only dismissal on a missing buff, Down suspends
// attacks, children bound to the parent's identity, ordinary native projectile replication, no new packet.
public sealed class EbonLastWaltz : ModItem
{
    public override string Texture => EbonRewardItems.Icon(nameof(EbonLastWaltz), ItemID.Umbrella);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.StaffMinionSlotsRequired[Type] = EbonLastWaltzRules.Slots;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
    }

    public override void SetDefaults()
    {
        EbonRewardItems.Defaults(Item, EbonRewardKind.Summon);
        Item.width = Item.height = 40;
        Item.damage = EbonLastWaltzRules.ItemDamage;
        Item.mana = EbonLastWaltzRules.Mana;
        Item.knockBack = 3;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.noMelee = true;
        Item.buffType = ModContent.BuffType<EbonLastWaltzBuff>();
        Item.shoot = ModContent.ProjectileType<EbonLastWaltzCompanion>();
        Item.shootSpeed = 1;
    }

    public override bool CanUseItem(Player player) => EbonRewardItems.Usable(player)
        && EbonLastWaltzRules.CanSummon(player.maxMinions, player.ownedProjectileCounts[Item.shoot]);

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        player.AddBuff(Item.buffType, 2);
        int index = Projectile.NewProjectile(source, player.Center - new Vector2(player.direction * 54, 38),
            Vector2.Zero, Item.shoot, damage, knockback, player.whoAmI, 0, -1, 0);
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }

    // One of each Ebon weapon (consumed) at a Work Bench: no other source.
    public override void AddRecipes()
    {
        var recipe = CreateRecipe();
        foreach (int weapon in EbonRewardItems.RewardTypes()) recipe.AddIngredient(weapon);
        recipe.AddTile(TileID.WorkBenches).Register();
    }
}

public sealed class EbonLastWaltzBuff : ModBuff
{
    public override string Texture => EbonRewardItems.Icon(nameof(EbonLastWaltzBuff), ItemID.Umbrella);
    public override void SetStaticDefaults()
    { Main.buffNoSave[Type] = true; Main.buffNoTimeDisplay[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<EbonLastWaltzCompanion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

// Ordinary owner-replicated minion. Never a Raid participant, NPC proxy or Ready vote, and never held up by a thread.
// ai[0] = score tick (0 idle, 1..230 loop), ai[1] = target NPC (-1 none, owner-chosen), ai[2] unused.
public sealed class EbonLastWaltzCompanion : ModProjectile
{
    internal const float HandX = 18, HandY = -24;
    private bool observed;
    private int restart;
    public override string Texture => "Convergence/Assets/Textures/EbonManor/Noirette";
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 26; Projectile.height = 44;
        Projectile.minion = true; Projectile.minionSlots = EbonLastWaltzRules.Slots;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.ignoreWater = true;
        Projectile.tileCollide = false; Projectile.netImportant = true; Projectile.timeLeft = 18000;
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool OnTileCollide(Vector2 oldVelocity) => false;

    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || !EbonLastWaltzRules.ValidTick(Projectile.ai[0])) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        int buff = ModContent.BuffType<EbonLastWaltzBuff>();
        if (!observed)
        {
            observed = true;
            Mod.Logger.Info($"EbonLastWaltz event=ReplicaActive owner={Projectile.owner} identity={Projectile.identity} local={Main.myPlayer} net_mode={Main.netMode} owner_buff={owner.HasBuff(buff)}");
        }
        if (!owner.active || owner.dead) { owner.ClearBuff(buff); Projectile.Kill(); return; }
        bool authority = Projectile.owner == Main.myPlayer;
        if (EbonLastWaltzRules.DismissForMissingBuff(authority, owner.HasBuff(buff))) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;

        // Down, death, stun and item restrictions suspend the score; the figure keeps attending the owner.
        bool usable = EbonRewardItems.Usable(owner) && !owner.noItems && !owner.CCed;
        NPC? target = usable ? RitualTargeting.Acquire(Projectile, owner, manual: true) : null;
        int before = (int)Projectile.ai[0], tick = EbonLastWaltzRules.Advance(before, target is not null);
        Projectile.ai[0] = tick;
        if (authority && (before == 0 && tick == 1 || before > 0 && tick == 0 || tick > 0 && tick % 45 == 0)) Projectile.netUpdate = true;
        // A target that flickers in and out of sight restarts the score each time; the piece lifted on the
        // restart beat is skipped unless the score has been idle for RestartCooldown ticks.
        if (restart > 0) restart--;
        bool skipFirst = false;
        if (authority && before == 0 && tick == 1) { skipFirst = restart > 0; restart = EbonLastWaltzRules.RestartCooldown; }

        // Floats at the owner's shoulder: a soft spring, no ground state.
        Vector2 home = owner.Center + new Vector2(-owner.direction * 54, -38 * owner.gravDir);
        Vector2 desired = (home - Projectile.Center) * .11f;
        float limit = Math.Max(15, owner.velocity.Length() + 5);
        if (desired.LengthSquared() > limit * limit) desired = Vector2.Normalize(desired) * limit;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .19f);
        if (authority && Vector2.DistanceSquared(Projectile.Center, owner.Center) > 2200 * 2200)
        {
            Projectile.Center = home; Projectile.velocity = Vector2.Zero; Projectile.netUpdate = true;
        }
        float facing = target is not null ? target.Center.X - Projectile.Center.X
            : Math.Abs(Projectile.velocity.X) > .8f ? Projectile.velocity.X : owner.direction;
        if (Math.Abs(facing) > .3f) Projectile.spriteDirection = facing >= 0 ? 1 : -1;
        Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Math.Clamp(Projectile.velocity.X * .012f, -.16f, .16f), .15f);

        // Only the owner spawns children; peers consume the native projectile replication.
        if (!authority || target is null || !usable) return;
        if (EbonLastWaltzRules.FlingAt(tick, out int piece) && !skipFirst)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Hand(Projectile), Vector2.Zero,
                ModContent.ProjectileType<EbonWaltzFling>(), EbonRewardItems.Hit(Projectile.damage, EbonRewardRules.FlingMultiplier),
                Projectile.knockBack, Projectile.owner, piece, target.whoAmI, Projectile.identity);
        if (EbonLastWaltzRules.WaltzAt(tick))
            for (int spoke = 0; spoke < EbonRewardRules.Spokes; spoke++)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<EbonWaltzSpoke>(), EbonRewardItems.Hit(Projectile.damage, EbonRewardRules.SpokeMultiplier),
                    Projectile.knockBack, Projectile.owner, spoke, target.whoAmI, Projectile.identity);
    }

    // Where a flung piece is lifted from; the visual layer draws the real fingertip from the pose.
    internal static Vector2 Hand(Projectile parent) => parent.Center + new Vector2(parent.spriteDirection * HandX, HandY);

    internal static bool TryGetParent(Projectile child, out Projectile parent)
    {
        parent = null!;
        if (!RitualTargeting.ValidState(child) || child.ai[2] < 0 || child.ai[2] != (int)child.ai[2]) return false;
        Player owner = Main.player[child.owner];
        if (!EbonRewardItems.Usable(owner) || owner.noItems || owner.CCed
            || EbonLastWaltzRules.DismissForMissingBuff(child.owner == Main.myPlayer,
                owner.HasBuff(ModContent.BuffType<EbonLastWaltzBuff>()))) return false;
        foreach (Projectile candidate in Main.ActiveProjectiles)
            if (candidate.owner == child.owner && candidate.identity == (int)child.ai[2] && candidate.ModProjectile is EbonLastWaltzCompanion)
            { parent = candidate; return true; }
        return false;
    }

    // The NPC a child was aimed at when the owner spawned it, if it is still the same living target.
    internal static NPC? Aimed(Projectile child, bool mustBeHittable)
    {
        int index = (int)child.ai[1];
        if (index < 0 || index >= Main.maxNPCs) return null;
        NPC npc = Main.npc[index];
        return npc.active && (!mustBeHittable || npc.CanBeChasedBy(child)) ? npc : null;
    }
}

// One thrown piece (ai[0] = piece 0..5, ai[1] = target NPC, ai[2] = parent identity). Lifts on a silk thread
// beside Noirette for Lift ticks, is yanked at the target and crashes into the first NPC or tile (x FlingMultiplier).
// Local immunity -1 and penetrate 1: one piece can hit one NPC once, however many segments it overlaps.
public sealed class EbonWaltzFling : ModProjectile
{
    private Vector2 last;
    private int parentWait;
    internal int Piece => (int)Projectile.ai[0];
    // Local tick counter: replicas count from their own spawn, only the owner's contacts matter.
    internal int Age => (int)Projectile.localAI[0];
    internal bool Flying => Age > EbonLastWaltzRules.Lift;
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 34; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = 1; Projectile.timeLeft = EbonLastWaltzRules.Lift + EbonLastWaltzRules.MaxFlight;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanDamage() => Flying ? null : false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool OnTileCollide(Vector2 oldVelocity) => true;

    public override void AI()
    {
        if (Piece < 0 || Piece >= EbonLastWaltzRules.Flings || !EbonLastWaltzCompanion.TryGetParent(Projectile, out Projectile parent))
        {
            // A replica can precede its parent's packet: it stays harmless for a moment.
            if (Projectile.owner == Main.myPlayer || ++parentWait > 20 || Piece < 0 || Piece >= EbonLastWaltzRules.Flings) Projectile.Kill();
            return;
        }
        parentWait = 0;
        int age = Age;
        last = Projectile.Center;
        Projectile.damage = EbonRewardItems.Hit(parent.damage, EbonRewardRules.FlingMultiplier);
        if (age < EbonLastWaltzRules.Lift)
        {
            // Rises out of the dark and dangles from her raised fingertip, bobbing.
            Vector2 hold = EbonLastWaltzCompanion.Hand(parent) + new Vector2(parent.spriteDirection * 58, 40);
            float rise = EbonRewardRules.Smooth(age / (float)EbonLastWaltzRules.Lift);
            Projectile.Center = hold + new Vector2(0, (1 - rise) * 44 + MathF.Sin(age * .5f) * 2);
            Projectile.velocity = Vector2.Zero;
            last = Projectile.Center;
        }
        else
        {
            Projectile.tileCollide = true;
            NPC? target = EbonLastWaltzCompanion.Aimed(Projectile, mustBeHittable: true);
            float speed = EbonLastWaltzRules.FlingSpeed(age - EbonLastWaltzRules.Lift);
            if (age == EbonLastWaltzRules.Lift)
            {
                // The yank: a flat throw at the target, or a short toss if it has gone.
                Vector2 aim = target is null ? new Vector2(parent.spriteDirection, -.2f) : target.Center - Projectile.Center;
                Projectile.velocity = EbonRewardItems.Aim(aim, parent.spriteDirection) * speed;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }
            else if (target is not null) RitualTargeting.Home(Projectile, target.Center, target.velocity, speed);
            else Projectile.velocity.Y = Math.Min(24, Projectile.velocity.Y + .5f);
        }
        Projectile.localAI[0] = age + 1;
    }

    // Swept from where it was to where it is, so a 40 px/tick piece cannot tunnel through a thin NPC.
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!Flying) return false;
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), last, Projectile.Center, Projectile.width, ref point);
    }
}

// One of the six silk spokes (ai[0] = spoke 0..5, ai[1] = hub NPC, ai[2] = parent identity). The hub follows the
// target; length, angle and the beat-step turn are pure functions of the local age. Each spoke has its own native
// 8-tick local immunity plus a per-root ledger, so a segmented NPC counts once and a body across a spoke is
// struck once per beat step (EbonLastWaltzRules.SpokeRearm).
public sealed class EbonWaltzSpoke : ModProjectile
{
    private int[]? rearm;
    private int parentWait, hubType = -1;
    internal int Index => (int)Projectile.ai[0];
    internal int Age => (int)Projectile.localAI[0];
    internal float Phase => EbonLastWaltzRules.SpokePhase((int)Projectile.ai[2]);
    internal float Length(float age) => EbonLastWaltzRules.SpokeLength(Index, age);
    internal float Angle(float age) => EbonLastWaltzRules.SpokeAngle(Index, age, Phase);
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 400;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.netImportant = true; Projectile.timeLeft = EbonLastWaltzRules.SpokeLife + 12;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = EbonRewardRules.SpokeImmunity;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => Length(Age) >= EbonLastWaltzRules.SpokeLive ? null : false;
    public override bool? CanHitNPC(NPC target) => rearm is not null && Age < rearm[RitualTargeting.Root(target)] ? false : null;

    public override void AI()
    {
        if (Index < 0 || Index >= EbonRewardRules.Spokes || Age >= EbonLastWaltzRules.SpokeLife
            || !EbonLastWaltzCompanion.TryGetParent(Projectile, out Projectile parent))
        {
            if (Projectile.owner == Main.myPlayer || ++parentWait > 20 || Index < 0 || Index >= EbonRewardRules.Spokes
                || Age >= EbonLastWaltzRules.SpokeLife) Projectile.Kill();
            return;
        }
        parentWait = 0;
        rearm ??= new int[Main.maxNPCs];
        Projectile.damage = EbonRewardItems.Hit(parent.damage, EbonRewardRules.SpokeMultiplier);
        // The hub rides the target; if it dies or its slot is reused the waltz finishes where it stood.
        NPC? hub = EbonLastWaltzCompanion.Aimed(Projectile, mustBeHittable: false);
        if (hub is not null && hubType < 0) hubType = hub.type;
        if (hub is not null && hub.type == hubType) Projectile.Center = hub.Center;
        Projectile.localAI[0] = Age + 1;
    }

    // The capsule from the hub clearing to the tip, sampled at three angles per tick so a spoke
    // turning 40 px/tick at the beat cannot skip a thin NPC (Soboro's swept method).
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float length = Length(Age);
        if (length < EbonLastWaltzRules.SpokeLive) return false;
        float point = 0;
        for (int sample = 0; sample < 3; sample++)
        {
            Vector2 axis = Angle(Age - sample / 3f).ToRotationVector2();
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center + axis * EbonLastWaltzRules.SpokeInner, Projectile.Center + axis * length,
                EbonLastWaltzRules.SpokeWidth, ref point)) return true;
        }
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        rearm ??= new int[Main.maxNPCs];
        rearm[RitualTargeting.Root(target)] = EbonLastWaltzRules.SpokeRearm(Age);
    }
}
