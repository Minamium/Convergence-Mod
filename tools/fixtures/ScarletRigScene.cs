// Scarlet rig harness: the REAL Vespera (CrimsonRig.DrawPerformer, CrimsonRig.Performer.cs linked unchanged), the REAL
// apparition rigs (ScarletApparitionRig, CrimsonChoirRig), the REAL forecasts (CrimsonEnergy), seals
// (ScarletSorcery) and strikes (ScarletInkStroke) over the real backdrop shader, composited in the in-game layer
// order and filmed through the in-game cameras. Driven by real phrases from the production rhythm (the S0
// planner fixture). Offline review on a hidden FNA D3D11 device: no game, Terraria or server is started.
// Every frame is labelled "offline render, not a playtest"; in-game acceptance stays not_run.
//
// Layer order (tML 1.4.4.9 Main.DoDraw, read from tModLoader.dll IL):
//   1. background: CrimsonSky (ScarletBackdrop.fxc with the beat pulse and the strike impulse)
//   2. NPCs: Main.DrawNPCs walks slots 199 -> 0, so the apparition (summoned later, higher slot) is drawn
//      first and Vespera (the boss, lower slot) over it
//   3. PostDrawTiles: CrimsonGestureVisuals.DrawTrackingBeams in its own order: the crossflow seals while they
//      charge, then a signature move's yielding residue (under the forecasts), then every forecast
//      (CrimsonEnergy.Draw), then live strikes and the other residues (ScarletInk), then a live crossflow's seals
//      over its stream (protocol80)
//   4. players (20 x 42 boxes), then 5. the participant mask (black outside the field + 2 px red edge)
// Not reproduced offline (labelled where they would show): terrain, walls and lighting, the metaball embers of
// basic Act I/III strikes (Luminance metaball RTs), the ChoirRakes ribbon (Luminance trail tessellator; drawn
// here as the hit-shape overlay), camera shake, cinematic camera, frame pacing (RenderAge sub-tick blend).
//
// The signal, the Choir cues and the notes are the production plan-list functions (CrimsonRig.Signal,
// ScarletNotes) over the gestures alive at the tick, as CrimsonRig.DrawEffigy reads ScarletCueFrame. The proposed
// motion (--motion proposed) and the body material (--material on) are computed by S1's ScarletGestureMotion /
// ScarletBodyMaterial and passed to the rigs exactly as DrawEffigy does (S2 draws them for Crown/Mantle, S3 for the
// Choir); Vespera is CrimsonRig.DrawConductor, which commands her Act's body under --motion proposed (S4). The game
// always draws "proposed" + "material on"; "current" / "off" are today's picture for comparison (RigDriver).
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Convergence.Client.Encounters.CrimsonFoundry;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed class RigOptions
{
    internal string[] Scenes = { "act1-signature", "act2-signature", "act3-signature" };
    internal string[] Cameras = { "A2" };
    internal bool[] Reduced = { false };
    internal bool[] Material = { false };
    internal bool[] Yield = { true };
    internal bool Proposed;
    internal string Frames = "stream";   // stream (webm through ffmpeg) | png (f####.png) | none
    internal string Ffmpeg = "";
    internal string Stills = "auto";     // auto (camera A2 only) | all | off
    internal string Gates = "on";        // on | off | only
    internal string Mask = "black";      // black (in game) | dim | none
    internal bool Labels = true;
    internal bool Context = true;        // the neighbouring phrases (serial -1 / +1) a real fight has on screen
    internal string Flip = "auto";       // auto | on | off (the apparition's spriteDirection < 0)
    internal string Baseline = "";       // G8 / G11 / G8v baseline directory (compared when its manifest allows, see RigBaseline)
    internal bool WriteBaseline;         // --write-baseline on: (re)write the baselines from the code under the harness now
    internal int Width = 1920, Height = 1080, Limit;
    internal int? From;                  // first frame relative to the phrase's first warning (default -50)
    internal bool Audio = true;
}

internal static class ScarletRigPreview
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
        var options = Parse(args.Skip(4).ToArray());
        IntPtr Resolve(string name, System.Reflection.Assembly a, DllImportSearchPath? p)
        { string file = Path.Combine(native, name.EndsWith(".dll") ? name : name + ".dll"); return File.Exists(file) ? NativeLibrary.Load(file) : IntPtr.Zero; }
        NativeLibrary.SetDllImportResolver(typeof(ScarletRigPreview).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly, Resolve);
        if (SDL_Init(0x20) != 0) throw new Exception("SDL video initialization failed");
        IntPtr window = SDL_CreateWindow("Scarlet rigs", 0, 0, 640, 360, FNA3D_PrepareWindowAttributes() | 0x8);
        if (window == IntPtr.Zero) throw new Exception("Hidden device surface unavailable");
        try
        {
            using var device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, new PresentationParameters
            {
                DeviceWindowHandle = window, BackBufferWidth = 640, BackBufferHeight = 360, BackBufferFormat = SurfaceFormat.Color,
                IsFullScreen = false, DepthStencilFormat = DepthFormat.Depth24Stencil8, PresentationInterval = PresentInterval.Immediate
            });
            RigHost.Device = device;
            RigHost.Wrapped = new PreviewDevice(device);
            using var assets = new PreviewAssets(device, root, luminance);
            RigHost.Assets = assets;
            // The production loaders (ImmediateLoad through the ModContent shim), Vespera's through CrimsonRig.LoadPerformer.
            ScarletApparitionRig.Load();
            CrimsonChoirRig.Load();
            CrimsonRig.LoadPreviewPerformer();
            Directory.CreateDirectory(output);
            using var renderer = new RigRenderer(device, assets, options);
            return new RigRun(renderer, options, root, output).Execute();
        }
        finally { SDL_DestroyWindow(window); SDL_Quit(); }
    }

    static RigOptions Parse(string[] args)
    {
        var o = new RigOptions();
        bool[] Switch(string v) => v switch { "on" => new[] { true }, "off" => new[] { false }, "both" => new[] { false, true },
            _ => throw new ArgumentException("expected on|off|both, got " + v) };
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--")) throw new ArgumentException("unexpected argument " + args[i]);
            string key = args[i][2..];
            string value = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "on";
            switch (key)
            {
                case "scenes": o.Scenes = value.Split(',', StringSplitOptions.RemoveEmptyEntries); break;
                case "cameras": o.Cameras = value.Split(',', StringSplitOptions.RemoveEmptyEntries); break;
                case "reduced": o.Reduced = Switch(value); break;
                case "material": o.Material = Switch(value); break;
                case "yield": o.Yield = Switch(value); break;
                case "motion": o.Proposed = value switch { "proposed" => true, "current" => false, _ => throw new ArgumentException("--motion current|proposed") }; break;
                case "frames": o.Frames = value; break;
                case "ffmpeg": o.Ffmpeg = value; break;
                case "stills": o.Stills = value; break;
                case "gates": o.Gates = value; break;
                case "mask": o.Mask = value; break;
                case "labels": o.Labels = value != "off"; break;
                case "context": o.Context = value != "off"; break;
                case "flip": o.Flip = value; break;
                case "baseline": o.Baseline = value; break;
                case "write-baseline": o.WriteBaseline = value != "off"; break;
                case "audio": o.Audio = value != "off"; break;
                case "limit": o.Limit = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "from": o.From = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "size":
                    var parts = value.Split('x');
                    o.Width = int.Parse(parts[0], CultureInfo.InvariantCulture); o.Height = int.Parse(parts[1], CultureInfo.InvariantCulture); break;
                default: throw new ArgumentException("unknown option --" + key);
            }
        }
        if (o.Frames == "stream" && o.Gates != "only" && o.Ffmpeg.Length == 0) throw new ArgumentException("--frames stream needs --ffmpeg <exe>");
        return o;
    }
}

