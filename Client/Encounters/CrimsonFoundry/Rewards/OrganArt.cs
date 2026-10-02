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
// the palm sit inside them, and CanticleRules holds the pipe mouths. Anchors were measured by
// tools/export_scarlet_reward_art.py (texture pixels from the top-left, texel centre +0.5):
//   OrganHeld   "CanticleOrgan"  43 x 22 logical, facing right; grip (5.86, 16.84), heart-gem (17.71, 7.86),
//                                mouths (43, 3) (42, 6) (42, 10) (42, 13) -> CanticleRules.MouthForward/MouthAcross
//   OrganShard  "CanticleShard"  11 x 6, pointing right
//   BoneHand    "BoneHand"       20 x 32, palm down, fingers down; palm (11.5, 12.43) under the cuff
//   Organ       "CrimsonCanticleOrgan" icon, 27 x 28 logical at 2 texels each
internal static class OrganArt
{
    private static readonly Vector2 FinalGrip = new(5.86f, 16.84f), FinalGem = new(17.71f, 7.86f), FinalPalm = new(11.5f, 12.43f);
    // Stand-in anchors as fractions of the Onyx Blaster's texture: grip under the stock, muzzle at the right edge.
    private static readonly Vector2 PlaceholderGrip = new(.27f, .72f), PlaceholderGem = new(.48f, .42f);
    private const float PlaceholderMouthX = .97f;
    // The held organ is drawn in the player's draw set, which cannot switch to point sampling, so the delivered art is
    // enlarged this many times by nearest neighbour and drawn at PixelScale / Sub (the Moonloom Harp's method): the
    // linear sampler then only blends within a quarter pixel of a texel edge.
    private const int Sub = 4;

    // The held organ. Origin is the grip (turned over with the gun when it is aimed left), in the drawn texture's
    // pixels; Scale is screen px per drawn-texture pixel and puts the mouths CanticleRules.MouthForward px ahead of the
    // grip, so shards leave the drawn mouths. Owned is true for an enlarged copy this class must dispose.
    internal sealed record Held(Texture2D Texture, Rectangle Source, float Scale, bool Pixel, Vector2 Grip, Vector2 Gem, bool Owned = false)
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
    internal readonly record struct Hand(ScarletRewardArt.Sprite Sprite, Vector2 Palm, float Rotation, SpriteEffects Mirror)
    {
        // The draw origin: SpriteBatch keeps the origin in drawn space and mirrors the texture inside the quad, so a
        // mirrored hand needs the palm mirrored too to land on its point.
        internal Vector2 Origin(bool mirrored) => !mirrored ? Palm
            : Mirror == SpriteEffects.FlipHorizontally ? new Vector2(Sprite.Source.Width - Palm.X, Palm.Y)
            : new Vector2(Palm.X, Sprite.Source.Height - Palm.Y);
    }

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
                    held = new Held(Enlarge(sprite.Texture, src), new Rectangle(0, 0, src.Width * Sub, src.Height * Sub),
                        CrimsonRewardSprites.PixelScale / Sub, true, FinalGrip * Sub, FinalGem * Sub, true);
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

    // `source` of the texture enlarged Sub times by nearest neighbour.
    private static Texture2D Enlarge(Texture2D texture, Rectangle source)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        int width = source.Width * Sub, height = source.Height * Sub;
        var big = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                big[y * width + x] = data[(source.Y + y / Sub) * texture.Width + source.X + x / Sub];
        var result = new Texture2D(texture.GraphicsDevice, width, height);
        result.SetData(big);
        return result;
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

    // The enlarged copy is ours; the delivered and vanilla textures are not.
    internal static void Reset()
    {
        Texture2D? owned = held is { Owned: true } h ? h.Texture : null;
        held = null;
        heldFailed = false;
        if (owned is not null) Main.QueueMainThreadAction(() => owned.Dispose());
    }
}
