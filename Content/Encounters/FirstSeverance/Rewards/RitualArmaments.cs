#nullable enable
using System;
using Convergence.Common.Compatibility.Calamity;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public interface IRitualArmament { RitualArmamentKind Kind { get; } }

internal static class RitualArmamentItems
{
    internal const string TexturePath = "Convergence/Assets/Textures/Items/RitualArmaments/NullRefrain";
    internal static bool Usable(Player player) => player.active && !player.dead
        && !player.GetModPlayer<FirstSeveranceRaidPlayer>().IsRaidDowned
        && !player.GetModPlayer<FirstSeveranceRaidPlayer>().IsRaidEliminated;
    internal static DamageClass DamageClassFor(RitualArmamentKind kind) => kind switch
    {
        RitualArmamentKind.Melee => DamageClass.Melee,
        RitualArmamentKind.Ranged => DamageClass.Ranged,
        RitualArmamentKind.Magic => DamageClass.Magic,
        RitualArmamentKind.Summon => DamageClass.Summon,
        _ => CalamityRogueArmamentDamage.Class,
    };
    internal static int TypeFor(RitualArmamentKind kind) => kind switch
    {
        RitualArmamentKind.Melee => ModContent.ItemType<NullRefrain>(),
        RitualArmamentKind.Ranged => ModContent.ItemType<PaleMeridian>(),
        RitualArmamentKind.Magic => ModContent.ItemType<LacunaTestament>(),
        RitualArmamentKind.Summon => ModContent.ItemType<ChoirOfTheUnmade>(),
        _ => ModContent.ItemType<LastWitness>(),
    };
    // One authoritative pool for the box and the collection-completion recipe.
    internal static int[] RewardTypes() => new[]
    {
        TypeFor(RitualArmamentKind.Melee), TypeFor(RitualArmamentKind.Ranged),
        TypeFor(RitualArmamentKind.Magic), TypeFor(RitualArmamentKind.Summon),
        TypeFor(RitualArmamentKind.Rogue),
    };
    internal static void Defaults(Item item, RitualArmamentKind kind)
    {
        item.width = 58; item.height = 28;
        item.damage = RitualArmamentRules.Damage(kind);
        item.DamageType = DamageClassFor(kind);
        item.useTime = item.useAnimation = RitualArmamentRules.UseTicks(kind);
        item.useStyle = ItemUseStyleID.Shoot;
        item.autoReuse = true; item.noMelee = true; item.noUseGraphic = true;
        item.knockBack = 6; item.crit = kind == RitualArmamentKind.Summon ? 0 : 8;
        item.rare = ItemRarityID.Red; item.value = Item.sellPrice(gold: 40);
    }
    internal static Vector2 Aim(Vector2 velocity, int facing)
        => velocity.LengthSquared() > .001f && float.IsFinite(velocity.X) && float.IsFinite(velocity.Y)
            ? Vector2.Normalize(velocity) : new Vector2(facing == -1 ? -1 : 1, 0);
    internal static void Pose(Player player, IEntitySource source, RitualArmamentKind kind, Vector2 aim, int ticks, int accent)
    {
        foreach (Projectile old in Main.ActiveProjectiles)
            if (old.owner == player.whoAmI && old.ModProjectile is RitualArmamentPose pose && pose.Kind == kind)
            { pose.Rearm(aim, ticks, accent); return; }
        Projectile.NewProjectile(source, player.MountedCenter, aim,
            ModContent.ProjectileType<RitualArmamentPose>(), 0, 0, player.whoAmI,
            (float)kind, Math.Clamp(ticks, 6, 90), accent);
    }
}

public abstract class RitualArmament : ModItem, IRitualArmament
{
    public abstract RitualArmamentKind Kind { get; }
    public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/V3/" + Name;
    public override LocalizedText DisplayName => Language.GetText("Mods.Convergence.RitualArmaments." + Name + ".Name");
    public override LocalizedText Tooltip => Language.GetText("Mods.Convergence.RitualArmaments." + Name + ".Tooltip");
    public override bool CanUseItem(Player player) => RitualArmamentItems.Usable(player);
}

public sealed class PaleMeridian : RitualArmament
{
    public override RitualArmamentKind Kind => RitualArmamentKind.Ranged;
    public override void SetDefaults()
    {
        RitualArmamentItems.Defaults(Item, Kind);
        Item.shoot = ModContent.ProjectileType<MeridianBastion>();
        Item.shootSpeed = 19; Item.useAmmo = AmmoID.Bullet;
        Item.channel = true; Item.autoReuse = false;
    }
    public override bool CanUseItem(Player player) => base.CanUseItem(player) && player.ownedProjectileCounts[Item.shoot] == 0;
    public override bool CanConsumeAmmo(Item ammo, Player player)
        => player.ownedProjectileCounts[Item.shoot] > 0 && Main.rand.Next(4) == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Vector2 aim = RitualArmamentItems.Aim(velocity, player.direction);
        Projectile.NewProjectile(source, player.MountedCenter, aim, Item.shoot, damage, knockback, player.whoAmI, 0, -1, 0);
        return false;
    }
}

