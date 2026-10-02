#nullable enable
using Convergence.Common.Compatibility.Calamity;
using Terraria;
using Terraria.ID;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Rogue: Bloodink Quill (REWARDS.md, "Rogue - Bloodink Quill"). Calamity is a required dependency, so the weapon
// always exists; rogue damage and stealth come through CalamityRogueArmament. Stub from the shared base: the item
// contract only, no behaviour yet. Owned by the quill slice (.local/design/rewards-slices.md), which adds the throws,
// the ink and the Sealed Score (BloodinkQuill, BloodinkTrail, SealedScore) to this file.
public sealed class CrimsonBloodinkQuill : CalamityRogueArmament
{
    public override string Texture => CrimsonRewardItems.Icon(CrimsonRewardSprites.Quill);

    public override void SetDefaults()
    {
        CrimsonRewardItems.Defaults(Item, CrimsonRewardKind.Rogue);
        Item.DamageType = RogueClass;
        Item.width = 28;
        Item.height = 32;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shootSpeed = CrimsonRewardRules.QuillSpeed;
    }

    public override bool CanUseItem(Player player) => CrimsonRewardItems.Usable(player);
}
