using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

public sealed class FoundationCoreItem : ModItem
{
    public override void ModifyTooltips(List<TooltipLine> tooltips)
        => tooltips.Add(new TooltipLine(Mod, "TheaterDollKey",
            Language.GetTextValue("Mods.Convergence.UI.DollActivation.RequiresDoll")));

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