public sealed class LacunaTestament : RitualArmament
{
    public override RitualArmamentKind Kind => RitualArmamentKind.Magic;
    public override void SetDefaults()
    {
        RitualArmamentItems.Defaults(Item, Kind);
        Item.mana = 8; Item.shootSpeed = 13;
        Item.channel = true; Item.autoReuse = false;
        Item.shoot = ModContent.ProjectileType<LacunaConvergence>();
    }
    public override bool CanUseItem(Player player) => base.CanUseItem(player) && player.ownedProjectileCounts[Item.shoot] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Vector2 aim = RitualArmamentItems.Aim(velocity, player.direction);
        Projectile.NewProjectile(source, player.MountedCenter, aim, Item.shoot,
            damage, knockback, player.whoAmI, 0, aim.X >= 0 ? 1 : -1, 0);
        return false;
    }
}

public sealed class ChoirOfTheUnmade : RitualArmament
{
    public override RitualArmamentKind Kind => RitualArmamentKind.Summon;
    public override void SetStaticDefaults()
    {
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
    }
    public override void SetDefaults()
    {
        RitualArmamentItems.Defaults(Item, Kind);
        Item.mana = 10; Item.shootSpeed = 1;
        Item.buffType = ModContent.BuffType<ChoirOfTheUnmadeBuff>();
        Item.shoot = ModContent.ProjectileType<ChoirSentinel>();
    }
    public override bool CanUseItem(Player player) => base.CanUseItem(player) && player.maxMinions >= 1;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        player.AddBuff(Item.buffType, 2);
        // Spawn near the owner, not at an unchecked distant cursor coordinate.
        float clock = 0;
        foreach (Projectile existing in Main.ActiveProjectiles)
            if (existing.owner == player.whoAmI && existing.ModProjectile is ChoirSentinel)
            { clock = existing.ai[0]; break; }
        int index = Projectile.NewProjectile(source, player.MountedCenter - new Vector2(0, 80),
            Vector2.Zero, Item.shoot, damage, knockback, player.whoAmI, clock, -1, 0);
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        RitualArmamentItems.Pose(player, source, Kind, RitualArmamentItems.Aim(velocity, player.direction), player.itemAnimationMax, 1);
        return false;
    }
}

public sealed class LastWitness : CalamityRogueArmament, IRitualArmament
{
    public RitualArmamentKind Kind => RitualArmamentKind.Rogue;
    public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/V3/" + Name;
    public override LocalizedText DisplayName => Language.GetText("Mods.Convergence.RitualArmaments.LastWitness.Name");
    public override LocalizedText Tooltip => Language.GetText("Mods.Convergence.RitualArmaments.LastWitness.Tooltip");
    public override void SetDefaults()
    {
        RitualArmamentItems.Defaults(Item, Kind);
        Item.DamageType = RogueClass;
        Item.shootSpeed = 14; Item.shoot = ModContent.ProjectileType<WitnessLitany>();
        Item.channel = true; Item.autoReuse = true;
    }
    public override bool CanUseItem(Player player) => RitualArmamentItems.Usable(player) && player.ownedProjectileCounts[Item.shoot] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        bool stealth = HasStealthStrike(player);
        Vector2 aim = RitualArmamentItems.Aim(velocity, player.direction);
        int index = Projectile.NewProjectile(source, player.MountedCenter, aim * 14, Item.shoot,
            damage, knockback, player.whoAmI, 0, -1, stealth ? 1 : 0);
        MarkStealthStrike(index, stealth);
        return false;
    }
}

public sealed class RitualArmamentPlayer : ModPlayer
{
    private readonly int[] sequence = new int[5];
    private int previousItem;
    private int idle;
    internal int Next(RitualArmamentKind kind, int count)
    {
        int value = sequence[(int)kind];
        sequence[(int)kind] = (value + 1) % count; idle = 0;
        return value;
    }
    public override void PostUpdate()
    {
        if (previousItem != Player.HeldItem.type || !RitualArmamentItems.Usable(Player) || ++idle > 90)
        { Array.Clear(sequence); idle = 91; }
        previousItem = Player.HeldItem.type;
    }
}
