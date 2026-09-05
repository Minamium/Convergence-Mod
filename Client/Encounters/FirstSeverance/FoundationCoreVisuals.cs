#nullable enable

using System;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// Presentation only: existing 2x2 tile frames, TE identity and activation stay intact.
[Autoload(Side = ModSide.Client)]
public sealed class FoundationCoreVisuals : GlobalTile
{
    private Asset<Texture2D>? monument;
    private const float VisualHeight = 176f;

    public override void Load()
        => monument = ModContent.Request<Texture2D>(
            "Convergence/Assets/Textures/Tiles/FoundationCoreMonument");

    public override void Unload() => monument = null;

    public override void DrawEffects(int i, int j, int type, SpriteBatch spriteBatch,
        ref TileDrawInfo drawData)
    {
        if (type == ModContent.TileType<FoundationCoreTile>()
            && Main.tile[i, j].TileFrameX == 0 && Main.tile[i, j].TileFrameY == 0)
            Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
    }

    public override void SpecialDraw(int i, int j, int type, SpriteBatch spriteBatch)
    {
        if (Main.dedServ || type != ModContent.TileType<FoundationCoreTile>())
            return;
        Tile tile = Main.tile[i, j];
        if (!tile.HasTile || tile.IsActuated || tile.TileFrameX != 0 || tile.TileFrameY != 0)
            return;
        bool active = TileEntity.TryGet(i, j, out FoundationCoreTileEntity core)
            && core.ProtectionState == FirstSeveranceCoreProtectionState.Active;
        DrawMonument(spriteBatch, new Vector2(i * 16 + 16, j * 16 + 32) - Main.screenPosition,
            Color.White, active);
    }

    public override void PostDrawPlacementPreview(int i, int j, int type,
        SpriteBatch spriteBatch, Rectangle frame, Vector2 position, Color color,
        bool validPlacement, SpriteEffects spriteEffects)
    {
        if (type == ModContent.TileType<FoundationCoreTile>() && frame.X == 0 && frame.Y == 0)
            DrawMonument(spriteBatch, position + new Vector2(16, 32),
                validPlacement ? Color.LightCyan * 0.65f : Color.IndianRed * 0.65f, false);
    }

    private void DrawMonument(SpriteBatch batch, Vector2 foot, Color tint, bool active)
    {
        if (monument is null)
            return;
        Texture2D texture = monument.Value;
        float scale = VisualHeight / texture.Height;
        // The generated sprite has a small transparent bottom margin.
        Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.957f);
        batch.Draw(texture, foot, null, tint, 0f, origin, scale, SpriteEffects.None, 0f);
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        float pulse = reduced ? 0.6f : 0.6f + 0.2f * MathF.Sin((float)Main.GlobalTimeWrappedHourly * 2.2f);
        Color glow = (active ? Color.LightGoldenrodYellow : Color.Cyan) * (pulse * tint.A / 255f);
        // A 32-pixel console light marks the unchanged, clickable 2x2 base.
        batch.Draw(TextureAssets.MagicPixel.Value, foot - new Vector2(16, 9),
            new Rectangle(0, 0, 1, 1), glow, 0f, Vector2.Zero,
            new Vector2(32, 2), SpriteEffects.None, 0f);
    }
}
