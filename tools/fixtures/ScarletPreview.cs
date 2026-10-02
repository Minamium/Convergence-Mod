// Hidden FNA D3D11 preview of the Scarlet Invocation field. It links the production
// Vfx/ foundation (ScarletView, IScarletAssets, ScarletGeometryOverlay) and the
// Terraria-independent authority (CrimsonTechniqueGeometry and friends), builds real
// phrases with the production rhythm/choreography, and draws the authoritative hit
// shapes over stand-in characters. Offline review only; no game, Terraria or server.
// A passing run is not a playtest: in-game acceptance stays not_run.
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal enum Backdrop { Sanctum, Night, Day }
internal enum MaskMode { Black, Dim, None }

internal sealed class PreviewOptions
{
    internal string Only = "";
    internal int Step = 8, PhraseStart = 1000, Width = 1920, Height = 1080;
    internal bool ConstantBeats, Sequences = true, Matrix = true, Smoke = true, Files = true, Contract = true, Rewards;
    internal double Bpm = 128;
    internal Backdrop[] Backdrops = { Backdrop.Sanctum, Backdrop.Night, Backdrop.Day };
    internal float[] Zooms = { .65f, 1f, 2f };
    internal Backdrop SequenceBackdrop = Backdrop.Night;
    internal float SequenceZoom = .65f;
    internal string[] Players = { "center", "edge" };
    internal bool[] ReducedModes = { false };
    internal MaskMode Mask = MaskMode.Dim;
    // overlay = authoritative hit shapes, ink = ScarletInk live/residue on field beams only, portal = the PortalBeam stand-in for every plan,
    // ink+overlay = both, proposal = what production draws (portal forecast, then ScarletInk) with the overlay left out.
    internal string Look = "overlay";
}

internal sealed record SceneDef(string Name, int Phase, int Serial);

internal static class ScarletPreview
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)] static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SDL_CreateWindow(string title, int x, int y, int w, int h, uint flags);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] static extern void SDL_Quit();

    static int Main(string[] args)
    {
        if (args.Length < 4) { Console.Error.WriteLine("usage: <repo> <Luminance.tmod> <native dir> <output> [--flag value]..."); return 2; }
        string root = args[0], luminance = args[1], native = args[2], output = args[3];
        var options = ParseOptions(args.Skip(4).ToArray());
        IntPtr Resolve(string name, System.Reflection.Assembly a, DllImportSearchPath? p)
        { string file = Path.Combine(native, name.EndsWith(".dll") ? name : name + ".dll"); return File.Exists(file) ? NativeLibrary.Load(file) : IntPtr.Zero; }
        NativeLibrary.SetDllImportResolver(typeof(ScarletPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL video initialization failed");
        IntPtr window = SDL_CreateWindow("Scarlet preview", 0, 0, 640, 360, FNA3D_PrepareWindowAttributes() | 0x8);
        if (window == IntPtr.Zero) throw new Exception("Hidden device surface unavailable");
        try
        {
            using var device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, new PresentationParameters
            {
                DeviceWindowHandle = window, BackBufferWidth = 640, BackBufferHeight = 360, BackBufferFormat = SurfaceFormat.Color,
                IsFullScreen = false, DepthStencilFormat = DepthFormat.Depth24Stencil8, PresentationInterval = PresentInterval.Immediate
            });
            Directory.CreateDirectory(output);
            using var assets = new PreviewAssets(device, root, luminance);
            using var renderer = new PreviewRenderer(device, assets, options, root);
            return new PreviewRun(renderer, options, output).Execute();
        }
        finally { SDL_DestroyWindow(window); SDL_Quit(); }
    }

    static PreviewOptions ParseOptions(string[] args)
    {
        var o = new PreviewOptions();
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--")) throw new ArgumentException("unexpected argument " + args[i]);
            string key = args[i][2..];
            string value = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "true";
            switch (key)
            {
                case "only": o.Only = value; break;
                case "step": o.Step = Math.Max(1, int.Parse(value, CultureInfo.InvariantCulture)); break;
                case "phrase-start": o.PhraseStart = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "beats":
                    o.ConstantBeats = value != "score";
                    if (o.ConstantBeats && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bpm)) o.Bpm = bpm;
                    break;
                case "bg": o.Backdrops = value.Split(',').Select(v => Enum.Parse<Backdrop>(v, true)).ToArray(); break;
                case "zoom": o.Zooms = value.Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray(); break;
                case "seq-bg": o.SequenceBackdrop = Enum.Parse<Backdrop>(value, true); break;
                case "seq-zoom": o.SequenceZoom = float.Parse(value, CultureInfo.InvariantCulture); break;
                case "size":
                    var parts = value.Split('x');
                    o.Width = int.Parse(parts[0], CultureInfo.InvariantCulture); o.Height = int.Parse(parts[1], CultureInfo.InvariantCulture); break;
                case "players": o.Players = value.Split(','); break;
                case "reduced": o.ReducedModes = value switch { "on" => new[] { true }, "both" => new[] { false, true }, _ => new[] { false } }; break;
                case "mask": o.Mask = Enum.Parse<MaskMode>(value, true); break;
                case "look": o.Look = value; break;
                case "no-sequences": o.Sequences = false; break;
                case "no-matrix": o.Matrix = false; break;
                case "no-smoke": o.Smoke = false; break;
                case "no-contract": o.Contract = false; break;
                case "sheets-only": o.Files = false; break;
                case "rewards": o.Rewards = true; break;
                default: throw new ArgumentException("unknown option --" + key);
            }
        }
        return o;
    }
}

