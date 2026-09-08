#nullable enable
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Retains the existing item identity and Victory drop. All five forms can be
// exchanged one-for-one at a workbench; no extra item is created by conversion.
public sealed class NullRefrain : RitualArmament
{
    public override RitualArmamentKind Kind => RitualArmamentKind.Melee;
    public override void SetDefaults()
    {
        RitualArmamentItems.Defaults(Item, Kind);
        Item.shoot = ModContent.ProjectileType<NullRefrainSlash>();
        Item.shootSpeed = 1; Item.knockBack = 8;
    }
    public override bool CanUseItem(Player player) => base.CanUseItem(player)
        && player.ownedProjectileCounts[Item.shoot] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        int combo = player.GetModPlayer<RitualArmamentPlayer>().Next(Kind, 3);
        Vector2 aim = RitualArmamentItems.Aim(velocity, player.direction);
        Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, Item.shoot,
            RitualArmamentRules.ScaledDamage(damage, combo == 2 ? 1.7f : 1), knockback,
            player.whoAmI, combo, aim.ToRotation(),
            NullRefrainMotion.Duration(combo, player.GetTotalAttackSpeed(DamageClass.Melee)));
        return false;
    }
}
