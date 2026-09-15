using System;

namespace Convergence.Common.Raids.Arena;

// Ground-anchored footprint shared by the two pedestal Raids. No Terraria types.
internal readonly record struct RaidFieldGeometry(float Left, float Top, float Right, float Bottom)
{
    internal const int WidthInTiles = 160, HeightInTiles = 70;
    internal static RaidFieldGeometry FromGround(float x, float y)
        => new(x - WidthInTiles * 8, y - HeightInTiles * 16, x + WidthInTiles * 8, y);
    internal float CenterX => (Left + Right) * .5f;
    internal float CenterY => (Top + Bottom) * .5f;
    internal bool FitsWorld(int tilesX, int tilesY) => Left >= 320 && Top >= 320
        && Right <= tilesX * 16 - 320 && Bottom <= tilesY * 16 - 320;
    internal (float X, float Y) ClampBody(float x, float y, int width, int height, float inset = 2)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y)) return (CenterX - width * .5f, CenterY - height * .5f);
        return (Math.Clamp(x, Left + inset, Right - width - inset), Math.Clamp(y, Top + inset, Bottom - height - inset));
    }
    internal bool ClipAxis(float x, float y, float dx, float dy, out float start, out float end)
    {
        float near = float.NegativeInfinity, far = float.PositiveInfinity;
        bool axisX = Clip(x, dx, Left, Right), axisY = Clip(y, dy, Top, Bottom);
        start = near; end = far;
        return axisX && axisY && float.IsFinite(near) && float.IsFinite(far) && far - near > 1;
        bool Clip(float p, float d, float low, float high)
        {
            if (Math.Abs(d) < .0001f) return p >= low && p <= high;
            float a = (low - p) / d, b = (high - p) / d;
            near = Math.Max(near, Math.Min(a, b)); far = Math.Min(far, Math.Max(a, b));
            return near < far;
        }
    }
}