// What gets rendered and where it goes.
internal sealed class PreviewRun
{
    private readonly PreviewRenderer renderer;
    private readonly PreviewOptions options;
    private readonly string output;
    private readonly List<object> index = new();
    private readonly List<SheetCell> overview = new();
    private int written;
    private PreviewContract.Result contract;

    internal PreviewRun(PreviewRenderer renderer, PreviewOptions options, string output)
    {
        this.renderer = renderer; this.options = options; this.output = output;
    }

    // Real phrases: Act I..III (four aimed notes + the crossflow) and the three Final pairs (+ ClusterVolley).
    private static readonly SceneDef[] Scenes =
    {
        new("act1", 0, 1), new("act2", 1, 1), new("act3", 2, 1),
        new("final-tracking-rift", 3, 3), new("final-rift-grid", 3, 1), new("final-grid-tracking", 3, 2)
    };

    internal int Execute()
    {
        if (options.Rewards) return RewardsPreview.Run(renderer, options, output);
        // Gameplay has one fixed 128 BPM grid since #101 (CrimsonMeter); --beats no longer selects a score.
        var score = PreviewPlanner.Grid;
        renderer.Score = score;
        string beats = "CrimsonMeter 128 BPM grid (28.125 ticks per beat)";
        Console.WriteLine($"beats: {beats}; phrase start (score tick): {options.PhraseStart}");
        var players = PreviewPlanner.Players().Where(p => options.Players.Contains(p.Name)).ToArray();
        var phrases = new List<PreviewPhrase>();
        foreach (var scene in Scenes)
        {
            foreach (var player in players)
            {
                string name = scene.Name + "-" + player.Name;
                if (options.Only.Length > 0 && !name.Contains(options.Only, StringComparison.OrdinalIgnoreCase)) continue;
                phrases.Add(PreviewPlanner.Build(name, score, scene.Phase, scene.Serial, player, options.PhraseStart));
            }
        }
        foreach (var phrase in phrases)
        {
            Console.WriteLine($"{phrase.Name}: beats {string.Join(",", phrase.BeatTicks.Select(t => t - phrase.MusicStart))} | "
                + string.Join(" ", phrase.Plans.Select(p => $"{p.Technique}[B{p.Born - phrase.MusicStart}/F{p.Fire - phrase.MusicStart}/E{p.End - phrase.MusicStart}]")));
            if (options.Contract) CheckContract(phrase);
            if (options.Sequences) Sequence(phrase);
            if (options.Matrix) VariantSheets(phrase);
        }
        if (overview.Count > 0)
            PreviewSheet.Save(renderer.Device, renderer.Pixel, overview, 4, 480, 270, "OVERVIEW KEY FRAMES", Path.Combine(output, "contact-overview.png"));
        if (options.Smoke && (options.Only.Length == 0 || "shader-smoke".Contains(options.Only, StringComparison.OrdinalIgnoreCase)))
            ShaderSmoke(score, players.Length > 0 ? players[0] : PreviewPlanner.Players()[0]);
        File.WriteAllText(Path.Combine(output, "index.json"), JsonSerializer.Serialize(new
        {
            note = "Offline preview of the production geometry and Vfx foundation. Not a playtest; in-game acceptance is not_run.",
            beats, options.PhraseStart, options.Step, size = $"{options.Width}x{options.Height}", scenes = index
        }, new JsonSerializerOptions { WriteIndented = true }));
        if (contract.Wrong > 0)
        {
            Console.Error.WriteLine($"FAIL overlay contract: {contract.Wrong} of {contract.Checked} sampled pixels differ from the authoritative capsules.");
            return 1;
        }
        if (contract.Checked > 0)
            Console.WriteLine($"overlay contract: {contract.Checked} sampled pixels ({contract.FillSamples} fill, {contract.RingSamples} outline) match the authoritative capsules, {contract.Wrong} wrong.");
        Console.WriteLine($"PASS {written} PNGs under {output}. Production Vfx foundation + authoritative geometry; stand-in characters; no game launched.");
        return 0;
    }

