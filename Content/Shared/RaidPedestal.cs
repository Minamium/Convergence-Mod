#nullable enable
using Convergence.Common.Foundation.Identifiers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Convergence.Content.Shared;

// The old tile/TE IDs stay stable in saves. Features only consume this adapter;
// the existing pedestal owns its lease and protection implementation.
internal interface IRaidPedestal
{
    Vector2 Ground { get; }
    bool Claim(ulong sequence, FightId fight);
    bool IsOwned(FightId fight);
    bool Activate(FightId fight);
    void Release(FightId fight);
}
internal interface IRaidPedestalKey
{
    void Interact(int tileX, int tileY);
}
internal static class RaidPedestal
{
    internal static bool TryResolve(int x, int y, out IRaidPedestal pedestal)
    {
        pedestal = null!;
        if (!WorldGen.InWorld(x, y, 1)) return false;
        Point16 top = TileObjectData.TopLeft(x, y);
        if (TileEntity.ByPosition.TryGetValue(top, out TileEntity? entity)
            && entity is ModTileEntity tile && tile.IsTileValidForEntity(top.X, top.Y)
            && entity is IRaidPedestal candidate)
        { pedestal = candidate; return true; }
        return false;
    }
    internal static bool UseHeldKey(int x, int y)
    {
        if (Main.netMode == NetmodeID.Server || Main.LocalPlayer.HeldItem.ModItem is not IRaidPedestalKey key) return false;
        key.Interact(x, y); return true;
    }
}
