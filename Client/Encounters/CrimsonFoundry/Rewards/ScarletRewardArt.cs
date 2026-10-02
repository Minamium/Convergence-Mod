#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// Client textures for the Scarlet rewards, resolved through the one table in CrimsonRewardSprites: the delivered
// pixel art (TexelScale texels per logical pixel, drawn so one logical pixel is PixelScale px, with point sampling) or
// the vanilla placeholder (painted, drawn with linear sampling, scaled to about the planned on-screen size). Every
// texture is requested with ImmediateLoad before it is cached: a cached AsyncLoad .Value is an empty placeholder until
// the load finishes, and that once left every Ebon reward sprite invisible. Client only; Reset() on Mod unload.
internal static class ScarletRewardArt
{
    internal readonly record struct Sprite(Texture2D Texture, Rectangle Source, float Scale, bool Pixel)
    {
        internal Vector2 Origin => new(Source.Width * .5f, Source.Height * .5f);
        internal Vector2 Size => new(Source.Width * Scale, Source.Height * Scale);
        internal SamplerState Sampler => Pixel ? SamplerState.PointClamp : SamplerState.LinearClamp;
    }

    private static readonly Dictionary<string, Sprite> sprites = new();

    // The final art at PixelScale, or the placeholder scaled so its larger side matches the planned size at 2x.
    internal static Sprite Get(in CrimsonRewardSprite sprite)
    {
        if (sprites.TryGetValue(sprite.Name, out var found) && !found.Texture.IsDisposed) return found;
        bool final = sprite.HasFinal;
        var texture = ModContent.Request<Texture2D>(final ? sprite.FinalPath : sprite.PlaceholderTexture, AssetRequestMode.ImmediateLoad).Value;
        float scale = final ? CrimsonRewardSprites.PixelScale / sprite.TexelScale
            : Math.Clamp(Math.Max(sprite.Width, sprite.Height) * CrimsonRewardSprites.PixelScale / Math.Max(1f, Math.Max(texture.Width, texture.Height)), .5f, 3f);
        return sprites[sprite.Name] = new Sprite(texture, texture.Bounds, scale, final);
    }

    internal static void Reset() => sprites.Clear();
}
