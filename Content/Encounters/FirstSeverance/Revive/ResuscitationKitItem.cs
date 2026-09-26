using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking;
using Convergence.Common.Networking.Replication;

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
            var snapshot = Main.netMode == NetmodeID.MultiplayerClient
                ? ModContent.GetInstance<EncounterReplicaSystem>().Snapshot
                : ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot;
            if (!string.IsNullOrEmpty(snapshot.DefinitionKey) && EncounterPacketRouter.Routes.TryGet(snapshot.DefinitionKey, out var handler)
                && handler is IEncounterRecoveryClientActions recovery)
                recovery.RequestReviveNearest();
            else
                Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.CombatNotActive"));
            return true;
        }

        return false;
    }
}
