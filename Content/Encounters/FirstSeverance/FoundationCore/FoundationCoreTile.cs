#nullable enable

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

public class FoundationCoreTile : ModTile
{
    protected virtual int TileWidth => 2;
    protected virtual int TileHeight => 2;

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = false;
        TileID.Sets.DisableSmartCursor[Type] = true;
        TileID.Sets.AvoidedByMeteorLanding[Type] = true;
        TileID.Sets.DoesntGetReplacedWithTileReplacement[Type] = true;
        TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
        TileID.Sets.PreventsTileHammeringIfOnTopOfIt[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Width = TileWidth;
        TileObjectData.newTile.Height = TileHeight;
        TileObjectData.newTile.Origin = new Point16(TileWidth / 2, TileHeight - 1);
        TileObjectData.newTile.CoordinateHeights = new int[TileHeight];
        System.Array.Fill(TileObjectData.newTile.CoordinateHeights, 16);
        TileObjectData.newTile.CoordinateHeights[TileHeight - 1] = 18;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop,
            TileWidth, 0);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.WaterDeath = false;
        TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent
            .GetInstance<FoundationCoreTileEntity>()
            .Generic_HookPostPlaceMyPlayer;
        TileObjectData.addTile(Type);

        DustType = DustID.Electric;
        RegisterItemDrop(ModContent.ItemType<FoundationCoreItem>());
        AddMapEntry(new Color(86, 210, 229), CreateMapEntryName());
    }

    public override bool CanKillTile(int i, int j, ref bool blockDamaged)
    {
        _ = blockDamaged;
        return !TileEntity.TryGet(i, j, out FoundationCoreTileEntity core)
            || core.ProtectionState != FirstSeveranceCoreProtectionState.Active;
    }

    public override bool CanExplode(int i, int j)
    {
        return !TileEntity.TryGet(i, j, out FoundationCoreTileEntity core)
            || core.ProtectionState != FirstSeveranceCoreProtectionState.Active;
    }

    public override bool CanReplace(int i, int j, int tileTypeBeingPlaced)
    {
        _ = i;
        _ = j;
        _ = tileTypeBeingPlaced;
        return false;
    }

    public override bool Slope(int i, int j)
    {
        _ = i;
        _ = j;
        return false;
    }

    public override void HitWire(int i, int j)
    {
        Point16 topLeft = TileObjectData.TopLeft(i, j);
        for (int x = topLeft.X; x < topLeft.X + TileWidth; x++)
        {
            for (int y = topLeft.Y; y < topLeft.Y + TileHeight; y++)
            {
                Wiring.SkipWire(x, y);
            }
        }
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        _ = frameX;
        _ = frameY;
        ModContent.GetInstance<FoundationCoreTileEntity>().Kill(i, j);
    }

    public override bool RightClick(int i, int j)
    {
        if (Main.netMode != NetmodeID.Server)
        {
            Point16 topLeft = TileObjectData.TopLeft(i, j);
            FirstSeveranceClientActions.InteractWithCore(topLeft.X, topLeft.Y);
        }

        return true;
    }

    public override void MouseOver(int i, int j)
    {
        _ = i;
        _ = j;
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<TheaterDoll>();
    }
}

// New footprint has its own tile identity: old 2x2 saves are not reinterpreted.
public sealed class FoundationPlinthTile : FoundationCoreTile
{
    protected override int TileWidth => 12;
    protected override int TileHeight => 4;
    public override string Texture => "Terraria/Images/Tiles_0";
    public override bool CanPlace(int i, int j) => FoundationPlinthSpace.IsOpen(i, j + 1);
    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;
}
