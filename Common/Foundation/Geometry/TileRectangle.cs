namespace Convergence.Common.Foundation.Geometry;

internal readonly record struct TileRectangle(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;

    public int Bottom => Top + Height;

    public bool IsValid => Width > 0 && Height > 0;

    public bool Contains(TilePoint point)
    {
        return point.X >= Left
            && point.X < Right
            && point.Y >= Top
            && point.Y < Bottom;
    }
}

