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
        Chalk = new(.84f, .80f, .72f), Gold = new(.90f, .72f, .44f), Candle = new(1f, .70f, .38f), Hot = new(1f, .84f, .88f);

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
                ("throw-01-warning", () => Throw(.25f, false, 0)),
                ("throw-02-tight", () => Throw(.92f, false, 0)),
                ("throw-03-flight", () => Throw(1, true, 14)),
                ("chandelier-01-warning", () => Chandelier(.35f, 0, 0)),
                ("chandelier-02-fall", () => Chandelier(1, 8, 0)),
                ("chandelier-03-burst", () => Chandelier(1, 0, 3)),
                ("loom-01-warning", () => Loom(.45f, -1)),
                ("loom-02-live", () => Loom(1, 2)),
                ("shears-01-warning", () => Shears(.6f, -1)),
                ("shears-02-live", () => Shears(1, 2)),
                ("shears-03-live-late", () => Shears(1, 9)),
                ("waltz-01-warning", () => Waltz(.5f, false)),
                ("waltz-02-live", () => Waltz(1, true)),
                ("stitch-01-stack", () => Stitch(false)),
                ("stitch-02-spread", () => Stitch(true)),
                ("noirette-01-poses", () => Poses()),
                ("noirette-02-weave", () => WeaveSequence()),
            };
            foreach (var (name, draw) in scenes)
            {
                device.SetRenderTarget(target); device.Clear(new Color(8, 8, 12)); clock = 3.7f;
                draw();
                device.SetRenderTarget(null);
                using var file = File.Create(Path.Combine(output, name + ".png")); target.SaveAsPng(file, W, H);
            }
        }
        finally { device?.Dispose(); SDL_DestroyWindow(window); SDL_Quit(); }
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
        Set(manor, "uWorldViewProjection", screen); Set(manor, "clock", clock);
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
    static void Lane(Vector2 a, Vector2 b, float radius, float progress, float live, Vector3 tint, float opacity)
    {
        float length = Vector2.Distance(a, b);
        Set(manor, "shape", new Vector4(length, radius, progress, live)); Set(manor, "signal", new Vector4(opacity, 1, 0, a.X * .001f)); Set(manor, "tint", tint);
        var d = b - a; Quad("LanePass", null, (a + b) * .5f, new Vector2(length, radius * 2), MathF.Atan2(d.Y, d.X));
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
    static void Shard(Vector2 c, float length, float width, float rotation, int kind, float opacity)
    {
        Set(manor, "shape", new Vector4(kind, 0, 0, 0)); Set(manor, "signal", new Vector4(opacity, .5f, 0, 0));
        Quad("ShardPass", null, c, new Vector2(length, width), rotation);
    }
    static void Thread(IReadOnlyList<Vector2> points, float half, Vector3 tint, float opacity, float tension, float heat, bool taper = false)
    {
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
        Set(silk, "signal", new Vector4(opacity, tension, heat, .3f)); Set(silk, "tint", tint); Set(silk, "footprint", new Vector2(Math.Max(1, length), half));
        silk.CurrentTechnique.Passes[0].Apply(); device.Textures[1] = wavy; device.SamplerStates[1] = SamplerState.LinearWrap;
        var array = strip.ToArray(); device.DrawUserPrimitives(PrimitiveType.TriangleList, array, 0, array.Length / 3);
    }
    static void Straight(Vector2 a, Vector2 b, float half, Vector3 tint, float opacity, float tension, float heat, float amplitude = 0, float frequency = 0)
    {
        var line = new List<Vector2>(); int steps = amplitude > .05f ? 24 : 2; var n = Vector2.Normalize(b - a); n = new(-n.Y, n.X);
        for (int i = 0; i <= steps; i++) { float u = i / (float)steps; line.Add(Vector2.Lerp(a, b, u) + n * MathF.Sin(u * MathF.PI) * MathF.Sin(u * MathF.PI * 3 + clock * 60 * frequency) * amplitude); }
        Thread(line, half, tint, opacity, tension, heat);
    }

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

    static void Throw(float progress, bool live, float t)
    {
        Hall(1, 1, 1, 0, 0, 0);
        Vector2 origin = new(140, 250), target = new(1150, 560); var d = Vector2.Normalize(target - origin); Vector2 to = origin + d * 1180;
        float travel = live ? 9 * t + .5f * 1.15f * t * t : 0; Vector2 prop = origin + d * travel + (live ? Vector2.Zero : new Vector2(0, -26 * progress));
        Lane(live ? prop : origin, to, 46, progress, 0, Chalk, live ? .55f : .95f);
        Player(new(900, 470));
        var hook = prop + new Vector2(0, -60); var spine = prop + d * 90;
        var tie = new List<Vector2> { hook, Vector2.Lerp(hook, spine, .5f), spine, to };
        if (!live) { Thread(tie, 2.2f, SilkTint, .35f, progress, 0); Straight(spine, to, 2.4f, SilkTint, .45f, progress, 0, 5 * (1 - progress) + .6f, .12f + .4f * progress); Straight(new(640, 330), hook, 2.8f, SilkTint, .72f, progress, 0); }
        else
        {
            Thread(tie, 3f, Hot, .6f, 1, .5f); Straight(spine, to, 3.4f, Hot, .8f, 1, .7f, 2.5f * MathF.Exp(-t / 5), 1.4f);
            var trail = new List<Vector2>(); for (int k = 8; k >= 0; k--) { float tk = Math.Max(0, t - k * 1.4f); trail.Add(origin + d * (9 * tk + .5f * 1.15f * tk * tk)); }
            Thread(trail, 26, SilkTint, .32f, 1, .5f, true);
        }
        Sprite(props, new Rectangle(0, 0, 176, 176), prop, new(88, 90), .75f, live ? travel * .0045f : .04f, false, new Vector4(1, live ? 1 : Math.Min(1, progress * 2.5f), live ? .3f : .25f * progress * progress, 0));
    }
    static void Chandelier(float progress, float fall, float burst)
    {
        Hall(1, 1, 1, 0, 0, 0);
        float x = 640, anchor = 40, floor = 700; Vector2 impact = new(x, floor - 30);
        float drop = .5f * 3.1f * fall * fall; Vector2 body = new(x, anchor + 150 + drop);
        if (burst <= 0)
        {
            Lane(fall > 0 ? body : new Vector2(x, anchor + 150), impact, 118, progress, 0, Rose, .75f);
            float diameter = 270 * 2 / .96f;
            Glow(impact, diameter, Rose, .2f * progress, 0, .1f, .2f); Glow(impact, diameter, Rose, .55f + .35f * progress, .96f, .012f, .4f);
            Straight(new(x - 3, -200), body - new Vector2(0, (198 - 10) * .75f), 2.6f, SilkTint, fall > 0 ? 0 : .75f, progress, 0);
            Glow(body - new Vector2(0, 30), 255, Candle, .22f);
            Sprite(wide, new Rectangle(0, 0, wide.Width, wide.Height), body, new(149.1f, 198.1f), .75f, .02f, false, new Vector4(1, 1, fall > 0 ? .2f : 0, .3f));
        }
        else
        {
            float diameter = 270 * 2 / .96f, live = 1 - burst / 10;
            Glow(impact, diameter, Hot, live * .85f, 0, .1f, .35f); Glow(impact, diameter, Hot, live, .96f, .02f, .2f);
            Glow(impact, 620, Hot, .9f * MathF.Exp(-burst / 6));
            for (int i = 0; i < 24; i++) { float a = -MathF.PI / 2 + (i / 24f - .5f) * 3.9f, v = 6 + i % 5; var p = impact + new Vector2(MathF.Cos(a), MathF.Sin(a)) * v * 14 * (1 - MathF.Exp(-burst / 14)) + new Vector2(0, .17f * burst * burst); Shard(p, 22, 6, a + burst * .2f, 0, .9f); }
        }
        Player(new(560, 640)); Player(new(980, 640));
    }
    static void Loom(float progress, float live)
    {
        Hall(1, 1, 1, 1, 0, 0);
        float angle = -.64f; var d = new Vector2(MathF.Cos(angle), MathF.Sin(angle)); var n = new Vector2(-d.Y, d.X);
        for (int k = -4; k <= 4; k++)
        {
            var through = new Vector2(640, 360) + n * (k * 300 * .55f + 40);
            var a = through - d * 900; var b = through + d * 900;
            if (live < 0) { Lane(a, b, 14, progress, 0, SilkTint, .8f); Straight(a, b, 3, SilkTint, .65f, progress, 0, 7 * (1 - progress) + .8f, .08f + .45f * progress); }
            else { Lane(a, b, 14, 1, 1, SilkTint, 1); Straight(a, b, 6, Hot, 1, 1, 1, 4.5f * MathF.Exp(-live / 3.5f), 1.9f); }
        }
        Player(new(700, 420));
    }
    static void Shears(float progress, float live)
    {
        Hall(1, 1, 1, 1, 0, 0);
        Vector2 a = new(0, 430), b = new(W, 430); float rotation = 0;
        Vector2 pivot; float open;
        if (live < 0)
        {
            pivot = a + new Vector2(190, 0); open = .42f + .22f * progress;
            Lane(a, b, 74, progress, 0, Chalk, .95f);
            Straight(new(pivot.X, -200), pivot, 2.2f, SilkTint, .55f, progress, 0);
        }
        else
        {
            float run = 1 - MathF.Pow(2, -10 * live / 14); pivot = Vector2.Lerp(a + new Vector2(190, 0), b - new Vector2(60, 0), run); open = .55f * MathF.Abs(MathF.Cos(live * .75f));
            Set(manor, "shape", new Vector4(W, 74, live, 14)); Set(manor, "signal", new Vector4(1, 0, 0, .2f));
            Quad("TearPass", null, (a + b) * .5f, new Vector2(W, 148), 0);
        }
        Player(new(820, 410));
        var sig = new Vector4(1, 1, live >= 0 ? .4f : .2f * progress, .4f);
        Sprite(shears, new Rectangle(0, 0, 360, 140), pivot, new(159, 21.5f), .92f, rotation - open, false, sig);
        Sprite(shears, new Rectangle(0, 140, 360, 140), pivot, new(137.7f, 121.8f), .92f, rotation + open, false, sig);
    }
    static void Waltz(float progress, bool live)
    {
        Hall(1, 1, 1, 1, 0, 0);
        Vector2 c = new(640, 300); int count = live ? 8 : 6; float spin = live ? .5f : 0;
        for (int k = 0; k < count; k++)
        {
            float a = .3f + MathF.Tau * k / count + spin; var d = new Vector2(MathF.Cos(a), MathF.Sin(a));
            var end = c + d * 900;
            Lane(c + d * 40, end, 13, live ? 1 : progress, live ? 1 : 0, SilkTint, live ? 1 : .85f);
            Straight(c + d * 6, c + d * 40, 1.8f, SilkTint, .6f, 1, 0);
            if (live) Straight(c + d * 40, end, 4.5f, Hot, 1, 1, 1); else Straight(c + d * 40, end, 2.2f, SilkTint, .45f, progress, 0, 1.5f * (1 - progress), .3f);
        }
        Glow(c, 120, Moon, .4f);
        Body(c, 6, 1, 1);
        Player(new(900, 560));
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
                Straight(p, c, 1.8f, Gold, .38f, .35f, 0, 2, .2f);
                for (int r = 0; r < 2; r++) { var loop = new List<Vector2>(); float radius = (30 + r * 10) * .88f; for (int k = 0; k <= 24; k++) { float a = k / 24f * MathF.Tau + r; loop.Add(p + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius * .42f + (r - .5f) * 16)); } Thread(loop, 2.2f, Gold, .75f, .35f, 0); }
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
