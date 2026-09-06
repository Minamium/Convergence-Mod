using System;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

public sealed class NullRefrain : ModItem
{
    public override string Texture => "Convergence/Assets/Textures/Items/NullRefrain";

    public override void SetDefaults()
    {
        Item.width = 58; Item.height = 28;
        Item.damage = 7800; Item.DamageType = DamageClass.Melee;
        Item.knockBack = 8; Item.crit = 8;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 24;
        Item.autoReuse = true; Item.noMelee = true; Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<NullRefrainSlash>(); Item.shootSpeed = 1;
        Item.rare = ItemRarityID.Red; Item.value = Item.sellPrice(gold: 40);
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0
        && !player.GetModPlayer<FirstSeveranceRaidPlayer>().IsRaidDowned;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        int combo = player.GetModPlayer<NullRefrainCombo>().TakeStroke();
        float aim = velocity.LengthSquared() > .001f ? MathF.Atan2(velocity.Y, velocity.X)
            : player.direction == 1 ? 0 : MathF.PI;
        Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
            combo == 2 ? (int)(damage * 2.3f) : damage, knockback, player.whoAmI,
            combo, aim, NullRefrainMotion.Duration(combo, player.GetTotalAttackSpeed(DamageClass.Melee)));
        return false;
    }
}

public sealed class NullRefrainCombo : ModPlayer
{
    private int next, idle;
    internal int TakeStroke() { int result = next; next = (next + 1) % 3; idle = 0; return result; }
    public override void PostUpdate()
    {
        if (Player.HeldItem.type != ModContent.ItemType<NullRefrain>() || Player.dead || ++idle > 90)
        { next = 0; idle = 91; }
        if (Player.ownedProjectileCounts[ModContent.ProjectileType<NullRefrainSlash>()] > 0) idle = 0;
    }
}
