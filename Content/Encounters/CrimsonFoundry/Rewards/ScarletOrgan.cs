#nullable enable
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Ranged: Canticle Organ (REWARDS.md, "Ranged - Canticle Organ"). Stub from the shared base: the item contract only,
// no behaviour yet. Owned by the organ slice (.local/design/rewards-slices.md), which adds the shards, the mark ledger
// and the Hymn of Hands (CanticleShard, BoneHand) to this file.
public sealed class CrimsonCanticleOrgan : ModItem
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Organ);

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Ranged);
        Item.width = 44;
        Item.height = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
    }

    public override bool CanUseItem(Player player) => CrimsonRewardItems.Usable(player);
}
