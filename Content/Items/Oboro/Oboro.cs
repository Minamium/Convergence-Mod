using Convergence.Common.Compatibility.Calamity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Items.Oboro;

public sealed class Oboro : ModItem
{
    public override string Texture => "Convergence/Assets/Textures/Items/Oboro/Blade";
    public override void SetDefaults()
    {
        Item.width = Item.height = 56; Item.damage = OboroRules.BaseDamage;
        Item.DamageType = CalamityTrueMelee.Damage; Item.knockBack = 9;
        Item.useTime = Item.useAnimation = OboroRules.LightTicks;
        Item.useStyle = ItemUseStyleID.Shoot; Item.autoReuse = true;
        Item.noMelee = Item.noUseGraphic = true;
        Item.rare = ItemRarityID.Red; Item.value = Item.sellPrice(gold: 50);
    }
    public override bool MeleePrefix() => true;
    public override bool CanUseItem(Player player)
    {
        var state = player.GetModPlayer<OboroPlayer>();
        return !state.SwingVisible && !player.noItems && !player.CCed;
    }
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer && !Main.dedServ)
            OboroPackets.Request(player, OboroAction.Swing, (Main.MouseWorld - player.MountedCenter).ToRotation());
        return true;
    }
    // The client GlobalItem draws the generated sprite at explicit inventory/world sizes.
}

public sealed class Zanshin : ModBuff
{
    public override string Texture => "Convergence/Assets/Textures/Items/Oboro/Spirit";
    public override void SetStaticDefaults() { Main.buffNoSave[Type] = true; Main.debuff[Type] = false; }
    public override void Update(Player player, ref int buffIndex)
    {
        // A vanilla buff packet cannot grant gameplay stats: the accepted weapon state does.
        if (player.GetModPlayer<OboroPlayer>().ZanshinRemaining <= 0) player.DelBuff(buffIndex--);
    }
}