internal sealed record RigSceneDef(string Name, int Phase, int Serial, int CurtainMask = 0, bool Pickup = false)
{
    // The phrases of protocol80 (CrimsonChoreography.Create): each Act's signature phrase (serial 3: four steps a dotted
    // quarter apart, the last on the next downbeat; the crossflow that released on its downbeat is the previous phrase's
    // closer), its first phrase (serial 1: the pickup crossflow on the downbeat, ordinary cell A on beats 3, 4.5 and 6,
    // the closing crossflow) and its second (serial 2: cell B on beats 3.5, 5 and 6, whose third note opens as the second
    // strikes, after the first phrase's closer); Act I's signature also with three occupied columns.
    internal static readonly RigSceneDef[] Catalog =
    {
        new("act1-signature", 0, 3), new("act2-signature", 1, 3), new("act3-signature", 2, 3),
        new("act1-basic", 0, 1, 0, true), new("act2-basic", 1, 1, 0, true), new("act3-basic", 2, 1, 0, true),
        new("act1-basic2", 0, 2), new("act2-basic2", 1, 2), new("act3-basic2", 2, 2),
        new("act1-signature-trio", 0, 3, 1 << 1 | 1 << 5 | 1 << 8),
    };
    internal static RigSceneDef Find(string name)
        => Array.Find(Catalog, s => s.Name == name) ?? throw new ArgumentException("unknown scene " + name + "; known: " + string.Join(",", Catalog.Select(s => s.Name)));
}

// Cameras, all 1920 x 1080. A/A2/C/V are in-game zooms (1 and 2); B is a review scale the game never uses.
internal sealed record RigCamera(string Name, float Zoom, string Subject, string Note)
{
    internal static readonly RigCamera[] All =
    {
        new("A", 1, "ground", "zoom 1 on the player standing on the floor (the in-game screen)"),
        new("A2", 1, "air", "zoom 1 on the player flying at Top+300"),
        new("C", 2, "apparition", "zoom 2 on the apparition"),
        new("V", 2, "vespera", "zoom 2 on Vespera (her 56 px drawn as 112 px)"),
        new("B", .65f, "field", "zoom 0.65 field overview: a review scale the game never uses"),
    };
    internal static RigCamera Find(string name)
        => Array.Find(All, c => c.Name == name) ?? throw new ArgumentException("unknown camera " + name);
}

internal readonly record struct RigVariant(bool Reduced, bool Material, bool Proposed, bool Yield)
{
    internal string Suffix => (Reduced ? "reduced" : "normal") + (Material ? "-material" : "") + (Proposed ? "-proposed" : "") + (Yield ? "" : "-yieldoff");
}

// One phrase scene filmed by one camera: the planned phrase (and its neighbours) plus everything the call sites
// read from the boss and the NPCs. Assumptions are written into state.json.
internal sealed class RigScene
{
    internal const int MusicStart = 3000;
    // Stage start bars of the arrangement (Act I at musicStart; Acts II/III assumed at bars 40 / 80, bar-aligned like
    // phaseStart). A phrase sits after the opening (Act I) or the two transition bars (Acts II/III), two bars per serial.
    internal static readonly int[] StageBars = { 0, 40, 80 };
    internal const int LeadTicks = 50, TailTicks = 44; // FirstBorn-50 .. End+24+20
    // A phrase with a previous one starts its film where the previous closer's seals begin to bloom, two beats before the
    // downbeat the crossflow releases on (CrimsonChoreography.Closer: 4 eighths of 14.06 ticks), so the release is on screen.
    internal const int CloserLeadTicks = 60;
    internal RigSceneDef Def = null!;
    internal RigCamera Camera = null!;
    internal PreviewPhrase Phrase = null!;
    internal PreviewPhrase? Previous, Next;
    internal CrimsonGesturePlan[] Plans = Array.Empty<CrimsonGesturePlan>(); // previous, current, next (slot order)
    internal string[] Roles = Array.Empty<string>();
    internal PreviewPlayer Player;
    internal Vector2[] Boxes = Array.Empty<Vector2>();
    internal int First, Last, PhraseBar;
    internal bool Flipped;
    internal string FlipReason = "";
    internal const int VesperaFacing = -1; // NPC.spriteDirection's default (-1 in the NPC ctor and SetDefaults IL); never written for her
    internal int Phase => Def.Phase;
    internal int Frames => Last - First + 1;

    // The scene's key notes. Notes: the phrase's strikes in order (an ordinary phrase's three cell notes, a signature
    // phrase's four steps), never a crossflow. Second / Third: by Fire. Flow: the seal crossflow the scene is about: an
    // ordinary phrase's own closing crossflow (released on the next downbeat), or, for a signature phrase (which has no
    // crossflow of its own), the previous phrase's closer that releases on its downbeat.
    internal CrimsonGesturePlan[] Notes => Phrase.Plans.Where(p => p.Technique != CrimsonTechnique.SideBeams).OrderBy(p => p.Fire).ToArray();
    internal CrimsonGesturePlan Second => Notes[1];
    internal CrimsonGesturePlan Third => Notes[2];
    internal IEnumerable<CrimsonGesturePlan> Flows
        => Phrase.Plans.Where(p => p.Technique == CrimsonTechnique.SideBeams && p.Pulse == CrimsonChoreography.Closer).Concat(
            Previous?.Plans.Where(p => p.Technique == CrimsonTechnique.SideBeams && p.Pulse == CrimsonChoreography.Closer) ?? Enumerable.Empty<CrimsonGesturePlan>());
    internal bool HasFlow => Flows.Any();
    internal CrimsonGesturePlan Flow => Flows.First();

    internal static RigScene Build(RigSceneDef def, RigCamera camera, RigOptions o)
    {
        var field = PreviewPlanner.Field;
        float x = field.CenterX + 160, y = camera.Subject == "air" ? field.Top + 300 : field.Bottom - 21;
        var player = new PreviewPlayer(camera.Subject == "air" ? "air" : "ground", new Vector2(x, y), Vector2.Zero);
        int bar = StageBars[def.Phase] + (def.Phase == 0 ? CrimsonMeter.OpeningBars : CrimsonArrangement.TransitionBars(def.Phase)) + 2 * (def.Serial - 1);
        var s = new RigScene { Def = def, Camera = camera, Player = player, PhraseBar = bar };
        s.Phrase = PreviewPlanner.Build(def.Name, def.Phase, def.Serial, player, CrimsonMeter.BarTick(bar), MusicStart, def.CurtainMask, def.Pickup);
        if (o.Context && def.Serial > 1)
            s.Previous = PreviewPlanner.Build(def.Name + "-previous", def.Phase, def.Serial - 1, player, CrimsonMeter.BarTick(bar - 2), MusicStart, pickup: def.Serial == 2);
        if (o.Context)
            s.Next = PreviewPlanner.Build(def.Name + "-next", def.Phase, def.Serial + 1, player, CrimsonMeter.BarTick(bar + 2), MusicStart);
        var plans = new List<CrimsonGesturePlan>(); var roles = new List<string>();
        void Add(PreviewPhrase? phrase, string role) { if (phrase is null) return; foreach (var p in phrase.Plans) { plans.Add(p); roles.Add(role); } }
        Add(s.Previous, "previous"); Add(s.Phrase, "current"); Add(s.Next, "next");
        s.Plans = plans.ToArray(); s.Roles = roles.ToArray();
        s.First = s.Phrase.FirstBorn + (o.From ?? -LeadTicks);
        if (o.From is null && s.Previous is not null) s.First = Math.Min(s.First, MusicStart + CrimsonMeter.BarTick(bar) - CloserLeadTicks);
        s.Last = s.Phrase.Plans.Max(p => p.End) + TailTicks;
        if (o.Limit > 0) s.Last = Math.Min(s.Last, s.First + o.Limit - 1);
        // Players: the aimed one, and for the trio the members standing in the other occupied columns.
        var boxes = new List<Vector2> { player.Center };
        for (int column = 0; column < CrimsonSignatureMoves.CurtainColumns; column++)
            if ((def.CurtainMask >> column & 1) != 0 && column != CrimsonSignatureMoves.CurtainColumn(field, x))
                boxes.Add(new Vector2(field.Left + CrimsonSignatureMoves.ColumnWidth * (column + .5f), y));
        s.Boxes = boxes.ToArray();
        // The apparition's facing: ProjectMotion writes spriteDirection only while the pose moves, i.e. while it
        // glides from its idle goal (CrimsonRuntime.Move: focus + offset) to the stage; consecutive phrases keep it.
        var stage = s.Phrase.Plans[0].Stage;
        Vector2[] offsets = { new(-230, -180), new(270, -80), new(-150, -290) };
        float idle = Math.Clamp(field.CenterX + 160 + offsets[def.Phase].X, field.Left + 24, field.Right - 24);
        int direction = Math.Abs(stage.X - idle) > .1f ? Math.Sign(stage.X - idle) : -1;
        s.Flipped = o.Flip switch { "on" => true, "off" => false, _ => direction < 0 };
        s.FlipReason = o.Flip == "auto" ? $"approach from the idle goal x={idle:0} to the stage x={stage.X:0} (spriteDirection {direction})" : "forced --flip " + o.Flip;
        return s;
    }

