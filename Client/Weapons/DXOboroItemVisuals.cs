using System;
using Convergence.Content.Items.DXOboro;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using DXOboroItem = Convergence.Content.Items.DXOboro.DXOboro;

namespace Convergence.Client.Weapons;

[Autoload(Side = ModSide.Client)]
public sealed class DXOboroItemVisuals : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is DXOboroItem;

    public override bool PreDrawInInventory(Item item, SpriteBatch batch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        // tML already fits the source frame into the inventory slot and passes
        // that fitted scale here. Recover the intended on-screen dimension once.
        DXOboroArt.Icon(batch, position, 0,
            Math.Min(44f, Math.Max(frame.Width, frame.Height) * scale * 1.35f), Color.White);
        return false;
    }

    public override bool PreDrawInWorld(Item item, SpriteBatch batch, Color lightColor,
        Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        DXOboroArt.Icon(batch, item.Center - Main.screenPosition, rotation, 52f, lightColor);
        return false;
    }
}
