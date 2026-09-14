using System;
using System.IO;

namespace Convergence.Content.Encounters.GhostSamurai;

// Same 160x70 tile size; the summoner foot position is the bottom midpoint.
// Reject a world edge that cannot fit the complete field. No tile edits.
internal readonly record struct SamuraiArenaBounds(float CenterX, float CenterY, float HalfWidth, float HalfHeight)
{
    internal const float Width = 2560, Height = 1120, WorldMargin = 32;
    internal float Left => CenterX - HalfWidth;
    internal float Right => CenterX + HalfWidth;
    internal float Top => CenterY - HalfHeight;
    internal float Bottom => CenterY + HalfHeight;
    internal bool IsValid => float.IsFinite(CenterX) && float.IsFinite(CenterY)
        && CenterX is >= WorldMargin and <= 500000 && CenterY is >= WorldMargin and <= 500000
        && HalfWidth == Width / 2 && HalfHeight == Height / 2
        && Left >= WorldMargin && Top >= WorldMargin && Right <= 500000 && Bottom <= 500000;
    internal static SamuraiArenaBounds Create(float x, float y, float worldWidth, float worldHeight)
    {
        var field = new SamuraiArenaBounds(x, y - Height / 2, Width / 2, Height / 2);
        return field.IsValid && field.Right <= worldWidth - WorldMargin && field.Bottom <= worldHeight - WorldMargin
            ? field : default;
    }
    internal bool Contains(float x, float y) => IsValid && x >= Left && x <= Right && y >= Top && y <= Bottom;
    internal (float X, float Y) ClampBody(float x, float y, int width, int height, float inset = 0)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || width <= 0 || height <= 0
            || width + inset * 2 >= HalfWidth * 2 || height + inset * 2 >= HalfHeight * 2)
            return (CenterX - width / 2f, CenterY - height / 2f);
        return (Math.Clamp(x, Left + inset, Right - width - inset), Math.Clamp(y, Top + inset, Bottom - height - inset));
    }
    internal void Write(BinaryWriter w) { w.Write(CenterX); w.Write(CenterY); w.Write(HalfWidth); w.Write(HalfHeight); }
    internal static SamuraiArenaBounds Read(BinaryReader r)
    {
        var result = new SamuraiArenaBounds(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        if (!result.IsValid) throw new InvalidDataException("ghost_samurai.arena_invalid");
        return result;
    }
}
