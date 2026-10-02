// Offline FNA preview of Choir of the Unmade (2026-10 refresh). Links the production layer canvas, placement,
// DollPixelArt, the real ChoirPresentation.Emit, the pure ChoirConcertRules and the generated DollArtAnchors with
// the compiled DollPixel.fxc and DollChoirEnergy.fxc and the exported DollWeapons PNGs; no Terraria types.
// A small simulation drives the concert with the same pure rules the game uses (clock, glide, seats, notes,
// beam turn), frames render in the in-game order (Back stratum, a 20 x 42 px player silhouette, then the Art and
// Light composites) at zoom 1 on dark #121017 and bright #bac6d6 ground.
// Checks: nothing over budget, every light dot ringed by ink or light, the beam's ramp tones, white spine and pale
// share, beam light inside its collision body, the closed mouth drawn on frame 0, the organ's mouth on the beam
// origin, pipe columns covering the organ art, the organ behind the player and the voices in front.
// Stand-in noise replaces Luminance's noise textures (s1/s2); everything else is the production path.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

internal static class DollChoirPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] private static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_Quit();

    private const int Width = 1280, Height = 720;
    private static readonly Color Dark = new(0x12, 0x10, 0x17), Bright = new(0xba, 0xc6, 0xd6);
    private static readonly Vector2 Camera = new(10001, 6001);
    private static readonly Vector2 Owner = Camera + new Vector2(470, 600);
    private static GraphicsDevice device;
    private static Effect pixelEffect;
    private static SpriteBatch batch;
    private static Texture2D pixel;
    private static RenderTarget2D art, light, frame;
    private static readonly DollWeaponCanvas canvas = new();
    private static readonly ChoirHymnMaterial hymn = new();
    private static ChoirSprites sprites;
    private static readonly Dictionary<string, Color[]> texels = new();
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
        NativeLibrary.SetDllImportResolver(typeof(DollChoirPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL initialization failed");
        IntPtr window = SDL_CreateWindow("Offline Choir of the Unmade", 0, 0, Width, Height, FNA3D_PrepareWindowAttributes() | 0x8);
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
            using var choirMaterial = new Effect(device, File.ReadAllBytes(Path.Combine(shaders, "DollChoirEnergy.fxc")));
            pixelEffect = pixelMaterial;
            hymn.Effect = choirMaterial;
            hymn.NoiseA = Noise(128, 7, 4);
            hymn.NoiseB = Noise(128, 19, 8);
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            string art = Path.Combine(root, "Assets/Textures/Items/DollWeapons");
            sprites = new ChoirSprites(Load(art, "Chorister0"), Load(art, "Chorister1"), Load(art, "Chorister2"), Load(art, "ChoirOrgan"),
                Load(art, "ChoirBaton"));
            batch = new SpriteBatch(device);
            DollChoirPreview.art = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            light = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            frame = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);

            CheckArt();
            frames += Sequence();
            frames += Checks();

            batch.Dispose(); pixel.Dispose(); hymn.NoiseA.Dispose(); hymn.NoiseB.Dispose();
            DollChoirPreview.art.Dispose(); light.Dispose(); frame.Dispose();
            foreach (Texture2D t in new[] { sprites.Chorister0, sprites.Chorister1, sprites.Chorister2, sprites.Organ, sprites.Baton }) t.Dispose();
        }
        finally
        {
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        Console.WriteLine(failures.Count == 0
            ? $"PASS {frames} offline Choir of the Unmade frames in {output}; checks passed. Offline only: stand-in noise, simulated concert, no native game, zoom UI, peers or FPS."
            : $"{failures.Count} check(s) failed; {frames} frames written to {output}.");
        return failures.Count == 0 ? 0 : 1;
    }

    private static Texture2D Load(string folder, string name)
    {
        using var stream = File.OpenRead(Path.Combine(folder, name + ".png"));
        Texture2D texture = Texture2D.FromStream(device, stream);
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        texels[name] = data;
        return texture;
    }

    // Tileable value noise standing in for Luminance's noise textures.
    private static Texture2D Noise(int size, int seed, int cells)
    {
        var random = new Random(seed);
        var lattice = new float[cells, cells];
        for (int y = 0; y < cells; y++) for (int x = 0; x < cells; x++) lattice[x, y] = (float)random.NextDouble();
        var data = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float fx = x * cells / (float)size, fy = y * cells / (float)size;
            int x0 = (int)fx, y0 = (int)fy;
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float a = lattice[x0 % cells, y0 % cells], b = lattice[(x0 + 1) % cells, y0 % cells];
            float c = lattice[x0 % cells, (y0 + 1) % cells], d = lattice[(x0 + 1) % cells, (y0 + 1) % cells];
            float v = MathHelper.Lerp(MathHelper.Lerp(a, b, tx), MathHelper.Lerp(c, d, tx), ty);
            byte g = (byte)(v * 255);
            data[y * size + x] = new Color(g, g, g, (byte)255);
        }
        var texture = new Texture2D(device, size, size);
        texture.SetData(data);
        return texture;
    }

    // ---- Simulation (the pure rules, as the projectiles apply them) -----------------------------------------

    private sealed class Sim
    {
        internal readonly int Voices;
        internal readonly NVector2[] Position, Velocity;
        internal readonly int[] Life, Facing;
        internal NVector2 Stage, Target;
        internal bool StageSet, Aimed;
        internal int Clock, PreviousClock;
        internal float BeamAngle;
        internal readonly List<Note> Notes = new();
        internal readonly List<ChoirSpark> Sparks = new();
        internal float BatonUse = -1;   // use progress while summoning, else -1

        internal sealed class Note
        {
            internal NVector2 Position, Velocity;
            internal float Age;
            internal int Pitch;
            internal readonly Vector2[] Trail = new Vector2[ChoirDrawState.TrailPoints];
            internal int TrailCount, TrailHead;
        }

        internal Sim(int voices, NVector2 target)
        {
            Voices = voices;
            Target = target;
            Position = new NVector2[voices];
            Velocity = new NVector2[voices];
            Life = new int[voices];
            Facing = new int[voices];
            for (int i = 0; i < voices; i++)
            {
                Position[i] = N(Owner) + ChoirConcertRules.CloudSeat(i, voices, 0);
                Life[i] = 400;
                Facing[i] = 1;
            }
        }

        internal void Step(bool running)
        {
            PreviousClock = Clock;
            Clock = ChoirConcertRules.Advance(Clock, running);
            Stage = ChoirConcertRules.EaseStage(Stage, StageSet, N(Owner));
            StageSet = true;
            for (int i = 0; i < Voices; i++)
            {
                NVector2 destination = ChoirConcertRules.Destination(N(Owner), Stage, i, Voices, Clock, Life[i]);
                Velocity[i] = ChoirConcertRules.GlideVelocity(Position[i], Velocity[i], destination);
                Position[i] += Velocity[i];
                Life[i]++;
                bool chorus = ChoirConcertRules.Warning(Clock) || ChoirConcertRules.Live(Clock);
                float facing = Target.X - (chorus ? Stage.X : Position[i].X);
                if (MathF.Abs(facing) > .3f) Facing[i] = facing >= 0 ? 1 : -1;
                int note = ChoirConcertRules.NoteAt(Clock, i);
                if (note >= 0 && Clock > PreviousClock)
                {
                    NVector2 mouth = Position[i] + new NVector2(ChoirConcertRules.Mouth.X * Facing[i], ChoirConcertRules.Mouth.Y);
                    NVector2 aim = NVector2.Normalize(Target - mouth);
                    Notes.Add(new Note { Position = mouth, Velocity = (aim * ChoirConcertRules.NoteLaunch + new NVector2(0, -ChoirConcertRules.NoteLift)) / 2,
                        Pitch = ChoirConcertRules.NotePitch(note, ChoirConcertRules.Part(i)) });
                }
            }
            for (int n = Notes.Count - 1; n >= 0; n--)
            {
                Note note = Notes[n];
                for (int update = 0; update < 2; update++)
                {
                    note.Age += .5f;
                    if (note.Age >= ChoirConcertRules.NoteHomingDelay)
                    {
                        NVector2 delta = Target - note.Position;
                        float current = MathF.Atan2(note.Velocity.Y, note.Velocity.X), wanted = MathF.Atan2(delta.Y, delta.X);
                        float turn = Math.Clamp(MathF.IEEERemainder(wanted - current, MathF.Tau), -.12f, .12f);
                        float speed = note.Velocity.Length() + (ChoirConcertRules.NoteSpeed / 2 - note.Velocity.Length()) * (1 - MathF.Exp(-.16f));
                        note.Velocity = new NVector2(MathF.Cos(current + turn), MathF.Sin(current + turn)) * speed;
                    }
                    note.Position += note.Velocity;
                }
                note.Trail[note.TrailHead] = V(note.Position);
                note.TrailHead = (note.TrailHead + 1) % note.Trail.Length;
                note.TrailCount = Math.Min(note.Trail.Length, note.TrailCount + 1);
                if (NVector2.Distance(note.Position, Target) < 26)
                {
                    Sparks.Add(new ChoirSpark { At = V(note.Position), Age = 0, Seed = n * 31 + Clock });
                    Notes.RemoveAt(n);
                }
            }
            for (int s = Sparks.Count - 1; s >= 0; s--)
            {
                ChoirSpark spark = Sparks[s];
                spark.Age++;
                if (spark.Age > 16) Sparks.RemoveAt(s); else Sparks[s] = spark;
            }
            if (ChoirConcertRules.Warning(Clock) || ChoirConcertRules.Live(Clock))
            {
                NVector2 delta = Target - Stage;
                BeamAngle = ChoirConcertRules.Turn(BeamAngle, MathF.Atan2(delta.Y, delta.X), !Aimed);
                Aimed = true;
            }
            else Aimed = false;
        }

        internal void Fill(ChoirDrawState s, float cancelAge = -1, int cancelClock = 0)
        {
            s.Clear();
            s.Seed = 3;
            s.Stage = V(Stage);
            s.Clock = Clock;
            s.VoiceTotal = Voices;
            for (int i = 0; i < Voices && s.VoiceCount < ChoirDrawState.MaxVoices; i++)
                s.Voices[s.VoiceCount++] = new ChoirVoiceDraw
                {
                    Center = V(Position[i]), Velocity = V(Velocity[i]), Facing = Facing[i], Variant = (i * 2 + 1) % 3, Ordinal = i, Life = Life[i],
                };
            foreach (Note note in Notes)
            {
                if (s.NoteCount >= ChoirDrawState.MaxNotes) break;
                s.Notes[s.NoteCount++] = new ChoirNoteDraw
                {
                    Center = V(note.Position), Pitch = note.Pitch, Age = note.Age, Trail = note.Trail, TrailCount = note.TrailCount, TrailHead = note.TrailHead,
                };
            }
            foreach (ChoirSpark spark in Sparks)
                if (s.SparkCount < ChoirDrawState.MaxSparks) s.Sparks[s.SparkCount++] = spark;
            if (ChoirConcertRules.Warning(Clock) || ChoirConcertRules.Live(Clock))
            {
                s.Beam = true;
                s.BeamOrigin = V(Stage);
                s.BeamAngle = BeamAngle;
                s.BeamAge = Clock - ChoirConcertRules.Inhale;
                s.ChordVoices = ChoirConcertRules.ChordVoices(Voices);
            }
            if (Clock >= ChoirConcertRules.Release && Clock < ChoirConcertRules.Release + ChoirPresentation.ResidueTicks)
            {
                s.ResidueAge = Clock - ChoirConcertRules.Release;
                s.ResidueOrigin = V(Stage);
                s.ResidueAngle = BeamAngle;
                s.ResidueLength = ChoirConcertRules.BeamLength;
            }
            if (cancelAge >= 0)
            {
                s.CancelAge = cancelAge;
                s.CancelClock = cancelClock;
            }
            float conduct = BatonUse >= 0 ? ChoirConducting.Flick(BatonUse) : ChoirConcertRules.Running(Clock) ? ChoirConducting.Conduct(Clock) : float.NaN;
            if (float.IsFinite(conduct))
            {
                int facing = Target.X >= Owner.X ? 1 : -1;
                float angle = ChoirConducting.World(conduct, facing);
                s.Baton = true;
                s.BatonFacing = facing;
                s.BatonAngle = angle;
                s.BatonGrip = Hand(angle, facing);
                float previous = BatonUse >= 0 ? ChoirConducting.Flick(BatonUse - .04f) : ChoirConducting.Conduct(Clock - 1);
                s.BatonSwing = Math.Clamp(MathF.Abs(conduct - previous) * 4, 0, 1);
                for (int k = ChoirDrawState.BatonTrailPoints - 1; k >= 0; k--)
                {
                    float back = BatonUse >= 0 ? ChoirConducting.Flick(BatonUse - k * .02f) : ChoirConducting.Conduct(Clock - k * .5f);
                    float world = ChoirConducting.World(back, facing);
                    s.BatonTrail[s.BatonTrailCount++] = ChoirPresentation.BatonTipWorld(Hand(world, facing), world);
                }
            }
        }

        // Approximates Player.GetFrontHandPosition for the stand-in player: shoulder, then a 14 px forearm.
        private static Vector2 Hand(float angle, int facing) => Owner + new Vector2(-3 * facing, -6) + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 14;
    }

    private static NVector2 N(Vector2 v) => new(v.X, v.Y);
    private static Point P(Vector2 v) => new((int)MathF.Floor(v.X), (int)MathF.Floor(v.Y));
    private static Vector2 V(NVector2 v) => new(v.X, v.Y);

    // ---- Frames --------------------------------------------------------------------------------------------

    private static void Record(Action<DollWeaponCanvas> scene, bool reduced, double ticks)
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
            if (canvas.HasArt) { Bind(art); canvas.DrawArt(device, pixelEffect, art.Width, art.Height); artDrawn = true; }
            if (canvas.HasLight) { Bind(light); canvas.DrawLight(device, pixelEffect, light.Width, light.Height); lightDrawn = true; }
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

    private static void States()
    {
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
    }

    // One in-game ordered frame: Back stratum, the player silhouette, then the Art and Light composites.
    private static Color[] Frame(Action<DollWeaponCanvas> scene, Color backdrop, bool reduced, string name, double ticks, bool player = true)
    {
        Record(scene, reduced, ticks);
        if (canvas.Dropped > 0) failures.Add($"{name}: {canvas.Dropped} command(s) dropped over budget");
        device.SetRenderTarget(frame);
        RenderLayers();
        device.Clear(backdrop);
        if (canvas.HasBack)
        {
            DollDeviceState state = DollDeviceState.Capture(device, false);
            States();
            canvas.DrawBack(device, pixelEffect, Camera, Matrix.Identity);
            state.Restore(device);
        }
        if (player) Player(backdrop == Bright);
        if (artDrawn || lightDrawn)
        {
            DollDeviceState state = DollDeviceState.Capture(device, false);
            States();
            Vector2 offset = canvas.Origin - Camera;
            if (artDrawn) DollPixelArt.CompositeArt(device, pixelEffect, art, canvas.ArtArea, offset, Matrix.Identity);
            if (lightDrawn) DollPixelArt.CompositeLight(device, pixelEffect, light, artDrawn ? art : null, canvas.LightArea, offset, Matrix.Identity, reduced);
            state.Restore(device);
        }
        device.SetRenderTarget(null);
        var pixels = new Color[Width * Height];
        frame.GetData(pixels);
        if (name is not null)
            using (var file = File.Create(Path.Combine(output, name + ".png"))) frame.SaveAsPng(file, Width, Height);
        return pixels;
    }

    // The 20 x 42 px player silhouette at the owner, plus a stand-in target dummy.
    private static void Player(bool bright)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        Point at = P(Owner - Camera);
        batch.Draw(pixel, new Rectangle(at.X - 10, at.Y - 21, 20, 42), new Color(52, 60, 84));
        batch.Draw(pixel, new Rectangle(at.X - 8, at.Y - 37 + 16, 16, 14), new Color(214, 184, 160));
        Point target = P(TargetWorld - Camera);
        batch.Draw(pixel, new Rectangle(target.X - 18, target.Y - 24, 36, 48), bright ? new Color(120, 98, 78) : new Color(90, 70, 70));
        batch.Draw(pixel, new Rectangle(0, at.Y + 21, Width, Height - at.Y - 21), bright ? new Color(150, 130, 110) : new Color(40, 34, 44));
        batch.End();
    }

    private static readonly Vector2 TargetWorld = Owner + new Vector2(560, -60);

    // The concert at its landmarks, dark and bright, with 4 voices (and 1 and 10 for the formation); a cancel; the
    // summon flick. Each frame is cropped into the contact sheet at 1:1.
    private static int Sequence()
    {
        var shots = new List<(string Label, Color[] Pixels, Color Backdrop)>();
        int frames = 0;
        void Run(int voices, int[] ticks, string tag, bool reduced = false, int cancelAt = -1, int[] summon = null)
        {
            var sim = new Sim(voices, N(TargetWorld));
            var state = new ChoirDrawState();
            int last = ticks[^1];
            int cancelClock = 0;
            for (int t = 1; t <= last; t++)
            {
                bool running = cancelAt < 0 || t < cancelAt;
                if (t == cancelAt) cancelClock = sim.Clock;
                sim.Step(running);
                if (Array.IndexOf(ticks, t) < 0) continue;
                float cancelAge = cancelAt >= 0 && t >= cancelAt ? t - cancelAt : -1;
                foreach (Color backdrop in new[] { Dark, Bright })
                {
                    sim.Fill(state, cancelAge, cancelClock);
                    string name = $"choir-{tag}-{t:D3}-{(backdrop == Dark ? "dark" : "bright")}";
                    Color[] pixels = Frame(c => ChoirPresentation.Emit(c, state, sprites, hymn), backdrop, reduced, name, t);
                    shots.Add(($"{tag} t{t}", pixels, backdrop));
                    frames++;
                }
            }
            if (summon is null) return;
            foreach (int step in summon)
            {
                sim.BatonUse = step / 24f;
                sim.Fill(state);
                for (int i = 0; i < state.VoiceCount; i++) state.Voices[i].Life = step;
                foreach (Color backdrop in new[] { Dark, Bright })
                {
                    string name = $"choir-{tag}-flick{step:D2}-{(backdrop == Dark ? "dark" : "bright")}";
                    Color[] pixels = Frame(c => ChoirPresentation.Emit(c, state, sprites, hymn), backdrop, reduced, name, step);
                    shots.Add(($"{tag} flick {step}", pixels, backdrop));
                    frames++;
                }
            }
        }
        Run(4, new[] { 2, 74, 112, 300, 340, 372, 392, 398, 420, 470, 545, 590, 612 }, "v4");
        Run(1, new[] { 360, 470 }, "v1");
        Run(10, new[] { 100, 470 }, "v10");
        Run(4, new[] { 470 }, "v4reduced", reduced: true);
        Run(4, new[] { 400, 404, 410 }, "v4cancel", cancelAt: 398);
        Run(1, new[] { 1 }, "summon", summon: new[] { 0, 6, 12, 18 });
        Sheet(shots);
        return frames;
    }

    // A contact sheet of 1:1 crops (dark | bright side by side, two moments per row), written as a PNG on the CPU
    // (it is larger than a texture may be).
    private static void Sheet(List<(string Label, Color[] Pixels, Color Backdrop)> shots)
    {
        const int cropX = 190, cropY = 50, cropW = 980, cropH = 610, columns = 4;
        int rows = (shots.Count + columns - 1) / columns, width = columns * cropW, height = rows * cropH;
        var sheet = new Color[width * height];
        for (int i = 0; i < shots.Count; i++)
        {
            int ox = i % columns * cropW, oy = i / columns * cropH;
            Color[] pixels = shots[i].Pixels;
            for (int y = 0; y < cropH; y++)
            for (int x = 0; x < cropW; x++)
            {
                Color c = pixels[(cropY + y) * Width + cropX + x];
                if (x < 2 || y < 2) c = new Color(90, 90, 100);
                sheet[(oy + y) * width + ox + x] = c;
            }
        }
        WritePng(Path.Combine(output, "choir-contact.png"), sheet, width, height);
    }

    private static void WritePng(string path, Color[] pixels, int width, int height)
    {
        var raw = new byte[height * (width * 3 + 1)];
        for (int y = 0; y < height; y++)
        {
            int row = y * (width * 3 + 1);
            for (int x = 0; x < width; x++)
            {
                Color c = pixels[y * width + x];
                raw[row + 1 + x * 3] = c.R; raw[row + 2 + x * 3] = c.G; raw[row + 3 + x * 3] = c.B;
            }
        }
        using var data = new MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(data, System.IO.Compression.CompressionLevel.Optimal, true)) z.Write(raw);
        using var file = File.Create(path);
        file.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; header[9] = 2;
        Chunk(file, "IHDR", header);
        Chunk(file, "IDAT", data.ToArray());
        Chunk(file, "IEND", Array.Empty<byte>());
    }

    private static void Chunk(Stream file, string type, byte[] body)
    {
        var length = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, body.Length);
        file.Write(length);
        byte[] name = System.Text.Encoding.ASCII.GetBytes(type);
        file.Write(name);
        file.Write(body);
        uint crc = 0xFFFFFFFFu;
        foreach (byte[] part in new[] { name, body })
            foreach (byte b in part)
            {
                crc ^= b;
                for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        var tail = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(tail, crc ^ 0xFFFFFFFFu);
        file.Write(tail);
    }

    // ---- Checks --------------------------------------------------------------------------------------------

    // The organ art: the pipe columns cover every opaque texel above the rail; the rail row spans the width; the
    // mouth anchor is see-through. The closed-mouth fix sits on opaque face texels of frame 0.
    private static void CheckArt()
    {
        Color[] organ = texels["ChoirOrgan"];
        int w = sprites.Organ.Width;
        int uncovered = 0;
        for (int y = 0; y < ChoirPresentation.OrganRail; y++)
        for (int x = 0; x < w; x++)
        {
            if (organ[y * w + x].A == 0) continue;
            bool covered = false;
            for (int c = 0; c < ChoirConcertRules.PipeColumns; c++)
                covered |= x >= ChoirPresentation.PipeX0[c] && x <= ChoirPresentation.PipeX1[c] && y >= ChoirPresentation.PipeTop[c];
            if (!covered) uncovered++;
        }
        if (uncovered > 0) failures.Add($"{uncovered} organ texels above the rail are outside every pipe column");
        int rail = 0;
        for (int x = 0; x < w; x++) if (organ[ChoirPresentation.OrganRail * w + x].A > 0) rail++;
        if (rail != w) failures.Add($"the organ's rail row {ChoirPresentation.OrganRail} is {rail}/{w} opaque");
        NVector2 mouth = DollArtAnchors.ChoirOrgan.Mouth;
        if (organ[(int)mouth.Y * w + (int)mouth.X].A != 0) failures.Add("the organ mouth anchor is not see-through");
        for (int variant = 0; variant < 3; variant++)
        {
            if (ChoirPresentation.ClosedMouth[variant] is not { } line) continue;
            Color[] cells = texels["Chorister" + variant];
            for (int x = line.X; x < line.Right; x++)
                if (cells[line.Y * sprites.Chorister0.Width + x].A == 0) failures.Add($"Chorister{variant} closed mouth texel {x},{line.Y} is not on the face");
        }
    }

    private static int Checks()
    {
        int frames = 0;
        // Light outline and beam tones, live chorus with four voices, plain composite on a flat probe backdrop.
        var sim = new Sim(4, N(TargetWorld));
        for (int t = 1; t <= 440; t++) sim.Step(true);
        var state = new ChoirDrawState();
        sim.Fill(state);
        Color probe = new(40, 160, 90);
        Color[] pixels = Frame(c => ChoirPresentation.Emit(c, state, sprites, hymn), probe, true, "check-chorus-outline", 440, false);
        frames++;
        // Only light dots are tested (sprites carry their own authored outline, e.g. the brass rim of the organ's
        // see-through mouth).
        var lightDots = new Color[light.Width * light.Height];
        if (lightDrawn) light.GetData(lightDots);
        Color ink = DollPixelArt.Tone(DollTone.Ink);
        int bare = 0, lit = 0;
        Vector2 offset = canvas.Origin - Camera;
        int ox = (int)offset.X, oy = (int)offset.Y;
        Color Dot(int x, int y)
        {
            int px = ox + x * 2 + 1, py = oy + y * 2 + 1;
            return px < 0 || py < 0 || px >= Width || py >= Height ? probe : pixels[py * Width + px];
        }
        // Dots on the frame edge are skipped: their outside neighbour is beyond the screen, not backdrop.
        for (int y = 2; y < Height / 2 - 2; y++)
        for (int x = 2; x < Width / 2 - 2; x++)
        {
            Color c = Dot(x, y);
            if (c == probe || c == ink || lightDots[y * light.Width + x].A == 0) continue;
            lit++;
            if (Dot(x - 1, y) == probe || Dot(x + 1, y) == probe || Dot(x, y - 1) == probe || Dot(x, y + 1) == probe) bare++;
        }
        if (lit == 0) failures.Add("the chorus frame drew nothing");
        if (bare > 0) failures.Add($"{bare} lit dot(s) border the backdrop without an ink outline");

        // Beam tones: read the Light target alone.
        Record(c => ChoirPresentation.Emit(c, Isolate(state, beam: true), sprites, hymn), false, 440);
        Color[] dots = ReadLight();
        DollTone[] ramp = { DollTone.Plum, DollTone.PlumLight, DollTone.Violet, DollTone.Lilac, DollTone.PearlViolet, DollTone.Bone, DollTone.White };
        var counts = new Dictionary<Color, int>();
        int beamLit = 0, pale = 0, outside = 0;
        float live = state.BeamAge - ChoirConcertRules.WarnTicks, half = ChoirConcertRules.HalfWidth(live);
        Vector2 axis = new(MathF.Cos(state.BeamAngle), MathF.Sin(state.BeamAngle));
        for (int y = 0; y < light.Height; y++)
        for (int x = 0; x < light.Width; x++)
        {
            Color c = dots[y * light.Width + x];
            if (c.A == 0) continue;
            beamLit++;
            counts[c] = counts.TryGetValue(c, out int n) ? n + 1 : 1;
            if (c == DollPixelArt.Tone(DollTone.PearlViolet) || c == DollPixelArt.Tone(DollTone.Bone) || c == DollPixelArt.Tone(DollTone.White)
                || c == DollPixelArt.Tone(DollTone.BrassLight)) pale++;
            Vector2 world = canvas.Origin + new Vector2(x + .5f, y + .5f) * 2 - state.BeamOrigin;
            float along = Vector2.Dot(world, axis), across = MathF.Abs(world.X * axis.Y - world.Y * axis.X);
            if (along < -2 || along > ChoirConcertRules.End(live) + 2 || across > half + 2) outside++;
        }
        int tones = 0;
        foreach (DollTone tone in ramp) if (counts.ContainsKey(DollPixelArt.Tone(tone))) tones++;
        if (tones < 4) failures.Add($"the chorus beam shows {tones} ramp tones");
        if (!counts.ContainsKey(DollPixelArt.Tone(DollTone.White))) failures.Add("the chorus beam has no white-hot spine");
        if (!counts.ContainsKey(DollPixelArt.Tone(DollTone.BrassLight))) failures.Add("the chorus beam shows no brass standing wave");
        if (beamLit == 0 || pale * 100 / beamLit < 40) failures.Add($"the chorus beam's pale share is {(beamLit == 0 ? 0 : pale * 100 / beamLit)}%");
        if (outside > 0) failures.Add($"{outside} beam light dot(s) outside its collision body");
        Console.WriteLine($"beam: {beamLit} lit dots, {tones} ramp tones, pale {pale * 100 / Math.Max(1, beamLit)}%, outside {outside}");

        // The closed mouth on frame 0 (idle) is Iron on every Chorister0 / Chorister2.
        var idle = new Sim(3, N(TargetWorld));
        for (int t = 1; t <= 60; t++) idle.Step(false);
        idle.Fill(state);
        Record(c => ChoirPresentation.Emit(c, state, sprites, hymn), false, 60);
        device.SetRenderTarget(frame);
        RenderLayers();
        device.SetRenderTarget(null);
        var artDots = new Color[art.Width * art.Height];
        art.GetData(artDots);
        for (int i = 0; i < state.VoiceCount; i++)
        {
            ChoirVoiceDraw v = state.Voices[i];
            if (ChoirPresentation.ClosedMouth[v.Variant] is not { } line) continue;
            NVector2 pivot = DollSpritePlacement.Snap(N(v.Center + new Vector2(0, ChoirConcertRules.StandTip.Y)), ChoirPresentation.StandTipTexel, 0,
                v.Facing < 0 ? DollFlip.Horizontal : DollFlip.None);
            NVector2 texel = new(line.X + .5f, line.Y + .5f);
            NVector2 world = DollSpritePlacement.World(texel, ChoirPresentation.StandTipTexel, pivot, 0, v.Facing < 0 ? DollFlip.Horizontal : DollFlip.None);
            Vector2 dot = (V(world) - canvas.Origin) * .5f;
            Color c = artDots[(int)dot.Y * art.Width + (int)dot.X];
            if (c != DollPixelArt.Tone(DollTone.Iron)) failures.Add($"voice {i} (Chorister{v.Variant}) frame 0 mouth is {c}, not Iron");
        }

        // The organ draws behind the player (Back stratum) and its mouth sits on the beam origin S.
        var open = new Sim(4, N(TargetWorld));
        for (int t = 1; t <= 380; t++) open.Step(true);
        open.Fill(state);
        state.Beam = false;
        state.VoiceCount = 0;
        state.Baton = false;
        Color[] organFrame = Frame(c => ChoirPresentation.Emit(c, state, sprites, hymn), Dark, false, "check-organ", 380);
        frames++;
        Point s = P(state.Stage - Camera);
        Color centre = organFrame[s.Y * Width + s.X];
        if (centre != Dark) failures.Add($"the organ mouth at S is {centre}, not see-through");
        int rimHits = 0;
        for (int k = 0; k < 16; k++)
        {
            float angle = k * MathF.Tau / 16, r = ChoirConcertRules.OrganMouthRadius + 3;
            Point p = P(state.Stage + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r - Camera);
            if (organFrame[p.Y * Width + p.X] != Dark) rimHits++;
        }
        if (rimHits < 12) failures.Add($"the organ rim around S is incomplete ({rimHits}/16)");
        return frames;
    }

    private static ChoirDrawState Isolate(ChoirDrawState source, bool beam)
    {
        var s = new ChoirDrawState();
        s.Clear();
        s.Seed = source.Seed;
        s.Stage = source.Stage;
        s.Clock = source.Clock;
        s.VoiceTotal = source.VoiceTotal;
        s.Beam = beam && source.Beam;
        s.BeamOrigin = source.BeamOrigin;
        s.BeamAngle = source.BeamAngle;
        s.BeamAge = source.BeamAge;
        s.ChordVoices = source.ChordVoices;
        return s;
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
