#nullable enable
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public sealed class DollCovenant : ModItem
{
    public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/DollCovenant";
    public override void SetStaticDefaults()
    {
        ItemID.Sets.StaffMinionSlotsRequired[Type] = DollCompanionRules.Slots;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 40;
        Item.damage = DollCompanionRules.Damage;
        Item.DamageType = DamageClass.Summon;
        Item.mana = 10; Item.knockBack = 3;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.HoldUp; Item.noMelee = true;
        Item.rare = ItemRarityID.Red; Item.value = Item.sellPrice(gold: 40);
        Item.buffType = ModContent.BuffType<DollCovenantBuff>();
        Item.shoot = ModContent.ProjectileType<DollCompanion>(); Item.shootSpeed = 1;
    }
    public override bool CanUseItem(Player player) => RitualArmamentItems.Usable(player)
        && DollCompanionRules.CanSummon(player.maxMinions, player.ownedProjectileCounts[Item.shoot]);
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        player.AddBuff(Item.buffType, 2);
        int index = Projectile.NewProjectile(source, player.Center - new Vector2(player.direction * 48, 32),
            Vector2.Zero, Item.shoot, damage, knockback, player.whoAmI, 0, -1, 1);
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
    public override void AddRecipes()
    {
        var recipe = CreateRecipe();
        foreach (int weapon in RitualArmamentItems.RewardTypes()) recipe.AddIngredient(weapon);
        recipe.AddTile(TileID.WorkBenches).Register();
    }
}

public sealed class DollCovenantBuff : ModBuff
{
    public override string Texture => "Convergence/Assets/Textures/NPCs/DollTheater/DollHead";
    public override void SetStaticDefaults()
    { Main.buffNoSave[Type] = true; Main.buffNoTimeDisplay[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<DollCompanion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}