    // The same scene with another set of live gestures (G8: none at all).
    internal RigScene WithPlans(CrimsonGesturePlan[] plans, string[] roles) => new()
    {
        Def = Def, Camera = Camera, Phrase = Phrase, Previous = Previous, Next = Next, Plans = plans, Roles = roles, Player = Player,
        Boxes = Boxes, First = First, Last = Last, PhraseBar = PhraseBar, Flipped = Flipped, FlipReason = FlipReason
    };

    internal Vector2 Focus => Camera.Subject switch
    {
        "ground" or "air" => Player.Center,
        "apparition" => new Vector2(Phrase.Plans[0].Stage.X, Phrase.Plans[0].Stage.Y),
        "vespera" => Conductor,
        _ => new Vector2(PreviewPlanner.Field.CenterX, PreviewPlanner.Field.CenterY)
    };
    internal static Vector2 Conductor { get { var c = CrimsonChoreography.Conductor(PreviewPlanner.Field); return new(c.X, c.Y); } }
    internal Vector2 Stage => new(Phrase.Plans[0].Stage.X, Phrase.Plans[0].Stage.Y);
    internal static readonly float[] Heights = { 350, 430, 490 };

    internal ScarletView View(GraphicsDevice device, int width, int height, int tick, bool reduced)
        => ScarletView.Create(device, width, height, Focus - new Vector2(width, height) * .5f, Camera.Zoom, tick, 0, reduced);

    // Which notes are on screen at a tick (warning through residue).
    internal IEnumerable<int> Showing(float age)
    {
        for (int i = 0; i < Plans.Length; i++)
            if (age >= Plans[i].Born && age < Plans[i].End + ScarletGeometryOverlay.ResidueTicks(Plans[i])) yield return i;
    }
}

// The Terraria-bound call sites over the scene's plan list instead of Main.ActiveProjectiles. The signal, the Choir
// cues and the notes call the production plan-list functions (CrimsonRig.Signal in CrimsonRig.Performer.cs and
// ScarletNotes, linked unchanged) over the gestures alive at the tick, which is what ScarletCueFrame hands
// CrimsonRig.DrawEffigy / Draw in game. Acts I-III have no chorus plans.
internal static class RigMirror
{
    // A CrimsonGesture lives until LastEnd + CrimsonRhythm.LeaseTicks (CrimsonGesture.AI); it is spawned at scheduling.
    internal static bool Alive(in CrimsonGesturePlan p, float age) => age >= p.Begin && age < p.LastEnd + CrimsonRhythm.LeaseTicks;

    // ScarletCueFrame.Of(boss).Gestures at this tick: the plans of the boss's live gestures.
    internal static CrimsonGesturePlan[] Live(IReadOnlyList<CrimsonGesturePlan> plans, float age)
    {
        var live = new List<CrimsonGesturePlan>(plans.Count);
        foreach (var p in plans) if (Alive(p, age)) live.Add(p);
        return live.ToArray();
    }

    // ScarletCueFrame.Of(boss).Known at this tick: Live without an aimed plan whose lock has not arrived. The harness's plans
    // lock on Born (CrimsonTrackingBeam.LockAt, as on the server and in single player), so an aimed plan is known from Born on.
    internal static CrimsonGesturePlan[] Known(IReadOnlyList<CrimsonGesturePlan> plans, float age)
    {
        var known = new List<CrimsonGesturePlan>(plans.Count);
        foreach (var p in plans) if (Alive(p, age) && ScarletNotes.AimKnown(p, age >= p.Born)) known.Add(p);
        return known.ToArray();
    }

    // CrimsonRig.Signal(boss, source, age) = the production plan-list Signal over the frame (source -1 = all).
    internal static (float Charge, float Recoil) Signal(IReadOnlyList<CrimsonGesturePlan> plans, int source, float age)
        => CrimsonRig.Signal(Live(plans, age), ReadOnlySpan<CrimsonChorusPlan>.Empty, source, age);

    // CrimsonRig.DrawEffigy's Choir cues: ScarletNotes.ChoirCues (the arms owning the struck ground, flip applied).
    internal static int ChoirCues(IReadOnlyList<CrimsonGesturePlan> plans, float age, bool flipped, Span<CrimsonChoirCue> cues)
        => ScarletNotes.ChoirCues(Live(plans, age), age, flipped, cues);

    // CrimsonRig.DrawEffigy's notes for a member of the fight (the filmed player is in it), aimed from Vespera; the
    // Crown and Mantle pass lookback = ScarletNotes.PastTicks for their past poses.
    internal static int Notes(IReadOnlyList<CrimsonGesturePlan> plans, int source, float age, bool flipped, Span<ScarletNote> notes, float lookback = 0)
        => ScarletNotes.Collect(Known(plans, age), source, age, flipped, RigScene.Conductor.X, RigScene.Conductor.Y, notes, lookback);

    // CrimsonGestureVisuals.PostUpdateEverything's Cue (protocol80): an impact on every Fire; a foretell only where a
    // note is announced (the crossflow's charge on its Born, a signature move's first and final step); a curtain note
    // with nothing to burn has no cue. One voice per (phrase, tick, cue).
    internal readonly record struct SoundEvent(int Tick, string Cue, int Phrase, int Pulse, string Technique);
    internal static List<SoundEvent> Sounds(IReadOnlyList<CrimsonGesturePlan> plans)
    {
        var heard = new HashSet<(int, byte, byte, bool)>(); var voiced = new HashSet<(int, int, string)>();
        var events = new List<SoundEvent>();
        foreach (var p in plans)
            foreach (bool impact in new[] { false, true })
            {
                int tick = impact ? p.Fire : p.Born;
                if (p.Technique == CrimsonTechnique.CinderCurtain && CrimsonSignatureMoves.CurtainBurning(p) == 0) continue;
                if (!heard.Add((p.Phrase, p.Pulse, p.Source, impact))) continue;
                bool crossflow = p.Technique is CrimsonTechnique.SideBeams or CrimsonTechnique.ClusterVolley;
                bool announced = crossflow || p.IsSignature && (p.Pulse == 0 || p.Pulse == CrimsonChoreography.SignatureClimax);
                if (!impact && !announced) continue;
                string cue = crossflow ? impact ? "CrossflowRelease" : "CrossflowCharge" : impact ? "Impact" : "Foretell";
                if (voiced.Add((p.Phrase, tick, cue))) events.Add(new(tick, cue, p.Phrase, p.Pulse, p.Technique.ToString()));
            }
        events.Sort((a, b) => a.Tick.CompareTo(b.Tick));
        return events;
    }

