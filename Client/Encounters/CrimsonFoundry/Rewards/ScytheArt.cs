#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using NVector = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The held Sable Scythe body (REWARDS.md, "Melee - Sable Scythe", Presentation): the held sprite drawn about its grip
// anchor so that its grip -> hook-tip line lies on the motion's tip frame, with the swing plane's roll projected (the
// sprite squashes edge-on while the hook rolls over the wrist). The same SablePose drives the blade capsules, so the
// drawn hook is the hook that collides. The texture comes from CrimsonRewardSprites.ScytheHeld: the delivered SR02
// art (point sampled at the 2 px dot) or the placeholder Death Sickle (linear). Client only.
internal static class ScytheArt
{
    // Anchors in texel space (origin top-left, texel centre +0.5). SableScythe.png (SR02_c, 64 x 56 logical, haft from
    // the lower left to the blade at the upper right) as tools/export_scarlet_reward_art.py measured it: the grip on the
    // haft line 1.5 texels past the gold butt cap, the hook tip at the end of the downswept blade. Grip to tip is 58.91
    // texels, so CrimsonRewardRules.ScytheReach (117.82 px) draws it at exactly the 2 px dot. The placeholder (Death
    // Sickle, 70 x 64) was measured from its alpha the same way.
    private static readonly Vector2 FinalGrip = new(5.8f, 50.67f), FinalTip = new(61.5f, 31.5f);
    private static readonly Vector2 PlaceholderGrip = new(7f, 57f), PlaceholderTip = new(55f, 53.5f);
    private static readonly Vector2 PlaceholderSize = new(70, 64);

    internal readonly record struct Anchors(Vector2 Grip, Vector2 Tip);

    internal static Anchors AnchorsFor(in ScarletRewardArt.Sprite sprite)
    {
        if (sprite.Pixel) return new Anchors(FinalGrip, FinalTip);
        var scale = new Vector2(sprite.Source.Width / PlaceholderSize.X, sprite.Source.Height / PlaceholderSize.Y);
        return new Anchors(PlaceholderGrip * scale, PlaceholderTip * scale);
    }

    // Texel -> world as one affine map: rotate the grip -> tip line onto the tip frame, scale it to the reach, spin it,
    // project the rolled plane, add the hand, then rotate/mirror into the world along the aim.
    internal static Matrix Transform(in Anchors anchors, Vector2 shoulder, in SablePose pose, float aim, int facing, Vector2 screen)
    {
        Vector2 span = anchors.Tip - anchors.Grip;
        float length = Math.Max(1, span.Length()), tilt = MathF.Atan2(span.Y, span.X);
        // Tip frame from texels: rotate by -tilt and divide by the texel reach; then spin and roll.
        float c0 = MathF.Cos(-tilt) / length, s0 = MathF.Sin(-tilt) / length;
        float cs = MathF.Cos(pose.Spin), ss = MathF.Sin(pose.Spin), roll = MathF.Cos(pose.Roll), reach = SableScytheMotion.Reach;
        // Local (aim frame) = reach * P(roll) * R(spin) * R(-tilt)/length * texel
        float a00 = reach * (cs * c0 - ss * s0), a01 = reach * (cs * -s0 - ss * c0);
        float a10 = reach * roll * (ss * c0 + cs * s0), a11 = reach * roll * (ss * -s0 + cs * c0);
        if (facing < 0) { a10 = -a10; a11 = -a11; }
        float ca = MathF.Cos(aim), sa = MathF.Sin(aim);
        float w00 = ca * a00 - sa * a10, w01 = ca * a01 - sa * a11, w10 = sa * a00 + ca * a10, w11 = sa * a01 + ca * a11;
        NVector grip = SableScytheMotion.ToWorld(pose.Hand, aim, facing);
        Vector2 origin = shoulder + new Vector2(grip.X, grip.Y) - screen;
        // Row-vector convention: (x, y) -> (x * M11 + y * M21 + M41, x * M12 + y * M22 + M42).
        return new Matrix(w00, w10, 0, 0, w01, w11, 0, 0, 0, 0, 1, 0, origin.X, origin.Y, 0, 1);
    }

    // Draw the scythe at a pose. The caller's batch (any state) is captured, ended, restarted with the scythe's own
    // affine map composed onto it, and restored.
    internal static void Draw(SpriteBatch batch, Vector2 shoulder, in SablePose pose, float aim, int facing, Color color)
    {
        if (color.A == 0) return;
        var sprite = ScarletRewardArt.Get(CrimsonRewardSprites.ScytheHeld);
        var anchors = AnchorsFor(sprite);
        var saved = WorldBatchParameters.Capture(batch);
        batch.End();
        try
        {
            Matrix local = Transform(anchors, shoulder, pose, aim, facing, Main.screenPosition);
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, sprite.Sampler, DepthStencilState.None, RasterizerState.CullNone,
                null, local * saved.Transform);
            batch.Draw(sprite.Texture, Vector2.Zero, sprite.Source, color, 0, anchors.Grip, 1, SpriteEffects.None, 0);
            batch.End();
        }
        finally { saved.Restore(batch); }
    }
}
