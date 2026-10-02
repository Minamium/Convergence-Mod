#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The Canticle Organ's sprites and anchors (REWARDS.md, "Presentation"; SR03 in
// asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md). Textures resolve through CrimsonRewardSprites (the delivered
// art or the vanilla Onyx Blaster stand-in); this file is the one place that knows where the grip, the heart-gem and
// the pipe mouths sit inside them. When SR03 is exported, replace the Final* anchors with the exporter's measured
// values (texture pixels from the top-left, texel centre +0.5); nothing else changes.
//   OrganHeld   "CanticleOrgan"  planned 40 x 18 logical (never more than 44 x 22), facing right; grip, four pipe mouths
//   OrganShard  "CanticleShard"  planned 10 x 4 (12 x 6), pointing right
//   BoneHand    "BoneHand"       planned 20 x 28 (24 x 32), palm down, fingers down; palm anchor
//   Organ       "CrimsonCanticleOrgan" icon, 28 x 28
internal static class OrganArt
{
    // Planned SR03 anchors (not yet measured): the grip under the rear, the heart-gem in the frame, the mouths' column.
    private static readonly Vector2 FinalGrip = new(11.5f, 13.5f), FinalGem = new(21.5f, 8.5f), FinalPalm = new(10f, 17f);
    private const float FinalMouthX = 39.5f;
    // Stand-in anchors as fractions of the Onyx Blaster's texture: grip under the stock, muzzle at the right edge.
    private static readonly Vector2 PlaceholderGrip = new(.27f, .72f), PlaceholderGem = new(.48f, .42f);
    private const float PlaceholderMouthX = .97f;

    // The held organ. Origin is the grip (turned over with the gun when it is aimed left); Scale puts the mouths
    // CanticleRules.MouthForward px ahead of the grip, so shards leave the drawn mouths.
    internal sealed record Held(Texture2D Texture, Rectangle Source, float Scale, bool Pixel, Vector2 Grip, Vector2 Gem)
    {
        internal Vector2 Origin(bool flip) => new(Grip.X, flip ? Source.Height - Grip.Y : Grip.Y);
        // World offset of the heart-gem from the grip at this aim.
        internal Vector2 GemOffset(float aim, bool flip)
        {
            Vector2 d = (Gem - Grip) * Scale;
            if (flip) d.Y = -d.Y;
            return d.RotatedBy(aim);
        }
    }

    // A falling hand: Rotation turns the stand-in (a gun, muzzle right) so it points down like the hand's fingers, and
    // Mirror is the flip that mirrors it left-right on screen after that turn.
    internal readonly record struct Hand(ScarletRewardArt.Sprite Sprite, Vector2 Palm, float Rotation, SpriteEffects Mirror);

    private static Held? held;
    private static bool heldFailed;

    internal static Held? Gun
    {
        get
        {
            if (held is not null || heldFailed) return held;
            try
            {
                var sprite = ScarletRewardArt.Get(CrimsonRewardSprites.OrganHeld);
                Rectangle src = sprite.Source;
                if (sprite.Pixel)
                    held = new Held(sprite.Texture, src, CrimsonRewardSprites.PixelScale, true, FinalGrip, FinalGem);
                else
                {
                    Vector2 size = new(src.Width, src.Height);
                    Vector2 grip = PlaceholderGrip * size;
                    float reach = Math.Max(1f, (PlaceholderMouthX - PlaceholderGrip.X) * src.Width);
                    held = new Held(sprite.Texture, src, CanticleRules.MouthForward / reach, false, grip, PlaceholderGem * size);
                }
            }
            catch (Exception e)
            {
                heldFailed = true;
                global::Convergence.ConvergenceMod.Instance.Logger.Warn("Canticle Organ art unavailable; the organ is not drawn.", e);
            }
            return held;
        }
    }

    internal static ScarletRewardArt.Sprite Shard => ScarletRewardArt.Get(CrimsonRewardSprites.OrganShard);

    internal static Hand BoneHand
    {
        get
        {
            var sprite = ScarletRewardArt.Get(CrimsonRewardSprites.BoneHand);
            return sprite.Pixel ? new Hand(sprite, FinalPalm, 0, SpriteEffects.FlipHorizontally)
                : new Hand(sprite, sprite.Origin, MathHelper.PiOver2, SpriteEffects.FlipVertically);
        }
    }

    // Draws one body inside the projectile layer's SpriteBatch. Delivered pixel art needs point sampling, so the batch
    // is restarted around it (and put back exactly as Main.DrawProjectiles began it); the painted stand-ins draw as is.
    internal static void Draw(in ScarletRewardArt.Sprite sprite, Vector2 world, float rotation, Vector2 origin, Vector2 scale, Color color, SpriteEffects effects)
    {
        var batch = Main.spriteBatch;
        if (!sprite.Pixel)
        {
            batch.Draw(sprite.Texture, world - Main.screenPosition, sprite.Source, color, rotation, origin, scale, effects, 0);
            return;
        }
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        batch.Draw(sprite.Texture, world - Main.screenPosition, sprite.Source, color, rotation, origin, scale, effects, 0);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }

    // Lit like the world, but never quite lost in the dark: the bone reads against black blood.
    internal static Color Lit(Color light, float floor = .42f)
    {
        Vector3 c = light.ToVector3();
        return new Color(Math.Max(c.X, floor), Math.Max(c.Y, floor * .95f), Math.Max(c.Z, floor * .9f));
    }

    internal static void Reset()
    {
        held = null;
        heldFailed = false;
    }
}
