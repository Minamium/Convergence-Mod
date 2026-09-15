using Convergence.Common.Encounters.Abstractions;
using Convergence.Content.Shared;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

public sealed class CrimsonConductor : ModItem, IRaidPedestalKey
{
    void IRaidPedestalKey.Interact(int tileX, int tileY) => CrimsonPackets.Summon(tileX, tileY);
    public override string Texture => "Terraria/Images/Item_" + ItemID.MechanicalBatteryPiece;
    public override void SetDefaults()
    {
        Item.width = Item.height = 32; Item.useAnimation = Item.useTime = 35;
        Item.useStyle = ItemUseStyleID.HoldUp; Item.rare = ItemRarityID.Red;
        Item.consumable = false; Item.UseSound = SoundID.Item4;
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) => !player.dead && (CrimsonPackets.Snapshot.Lifecycle == EncounterLifecycle.Idle
        && RaidPedestal.TryResolve(Player.tileTargetX, Player.tileTargetY, out _)
        || player.altFunctionUse == 2 && CrimsonPackets.Snapshot.DefinitionKey == CrimsonDefinition.EncounterKey);
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            if (player.altFunctionUse == 2) CrimsonPackets.Ready(false, true);
            else CrimsonPackets.Summon(Player.tileTargetX, Player.tileTargetY);
        }
        return true;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.IronBar, 10)
        .AddIngredient(ItemID.Wire, 20).AddIngredient(ItemID.SoulofNight, 3).AddTile(TileID.MythrilAnvil).Register();
}
