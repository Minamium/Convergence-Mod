#nullable enable
using System.Numerics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Which exported Lacuna Testament sprites the presentation draws, at which pivots, and where their anchors land
// against the design anchors of LacunaTestamentScore (Content owns hit shapes; the art is fitted to them). One texel
// is one dot (2 world px); no sprite is scaled. Pure (System.Numerics, the generated anchors and the placement):
// linked into the domain tests, whose art-fit test keeps every drawn anchor within one dot (2 px x k) of its design.
internal static class LacunaArtFit
{
    // The held book is the k=2 rung (21x28 dots, 42x56 px), about the player's own height. The k=1 rung (42x55
    // dots, 84x110 px) floating 24 px past the hand would cover the whole front half of a 20x42 player at every aim
    // (offline composites, docs/encounters/first-severance/WEAPONS.md); the small rung clears the body.
    internal const string Book = "LacunaBook_S", LargeBook = "LacunaBook", Iris = "LacunaIris", Great = "LacunaGreatIris";
    internal const string Icon = "LacunaTestamentIcon";
    internal const int BookK = DollArtAnchors.LacunaBook_S.K, IrisK = DollArtAnchors.LacunaIris.K, GreatK = DollArtAnchors.LacunaGreatIris.K;

    // Pivots in texels: the book's centre (it floats upright, mirrored when the owner faces left), each iris frame's
    // centre, the great iris's centre (it turns only in ratchet steps about its hole).
    internal static readonly Vector2 BookPivot = new(DollArtAnchors.LacunaBook_S.Width / 2f, DollArtAnchors.LacunaBook_S.Height / 2f);
    internal static readonly Vector2 LargeBookPivot = new(DollArtAnchors.LacunaBook.Width / 2f, DollArtAnchors.LacunaBook.Height / 2f);
    internal static readonly Vector2 IrisPivot = new(DollArtAnchors.LacunaIris.FrameWidth / 2f, DollArtAnchors.LacunaIris.FrameHeight / 2f);
    internal static readonly Vector2 GreatPivot = new(DollArtAnchors.LacunaGreatIris.Width / 2f, DollArtAnchors.LacunaGreatIris.Height / 2f);

    // Hole radii in world px (texel radius x 2): the void mouths are fitted to the art's measured holes.
    internal static float BookHoleRadius => DollArtAnchors.LacunaBook_S.HoleRadius * DollSpritePlacement.WorldPerTexel;
    internal static float IrisApertureRadius(int frame)
        => DollArtAnchors.LacunaIris.ApertureRadii[System.Math.Clamp(frame, 0, 3)] * DollSpritePlacement.WorldPerTexel;
    internal static float GreatHoleRadius => DollArtAnchors.LacunaGreatIris.HoleRadius * DollSpritePlacement.WorldPerTexel;
    internal static float GreatRingRadius => DollArtAnchors.LacunaGreatIris.RingRadius * DollSpritePlacement.WorldPerTexel;
    internal static float GreatOuterRadius => DollArtAnchors.LacunaGreatIris.OuterRadius * DollSpritePlacement.WorldPerTexel;

    // Tolerance of the art-fit test: one dot of the export rung.
    internal static float Tolerance(int k) => DollSpritePlacement.WorldPerTexel * k;

    // Where the book's hole is drawn when its pivot is placed at `pivotWorld` (axis-aligned, so it snaps).
    internal static Vector2 BookHoleAt(Vector2 pivotWorld, DollFlip flip)
        => Drawn(DollArtAnchors.LacunaBook_S.Hole, BookPivot, pivotWorld, 0, flip);

    // Where an iris frame's centre (its hole) is drawn when its pivot is placed at `pivotWorld`.
    internal static Vector2 IrisHoleAt(Vector2 pivotWorld) => Drawn(DollArtAnchors.LacunaIris.Centre, IrisPivot, pivotWorld, 0, DollFlip.None);

    // Where the great iris's hole is drawn when its pivot is placed at `pivotWorld`, turned by `rotation`.
    internal static Vector2 GreatHoleAt(Vector2 pivotWorld, float rotation)
        => Drawn(DollArtAnchors.LacunaGreatIris.Hole, GreatPivot, pivotWorld, rotation, DollFlip.None);

    // The great iris's ratchet rotation for a score age.
    internal static float GreatRotation(float age) => LacunaTestamentScore.RatchetSteps(age) * LacunaTestamentScore.RatchetStep;

    private static Vector2 Drawn(Vector2 anchor, Vector2 pivotTexel, Vector2 pivotWorld, float rotation, DollFlip flip)
        => DollSpritePlacement.World(anchor, pivotTexel, DollSpritePlacement.Snap(pivotWorld, pivotTexel, rotation, flip), rotation, flip);
}