    // ScarletAtmosphere.Impulse: the backdrop's strike impulse from the latest impact cue (Emit sets impactAt only
    // when not Reduced, and the backdrop reads 0 when Reduced).
    internal static float Impulse(IReadOnlyList<CrimsonGesturePlan> plans, float age)
    {
        float impactAt = -100;
        foreach (var p in plans)
        {
            if (p.Fire > age || p.Technique == CrimsonTechnique.CinderCurtain && CrimsonSignatureMoves.CurtainBurning(p) == 0) continue;
            impactAt = Math.Max(impactAt, p.Fire);
        }
        return MathF.Exp(-Math.Max(0, age - impactAt) / 10);
    }
}

// The S1/S2/S3/S4 seam: CrimsonRig.DrawEffigy's wiring (notes -> motion / heave / material) with the comparison
// switches. "current" passes the rigs' default motion (today's root, tilt, skin and heave) and --material off the
// default material (today's shading); --motion proposed passes ScarletGestureMotion (Crown / Mantle motion, the Choir
// heave) and --material on passes ScarletBodyMaterial, both computed exactly as DrawEffigy computes them. The notes
// (the apparitions' analytic particles and past poses) and the Choir cues (the arms owning the struck ground) are the
// production ones in every variant, as in game. Vespera is the boss path's CrimsonRig.DrawConductor: "proposed"
// passes her Act's body as DrawEffigy's local participant does (her command, S4); "current" passes -1 (today).
internal static class RigDriver
{
    // S2 (Crown / Mantle), S3 (Choir) and S4 (Vespera) draw what S1 computes; kept as switches so a slice that stops
    // drawing them reports its variants not_run instead of rendering today's picture under another name.
    internal static bool MotionAvailable => true;
    internal static bool MaterialAvailable => true;
    internal const string Pending = "S1's notes, motion, heave and material are drawn by the rigs (S2 Crown/Mantle, S3 Choir) and Vespera's command by DrawConductor (S4)";

    // CrimsonRig.DrawEffigy for the Act's apparition (phase < 3): appear 1, presence 1, dissolve 0, pose at the stage.
    internal static void Apparition(SpriteBatch batch, RigScene s, float age, in RigVariant v)
    {
        int index = s.Phase;
        var signal = RigMirror.Signal(s.Plans, index, age);
        float size = RigScene.Heights[index];
        Vector2 at = s.Stage; // TryPose holds the whole window: consecutive phrases keep the pose (Begin = previous LastEnd + 6)
        bool reduced = CrimsonVisuals.Reduced;
        Span<ScarletNote> notes = stackalloc ScarletNote[ScarletNotes.Capacity];
        int noted = RigMirror.Notes(s.Plans, index, age, s.Flipped, notes, index == 2 ? 0 : ScarletNotes.PastTicks);
        RigHost.Tag = index switch { 0 => "crown", 1 => "mantle", _ => "choir" };
        if (index == 2)
        {
            Span<CrimsonChoirCue> cues = stackalloc CrimsonChoirCue[16];
            int count = RigMirror.ChoirCues(s.Plans, age, s.Flipped, cues);
            // Material off keeps the proposed motion's attack clock (the heart's throb) and drops only the light
            // (Active); today's picture (current motion) has neither.
            var choir = v.Material || v.Proposed ? ScarletBodyMaterial.Choir(age, notes[..noted], reduced) : default;
            if (!v.Material) choir = choir with { Active = false };
            CrimsonChoirRig.Draw(batch, at, size, age, signal.Charge, signal.Recoil, 1, s.Flipped,
                0 + signal.Recoil * .045f, 0, cues: cues[..count],
                heave: v.Proposed ? ScarletGestureMotion.Heave : 0, material: choir);
        }
        else
        {
            var motion = index == 0 ? ScarletGestureMotion.Crown(age, notes[..noted]) : ScarletGestureMotion.Mantle(age, notes[..noted]);
            var body = v.Material ? ScarletBodyMaterial.Apparition(age, notes[..noted], motion, s.Flipped, reduced) : default;
            ScarletApparitionRig.Draw(batch, index, at, size, age, signal.Charge, signal.Recoil, 1, s.Flipped, 0, 0,
                notes: notes[..noted], motion: v.Proposed ? motion : default, material: body);
        }
        RigHost.Tag = "";
    }

    // CrimsonRig.Draw for the boss after the opening (reveal 1, consumed 0, ending 1): the production DrawConductor
    // (performer and held orb). --motion proposed passes the notes of her Act's body as the boss path does for a local
    // participant (Vespera's command, S4); "current" passes none, which is today's picture exactly (G8v).
    internal static void Vespera(SpriteBatch batch, RigScene s, float age, in RigVariant v)
    {
        RigHost.Tag = "vespera";
        CrimsonRig.DrawConductor(batch, Terraria.Main.screenPosition, RigScene.Conductor, age, Vector2.Zero, RigScene.VesperaFacing,
            RigMirror.Live(s.Plans, age), RigMirror.Known(s.Plans, age), ReadOnlySpan<CrimsonChorusPlan>.Empty, v.Proposed ? s.Phase : -1, 1, 1, 0, 0);
        RigHost.Tag = "";
    }
}

[Flags]
internal enum RigLayers
{
    None = 0, Backdrop = 1, Apparition = 2, Vespera = 4, Field = 8, Players = 16, Mask = 32, Labels = 64,
    Npc = Apparition | Vespera, Frame = Backdrop | Npc | Field | Players | Mask | Labels
}

internal sealed class RigRenderer : IDisposable
{
    internal readonly GraphicsDevice Device;
    internal readonly PreviewAssets Assets;
    internal readonly RigOptions Options;
    internal readonly Texture2D Pixel;
    private readonly SpriteBatch batch;
    private readonly ScarletInkStroke ink = new();
    private readonly ScarletGeometryOverlay overlay;
    private readonly List<CrimsonGesturePlan> strikes = new(), residues = new(), signatures = new(), sealsOver = new(), stand = new();
    private readonly CrimsonStroke[] strokes = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
    private readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private RenderTarget2D? frame;

    internal RigRenderer(GraphicsDevice device, PreviewAssets assets, RigOptions options)
    {
        Device = device; Assets = assets; Options = options;
        batch = new SpriteBatch(device);
        overlay = new ScarletGeometryOverlay(device);
        Pixel = new Texture2D(device, 1, 1);
        Pixel.SetData(new[] { Color.White });
    }

    public void Dispose() { batch.Dispose(); overlay.Dispose(); Pixel.Dispose(); frame?.Dispose(); }

