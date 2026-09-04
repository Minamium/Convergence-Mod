#nullable enable

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

public sealed class FoundationCoreTile : ModTile
{
    private const int TileWidth = 2;
    private const int TileHeight = 2;

    // Development placeholder: the 2x2 Crystal Ball sheet matches this TileObjectData.
    public override string Texture => $"Terraria/Images/Tiles_{TileID.CrystalBall}";

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
        TileObjectData.newTile.CoordinateHeights = [16, 18];
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.WaterDeath = false;
        TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent
            .GetInstance<FoundationCoreTileEntity>()
            .Generic_HookPostPlaceMyPlayer;
        TileObjectData.addTile(Type);

        DustType = DustID.Electric;
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
        player.cursorItemIconID = ModContent.ItemType<FoundationCoreItem>();
    }
}
