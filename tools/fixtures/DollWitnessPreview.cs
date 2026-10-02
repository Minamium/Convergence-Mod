// Offline FNA preview of Last Witness v2 through the shared Doll weapon layer. Links the production layer contract,
// DollWeaponCanvas, DollPixelArt, DollSpritePlacement, the exported anchors, WitnessPresentation and the pure
// WitnessRules (plus the legacy pure scores it reads), loads the real exported PNGs and the compiled DollPixel.fxc
// and DollWitnessEnergy.fxc. No Terraria types: the frame states are built from WitnessRules exactly as the client
// sources fill them (a deterministic flight against a stand-in target). Each frame follows the in-game order: record,
// Art and Light targets, backdrop, a 20 x 42 player silhouette, then the Art and Light composites in front of it.
// Frames at zoom 1 on dark #121017 and bright #bac6d6 ground through the hang, testimonies, seal, lift, whip, flight,
// Axiom turns, return and catch, the stealth judgement and the residues; a contact sheet of them all.
// Checks: nothing dropped and no material error; every light dot ringed by light or ink on a flat backdrop; the
// damaging light shows at least four ramp tones with pearl, bone and white at least 40% of its lit dots; the
// spinning blade's art is never more than a quarter covered by light; the spin arc stays in its band (outside 0.8
// of the tip's reach, at most 2 dots wide); a peer's light composites at 65%; Reduced Effects halves residue.
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