    internal RenderTarget2D Frame(int width, int height)
    {
        if (frame is null || frame.Width != width || frame.Height != height)
        {
            frame?.Dispose();
            frame = new RenderTarget2D(Device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        }
        return frame;
    }

    internal static Color[] Read(RenderTarget2D target)
    {
        var pixels = new Color[target.Width * target.Height];
        target.GetData(pixels);
        return pixels;
    }

    // One frame of one scene at one tick through `view` (camera or any world window) into `target`.
    internal void Render(RigScene s, in ScarletView view, in RigVariant v, RigLayers layers, RenderTarget2D target, Color clear, string caption = "")
    {
        CrimsonVisuals.Reduced = v.Reduced;
        ScarletResidueYield.Enabled = v.Yield;
        Terraria.Main.screenPosition = view.ScreenPosition;
        Terraria.Main.GameViewMatrix.TransformationMatrix = view.GameView;
        Terraria.Main.GameViewMatrix.Zoom = new Vector2(view.Zoom);
        Device.SetRenderTarget(target);
        RigHost.CheckTarget(Device);
        Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, clear, 1f, 0);
        float age = view.Clock;
        if (layers.HasFlag(RigLayers.Backdrop)) Backdrop(s, view);
        if ((layers & RigLayers.Npc) != 0)
        {
            // NPC pass: Main.DrawNPCs' batch (world transform); slots 199 -> 0, the apparition before Vespera.
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
                Terraria.Main.Rasterizer, null, view.GameView);
            if (layers.HasFlag(RigLayers.Apparition)) RigDriver.Apparition(batch, s, age, v);
            if (layers.HasFlag(RigLayers.Vespera)) RigDriver.Vespera(batch, s, age, v);
            batch.End();
        }
        if (layers.HasFlag(RigLayers.Field))
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
                Terraria.Main.Rasterizer, null, view.GameView);
            RigHost.Tag = "field";
            TrackingBeams(s, view, age);
            Standins(s, view, age);
            RigHost.Tag = "";
            batch.End();
        }
        if (layers.HasFlag(RigLayers.Players)) Players(s, view);
        if (layers.HasFlag(RigLayers.Mask)) Mask(view);
        if (layers.HasFlag(RigLayers.Labels)) Labels(s, view, v, caption);
        Device.SetRenderTarget(null);
    }

    // CrimsonSky.Draw: the approved painting through ScarletBackdrop.fxc, screen space, with the Act's weights (settled),
    // the beat pulse (CrimsonMeter.Pulse from musicStart) and the strike impulse; Reduced drops both and the drift.
    private void Backdrop(RigScene s, in ScarletView view)
    {
        int w = view.Width, h = view.Height;
        var art = Assets.GetTexture("Backgrounds/ScarletSanctum");
        float age = view.Clock;
        bool reduced = view.Reduced;
        float scale = MathF.Max(w / (float)art.Width, h / (float)art.Height) * 1.10f;
        Vector2 size = new(w / (art.Width * scale), h / (art.Height * scale));
        Vector2 drift = reduced ? Vector2.Zero : new Vector2(MathF.Sin(view.ScreenPosition.X * .00035f + age * .0009f) * .012f,
            MathF.Sin(view.ScreenPosition.Y * .00040f + age * .0011f) * .008f);
        Vector2 origin = (Vector2.One - size) * .5f + drift;
        Vector4 weights = s.Phase switch { 0 => Vector4.UnitX, 1 => Vector4.UnitY, 2 => Vector4.UnitZ, _ => Vector4.UnitW };
        float beat = CrimsonMeter.Pulse(Math.Max(0, age - RigScene.MusicStart));
        var fx = Assets.GetEffect("ScarletBackdrop");
        fx.Parameters["uWorldViewProjection"]?.SetValue(Matrix.CreateOrthographicOffCenter(0, w, h, 0, -1, 1));
        fx.Parameters["crop"]?.SetValue(new Vector4(origin, size.X, size.Y));
        fx.Parameters["phaseWeights"]?.SetValue(weights);
        fx.Parameters["signal"]?.SetValue(new Vector4(1, reduced ? 0 : beat, reduced ? 0 : RigMirror.Impulse(s.Plans, age), reduced ? 1 : 0));
        fx.Parameters["clock"]?.SetValue(age / 60);
        quad[0] = new(new Vector3(0, 0, 0), Color.White, new(0, 0));
        quad[1] = new(new Vector3(0, h, 0), Color.White, new(0, 1));
        quad[2] = new(new Vector3(w, 0, 0), Color.White, new(1, 0));
        quad[3] = quad[2]; quad[4] = quad[1];
        quad[5] = new(new Vector3(w, h, 0), Color.White, new(1, 1));
        fx.CurrentTechnique.Passes["AutoloadPass"].Apply();
        Device.BlendState = BlendState.AlphaBlend; Device.DepthStencilState = DepthStencilState.None; Device.RasterizerState = RasterizerState.CullNone;
        Device.Textures[0] = art; Device.SamplerStates[0] = SamplerState.LinearClamp;
        Device.Textures[1] = Assets.GetTexture("Noise/TurbulentNoise"); Device.SamplerStates[1] = SamplerState.LinearWrap;
        Device.Textures[2] = Assets.GetTexture("Noise/WavyBlotchNoise"); Device.SamplerStates[2] = SamplerState.LinearWrap;
        Device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    // CrimsonGestureVisuals.DrawTrackingBeams, line for line, over the scene's plans (ForecastReady: LockAt = Born).
    private void TrackingBeams(RigScene s, in ScarletView view, float age)
    {
        CrimsonEnergy.Begin();
        strikes.Clear(); residues.Clear(); signatures.Clear(); sealsOver.Clear();
        foreach (var p in s.Plans)
        {
            if (!RigMirror.Alive(p, age)) continue;
            if (p.IsSignature) signatures.Add(p);
            int tail = p.IsRift ? CrimsonSpatialCuts.ResidueTicks : ScarletInkStroke.Applies(p) ? ScarletInkStroke.ResidueTicksOf(p) : 0;
            if ((!p.Aimed && !p.IsRift && !p.IsSignature) || age < p.Born || age >= p.End + tail) continue;
            if (p.Technique == CrimsonTechnique.SideBeams && age < p.End)
            {
                if (age < p.Fire) ScarletSorcery.CrossflowSeals(batch, p, age);
                else sealsOver.Add(p);
            }
            if (ScarletInkStroke.Underlies(p, age)) { residues.Add(p); continue; }
            if (ScarletInkStroke.Owns(p, age)) { strikes.Add(p); continue; }
            bool warning = age < p.Fire;
            int count = CrimsonTechniqueGeometry.Write(p, age, strokes, warning || p.IsRift);
            for (int i = 0; i < count; i++)
            {
                var stroke = strokes[i];
                Vector2 a = new(stroke.A.X, stroke.A.Y), delta = new(stroke.B.X - stroke.A.X, stroke.B.Y - stroke.A.Y);
                float length = delta.Length();
                if (length < .01f || stroke.Radius < .01f) continue;
                float opacity = warning ? .95f * CrimsonInvocation.Ease((age - p.Born) / 5) : 1;
                if (p.IsRift)
                {
                    ScarletSorcery.Tear(batch, stroke, age, p.Fire, p.End, Math.Clamp((age - p.Born) / (p.Fire - p.Born), 0, 1),
                        opacity, p.Phrase * 7 + p.Pulse * 3 + i);
                    continue;
                }
                CrimsonEnergy.Add(a, delta / length, length, stroke.Radius, age, p.Fire, p.End, opacity, CrimsonVisuals.Reduced, p.Born, fieldBeam: true);
            }
        }
        if (residues.Count > 0)
        {
            using var under = new Convergence.Client.Graphics.WorldGraphicsScope(batch);
            foreach (var residue in residues) ink.Draw(view, Assets, residue, CollectionsMarshal.AsSpan(signatures));
        }
        CrimsonEnergy.Draw(batch);
        if (strikes.Count > 0)
        {
            using var scope = new Convergence.Client.Graphics.WorldGraphicsScope(batch);
            foreach (var strike in strikes) ink.Draw(view, Assets, strike, CollectionsMarshal.AsSpan(signatures));
        }
        foreach (var p in sealsOver) ScarletSorcery.CrossflowSeals(batch, p, age);
    }

    // The ChoirRakes ribbon (ScarletMaterials.Strokes over Luminance's trail tessellator) is not reproduced offline: its
    // authoritative hit shapes stand in, and every frame that shows one says so.
    private void Standins(RigScene s, in ScarletView view, float age)
    {
        stand.Clear();
        foreach (var p in s.Plans)
            if (RigMirror.Alive(p, age) && !p.Aimed && !p.IsRift && !p.IsSignature && p.Technique != CrimsonTechnique.ClusterVolley
                && age >= p.Born && age < p.End + CrimsonRhythm.ResidueTicks) stand.Add(p);
        if (stand.Count == 0) return;
        using var scope = new Convergence.Client.Graphics.WorldGraphicsScope(batch);
        overlay.Draw(view, stand);
    }
    // A stand-in is on screen (a ChoirRakes note between its warning and the end of its residue).
    internal static bool StandinShown(RigScene s, float age) => s.Plans.Any(p => !p.Aimed && !p.IsRift && !p.IsSignature
        && p.Technique != CrimsonTechnique.ClusterVolley && age >= p.Born && age < p.End + CrimsonRhythm.ResidueTicks);
    // ScarletAtmosphere.Emit (F3) leaves embers only after a basic Act I / III strike (crossflow included); they live <= 59 ticks.
    internal static bool MetaballsShown(RigScene s, float age) => !CrimsonVisuals.Reduced
        && s.Plans.Any(p => !p.IsSignature && p.Source is 0 or 2 && age >= p.Fire && age < p.Fire + 60);

    private void Players(RigScene s, in ScarletView view)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, view.GameView);
        foreach (var at in s.Boxes)
        {
            var box = new Rectangle((int)(at.X - 10 - view.ScreenPosition.X), (int)(at.Y - 21 - view.ScreenPosition.Y), 20, 42);
            batch.Draw(Pixel, new Rectangle(box.X - 1, box.Y - 1, box.Width + 2, box.Height + 2), new Color(20, 14, 16));
            batch.Draw(Pixel, box, new Color(232, 200, 168));
        }
        batch.End();
    }

    // CrimsonVisuals.Overlay for a participant: physical-pixel black outside the field and a 2 px red edge.
    private void Mask(in ScarletView view)
    {
        if (Options.Mask == "none") return;
        var field = PreviewPlanner.Field;
        int w = view.Width, h = view.Height;
        Vector2 tl = view.ToTarget(new(field.Left, field.Top)), br = view.ToTarget(new(field.Right, field.Bottom));
        int left = Math.Clamp((int)MathF.Floor(tl.X), 0, w), right = Math.Clamp((int)MathF.Ceiling(br.X), 0, w);
        int top = Math.Clamp((int)MathF.Floor(tl.Y), 0, h), bottom = Math.Clamp((int)MathF.Ceiling(br.Y), 0, h);
        Color mask = Options.Mask == "dim" ? Color.Black * .72f : Color.Black;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        Fill(new(0, 0, w, top), mask); Fill(new(0, bottom, w, h - bottom), mask);
        Fill(new(0, top, left, Math.Max(0, bottom - top)), mask); Fill(new(right, top, w - right, Math.Max(0, bottom - top)), mask);
        Color edge = new(213, 53, 67);
        if (tl.X >= 0 && tl.X < w) Fill(new(left, top, 2, Math.Max(0, bottom - top)), edge);
        if (br.X > 0 && br.X <= w) Fill(new(Math.Max(0, right - 2), top, 2, Math.Max(0, bottom - top)), edge);
        if (tl.Y >= 0 && tl.Y < h) Fill(new(left, top, Math.Max(0, right - left), 2), edge);
        if (br.Y > 0 && br.Y <= h) Fill(new(left, Math.Max(0, bottom - 2), Math.Max(0, right - left), 2), edge);
        batch.End();
        void Fill(Rectangle r, Color c) { if (r.Width > 0 && r.Height > 0) batch.Draw(Pixel, r, c); }
    }

    private void Labels(RigScene s, in ScarletView view, in RigVariant v, string caption)
    {
        int h = view.Height;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        PreviewText.Draw(batch, Pixel, caption, new Vector2(12, 10), 2, new Color(236, 232, 236));
        var notes = new List<string> { "OFFLINE RENDER - NOT A PLAYTEST - IN-GAME ACCEPTANCE NOT_RUN" };
        if (s.Camera.Name == "B") notes.Add("REVIEW SCALE 0.65 - NOT AN IN-GAME ZOOM");
        if (StandinShown(s, view.Clock)) notes.Add("RAKES: HIT-SHAPE STAND-IN - RIBBON MATERIAL NOT REPRODUCED");
        if (MetaballsShown(s, view.Clock)) notes.Add("METABALL EMBERS OF THIS STRIKE NOT REPRODUCED");
        for (int i = 0; i < notes.Count; i++)
            PreviewText.Draw(batch, Pixel, notes[i], new Vector2(12, h - 22 - (notes.Count - 1 - i) * 16), 2, new Color(214, 206, 212));
        batch.End();
    }
}