    // Warning, both live key moments and a residue moment, at 1:1 over black.
    private void CheckContract(PreviewPhrase phrase)
    {
        int[] ticks = { phrase.Plans[1].Born + 10, phrase.Plans[1].Fire + 5, phrase.Plans[4].Fire + (phrase.Phase == 3 ? 24 : 14), phrase.Plans[3].End + 6 };
        var result = new PreviewContract.Result();
        foreach (int tick in ticks) result += renderer.CheckContract(phrase, tick);
        contract += result;
        Console.WriteLine($"  contract {phrase.Name}: {result.Checked} samples, {result.Wrong} wrong");
    }

    private void Sequence(PreviewPhrase phrase)
    {
        int cellW = 384, cellH = 216;
        int first = phrase.FirstBorn, t0 = first - options.Step;
        var cells = new List<SheetCell>();
        var frames = new List<int>();
        string dir = Path.Combine(output, phrase.Name);
        int number = 0;
        for (int tick = t0; tick <= phrase.LastEnd + options.Step; tick += options.Step, number++)
        {
            string caption = $"{phrase.Name} T{tick - first:+0;-0;0}";
            var target = renderer.Render(phrase, tick, options.SequenceBackdrop, options.SequenceZoom, options.ReducedModes[0], caption);
            if (options.Files) Save(target, Path.Combine(dir, $"f{number:000}_t{tick - first:+000;-000;+000}.png"));
            cells.Add(PreviewSheet.Cell(target, cellW, cellH, Label(phrase, tick, first), Bar(phrase, tick)));
            frames.Add(tick - first);
        }
        PreviewSheet.Save(renderer.Device, renderer.Pixel, cells, 5, cellW, cellH,
            $"{phrase.Name} STEP {options.Step} {options.SequenceBackdrop} ZOOM {options.SequenceZoom.ToString(CultureInfo.InvariantCulture)}",
            Path.Combine(output, "contact-" + phrase.Name + ".png"));
        written++;
        index.Add(new
        {
            name = phrase.Name, phase = phrase.Phase, serial = phrase.Serial, player = phrase.Player.Name,
            musicStart = phrase.MusicStart, beatTicks = phrase.BeatTicks, firstBorn = first, frameOffsets = frames,
            plans = phrase.Plans.Select(p => new
            {
                technique = p.Technique.ToString(), pulse = p.Pulse, source = p.Source,
                born = p.Born, fire = p.Fire, end = p.End, target = new[] { p.Target.X, p.Target.Y }
            })
        });
    }

