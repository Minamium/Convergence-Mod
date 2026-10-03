// Gates of the Scarlet attack-expression design (§4.3) over the rig harness, plus the harness's own self-checks.
// "Game" is the in-game picture (body material on, proposed motion, Vespera's command); "Plain" is the same motion
// with the material off, so Game - Plain is exactly the material's decoration. Decoration is light the material ADDS
// (any channel up by more than 2/255); light it takes away (the free sparks and the free heartbeat handed to the
// attack clock, Drain) is reported beside it, never counted as decoration. A gate whose input does not exist is
// reported "not_run" with the reason; "vacuous" marks a pass that cannot fail. Status values: pass | fail | not_run |
// baseline_written. Offline review only.
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

// Baselines are written only on request (--write-baseline on, from the reference code) and carry a manifest
// (baseline.json): the git revision they were rendered from, whether its tracked files were dirty, and the sha256 of
// the files whose picture they pin. A gate compares only against a baseline with a manifest; for the gates pinning
// files the attack expression changes (g8 the rigs, s4 Vespera's command), a manifest whose files hash exactly like
// the tree under test is a self-comparison and is refused too. Refusals report not_run with the reason.
internal static class RigBaseline
{
    internal sealed record Manifest(string Gate, string Revision, bool Dirty, string Written, Dictionary<string, string> Files, string Note);
    private static readonly Dictionary<string, string[]> Tracked = new()
    {
        ["g8"] = new[] { "Client/Encounters/CrimsonFoundry/ScarletApparitionRig.cs", "Client/Encounters/CrimsonFoundry/CrimsonChoirRig.cs",
            "Assets/AutoloadedEffects/Shaders/ScarletApparitions.fx", "Assets/AutoloadedEffects/Shaders/ScarletChoir.fx" },
        ["s4"] = new[] { "Client/Encounters/CrimsonFoundry/CrimsonRig.Performer.cs", "Client/Encounters/CrimsonFoundry/Vfx/ScarletGestureMotion.cs" },
    };
    private static readonly HashSet<string> UnderChange = new() { "g8", "s4" };
    internal const string File = "baseline.json";

    internal static Dictionary<string, string> Hashes(string root, string gate)
        => Tracked[gate].ToDictionary(f => f, f => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            System.IO.File.ReadAllText(Path.Combine(root, f)).Replace("\r\n", "\n")))).ToLowerInvariant());

    internal static void Write(string root, string dir, string gate, string note)
    {
        Directory.CreateDirectory(dir);
        string revision = Git(root, "rev-parse HEAD").Trim();
        bool dirty = Git(root, "status --porcelain -- " + string.Join(" ", Tracked[gate])).Trim().Length > 0;
        var manifest = new Manifest(gate, revision, dirty, DateTime.UtcNow.ToString("u"), Hashes(root, gate), note);
        System.IO.File.WriteAllText(Path.Combine(dir, File), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    // null when the baseline may be compared; otherwise why not.
    internal static string? Refuse(string root, string dir, string gate, out Manifest? manifest)
    {
        manifest = null;
        string path = Path.Combine(dir, File);
        if (!System.IO.File.Exists(path)) return $"no baseline with a provenance manifest in {dir}; write one from the reference code with -WriteBaseline";
        var read = JsonSerializer.Deserialize<Manifest>(System.IO.File.ReadAllText(path));
        manifest = read;
        if (read is null) return "unreadable baseline manifest " + path;
        if (UnderChange.Contains(gate))
        {
            var now = Hashes(root, gate);
            if (now.All(x => read.Files.TryGetValue(x.Key, out var h) && h == x.Value))
                return $"baseline {path} was written from the files under test ({read.Revision}): comparing them to themselves proves nothing";
        }
        return null;
    }

    private static string Git(string root, string arguments)
    {
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo("git", "-C \"" + root + "\" " + arguments) { RedirectStandardOutput = true, UseShellExecute = false };
            using var process = System.Diagnostics.Process.Start(start)!;
            string text = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? text : "unknown";
        }
        catch (Exception e) { return "unknown (" + e.GetType().Name + ")"; }
    }
}

internal sealed class RigGates
{
    internal static bool Failed;
    private readonly RigRenderer r;
    private readonly RigOptions o;
    private readonly string root, output;
    private readonly List<RigGate> gates = new();
    private readonly Dictionary<string, RigScene> scenes = new();
    private RenderTarget2D? world;

