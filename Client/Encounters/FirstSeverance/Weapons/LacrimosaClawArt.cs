#nullable enable
using System;
using System.Numerics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// One exported claw sprite: its texture name under Assets/Textures/Items/DollWeapons/, size, cuff pivot, talon
// tips and the six bead centres (texel space from DollArtAnchors.g.cs) and its export rung K.
internal readonly record struct LacrimosaSpriteArt(string Name, int Width, int Height, Vector2 Pivot, Vector2[] Tips, Vector2[] Beads, int K);

// How the DW01 hand art is laid on LacrimosaClawMotion's frames. Pure (System.Numerics, the generated anchors and
// DollSpritePlacement): linked into the domain tests, whose art-fit check keeps each hit-shape anchor within one
// source dot (2 world px x K) of its design anchor.
//  - The sprite's pivot is the cuff, placed on the frame's wrist; one texel is one dot (2 world px), never scaled.
//  - Rake and Thrust hands are turned by a constant offset so the talon tip that defines the capsule lies on the
//    hand axis (Rake: the longest talon; Thrust: the middle of the three front tips). Open and Clench point +X.
//  - The left hand is the right-hand art mirrored about its own axis (DollFlip.Vertical); a left-facing or
//    reversed-gravity owner mirrors both again.
internal static class LacrimosaClawArt
{
    internal static readonly LacrimosaSpriteArt OpenSmall = Make("ClawOpen", DollArtAnchors.ClawOpen.Width, DollArtAnchors.ClawOpen.Height,
        DollArtAnchors.ClawOpen.Pivot, DollArtAnchors.ClawOpen.Tips, DollArtAnchors.ClawOpen.Beads, DollArtAnchors.ClawOpen.K);
    internal static readonly LacrimosaSpriteArt OpenLarge = Make("ClawOpen_L", DollArtAnchors.ClawOpen_L.Width, DollArtAnchors.ClawOpen_L.Height,
        DollArtAnchors.ClawOpen_L.Pivot, DollArtAnchors.ClawOpen_L.Tips, DollArtAnchors.ClawOpen_L.Beads, DollArtAnchors.ClawOpen_L.K);
    internal static readonly LacrimosaSpriteArt RakeSmall = Make("ClawRake", DollArtAnchors.ClawRake.Width, DollArtAnchors.ClawRake.Height,
        DollArtAnchors.ClawRake.Pivot, DollArtAnchors.ClawRake.Tips, DollArtAnchors.ClawRake.Beads, DollArtAnchors.ClawRake.K);
    internal static readonly LacrimosaSpriteArt RakeLarge = Make("ClawRake_L", DollArtAnchors.ClawRake_L.Width, DollArtAnchors.ClawRake_L.Height,
        DollArtAnchors.ClawRake_L.Pivot, DollArtAnchors.ClawRake_L.Tips, DollArtAnchors.ClawRake_L.Beads, DollArtAnchors.ClawRake_L.K);
    internal static readonly LacrimosaSpriteArt ClenchSmall = Make("ClawClench", DollArtAnchors.ClawClench.Width, DollArtAnchors.ClawClench.Height,
        DollArtAnchors.ClawClench.Pivot, DollArtAnchors.ClawClench.Tips, DollArtAnchors.ClawClench.Beads, DollArtAnchors.ClawClench.K);
    internal static readonly LacrimosaSpriteArt ClenchLarge = Make("ClawClench_L", DollArtAnchors.ClawClench_L.Width, DollArtAnchors.ClawClench_L.Height,
        DollArtAnchors.ClawClench_L.Pivot, DollArtAnchors.ClawClench_L.Tips, DollArtAnchors.ClawClench_L.Beads, DollArtAnchors.ClawClench_L.K);
    internal static readonly LacrimosaSpriteArt ThrustSmall = Make("ClawThrust", DollArtAnchors.ClawThrust.Width, DollArtAnchors.ClawThrust.Height,
        DollArtAnchors.ClawThrust.Pivot, DollArtAnchors.ClawThrust.Tips, DollArtAnchors.ClawThrust.Beads, DollArtAnchors.ClawThrust.K);
    internal static readonly LacrimosaSpriteArt ThrustLarge = Make("ClawThrust_L", DollArtAnchors.ClawThrust_L.Width, DollArtAnchors.ClawThrust_L.Height,
        DollArtAnchors.ClawThrust_L.Pivot, DollArtAnchors.ClawThrust_L.Tips, DollArtAnchors.ClawThrust_L.Beads, DollArtAnchors.ClawThrust_L.K);

