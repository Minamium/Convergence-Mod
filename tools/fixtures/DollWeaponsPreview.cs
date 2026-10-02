// Offline FNA preview of the shared Doll weapon layer. Links the production contract (DollWeaponLayer.cs),
// DollWeaponCanvas, DollPixelArt and DollSpritePlacement with the compiled DollPixel.fxc; no Terraria types.
// Each frame follows the in-game order: commands are recorded, Front sprites render into the Art target and
// light into the Light target behind a captured device state; then the backdrop, the Back stratum (direct
// world draw), stand-in players, and the Art and Light composites in front of them.
// Synthetic, palette-locked test sprites stand in for the DW art (which is not exported yet).
// Pixel checks: axis-aligned sprites map each texel to an exact 2x2 px block (shader, Back and degraded
// fallback paths, even and odd camera); rotated sprites keep a closed ink ring at 64 angles; every light
// dot is ringed by ink or light; line/forecast thickness is exact; an energy body shows 4+ ramp tones; the
// glow stays bounded; Reduced Effects halves debris; budgets drop instead of growing; device state restores.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

internal static class DollWeaponsPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] private static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_Quit();

    private const int Width = 1280, Height = 720;
    private static readonly Color Dark = new(0x12, 0x10, 0x17), Bright = new(0xba, 0xc6, 0xd6), Probe = new(40, 160, 90);
    private static readonly Vector2 EvenCamera = new(10000, 6000), OddCamera = new(10001, 6001);
    private static GraphicsDevice device;
    private static Effect effect;
    private static SpriteBatch batch;
    private static Texture2D pixel, blade, diamond, organ;
    private static Color[] bladeTexels, diamondTexels, organTexels;
    private static RenderTarget2D art, light, frame;
    private static readonly DollWeaponCanvas canvas = new();
    private static readonly List<string> failures = new();
    private static string output;
    private static bool artDrawn, lightDrawn;

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
        NativeLibrary.SetDllImportResolver(typeof(DollWeaponsPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL initialization failed");
        IntPtr window = SDL_CreateWindow("Offline Doll weapon layer", 0, 0, Width, Height, FNA3D_PrepareWindowAttributes() | 0x8);
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
            using var material = new Effect(device, File.ReadAllBytes(Path.Combine(root, "Assets/AutoloadedEffects/Shaders/DollPixel.fxc")));
            effect = material;
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            (blade, bladeTexels) = MakeBlade();
            (diamond, diamondTexels) = MakeDiamond();
            (organ, organTexels) = MakeOrgan();
            batch = new SpriteBatch(device);
            art = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            light = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            frame = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);

            Checks();
            foreach (bool bright in new[] { false, true })
            foreach (bool reduced in new[] { false, true })
            {
                string name = $"sheet-{(bright ? "bright" : "dark")}-{(reduced ? "reduced" : "normal")}";
                Frame(c => Sheet(c, 12f), OddCamera, bright ? Bright : Dark, reduced, Matrix.Identity, name, !bright && !reduced);
                frames++;
            }
            // Terraria's GameViewMatrix zoom scales about the screen centre.
            Matrix zoom = Matrix.CreateTranslation(-Width / 2f, -Height / 2f, 0) * Matrix.CreateScale(1.5f) * Matrix.CreateTranslation(Width / 2f, Height / 2f, 0);
            Frame(c => Sheet(c, 12f), OddCamera, Dark, false, zoom, "sheet-dark-zoom150", false);
            Frame(c => Sheet(c, 12f), OddCamera, Bright, false, zoom, "sheet-bright-zoom150", false);
            Frame(Rotations, EvenCamera, Dark, false, Matrix.Identity, "rotations-dark", true);
            Frame(Rotations, EvenCamera, Bright, false, Matrix.Identity, "rotations-bright", false);
            frames += 4;
            for (int i = 0; i < 48; i++)
            {
                float t = i * 1.5f;
                Frame(c => Sheet(c, t), OddCamera, Dark, false, Matrix.Identity, $"seq-dark-{i:D3}", false);
                frames++;
            }
            batch.Dispose(); pixel.Dispose(); blade.Dispose(); diamond.Dispose(); organ.Dispose();
            art.Dispose(); light.Dispose(); frame.Dispose();
        }
        finally
        {
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        Console.WriteLine(failures.Count == 0
            ? $"PASS {frames} offline Doll weapon layer frames in {output}; pixel checks passed. Offline only; synthetic sprites, no native game, real DW art, zoom UI or peer acceptance."
            : $"{failures.Count} check(s) failed; {frames} frames written to {output}.");
        return failures.Count == 0 ? 0 : 1;
    }

    // ---- Frames --------------------------------------------------------------------------------

    private static void Record(Action<DollWeaponCanvas> scene, Vector2 camera, bool reduced, double ticks)
    {
        canvas.Begin(camera, Width, Height, 1f, reduced, ticks);
        scene(canvas);
        canvas.EndRecording();
    }

    // Front sprites into Art and light into Light, as DollWeaponLayer does in CheckMonoliths.
    private static void RenderLayers()
    {
        DollDeviceState state = DollDeviceState.Capture(device, true);
        artDrawn = lightDrawn = false;
        try
        {
            if (canvas.HasArt)
            {
                Bind(art);
                canvas.DrawArt(device, effect, art.Width, art.Height);
                artDrawn = true;
            }
            if (canvas.HasLight)
            {
                Bind(light);
                canvas.DrawLight(device, effect, light.Width, light.Height);
                lightDrawn = true;
            }
        }
        finally
        {
            state.Restore(device);
        }
        if (canvas.MaterialError is not null) failures.Add($"energy material failed: {canvas.MaterialError.Message}");
    }

    private static void Bind(RenderTarget2D target)
    {
        device.SetRenderTarget(target);
        device.Clear(Color.Transparent);
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
    }

    private static Color[] Frame(Action<DollWeaponCanvas> scene, Vector2 camera, Color backdrop, bool reduced, Matrix view, string name,
        bool saveLayers, bool fallback = false, bool players = true, double ticks = 40)
    {
        Record(scene, camera, reduced, ticks);
        if (canvas.Dropped > 0) failures.Add($"{name}: {canvas.Dropped} command(s) over budget");
        device.SetRenderTarget(frame);
        if (!fallback) RenderLayers();
        if (device.GetRenderTargets().Length != 1 || device.GetRenderTargets()[0].RenderTarget != frame)
            failures.Add($"{name}: device state did not restore the caller's render target");
        device.Clear(backdrop);
        if (players) Backdrop(backdrop == Bright, view);
        if (canvas.HasBack)
        {
            if (fallback) Fallback(DollStratum.Back, camera, view);
            else
            {
                DollDeviceState state = DollDeviceState.Capture(device, false);
                States();
                canvas.DrawBack(device, effect, camera, view);
                state.Restore(device);
            }
        }
        if (players) Players(view);
        if (fallback) Fallback(DollStratum.Front, camera, view);
        else if (artDrawn || lightDrawn)
        {
            DollDeviceState state = DollDeviceState.Capture(device, false);
            States();
            Vector2 offset = canvas.Origin - camera;
            if (artDrawn) DollPixelArt.CompositeArt(device, effect, art, canvas.ArtArea, offset, view);
            if (lightDrawn) DollPixelArt.CompositeLight(device, effect, light, artDrawn ? art : null, canvas.LightArea, offset, view, reduced);
            state.Restore(device);
        }
        device.SetRenderTarget(null);
        var pixels = new Color[Width * Height];
        frame.GetData(pixels);
        if (name is not null)
        {
            using (var file = File.Create(Path.Combine(output, name + ".png"))) frame.SaveAsPng(file, Width, Height);
            if (saveLayers && artDrawn)
                using (var file = File.Create(Path.Combine(output, name + "-art.png"))) art.SaveAsPng(file, art.Width, art.Height);
            if (saveLayers && lightDrawn)
                using (var file = File.Create(Path.Combine(output, name + "-light.png"))) light.SaveAsPng(file, light.Width, light.Height);
        }
        return pixels;
    }

    private static void States()
    {
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
    }

    private static void Fallback(DollStratum stratum, Vector2 camera, Matrix view)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, view);
        canvas.DrawSpritesDirect(batch, stratum, camera);
        batch.End();
    }

    // Stand-in ground and blocks so readability shows on mixed values.
    private static void Backdrop(bool bright, Matrix view)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, view);
        batch.Draw(pixel, new Rectangle(0, 640, Width, 80), bright ? new Color(120, 98, 78) : new Color(48, 40, 52));
        batch.Draw(pixel, new Rectangle(900, 60, 140, 140), bright ? new Color(236, 238, 242) : new Color(92, 84, 104));
        batch.Draw(pixel, new Rectangle(140, 420, 120, 90), bright ? new Color(150, 110, 80) : new Color(70, 52, 60));
        batch.End();
    }

    // 20 x 42 px stand-in players, drawn between the Back and Front strata as in game.
    private static void Players(Matrix view)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, view);
        foreach (Point at in new[] { new Point(250, 300), new Point(640, 520), new Point(1010, 300), new Point(470, 120) })
        {
            batch.Draw(pixel, new Rectangle(at.X - 10, at.Y - 21, 20, 42), new Color(52, 60, 84));
            batch.Draw(pixel, new Rectangle(at.X - 8, at.Y - 37, 16, 16), new Color(214, 184, 160));
        }
        batch.End();
    }

    // ---- Scenes --------------------------------------------------------------------------------

    private static Vector2 W(float x, float y) => OddCamera + new Vector2(x, y);

    // Every command at representative settings; `t` drives forecasts, debris, reveal and dissolve.
    private static void Sheet(DollWeaponCanvas c, float t)
    {
        // Back stratum: an organ behind a player, rising row by row.
        c.Sprite(new DollSprite(organ, organ.Bounds, new Vector2(organ.Width / 2f, organ.Height)), W(250, 330), 0, DollFlip.None,
            DollStratum.Back, 0, new DollSpriteFx { Hide = Math.Clamp(1f - t / 24f, 0f, 1f), HideFromBottom = false });
        // Front sprites: held bodies over players at several rotations and flips, then the fx row.
        var bladeSprite = new DollSprite(blade, blade.Bounds, new Vector2(4, 13));
        c.Sprite(bladeSprite, W(1010, 300), -.6f + t * .02f, DollFlip.None);
        c.Sprite(bladeSprite, W(640, 520), 0, DollFlip.None);
        c.Sprite(bladeSprite, W(470, 130), MathF.PI * .75f, DollFlip.Vertical, DollStratum.Front, 2);
        c.Sprite(new DollSprite(diamond, diamond.Bounds, new Vector2(15.5f, 15.5f)), W(820, 520), .4f, DollFlip.Horizontal);
        c.Sprite(bladeSprite, W(60, 600), 0, DollFlip.None, DollStratum.Front, 0, new DollSpriteFx { Flash = .6f });
        c.Sprite(bladeSprite, W(220, 600), 0, DollFlip.None, DollStratum.Front, 0, new DollSpriteFx { Dissolve = Math.Clamp(t / 60f, 0f, .9f), Seed = 7 });
        c.Sprite(bladeSprite, W(500, 600), 0, DollFlip.Horizontal, DollStratum.Front, 0, new DollSpriteFx { Silhouette = DollTone.Plum });
        c.Sprite(bladeSprite, W(60, 680), 0, DollFlip.None, DollStratum.Front, 0, new DollSpriteFx { Fade = .5f });
        c.Sprite(bladeSprite, W(220, 680), 0, DollFlip.None, DollStratum.Front, 0, new DollSpriteFx { Hide = .5f, HideFromBottom = true });

        // Light: lines in three weights, rings, arcs both ways, forecasts, debris and energy.
        c.Line(W(560, 60), W(760, 60), DollTone.Lilac, 1);
        c.Line(W(560, 72), W(760, 100), DollTone.PearlViolet, 2);
        c.Line(W(560, 90), W(700, 180), DollTone.White, 3);
        c.Ring(W(820, 140), 30, DollTone.Violet);
        c.Ring(W(820, 140), 44, DollTone.Bone, 2);
        c.Arc(W(960, 160), 40, -MathF.PI, -MathF.PI / 4, DollTone.Lilac, 2);
        c.Arc(W(1100, 160), 40, 0, -2.4f, DollTone.PearlViolet, 1);
        c.Forecast(W(560, 240), W(1220, 240), Math.Clamp(t / 30f, .05f, 1f));
        c.Forecast(W(700, 400), W(1180, 560));
        c.ForecastArc(W(1120, 420), 70, -MathF.PI / 2, -MathF.PI / 2 + MathF.Tau, Math.Clamp(t / 40f, .05f, 1f));
        DollShardKind[] kinds = { DollShardKind.Porcelain, DollShardKind.Brass, DollShardKind.Pearl, DollShardKind.Spark };
        for (int k = 0; k < kinds.Length; k++)
            c.Burst(W(860 + k * 100, 640), 21 + k, 20, 4f + t % 28f, 32f, 3.4f, .14f, kinds[k]);
        for (int d = 0; d < 6; d++) c.Dot(W(560 + d * 14, 300), d % 2 == 0 ? DollTone.Plum : DollTone.Violet, 2);
        // A live body: the built-in ramp across an energy strip (plum at its tail to white at its head).
        RampBody(c, W(560, 340), W(860, 380), 26f);
        Span<Vector2> spine = stackalloc Vector2[6];
        for (int i = 0; i < spine.Length; i++) spine[i] = W(320 + i * 40, 420 + MathF.Sin(i * .9f + t * .1f) * 24f);
        c.EnergyStrip(DollPixelArt.Ramp, 0, spine, 10f, new Vector4(.62f, 1f, 0f, 0f));
        // A void band over a player: dark light (negative ramp intensity) occludes what is behind it.
        Span<DollPixelVertex> voidBand = c.Energy(DollPixelArt.Ramp, 0, 2);
        if (!voidBand.IsEmpty)
        {
            Band(c, voidBand, W(600, 470), W(700, 560), 14f);
            for (int i = 0; i < 6; i++) voidBand[i].Style = new Vector4(-.8f, 1f, 0f, 0f);
        }
    }

    // A quad whose intensity runs 0 -> 1 from a to b through the ramp material.
    private static void RampBody(DollWeaponCanvas c, Vector2 a, Vector2 b, float width)
    {
        Span<DollPixelVertex> quad = c.Energy(DollPixelArt.Ramp, 0, 2);
        if (quad.IsEmpty) return;
        Band(c, quad, a, b, width);
        for (int i = 0; i < 6; i++)
        {
            bool head = i == 1 || i == 4 || i == 5;
            quad[i].Style = new Vector4(head ? 1f : 0f, 1f, 0f, 0f);
        }
    }

    private static void Band(DollWeaponCanvas c, Span<DollPixelVertex> quad, Vector2 a, Vector2 b, float width)
    {
        Vector2 da = c.ToDot(a), db = c.ToDot(b), direction = Vector2.Normalize(db - da);
        Vector2 normal = new Vector2(-direction.Y, direction.X) * width * .25f;
        Vector2[] corners = { da - normal, db - normal, da + normal, da + normal, db - normal, db + normal };
        for (int i = 0; i < 6; i++)
        {
            quad[i].Position = new Vector3(corners[i], 0);
            quad[i].Shape = new Vector4(corners[i], 0, 0);
            quad[i].Style = new Vector4(1f, 1f, 0f, 0f);
        }
    }

    // The blade at 16 angles in both mirrors, and the diamond (diagonal staircase outline) at 8.
    private static void Rotations(DollWeaponCanvas c)
    {
        var bladeSprite = new DollSprite(blade, blade.Bounds, new Vector2(32, 13));
        for (int i = 0; i < 16; i++)
        {
            float angle = i * MathF.Tau / 16f + .05f;
            c.Sprite(bladeSprite, EvenCamera + new Vector2(90 + (i % 8) * 150, 110 + i / 8 * 150), angle, DollFlip.None);
            c.Sprite(bladeSprite, EvenCamera + new Vector2(90 + (i % 8) * 150, 410 + i / 8 * 150), angle, DollFlip.Vertical);
        }
    }

    // ---- Synthetic sprites ---------------------------------------------------------------------

    private static Color Tone(DollTone tone) => DollPixelArt.Palette[(int)tone];

    private static (Texture2D, Color[]) Make(int w, int h, Func<int, int, DollTone?> fill)
    {
        var mask = new DollTone?[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                mask[x, y] = fill(x, y);
        var texels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (mask[x, y] is not { } tone) continue;
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1 || mask[x - 1, y] is null || mask[x + 1, y] is null
                    || mask[x, y - 1] is null || mask[x, y + 1] is null;
                texels[y * w + x] = Tone(edge && tone != DollTone.Brass ? DollTone.Ink : tone);
            }
        var texture = new Texture2D(device, w, h);
        texture.SetData(texels);
        return (texture, texels);
    }

    // A 64 x 26 side-view blade: iron spine, pearl body with a highlight, a see-through hole with a brass
    // rim (no ink), a brass guard and an ink detail line; the tip tapers in diagonal steps.
    private static (Texture2D, Color[]) MakeBlade() => Make(64, 26, (x, y) =>
    {
        bool body = x >= 2 && x <= 61 && y >= 4 + (x > 40 ? (x - 40) / 3 : 0) && y <= 21 - (x > 46 ? (x - 46) / 2 : 0);
        if (!body) return null;
        int hx = x - 16, hy = y - 13;
        if (hx * hx + hy * hy <= 6) return null;
        if (hx * hx + hy * hy <= 12) return DollTone.Brass;
        if (x >= 8 && x <= 10) return DollTone.BrassLight;
        if (x < 8) return DollTone.Porcelain;
        if (y <= 6 + (x > 40 ? (x - 40) / 3 : 0)) return DollTone.IronDark;
        if (x >= 24 && x <= 34 && y == x - 16) return DollTone.Ink;
        return y == 8 + (x > 40 ? (x - 40) / 3 : 0) ? DollTone.Pearl : DollTone.PearlGrey;
    });

    // A 31 x 31 diamond: its outline is all diagonal staircase, the worst case for rotated point sampling.
    private static (Texture2D, Color[]) MakeDiamond() => Make(31, 31, (x, y) =>
    {
        int d = Math.Abs(x - 15) + Math.Abs(y - 15);
        return d > 14 ? null : d < 5 ? DollTone.Bone : DollTone.Porcelain;
    });

    // A 40 x 48 upright case with brass pipes for the Back stratum.
    private static (Texture2D, Color[]) MakeOrgan() => Make(40, 48, (x, y) =>
    {
        if (y < 8 && (x % 8 < 3 || y < 4 && x % 8 > 5)) return null;
        if (y < 20) return x % 8 < 4 ? DollTone.BrassLight : DollTone.Brass;
        return y > 40 ? DollTone.Iron : (x + y) % 9 == 0 ? DollTone.IronLight : DollTone.IronDark;
    });

    // ---- Checks --------------------------------------------------------------------------------

    private static void Checks()
    {
        CheckAxisAligned();
        CheckSpriteFx();
        CheckRotations();
        CheckLightOutline();
        CheckLines();
        CheckRampAndGlow();
        CheckBudgets();
    }

    // Every axis-aligned placement maps each texel to an exact 2 x 2 px block, through the Art composite,
    // the Back stratum's direct draw and the degraded SpriteBatch fallback, on even and odd cameras.
    private static void CheckAxisAligned()
    {
        DollFlip[] flips = { DollFlip.None, DollFlip.Horizontal, DollFlip.Vertical, DollFlip.Both };
        int checkedSprites = 0;
        foreach (Vector2 camera in new[] { EvenCamera, OddCamera })
        foreach (DollFlip flip in flips)
        for (int quarter = 0; quarter < 4; quarter++)
        foreach (int path in new[] { 0, 1, 2 })
        {
            float rotation = quarter * MathF.PI / 2;
            Vector2 pivotTexel = new(4.5f, 13), pivotWorld = camera + new Vector2(300.3f, 200.7f);
            DollStratum stratum = path == 1 ? DollStratum.Back : DollStratum.Front;
            Color[] pixels = Frame(c => c.Sprite(new DollSprite(blade, blade.Bounds, pivotTexel), pivotWorld, rotation, flip, stratum),
                camera, Probe, false, Matrix.Identity, null, false, path == 2, false);
            NVector2 snapped = DollSpritePlacement.Snap(new NVector2(pivotWorld.X, pivotWorld.Y), new NVector2(pivotTexel.X, pivotTexel.Y), rotation, flip);
            int wrong = 0;
            for (int ty = 0; ty < blade.Height; ty++)
            for (int tx = 0; tx < blade.Width; tx++)
            {
                NVector2 a = DollSpritePlacement.World(new NVector2(tx, ty), new NVector2(pivotTexel.X, pivotTexel.Y), snapped, rotation, flip);
                NVector2 b = DollSpritePlacement.World(new NVector2(tx + 1, ty + 1), new NVector2(pivotTexel.X, pivotTexel.Y), snapped, rotation, flip);
                int x0 = (int)MathF.Round(MathF.Min(a.X, b.X) - camera.X), y0 = (int)MathF.Round(MathF.Min(a.Y, b.Y) - camera.Y);
                Color expected = bladeTexels[ty * blade.Width + tx].A == 0 ? Probe : bladeTexels[ty * blade.Width + tx];
                if (MathF.Abs(MathF.Abs(a.X - b.X) - 2) > .01f || MathF.Abs(MathF.Abs(a.Y - b.Y) - 2) > .01f) wrong++;
                for (int py = 0; py < 2; py++)
                    for (int px = 0; px < 2; px++)
                        if (pixels[(y0 + py) * Width + x0 + px] != expected) wrong++;
            }
            if (wrong > 0)
                failures.Add($"axis-aligned {(path == 0 ? "art" : path == 1 ? "back" : "fallback")} sprite (camera {camera}, {flip}, {quarter} quarter turns): {wrong} pixel(s) off the 2x2 texel blocks");
            checkedSprites++;
        }
        if (checkedSprites != 96) failures.Add($"axis-aligned coverage ran {checkedSprites} placements");
    }

    // Sprite effects do what they say on an axis-aligned blade: dissolve removes about its share of
    // texels, hide removes whole rows from its edge, fade keeps its dithered share, flash and silhouette
    // recolour every non-ink texel while the ink ring stays.
    private static void CheckSpriteFx()
    {
        int area = 0, inkArea = 0;
        foreach (Color texel in bladeTexels)
        {
            if (texel.A > 0) area++;
            if (texel == Tone(DollTone.Ink)) inkArea++;
        }
        (int Opaque, int Ink, int White, int Plum, int TopRows) Measure(DollSpriteFx fx)
        {
            Record(c => c.Sprite(new DollSprite(blade, blade.Bounds, Vector2.Zero), EvenCamera + new Vector2(200, 200), 0, DollFlip.None,
                DollStratum.Front, 0, fx), EvenCamera, false, 0);
            device.SetRenderTarget(frame);
            RenderLayers();
            device.SetRenderTarget(null);
            var dots = new Color[art.Width * art.Height];
            if (artDrawn) art.GetData(dots);
            int opaque = 0, ink = 0, white = 0, plum = 0, top = 0;
            for (int y = 0; y < blade.Height; y++)
            for (int x = 0; x < blade.Width; x++)
            {
                Color c = dots[(100 + y) * art.Width + 100 + x];
                if (c.A == 0) continue;
                opaque++;
                if (y < blade.Height / 2) top++;
                if (c == Tone(DollTone.Ink)) ink++;
                else if (c == Tone(DollTone.White)) white++;
                else if (c == Tone(DollTone.Plum)) plum++;
            }
            return (opaque, ink, white, plum, top);
        }
        var plain = Measure(default);
        if (plain.Opaque != area || plain.Ink != inkArea) failures.Add($"fx baseline drew {plain.Opaque}/{area} texels, {plain.Ink}/{inkArea} ink");
        var dissolved = Measure(new DollSpriteFx { Dissolve = .5f, Seed = 3 });
        if (!(dissolved.Opaque > area * .2f && dissolved.Opaque < area * .8f)) failures.Add($"dissolve .5 kept {dissolved.Opaque} of {area} texels");
        var hidden = Measure(new DollSpriteFx { Hide = .5f });
        if (hidden.TopRows != 0 || hidden.Opaque == 0) failures.Add($"hide .5 from the top left {hidden.TopRows} top-half texels");
        var faded = Measure(new DollSpriteFx { Fade = .5f });
        if (!(faded.Opaque > area * .4f && faded.Opaque < area * .6f)) failures.Add($"fade .5 kept {faded.Opaque} of {area} texels");
        var flashed = Measure(new DollSpriteFx { Flash = 1f });
        if (flashed.Ink != inkArea || flashed.White != area - inkArea) failures.Add($"flash 1 whitened {flashed.White} of {area - inkArea} body texels, ink {flashed.Ink}");
        var silhouette = Measure(new DollSpriteFx { Silhouette = DollTone.Plum });
        if (silhouette.Ink != inkArea || silhouette.Plum != area - inkArea) failures.Add($"silhouette recoloured {silhouette.Plum} of {area - inkArea}");
    }

    // Rotated sprites keep a closed ink ring: no coloured dot touches the empty outside (4-neighbourhood,
    // flood-filled from the target border) in the Art target at 64 angles, both mirrors and three sub-dot
    // pivots, while the blade's authored brass hole rim survives; the drawn area stays close to the texel
    // area (no erosion or bloat).
    private static void CheckRotations()
    {
        int leaks = 0, worstArea = 0, rimLost = 0;
        Color ink = Tone(DollTone.Ink), brass = Tone(DollTone.Brass);
        var dots = new Color[art.Width * art.Height];
        var outside = new bool[art.Width * art.Height];
        var queue = new Queue<int>();
        void Visit(int next)
        {
            if (outside[next] || dots[next].A != 0) return;
            outside[next] = true;
            queue.Enqueue(next);
        }
        foreach ((Texture2D texture, Color[] texels) in new[] { (blade, bladeTexels), (diamond, diamondTexels) })
        {
            int area = 0;
            foreach (Color texel in texels) if (texel.A > 0) area++;
            for (int i = 0; i < 64; i++)
            foreach (DollFlip flip in new[] { DollFlip.None, DollFlip.Vertical })
            foreach (Vector2 sub in new[] { new Vector2(.3f, .9f), new Vector2(1.1f, .2f), new Vector2(.7f, 1.6f) })
            {
                float angle = i * MathF.Tau / 64f + .013f;
                Record(c => c.Sprite(new DollSprite(texture, texture.Bounds, new Vector2(texture.Width / 2f, texture.Height / 2f)),
                    EvenCamera + new Vector2(640, 360) + sub, angle, flip), EvenCamera, false, 0);
                device.SetRenderTarget(frame);
                RenderLayers();
                device.SetRenderTarget(null);
                art.GetData(dots);
                Array.Clear(outside);
                queue.Clear();
                queue.Enqueue(0);
                outside[0] = true;
                while (queue.Count > 0)
                {
                    int at = queue.Dequeue(), x = at % art.Width, y = at / art.Width;
                    if (x > 0) Visit(at - 1);
                    if (x < art.Width - 1) Visit(at + 1);
                    if (y > 0) Visit(at - art.Width);
                    if (y < art.Height - 1) Visit(at + art.Width);
                }
                int drawn = 0, rim = 0;
                for (int y = 1; y < art.Height - 1; y++)
                for (int x = 1; x < art.Width - 1; x++)
                {
                    int at = y * art.Width + x;
                    Color c = dots[at];
                    if (c.A == 0) continue;
                    drawn++;
                    if (c == brass) rim++;
                    if (c == ink) continue;
                    if (outside[at - 1] || outside[at + 1] || outside[at - art.Width] || outside[at + art.Width]) leaks++;
                }
                if (texture == blade && rim == 0) rimLost++;
                worstArea = Math.Max(worstArea, Math.Abs(drawn - area) * 100 / area);
            }
        }
        if (leaks > 0) failures.Add($"rotated sprites: {leaks} coloured dot(s) touch the empty outside (open ink ring)");
        if (rimLost > 0) failures.Add($"rotated sprites: the authored brass hole rim vanished in {rimLost} pose(s)");
        if (worstArea > 12) failures.Add($"rotated sprites: drawn area differs from the texel area by up to {worstArea}%");
    }

    // Every light dot is ringed by light or the ink outline (plain composite, odd camera, flat backdrop).
    private static void CheckLightOutline()
    {
        Color[] pixels = Frame(c =>
        {
            c.Line(W(100, 100), W(420, 180), DollTone.Lilac, 1);
            c.Line(W(100, 140), W(420, 300), DollTone.White, 3);
            c.Ring(W(600, 200), 50, DollTone.Bone, 2);
            c.Arc(W(820, 200), 50, .3f, 2.8f, DollTone.Violet, 1);
            c.Forecast(W(100, 400), W(1100, 460));
            c.ForecastArc(W(1000, 200), 60, 0, 4f, .8f);
            c.Burst(W(300, 560), 3, 24, 10f, 40f, 3.2f, .1f, DollShardKind.Porcelain);
            c.Burst(W(500, 560), 4, 24, 10f, 40f, 3.2f, .1f, DollShardKind.Spark);
            RampBody(c, W(640, 600), W(1000, 640), 24f);
        }, OddCamera, Probe, true, Matrix.Identity, "check-light-outline", true, false, false);
        Color ink = Tone(DollTone.Ink);
        int bare = 0, lit = 0, outlined = 0;
        Vector2 offset = canvas.Origin - OddCamera;
        int ox = (int)offset.X, oy = (int)offset.Y;
        Color Dot(int x, int y)
        {
            int px = ox + x * 2, py = oy + y * 2;
            if (px < 0) px += 1;
            if (py < 0) py += 1;
            return px < 0 || py < 0 || px >= Width || py >= Height ? Probe : pixels[py * Width + px];
        }
        for (int y = 1; y < Height / 2; y++)
        for (int x = 1; x < Width / 2; x++)
        {
            Color c = Dot(x, y);
            if (c == ink) outlined++;
            if (c == Probe || c == ink) continue;
            lit++;
            if (Dot(x - 1, y) == Probe || Dot(x + 1, y) == Probe || Dot(x, y - 1) == Probe || Dot(x, y + 1) == Probe) bare++;
        }
        if (lit == 0 || outlined == 0) failures.Add($"light outline scene drew {lit} lit and {outlined} ink dots");
        if (bare > 0) failures.Add($"{bare} light dot(s) border the backdrop without an ink outline");
    }

    // Lines and forecasts hold exactly their thickness on every major-axis step; arcs keep their window.
    private static void CheckLines()
    {
        (Vector2 a, Vector2 b)[] lines =
        {
            (new(100, 100), new(500, 100)), (new(101.3f, 103.9f), new(503.7f, 251.2f)), (new(100, 100), new(300, 300)),
            (new(400, 100), new(330, 380)), (new(250, 400), new(250, 120)), (new(600, 300), new(180, 210)),
        };
        foreach (var (a, b) in lines)
        for (int thickness = 0; thickness <= 3; thickness++)
        {
            Record(c =>
            {
                if (thickness == 0) c.Forecast(EvenCamera + a, EvenCamera + b);
                else c.Line(EvenCamera + a, EvenCamera + b, DollTone.Lilac, thickness);
            }, EvenCamera, false, 0);
            Color[] dots = ReadLight();
            int want = Math.Max(1, thickness);
            int ax = (int)MathF.Floor(a.X / 2), ay = (int)MathF.Floor(a.Y / 2), bx = (int)MathF.Floor(b.X / 2), by = (int)MathF.Floor(b.Y / 2);
            bool xMajor = Math.Abs(bx - ax) >= Math.Abs(by - ay);
            int from = xMajor ? Math.Min(ax, bx) : Math.Min(ay, by), to = xMajor ? Math.Max(ax, bx) : Math.Max(ay, by);
            int wrong = 0, outside = 0, heads = 0;
            for (int major = 0; major < (xMajor ? light.Width : light.Height); major++)
            {
                int drawn = 0;
                for (int minor = 0; minor < (xMajor ? light.Height : light.Width); minor++)
                {
                    Color c = dots[xMajor ? minor * light.Width + major : major * light.Width + minor];
                    if (c.A == 0) continue;
                    drawn++;
                    if (c == Tone(DollTone.White)) heads++;
                }
                if (major >= from && major <= to) { if (drawn != want) wrong++; }
                else if (drawn > 0) outside++;
            }
            string what = thickness == 0 ? "forecast" : $"line x{thickness}";
            if (wrong + outside > 0) failures.Add($"{what} {a}->{b}: {wrong} steps not {want} dots, {outside} stray steps");
            if (thickness == 0 && heads == 0) failures.Add($"forecast {a}->{b} has no travelling heads");
        }
        // A negative sweep runs from its start the other way round: dots near both ends, none opposite.
        Record(c => c.Arc(EvenCamera + new Vector2(640, 360), 80, 0, -MathF.PI / 2, DollTone.Bone, 1), EvenCamera, false, 0);
        Color[] arc = ReadLight();
        bool At(float angle)
        {
            int cx = 320 + (int)MathF.Floor(MathF.Cos(angle) * 40f + .5f), cy = 180 + (int)MathF.Floor(MathF.Sin(angle) * 40f + .5f);
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (arc[(cy + dy) * light.Width + cx + dx].A > 0) return true;
            return false;
        }
        if (!At(-.1f) || !At(-MathF.PI / 2 + .1f) || !At(-MathF.PI / 4) || At(MathF.PI / 2) || At(MathF.PI))
            failures.Add("arc with a negative sweep does not cover exactly its window");
    }

    // The built-in ramp shows the light ramp's tones across a live body, and the glow stays within reach.
    private static void CheckRampAndGlow()
    {
        Record(c => RampBody(c, EvenCamera + new Vector2(300, 300), EvenCamera + new Vector2(900, 300), 40f), EvenCamera, false, 0);
        Color[] dots = ReadLight();
        var tones = new HashSet<Color>();
        int lit = 0, pale = 0;
        foreach (Color c in dots)
        {
            if (c.A == 0) continue;
            lit++;
            tones.Add(c);
            if (c == Tone(DollTone.PearlViolet) || c == Tone(DollTone.Bone) || c == Tone(DollTone.White)) pale++;
        }
        DollTone[] ramp = { DollTone.Plum, DollTone.PlumLight, DollTone.Violet, DollTone.Lilac, DollTone.PearlViolet, DollTone.Bone, DollTone.White };
        int rampTones = 0;
        foreach (DollTone tone in ramp) if (tones.Contains(Tone(tone))) rampTones++;
        if (rampTones < 4 || tones.Count != rampTones) failures.Add($"ramp body shows {rampTones} ramp tones and {tones.Count - rampTones} off-palette colours");
        if (lit == 0 || pale * 100 / lit < 25) failures.Add($"ramp body pale share {(lit == 0 ? 0 : pale * 100 / lit)}%");

        // A single white dot: the glow tints the backdrop, but never beyond Reach dots.
        Color[] pixels = Frame(c => c.Dot(EvenCamera + new Vector2(640, 360), DollTone.White, 2), EvenCamera, Probe, false, Matrix.Identity,
            "check-glow", false, false, false);
        int tinted = 0, far = 0;
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            if (pixels[y * Width + x] == Probe) continue;
            tinted++;
            if (Math.Abs(x - 641) > (DollWeaponCanvas.Reach + 1) * 2 || Math.Abs(y - 361) > (DollWeaponCanvas.Reach + 1) * 2) far++;
        }
        if (tinted <= 16 || far > 0) failures.Add($"glow: {tinted} tinted px, {far} beyond reach");
        Color[] plain = Frame(c => c.Dot(EvenCamera + new Vector2(640, 360), DollTone.White, 2), EvenCamera, Probe, true, Matrix.Identity,
            null, false, false, false);
        int plainTinted = 0;
        foreach (Color c in plain) if (c != Probe) plainTinted++;
        if (plainTinted >= tinted) failures.Add("Reduced Effects did not drop the glow");
        // A porcelain shard is matte: no glow around it.
        Color[] matte = Frame(c => c.Dot(EvenCamera + new Vector2(640, 360), DollTone.PorcelainLight, 2), EvenCamera, Probe, false, Matrix.Identity,
            null, false, false, false);
        int matteTinted = 0;
        foreach (Color c in matte) if (c != Probe) matteTinted++;
        if (matteTinted != plainTinted) failures.Add($"porcelain glowed ({matteTinted} px vs {plainTinted} plain)");

        // Reduced Effects halves debris.
        int Pieces(bool reduced)
        {
            Record(c => c.Burst(EvenCamera + new Vector2(640, 360), 5, 30, 6f, 40f, 3f, .1f, DollShardKind.Pearl), EvenCamera, reduced, 0);
            int n = 0;
            foreach (Color c in ReadLight()) if (c.A > 0) n++;
            return n;
        }
        int full = Pieces(false), half = Pieces(true);
        if (!(half > 0 && half < full * 3 / 4)) failures.Add($"Reduced Effects debris {half} dots vs {full}");
    }

    private static void CheckBudgets()
    {
        // Over budget drops and counts, never grows.
        Record(c =>
        {
            for (int i = 0; i <= DollWeaponCanvas.MaxSprites; i++)
                c.Sprite(new DollSprite(diamond, diamond.Bounds, Vector2.Zero), EvenCamera + new Vector2(100 + i, 100), 0, DollFlip.None);
            for (int i = 0; i <= DollWeaponCanvas.MaxLines; i++) c.Line(EvenCamera + new Vector2(10, 10 + i), EvenCamera + new Vector2(40, 10 + i), DollTone.Lilac);
        }, EvenCamera, false, 0);
        if (canvas.Dropped != 2) failures.Add($"budgets dropped {canvas.Dropped} commands, expected 2");
        // The rest of the overflow contract is counted too: a strip over MaxStripPoints, rejected sprites (invalid
        // input or over MaxSpriteTexels) and a truncated burst; a sprite that is merely fully faded is not a drop.
        var longSpine = new Vector2[DollWeaponCanvas.MaxStripPoints + 1];
        for (int i = 0; i < longSpine.Length; i++) longSpine[i] = EvenCamera + new Vector2(20 + i * 10, 200);
        Record(c =>
        {
            c.EnergyStrip(DollPixelArt.Ramp, 0, longSpine, 10f, new Vector4(.62f, 1f, 0f, 0f));
            c.EnergyStrip(DollPixelArt.Ramp, 0, longSpine.AsSpan(0, DollWeaponCanvas.MaxStripPoints), 10f, new Vector4(.62f, 1f, 0f, 0f));
        }, EvenCamera, false, 0);
        if (canvas.Dropped != 1 || !canvas.HasLight) failures.Add($"strip over {DollWeaponCanvas.MaxStripPoints} points dropped {canvas.Dropped}, expected 1 (and the exact-fit strip drawn)");
        Record(c =>
        {
            Vector2 at = EvenCamera + new Vector2(200, 200);
            c.Sprite(new DollSprite(diamond, new Rectangle(0, 0, DollWeaponCanvas.MaxSpriteTexels + 1, 1), Vector2.Zero), at, 0, DollFlip.None);
            c.Sprite(new DollSprite(diamond, diamond.Bounds, Vector2.Zero), new Vector2(float.NaN, 0f), 0, DollFlip.None);
            c.Sprite(new DollSprite(diamond, diamond.Bounds, Vector2.Zero), at, 0, DollFlip.None, DollStratum.Front, 0, new DollSpriteFx { Fade = .5f });
            c.Sprite(new DollSprite(diamond, diamond.Bounds, Vector2.Zero), at, 0, DollFlip.None, DollStratum.Front, 0, new DollSpriteFx { Fade = 1f });
        }, EvenCamera, false, 0);
        if (canvas.Dropped != 2 || !canvas.HasArt) failures.Add($"rejected sprites dropped {canvas.Dropped}, expected 2 (a faded-out sprite is not a drop)");
        Record(c =>
        {
            c.Burst(EvenCamera + new Vector2(300, 300), 5, DollWeaponCanvas.MaxBurstPieces, 6f, 40f, 3f, .1f, DollShardKind.Pearl);
            if (canvas.Dropped != 0) failures.Add("a burst within MaxBurstPieces was counted as dropped");
            c.Burst(EvenCamera + new Vector2(300, 300), 5, DollWeaponCanvas.MaxBurstPieces + 12, 6f, 40f, 3f, .1f, DollShardKind.Pearl);
        }, EvenCamera, false, 0);
        if (canvas.Dropped != 1) failures.Add($"truncated burst dropped {canvas.Dropped}, expected 1");
        // A NaN or infinite energy vertex is skipped, so it cannot empty the frame's Light bounds.
        Record(c =>
        {
            RampBody(c, EvenCamera + new Vector2(300, 300), EvenCamera + new Vector2(600, 300), 24f);
            Span<DollPixelVertex> bad = c.Energy(DollPixelArt.Ramp, 0, 1);
            bad[0].Position = new Vector3(float.NaN, 0f, 0f);
            bad[1].Position = new Vector3(0f, float.PositiveInfinity, 0f);
        }, EvenCamera, false, 0);
        if (!canvas.HasLight || canvas.LightArea.IsEmpty) failures.Add("a non-finite energy vertex emptied the Light bounds");
        else
        {
            int lit = 0;
            foreach (Color c in ReadLight()) if (c.A > 0) lit++;
            if (lit == 0) failures.Add("a non-finite energy vertex hid the frame's light");
        }
        // Off-screen commands are culled; a rewind forgets a failed source.
        Record(c =>
        {
            c.Line(EvenCamera - new Vector2(900, 900), EvenCamera - new Vector2(700, 800), DollTone.Lilac);
            c.Sprite(new DollSprite(diamond, diamond.Bounds, Vector2.Zero), EvenCamera - new Vector2(900, 900), 0, DollFlip.None);
        }, EvenCamera, false, 0);
        if (canvas.HasArt || canvas.HasLight) failures.Add("off-screen commands were not culled");
        canvas.Begin(EvenCamera, Width, Height, 1f, false, 0);
        DollWeaponCanvas.Checkpoint mark = canvas.Mark();
        canvas.Energy(DollPixelArt.Ramp, 0, 4);
        canvas.Line(EvenCamera + new Vector2(10, 10), EvenCamera + new Vector2(90, 10), DollTone.Lilac);
        canvas.Rewind(mark);
        canvas.EndRecording();
        if (canvas.HasLight) failures.Add("rewind kept a failed source's commands");
        // The layer restores the backbuffer as well as a bound target.
        Record(c => c.Dot(EvenCamera + new Vector2(10, 10), DollTone.White), EvenCamera, false, 0);
        device.SetRenderTarget(null);
        RenderLayers();
        if (device.GetRenderTargets().Length != 0) failures.Add("device state did not restore the backbuffer");
    }

    private static Color[] ReadLight()
    {
        device.SetRenderTarget(frame);
        RenderLayers();
        device.SetRenderTarget(null);
        var dots = new Color[light.Width * light.Height];
        if (lightDrawn) light.GetData(dots);
        return dots;
    }
}