// Writes webm (frames piped to ffmpeg as raw RGBA, VP9 crf 32, 60 fps) without staging PNGs on disk.
internal sealed class RigVideo : IDisposable
{
    private readonly Process process;
    private readonly Stream input;
    private readonly List<string> errors = new();
    internal readonly string Path;
    internal RigVideo(string ffmpeg, string path, int width, int height)
    {
        Path = path;
        var info = new ProcessStartInfo(ffmpeg) { RedirectStandardInput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var a in new[] { "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgba", "-s", $"{width}x{height}", "-framerate", "60",
                     "-i", "-", "-an", "-c:v", "libvpx-vp9", "-crf", "32", "-b:v", "0", "-pix_fmt", "yuv420p", "-row-mt", "1",
                     "-deadline", "good", "-cpu-used", "4", path })
            info.ArgumentList.Add(a);
        process = Process.Start(info) ?? throw new Exception("ffmpeg did not start");
        process.ErrorDataReceived += (_, e) => { if (e.Data is { Length: > 0 } line) lock (errors) errors.Add(line); };
        process.BeginErrorReadLine();
        input = process.StandardInput.BaseStream;
    }
    internal void Write(Color[] pixels) => input.Write(MemoryMarshal.AsBytes(pixels.AsSpan()));
    public void Dispose()
    {
        input.Close();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new Exception($"ffmpeg failed ({process.ExitCode}) for {Path}: {string.Join(" | ", errors)}");
        process.Dispose();
    }
}

// Graceful Ordeal decoded exactly like CrimsonAudio.PostSetupContent (NVorbis, 48 kHz stereo PCM16) and arranged by the
// production CrimsonMusicMixer (bar re-sequencing and 14 ms blends) for a scene's window. Written at mixer level;
// the encoder applies CrimsonInvocation.MusicGain and the user's music slider.
internal static class RigAudio
{
    internal static short[]? LoadSong(string root)
    {
        string path = Path.Combine(root, "Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg");
        if (!File.Exists(path)) return null;
        using var stream = new MemoryStream(File.ReadAllBytes(path));
        using var reader = new NVorbis.VorbisReader(stream, false);
        if (reader.Channels != 2 || reader.SampleRate != CrimsonMeter.SampleRate) throw new InvalidDataException("crimson.audio_format");
        var pcm = new short[reader.TotalSamples * 2];
        float[] samples = new float[8192]; int count, written = 0;
        while ((count = reader.ReadSamples(samples, 0, samples.Length)) > 0)
            for (int i = 0; i < count && written < pcm.Length; i++)
                pcm[written++] = (short)Math.Clamp((int)(samples[i] * 32767), short.MinValue, short.MaxValue);
        return pcm;
    }

    internal static object WriteBgm(string path, short[] song, RigScene s)
    {
        var mixer = new CrimsonMusicMixer(song);
        for (int stage = 1; stage <= s.Phase && stage < RigScene.StageBars.Length; stage++) mixer.SetStage(stage, RigScene.StageBars[stage]);
        long from = (long)(s.First - RigScene.MusicStart) * CrimsonMeter.SamplesPerTick;
        int frames = s.Frames * CrimsonMeter.SamplesPerTick;
        var buffer = new float[frames * 2];
        mixer.Render(from, buffer, frames);
        using (var file = File.Create(path))
        using (var w = new BinaryWriter(file))
        {
            int bytes = buffer.Length * 4;
            w.Write("RIFF"u8); w.Write(36 + bytes); w.Write("WAVE"u8);
            w.Write("fmt "u8); w.Write(16); w.Write((short)3); w.Write((short)2); w.Write(CrimsonMeter.SampleRate);
            w.Write(CrimsonMeter.SampleRate * 8); w.Write((short)8); w.Write((short)32);
            w.Write("data"u8); w.Write(bytes);
            foreach (float f in buffer) w.Write(f);
        }
        int firstBar = (int)(from / CrimsonMeter.BarSamples), lastBar = (int)((from + frames - 1) / CrimsonMeter.BarSamples);
        return new
        {
            file = Path.GetFileName(path), source = "Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg", sampleRate = CrimsonMeter.SampleRate,
            startTick = s.First, arrangementFrame = from, stage = s.Phase, stageBars = RigScene.StageBars.Take(s.Phase + 1),
            musicGain = CrimsonInvocation.MusicGain(1e9), // full level after the fade-in; the voice is musicGain * Main.musicVolume (linear)
            bars = Enumerable.Range(firstBar, lastBar - firstBar + 1).Select(b => new { bar = b, songBar = mixer.SongBarAt(b) }),
            assumption = "Acts II/III start their stage at arrangement bars 40 / 80; Act I at musicStart. Beats align exactly because phrases start on bar heads."
        };
    }
}