    // Two key moments per phrase: the second note's live window (warning of the next one overlaps it),
    // and the fifth note (crossflow in Acts, ClusterVolley in the Final).
    private void VariantSheets(PreviewPhrase phrase)
    {
        var keys = new[] { phrase.Plans[1].Fire + 5, phrase.Plans[4].Fire + (phrase.Phase == 3 ? 24 : 14) };
        for (int k = 0; k < keys.Length; k++)
        {
            var cells = new List<SheetCell>();
            foreach (bool reduced in options.ReducedModes)
                foreach (var bg in options.Backdrops)
                    foreach (float zoom in options.Zooms)
                    {
                        string tag = $"{bg}-z{zoom.ToString(CultureInfo.InvariantCulture)}{(reduced ? "-reduced" : "")}".ToLowerInvariant();
                        var target = renderer.Render(phrase, keys[k], bg, zoom, reduced, $"{phrase.Name} K{k + 1} {tag}");
                        if (options.Files) Save(target, Path.Combine(output, phrase.Name, "matrix", $"k{k + 1}-{tag}.png"));
                        cells.Add(PreviewSheet.Cell(target, 640, 360, $"{bg} ZOOM {zoom.ToString(CultureInfo.InvariantCulture)}{(reduced ? " REDUCED" : "")}", Bar(phrase, keys[k])));
                        if (k == 0 && bg == options.Backdrops[0] && zoom == options.Zooms[0] && !reduced)
                            overview.Add(PreviewSheet.Cell(target, 480, 270, phrase.Name, Bar(phrase, keys[k])));
                    }
            PreviewSheet.Save(renderer.Device, renderer.Pixel, cells, options.Zooms.Length, 640, 360,
                $"{phrase.Name} KEY {k + 1} T{keys[k] - phrase.FirstBorn:+0;-0;0}", Path.Combine(output, $"matrix-{phrase.Name}-k{k + 1}.png"));
            written++;
        }
    }

    // Loads PortalBeam.fxc exactly like production and draws its real passes for a TrackingBeam
    // through the Vfx foundation, with the authoritative overlay on top.
    private void ShaderSmoke(PreviewGrid score, PreviewPlayer player)
    {
        var phrase = PreviewPlanner.Build("shader-smoke", score, 0, 1, player, options.PhraseStart);
        var plan = phrase.Plans[1];
        var cells = new List<SheetCell>();
        var moments = new (int Tick, bool Reduced)[]
        {
            (plan.Born + 12, false), (plan.Fire - 8, false), (plan.Fire - 1, false), (plan.Fire + 4, false),
            (plan.Fire + 16, false), (plan.Fire + 16, true)
        };
        foreach (var (tick, reduced) in moments)
        {
            string label = $"T{tick - plan.Fire:+0;-0;0} FIRE{(reduced ? " REDUCED" : "")}";
            var target = renderer.Render(phrase, tick, Backdrop.Night, .65f, reduced, "shader-smoke PortalBeam.fxc " + label,
                view => renderer.DrawPortalBeam(view, plan));
            if (options.Files) Save(target, Path.Combine(output, "shader-smoke", $"portalbeam-t{tick - plan.Fire:+000;-000;+000}{(reduced ? "-reduced" : "")}.png"));
            cells.Add(PreviewSheet.Cell(target, 640, 360, label, Bar(phrase, tick)));
        }
        PreviewSheet.Save(renderer.Device, renderer.Pixel, cells, 3, 640, 360, "SHADER SMOKE PORTALBEAM FXC", Path.Combine(output, "shader-smoke.png"));
        written++;
        Console.WriteLine($"shader smoke: PortalBeam.fxc loaded from {renderer.AssetsShaderPath("PortalBeam")} and drawn ({moments.Length} frames).");
    }

    private void Save(RenderTarget2D target, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = File.Create(path);
        target.SaveAsPng(file, target.Width, target.Height);
        written++;
    }

    private static string Label(PreviewPhrase phrase, int tick, int first)
    {
        var tags = new List<string> { $"T{tick - first:+0;-0;0}" };
        for (int i = 0; i < phrase.Plans.Length; i++)
        {
            if (phrase.Plans[i].Born is var b && b >= tick && b < tick + 8) tags.Add("W" + i);
            if (phrase.Plans[i].Fire is var f && f >= tick && f < tick + 8) tags.Add("F" + i);
        }
        return string.Join(" ", tags);
    }

    private static Color Bar(PreviewPhrase phrase, int tick)
    {
        bool warning = false, residue = false;
        foreach (var plan in phrase.Plans)
        {
            var phase = ScarletGeometryOverlay.Classify(plan, tick);
            if (phase == ScarletOverlayPhase.Active) return new Color(226, 32, 48);
            warning |= phase == ScarletOverlayPhase.Warning;
            residue |= phase == ScarletOverlayPhase.Residue;
        }
        return warning ? new Color(240, 240, 240) : residue ? new Color(110, 20, 32) : new Color(60, 60, 66);
    }
}

