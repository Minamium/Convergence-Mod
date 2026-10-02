// Offline FNA/D3D11 proof of the Pale Meridian presentation: the real MeridianPresentation.Emit* sequence (linked
// production code) with the real compiled DollPixel.fxc and DollMeridianEnergy.fxc, the exported DollWeapons PNGs and
// the Luminance noise textures read from the installed Luminance package. Each frame follows the in-game order of the
// shared Doll weapon layer: commands are recorded, Front sprites render into the half-resolution Art target and light
// into the Light target; then the backdrop, the Back stratum (the wind-up key), a stand-in player in Terraria's
// 20x42 hitbox and the two composites in front of it. Zoom 1.
// Frames walk the build-up (arrival, notes, part flights and seats, the key, the wind, the overcharge with heavy
// rounds), a tier-3 and a tier-2 release (forecast, meridian, split, lattice, residue), a failed release, a peer's
// view and Reduced Effects, on dark #121017 and bright #bac6d6 ground, and are composed into contact sheets.
// Checks: nothing over budget and no material error; every light dot is an exact palette tone (own light) and is
// ringed by ink or light; the live meridian body shows 4+ ramp tones with a pale (pearl-violet, bone, white) share of
// at least 40%; the gun's art stays readable (little of it under light) and is drawn every held frame; the wind-up
// key is on the Back stratum and no front art covers the stand-in's face; a peer's light is drawn at 65%; Reduced
// Effects applies no glow pass; eight players' weapons fit the budget in a handful of light batches. Offline only:
// no Terraria, no game launch, no peer, FPS or in-game readability claim.
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

