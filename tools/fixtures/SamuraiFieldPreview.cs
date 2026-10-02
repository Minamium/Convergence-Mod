// Hidden FNA D3D11 preview linked to the production SamuraiFieldRenderer and the
// exported SamuraiBattlefield.fxc. Terraria and Luminance are shims; the world is
// a stand-in (daylit sky, dirt/grass tiles, a player, a boss and a violet forecast)
// so the backdrop, seal, abyss and readability can be judged without the game.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Convergence.Client.Encounters.GhostSamurai;
using Convergence.Content.Encounters.GhostSamurai;

internal static class SamuraiFieldPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern void SDL_Quit();
    internal static string Root, LuminancePackage;
    internal static GraphicsDevice Device;
    static readonly Dictionary<string, Texture2D> noise = new();
    static Texture2D pixel;
    static readonly SamuraiArenaBounds Field = new(20000, 10000 - SamuraiArenaBounds.Height / 2, SamuraiArenaBounds.Width / 2, SamuraiArenaBounds.Height / 2);

    internal static Texture2D Noise(string name)
    {
        if (noise.TryGetValue(name, out var found)) return found;
        using var file = File.OpenRead(LuminancePackage);
        using var reader = new BinaryReader(file);
        if (System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4)) != "TMOD") throw new InvalidDataException("TMOD magic");
        reader.ReadString(); reader.ReadBytes(276); reader.ReadInt32(); reader.ReadString(); reader.ReadString();
        int count = reader.ReadInt32(), offset = 0, position = -1, length = 0, stored = 0;
        for (int i = 0; i < count; i++)
        {
            string path = reader.ReadString(); int size = reader.ReadInt32(), compressed = reader.ReadInt32();
            if (path == "Assets/Noise/" + name + ".rawimg") { position = offset; length = size; stored = compressed; }
            offset += compressed;
        }
        if (position < 0) throw new FileNotFoundException("Luminance noise: " + name);
        file.Position += position;
        using var bytes = new MemoryStream(reader.ReadBytes(stored));
        using Stream data = length == stored ? bytes : new DeflateStream(bytes, CompressionMode.Decompress);
        using var raw = new BinaryReader(data);
        if (raw.ReadInt32() != 1) throw new InvalidDataException("rawimg version");
        int width = raw.ReadInt32(), height = raw.ReadInt32();
        var texture = new Texture2D(Device, width, height); texture.SetData(raw.ReadBytes(width * height * 4));
        noise.Add(name, texture); return texture;
    }

    record Shot(string Name, int Width, int Height, Vector2 View, float Zoom, SamuraiFieldLook Look, bool Seal = true, bool Actors = true);

    static void Main(string[] args)
    {
        Root = args[0]; LuminancePackage = args[1]; string native = args[2], output = args[3];
        IntPtr Resolve(string name, System.Reflection.Assembly a, DllImportSearchPath? p)
        { string file = Path.Combine(native, name.EndsWith(".dll") ? name : name + ".dll"); return File.Exists(file) ? NativeLibrary.Load(file) : IntPtr.Zero; }
        NativeLibrary.SetDllImportResolver(typeof(SamuraiFieldPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL video initialization failed");
        IntPtr window = SDL_CreateWindow("Samurai field preview", 0, 0, 640, 360, FNA3D_PrepareWindowAttributes() | 0x8);
        if (window == IntPtr.Zero) throw new Exception("Hidden device surface unavailable");
        try
        {
            using var device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, new PresentationParameters
            {
                DeviceWindowHandle = window, BackBufferWidth = 640, BackBufferHeight = 360, BackBufferFormat = SurfaceFormat.Color,
                IsFullScreen = false, DepthStencilFormat = DepthFormat.None, PresentationInterval = PresentInterval.Immediate
            });
            Device = device;
            pixel = new Texture2D(device, 1, 1); pixel.SetData(new[] { Color.White });
            Directory.CreateDirectory(output);
            float floorView = Field.Bottom - 21;           // the camera follows a grounded player's centre
            SamuraiFieldLook Fight(float clock = 7.3f, float phase = 0) => new()
            { Clock = clock, Presence = 1, Deploy = 1, Phase = phase };
            var shots = new List<Shot>
            {
                new("01-floor-centre-z1", 1920, 1080, new(Field.CenterX, floorView), 1, Fight()),
                new("02-left-wall-z1", 1920, 1080, new(Field.Left + 60, floorView), 1, Fight()),
                new("03-roof-corner-z1", 1920, 1080, new(Field.Right - 200, Field.Top + 140), 1, Fight(11.2f, 1)),
                new("04-right-wall-z15", 1920, 1080, new(Field.Right - 90, floorView - 200), 1.5f, Fight(13.9f, 1)),
                new("05-wide-2560-z1", 2560, 1440, new(Field.CenterX - 300, floorView - 300), 1, Fight(15.1f, 2)),
                new("06-deploy-35", 1920, 1080, new(Field.CenterX, floorView), 1, Fight(.3f) with { Deploy = .35f }),
                new("07-deploy-75", 1920, 1080, new(Field.CenterX, floorView), 1, Fight(.7f) with { Deploy = .75f }),
                new("08-victory-burn-30", 1920, 1080, new(Field.CenterX, floorView), 1, Fight(60) with { Ending = .3f }),
                new("09-victory-unravel-80", 1920, 1080, new(Field.CenterX, floorView), 1, Fight(61) with { Ending = .8f, Presence = .55f }),
                new("10-phase3-transition", 1920, 1080, new(Field.CenterX + 500, floorView - 260), 1, Fight(18.7f, 2) with { Surge = .9f }),
                new("11-outsider", 1920, 1080, new(Field.Left - 520, floorView), 1, Fight(20) with { Outsider = true }),
                new("12-reduced", 1920, 1080, new(Field.CenterX, floorView), 1, Fight(7.3f, 1) with { Reduced = 1 }),
                new("13-wall-ripple", 1920, 1080, new(Field.Left + 60, floorView), 1, Fight() with { Ripple0 = new(0, SamuraiArenaBounds.Height - 40, .12f, 1), Ripple1 = new(0, SamuraiArenaBounds.Height - 300, .34f, 1) }),
                new("14-backdrop-only", 1920, 1080, new(Field.CenterX, Field.CenterY), .75f, Fight(9.5f, 2), Seal: false),
                // Scenery only, for the luminance budget check (no stand-in terrain or actors).
                new("15-scenery-p1", 1920, 1080, new(Field.CenterX, Field.CenterY + 100), .75f, Fight(7.3f), Seal: false, Actors: false),
                new("16-scenery-p3-storm", 1920, 1080, new(Field.CenterX, Field.CenterY + 100), .75f, Fight(18.7f, 2) with { Surge = .9f }, Seal: false, Actors: false),
            };
            // Lightning windows: pick clocks inside a strike for the storm shots.
            foreach (var shot in shots) Render(device, shot, output);
            Console.WriteLine($"PASS {shots.Count} field frames from the production renderer and exported SamuraiBattlefield.fxc; stand-in world; no game launched.");
        }
        finally { SDL_DestroyWindow(window); SDL_Quit(); }
    }

    static void Render(GraphicsDevice device, Shot shot, string output)
    {
        using var target = new RenderTarget2D(device, shot.Width, shot.Height, false, SurfaceFormat.Color, DepthFormat.None);
        device.SetRenderTarget(target);
        device.Clear(Color.Black);
        Vector2 screen = new(shot.Width, shot.Height);
        Vector2 screenPosition = shot.View - screen / 2;
        Matrix view = Matrix.CreateTranslation(-screen.X / 2, -screen.Y / 2, 0) * Matrix.CreateScale(shot.Zoom, shot.Zoom, 1)
            * Matrix.CreateTranslation(screen.X / 2, screen.Y / 2, 0);
        var look = shot.Look with { Camera = shot.View - new Vector2(Field.CenterX, Field.CenterY) };
        var batch = new SpriteBatch(device);
        // vanilla daylight sky (what the seal must replace)
        batch.Begin();
        for (int y = 0; y < shot.Height; y += 8)
            batch.Draw(pixel, new Rectangle(0, y, shot.Width, 8), Color.Lerp(new Color(70, 140, 230), new Color(150, 200, 250), y / (float)shot.Height));
        batch.End();
        if (!look.Outsider)
            SamuraiFieldRenderer.DrawBackdrop(device, view, new Vector2(Field.Left, Field.Top) - screenPosition, look);
        if (!shot.Actors) { Save(device, target, shot, output); batch.Dispose(); return; }
        // terrain, players and a forecast in world space
        float tint = look.Outsider ? 0 : .38f * look.Presence;
        // ModifySunLightColor caps the daylight itself; tiles are lit by it
        Color Lit(Color c) => new(c.ToVector3() * Vector3.Lerp(Vector3.One, new Vector3(176, 166, 214) / 255f, tint > 0 ? 1 : 0));
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null,
            Matrix.CreateTranslation(-screenPosition.X, -screenPosition.Y, 0) * view);
        for (float x = Field.Left - 1600; x < Field.Right + 1600; x += 16)
        {
            float hill = x > Field.CenterX + 420 && x < Field.CenterX + 900 ? 64 + 32 * MathF.Sin((x - Field.CenterX - 420) / 480 * MathF.PI) : 0;
            float surface = Field.Bottom - MathF.Floor(hill / 16) * 16;
            batch.Draw(pixel, new Rectangle((int)x, (int)surface, 16, 16), Lit(new Color(40, 160, 60)));
            for (float y = surface + 16; y < Field.Bottom + 1400; y += 16)
                batch.Draw(pixel, new Rectangle((int)x, (int)y, 16, 16), Lit(y > Field.Bottom + 400 ? new Color(90, 90, 100) : new Color(130, 90, 60)));
        }
        foreach (var (x, y, w) in new[] { (Field.CenterX - 700, Field.Bottom - 230, 160), (Field.CenterX - 300, Field.Bottom - 420, 128), (Field.Left + 300, Field.Bottom - 300, 96) })
            batch.Draw(pixel, new Rectangle((int)x, (int)y, w, 8), Lit(new Color(150, 110, 70)));
        Vector2 playerAt = shot.Name.Contains("left-wall") || shot.Name.Contains("ripple") ? new(Field.Left + 1, Field.Bottom - 42)
            : shot.Name.Contains("roof") ? new(Field.Right - 220, Field.Top + 1) : shot.Name.Contains("outsider") ? new(Field.Left - 540, Field.Bottom - 42)
            : new(shot.View.X - 10, Field.Bottom - 42);
        batch.End();
        Vector2 ra = Vector2.Transform(new Vector2(Field.Left, Field.Top) - screenPosition, view);
        Vector2 rb = Vector2.Transform(new Vector2(Field.Right, Field.Bottom) - screenPosition, view);
        if (shot.Seal) SamuraiFieldRenderer.DrawRim(device, new Vector4(ra.X, ra.Y, rb.X, rb.Y), false, look);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null,
            Matrix.CreateTranslation(-screenPosition.X, -screenPosition.Y, 0) * view);
        batch.Draw(pixel, new Rectangle((int)playerAt.X, (int)playerAt.Y, 20, 42), Lit(new Color(230, 200, 170)));
        batch.Draw(pixel, new Rectangle((int)Field.CenterX + 120, (int)Field.Bottom - 420, 116, 170), new Color(110, 60, 200));
        // a forecast band in the cut palette (tone 2 dither field, tone 4 contour) to judge salience
        var forecast = new Rectangle((int)Field.CenterX - 260, (int)Field.Top + 120, 90, (int)SamuraiArenaBounds.Height - 120);
        batch.Draw(pixel, forecast, new Color(62, 16, 138) * .55f);
        batch.Draw(pixel, new Rectangle(forecast.X, forecast.Y, 4, forecast.Height), new Color(170, 108, 255));
        batch.Draw(pixel, new Rectangle(forecast.Right - 4, forecast.Y, 4, forecast.Height), new Color(170, 108, 255));
        batch.End();
        if (shot.Seal)
        {
            Vector2 a = Vector2.Transform(new Vector2(Field.Left, Field.Top) - screenPosition, view);
            Vector2 b = Vector2.Transform(new Vector2(Field.Right, Field.Bottom) - screenPosition, view);
            device.BlendState = BlendState.AlphaBlend;
            SamuraiFieldRenderer.DrawSeal(device, new Vector4(a.X, a.Y, b.X, b.Y), false, look);
        }
        Save(device, target, shot, output);
        batch.Dispose();
    }

    static void Save(GraphicsDevice device, RenderTarget2D target, Shot shot, string output)
    {
        device.SetRenderTarget(null);
        using var file = File.Create(Path.Combine(output, shot.Name + ".png"));
        target.SaveAsPng(file, shot.Width, shot.Height);
    }
}

