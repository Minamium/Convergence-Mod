// Offline FNA preview of the Lacuna Testament on the shared Doll weapon layer. Links the production presentation
// (LacunaPresentation.Emit*, LacunaEnergy), the layer (DollWeaponLayer contract, DollWeaponCanvas, DollPixelArt,
// DollSpritePlacement), the pure score and art fit, the exported PNGs and the compiled DollPixel.fxc and
// DollLacunaEnergy.fxc, with Luminance's own noise textures read from its package; no Terraria types.
// Each frame follows the in-game order: record, Art and Light targets, backdrop, a 20 x 42 stand-in player, then the
// Art and Light composites in front of it, at zoom 1 on dark #121017 and bright #bac6d6 ground.
// Writes frames, a contact sheet (build-up, release, residue, collapse), a book-rung comparison and an angle sheet,
// and exits non-zero on a failed check: nothing dropped, light ringed by ink, at least four ramp tones and 40% pale
// lit dots in the live beam, a black core with a one-dot pearl lip, the drawn edge on the collision width, the art
// kept in front of its own light, peers at 65% / 60%, Reduced Effects keeping every body.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Score = Convergence.Content.Encounters.FirstSeverance.Rewards.LacunaTestamentScore;

internal static class DollLacunaPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] private static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_Quit();

    private const int Width = 1280, Height = 720;
    private static readonly Color Dark = new(0x12, 0x10, 0x17), Bright = new(0xba, 0xc6, 0xd6), Probe = new(40, 160, 90);
    private static readonly Vector2 Camera = new(20001, 12001);
    // The stand-in player's centre on screen: left of centre so the beam has room.
    private static readonly Vector2 PlayerScreen = new(400, 430);
    private static GraphicsDevice device;
    private static Effect pixelEffect, lacunaEffect;
    private static Texture2D pixel, cloud, flow, book, bookLarge, iris, great;
    private static SpriteBatch batch;
    private static RenderTarget2D art, light, frame;
    private static LacunaEnergy material;
    private static readonly DollWeaponCanvas canvas = new();
    private static readonly List<string> failures = new();
    private static string output;
    private static bool artDrawn, lightDrawn, largeBook, plainComposite;

    private static int Main(string[] args)
    {
        string root = args[0], luminance = args[1], native = args[2];
        output = args[3];
        Directory.CreateDirectory(output);
        IntPtr Resolve(string name, System.Reflection.Assembly assembly, DllImportSearchPath? path)
        {
            string file = Path.Combine(native, name.EndsWith(".dll") ? name : name + ".dll");
            return File.Exists(file) ? NativeLibrary.Load(file) : IntPtr.Zero;
        }
        NativeLibrary.SetDllImportResolver(typeof(DollLacunaPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL initialization failed");
        IntPtr window = SDL_CreateWindow("Offline Lacuna Testament", 0, 0, Width, Height, FNA3D_PrepareWindowAttributes() | 0x8);
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
            using var pixelShader = new Effect(device, File.ReadAllBytes(Path.Combine(shaders, "DollPixel.fxc")));
            using var lacunaShader = new Effect(device, File.ReadAllBytes(Path.Combine(shaders, "DollLacunaEnergy.fxc")));
            pixelEffect = pixelShader;
            lacunaEffect = lacunaShader;
            cloud = Noise(luminance, "WavyBlotchNoise");
            flow = Noise(luminance, "TurbulentNoise");
            material = new LacunaEnergy(() => lacunaEffect, () => cloud, () => flow);
            string textures = Path.Combine(root, "Assets/Textures/Items/DollWeapons");
            book = Png(Path.Combine(textures, LacunaArtFit.Book + ".png"));
            bookLarge = Png(Path.Combine(textures, LacunaArtFit.LargeBook + ".png"));
            iris = Png(Path.Combine(textures, LacunaArtFit.Iris + ".png"));
            great = Png(Path.Combine(textures, LacunaArtFit.Great + ".png"));
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            batch = new SpriteBatch(device);
            art = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            light = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            frame = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);

            Checks();
            frames += Sheets();
            foreach (Texture2D t in new[] { pixel, cloud, flow, book, bookLarge, iris, great }) t.Dispose();
            batch.Dispose(); art.Dispose(); light.Dispose(); frame.Dispose();
        }
        finally
        {
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        Console.WriteLine(failures.Count == 0
            ? $"PASS {frames} offline Lacuna Testament frames in {output}; checks passed. Offline only: no native game, input, network, peers or FPS."
            : $"{failures.Count} check(s) failed; {frames} frames written to {output}.");
        return failures.Count == 0 ? 0 : 1;
    }

    // ---- Assets --------------------------------------------------------------------------------------------------

    private static Texture2D Png(string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
    }

    // A Luminance noise texture from its .tmod package (TMOD header, file table, deflated rawimg).
    private static Texture2D Noise(string package, string name)
    {
        using var file = File.OpenRead(package);
        using var reader = new BinaryReader(file);
        if (System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4)) != "TMOD") throw new InvalidDataException("TMOD magic");
        reader.ReadString(); reader.ReadBytes(276); reader.ReadInt32(); reader.ReadString(); reader.ReadString();
        int count = reader.ReadInt32(), offset = 0, position = -1, length = 0, stored = 0;
        for (int i = 0; i < count; i++)
        {
            string path = reader.ReadString();
            int size = reader.ReadInt32(), compressed = reader.ReadInt32();
            if (path == "Assets/Noise/" + name + ".rawimg") { position = offset; length = size; stored = compressed; }
            offset += compressed;
        }
        if (position < 0) throw new FileNotFoundException("Luminance noise: " + name);
        file.Position += position;
        using var bytes = new MemoryStream(reader.ReadBytes(stored));
        using Stream data = length == stored ? bytes : new DeflateStream(bytes, CompressionMode.Decompress);
        using var raw = new BinaryReader(data);
        if (raw.ReadInt32() != 1) throw new InvalidDataException("Raw image version");
        int width = raw.ReadInt32(), height = raw.ReadInt32();
        var texture = new Texture2D(device, width, height);
        texture.SetData(raw.ReadBytes(width * height * 4));
        return texture;
    }

    // ---- Scenes --------------------------------------------------------------------------------------------------

    internal readonly record struct Shot(string Label, float Age, LacunaPhase Phase = LacunaPhase.Live, float Fade = 0, float Aim = -.18f,
        bool Peer = false, bool Reduced = false, int Facing = 1, bool Pellets = true);

    private static Vector2 Centre => Camera + PlayerScreen;

    // The front hand of a 20 x 42 player at full stretch toward `aim` (approximately Player.GetFrontHandPosition).
    private static Vector2 Hand(float aim, int direction)
        => Centre + new Vector2(-3 * direction, -4) + new Vector2(MathF.Cos(aim), MathF.Sin(aim)) * 13;

    private static LacunaDrawState State(in Shot s)
    {
        int direction = MathF.Cos(s.Aim) < 0 ? -1 : 1;
        return new LacunaDrawState
        {
            Center = Centre, Hand = Hand(s.Aim, direction), Aim = s.Aim, Age = s.Age, Facing = s.Facing, Direction = direction,
            GravDir = 1, Fade = s.Fade, Phase = s.Phase, End = LacunaEnd.Release, Peer = s.Peer, Seed = 4021,
        };
    }

    private static LacunaSprites Sprites() => new(largeBook ? bookLarge : book, iris, great);

    // The scene of one shot: the channel, the pellets in flight from the last shots, and a few bites.
    private static void Scene(DollWeaponCanvas c, Shot s)
    {
        LacunaPresentation.EmitChannel(c, State(s), Sprites(), material);
        if (!s.Pellets || s.Phase != LacunaPhase.Live) return;
        Vector2 target = Centre + new Vector2(520, -40);
        int tick = (int)MathF.Floor(s.Age);
        Span<Vector2> trail = stackalloc Vector2[6];
        for (int back = 0; back < 24; back++)
        {
            int shot = tick - back;
            for (int i = 0; i < Score.Irises; i++)
            {
                if (!Score.ShotAt(shot, i)) continue;
                var seat = Score.Seat(i, s.Facing);
                Vector2 hole = Centre + new Vector2(seat.X, seat.Y);
                float age = s.Age - shot;
                Vector2 direction = Vector2.Normalize(target - hole);
                Vector2 head = hole + direction * (Score.PelletHoleOffset + age * 36f);
                if (Vector2.Distance(head, hole) > Vector2.Distance(target, hole))
                {
                    LacunaPresentation.EmitBite(c, target + new Vector2(0, (i - 3) * 6), age - Vector2.Distance(target, hole) / 36f, shot * 7 + i, s.Peer, false);
                    continue;
                }
                int n = 0;
                for (int k = 5; k >= 1; k--)
                {
                    float back2 = age - k * .5f;
                    if (back2 < 0) continue;
                    trail[n++] = hole + direction * (Score.PelletHoleOffset + back2 * 36f);
                }
                var state = new LacunaPelletState(head, direction, age, i, s.Peer, shot * 7 + i);
                LacunaPresentation.EmitPellet(c, state, trail[..n], material);
            }
        }
        if (s.Age > Score.Fire + 12)
            LacunaPresentation.EmitBite(c, Centre + new Vector2(640, -120), (s.Age - Score.Fire) % 20, 99 + tick / 20, s.Peer, true);
    }

    private static readonly Shot[] Sequence =
    {
        new("0", 1), new("8", 8.5f), new("15", 15.5f), new("60", 60), new("120", 124.5f), new("200", 200.5f),
        new("316", 318), new("331", 331.5f), new("340", 341), new("350", 350.5f), new("356", 356.5f), new("362", 362.5f),
        new("380", 381), new("392", 392.5f), new("404", 405), new("410", 410.5f), new("412", 412.5f), new("417", 418),
        new("450", 452), new("530", 531), new("650", 652), new("770", 772),
        new("R2", 772, LacunaPhase.Fading, 2), new("R6", 772, LacunaPhase.Fading, 6), new("R12", 772, LacunaPhase.Fading, 12),
        new("R20", 772, LacunaPhase.Fading, 20), new("R23", 772, LacunaPhase.Fading, 23),
        new("C200", 200, LacunaPhase.Fading, 5), new("X3", 600, LacunaPhase.Collapse, 3), new("X10", 600, LacunaPhase.Collapse, 10),
    };

    private static int Sheets()
    {
        int frames = 0;
        // The contact sheet: each shot on dark and bright ground, two shots per row.
        Rectangle crop = new((int)PlayerScreen.X - 240, (int)PlayerScreen.Y - 300, 720, 400);
        var tiles = new List<(Color[] Pixels, string Label)>();
        foreach (Shot s in Sequence)
        {
            foreach (bool bright in new[] { false, true })
            {
                Color[] pixels = Frame(c => Scene(c, s), bright ? Bright : Dark, s.Reduced, $"lacuna-{(bright ? "bright" : "dark")}-{s.Label}", true, 1000 + s.Age);
                tiles.Add((Crop(pixels, crop), s.Label + (bright ? "B" : "D")));
                frames++;
            }
        }
        Sheet(tiles, crop.Width, crop.Height, 4, "lacuna-contact.png");

        // Book rung choice: the k=1 and k=2 books at three moments, dark and bright.
        tiles.Clear();
        foreach (bool large in new[] { true, false })
        foreach (float age in new[] { 124.5f, 381f, 600f })
        foreach (bool bright in new[] { false, true })
        {
            largeBook = large;
            var s = new Shot("b", age);
            Color[] pixels = Frame(c => Scene(c, s), bright ? Bright : Dark, false, null, true, 1000 + age);
            tiles.Add((Crop(pixels, crop), (large ? "1" : "2") + (bright ? "B" : "D")));
            frames++;
        }
        largeBook = false;
        Sheet(tiles, crop.Width, crop.Height, 6, "lacuna-book-rungs.png");

        // Aim angles (beam and build), peers and Reduced Effects.
        tiles.Clear();
        Rectangle wide = new((int)PlayerScreen.X - 360, (int)PlayerScreen.Y - 330, 720, Height - ((int)PlayerScreen.Y - 330));
        foreach (float aim in new[] { -1.4f, -.8f, 0f, .6f, MathF.PI - .3f, MathF.PI + .6f })
        {
            var s = new Shot("a", 600, Aim: aim, Facing: MathF.Cos(aim) < 0 ? -1 : 1);
            tiles.Add((Crop(Frame(c => Scene(c, s), Dark, false, null, true, 1600), wide), "A"));
            frames++;
        }
        foreach (Shot s in new[] { new Shot("p", 600, Peer: true), new Shot("p", 381, Peer: true), new Shot("r", 600, Reduced: true), new Shot("r", 331.5f, Reduced: true) })
        foreach (bool bright in new[] { false, true })
        {
            tiles.Add((Crop(Frame(c => Scene(c, s), bright ? Bright : Dark, s.Reduced, null, true, 1000 + s.Age), wide), s.Label.ToUpperInvariant()));
            frames++;
        }
        Sheet(tiles, wide.Width, wide.Height, 4, "lacuna-angles-peers-reduced.png");
        return frames;
    }

    // ---- Frames --------------------------------------------------------------------------------------------------

    private static void Record(Action<DollWeaponCanvas> scene, bool reduced, double ticks = 40)
    {
        canvas.Begin(Camera, Width, Height, 1f, reduced, ticks);
        scene(canvas);
        canvas.EndRecording();
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

    private static Color[] Frame(Action<DollWeaponCanvas> scene, Color backdrop, bool reduced, string name, bool players, double ticks = 40)
    {
        Record(scene, reduced, ticks);
        if (canvas.Dropped > 0) failures.Add($"{name ?? "frame"}: {canvas.Dropped} command(s) over budget");
        device.SetRenderTarget(frame);
        RenderLayers();
        device.Clear(backdrop);
        if (players) Ground(backdrop == Bright);
        Player();
        if (artDrawn || lightDrawn)
        {
            DollDeviceState state = DollDeviceState.Capture(device, false);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            Vector2 offset = canvas.Origin - Camera;
            if (artDrawn) DollPixelArt.CompositeArt(device, pixelEffect, art, canvas.ArtArea, offset, Matrix.Identity);
            if (lightDrawn) DollPixelArt.CompositeLight(device, pixelEffect, light, artDrawn ? art : null, canvas.LightArea, offset, Matrix.Identity, reduced || plainComposite);
            state.Restore(device);
        }
        device.SetRenderTarget(null);
        var pixels = new Color[Width * Height];
        frame.GetData(pixels);
        if (name is not null)
            using (var file = File.Create(Path.Combine(output, name + ".png"))) frame.SaveAsPng(file, Width, Height);
        return pixels;
    }

    private static void Ground(bool bright)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        batch.Draw(pixel, new Rectangle(0, (int)PlayerScreen.Y + 21, Width, 80), bright ? new Color(120, 98, 78) : new Color(48, 40, 52));
        batch.Draw(pixel, new Rectangle(820, 300, 160, 120), bright ? new Color(236, 238, 242) : new Color(92, 84, 104));
        batch.End();
    }

    // The 20 x 42 stand-in player, between the world and the layer's Front stratum as in game.
    private static void Player()
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        Point at = new((int)PlayerScreen.X, (int)PlayerScreen.Y);
        batch.Draw(pixel, new Rectangle(at.X - 10, at.Y - 21, 20, 42), new Color(52, 60, 84));
        batch.Draw(pixel, new Rectangle(at.X - 8, at.Y - 37, 16, 16), new Color(214, 184, 160));
        batch.End();
    }

    private static Color[] Crop(Color[] pixels, Rectangle crop)
    {
        var result = new Color[crop.Width * crop.Height];
        for (int y = 0; y < crop.Height; y++)
            Array.Copy(pixels, (crop.Y + y) * Width + crop.X, result, y * crop.Width, crop.Width);
        return result;
    }

    private static void Sheet(List<(Color[] Pixels, string Label)> tiles, int w, int h, int columns, string name)
    {
        int rows = (tiles.Count + columns - 1) / columns, gap = 4;
        int sheetW = columns * (w + gap) - gap, sheetH = rows * (h + gap) - gap;
        var data = new Color[sheetW * sheetH];
        Array.Fill(data, new Color(70, 70, 76));
        for (int t = 0; t < tiles.Count; t++)
        {
            int ox = t % columns * (w + gap), oy = t / columns * (h + gap);
            for (int y = 0; y < h; y++)
                Array.Copy(tiles[t].Pixels, y * w, data, (oy + y) * sheetW + ox, w);
            Label(data, sheetW, ox + 6, oy + 6, tiles[t].Label);
        }
        using var texture = new Texture2D(device, sheetW, sheetH);
        texture.SetData(data);
        using var file = File.Create(Path.Combine(output, name));
        texture.SaveAsPng(file, sheetW, sheetH);
    }

    // A 3 x 5 bitmap label (digits and a few capitals) drawn 3x, on a dark box.
    private static readonly Dictionary<char, string> glyphs = new()
    {
        ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "111001111100111", ['3'] = "111001111001111",
        ['4'] = "101101111001001", ['5'] = "111100111001111", ['6'] = "111100111101111", ['7'] = "111001010010010",
        ['8'] = "111101111101111", ['9'] = "111101111001111", ['A'] = "010101111101101", ['B'] = "110101110101110",
        ['C'] = "111100100100111", ['D'] = "110101101101110", ['P'] = "111101111100100", ['R'] = "110101110101101",
        ['X'] = "101101010101101",
    };

    private static void Label(Color[] data, int stride, int x, int y, string text)
    {
        const int s = 3;
        int width = text.Length * 4 * s + s;
        for (int yy = 0; yy < 7 * s; yy++)
            for (int xx = 0; xx < width; xx++)
                data[(y + yy) * stride + x + xx] = new Color(18, 16, 23);
        for (int k = 0; k < text.Length; k++)
        {
            if (!glyphs.TryGetValue(text[k], out string glyph)) continue;
            for (int gy = 0; gy < 5; gy++)
                for (int gx = 0; gx < 3; gx++)
                    if (glyph[gy * 3 + gx] == '1')
                        for (int py = 0; py < s; py++)
                            for (int px = 0; px < s; px++)
                                data[(y + s + gy * s + py) * stride + x + s + k * 4 * s + gx * s + px] = new Color(255, 230, 120);
        }
    }

    // ---- Checks --------------------------------------------------------------------------------------------------

    private static Color Tone(DollTone tone) => DollPixelArt.Palette[(int)tone];

    private static Color[] ReadLight()
    {
        device.SetRenderTarget(frame);
        RenderLayers();
        device.SetRenderTarget(null);
        var dots = new Color[light.Width * light.Height];
        if (lightDrawn) light.GetData(dots);
        return dots;
    }

    private static Color[] ReadArt()
    {
        var dots = new Color[art.Width * art.Height];
        if (artDrawn) art.GetData(dots);
        return dots;
    }

    private static void Checks()
    {
        CheckBeamMaterial();
        CheckOutlineAndBudget();
        CheckArtInFront();
        CheckPeersAndReduced();
    }

    // The live beam (horizontal aim): >= 4 ramp tones, >= 40% pale lit dots, a black core on the axis, exactly one
    // pearl lip dot between the core and the rim, and the pearl silhouette on the collision edge (+-1 dot).
    private static void CheckBeamMaterial()
    {
        foreach (float age in new[] { 452f, 600f, 772f, 1200f })
        {
            var s = new Shot("check", age, Aim: 0, Pellets: false);
            Record(c => LacunaPresentation.EmitChannel(c, State(s), Sprites(), material), false);
            Color[] dots = ReadLight();
            Vector2 muzzle = canvas.ToDot(Centre + new Vector2(Score.MuzzleDistance, 0));
            int axisY = (int)MathF.Floor(muzzle.Y);
            var ramp = new HashSet<Color>();
            int lit = 0, pale = 0, voidDots = 0;
            DollTone[] rampTones = { DollTone.Plum, DollTone.PlumLight, DollTone.Violet, DollTone.Lilac, DollTone.PearlViolet, DollTone.Bone, DollTone.White };
            Color[] paleTones = { Tone(DollTone.Pearl), Tone(DollTone.PearlViolet), Tone(DollTone.Bone), Tone(DollTone.White) };
            int x0 = (int)muzzle.X + 60, x1 = Math.Min(light.Width - 2, (int)muzzle.X + 520);
            for (int y = 0; y < light.Height; y++)
            for (int x = x0; x < x1; x++)
            {
                Color c = dots[y * light.Width + x];
                if (c.A == 0) continue;
                if (c.A < 250) { voidDots++; continue; }
                lit++;
                foreach (DollTone t in rampTones) if (c == Tone(t)) ramp.Add(c);
                if (Array.IndexOf(paleTones, c) >= 0) pale++;
            }
            if (ramp.Count < 4) failures.Add($"beam at {age}: {ramp.Count} ramp tones");
            if (lit == 0 || pale * 100 / lit < 40) failures.Add($"beam at {age}: pale share {(lit == 0 ? 0 : pale * 100 / lit)}% of {lit} lit dots");
            if (voidDots < 200) failures.Add($"beam at {age}: only {voidDots} void dots");
            float halfEdge = Score.Width(age) * .25f;
            int lipChecks = 0, lipFailures = 0, edgeMisses = 0;
            for (int x = x0; x < x1; x += 7)
            {
                Color centre = dots[axisY * light.Width + x];
                if (!(centre.A > 0 && centre.A < 250)) { failures.Add($"beam at {age}: axis dot at x {x} is not void ({centre})"); break; }
                foreach (int sign in new[] { -1, 1 })
                {
                    // Walk out from the axis: void dots, then one pearl or white lip dot, then rim.
                    int y = axisY, steps = 0;
                    while (steps < 80 && dots[y * light.Width + x].A is > 0 and < 250) { y += sign; steps++; }
                    Color lip = dots[y * light.Width + x], after = dots[(y + sign) * light.Width + x];
                    lipChecks++;
                    if (!(lip == Tone(DollTone.Pearl) || lip == Tone(DollTone.White)) || after == Tone(DollTone.Pearl) || after.A < 250) lipFailures++;
                    // The silhouette: a pearl-violet (or pulse bone) dot within one dot of the collision edge.
                    bool edge = false;
                    for (int d = -1; d <= 1; d++)
                    {
                        int ey = (int)MathF.Floor(muzzle.Y + sign * (halfEdge - .5f)) + d;
                        Color e = dots[ey * light.Width + x];
                        if (e == Tone(DollTone.PearlViolet) || e == Tone(DollTone.Bone)) edge = true;
                    }
                    if (!edge) edgeMisses++;
                }
            }
            if (lipFailures * 10 > lipChecks) failures.Add($"beam at {age}: {lipFailures} of {lipChecks} cross-sections lack a one-dot pearl lip");
            if (edgeMisses * 10 > lipChecks) failures.Add($"beam at {age}: {edgeMisses} of {lipChecks} cross-sections lack the silhouette on the collision edge");
        }
    }

    // Every shot of the sequence: nothing dropped, every light dot ringed by ink or light (flat probe ground). The
    // fading and crumbling shots (and an iris's first dithered ticks) expose sprite texels by the layer's own fade and
    // dissolve contract, so they are checked for drops and content only.
    private static void CheckOutlineAndBudget()
    {
        foreach (Shot s in Sequence)
        {
            bool dithered = s.Phase != LacunaPhase.Live || s.Age - Score.Birth(Math.Max(0, Score.OpenCount(s.Age) - 1)) < 4;
            // The glow-free composite, so a glow-tinted ground pixel is not mistaken for an unringed dot.
            plainComposite = true;
            Color[] pixels = Frame(c => Scene(c, s), Probe, s.Reduced, null, false, 1000 + s.Age);
            plainComposite = false;
            // Light only: the exported sprites carry their own (owner-approved) outlines, and a few of their edge
            // texels (the book's clasp, an iris's brass foot) are authored against transparency.
            Color[] arts = ReadArt();
            Vector2 offset = canvas.Origin - Camera;
            bool ArtDot(int x, int y)
            {
                int dx = (int)MathF.Floor((x - offset.X) / 2), dy = (int)MathF.Floor((y - offset.Y) / 2);
                return dx >= 0 && dy >= 0 && dx < art.Width && dy < art.Height && arts[dy * art.Width + dx].A > 0;
            }
            int bare = 0, lit = 0;
            for (int y = 1; y < Height - 1; y += 2)
            for (int x = 1; x < Width - 1; x += 2)
            {
                if (!Lit(pixels, x, y) || ArtDot(x, y)) continue;
                lit++;
                if (Bare(pixels, x, y)) bare++;
            }
            if (lit == 0 && s.Phase == LacunaPhase.Live) failures.Add($"shot {s.Label}: nothing drawn");
            if (bare > 0 && !dithered)
            {
                failures.Add($"shot {s.Label}: {bare} dot(s) border the ground without an ink outline");
                // Mark them for inspection.
                for (int y = 1; y < Height - 1; y += 2)
                for (int x = 1; x < Width - 1; x += 2)
                    if (Lit(pixels, x, y) && !ArtDot(x, y) && Bare(pixels, x, y)) pixels[y * Width + x] = pixels[y * Width + x - 1] = Color.Red;
                using var marked = new Texture2D(device, Width, Height);
                marked.SetData(pixels);
                using var file = File.Create(Path.Combine(output, $"bare-{s.Label}.png"));
                marked.SaveAsPng(file, Width, Height);
            }
        }
    }

    private static bool Near(Color[] pixels, int x, int y, int r, int g, int b)
    {
        Color c = pixels[y * Width + x];
        return c.R == r && c.G == g && c.B == b;
    }

    // Ink composited at any opacity over the probe ground (a fading ring's outline) is outline, not a lit dot.
    private static bool InkOverProbe(Color c)
    {
        Color ink = Tone(DollTone.Ink);
        float a = (Probe.G - c.G) / (float)(Probe.G - ink.G);
        if (a < -.02f || a > 1.02f) return false;
        return MathF.Abs(c.R - (ink.R * a + Probe.R * (1 - a))) <= 3 && MathF.Abs(c.B - (ink.B * a + Probe.B * (1 - a))) <= 3;
    }

    // A drawn weapon dot: neither ground, ink nor the stand-in player.
    private static bool Lit(Color[] pixels, int x, int y)
    {
        Color c = pixels[y * Width + x];
        return c != Probe && !InkOverProbe(c) && !Near(pixels, x, y, 52, 60, 84) && !Near(pixels, x, y, 214, 184, 160);
    }

    private static bool Bare(Color[] pixels, int x, int y)
        => pixels[y * Width + x - 2] == Probe || pixels[y * Width + x + 2] == Probe
            || pixels[(y - 2) * Width + x] == Probe || pixels[(y + 2) * Width + x] == Probe;

    // The art stays in front of its own light: the great aperture's ring is not painted over by the beam, the book
    // and the seated irises keep their bodies (only their see-through holes carry the void).
    private static void CheckArtInFront()
    {
        foreach (float age in new[] { 124.5f, 331.5f, 381f, 600f, 772f })
        {
            var s = new Shot("art", age, Pellets: false);
            Record(c => LacunaPresentation.EmitChannel(c, State(s), Sprites(), material), false);
            Color[] lights = ReadLight(), arts = ReadArt();
            int body = 0, covered = 0;
            for (int i = 0; i < arts.Length; i++)
            {
                if (arts[i].A == 0) continue;
                body++;
                if (lights[i].A > 0) covered++;
            }
            if (body == 0) failures.Add($"art at {age}: no sprite drawn");
            else if (covered * 100 / body > 12) failures.Add($"art at {age}: light covers {covered * 100 / body}% of the sprites");
        }
    }

    // Another player's light at 65% and void at 60%; Reduced Effects keeps every body and the beam.
    private static void CheckPeersAndReduced()
    {
        var owner = new Shot("o", 600, Aim: 0, Pellets: false);
        var peer = owner with { Peer = true };
        Record(c => LacunaPresentation.EmitChannel(c, State(peer), Sprites(), material), false);
        Color[] dots = ReadLight();
        var alphas = new HashSet<byte>();
        // Past the great aperture's ring, where nothing else overlaps the beam.
        int clear = (int)canvas.ToDot(Centre + new Vector2(Score.MuzzleDistance + LacunaArtFit.GreatOuterRadius + 8, 0)).X;
        for (int y = 0; y < light.Height; y++)
            for (int x = clear; x < light.Width; x++)
                if (dots[y * light.Width + x].A > 0) alphas.Add(dots[y * light.Width + x].A);
        byte lightA = (byte)MathF.Round(255 * DollWeaponCanvas.PeerLightAlpha), voidA = (byte)MathF.Round(255 * DollWeaponCanvas.PeerVoidAlpha);
        foreach (byte a in alphas)
            if (Math.Abs(a - lightA) > 1 && Math.Abs(a - voidA) > 1) { failures.Add($"peer beam has alpha {a}, expected {lightA} light or {voidA} void"); break; }
        int Count(Shot s, bool reduced, bool artOnly)
        {
            Record(c => LacunaPresentation.EmitChannel(c, State(s), Sprites(), material), reduced);
            Color[] read = ReadLight();
            Color[] target = artOnly ? ReadArt() : read;
            int n = 0;
            foreach (Color c in target) if (c.A > 0) n++;
            return n;
        }
        foreach (float age in new[] { 124.5f, 381f, 600f })
        {
            var s = new Shot("r", age, Pellets: false);
            int artFull = Count(s, false, true), artReduced = Count(s, true, true);
            if (artFull != artReduced) failures.Add($"Reduced Effects changed the bodies at {age} ({artReduced} vs {artFull} texels)");
            int lightFull = Count(s, false, false), lightReduced = Count(s, true, false);
            if (lightReduced < lightFull * 7 / 10) failures.Add($"Reduced Effects dropped the live light at {age} ({lightReduced} vs {lightFull} dots)");
        }
    }
}