internal static class MeridianPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] private static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_Quit();

    private const int Width = 1600, Height = 900;
    private static readonly Color Dark = new(0x12, 0x10, 0x17), Bright = new(0xba, 0xc6, 0xd6), Probe = new(40, 160, 90);
    private static readonly Vector2 Camera = new(10001, 6001);
    private static readonly Vector2 PlayerScreen = new(420, 520);
    private static GraphicsDevice device;
    private static Effect pixelEffect, meridianEffect;
    private static SpriteBatch batch;
    private static Texture2D pixel, noiseA, noiseB;
    private static RenderTarget2D art, light, frame;
    private static readonly DollWeaponCanvas canvas = new();
    private static readonly MeridianArt meridian = new();
    private static readonly List<string> failures = new();
    private static readonly List<string> notes = new();
    private static string output;
    private static bool artDrawn, lightDrawn, backDrawn;
    private static CountingMaterial energy;
    private static int frames;

    private static Vector2 Player => Camera + PlayerScreen;

    private static int Main(string[] args)
    {
        string root = args[0], native = args[1], luminance = args[2];
        output = args[3];
        Directory.CreateDirectory(output);
        IntPtr Resolve(string name, System.Reflection.Assembly assembly, DllImportSearchPath? path)
        {
            string file = Path.Combine(native, name.EndsWith(".dll") ? name : name + ".dll");
            return File.Exists(file) ? NativeLibrary.Load(file) : IntPtr.Zero;
        }
        NativeLibrary.SetDllImportResolver(typeof(MeridianPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL initialization failed");
        IntPtr window = SDL_CreateWindow("Offline Pale Meridian", 0, 0, Width, Height, FNA3D_PrepareWindowAttributes() | 0x8);
        if (window == IntPtr.Zero) throw new Exception("Hidden device unavailable");
        try
        {
            using var graphics = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, new PresentationParameters
            {
                DeviceWindowHandle = window, BackBufferWidth = Width, BackBufferHeight = Height, BackBufferFormat = SurfaceFormat.Color,
                IsFullScreen = false, DepthStencilFormat = DepthFormat.None, PresentationInterval = PresentInterval.Immediate,
            });
            device = graphics;
            string shaders = Path.Combine(root, "Assets/AutoloadedEffects/Shaders");
            using var pixelFx = new Effect(device, File.ReadAllBytes(Path.Combine(shaders, "DollPixel.fxc")));
            using var meridianFx = new Effect(device, File.ReadAllBytes(Path.Combine(shaders, "DollMeridianEnergy.fxc")));
            pixelEffect = pixelFx;
            meridianEffect = meridianFx;
            noiseA = LuminanceNoise(luminance, "TurbulentNoise");
            noiseB = LuminanceNoise(luminance, "WavyBlotchNoise");
            string textures = Path.Combine(root, "Assets/Textures/Items/DollWeapons");
            meridian.Gun = Load(Path.Combine(textures, "MeridianGun.png"));
            meridian.Bare = Load(Path.Combine(textures, "MeridianBare.png"));
            meridian.Parts = Load(Path.Combine(textures, "MeridianParts.png"));
            meridian.Key = Load(Path.Combine(textures, "MeridianKey.png"));
            energy = new CountingMaterial(new MeridianEnergyMaterial(() => meridianEffect, () => noiseA, () => noiseB));
            meridian.Energy = energy;
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            batch = new SpriteBatch(device);
            art = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            light = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            frame = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);

            Checks();
            Sheets();

            batch.Dispose(); pixel.Dispose(); noiseA.Dispose(); noiseB.Dispose();
            meridian.Gun.Dispose(); meridian.Bare.Dispose(); meridian.Parts.Dispose(); meridian.Key.Dispose();
            art.Dispose(); light.Dispose(); frame.Dispose();
        }
        finally
        {
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
        foreach (string note in notes) Console.WriteLine("NOTE " + note);
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        Console.WriteLine(failures.Count == 0
            ? $"PASS {frames} offline Pale Meridian frames in {output}. Offline only; no native game, peer, FPS or in-game readability acceptance."
            : $"{failures.Count} check(s) failed; {frames} frames rendered into {output}.");
        return failures.Count == 0 ? 0 : 1;
    }

    // The real material, counting how often each of its passes is applied in a frame.
    private sealed class CountingMaterial : IDollEnergyMaterial
    {
        private readonly IDollEnergyMaterial inner;
        internal readonly int[] Applied = new int[4];

        internal CountingMaterial(IDollEnergyMaterial inner) => this.inner = inner;
        internal void Reset() => Array.Clear(Applied);

        public bool Apply(GraphicsDevice device, in DollEnergyContext context, int pass)
        {
            if ((uint)pass < (uint)Applied.Length) Applied[pass]++;
            return inner.Apply(device, in context, pass);
        }
    }

    private static Texture2D Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
    }

    // A Luminance noise texture straight out of the installed .tmod (its raw image entry), as tools/fixtures/DollCorePreview does.
    private static Texture2D LuminanceNoise(string package, string name)
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

    // ---- Scenes --------------------------------------------------------------------------------

    // One frame of the weapon: the held gun (or its pack-away), its rounds and its release lines. Scene.Lobby draws
    // eight players' weapons at once (the first is the local player's, the rest are peers): half overcharging, half
    // releasing tier 3, to load the shared budget the way a full lobby does.
    private sealed record Scene(string Label, float Age, float Aim, bool Released = false, float Stowed = 0, int Fired = 0, int Tier = 0,
        float Node = 360, bool Peer = false, bool Reduced = false, float Gravity = 1, bool Lines = true, bool Gun = true, bool Rounds = true,
        bool Lobby = false);

    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

    internal const int LobbySize = 8;

    private static Vector2 LobbyOffset(int i) => new(250 * (i % 4), -280 * (i / 4));

    private static Scene LobbyMember(Scene scene, int i)
        => i % 2 == 0 ? scene with { Lobby = false, Peer = i > 0, Age = 384.5f, Released = false, Stowed = 0, Fired = 0, Tier = 0 }
            : scene with { Lobby = false, Peer = true, Age = 420, Released = true, Stowed = 12 + i, Fired = 3, Tier = 3, Node = 300 + 40 * i };

    // Every weapon of the frame, pass by pass in MeridianPass order, as MeridianLayerSource records them in game.
    private static void Draw(DollWeaponCanvas c, Scene scene)
    {
        int members = scene.Lobby ? LobbySize : 1;
        Span<Vector2> trail = stackalloc Vector2[6];
        for (int pass = 0; pass < MeridianPresentation.PassCount; pass++)
        {
            var stage = (MeridianPass)pass;
            for (int member = 0; member < members; member++)
            {
                Scene weapon = scene.Lobby ? LobbyMember(scene, member) : scene;
                Vector2 at = Player + (scene.Lobby ? LobbyOffset(member) : Vector2.Zero);
                int seed = 4217 + 977 * member;
                float age = weapon.Age;
                Vector2 aim = Unit(weapon.Aim);
                if (stage <= MeridianPass.LineLight)
                {
                    if (!weapon.Released || weapon.Fired <= 0 || !weapon.Lines) continue;
                    Vector2 origin = at + aim * PaleMeridianRig.MuzzleReach;
                    MeridianPresentation.EmitLine(c, meridian, new MeridianLineView(origin, aim, weapon.Node, weapon.Fired, false, weapon.Stowed,
                        weapon.Peer, seed), stage);
                    if (PaleMeridianScore.HasLattice(weapon.Fired))
                        MeridianPresentation.EmitLine(c, meridian, new MeridianLineView(origin, aim, weapon.Node, weapon.Fired, true,
                            PaleMeridianLattice.LatticeStart(weapon.Node) + weapon.Stowed, weapon.Peer, seed), stage);
                }
                else if (stage == MeridianPass.Gun)
                {
                    if (weapon.Gun)
                        MeridianPresentation.EmitGun(c, meridian, new MeridianGunView(at, weapon.Aim, age, weapon.Released, weapon.Stowed, weapon.Fired,
                            weapon.Gravity, weapon.Peer, seed));
                }
                else if (weapon.Rounds)
                {
                    // Rounds already fired fly straight on along the aim (no homing offline).
                    float now = weapon.Released ? age + weapon.Stowed : age;
                    for (int shotAge = Math.Max(1, (int)now - 40); shotAge <= (int)MathF.Min(now, age); shotAge++)
                    {
                        MeridianShot kind = PaleMeridianScore.Shot(shotAge);
                        if (kind == MeridianShot.None) continue;
                        float flight = now - shotAge;
                        float speed = kind == MeridianShot.Heavy ? PaleMeridianScore.HeavyLaunch : PaleMeridianScore.NoteSpeed;
                        Vector2 muzzle = at + aim * PaleMeridianRig.MuzzleReach;
                        Vector2 head = muzzle + aim * speed * flight;
                        if (Vector2.Distance(head, at) > 1500) continue;
                        var round = new MeridianRoundView(head, aim * speed, kind, flight, weapon.Peer, seed + shotAge);
                        if (stage == MeridianPass.RoundHead)
                        {
                            MeridianPresentation.EmitRoundHead(c, meridian, round);
                            continue;
                        }
                        int points = MeridianPresentation.TrailPoints(kind), n = 0;
                        for (int k = points; k >= 1; k--)
                        {
                            float back = flight - k * .5f;
                            if (back < 0) continue;
                            trail[n++] = muzzle + aim * speed * back;
                        }
                        MeridianPresentation.EmitRoundWake(c, meridian, round, trail[..n]);
                    }
                }
            }
        }
    }

    private static List<Scene> Build(float aim)
    {
        var list = new List<Scene>();
        foreach (float age in new[] { 2f, 8f, 12.5f, 36.5f, 94f, 100f, 104f, 108.5f, 112f, 170f, 180.5f, 216.5f, 230f, 250f, 258f, 264.5f, 268f,
                     294.5f, 302f, 318.5f, 330f, 340f, 346f, 348.5f, 352f, 360f, 384.5f, 386f, 420f })
            list.Add(new Scene($"A{age:0.#}", age, aim));
        return list;
    }

    private static List<Scene> Release(float aim, int tier, float age, float node)
    {
        var list = new List<Scene>();
        foreach (float s in new[] { 0f, 3f, 7f, 9.5f, 10.5f, 12f, 14f, 17f, 20f, 22.5f, 25f, 28f, 31f, 34f, 40f, 48f })
            list.Add(new Scene($"T{tier}S{s:0.#}", age, aim, true, s, tier, tier, node));
        return list;
    }

    // ---- Frames --------------------------------------------------------------------------------

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

    private sealed class Layers
    {
        internal Color[] Frame, Art, Light;
    }

    private static Layers Render(Scene scene, Color backdrop, bool players = true)
    {
        energy.Reset();
        Record(c => Draw(c, scene), scene.Reduced, 1000 + scene.Age + scene.Stowed);
        if (canvas.Dropped > 0) failures.Add($"{scene.Label}: {canvas.Dropped} command(s) over budget");
        backDrawn = canvas.HasBack;
        device.SetRenderTarget(frame);
        RenderLayers();
        device.Clear(backdrop);
        if (players) Backdrop(backdrop == Bright);
        // The Back stratum (the wind-up key) between the ground and the players, as DollWeaponLayer draws it after
        // DrawProjectiles; then the players; then the Front composites.
        if (players && canvas.HasBack)
        {
            DollDeviceState back = DollDeviceState.Capture(device, false);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            canvas.DrawBack(device, pixelEffect, Camera, Matrix.Identity);
            back.Restore(device);
        }
        if (players) Players(scene.Lobby ? LobbySize : 1);
        if (artDrawn || lightDrawn)
        {
            DollDeviceState state = DollDeviceState.Capture(device, false);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            Vector2 offset = canvas.Origin - Camera;
            if (artDrawn) DollPixelArt.CompositeArt(device, pixelEffect, art, canvas.ArtArea, offset, Matrix.Identity);
            if (lightDrawn) DollPixelArt.CompositeLight(device, pixelEffect, light, artDrawn ? art : null, canvas.LightArea, offset, Matrix.Identity, scene.Reduced);
            state.Restore(device);
        }
        device.SetRenderTarget(null);
        var layers = new Layers { Frame = new Color[Width * Height], Art = new Color[art.Width * art.Height], Light = new Color[light.Width * light.Height] };
        frame.GetData(layers.Frame);
        if (artDrawn) art.GetData(layers.Art);
        if (lightDrawn) light.GetData(layers.Light);
        frames++;
        return layers;
    }

    // Ground and a block so readability shows on mixed values.
    private static void Backdrop(bool bright)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        batch.Draw(pixel, new Rectangle(0, (int)PlayerScreen.Y + 21, Width, Height), bright ? new Color(120, 98, 78) : new Color(48, 40, 52));
        batch.Draw(pixel, new Rectangle(980, 120, 220, 260), bright ? new Color(236, 238, 242) : new Color(92, 84, 104));
        batch.End();
    }

    // The stand-in player inside Terraria's 20 x 42 px hitbox (centred on the rotated centre): a 16 x 16 head over the
    // top and the body below, drawn between the strata as in game. Face: the head's top 8 px (brow and eyes, which the
    // art must never hide; a level gun's stock and spring-housing outline reach the lower head, as a shouldered rifle
    // does; the whole-head overlap is reported and the real sprite is an in-game check).
    private static readonly Rectangle Head = new(-8, -21, 16, 16), Face = new(-8, -21, 16, 8);

    private static void Players(int count)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = count > 1 ? LobbyOffset(i) : Vector2.Zero;
            Point at = new((int)(PlayerScreen.X + offset.X), (int)(PlayerScreen.Y + offset.Y));
            batch.Draw(pixel, new Rectangle(at.X - 10, at.Y - 5, 20, 26), new Color(52, 60, 84));
            batch.Draw(pixel, new Rectangle(at.X + Head.X, at.Y + Head.Y, Head.Width, Head.Height), new Color(214, 184, 160));
        }
        batch.End();
    }

    // ---- Checks --------------------------------------------------------------------------------

    private static readonly HashSet<Color> palette = new(DollPixelArt.Palette);

    private static void Checks()
    {
        var scenes = new List<Scene>();
        scenes.AddRange(Build(-.12f));
        scenes.AddRange(Build(MathF.PI + .3f));
        scenes.AddRange(Release(-.08f, 3, 420, 360));
        scenes.AddRange(Release(MathF.PI - .2f, 2, 300, 300));
        scenes.Add(new Scene("miss", 260, -.1f, true, 6, 0));
        int gunFrames = 0, coveredWorst = 0, offPalette = 0, bare = 0, faceWorst = 0, headWorst = 0, keyFrames = 0;
        string coveredAt = "-", faceAt = "-";
        int tiltedWorst = 0;
        foreach (Scene scene in scenes)
        {
            Layers layers = Render(scene, Probe, false);
            // The key stands behind the players (Back) from its rise until it has sunk back after a release.
            if (!scene.Released && scene.Age >= PaleMeridianScore.KeyRise + 2)
            {
                if (!backDrawn) failures.Add($"{scene.Label}: the wind-up key is not on the Back stratum");
                else keyFrames++;
            }
            // Every own light dot is an exact palette tone (premultiplied at alpha 1).
            for (int i = 0; i < layers.Light.Length; i++)
            {
                Color c = layers.Light[i];
                if (c.A == 0) continue;
                if (c.A != 255 || !palette.Contains(new Color(c.R, c.G, c.B))) offPalette++;
            }
            // Every lit dot of the composite borders light or the ink outline, never the bare backdrop.
            bare += BareLight(layers);
            if (!scene.Released || scene.Stowed < MeridianPresentation.FadeStart)
            {
                int artDots = 0, covered = 0;
                for (int i = 0; i < layers.Art.Length; i++)
                {
                    if (layers.Art[i].A == 0) continue;
                    artDots++;
                    if (layers.Light[i].A > 0) covered++;
                }
                // (It fades in over the first 4 ticks of the arrival.)
                if (artDots < 400 && scene.Age >= 4) failures.Add($"{scene.Label}: the gun drew only {artDots} art dots");
                else gunFrames++;
                // The gun's art stays readable: light may cross it only as pins and flashes.
                if (!scene.Released && scene.Age > 20 && covered * 100 / artDots > coveredWorst)
                {
                    coveredWorst = covered * 100 / artDots;
                    coveredAt = scene.Label;
                }
            }
            // Front art never hides the owner's face (Face) at a level aim (within 0.15 rad); a tilted aim swings the
            // receiver toward the head as any held gun does, which is reported. The rest of the head is reported too.
            int face = ArtDots(layers, Face);
            if (MathF.Abs(MathF.Sin(scene.Aim)) > MathF.Sin(.15f)) tiltedWorst = Math.Max(tiltedWorst, face);
            else if (face > faceWorst)
            {
                faceWorst = face;
                faceAt = $"{scene.Label} (aim {scene.Aim:0.##})";
            }
            headWorst = Math.Max(headWorst, ArtDots(layers, Head));
        }
        if (faceWorst > 0) failures.Add($"the weapon's front art covers up to {faceWorst} dot(s) of the owner's face ({faceAt})");
        if (keyFrames == 0) failures.Add("no frame drew the wind-up key");
        notes.Add($"front art over the stand-in head: face 0 dots at level aims ({tiltedWorst} of {Face.Width * Face.Height / 4} at most "
            + $"with the aim 0.3 rad up), whole head at most {headWorst} of {Head.Width * Head.Height / 4} dots; key on Back in {keyFrames} frames");
        // A steep aim swings the gun's receiver across the head, as any held gun does (informational).
        var steep = new List<string>();
        foreach (float aim in new[] { -MathF.PI / 2, -MathF.PI / 3, -MathF.PI / 4, MathF.PI / 4 })
        {
            Layers layers = Render(new Scene("steep", 330, aim, Rounds: false), Probe, false);
            steep.Add($"{aim * 180 / MathF.PI:0} deg: face {ArtDots(layers, Face)} of {Face.Width * Face.Height / 4}");
        }
        notes.Add("front art over the face at steep aims (informational): " + string.Join(", ", steep));

        // Reduced Effects removes the glow discs (no DollMeridianEnergy glow pass at all) and keeps the rest.
        foreach (Scene scene in new[]
                 {
                     new Scene("glow-held", 384.5f, -.12f), new Scene("glow-release", 420, -.08f, true, 12, 3, 3, 360),
                 })
        {
            Render(scene, Probe, false);
            int fullGlow = energy.Applied[MeridianEnergyMaterial.GlowPass];
            Layers reduced = Render(scene with { Reduced = true }, Probe, false);
            int glow = energy.Applied[MeridianEnergyMaterial.GlowPass], reducedLit = 0;
            foreach (Color c in reduced.Light) if (c.A > 0) reducedLit++;
            if (fullGlow == 0) failures.Add($"{scene.Label}: no glow drawn at full effects (check is vacuous)");
            if (glow > 0) failures.Add($"{scene.Label}: Reduced Effects still drew {glow} glow batch(es)");
            if (reducedLit == 0) failures.Add($"{scene.Label}: Reduced Effects drew no light at all");
        }

        // A full lobby: eight players (four overcharging, four releasing tier 3) fit the shared budget, and the line
        // passes stay one batch each however many lines are alive.
        Render(new Scene("lobby", 384.5f, -.08f, Lobby: true), Probe, false);
        int batches = canvas.Mark().Batches;
        if (batches > 6) failures.Add($"eight players' weapons take {batches} light batches (> 6)");
        notes.Add($"eight players' weapons: {batches} light batches of {DollWeaponCanvas.MaxEnergyBatches}, nothing over budget");
        if (offPalette > 0) failures.Add($"{offPalette} light dot(s) off the Doll palette");
        if (bare > 0) failures.Add($"{bare} light dot(s) border the backdrop without an ink outline");
        if (coveredWorst > 12) failures.Add($"light covers up to {coveredWorst}% of the gun's art while it is held ({coveredAt})");
        notes.Add($"held gun drawn in {gunFrames} frames; at most {coveredWorst}% of its art dots under light");

        // The live meridian body: 4+ ramp tones and a pale share of at least 40% of its lit dots.
        var tones = new Dictionary<Color, int>();
        int lit = 0, pale = 0;
        foreach (float s in new[] { 11f, 13f, 15f, 17f })
        {
            Layers layers = Render(new Scene("body", 420, 0, true, s, 3, 3, 900, Gun: false, Rounds: false), Probe, false);
            foreach (Color c in layers.Light)
            {
                if (c.A == 0) continue;
                lit++;
                tones[c] = tones.GetValueOrDefault(c) + 1;
                if (c == Tone(DollTone.PearlViolet) || c == Tone(DollTone.Bone) || c == Tone(DollTone.White)) pale++;
            }
        }
        DollTone[] ramp = { DollTone.Plum, DollTone.PlumLight, DollTone.Violet, DollTone.Lilac, DollTone.PearlViolet, DollTone.Bone, DollTone.White };
        int rampTones = 0;
        foreach (DollTone tone in ramp) if (tones.GetValueOrDefault(Tone(tone)) > 20) rampTones++;
        int paleShare = lit == 0 ? 0 : pale * 100 / lit;
        if (rampTones < 4) failures.Add($"the live meridian shows {rampTones} ramp tones");
        if (paleShare < 40) failures.Add($"the live meridian's pale share is {paleShare}% (< 40%)");
        notes.Add($"live meridian: {rampTones} ramp tones, pale share {paleShare}% of {lit} lit dots");

        // A peer's finisher light is drawn at 65%.
        Layers peer = Render(new Scene("peer", 420, 0, true, 13, 3, 3, 900, Peer: true, Gun: false, Rounds: false), Probe, false);
        // Overlapping light composes above 65% (.65 over .65 is .88; the split's five hairlines all start on the node
        // dot), so a few stacked dots may round to opaque; the light itself is never drawn opaque.
        int peerLit = 0, peerSingle = 0, peerOpaque = 0;
        foreach (Color c in peer.Light)
        {
            if (c.A == 0) continue;
            peerLit++;
            if (Math.Abs(c.A - 166) <= 2) peerSingle++;
            if (c.A == 255) peerOpaque++;
        }
        if (peerLit == 0 || peerOpaque * 200 > peerLit || peerSingle * 2 < peerLit)
            failures.Add($"peer light: {peerOpaque} opaque and {peerSingle} of {peerLit} dots at 65%");

        // Reduced Effects keeps bodies and counts and halves the residue (a tier-1 meridian after its tail has passed).
        int Residue(bool reduced)
        {
            Layers layers = Render(new Scene("residue", 200, 0, true, 25, 1, 1, 900, Reduced: reduced, Gun: false, Rounds: false), Probe, false);
            int n = 0;
            foreach (Color c in layers.Light) if (c.A > 0) n++;
            return n;
        }
        int full = Residue(false), half = Residue(true);
        if (!(full > 0 && half < full * 3 / 4)) failures.Add($"Reduced Effects residue {half} dots vs {full}");
        notes.Add($"meridian residue 26 ticks after the release: {full} dots, Reduced {half}");
    }

    private static Color Tone(DollTone tone) => DollPixelArt.Palette[(int)tone];

    // Front art dots over a rectangle in the local player's frame (world px from the rotated centre).
    private static int ArtDots(Layers layers, Rectangle area)
    {
        int count = 0;
        for (int y = area.Top; y < area.Bottom; y += 2)
        for (int x = area.Left; x < area.Right; x += 2)
        {
            Vector2 dot = (Player + new Vector2(x, y) - canvas.Origin) * DollWeaponCanvas.DotScale;
            int dx = (int)MathF.Floor(dot.X), dy = (int)MathF.Floor(dot.Y);
            if (dx < 0 || dy < 0 || dx >= art.Width || dy >= art.Height) continue;
            if (layers.Art[dy * art.Width + dx].A > 0) count++;
        }
        return count;
    }

    private static int BareLight(Layers layers)
    {
        Color[] pixels = layers.Frame;
        Color ink = Tone(DollTone.Ink);
        Vector2 offset = canvas.Origin - Camera;
        int ox = (int)offset.X, oy = (int)offset.Y, bare = 0;
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
            if (c == Probe || c == ink) continue;
            if (layers.Light[y * light.Width + x].A == 0) continue;
            if (Dot(x - 1, y) == Probe || Dot(x + 1, y) == Probe || Dot(x, y - 1) == Probe || Dot(x, y + 1) == Probe) bare++;
        }
        return bare;
    }

    // ---- Contact sheets ------------------------------------------------------------------------

    private static void Sheets()
    {
        var held = new List<Scene>();
        foreach (Scene scene in Build(-.12f)) held.Add(scene);
        var mirrored = new List<Scene>
        {
            new("L108.5", 108.5f, MathF.PI + .3f), new("L230", 230f, MathF.PI + .3f), new("L330", 330f, MathF.PI + .3f),
            new("L384.5", 384.5f, MathF.PI + .3f), new("R384.5", 384.5f, -.12f, Reduced: true), new("P384.5", 384.5f, -.12f, Peer: true),
        };
        var released = new List<Scene>();
        released.AddRange(Release(-.08f, 3, 420, 360));
        var tier2 = new List<Scene>();
        foreach (float s in new[] { 3f, 10.5f, 14f, 20f, 23f, 28f }) tier2.Add(new Scene($"T2S{s:0.#}", 300, -.08f, true, s, 2, 2, 300));
        tier2.Add(new Scene("M2", 260, -.1f, true, 2, 0));
        tier2.Add(new Scene("M8", 260, -.1f, true, 8, 0));
        tier2.Add(new Scene("RT3S14", 420, -.08f, true, 14, 3, 3, 360, Reduced: true));
        tier2.Add(new Scene("PT3S14", 420, -.08f, true, 14, 3, 3, 360, Peer: true));
        tier2.Add(new Scene("8P", 384.5f, -.08f, Lobby: true));

        var gunCrop = new Rectangle((int)PlayerScreen.X - 110, (int)PlayerScreen.Y - 150, 460, 230);
        var wideCrop = new Rectangle((int)PlayerScreen.X - 110, (int)PlayerScreen.Y - 420, 1180, 640);
        var sections = new List<(string, List<Scene>, Rectangle, int)>
        {
            ("build", held, gunCrop, 5), ("facing", mirrored, gunCrop, 3), ("release-t3", released, wideCrop, 2), ("release-t2", tier2, wideCrop, 2),
        };
        // Each section: dark and bright side by side; the contact sheet stacks the sections.
        var sectionFiles = new List<string>();
        foreach (var (name, scenes, crop, columns) in sections)
        {
            var pair = new List<string>();
            foreach (bool bright in new[] { false, true })
            {
                string file = Path.Combine(output, $"meridian-{name}-{(bright ? "bright" : "dark")}.png");
                Compose(scenes, crop, columns, bright ? Bright : Dark, file);
                pair.Add(file);
            }
            string section = Path.Combine(output, $"meridian-{name}.png");
            Combine(pair, section, true);
            sectionFiles.Add(section);
        }
        Combine(sectionFiles, Path.Combine(output, "meridian-contact.png"), false);
    }

    private static void Compose(List<Scene> scenes, Rectangle crop, int columns, Color backdrop, string file)
    {
        const int gap = 6, label = 16;
        int rows = (scenes.Count + columns - 1) / columns;
        int sheetWidth = columns * (crop.Width + gap) + gap, sheetHeight = rows * (crop.Height + gap + label) + gap;
        var sheet = new Color[sheetWidth * sheetHeight];
        Array.Fill(sheet, new Color(30, 28, 36));
        for (int n = 0; n < scenes.Count; n++)
        {
            Layers layers = Render(scenes[n], backdrop);
            int ox = gap + n % columns * (crop.Width + gap), oy = gap + n / columns * (crop.Height + gap + label);
            Text(sheet, sheetWidth, ox, oy + 3, scenes[n].Label, new Color(236, 228, 220));
            for (int y = 0; y < crop.Height; y++)
            for (int x = 0; x < crop.Width; x++)
            {
                int sx = crop.X + x, sy = crop.Y + y;
                if (sx < 0 || sy < 0 || sx >= Width || sy >= Height) continue;
                sheet[(oy + label + y) * sheetWidth + ox + x] = layers.Frame[sy * Width + sx];
            }
        }
        Save(sheet, sheetWidth, sheetHeight, file);
    }

    // Side by side (horizontal) or stacked.
    private static void Combine(List<string> files, string target, bool horizontal)
    {
        var images = new List<(Color[], int, int)>();
        int width = 0, height = 0;
        foreach (string file in files)
        {
            using var texture = Load(file);
            var data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            images.Add((data, texture.Width, texture.Height));
            width = horizontal ? width + texture.Width : Math.Max(width, texture.Width);
            height = horizontal ? Math.Max(height, texture.Height) : height + texture.Height;
        }
        var sheet = new Color[width * height];
        Array.Fill(sheet, new Color(30, 28, 36));
        int x0 = 0, y0 = 0;
        foreach (var (data, w, h) in images)
        {
            for (int y = 0; y < h; y++) Array.Copy(data, y * w, sheet, (y0 + y) * width + x0, w);
            if (horizontal) x0 += w;
            else y0 += h;
        }
        Save(sheet, width, height, target);
    }

    private static void Save(Color[] data, int width, int height, string file)
    {
        using var texture = new Texture2D(device, width, height);
        texture.SetData(data);
        using var stream = File.Create(file);
        texture.SaveAsPng(stream, width, height);
    }

    // A 3x5 pixel font at 2x for frame labels.
    private static readonly Dictionary<char, string> glyphs = new()
    {
        ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "111001111100111", ['3'] = "111001111001111",
        ['4'] = "101101111001001", ['5'] = "111100111001111", ['6'] = "111100111101111", ['7'] = "111001010010010",
        ['8'] = "111101111101111", ['9'] = "111101111001111", ['.'] = "000000000000010", ['A'] = "010101111101101",
        ['L'] = "100100100100111", ['M'] = "101111111101101", ['P'] = "111101111100100", ['R'] = "110101110101101",
        ['S'] = "111100111001111", ['T'] = "111010010010010",
    };

    private static void Text(Color[] sheet, int width, int x, int y, string text, Color color)
    {
        foreach (char ch in text)
        {
            if (glyphs.TryGetValue(ch, out string glyph))
                for (int i = 0; i < 15; i++)
                {
                    if (glyph[i] != '1') continue;
                    int gx = x + i % 3 * 2, gy = y + i / 3 * 2;
                    for (int dy = 0; dy < 2; dy++)
                        for (int dx = 0; dx < 2; dx++)
                            sheet[(gy + dy) * width + gx + dx] = color;
                }
            x += 8;
        }
    }
}