    // The inventory icon (stored at 2x).
    internal const string IconTexture = DollArtAnchors.NullRefrainIcon.Texture;

    internal static readonly LacrimosaSpriteArt[] All =
    {
        OpenSmall, OpenLarge, RakeSmall, RakeLarge, ClenchSmall, ClenchLarge, ThrustSmall, ThrustLarge,
    };

    private static LacrimosaSpriteArt Make(string name, int width, int height, Vector2 pivot, Vector2[] tips, Vector2[] beads, int k)
        => new(name, width, height, pivot, tips, beads, k);

    internal static LacrimosaSpriteArt Sprite(LacrimosaPose pose, bool large) => pose switch
    {
        LacrimosaPose.Rake => large ? RakeLarge : RakeSmall,
        LacrimosaPose.Clench => large ? ClenchLarge : ClenchSmall,
        LacrimosaPose.Thrust => large ? ThrustLarge : ThrustSmall,
        _ => large ? OpenLarge : OpenSmall,
    };

    // The tip laid on the hand axis, or -1 (the sprite's +X is the axis).
    internal static int AxisTip(LacrimosaPose pose, in LacrimosaSpriteArt art)
    {
        if (art.Tips.Length == 0) return -1;
        if (pose == LacrimosaPose.Rake)
        {
            int longest = 0;
            for (int i = 1; i < art.Tips.Length; i++)
                if (Vector2.DistanceSquared(art.Tips[i], art.Pivot) > Vector2.DistanceSquared(art.Tips[longest], art.Pivot)) longest = i;
            return longest;
        }
        if (pose == LacrimosaPose.Thrust)
        {
            // Of the tips at the front (within 3 texels of the farthest x), the middle one by angle.
            float front = float.MinValue;
            foreach (Vector2 tip in art.Tips) front = MathF.Max(front, tip.X);
            int first = -1, last = -1;
            for (int i = 0; i < art.Tips.Length; i++)
            {
                if (art.Tips[i].X < front - 3) continue;
                if (first < 0) first = i;
                last = i;
            }
            return first < 0 ? -1 : (first + last) / 2;
        }
        return -1;
    }

    // World px from the cuff to the axis tip (or to the front edge for Open/Clench) along the hand axis.
    internal static float Reach(LacrimosaPose pose, in LacrimosaSpriteArt art)
    {
        int tip = AxisTip(pose, art);
        if (tip >= 0) return Vector2.Distance(art.Tips[tip], art.Pivot) * DollSpritePlacement.WorldPerTexel;
        float front = 0;
        foreach (Vector2 t in art.Tips) front = MathF.Max(front, t.X - art.Pivot.X);
        return front * DollSpritePlacement.WorldPerTexel;
    }

    internal static DollFlip Flip(bool left, float mirror) => left != mirror < 0 ? DollFlip.Vertical : DollFlip.None;

    // Sprite rotation for a hand whose axis points at `axis` (world radians).
    internal static float Rotation(LacrimosaPose pose, in LacrimosaSpriteArt art, float axis, DollFlip flip)
    {
        int tip = AxisTip(pose, art);
        if (tip < 0) return axis;
        Vector2 local = DollSpritePlacement.Mirror(art.Tips[tip] - art.Pivot, flip);
        return axis - MathF.Atan2(local.Y, local.X);
    }

    // A texel anchor of the sprite drawn with its cuff on `wrist` (world) and its axis at `axis` (world radians).
    internal static Vector2 Anchor(Vector2 texel, LacrimosaPose pose, in LacrimosaSpriteArt art, Vector2 wrist, float axis, DollFlip flip)
        => DollSpritePlacement.World(texel, art.Pivot, wrist, Rotation(pose, art, axis, flip), flip);

    // The design anchor of a hit-shape pose (the capsule tip on the axis), or null for poses without one.
    internal static float? DesignReach(LacrimosaPose pose, bool large) => !large ? null : pose switch
    {
        LacrimosaPose.Rake => LacrimosaClawMotion.RakeTip,
        LacrimosaPose.Thrust => LacrimosaClawMotion.ClapTip,
        LacrimosaPose.Clench => LacrimosaClawMotion.FistReach,
        _ => null,
    };
}
