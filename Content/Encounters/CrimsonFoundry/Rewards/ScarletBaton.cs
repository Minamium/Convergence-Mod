#nullable enable
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Magic: Scarlet Baton (REWARDS.md, "Magic - Scarlet Baton"). Stub from the shared base: the item contract only, no
// behaviour yet. Owned by the baton slice (.local/design/rewards-slices.md), which adds conducting, the score and the
// Tutti (BatonSwing, BatonStroke, BatonRiver) to this file.
public sealed class CrimsonBaton : ModItem
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Baton);

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Magic);
        Item.width = Item.height = 40;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
    }

    public override bool CanUseItem(Player player) => CrimsonRewardItems.Usable(player);
}
