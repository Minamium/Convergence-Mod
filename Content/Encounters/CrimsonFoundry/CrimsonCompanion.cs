#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

public sealed class CrimsonPact : ModItem
{
    public override string Texture => "Convergence/Assets/Textures/CrimsonFoundry/CrimsonPact";
    public override void SetDefaults()
    {
        Item.width = Item.height = 30; Item.damage = 900; Item.DamageType = DamageClass.Summon;
        Item.mana = 20; Item.knockBack = 2; Item.noMelee = true; Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.HoldUp; Item.useTime = Item.useAnimation = 30;
        Item.rare = ItemRarityID.Red; Item.UseSound = SoundID.Item44;
        Item.shoot = ModContent.ProjectileType<CrimsonCompanion>(); Item.buffType = ModContent.BuffType<CrimsonPactBuff>();
    }
    public override bool CanUseItem(Player player) => player.maxMinions >= 10 && player.ownedProjectileCounts[Item.shoot] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 18000);
        int id = Projectile.NewProjectile(source, player.Center + new Vector2(-player.direction * 60, -20), Vector2.Zero,
            type, damage, knockback, player.whoAmI);
        if (id < Main.maxProjectiles) Main.projectile[id].originalDamage = Item.damage;
        return false;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient<CrimsonConductor>()
        .AddIngredient(ItemID.Silk, 10).AddIngredient(ItemID.SoulofNight, 5).AddTile(TileID.Bookcases).Register();
}

