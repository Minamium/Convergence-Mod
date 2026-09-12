using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking.Replication;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

public sealed class GhostSamuraiSummon : ModItem
{
    // Vanilla asset reference only; no extracted asset is distributed.
    public override string Texture => "Terraria/Images/Item_" + ItemID.MechanicalSkull;
    public override void SetDefaults()
    {
        Item.width = 32; Item.height = 32;
        Item.useTime = Item.useAnimation = 45;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.rare = ItemRarityID.Cyan;
        Item.consumable = false;
        Item.UseSound = SoundID.Item44;
    }
    public override bool CanUseItem(Player player)
    {
        var state = Main.netMode == NetmodeID.MultiplayerClient
            ? ModContent.GetInstance<EncounterReplicaSystem>().Snapshot
            : ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot;
        return !player.dead && state.Lifecycle == EncounterLifecycle.Idle && !NPC.AnyNPCs(ModContent.NPCType<GhostSamuraiBoss>());
    }
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer) GhostSamuraiPackets.RequestSummon(player);
        return true;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bone, 10)
        .AddIngredient(ItemID.FallenStar, 5).AddTile(TileID.WorkBenches).Register();
}