    // Design §2.7: draws per frame of the new rigs (Normal / Reduced) and the vertices they may add per frame over
    // today's. Today's vertices per frame (Normal / Reduced) were measured through this shim on the rigs before S2/S3
    // (feat/scarlet-attack-expression 0499a91: Crown 32/11 draws, Mantle 31/11, Choir 65-69/23, as the design counts).
    private static readonly Dictionary<string, (int Normal, int Reduced, int Added, int TodayVertices, int TodayVerticesReduced)> Budget = new()
    {
        ["crown"] = (18, 11, 228, 30558, 29382), ["mantle"] = (9, 8, 252, 30342, 29382), ["choir"] = (52, 23, 120, 40434, 35622),
    };
    // The design's per-element vertex numbers (§2.1-2.3), reported beside the per-frame total.
    private static readonly Dictionary<string, (string Pass, int Vertices)[]> Elements = new()
    {
        ["crown"] = new[] { ("SparkPass", 192), ("DripPass", 36) }, ["mantle"] = new[] { ("WispPass", 168), ("SparkPass", 84) },
        ["choir"] = new[] { ("SparkPass", 120) },
    };
    // Shaders the attack expression must not change (design §2.0.6, G11). .fx hashed with LF line ends.
    private static readonly Dictionary<string, string> Pinned = new()
    {
        ["ScarletInk.fx"] = "79d27870a6977e089e3dd02f677e39e93a8620c3f1d3644330eccb7986d34ee8",
        ["ScarletInk.fxc"] = "9d8a2cfe1d321915ef50c799fc22e1949e0c1f8fa41fec822b7225d459b15468",
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
    // The body range is the union of the true past silhouettes over the last 21 ticks, dilated 12 px. The design
    // samples it at lags {0,4,8,12,16,21}; a whip moves a hook ~20 px per tick, so those six samples leave gaps in the
    // swept surface wider than the 12 px dilation. G1 gates on every tick of the same 21-tick window and reports the
    // six-sample count beside it.
    private static readonly int[] Lags = Enumerable.Range(0, 22).ToArray();
    private static readonly int[] DesignLags = { 0, 4, 8, 12, 16, 21 };
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
    // An Act's first phrase (pickup crossflow, cell A, closing crossflow) and its second (cell B, the third note opening as the second
    // strikes). The idle ticks the G8 baselines were rendered at: the phrase is not part of an idle frame, only the absolute clock.
    private static readonly string[] Basics = { "act1-basic", "act2-basic", "act3-basic", "act1-basic2", "act2-basic2", "act3-basic2" };
    private static readonly Dictionary<string, int> IdleBase = new() { ["act1-signature"] = 3950, ["act2-signature"] = 7775, ["act3-signature"] = 12275 };
    private static readonly RigVariant Normal = new(false, false, false, true);   // today's picture
    private static readonly RigVariant Plain = new(false, false, true, true);    // proposed motion, material off
    private static readonly RigVariant Game = new(false, true, true, true);      // the in-game picture

    internal object Run()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Step("G0", () => G0()); Step("G1+G2", () => G1G2()); Step("G3", () => G3()); Step("G4", () => G4()); Step("G5", () => G5());
        Step("G6", () => G6()); Step("G7", () => G7()); Step("G8", () => G8()); Step("G9", () => G9()); Step("G10", () => G10());
        Step("G11", () => G11()); Step("G12", () => G12()); Step("G13", () => G13()); Step("G14", () => G14());
        Step("S4", () => gates.AddRange(new ConductorGates(r, o, root, output).Run()));
        var self = Self();
        world?.Dispose();
        foreach (var g in gates) Console.WriteLine($"{g.Id,-5} {g.Status,-17}{(g.Vacuous ? " (vacuous)" : "")} {g.Title}");
        Failed = gates.Any(g => g.Status == "fail") || self.Any(g => g.Status == "fail");
        return new
        {
            note = "Gates of the Scarlet attack-expression design over the offline rig harness. Not a playtest; in-game acceptance is not_run.",
            material = RigDriver.MaterialAvailable, motion = RigDriver.MotionAvailable,
            variants = new { game = "material on, proposed motion, Vespera's command (the in-game picture)", plain = "proposed motion, material off", today = "material off, current motion (today's picture)" },
            gates, self, seconds = Math.Round(watch.Elapsed.TotalSeconds, 1)
        };
        void Step(string id, Action run)
        {
            // SCARLET_GATE_ONLY=G6,S4 runs only those steps (a review aid; the gates file then holds only them).
            if (Environment.GetEnvironmentVariable("SCARLET_GATE_ONLY") is { Length: > 0 } only && !only.Split(',').Contains(id)) return;
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
    // G2 has two parts. The design's (§4.3): no decoration outside the body range (lags 0..21, +12 px) in a safe zone,
    // which only G1's own failures can trip. The second gates what a player in the safe ground actually sees: on the
    // composited frame (backdrop and NPC layer), the light the material adds over safe ground outside the body's
    // current silhouette (+4 px) stays at or below the forecast band's median, and on the body over safe ground at or
    // below .8 x the live ink (rule 3 limits, the scene's own references from G3), both at p99.5.
    private void G1G2()
    {
        var range = new List<object>(); var zones = new List<object>(); var seen = new List<object>();
        int outside = 0, inZones = 0, outsideDesignLags = 0, dimmedOutside = 0, dimmedInZones = 0;
        bool seenOk = true;
        foreach (string name in Signatures.Concat(Basics))
        {
            var s = Scene(name, "C");
            var limits = References(name);
            var offSilhouette = new List<float>(); var onSilhouette = new List<float>();
            int safePixels = 0;
            var third = s.Third; var flow = s.Flow;
            var ticks = new SortedDictionary<int, string>();
            foreach (int rel in new[] { -14, -3, 0, 1, 2, 4, 6, 10, 16 }) ticks.TryAdd(third.Fire + rel, $"n3{rel:+0;-0;+0}");
            foreach (int rel in new[] { -28, 0, 2, 7 }) ticks.TryAdd(flow.Fire + rel, $"xf{rel:+0;-0;+0}");
            foreach (var (tick, label) in ticks)
            {
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                var (body, designBody) = BodyRanges(s, view, Plain);
                var (added, dimmed) = Change(s, view, RigLayers.Npc, Plain, Game, 2);
                var safe = SafeZones(s, view, tick);
                int bodyPixels = body.Count(b => b), addedPixels = 0, addedOutside = 0, addedOutsideDesign = 0, addedInZone = 0, dimOut = 0, dimZone = 0, zonePixels = 0;
                for (int i = 0; i < body.Length; i++)
                {
                    if (safe[i] && !body[i]) zonePixels++;
                    if (dimmed[i] && !body[i]) { dimOut++; if (safe[i]) dimZone++; }
                    if (!added[i]) continue;
                    addedPixels++;
                    if (!designBody[i]) addedOutsideDesign++;
                    if (!body[i]) { addedOutside++; if (safe[i]) addedInZone++; }
                }
                outside += addedOutside; inZones += addedInZone; outsideDesignLags += addedOutsideDesign; dimmedOutside += dimOut; dimmedInZones += dimZone;
                if (Dumps is not null && addedOutside > 0)
                {
                    // Review aid (SCARLET_GATE_DUMPS=<dir>): the in-game NPC layer over black, the body range tinted blue,
                    // decoration outside it magenta.
                    var px = Pixels(s, view, Game, RigLayers.Npc, Color.Black);
                    for (int i = 0; i < px.Length; i++)
                        if (added[i] && !body[i]) px[i] = new Color(255, 0, 255);
                        else if (body[i]) px[i] = new Color(px[i].R, px[i].G, (byte)Math.Min(255, px[i].B + 40));
                    Dump($"g1-{name}-{label}.png", px, view.Width, view.Height);
                }
                // Which pass draws decoration outside the range (each pass alone, the same comparison).
                Dictionary<string, int>? byPass = null;
                if (addedOutside > 0)
                {
                    byPass = new();
                    foreach (string pass in new[] { "AutoloadPass", "AuraPass", "RibbonPass", "HeartPass", "SparkPass", "DripPass", "WispPass" })
                    {
                        RigHost.PassFilter = (_, applied) => applied == pass;
                        try
                        {
                            var (one, _) = Change(s, view, RigLayers.Apparition, Plain, Game, 2);
                            int count = 0;
                            for (int i = 0; i < one.Length; i++) if (one[i] && !body[i]) count++;
                            if (count > 0) byPass[pass] = count;
                        }
                        finally { RigHost.PassFilter = null; }
                    }
                }
                range.Add(new { scene = name, tick = label, bodyRangePixels = bodyPixels, addedPixels, addedOutsideRange = addedOutside,
                    addedOutsideSixLagRange = addedOutsideDesign, dimmedOutsideRange = dimOut, outsideByPass = byPass });
                zones.Add(new { scene = name, tick = label, safeOutsideBody = zonePixels, addedInSafeZones = addedInZone, dimmedInSafeZones = dimZone });
                // What the safe ground shows: Game - Plain luma over the real backdrop, outside / on the current silhouette.
                var silhouette = BodyMask(s, view, Plain, 4);
                var plainFull = Pixels(s, view, Plain, RigLayers.Backdrop | RigLayers.Npc, Color.Black);
                var gameFull = Pixels(s, view, Game, RigLayers.Backdrop | RigLayers.Npc, Color.Black);
                Color[]? mark = Dumps is not null ? (Color[])gameFull.Clone() : null;
                bool marked = false;
                for (int i = 0; i < safe.Length; i++)
                {
                    if (!safe[i]) continue;
                    safePixels++;
                    float d = Y(gameFull[i]) - Y(plainFull[i]);
                    if (d <= 2) continue;
                    if (silhouette[i]) onSilhouette.Add(d); else offSilhouette.Add(d);
                    if (mark is not null && (silhouette[i] ? d > limits.Inside : d > limits.Outside)) { mark[i] = new Color(0, 255, 255); marked = true; }
                }
                if (marked) Dump($"g2-{name}-{label}.png", mark!, view.Width, view.Height);
            }
            float off = Percentile(offSilhouette.ToArray(), 99.5f), on = Percentile(onSilhouette.ToArray(), 99.5f);
            bool groundOk = off <= limits.Outside && on <= limits.Inside;
            seenOk &= groundOk;
            seen.Add(new { scene = name, safePixels, limitOutside = limits.Outside, limitInside = limits.Inside,
                offSilhouetteP995 = off, offSilhouetteMax = offSilhouette.Count == 0 ? 0 : offSilhouette.Max(), offSilhouettePixels = offSilhouette.Count,
                onSilhouetteP995 = on, onSilhouetteMax = onSilhouette.Count == 0 ? 0 : onSilhouette.Max(), onSilhouettePixels = onSilhouette.Count, pass = groundOk });
        }
        var hands = HandsParticles();
        Add("G1", "decoration (material on adds > 2/255) inside the body range (every lag 0..21, +12 px)", Status(outside == 0),
            new { addedOutsideRange = outside, addedOutsideSixLagRange = outsideDesignLags, dimmedOutsideRange = dimmedOutside, frames = range },
            "Camera C (zoom 2), Game vs Plain (the same proposed motion, material off), the NPC layer, Act I-III signature and basic phrases. "
            + "addedOutsideSixLagRange counts the same against the design's six lag samples {0,4,8,12,16,21} (not gated, see Lags). "
            + "dimmedOutsideRange is light the material removes (free sparks / heartbeat handed to the attack clock): reported, not decoration.");
        Add("G2", "safe zones (curtain corridors, hands gaps, staff gaps, ground between basic strikes, central 94 px): no decoration outside the body range, "
            + "added light over safe ground <= the forecast band median off the current silhouette and <= .8 x live ink on it; no Choir particles outside the body in FourHands windows",
            Status(inZones == 0 && hands.Outside == 0 && seenOk),
            new { addedInSafeZones = inZones, dimmedInSafeZones = dimmedInZones, safeGround = seen, frames = zones, fourHands = hands.Measured },
            "Safe = inside the field, outside every displayed footprint of every plan (signature and basic, warning to residue), plus the central 94 px band. "
            + "addedInSafeZones: the design's count (outside the body range, NPC layer over transparent). safeGround: camera C, G1's ticks, the composited "
            + "backdrop + NPC layer, luma(Game) - luma(Plain) > 2 over safe pixels, split by the current silhouette (lag 0, alpha > .05, +4 px); "
            + "limits are the scene's G3 references (camera A2, third note). "
            + "fourHands: every tick of every FourHands window (Born .. Fire+Span) of the Act III signature phrase, the Choir's SparkPass drawn alone in Game and in Plain: "
            + "pixels lit only in Game (added by the material) outside the body's own silhouette (lag 0, +12 px) gate; the free sparks the Choir already draws (lit in both) "
            + "fade out as a window opens and are reported, not gated.");
    }

    // The Choir's sparks during the FourHands windows (design §2.0.3-6: no particle outside the body there). The material adds none
    // (a SparkPass pixel lit in Game and not in Plain, outside the body, gates); the free sparks the Choir already draws (lit in both)
    // fade out as a window opens and back in as it closes, which since protocol80 can be a few ticks after a gap (the previous
    // closing crossflow's window ends 14 ticks before the first step is born), so they are reported, not gated.
    private (int Outside, object Measured) HandsParticles()
    {
        var s = Scene("act3-signature", "C");
        int outside = 0, inside = 0, free = 0, freeOutside = 0, ticksWithSparks = 0, lastRel = int.MinValue;
        var rows = new List<object>(); var outsideTicks = new List<object>();
        foreach (var p in s.Plans)
        {
            if (p.Technique != CrimsonTechnique.FourHands || !ScarletNotes.TryFrom(p, s.Flipped, RigScene.Conductor.X, RigScene.Conductor.Y, out var note)) continue;
            for (int tick = (int)note.Born; tick < note.Close; tick++)
            {
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                var body = BodyMask(s, view, Game, 12);
                Color[] px, plain;
                RigHost.PassFilter = (shader, pass) => shader == "ScarletChoir" && pass == "SparkPass";
                try { px = Pixels(s, view, Game, RigLayers.Apparition, Color.Transparent); plain = Pixels(s, view, Plain, RigLayers.Apparition, Color.Transparent); }
                finally { RigHost.PassFilter = null; }
                int o0 = 0, i0 = 0;
                for (int i = 0; i < px.Length; i++)
                    if (Lit(px[i], 2))
                    {
                        if (Lit(plain[i], 2)) { free++; if (!body[i]) freeOutside++; continue; }
                        if (body[i]) i0++; else o0++;
                    }
                outside += o0; inside += i0;
                if (o0 > 0) outsideTicks.Add(new { pulse = p.Pulse, afterBorn = tick - (int)note.Born, afterFire = tick - (int)note.Fire, outside = o0 });
                if (o0 + i0 > 0) { ticksWithSparks++; lastRel = Math.Max(lastRel, tick - (int)note.Born); }
            }
            rows.Add(new { phrase = p.Phrase, pulse = p.Pulse, born = note.Born, fire = note.Fire, close = note.Close });
        }
        return (outside, new { windows = rows, addedSparkPixelsOutsideBody = outside, addedSparkPixelsInsideBody = inside, freeSparkPixels = free,
            freeSparkPixelsOutsideBody = freeOutside, outsideTicks, ticksWithSparks,
            lastSparkTickAfterBorn = lastRel == int.MinValue ? (int?)null : lastRel });
    }

    // Rule 3's references for a scene (camera A2, its third note): the forecast band's median luma at Fire-8 and the
    // live ink's p99.5 at Fire+4 on the composited field; outside limit = min(median, .5 x ink), inside = .8 x ink.
    private readonly Dictionary<string, (float BandMedian, float InkP995, float Outside, float Inside)> references = new();
    private (float BandMedian, float InkP995, float Outside, float Inside) References(string name)
    {
        if (references.TryGetValue(name, out var cached)) return cached;
        var s = Scene(name, "A2");
        var third = s.Third;
        // The Choir's rake ribbon is not reproduced offline: its hit-shape stand-in is not the live strike, so its brightness (a function
        // of the backdrop's beat under it) is no measure of what the decoration must stay under. The Choir's basic phrases use the
        // reference of its signature phrase, whose FourHands are drawn in ScarletInk like the other Acts' strikes.
        if (third.Technique == CrimsonTechnique.ChoirRakes) return references[name] = References("act3-signature");
        var warnView = s.View(r.Device, o.Width, o.Height, third.Fire - 8, false);
        var warn = Pixels(s, warnView, Game, RigLayers.Backdrop | RigLayers.Field, Color.Black);
        var band = Capsules(s, warnView, third, third.Fire, true);
        var liveView = s.View(r.Device, o.Width, o.Height, third.Fire + 4, false);
        var live = Pixels(s, liveView, Game, RigLayers.Backdrop | RigLayers.Field, Color.Black);
        var strike = Capsules(s, liveView, third, third.Fire + 4, false);
        float bandMedian = Percentile(Luma(warn, band), 50), inkP995 = Percentile(Luma(live, strike), 99.5f);
        return references[name] = (bandMedian, inkP995, Math.Min(bandMedian, inkP995 * .5f), inkP995 * .8f);
    }

    // ---- G3: brightness of the decoration against the forecast band and the live ink --------------------------
    // Gated on every pass alone as well as on all of them together: a bright pass over a small area must not hide in
    // the pooled percentile.
    private void G3()
    {
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Signatures)
        {
            var (bandMedian, inkP995, limitOutside, limitInside) = References(name);
            // The decoration itself: Game - Plain on the apparition layer over black, camera C, around the third note and the crossflow.
            var c = Scene(name, "C");
            var n3 = c.Third; var flow = c.Flow;
            var sets = new Dictionary<string, (List<float> Outside, List<float> Inside)> { ["all"] = (new(), new()), ["heartExcluded"] = (new(), new()) };
            var filters = new List<(string, Func<string, string, bool>?)> { ("all", null), ("heartExcluded", (_, pass) => pass != "HeartPass") };
            foreach (string only in new[] { "AutoloadPass", "AuraPass", "RibbonPass", "HeartPass", "SparkPass", "DripPass", "WispPass" })
            {
                sets[only] = (new(), new());
                filters.Add((only, (_, pass) => pass == only));
            }
            foreach (int tick in new[] { n3.Born + 14, n3.Fire - 3, n3.Fire, n3.Fire + 1, n3.Fire + 2, n3.Fire + 4, n3.Fire + 10, flow.Fire, flow.Fire + 2, flow.Fire + 7 })
            {
                var view = c.View(r.Device, o.Width, o.Height, tick, false);
                // The body: the skinned mesh's alpha with its pinholes closed (2 px at zoom 1), so a speck of light
                // inside a see-through fleck of the painted body is not counted as outside it.
                var body = Close(BodyMask(c, view, Plain, 0), view.Width, view.Height, 2 * view.Zoom);
                foreach (var (key, filter) in filters)
                {
                    Color[] off, on;
                    RigHost.PassFilter = filter;
                    try { off = Pixels(c, view, Plain, RigLayers.Apparition, Color.Transparent); on = Pixels(c, view, Game, RigLayers.Apparition, Color.Transparent); }
                    finally { RigHost.PassFilter = null; }
                    bool marked = false;
                    var mark = Dumps is not null && key == "all" ? (Color[])on.Clone() : null;
                    for (int i = 0; i < on.Length; i++)
                    {
                        if (Added(on[i], off[i]) <= 2) continue;
                        float d = MathF.Max(0, Y(on[i]) - Y(off[i]));
                        (body[i] ? sets[key].Inside : sets[key].Outside).Add(d);
                        if (mark is not null && !body[i] && d > limitOutside) { mark[i] = new Color(0, 255, 255); marked = true; }
                    }
                    // Review aid: decoration outside the body brighter than the outside limit in cyan.
                    if (mark is not null && marked) Dump($"g3-{name}-{tick - n3.Fire:+0;-0;+0}.png", mark, view.Width, view.Height);
                }
            }
            float outAll = Percentile(sets["all"].Outside.ToArray(), 99.5f), inAll = Percentile(sets["all"].Inside.ToArray(), 99.5f);
            float outBody = Percentile(sets["heartExcluded"].Outside.ToArray(), 99.5f), inBody = Percentile(sets["heartExcluded"].Inside.ToArray(), 99.5f);
            var passes = sets.Where(x => x.Key.EndsWith("Pass")).ToArray();
            float outPass = passes.Length == 0 ? 0 : passes.Max(x => Percentile(x.Value.Outside.ToArray(), 99.5f));
            float inPass = passes.Length == 0 ? 0 : passes.Max(x => Percentile(x.Value.Inside.ToArray(), 99.5f));
            bool pass = outAll <= limitOutside && inAll <= limitInside && outPass <= limitOutside && inPass <= limitInside;
            ok &= pass;
            rows.Add(new { scene = name, forecastBandMedian = bandMedian, inkLiveP995 = inkP995, limitOutsideBody = limitOutside, limitInsideBody = limitInside,
                decorationOutsideP995 = outAll, decorationInsideP995 = inAll, worstPassOutsideP995 = outPass, worstPassInsideP995 = inPass,
                decorationPixels = new[] { sets["all"].Outside.Count, sets["all"].Inside.Count },
                heartExcluded = new { outsideP995 = outBody, insideP995 = inBody, pixels = new[] { sets["heartExcluded"].Outside.Count, sets["heartExcluded"].Inside.Count } },
                byPass = sets.Where(x => x.Key.EndsWith("Pass") && x.Value.Outside.Count + x.Value.Inside.Count > 0).ToDictionary(x => x.Key, x => new
                {
                    outsideP995 = Percentile(x.Value.Outside.ToArray(), 99.5f), insideP995 = Percentile(x.Value.Inside.ToArray(), 99.5f),
                    pixels = new[] { x.Value.Outside.Count, x.Value.Inside.Count }
                }), pass });
        }
        Add("G3", "decoration luminance (all passes and each pass alone): outside body <= forecast band median and <= .5 x ink live p99.5; inside <= .8 x", Status(ok), rows,
            "References on the composited frame at camera A2 (third note: forecast band at Fire-8, live ink at Fire+4). Decoration = luma(Game) - luma(Plain) "
            + "where the material adds light, apparition layer over black, camera C, at Born+14, Fire-3..Fire+10 of the third note and Fire, +2, +7 of the crossflow; "
            + "body = the skinned mesh's own alpha (lag 0) with its pinholes closed (2 px at zoom 1). The gate holds for all passes together and for each pass alone (worstPass*). "
            + "heartExcluded repeats it without the HeartPass (reported).");
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
                    var on = Pixels(s, view, Game, RigLayers.Field, Color.Transparent);
                    frames++;
                    if (!off.AsSpan().SequenceEqual(on)) different++;
                }
        }
        Add("G4", "forecasts, seals and ink (PostDrawTiles) byte-identical with the material on / off", Status(different == 0),
            new { frames, different }, "Today's picture (material off, current motion) against the in-game one (material on, proposed motion), Field layer only, Act I-III signature and basic.");
    }

    // ---- G5: local contrast of the forecast band keeps >= 90% -------------------------------------------------
    // Where a body sits under a forecast: every 100 px tile of every displayed forecast capsule near the body (its
    // silhouette + 48 px), on every tick of the third note's warning. The forecast's local contrast in a tile is how
    // far it lifts the picture under it, against that picture: (mean luma of the capsule with the forecast - without
    // it) / (mean luma without it + 8), Weber's contrast of the band over what it covers. The forecast layer is the
    // same in both pictures (G4), so the ratio Game / Plain (the material over the same motion) moves only when the
    // material brightens or darkens what lies under the band. A tile counts when the plain band lifts its picture by at
    // least 3 luma. The band-against-ring difference of the composited frame (|capsule - 24 px ring|) is reported too:
    // it also moves when the body's own light changes in the ring (a heartbeat), so it is not the gate.
    private void G5()
    {
        var rows = new List<object>(); bool ok = true;
        const int Tile = 100, MinimumPixels = 150;
        var withField = RigLayers.Frame & ~RigLayers.Labels;
        var withoutField = withField & ~RigLayers.Field;
        foreach (string name in Signatures.Concat(Basics))
        {
            var s = Scene(name, "A2");
            var third = s.Third;
            float worst = float.PositiveInfinity, worstToday = float.PositiveInfinity, ringWorst = float.PositiveInfinity; string worstAt = "";
            float worstOff = 0, worstOn = 0;
            int tiles = 0;
            var masks = new Dictionary<int, (bool[] Band, bool[] Ring)>();
            for (int tick = third.Born; tick < third.Fire; tick++)
            {
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                int w = view.Width, h = view.Height;
                var near = BodyMask(s, view, Plain, 48);
                Color[][]? px = null; // plain, game, today with the forecasts; the same three without them
                for (int k = 0; k < s.Plans.Length; k++)
                {
                    var p = s.Plans[k];
                    if (tick < p.Born || tick >= p.Fire) continue;
                    if (!masks.TryGetValue(k, out var m))
                    {
                        var band = Capsules(s, view, p, p.Fire, true);
                        masks[k] = m = (band, Ring(band, w, h, 24));
                    }
                    px ??= new[]
                    {
                        Pixels(s, view, Plain, withField, Color.Black), Pixels(s, view, Game, withField, Color.Black), Pixels(s, view, Normal, withField, Color.Black),
                        Pixels(s, view, Plain, withoutField, Color.Black), Pixels(s, view, Game, withoutField, Color.Black), Pixels(s, view, Normal, withoutField, Color.Black),
                    };
                    for (int ty = 0; ty < h; ty += Tile)
                        for (int tx = 0; tx < w; tx += Tile)
                        {
                            int bandNear = 0, ringCount = 0, bandCount = 0;
                            var lit = new double[3]; var under = new double[3]; var ring = new double[2];
                            for (int y = ty; y < Math.Min(h, ty + Tile); y++)
                                for (int x = tx; x < Math.Min(w, tx + Tile); x++)
                                {
                                    int i = y * w + x;
                                    if (m.Band[i])
                                    {
                                        bandCount++; if (near[i]) bandNear++;
                                        for (int v = 0; v < 3; v++) { lit[v] += Y(px[v][i]); under[v] += Y(px[v + 3][i]); }
                                    }
                                    else if (m.Ring[i]) { ringCount++; ring[0] += Y(px[0][i]); ring[1] += Y(px[1][i]); }
                                }
                            if (bandNear < MinimumPixels) continue;
                            float Lift(int v) => (float)((lit[v] - under[v]) / bandCount);
                            float Weber(int v) => Lift(v) / (float)(under[v] / bandCount + 8);
                            if (Lift(0) < 3) continue;
                            tiles++;
                            float ratio = Weber(1) / Weber(0);
                            if (ratio < worst) { worst = ratio; worstAt = $"born{tick - p.Born:+0;-0;+0} {p.Technique} pulse {p.Pulse} tile ({tx},{ty})"; worstOff = Weber(0); worstOn = Weber(1); }
                            if (Lift(2) >= 3) worstToday = Math.Min(worstToday, Weber(1) / Weber(2));
                            if (ringCount >= MinimumPixels)
                            {
                                float plainEdge = (float)Math.Abs(lit[0] / bandCount - ring[0] / ringCount), gameEdge = (float)Math.Abs(lit[1] / bandCount - ring[1] / ringCount);
                                if (plainEdge >= 6) ringWorst = Math.Min(ringWorst, gameEdge / plainEdge);
                            }
                        }
                }
            }
            bool pass = tiles == 0 || worst >= .9f;
            ok &= pass;
            rows.Add(new { scene = name, tiles, minRatio = tiles == 0 ? (float?)null : worst, worstAt, weberOff = worstOff, weberOn = worstOn,
                minRatioOverToday = float.IsFinite(worstToday) ? worstToday : (float?)null,
                bandRingMinRatio = float.IsFinite(ringWorst) ? ringWorst : (float?)null, vacuous = tiles == 0, pass });
        }
        Add("G5", "forecast band local contrast near the bodies (100 px tiles, every warning tick) with decoration >= 90% of without", Status(ok), rows,
            "Camera A2, every tick of the third note's warning (Born .. Fire-1) and every forecast displayed then; a tile counts when it holds >= 150 capsule "
            + "pixels within 48 px of the body's silhouette and the plain forecast lifts its capsule by >= 3 luma. Contrast = the forecast's Weber contrast "
            + "over the picture it covers (with - without the PostDrawTiles layer, over the luma without it + 8). Gate: min over tiles of Game / Plain. "
            + "minRatioOverToday also includes the proposed motion; bandRingMinRatio is |capsule - 24 px ring| of the composite where its plain value is "
            + ">= 6 luma (reported: the body's own light in the ring moves it).");
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
                    if (InkOnly(s, view, p, target).Any(c => Lit(c, 3))) first = tick;
                }
                bool ok = first >= p.Fire - 1 && first <= p.Fire + 1;
                inkOk &= ok;
                inkRows.Add(new { scene = name, technique = p.Technique.ToString(), pulse = p.Pulse, firstInkPixel = first == int.MinValue ? (int?)null : first - p.Fire, ok });
            }
        }
        bool glowOk = true, envelopeOk = true;
        var envelopeRows = new List<object>();
        foreach (string name in Signatures.Concat(Basics))
        {
            var s = Scene(name, "C");
            foreach (var (p, label) in new[] { (s.Third, "n3"), (s.Flow, "crossflow") })
            {
                // The drawn body glow the material adds (sum of luma(Game) - luma(Plain) where positive) around this
                // note's strike, Fire-10 .. Fire+12 (the previous note fired one to one and a half beats earlier).
                float best = -1; int at = 0;
                for (int tick = p.Fire - 10; tick <= p.Fire + 12; tick++)
                {
                    var view = s.View(r.Device, o.Width, o.Height, tick, false);
                    var off = Pixels(s, view, Plain, RigLayers.Apparition, Color.Transparent);
                    var on = Pixels(s, view, Game, RigLayers.Apparition, Color.Transparent);
                    float glow = 0;
                    for (int i = 0; i < on.Length; i++) glow += MathF.Max(0, Y(on[i]) - Y(off[i]));
                    if (glow > best) { best = glow; at = tick; }
                }
                bool ok = at - p.Fire is >= 0 and <= 2;
                glowOk &= ok;
                glowRows.Add(new { scene = name, note = label, bodyGlowPeak = at - p.Fire, ok });
                // S1's envelopes for the body (as DrawEffigy computes them): Ignite rises on Fire, Send is 1 on Fire, Heat peaks in [Fire-3, Fire].
                var e = Envelopes(s, p.Fire - 10, p.Fire + 8);
                int rise = 0; float riseBest = float.MinValue, heatBest = float.MinValue; int heatAt = 0;
                for (int k = 1; k < e.Count; k++) if (e[k].Ignite - e[k - 1].Ignite > riseBest) { riseBest = e[k].Ignite - e[k - 1].Ignite; rise = e[k].Tick; }
                foreach (var x in e) if (x.Heat > heatBest + 1e-6f) { heatBest = x.Heat; heatAt = x.Tick; }
                // Send: the Choir's struck limbs as drawn; an apparition draws the warning's Send as Run up to Fire,
                // then the whip, so its own note's Send(p) is read at p = 1.
                float send = s.Phase == 2 ? e.Find(x => x.Tick == p.Fire).Send : ScarletEnvelope.Send(ScarletEnvelope.Progress(p.Fire, p.Born, p.Fire));
                bool envOk = rise == p.Fire && send >= .999f && heatAt - p.Fire is >= -3 and <= 0;
                envelopeOk &= envOk;
                envelopeRows.Add(new { scene = name, note = label, igniteRise = rise - p.Fire, sendAtFire = send, heatPeak = heatAt - p.Fire, ok = envOk });
            }
        }
        Add("G6", "timing: sounds on Born/Fire, first ink pixel = Fire +/- 1, body glow peak in [Fire, Fire+2], Ignite rises on Fire, Send 1 on Fire, Heat peaks in [Fire-3, Fire]",
            Status(soundOk && inkOk && glowOk && envelopeOk),
            new { sounds = soundRows, ink = inkRows, bodyGlow = glowRows, envelopes = envelopeRows },
            "Sound ticks mirror CrimsonGestureVisuals.Cue (one voice per phrase/tick/cue, no cue for a curtain with nothing to burn). Body glow: camera C, the light the material adds, "
            + "third note and crossflow of every Act I-III phrase. Envelopes: the aggregated body state (Crown/Mantle; the Choir's struck limbs for Send).");
    }

    // ---- G7: Reduced Effects keeps every vertex -----------------------------------------------------------------
    private void G7()
    {
        var rows = new List<object>(); bool ok = true;
        var energyRows = new List<object>(); bool energyOk = true;
        var particleRows = new List<object>(); bool particleOk = true;
        var reducedGame = Game with { Reduced = true }; var reducedPlain = Plain with { Reduced = true };
        foreach (string name in Signatures.Concat(Basics))
        {
            var s = Scene(name, "C");
            var third = s.Third;
            double normalEnergy = 0, reducedEnergy = 0;
            foreach (int tick in new[] { third.Born + 6, third.Born + 14, third.Fire, third.Fire + 2, third.Fire + 3, third.Fire + 10, third.End })
            {
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                var normal = Mesh(s, view, Game); var reduced = Mesh(s, view.At(tick) with { Reduced = true }, reducedGame);
                float max = normal.Count == reduced.Count && normal.Count > 0 ? normal.Zip(reduced, (a, b) => Vector2.Distance(a, b)).Max() : float.PositiveInfinity;
                ok &= max <= 1e-3f;
                rows.Add(new { scene = name, rel = tick - third.Fire, vertices = normal.Count, reducedVertices = reduced.Count, maxShift = float.IsFinite(max) ? max : -1 });
                normalEnergy += Energy(s, view, Plain, Game);
                reducedEnergy += Energy(s, view.At(tick) with { Reduced = true }, reducedPlain, reducedGame);
            }
            float ratio = normalEnergy > 0 ? (float)(reducedEnergy / normalEnergy) : 0;
            energyOk &= ratio <= .40f;
            // Where the energy is: the same sums with one pass drawn at a time (third note, Fire and Fire+2).
            var split = new Dictionary<string, double[]>();
            foreach (string pass in new[] { "AutoloadPass", "AuraPass", "RibbonPass", "HeartPass", "SparkPass", "DripPass", "WispPass" })
            {
                double n0 = 0, r0 = 0;
                RigHost.PassFilter = (_, applied) => applied == pass;
                try
                {
                    foreach (int tick in new[] { third.Fire, third.Fire + 2 })
                    {
                        var view = s.View(r.Device, o.Width, o.Height, tick, false);
                        n0 += Energy(s, view, Plain, Game);
                        r0 += Energy(s, view.At(tick) with { Reduced = true }, reducedPlain, reducedGame);
                    }
                }
                finally { RigHost.PassFilter = null; }
                if (n0 + r0 > 0) split[pass] = new[] { Math.Round(n0), Math.Round(r0) };
            }
            energyRows.Add(new { scene = name, normalEnergy, reducedEnergy, ratio, byPassAtFire = split });
            // No particle born on Fire under Reduced (sparks, embers, intake, drips): every tick of the third note's strike.
            int particles = 0;
            for (int tick = third.Fire; tick <= third.Fire + 20; tick++)
            {
                var view = s.View(r.Device, o.Width, o.Height, tick, true);
                RigHost.ResetCounts();
                r.Render(s, view, reducedGame, RigLayers.Apparition, r.Frame(view.Width, view.Height), Color.Transparent);
                foreach (var (key, c) in RigHost.PassCounts) if (key.EndsWith(".SparkPass") || key.EndsWith(".DripPass")) particles += c.Vertices;
            }
            particleOk &= particles == 0;
            particleRows.Add(new { scene = name, particleVertices = particles });
        }
        Add("G7", "Reduced: skinned body vertices identical to Normal, decoration energy <= 40% of Normal, no Fire-born particles", Status(ok && energyOk && particleOk),
            new { vertices = rows, energy = energyRows, particles = particleRows },
            "In-game variant (material on, proposed motion). Vertices from every AutoloadPass draw of ScarletApparitions / ScarletChoir (the skinned mesh) through the device shim. "
            + "Energy = sum over pixels of |luma(on) - luma(off)| of the material on the apparition layer, camera C, at Born+6..End of the third note. "
            + "Particles = SparkPass / DripPass vertices under Reduced over Fire..Fire+20.");
    }

    // ---- G8: with no notes the bodies are today's picture --------------------------------------------------------
    private void G8()
    {
        string dir = BaselineDir("g8");
        RigBaseline.Manifest? manifest = null;
        string? refused = o.WriteBaseline ? null : RigBaseline.Refuse(root, dir, "g8", out manifest);
        if (refused is not null) { Add("G8", "no notes: Crown/Choir within 1/255, Vespera and companion identical, Mantle by eye", "not_run", null, refused); return; }
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Signatures)
        {
            var s = Scene(name, "C");
            var empty = Idle(s);
            foreach (int offset in new[] { 0, 37, 91 })
            {
                int tick = IdleBase[name] + offset; // the rest between phrases: no gesture alive; the clock the baselines used
                var view = empty.View(r.Device, o.Width, o.Height, tick, false);
                var px = Pixels(empty, view, Game, RigLayers.Apparition, Color.Transparent);
                rows.Add(Check($"{name}-apparition-{offset}", px, view.Width, view.Height, name.StartsWith("act2") ? -1 : 1));
            }
        }
        {
            var s = Idle(Scene("act1-signature", "V"));
            foreach (int offset in new[] { 0, 53 })
            {
                int tick = IdleBase["act1-signature"] + offset;
                var view = s.View(r.Device, o.Width, o.Height, tick, false);
                rows.Add(Check($"vespera-{offset}", Pixels(s, view, Game, RigLayers.Vespera, Color.Transparent), view.Width, view.Height, 0));
                rows.Add(Check($"companion-{offset}", Companion(view, tick), view.Width, view.Height, 0));
            }
        }
        if (o.WriteBaseline) RigBaseline.Write(root, dir, "g8", "today's idle Crown / Mantle / Choir / Vespera / companion frames (G8)");
        Add("G8", "no notes: Crown/Choir within 1/255, Vespera and companion identical, Mantle by eye", o.WriteBaseline ? "baseline_written" : Status(ok), rows,
            o.WriteBaseline ? "Idle frames written to " + dir + " with their manifest; run the gates on the change to compare."
                : "The in-game variant (material on, proposed motion, Vespera's command) with no gesture alive, compared with the idle frames in "
                  + Path.GetRelativePath(output, dir) + $" (rendered from {manifest!.Revision}{(manifest.Dirty ? ", dirty" : "")}, {manifest.Written}).");

        object Check(string label, Color[] px, int w, int h, int tolerance)
        {
            string file = Path.Combine(dir, label + ".rgba.gz");
            if (o.WriteBaseline) { Directory.CreateDirectory(dir); SaveRaw(px, w, h, file); return new { label, written = true }; }
            if (!File.Exists(file)) { ok = false; return new { label, missing = true }; }
            var basePx = LoadRaw(file, w, h);
            // Review aid (the Mantle is judged by eye): baseline | now, side by side.
            if (Dumps is not null)
            {
                var pair = new Color[w * 2 * h];
                for (int y = 0; y < h; y++) { Array.Copy(basePx, y * w, pair, y * w * 2, w); Array.Copy(px, y * w, pair, y * w * 2 + w, w); }
                Dump($"g8-{label}.png", pair, w * 2, h);
            }
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
            var second = s.Second;
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
                if (s.Phase == 2) worst = Math.Max(worst, ArmGap(s.Plans, shifted, t, a, s.Flipped));
                worst = Math.Max(worst, AttackGap(s, shifted, t));
            }
            bool soundsShift = RigMirror.Sounds(s.Plans).Select(e => e.Tick + 7).SequenceEqual(RigMirror.Sounds(shifted).Select(e => e.Tick));
            ok &= same && worst <= 1e-5f && soundsShift;
            rows.Add(new { scene = name, frames = ticks.Length, sequentialEqualsJump = same, shiftWorstInputDelta = worst, soundsShift });
        }
        Add("G9", "determinism: sequential = jump render; plans +7 ticks shift every attack input by 7", Status(ok), rows,
            "Pixels compared by SHA-256 of the full frame (labels off). The idle oscillations read the absolute clock by design (today's rigs); the attack inputs (Signal, the Choir cues, S1's notes, motion and body material) read only the plans.");

        float ArmGap(CrimsonGesturePlan[] plans, CrimsonGesturePlan[] shifted, int t, (float Charge, float Recoil) signal, bool flipped)
        {
            Span<CrimsonChoirCue> a = stackalloc CrimsonChoirCue[16];
            Span<CrimsonChoirCue> b = stackalloc CrimsonChoirCue[16];
            int na = RigMirror.ChoirCues(plans, t, flipped, a), nb = RigMirror.ChoirCues(shifted, t + 7, flipped, b);
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

        // S1's attack clock (CrimsonRig.DrawEffigy's notes -> ScarletGestureMotion / ScarletBodyMaterial): every channel
        // at t over the plans equals the same channel at t + 7 over the shifted plans.
        static float AttackGap(RigScene s, CrimsonGesturePlan[] shifted, int t)
        {
            Span<ScarletNote> a = stackalloc ScarletNote[ScarletNotes.Capacity];
            Span<ScarletNote> b = stackalloc ScarletNote[ScarletNotes.Capacity];
            int na = RigMirror.Notes(s.Plans, s.Phase, t, s.Flipped, a), nb = RigMirror.Notes(shifted, s.Phase, t + 7, s.Flipped, b);
            if (na != nb) return float.PositiveInfinity;
            float[] x, y;
            if (s.Phase == 2)
            {
                x = Choir(ScarletBodyMaterial.Choir(t, a[..na], false));
                y = Choir(ScarletBodyMaterial.Choir(t + 7, b[..nb], false));
            }
            else
            {
                var ma = s.Phase == 0 ? ScarletGestureMotion.Crown(t, a[..na]) : ScarletGestureMotion.Mantle(t, a[..na]);
                var mb = s.Phase == 0 ? ScarletGestureMotion.Crown(t + 7, b[..nb]) : ScarletGestureMotion.Mantle(t + 7, b[..nb]);
                x = Body(ma, ScarletBodyMaterial.Apparition(t, a[..na], ma, s.Flipped, false));
                y = Body(mb, ScarletBodyMaterial.Apparition(t + 7, b[..nb], mb, s.Flipped, false));
            }
            float gap = 0;
            for (int i = 0; i < x.Length; i++) gap = Math.Max(gap, Math.Abs(x[i] - y[i]));
            return gap;

            static float[] Body(in ScarletApparitionMotion m, in ScarletBodyState c) => new[]
            {
                m.OffsetX, m.OffsetY, m.Turn, m.Flare, m.Kick, m.Sweep, m.Row,
                c.Heat, c.Ignite, c.Front, c.Drain, c.Lean, c.PourLimit, c.Run, c.Row, c.Swing, c.Wind, c.Send, c.Return, c.Snap, c.Engaged
            };
            static float[] Choir(in ScarletChoirState c)
            {
                var list = new List<float> { c.Heat, c.Ignite, c.Drain, c.Surge, c.Engaged };
                for (int arm = 0; arm < 4; arm++) { var l = c.Limb(arm); list.AddRange(new[] { l.Lift, l.Send, l.Return, l.Tear, l.Burst }); }
                return list.ToArray();
            }
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
                try { r.Render(ground, view, Game, RigLayers.Apparition, world, Color.Transparent); }
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
        // The tracking beams of the Act I phrases, the production ink against the reference class (main's ScarletInkStroke before
        // the signature moves, carrying the same quad-margin rule ScarletInkMargin.Of: the rule is the one edit to main's class):
        // live at Fire+2 and +10, residue 5 ticks after End. The crossflow's stream is no longer main's capsule (it is cut square
        // on its ends, owner 2026-10-04), so it has its own check below.
        var frames = new List<object>(); bool frameOk = true; int checkedFrames = 0;
        foreach (string name in new[] { "act1-basic", "act1-basic2" })
        {
            var s = Scene(name, "A2");
            foreach (var p in s.Phrase.Plans.Where(q => q.Technique == CrimsonTechnique.TrackingBeam))
                foreach (int rel in new[] { 2, 10, p.End - p.Fire + 5 })
                {
                    int tick = p.Fire + rel;
                    var view = s.View(r.Device, o.Width, o.Height, tick, false);
                    var production = InkOnly(s, view, p, r.Frame(view.Width, view.Height));
                    var reference = InkOnlyReference(view, p, r.Frame(view.Width, view.Height));
                    bool same = production.AsSpan().SequenceEqual(reference);
                    frameOk &= same; checkedFrames++;
                    frames.Add(new { label = $"{name}-{p.Technique}-p{p.Pulse}-fire+{rel}", identical = same, lit = production.Count(c => Lit(c, 3)) });
                }
        }
        var crossflow = Crossflow(out bool crossflowOk);
        Add("G11", "ink regression: ScarletInk (and the other approved field shaders) unchanged; the tracking beams' ink identical to main's ScarletInkStroke; the crossflow stream cut square on its ends",
            Status(hashOk && frameOk && crossflowOk),
            new { hashes, framesChecked = checkedFrames, frames, crossflow },
            "Pinned shader hashes, and every live-strike and residue frame of the Act I basic phrases' tracking beams (cells A and B) "
            + "drawn by the production ScarletInkStroke and by ScarletInkStrokeReference (tools/fixtures/ScarletInkReference.cs, a copy of main's class before "
            + "the signature moves that carries the quad-margin rule, max(10, ceil(.4 R + 9)) px, as its one edit; the rule is copied there, not shared) must be pixel for pixel identical, so the signature moves' "
            + "changes to the class cannot reach a basic strike. "
            + "The seal crossflow (the pickup's and the closing one, in the open and with a stream end on each wall) is drawn as a band cut square on its two "
            + "ends and on its growing front, not as main's capsule with round ends: no ink beyond the capsule's span along the stream or beyond its radius + the quad's margin across it, "
            + "and a live column 2.5 px inside either cut as full as the middle of the stream (a round end would be a sliver); the last pixel rows inside the quad's edge across the band carry "
            + "at most " + EdgeLimit + "/255 of glow (the glow fades out before the quad's edge instead of being cut into a lighter box; live, half-width >= 20 px, away from the cut ends); and every pixel more than 1 px inside the collision "
            + "capsule (CrimsonTechniqueGeometry.Write at that tick) carries ink, so the picture covers what hurts. sha256 is recorded per frame, not gated.");
    }

    // The quad edge's glow: 0..255 of the brightest channel in the last pixel rows inside the quad, across the live crossflow. A flat 10 px
    // margin leaves ~60 there (a lighter box), a margin fitted to the halo at R = r (.32 r + 7 or .4 r + 9) 19-22 at F+3, where the
    // shader's body overshoots its capsule by 14%; the shipped .6 r + 9 leaves 7 at most (F+3) and 1 from F+7 on. The limit is set between.
    private const int EdgeLimit = 10;

    // The seal crossflow's ink at real size (zoom 1, centred on the stream), 1:1 over transparent black.
    private List<object> Crossflow(out bool ok)
    {
        ok = true;
        var rows = new List<object>();
        const int width = 1500, height = 560;
        var strokes = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (string name in new[] { "act1-basic", "act1-basic2" })
        {
            var s = Scene(name, "A2");
            foreach (var plan in s.Phrase.Plans.Where(q => q.Technique == CrimsonTechnique.SideBeams))
                foreach (var (where, p) in new[]
                {
                    ("scene", plan),
                    ("wallL", plan with { Target = new(plan.Field.Left + 120, plan.Target.Y) }),
                    ("wallR", plan with { Target = new(plan.Field.Right - 120, plan.Target.Y) })
                })
                {
                    var (right, left) = CrimsonChoreography.Reach(p);
                    var centre = new Vector2((right.X + left.X) * .5f, right.Y);
                    foreach (int rel in new[] { 1, 2, 3, 7, 12, 30, 44, 48, 50, 53, p.End - p.Fire + 5 })
                    {
                        int tick = p.Fire + rel;
                        bool live = tick < p.End;
                        var view = ScarletView.Create(r.Device, width, height, centre - new Vector2(width, height) * .5f, 1f, tick);
                        int count = CrimsonTechniqueGeometry.Write(p, live ? tick : p.End - 1, strokes, false);
                        if (count != 1) { ok = false; rows.Add(new { scene = name, where, rel, error = "strokes " + count }); continue; }
                        var stroke = strokes[0];
                        float hi = MathF.Max(stroke.A.X, stroke.B.X) + stroke.Radius, lo = MathF.Min(stroke.A.X, stroke.B.X) - stroke.Radius;
                        float y = stroke.A.Y, extent = stroke.Radius + ScarletInkMargin.Of(stroke.Radius), reach = extent + 2;
                        // The quad's edge across the band: the last pixel rows inside it, away from the cut ends, must carry (almost) no glow, or the
                        // edge shows as a lighter box (the 10 px margin left up to a quarter of the glow's peak there). Live frames of a stream at least 20 px wide.
                        bool edgeFrame = live && stroke.Radius >= 20;
                        int edgeMax = 0, edgeCount = 0; long edgeSum = 0;
                        var pixels = InkOnly(s, view, p, r.Frame(width, height));
                        int lit = 0, outsideAlong = 0, outsideAcross = 0, hurts = 0, bare = 0;
                        float bareDepth = 0;
                        var column = new Dictionary<int, int>();
                        for (int py = 0; py < height; py++)
                            for (int px = 0; px < width; px++)
                            {
                                // The picture covers the collision: a pixel centre more than 1 px inside the capsule (what hurts, the
                                // authority's own Write at this tick) must carry ink. bareDepth is how deep inside the worst bare pixel is.
                                if (live)
                                {
                                    float cx = view.ScreenPosition.X + px + .5f, cy = view.ScreenPosition.Y + py + .5f;
                                    float vx = stroke.B.X - stroke.A.X, along = vx == 0 ? 0 : Math.Clamp((cx - stroke.A.X) / vx, 0, 1);
                                    float dist = MathF.Sqrt((cx - (stroke.A.X + vx * along)) * (cx - (stroke.A.X + vx * along)) + (cy - stroke.A.Y) * (cy - stroke.A.Y));
                                    if (dist <= stroke.Radius - 1)
                                    {
                                        hurts++;
                                        if (!Lit(pixels[py * width + px], 3)) { bare++; bareDepth = MathF.Max(bareDepth, stroke.Radius - dist); }
                                    }
                                }
                                float wx = view.ScreenPosition.X + px + .5f, wy = view.ScreenPosition.Y + py + .5f;
                                if (edgeFrame && MathF.Abs(wy - y) <= extent && MathF.Abs(wy - y) > extent - 1.5f && wx > lo + 60 && wx < hi - 60)
                                {
                                    var e = pixels[py * width + px];
                                    int level = Math.Max(Math.Max(e.R, e.G), Math.Max(e.B, e.A));
                                    edgeMax = Math.Max(edgeMax, level); edgeSum += level; edgeCount++;
                                }
                                if (!Lit(pixels[py * width + px], 3)) continue;
                                lit++;
                                if (wx < lo - 1.5f || wx > hi + 1.5f) outsideAlong++;
                                if (MathF.Abs(wy - y) > reach) outsideAcross++;
                                int c = (int)MathF.Floor(wx); column[c] = column.TryGetValue(c, out var n) ? n + 1 : 1;
                            }
                        int Column(float wx) => column.TryGetValue((int)MathF.Floor(wx), out var n) ? n : 0;
                        // Square ends: the columns 2.5 px inside the cuts are as full as the middle of the stream (live only: a scar is uneven).
                        float middle = Math.Max(1, Column((hi + lo) * .5f));
                        float atRight = Column(hi - 2.5f) / middle, atLeft = hi - lo > 12 ? Column(lo + 2.5f) / middle : 1;
                        // (A sliver under 10 px of half-width, the first tick of the opening, has too few lit pixels per column for the ratio.)
                        bool square = !live || stroke.Radius < 10 || atRight >= .85f && atLeft >= .85f;
                        bool softEdge = !edgeFrame || edgeMax <= EdgeLimit;
                        bool good = lit > 0 && outsideAlong == 0 && outsideAcross == 0 && square && bare == 0 && softEdge;
                        ok &= good;
                        using var sha = SHA256.Create();
                        var bytes = new byte[pixels.Length * 4];
                        for (int i = 0; i < pixels.Length; i++) { bytes[i * 4] = pixels[i].R; bytes[i * 4 + 1] = pixels[i].G; bytes[i * 4 + 2] = pixels[i].B; bytes[i * 4 + 3] = pixels[i].A; }
                        rows.Add(new { scene = name, where, pulse = p.Pulse, rel, live, span = new[] { lo, hi }, radius = stroke.Radius, lit, outsideAlong, outsideAcross, collisionPixels = hurts, collisionPixelsWithoutInk = bare, bareDepth,
                            columnAtRightCut = atRight, columnAtLeftCut = atLeft,
                            quadEdgeMax = edgeFrame ? edgeMax : (int?)null, quadEdgeMean = edgeFrame && edgeCount > 0 ? (double)edgeSum / edgeCount : (double?)null, ok = good, sha256 = Convert.ToHexString(sha.ComputeHash(bytes))[..16].ToLowerInvariant() });
                    }
                }
        }
        return rows;
    }

    // ---- G12: draw / vertex budget -----------------------------------------------------------------------------
    private void G12()
    {
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Signatures.Concat(Basics))
        {
            var s = Scene(name, "C");
            string tag = s.Phase switch { 0 => "crown", 1 => "mantle", _ => "choir" };
            var budget = Budget[tag];
            foreach (bool reduced in new[] { false, true })
            {
                int minDraws = int.MaxValue, maxDraws = 0, maxVertices = 0, worstTick = 0, frames = 0;
                var passMax = new Dictionary<string, int>();
                var target = r.Frame(o.Width, o.Height);
                for (int tick = s.First; tick <= s.Last; tick++, frames++)
                {
                    var view = s.View(r.Device, o.Width, o.Height, tick, reduced);
                    RigHost.ResetCounts();
                    r.Render(s, view, Game with { Reduced = reduced }, RigLayers.Apparition, target, Color.Transparent);
                    var c = RigHost.Counts.TryGetValue(tag, out var found) ? found : default;
                    minDraws = Math.Min(minDraws, c.Draws);
                    if (c.Draws > maxDraws) { maxDraws = c.Draws; worstTick = tick; }
                    maxVertices = Math.Max(maxVertices, c.Vertices);
                    foreach (var (key, pc) in RigHost.PassCounts)
                    {
                        if (!key.StartsWith(tag + "/")) continue;
                        string passName = key[(key.IndexOf('.') + 1)..];
                        passMax[passName] = Math.Max(passMax.TryGetValue(passName, out var m) ? m : 0, pc.Vertices);
                    }
                }
                int budgetDraws = reduced ? budget.Reduced : budget.Normal, today = reduced ? budget.TodayVerticesReduced : budget.TodayVertices;
                bool pass = maxDraws <= budgetDraws && maxVertices - today <= budget.Added;
                ok &= pass;
                rows.Add(new
                {
                    body = tag, scene = name, reduced, frames, draws = new[] { minDraws, maxDraws }, budgetDraws, worstDrawTick = worstTick - s.Phrase.FirstBorn,
                    vertices = maxVertices, todayVertices = today, addedVertices = maxVertices - today, budgetAdded = budget.Added,
                    elements = Elements[tag].Select(x => new { x.Pass, max = passMax.TryGetValue(x.Pass, out var v) ? v : 0, design = x.Vertices }).ToArray(),
                    passes = passMax, pass
                });
            }
        }
        Add("G12", "draws / vertices per body within §2.7 (every tick of every phrase, Normal and Reduced)", Status(ok), rows,
            "In-game variant through the device shim, camera C. Gate: max draws <= §2.7 and per-frame vertices - today's <= §2.7's added vertices. "
            + "elements lists the design's per-element numbers beside the measured maxima (not gated: the Mantle's 84 spark vertices cannot hold today's 16 free sparks, 96 vertices, which it now draws in the same batch).");
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
            var result = new PreviewContract.Result();
            foreach (int tick in new[] { s.Second.Born + 10, s.Second.Fire + 5, s.Third.Fire + 2, s.Flow.Fire + 14, s.Notes[^1].End + 6 })
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
        list.Add(new("performer", "Vespera (DrawPerformer, poses, pivots, the plan-list Signal) is CrimsonRig.Performer.cs linked unchanged", "pass", false, RigProvenance.Performer, ""));
        foreach (var g in list) Console.WriteLine($"self  {g.Status,-17} {g.Title}");
        return list;
    }

    // ---- helpers -------------------------------------------------------------------------------------------------
    internal static bool BodyOnly(string shader, string pass) => shader is "ScarletApparitions" or "ScarletChoir" && pass == "AutoloadPass";

    // Premultiplied output: an additive glow leaves alpha at 0 and still lights the pixel.
    private static bool Lit(Color c, int threshold) => Math.Max(Math.Max(c.R, c.G), Math.Max(c.B, c.A)) > threshold;

    private Color[] Pixels(RigScene s, in ScarletView view, in RigVariant v, RigLayers layers, Color clear)
    {
        var target = r.Frame(view.Width, view.Height);
        r.Render(s, view, v, layers, target, clear);
        return RigRenderer.Read(target);
    }

    private string Hash(RigScene s, int tick)
    {
        var view = s.View(r.Device, o.Width, o.Height, tick, false);
        var px = Pixels(s, view, Game, RigLayers.Frame & ~RigLayers.Labels, Color.Black);
        return Convert.ToHexString(SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(px.AsSpan())));
    }

    // Skinned-mesh alpha > .05 at every lag 0..21 (true past poses) and at the design's six lags, each dilated by
    // 12 px at zoom 1.
    private (bool[] Range, bool[] DesignRange) BodyRanges(RigScene s, in ScarletView view, in RigVariant v)
    {
        var union = new bool[view.Width * view.Height]; var design = new bool[view.Width * view.Height];
        RigHost.PassFilter = BodyOnly;
        try
        {
            foreach (int lag in Lags)
            {
                var px = Pixels(s, view.At(view.Tick - lag), v, RigLayers.Apparition, Color.Transparent);
                bool sampled = Array.IndexOf(DesignLags, lag) >= 0;
                for (int i = 0; i < px.Length; i++) { bool a = px[i].A > 12; union[i] |= a; if (sampled) design[i] |= a; }
            }
        }
        finally { RigHost.PassFilter = null; }
        return (Dilate(union, view.Width, view.Height, RangeDilate * view.Zoom), Dilate(design, view.Width, view.Height, RangeDilate * view.Zoom));
    }

    // The body's own silhouette at this tick (skinned mesh alpha > .05), dilated by `dilate` px at zoom 1.
    private bool[] BodyMask(RigScene s, in ScarletView view, in RigVariant v, float dilate)
    {
        Color[] px;
        RigHost.PassFilter = BodyOnly;
        try { px = Pixels(s, view, v, RigLayers.Apparition, Color.Transparent); }
        finally { RigHost.PassFilter = null; }
        var mask = new bool[px.Length];
        for (int i = 0; i < px.Length; i++) mask[i] = px[i].A > 12;
        return dilate > 0 ? Dilate(mask, view.Width, view.Height, dilate * view.Zoom) : mask;
    }

    // Light `b` adds over `a` (any channel up by more than `threshold`) and light it only takes away.
    private (bool[] Added, bool[] Dimmed) Change(RigScene s, in ScarletView view, RigLayers layers, in RigVariant a, in RigVariant b, int threshold)
    {
        var x = Pixels(s, view, a, layers, Color.Transparent); var y = Pixels(s, view, b, layers, Color.Transparent);
        var added = new bool[x.Length]; var dimmed = new bool[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            added[i] = Added(y[i], x[i]) > threshold;
            dimmed[i] = !added[i] && Added(x[i], y[i]) > threshold;
        }
        return (added, dimmed);
    }
    private static int Added(Color on, Color off) => Math.Max(Math.Max(on.R - off.R, on.G - off.G), Math.Max(on.B - off.B, on.A - off.A));
    private static float Y(Color c) => .2126f * c.R + .7152f * c.G + .0722f * c.B;

    // Sum of |luma(b) - luma(a)| on the apparition layer (the material's decoration energy).
    private double Energy(RigScene s, in ScarletView view, in RigVariant a, in RigVariant b)
    {
        var x = Pixels(s, view, a, RigLayers.Apparition, Color.Transparent); var y = Pixels(s, view, b, RigLayers.Apparition, Color.Transparent);
        double sum = 0;
        for (int i = 0; i < x.Length; i++) sum += MathF.Abs(Y(y[i]) - Y(x[i]));
        return sum;
    }

    // S1's body envelopes per tick, as CrimsonRig.DrawEffigy computes them (the Choir: Send of its most advanced limb).
    private static List<(int Tick, float Heat, float Ignite, float Send)> Envelopes(RigScene s, int from, int to)
    {
        var list = new List<(int, float, float, float)>();
        Span<ScarletNote> notes = stackalloc ScarletNote[ScarletNotes.Capacity];
        for (int t = from; t <= to; t++)
        {
            int n = RigMirror.Notes(s.Plans, s.Phase, t, s.Flipped, notes);
            if (s.Phase == 2)
            {
                var c = ScarletBodyMaterial.Choir(t, notes[..n], false);
                float send = 0;
                for (int arm = 0; arm < 4; arm++) send = Math.Max(send, c.Limb(arm).Send);
                list.Add((t, c.Heat, c.Ignite, send));
            }
            else
            {
                var m = s.Phase == 0 ? ScarletGestureMotion.Crown(t, notes[..n]) : ScarletGestureMotion.Mantle(t, notes[..n]);
                var b = ScarletBodyMaterial.Apparition(t, notes[..n], m, s.Flipped, false);
                list.Add((t, b.Heat, b.Ignite, b.Send));
            }
        }
        return list;
    }


    // Inside the field, outside every displayed footprint (signature and basic, warning to residue), plus the central
    // 94 px band (empty when nothing is displayed, apart from that band).
    private bool[] SafeZones(RigScene s, in ScarletView view, int tick)
    {
        var field = PreviewPlanner.Field;
        var shown = new List<CrimsonStroke>();
        var buffer = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var p in s.Plans)
        {
            if (tick < p.Born || tick >= p.End + ScarletGeometryOverlay.ResidueTicks(p)) continue;
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

    // The same plan through the reference class (main's ScarletInkStroke).
    private Color[] InkOnlyReference(in ScarletView view, in CrimsonGesturePlan p, RenderTarget2D target)
    {
        CrimsonVisuals.Reduced = view.Reduced;
        r.Device.SetRenderTarget(target);
        r.Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1f, 0);
        new ScarletInkStrokeReference().Draw(view, r.Assets, p);
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
            CrimsonRig.DrawPerformer(batch, view.ScreenPosition, RigScene.Conductor, tick, Vector2.Zero, 1, false, 0, 0);
            batch.End();
        }
        r.Device.SetRenderTarget(null);
        return RigRenderer.Read(target);
    }

    // SCARLET_GATE_DUMPS=<dir>: frames behind a gate's count, for looking (G1 outside-range decoration, G3 over-limit pixels).
    private static readonly string? Dumps = Environment.GetEnvironmentVariable("SCARLET_GATE_DUMPS") is { Length: > 0 } d ? d : null;
    private void Dump(string name, Color[] px, int w, int h)
    {
        Directory.CreateDirectory(Dumps!);
        using var texture = new Texture2D(r.Device, w, h);
        texture.SetData(px);
        using var file = File.Create(Path.Combine(Dumps!, name));
        texture.SaveAsPng(file, w, h);
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

    // Morphological closing: fills holes and gaps narrower than 2 x radius.
    internal static bool[] Close(bool[] mask, int w, int h, float radius)
    {
        var grown = Dilate(mask, w, h, radius);
        for (int i = 0; i < grown.Length; i++) grown[i] = !grown[i];
        var shrunk = Dilate(grown, w, h, radius);
        for (int i = 0; i < shrunk.Length; i++) shrunk[i] = !shrunk[i];
        return shrunk;
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
