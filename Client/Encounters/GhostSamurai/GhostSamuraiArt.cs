#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// Original generated sprite parts. The source uses a magenta color key because
// the image generator returned opaque RGB instead of the requested alpha PNG.
// Key it once on the drawing thread, never once per frame or on a server.
internal sealed class GhostSamuraiArt
{
    internal const string TexturePath = "Convergence/Assets/Textures/GhostSamurai/GhostSamuraiAtlas";
    internal const float Scale = .30f;
    internal static readonly Rectangle Body = new(0, 0, 690, 1254);
    internal static readonly Rectangle SwordArm = new(690, 0, 564, 1254);
    // Partition the existing rig at the blade root: the hand/guard keep their
    // original size while only the blade grows from its attached base.
    internal static readonly Rectangle ArmUpper = new(690, 0, 315, 765);
    internal static readonly Rectangle ArmLower = new(690, 765, 564, 489);
    internal static readonly Rectangle Blade = new(1005, 0, 249, 765);
    internal static readonly Vector2 BladeBase = new(1044 - 690, 765);
    internal static readonly Vector2 BodyPivot = new(366, 550);
    internal static readonly Vector2 ShoulderPivot = new(65, 756);
    private Asset<Texture2D>? source;
    private Texture2D? texture;

    internal Texture2D Texture
    {
        get
        {
            if (texture is not null) return texture;
            source ??= ModContent.Request<Texture2D>(TexturePath, AssetRequestMode.ImmediateLoad);
            Texture2D input = source.Value;
            var pixels = new Color[input.Width * input.Height];
            input.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                // Blue/ivory/navy art has no magenta. Tolerance removes keyed edge
                // pixels too; genuine incoming alpha on the art is preserved.
                if (c.R > c.G + 48 && c.B > c.G + 48) pixels[i] = Color.Transparent;
            }
            texture = new Texture2D(Main.instance.GraphicsDevice, input.Width, input.Height);
            texture.SetData(pixels);
            return texture;
        }
    }

    internal void Unload()
    {
        var oldTexture = texture;
        texture = null;
        source = null;
        // The keyed copy is ours; the asset repository owns source. Unload may
        // run on a worker, and a queued release must not touch a new reload.
        if (oldTexture is not null) Main.QueueMainThreadAction(oldTexture.Dispose);
    }
}
