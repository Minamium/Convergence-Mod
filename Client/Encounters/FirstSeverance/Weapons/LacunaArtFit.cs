#nullable enable
using System.Numerics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Which exported Lacuna Testament sprites the presentation draws, at which pivots, and where their anchors land
// against the design anchors of LacunaTestamentScore (Content owns hit shapes; the art is fitted to them). One texel
// is one dot (2 world px); no sprite is scaled. Pure (System.Numerics, the generated anchors and the placement):
// linked into the domain tests, whose art-fit test keeps every drawn anchor within one dot (2 world px, whatever the
// export rung) of its design, and whose head test keeps the book off the owner's head at every aim.
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

    // Tolerance of the art-fit test: one dot (one texel at 2 world px; every rung draws at that dot).
    internal static float Tolerance => DollSpritePlacement.WorldPerTexel;

    // The owner's head in world px from the draw centre, for an upright owner (mirrored under reversed gravity): wide
    // enough for a real player's head (frame rows about 6-24, -25..-7 px, hair a little higher) and the offline
    // stand-in's (-37..-21). Shared rule: the weapon's art never hides the character's head.
    internal static readonly Vector2 HeadMin = new(-10, -37), HeadMax = new(10, -7);
    // Kept between the book and the head box beyond the book's half size: its one-dot bob plus a dot of grid snap.
    internal const float HeadMargin = 4;
    internal static Vector2 BookHalf => new(DollArtAnchors.LacunaBook_S.Width * DollSpritePlacement.WorldPerTexel * .5f,
        DollArtAnchors.LacunaBook_S.Height * DollSpritePlacement.WorldPerTexel * .5f);

    // The book's centre pushed out along `axis` (away from the hand) just far enough that the upright book clears the
    // head box, or unchanged when it already does. Continuous in the aim and the hand, so the book glides rather than
    // jumps as the aim sweeps over the head; only a steep upward aim moves it at all.
    internal static Vector2 ClearHead(Vector2 book, Vector2 axis, Vector2 centre, float gravDir)
    {
        Vector2 half = BookHalf + new Vector2(HeadMargin);
        bool upright = !(gravDir < 0);
        float minY = upright ? HeadMin.Y : -HeadMax.Y, maxY = upright ? HeadMax.Y : -HeadMin.Y;
        Vector2 low = centre + new Vector2(HeadMin.X, minY) - half, high = centre + new Vector2(HeadMax.X, maxY) + half;
        if (!(book.X > low.X && book.X < high.X && book.Y > low.Y && book.Y < high.Y)) return book;
        float exit = float.MaxValue;
        if (axis.X > 1e-4f) exit = System.MathF.Min(exit, (high.X - book.X) / axis.X);
        else if (axis.X < -1e-4f) exit = System.MathF.Min(exit, (low.X - book.X) / axis.X);
        if (axis.Y > 1e-4f) exit = System.MathF.Min(exit, (high.Y - book.Y) / axis.Y);
        else if (axis.Y < -1e-4f) exit = System.MathF.Min(exit, (low.Y - book.Y) / axis.Y);
        return exit < float.MaxValue ? book + axis * exit : book;
    }

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
