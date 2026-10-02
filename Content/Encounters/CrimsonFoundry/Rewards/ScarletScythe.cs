#nullable enable
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Melee: Sable Scythe (REWARDS.md, "Melee - Sable Scythe"). Stub from the shared base: the item contract only, no
// behaviour yet. Owned by the scythe slice (.local/design/rewards-slices.md), which adds the figure-eight strokes, the
// staff and Staff Reap (SableStroke, SableStaff, SableRelease, StaffCut) to this file.
public sealed class CrimsonSableScythe : ModItem
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Scythe);

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Melee);
        Item.width = Item.height = 56;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
    }

    public override bool CanUseItem(Player player) => CrimsonRewardItems.Usable(player);
}
