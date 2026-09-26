using Convergence.Common.Compatibility.Calamity;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Items.DXOboro;

// An independent trial weapon. Oboro's drop, state, and packet contract are untouched.
public sealed class DXOboro : ModItem
{
    public override string Texture => "Convergence/Assets/Textures/Items/DXOboro/Blade";

    public override void SetDefaults()
    {
        Item.width = 54;
        Item.height = 54;
        Item.damage = OboroRules.BaseDamage;
        Item.DamageType = CalamityTrueMelee.Damage;
        Item.knockBack = 9;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = DXOboroMotion.Duration + 2;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<DXOboroCut>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Red;
        Item.value = Item.sellPrice(gold: 50);
    }

    public override bool MeleePrefix() => true;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        // Owner-spawned native projectile sync carries the accepted step and aim.
        // Native projectile hit processing retains Calamity, accessory and NPC hooks.
        if (player.whoAmI != Main.myPlayer || Main.dedServ) return false;
        // Attack-speed bonuses can make useTime shorter than the fixed cut clock.
        // Keep one damaging cut per wielder, including rapid native auto-reuse.
        if (player.ownedProjectileCounts[type] > 0) return false;
        int step = player.GetModPlayer<DXOboroComboPlayer>().TakeStep();
        float aim = velocity.LengthSquared() > .001f
            ? velocity.ToRotation() : player.direction == 1 ? 0f : MathHelper.Pi;
        int projectile = Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
            damage, knockback, player.whoAmI, step, aim);
        if (projectile >= 0 && projectile < Main.maxProjectiles)
            Main.projectile[projectile].netUpdate = true;
        return false;
    }

    public override void AddRecipes()
    {
        // A cheap, reversible trial recipe; the existing Ghost Samurai drop stays Oboro.
        CreateRecipe().AddIngredient(ItemID.FallenStar).AddTile(TileID.WorkBenches).Register();
    }
}

public sealed class DXOboroComboPlayer : ModPlayer
{
    private int nextStep;
    private ulong lastUse;

    internal int TakeStep()
    {
        ulong now = Main.GameUpdateCount;
        if (now - lastUse > 80) nextStep = 0;
        int step = nextStep;
        nextStep = (nextStep + 1) % 3;
        lastUse = now;
        return step;
    }

    public override void UpdateDead() { nextStep = 0; lastUse = 0; }
    public override void PostUpdate()
    {
        if (Player.HeldItem.type != ModContent.ItemType<DXOboro>())
        { nextStep = 0; lastUse = 0; }
    }
}
