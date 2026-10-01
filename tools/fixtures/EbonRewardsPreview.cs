// Offline FNA preview of the Ebon reward pixel layer. Links the production EbonPixelLayer contract and
// EbonPixelArt (canvas, composite, device state) with the compiled EbonPixel.fxc; no Terraria types.
// Each frame follows the in-game order: record primitives, draw the half-resolution layer behind a
// captured device state, then composite once with the outline/glow over a stand-in backdrop.
// Also runs pixel checks (exact line thickness, unbroken chalk, budgets, state restore).
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Convergence.Client.Encounters.EbonManor.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal static class EbonRewardsPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] private static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_Quit();

    private const int Width = 1280, Height = 720;
    private static readonly Color Dark = new(18, 16, 28), Bright = new(186, 198, 214);
    private static GraphicsDevice device;
    private static Effect effect;
    private static Texture2D noise, pixel;
    private static SpriteBatch batch;
    private static RenderTarget2D layer, frame;
    private static readonly EbonPixelCanvas canvas = new();
    private static readonly List<string> failures = new();
    private static string output;

    private static int Main(string[] args)
    {
        string root = args[0], native = args[1];
        output = args[2];
        Directory.CreateDirectory(output);
        IntPtr Resolve(string name, System.Reflection.Assembly assembly, DllImportSearchPath? path)
        {
            string file = Path.Combine(native, name.EndsWith(".dll") ? name : name + ".dll");
            return File.Exists(file) ? NativeLibrary.Load(file) : IntPtr.Zero;
        }
        NativeLibrary.SetDllImportResolver(typeof(EbonRewardsPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL initialization failed");
        IntPtr window = SDL_CreateWindow("Offline Ebon pixel layer", 0, 0, Width, Height, FNA3D_PrepareWindowAttributes() | 0x8);
        if (window == IntPtr.Zero) throw new Exception("Hidden device unavailable");
        int frames = 0;
        try
        {
            using var graphics = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, new PresentationParameters
            {
                DeviceWindowHandle = window, BackBufferWidth = Width, BackBufferHeight = Height, BackBufferFormat = SurfaceFormat.Color,
                IsFullScreen = false, DepthStencilFormat = DepthFormat.None, PresentationInterval = PresentInterval.Immediate,
            });
            device = graphics;
            using var material = new Effect(device, File.ReadAllBytes(Path.Combine(root, "Assets/AutoloadedEffects/Shaders/EbonPixel.fxc")));
            effect = material;
            using (var stream = File.OpenRead(Path.Combine(root, "Assets/Textures/Items/DXOboro/SlashNoise.png"))) noise = Texture2D.FromStream(device, stream);
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            batch = new SpriteBatch(device);
            layer = new RenderTarget2D(device, Width / 2, Height / 2, false, SurfaceFormat.Color, DepthFormat.None);
            frame = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);

            Checks();
            foreach (bool bright in new[] { false, true })
            foreach (bool reduced in new[] { false, true })
            {
                string name = $"sheet-{(bright ? "bright" : "dark")}-{(reduced ? "reduced" : "normal")}";
                Frame(Sheet, bright, reduced, 40, Matrix.Identity, name, !bright && !reduced);
                frames++;
            }
            // Terraria's GameViewMatrix zoom scales about the screen centre.
            Matrix zoom = Matrix.CreateTranslation(-Width / 2f, -Height / 2f, 0) * Matrix.CreateScale(1.5f) * Matrix.CreateTranslation(Width / 2f, Height / 2f, 0);
            Frame(Sheet, false, false, 40, zoom, "sheet-dark-zoom150", false);
            frames++;
            for (int i = 0; i < 128; i++)
            {
                float t = i * .5f;
                Frame(c => Sequence(c, t), false, false, t, Matrix.Identity, $"seq-dark-normal-{i:D3}", false);
                frames++;
                if (i % 4 != 0) continue;
                Frame(c => Sequence(c, t), true, false, t, Matrix.Identity, $"seq-bright-normal-{i:D3}", false);
                Frame(c => Sequence(c, t), false, true, t, Matrix.Identity, $"seq-dark-reduced-{i:D3}", false);
                frames += 2;
            }
            batch.Dispose(); pixel.Dispose(); noise.Dispose(); layer.Dispose(); frame.Dispose();
        }
        finally
        {
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        Console.WriteLine(failures.Count == 0
            ? $"PASS {frames} offline Ebon pixel frames in {output}; pixel checks passed. Offline only; no native game, zoom UI or peer acceptance."
            : $"{failures.Count} check(s) failed; {frames} frames written to {output}.");
        return failures.Count == 0 ? 0 : 1;
    }

    private static void Frame(Action<EbonPixelCanvas> scene, bool bright, bool reduced, double ticks, Matrix view, string name, bool raw)
    {
        canvas.Begin(Vector2.Zero, layer.Width, layer.Height, 1f, reduced, ticks);
        scene(canvas);
        if (canvas.Dropped > 0) failures.Add($"{name}: {canvas.Dropped} primitive(s) over budget");
        // As in game, the layer is drawn before the world (switching targets discards the caller's contents).
        device.SetRenderTarget(frame);
        DrawLayer();
        if (device.GetRenderTargets().Length != 1 || device.GetRenderTargets()[0].RenderTarget != frame)
            failures.Add($"{name}: device state did not restore the caller's render target");
        device.Clear(bright ? Bright : Dark);
        Backdrop(bright, view);
        Rectangle dots = canvas.DotBounds;
        if (!canvas.Empty && !dots.IsEmpty)
        {
            EbonDeviceState state = EbonDeviceState.Capture(device, false);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            Rectangle screen = Rectangle.Intersect(new Rectangle(dots.X * 2, dots.Y * 2, dots.Width * 2, dots.Height * 2), new Rectangle(0, 0, Width, Height));
            EbonPixelArt.Composite(device, effect, layer, screen, view, reduced);
            state.Restore(device);
        }
        device.SetRenderTarget(null);
        using (var file = File.Create(Path.Combine(output, name + ".png"))) frame.SaveAsPng(file, Width, Height);
        if (raw)
            using (var file = File.Create(Path.Combine(output, name + "-layer.png"))) layer.SaveAsPng(file, layer.Width, layer.Height);
    }

    private static void DrawLayer()
    {
        EbonDeviceState state = EbonDeviceState.Capture(device, true);
        device.SetRenderTarget(layer);
        device.Clear(Color.Transparent);
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        canvas.Draw(device, effect, noise, layer.Width, layer.Height);
        state.Restore(device);
    }

    // Stand-in ground, wall blocks and a figure so outline readability shows on mixed values.
    private static void Backdrop(bool bright, Matrix view)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, view);
        Color ground = bright ? new Color(120, 98, 78) : new Color(48, 40, 52);
        batch.Draw(pixel, new Rectangle(0, 640, Width, 80), ground);
        batch.Draw(pixel, new Rectangle(900, 120, 140, 140), bright ? new Color(236, 238, 242) : new Color(92, 84, 104));
        batch.Draw(pixel, new Rectangle(140, 420, 120, 90), bright ? new Color(150, 110, 80) : new Color(70, 52, 60));
        batch.Draw(pixel, new Rectangle(612, 520, 16, 42), new Color(40, 46, 61));
        batch.Draw(pixel, new Rectangle(613, 504, 14, 14), new Color(188, 175, 163));
        batch.End();
    }

    // Every primitive at representative settings.
    private static void Sheet(EbonPixelCanvas c)
    {
        float[] fades = { 0f, 0f, 0f, .35f, .7f };
        float[] heats = { 0f, .55f, 1f, .2f, .4f };
        for (int i = 0; i < 5; i++)
        {
            Vector2 root = new(130 + i * 250, 190);
            c.Crescent(root, -2.75f, -.35f, 62f, 118f, fades[i], heats[i], 11 + i);
        }
        c.Band(new Vector2(40, 290), new Vector2(600, 270), 10f, EbonBandMode.Chalk, 1f, 1f, 3);
        c.Band(new Vector2(40, 360), new Vector2(600, 360), 64f, EbonBandMode.Chalk, .7f, 1f, 4);
        c.Band(new Vector2(680, 290), new Vector2(1240, 290), 64f, EbonBandMode.Tear, 1f, 1f, 5);
        c.Band(new Vector2(680, 380), new Vector2(1240, 410), 24f, EbonBandMode.Tear, .6f, 1f, 6);
        c.Thread(new Vector2(40, 440), new Vector2(380, 440), EbonTone.Silver, 1, 1f, 0f);
        c.Thread(new Vector2(40, 452), new Vector2(380, 500), EbonTone.Ivory, 1, 1f, .5f);
        c.Thread(new Vector2(40, 470), new Vector2(240, 600), EbonTone.Rose, 2, 1f, 0f);
        c.Thread(new Vector2(260, 600), new Vector2(380, 520), EbonTone.Gold, 3, .8f, 1f);
        c.String(new Vector2(420, 440), new Vector2(760, 440), EbonTone.Moon, 14f, 0f, 1, 1f);
        c.String(new Vector2(420, 480), new Vector2(760, 520), EbonTone.Silver, 10f, 1.1f, 1, 1f);
        c.String(new Vector2(420, 600), new Vector2(560, 470), EbonTone.Ivory, 12f, .4f, 2, .7f);
        EbonShardKind[] kinds = { EbonShardKind.Glass, EbonShardKind.Wood, EbonShardKind.Silk, EbonShardKind.Spark, EbonShardKind.Lace };
        for (int k = 0; k < kinds.Length; k++)
            c.Burst(new Vector2(820 + k * 95, 560), 21 + k, 18, 9f, 30f, 3.6f, .16f, kinds[k]);
        for (int s = 0; s < 4; s++) c.Star(new Vector2(70 + s * 70, 680), 30f, .08f + s * .24f, 31 + s);
        c.Ring(new Vector2(380, 670), 16f, EbonTone.Ivory, 1, 1f);
        c.Ring(new Vector2(460, 670), 34f, EbonTone.Rose, 2, 1f);
        c.Ring(new Vector2(560, 670), 46f, EbonTone.Candle, 1, .55f);
        for (int s = 0; s < 3; s++) c.Stitch(new Vector2(650 + s * 34, 676), 2 + s, s == 1 ? EbonTone.Rose : EbonTone.Ivory);
        for (int s = 0; s < 4; s++) c.Dot(new Vector2(780 + s * 16, 676), (EbonTone)(s + 4), 1 + s % 3);
    }

    // A short performance on one clock: a two-stroke kata, a chalk Cut Line that tears and pops
    // its marks, and a glissando over shimmering harp strings.
    private static void Sequence(EbonPixelCanvas c, float t)
    {
        int stroke = (int)(t / 32f);
        float local = t % 32f, direction = stroke % 2 == 0 ? 1f : -1f;
        Vector2 root = new(260, 330);
        float start = direction > 0 ? -2.9f : -.25f, sweep = 2.65f * direction;
        float swing = Ease(Math.Clamp((local - 2f) / 11f, 0f, 1f));
        float fade = Math.Clamp((local - 13f) / 10f, 0f, 1f);
        if (local >= 2f) c.Crescent(root, start, start + sweep * swing, 44f, 132f, fade, stroke % 2 == 0 ? .1f : .85f, stroke);
        float hitAngle = start + sweep * .72f;
        Vector2 hit = root + new Vector2(MathF.Cos(hitAngle), MathF.Sin(hitAngle)) * 118f;
        if (local >= 9f)
        {
            c.Star(hit, 26f, (local - 9f) / 7f, stroke);
            c.Burst(hit, stroke, 12, local - 9f, 26f, 3.4f, .18f, EbonShardKind.Glass, MathF.PI * 1.2f, hitAngle + MathF.PI / 2f * direction);
        }

        Vector2 from = new(470, 600), to = new(1180, 380);
        Vector2[] marks = { Vector2.Lerp(from, to, .3f) + new Vector2(0, -14), Vector2.Lerp(from, to, .55f) + new Vector2(0, 12), Vector2.Lerp(from, to, .8f) + new Vector2(0, -6) };
        if (t < 30f) c.Band(from, to, 18f, EbonBandMode.Chalk, Math.Clamp(t / 18f, .02f, 1f), 1f, 9);
        else if (t < 36f) c.Band(from, to, 18f, EbonBandMode.Chalk, 1f, 1f - (t - 30f) / 6f, 9);
        if (t >= 18f && t < 30f)
        {
            Vector2 race = Vector2.Lerp(from, to, Ease((t - 18f) / 12f));
            c.Star(race, 18f, .1f + (t % 3f) / 10f, 77);
        }
        if (t >= 30f) c.Band(from, to, 64f, EbonBandMode.Tear, Math.Clamp((t - 30f) / 10f, 0f, 1f), 1f - Math.Clamp((t - 48f) / 14f, 0f, 1f), 10);
        for (int m = 0; m < marks.Length; m++)
        {
            float pop = 42f + m * 4f;
            if (t < pop) c.Stitch(marks[m], 3, EbonTone.Rose);
            else if (t < pop + 12f)
            {
                c.Star(marks[m], 22f, (t - pop) / 12f, 40 + m);
                c.Burst(marks[m], 50 + m, 10, t - pop, 20f, 2.6f, .05f, m == 2 ? EbonShardKind.Lace : EbonShardKind.Silk);
            }
        }

        Vector2 bow = new(1080, 120);
        for (int s = 0; s < 6; s++)
        {
            Vector2 end = new(720 + s * 30, 230 + s * 22);
            float pluck = 20f + 7.2f * s, since = t - pluck;
            if (since < 0f) c.Thread(bow, end, EbonTone.Silver, 1, .85f, .35f);
            else if (since < 30f)
            {
                float amplitude = 12f * MathF.Exp(-since / 9f);
                c.String(bow, end, since < 6f ? EbonTone.Moon : EbonTone.Ivory, amplitude, since * 1.3f, since < 6f ? 2 : 1, 1f - since / 30f);
                if (since < 8f) c.Ring(Vector2.Lerp(bow, end, .5f), 8f + since * 3f, EbonTone.Ivory, 1, 1f - since / 8f);
            }
        }
    }

    private static float Ease(float x) => x < .5f ? 4f * x * x * x : 1f - MathF.Pow(-2f * x + 2f, 3f) / 2f;

    private static void Checks()
    {
        // Lines: exactly `thickness` dots on each major-axis step, at any angle, from any sub-dot start.
        (Vector2 a, Vector2 b)[] lines =
        {
            (new(100, 100), new(500, 100)), (new(101.3f, 103.9f), new(503.7f, 251.2f)), (new(100, 100), new(300, 300)),
            (new(400, 100), new(330, 380)), (new(250, 400), new(250, 120)), (new(600, 300), new(180, 210)),
        };
        foreach (var (a, b) in lines)
        for (int thickness = 1; thickness <= 3; thickness++)
        {
            canvas.Begin(Vector2.Zero, layer.Width, layer.Height, 1f, false, 0);
            canvas.Thread(a, b, EbonTone.Ivory, thickness);
            Color[] dots = Read();
            int ax = (int)MathF.Floor(a.X / 2), ay = (int)MathF.Floor(a.Y / 2), bx = (int)MathF.Floor(b.X / 2), by = (int)MathF.Floor(b.Y / 2);
            bool xMajor = Math.Abs(bx - ax) >= Math.Abs(by - ay);
            int from = xMajor ? Math.Min(ax, bx) : Math.Min(ay, by), to = xMajor ? Math.Max(ax, bx) : Math.Max(ay, by);
            int wrong = 0, outside = 0;
            for (int major = 0; major < (xMajor ? layer.Width : layer.Height); major++)
            {
                int drawn = 0;
                for (int minor = 0; minor < (xMajor ? layer.Height : layer.Width); minor++)
                    if (dots[xMajor ? minor * layer.Width + major : major * layer.Width + minor].A > 0) drawn++;
                if (major >= from && major <= to) { if (drawn != thickness) wrong++; }
                else if (drawn > 0) outside++;
            }
            if (wrong + outside > 0) failures.Add($"thread {a}->{b} x{thickness}: {wrong} steps not {thickness} dots, {outside} stray steps");
        }

        // Chalk is one unbroken stroke (thin) or an unbroken ruled lane (wide): the centre line never has a gap.
        foreach (float width in new[] { 8f, 64f })
        {
            canvas.Begin(Vector2.Zero, layer.Width, layer.Height, 1f, false, 0);
            canvas.Band(new Vector2(60, 300), new Vector2(1200, 420), width, EbonBandMode.Chalk, 1f, 1f, 3);
            Color[] chalk = Read();
            int gaps = 0;
            for (int x = 31; x < 599; x++)
            {
                int y = (int)MathF.Floor((300 + (x * 2 + 1 - 60) / 1140f * 120f) / 2f);
                if (chalk[y * layer.Width + x].A < 250) gaps++;
            }
            if (gaps > 0) failures.Add($"{width} px chalk centre line has {gaps} undrawn dots");
        }

        // Strings stay connected: every major-axis step along the chord holds at least one dot.
        foreach (var (a, b, amplitude, phase) in new[] { (new Vector2(100, 300), new Vector2(900, 300), 24f, 0f),
            (new Vector2(150, 120), new Vector2(700, 560), 30f, .7f), (new Vector2(500, 80), new Vector2(560, 640), -18f, 2.4f),
            (new Vector2(300, 300), new Vector2(400, 330), 40f, .2f) })
        {
            canvas.Begin(Vector2.Zero, layer.Width, layer.Height, 1f, false, 0);
            canvas.String(a, b, EbonTone.Ivory, amplitude, phase);
            Color[] dots = Read();
            int ax = (int)(a.X / 2), ay = (int)(a.Y / 2), bx = (int)(b.X / 2), by = (int)(b.Y / 2);
            bool xMajor = Math.Abs(bx - ax) >= Math.Abs(by - ay);
            int empty = 0;
            for (int major = xMajor ? Math.Min(ax, bx) : Math.Min(ay, by); major <= (xMajor ? Math.Max(ax, bx) : Math.Max(ay, by)); major++)
            {
                bool any = false;
                for (int minor = 0; minor < (xMajor ? layer.Height : layer.Width) && !any; minor++)
                    any = dots[xMajor ? minor * layer.Width + major : major * layer.Width + minor].A > 0;
                if (!any) empty++;
            }
            if (empty > 0) failures.Add($"string {a}->{b} amp {amplitude}: {empty} empty steps");
        }

        // Rings: every point of the ideal circle has a drawn dot within one dot.
        canvas.Begin(Vector2.Zero, layer.Width, layer.Height, 1f, false, 0);
        canvas.Ring(new Vector2(640, 360), 90f, EbonTone.Ivory, 1, 1f);
        Color[] ring = Read();
        int misses = 0;
        for (int n = 0; n < 360; n++)
        {
            float angle = n * MathF.Tau / 360f;
            int cx = 320 + (int)MathF.Floor(MathF.Cos(angle) * 45f + .5f), cy = 180 + (int)MathF.Floor(MathF.Sin(angle) * 45f + .5f);
            bool near = false;
            for (int dy = -1; dy <= 1 && !near; dy++)
                for (int dx = -1; dx <= 1 && !near; dx++)
                    near = ring[(cy + dy) * layer.Width + cx + dx].A > 0;
            if (!near) misses++;
        }
        if (misses > 0) failures.Add($"ring misses {misses} of 360 sampled angles");

        // The layer restores the backbuffer as well as a bound target.
        device.SetRenderTarget(null);
        canvas.Begin(Vector2.Zero, layer.Width, layer.Height, 1f, false, 0);
        canvas.Dot(new Vector2(10, 10), EbonTone.Moon);
        DrawLayer();
        if (device.GetRenderTargets().Length != 0) failures.Add("device state did not restore the backbuffer");

        // Budgets drop instead of growing; an empty canvas skips all target work.
        canvas.Begin(Vector2.Zero, layer.Width, layer.Height, 1f, false, 0);
        if (!canvas.Empty || !canvas.DotBounds.IsEmpty) failures.Add("empty canvas reported content");
        for (int i = 0; i <= EbonPixelCanvas.MaxCrescents; i++) canvas.Crescent(new Vector2(600, 300), 0, 1, 10, 60, 0, 0, i);
        if (canvas.Dropped != 1) failures.Add($"crescent budget dropped {canvas.Dropped}, expected 1");
        canvas.Begin(Vector2.Zero, layer.Width, layer.Height, 1f, false, 0);
        canvas.Thread(new Vector2(-900, -900), new Vector2(-700, -800), EbonTone.Ivory);
        if (!canvas.Empty) failures.Add("off-screen thread was not culled");
        EbonPixelCanvas.Checkpoint mark = canvas.Mark();
        canvas.Star(new Vector2(300, 300), 20, .2f, 1);
        canvas.Rewind(mark);
        if (!canvas.Empty) failures.Add("rewind kept a failed source's primitives");
    }

    private static Color[] Read()
    {
        device.SetRenderTarget(frame);
        DrawLayer();
        device.SetRenderTarget(null);
        var dots = new Color[layer.Width * layer.Height];
        layer.GetData(dots);
        return dots;
    }
}
