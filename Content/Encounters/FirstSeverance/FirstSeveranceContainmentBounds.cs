using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// One shared, ground-anchored rectangle. No tiles are created or removed.
internal readonly record struct FirstSeveranceContainmentBounds(float Left, float Top, float Right, float Bottom)
{
    internal static FirstSeveranceContainmentBounds FromGround(float centerX, float groundY)
        => new(centerX - FirstSeveranceArenaBlueprint.WidthInTiles * 8f,
            groundY - FirstSeveranceArenaBlueprint.HeightInTiles * 16f,
            centerX + FirstSeveranceArenaBlueprint.WidthInTiles * 8f, groundY);

    internal float CenterX => (Left + Right) * .5f;
    internal float CenterY => (Top + Bottom) * .5f;

    internal (float X, float Y) ClampBody(float x, float y, int width, int height, float inset = 0f)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || width <= 0 || height <= 0
            || width + inset * 2 >= Right - Left || height + inset * 2 >= Bottom - Top)
            return (CenterX - width * .5f, Bottom - height - Math.Max(0, inset));
        return (Math.Clamp(x, Left + inset, Right - width - inset),
            Math.Clamp(y, Top + inset, Bottom - height - inset));
    }
}
