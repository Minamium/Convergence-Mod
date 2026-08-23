namespace Convergence.Common.Foundation.Geometry;

internal readonly record struct ArenaProfile(int WidthInTiles, int HeightInTiles)
{
    public bool IsValid => WidthInTiles > 0 && HeightInTiles > 0;
}

