using System;
using System.Numerics;

namespace Convergence.Client.Encounters.FirstSeverance;

// Physical viewport pixels, captured in the same world pass as the boundary.
// No UI scale or UI-layer Main.screenWidth/Height may enter this calculation.
internal readonly record struct FirstSeveranceMaskRect(float X, float Y, float Width, float Height);

internal readonly record struct FirstSeveranceFieldMaskLayout(
    FirstSeveranceMaskRect Top, FirstSeveranceMaskRect Bottom,
    FirstSeveranceMaskRect Left, FirstSeveranceMaskRect Right)
{
    internal static FirstSeveranceFieldMaskLayout Capture(
        float left, float top, float right, float bottom,
        Matrix3x2 worldToScreen, int viewportWidth, int viewportHeight)
    {
        // Four corners also preserve correct top/bottom when gravity flips the view.
        Vector2 a = Vector2.Transform(new(left, top), worldToScreen);
        Vector2 b = Vector2.Transform(new(right, top), worldToScreen);
        Vector2 c = Vector2.Transform(new(left, bottom), worldToScreen);
        Vector2 d = Vector2.Transform(new(right, bottom), worldToScreen);
        Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        float l = Math.Clamp(min.X, 0, viewportWidth), r = Math.Clamp(max.X, 0, viewportWidth);
        float t = Math.Clamp(min.Y, 0, viewportHeight), u = Math.Clamp(max.Y, 0, viewportHeight);
        // Preserve subpixel boundaries: integer rounding here moves the black edge
        // independently of the world outline at fractional zoom/camera positions.
        return new(new(0, 0, viewportWidth, t), new(0, u, viewportWidth, viewportHeight - u),
            new(0, t, l, u - t), new(r, t, viewportWidth - r, u - t));
    }
}
