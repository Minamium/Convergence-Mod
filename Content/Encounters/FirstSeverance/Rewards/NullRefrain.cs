#nullable enable
using System;
using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Existing saved item identity, Victory drop and all one-for-one exchanges survive.
public sealed class NullRefrain : RitualArmament
{
    public override RitualArmamentKind Kind => RitualArmamentKind.Melee;
    public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/NullCantorClaws";
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        // Exclude the export's transparent padding, preserving the original artwork.
        // Use the same longest-edge slot fit as the other ritual weapon icons.
        Rectangle artwork = new(21, 26, 86, 87);
        float fittedScale = scale * Math.Max(frame.Width, frame.Height) / Math.Max(artwork.Width, artwork.Height);
        spriteBatch.Draw(TextureAssets.Item[Type].Value, position, artwork, Item.GetAlpha(drawColor),
            0f, new Vector2(artwork.Width, artwork.Height) * .5f, fittedScale, SpriteEffects.None, 0f);
        return false;
    }
    public override void SetDefaults()
    {
        RitualArmamentItems.Defaults(Item, Kind);
        Item.damage = NullCantorClawMotion.BaseDamage;
        Item.DamageType = CalamityTrueMelee.Damage;
        Item.useTime = Item.useAnimation = NullCantorClawMotion.SwingTicks;
        Item.shoot = ModContent.ProjectileType<NullCantorClawSwipe>();
        Item.shootSpeed = 1; Item.knockBack = 9;
        Item.width = Item.height = 46;
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player)
    {
        bool crushing = player.altFunctionUse == 2;
        if (player.noItems || player.CCed || !base.CanUseItem(player)
            || player.ownedProjectileCounts[ModContent.ProjectileType<NullCantorClawSwipe>()] != 0
            || player.ownedProjectileCounts[ModContent.ProjectileType<NullCantorClawCrush>()] != 0
            || crushing && !player.GetModPlayer<NullCantorClawPlayer>().Charge.Ready) return false;
        // Only the physically swung claws receive true-melee bonuses. The remote
        // execution is ordinary melee; its separate cooldown ignores attack speed.
        Item.DamageType = crushing ? DamageClass.Melee : CalamityTrueMelee.Damage;
        return true;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var state = player.GetModPlayer<NullCantorClawPlayer>();
        if (player.altFunctionUse == 2)
        {
            Vector2 point = Main.MouseWorld;
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) return false;
            var bounded = NullCantorClawMotion.ClampTarget(new(player.Center.X, player.Center.Y), new(point.X, point.Y));
            point = new(Math.Clamp(bounded.X, 32, Main.maxTilesX * 16 - 32), Math.Clamp(bounded.Y, 32, Main.maxTilesY * 16 - 32));
            if (!state.Charge.TrySpend()) return false;
            Projectile.NewProjectile(source, point, Vector2.Zero, ModContent.ProjectileType<NullCantorClawCrush>(),
                RitualArmamentRules.ScaledDamage(damage, NullCantorClawMotion.CrushMultiplier), knockback * 1.4f,
                player.whoAmI);
        }
        else
        {
            Vector2 aim = RitualArmamentItems.Aim(velocity, player.direction);
            int hand = state.NextHand;
            state.NextHand ^= 1;
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, Item.shoot,
                damage, knockback, player.whoAmI, hand, aim.ToRotation(),
                NullCantorClawMotion.Duration(player.GetTotalAttackSpeed(CalamityTrueMelee.Damage)));
        }
        return false;
    }
}

public sealed class NullCantorClawPlayer : ModPlayer
{
    internal readonly NullCantorClawCharge Charge = new();
    internal int NextHand;
    public override void PostUpdate()
    {
        if (!Player.active || Player.dead) { Charge.Reset(); NextHand = 0; return; }
        Charge.Tick(Player.HeldItem.type == ModContent.ItemType<NullRefrain>(), RitualArmamentItems.Usable(Player) && !Player.noItems && !Player.CCed,
            Player.ownedProjectileCounts[ModContent.ProjectileType<NullCantorClawCrush>()] > 0);
    }
    public override void OnEnterWorld() { Charge.Reset(); NextHand = 0; }
    public override void UpdateDead() { Charge.Reset(); NextHand = 0; }
}
