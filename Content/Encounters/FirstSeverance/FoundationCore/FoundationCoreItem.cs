using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

public sealed class FoundationCoreItem : ModItem
{
    // Development placeholder: reference a shipped Terraria asset without copying it.
    public override string Texture => $"Terraria/Images/Item_{ItemID.CrystalBall}";

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<FoundationCoreTile>());
        Item.width = 24;
        Item.height = 24;
        Item.maxStack = 1;
        Item.rare = ItemRarityID.Red;
        Item.value = 0;
    }
}