internal sealed class RigRun
{
    private readonly RigRenderer renderer;
    private readonly RigOptions o;
    private readonly string root, output;
    private readonly List<object> index = new();
    private readonly List<object> skipped = new();
    private int videos, pngs;

    internal RigRun(RigRenderer renderer, RigOptions options, string root, string output)
    { this.renderer = renderer; o = options; this.root = root; this.output = output; }

    internal int Execute()
    {
        var watch = Stopwatch.StartNew();
        var song = o.Audio && o.Gates != "only" ? RigAudio.LoadSong(root) : null;
        if (o.Gates != "only")
            foreach (string name in o.Scenes)
            {
                var def = RigSceneDef.Find(name);
                foreach (string cameraName in o.Cameras)
                {
                    var scene = RigScene.Build(def, RigCamera.Find(cameraName), o);
                    foreach (bool reduced in o.Reduced)
                        foreach (bool material in o.Material)
                            foreach (bool yield in o.Yield)
                            {
                                var variant = new RigVariant(reduced, material, o.Proposed, yield);
                                if ((material && !RigDriver.MaterialAvailable) || (o.Proposed && !RigDriver.MotionAvailable))
                                {
                                    skipped.Add(new { scene = name, camera = cameraName, variant = variant.Suffix, status = "not_run", reason = RigDriver.Pending });
                                    Console.WriteLine($"not_run {name} {cameraName}-{variant.Suffix}: {RigDriver.Pending}");
                                    continue;
                                }
                                Film(scene, variant, song);
                            }
                }
            }
        object? gates = null;
        if (o.Gates != "off")
        {
            gates = new RigGates(renderer, o, root, output).Run();
            File.WriteAllText(Path.Combine(output, "gates.json"), JsonSerializer.Serialize(gates, Json));
        }
        File.WriteAllText(Path.Combine(output, "index.json"), JsonSerializer.Serialize(new
        {
            note = "Offline render of the production rigs, forecasts, seals and ink in the in-game layer order. Not a playtest; in-game acceptance is not_run.",
            size = $"{o.Width}x{o.Height}", fps = 60, mask = o.Mask, context = o.Context,
            performer = RigProvenance.Performer, cameras = RigCamera.All.Select(c => new { c.Name, c.Zoom, c.Note }),
            notReproduced = new[] { "terrain, walls and lighting", "metaball embers of basic Act I/III strikes", "ChoirRakes ribbon (hit-shape stand-in)",
                "camera shake and cinematic camera", "RenderAge sub-tick blend (one frame = one tick)", "real player sprites (20 x 42 boxes)" },
            renders = Merge("renders", index), skipped = Merge("skipped", skipped),
            gates = gates is not null || File.Exists(Path.Combine(output, "gates.json")) ? "gates.json" : null,
            seconds = Math.Round(watch.Elapsed.TotalSeconds, 1)
        }, Json));
        Console.WriteLine($"{videos} videos, {pngs} PNGs under {output} in {watch.Elapsed.TotalSeconds:0}s. Offline render; not a playtest.");
        return RigGates.Failed ? 1 : 0;
    }

