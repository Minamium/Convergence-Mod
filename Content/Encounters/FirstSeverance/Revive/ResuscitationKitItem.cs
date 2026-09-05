using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Revive;

public sealed class ResuscitationKitItem : ModItem
{
    public override string Texture =>
        "Convergence/Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem";

    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 28;
        Item.maxStack = 1;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.UseSound = SoundID.Item4;
        Item.rare = ItemRarityID.Cyan;
        Item.value = 0;
        Item.noMelee = true;
        Item.consumable = false;
        Item.channel = false;
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server
            && !player.GetModPlayer<FirstSeveranceRaidPlayer>().IsReviving)
        {
            FirstSeveranceClientActions.RequestReviveNearest();
            return true;
        }

        return false;
    }
}
