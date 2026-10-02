// Gates of the Scarlet attack-expression design (§4.3) over the rig harness, plus the harness's own self-checks.
// A gate whose input does not exist yet (the body material of S2/S3, the motion of S1) is still computed as far as
// it can be and reported "not_run" with its measurements and the reason; "vacuous" marks a pass that cannot fail
// before the material exists. Status values: pass | fail | not_run | baseline_written. Offline review only.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Convergence.Client.Encounters.CrimsonFoundry;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed record RigGate(string Id, string Title, string Status, bool Vacuous, object? Measured, string Note);

internal sealed class RigGates
{
    internal static bool Failed;
    private readonly RigRenderer r;
    private readonly RigOptions o;
    private readonly string root, output;
    private readonly List<RigGate> gates = new();
    private readonly Dictionary<string, RigScene> scenes = new();
    private RenderTarget2D? world;

    // Design §2.7: draws per frame of the NEW rigs (Normal / Reduced). Today's counts are the design's "今の draw".
    private static readonly Dictionary<string, (int Normal, int Reduced, int TodayNormalMin, int TodayNormalMax, int TodayReduced)> Budget = new()
    {
        ["crown"] = (18, 11, 32, 32, 11), ["mantle"] = (9, 8, 31, 31, 11), ["choir"] = (52, 23, 65, 69, 23),
    };
    // Shaders the attack expression must not change (design §2.0.6, G11). .fx hashed with LF line ends.
    private static readonly Dictionary<string, string> Pinned = new()
    {
        ["ScarletInk.fx"] = "e801511955804f740b45cb1472f3005f4ab48e12ce19fc2aa1f8e180bc64a227",
        ["ScarletInk.fxc"] = "689a5d7bd1136d8104d8a094c3f317be4578498b2b882c60c44c51a87b1020f2",
        ["PortalBeam.fx"] = "645ffd856e4ee9cdfc7d663eca01f626aab8e3b883a726876559f7d73eaec1d8",
        ["PortalBeam.fxc"] = "cf2ffeb0e7441e6b0c0597f2f9cfd87c77a58af87bc51e4f59b333e73a28d699",
        ["RaidEnergy.fx"] = "924c0c25abdd983ed7e94f4e2880a015f4afa07fcf5ac4c16425f7a1d6c35fb8",
        ["RaidEnergy.fxc"] = "299821ce56aca68b658a20ec7fd126d575d13022b74bf932db5968156062e9c7",
        ["CrimsonReactor.fx"] = "e016c33b62286a5781a530d027a7a6b8ae99999bceccd52bbb7cd54933b90607",
        ["CrimsonReactor.fxc"] = "8df1ac1bdb9dcb1b46eed0490ed4c5ce25272dc7f6a7ef19563880508005180f",
        ["ScarletBackdrop.fx"] = "ab810c34dda508df2d8f23b3bba37be316937803500811b7778599d87de63f16",
        ["ScarletBackdrop.fxc"] = "8c6a64ddf9ad29cd2e2537aa19013587120c7340c6a1577c908726e7f1eb2323",
        ["ScarletSorcery.fx"] = "4ff0ae497a1aba6edd7e16440e78fb5247e7a35089632608ce09d9542cd0ace4",
        ["ScarletSorcery.fxc"] = "6f6a3492f7c29ebb8250ac19aeba2705950a5f5c38d386bc6a53146a681738aa",
        ["ScarletRibbon.fx"] = "55250e8e4894fb4d12bee34e6d546914606d71cca0e75c1c556d5f45188d83a6",
        ["ScarletRibbon.fxc"] = "53a2fff40e35b5da56550eec515cacf234d83def0822524d61663d5bac018d4b",
    };
    private static readonly int[] Lags = { 0, 4, 8, 12, 16, 21 };
    private const float RangeDilate = 12;

    internal RigGates(RigRenderer renderer, RigOptions options, string root, string output)
    { r = renderer; o = options; this.root = root; this.output = output; }

    private RigScene Scene(string name, string camera)
    {
        string key = name + "/" + camera;
        if (!scenes.TryGetValue(key, out var scene))
        {
            var options = new RigOptions { Context = true, Flip = o.Flip };
            scenes[key] = scene = RigScene.Build(RigSceneDef.Find(name), RigCamera.Find(camera), options);
        }
        return scene;
    }
    private static readonly string[] Signatures = { "act1-signature", "act2-signature", "act3-signature" };
    private static readonly string[] Basics = { "act1-basic", "act2-basic", "act3-basic" };
    private static readonly RigVariant Normal = new(false, false, false, true);

