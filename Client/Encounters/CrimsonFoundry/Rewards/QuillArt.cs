#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The Bloodink Quill's bodies, in one place (asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md, SR06; names and
// sizes in CrimsonRewardSprites; anchors measured by tools/export_scarlet_reward_art.py, texel centre +0.5):
//   CrimsonBloodinkQuill.png  icon, three quills in an ink bottle   20x32 logical at 2 texels each
//   BloodinkQuill.png         thrown quill lying horizontal, nib RIGHT  25x7 logical; nib tip (25, 4) (the ink bead
//                             Codex drew detached ahead of the nib was cut off by the exporter)
//   SealedScore.png           rolled score, horizontal, crimson cord and a blood-red wax seal  24x11; seal (13.43, 3.57)
// Final art is drawn at CrimsonRewardSprites.PixelScale with point sampling. The vanilla placeholder (Bone Javelin) lies
// diagonally with its tip at the upper right: the tip becomes the nib and the body turns a quarter-pi to lie flat.
internal static class QuillArt
{
    internal readonly record struct Body(Texture2D Texture, Rectangle Source, Vector2 Origin, float Scale, float Turn, bool Pixel)
    {
        internal SamplerState Sampler => Pixel ? SamplerState.PointClamp : SamplerState.LinearClamp;
        // Where `local` (texture px relative to the origin, before the turn) lands for a body drawn at `at`, `rotation`.
        internal Vector2 Point(Vector2 at, float rotation, Vector2 local, Vector2 stretch)
            => at + Vector2.Transform(local * Scale * stretch, Matrix.CreateRotationZ(rotation + Turn));
    }

    private static readonly Vector2 FinalNib = new(25f, 4f), FinalSeal = new(13.43f, 3.57f);
    private static Body? quill, score;

    internal static Body Quill()
    {
        if (quill is { } cached && !cached.Texture.IsDisposed) return cached;
        Body made = Resolve(CrimsonRewardSprites.QuillProjectile, nib: true);
        quill = made;
        return made;
    }

    internal static Body Score()
    {
        if (score is { } cached && !cached.Texture.IsDisposed) return cached;
        Body made = Resolve(CrimsonRewardSprites.SealedScore, nib: false);
        score = made;
        return made;
    }

    // The wax seal on the score, in texture px from its origin (before the turn); the placeholder has none, so its centre.
    internal static Vector2 Seal => Score() is { Pixel: true } art ? FinalSeal - art.Origin : Vector2.Zero;

    private static Body Resolve(in CrimsonRewardSprite sprite, bool nib)
    {
        var art = ScarletRewardArt.Get(sprite);
        if (art.Pixel)
            return new Body(art.Texture, art.Source, nib ? FinalNib : art.Origin, art.Scale, 0, true);
        Rectangle bounds = OpaqueBounds(art.Texture, out Vector2 upperRight);
        Vector2 centre = new(bounds.X + bounds.Width * .5f, bounds.Y + bounds.Height * .5f);
        return new Body(art.Texture, art.Source, nib ? upperRight : centre, art.Scale, MathF.PI / 4, false);
    }

    internal static void Reset() { quill = null; score = null; }

    // Opaque bounds and the upper-right-most opaque texel (the tip of the diagonal placeholder). Read once per texture.
    private static Rectangle OpaqueBounds(Texture2D texture, out Vector2 upperRight)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        int x0 = texture.Width, y0 = texture.Height, x1 = -1, y1 = -1, best = int.MinValue;
        upperRight = new Vector2(texture.Width, 0);
        for (int y = 0; y < texture.Height; y++)
            for (int x = 0; x < texture.Width; x++)
            {
                if (pixels[y * texture.Width + x].A <= 8) continue;
                x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
                if (x - y > best) { best = x - y; upperRight = new Vector2(x + .5f, y + .5f); }
            }
        return x1 >= x0 ? new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1) : texture.Bounds;
    }
}
