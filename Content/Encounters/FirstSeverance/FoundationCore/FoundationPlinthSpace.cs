using Terraria;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

internal static class FoundationPlinthSpace
{
    private static int queryX, queryY, queryWorld;
    private static ulong queryTick;
    private static bool queryResult, hasQuery;
    // Placement uses the cursor anchor (middle column, bottom row). Activation
    // repeats the complete authority scan; this local preview is not a permission.
    internal static bool IsOpen(int centerX, int baseY)
    {
        ulong now = Main.GameUpdateCount;
        if (hasQuery && queryX == centerX && queryY == baseY && queryWorld == Main.worldID
            && now >= queryTick && now - queryTick < 8) return queryResult;
        queryX = centerX;
        queryY = baseY;
        queryWorld = Main.worldID;
        queryTick = now;
        hasQuery = true;
        return queryResult = Scan(centerX, baseY);
    }

    private static bool Scan(int centerX, int baseY)
    {
        int left = centerX - FirstSeveranceArenaBlueprint.WidthInTiles / 2;
        int top = baseY - FirstSeveranceArenaBlueprint.HeightInTiles;
        if (left < 20 || top < 20 || left + 320 >= Main.maxTilesX - 20 || baseY >= Main.maxTilesY - 20) return false;
        for (int x = left; x < left + 320; x++)
            for (int y = top; y < baseY; y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]) return false;
            }
        return true;
    }
}
