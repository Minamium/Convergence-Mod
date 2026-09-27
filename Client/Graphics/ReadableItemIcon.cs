#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Graphics;

// Measure once, not every inventory draw. Ignore transparent padding while
// retaining the native slot's scale (including compact hotbars).
internal sealed class ReadableItemIcon
{
    private Texture2D? cached;
    private Rectangle bounds;
    internal void Draw(SpriteBatch batch, Texture2D texture, Vector2 at, float extent, Color tint, float rotation = 0)
    {
        if (!ReferenceEquals(cached, texture))
        {
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            int x0 = texture.Width, y0 = texture.Height, x1 = -1, y1 = -1;
            for (int y = 0; y < texture.Height; y++)
            for (int x = 0; x < texture.Width; x++)
                if (pixels[y * texture.Width + x].A > 8)
                { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
            bounds = x1 >= x0 ? new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1) : texture.Bounds;
            cached = texture;
        }
        batch.Draw(texture, at, bounds, tint, rotation, new Vector2(bounds.Width, bounds.Height) * .5f,
            extent / Math.Max(bounds.Width, bounds.Height), SpriteEffects.None, 0);
    }
    internal static float InventoryExtent(Rectangle frame, float scale)
        => Math.Min(44f, Math.Max(frame.Width, frame.Height) * scale * 1.35f);
}
