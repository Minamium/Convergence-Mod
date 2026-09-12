using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// An ordinary openable reward container in every difficulty. Deliberately not
// ItemID.Sets.BossBag: that flag also injects vanilla developer-armour drops.
public sealed class DollTreasureBox : ModItem
{
    public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/DollTreasureBox";

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 3;
    }

    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 40;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.rare = ItemRarityID.Red;
        Item.value = Item.sellPrice(gold: 40);
    }

    public override bool CanRightClick() => true;

    public override void ModifyItemLoot(ItemLoot itemLoot)
    {
        // One guaranteed draw, uniform among five distinct forms (20% each).
        // Native opening consumes one box; no duplicate RightClick grant.
        itemLoot.Add(ItemDropRule.OneFromOptionsNotScalingWithLuck(1, RitualArmamentItems.RewardTypes()));
    }
}
