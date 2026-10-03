// S4 gates for Vespera's command (design §2.5) over the offline rig harness (tools/preview-scarlet-rigs.ps1).
// Vespera is drawn by the production files only: RigDriver.Vespera -> CrimsonRig.DrawConductor (the boss path of
// CrimsonRig.Draw) and CrimsonRig.DrawPerformer (the companion), both in the linked CrimsonRig.Performer.cs. Every
// tick of each Act's signature and basic phrase (with the neighbouring phrases alive) is checked, Normal and Reduced.
//   G8v  identity: with no note (the plans alive, the command off) every tick draws exactly today's Vespera and held
//        orb, compared pixel for pixel with the pre-S4 boss path kept below as TodayVespera (the harness's own drawing
//        before S4: DrawPerformer with today's arguments and the held orb at today's offset and radius), so it holds under
//        any phrase timing; the companion's performer (default arguments) is byte-identical over its charge / recoil /
//        facing / floating / motion range, SHA-256 against a baseline written from the code before S4.
//   V94  the held orb's centre is never closer to her than 94 px nor than today's hold, moves at most 24 px, and its
//        near edge (centre - radius toward her) never comes closer to her than today's near edge.
//   G2v  central 94 px gap (|x - Cx| <= 47, the Hands' gap she stands in): the command adds no light outside her own
//        sprite (alpha, dilated 12 px) and today's orb footprint. Light it adds inside today's footprint is measured.
//   G6v  timing from the notes only: the orb's Ignite rises on Fire, the release (Snap) peaks in [Fire, Fire + 3],
//        Heat peaks in [Fire - 3, Fire], the orb is drawn back against the aim before Fire and moves toward it after;
//        the orb's drawn light starts rising on the Fire tick and rises most within [Fire, Fire + 2] (no new flare on
//        Fire: the reactor keeps today's impulse). Plans shifted by +7 ticks shift every command channel by exactly 7
//        (no beat clock).
//   G7v  Reduced: same timing, displacement / radius change / rim boost at most half of Normal.
// Offline review only: not a playtest; in-game acceptance stays not_run.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Convergence.Client.Encounters.CrimsonFoundry;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed class ConductorGates
{
    internal static readonly string[] Scenes = { "act1-signature", "act2-signature", "act3-signature", "act1-basic", "act2-basic", "act3-basic",
        "act1-basic2", "act2-basic2", "act3-basic2" };
    // Vespera's crop at the in-game zoom 2 (camera V): world x in [Cx - 260, Cx + 120], y in [Cy - 150, Cy + 90].
    internal const int CropWidth = 760, CropHeight = 480;
    internal static readonly Vector2 CropCentre = new(-70, -30);

    private readonly RigRenderer r;
    private readonly RigOptions o;
    private readonly string output;
    private readonly List<RigGate> gates = new();
    private readonly Dictionary<string, RigScene> scenes = new();
    private RenderTarget2D? crop;

    private readonly string root;
    internal ConductorGates(RigRenderer renderer, RigOptions options, string root, string output) { r = renderer; o = options; this.root = root; this.output = output; }

    internal List<RigGate> Run()
    {
        Identity(); Distance(); CentralGap(); Timing(); ReducedHalf();
        if (Environment.GetEnvironmentVariable("SCARLET_CONDUCTOR_SHEETS") is { Length: > 0 } sheets) Sheets(sheets);
        foreach (var g in gates) Console.WriteLine($"{g.Id,-5} {g.Status,-17} {g.Title}");
        return gates;
    }

    // Review crops (SCARLET_CONDUCTOR_SHEETS=<dir>): the composited frame (backdrop, Vespera, forecasts / ink, players,
    // mask) at zoom 2 around Vespera, today's and commanded, Normal and Reduced, at the third note's and the crossflow's
    // key ticks. <scene>/<variant>/<label>.png, labels relative to the note's Fire.
    private void Sheets(string dir)
    {
        foreach (string name in Scenes)
        {
            var s = Scene(name);
            var third = s.Third; var flow = s.Flow;
            var ticks = new SortedDictionary<int, string>();
            foreach (int rel in new[] { -28, -24, -20, -14, -8, -3, -1, 0, 1, 2, 3, 5, 8, 12, 18, 26 }) ticks.TryAdd(third.Fire + rel, $"n3{rel:+0;-0;+0}");
            foreach (int rel in new[] { -56, -40, -28, -14, -4, -1, 0, 1, 2, 4, 7, 14, 30, 52 }) ticks.TryAdd(flow.Fire + rel, $"xf{rel:+0;-0;+0}");
            foreach (var (variant, label) in new[] { (Current, "today"), (Commanded, "command"), (Current with { Reduced = true }, "today-reduced"), (Commanded with { Reduced = true }, "command-reduced") })
            {
                string folder = Path.Combine(dir, name, label);
                Directory.CreateDirectory(folder);
                foreach (var (tick, still) in ticks)
                {
                    var view = Crop(tick, variant.Reduced);
                    var target = CropTarget();
                    r.Render(s, view, variant, RigLayers.Frame & ~RigLayers.Labels, target, Color.Black);
                    using var file = File.Create(Path.Combine(folder, $"t{tick - third.Fire:+000;-000;+000}_{still}.png"));
                    target.SaveAsPng(file, target.Width, target.Height);
                }
            }
        }
    }

    private static readonly RigVariant Current = new(false, false, false, true), Commanded = new(false, false, true, true);
    private const int Facing = RigScene.VesperaFacing;

    // The boss path's inputs at a tick, exactly as RigDriver.Vespera / CrimsonRig.Draw compute them.
    internal static (float Charge, float Recoil, ScarletCommand Command, CrimsonRig.ConductorHold Hold, CrimsonRig.ConductorHold Today, int Notes,
        float Cast, float TodayCast) Inputs(CrimsonGesturePlan[] plans, int phase, float tick, bool reduced)
    {
        var live = RigMirror.Live(plans, tick); var known = RigMirror.Known(plans, tick);
        var signal = CrimsonRig.Signal(live, ReadOnlySpan<CrimsonChorusPlan>.Empty, -1, tick);
        // The casting pose reads every plan's timing, the orb's command the plans whose aim is known (DrawConductor).
        Span<ScarletNote> casting = stackalloc ScarletNote[ScarletNotes.Capacity * 2], notes = stackalloc ScarletNote[ScarletNotes.Capacity * 2];
        int cast = CrimsonRig.ConductorNotes(live, phase, tick, RigScene.Conductor.X, RigScene.Conductor.Y, casting);
        int n = CrimsonRig.ConductorNotes(known, phase, tick, RigScene.Conductor.X, RigScene.Conductor.Y, notes);
        var command = ScarletGestureMotion.Command(tick, notes[..n], signal.Charge, signal.Recoil, Facing, reduced);
        var today = ScarletGestureMotion.Command(tick, ReadOnlySpan<ScarletNote>.Empty, signal.Charge, signal.Recoil, Facing, reduced);
        float idle = CrimsonInvocation.Ease(signal.Charge * 2);
        return (signal.Charge, signal.Recoil, command, CrimsonRig.Hold(command, signal.Charge, signal.Recoil, Facing),
            CrimsonRig.Hold(today, signal.Charge, signal.Recoil, Facing), n, cast == 0 ? idle : CrimsonRig.ConductorCast(tick, casting[..cast], signal.Charge), idle);
    }

    // The current phrase's notes of the Act's body (the ones Vespera commands).
    private static IEnumerable<CrimsonGesturePlan> Notes(RigScene s)
    {
        for (int i = 0; i < s.Plans.Length; i++)
            if (s.Roles[i] == "current" && s.Plans[i].Source == s.Phase && ScarletNotes.KindOf(s.Plans[i].Technique, out _)) yield return s.Plans[i];
    }

    // ---- V94: never closer than today's hold ---------------------------------------------------------------------
    private void Distance()
    {
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Scenes)
        {
            var s = Scene(name);
            float minCentre = float.MaxValue, minEdgeMargin = float.MaxValue, maxMove = 0, maxGrow = float.MinValue, minGrow = float.MaxValue;
            int commanded = 0;
            foreach (bool reduced in new[] { false, true })
                for (int tick = s.First; tick <= s.Last; tick++)
                {
                    var x = Inputs(s.Plans, s.Phase, tick, reduced);
                    if (x.Notes > 0) commanded++;
                    float centre = Facing * x.Hold.Offset.X, held = Facing * x.Today.Offset.X;
                    float edge = centre - x.Hold.Radius, todayEdge = held - x.Today.Radius;
                    minCentre = Math.Min(minCentre, centre);
                    minEdgeMargin = Math.Min(minEdgeMargin, edge - todayEdge);
                    maxMove = Math.Max(maxMove, Vector2.Distance(x.Hold.Offset, x.Today.Offset));
                    maxGrow = Math.Max(maxGrow, x.Hold.Radius - x.Today.Radius); minGrow = Math.Min(minGrow, x.Hold.Radius - x.Today.Radius);
                    ok &= centre >= 94 - 1e-4f && centre >= held - 1e-4f && edge >= todayEdge - 1e-4f
                        && Vector2.Distance(x.Hold.Offset, x.Today.Offset) <= ScarletGestureMotion.CommandReach + 1e-3f;
                }
            rows.Add(new { scene = name, commandedTicks = commanded, minCentreDistance = minCentre, minNearEdgeMarginOverToday = minEdgeMargin,
                maxDisplacement = maxMove, radiusChange = new[] { minGrow, maxGrow } });
        }
        Add("V94", "held orb never closer than 94 px / today's hold, displacement <= 24 px, near edge never nearer to her than today's",
            ok ? "pass" : "fail", rows, "CrimsonRig.Hold over every tick, Normal and Reduced (facing -1: distances are measured toward her).");
    }

    // ---- G2v: the Hands' central gap -------------------------------------------------------------------------------
    private void CentralGap()
    {
        var rows = new List<object>(); bool ok = true;
        var field = PreviewPlanner.Field;
        foreach (string name in Scenes)
        {
            var s = Scene(name);
            int addedOutside = 0, addedInside = 0, removed = 0, frames = 0, bandPixels = 0, maxAddedInside = 0, maxAddedOutside = 0;
            int? worstTick = null;
            for (int tick = s.First; tick <= s.Last; tick++)
            {
                var x = Inputs(s.Plans, s.Phase, tick, false);
                if (x.Notes == 0) continue;
                frames++;
                var view = Crop(tick, false);
                var off = Vespera(s, tick, Current); var on = Vespera(s, tick, Commanded);
                var sprite = new bool[off.Length];
                for (int i = 0; i < off.Length; i++) sprite[i] = off[i].A > 12 || on[i].A > 12;
                var body = RigGates.Dilate(sprite, view.Width, view.Height, 12 * view.Zoom);
                var inverse = Matrix.Invert(view.GameView);
                Vector2 orb = RigScene.Conductor + x.Today.Offset;
                for (int py = 0; py < view.Height; py++)
                    for (int px = 0; px < view.Width; px++)
                    {
                        var w = Vector2.Transform(new Vector2(px + .5f, py + .5f), inverse) + view.ScreenPosition;
                        if (MathF.Abs(w.X - field.CenterX) > 47 || w.Y < field.Top || w.Y > field.Bottom) continue;
                        int i = py * view.Width + px;
                        if (body[i]) continue;
                        bandPixels++;
                        int added = Math.Max(Math.Max(on[i].R - off[i].R, on[i].G - off[i].G), Math.Max(on[i].B - off[i].B, on[i].A - off[i].A));
                        int lost = Math.Max(Math.Max(off[i].R - on[i].R, off[i].G - on[i].G), Math.Max(off[i].B - on[i].B, off[i].A - on[i].A));
                        bool inToday = MathF.Abs(w.X - orb.X) <= x.Today.Radius && MathF.Abs(w.Y - orb.Y) <= x.Today.Radius;
                        if (added > 2)
                        {
                            if (inToday) { addedInside++; maxAddedInside = Math.Max(maxAddedInside, added); }
                            else { addedOutside++; maxAddedOutside = Math.Max(maxAddedOutside, added); worstTick ??= tick; }
                        }
                        if (lost > 2) removed++;
                    }
            }
            ok &= addedOutside == 0;
            rows.Add(new { scene = name, commandedFrames = frames, bandPixelsChecked = bandPixels, addedOutsideTodayFootprint = addedOutside,
                maxAddedOutside, firstTickOutside = worstTick, addedInsideTodayFootprint = addedInside, maxAddedInside, removed });
        }
        Add("G2v", "central 94 px gap: the command adds no light outside her sprite and today's orb footprint",
            ok ? "pass" : "fail", rows,
            "Camera V crop (zoom 2), every commanded tick, Normal. Pixels within 12 px of her sprite are hers. addedInsideTodayFootprint is light the release adds where today's orb already reaches (its quad): reported, not gated.");
    }

    // ---- G6v: timing from the notes only -----------------------------------------------------------------------------
    private void Timing()
    {
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Scenes)
        {
            var s = Scene(name);
            foreach (var p in Notes(s))
            {
                ScarletNotes.TryFrom(p, false, RigScene.Conductor.X, RigScene.Conductor.Y, out var note);
                Vector2 aim = new(note.AimX, note.AimY);
                // The crossflow charges toward the upper right (design §2.5) and releases along the flow.
                Vector2 charge = note.Broad ? new Vector2(.70710678f, -.70710678f) : aim;
                var before = Inputs(s.Plans, s.Phase, p.Fire - 1, false);
                var fire = Inputs(s.Plans, s.Phase, p.Fire, false);
                bool ignite = fire.Command.Ignite - before.Command.Ignite > .9f;
                int snapPeak = Peak(t => Inputs(s.Plans, s.Phase, t, false).Command.Snap, p.Fire - 3, p.Fire + 8, false) - p.Fire;
                // Heat peaks on Fire; the crossflow's register (1.25, clamped at 1) holds it on a plateau that must include
                // [Fire-3, Fire]. heatPeak = first tick of the maximum, heatAtFire = Heat(Fire) is that maximum.
                var heat = Enumerable.Range(p.Born + 1, p.Fire + 8 - p.Born).Select(t => Inputs(s.Plans, s.Phase, t, false).Command.Heat).ToArray();
                int heatPeak = Array.FindIndex(heat, h => h >= heat.Max() - 1e-5f) + p.Born + 1 - p.Fire;
                bool heatAtFire = heat[p.Fire - p.Born - 1] >= heat.Max() - 1e-5f;
                // The command's own offset over today's hold (today's hold itself follows the accepted charge signal).
                float pull = Vector2.Dot(before.Hold.Offset - before.Today.Offset, charge);
                var after = Inputs(s.Plans, s.Phase, p.Fire + 2, false);
                float release = Vector2.Dot(after.Hold.Offset - after.Today.Offset - (before.Hold.Offset - before.Today.Offset), aim);
                // The drawn orb (sprite pixels excluded) over [Fire-8, Fire+12]: it starts brightening on the Fire tick (the
                // rim heat), rises most within the three-tick release [Fire, Fire+2] (the reactor keeps today's recoil
                // impulse, so no new flare lights on Fire itself) and its bloom peaks by Fire+8 (today's recoil bloom
                // peaks at about Fire+7).
                var light = Enumerable.Range(p.Fire - 9, 22).Select(t => Light(Vespera(s, t, Commanded))).ToArray();
                var todayLight = Enumerable.Range(p.Fire - 9, 22).Select(t => Light(Vespera(s, t, Current))).ToArray();
                // The bloom's peak is the strongest light after the tick the charged orb is spent on (Fire + 1 .. Fire + 12): since
                // protocol80 the phrase's notes are a beat or more apart, so the orb is fully charged for a few ticks before Fire
                // and today's accepted recoil spends it on Fire (today's light dips there too) before its bloom builds again.
                int lightPeak = Enumerable.Range(10, 12).MaxBy(i => light[i]) - 9, todayPeak = Enumerable.Range(10, 12).MaxBy(i => todayLight[i]) - 9;
                int rise = Enumerable.Range(1, 21).MaxBy(i => light[i] - light[i - 1]) - 9;
                int todayRise = Enumerable.Range(1, 21).MaxBy(i => todayLight[i] - todayLight[i - 1]) - 9;
                float largest = light[rise + 9] - light[rise + 8], onFire = light[9] - light[8];
                bool good = ignite && snapPeak >= 0 && snapPeak <= 3 && heatPeak <= 0 && heatAtFire && pull <= 1 && release >= -1e-3f
                    && rise >= 0 && rise <= 2 && onFire > 0 && lightPeak >= 1 && lightPeak <= 8;
                ok &= good;
                rows.Add(new { scene = name, technique = p.Technique.ToString(), pulse = p.Pulse, aim = new[] { aim.X, aim.Y }, igniteOnFire = ignite,
                    snapPeak, heatPeak, pullAlongAim = pull, releaseAlongAim = release, drawnLightLargestRise = rise, drawnLightPeak = lightPeak,
                    riseOnFireShare = onFire / Math.Max(1, largest), todayLargestRise = todayRise, todayPeak,
                    lightAtFireOverToday = light[9] / Math.Max(1, todayLight[9]), ok = good,
                    light = good ? null : light.Select(x => (int)MathF.Round(x / 1000)).ToArray(), todayLight = good ? null : todayLight.Select(x => (int)MathF.Round(x / 1000)).ToArray() });
            }
        }
        var shift = new List<object>(); bool shifted = true;
        foreach (string name in Scenes)
        {
            var s = Scene(name);
            var moved = s.Plans.Select(p => p with { Begin = p.Begin + 7, Born = p.Born + 7, Fire = p.Fire + 7, End = p.End + 7, FirstFire = p.FirstFire + 7, LastEnd = p.LastEnd + 7 }).ToArray();
            float worst = 0;
            for (int t = s.First; t <= s.Last; t++)
            {
                var a = Inputs(s.Plans, s.Phase, t, false); var b = Inputs(moved, s.Phase, t + 7, false);
                worst = Math.Max(worst, Gap(a.Command, b.Command));
                worst = Math.Max(worst, Math.Max(Vector2.Distance(a.Hold.Offset, b.Hold.Offset), MathF.Abs(a.Hold.Radius - b.Hold.Radius)));
            }
            shifted &= worst <= 1e-4f;
            shift.Add(new { scene = name, worstDelta = worst });
        }
        // The casting pose. A dip is a run of ticks below full pose 1 (.98) between two ticks at full pose 1. A blink is a dip of at
        // most 8 ticks: the pose leaves and returns at once, which the gate forbids. A rest is a longer dip, up to 32 ticks: she
        // relaxes toward the idle pose between two phrases whose cast windows do not touch (CastLead ticks before a note's Born
        // is later than the previous window's close). Rests are reported, not gated: their start (ticks after the scene's first
        // tick), length and the ticks spent fully idle. The crossfades (both poses half-drawn) are counted as well.
        var pose = new List<object>(); bool steady = true;
        foreach (string name in Scenes)
        {
            var s = Scene(name);
            var cast = Enumerable.Range(s.First, s.Last - s.First + 1).Select(t => Inputs(s.Plans, s.Phase, t, false)).ToArray();
            // Every dip as (start index, length, idle ticks: pose 1 below .02 inside it).
            List<(int Start, int Length, int Idle)> Dips(Func<int, float> c)
            {
                var dips = new List<(int, int, int)>();
                int last = -1;                                    // the latest tick at full pose 1
                for (int i = 0; i < cast.Length; i++)
                {
                    if (c(i) < .98f) continue;
                    if (last >= 0 && i - last > 1) dips.Add((last + 1, i - last - 1, Enumerable.Range(last + 1, i - last - 1).Count(j => c(j) < .02f)));
                    last = i;
                }
                return dips;
            }
            var dipsNow = Dips(i => cast[i].Cast); var dipsToday = Dips(i => cast[i].TodayCast);
            int blink = dipsNow.Count(d => d.Length <= 8), todayBlink = dipsToday.Count(d => d.Length <= 8);
            var rests = dipsNow.Where(d => d.Length is > 8 and <= 32).ToList();
            int crossfade = cast.Count(c => c.Cast > .02f && c.Cast < .98f), todayCrossfade = cast.Count(c => c.TodayCast > .02f && c.TodayCast < .98f);
            steady &= blink == 0;
            pose.Add(new { scene = name, blinks = blink, rests = rests.Count, restTicks = rests.Select(d => d.Length).ToArray(),
                restIdleTicks = rests.Select(d => d.Idle).ToArray(), restStartsAfterFirstTick = rests.Select(d => d.Start).ToArray(),
                longerDips = dipsNow.Count(d => d.Length > 32), crossfadeTicks = crossfade, todayBlinks = todayBlink, todayCrossfadeTicks = todayCrossfade,
                poseOneTicks = cast.Count(c => c.Cast >= .98f), todayPoseOneTicks = cast.Count(c => c.TodayCast >= .98f) });
        }
        Add("G6v", "timing: orb ignites on Fire, release peaks in [Fire, Fire+3], heat peaks by Fire, pull against / release toward the aim, drawn light starts rising on Fire, rises most by Fire+2 and peaks by Fire+8; +7 shift; no pose blink (the rests between phrases are reported)",
            ok && shifted && steady ? "pass" : "fail", new { notes = rows, shift, pose },
            "Notes = the current phrase's notes of the Act's body (source = phase). pullAlongAim: the command's orb offset over today's hold at Fire-1 along the aim (the crossflow: along its upper-right charge), <= 1 px (the design's 5 px lift may lean slightly into an upward aim). releaseAlongAim: how far that offset moves along the aim from Fire-1 to Fire+2 (0 when the aim points at her side: the orb never comes nearer than its hold). drawnLight*: ticks relative to Fire of the largest one-tick rise and of the peak of the orb's drawn light, commanded and today.");

        static int Peak(Func<int, float> value, int from, int to, bool last)
        {
            int at = from; float best = float.MinValue;
            for (int t = from; t <= to; t++) { float v = value(t); if (v > best + 1e-6f || last && v >= best - 1e-6f) { best = Math.Max(best, v); at = t; } }
            return at;
        }
        static float Gap(in ScarletCommand a, in ScarletCommand b) => new[]
        {
            MathF.Abs(a.OrbX - b.OrbX), MathF.Abs(a.OrbY - b.OrbY), MathF.Abs(a.Radius - b.Radius), MathF.Abs(a.ReactorCharge - b.ReactorCharge),
            MathF.Abs(a.ReactorImpulse - b.ReactorImpulse), MathF.Abs(a.AlphaScale - b.AlphaScale), MathF.Abs(a.Cast - b.Cast), MathF.Abs(a.Tilt - b.Tilt),
            Math.Abs(a.NudgeX - b.NudgeX), Math.Abs(a.NudgeY - b.NudgeY), MathF.Abs(a.RimAlpha - b.RimAlpha), MathF.Abs(a.RimHeat - b.RimHeat)
        }.Max();
    }

    // The orb's light in a Vespera-layer crop: everything that is not her opaque sprite (the reactor writes alpha 0).
    private static float Light(Color[] px)
    {
        double sum = 0;
        foreach (var c in px) if (c.A <= 12) sum += c.R + c.G + c.B;
        return (float)sum;
    }

    // ---- G7v: Reduced keeps the timing at half the amplitude ----------------------------------------------------------
    private void ReducedHalf()
    {
        var rows = new List<object>(); bool ok = true;
        foreach (string name in Scenes)
        {
            var s = Scene(name);
            float worstMove = 0, worstRadius = 0, worstRim = 0, heatGrowth = 0; int timing = 0;
            for (int t = s.First; t <= s.Last; t++)
            {
                var n = Inputs(s.Plans, s.Phase, t, false); var r = Inputs(s.Plans, s.Phase, t, true);
                float moveN = Vector2.Distance(n.Hold.Offset, n.Today.Offset), moveR = Vector2.Distance(r.Hold.Offset, r.Today.Offset);
                worstMove = Math.Max(worstMove, moveR - .5f * moveN);
                // The command's radius terms (Draw shrink, Snap swell, Lift) over its core radius 53 + 24 core + 18 recoil;
                // core = max(charge, Heat) is the reactor's charge input (as today's charge) and is the same in both.
                float core = 53 + 24 * n.Command.ReactorCharge + 18 * n.Recoil;
                worstRadius = Math.Max(worstRadius, MathF.Abs(r.Command.Radius - core) - .5f * MathF.Abs(n.Command.Radius - core));
                heatGrowth = Math.Max(heatGrowth, core - n.Today.Radius);
                worstRim = Math.Max(worstRim, (r.Command.RimAlpha - .26f) - .5f * (n.Command.RimAlpha - .26f));
                if ((n.Command.Snap > 0) != (r.Command.Snap > 0) || (n.Command.Draw > 0) != (r.Command.Draw > 0)) timing++;
            }
            ok &= worstMove <= 1e-3f && worstRadius <= 1e-3f && worstRim <= 1e-4f && timing == 0;
            rows.Add(new { scene = name, displacementOverHalf = worstMove, radiusTermsOverHalf = worstRadius, rimBoostOverHalf = worstRim,
                heatCoreGrowthBothModes = heatGrowth, timingMismatches = timing });
        }
        Add("G7v", "Reduced: same timing; displacement, the command's radius terms and rim boost at most half of Normal", ok ? "pass" : "fail", rows,
            "Values over every tick; the per-tick difference (Reduced - .5 x Normal) must not be positive. heatCoreGrowthBothModes: the radius the warning's Heat adds through the reactor's charge input (core = max(charge, Heat), S1's Command) - identical in Normal and Reduced, like today's charge; the near-edge guard (V94) bounds it in both.");
    }

    private RigScene Scene(string name)
    {
        if (!scenes.TryGetValue(name, out var scene))
            scenes[name] = scene = RigScene.Build(RigSceneDef.Find(name), RigCamera.Find("V"), new RigOptions { Context = true, Flip = o.Flip });
        return scene;
    }

    internal ScarletView Crop(int tick, bool reduced)
        => ScarletView.Create(r.Device, CropWidth, CropHeight, RigScene.Conductor + CropCentre - new Vector2(CropWidth, CropHeight) * .5f, 2, tick, 0, reduced);

    internal RenderTarget2D CropTarget() => crop ??= new RenderTarget2D(r.Device, CropWidth, CropHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

    // The Vespera layer of a scene at a tick through the crop (transparent background).
    internal Color[] Vespera(RigScene s, int tick, in RigVariant v)
    {
        var view = Crop(tick, v.Reduced);
        var target = CropTarget();
        r.Render(s, view, v, RigLayers.Vespera, target, Color.Transparent);
        return RigRenderer.Read(target);
    }

    // Today's boss-path Vespera as the harness drew it before S4 (and the game before it): the performer with today's
    // arguments (opening over, reveal 1, nothing consumed) and the held orb at facing * (94 + 12 charge) px beside her and
    // 25 px up with radius 53 + 24 charge + 18 recoil, from the plan list's Signal only. The oracle of G8v.
    private Color[] TodayVespera(RigScene s, int tick, bool reduced)
    {
        var view = Crop(tick, reduced);
        var target = CropTarget();
        CrimsonVisuals.Reduced = reduced;
        Terraria.Main.screenPosition = view.ScreenPosition;
        Terraria.Main.GameViewMatrix.TransformationMatrix = view.GameView;
        Terraria.Main.GameViewMatrix.Zoom = new Vector2(view.Zoom);
        r.Device.SetRenderTarget(target);
        RigHost.CheckTarget(r.Device);
        r.Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1f, 0);
        var signal = RigMirror.Signal(s.Plans, -1, tick);
        Vector2 at = RigScene.Conductor;
        using (var batch = new SpriteBatch(r.Device))
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Terraria.Main.Rasterizer, null, view.GameView);
            RigHost.Tag = "vespera";
            CrimsonRig.DrawPerformer(batch, Terraria.Main.screenPosition, at, tick, Vector2.Zero, RigScene.VesperaFacing, true, signal.Charge, signal.Recoil, 1, false, 1);
            Vector2 held = new(RigScene.VesperaFacing * (94 + signal.Charge * 12), -25);
            float radius = 53 + signal.Charge * 24 + signal.Recoil * 18;
            CrimsonEnergy.Begin();
            CrimsonEnergy.AddCore(at + held, radius, tick, Math.Max(signal.Charge, 0), signal.Recoil, 1, CrimsonVisuals.Reduced);
            CrimsonEnergy.Draw(batch);
            RigHost.Tag = "";
            batch.End();
        }
        r.Device.SetRenderTarget(null);
        return RigRenderer.Read(target);
    }

    // CrimsonCompanionVisuals.PreDraw's call: positional arguments only (every new parameter at its default).
    private Color[] Companion(int tick, Vector2 velocity, int facing, bool floating, float charge, float recoil, bool reduced)
    {
        var view = Crop(tick, reduced);
        var target = CropTarget();
        CrimsonVisuals.Reduced = reduced;
        Terraria.Main.screenPosition = view.ScreenPosition;
        Terraria.Main.GameViewMatrix.TransformationMatrix = view.GameView;
        r.Device.SetRenderTarget(target);
        r.Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1f, 0);
        using (var batch = new SpriteBatch(r.Device))
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Terraria.Main.Rasterizer, null, view.GameView);
            CrimsonRig.DrawPerformer(batch, view.ScreenPosition, RigScene.Conductor, tick, velocity, facing, floating, charge, recoil);
            batch.End();
        }
        r.Device.SetRenderTarget(null);
        return RigRenderer.Read(target);
    }

    internal static string Hash(Color[] px) => Convert.ToHexString(SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(px.AsSpan())));

    // ---- G8v: no note = today's Vespera, every tick; the companion untouched ------------------------------------
    private void Identity()
    {
        string dir = Path.Combine(o.Baseline.Length > 0 ? o.Baseline : Path.Combine(output, "baseline"), "s4");
        string file = Path.Combine(dir, "identity.json");
        RigBaseline.Manifest? manifest = null;
        string? refused = o.WriteBaseline ? null : RigBaseline.Refuse(root, dir, "s4", out manifest);
        if (refused is not null)
        {
            Add("G8v", "no note: Vespera's boss path draws today's frame on every tick (Normal, Reduced); the companion is byte-identical", "not_run", null, refused);
            return;
        }
        bool compare = !o.WriteBaseline;
        var expected = compare ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))! : null;
        var actual = new Dictionary<string, string>();
        var rows = new List<object>();
        int frames = 0, different = 0, missing = 0;
        string? firstDifferent = null;
        void Record(string key, Color[] px, string? oracle = null)
        {
            string hash = Hash(px);
            actual[key] = hash; frames++;
            if (oracle is not null) { if (oracle != hash) { different++; firstDifferent ??= key; } return; }
            if (expected is null) return;
            if (!expected.TryGetValue(key, out var old)) { missing++; return; }
            if (old != hash) { different++; firstDifferent ??= key; }
        }
        foreach (string name in Scenes)
        {
            var s = Scene(name);
            int before = different;
            foreach (bool reduced in new[] { false, true })
                for (int tick = s.First; tick <= s.Last; tick++)
                    Record($"{name}|{(reduced ? "reduced" : "normal")}|{tick}", Vespera(s, tick, new RigVariant(reduced, false, false, true)),
                        compare ? Hash(TodayVespera(s, tick, reduced)) : null);
            rows.Add(new { scene = name, ticks = s.Last - s.First + 1, different = different - before });
        }
        int companionBefore = different;
        float[][] signals = { new[] { 0f, 0f }, new[] { .4f, 0f }, new[] { 1f, 0f }, new[] { 0f, .6f }, new[] { .7f, .3f } };
        foreach (int tick in new[] { 0, 37, 91, 150 })
            foreach (var velocity in new[] { Vector2.Zero, new Vector2(3, -1) })
                foreach (int facing in new[] { 1, -1 })
                    foreach (bool floating in new[] { false, true })
                        foreach (var signal in signals)
                            foreach (bool reduced in new[] { false, true })
                                Record($"companion|{tick}|{velocity.X}|{facing}|{floating}|{signal[0]}|{signal[1]}|{reduced}",
                                    Companion(tick, velocity, facing, floating, signal[0], signal[1], reduced));
        rows.Add(new { scene = "companion", combinations = 160, different = different - companionBefore });
        if (!compare)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(file, JsonSerializer.Serialize(actual));
            RigBaseline.Write(root, dir, "s4", "today's Vespera (boss path, no note) and companion crops (G8v)");
        }
        Add("G8v", "no note: Vespera's boss path draws today's frame on every tick (Normal, Reduced); the companion is byte-identical",
            !compare ? "baseline_written" : different == 0 && missing == 0 ? "pass" : "fail",
            new { frames, different, missing, firstDifferent, rows },
            compare ? "Vespera: SHA-256 of each crop (zoom 2 around Vespera) of the boss path with the command off against TodayVespera, the pre-S4 drawing "
                    + "(DrawPerformer with today's arguments and the held orb at today's offset and radius) over the same plans. Companion: against "
                    + Path.GetRelativePath(output, file) + $" (rendered from {manifest!.Revision}{(manifest.Dirty ? ", dirty" : "")}, {manifest.Written})."
                : "Hashes written to " + file + " with their manifest; run the gates on the change to compare.");
    }

    private void Add(string id, string title, string status, object? measured, string note) => gates.Add(new RigGate(id, title, status, false, measured, note));
}
