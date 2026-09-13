using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

// The reusable stage key is deliberately separate from the ten-slot companion:
// requiring a Boss reward to start the first fight would create a progression loop.
public sealed class TheaterDoll : ModItem
{
    public override string Texture => "Convergence/Assets/Textures/Items/TheaterDoll";
    public override void SetDefaults()
    {
        Item.width = Item.height = 40;
        Item.maxStack = 1;
        Item.rare = ItemRarityID.Pink;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.useTime = Item.useAnimation = 20;
        Item.noMelee = true;
        Item.consumable = false;
        Item.value = 0;
    }

    public override bool CanUseItem(Player player)
        => TileEntity.TryGet(Player.tileTargetX, Player.tileTargetY, out FoundationCoreTileEntity _);

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer
            && TileEntity.TryGet(Player.tileTargetX, Player.tileTargetY, out FoundationCoreTileEntity core))
            FirstSeveranceClientActions.InteractWithCore(core.Position.X, core.Position.Y);
        return true;
    }

    public override void AddRecipes()
        => CreateRecipe().AddIngredient(ItemID.Silk, 10).AddIngredient(ItemID.FallenStar)
            .AddTile(TileID.WorkBenches).Register();
}
