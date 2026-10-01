#nullable enable
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// The Ebon Manor reward box. An ordinary openable container in every difficulty;
// deliberately not ItemID.Sets.BossBag (that flag injects vanilla developer drops).
public sealed class EbonHatbox : ModItem
{
    // Client-only presentation hook; Content never references Client code directly.
    internal static System.Action<Player>? Opened;

    public override string Texture => EbonRewardItems.Icon(nameof(EbonHatbox), ItemID.Present);

    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 3;

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 30;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.rare = ItemRarityID.Red;
        Item.value = Item.sellPrice(gold: 40);
    }

    public override bool CanRightClick() => true;

    // Presentation only: native opening consumes the box and grants the loot below.
    public override void RightClick(Player player)
    {
        if (!Main.dedServ && player.whoAmI == Main.myPlayer) Opened?.Invoke(player);
    }

    public override void ModifyItemLoot(ItemLoot itemLoot)
        => itemLoot.Add(ItemDropRule.OneFromOptionsNotScalingWithLuck(1, EbonRewardItems.RewardTypes()));
}
