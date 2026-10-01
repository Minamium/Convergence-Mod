#nullable enable
using Convergence.Common.Compatibility.Calamity;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

// Shared item contract for the Ebon Manor reward set. Weapon classes:
// EbonMoonshear (melee), EbonLoomHarp (ranged), EbonThimble (magic),
// EbonChandelierPole (summon), EbonSeveringSilk (rogue); companion EbonLastWaltz.
internal static class EbonRewardItems
{
    internal const string TextureRoot = "Convergence/Assets/Textures/Items/EbonRewards/";

    // Final pixel art lands in TextureRoot; until then a vanilla item image stands in by reference.
    internal static string Icon(string name, int placeholderItem)
        => ModContent.HasAsset(TextureRoot + name) ? TextureRoot + name : $"Terraria/Images/Item_{placeholderItem}";

    internal static bool Usable(Player player) => player.active && !player.dead
        && !player.GetModPlayer<EbonRecoveryPlayer>().IsIncapacitated
        && !player.GetModPlayer<FirstSeveranceRaidPlayer>().IsRaidDowned
        && !player.GetModPlayer<FirstSeveranceRaidPlayer>().IsRaidEliminated;

    internal static DamageClass DamageClassFor(EbonRewardKind kind) => kind switch
    {
        EbonRewardKind.Melee => CalamityTrueMelee.Damage,
        EbonRewardKind.Ranged => DamageClass.Ranged,
        EbonRewardKind.Magic => DamageClass.Magic,
        EbonRewardKind.Summon => DamageClass.Summon,
        _ => CalamityRogueArmamentDamage.Class,
    };

    internal static int TypeFor(EbonRewardKind kind) => kind switch
    {
        EbonRewardKind.Melee => ModContent.ItemType<EbonMoonshear>(),
        EbonRewardKind.Ranged => ModContent.ItemType<EbonLoomHarp>(),
        EbonRewardKind.Magic => ModContent.ItemType<EbonThimble>(),
        EbonRewardKind.Summon => ModContent.ItemType<EbonChandelierPole>(),
        _ => ModContent.ItemType<EbonSeveringSilk>(),
    };

    // One authoritative pool for the hatbox and The Last Waltz recipe.
    internal static int[] RewardTypes() => new[]
    {
        TypeFor(EbonRewardKind.Melee), TypeFor(EbonRewardKind.Ranged), TypeFor(EbonRewardKind.Magic),
        TypeFor(EbonRewardKind.Summon), TypeFor(EbonRewardKind.Rogue),
    };

    internal static void Defaults(Item item, EbonRewardKind kind)
    {
        item.damage = EbonRewardRules.Damage(kind);
        item.DamageType = DamageClassFor(kind);
        item.useTime = item.useAnimation = EbonRewardRules.UseTicks(kind);
        item.knockBack = 6;
        item.crit = kind == EbonRewardKind.Summon ? 0 : 8;
        item.rare = ItemRarityID.Red;
        item.value = Item.sellPrice(gold: 40);
    }

    internal static Vector2 Aim(Vector2 velocity, int facing)
        => velocity.LengthSquared() > .001f && float.IsFinite(velocity.X) && float.IsFinite(velocity.Y)
            ? Vector2.Normalize(velocity) : new Vector2(facing == -1 ? -1 : 1, 0);

    // Damage helper for secondary hits: always derived from the live weapon damage.
    internal static int Hit(int damage, float multiplier) => EbonRewardRules.Scaled(damage, multiplier);
}
