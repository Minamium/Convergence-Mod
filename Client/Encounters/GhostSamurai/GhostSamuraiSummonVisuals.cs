using Convergence.Client.Graphics;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

[Autoload(Side = ModSide.Client)]
public sealed class GhostSamuraiSummonVisuals : GlobalItem
{
    private static readonly ReadableItemIcon icon = new();
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is GhostSamuraiSummon;
    public override bool PreDrawInInventory(Item item, SpriteBatch batch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        icon.Draw(batch, TextureAssets.Item[item.type].Value, position, ReadableItemIcon.InventoryExtent(frame, scale), Color.White);
        return false;
    }
}
