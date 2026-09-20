using Convergence.Common.Encounters.Abstractions;
using Convergence.Content.Shared;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.AzureCathedral;

public sealed class AzureConductor : ModItem, IRaidPedestalKey
{
    public override string Texture => "Convergence/Assets/Textures/AzureCathedral/GlacialChime";
    void IRaidPedestalKey.Interact(int x, int y) => AzurePackets.Summon(x, y);
    public override void SetDefaults()
    {
        Item.width = Item.height = 32; Item.useTime = Item.useAnimation = 35;
        Item.useStyle = ItemUseStyleID.HoldUp; Item.rare = ItemRarityID.Cyan;
        Item.consumable = false; Item.noUseGraphic = true; Item.UseSound = SoundID.Item4;
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) => !player.dead && (AzurePackets.Snapshot.Lifecycle == EncounterLifecycle.Idle
        && RaidPedestal.TryResolve(Player.tileTargetX, Player.tileTargetY, out _)
        || player.altFunctionUse == 2 && AzurePackets.Snapshot.DefinitionKey == AzureDefinition.EncounterKey);
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        { if (player.altFunctionUse == 2) AzurePackets.Ready(false, true); else AzurePackets.Summon(Player.tileTargetX, Player.tileTargetY); }
        return true;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.IceBlock, 30)
        .AddIngredient(ItemID.Glass, 15).AddIngredient(ItemID.Sapphire, 3).AddTile(TileID.WorkBenches).Register();
}
