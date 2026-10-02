#nullable enable
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// The Scarlet Score Reliquary (REWARDS.md, "Scarlet Score Reliquary"): an ordinary openable container in
// every difficulty. Deliberately not ItemID.Sets.BossBag (that flag injects vanilla developer drops). Dropped only by
// CrimsonRuntime on an accepted Victory, one shared world item per frozen member.
public sealed class CrimsonScoreReliquary : ModItem
{
    // Client-only presentation hook; Content never references Client code directly.
    internal static System.Action<Player>? Opened;

    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Reliquary);

    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 3;

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 28;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.rare = ItemRarityID.Red;
        Item.value = Item.sellPrice(gold: CrimsonRewardRules.SellGold);
    }

    public override bool CanRightClick() => true;

    // Presentation only: native opening consumes the reliquary and grants the loot below.
    public override void RightClick(Player player)
    {
        if (!Main.dedServ && player.whoAmI == Main.myPlayer) Opened?.Invoke(player);
    }

    // Exactly one of the five weapons, 20% each, independent of Luck.
    public override void ModifyItemLoot(ItemLoot itemLoot)
        => itemLoot.Add(ItemDropRule.OneFromOptionsNotScalingWithLuck(1, CrimsonRewardItems.RewardTypes()));
}
