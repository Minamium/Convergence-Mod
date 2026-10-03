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
    internal bool Sequences = true, Matrix = true, Smoke = true, Files = true, Contract = true, Rewards;
    internal Backdrop[] Backdrops = { Backdrop.Sanctum, Backdrop.Night, Backdrop.Day };
    internal float[] Zooms = { .65f, 1f, 2f };
    internal Backdrop SequenceBackdrop = Backdrop.Night;
    internal float SequenceZoom = .65f;
    internal string[] Players = { "center", "edge" };
    internal bool[] ReducedModes = { false };
    internal MaskMode Mask = MaskMode.Dim;
    // overlay = authoritative hit shapes, ink = ScarletInk live/residue (field beams and signature moves), portal = the PortalBeam stand-in for every plan,
    // ink+overlay = both, proposal = what production draws, in its order (a signature move's yielding residue, then the
    // portal forecasts, then ScarletInk live strikes and residues) with the overlay left out.
    internal string Look = "overlay";
    // ScarletResidueYield.Enabled, the owner's switch for a signature move's residue (under the forecast, yielding).
    internal bool Yield = true;
}

// CurtainMask: the occupied columns an Act I signature phrase observes (0 = the player's own column).
internal sealed record SceneDef(string Name, int Phase, int Serial, bool Pickup = false, int CurtainMask = 0);

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
                case "yield": o.Yield = value != "off"; break;
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
    private PreviewContract.YieldResult residueYield;

    internal PreviewRun(PreviewRenderer renderer, PreviewOptions options, string output)
    {
        this.renderer = renderer; this.options = options; this.output = output;
    }

    // Real phrases (protocol80): an Act's first phrase (pickup crossflow on its downbeat, cell A, closing crossflow), the
    // ordinary cells of Acts II/III, each Act's signature phrase (four steps, the last on the next downbeat; Act I also
    // with three occupied columns 1, 5 and 8), the three Final pair families (the drop opens with a pickup cluster) and
    // the closing crossflow at real size.
    private static readonly SceneDef[] Scenes =
    {
        new("act1", 0, 1, true), new("act2", 1, 2), new("act3", 2, 1),
        new("act1-sig", 0, 3), new("act1-sig-trio", 0, 3, false, 1 << 1 | 1 << 5 | 1 << 8),
        new("act2-sig", 1, 6), new("act3-sig", 2, 9),
        new("final-drop", 3, 1, true), new("final-grid-tracking", 3, 2), new("final-tracking-rift", 3, 3),
        new("crossflow", 0, 1)
    };

    internal int Execute()
    {
        if (options.Rewards) return RewardsPreview.Run(renderer, options, output);
        // Gameplay has one fixed 128 BPM grid since #101 (CrimsonMeter).
        const string beats = "CrimsonMeter 128 BPM grid (28.125 ticks per beat)";
        ScarletResidueYield.Enabled = options.Yield;
        Console.WriteLine($"beats: {beats}; phrase start (earliest tick after musicStart): {options.PhraseStart}; signature residue yield {(options.Yield ? "on" : "off")}");
        var players = PreviewPlanner.Players().Where(p => options.Players.Contains(p.Name)).ToArray();
        var phrases = new List<PreviewPhrase>();
        foreach (var scene in Scenes)
        {
            foreach (var player in players)
            {
                string name = scene.Name + "-" + player.Name;
                if (options.Only.Length > 0 && !name.Contains(options.Only, StringComparison.OrdinalIgnoreCase)) continue;
                phrases.Add(PreviewPlanner.Build(name, scene.Phase, scene.Serial, player, options.PhraseStart,
                    curtainMask: scene.CurtainMask, pickup: scene.Pickup));
            }
        }
        foreach (var phrase in phrases)
        {
            if (phrase.Name.StartsWith("crossflow", StringComparison.Ordinal)) { CrossflowSheet(phrase); continue; }
            Console.WriteLine($"{phrase.Name}: beats {string.Join(",", phrase.BeatTicks.Select(t => t - phrase.MusicStart))} | "
                + string.Join(" ", phrase.Plans.Select(p => $"{p.Technique}[B{p.Born - phrase.MusicStart}/F{p.Fire - phrase.MusicStart}/E{p.End - phrase.MusicStart}]")));
            if (options.Contract) CheckContract(phrase);
            if (options.Sequences) Sequence(phrase);
            if (options.Matrix) VariantSheets(phrase);
        }
        if (overview.Count > 0)
            PreviewSheet.Save(renderer.Device, renderer.Pixel, overview, 4, 480, 270, "OVERVIEW KEY FRAMES", Path.Combine(output, "contact-overview.png"));
        if (options.Smoke && (options.Only.Length == 0 || "shader-smoke".Contains(options.Only, StringComparison.OrdinalIgnoreCase)))
            ShaderSmoke(players.Length > 0 ? players[0] : PreviewPlanner.Players()[0]);
        File.WriteAllText(Path.Combine(output, "index.json"), JsonSerializer.Serialize(new
        {
            note = "Offline preview of the production geometry and Vfx foundation. Not a playtest; in-game acceptance is not_run.",
            beats, options.PhraseStart, options.Step, size = $"{options.Width}x{options.Height}", residueYield = options.Yield, scenes = index
        }, new JsonSerializerOptions { WriteIndented = true }));
        if (contract.Wrong > 0)
        {
            Console.Error.WriteLine($"FAIL overlay contract: {contract.Wrong} of {contract.Checked} sampled pixels differ from the authoritative capsules.");
            return 1;
        }
        if (residueYield.Wrong > 0)
        {
            Console.Error.WriteLine($"FAIL residue yield: {residueYield.Wrong} wrong of {residueYield.Strokes} signature residue strokes / lit pixels checked.");
            return 1;
        }
        if (contract.Checked > 0)
            Console.WriteLine($"overlay contract: {contract.Checked} sampled pixels ({contract.FillSamples} fill, {contract.RingSamples} outline) match the authoritative capsules, {contract.Wrong} wrong.");
        if (residueYield.Strokes > 0)
            Console.WriteLine($"residue yield ({(options.Yield ? "on" : "off")}): {residueYield.Strokes} signature residue strokes ({residueYield.Holding} keep the full residue), {residueYield.LitPixels} lit pixels at End+{ScarletResidueYield.YieldTicks}, {residueYield.Wrong} wrong.");
        Console.WriteLine($"PASS {written} PNGs under {output}. Production Vfx foundation + authoritative geometry; stand-in characters; no game launched.");
        return 0;
    }

    // The phrase's first note (an ordinary note or signature step) and the note that lands on the next downbeat
    // (the closing crossflow / cluster, or a signature move's final step).
    private static (CrimsonGesturePlan Note, CrimsonGesturePlan Closer) KeyNotes(PreviewPhrase phrase)
    {
        var notes = phrase.Plans.Where(p => p.Pulse < CrimsonChoreography.BasicNotes).OrderBy(p => p.Fire).ToArray();
        return (notes[0], phrase.Plans.OrderBy(p => p.Fire).Last());
    }
    private static int CloserKey(PreviewPhrase phrase, CrimsonGesturePlan closer)
        => closer.Fire + (closer.Technique == CrimsonTechnique.ClusterVolley ? 24 : closer.IsSignature ? 4 : 14);

    // Warning, both live key moments and a residue moment, at 1:1 over black.
    private void CheckContract(PreviewPhrase phrase)
    {
        var (note, closer) = KeyNotes(phrase);
        int[] ticks = { note.Born + 10, note.Fire + 5, CloserKey(phrase, closer), note.End + 6 };
        var result = new PreviewContract.Result();
        foreach (int tick in ticks) result += renderer.CheckContract(phrase, tick);
        contract += result;
        Console.WriteLine($"  contract {phrase.Name}: {result.Checked} samples, {result.Wrong} wrong");
        if (!phrase.Plans.Any(p => p.IsSignature)) return;
        var yielded = renderer.CheckYield(phrase);
        residueYield += yielded;
        Console.WriteLine($"  residue yield {phrase.Name}: {yielded.Strokes} strokes, {yielded.Holding} full, {yielded.Wrong} wrong");
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

    // The closing seal crossflow at real size (zoom 1) over the approved sanctum, drawn the production way (--look proposal):
    // charge, release, the stream's growth and full width, the collapse and the residue, 2x close-ups of both seal ends and
    // three consecutive ticks of the collapse.
    private void CrossflowSheet(PreviewPhrase phrase)
    {
        var x = phrase.Plans.Where(p => p.Technique == CrimsonTechnique.SideBeams).OrderBy(p => p.Fire).Last();
        var (right, left) = CrimsonChoreography.Seals(x);
        var centre = new Vector2((right.X + left.X) * .5f, right.Y);
        int life = x.End - x.Fire;
        var moments = new (int Offset, float Zoom, Vector2 Focus, string Tag)[]
        {
            (-40, 1, centre, "charge"), (-4, 1, centre, "charged"), (0, 1, centre, "release"), (3, 1, centre, "reach"),
            (7, 1, centre, "reached"), (12, 1, centre, "full"), (30, 1, centre, "flow"), (life - 8, 1, centre, "closing"),
            (life + 6, 1, centre, "residue"),
            (12, 2, new(right.X, right.Y), "right seal 2x"), (12, 2, new(left.X, left.Y), "left seal 2x"), (30, 2, new(left.X, left.Y), "left seal flow 2x"),
            // Three consecutive ticks while the stream narrows: the black blood must flow on, not re-roll every tick.
            (life - 12, 1, centre, "narrowing"), (life - 11, 1, centre, "narrowing +1"), (life - 10, 1, centre, "narrowing +2")
        };
        foreach (var backdrop in new[] { Backdrop.Sanctum, Backdrop.Night })
        {
            var cells = new List<SheetCell>();
            int index = 0;
            foreach (var (offset, zoom, focus, tag) in moments)
            {
                index++;
                int tick = x.Fire + offset;
                string label = $"F{offset:+0;-0;0} {tag}";
                var target = renderer.Render(phrase, tick, backdrop, zoom, false, $"{phrase.Name} {label}", focus: focus);
                if (options.Files) Save(target, Path.Combine(output, phrase.Name, $"{backdrop}-{index:00}-f{offset:+000;-000;+000}-z{zoom}.png".ToLowerInvariant()));
                cells.Add(PreviewSheet.Cell(target, 640, 360, label, Bar(phrase, tick)));
            }
            PreviewSheet.Save(renderer.Device, renderer.Pixel, cells, 3, 640, 360, $"{phrase.Name} CROSSFLOW REAL SIZE {backdrop}",
                Path.Combine(output, $"crossflow-{phrase.Name}-{backdrop}.png".ToLowerInvariant()));
            written++;
        }
        Console.WriteLine($"{phrase.Name}: crossflow B{x.Born - phrase.MusicStart}/F{x.Fire - phrase.MusicStart}/E{x.End - phrase.MusicStart} seals {left.X:F0}..{right.X:F0} y {right.Y:F0}");
    }

    // Two key moments per phrase: the second note's live window (warning of the next one overlaps it),
    // and the fifth note (crossflow in Acts, ClusterVolley in the Final).
    private void VariantSheets(PreviewPhrase phrase)
    {
        var (note, closer) = KeyNotes(phrase);
        var keys = new[] { note.Fire + 5, CloserKey(phrase, closer) };
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
    private void ShaderSmoke(PreviewPlayer player)
    {
        var phrase = PreviewPlanner.Build("shader-smoke", 0, 1, player, options.PhraseStart);
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

    internal PreviewContract.YieldResult CheckYield(PreviewPhrase phrase)
        => PreviewContract.ResidueYield(Device, ink, assets, phrase);

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
        string caption, Action<ScarletView>? underlay = null, Vector2? focus = null)
    {
        int width = options.Width, height = options.Height;
        var field = PreviewPlanner.Field;
        // Like the game, the camera follows the player; when the whole field fits on screen at this
        // zoom it is centred on the field instead so the full arena can be read.
        bool fits = (field.Right - field.Left) * zoom <= width && (field.Bottom - field.Top) * zoom <= height;
        Vector2 camera = focus ?? (fits ? new Vector2(field.CenterX, field.CenterY) : phrase.Player.Center);
        var view = ScarletView.Create(Device, width, height, camera - new Vector2(width, height) * .5f, zoom, tick, 0, reduced);
        var rt = Target(width, height);
        Device.SetRenderTarget(rt);
        Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
        DrawBackdrop(view, backdrop, phrase);
        DrawCharacters(view, phrase);
        underlay?.Invoke(view);
        if (options.Look == "proposal")
        {
            // The shipped decision, in the order CrimsonGestureVisuals.DrawTrackingBeams draws it: a field beam (TrackingBeam,
            // SideBeams) or signature move (CinderCurtain, ShroudRope, FourHands) shows the original portal forecast until Fire,
            // then ScarletInk for the live strike and its residue. The crossflow's seals (ScarletSorcery.fxc, drawn like
            // ScarletSorcery.CrossflowSeals) lie under the forecast veil while they charge and, from Fire until they fade, over
            // the live stream, which is the band between the stream ends cut square on both (ScarletInkStroke), each cut sinking
            // into its seal's ring. A signature move's yielding residue goes under every forecast; the forecasts follow; live
            // strikes and the other residues lie on top, the live seals last. Every other technique has its own production
            // material (ScarletMaterials / ScarletSorcery / ScarletClusters) that the preview does not reproduce; PortalBeam is
            // only a stand-in silhouette for those.
            float clock = view.Clock;
            var sealsOver = new List<CrimsonGesturePlan>();
            foreach (var plan in phrase.Plans)
            {
                if (plan.Technique != CrimsonTechnique.SideBeams || clock < plan.Born || clock >= plan.End) continue;
                if (clock < plan.Fire) DrawSeals(view, plan); else sealsOver.Add(plan);
            }
            foreach (var plan in phrase.Plans) if (ScarletInkStroke.Underlies(plan, clock)) ink.Draw(view, assets, plan, phrase.Plans);
            foreach (var plan in phrase.Plans) if (!ScarletInkStroke.Owns(plan, clock)) DrawPortalBeam(view, plan);
            foreach (var plan in phrase.Plans)
                if (ScarletInkStroke.Owns(plan, clock) && !ScarletInkStroke.Underlies(plan, clock)) ink.Draw(view, assets, plan, phrase.Plans);
            foreach (var plan in sealsOver) DrawSeals(view, plan);
        }
        else
        {
            if (options.Look.Contains("portal")) foreach (var plan in phrase.Plans) DrawPortalBeam(view, plan);
            if (options.Look.Contains("ink")) foreach (var plan in phrase.Plans) ink.Draw(view, assets, plan, phrase.Plans);
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
            float beat = CrimsonMeter.Pulse(Math.Max(0, age - phrase.MusicStart));
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

    // ScarletSorcery.CrossflowSeals on the preview device: the same seal centres, charge, alpha, radius, flattening and
    // vapor quad, through ScarletSorcery.fxc's AutoloadPass / VaporPass with its production parameters.
    internal void DrawSeals(in ScarletView view, CrimsonGesturePlan p)
    {
        float age = view.Clock;
        var (right, left) = CrimsonChoreography.Seals(p);
        float charge = Math.Clamp((age - p.Born) / (p.Fire - p.Born), 0, 1);
        float alpha = CrimsonInvocation.Ease((age - p.Born) / 7) * (1 - CrimsonInvocation.Ease((age - (p.End - 15)) / 15));
        float radius = 80 + charge * 105;
        Seal(view, new(right.X, right.Y), radius, .28f, age, charge, alpha, p.Phrase);
        Seal(view, new(left.X, left.Y), radius, .28f, age, charge, alpha, p.Phrase + .5f);
        float smoke = CrimsonInvocation.Ease((age - p.Fire - 7) / 12) * alpha;
        if (smoke > .001f)
            Sorcery(view, new(left.X - 170, left.Y - 200), new(240, 0), new(0, 400), age,
                new(charge, 1, smoke, view.Reduced ? 1 : 0), new(240, 200, p.Phrase, 0), "VaporPass");
    }
    // ScarletSorcery.Seal at angle pi/2: u runs down the seal's long axis, v across its flattened width.
    private void Seal(in ScarletView view, Vector2 center, float radius, float flatten, float age, float charge, float alpha, float seed)
    {
        if (radius < .1f || alpha <= .001f) return;
        Vector2 u = new(0, radius * 2), v = new(-radius * 2 * flatten, 0);
        Sorcery(view, center - u * .5f - v * .5f, u, v, age, new(charge, 0, alpha, view.Reduced ? 1 : 0), new(radius * 2, radius, seed, 0), "AutoloadPass");
    }
    private void Sorcery(in ScarletView view, Vector2 origin, Vector2 u, Vector2 v, float age, Vector4 signal, Vector4 shape, string pass)
    {
        var fx = assets.GetEffect("ScarletSorcery");
        Set(fx, "uWorldViewProjection", view.ScreenClip);
        Set(fx, "clock", age / 60); Set(fx, "signal", signal); Set(fx, "shape", shape);
        Set(fx, "hue", new Vector3(.82f, .04f, .11f));
        Set(fx, "cutTint", new Vector3(1, .015f, .065f)); Set(fx, "cutCore", new Vector3(1, .82f, .84f));
        Set(fx, "cutHot", new Vector3(1, .12f, .19f)); Set(fx, "cutSmoke", new Vector3(.13f, .025f, .04f));
        Set(fx, "cutForecast", new Vector3(.9f, .83f, .9f));
        // DrawQuad's 'along' is the u texture axis and 'across' the v axis, like ScarletSorcery's mesh.
        DrawQuad(fx, pass, origin - view.ScreenPosition, v, u,
            (1, assets.GetTexture("Noise/WavyBlotchNoise"), SamplerState.LinearWrap),
            (2, assets.GetTexture("Noise/DendriticNoiseZoomedOut"), SamplerState.LinearWrap));
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
