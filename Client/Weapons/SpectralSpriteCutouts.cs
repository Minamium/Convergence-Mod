using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

// The generator can export an opaque matte. New violet sprites use pure green
// (absent from their palette); process it once, on the drawing thread only.
[Autoload(Side = ModSide.Client)]
internal sealed class SpectralSpriteCutouts : ModSystem
{
    private static readonly Dictionary<string, Texture2D> textures = new();
    internal static Texture2D Get(string path)
    {
        if (textures.TryGetValue(path, out var result)) return result;
        var input = ModContent.Request<Texture2D>(path).Value;
        var pixels = new Color[input.Width * input.Height]; input.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            if (c.A < 8 || c.G > c.R + 45 && c.G > c.B + 45) pixels[i] = Color.Transparent;
        }
        result = new Texture2D(Main.graphics.GraphicsDevice, input.Width, input.Height); result.SetData(pixels);
        textures.Add(path, result); return result;
    }
    public override void Unload()
    {
        var retired = new List<Texture2D>(textures.Values); textures.Clear();
        if (retired.Count > 0) Main.QueueMainThreadAction(() => { foreach (var texture in retired) texture.Dispose(); });
    }
}