// Draws one frame: backdrop, stand-in characters, the production overlay, then the HUD mask.
internal sealed class PreviewRenderer : IDisposable
{
    internal readonly GraphicsDevice Device;
    internal readonly Texture2D Pixel;
    internal string Root = "";
    internal PreviewGrid? Score;
    private readonly PreviewAssets assets;
    private readonly PreviewOptions options;
    private readonly ScarletGeometryOverlay overlay;
    private readonly ScarletInkStroke ink = new();
    private readonly SpriteBatch batch;
    internal SpriteBatch Batch => batch;
    internal PreviewAssets Assets => assets;
    private readonly Texture2D disc;
    private readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private RenderTarget2D? target;

    internal PreviewRenderer(GraphicsDevice device, PreviewAssets assets, PreviewOptions options, string root)
    {
        Device = device; this.assets = assets; this.options = options; Root = root;
        overlay = new ScarletGeometryOverlay(device);
        batch = new SpriteBatch(device);
        Pixel = new Texture2D(device, 1, 1);
        Pixel.SetData(new[] { Color.White });
        // Soft-edged unit disc, premultiplied; stretched into the Eidolon's 420 x 360 body ellipse.
        const int n = 256;
        var pixels = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = MathF.Sqrt((x + .5f - n / 2f) * (x + .5f - n / 2f) + (y + .5f - n / 2f) * (y + .5f - n / 2f));
                float a = Math.Clamp(n / 2f - d, 0, 1);
                pixels[y * n + x] = new Color(a, a, a, a);
            }
        disc = new Texture2D(device, n, n);
        disc.SetData(pixels);
    }

    internal string AssetsShaderPath(string name) => assets.ShaderPath(name);

    internal PreviewContract.Result CheckContract(PreviewPhrase phrase, int tick)
        => PreviewContract.Run(Device, overlay, phrase, tick);

    public void Dispose()
    {
        overlay.Dispose(); batch.Dispose(); Pixel.Dispose(); disc.Dispose(); target?.Dispose();
    }

    internal RenderTarget2D Target(int width, int height)
    {
        if (target is null || target.Width != width || target.Height != height)
        {
            target?.Dispose();
            target = new RenderTarget2D(Device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        }
        return target;
    }

    // The returned target is reused by the next call; consume (save / downsample) it first.
    internal RenderTarget2D Render(PreviewPhrase phrase, int tick, Backdrop backdrop, float zoom, bool reduced,
        string caption, Action<ScarletView>? underlay = null)
    {
        int width = options.Width, height = options.Height;
        var field = PreviewPlanner.Field;
        // Like the game, the camera follows the player; when the whole field fits on screen at this
        // zoom it is centred on the field instead so the full arena can be read.
        bool fits = (field.Right - field.Left) * zoom <= width && (field.Bottom - field.Top) * zoom <= height;
        Vector2 camera = fits ? new Vector2(field.CenterX, field.CenterY) : phrase.Player.Center;
        var view = ScarletView.Create(Device, width, height, camera - new Vector2(width, height) * .5f, zoom, tick, 0, reduced);
        var rt = Target(width, height);
        Device.SetRenderTarget(rt);
        Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
        DrawBackdrop(view, backdrop, phrase);
        DrawCharacters(view, phrase);
        underlay?.Invoke(view);
        if (options.Look == "proposal")
            // The shipped decision, made per plan exactly as CrimsonGestureVisuals.DrawTrackingBeams does: a field beam
            // (TrackingBeam, SideBeams) shows the original portal forecast until Fire, then ScarletInk for the live strike and
            // its residue. Every other technique has its own production material (ScarletMaterials / ScarletSorcery /
            // ScarletClusters) that the preview does not reproduce; PortalBeam is only a stand-in silhouette for those.
            // The seal crossflow's two seals and vapor (Terraria-bound ScarletSorcery) are not drawn either.
            foreach (var plan in phrase.Plans)
            {
                if (ScarletInkStroke.Owns(plan, view.Clock)) ink.Draw(view, assets, plan);
                else DrawPortalBeam(view, plan);
            }
        else
        {
            if (options.Look.Contains("portal")) foreach (var plan in phrase.Plans) DrawPortalBeam(view, plan);
            if (options.Look.Contains("ink")) foreach (var plan in phrase.Plans) ink.Draw(view, assets, plan);
        }
        if (options.Look.Contains("overlay")) overlay.Draw(view, phrase.Plans);
        DrawHud(view, phrase, caption);
        Device.SetRenderTarget(null);
        return rt;
    }

    internal void DrawBackdrop(in ScarletView view, Backdrop kind, PreviewPhrase phrase)
    {
        int w = view.Width, h = view.Height;
        if (kind == Backdrop.Sanctum)
        {
            // CrimsonSky.Draw: the approved painting, covering the screen at 1.10x, through ScarletBackdrop.fxc.
            var art = assets.GetTexture("Backgrounds/ScarletSanctum");
            float scale = MathF.Max(w / (float)art.Width, h / (float)art.Height) * 1.10f;
            Vector2 size = new(w / (art.Width * scale), h / (art.Height * scale));
            float age = view.Clock;
            Vector2 drift = view.Reduced ? Vector2.Zero
                : new Vector2(MathF.Sin(view.ScreenPosition.X * .00035f + age * .0009f) * .012f, MathF.Sin(view.ScreenPosition.Y * .00040f + age * .0011f) * .008f);
            Vector2 origin = (Vector2.One - size) * .5f + drift;
            Vector4 weights = phrase.Phase switch { 0 => Vector4.UnitX, 1 => Vector4.UnitY, 2 => Vector4.UnitZ, _ => Vector4.UnitW };
            float beat = Score is null ? 0 : Score.Pulse(Math.Max(0, age - phrase.MusicStart));
            var fx = assets.GetEffect("ScarletBackdrop");
            Set(fx, "uWorldViewProjection", Matrix.CreateOrthographicOffCenter(0, w, h, 0, -1, 1));
            Set(fx, "crop", new Vector4(origin, size.X, size.Y));
            Set(fx, "phaseWeights", weights);
            Set(fx, "signal", new Vector4(1, view.Reduced ? 0 : beat, 0, view.Reduced ? 1 : 0));
            Set(fx, "clock", age / 60);
            DrawQuad(fx, "AutoloadPass", Vector2.Zero, new(0, h), new(w, 0),
                (0, art, SamplerState.LinearClamp), (1, assets.GetTexture("Noise/TurbulentNoise"), SamplerState.LinearWrap),
                (2, assets.GetTexture("Noise/WavyBlotchNoise"), SamplerState.LinearWrap));
            return;
        }
        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        if (kind == Backdrop.Night) batch.Draw(Pixel, new Rectangle(0, 0, w, h), new Color(17, 22, 36));
        else
            for (int y = 0; y < h; y += 8)
                batch.Draw(Pixel, new Rectangle(0, y, w, 8), Color.Lerp(new Color(96, 160, 232), new Color(176, 216, 250), y / (float)h));
        batch.End();
    }

    private void DrawCharacters(in ScarletView view, PreviewPhrase phrase)
    {
        var field = PreviewPlanner.Field;
        Matrix world = Matrix.CreateTranslation(-view.ScreenPosition.X, -view.ScreenPosition.Y, 0) * view.GameView;
        // Apparition heights as drawn by CrimsonRig.DrawEffigy, at the pose position CrimsonGesture.ProjectMotion gives
        // (the attack staging point, 210 px under the field top). Texture only: no mesh, no shader.
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        if (phrase.Phase < 3)
        {
            string[] names = { "EmberCrown", "SableMantle", "ThornChoir" };
            float[] heights = { 350, 430, 490 };
            var art = assets.GetTexture("CrimsonFoundry/" + names[phrase.Phase]);
            var stage = phrase.Plans[0].Stage;
            batch.Draw(art, new Vector2(stage.X, stage.Y), null, Color.White * .92f, 0, new Vector2(art.Width, art.Height) * .5f,
                heights[phrase.Phase] / art.Height, SpriteEffects.None, 0);
        }
        else
        {
            // Final Eidolon: only its 420 x 360 body box is known here, so a plain ellipse stands in.
            Vector2 centre = new(field.CenterX, field.CenterY);
            Vector2 unit = new Vector2(CrimsonEnsemble.BodyWidth, CrimsonEnsemble.BodyHeight) / disc.Width;
            batch.Draw(disc, centre, null, new Color(214, 70, 92) * .55f, 0, new Vector2(disc.Width * .5f), unit + new Vector2(.03f), SpriteEffects.None, 0);
            batch.Draw(disc, centre, null, new Color(30, 10, 18) * .85f, 0, new Vector2(disc.Width * .5f), unit, SpriteEffects.None, 0);
        }
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        // Vespera: pose 0 of ScarletConjurer, 56 px tall, fixed at the field centre (CrimsonRig.poses/pivots).
        var vespera = assets.GetTexture("CrimsonFoundry/ScarletConjurer");
        batch.Draw(vespera, new Vector2(field.CenterX, field.CenterY), new Rectangle(20, 202, 335, 540), Color.White, 0,
            new Vector2(190, 278), 56f / 540, SpriteEffects.None, 0);
        // A 20 x 42 player and, while an aimed note is alive, its locked aim point.
        var player = phrase.Player.Center;
        batch.Draw(Pixel, new Rectangle((int)player.X - 10, (int)player.Y - 21, 20, 42), new Color(232, 200, 168));
        foreach (var plan in phrase.Plans)
        {
            if (!plan.Aimed || view.Clock < plan.Born || view.Clock >= plan.End) continue;
            var color = new Color(90, 220, 255) * .85f;
            batch.Draw(Pixel, new Rectangle((int)plan.Target.X - 14, (int)plan.Target.Y - 1, 28, 2), color);
            batch.Draw(Pixel, new Rectangle((int)plan.Target.X - 1, (int)plan.Target.Y - 14, 2, 28), color);
        }
        batch.End();
    }

    private void DrawHud(in ScarletView view, PreviewPhrase phrase, string caption)
    {
        var field = PreviewPlanner.Field;
        int w = view.Width, h = view.Height;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        if (options.Mask != MaskMode.None)
        {
            // CrimsonVisuals.Overlay: physical-pixel mask outside the field plus a 2 px red edge.
            Vector2 tl = view.ToTarget(new(field.Left, field.Top)), br = view.ToTarget(new(field.Right, field.Bottom));
            int left = Math.Clamp((int)MathF.Floor(tl.X), 0, w), right = Math.Clamp((int)MathF.Ceiling(br.X), 0, w);
            int top = Math.Clamp((int)MathF.Floor(tl.Y), 0, h), bottom = Math.Clamp((int)MathF.Ceiling(br.Y), 0, h);
            Color mask = options.Mask == MaskMode.Black ? Color.Black : Color.Black * .72f;
            Fill(new(0, 0, w, top), mask); Fill(new(0, bottom, w, h - bottom), mask);
            Fill(new(0, top, left, Math.Max(0, bottom - top)), mask); Fill(new(right, top, w - right, Math.Max(0, bottom - top)), mask);
            Color edge = new(213, 53, 67);
            if (tl.X >= 0 && tl.X < w) Fill(new(left, top, 2, Math.Max(0, bottom - top)), edge);
            if (br.X > 0 && br.X <= w) Fill(new(Math.Max(0, right - 2), top, 2, Math.Max(0, bottom - top)), edge);
            if (tl.Y >= 0 && tl.Y < h) Fill(new(left, top, Math.Max(0, right - left), 2), edge);
            if (br.Y > 0 && br.Y <= h) Fill(new(left, Math.Max(0, bottom - 2), Math.Max(0, right - left), 2), edge);
        }
        PreviewText.Draw(batch, Pixel, caption, new Vector2(12, 10), 3, new Color(240, 240, 245));
        PreviewText.Draw(batch, Pixel, "CHARACTERS = PLACEHOLDER TEXTURES, NOT PRODUCTION DRAWING", new Vector2(12, h - 44), 2, new Color(200, 200, 208));
        PreviewText.Draw(batch, Pixel, "OVERLAY = HIT SHAPES: WHITE LINE WARNING / RED LIVE / DARK RED RESIDUE / SAFE NOT DRAWN", new Vector2(12, h - 26), 2, new Color(200, 200, 208));
        batch.End();
        void Fill(Rectangle r, Color c) { if (r.Width > 0 && r.Height > 0) batch.Draw(Pixel, r, c); }
    }

    // ---- effect helpers -------------------------------------------------------------------------------------------

    private static void Set(Effect fx, string name, float value) => fx.Parameters[name]?.SetValue(value);
    private static void Set(Effect fx, string name, Vector3 value) => fx.Parameters[name]?.SetValue(value);
    private static void Set(Effect fx, string name, Vector4 value) => fx.Parameters[name]?.SetValue(value);
    private static void Set(Effect fx, string name, Matrix value) => fx.Parameters[name]?.SetValue(value);

    // Same vertex layout as CrimsonEnergy.Quad: start, start+across (v), start+along (u).
    private void DrawQuad(Effect fx, string pass, Vector2 start, Vector2 across, Vector2 along,
        params (int Slot, Texture2D Texture, SamplerState Sampler)[] textures)
    {
        quad[0] = new(new Vector3(start, 0), Color.White, new(0, 0));
        quad[1] = new(new Vector3(start + across, 0), Color.White, new(0, 1));
        quad[2] = new(new Vector3(start + along, 0), Color.White, new(1, 0));
        quad[3] = quad[2]; quad[4] = quad[1];
        quad[5] = new(new Vector3(start + across + along, 0), Color.White, new(1, 1));
        fx.CurrentTechnique.Passes[pass].Apply();
        Device.BlendState = BlendState.AlphaBlend; Device.DepthStencilState = DepthStencilState.None; Device.RasterizerState = RasterizerState.CullNone;
        foreach (var (slot, texture, sampler) in textures) { Device.Textures[slot] = texture; Device.SamplerStates[slot] = sampler; }
        Device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    // CrimsonEnergy.Draw's per-ray work for a field beam, driven by the same strokes the overlay draws.
    // Only the PortalBeam.fxc passes (forecast / jet / corona / mouth); RaidEnergy dust is left out.
    internal void DrawPortalBeam(in ScarletView view, CrimsonGesturePlan plan)
    {
        float age = view.Clock;
        if (age < plan.Born || age >= plan.End) return;
        var strokes = new List<CrimsonStroke>();
        ScarletGeometryOverlay.Collect(plan, age, strokes);
        var fx = assets.GetEffect("PortalBeam");
        var noise = new (int, Texture2D, SamplerState)[]
        {
            (1, assets.GetTexture("Noise/WavyBlotchNoise"), SamplerState.LinearWrap),
            (2, assets.GetTexture("Noise/TurbulentNoise"), SamplerState.LinearWrap),
            (3, assets.GetTexture("Noise/DendriticNoiseZoomedOut"), SamplerState.LinearWrap)
        };
        Set(fx, "uWorldViewProjection", view.ScreenClip);
        float detail = view.Detail; // local functions cannot capture an in parameter
        Vector2 screenPosition = view.ScreenPosition;
        bool live = age >= plan.Fire;
        float opacity = live ? 1 : .95f * CrimsonInvocation.Ease((age - plan.Born) / 5);
        var signal = new Vector4(Math.Clamp((age - plan.Born) / Math.Max(1, plan.Fire - plan.Born), 0, 1), live ? 1 : 0, opacity,
            MathF.Exp(-Math.Max(0, age - plan.Fire) / 4));
        for (int i = 0; i < strokes.Count; i++)
        {
            var s = strokes[i];
            Vector2 a = new Vector2(s.A.X, s.A.Y) - screenPosition, delta = new Vector2(s.B.X - s.A.X, s.B.Y - s.A.Y);
            float length = delta.Length();
            if (length < .01f || s.Radius < .01f) continue;
            Vector2 d = delta / length, normal = new(-d.Y, d.X);
            DrawPass(live ? "AutoloadPass" : "PortalForecastPass", s.Radius);
            if (live) DrawPass("PortalCoronaPass", s.Radius + Math.Min(24, s.Radius * .3f));
            float radius = live ? 38 + signal.W * 30 : 12 + signal.X * 24;
            Set(fx, "shape", new Vector4(radius * 2, radius, i, detail));
            Set(fx, "signal", signal); Set(fx, "clock", age / 60);
            Set(fx, "ceremony", new Vector4(age - plan.Fire, Math.Max(0, age - plan.End), 0, 0));
            DrawQuad(fx, "PortalMouthPass", a - d * radius - normal * radius, normal * radius * 2, d * radius * 2, noise);
            void DrawPass(string pass, float width)
            {
                Set(fx, "beamColor", new Vector3(1, .06f, .13f)); Set(fx, "signal", signal);
                Set(fx, "shape", new Vector4(length, width, (s.A.X + s.A.Y) * .001f % 11, detail));
                Set(fx, "clock", age / 60);
                Set(fx, "ceremony", new Vector4(age - plan.Fire, Math.Max(0, age - plan.End), 0, 0));
                Vector2 n = normal * width;
                DrawQuad(fx, pass, a - n, n * 2, d * length, noise);
            }
        }
    }
}
