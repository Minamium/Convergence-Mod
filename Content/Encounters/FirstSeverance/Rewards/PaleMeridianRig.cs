#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Where the held Pale Meridian is, in the gun frame (px from the owner's rotated centre; +X along the aim, +Y toward
// the gun's top side). These are the design anchors: rounds and the release leave from Muzzle, and the exported
// pixel art (MeridianGun / MeridianBare at k = 3, 87x17 texels = 174x34 px) is fitted to them, never the reverse.
// The 150 px muzzle puts the art's grip at the front hand, 6 px ahead of and 12.5 px below the centre, and its
// stock 24 px behind it. Pure (System.Numerics only): linked into the domain tests and the offline preview.
internal static class PaleMeridianRig
{
    internal const float MuzzleReach = 150;
    internal static readonly Vector2 Muzzle = new(MuzzleReach, 0);
    internal static readonly Vector2 Grip = new(6, -12.5f);
    internal static readonly Vector2 KeySeat = new(-1, 10);
    // The four exported brass parts' sizes in texels inside their 47x6 cells (MeridianParts at k = 3: cylinder, barrel
    // shroud, ring sight, spring housing), for their centres in flight.
    internal static readonly Vector2[] PartSize = { new(5, 5), new(47, 2), new(4, 6), new(3, 6) };

    // Gun frame -> world for an aim direction (unit) and a facing: the gun's top side is toward -Y of the world when
    // aiming right and is mirrored about the aim axis when aiming left (the gun is never drawn upside down).
    internal static Vector2 World(Vector2 pivot, Vector2 axis, bool mirrored, Vector2 local)
    {
        Vector2 top = mirrored ? new Vector2(-axis.Y, axis.X) : new Vector2(axis.Y, -axis.X);
        return pivot + axis * local.X + top * local.Y;
    }

    // The gun is mirrored when it points into the half plane behind its owner's upright side.
    internal static bool Mirrored(Vector2 axis, float gravity) => axis.X < 0 != gravity < 0;

    internal static Vector2 MuzzleAt(Vector2 pivot, Vector2 axis) => pivot + axis * MuzzleReach;

    internal static Vector2 Aim(Vector2 direction, int facing)
        => direction.LengthSquared() > 1e-6f && float.IsFinite(direction.X) && float.IsFinite(direction.Y)
            ? Vector2.Normalize(direction) : new Vector2(facing < 0 ? -1 : 1, 0);
}
