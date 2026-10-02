#nullable enable
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Summon: Ember Censer (REWARDS.md, "Summon - Ember Censer"). Stub from the shared base: the item contract and the
// buff's identity only, no behaviour yet. Owned by the censer slice (.local/design/rewards-slices.md), which adds the
// minion (EmberCenserMinion), the pendulum and the pours, and the buff's native minion upkeep to this file.
public sealed class CrimsonEmberCenser : ModItem
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Censer);

    public override void SetStaticDefaults() => ItemID.Sets.StaffMinionSlotsRequired[Type] = CrimsonRewardRules.CenserSlots;

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Summon);
        Item.width = 24;
        Item.height = 32;
        Item.mana = CrimsonRewardRules.CenserMana;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
    }

    public override bool CanUseItem(Player player) => CrimsonRewardItems.Usable(player);
}

public sealed class CrimsonEmberCenserBuff : ModBuff
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.CenserBuff);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}