internal static class DollWitnessPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] private static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_Quit();

    private const int Width = 1280, Height = 720;
    // Contact sheet tile: the part of the frame around the player and the target, at 1:1 (zoom 1).
    private static readonly Rectangle Tile = new(250, 150, 820, 420);
    private static readonly Color Dark = new(0x12, 0x10, 0x17), Bright = new(0xba, 0xc6, 0xd6), Probe = new(40, 160, 90);
    private static readonly Vector2 Camera = new(20001, 12001);
    private static readonly Vector2 Player = Camera + new Vector2(430, 430);
    private static readonly Vector2 Target = Player + new Vector2(470, -70);
    private static readonly Vector2 TargetHalf = new(28, 44);
    private static GraphicsDevice device;
    private static Effect pixelEffect, witnessEffect;
    private static SpriteBatch batch;
    private static Texture2D pixel, blade, bladeLarge, sword, shards;
    private static RenderTarget2D art, light, frame;
    private static WitnessEnergyMaterial energy;
    private static readonly DollWeaponCanvas canvas = new();
    private static readonly List<string> failures = new();
    private static readonly List<(string Name, Color[] Dark, Color[] Bright)> tiles = new();
    private static string output;
    private static bool artDrawn, lightDrawn;
    private static float aim;

    private struct Flight
    {
        internal Vector2 Center;
        internal float Rotation;
        internal WitnessPhase Phase;
        internal int PhaseStart;
        internal bool Anchored;
    }

    private static readonly List<Flight> flight = new();
    private static int catchAge = -1, contactAge = -1;

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
        NativeLibrary.SetDllImportResolver(typeof(DollWitnessPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL initialization failed");
        IntPtr window = SDL_CreateWindow("Offline Last Witness", 0, 0, Width, Height, FNA3D_PrepareWindowAttributes() | 0x8);
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
            using var witnessMaterial = new Effect(device, File.ReadAllBytes(Path.Combine(shaders, "DollWitnessEnergy.fxc")));
            pixelEffect = pixelMaterial;
            witnessEffect = witnessMaterial;
            energy = new WitnessEnergyMaterial(() => witnessEffect);
            string textures = Path.Combine(root, "Assets/Textures/Items/DollWeapons");
            blade = Load(Path.Combine(textures, "WitnessBlade.png"));
            bladeLarge = Load(Path.Combine(textures, "WitnessBlade_L.png"));
            sword = Load(Path.Combine(textures, "WitnessSword.png"));
            shards = Load(Path.Combine(textures, "WitnessShards.png"));
            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            batch = new SpriteBatch(device);
            art = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            light = new RenderTarget2D(device, Width / 2 + 2, Height / 2 + 2, false, SurfaceFormat.Color, DepthFormat.None);
            frame = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);

            Simulate();
            frames += Sequence();
            Checks();
            ContactSheet();
            foreach (Texture2D texture in new[] { pixel, blade, bladeLarge, sword, shards }) texture.Dispose();
            batch.Dispose(); art.Dispose(); light.Dispose(); frame.Dispose();
        }
        finally
        {
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        Console.WriteLine(failures.Count == 0
            ? $"PASS {frames} offline Last Witness frames in {output}; contact sheet witness-contact.png; checks passed. Offline only: no native game, peer, zoom UI or FPS acceptance."
            : $"{failures.Count} check(s) failed; {frames} frames written to {output}.");
        return failures.Count == 0 ? 0 : 1;
    }

    private static Texture2D Load(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
    }

    // ---- Deterministic flight (the client sources fill the same states from replicated projectiles) -------------

    private static Vector2 X(NVector2 v) => new(v.X, v.Y);
    private static NVector2 N(Vector2 v) => new(v.X, v.Y);
    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

    private static Vector2 RestPoint() => X(WitnessRules.BalancePoint(N(Player), aim, 1, new WitnessPose(WitnessRules.HangBeta, WitnessRules.HangRadius)));

    private static void Simulate()
    {
        aim = MathF.Atan2(Target.Y - Player.Y, Target.X - Player.X);
        Vector2 axis = Unit(aim), position = Player + axis * WitnessRules.ReleaseRadius, velocity = axis * WitnessRules.OutboundSpeed;
        float rotation = aim;
        var phase = WitnessPhase.Outbound;
        int phaseStart = 0, cap = WitnessRules.OutboundCap(Vector2.Distance(Target, position));
        bool anchored = false;
        flight.Add(new Flight { Center = position, Rotation = rotation, Phase = phase });
        for (int age = 1; age < 240; age++)
        {
            if (phase == WitnessPhase.Outbound && age > cap) { phase = WitnessPhase.Turn; phaseStart = age; }
            if (phase == WitnessPhase.Turn && age - phaseStart >= WitnessRules.ReturnTick) { phase = WitnessPhase.Return; phaseStart = age; }
            int t = age - phaseStart;
            switch (phase)
            {
                case WitnessPhase.Outbound:
                {
                    float want = MathF.Atan2(Target.Y - position.Y, Target.X - position.X), current = MathF.Atan2(velocity.Y, velocity.X);
                    float turn = Math.Clamp(MathF.IEEERemainder(want - current, MathF.Tau), -.24f, .24f);
                    velocity = Unit(current + turn) * WitnessRules.OutboundSpeed;
                    break;
                }
                case WitnessPhase.Turn:
                {
                    Vector2 pull = (Target - position) * WitnessRules.TurnFollow;
                    velocity = pull.Length() > WitnessRules.TurnMaxSpeed ? Vector2.Normalize(pull) * WitnessRules.TurnMaxSpeed : pull;
                    break;
                }
                default:
                {
                    Vector2 delta = RestPoint() - position;
                    float distance = delta.Length();
                    if (distance <= WitnessRules.CatchRadius) { catchAge = WitnessRules.Throw + age; return; }
                    Vector2 want = delta / distance * MathF.Min(WitnessRules.ReturnSpeed, distance);
                    velocity = Vector2.Lerp(velocity, want, distance <= WitnessRules.ReturnSpeed ? 1 : WitnessRules.ReturnResponse);
                    break;
                }
            }
            rotation += phase switch
            {
                WitnessPhase.Turn => t >= 1 ? WitnessRules.TurnAngle(t) - WitnessRules.TurnAngle(t - 1) : WitnessRules.CruiseSpin,
                WitnessPhase.Return => WitnessRules.ReturnSpin(t),
                _ => WitnessRules.CruiseSpin,
            };
            Vector2 from = position;
            position += velocity;
            if (phase == WitnessPhase.Outbound && WitnessRules.SweptDiscTouchesBox(N(from), N(position), WitnessRules.SpinRadius,
                N(Target - TargetHalf), N(Target + TargetHalf)))
            {
                phase = WitnessPhase.Turn;
                phaseStart = age;
                anchored = true;
                contactAge = WitnessRules.Throw + age;
            }
            flight.Add(new Flight { Center = position, Rotation = rotation, Phase = phase, PhaseStart = phaseStart, Anchored = anchored });
        }
        failures.Add("the simulated blade was never caught");
    }

    // A testimony shard t ticks after leaving its seat: launched along the aim, then seeking the target; it hits
    // when its head sweeps the target box. Returns false once it has hit (and gives the hit tick).
    private static bool Shard(int birth, float t, out Vector2 center, out float heading, out Vector2[] trail, out int count, out float hitAt)
    {
        WitnessPose pose = WitnessRules.Pose(WitnessRules.TestimonyFire(birth));
        NVector2 balance = WitnessRules.BalancePoint(N(Player), aim, 1, pose);
        Vector2 position = X(WitnessRules.BladeToWorld(balance, WitnessRules.BladeAngle(aim, 1, pose), 1,
            WitnessRules.SeatLocal(birth, WitnessRules.TestimonyFire(birth))));
        Vector2 velocity = Unit(aim) * WitnessRules.ShardLaunch;
        var history = new List<Vector2> { position };
        hitAt = -1;
        int steps = (int)MathF.Ceiling(t * 2);
        for (int i = 1; i <= steps; i++)
        {
            float want = MathF.Atan2(Target.Y - position.Y, Target.X - position.X), current = MathF.Atan2(velocity.Y, velocity.X);
            float turn = Math.Clamp(MathF.IEEERemainder(want - current, MathF.Tau), -.12f, .12f);
            float speed = velocity.Length() + (WitnessRules.ShardSpeed - velocity.Length()) * (1 - MathF.Exp(-.16f));
            velocity = Unit(current + turn) * speed;
            Vector2 from = position;
            position += velocity * .5f;
            if (i % 2 == 0) history.Add(position);
            Rectangle box = new((int)(Target.X - TargetHalf.X), (int)(Target.Y - TargetHalf.Y), (int)(TargetHalf.X * 2), (int)(TargetHalf.Y * 2));
            if (box.Contains((int)position.X, (int)position.Y)) { hitAt = i * .5f; break; }
        }
        center = position;
        heading = MathF.Atan2(velocity.Y, velocity.X);
        history.Reverse();
        history[0] = position;
        trail = history.ToArray();
        count = Math.Min(trail.Length, 5);
        return hitAt < 0;
    }

    // ---- Frame states ---------------------------------------------------------------------------------------

    private static WitnessDrawState Base(WitnessPart part, bool peer) => new()
    {
        Part = part, Blade = blade, BladeLarge = bladeLarge, Sword = sword, Shards = shards, Energy = energy, Peer = peer,
        Seed = 1234, Gone = -1, Ghost = -1, Facing = 1, SpinSign = 1,
    };

    // Everything Last Witness draws at hang age `age` (fractional) of a score that started on a cold press.
    private static void Score(DollWeaponCanvas c, float age, bool stealth, bool peer = false, float gone = -1, bool forceBody = false)
    {
        bool thrown = age >= WitnessRules.Throw && catchAge > 0;
        bool caught = thrown && age >= catchAge;
        var hang = Base(WitnessPart.Hang, peer);
        hang.Root = Player;
        hang.Aim = aim;
        hang.Age = age;
        hang.Clock = age;
        hang.Stealth = stealth;
        hang.Spoken = WitnessRules.Spoken(age);
        hang.Body = forceBody || !thrown || caught;
        hang.Caught = caught;
        hang.BodyAge = caught ? age - catchAge : age;
        hang.Gone = gone;
        WitnessPresentation.Emit(c, in hang);
        for (int birth = 0; birth < WitnessRules.Testimonies; birth++)
        {
            float t = age - WitnessRules.TestimonyFire(birth);
            if (t < 0 || gone >= 0) continue;
            bool flying = Shard(birth, t, out Vector2 center, out float heading, out Vector2[] shardTrail, out int count, out float hitAt);
            var shard = Base(WitnessPart.Shard, peer);
            shard.Center = center;
            shard.Heading = heading;
            shard.Shape = birth % 3;
            shard.Trail = shardTrail;
            shard.TrailCount = count;
            shard.Gone = flying ? -1 : t - hitAt;
            if (!flying && shard.Gone >= WitnessPresentation.ShardPuff) continue;
            WitnessPresentation.Emit(c, in shard);
        }
        if (!thrown || caught) return;
        float bladeAge = age - WitnessRules.Throw;
        int tick = (int)MathF.Floor(bladeAge);
        if (tick >= flight.Count) return;
        // The sources draw between the last two samples; discrete state is the nearer sample.
        Flight a = flight[tick], b = flight[Math.Min(tick + 1, flight.Count - 1)];
        float u = bladeAge - tick;
        Flight now = u > .5f ? b : a;
        var state = Base(WitnessPart.Blade, peer);
        state.Center = Vector2.Lerp(a.Center, b.Center, u);
        state.Rotation = a.Rotation + (b.Rotation - a.Rotation) * u;
        state.Phase = now.Phase;
        state.PhaseAge = MathF.Max(0, bladeAge - now.PhaseStart);
        state.Anchored = now.Anchored;
        state.Stealth = stealth;
        state.Age = bladeAge;
        state.Clock = age;
        float distance = Vector2.Distance(state.Center, Player);
        state.Large = distance > WitnessPresentation.LargeRungDistance;
        int crossed = 0;
        for (int i = 1; i <= tick; i++)
            if (Vector2.Distance(flight[i].Center, Player) > WitnessPresentation.LargeRungDistance != Vector2.Distance(flight[i - 1].Center, Player) > WitnessPresentation.LargeRungDistance)
                crossed = i;
        state.Swap = crossed == 0 || state.Large != Vector2.Distance(flight[crossed].Center, Player) > WitnessPresentation.LargeRungDistance
            ? 99 : bladeAge - crossed;
        var trail = new Vector2[13];
        trail[0] = state.Center;
        int n = 1;
        for (int i = tick; i >= 0 && n < trail.Length; i--) trail[n++] = flight[i].Center;
        state.Trail = trail;
        state.TrailCount = n;
        WitnessPresentation.Emit(c, in state);
        if (stealth && contactAge > 0 && age >= contactAge)
        {
            var judgement = Base(WitnessPart.Judgement, peer);
            judgement.Center = Target;
            judgement.Age = age - contactAge;
            judgement.Struck = true;
            judgement.Clock = age;
            WitnessPresentation.Emit(c, in judgement);
        }
    }

    private static void Judgement(DollWeaponCanvas c, float t, bool struck = true, bool peer = false)
    {
        var judgement = Base(WitnessPart.Judgement, peer);
        judgement.Center = Target;
        judgement.Age = t;
        judgement.Struck = struck;
        judgement.Clock = t;
        WitnessPresentation.Emit(c, in judgement);
    }

    // ---- Frames -----------------------------------------------------------------------------------------------

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

    private static Color[] Frame(Action<DollWeaponCanvas> scene, Color backdrop, bool reduced, string name, bool players = true, double ticks = 40)
    {
        Record(scene, reduced, ticks);
        if (canvas.Dropped > 0) failures.Add($"{name}: {canvas.Dropped} command(s) over budget");
        device.SetRenderTarget(frame);
        RenderLayers();
        device.Clear(backdrop);
        if (players) Backdrop(backdrop == Bright);
        if (players) Players();
        if (artDrawn || lightDrawn)
        {
            DollDeviceState state = DollDeviceState.Capture(device, false);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
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

    // Stand-in ground, a lit wall and the target (an enemy-sized box) so readability shows on mixed values.
    private static void Backdrop(bool bright)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        batch.Draw(pixel, new Rectangle(0, 451, Width, 270), bright ? new Color(120, 98, 78) : new Color(48, 40, 52));
        batch.Draw(pixel, new Rectangle(560, 170, 150, 120), bright ? new Color(236, 238, 242) : new Color(92, 84, 104));
        Vector2 t = Target - Camera;
        batch.Draw(pixel, new Rectangle((int)(t.X - TargetHalf.X), (int)(t.Y - TargetHalf.Y), (int)(TargetHalf.X * 2), (int)(TargetHalf.Y * 2)),
            bright ? new Color(140, 120, 150) : new Color(70, 60, 82));
        batch.End();
    }

    // The owner: a 20 x 42 px stand-in player, drawn between the strata as in game.
    private static void Players()
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        Vector2 p = Player - Camera;
        batch.Draw(pixel, new Rectangle((int)p.X - 10, (int)p.Y - 21, 20, 42), new Color(52, 60, 84));
        batch.Draw(pixel, new Rectangle((int)p.X - 8, (int)p.Y - 37, 16, 16), new Color(214, 184, 160));
        batch.End();
    }

    private static int Sequence()
    {
        var shots = new List<(string Name, Action<DollWeaponCanvas> Scene)>();
        void Add(string name, Action<DollWeaponCanvas> scene) => shots.Add((name, scene));
        int contact = contactAge, caught = catchAge;
        // The hang and the testimonies.
        foreach (float age in new[] { 3f, 10f, 13f, 16.5f, 20f, 70f, 78f, 150f, 168f })
            Add($"hang-{age:000.0}", c => Score(c, age, false));
        // The seal, the lift, the hold, the whip and the throw.
        foreach (float age in new[] { 176f, 180f, 190f, 200f, 206f, 212f, 215f, 217.5f, 219f, 222f })
            Add($"throw-{age:000.0}", c => Score(c, age, false));
        // The flight: contact, the Axiom turns and their bites, the return and the catch.
        foreach (float offset in new[] { 0f, 2f, 6f, 9f, 12f, 17f, 21f, 23f })
            Add($"axiom-{offset:00.0}", c => Score(c, contact + offset, false));
        foreach (float offset in new[] { 26f, 30f })
            Add($"return-{offset:00.0}", c => Score(c, contact + offset, false));
        foreach (float since in new[] { 0f, 3f, 8f })
            Add($"catch-{since:00.0}", c => Score(c, caught + since, false));
        // The stealth judgement (with its blade turning in the target at first).
        foreach (float t in new[] { 2f, 8f, 13f, 16f, 19f, 24f, 28f, 30f, 34f, 40f, 48f })
            Add($"judgement-{t:00}", c => { if (t < 23) Score(c, contact + t, true); else Judgement(c, t); });
        Add("judgement-miss-30", c => Judgement(c, 30, false));
        // Residues: a release before the seal crumbles the hanging blade.
        foreach (float gone in new[] { 0f, 6f, 12f })
            Add($"cancel-{gone:00}", c => Score(c, 120, false, false, gone));
        // A peer's weapon: its damaging light at 65%, bodies opaque.
        Add("peer-axiom-06", c => Score(c, contact + 6, false, true));

        foreach (var (name, scene) in shots)
        {
            Color[] dark = Frame(scene, Dark, false, $"{name}-dark");
            Color[] bright = Frame(scene, Bright, false, $"{name}-bright");
            tiles.Add((name, Crop(dark), Crop(bright)));
        }
        // Reduced Effects for a few beats.
        int reducedFrames = 0;
        foreach (var (name, scene) in shots)
        {
            if (name is not ("axiom-06.0" or "judgement-28" or "cancel-06")) continue;
            Frame(scene, Dark, true, $"{name}-dark-reduced");
            reducedFrames++;
        }
        return shots.Count * 2 + reducedFrames;
    }

    private static Color[] Crop(Color[] pixels)
    {
        var tile = new Color[Tile.Width * Tile.Height];
        for (int y = 0; y < Tile.Height; y++)
            Array.Copy(pixels, (Tile.Y + y) * Width + Tile.X, tile, y * Tile.Width, Tile.Width);
        return tile;
    }

    // Two columns per frame (dark, bright), two frames per row, at 1:1.
    private static void ContactSheet()
    {
        const int perRow = 2, gap = 6;
        int rows = (tiles.Count + perRow - 1) / perRow;
        int width = perRow * 2 * (Tile.Width + gap) + gap, height = rows * (Tile.Height + gap) + gap;
        var sheet = new Color[width * height];
        Array.Fill(sheet, new Color(60, 60, 70));
        for (int i = 0; i < tiles.Count; i++)
        {
            int column = i % perRow * 2, row = i / perRow;
            for (int side = 0; side < 2; side++)
            {
                Color[] tile = side == 0 ? tiles[i].Dark : tiles[i].Bright;
                int x0 = gap + (column + side) * (Tile.Width + gap), y0 = gap + row * (Tile.Height + gap);
                for (int y = 0; y < Tile.Height; y++)
                    Array.Copy(tile, y * Tile.Width, sheet, (y0 + y) * width + x0, Tile.Width);
            }
        }
        using var texture = new Texture2D(device, width, height);
        texture.SetData(sheet);
        using var file = File.Create(Path.Combine(output, "witness-contact.png"));
        texture.SaveAsPng(file, width, height);
        File.WriteAllLines(Path.Combine(output, "witness-contact.txt"), tiles.ConvertAll(t => t.Name));
    }

    // ---- Checks -----------------------------------------------------------------------------------------------

    private static Color Tone(DollTone tone) => DollPixelArt.Palette[(int)tone];

    private static bool Near(Color c) => Snap(c) != c || Array.IndexOf(DollPixelArt.Palette, c) >= 0;

    private static Color Snap(Color c)
    {
        foreach (Color p in DollPixelArt.Palette)
            if (Math.Abs(p.R - c.R) <= 3 && Math.Abs(p.G - c.G) <= 3 && Math.Abs(p.B - c.B) <= 3) return p;
        return c;
    }

    private static void Checks()
    {
        CheckOutline();
        CheckDamagingLight();
        CheckBladeReadable();
        CheckSpinArc();
        CheckPeerAndReduced();
    }

    private static Color[] ReadLight()
    {
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

    private static void RenderOnly(Action<DollWeaponCanvas> scene, bool reduced = false)
    {
        Record(scene, reduced, 40);
        if (canvas.Dropped > 0) failures.Add($"{canvas.Dropped} command(s) over budget");
        device.SetRenderTarget(frame);
        RenderLayers();
        device.SetRenderTarget(null);
    }

    // Every light dot is ringed by light, art or the ink outline on a flat backdrop (no players), for every beat.
    // Only light is checked: a sprite's own dithered fade or crumble shows the backdrop through its holes by design.
    private static void CheckOutline()
    {
        float[] ages = { 13, 16.5f, 168, 180, 212, 217.5f };
        var scenes = new List<Action<DollWeaponCanvas>>();
        foreach (float age in ages) scenes.Add(c => Score(c, age, false));
        foreach (float offset in new[] { -6f, 0f, 6f, 17f, 23f, 30f }) scenes.Add(c => Score(c, contactAge + offset, false));
        foreach (float t in new[] { 8f, 19f, 28f, 30f, 40f }) scenes.Add(c => Judgement(c, t));
        int bare = 0, lit = 0;
        foreach (var scene in scenes)
        {
            // The plain (Reduced Effects) composite: the glow tints the backdrop around light by design.
            Color[] pixels = Frame(scene, Probe, true, null, false);
            Color[] lightDots = ReadLight(), artDots = ReadArt();
            Vector2 offset = canvas.Origin - Camera;
            int ox = (int)offset.X, oy = (int)offset.Y;
            Color Pixel(int x, int y)
            {
                int px = ox + x * 2, py = oy + y * 2;
                if (px < 0) px += 1;
                if (py < 0) py += 1;
                return px < 0 || py < 0 || px >= Width || py >= Height ? Probe : pixels[py * Width + px];
            }
            bool Empty(int x, int y) => lightDots[y * light.Width + x].A == 0 && artDots[y * art.Width + x].A == 0;
            for (int y = 1; y < light.Height - 1; y++)
            for (int x = 1; x < light.Width - 1; x++)
            {
                if (lightDots[y * light.Width + x].A == 0 || artDots[y * art.Width + x].A != 0) continue;
                lit++;
                if (Empty(x - 1, y) && Pixel(x - 1, y) == Probe || Empty(x + 1, y) && Pixel(x + 1, y) == Probe
                    || Empty(x, y - 1) && Pixel(x, y - 1) == Probe || Empty(x, y + 1) && Pixel(x, y + 1) == Probe) bare++;
            }
        }
        if (lit == 0) failures.Add("outline scenes drew nothing");
        if (bare > 0) failures.Add($"{bare} lit dot(s) border the backdrop without an ink outline");
    }

    // The damaging light (wake, spin arc, Axiom bites, edges, execution) is never flat: at least four ramp tones,
    // pearl/bone/white at least 40% of its lit dots, and every dot on the palette.
    private static void CheckDamagingLight()
    {
        var palette = new HashSet<Color>(DollPixelArt.Palette);
        DollTone[] ramp = { DollTone.Plum, DollTone.PlumLight, DollTone.Violet, DollTone.Lilac, DollTone.PearlViolet, DollTone.Bone, DollTone.White };
        var cases = new (string Name, Action<DollWeaponCanvas> Scene)[]
        {
            ("outbound wake", c => Score(c, WitnessRules.Throw + 5, false)),
            ("Axiom bite", c => Score(c, contactAge + 6, false)),
            ("return wake", c => Score(c, contactAge + 26, false)),
            ("whip arc", c => Score(c, 216, false)),
            ("judgement edges", c => Judgement(c, 21)),
            ("execution", c => Judgement(c, 29)),
        };
        foreach (var (name, scene) in cases)
        {
            RenderOnly(scene);
            Color[] dots = ReadLight();
            int lit = 0, pale = 0, off = 0;
            var tones = new HashSet<Color>();
            foreach (Color raw in dots)
            {
                if (raw.A == 0) continue;
                lit++;
                // Premultiplied: a translucent band (the spin arc at .7) carries a palette colour times its alpha.
                Color c = raw.A == 255 ? raw : new Color((int)MathF.Round(raw.R * 255f / raw.A), (int)MathF.Round(raw.G * 255f / raw.A),
                    (int)MathF.Round(raw.B * 255f / raw.A));
                if (!Near(c)) { off++; continue; }
                c = Snap(c);
                tones.Add(c);
                if (c == Tone(DollTone.PearlViolet) || c == Tone(DollTone.Bone) || c == Tone(DollTone.White) || c == Tone(DollTone.Pearl)) pale++;
            }
            int rampTones = 0;
            foreach (DollTone tone in ramp) if (tones.Contains(Tone(tone))) rampTones++;
            if (lit == 0) { failures.Add($"{name}: no light"); continue; }
            if (rampTones < 4) failures.Add($"{name}: only {rampTones} ramp tones");
            if (pale * 100 / lit < 40) failures.Add($"{name}: pale share {pale * 100 / lit}% (< 40%)");
            // Fading accents (rings, residue) laid over a band blend with it; the bands themselves stay on the palette.
            if (off * 100 > lit * 2) failures.Add($"{name}: {off} of {lit} light dots off the palette");
            Console.WriteLine($"  {name}: {lit} lit dots, {rampTones} ramp tones, {pale * 100 / lit}% pearl/bone/white");
        }
    }

    // The thrown blade stays readable: through the whole flight at most a quarter of its drawn art is under light.
    private static void CheckBladeReadable()
    {
        int worst = 0;
        string worstAt = "";
        for (int age = WitnessRules.Throw + 1; age < catchAge; age++)
        {
            int bladeAge = age - WitnessRules.Throw;
            Flight f = flight[bladeAge];
            RenderOnly(c => Score(c, age, false));
            Color[] artDots = ReadArt(), lightDots = ReadLight();
            Vector2 center = canvas.ToDot(f.Center);
            float reach = (Vector2.Distance(f.Center, Player) > WitnessPresentation.LargeRungDistance ? 135 : 70) * DollWeaponCanvas.DotScale;
            int drawn = 0, covered = 0;
            for (int y = (int)(center.Y - reach); y <= (int)(center.Y + reach); y++)
            for (int x = (int)(center.X - reach); x <= (int)(center.X + reach); x++)
            {
                if (x < 0 || y < 0 || x >= art.Width || y >= art.Height) continue;
                int i = y * art.Width + x;
                if (artDots[i].A == 0) continue;
                drawn++;
                if (lightDots[i].A > 0) covered++;
            }
            if (drawn == 0) { failures.Add($"blade art missing at age {age}"); continue; }
            int share = covered * 100 / drawn;
            if (share > worst) { worst = share; worstAt = $"age {age} ({f.Phase})"; }
        }
        Console.WriteLine($"  thrown blade: at most {worst}% of its art under light ({worstAt})");
        if (worst > 25) failures.Add($"the thrown blade's art is {worst}% covered by light at {worstAt}");
    }

    // The spin arc keeps to its band: outside 0.8 of the tip's reach and at most 2 dots across.
    private static void CheckSpinArc()
    {
        int age = WitnessRules.Throw + 8;
        Flight f = flight[age - WitnessRules.Throw];
        var state = Base(WitnessPart.Blade, false);
        state.Center = f.Center;
        state.Rotation = f.Rotation;
        state.Phase = WitnessPhase.Outbound;
        state.Large = true;
        state.Swap = 99;
        state.Clock = age;
        RenderOnly(c => WitnessPresentation.Emit(c, in state));
        Color[] dots = ReadLight();
        Vector2 center = canvas.ToDot(f.Center);
        float reach = WitnessRules.ThrownTip.X * DollWeaponCanvas.DotScale;
        Vector2 along = Unit(f.Rotation), across = new(-along.Y, along.X);
        Vector2 eye = canvas.ToDot(f.Center + along * WitnessRules.ThrownEye.X + across * WitnessRules.ThrownEye.Y);
        int inside = 0, total = 0;
        for (int y = 0; y < light.Height; y++)
        for (int x = 0; x < light.Width; x++)
        {
            if (dots[y * light.Width + x].A == 0) continue;
            // The eye light is not part of the arc.
            if (Vector2.Distance(new Vector2(x + .5f, y + .5f), eye) < 4) continue;
            total++;
            float r = Vector2.Distance(new Vector2(x + .5f, y + .5f), center);
            if (r < .8f * reach - .5f) inside++;
            if (r > reach + 3) inside++;
        }
        if (total == 0) failures.Add("spin arc drew nothing");
        if (inside > 0) failures.Add($"spin arc: {inside} dot(s) outside its band (0.8-1.0 of the tip's reach)");
        // Radial width: no ray from the centre crosses more than 2 lit dots of the arc.
        int thick = 0;
        for (int k = 0; k < 360; k++)
        {
            float a = k * MathF.Tau / 360;
            int run = 0, best = 0;
            for (float r = .78f * reach; r < reach + 3; r += .25f)
            {
                int x = (int)MathF.Floor(center.X + MathF.Cos(a) * r), y = (int)MathF.Floor(center.Y + MathF.Sin(a) * r);
                bool on = x >= 0 && y >= 0 && x < light.Width && y < light.Height && dots[y * light.Width + x].A > 0;
                run = on ? run + 1 : 0;
                best = Math.Max(best, run);
            }
            // .25-dot steps: three dots across is more than 8 consecutive samples on a ray
            if (best > 12) thick++;
        }
        if (thick > 0) failures.Add($"spin arc wider than 2 dots on {thick} ray(s)");
    }

    // A peer's damaging light composites at 65% while the bodies stay opaque; Reduced Effects halves residue.
    private static void CheckPeerAndReduced()
    {
        RenderOnly(c => Score(c, contactAge + 6, false, true));
        int translucent = 0, opaque = 0;
        foreach (Color c in ReadLight())
        {
            if (c.A == 0) continue;
            if (c.A > 160 && c.A < 175) translucent++;
            else if (c.A == 255) opaque++;
        }
        if (translucent == 0) failures.Add("a peer's damaging light is not at 65%");
        int artOpaque = 0, artOther = 0;
        foreach (Color c in ReadArt()) { if (c.A == 255) artOpaque++; else if (c.A > 0) artOther++; }
        if (artOpaque == 0 || artOther > 0) failures.Add($"a peer's bodies are not opaque ({artOpaque} opaque, {artOther} translucent texels)");
        bool Drawn(float gone, bool reduced)
        {
            Record(c => Score(c, 120, false, false, gone), reduced, 40);
            return canvas.HasArt;
        }
        if (!Drawn(12, false) || Drawn(12, true)) failures.Add("Reduced Effects does not halve the cancel residue");
        if (!Drawn(2, true)) failures.Add("the residue vanished at once under Reduced Effects");
    }
}