    // Several runs fill one output directory: entries of earlier runs stay unless this run rendered the same
    // scene / camera / variant again.
    private List<System.Text.Json.Nodes.JsonNode> Merge(string key, List<object> fresh)
    {
        var nodes = fresh.Select(x => JsonSerializer.SerializeToNode(x, Json)!).ToList();
        string Key(System.Text.Json.Nodes.JsonNode n) => $"{n["scene"]}|{n["camera"]}|{n["variant"]}";
        var now = nodes.Select(Key).ToHashSet();
        string path = Path.Combine(output, "index.json");
        if (!File.Exists(path)) return nodes;
        try
        {
            if (System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))?[key] is System.Text.Json.Nodes.JsonArray old)
                foreach (var entry in old)
                    if (entry is not null && !now.Contains(Key(entry))
                        && (key != "renders" || Directory.Exists(Path.Combine(output, entry["dir"]?.ToString() ?? "-"))))
                        nodes.Add(entry.DeepClone());
        }
        catch (JsonException) { }
        return nodes.OrderBy(Key).ToList();
    }

    internal static readonly JsonSerializerOptions Json = new() { WriteIndented = true, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };

    private void Film(RigScene s, RigVariant v, short[]? song)
    {
        string dir = Path.Combine(output, s.Def.Name, s.Camera.Name + "-" + v.Suffix);
        Directory.CreateDirectory(dir);
        var target = renderer.Frame(o.Width, o.Height);
        var pixels = new Color[o.Width * o.Height];
        var stills = StillTicks(s);
        bool writeStills = o.Stills == "all" || o.Stills == "auto" && s.Camera.Name == "A2";
        var state = new List<object>();
        var counts = new List<object>();
        using var video = o.Frames == "stream" ? new RigVideo(o.Ffmpeg, Path.Combine(dir, "video.webm"), o.Width, o.Height) : null;
        int number = 0;
        for (int tick = s.First; tick <= s.Last; tick++, number++)
        {
            RigHost.ResetCounts();
            var view = s.View(renderer.Device, o.Width, o.Height, tick, v.Reduced);
            renderer.Render(s, view, v, o.Labels ? RigLayers.Frame : RigLayers.Frame & ~RigLayers.Labels, target, Color.Black, Caption(s, v, tick));
            if (video is not null || o.Frames == "png") target.GetData(pixels);
            video?.Write(pixels);
            if (o.Frames == "png") { Save(target, Path.Combine(dir, $"f{number:0000}.png")); }
            if (writeStills && stills.TryGetValue(tick, out var label)) Save(target, Path.Combine(dir, "stills", label + ".png"));
            state.Add(State(s, tick, v));
            counts.Add(RigHost.Counts.ToDictionary(k => k.Key.Length == 0 ? "other" : k.Key, k => new { draws = k.Value.Draws, vertices = k.Value.Vertices }));
        }
        object? bgm = song is null ? null : RigAudio.WriteBgm(Path.Combine(dir, "bgm.wav"), song, s);
        var events = RigMirror.Sounds(s.Plans).Where(e => e.Tick >= s.First && e.Tick <= s.Last)
            .Select(e => new { tick = e.Tick, frame = e.Tick - s.First, cue = e.Cue, phrase = e.Phrase, pulse = e.Pulse, technique = e.Technique });
        File.WriteAllText(Path.Combine(dir, "state.json"), JsonSerializer.Serialize(new
        {
            note = "Offline render; not a playtest. One frame = one tick at 60 fps; fraction is always 0.",
            scene = s.Def.Name, camera = s.Camera.Name, cameraNote = s.Camera.Note, zoom = s.Camera.Zoom, variant = v.Suffix,
            motion = new { requested = v.Proposed ? "proposed" : "current", rendered = v.Proposed && RigDriver.MotionAvailable ? "proposed" : "current" },
            material = new { requested = v.Material, rendered = v.Material && RigDriver.MaterialAvailable },
            residueYield = v.Yield,
            musicStart = RigScene.MusicStart, phraseBar = s.PhraseBar, firstTick = s.First, lastTick = s.Last,
            player = new[] { s.Player.Center.X, s.Player.Center.Y }, boxes = s.Boxes.Select(b => new[] { b.X, b.Y }),
            focus = new[] { s.Focus.X, s.Focus.Y },
            apparition = new { species = s.Phase, at = new[] { s.Stage.X, s.Stage.Y }, height = RigScene.Heights[s.Phase], flipped = s.Flipped, why = s.FlipReason },
            vespera = new { at = new[] { RigScene.Conductor.X, RigScene.Conductor.Y }, facing = RigScene.VesperaFacing,
                why = "NPC.spriteDirection keeps its default -1 (NPC ctor / SetDefaults IL in tModLoader.dll); Convergence never writes it for the conductor" },
            plans = s.Plans.Select((p, i) => new
            {
                role = s.Roles[i], technique = p.Technique.ToString(), phrase = p.Phrase, pulse = p.Pulse, source = p.Source, step = p.Step,
                begin = p.Begin, born = p.Born, fire = p.Fire, end = p.End, residueEnd = p.End + ScarletGeometryOverlay.ResidueTicks(p),
                target = new[] { p.Target.X, p.Target.Y }, signature = p.IsSignature
            }),
            sounds = events,
            bgm,
            ticks = state,
            draws = counts,
            envelopes = "ticks[].attack: S1's notes, motion [offsetX, offsetY, turn] and body envelopes (Crown/Mantle: Heat, Ignite, Front, Drain, Send, Return, Snap, Engaged; "
                + "Choir: Heat, Ignite, Drain, Surge, Engaged and per arm [Lift, Send, Return, Tear, Burst]) computed as DrawEffigy computes them; "
                + "the rigs draw them when the variant has the material on (and the motion when proposed). " + RigDriver.Pending,
        }, RigRun.Json));
        if (video is not null) videos++;
        index.Add(new { scene = s.Def.Name, camera = s.Camera.Name, variant = v.Suffix, dir = Path.GetRelativePath(output, dir).Replace('\\', '/'),
            frames = s.Frames, video = video is null ? null : "video.webm", stills = writeStills ? stills.Values : null });
        Console.WriteLine($"{s.Def.Name} {s.Camera.Name}-{v.Suffix}: {s.Frames} frames{(video is null ? "" : " -> video.webm")}");
    }

    // The design's stills: the third note (Born+14, Fire, Fire+2, Fire+10, End+8) and the crossflow (Born+28, Fire, Fire+7,
    // End-4); a signature phrase's crossflow is the previous phrase's closer released on its downbeat.
    internal static Dictionary<int, string> StillTicks(RigScene s)
    {
        var result = new Dictionary<int, string>();
        var third = s.Third;
        void Add(int tick, string label) { if (tick >= s.First && tick <= s.Last) result.TryAdd(tick, label); }
        Add(third.Born + 14, "n3-born+14"); Add(third.Fire, "n3-fire"); Add(third.Fire + 2, "n3-fire+2");
        Add(third.Fire + 10, "n3-fire+10"); Add(third.End + 8, "n3-end+8");
        if (!s.HasFlow) return result;
        var flow = s.Flow;
        Add(flow.Born + 28, "crossflow-born+28"); Add(flow.Fire, "crossflow-fire"); Add(flow.Fire + 7, "crossflow-fire+7"); Add(flow.End - 4, "crossflow-end-4");
        return result;
    }

    private static string Caption(RigScene s, in RigVariant v, int tick)
    {
        var tags = new List<string> { $"{s.Def.Name} {s.Camera.Name} {(v.Reduced ? "REDUCED" : "NORMAL")}", $"T{tick - s.Phrase.FirstBorn:+0;-0;+0}" };
        for (int i = 0; i < s.Plans.Length; i++)
        {
            // Named by what it is: N1..N3 / S1..S4 for the notes in order, XF for a crossflow (the previous phrase's too).
            var p = s.Plans[i];
            string name = p.Technique == CrimsonTechnique.SideBeams ? "XF" : (p.IsSignature ? "S" : "N") + (Array.FindIndex(s.Notes, q => q.Pulse == p.Pulse) + 1);
            if (s.Roles[i] != "current" && p.Technique != CrimsonTechnique.SideBeams) continue;
            if (p.Born == tick) tags.Add("WARN " + name);
            if (p.Fire == tick) tags.Add("FIRE " + name);
        }
        if (!v.Yield) tags.Add("YIELD OFF");
        if (v.Material) tags.Add("MATERIAL ON");
        if (v.Proposed) tags.Add("PROPOSED MOTION");
        return string.Join("  ", tags);
    }

    private static object State(RigScene s, int tick, in RigVariant v)
    {
        var apparition = RigMirror.Signal(s.Plans, s.Phase, tick);
        var vespera = RigMirror.Signal(s.Plans, -1, tick);
        // S1's attack clock for the Act's body, computed as CrimsonRig.DrawEffigy computes it (whether or not the
        // rigs draw it yet): the notes, the approved motion and the named envelopes of the body material.
        Span<ScarletNote> notes = stackalloc ScarletNote[ScarletNotes.Capacity];
        int noted = RigMirror.Notes(s.Plans, s.Phase, tick, s.Flipped, notes);
        object? arms = null, motion = null, body;
        if (s.Phase == 2)
        {
            Span<CrimsonChoirCue> cues = stackalloc CrimsonChoirCue[16];
            int count = RigMirror.ChoirCues(s.Plans, tick, s.Flipped, cues);
            var list = new List<object>();
            for (int i = 0; i < 4; i++) { var arm = CrimsonChoirMotion.Arm(i, tick, apparition.Charge, apparition.Recoil, cues[..count]); list.Add(new[] { Round(arm.Power), Round(arm.Burst) }); }
            arms = list;
            var c = ScarletBodyMaterial.Choir(tick, notes[..noted], v.Reduced);
            body = new
            {
                heat = Round(c.Heat), ignite = Round(c.Ignite), drain = Round(c.Drain), surge = Round(c.Surge), engaged = Round(c.Engaged),
                limbs = Enumerable.Range(0, 4).Select(i => { var l = c.Limb(i); return new[] { Round(l.Lift), Round(l.Send), Round(l.Return), Round(l.Tear), Round(l.Burst) }; }).ToArray(),
            };
        }
        else
        {
            var m = s.Phase == 0 ? ScarletGestureMotion.Crown(tick, notes[..noted]) : ScarletGestureMotion.Mantle(tick, notes[..noted]);
            motion = new[] { Round(m.OffsetX), Round(m.OffsetY), Round(m.Turn) };
            var b = ScarletBodyMaterial.Apparition(tick, notes[..noted], m, s.Flipped, v.Reduced);
            body = new
            {
                heat = Round(b.Heat), ignite = Round(b.Ignite), front = Round(b.Front), drain = Round(b.Drain), send = Round(b.Send),
                @return = Round(b.Return), snap = Round(b.Snap), engaged = Round(b.Engaged),
            };
        }
        return new
        {
            tick, fraction = 0, rel = tick - s.Phrase.FirstBorn,
            apparition = new[] { Round(apparition.Charge), Round(apparition.Recoil) },
            vespera = new[] { Round(vespera.Charge), Round(vespera.Recoil) },
            arms,
            attack = new { notes = noted, motion, body },
            backdrop = new[] { Round(v.Reduced ? 0 : CrimsonMeter.Pulse(Math.Max(0, tick - RigScene.MusicStart))), Round(v.Reduced ? 0 : RigMirror.Impulse(s.Plans, tick)) },
            notes = s.Showing(tick).ToArray(),
            // What this frame lacks against the game (labelled on the frame).
            standin = RigRenderer.StandinShown(s, tick), embersMissing = RigRenderer.MetaballsShown(s, tick),
        };
    }

    internal static float Round(float value) => MathF.Round(value, 5);

    private void Save(RenderTarget2D target, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = File.Create(path);
        target.SaveAsPng(file, target.Width, target.Height);
        pngs++;
    }
}
