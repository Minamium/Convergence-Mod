// Offline asset source for the Scarlet preview: IScarletAssets over loose .fxc files,
// loose PNGs and the noise packed in Luminance.tmod. Hidden FNA device only; no Terraria.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed class PreviewAssets : IScarletAssets, IDisposable
{
    private readonly GraphicsDevice device;
    private readonly string root, luminance;
    private readonly Dictionary<string, Effect> effects = new();
    private readonly Dictionary<string, IScarletShader> shaders = new();
    private readonly Dictionary<string, Texture2D> textures = new();

    internal PreviewAssets(GraphicsDevice device, string root, string luminance)
    {
        this.device = device; this.root = root; this.luminance = luminance;
    }

    // The Vfx seam: an Effect wrapped so a Vfx class only sets parameters and applies a pass, like Luminance's ManagedShader.
    public IScarletShader GetShader(string name)
    {
        if (!shaders.TryGetValue(name, out var found)) shaders.Add(name, found = new PreviewShader(GetEffect(name)));
        return found;
    }

    // "PortalBeam" -> Assets/AutoloadedEffects/Shaders/PortalBeam.fxc, loaded straight from the exported bytecode.
    // Preview-only (the old portal stand-in and the backdrop use the raw Effect); Vfx classes go through GetShader.
    public Effect GetEffect(string name)
    {
        if (effects.TryGetValue(name, out var found)) return found;
        string path = ShaderPath(name);
        var effect = new Effect(device, File.ReadAllBytes(path));
        effects.Add(name, effect);
        return effect;
    }

    internal string ShaderPath(string name) => Path.Combine(root, "Assets/AutoloadedEffects/Shaders", name + ".fxc");

    // "Noise/WavyBlotchNoise" and "Luminance/BloomCircleSmall" come from Luminance.tmod (.rawimg, already premultiplied);
    // anything else is Assets/Textures/<name>.png, premultiplied here like tModLoader does on load.
    public Texture2D GetTexture(string name)
    {
        if (textures.TryGetValue(name, out var found)) return found;
        Texture2D texture;
        if (name.StartsWith("Noise/", StringComparison.Ordinal))
            texture = Rawimg("Assets/Noise/" + name["Noise/".Length..] + ".rawimg");
        else if (name.StartsWith("Luminance/", StringComparison.Ordinal))
            texture = Rawimg("Assets/GreyscaleTextures/" + name["Luminance/".Length..] + ".rawimg");
        else
        {
            using var stream = File.OpenRead(Path.Combine(root, "Assets/Textures", name + ".png"));
            texture = Texture2D.FromStream(device, stream);
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.FromNonPremultiplied(pixels[i].ToVector4());
            texture.SetData(pixels);
        }
        textures.Add(name, texture);
        return texture;
    }

    // Same hand-written TMOD reader the Samurai previews use: header, file table, then the packed payloads.
    private Texture2D Rawimg(string entry)
    {
        using var file = File.OpenRead(luminance);
        using var reader = new BinaryReader(file);
        if (System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4)) != "TMOD") throw new InvalidDataException("TMOD magic");
        reader.ReadString(); reader.ReadBytes(276); reader.ReadInt32(); reader.ReadString(); reader.ReadString();
        int count = reader.ReadInt32(), offset = 0, position = -1, length = 0, stored = 0;
        for (int i = 0; i < count; i++)
        {
            string path = reader.ReadString(); int size = reader.ReadInt32(), compressed = reader.ReadInt32();
            if (path == entry) { position = offset; length = size; stored = compressed; }
            offset += compressed;
        }
        if (position < 0) throw new FileNotFoundException("Luminance package has no " + entry);
        file.Position += position;
        using var bytes = new MemoryStream(reader.ReadBytes(stored));
        using Stream data = length == stored ? bytes : new DeflateStream(bytes, CompressionMode.Decompress);
        using var raw = new BinaryReader(data);
        if (raw.ReadInt32() != 1) throw new InvalidDataException("rawimg version");
        int width = raw.ReadInt32(), height = raw.ReadInt32();
        var texture = new Texture2D(device, width, height);
        texture.SetData(raw.ReadBytes(width * height * 4));
        return texture;
    }

    public void Dispose()
    {
        foreach (var effect in effects.Values) effect.Dispose();
        foreach (var texture in textures.Values) texture.Dispose();
        effects.Clear(); textures.Clear();
    }
}

// IScarletShader over a loose Effect: unknown parameters are ignored, exactly like ManagedShader.TrySetParameter.
internal sealed class PreviewShader : IScarletShader
{
    private readonly Effect effect;
    internal PreviewShader(Effect effect) => this.effect = effect;
    public void Set(string name, float value) => effect.Parameters[name]?.SetValue(value);
    public void Set(string name, Vector4 value) => effect.Parameters[name]?.SetValue(value);
    public void Set(string name, Matrix value) => effect.Parameters[name]?.SetValue(value);
    public void Apply(string pass) => effect.CurrentTechnique.Passes[pass].Apply();
}