    internal object Run()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Step("G0", () => G0()); Step("G1+G2", () => G1G2()); Step("G3", () => G3()); Step("G4", () => G4()); Step("G5", () => G5());
        Step("G6", () => G6()); Step("G7", () => G7()); Step("G8", () => G8()); Step("G9", () => G9()); Step("G10", () => G10());
        Step("G11", () => G11()); Step("G12", () => G12()); Step("G13", () => G13()); Step("G14", () => G14());
        var self = Self();
        world?.Dispose();
        foreach (var g in gates) Console.WriteLine($"{g.Id,-5} {g.Status,-17}{(g.Vacuous ? " (vacuous)" : "")} {g.Title}");
        Failed = gates.Any(g => g.Status == "fail") || self.Any(g => g.Status == "fail");
        return new
        {
            note = "Gates of the Scarlet attack-expression design over the offline rig harness. Not a playtest; in-game acceptance is not_run.",
            material = RigDriver.MaterialAvailable, motion = RigDriver.MotionAvailable, pending = RigDriver.MaterialAvailable && RigDriver.MotionAvailable ? null : RigDriver.Pending,
            gates, self, seconds = Math.Round(watch.Elapsed.TotalSeconds, 1)
        };
        void Step(string id, Action run)
        {
            var t = watch.Elapsed;
            run();
            Console.WriteLine($"  {id} {(watch.Elapsed - t).TotalSeconds:0.0}s");
        }
    }

    private void Add(string id, string title, string status, object? measured, string note, bool vacuous = false)
        => gates.Add(new RigGate(id, title, status, vacuous, measured, note));
    private static string Status(bool ok) => ok ? "pass" : "fail";

    // ---- G0 ---------------------------------------------------------------------------------------------------
    private void G0() => Add("G0", "motion golden (C# vs motion.js / rigs.js, 1e-4)", "not_run", null,
        "S1 owns it as a domain test (ScarletGestureMotionTests against Data/scarlet-motion-golden.json); the harness has no motion of its own.");

    // ---- G1 / G2: decoration stays inside the body range and out of every safe zone -----------------------------
    private void G1G2()
    {
        var range = new List<object>(); var zones = new List<object>();
        int outside = 0, inZones = 0;
        foreach (string name in Signatures)
        {
            var s = Scene(name, "C");
            var third = s.Phrase.Plans[2];
            foreach (int tick in new[] { third.Born + 14, third.Fire, third.Fire + 2, third.Fire + 10 })
            {
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                var body = BodyRange(s, view, Normal);
                var diff = Diff(s, view, RigLayers.Npc, Normal, Normal with { Material = true }, 2);
                var safe = SafeZones(s, view, tick);
                int bodyPixels = body.Count(b => b), diffPixels = 0, diffOutside = 0, diffInZone = 0, zonePixels = 0, npcInZone = 0;
                var npc = Pixels(s, view, Normal, RigLayers.Npc, Color.Transparent);
                for (int i = 0; i < body.Length; i++)
                {
                    if (safe[i] && !body[i]) zonePixels++;
                    if (safe[i] && !body[i] && npc[i].A > 2) npcInZone++;
                    if (!diff[i]) continue;
                    diffPixels++;
                    if (!body[i]) diffOutside++;
                    if (safe[i] && !body[i]) diffInZone++;
                }
                outside += diffOutside; inZones += diffInZone;
                range.Add(new { scene = name, tick, rel = tick - third.Fire, bodyRangePixels = bodyPixels, diffPixels, diffOutsideRange = diffOutside });
                zones.Add(new { scene = name, tick, rel = tick - third.Fire, safeOutsideBody = zonePixels, diffInSafeZones = diffInZone,
                    todayNpcPixelsInSafeZones = npcInZone });
            }
        }
        bool vacuous = !RigDriver.MaterialAvailable;
        Add("G1", "decoration (material on - off > 2/255) inside the body range (lags 0..21, +12 px)", vacuous ? "not_run" : Status(outside == 0), range,
            vacuous ? "No body material yet (S2/S3): on and off are the same frame. The body range (skinned mesh alpha > .05 over six past poses, dilated 12 px at zoom 1) is measured so the mask is exercised." : "", vacuous);
        Add("G2", "no decoration in safe zones outside the body (curtain corridors, hands gaps, staff gaps, central 94 px)", vacuous ? "not_run" : Status(inZones == 0), zones,
            "Safe = inside the field, outside every displayed signature footprint, plus the central 94 px band. todayNpcPixelsInSafeZones is the CURRENT rigs' aura/ribbon coverage there (baseline, not a gate)." + (vacuous ? " No body material yet." : ""), vacuous);
    }

    // ---- G3: brightness of the decoration against the forecast band and the live ink --------------------------
    private void G3()
    {
        var rows = new List<object>();
        foreach (string name in Signatures)
        {
            var s = Scene(name, "A2");
            var third = s.Phrase.Plans[2];
            var warnView = s.View(r.Device, o.Width, o.Height, third.Fire - 8, false);
            var warn = Pixels(s, warnView, Normal, RigLayers.Backdrop | RigLayers.Field, Color.Black);
            var band = Capsules(s, warnView, third, third.Fire, true);
            var liveView = s.View(r.Device, o.Width, o.Height, third.Fire + 4, false);
            var live = Pixels(s, liveView, Normal, RigLayers.Backdrop | RigLayers.Field, Color.Black);
            var strike = Capsules(s, liveView, third, third.Fire + 4, false);
            float bandMedian = Percentile(Luma(warn, band), 50), inkP995 = Percentile(Luma(live, strike), 99.5f);
            rows.Add(new { scene = name, forecastBandMedian = bandMedian, inkLiveP995 = inkP995,
                limitOutsideBody = Math.Min(bandMedian, inkP995 * .5f), limitInsideBody = inkP995 * .8f, decorationP995 = (float?)null });
        }
        Add("G3", "decoration luminance: outside body <= forecast band median and <= .5 x ink live p99.5; inside <= .8 x", "not_run", rows,
            "Reference luminances measured on today's frames (camera A2, third note: forecast at Fire-8, live at Fire+4); the decoration they bound arrives with S2/S3.", true);
    }

    // ---- G4: the PostDrawTiles layer is byte-identical with the material on or off -----------------------------
    private void G4()
    {
        int frames = 0, different = 0;
        foreach (string name in Signatures.Concat(Basics))
        {
            var s = Scene(name, "A2");
            foreach (var p in s.Phrase.Plans)
                foreach (int tick in new[] { p.Born + 10, p.Fire + 4, p.End + 4 })
                {
                    var view = s.View(r.Device, o.Width, o.Height, tick, false);
                    var off = Pixels(s, view, Normal, RigLayers.Field, Color.Transparent);
                    var on = Pixels(s, view, Normal with { Material = true }, RigLayers.Field, Color.Transparent);
                    frames++;
                    if (!off.AsSpan().SequenceEqual(on)) different++;
                }
        }
        Add("G4", "forecasts, seals and ink (PostDrawTiles) byte-identical with the material on / off", Status(different == 0),
            new { frames, different }, RigDriver.MaterialAvailable ? "" : "The material switch does not exist yet; this proves the comparison path.", !RigDriver.MaterialAvailable);
    }

    // ---- G5: local contrast of the forecast band keeps >= 90% -------------------------------------------------
    private void G5()
    {
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Signatures)
        {
            var s = Scene(name, "A2");
            var third = s.Phrase.Plans[2];
            var view = s.View(r.Device, o.Width, o.Height, third.Fire - 8, false);
            var band = Capsules(s, view, third, third.Fire, true);
            var ring = Ring(band, view.Width, view.Height, 24);
            float Contrast(RigVariant v)
            {
                var px = Pixels(s, view, v, RigLayers.Frame & ~RigLayers.Labels, Color.Black);
                return MathF.Abs(Mean(Luma(px, band)) - Mean(Luma(px, ring)));
            }
            float off = Contrast(Normal), on = Contrast(Normal with { Material = true });
            float ratio = off > 1e-3f ? on / off : 1;
            ok &= ratio >= .9f;
            rows.Add(new { scene = name, tick = third.Fire - 8, contrastOff = off, contrastOn = on, ratio });
        }
        Add("G5", "forecast band local contrast with decoration >= 90% of without", RigDriver.MaterialAvailable ? Status(ok) : "not_run", rows,
            "Contrast = |mean luma inside the next note's forecast capsules - mean luma of a 24 px ring around them| on the full frame." +
            (RigDriver.MaterialAvailable ? "" : " No decoration yet: the ratio is 1 by construction."), !RigDriver.MaterialAvailable);
    }

    // ---- G6: timing ------------------------------------------------------------------------------------------
    private void G6()
    {
        var soundRows = new List<object>(); bool soundOk = true;
        var inkRows = new List<object>(); bool inkOk = true;
        var glowRows = new List<object>();
        foreach (string name in Signatures.Concat(Basics))
        {
            var s = Scene(name, "A2");
            var sounds = RigMirror.Sounds(s.Plans);
            foreach (var e in sounds)
            {
                var p = Array.Find(s.Plans, q => q.Phrase == e.Phrase && q.Pulse == e.Pulse);
                bool impact = e.Cue is "Impact" or "CrossflowRelease";
                bool ok = e.Tick == (impact ? p.Fire : p.Born);
                soundOk &= ok;
                if (s.Roles[Array.IndexOf(s.Plans, p)] == "current") soundRows.Add(new { scene = name, e.Cue, pulse = e.Pulse, rel = e.Tick - (impact ? p.Fire : p.Born), ok });
            }
            foreach (var p in s.Phrase.Plans)
            {
                if (!ScarletInkStroke.Applies(p)) continue;
                int first = int.MinValue;
                // Over the whole field (a beam that grows in from a wall reaches a camera later than it starts).
                for (int tick = p.Fire - 2; tick <= p.Fire + 3 && first == int.MinValue; tick++)
                {
                    var view = WorldView(tick, out var target);
                    if (InkOnly(s, view, p, target).Any(c => c.A > 3)) first = tick;
                }
                bool ok = first >= p.Fire - 1 && first <= p.Fire + 1;
                inkOk &= ok;
                inkRows.Add(new { scene = name, technique = p.Technique.ToString(), pulse = p.Pulse, firstInkPixel = first == int.MinValue ? (int?)null : first - p.Fire, ok });
            }
        }
        foreach (string name in Signatures)
        {
            var s = Scene(name, "C");
            var third = s.Phrase.Plans[2];
            float best = -1; int at = 0;
            for (int tick = third.Born; tick < third.End; tick++)
            {
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                float glow = Mean(Luma(Pixels(s, view, Normal, RigLayers.Apparition, Color.Black), null));
                if (glow > best) { best = glow; at = tick; }
            }
            glowRows.Add(new { scene = name, todayBodyGlowPeak = at - third.Fire, window = "[Fire, Fire+2] for the new material" });
        }
        Add("G6", "timing: sounds on Born/Fire, first ink pixel = Fire +/- 1 (Ignite/Heat/Send/body peak need S1/S2)", Status(soundOk && inkOk),
            new { sounds = soundRows, ink = inkRows, bodyGlow = glowRows, envelopes = "not_run: " + RigDriver.Pending },
            "Sound ticks mirror CrimsonGestureVisuals.Cue (one voice per phrase/tick/cue, no cue for a curtain with nothing to burn). The body glow peak is TODAY's (Signal recoil), reported for comparison only.");
    }

    // ---- G7: Reduced Effects keeps every vertex -----------------------------------------------------------------
    private void G7()
    {
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Signatures.Concat(Basics))
        {
            var s = Scene(name, "C");
            var third = s.Phrase.Plans[2];
            foreach (int tick in new[] { third.Born + 6, third.Fire, third.Fire + 3, third.End })
            {
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                var normal = Mesh(s, view, Normal); var reduced = Mesh(s, view.At(tick) with { Reduced = true }, Normal with { Reduced = true });
                float max = normal.Count == reduced.Count && normal.Count > 0 ? normal.Zip(reduced, (a, b) => Vector2.Distance(a, b)).Max() : float.PositiveInfinity;
                ok &= max <= 1e-3f;
                rows.Add(new { scene = name, rel = tick - third.Fire, vertices = normal.Count, reducedVertices = reduced.Count, maxShift = float.IsFinite(max) ? max : -1 });
            }
        }
        Add("G7", "Reduced: skinned body vertices identical to Normal (diff energy <= 40% and no Fire-born particles need S2/S3)", Status(ok),
            new { vertices = rows, energy = "not_run: " + RigDriver.Pending, particles = "not_run: " + RigDriver.Pending },
            "Captured from every AutoloadPass draw of ScarletApparitions / ScarletChoir (the skinned mesh) through the device shim.");
    }

    // ---- G8: with no notes the bodies are today's picture --------------------------------------------------------
    private void G8()
    {
        string dir = BaselineDir("g8");
        bool compare = Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.rgba.gz").Any();
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Signatures)
        {
            var s = Scene(name, "C");
            var empty = Idle(s);
            foreach (int offset in new[] { 0, 37, 91 })
            {
                int tick = s.Phrase.FirstBorn - 400 + offset; // the rest a phrase leaves: no gesture alive
                var view = empty.View(r.Device, o.Width, o.Height, tick, false);
                var px = Pixels(empty, view, Normal, RigLayers.Apparition, Color.Transparent);
                rows.Add(Check($"{name}-apparition-{offset}", px, view.Width, view.Height, name.StartsWith("act2") ? -1 : 1));
            }
        }
        {
            var s = Idle(Scene("act1-signature", "V"));
            foreach (int offset in new[] { 0, 53 })
            {
                int tick = s.Phrase.FirstBorn - 400 + offset;
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                rows.Add(Check($"vespera-{offset}", Pixels(s, view, Normal, RigLayers.Vespera, Color.Transparent), view.Width, view.Height, 0));
                rows.Add(Check($"companion-{offset}", Companion(view, tick), view.Width, view.Height, 0));
            }
        }
        Add("G8", "no notes: Crown/Choir within 1/255, Vespera and companion identical, Mantle by eye", compare ? Status(ok) : "baseline_written", rows,
            compare ? "Compared with the baseline in " + Path.GetRelativePath(output, dir) : "Today's idle frames written to " + dir + "; later slices compare against them.");

        object Check(string label, Color[] px, int w, int h, int tolerance)
        {
            string file = Path.Combine(dir, label + ".rgba.gz");
            if (!compare) { Directory.CreateDirectory(dir); SaveRaw(px, w, h, file); return new { label, written = true }; }
            if (!File.Exists(file)) { ok = false; return new { label, missing = true }; }
            var basePx = LoadRaw(file, w, h);
            int max = 0;
            for (int i = 0; i < px.Length; i++)
                max = Math.Max(max, Math.Max(Math.Max(Math.Abs(px[i].R - basePx[i].R), Math.Abs(px[i].G - basePx[i].G)), Math.Max(Math.Abs(px[i].B - basePx[i].B), Math.Abs(px[i].A - basePx[i].A))));
            bool pass = tolerance < 0 || max <= tolerance;
            ok &= pass;
            return new { label, maxChannelDiff = max, tolerance = tolerance < 0 ? "visual" : tolerance.ToString(), pass };
        }
    }

    // ---- G9: determinism -------------------------------------------------------------------------------------
    private void G9()
    {
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Signatures)
        {
            var s = Scene(name, "C");
            var second = s.Phrase.Plans[1];
            var ticks = Enumerable.Range(second.Fire - 5, 11).ToArray();
            var sequential = ticks.Select(t => Hash(s, t)).ToArray();
            var shuffled = ticks.Select((t, i) => (t, i)).OrderBy(x => (x.i * 7919) % 11).ToArray();
            var jumped = new string[ticks.Length];
            foreach (var (t, i) in shuffled) jumped[i] = Hash(s, t);
            bool same = sequential.SequenceEqual(jumped);
            // +7: the inputs the rigs read from the plans (Signal, the choir arms, the sound ticks) shift exactly with them.
            var shifted = s.Plans.Select(p => p with { Begin = p.Begin + 7, Born = p.Born + 7, Fire = p.Fire + 7, End = p.End + 7, FirstFire = p.FirstFire + 7, LastEnd = p.LastEnd + 7 }).ToArray();
            float worst = 0;
            for (int t = s.First; t <= s.Last; t += 3)
            {
                var a = RigMirror.Signal(s.Plans, s.Phase, t); var b = RigMirror.Signal(shifted, s.Phase, t + 7);
                worst = Math.Max(worst, Math.Max(Math.Abs(a.Charge - b.Charge), Math.Abs(a.Recoil - b.Recoil)));
                if (s.Phase == 2) worst = Math.Max(worst, ArmGap(s.Plans, shifted, t, a));
            }
            bool soundsShift = RigMirror.Sounds(s.Plans).Select(e => e.Tick + 7).SequenceEqual(RigMirror.Sounds(shifted).Select(e => e.Tick));
            ok &= same && worst <= 1e-5f && soundsShift;
            rows.Add(new { scene = name, frames = ticks.Length, sequentialEqualsJump = same, shiftWorstInputDelta = worst, soundsShift });
        }
        Add("G9", "determinism: sequential = jump render; plans +7 ticks shift every attack input by 7", Status(ok), rows,
            "Pixels compared by SHA-256 of the full frame (labels off). The idle oscillations read the absolute clock by design (today's rigs); the attack inputs read only the plans.");

        float ArmGap(CrimsonGesturePlan[] plans, CrimsonGesturePlan[] shifted, int t, (float Charge, float Recoil) signal)
        {
            Span<CrimsonChoirCue> a = stackalloc CrimsonChoirCue[16];
            Span<CrimsonChoirCue> b = stackalloc CrimsonChoirCue[16];
            int na = RigMirror.ChoirCues(plans, t, a), nb = RigMirror.ChoirCues(shifted, t + 7, b);
            float gap = 0;
            for (int arm = 0; arm < 4; arm++)
            {
                // Same absolute clock for the idle sway, shifted cues: only the attack terms may differ, and they must not.
                var x = CrimsonChoirMotion.Arm(arm, t, signal.Charge, signal.Recoil, a[..na]);
                var shiftedCues = new CrimsonChoirCue[nb];
                for (int i = 0; i < nb; i++) shiftedCues[i] = b[i] with { Born = b[i].Born - 7, Fire = b[i].Fire - 7, End = b[i].End - 7 };
                var y = CrimsonChoirMotion.Arm(arm, t, signal.Charge, signal.Recoil, shiftedCues);
                gap = Math.Max(gap, Math.Max(Math.Abs(x.Power - y.Power), Math.Abs(x.Burst - y.Burst)));
            }
            return gap;
        }
    }

    // ---- G10: how much of the body the in-game cameras show ----------------------------------------------------
    private void G10()
    {
        var rows = new List<object>();
        var field = PreviewPlanner.Field;
        const int margin = WorldMargin;
        foreach (string name in Signatures.Concat(Basics))
        {
            var ground = Scene(name, "A"); var air = Scene(name, "A2");
            var fractions = new Dictionary<string, List<float>> { ["A"] = new(), ["A2"] = new() };
            for (int tick = ground.Phrase.FirstBorn; tick <= ground.Phrase.Plans.Max(p => p.End); tick += 8)
            {
                var view = WorldView(tick, out var world);
                int w = view.Width, h = view.Height;
                RigHost.PassFilter = BodyOnly;
                try { r.Render(ground, view, Normal, RigLayers.Apparition, world, Color.Transparent); }
                finally { RigHost.PassFilter = null; }
                var px = RigRenderer.Read(world);
                foreach (var (camera, scene) in new[] { ("A", ground), ("A2", air) })
                {
                    var c = scene.Player.Center;
                    int x0 = (int)(c.X - o.Width / 2f - (field.Left - margin)), y0 = (int)(c.Y - o.Height / 2f - (field.Top - margin));
                    int total = 0, seen = 0;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            if (px[y * w + x].A <= 12) continue;
                            total++;
                            if (x >= x0 && x < x0 + o.Width && y >= y0 && y < y0 + o.Height) seen++;
                        }
                    fractions[camera].Add(total == 0 ? 0 : seen / (float)total);
                }
            }
            rows.Add(new
            {
                scene = name,
                A = new { mean = fractions["A"].Average(), min = fractions["A"].Min(), max = fractions["A"].Max() },
                A2 = new { mean = fractions["A2"].Average(), min = fractions["A2"].Min(), max = fractions["A2"].Max() },
                screenTopA = ground.Player.Center.Y - o.Height / 2f - field.Bottom
            });
        }
        Add("G10", "share of the body (skinned mesh alpha > .05) inside the camera A / A2 screens", "pass", rows,
            "A measurement for the owner's question (design §6 Q1), not a threshold. screenTopA is the top of camera A's screen relative to the floor (px).");
    }

    // ---- G11: the approved ink is untouched --------------------------------------------------------------------
    private void G11()
    {
        var hashes = new List<object>(); bool hashOk = true;
        foreach (var (file, expected) in Pinned)
        {
            var bytes = File.ReadAllBytes(Path.Combine(root, "Assets/AutoloadedEffects/Shaders", file));
            if (file.EndsWith(".fx")) bytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n"));
            string actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            hashOk &= actual == expected;
            hashes.Add(new { file, ok = actual == expected });
        }
        string dir = BaselineDir("g11");
        bool compare = Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.rgba.gz").Any();
        var frames = new List<object>(); bool frameOk = true;
        var s = Scene("act1-basic", "A2");
        foreach (var p in s.Phrase.Plans.Where(q => ScarletInkStroke.Applies(q)))
            foreach (int rel in new[] { 2, 10, p.End - p.Fire + 5 })
            {
                int tick = p.Fire + rel;
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                var px = InkOnly(s, view, p, r.Frame(view.Width, view.Height));
                string label = $"act1-basic-n{p.Pulse + 1}-fire+{rel}", file = Path.Combine(dir, label + ".rgba.gz");
                if (!compare) { Directory.CreateDirectory(dir); SaveRaw(px, view.Width, view.Height, file); frames.Add(new { label, written = true }); continue; }
                bool same = File.Exists(file) && LoadRaw(file, view.Width, view.Height).AsSpan().SequenceEqual(px);
                frameOk &= same;
                frames.Add(new { label, identical = same });
            }
        string status = !hashOk ? "fail" : compare ? Status(frameOk) : "baseline_written";
        Add("G11", "ink regression: ScarletInk (and the other approved field shaders) unchanged; Act I basic ink frames identical", status,
            new { hashes, frames }, compare ? "" : "Pinned hashes checked; today's Act I basic ink frames written to " + dir + ".");
    }

    // ---- G12: draw / vertex budget -----------------------------------------------------------------------------
    private void G12()
    {
        var rows = new List<object>(); bool matchesDesign = true;
        foreach (string name in Signatures)
        {
            var s = Scene(name, "C");
            var third = s.Phrase.Plans[2];
            string tag = s.Phase switch { 0 => "crown", 1 => "mantle", _ => "choir" };
            var budget = Budget[tag];
            foreach (bool reduced in new[] { false, true })
            {
                int draws = 0, vertices = 0, minDraws = int.MaxValue, maxDraws = 0;
                foreach (int tick in new[] { third.Born + 6, third.Fire + 2, third.End + 4, s.Phrase.FirstBorn - 40 })
                {
                    var view = s.View(r.Device, o.Width, o.Height, tick, reduced);
                    RigHost.ResetCounts();
                    Pixels(s, view, Normal with { Reduced = reduced }, RigLayers.Apparition, Color.Transparent);
                    var c = RigHost.Counts.TryGetValue(tag, out var found) ? found : default;
                    draws = Math.Max(draws, c.Draws); vertices = Math.Max(vertices, c.Vertices);
                    minDraws = Math.Min(minDraws, c.Draws); maxDraws = Math.Max(maxDraws, c.Draws);
                }
                bool today = reduced ? maxDraws == budget.TodayReduced : minDraws >= budget.TodayNormalMin && maxDraws <= budget.TodayNormalMax;
                matchesDesign &= today;
                rows.Add(new { body = tag, reduced, todayDraws = new[] { minDraws, maxDraws }, todayVertices = vertices,
                    budgetDraws = reduced ? budget.Reduced : budget.Normal, todayMatchesDesignCount = today });
            }
        }
        Add("G12", "draws / vertices per body within §2.7", "not_run", rows,
            "The §2.7 budget applies to the S2/S3 rigs. Today's counts (through the device shim) " + (matchesDesign ? "match" : "DO NOT match") + " the design's '今の draw' (Crown 32/11, Mantle 31/11, Choir 65-69/23).");
        if (!matchesDesign) gates[^1] = gates[^1] with { Status = "fail", Note = gates[^1].Note + " The counter disagrees with the design's numbers: check the shim before trusting G12." };
    }

    // ---- G13 / G14: the existing preview contracts on the new moves --------------------------------------------
    private void G13()
    {
        var rows = new List<object>(); int wrong = 0;
        bool saved = ScarletResidueYield.Enabled;
        foreach (bool enabled in new[] { true, false })
        {
            ScarletResidueYield.Enabled = enabled;
            foreach (string name in Signatures.Append("act1-signature-trio"))
            {
                var s = Scene(name, "A2");
                var result = PreviewContract.ResidueYield(r.Device, new ScarletInkStroke(), r.Assets, s.Phrase);
                wrong += result.Wrong;
                rows.Add(new { scene = name, yield = enabled, result.Strokes, result.Holding, result.LitPixels, result.Wrong });
            }
        }
        ScarletResidueYield.Enabled = saved;
        Add("G13", "signature residue yield: safe-next strokes dry by End+6, held strokes keep the full residue", Status(wrong == 0), rows,
            "tools/fixtures/ScarletPreviewContract.ResidueYield (production ScarletInkStroke, 1:1 over the field).");
    }

    private void G14()
    {
        var rows = new List<object>(); int wrong = 0;
        using var overlay = new ScarletGeometryOverlay(r.Device);
        foreach (string name in Signatures.Concat(Basics).Append("act1-signature-trio"))
        {
            var s = Scene(name, "A2");
            var plans = s.Phrase.Plans;
            var result = new PreviewContract.Result();
            foreach (int tick in new[] { plans[1].Born + 10, plans[1].Fire + 5, plans[2].Fire + 2, plans[4].Fire + 14, plans[3].End + 6 })
                result += PreviewContract.Run(r.Device, overlay, s.Phrase, tick);
            wrong += result.Wrong;
            rows.Add(new { scene = name, result.Checked, result.Wrong });
        }
        Add("G14", "overlay contract (rasterised capsules = authority) on the scenes' phrases", Status(wrong == 0), rows,
            "tools/fixtures/ScarletPreviewContract.Run.");
    }

    // ---- self-checks ---------------------------------------------------------------------------------------------
    private List<RigGate> Self()
    {
        var list = new List<RigGate>();
        list.Add(new("viewport", "every shimmed draw ran with viewport and scissor = the bound render target", Status(RigHost.Violations == 0 && RigHost.Checked > 0), false,
            new { RigHost.Checked, RigHost.Violations, first = RigHost.FirstViolation }, ""));
        var missing = new List<string>(); var mismatched = new List<string>();
        foreach (var (shader, name, width) in RigHost.Parameters.OrderBy(p => p.Shader).ThenBy(p => p.Name))
        {
            var parameter = r.Assets.GetEffect(shader).Parameters[name];
            if (parameter is null) { missing.Add(shader + "." + name); continue; }
            int actual = parameter.RowCount * parameter.ColumnCount;
            if (actual != width) mismatched.Add($"{shader}.{name}: set {width}, compiled {parameter.RowCount}x{parameter.ColumnCount}");
        }
        list.Add(new("shader-parameters", "every TrySetParameter name/width the production files set matches the compiled .fxc", Status(mismatched.Count == 0), false,
            new { set = RigHost.Parameters.Count, mismatched, missingDroppedByCompiler = missing, passes = RigHost.Passes.Select(p => p.Shader + "." + p.Pass).OrderBy(x => x) },
            "A missing name is a uniform the compiler removed (unused); TrySetParameter ignores it in game exactly as here."));
        list.Add(new("performer", "Vespera's DrawPerformer is a verbatim build-time extraction of the production method", "pass", false, RigProvenance.Performer, ""));
        foreach (var g in list) Console.WriteLine($"self  {g.Status,-17} {g.Title}");
        return list;
    }

    // ---- helpers -------------------------------------------------------------------------------------------------
    internal static bool BodyOnly(string shader, string pass) => shader is "ScarletApparitions" or "ScarletChoir" && pass == "AutoloadPass";

    private Color[] Pixels(RigScene s, in ScarletView view, in RigVariant v, RigLayers layers, Color clear)
    {
        var target = r.Frame(view.Width, view.Height);
        r.Render(s, view, v, layers, target, clear);
        return RigRenderer.Read(target);
    }

    private string Hash(RigScene s, int tick)
    {
        var view = s.View(r.Device, o.Width, o.Height, tick, false);
        var px = Pixels(s, view, Normal, RigLayers.Frame & ~RigLayers.Labels, Color.Black);
        return Convert.ToHexString(SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(px.AsSpan())));
    }

    // Skinned-mesh alpha > .05 at lags 0..21 (true past poses), dilated by 12 px at zoom 1.
    private bool[] BodyRange(RigScene s, in ScarletView view, in RigVariant v)
    {
        var union = new bool[view.Width * view.Height];
        RigHost.PassFilter = BodyOnly;
        try
        {
            foreach (int lag in Lags)
            {
                var px = Pixels(s, view.At(view.Tick - lag), v, RigLayers.Apparition, Color.Transparent);
                for (int i = 0; i < px.Length; i++) union[i] |= px[i].A > 12;
            }
        }
        finally { RigHost.PassFilter = null; }
        return Dilate(union, view.Width, view.Height, RangeDilate * view.Zoom);
    }

    private bool[] Diff(RigScene s, in ScarletView view, RigLayers layers, in RigVariant a, in RigVariant b, int threshold)
    {
        var x = Pixels(s, view, a, layers, Color.Transparent); var y = Pixels(s, view, b, layers, Color.Transparent);
        var d = new bool[x.Length];
        for (int i = 0; i < x.Length; i++)
            d[i] = Math.Abs(x[i].R - y[i].R) > threshold || Math.Abs(x[i].G - y[i].G) > threshold || Math.Abs(x[i].B - y[i].B) > threshold || Math.Abs(x[i].A - y[i].A) > threshold;
        return d;
    }

    // Inside the field, outside every displayed signature footprint (warning to residue), plus the central 94 px band.
    private bool[] SafeZones(RigScene s, in ScarletView view, int tick)
    {
        var field = PreviewPlanner.Field;
        var shown = new List<CrimsonStroke>();
        var buffer = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var p in s.Plans)
        {
            if (!p.IsSignature || tick < p.Born || tick >= p.End + ScarletInkStroke.ResidueTicksOf(p)) continue;
            int n = CrimsonTechniqueGeometry.Write(p, p.Fire, buffer, true);
            for (int i = 0; i < n; i++) shown.Add(buffer[i]);
        }
        var safe = new bool[view.Width * view.Height];
        var inverse = Matrix.Invert(view.GameView);
        for (int y = 0; y < view.Height; y++)
            for (int x = 0; x < view.Width; x++)
            {
                var w = Vector2.Transform(new Vector2(x + .5f, y + .5f), inverse) + view.ScreenPosition;
                if (w.X < field.Left || w.X > field.Right || w.Y < field.Top || w.Y > field.Bottom) continue;
                if (MathF.Abs(w.X - field.CenterX) <= 47) { safe[y * view.Width + x] = true; continue; }
                bool covered = false;
                foreach (var st in shown) if (Distance(w, st) <= 2) { covered = true; break; }
                safe[y * view.Width + x] = !covered && shown.Count > 0;
            }
        return safe;
    }

    // Pixels inside a plan's capsules (forecast footprint, or the live strokes at `age`) on screen.
    private bool[] Capsules(RigScene s, in ScarletView view, in CrimsonGesturePlan p, float age, bool forecast)
    {
        var buffer = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        int n = CrimsonTechniqueGeometry.Write(p, forecast ? p.Fire : age, buffer, forecast);
        var mask = new bool[view.Width * view.Height];
        var inverse = Matrix.Invert(view.GameView);
        for (int y = 0; y < view.Height; y++)
            for (int x = 0; x < view.Width; x++)
            {
                var w = Vector2.Transform(new Vector2(x + .5f, y + .5f), inverse) + view.ScreenPosition;
                for (int i = 0; i < n; i++) if (Distance(w, buffer[i]) <= -2) { mask[y * view.Width + x] = true; break; }
            }
        return mask;
    }

    // The whole field plus a margin, at zoom 1 (world pixels = target pixels).
    private const int WorldMargin = 220;
    private ScarletView WorldView(int tick, out RenderTarget2D target)
    {
        var field = PreviewPlanner.Field;
        int w = (int)(field.Right - field.Left) + WorldMargin * 2, h = (int)(field.Bottom - field.Top) + WorldMargin * 2;
        world ??= new RenderTarget2D(r.Device, w, h, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        target = world;
        return ScarletView.Create(r.Device, w, h, new Vector2(field.Left - WorldMargin, field.Top - WorldMargin), 1, tick);
    }

    // One plan's ScarletInk alone (production ScarletInkStroke), transparent background.
    private Color[] InkOnly(RigScene s, in ScarletView view, in CrimsonGesturePlan p, RenderTarget2D target)
    {
        CrimsonVisuals.Reduced = view.Reduced;
        r.Device.SetRenderTarget(target);
        r.Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1f, 0);
        new ScarletInkStroke().Draw(view, r.Assets, p, s.Plans);
        r.Device.SetRenderTarget(null);
        return RigRenderer.Read(target);
    }

    private List<Vector2> Mesh(RigScene s, in ScarletView view, in RigVariant v)
    {
        var capture = new List<Vector2>();
        RigHost.MeshCapture = capture;
        try { Pixels(s, view, v, RigLayers.Apparition, Color.Transparent); }
        finally { RigHost.MeshCapture = null; }
        return capture;
    }

    // The scene with nothing alive: today's idle picture (G8).
    private static RigScene Idle(RigScene s) => s.WithPlans(Array.Empty<CrimsonGesturePlan>(), Array.Empty<string>());

    // CrimsonCompanionVisuals.PreDraw's performer with no attack (phase 0): grounded, facing right.
    private Color[] Companion(in ScarletView view, int tick)
    {
        var target = r.Frame(view.Width, view.Height);
        CrimsonVisuals.Reduced = false;
        Terraria.Main.screenPosition = view.ScreenPosition;
        Terraria.Main.GameViewMatrix.TransformationMatrix = view.GameView;
        r.Device.SetRenderTarget(target);
        r.Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1f, 0);
        using (var batch = new SpriteBatch(r.Device))
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Terraria.Main.Rasterizer, null, view.GameView);
            CrimsonRigPerformerExtract.DrawPerformer(batch, view.ScreenPosition, RigScene.Conductor, tick, Vector2.Zero, 1, false, 0, 0);
            batch.End();
        }
        r.Device.SetRenderTarget(null);
        return RigRenderer.Read(target);
    }

    private string BaselineDir(string gate) => Path.Combine(o.Baseline.Length > 0 ? o.Baseline : Path.Combine(output, "baseline"), gate);

    // Baselines keep the exact premultiplied RGBA bytes (a PNG round trip through FNA is not byte-exact for
    // translucent pixels), gzip-compressed, with a PNG beside them for looking.
    private void SaveRaw(Color[] px, int w, int h, string path)
    {
        using (var file = File.Create(path))
        using (var zip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionLevel.Optimal))
        using (var writer = new BinaryWriter(zip))
        {
            writer.Write(w); writer.Write(h);
            writer.Write(System.Runtime.InteropServices.MemoryMarshal.AsBytes(px.AsSpan()));
        }
        using var texture = new Texture2D(r.Device, w, h);
        texture.SetData(px);
        using var png = File.Create(Path.ChangeExtension(Path.ChangeExtension(path, null), ".png"));
        texture.SaveAsPng(png, w, h);
    }
    private static Color[] LoadRaw(string path, int w, int h)
    {
        var px = new Color[w * h];
        using var file = File.OpenRead(path);
        using var zip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new BinaryReader(zip);
        if (reader.ReadInt32() != w || reader.ReadInt32() != h) return px;
        var bytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(px.AsSpan());
        int read = 0;
        while (read < bytes.Length) { int n = zip.Read(bytes[read..]); if (n == 0) break; read += n; }
        return px;
    }

    private static float Distance(Vector2 p, CrimsonStroke s)
    {
        Vector2 a = new(s.A.X, s.A.Y), b = new(s.B.X, s.B.Y), v = b - a;
        float t = v.LengthSquared() < 1e-6f ? 0 : Math.Clamp(Vector2.Dot(p - a, v) / v.LengthSquared(), 0, 1);
        return Vector2.Distance(p, a + v * t) - s.Radius;
    }

    private static float[] Luma(Color[] px, bool[]? mask)
    {
        var list = new List<float>();
        for (int i = 0; i < px.Length; i++)
            if (mask is null || mask[i]) list.Add(.2126f * px[i].R + .7152f * px[i].G + .0722f * px[i].B);
        return list.ToArray();
    }
    private static float Mean(float[] values) => values.Length == 0 ? 0 : values.Average();
    private static float Percentile(float[] values, float p)
    {
        if (values.Length == 0) return 0;
        Array.Sort(values);
        return values[Math.Clamp((int)MathF.Round(p / 100 * (values.Length - 1)), 0, values.Length - 1)];
    }

    // A band `width` px wide around a mask (for local contrast).
    private static bool[] Ring(bool[] mask, int w, int h, float width)
    {
        var grown = Dilate(mask, w, h, width);
        for (int i = 0; i < grown.Length; i++) grown[i] &= !mask[i];
        return grown;
    }

    // Exact Euclidean dilation (Felzenszwalb-Huttenlocher squared distance transform).
    internal static bool[] Dilate(bool[] mask, int w, int h, float radius)
    {
        const double inf = 1e20;
        var grid = new double[w * h];
        for (int i = 0; i < grid.Length; i++) grid[i] = mask[i] ? 0 : inf;
        int n = Math.Max(w, h);
        var f = new double[n]; var d = new double[n]; var v = new int[n]; var z = new double[n + 1];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++) f[y] = grid[y * w + x];
            Transform(f, h, d, v, z);
            for (int y = 0; y < h; y++) grid[y * w + x] = d[y];
        }
        for (int y = 0; y < h; y++)
        {
            Array.Copy(grid, y * w, f, 0, w);
            Transform(f, w, d, v, z);
            Array.Copy(d, 0, grid, y * w, w);
        }
        var result = new bool[w * h];
        double limit = (double)radius * radius;
        for (int i = 0; i < grid.Length; i++) result[i] = grid[i] <= limit;
        return result;

        static void Transform(double[] f, int n, double[] d, int[] v, double[] z)
        {
            int k = 0; v[0] = 0; z[0] = double.NegativeInfinity; z[1] = double.PositiveInfinity;
            for (int q = 1; q < n; q++)
            {
                double s = (f[q] + (double)q * q - (f[v[k]] + (double)v[k] * v[k])) / (2.0 * q - 2.0 * v[k]);
                while (s <= z[k]) { k--; s = (f[q] + (double)q * q - (f[v[k]] + (double)v[k] * v[k])) / (2.0 * q - 2.0 * v[k]); }
                k++; v[k] = q; z[k] = s; z[k + 1] = double.PositiveInfinity;
            }
            k = 0;
            for (int q = 0; q < n; q++) { while (z[k + 1] < q) k++; d[q] = (double)(q - v[k]) * (q - v[k]) + f[v[k]]; }
        }
    }
}
