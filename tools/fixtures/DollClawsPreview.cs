// Offline FNA preview of Lacrimosa's Claws on the shared Doll weapon layer. Links the production canvas, pixel
// art, placement, anchors, the claw's pure motion/heart and LacrimosaClawPresentation.Emit with the real compiled
// DollPixel.fxc and DollClawEnergy.fxc and the exported DW01 PNGs; no Terraria types. A scripted owner runs the
// real tick sequence (rest -> A -> B -> C -> rest with beads lighting -> grasp -> crush -> rest), Emit is called
// every tick with a presentation memory as in game, and chosen frames are composited at zoom 1 over a 20 x 42
// player silhouette on dark #121017 and bright #bac6d6 ground (and a flat probe ground for the checks).
// Checks: no command dropped and no material error; light is always ringed by ink or light; live damage shows 4+
// ramp tones with pale (pearl-violet, bone, white) at least 40% of lit dots; the crush void is ink with a pearl
// lip; the crush residue is plum by the grasp's last tick; the hands' art stays mostly visible under the light;
// every pose is drawn; Reduced Effects halves debris.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal static class DollClawsPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] private static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_Quit();

    private const int Width = 1280, Height = 720, CellW = 780, CellH = 560;
    private static readonly Color Dark = new(0x12, 0x10, 0x17), Bright = new(0xba, 0xc6, 0xd6), Probe = new(40, 160, 90);
    private static readonly Vector2 Camera = new(20001, 12001);
    // The owner stands here on screen; the grasp target 300 px to the right and a little up.
    private static readonly Vector2 PlayerScreen = new(520, 400), TargetOffset = new(300, -40);
    private static GraphicsDevice device;
    private static Effect pixelEffect, clawEffect;
    private static SpriteBatch batch;
    private static Texture2D pixel;
    private static RenderTarget2D art, light, frame;
    private static readonly DollWeaponCanvas canvas = new();
    private static readonly List<string> failures = new();
    private static string output;
    private static bool artDrawn, lightDrawn;
    private static LacrimosaClawSprites sprites;
    private static LacrimosaClawMaterial material;

    private sealed class Shot
    {
        internal string Label;
        internal Color[] Dark, Bright;
    }

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
        NativeLibrary.SetDllImportResolver(typeof(DollClawsPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL initialization failed");
        IntPtr window = SDL_CreateWindow("Offline Lacrimosa's Claws", 0, 0, Width, Height, FNA3D_PrepareWindowAttributes() | 0x8);
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
            string shaders = Path.Combine(root, "Assets/AutoloadedEffects/Shaders");
            using var pixelMaterial = new Effect(device, File.ReadAllBytes(Path.Combine(shaders, "DollPixel.fxc")));
            using var clawMaterial = new Effect(device, File.ReadAllBytes(Path.Combine(shaders, "DollClawEnergy.fxc")));
            pixelEffect = pixelMaterial;
            clawEffect = clawMaterial;
            material = new LacrimosaClawMaterial(() => clawEffect);
            string textures = Path.Combine(root, "Assets/Textures/Items/DollWeapons");
            Texture2D Load(LacrimosaSpriteArt a)
            {
                using var stream = File.OpenRead(Path.Combine(textures, a.Name + ".png"));
                var texture = Texture2D.FromStream(device, stream);
                if (texture.Width != a.Width || texture.Height != a.Height) failures.Add($"{a.Name}: {texture.Width}x{texture.Height}, anchors say {a.Width}x{a.Height}");
                return texture;
            }
            sprites = new LacrimosaClawSprites(Load(LacrimosaClawArt.OpenSmall), Load(LacrimosaClawArt.OpenLarge), Load(LacrimosaClawArt.RakeSmall),
                Load(LacrimosaClawArt.RakeLarge), Load(LacrimosaClawArt.ClenchSmall), Load(LacrimosaClawArt.ClenchLarge),
                Load(LacrimosaClawArt.ThrustSmall), Load(LacrimosaClawArt.ThrustLarge));
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            batch = new SpriteBatch(device);
            art = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            light = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            frame = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);

            var shots = new List<Shot>();
            frames += Run(false, true, shots);
            frames += Run(true, true, shots);
            frames += Run(false, false, shots);
            // Mirrors: a left-facing owner and a reversed-gravity owner (a few frames each).
            frames += Run(false, true, shots, -1, 1);
            frames += Run(false, true, shots, 1, -1);
            Checks();
            Sheet(shots, "claws-contact");
            batch.Dispose(); pixel.Dispose(); art.Dispose(); light.Dispose(); frame.Dispose();
        }
        finally
        {
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        Console.WriteLine(failures.Count == 0
            ? $"PASS {frames} offline Lacrimosa's Claws frames in {output}; checks passed. Offline only: no native game, peers, zoom UI or FPS."
            : $"{failures.Count} check(s) failed; {frames} frames written to {output}.");
        return failures.Count == 0 ? 0 : 1;
    }

    // ---- The scripted owner ---------------------------------------------------------------------------

    // Ticks of the script (attack speed 1): rest, A from 20 (B and C follow at once), rest with full beads, the grasp
    // from 160. A stroke or grasp age a falls on tick start + a - 1.
    private const int StrokeA = 20, StrokeB = StrokeA + 26, StrokeC = StrokeB + 24, GraspStart = 160, End = 260;
    private static int A(int age) => StrokeA + age - 1;
    private static int B(int age) => StrokeB + age - 1;
    private static int C(int age) => StrokeC + age - 1;
    private static int G(int age) => GraspStart + age - 1;

    private static readonly (int Tick, string Label)[] Wanted =
    {
        (12, "rest, a bead lights"), (A(5), "A wind-up"), (A(9), "A live (first tick)"), (A(12), "A live"),
        (A(15), "A live (late)"), (A(20), "A residue"), (B(7), "B live (first tick)"), (B(10), "B live (late)"),
        (C(10), "C flung wide"), (C(19), "C drive (live)"), (C(22), "C contact"), (C(25), "C slit and ring"),
        (C(30), "C pipe bars"), (C(34) + 8, "return to rest"), (144, "full beads (heartbeat)"), (G(3), "grasp discharge"),
        (G(8), "grasp flight"), (G(13), "grasp flight (braking)"), (G(15), "grasp arrival"), (G(17), "grasp contact"),
        (G(27), "grasp hold"), (G(39), "grasp brace"), (G(41), "crush"), (G(44), "crush (late)"),
        (G(49), "burst and return"), (G(59), "residue"), (G(63), "residue cooled (last grasp tick)"), (G(76), "rest again"),
    };

    // Where a stroke's first live capsule tip is: the contact star's place on a target in reach.
    private static Vector2 Contact(Vector2 center, int stroke, float aim, int facing, float gravDir)
    {
        if (stroke == LacrimosaClawMotion.None) return center;
        int hand = stroke == LacrimosaClawMotion.RakeUp ? LacrimosaClawMotion.Left : LacrimosaClawMotion.Right;
        LacrimosaClawMotion.TryCapsule(stroke, hand, LacrimosaClawMotion.LiveStart(stroke) + 2, out _, out System.Numerics.Vector2 tip, out _);
        System.Numerics.Vector2 world = LacrimosaClawMotion.ToWorld(tip, aim, facing, gravDir);
        return center + new Vector2(world.X, world.Y);
    }

    private static Vector2 targetScreen = PlayerScreen + TargetOffset;
    private static int cropX = -230;

    private static int Run(bool reduced, bool owner, List<Shot> shots, int facing = 1, float gravDir = 1)
    {
        var memory = new LacrimosaClawMemory();
        int stroke = LacrimosaClawMotion.None, age = 0, duration = 0, serial = 0, impact = 0, combo = 0, frames = 0;
        float aim = facing < 0 ? MathF.PI + .12f : -.12f;
        bool hit = false, mirrored = facing < 0 || gravDir < 0;
        Vector2 offset = new(TargetOffset.X * facing, TargetOffset.Y * gravDir);
        targetScreen = PlayerScreen + offset;
        cropX = facing < 0 ? -CellW + 230 : -230;
        Vector2 center = Camera + PlayerScreen, target = center + offset;
        for (int tick = 0; tick < End; tick++)
        {
            // Item use: the kata from StrokeA through one C, then rest; the grasp at GraspStart.
            if (tick >= StrokeA && combo < 3 && (stroke == LacrimosaClawMotion.None || age >= duration))
            {
                stroke = combo++;
                duration = LacrimosaClawMotion.BaseTicks(stroke);
                age = 0;
                serial++;
                hit = false;
            }
            if (stroke != LacrimosaClawMotion.None && age < 1000) age++;
            if (stroke != LacrimosaClawMotion.None && !hit && age == LacrimosaClawMotion.FirstLiveAge(stroke, duration))
            {
                hit = true;
                impact = serial;
            }
            int beads = Math.Min(6, tick / 6);
            if (tick < 12) beads = 2;
            bool grasping = tick >= GraspStart && tick < GraspStart + LacrimosaClawMotion.GraspEnd;
            if (tick == GraspStart) { stroke = LacrimosaClawMotion.None; age = 0; }
            if (tick >= GraspStart) beads = 0;
            var state = new LacrimosaClawDrawState
            {
                Center = center, GravDir = gravDir, Facing = facing, Stroke = stroke, Aim = aim, Age = age, Duration = duration, Serial = serial,
                Beads = beads, Owner = owner, ImpactSerial = impact,
                ImpactAt = Contact(center, stroke, aim, facing, gravDir),
                Grasping = grasping, GraspId = 7, GraspAge = tick - GraspStart + 1, GraspCenter = target, GraspHalfExtent = 30, GraspSide = facing,
                GraspHeld = true, HasAimTarget = owner && beads >= 6 && !grasping, AimTarget = new Rectangle((int)target.X - 30, (int)target.Y - 30, 60, 60),
                DryClick = tick >= 5 && tick < 9 ? 5 : -1, Sprites = sprites, Material = material,
            };
            bool wanted = false;
            string label = null;
            foreach (var w in Wanted) if (w.Tick == tick) { wanted = true; label = w.Label; }
            if (!wanted)
            {
                // Advance the presentation memory exactly as the layer would on frames we do not keep.
                canvas.Begin(Camera, Width, Height, 1f, reduced, tick);
                LacrimosaClawPresentation.Emit(canvas, state, memory);
                canvas.EndRecording();
                if (canvas.Dropped > 0) failures.Add($"tick {tick}: {canvas.Dropped} command(s) dropped");
                continue;
            }
            if (mirrored && !(label.StartsWith("A live (late)", StringComparison.Ordinal) || label == "C contact" || label == "grasp hold"))
            {
                canvas.Begin(Camera, Width, Height, 1f, reduced, tick);
                LacrimosaClawPresentation.Emit(canvas, state, memory);
                canvas.EndRecording();
                continue;
            }
            string variant = facing < 0 ? "left" : gravDir < 0 ? "gravity" : reduced ? "reduced" : owner ? "owner" : "peer";
            string name = $"claws-{variant}-{tick:D3}";
            Color[] darkPixels = Frame(state, memory, Dark, reduced, tick, name + "-dark", true, !reduced && owner);
            Color[] brightPixels = Frame(state, memory, Bright, reduced, tick, name + "-bright", false, false);
            frames += 2;
            bool keep = owner && !reduced || mirrored || label.Contains("contact") || label.StartsWith("crush", StringComparison.Ordinal);
            if (keep && shots is not null)
                shots.Add(new Shot
                {
                    Label = $"{(tick >= GraspStart ? $"grasp age {tick - GraspStart + 1}" : $"tick {tick}")}: {label}{(owner ? "" : " (peer, light 65%)")}{(reduced ? " (Reduced Effects)" : "")}{(facing < 0 ? " (facing left)" : "")}{(gravDir < 0 ? " (reversed gravity)" : "")}",
                    Dark = Crop(darkPixels), Bright = Crop(brightPixels),
                });
            if (!reduced && owner && !mirrored) CheckFrame(state, memory, tick, label);
        }
        return frames;
    }

    // ---- Rendering ----------------------------------------------------------------------------------

    private static Color[] Frame(LacrimosaClawDrawState state, LacrimosaClawMemory memory, Color backdrop, bool reduced, double clock,
        string name, bool advance, bool saveLayers, bool players = true)
    {
        if (advance)
        {
            canvas.Begin(Camera, Width, Height, 1f, reduced, clock);
            LacrimosaClawPresentation.Emit(canvas, state, memory);
            canvas.EndRecording();
            if (canvas.Dropped > 0) failures.Add($"{name}: {canvas.Dropped} command(s) dropped");
        }
        device.SetRenderTarget(frame);
        RenderLayers();
        device.Clear(backdrop);
        if (players) Scene(backdrop == Bright, state);
        if (artDrawn || lightDrawn)
        {
            DollDeviceState saved = DollDeviceState.Capture(device, false);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            Vector2 offset = canvas.Origin - Camera;
            if (artDrawn) DollPixelArt.CompositeArt(device, pixelEffect, art, canvas.ArtArea, offset, Matrix.Identity);
            if (lightDrawn) DollPixelArt.CompositeLight(device, pixelEffect, light, artDrawn ? art : null, canvas.LightArea, offset, Matrix.Identity, reduced);
            saved.Restore(device);
        }
        device.SetRenderTarget(null);
        var pixels = new Color[Width * Height];
        frame.GetData(pixels);
        if (name is not null)
        {
            using (var file = File.Create(Path.Combine(output, name + ".png"))) frame.SaveAsPng(file, Width, Height);
            if (saveLayers && lightDrawn)
                using (var file = File.Create(Path.Combine(output, name + "-light.png"))) light.SaveAsPng(file, light.Width, light.Height);
        }
        return pixels;
    }

    private static void RenderLayers()
    {
        DollDeviceState state = DollDeviceState.Capture(device, true);
        artDrawn = lightDrawn = false;
        try
        {
            if (canvas.HasArt)
            {
                Bind(art);
                canvas.DrawArt(device, pixelEffect, art.Width, art.Height);
                artDrawn = true;
            }
            if (canvas.HasLight)
            {
                Bind(light);
                canvas.DrawLight(device, pixelEffect, light.Width, light.Height);
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

    // Ground, a block and the 20 x 42 owner (head, body), drawn between the strata as in game; the grasp target is a
    // 60 x 60 stand-in body.
    private static void Scene(bool bright, LacrimosaClawDrawState state)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        batch.Draw(pixel, new Rectangle(0, (int)PlayerScreen.Y + 21, Width, 80), bright ? new Color(120, 98, 78) : new Color(48, 40, 52));
        batch.Draw(pixel, new Rectangle(880, 120, 160, 140), bright ? new Color(236, 238, 242) : new Color(92, 84, 104));
        Vector2 target = targetScreen;
        batch.Draw(pixel, new Rectangle((int)target.X - 30, (int)target.Y - 30, 60, 60), bright ? new Color(150, 110, 80) : new Color(96, 60, 64));
        Point at = new((int)PlayerScreen.X, (int)PlayerScreen.Y);
        batch.Draw(pixel, new Rectangle(at.X - 10, at.Y - 21, 20, 42), new Color(52, 60, 84));
        batch.Draw(pixel, new Rectangle(at.X - 8, at.Y - 37, 16, 16), new Color(214, 184, 160));
        batch.End();
    }

    private static Color[] Crop(Color[] pixels)
    {
        var cell = new Color[CellW * CellH];
        int x0 = (int)PlayerScreen.X + cropX, y0 = (int)PlayerScreen.Y - 300;
        for (int y = 0; y < CellH; y++)
            for (int x = 0; x < CellW; x++)
            {
                int sx = x0 + x, sy = y0 + y;
                cell[y * CellW + x] = sx >= 0 && sy >= 0 && sx < Width && sy < Height ? pixels[sy * Width + sx] : Color.Black;
            }
        return cell;
    }

    // Contact sheet: per row, dark and bright cells side by side with a label strip (drawn as pixels: no font).
    private static void Sheet(List<Shot> shots, string name)
    {
        const int columns = 4, gap = 6;
        int rows = (shots.Count + 1) / 2;
        int w = columns * CellW + (columns - 1) * gap, h = rows * (CellH + gap);
        var sheet = new Color[w * h];
        Array.Fill(sheet, new Color(30, 30, 34));
        for (int i = 0; i < shots.Count; i++)
        {
            int row = i / 2, col = (i % 2) * 2;
            Blit(sheet, w, shots[i].Dark, col * (CellW + gap), row * (CellH + gap));
            Blit(sheet, w, shots[i].Bright, (col + 1) * (CellW + gap), row * (CellH + gap));
        }
        using var texture = new Texture2D(device, w, h);
        texture.SetData(sheet);
        using (var file = File.Create(Path.Combine(output, name + ".png"))) texture.SaveAsPng(file, w, h);
        File.WriteAllLines(Path.Combine(output, name + ".txt"), LabelLines(shots));
    }

    private static IEnumerable<string> LabelLines(List<Shot> shots)
    {
        for (int i = 0; i < shots.Count; i++) yield return $"row {i / 2 + 1}, {(i % 2 == 0 ? "left" : "right")} pair: {shots[i].Label}";
    }

    private static void Blit(Color[] sheet, int sheetWidth, Color[] cell, int x0, int y0)
    {
        for (int y = 0; y < CellH; y++)
            Array.Copy(cell, y * CellW, sheet, (y0 + y) * sheetWidth + x0, CellW);
    }

    // ---- Checks -------------------------------------------------------------------------------------

    private static readonly DollTone[] RampTones = { DollTone.Plum, DollTone.PlumLight, DollTone.Violet, DollTone.Lilac, DollTone.PearlViolet, DollTone.Bone, DollTone.White };
    private static int damagingFrames, voidFrames, cooledFrames, poses;
    private static readonly HashSet<string> drawnPoses = new();

    // Per kept frame (owner, normal effects): re-render over the flat probe without players and check the light.
    private static void CheckFrame(LacrimosaClawDrawState state, LacrimosaClawMemory memory, int tick, string label)
    {
        foreach (LacrimosaHand hand in memory.Shown) drawnPoses.Add($"{hand.Pose}{(hand.Large ? "_L" : "")}");
        // Light dots are ringed by ink or light (the art carries its own outline): probe ground, no players.
        // The plain (Reduced) composite: the glow is meant to tint the ground around lit dots.
        Color[] pixels = Frame(state, memory, Probe, true, tick, null, false, false, false);
        Color ink = DollPixelArt.Tone(DollTone.Ink);
        int bare = 0;
        for (int y = 1; y < Height - 1; y++)
            for (int x = 1; x < Width - 1; x++)
            {
                Color c = pixels[y * Width + x];
                if (c == Probe || c == ink) continue;
                if (pixels[y * Width + x - 1] == Probe || pixels[y * Width + x + 1] == Probe || pixels[(y - 1) * Width + x] == Probe
                    || pixels[(y + 1) * Width + x] == Probe) bare++;
            }
        if (bare > 0) failures.Add($"{label}: {bare} px of light or art touch the ground without an ink outline");

        // The light target's tones while something damages: 4+ ramp tones, pale at least 40% of the lit ramp dots.
        var dots = new Color[light.Width * light.Height];
        if (lightDrawn) light.GetData(dots);
        int lit = 0, pale = 0, voidInk = 0, lip = 0;
        var tones = new HashSet<DollTone>();
        foreach (Color c in dots)
        {
            if (c.A < 250) continue;
            foreach (DollTone tone in RampTones)
                if (c == DollPixelArt.Tone(tone)) { lit++; tones.Add(tone); if (tone >= DollTone.PearlViolet) pale++; }
            if (c == ink || c == DollPixelArt.Tone(DollTone.IronDark)) voidInk++;
            if (c == DollPixelArt.Tone(DollTone.Pearl)) lip++;
        }
        bool damaging = label.Contains("live") || label.Contains("contact") || label.StartsWith("crush", StringComparison.Ordinal) || label.Contains("slit");
        if (damaging)
        {
            damagingFrames++;
            if (tones.Count < 4) failures.Add($"{label}: live light shows {tones.Count} ramp tones");
            if (lit == 0 || pale * 100 / lit < 40) failures.Add($"{label}: pale share {(lit == 0 ? 0 : pale * 100 / lit)}% of {lit} lit dots");
        }
        // Residue cools to plum within 24 ticks: by the grasp's last tick nothing hotter than plum light is left
        // (a stray late debris dot or two aside).
        if (label.Contains("cooled"))
        {
            int hot = 0;
            foreach (Color c in dots)
                if (c.A >= 250)
                    foreach (DollTone tone in RampTones)
                        if ((tone >= DollTone.Violet || tone == DollTone.Bone) && c == DollPixelArt.Tone(tone)) hot++;
            if (hot > 2) failures.Add($"{label}: {hot} dots hotter than plum");
            cooledFrames++;
        }
        if (label.StartsWith("burst", StringComparison.Ordinal))
        {
            voidFrames++;
            if (voidInk < 40 || lip < 10) failures.Add($"{label}: crush void {voidInk} dark dots, {lip} pearl lip dots");
        }
        // The hands stay readable: most of the art's dots keep their art colour after the light composite.
        var artDots = new Color[art.Width * art.Height];
        if (artDrawn) art.GetData(artDots);
        int body = 0, covered = 0;
        Vector2 offset = canvas.Origin - Camera;
        for (int y = 0; y < art.Height; y++)
            for (int x = 0; x < art.Width; x++)
            {
                Color a = artDots[y * art.Width + x];
                if (a.A == 0) continue;
                int px = (int)offset.X + x * 2 + 1, py = (int)offset.Y + y * 2 + 1;
                if (px < 0 || py < 0 || px >= Width || py >= Height) continue;
                body++;
                if (pixels[py * Width + px] != a) covered++;
            }
        // The hands fly back through their own burst once: that frame may be covered more.
        int allowed = label.StartsWith("burst", StringComparison.Ordinal) ? 45 : 25;
        if (body == 0) failures.Add($"{label}: no hand drawn");
        else if (covered * 100 / body > allowed) failures.Add($"{label}: light covers {covered * 100 / body}% of the hands");
        poses++;
    }

    private static void Checks()
    {
        foreach (string pose in new[] { "Open", "Open_L", "Rake_L", "Thrust", "Thrust_L", "Clench_L" })
            if (!drawnPoses.Contains(pose)) failures.Add($"pose {pose} never drawn");
        if (damagingFrames < 8) failures.Add($"only {damagingFrames} damaging frames checked");
        if (voidFrames < 1) failures.Add("crush void not checked");
        if (cooledFrames < 1) failures.Add("crush cooling not checked");

        // Reduced Effects halves debris: count crush shards in both modes.
        int Pieces(bool reduced)
        {
            canvas.Begin(Camera, Width, Height, 1f, reduced, 0);
            canvas.Burst(Camera + new Vector2(640, 360), 4093, 24, 6f, 36f, 5.2f, .16f, DollShardKind.Porcelain);
            canvas.EndRecording();
            device.SetRenderTarget(frame);
            RenderLayers();
            device.SetRenderTarget(null);
            var dots = new Color[light.Width * light.Height];
            if (lightDrawn) light.GetData(dots);
            int n = 0;
            foreach (Color c in dots) if (c.A > 0) n++;
            return n;
        }
        int full = Pieces(false), half = Pieces(true);
        if (!(half > 0 && half < full * 3 / 4)) failures.Add($"Reduced Effects debris {half} dots vs {full}");
    }
}
