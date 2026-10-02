#nullable enable
using System;
using System.Numerics;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Mirrors applied about a sprite's pivot, before its rotation. Horizontal negates texel x, Vertical texel y.
[Flags]
internal enum DollFlip : byte { None = 0, Horizontal = 1, Vertical = 2, Both = 3 }

// Texel <-> world placement of a Doll pixel sprite. One texel is one dot is WorldPerTexel world px,
// always: size changes only through integer export rungs, never by scaling at draw time.
//   world(t) = pivotWorld + R(rotation) * F(flip) * ((t - pivotTexel) * WorldPerTexel)
// Texel coordinates run from the image's top-left corner; texel i spans [i, i + 1), centre i + .5.
// Side-view art points +X and mirrors Vertical about its aim axis; upright art never rotates and mirrors
// Horizontal; symmetric art never mirrors. Pure (System.Numerics only): linked into the domain tests.
internal static class DollSpritePlacement
{
    internal const float WorldPerTexel = 2f;
    // A rotation this close to a quarter turn is drawn axis-aligned and snapped to the dot grid.
    internal const float AxisTolerance = 1e-4f;

    // 0-3 quarter turns when the rotation is axis-aligned, otherwise -1.
    internal static int QuarterTurns(float rotation)
    {
        if (!float.IsFinite(rotation)) return -1;
        float turns = rotation / (MathF.PI / 2f);
        float nearest = MathF.Round(turns);
        if (MathF.Abs(turns - nearest) * (MathF.PI / 2f) > AxisTolerance) return -1;
        return (int)(((long)nearest % 4 + 4) % 4);
    }

    // cos/sin of the rotation, exact (0 or +-1) for axis-aligned turns.
    internal static void Basis(float rotation, out float cos, out float sin)
    {
        switch (QuarterTurns(rotation))
        {
            case 0: cos = 1; sin = 0; return;
            case 1: cos = 0; sin = 1; return;
            case 2: cos = -1; sin = 0; return;
            case 3: cos = 0; sin = -1; return;
        }
        cos = MathF.Cos(rotation);
        sin = MathF.Sin(rotation);
    }

    internal static Vector2 Mirror(Vector2 v, DollFlip flip)
        => new((flip & DollFlip.Horizontal) != 0 ? -v.X : v.X, (flip & DollFlip.Vertical) != 0 ? -v.Y : v.Y);

    internal static Vector2 Rotate(Vector2 v, float cos, float sin) => new(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);

    internal static Vector2 World(Vector2 texel, Vector2 pivotTexel, Vector2 pivotWorld, float rotation, DollFlip flip)
    {
        Basis(rotation, out float cos, out float sin);
        return pivotWorld + Rotate(Mirror((texel - pivotTexel) * WorldPerTexel, flip), cos, sin);
    }

    // Inverse of World: the texel coordinate under a world point.
    internal static Vector2 Texel(Vector2 world, Vector2 pivotTexel, Vector2 pivotWorld, float rotation, DollFlip flip)
    {
        Basis(rotation, out float cos, out float sin);
        return pivotTexel + Mirror(Rotate((world - pivotWorld) / WorldPerTexel, cos, -sin), flip);
    }

    // An anchor measured on the unflipped image, expressed on the image mirrored within its own bounds:
    // (w - x, y) for Horizontal, (x, h - y) for Vertical. Placing a flipped sprite about pivot p equals
    // placing the pre-mirrored image about MirrorAnchor(p) without a flip (also SpriteBatch's origin).
    internal static Vector2 MirrorAnchor(Vector2 texel, Vector2 size, DollFlip flip)
        => new((flip & DollFlip.Horizontal) != 0 ? size.X - texel.X : texel.X,
            (flip & DollFlip.Vertical) != 0 ? size.Y - texel.Y : texel.Y);

    // An axis-aligned sprite moves its pivot to the nearest position that puts every texel corner on
    // even world coordinates, so each texel covers exactly one dot of the world-aligned 2 px grid.
    // A rotated sprite keeps its continuous pivot.
    internal static Vector2 Snap(Vector2 pivotWorld, Vector2 pivotTexel, float rotation, DollFlip flip)
    {
        if (QuarterTurns(rotation) < 0 || !Finite(pivotWorld) || !Finite(pivotTexel)) return pivotWorld;
        Basis(rotation, out float cos, out float sin);
        // Texel corner c lands on (pivotWorld - offset) + 2 M c with M integral; snap the constant part.
        Vector2 offset = Rotate(Mirror(pivotTexel * WorldPerTexel, flip), cos, sin);
        Vector2 constant = pivotWorld - offset;
        Vector2 snapped = new(MathF.Floor(constant.X / WorldPerTexel + .5f) * WorldPerTexel,
            MathF.Floor(constant.Y / WorldPerTexel + .5f) * WorldPerTexel);
        return snapped + offset;
    }

    // A placement authored for an upright owner, re-expressed for reversed gravity: Terraria draws the
    // player mirrored about a horizontal axis (axisY, usually the player's centre), so the pivot mirrors
    // about that axis, the rotation negates and the sprite gains a Vertical flip.
    internal static void ApplyGravity(float gravDir, float axisY, ref Vector2 pivotWorld, ref float rotation, ref DollFlip flip)
    {
        if (!(gravDir < 0)) return;
        pivotWorld.Y = 2 * axisY - pivotWorld.Y;
        rotation = -rotation;
        flip ^= DollFlip.Vertical;
    }

    internal static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
}
