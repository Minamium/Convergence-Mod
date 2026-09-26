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
        batch.Draw(texture, center, null, tint, rotation, texture.Size() * .5f,
            maxDimension / Math.Max(texture.Width, texture.Height), SpriteEffects.None, 0);
    }
}
