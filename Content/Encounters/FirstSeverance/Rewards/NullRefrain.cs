#nullable enable
using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Lacrimosa's Claws. The saved item identity, its place in the Curtainfall Treasure Box pool and in The Unbroken
// Promise recipe are unchanged. Left click runs the three-step kata of the held controller (LacrimosaClawKata);
// right click with six lit beads sends both hands to grasp and crush (LacrimosaClawGrasp). The 0.2.x swipe/crush
// projectiles stay as unused legacy types.
public sealed class NullRefrain : RitualArmament
{
    public override RitualArmamentKind Kind => RitualArmamentKind.Melee;
    public override string Texture => LacrimosaClawKata.IconTexture;

    public override void SetDefaults()
    {
        RitualArmamentItems.Defaults(Item, Kind);
        Item.damage = LacrimosaClawMotion.BaseDamage;
        Item.DamageType = CalamityTrueMelee.Damage;
        // Each stroke owns its own clock; the next begins the tick after the last one ends (Moonshear's pattern).
        Item.useTime = Item.useAnimation = 2;
        Item.shoot = ModContent.ProjectileType<LacrimosaClawKata>();
        Item.shootSpeed = 1; Item.knockBack = 9;
        Item.width = Item.height = 46;
    }

    public override bool AltFunctionUse(Player player) => true;

    // The hands float beside their owner whenever the claws are held and usable.
    public override void HoldItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer && LacrimosaClawKata.OwnerUsable(player)) Ensure(player);
    }

    private LacrimosaClawKata? Ensure(Player player)
    {
        if (LacrimosaClawKata.Find(player) is { } existing) return existing;
        if (player.ownedProjectileCounts[Item.shoot] > 0) return null;
        int index = Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, Vector2.Zero, Item.shoot, 0, 0,
            player.whoAmI, LacrimosaClawMotion.None, player.direction == -1 ? MathHelper.Pi : 0, 0);
        if (index < 0 || index >= Main.maxProjectiles || Main.projectile[index].ModProjectile is not LacrimosaClawKata kata) return null;
        player.GetModPlayer<LacrimosaClawPlayer>().KataIndex = index;
        return kata;
    }

    public override bool CanUseItem(Player player)
    {
        bool grasp = player.altFunctionUse == 2;
        var state = player.GetModPlayer<LacrimosaClawPlayer>();
        if (!base.CanUseItem(player) || !LacrimosaClawKata.OwnerUsable(player) || state.GraspBusy) return false;
        if (grasp ? !state.Heart.Ready : LacrimosaClawKata.Find(player) is { Finished: false }) return false;
        // Only the raking and clapping hands take true-melee bonuses; the grasp and crush are ordinary melee.
        Item.DamageType = grasp ? DamageClass.Melee : CalamityTrueMelee.Damage;
        return true;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var state = player.GetModPlayer<LacrimosaClawPlayer>();
        if (player.altFunctionUse == 2)
        {
            state.TryGrasp(source, Main.MouseWorld, damage, knockback);
            return false;
        }
        if (Ensure(player) is not { } kata || !kata.Finished) return false;
        Vector2 aim = RitualArmamentItems.Aim(velocity, player.direction);
        float speed = player.GetTotalAttackSpeed(CalamityTrueMelee.Damage);
        int stroke = state.Combo.Take(Main.GameUpdateCount, s => LacrimosaClawMotion.Duration(s, speed));
        kata.Begin(stroke, aim.ToRotation(), LacrimosaClawMotion.Duration(stroke, speed), damage, knockback,
            player.GetWeaponCrit(Item), player.GetWeaponArmorPenetration(Item));
        return false;
    }
}
