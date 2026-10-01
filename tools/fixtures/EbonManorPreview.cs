// Offline FNA D3D11 render of the distributable Ebon Manor effects on the real
// textures, with parameter conventions mirrored from EbonMaterials/EbonScene.
// This is material/geometry QA, not Terraria and not an in-game test.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct SilkVertex : IVertexType
{
    public Vector3 Position; public Color Color; public Vector3 TexCoord;
    public SilkVertex(Vector2 p, Vector3 uv) { Position = new(p, 0); Color = Color.White; TexCoord = uv; }
    public static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0));
    VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}

internal static class EbonManorPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern void SDL_Quit();
    const int W = 1280, H = 720;
    static GraphicsDevice device = null!;
    static Effect manor = null!, silk = null!;
    static Texture2D hall = null!, final = null!, frame = null!, props = null!, wide = null!, tall = null!, shears = null!, noirette = null!, invitation = null!, wavy = null!, turbulent = null!;
    static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    static readonly List<SilkVertex> strip = new();
    static Matrix screen;
    static float clock;
    static readonly Vector3 Moon = new(.62f, .70f, .90f), SilkTint = new(.66f, .68f, .86f), Rose = new(.86f, .46f, .56f),
        Chalk = new(.84f, .80f, .72f), Gold = new(.90f, .72f, .44f), Candle = new(1f, .70f, .38f), Hot = new(1f, .84f, .88f),
        Bloom = new(.74f, .80f, 1f), Dusty = new(.90f, .58f, .70f), Pale = new(.90f, .93f, 1f);
    static bool Reduced;

    static void Main(string[] args)
    {
        string root = args[0], noise = args[1], native = args[2], output = args[3];
        IntPtr Resolve(string name, System.Reflection.Assembly a, DllImportSearchPath? p)
        { string file = Path.Combine(native, name.EndsWith(".dll") ? name : name + ".dll"); return File.Exists(file) ? NativeLibrary.Load(file) : IntPtr.Zero; }
        NativeLibrary.SetDllImportResolver(typeof(EbonManorPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL video initialization failed");
        IntPtr window = SDL_CreateWindow("Offline Ebon QA", 0, 0, W, H, FNA3D_PrepareWindowAttributes() | 0x8);
        if (window == IntPtr.Zero) throw new Exception("Hidden device surface unavailable");
        try
        {
            device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, new PresentationParameters
            {
                DeviceWindowHandle = window, BackBufferWidth = W, BackBufferHeight = H, BackBufferFormat = SurfaceFormat.Color,
                IsFullScreen = false, DepthStencilFormat = DepthFormat.None, PresentationInterval = PresentInterval.Immediate
            });
            using var target = new RenderTarget2D(device, W, H, false, SurfaceFormat.Color, DepthFormat.None);
            Texture2D Load(string path, bool premultiply)
            {
                using var stream = File.OpenRead(path); var t = Texture2D.FromStream(device, stream);
                if (premultiply) { var c = new Color[t.Width * t.Height]; t.GetData(c); for (int i = 0; i < c.Length; i++) c[i] = Color.FromNonPremultiplied(c[i].ToVector4()); t.SetData(c); }
                return t;
            }
            string tex = Path.Combine(root, "Assets/Textures/EbonManor");
            hall = Load(Path.Combine(tex, "ManorHall.png"), false); final = Load(Path.Combine(tex, "ManorHallFinal.png"), false);
            frame = Load(Path.Combine(tex, "ManorFrame.png"), true); props = Load(Path.Combine(tex, "ManorProps.png"), true);
            wide = Load(Path.Combine(tex, "ChandelierWide.png"), true); tall = Load(Path.Combine(tex, "ChandelierTall.png"), true);
            shears = Load(Path.Combine(tex, "Shears.png"), true); noirette = Load(Path.Combine(tex, "Noirette.png"), true);
            invitation = Load(Path.Combine(tex, "BlackInvitation.png"), true);
            wavy = Load(Path.Combine(noise, "WavyBlotchNoise.png"), false); turbulent = Load(Path.Combine(noise, "TurbulentNoise.png"), false);
            manor = new Effect(device, File.ReadAllBytes(Path.Combine(root, "Assets/AutoloadedEffects/Shaders/EbonManor.fxc")));
            silk = new Effect(device, File.ReadAllBytes(Path.Combine(root, "Assets/AutoloadedEffects/Shaders/EbonSilk.fxc")));
            screen = Matrix.CreateOrthographicOffCenter(0, W, H, 0, -1, 1);
            var scenes = new (string Name, Action Draw)[]
            {
                ("hall-01-moonlight", () => Hall(1, .3f, 0, 0, 0, 0)),
                ("hall-02-candles", () => Hall(1, .62f, .5f, 0, 0, 0)),
                ("hall-03-act-one", () => Hall(1, 1, 1, 0, 0, .8f)),
                ("hall-04-act-two", () => Hall(1, 1, 1, 1, 0, 0)),
                ("hall-05-tear-12", () => Hall(1, 1, 1, 1, .12f, 0)),
                ("hall-06-tear-35", () => Hall(1, 1, 1, 1, .35f, 0)),
                ("hall-07-tear-65", () => Hall(1, 1, 1, 1, .65f, 0)),
                ("hall-08-finale", () => Hall(1, 1, 1, 1, 1, .6f)),
                ("throw-01-warning", () => Throw(.25f, -1)),
                ("throw-02-tight", () => Throw(.92f, -1)),
                ("throw-03-flight", () => At(14, () => Throw(1, 14))),
                ("throw-04-snap", () => At(1.5f, () => Throw(1, 1.5f))),
                ("throw-05-overlap", () => ThrowOverlap()),
                ("throw-06-crash-near", () => At(30, () => Throw(1, 30))),
                ("chandelier-01-warning", () => Chandelier(.35f, 0, 0)),
                ("chandelier-02-fall", () => Chandelier(1, 8, 0)),
                ("chandelier-03-burst", () => Chandelier(1, 0, 3)),
                ("chandelier-04-last-beat", () => Chandelier(.93f, 0, 0)),
                ("loom-01-warning", () => Loom(.45f * 86, -1)),
                ("loom-02-live", () => At(2, () => Loom(86, 2))),
                ("loom-03-strung", () => Loom(5, -1)),
                ("loom-04-last-beat", () => Loom(.92f * 86, -1)),
                ("loom-05-snap", () => At(.6f, () => Loom(86, .6f))),
                ("loom-06-live-late", () => At(10, () => Loom(86, 10))),
                ("loom-07-fray", () => At(18, () => Loom(86, -1, 6))),
                ("loom-08-ignite", () => At(2.5f, () => Loom(86, 2.5f))),
                ("loom-09-fray-late", () => At(24, () => Loom(86, -1, 12))),
                ("shears-01-warning", () => Shears(.6f, -1)),
                ("shears-02-live", () => Shears(1, 2)),
                ("shears-03-live-late", () => Shears(1, 9)),
                ("shears-04-heal", () => Shears(1, -1, 8)),
                ("shears-05-heal-early", () => Shears(1, -1, 2)),
                ("waltz-01-warning", () => Waltz(.5f * 115.2f, -1)),
                ("waltz-02-live", () => At(12, () => Waltz(115.2f, 12))),
                ("waltz-03-warning-beat", () => Waltz(2.1f * 28.8f, -1)),
                ("waltz-04-step", () => At(29.8f, () => Waltz(115.2f, 28.8f + 1))),
                ("waltz-05-release", () => Waltz(115.2f, -1, 7)),
                ("waltz-06-fire", () => At(1, () => Waltz(115.2f, 1))),
                ("waltz-07-warning-late", () => Waltz(3.2f * 28.8f, -1)),
                ("web-01-weaving", () => Web(.35f * 115.2f, -1)),
                ("web-02-tight", () => Web(.95f * 115.2f, -1)),
                ("web-03-live", () => At(2, () => Web(115.2f, 2))),
                ("web-04-snap", () => At(.6f, () => Web(115.2f, .6f))),
                ("web-05-fray", () => At(18, () => Web(115.2f, -1, 6))),
                ("web-06-ignite", () => At(2.5f, () => Web(115.2f, 2.5f))),
                ("reduced-01-loom-live", () => WithReduced(() => Loom(86, 2))),
                ("reduced-02-web-live", () => WithReduced(() => Web(115.2f, 2))),
                ("reduced-03-waltz-warning", () => WithReduced(() => Waltz(2.1f * 28.8f, -1))),
                ("reduced-04-throw-tight", () => WithReduced(() => Throw(.92f, -1))),
                ("reduced-05-chandelier-warning", () => WithReduced(() => Chandelier(.93f, 0, 0))),
                ("reduced-06-waltz-live", () => WithReduced(() => At(40, () => Waltz(115.2f, 40)))),
                ("reduced-07-chandelier-burst", () => WithReduced(() => Chandelier(1, 0, 3))),
                ("zoom-01-loom-live-067x", () => WithZoom(.67f, () => At(4, () => Loom(86, 4)))),
                ("zoom-02-web-warning-067x", () => WithZoom(.67f, () => Web(.95f * 115.2f, -1))),
                ("zoom-03-loom-live-2x", () => WithZoom(2, () => At(4, () => Loom(86, 4)))),
                ("stitch-01-stack", () => Stitch(false)),
                ("stitch-02-spread", () => Stitch(true)),
                ("noirette-01-poses", () => Poses()),
                ("noirette-02-weave", () => WeaveSequence()),
            };
            // Optional fifth argument: render only scenes whose name contains it (iteration aid).
            string? only = args.Length > 4 ? args[4] : null;
            foreach (var (name, draw) in scenes)
            {
                if (only is not null && !name.Contains(only)) continue;
                device.SetRenderTarget(target); device.Clear(new Color(8, 8, 12)); clock = 3.7f;
                draw();
                device.SetRenderTarget(null);
                using var file = File.Create(Path.Combine(output, name + ".png")); target.SaveAsPng(file, W, H);
            }
        }
        finally { device?.Dispose(); SDL_DestroyWindow(window); SDL_Quit(); }
    }

    // Advance the shared clock by a frame's ticks past the default sample time.
    static void At(float ticks, Action draw) { clock = 3.7f + ticks / 60; draw(); }
    static void WithReduced(Action draw) { Reduced = true; try { draw(); } finally { Reduced = false; } }
    // Game zoom about the screen centre; View is world px per screen px (EbonMaterials.View).
    static float View = 1;
    static void WithZoom(float zoom, Action draw)
    {
        var keep = screen;
        screen = Matrix.CreateTranslation(-W / 2f, -H / 2f, 0) * Matrix.CreateScale(zoom) * Matrix.CreateTranslation(W / 2f, H / 2f, 0) * keep;
        View = 1 / zoom;
        try { draw(); } finally { screen = keep; View = 1; }
    }
    static void Set(Effect e, string name, float v) => e.Parameters[name]?.SetValue(v);
    static void Set(Effect e, string name, Vector2 v) => e.Parameters[name]?.SetValue(v);
    static void Set(Effect e, string name, Vector3 v) => e.Parameters[name]?.SetValue(v);
    static void Set(Effect e, string name, Vector4 v) => e.Parameters[name]?.SetValue(v);
    static void Set(Effect e, string name, Matrix v) => e.Parameters[name]?.SetValue(v);

    static void Bind(Texture2D? art, SamplerState sampler, Texture2D? second = null)
    {
        device.Textures[0] = art; device.Textures[1] = wavy; device.Textures[2] = turbulent; device.Textures[3] = second;
        device.SamplerStates[0] = sampler; device.SamplerStates[1] = device.SamplerStates[2] = SamplerState.LinearWrap; device.SamplerStates[3] = SamplerState.LinearClamp;
    }
    static void Pass(string pass, Texture2D? art, SamplerState sampler, Texture2D? second = null)
    {
        device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
        Set(manor, "uWorldViewProjection", screen); Set(manor, "clock", clock); Set(manor, "view", View);
        manor.CurrentTechnique.Passes[pass].Apply(); Bind(art, sampler, second);
    }
    static void Submit(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br)
    {
        quad[0] = new(new(tl, 0), Color.White, new(0, 0)); quad[1] = new(new(bl, 0), Color.White, new(0, 1));
        quad[2] = new(new(tr, 0), Color.White, new(1, 0)); quad[3] = quad[2]; quad[4] = quad[1]; quad[5] = new(new(br, 0), Color.White, new(1, 1));
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }
    static void Quad(string pass, Texture2D? art, Vector2 center, Vector2 size, float rotation, SamplerState? sampler = null, Texture2D? second = null)
    {
        Pass(pass, art, sampler ?? SamplerState.LinearClamp, second);
        Vector2 dx = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * size.X * .5f, dy = new Vector2(-MathF.Sin(rotation), MathF.Cos(rotation)) * size.Y * .5f;
        Submit(center - dx - dy, center + dx - dy, center - dx + dy, center + dx + dy);
    }
    static void Sprite(Texture2D texture, Rectangle source, Vector2 pivot, Vector2 origin, float scale, float rotation, bool flip, Vector4 signal, float shade = 0, SamplerState? sampler = null, float rimless = 0, float weave = 12)
    {
        Set(manor, "region", new Vector4(source.X / (float)texture.Width, source.Y / (float)texture.Height, source.Width / (float)texture.Width, source.Height / (float)texture.Height));
        Set(manor, "shape", new Vector4(2f / source.Width, 2f / source.Height, shade, rimless)); Set(manor, "signal", signal); Set(manor, "weaveDensity", weave);
        Pass("AutoloadPass", texture, sampler ?? SamplerState.LinearClamp);
        Vector2 C(float u, float v) { var l = (new Vector2(u * source.Width, v * source.Height) - origin) * scale; if (flip) l.X = -l.X; return pivot + Vector2.Transform(l, Matrix.CreateRotationZ(rotation)); }
        Submit(C(0, 0), C(1, 0), C(0, 1), C(1, 1));
    }
    // ---- mirrors of EbonMaterials (same passes, parameter packing and limits) ----
    static float ReducedF => Reduced ? 1 : 0;
    static void Veil(Vector2 a, Vector2 b, float radius, float progress, float opacity, float seed, float live = 0, float clip = 0)
    {
        float length = Vector2.Distance(a, b); if (length < 1 || opacity <= .002f) return;
        Set(manor, "shape", new Vector4(length, radius, Math.Clamp(progress, 0, 1), Math.Clamp(live, 0, 1)));
        Set(manor, "signal", new Vector4(opacity, clip > 0 ? clip : -1e4f, ReducedF, seed));
        var d = b - a; Quad("VeilPass", null, (a + b) * .5f, new Vector2(length, radius * 2), MathF.Atan2(d.Y, d.X));
    }
    static void Hairline(Vector2 a, Vector2 b, float radius, float progress, float opacity, float seed, float reveal = 1, float tremble = 0)
    {
        float length = Vector2.Distance(a, b); if (length < 1 || opacity <= .002f) return;
        tremble = Math.Clamp(tremble * (Reduced ? .4f : 1), 0, Math.Max(0, Math.Min(radius - 2, length * .006f)));
        Set(manor, "shape", new Vector4(length, radius, Math.Clamp(progress, 0, 1), Math.Clamp(reveal, 0, 1)));
        Set(manor, "signal", new Vector4(opacity, tremble, ReducedF, seed));
        var d = b - a; Quad("HairlinePass", null, (a + b) * .5f, new Vector2(length, radius * 2), MathF.Atan2(d.Y, d.X));
    }
    static void VeilDisc(Vector2 c, float radius, float progress, float opacity, float seed)
    {
        if (opacity <= .002f) return;
        Set(manor, "shape", new Vector4(radius, Math.Clamp(progress, 0, 1), 0, 0)); Set(manor, "signal", new Vector4(opacity, 0, ReducedF, seed));
        Quad("DiscPass", null, c, new Vector2(radius * 2), 0);
    }
    static void Glint(Vector2 c, float size, Vector3 tint, float opacity, float rays = 1, float rotation = 0)
    {
        if (opacity <= .002f) return;
        Set(manor, "signal", new Vector4(opacity, rays, size, 0)); Set(manor, "tint", tint);
        Quad("GlintPass", null, c, new Vector2(size), rotation);
    }
    static void Tear(Vector2 a, Vector2 b, float radius, float since, float live, float opacity, float seed, float blade = -1)
    {
        float length = Vector2.Distance(a, b);
        Set(manor, "shape", new Vector4(length, radius, Math.Max(0, since), live)); Set(manor, "signal", new Vector4(opacity, blade >= 0 ? blade : -1e4f, ReducedF, seed));
        var d = b - a; Quad("TearPass", null, (a + b) * .5f, new Vector2(length, radius * 2), MathF.Atan2(d.Y, d.X));
    }
    static void Glow(Vector2 c, float diameter, Vector3 tint, float opacity, float ring = 0, float width = .1f, float additive = 1)
    {
        Set(manor, "shape", new Vector4(ring, width, additive, 0)); Set(manor, "signal", new Vector4(opacity, 0, 0, 0)); Set(manor, "tint", tint);
        Quad("GlowPass", null, c, new Vector2(diameter), 0);
    }
    static void Ring(Vector2 c, float radius, bool spread, float progress, Vector3 tint, float opacity)
    {
        float outer = radius + 2.4f;
        Set(manor, "shape", new Vector4(spread ? 1 : 0, progress, outer, 0)); Set(manor, "signal", new Vector4(opacity, 0, 0, radius * .01f)); Set(manor, "tint", tint);
        Quad("RingPass", null, c, new Vector2(outer * 2), 0);
    }
    static void Shard(Vector2 c, float length, float width, float rotation, int kind, float opacity, float glint = .5f)
    {
        if (opacity <= .002f) return;
        Set(manor, "shape", new Vector4(kind, 0, 0, 0)); Set(manor, "signal", new Vector4(opacity, glint, 0, 0));
        Quad("ShardPass", null, c, new Vector2(length, width), rotation);
    }
    // EbonMaterials.Burst / Hash: deterministic debris on analytic arcs.
    static void Burst(Vector2 origin, int seed, int count, float t, float life, int kind, float speed, float gravity, float size, float spread = MathF.Tau, float heading = 0)
    {
        if (t < 0 || t >= life) return;
        if (Reduced) count = Math.Max(2, count / 3);
        for (int i = 0; i < count; i++)
        {
            float h1 = Hash(seed, i, 1), h2 = Hash(seed, i, 2), h3 = Hash(seed, i, 3);
            float angle = heading + (h1 - .5f) * spread, v = speed * (.35f + .65f * h2), drag = 1 - MathF.Exp(-t / 14);
            Vector2 at = origin + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * v * 14 * drag + new Vector2(0, .5f * gravity * t * t);
            float fade = 1 - Ease((t - life * .45f) / (life * .55f)), spin = (h3 - .5f) * .5f * t + angle;
            Shard(at, size * (.6f + .8f * h3), size * (.22f + .2f * h2), spin, kind, fade, t * .3f + i);
        }
    }
    static float Hash(int seed, int i, int salt)
    {
        uint x = (uint)seed * 747796405u + (uint)i * 2891336453u + (uint)salt * 277803737u;
        x ^= x >> 16; x *= 2246822519; x ^= x >> 13; x *= 3266489917; x ^= x >> 16;
        return (x & 0xFFFFFF) / 16777216f;
    }
    static void Sweep(Vector2 c, float from, float to, float inner, float reach, Vector3 tint, float opacity, float seed = .3f, float outer = 0)
    {
        if (opacity <= .002f || MathF.Abs(to - from) < .002f) return;
        var fan = new List<VertexPositionColorTexture>();
        const int steps = 16;
        for (int k = 0; k < steps; k++)
        {
            float u0 = k / (float)steps, u1 = (k + 1) / (float)steps, a0 = MathHelper.Lerp(from, to, u0), a1 = MathHelper.Lerp(from, to, u1);
            Vector2 d0 = new(MathF.Cos(a0), MathF.Sin(a0)), d1 = new(MathF.Cos(a1), MathF.Sin(a1));
            Vector2 i0 = c + d0 * inner, i1 = c + d1 * inner, o0 = c + d0 * Math.Max(inner + 1, Math.Min(reach, Reach(c, d0))), o1 = c + d1 * Math.Max(inner + 1, Math.Min(reach, Reach(c, d1)));
            fan.Add(new(new(i0, 0), Color.White, new(0, u0))); fan.Add(new(new(o0, 0), Color.White, new(1, u0))); fan.Add(new(new(i1, 0), Color.White, new(0, u1)));
            fan.Add(new(new(o0, 0), Color.White, new(1, u0))); fan.Add(new(new(o1, 0), Color.White, new(1, u1))); fan.Add(new(new(i1, 0), Color.White, new(0, u1)));
        }
        Set(manor, "signal", new Vector4(opacity * (Reduced ? .5f : 1), outer > 0 ? outer : -1, ReducedF, seed)); Set(manor, "tint", tint);
        Pass("SweepPass", null, SamplerState.LinearClamp);
        var array = fan.ToArray(); device.DrawUserPrimitives(PrimitiveType.TriangleList, array, 0, array.Length / 3);
    }

    // Classic silk (razor.x = 0), razor silk (razor.z < 0) and frayed silk share the EbonSilk primitive material.
    static void Thread(IReadOnlyList<Vector2> points, float half, Vector3 tint, float opacity, float tension, float heat, float seed, bool taper = false)
        => Render(points, half, tint, opacity, tension, heat, taper, Vector4.Zero, Vector2.Zero, Vector4.Zero, Vector4.Zero, Vector4.Zero, seed);
    static void Razor(Vector2 a, Vector2 b, float radius, float opacity, float since, float seed, float twangAmplitude = 0, float twangRate = 0,
        float flash = 1, bool openStart = false, bool openEnd = false)
        => RazorPath(new List<Vector2> { a, b }, radius, opacity, since, seed, twangAmplitude, twangRate, flash, openStart, openEnd);
    static void RazorPath(IReadOnlyList<Vector2> path, float radius, float opacity, float since, float seed, float twangAmplitude = 0, float twangRate = 0,
        float flash = 1, bool openStart = false, bool openEnd = false)
    {
        radius = Math.Max(1, radius);
        float length = PathLength(path), gain = Math.Max(0, flash), t = Math.Max(0, since);
        float amplitude = Math.Min(twangAmplitude * (Reduced ? .4f : 1), Math.Max(0, Math.Min(radius - 1.5f, length * .006f)));
        float meet = 1 - MathF.Pow(1 - Math.Clamp(t / 6, 0, 1), 3);
        var snap = new Vector4(1 - .65f * gain, meet * (length * .5f + 16), .8f * (1 - Ease((meet - .86f) / .14f)) * gain,
            MathF.Pow(2, -t / 3.5f) * (1 - ReducedF * .5f) * gain);
        Render(path, radius + Math.Max(8, radius * .7f), Bloom, opacity, 1, 1, false,
            new Vector4(radius, t, -1, ReducedF), new Vector2(amplitude, twangRate),
            new Vector4(openStart ? 1 : 0, openEnd ? 1 : 0, Math.Min(radius, 12), Math.Max(.6f, .75f * View)), snap, Vector4.Zero, seed);
    }
    static void Frayed(IReadOnlyList<Vector2> path, float radius, float fray, int seed, bool freeStart, bool freeEnd)
    {
        if (fray >= 1) return;
        float f = Math.Clamp(fray, 0, 1), length = PathLength(path), left = 1 - f;
        bool second = Hash(seed, 5, 3) >= .45f && length >= 260;
        var breaks = new Vector4((.3f + .4f * Hash(seed, 5, 1)) * length, -14 + 75 * f,
            (.12f + .2f * Hash(seed, 5, 2)) * length, second ? -14 + 54 * Math.Clamp(f * 1.6f - .5f, 0, 1) : -100);
        var snap = new Vector4(left * left, Ease((f - .15f) / .25f) * left, 0, 0);
        Render(path, Math.Max(1, radius) + 8, Bloom, 1, 1, 0, false, new Vector4(Math.Max(1, radius), 0, f, ReducedF), Vector2.Zero,
            new Vector4(freeStart ? 1 : 0, freeEnd ? 1 : 0, 10 + 170 * f, Math.Max(.75f * (1 - .3f * f), .75f * View)), snap, breaks, seed);
    }
    static float PathLength(IReadOnlyList<Vector2> path)
    {
        float length = 0; for (int i = 1; i < path.Count; i++) length += Vector2.Distance(path[i - 1], path[i]); return length;
    }
    // EbonScene.Droop: tied at anchor, free end let go, drooping toward the free end.
    static List<Vector2> Droop(Vector2 anchor, Vector2 free, float sag)
    {
        var into = new List<Vector2>();
        for (int i = 0; i <= 13; i++) { float u = i / 13f; into.Add(Vector2.Lerp(anchor, free, u) + new Vector2(0, sag * u * u)); }
        return into;
    }
    // EbonScene.AftermathSilk timing for a strand released `t` ticks ago.
    static (float Fray, float Recoil, float Sag) Release(float t) => (Ease(t / 18), .35f * OutExpo(t / 10), 24 * Ease(t / 18));
    static void Render(IReadOnlyList<Vector2> points, float half, Vector3 tint, float opacity, float tension, float heat, bool taper, Vector4 razor, Vector2 twang,
        Vector4 razorEx, Vector4 snap, Vector4 breaks, float seed)
    {
        if (points.Count < 2 || opacity <= .003f) return;
        strip.Clear(); float length = 0;
        for (int i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
        float run = 0;
        var verts = new List<(Vector2 A, Vector2 B, float U)>();
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) run += Vector2.Distance(points[i - 1], points[i]);
            var d = points[Math.Min(i + 1, points.Count - 1)] - points[Math.Max(i - 1, 0)]; if (d.LengthSquared() < 1e-4f) d = Vector2.UnitX; d.Normalize();
            var n = new Vector2(-d.Y, d.X); float u = run / Math.Max(1, length), w = half * (taper ? .2f + .8f * u : 1);
            verts.Add((points[i] + n * w, points[i] - n * w, u));
        }
        for (int i = 0; i + 1 < verts.Count; i++)
        {
            var (a0, b0, u0) = verts[i]; var (a1, b1, u1) = verts[i + 1];
            strip.Add(new(a0, new(u0, 0, 1))); strip.Add(new(b0, new(u0, 1, 1))); strip.Add(new(a1, new(u1, 0, 1)));
            strip.Add(new(b0, new(u0, 1, 1))); strip.Add(new(b1, new(u1, 1, 1))); strip.Add(new(a1, new(u1, 0, 1)));
        }
        device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
        Set(silk, "uWorldViewProjection", screen); Set(silk, "clock", clock); Set(silk, "completionScale", 1f);
        Set(silk, "signal", new Vector4(opacity * (Reduced ? .85f : 1), tension, heat, seed)); Set(silk, "tint", tint); Set(silk, "footprint", new Vector2(Math.Max(1, length), half));
        Set(silk, "razor", razor); Set(silk, "twang", twang); Set(silk, "razorEx", razorEx); Set(silk, "snap", snap); Set(silk, "breaks", breaks);
        silk.CurrentTechnique.Passes[0].Apply(); device.Textures[1] = wavy; device.SamplerStates[1] = SamplerState.LinearWrap;
        var array = strip.ToArray(); device.DrawUserPrimitives(PrimitiveType.TriangleList, array, 0, array.Length / 3);
    }
    static void Straight(Vector2 a, Vector2 b, float half, Vector3 tint, float opacity, float tension, float heat, float amplitude = 0, float frequency = 0, float seed = .3f)
    {
        var line = new List<Vector2>(); int steps = amplitude > .05f ? 24 : 2; var n = Vector2.Normalize(b - a); n = new(-n.Y, n.X);
        for (int i = 0; i <= steps; i++) { float u = i / (float)steps; line.Add(Vector2.Lerp(a, b, u) + n * MathF.Sin(u * MathF.PI) * MathF.Sin(u * MathF.PI * 3 + clock * 60 * frequency) * amplitude); }
        Thread(line, half, tint, opacity, tension, heat, seed);
    }
    static void Wave(List<Vector2> into, Vector2 a, Vector2 b, float amplitude, float rate, int steps)
    {
        var n = Vector2.Normalize(b - a); n = new(-n.Y, n.X);
        for (int i = 1; i <= steps; i++) { float u = i / (float)steps; into.Add(Vector2.Lerp(a, b, u) + n * MathF.Sin(u * MathF.PI) * MathF.Sin(u * MathF.PI * 3 + clock * 60 * rate) * amplitude); }
    }
    static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    static float OutExpo(float x) => x <= 0 ? 0 : x >= 1 ? 1 : 1 - MathF.Pow(2, -10 * x);
    static float Pulse(float t, float decay) => t < 0 ? 0 : MathF.Exp(-t / decay);
    // Clip an infinite line to the fixture's field box (stand-in for RaidFieldGeometry.ClipAxis).
    static readonly Vector4 Field = new(80, 30, 1200, 700);
    static bool Clip(Vector2 through, Vector2 d, out Vector2 a, out Vector2 b)
    {
        float t0 = -1e6f, t1 = 1e6f;
        void Axis(float p, float v, float lo, float hi)
        {
            if (MathF.Abs(v) < 1e-6f) { if (p < lo || p > hi) { t0 = 1; t1 = 0; } return; }
            float x0 = (lo - p) / v, x1 = (hi - p) / v; if (x0 > x1) (x0, x1) = (x1, x0); t0 = Math.Max(t0, x0); t1 = Math.Min(t1, x1);
        }
        Axis(through.X, d.X, Field.X, Field.Z); Axis(through.Y, d.Y, Field.Y, Field.W);
        a = through + d * t0; b = through + d * t1; return t1 > t0;
    }
    static float Reach(Vector2 c, Vector2 d) { Clip(c, d, out _, out var b); return Vector2.Distance(c, b); }

    // The hall as the sky draws it: cover-fit painting plus the proscenium frame.
    static void Hall(float fade, float exposure, float candles, float tension, float tear, float flash, bool frameLayer = true)
    {
        float scale = Math.Max(W / (float)hall.Width, H / (float)hall.Height) * 1.08f;
        Vector2 size = new(W / (hall.Width * scale), H / (hall.Height * scale)), at = (Vector2.One - size) * .5f;
        Set(manor, "region", new Vector4(at, size.X, size.Y));
        Set(manor, "signal", new Vector4(fade, tear, 0, tension)); Set(manor, "shape", new Vector4(candles, .5f, exposure, flash));
        Quad("BackdropPass", hall, new(W / 2f, H / 2f), new(W, H), 0, SamplerState.LinearClamp, final);
        if (!frameLayer) return;
        Set(manor, "signal", new Vector4(fade, 0, 0, 0)); Set(manor, "shape", new Vector4(0, 0, Math.Min(1, exposure), 0));
        Quad("FramePass", frame, new(W / 2f, H / 2f), new(W, H), 0);
    }
    static void Player(Vector2 at)
    {
        Set(manor, "shape", new Vector4(0, 0, 0, 0)); Set(manor, "signal", new Vector4(.9f, 0, 0, 0)); Set(manor, "tint", new Vector3(.2f, .7f, .35f));
        Quad("GlowPass", null, at, new Vector2(26, 50), 0);
    }

    // ---- attack scenes: each mirrors EbonScene's Forecast / Silk / Body / Aftermath calls ----

    // One thrown piece of furniture. progress: warning progress; t: ticks since fire (< 0 while warning).
    // The route ends at the wall like EbonScene.CrashPoint (ClipLength = the field clip).
    static void ThrowOne(Vector2 origin, Vector2 target, int variant, float progress, float t, float seed)
    {
        var d = Vector2.Normalize(target - origin); float length = Reach(origin, d); Vector2 to = origin + d * length;
        bool live = t >= 0;
        float travel = live ? 9 * t + .5f * 1.15f * t * t : 0;
        if (live && travel >= length) return;
        Vector2 prop = origin + d * travel + (live ? Vector2.Zero : new Vector2(0, -26 * progress));
        float appear = 1;
        // Forecast: the veil on the route still ahead of the furniture.
        Vector2 from = live ? prop : origin;
        if (Vector2.Dot(to - from, d) > 8) Veil(from, to, 46, live ? 1 : progress, appear * (live ? .85f : 1), seed, live ? 1 : 0);
        // EbonThreads' control thread from her hand (Verlet in game; a straight stand-in here).
        var hook = prop + new Vector2(0, -60); var spine = prop + d * 90;
        if (!live) Straight(new(640, 330), hook, 2.6f + 1.2f * progress, SilkTint, .72f, progress, 0, 0, 0, seed);
        var path = new List<Vector2> { hook, Vector2.Lerp(hook, spine, .5f), spine };
        if (!live)
        {
            Wave(path, spine, to, (3 * (1 - progress) + .3f) * (Reduced ? .4f : 1), .12f + .4f * progress, 12);
            Thread(path, 2.2f, Vector3.Lerp(SilkTint, Dusty, Ease((progress - .66f) / .34f)), .42f + .2f * progress, progress, 0, seed);
        }
        else
        {
            Thread(path, 2.2f, SilkTint, .55f, 1, .3f, seed);
            if (Vector2.Dot(to - spine, d) > 8) Razor(spine, to, 2.5f, 1, t, seed, 2.2f * MathF.Exp(-t / 5), 1.4f, 1, openStart: true);
            var trail = new List<Vector2>(); for (int k = 8; k >= 0; k--) { float tk = Math.Max(0, t - k * 1.4f); trail.Add(origin + d * (9 * tk + .5f * 1.15f * tk * tk)); }
            Thread(trail, 26, SilkTint, .26f, 1, .5f, seed + 5, true);
        }
        // Body: the furniture and the far anchor pin.
        Sprite(props, new Rectangle(variant * 176, 0, 176, 176), prop, new(88, 90), .75f, live ? travel * .0045f : .04f, d.X < 0,
            new Vector4(1, 1, live ? .6f * Pulse(t, 5) : .25f * progress * progress, variant * .31f));
        float pin = live ? 1 : .45f + .3f * progress;
        Glint(to, 20 + 8 * (live ? 1 : progress), Pale, pin * (.8f + .2f * MathF.Sin(clock * 18)), .8f, MathF.Atan2(d.Y, d.X));
    }
    static void Throw(float progress, float t)
    {
        Hall(1, 1, 1, 0, 0, 0);
        Player(new(900, 470));
        ThrowOne(new(140, 250), new(1150, 560), 0, progress, t, .31f);
    }
    // Act One opens a bar with four throws on consecutive beats toward one member:
    // one in flight, one in its last beat, one mid-warning, one just risen.
    static void ThrowOverlap()
    {
        Hall(1, 1, 1, 0, 0, 0);
        var member = new Vector2(760, 520);
        Player(member);
        ThrowOne(new(1130, 210), member, 3, .98f, -1, .9f);
        ThrowOne(new(140, 230), member, 0, .97f, 7, .31f);
        ThrowOne(new(160, 600), member, 1, .52f, -1, .55f);
        ThrowOne(new(1140, 640), member, 2, .04f, -1, .73f);
    }

    static void Chandelier(float progress, float fall, float burst)
    {
        Hall(1, 1, 1, 0, 0, 0);
        float x = 640, anchor = 40, floor = 700; Vector2 impact = new(x, floor - 30);
        float drop = .5f * 3.1f * fall * fall; Vector2 body = new(x, anchor + 150 + drop);
        if (burst <= 0)
        {
            float hold = fall > 0 ? 1 : progress;
            // The corridor runs to the burst centre and fades out across the disc's edge.
            if (impact.Y - body.Y > 8) Veil(body, impact, 118, hold, 1, .2f, 0, 270);
            VeilDisc(impact, 270, hold, 1, .7f);
            if (fall <= 0) Straight(new(x - 3, -200), body - new Vector2(0, (198 - 10) * .75f), 2.6f, SilkTint, .75f, progress, 0, progress > .7f ? 1.2f : 0, 1.1f);
            else
            {
                float recoil = OutExpo(fall / 10); var cut = new Vector2(x, anchor + 150 - (198 - 10) * .75f);
                Straight(new(cut.X, -200), Vector2.Lerp(cut, new(cut.X, -200), recoil), 2.2f, SilkTint, .6f * (1 - fall / 14), 1, .3f, 0, 0, .4f);
            }
            Glow(body - new Vector2(0, 30 + (fall > 0 ? 26 : 0)), 255 + (fall > 0 ? 60 : 0), Candle, .22f);
            Sprite(wide, new Rectangle(0, 0, wide.Width, wide.Height), body, new(149.1f, 198.1f), .75f, .02f, false, new Vector4(1, 1, fall > 0 ? .35f * Pulse(fall, 6) : 0, .3f));
        }
        else
        {
            float diameter = 270 * 2 / .96f, live = 1 - burst / 10;
            Glow(impact, diameter, Pale, live * .8f, 0, .1f, .35f); Glow(impact, diameter, Dusty, live, .96f, .025f, .25f);
            Glow(impact, 620, Pale, (Reduced ? .35f : .65f) * MathF.Exp(-burst / 6));
            Glow(impact, 200 + burst * 14, Gold, .3f * (1 - burst / 46), .93f, .06f, .6f);
            Burst(impact, 1717, 30, burst, 80, 0, 9, .34f, 20, MathF.PI * 1.25f, -MathF.PI / 2);
        }
        Player(new(560, 640)); Player(new(980, 640));
    }

    static IEnumerable<(Vector2 A, Vector2 B)> LoomLines()
    {
        float angle = -.64f; var d = new Vector2(MathF.Cos(angle), MathF.Sin(angle)); var n = new Vector2(-d.Y, d.X);
        for (int k = -4; k <= 4; k++)
            if (Clip(new Vector2(640, 360) + n * (k * 300 * .55f + 40), d, out var a, out var b)) yield return (a, b);
    }
    // age: ticks since the loom was called (warning 0..86); live: ticks since fire; fray: ticks since End.
    static void Loom(float age, float live, float fray = -1)
    {
        Hall(1, 1, 1, 1, 0, 0);
        const float warning = 86;
        float progress = Math.Clamp(age / warning, 0, 1), appear = Ease(age / 8), strung = OutExpo(age / 14);
        int k = 0;
        foreach (var (a, b) in LoomLines())
        {
            float seed = k++ * .37f, rotation = MathF.Atan2(b.Y - a.Y, b.X - a.X);
            if (fray >= 0)
            {
                var (f, recoil, sag) = Release(fray);
                var mid = Vector2.Lerp(a, b, .5f + (k % 3 - 1) * .1f);
                Frayed(Droop(a, Vector2.Lerp(mid, a, recoil), sag), 3, f, 3000 + k * 7, false, true);
                Frayed(Droop(b, Vector2.Lerp(mid, b, recoil), sag), 3, f, 3001 + k * 7, false, true);
            }
            else if (live < 0)
            {
                Hairline(a, Vector2.Lerp(a, b, strung), 14, progress, appear, seed, strung, 3.5f * (1 - progress) + .3f);
                float glint = (.3f + .4f * progress) * appear;
                Glint(a, 20, SilkTint, glint, .8f, rotation); if (age >= 14) Glint(b, 20, SilkTint, glint, .8f, rotation);
            }
            else
            {
                Razor(a, b, 14, 1, live, seed, 4.5f * MathF.Exp(-live / 3.5f), 1.9f);
                Glint(a, 34, Bloom, 1 - live / 12, .8f, rotation); Glint(b, 34, Bloom, 1 - live / 12, .8f, rotation);
            }
        }
        Player(new(700, 420));
    }

    static void Shears(float progress, float live, float heal = -1)
    {
        Hall(1, 1, 1, 1, 0, 0);
        Vector2 a = new(Field.X, 430), b = new(Field.Z, 430); float rotation = 0;
        Vector2 pivot; float open, opacity = 1;
        if (heal >= 0)
        {
            // EbonScene.Aftermath ShearsHeal: a faint dusty seam closing and dissolving.
            if (heal < 16) Veil(a, b, MathHelper.Lerp(74 * .3f, 5, OutExpo(heal / 8)), 1, 1.1f * (1 - Ease(heal / 16)), .2f, 1);
            Player(new(820, 410));
            return;
        }
        if (live < 0)
        {
            pivot = a + new Vector2(190, 0); open = .42f + .22f * progress;
            Veil(a, b, 74, progress, 1, .2f);
            Hairline(a, b, 3, progress, .7f, .5f);
            Straight(new(pivot.X, -200), pivot, 2.2f, SilkTint, .55f, progress, 0);
        }
        else
        {
            float run = OutExpo(live / 14); pivot = Vector2.Lerp(a + new Vector2(190, 0), b - new Vector2(60, 0), run); open = .55f * MathF.Abs(MathF.Cos(live * .75f));
            Tear(a, b, 74, live, 14, 1, .2f, Vector2.Dot(pivot - a, Vector2.UnitX));
            opacity = 1 - Ease((live - 14 + 4) / 4);
        }
        Player(new(820, 410));
        var sig = new Vector4(opacity, 1, live >= 0 ? .4f : .2f * progress, .4f);
        Sprite(shears, new Rectangle(0, 0, 360, 140), pivot, new(159, 21.5f), .92f, rotation - open, false, sig);
        Sprite(shears, new Rectangle(0, 140, 360, 140), pivot, new(137.7f, 121.8f), .92f, rotation + open, false, sig);
    }

    // age: ticks since the waltz was called (warning 0..115); live: ticks since fire; release: ticks since End.
    // Warning and live use the same plan (six spokes, same base angle), so a
    // warning frame and the fire frame line up spoke for spoke.
    static void Waltz(float age, float live, float release = -1)
    {
        Hall(1, 1, 1, 1, 0, 0);
        Vector2 c = new(640, 300); const int count = 6; float sign = 1, spin = .012f;
        const float warning = 115.2f, beatTicks = 28.8f;
        float progress = Math.Clamp(age / warning, 0, 1), appear = Ease(age / 8), extend = OutExpo(age / 24), within = age / beatTicks % 1;
        float turn = live > 0 ? spin * (live + .7f * beatTicks / MathF.Tau * MathF.Sin(MathF.Tau * live / beatTicks)) : 0;
        float rate = live > 0 ? 1 + .7f * MathF.Cos(MathF.Tau * live / beatTicks) : 0;
        float beat = live >= 0 ? live % beatTicks : 0, step = Pulse(beat, 7);
        float flash = live < beatTicks ? 1 : Reduced ? .25f : .5f;
        Glow(c, 150, Moon, .22f + .18f * (live >= 0 ? 1 : progress));
        for (int k = 0; k < count; k++)
        {
            float a = .3f + MathF.Tau * k / count + turn; var d = new Vector2(MathF.Cos(a), MathF.Sin(a));
            float reach = Reach(c, d);
            if (release >= 0)
            {
                var (f, recoil, sag) = Release(release);
                Frayed(Droop(c + d * reach, Vector2.Lerp(c + d * 40, c + d * reach, recoil), sag), 3, f, 1000 + k, false, true);
            }
            else if (live < 0)
            {
                Sweep(c, a, a + sign * .24f * OutExpo(within / .6f), 40, reach, SilkTint, .18f * (1 - within) * (1 - within) * extend, k * .3f, .6f);
                Hairline(c + d * 40, c + d * (40 + (reach - 40) * extend), 13, progress, appear, k * .37f, extend, 1.5f * (1 - progress));
                // Body: a small glint slides along the wall the way the spokes will turn.
                float lean = sign * .24f * OutExpo(within / .6f), la = a + lean; var ld = new Vector2(MathF.Cos(la), MathF.Sin(la));
                Glint(c + ld * Reach(c, ld), 44, Pale, (1 - within) * (1 - within) * extend, 1, la);
            }
            else
            {
                Sweep(c, a, a - sign * spin * rate * 16, 40, reach, Dusty, .12f, k * .3f);
                Razor(c + d * 40, c + d * reach, 13, 1, beat, k * .37f, 1.6f * step, 2.6f, flash, openStart: true);
                Glint(c + d * reach, 34, Bloom, .85f, .8f, a);
                if (!Reduced) Burst(c + d * reach, 1000 + k * 31 + (int)(live / 18) * 7, 4, live % 18, 18, 2, 3.5f, .03f, 12, 1.4f, a + MathF.PI);
            }
        }
        Body(c, 6, 1, 1);
        Player(new(900, 560));
    }

    static readonly (Vector2 A, Vector2 B)[] WebLines =
    {
        (new(80, 120), new(1200, 520)), (new(240, 30), new(980, 700)), (new(1200, 90), new(300, 700)), (new(80, 610), new(1200, 260)),
        (new(560, 30), new(80, 520)), (new(1200, 640), new(700, 30)), (new(400, 700), new(1200, 380)), (new(860, 700), new(80, 330)),
    };
    // age: ticks since the first strand (one strand per sixteenth, 7.2 ticks); fire at 115; live: ticks since fire; fray: ticks since End.
    static void Web(float age, float live, float fray = -1)
    {
        Hall(1, 1, 1, 1, 0, 0);
        Vector2 hand = new(626, 288);
        float fire = 115.2f;
        var shown = new List<(Vector2 A, Vector2 B, float Show)>();
        for (int i = 0; i < WebLines.Length; i++)
        {
            var (a, b) = WebLines[i];
            float born = i * 7.2f, t0 = (live >= 0 ? fire + live : age) - born;
            if (fray < 0 && t0 < 0) continue;
            float progress = Math.Clamp(t0 / (fire - born), 0, 1), appear = Ease(t0 / 8), extend = OutExpo(t0 / 8), seed = i * .41f, rotation = MathF.Atan2(b.Y - a.Y, b.X - a.X);
            if (fray >= 0)
            {
                var (f, recoil, sag) = Release(fray);
                var mid = Vector2.Lerp(a, b, .5f + (i % 3 - 1) * .1f);
                Frayed(Droop(a, Vector2.Lerp(mid, a, recoil), sag), 3, f, 2000 + i * 2, false, true);
                Frayed(Droop(b, Vector2.Lerp(mid, b, recoil), sag), 3, f, 2001 + i * 2, false, true);
                continue;
            }
            // Flung from her hand to the first anchor: harmless classic silk.
            if (t0 < 10) Straight(hand, Vector2.Lerp(hand, a, OutExpo(t0 / 6)), 2.2f, SilkTint, .55f * (1 - t0 / 10), 1, .2f, 0, 0, seed);
            if (live < 0)
            {
                Hairline(a, Vector2.Lerp(a, b, extend), 11, progress, appear, seed, extend, 2.5f * (1 - progress) + .3f);
                float glint = .3f + .4f * progress;
                Glint(a, 18, SilkTint, glint, .8f, rotation); if (t0 >= 8) Glint(b, 18, SilkTint, glint, .8f, rotation);
            }
            else
            {
                Razor(a, b, 11, 1, live, seed, 3.5f * Pulse(live, 3), 2.2f);
                Glint(a, 30, Bloom, 1 - live / 12, .8f, rotation); Glint(b, 30, Bloom, 1 - live / 12, .8f, rotation);
            }
            if (t0 >= 8) shown.Add((a, b, Ease((t0 - 8) / 10)));
        }
        float knotLive = live >= 0 ? 1 - live / 12 : 0;
        if (fray < 0)
            for (int i = 0; i < shown.Count; i++)
                for (int j = i + 1; j < shown.Count; j++)
                {
                    var (a, b, sa) = shown[i]; var (c, d, sb) = shown[j];
                    Vector2 r = b - a, q = d - c; float den = r.X * q.Y - r.Y * q.X; if (MathF.Abs(den) < 1e-3f) continue;
                    float t = ((c.X - a.X) * q.Y - (c.Y - a.Y) * q.X) / den, u = ((c.X - a.X) * r.Y - (c.Y - a.Y) * r.X) / den;
                    if (t is < 0 or > 1 || u is < 0 or > 1) continue;
                    float show = Math.Min(sa, sb), bisector = (MathF.Atan2(r.Y, r.X) + MathF.Atan2(q.Y, q.X)) * .5f;
                    if (knotLive > 0) Glint(a + r * t, 26 + 12 * knotLive, Bloom, show * knotLive, 1, bisector);
                    else Glint(a + r * t, 14, SilkTint, show * .3f, .5f, bisector);
                }
        Body(new(640, 300), live >= 0 ? 5 : 4, 1, 1);
        Player(new(820, 470)); Player(new(330, 380));
    }
    static void Stitch(bool spread)
    {
        Hall(1, 1, 1, 1, 0, 0);
        var players = new[] { new Vector2(480, 520), new Vector2(640, 560), new Vector2(900, 500) };
        if (!spread)
        {
            Vector2 c = new(640, 540); Ring(c, 190, false, .35f, Gold, 1);
            for (int k = 0; k < 4; k++) { float a = MathF.PI / 4 + k * MathF.PI / 2; var d = new Vector2(MathF.Cos(a), MathF.Sin(a)); Shard(c + d * 212, 34, 7, a + MathF.PI, 2, .9f); }
            foreach (var p in players)
            {
                Straight(p, c, 1.8f, Gold, .38f, .35f, 0, 2, .2f, .3f);
                for (int r = 0; r < 2; r++) { var loop = new List<Vector2>(); float radius = (30 + r * 10) * .88f; for (int k = 0; k <= 24; k++) { float a = k / 24f * MathF.Tau + r; loop.Add(p + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius * .42f + (r - .5f) * 16)); } Thread(loop, 2.2f, Gold, .75f, .35f, 0, r * .5f); }
                Player(p);
            }
        }
        else foreach (var p in players)
            {
                Ring(p, 352 * .55f, true, .6f, Rose, .9f);
                for (int k = 0; k < 4; k++) { float a = MathF.PI / 4 + k * MathF.PI / 2; var d = new Vector2(MathF.Cos(a), MathF.Sin(a)); Shard(p + d * (352 * .55f + 26), 34, 7, a, 2, .9f); }
                Player(p);
            }
    }
    // Noirette at the production 2x scale with the aura pass behind her.
    static void Body(Vector2 root, int frameIndex, float weave, float opacity, bool aura = true)
    {
        if (aura)
        {
            Set(manor, "signal", new Vector4(opacity * (.55f + .45f * weave), .55f, 0, .2f));
            Quad("AuraPass", null, root + new Vector2(0, -6), new Vector2(210, 230), 0);
        }
        var src = new Rectangle(frameIndex % 4 * 48, frameIndex / 4 * 64, 48, 64);
        Set(manor, "region", new Vector4(src.X / (float)noirette.Width, src.Y / (float)noirette.Height, 48f / noirette.Width, 64f / noirette.Height));
        Set(manor, "signal", new Vector4(opacity, weave, 0, .29f)); Set(manor, "shape", new Vector4(0, .30f, 48, 64)); Set(manor, "weaveDensity", 9f);
        Pass("BodyPass", noirette, SamplerState.PointClamp);
        Vector2 o = new(26, 33); Vector2 C(float u, float v) => root + (new Vector2(u * 48, v * 64) - o) * 2;
        Submit(C(0, 0), C(1, 0), C(0, 1), C(1, 1));
    }
    static void Poses()
    {
        Hall(1, 1, 1, 0, 0, 0, false);
        for (int i = 0; i < 8; i++) Body(new(110 + i * 150, 330), i, 1, 1);
        Set(manor, "signal", new Vector4(1, 1, 0, 7));
        Sprite(invitation, new Rectangle(0, 0, 44, 44), new(640, 560), new(22, 22), 2, -.16f, false, new Vector4(1, 1, 0, 7), 0, SamplerState.PointClamp, 1, 10);
    }
    static void WeaveSequence()
    {
        Hall(1, 1, 1, 0, 0, 0, false);
        float[] steps = { .12f, .3f, .5f, .7f, .88f, 1 };
        for (int i = 0; i < steps.Length; i++) Body(new(160 + i * 190, 300), 2, steps[i], 1);
        for (int i = 0; i < 4; i++) Sprite(props, new Rectangle(i * 176, 0, 176, 176), new(220 + i * 280, 560), new(88, 90), .75f, 0, false, new Vector4(1, .2f + i * .26f, 0, i));
    }
}
