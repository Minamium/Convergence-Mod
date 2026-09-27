using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

internal static class DXOboroArt
{
    private const string BladePath = "Convergence/Assets/Textures/Items/DXOboro/Blade";
    private static readonly Vector2 SourceGrip = new(127.5f, 412f);
    private const float SourceAngle = -.5653f;
    private const float SourceLength = 737.4f;
    private static Texture2D Blade => ModContent.Request<Texture2D>(BladePath, AssetRequestMode.ImmediateLoad).Value;
    private static Rectangle? iconBounds;

    internal static void Sword(SpriteBatch batch, Vector2 hand, float axis, float length, Color tint, bool mirror)
    {
        Texture2D texture = Blade;
        batch.Draw(texture, hand - Main.screenPosition, null, tint,
            axis + (mirror ? SourceAngle : -SourceAngle),
            mirror ? new Vector2(SourceGrip.X, texture.Height - SourceGrip.Y) : SourceGrip,
            length / SourceLength,
            mirror ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
    }

    internal static void Icon(SpriteBatch batch, Vector2 center, float rotation, float maxDimension, Color tint)
    {
        Texture2D texture = Blade;
        if (iconBounds is null)
        {
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            int minX = texture.Width, minY = texture.Height, maxX = 0, maxY = 0;
            for (int y = 0; y < texture.Height; y++)
            for (int x = 0; x < texture.Width; x++)
            {
                if (pixels[y * texture.Width + x].A <= 8) continue;
                minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            }
            iconBounds = minX <= maxX
                ? new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1)
                : texture.Bounds;
        }
        Rectangle source = iconBounds.Value;
        float scale = Math.Min(44f, maxDimension) / Math.Max(source.Width, source.Height);
        batch.Draw(texture, center, source, tint, rotation,
            new Vector2(source.Width, source.Height) * .5f,
            scale, SpriteEffects.None, 0);
    }
}