public sealed class CrimsonPactBuff : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Pygmies;
    public override void SetStaticDefaults() { Main.buffNoTimeDisplay[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<CrimsonCompanion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

// Ordinary owner-replicated summon. Not a Ready vote or a substitute Raid member.
public sealed class CrimsonCompanion : ModProjectile
{
    private int grounded, blocked;
    private readonly int[] targetSlots = new int[CrimsonCovenantRules.MaximumTargets];
    internal const int Cycle = 150, Fire = 70;
    public override string Texture => "Convergence/Assets/Textures/CrimsonFoundry/ScarletConjurer";
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true; // HP-priority covenant, not the manual single-target Doll behavior.
        ProjectileID.Sets.MinionSacrificable[Type] = true; ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 26; Projectile.height = 52; Projectile.minion = true; Projectile.minionSlots = 10;
        Projectile.DamageType = DamageClass.Summon; Projectile.friendly = true; Projectile.penetrate = -1;
        Projectile.ignoreWater = true; Projectile.tileCollide = false; Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool OnTileCollide(Vector2 oldVelocity) => false;
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    { fallThrough = Main.player[Projectile.owner].Bottom.Y > Projectile.Bottom.Y + 48; return true; }
    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !float.IsFinite(Projectile.ai[0])
            || Projectile.ai[0] < 0 || Projectile.ai[0] >= Cycle || Projectile.ai[2] is not (0 or 1)) { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner]; bool authority = Projectile.owner == Main.myPlayer;
        if (!owner.active || owner.dead || authority && !owner.HasBuff(ModContent.BuffType<CrimsonPactBuff>()))
        { Projectile.Kill(); return; } // Remote player buffs are not a lifetime authority.
        Projectile.timeLeft = 2;
        int count = authority && !owner.noItems && !owner.CCed ? Targets() : 0;
        NPC? target = count > 0 ? Main.npc[targetSlots[0]] : null;
        if (authority)
        {
            float before = Projectile.ai[0]; Projectile.ai[0] = target is null ? 0 : (before + 1) % Cycle;
            if (before > 0 && Projectile.ai[0] == 0 || Projectile.ai[0] % 30 == 1) Projectile.netUpdate = true;
        }
        else if (Projectile.ai[0] > 0) Projectile.ai[0] = (Projectile.ai[0] + 1) % Cycle;
        Vector2 home = owner.Bottom - new Vector2(owner.direction * 72, 0);
        bool support = false;
        for (int i = 0; i < 3; i++)
        {
            Vector2 foot = owner.Bottom + new Vector2((i - 1) * (owner.width * .5f - 2), 2);
            support |= WorldGen.InWorld((int)(foot.X / 16), (int)(foot.Y / 16), 1)
                && Collision.IsWorldPointSolid(foot, treatPlatformsAsNonSolid: false);
        }
        bool airborne = owner.mount.Active || !support || Math.Abs(owner.velocity.Y) > .1f || owner.gravDir < 0;
        if (authority)
        {
            grounded = airborne ? 0 : Math.Min(grounded + 1, 30);
            bool catchup = Vector2.DistanceSquared(Projectile.Bottom, home) > 360 * 360 || blocked > 30;
            bool canLand = grounded >= 18 && Math.Abs(Projectile.Bottom.Y - home.Y) < 20
                && Math.Abs(Projectile.Bottom.X - home.X) < 85 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
            bool fly = airborne || catchup || Projectile.ai[2] == 1 && !canLand;
            if (Projectile.ai[2] != (fly ? 1 : 0)) { Projectile.ai[2] = fly ? 1 : 0; Projectile.netUpdate = true; }
        }
        bool floating = Projectile.ai[2] == 1; Projectile.tileCollide = !floating;
        if (floating)
        {
            Vector2 destination = airborne ? owner.Center + new Vector2(-owner.direction * 82, -44 * owner.gravDir)
                : home - new Vector2(0, Projectile.height * .5f + 2);
            Vector2 desired = (destination - Projectile.Center) * .12f;
            float speed = Math.Max(16, owner.velocity.Length() + 6);
            if (desired.Length() > speed) desired = desired.SafeNormalize(Vector2.UnitY) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .18f); blocked = 0;
        }
        else
        {
            float delta = home.X - Projectile.Bottom.X;
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, Math.Abs(delta) < 12 ? 0 : Math.Clamp(delta * .11f, -8, 8), .2f);
            bool wall = Collision.SolidCollision(Projectile.position + new Vector2(Math.Sign(delta) * 12, 0), Projectile.width, Projectile.height - 6);
            if (Math.Abs(Projectile.velocity.Y) < .1f && (wall || owner.Bottom.Y < Projectile.Bottom.Y - 25)) Projectile.velocity.Y = -8;
            Projectile.velocity.Y = Math.Min(11, Projectile.velocity.Y + .42f); blocked = wall ? blocked + 1 : 0;
        }
        if (authority && Vector2.DistanceSquared(Projectile.Center, owner.Center) > 2200 * 2200)
        { Projectile.Center = owner.Center; Projectile.velocity = Vector2.Zero; Projectile.ai[2] = 1; Projectile.netUpdate = true; }
        float face = target is null ? Projectile.velocity.X : target.Center.X - Projectile.Center.X;
        if (Math.Abs(face) > .3f) Projectile.spriteDirection = Math.Sign(face);
        Projectile.rotation = MathHelper.Lerp(Projectile.rotation, floating ? Projectile.velocity.X * .009f : 0, .16f);
        if (authority && (int)Projectile.ai[0] == Fire - CrimsonCovenantRules.ChargeTicks)
            for (int i = 0; i < count; i++)
            {
                NPC enemy = Main.npc[targetSlots[i]];
                Projectile.NewProjectile(new CrimsonCovenantSource(enemy), enemy.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrimsonCompanionRay>(), Projectile.damage, Projectile.knockBack,
                    Projectile.owner, 0, Math.Clamp(enemy.width * .5f + 110, 140, 600), Projectile.identity);
            }
    }
    private int Targets()
    {
        Span<CrimsonCovenantTarget> candidates = stackalloc CrimsonCovenantTarget[Main.maxNPCs];
        int count = 0;
        foreach (NPC n in Main.ActiveNPCs)
        {
            float d = Vector2.DistanceSquared(n.Center, Projectile.Center);
            if (n.CanBeChasedBy(Projectile) && d < 1600 * 1600
                && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, n.position, n.width, n.height))
                candidates[count++] = new(n.whoAmI, n.life);
        }
        return CrimsonCovenantRules.Select(candidates[..count], targetSlots);
    }
    internal static Vector2 Orb(Projectile p) => p.Center + new Vector2(p.spriteDirection * 28, -13).RotatedBy(p.rotation);
    internal static bool Parent(Projectile child, out Projectile parent)
    {
        parent = null!;
        if (child.owner < 0 || child.owner >= Main.maxPlayers || child.ai[2] < 0 || child.ai[2] != (int)child.ai[2]) return false;
        var owner = Main.player[child.owner];
        if (!owner.active || owner.dead || owner.noItems || owner.CCed
            || child.owner == Main.myPlayer && !owner.HasBuff(ModContent.BuffType<CrimsonPactBuff>())) return false;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == child.owner && p.identity == (int)child.ai[2] && p.ModProjectile is CrimsonCompanion)
            { parent = p; return true; }
        return false;
    }
}

