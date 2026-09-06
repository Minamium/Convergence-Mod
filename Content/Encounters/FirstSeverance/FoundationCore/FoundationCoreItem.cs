using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

public sealed class FoundationCoreItem : ModItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<FoundationPlinthTile>());
        Item.width = 48;
        Item.height = 24;
        Item.maxStack = 1;
        Item.rare = ItemRarityID.Red;
        Item.value = 0;
    }
}