namespace ReLogic.Content { public sealed class Asset<T> { public T Value; } }
namespace Luminance.Assets
{
    public static class MiscTexturesRegistry
    {
        public static readonly ReLogic.Content.Asset<Texture2D> TurbulentNoise = new() { Value = SamuraiFieldPreview.Noise("TurbulentNoise") };
        public static readonly ReLogic.Content.Asset<Texture2D> DendriticNoiseZoomedOut = new() { Value = SamuraiFieldPreview.Noise("DendriticNoiseZoomedOut") };
        public static readonly ReLogic.Content.Asset<Texture2D> WavyBlotchNoise = new() { Value = SamuraiFieldPreview.Noise("WavyBlotchNoise") };
    }
}
namespace Luminance.Core.Graphics
{
    public static class ShaderManager
    {
        static readonly Dictionary<string, ManagedShader> shaders = new();
        public static ManagedShader GetShader(string name)
        { if (!shaders.TryGetValue(name, out var s)) shaders[name] = s = new ManagedShader(name); return s; }
    }
    public sealed class ManagedShader
    {
        public readonly Effect Effect; readonly Texture[] textures = new Texture[4]; readonly SamplerState[] samplers = new SamplerState[4];
        public ManagedShader(string name)
        {
            string path = Path.Combine(SamuraiFieldPreview.Root, "Assets/AutoloadedEffects/Shaders/" + name.Split('.')[1] + ".fxc");
            Effect = new Effect(SamuraiFieldPreview.Device, File.ReadAllBytes(path));
        }
        public void TrySetParameter(string name, float value) => Effect.Parameters[name]?.SetValue(value);
        public void TrySetParameter(string name, Vector2 value) => Effect.Parameters[name]?.SetValue(value);
        public void TrySetParameter(string name, Vector4 value) => Effect.Parameters[name]?.SetValue(value);
        public void TrySetParameter(string name, Matrix value) => Effect.Parameters[name]?.SetValue(value);
        public void SetTexture(Texture texture, int slot, SamplerState sampler) { textures[slot] = texture; samplers[slot] = sampler; }
        public void Apply(string pass = "AutoloadPass")
        {
            Effect.CurrentTechnique.Passes[pass].Apply(); var d = SamuraiFieldPreview.Device;
            d.BlendState = BlendState.AlphaBlend; d.DepthStencilState = DepthStencilState.None; d.RasterizerState = RasterizerState.CullNone;
            for (int i = 0; i < textures.Length; i++) if (textures[i] != null) { d.Textures[i] = textures[i]; d.SamplerStates[i] = samplers[i]; }
        }
    }
}