internal sealed record CrimsonCovenantSource(NPC Target) : IEntitySource
{ public string Context => "ScarletCovenantClamp"; }

// A local incarnation guard is sufficient: only the projectile owner follows
// a target. Other peers receive the frozen native projectile center, never retarget.
internal sealed class CrimsonCovenantIncarnation : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private static ulong sequence;
    internal ulong Value;
    public override void OnSpawn(NPC npc, IEntitySource source) => Value = ++sequence;
}

public sealed class CrimsonCompanionRay : ModProjectile
{
    private NPC? target;
    private ulong incarnation;
    private int parentWait;
    internal float Opening => CrimsonCovenantRules.Opening(Projectile.ai[0]);
    internal float Reach => Projectile.ai[1] * 2;
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetStaticDefaults()
    { ProjectileID.Sets.MinionShot[Type] = true; ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1700; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14; Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = CrimsonCovenantRules.Duration; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 12;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanDamage() => Opening > 0 && CrimsonCompanion.Parent(Projectile, out _) ? null : false;
    public override void OnSpawn(IEntitySource source)
    {
        if (source is CrimsonCovenantSource own)
        { target = own.Target; incarnation = target.GetGlobalNPC<CrimsonCovenantIncarnation>().Value; }
    }
    public override void AI()
    {
        if (!float.IsFinite(Projectile.ai[0]) || !float.IsFinite(Projectile.ai[1])
            || Projectile.ai[0] < 0 || Projectile.ai[0] > CrimsonCovenantRules.Duration || Projectile.ai[1] is < 140 or > 600)
        { Projectile.Kill(); return; }
        if (!CrimsonCompanion.Parent(Projectile, out _))
        {
            // A remote native child can precede its parent packet. It stays harmless.
            if (Projectile.owner == Main.myPlayer || ++parentWait > 20) Projectile.Kill();
            return;
        }
        parentWait = 0;
        Projectile.ai[0]++;
        if (Projectile.owner == Main.myPlayer && Projectile.ai[0] <= CrimsonCovenantRules.ChargeTicks)
        {
            if (target is not null && target.active && target.CanBeChasedBy(Projectile)
                && target.GetGlobalNPC<CrimsonCovenantIncarnation>().Value == incarnation)
                Projectile.Center = target.Center;
            if (Projectile.ai[0] % 6 == 0 || Projectile.ai[0] == CrimsonCovenantRules.ChargeTicks) Projectile.netUpdate = true;
        }
        if (Projectile.ai[0] >= CrimsonCovenantRules.Duration) Projectile.Kill();
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Opening <= 0 || !CrimsonCompanion.Parent(Projectile, out _)) return false;
        float point = 0;
        Vector2 start = Projectile.Center + new Vector2(Projectile.ai[1], 0);
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start,
            start - new Vector2(Reach * CrimsonInvocation.Ease((Projectile.ai[0] - CrimsonCovenantRules.ChargeTicks) / 7), 0), 48 * Opening, ref point);
    }
}
