using Convergence.Common.Encounters.Abstractions;
using Convergence.Content.Shared;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor;

public sealed class EbonInvitation : ModItem, IRaidPedestalKey
{
    public override string Texture => "Convergence/Assets/Textures/EbonManor/BlackInvitation";
    void IRaidPedestalKey.Interact(int x, int y) => EbonPackets.Summon(x, y);
    public override void SetDefaults()
    {
        Item.width = Item.height = 32; Item.useTime = Item.useAnimation = 35;
        Item.useStyle = ItemUseStyleID.HoldUp; Item.rare = ItemRarityID.Pink;
        Item.consumable = false; Item.noUseGraphic = true; Item.UseSound = SoundID.Item4;
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) => !player.dead && (EbonPackets.Snapshot.Lifecycle == EncounterLifecycle.Idle
        && RaidPedestal.TryResolve(Player.tileTargetX, Player.tileTargetY, out _)
        || player.altFunctionUse == 2 && EbonPackets.Snapshot.DefinitionKey == EbonDefinition.EncounterKey);
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        { if (player.altFunctionUse == 2) EbonPackets.Ready(false, true); else EbonPackets.Summon(Player.tileTargetX, Player.tileTargetY); }
        return true;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Silk, 10)
        .AddIngredient(ItemID.BlackInk, 1).AddTile(TileID.WorkBenches).Register();
}
