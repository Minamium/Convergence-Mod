using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Convergence.Client.Encounters.CrimsonFoundry;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Common.Raids.Arena;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

// Scarlet attack expression, motion and material math (presentation only, never collision):
//  - G0: the C# Crown swing/pour, Mantle wind/whip and Choir arm match the owner-approved prototype. The golden
//    (Data/scarlet-motion-golden.json, embedded) was sampled once from the prototype's motion.js / rigs.js with
//    `node tools/scarlet_motion_golden.mjs <prototype dir> Tests/Convergence.DomainTests/Data/scarlet-motion-golden.json`;
//    it records both files' sha256. The rejected 4/4 figure is not part of it.
//  - CrimsonChoirMotion.Arm through ScarletEnvelope is bit-identical to the accepted arm (verbatim oracle below).
//  - Notes come from plans only; shifting every plan by +7 ticks shifts every response by +7 (no beat follows);
//    no note is the rest picture; window edges never pop; closed notes kept for past poses change nothing now.
//  - The Choir's blood (ScarletChoirBlood) reaches the struck fingertips exactly on Fire, stays below the white-hot
//    threshold through the warning, ignites only on Fire and has returned when the window closes.
internal static partial class Program
{
    private const float ScarletShift = 7;

    private static JsonDocument ScarletGolden()
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("scarlet-motion-golden.json")
            ?? throw new InvalidOperationException("scarlet-motion-golden.json is not embedded");
        return JsonDocument.Parse(stream);
    }

    private static void ScarletNear(double expected, float actual, double tolerance, string context)
    {
        if (!float.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Expected {context} {expected:R} +/- {tolerance}, got {actual:R}.");
    }

    private static float[] ScarletChannel(JsonElement scenario, string name)
    {
        var array = scenario.GetProperty(name);
        var values = new float[array.GetArrayLength()];
        int i = 0;
        foreach (var value in array.EnumerateArray())
            values[i++] = value.ValueKind switch { JsonValueKind.True => 1, JsonValueKind.False => 0, _ => (float)value.GetDouble() };
        return values;
    }

    private static ScarletNote ScarletGoldenNote(JsonElement cue, bool mantle)
    {
        float born = cue.GetProperty("born").GetSingle(), fire = cue.GetProperty("fire").GetSingle(), end = cue.GetProperty("end").GetSingle();
        float side = mantle ? cue.GetProperty("dir").GetSingle() : cue.GetProperty("side").GetSingle();
        bool low = mantle && cue.GetProperty("low").GetBoolean(), lead = !mantle && cue.GetProperty("lead").GetBoolean();
        return new(born, fire, fire, end - fire, mantle ? ScarletNoteKind.Rope : ScarletNoteKind.Curtain,
            side, low, lead, false, 3, 1, -1, -1, 0, 1, 0);
    }

    [DomainTest("Scarlet motion G0: the envelope, Crown, Mantle and Choir arm match the approved prototype golden")]
    private static void ScarletMotionGolden()
    {
        using var golden = ScarletGolden();
        var root = golden.RootElement;
        double tolerance = root.GetProperty("tolerance").GetDouble();
        AssertEqual(true, tolerance <= 1e-4, "golden tolerance is at most 1e-4");
        foreach (var q in root.GetProperty("quarters").EnumerateArray())
            AssertEqual(q.GetProperty("arm").GetInt32(), ScarletGestureMotion.ArmForQuarter(q.GetProperty("quarter").GetInt32(), q.GetProperty("flip").GetBoolean()), "quarter -> arm");
        int checkedSamples = 0;
        var kinds = new HashSet<string>();
        foreach (var scenario in root.GetProperty("scenarios").EnumerateArray())
        {
            string name = scenario.GetProperty("name").GetString()!, kind = scenario.GetProperty("kind").GetString()!;
            kinds.Add(kind);
            var cueElements = new List<JsonElement>();
            foreach (var cue in scenario.GetProperty("cues").EnumerateArray()) cueElements.Add(cue);
            if (kind == "choir")
            {
                float charge = scenario.GetProperty("charge").GetSingle(), recoil = scenario.GetProperty("recoil").GetSingle();
                var cues = new CrimsonChoirCue[cueElements.Count];
                for (int i = 0; i < cues.Length; i++)
                    cues[i] = new(cueElements[i].GetProperty("born").GetSingle(), cueElements[i].GetProperty("fire").GetSingle(),
                        cueElements[i].GetProperty("end").GetSingle(), cueElements[i].GetProperty("arm").GetInt32(), cueElements[i].GetProperty("broad").GetBoolean());
                int index = 0;
                foreach (var arm in scenario.GetProperty("arms").EnumerateArray())
                {
                    double start = arm.GetProperty("start").GetDouble(), step = arm.GetProperty("step").GetDouble();
                    float[] s = ScarletChannel(arm, "s"), e = ScarletChannel(arm, "e"), w = ScarletChannel(arm, "w"),
                        power = ScarletChannel(arm, "power"), burst = ScarletChannel(arm, "burst");
                    for (int k = 0; k < s.Length; k++)
                    {
                        float age = (float)(start + step * k);
                        var pose = ScarletGestureMotion.ChoirArm(index, age, charge, recoil, cues);
                        string at = $"{name} arm {index} age {age}";
                        ScarletNear(s[k], pose.Shoulder, tolerance, at + " shoulder");
                        ScarletNear(e[k], pose.Elbow, tolerance, at + " elbow");
                        ScarletNear(w[k], pose.Wrist, tolerance, at + " wrist");
                        ScarletNear(power[k], pose.Power, tolerance, at + " power");
                        ScarletNear(burst[k], pose.Burst, tolerance, at + " burst");
                        checkedSamples++;
                    }
                    index++;
                }
                AssertEqual(4, index, $"{name}: four arms");
                continue;
            }
            double first = scenario.GetProperty("start").GetDouble(), stride = scenario.GetProperty("step").GetDouble();
            int count = scenario.GetProperty("count").GetInt32();
            if (kind == "envelope")
            {
                var cue = cueElements[0];
                float born = cue.GetProperty("born").GetSingle(), fire = cue.GetProperty("fire").GetSingle(), end = cue.GetProperty("end").GetSingle();
                float[] p = ScarletChannel(scenario, "p"), t = ScarletChannel(scenario, "t"), live = ScarletChannel(scenario, "live"),
                    prepare = ScarletChannel(scenario, "prepare"), decay = ScarletChannel(scenario, "decay"),
                    wind = ScarletChannel(scenario, "wind"), follow = ScarletChannel(scenario, "follow");
                for (int k = 0; k < count; k++)
                {
                    float age = (float)(first + stride * k);
                    var phase = ScarletEnvelope.Phase(age, born, fire, end - fire);
                    string at = $"{name} age {age}";
                    ScarletNear(p[k], phase.P, tolerance, at + " p");
                    ScarletNear(t[k], phase.T, tolerance, at + " t");
                    AssertEqual(live[k] > .5f, phase.Live, at + " live");
                    ScarletNear(prepare[k], phase.Prepare, tolerance, at + " prepare");
                    ScarletNear(decay[k], phase.Decay, tolerance, at + " decay");
                    ScarletNear(wind[k], phase.Wind, tolerance, at + " wind");
                    ScarletNear(follow[k], phase.Follow, tolerance, at + " follow");
                    checkedSamples++;
                }
                continue;
            }
            bool mantle = kind == "mantle";
            var notes = new ScarletNote[cueElements.Count];
            for (int i = 0; i < notes.Length; i++) notes[i] = ScarletGoldenNote(cueElements[i], mantle);
            float[] x = ScarletChannel(scenario, "x"), y = ScarletChannel(scenario, "y"), turn = ScarletChannel(scenario, "turn");
            float[] fourth = ScarletChannel(scenario, mantle ? "sweep" : "flare"), fifth = ScarletChannel(scenario, mantle ? "row" : "kick");
            for (int k = 0; k < count; k++)
            {
                float age = (float)(first + stride * k);
                var motion = mantle ? ScarletGestureMotion.Mantle(age, notes) : ScarletGestureMotion.Crown(age, notes);
                string at = $"{name} age {age}";
                ScarletNear(x[k], motion.OffsetX, tolerance, at + " x");
                ScarletNear(y[k], motion.OffsetY, tolerance, at + " y");
                ScarletNear(turn[k], motion.Turn, tolerance, at + " turn");
                ScarletNear(fourth[k], mantle ? motion.Sweep : motion.Flare, tolerance, at + (mantle ? " sweep" : " flare"));
                ScarletNear(fifth[k], mantle ? motion.Row : motion.Kick, tolerance, at + (mantle ? " row" : " kick"));
                checkedSamples++;
            }
        }
        AssertEqual(true, kinds.SetEquals(new[] { "envelope", "crown", "mantle", "choir" }), "every approved function is pinned");
        AssertEqual(true, checkedSamples > 2500, "the golden is densely sampled");
    }

    // The accepted CrimsonChoirMotion.Arm before it was routed through ScarletEnvelope, verbatim.
    private static CrimsonChoirArm ScarletAcceptedArm(int index, float age, float charge, float recoil, ReadOnlySpan<CrimsonChoirCue> cues)
    {
        static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
        static float Out(float x) { x = 1 - Math.Clamp(x, 0, 1); return 1 - x * x * x; }
        float lift = 0, strike = 0, energy = 0, burst = 0;
        foreach (var cue in cues)
        {
            if ((!cue.Broad && cue.Arm != index) || age < cue.Born || age >= cue.End) continue;
            float warning = Math.Max(1, cue.Fire - cue.Born), p = Math.Clamp((age - cue.Born) / warning, 0, 1);
            float t = age - cue.Fire;
            float prepare = .80f * Out(p / .38f) + .12f * Ease((p - .38f) / .43f) + .08f * Ease((p - .81f) / .19f);
            float release = t <= 0 ? 0 : Out(t / 4f);
            float decay = t <= 0 ? 1 : 1 - Ease((t - 6) / Math.Max(7, cue.End - cue.Fire - 6));
            float pulse = t <= 0 ? 0 : Out(t / 1.8f) * MathF.Exp(-t / 11f) * decay;
            lift = Math.Max(lift, prepare * (1 - release) * decay);
            strike = Math.Max(strike, release * decay);
            energy = Math.Max(energy, (prepare * (1 - release) + .7f * release) * decay);
            burst = Math.Max(burst, pulse);
        }
        if (cues.IsEmpty) { lift = charge * .35f; strike = recoil * .45f; energy = charge * .45f; burst = recoil * .3f; }
        float side = (index & 1) == 0 ? -1 : 1, lower = index >= 2 ? -1 : 1;
        return new(side * (MathF.Sin(age * .025f + index * 1.7f) * .065f + lower * (lift * .72f - strike * .80f)),
            side * (MathF.Sin(age * .033f + index * 2.1f) * .09f - lift * .66f + strike * .49f),
            side * (MathF.Sin(age * .046f + index) * .07f + lift * .36f - strike * .54f), energy, burst);
    }

    [DomainTest("Scarlet motion: the Choir arm through ScarletEnvelope is bit-identical to the accepted arm")]
    private static void ScarletChoirArmIdentical()
    {
        var random = new Random(20261003);
        var cues = new CrimsonChoirCue[6];
        for (int trial = 0; trial < 400; trial++)
        {
            int count = trial % 7;
            float origin = 400 + random.Next(0, 9000);
            for (int i = 0; i < count; i++)
            {
                float born = origin + random.Next(-40, 40), fire = born + random.Next(1, 60) + (trial % 3 == 0 ? .5f : 0);
                cues[i] = new(born, fire, fire + random.Next(2, 70), random.Next(0, 4), random.Next(0, 5) == 0);
            }
            float charge = (float)random.NextDouble(), recoil = (float)random.NextDouble();
            for (float age = origin - 50; age < origin + 140; age += .375f)
                for (int arm = 0; arm < 4; arm++)
                {
                    var expected = ScarletAcceptedArm(arm, age, charge, recoil, cues.AsSpan(0, count));
                    var actual = ScarletGestureMotion.ChoirArm(arm, age, charge, recoil, cues.AsSpan(0, count));
                    if (expected != actual) throw new InvalidOperationException($"arm {arm} age {age} trial {trial}: {expected} != {actual}");
                }
        }
    }

    // The plans of one real phrase of an Act (CrimsonChoreography.Create, protocol80), in issue order: an ordinary phrase is its
    // three cell notes (pulses 0-2 or 1-3, one beat of warning, never on a bar head) and the closing crossflow (pulse 4,
    // charging for two beats and released on the next downbeat); a signature phrase (serial % 3 == 0) is the Act's move in four
    // steps (pulses 0-3, a dotted quarter apart, the last on the next downbeat) and has no crossflow of its own.
    private static CrimsonGesturePlan[] ScarletMotionPhrase(int phase, int phrase, float shift = 0, int curtainMask = 0)
    {
        var rhythm = CrimsonChoreography.Create(CrimsonChoreography.OpeningTicks, phrase, phase, false);
        var plans = new CrimsonGesturePlan[rhythm.Hits.Count];
        for (int i = 0; i < plans.Length; i++)
        {
            int note = rhythm.Hits[i].Pulse;
            var p = ScarletSignaturePlan(phase, phrase, note, 0, curtainMask: curtainMask > 0 ? curtainMask : 1 << 3);
            if (phase != 0 || p.Technique != CrimsonTechnique.CinderCurtain) p = p with { Target = new(p.Field.CenterX - 300 + 150 * note, p.Field.CenterY + 60) };
            int s = (int)shift;
            plans[i] = p with { Begin = p.Begin + s, Born = p.Born + s, Fire = p.Fire + s, End = p.End + s, FirstFire = p.FirstFire + s, LastEnd = p.LastEnd + s };
        }
        return plans;
    }

    // Everything the phrase's notes can still show: the latest window close.
    private static float ScarletPhraseRungOut(ReadOnlySpan<CrimsonGesturePlan> plans)
    {
        float close = 0;
        foreach (var p in plans) close = MathF.Max(close, p.Fire + ScarletNotes.SpanOf(p));
        return close;
    }

    private static (float X, float Y) ScarletVespera(in CrimsonGesturePlan plan) => (plan.Field.CenterX, plan.Field.CenterY);

    [DomainTest("Scarlet notes: kinds, spans, sides, rows and the arms that own the struck ground come from the plan")]
    private static void ScarletNoteDerivation()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        // Curtain: one member walks away from the nearer wall; the swing alternates on that walk; the crossflow is Broad.
        for (int column = 0; column < CrimsonSignatureMoves.CurtainColumns; column++)
        {
            var phrase = ScarletMotionPhrase(0, 3, curtainMask: 1 << column);
            int walk = CrimsonSignatureMoves.CurtainWalk(column, 3).Direction;
            for (int note = 0; note < 4; note++)
            {
                var (vx, vy) = ScarletVespera(phrase[note]);
                AssertEqual(true, ScarletNotes.TryFrom(phrase[note], false, vx, vy, out var n), "curtain note");
                AssertEqual(ScarletNoteKind.Curtain, n.Kind, "kind");
                AssertEqual(walk * (note % 2 == 0 ? 1f : -1f), n.Side, $"column {column} note {note} swing side");
                AssertEqual(false, n.Lead, "one plan cannot know a hand-over: Collect decides it");
                AssertEqual(44f, n.Span, "curtain span = live 20 + residue 24");
                AssertEqual(1f, n.Register, "signature register");
                AssertEqual(3f, n.Reach, "curtain reach");
                AssertEqual(true, n.AimY > 0 && MathF.Abs(n.AimX * n.AimX + n.AimY * n.AimY - 1) < 1e-5f, "aim is a downward unit vector");
            }
        }
        // The seal crossflow is an ordinary phrase's closer (pulse 4): charge two beats, spent with its band.
        var closer = ScarletMotionPhrase(0, 1)[^1];
        AssertEqual(CrimsonChoreography.Closer, (int)closer.Pulse, "an ordinary phrase closes with the crossflow");
        AssertEqual(true, ScarletNotes.TryFrom(closer, false, 0, 0, out var cross) && cross.Broad && cross.Kind == ScarletNoteKind.Crossflow
            && cross.Span == closer.End - closer.Fire && cross.Register == ScarletNotes.CrossflowRegister && cross.AimX == -1
            && MathF.Abs(cross.Fire - cross.Born - 2 * 28.125f) < 1.01f, "crossflow note charges for two beats");
        foreach (int phrase in new[] { 3, 6 })
        {
            var crowd = ScarletMotionPhrase(0, phrase, curtainMask: 1 << 1 | 1 << 8);
            AssertEqual(true, ScarletNotes.TryFrom(crowd[0], false, 0, 0, out var n), "crowd curtain");
            AssertEqual(phrase / 3 % 2 == 0 ? 1f : -1f, n.Side, "a crowd's walk alternates with the signature ordinal");
        }
        // Rope: the odd steps (the final one on the downbeat) are the low comb, the rope crosses rightward on even steps; span 12 + 20.
        var rope = ScarletMotionPhrase(1, 3);
        for (int note = 0; note < 4; note++)
        {
            AssertEqual(true, ScarletNotes.TryFrom(rope[note], false, 0, 0, out var n) && n.Kind == ScarletNoteKind.Rope, "rope note");
            AssertEqual(note % 2 == 1, n.Low, "the low comb is the odd steps' (it sweeps the floor on the downbeat)");
            AssertEqual(note % 2 == 0 ? 1f : -1f, n.Side, "the rope crosses rightward on even steps, whichever comb they are");
            AssertEqual(32f, n.Span, "rope span");
        }
        // Hands: the arm that owns each slammed quarter; a mirrored body swaps sides.
        Span<CrimsonChoirCue> cues = stackalloc CrimsonChoirCue[16];
        for (int phrase = 3; phrase <= 12; phrase += 3)
        {
            var hands = ScarletMotionPhrase(2, phrase);
            for (int note = 0; note < 4; note++)
            {
                var (a, b) = CrimsonSignatureMoves.HandsStruck(phrase, note);
                foreach (bool flipped in new[] { false, true })
                {
                    AssertEqual(true, ScarletNotes.TryFrom(hands[note], flipped, 0, 0, out var n) && n.Kind == ScarletNoteKind.Hands, "hands note");
                    AssertEqual(ScarletGestureMotion.ArmForQuarter(a, flipped), (int)n.ArmA, "first arm");
                    AssertEqual(ScarletGestureMotion.ArmForQuarter(b, flipped), (int)n.ArmB, "second arm");
                    AssertEqual(40f, n.Span, "hands span = live 16 + residue 24");
                    int count = ScarletNotes.ChoirCues(hands.AsSpan(note, 1), hands[note].Fire, flipped, cues);
                    AssertEqual(2, count, "two cues per slam");
                    AssertEqual(true, cues[0].Arm == n.ArmA && cues[1].Arm == n.ArmB && cues[0].End == hands[note].Fire + 40, "cue arms and the extended window");
                }
            }
        }
        // Basic Act I-III notes keep a lighter register.
        var beam = ScarletMotionPhrase(0, 1);
        AssertEqual(true, ScarletNotes.TryFrom(beam[0], false, f.CenterX, f.CenterY, out var basic) && basic.Kind == ScarletNoteKind.Beam
            && basic.Register == ScarletNotes.BasicRegister && basic.Span == 44 && basic.Reach == 5, "tracking beam note");
        // The two ordinary cells (serial % 3 == 1: pulses 0-2, == 2: pulses 1-3) between them use every pulse and so every rake edge.
        foreach (int serial in new[] { 1, 2 })
            foreach (var plan in ScarletMotionPhrase(1, serial))
            {
                if (plan.Technique == CrimsonTechnique.SideBeams) continue;
                var direction = CrimsonChoreography.Direction(plan.Phrase, plan.Pulse);
                AssertEqual(true, ScarletNotes.TryFrom(plan, false, 0, 0, out var n) && n.Kind == ScarletNoteKind.Rift && n.Span == 32, "rift note");
                AssertEqual(direction.Y >= .7f, n.Low, "rift row from its direction");
                if (MathF.Abs(direction.X) > .01f) AssertEqual((float)MathF.Sign(direction.X), n.Side, "rift side from its direction");
            }
        var edges = new HashSet<int>();
        foreach (int serial in new[] { 1, 2 })
            foreach (var plan in ScarletMotionPhrase(2, serial))
            {
                if (plan.Technique == CrimsonTechnique.SideBeams) continue;
                foreach (bool flipped in new[] { false, true })
                {
                    AssertEqual(true, ScarletNotes.TryFrom(plan, flipped, 0, 0, out var n) && n.Kind == ScarletNoteKind.Rakes, "rakes note");
                    AssertEqual(36f, n.Span, "rakes span = live 12 + residue 24");
                    int edge = plan.Pulse & 3, flip = flipped ? 1 : 0;
                    var expected = edge switch { 0 => (0, 1), 2 => (2, 3), 1 => (1 ^ flip, 3 ^ flip), _ => (0 ^ flip, 2 ^ flip) };
                    AssertEqual(expected, ((int)n.ArmA, (int)n.ArmB), $"edge {edge} flipped {flipped}: the arms on the entry side");
                    edges.Add(edge);
                }
            }
        AssertEqual(4, edges.Count, "the two cells reach all four rake edges");
        // Final and other techniques produce no note.
        AssertEqual(false, ScarletNotes.TryFrom(beam[0] with { Technique = CrimsonTechnique.SpatialGrid }, false, 0, 0, out _), "grid has no note");
        AssertEqual(false, ScarletNotes.TryFrom(beam[0] with { Technique = CrimsonTechnique.ClusterVolley }, false, 0, 0, out _), "cluster has no note");
    }

    [DomainTest("Scarlet notes: a body answers an aimed plan only once its lock is known (a peer's issue-time Target would turn it the other way)")]
    private static void ScarletAimKnown()
    {
        // An aimed plan (tracking beam, rift, crossflow) carries its issue-time Target until the authority's lock (tick >= Born)
        // reaches a peer; CrimsonGesture.ForecastReady is that moment and the forecast is drawn from it.
        var phrase = ScarletMotionPhrase(0, 1);
        var beam = phrase[0]; var crossflow = phrase[^1];
        AssertEqual(true, beam.Aimed && crossflow.Aimed, "a tracking beam and a crossflow are aimed");
        foreach (var aimed in new[] { beam, crossflow })
        {
            AssertEqual(false, ScarletNotes.AimKnown(aimed, false), "an aimed plan is unknown before its lock");
            AssertEqual(true, ScarletNotes.AimKnown(aimed, true), "an aimed plan is known once locked");
        }
        foreach (var plan in ScarletMotionPhrase(1, 1))
            if (plan.Technique == CrimsonTechnique.SpatialRift)
            {
                AssertEqual(true, plan.Aimed, "a rift is aimed");
                AssertEqual(false, ScarletNotes.AimKnown(plan, false), "a rift waits for its lock");
            }
        foreach (var signature in new[] { ScarletMotionPhrase(0, 3), ScarletMotionPhrase(1, 3), ScarletMotionPhrase(2, 3) })
            foreach (var plan in signature)
                AssertEqual(true, ScarletNotes.AimKnown(plan, false), "a signature step has no aim to wait for");
        foreach (var plan in ScarletMotionPhrase(2, 1))
            if (!plan.Aimed) AssertEqual(true, ScarletNotes.AimKnown(plan, false), "the Choir's rakes have no aim to wait for");
        // Why: the Target a peer holds before the lock (the player's position when the phrase was issued) and the locked one can
        // send the Crown's swing and Vespera's orb opposite ways (a diagonal note's stroke, and so its side and aim, follows the Target).
        int differ = 0, beams = 0;
        foreach (int serial in new[] { 1, 2 })
            foreach (var plan in ScarletMotionPhrase(0, serial))
            {
                if (plan.Technique != CrimsonTechnique.TrackingBeam) continue;
                beams++;
                var (vx, vy) = ScarletVespera(plan);
                var stale = plan with { Target = new(plan.Field.CenterX - 500, plan.Field.CenterY + 80) };
                var locked = plan with { Target = new(plan.Field.CenterX + 500, plan.Field.CenterY - 80) };
                AssertEqual(true, ScarletNotes.TryFrom(stale, false, vx, vy, out var before), "the stale beam note");
                AssertEqual(true, ScarletNotes.TryFrom(locked, false, vx, vy, out var after), "the locked beam note");
                if (before.Side != after.Side || MathF.Abs(before.AimX - after.AimX) > .1f || MathF.Abs(before.AimY - after.AimY) > .1f) differ++;
            }
        AssertEqual(true, beams >= 4 && differ > 0, "the stale and the locked Target answer differently for some beam note");
    }

    [DomainTest("Scarlet notes: only a note whose warning opens as another strikes takes the swing over (protocol80 phrases)")]
    private static void ScarletNoteHandover()
    {
        var notes = new ScarletNote[ScarletNotes.Capacity];
        // Ordinary cell A (fires on eighths 6, 9, 12 with warnings from 4, 7, 10), cell B (7, 10, 12 from 5, 8, 10) and a
        // signature phrase (7, 10, 13, 16 from 5, 8, 11, 14). Only cell B's third note opens as the second strikes.
        var expectedByPhrase = new Dictionary<int, string> { [1] = "", [2] = "3", [3] = "" };
        foreach (var (serial, expected) in expectedByPhrase)
        {
            var plans = ScarletMotionPhrase(0, serial);
            var v = ScarletVespera(plans[0]);
            var seen = new List<int>();
            for (float age = plans[0].Born; age < ScarletPhraseRungOut(plans) + 2; age += .5f)
            {
                int n = ScarletNotes.Collect(plans, 0, age, false, v.X, v.Y, notes);
                for (int i = 0; i < n; i++)
                {
                    bool oracle = false;
                    foreach (var p in plans)
                        oracle |= !notes[i].Broad && p.Technique != CrimsonTechnique.SideBeams && p.Fire == notes[i].Born && p.Fire < notes[i].Fire;
                    AssertEqual(oracle, notes[i].Lead, $"serial {serial} age {age} pulse {notes[i].Pulse}: Lead is a hand-over from a note firing on this Born");
                    if (notes[i].Lead && !seen.Contains(notes[i].Pulse)) seen.Add(notes[i].Pulse);
                }
            }
            seen.Sort();
            AssertEqual(expected, string.Join(",", seen), $"serial {serial}: the notes that take the swing over");
        }
        // The crossflow never takes a swing over, and a lone note has no Lead.
        var closer = ScarletMotionPhrase(0, 2)[^1];
        AssertEqual(true, ScarletNotes.Collect(new[] { closer }, 0, closer.Fire, false, 0, 0, notes) == 1 && !notes[0].Lead, "a crossflow is never a hand-over");
    }

    [DomainTest("Scarlet notes: collection windows, order, dedupe and the Choir cues agree with the prototype")]
    private static void ScarletNoteCollection()
    {
        var plans = ScarletMotionPhrase(1, 3);
        var notes = new ScarletNote[ScarletNotes.Capacity];
        AssertEqual(0, ScarletNotes.Collect(plans, 1, plans[0].Born - .01f, false, 0, 0, notes), "nothing before the first Born");
        AssertEqual(0, ScarletNotes.Collect(plans, 0, plans[1].Fire, false, 0, 0, notes), "another source's notes are not this body's");
        var doubled = new CrimsonGesturePlan[plans.Length * 2];
        for (int i = 0; i < plans.Length; i++) { doubled[i] = plans[plans.Length - 1 - i]; doubled[plans.Length + i] = plans[i]; }
        for (float age = plans[0].Born; age < ScarletPhraseRungOut(plans) + 2; age += .5f)
        {
            int count = ScarletNotes.Collect(doubled, 1, age, false, 0, 0, notes);
            int expected = 0;
            foreach (var p in plans) if (age >= p.Born && age < p.Fire + ScarletNotes.SpanOf(p)) expected++;
            AssertEqual(expected, count, $"age {age}: one note per plan inside its window, duplicates removed");
            for (int i = 1; i < count; i++) AssertEqual(true, notes[i - 1].Fire <= notes[i].Fire, "ordered by Fire");
        }
        // Final keeps its accepted single-arm cues; Acts use the extended window.
        var choir = ScarletMotionPhrase(2, 3);
        Span<CrimsonChoirCue> cues = stackalloc CrimsonChoirCue[16];
        int legacy = ScarletNotes.ChoirCues(choir, choir[1].Fire + 20, false, cues, true);
        int legacyExpected = 0;
        foreach (var p in choir) if (choir[1].Fire + 20 >= p.Born && choir[1].Fire + 20 < p.End) legacyExpected++;
        AssertEqual(legacyExpected, legacy, "final cues keep the plan window");
        for (int i = 0; i < legacy; i++) AssertEqual(true, cues[i].Arm is >= 0 and < 4, "final cue arm is Step % 4");
        // Against the prototype: each note's two cues are the golden's (born, fire, arm) for the same ordinal and flip.
        using var golden = ScarletGolden();
        foreach (var scenario in golden.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            if (!scenario.TryGetProperty("ordinal", out var ordinalElement)) continue;
            int ordinal = ordinalElement.GetInt32(); bool flip = scenario.GetProperty("flip").GetBoolean();
            var phrase = ScarletMotionPhrase(2, 3 * ordinal + 3 * 4); // rotation = phrase / 3 % 4 = ordinal
            var expected = new List<int>();
            foreach (var cue in scenario.GetProperty("cues").EnumerateArray()) expected.Add(cue.GetProperty("arm").GetInt32());
            var actual = new List<int>();
            for (int note = 0; note < 4; note++)
            {
                int n = ScarletNotes.ChoirCues(phrase.AsSpan(note, 1), phrase[note].Fire, flip, cues);
                for (int i = 0; i < n; i++)
                {
                    actual.Add(cues[i].Arm);
                    AssertEqual(phrase[note].Fire + 40, cues[i].End, "prototype end = fire + live + residue");
                }
            }
            AssertEqual(string.Join(",", expected), string.Join(",", actual), $"ordinal {ordinal} flip {flip}: struck arms");
        }
    }

    [DomainTest("Scarlet signal: the plan-list timing equals the accepted per-projectile signal")]
    private static void ScarletSignalTimes()
    {
        var plans = ScarletMotionPhrase(0, 1);
        var choruses = Array.Empty<CrimsonChorusPlan>();
        for (float age = plans[0].Born - 70; age < plans[^1].End + 120; age += .25f)
            for (int source = -1; source <= 3; source++)
            {
                float until = 60, since = 100;
                foreach (var p in plans)
                    if (age >= p.Born && (source < 0 || p.Source == source))
                    {
                        float delta = p.Fire - age;
                        if (delta >= 0) until = Math.Min(until, delta); else since = Math.Min(since, -delta);
                    }
                AssertEqual((until, since), ScarletNotes.SignalTimes(plans, choruses, source, age), $"age {age} source {source}");
            }
    }

    private static void ScarletEveryResponse(ReadOnlySpan<CrimsonGesturePlan> plans, int source, float age, bool flipped, bool reduced,
        List<float> output)
    {
        output.Clear();
        var v = ScarletVespera(plans[0]);
        Span<ScarletNote> notes = stackalloc ScarletNote[ScarletNotes.Capacity];
        int n = ScarletNotes.Collect(plans, source, age, flipped, v.X, v.Y, notes);
        var crown = ScarletGestureMotion.Crown(age, notes[..n]);
        var mantle = ScarletGestureMotion.Mantle(age, notes[..n]);
        var body = ScarletBodyMaterial.Apparition(age, notes[..n], source == 0 ? crown : mantle, flipped, reduced);
        var choir = ScarletBodyMaterial.Choir(age, notes[..n], reduced);
        var command = ScarletGestureMotion.Command(age, notes[..n], .25f, .1f, flipped ? -1 : 1, reduced);
        output.AddRange(new[] { crown.OffsetX, crown.OffsetY, crown.Turn, crown.Flare, crown.Kick,
            mantle.OffsetX, mantle.OffsetY, mantle.Turn, mantle.Sweep, mantle.Row, mantle.Flare, mantle.Kick,
            body.Heat, body.Ignite, body.Front, body.Drain, body.Lean, body.PourLimit, body.Run, body.Row, body.Swing,
            body.Wind, body.Send, body.Return, body.Snap, body.Engaged, body.Active ? 1 : 0,
            choir.Heat, choir.Ignite, choir.Drain, choir.Surge, choir.Engaged, choir.Active ? 1 : 0,
            command.OrbX, command.OrbY, command.Radius, command.ReactorCharge, command.ReactorImpulse, command.AlphaScale,
            command.Cast, command.Tilt, command.NudgeX, command.NudgeY, command.RimAlpha, command.Draw, command.Snap,
            command.Lift, command.Spend });
        for (int arm = 0; arm < 4; arm++)
        {
            var limb = choir.Limb(arm);
            output.AddRange(new[] { limb.Lift, limb.Send, limb.Return, limb.Tear, limb.Burst });
        }
        Span<CrimsonChoirCue> cues = stackalloc CrimsonChoirCue[16];
        int c = ScarletNotes.ChoirCues(plans, age, flipped, cues);
        for (int arm = 0; arm < 4; arm++)
        {
            var drive = ScarletGestureMotion.ChoirDrive(arm, age, cues[..c]);
            output.AddRange(new[] { drive.Lift, drive.Strike, drive.Energy, drive.Burst });
            var blood = ScarletChoirBlood.Of(cues[..c], arm, age, choir.Heat);
            output.AddRange(new[] { blood.Send, blood.Return, blood.Flash, blood.Fill, blood.Gain, blood.At(.3f), blood.At(.9f), blood.At(1) });
        }
        for (int i = 0; i < n; i++)
        {
            output.AddRange(new[] { notes[i].Born - age, notes[i].Fire - age, notes[i].Span, notes[i].Side, notes[i].AimX, notes[i].AimY });
            for (int k = 0; k < 10; k++)
            {
                bool any = ScarletBodyMaterial.Ember(notes[i], k, age, out var ember);
                output.AddRange(new[] { any ? 1 : 0, ember.X, ember.Y, ember.Alpha });
                any = ScarletBodyMaterial.Spark(notes[i], k, age, 1, 0, out var spark);
                output.AddRange(new[] { any ? 1 : 0, spark.X, spark.Y, spark.Alpha });
            }
        }
    }

    [DomainTest("Scarlet motion G9: shifting every plan by +7 ticks shifts every response by exactly +7 ticks")]
    private static void ScarletMotionShiftInvariance()
    {
        var before = new List<float>(); var after = new List<float>();
        for (int phase = 0; phase < 3; phase++)
            foreach (int serial in new[] { 1, 2, 3 })
            {
                var plans = ScarletMotionPhrase(phase, serial);
                var shifted = ScarletMotionPhrase(phase, serial, ScarletShift);
                foreach (bool flipped in new[] { false, true })
                    for (float age = plans[0].Born - 10; age < ScarletPhraseRungOut(plans) + 30; age += .375f)
                    {
                        ScarletEveryResponse(plans, phase, age, flipped, false, before);
                        ScarletEveryResponse(shifted, phase, age + ScarletShift, flipped, false, after);
                        AssertEqual(before.Count, after.Count, $"act {phase + 1} serial {serial} age {age}: same response shape");
                        for (int i = 0; i < before.Count; i++)
                            if (MathF.Abs(before[i] - after[i]) > 1e-5f)
                                throw new InvalidOperationException($"act {phase + 1} serial {serial} age {age} channel {i}: {before[i]} != {after[i]} after +7");
                    }
            }
    }

    [DomainTest("Scarlet motion G8: no note is the rest picture (Crown, Mantle, bodies and Vespera's held orb)")]
    private static void ScarletMotionRest()
    {
        ReadOnlySpan<ScarletNote> none = default;
        foreach (float age in new[] { 0f, 1234.5f, 7777.25f })
        {
            AssertEqual(default(ScarletApparitionMotion), ScarletGestureMotion.Crown(age, none), "crown at rest");
            AssertEqual(default(ScarletApparitionMotion), ScarletGestureMotion.Mantle(age, none), "mantle at rest");
            AssertEqual(default(ScarletBodyState), ScarletBodyMaterial.Apparition(age, none, default, false, false), "apparition material at rest");
            AssertEqual(default(ScarletChoirState), ScarletBodyMaterial.Choir(age, none, false), "choir material at rest");
            foreach (var (charge, recoil) in new[] { (0f, 0f), (.4f, .2f), (1f, .9f) })
                foreach (int facing in new[] { -1, 1 })
                    foreach (bool reduced in new[] { false, true })
                    {
                        var command = ScarletGestureMotion.Command(age, none, charge, recoil, facing, reduced);
                        AssertEqual((0f, 0f, 0, 0), (command.OrbX, command.OrbY, command.NudgeX, command.NudgeY), "held orb unmoved");
                        AssertEqual(53 + charge * 24 + recoil * 18, command.Radius, "today's orb radius");
                        AssertEqual(ScarletEnvelope.Ease(charge * 2), command.Cast, "today's cast weight");
                        AssertEqual((charge, recoil, 1f, 0f, .26f), (command.ReactorCharge, command.ReactorImpulse, command.AlphaScale, command.Tilt, command.RimAlpha), "today's reactor and rim");
                    }
        }
        // Outside every window the notes leave nothing behind either.
        var plans = ScarletMotionPhrase(0, 3);
        var notes = new ScarletNote[ScarletNotes.Capacity];
        float after = ScarletPhraseRungOut(plans) + 1;
        AssertEqual(0, ScarletNotes.Collect(plans, 0, after, false, 0, 0, notes), "the phrase has rung out");
    }

    [DomainTest("Scarlet motion: window edges never pop (continuous, bounded second difference at every Born, End and close)")]
    private static void ScarletMotionEdges()
    {
        const float h = 1f / 64;
        for (int phase = 0; phase < 3; phase++)
            foreach (int serial in new[] { 1, 2, 3 })
            {
                var plans = ScarletMotionPhrase(phase, serial);
                var v = ScarletVespera(plans[0]);
                var edges = new List<float>();
                foreach (var p in plans) { edges.Add(p.Born); edges.Add(p.End); edges.Add(p.Fire + ScarletNotes.SpanOf(p)); }
                foreach (float edge in edges)
                {
                    var a = Sample(edge - h); var b = Sample(edge + h);
                    for (int i = 0; i < a.Length; i++)
                        if (MathF.Abs(a[i] - b[i]) > Limit(i) * 2 * h)
                            throw new InvalidOperationException($"act {phase + 1} serial {serial} edge {edge} channel {i}: {a[i]} -> {b[i]}");
                    // One-tick second difference: the approved strike kinks (pour jolt, intake, release) and nothing larger.
                    var before = Sample(edge - 1); var at = Sample(edge); var after = Sample(edge + 1);
                    for (int i = 0; i < at.Length; i++)
                        if (MathF.Abs(before[i] - 2 * at[i] + after[i]) > Bend(i))
                            throw new InvalidOperationException($"act {phase + 1} serial {serial} edge {edge} channel {i}: second difference {before[i] - 2 * at[i] + after[i]}");
                }
                float[] Sample(float age)
                {
                    Span<ScarletNote> notes = stackalloc ScarletNote[ScarletNotes.Capacity];
                    int n = ScarletNotes.Collect(plans, phase, age, false, v.X, v.Y, notes);
                    var crown = ScarletGestureMotion.Crown(age, notes[..n]);
                    var mantle = ScarletGestureMotion.Mantle(age, notes[..n]);
                    var body = ScarletBodyMaterial.Apparition(age, notes[..n], mantle, false, false);
                    var choir = ScarletBodyMaterial.Choir(age, notes[..n], false);
                    Span<CrimsonChoirCue> cues = stackalloc CrimsonChoirCue[16];
                    int c = ScarletNotes.ChoirCues(plans, age, false, cues);
                    var drive = ScarletGestureMotion.ChoirDrive(0, age, cues[..c]);
                    var drive3 = ScarletGestureMotion.ChoirDrive(3, age, cues[..c]);
                    var blood = ScarletChoirBlood.Of(cues[..c], 0, age, choir.Heat);
                    var blood3 = ScarletChoirBlood.Of(cues[..c], 3, age, choir.Heat);
                    return new[] { crown.OffsetX, crown.OffsetY, crown.Turn * 100, mantle.OffsetX, mantle.OffsetY, mantle.Turn * 100,
                        mantle.Sweep * 10, mantle.Row * 10, body.Heat, body.Drain, body.Engaged, body.Wind, body.Snap,
                        choir.Heat, choir.Drain, choir.Engaged, drive.Lift, drive.Strike, drive.Energy, drive.Burst,
                        drive3.Lift, drive3.Strike, drive3.Energy, drive3.Burst,
                        // The arm's blood (below the hand: a lead note's Born is the previous note's Fire, where
                        // only the fingertips' ignition may step).
                        blood.At(.5f), blood.At(.75f), blood3.At(.5f), blood3.At(.75f) };
                }
            }
        // Velocity bounds (per tick): offsets in px, turns in centi-radians, normalised rows x10, material 0..1. The Crown's
        // swing is slow (about 4.2 px/tick at its fastest) so its offset and turn are held to 6 px and 3 centi-radians, which
        // catches the sub-pixel step a swing out that is cut when the next one begins would make. The Mantle's whip is the
        // steepest approved motion (up to MantleTravel * 1.55 * 1.5 / WhipTicks = 14 px/tick, which since protocol80 a
        // neighbouring note's window edge can coincide with), the pour jolt of the Crown's drop 12.
        static float Limit(int channel) => channel switch { 0 => 6, 2 => 3, 3 or 4 => 15, < 8 => 12, _ => 1.2f };
        // Second-difference bounds (per tick^2): the Crown's 22 px pour jolt and the Mantle's rapid intake are the
        // largest approved kinks (about 7.5 px); the accepted Choir slam burst (about .83) is the largest 0..1 one.
        static float Bend(int channel) => channel < 8 ? 10 : .9f;
    }

    [DomainTest("Scarlet material: analytic particles stay small, inside their note and absent under Reduced Effects")]
    private static void ScarletMaterialParticles()
    {
        foreach (int phase in new[] { 0, 1, 2 })
            foreach (int serial in new[] { 1, 2, 3 })
            {
                var plans = ScarletMotionPhrase(phase, serial);
                foreach (var plan in plans)
                {
                    if (!ScarletNotes.TryFrom(plan, false, 0, 0, out var note)) continue;
                    AssertEqual(0, ScarletBodyMaterial.EmberCount(note, true) + ScarletBodyMaterial.DripCount(note, true)
                        + ScarletBodyMaterial.SparkCount(note, true) + ScarletBodyMaterial.IntakeCountOf(note, true), "Reduced spawns none");
                    if (note.Kind == ScarletNoteKind.Hands)
                        AssertEqual(0, ScarletBodyMaterial.SparkCount(note, false) + ScarletBodyMaterial.EmberCount(note, false)
                            + ScarletBodyMaterial.DripCount(note, false), "no Choir particle in a FourHands window");
                    float previousRise = 0;
                    for (float age = note.Born - 5; age < note.Close + 5; age += .25f)
                        for (int k = 0; k < 10; k++)
                        {
                            if (ScarletBodyMaterial.Ember(note, k, age, out var ember))
                            {
                                AssertEqual(true, note.Holds(age) && age >= note.Fire, "embers are born on Fire inside the window");
                                AssertEqual(true, ember.Y <= 0 && -ember.Y <= ScarletBodyMaterial.EmberRise && MathF.Abs(ember.X) < .07f, "embers only rise, a little");
                                AssertEqual(true, ember.Alpha is >= 0 and <= .35f, "embers stay dim");
                                if (k == 0) { AssertEqual(true, -ember.Y >= previousRise - 1e-6f, "never falls back"); previousRise = -ember.Y; }
                            }
                            if (ScarletBodyMaterial.Spark(note, k, age, 0, 1, out var spark))
                                AssertEqual(true, note.Holds(age) && MathF.Sqrt(spark.X * spark.X + spark.Y * spark.Y) <= ScarletBodyMaterial.SparkReach, "sparks stop within 25 px");
                            if (ScarletBodyMaterial.Drip(note, k, age, out var drip))
                                AssertEqual(true, note.Holds(age) && drip.Y is >= 0 and <= ScarletBodyMaterial.DripFall, "drips fall a little");
                            if (ScarletBodyMaterial.Intake(note, k, age, out var intake))
                                AssertEqual(true, age >= note.Born && age < note.Fire && MathF.Sqrt(intake.X * intake.X + intake.Y * intake.Y) <= .2f + 1e-6f, "intake converges in the warning");
                        }
                }
            }
    }

    [DomainTest("Scarlet command: Vespera's orb never comes closer to her, moves at most 24 px and is halved under Reduced")]
    private static void ScarletCommandLimits()
    {
        Span<ScarletNote> notes = stackalloc ScarletNote[ScarletNotes.Capacity];
        for (int phase = 0; phase < 3; phase++)
            foreach (int serial in new[] { 1, 2, 3 })
            {
                var plans = ScarletMotionPhrase(phase, serial);
                var v = ScarletVespera(plans[0]);
                bool moved = false;
                for (float age = plans[0].Born - 10; age < ScarletPhraseRungOut(plans) + 30; age += .5f)
                {
                    int n = ScarletNotes.Collect(plans, phase, age, false, v.X, v.Y, notes);
                    foreach (int facing in new[] { -1, 1 })
                    {
                        var full = ScarletGestureMotion.Command(age, notes[..n], .3f, .1f, facing, false);
                        var reduced = ScarletGestureMotion.Command(age, notes[..n], .3f, .1f, facing, true);
                        foreach (var c in new[] { full, reduced })
                        {
                            AssertEqual(true, facing * c.OrbX >= 0, "never toward her face");
                            AssertEqual(true, MathF.Sqrt(c.OrbX * c.OrbX + c.OrbY * c.OrbY) <= ScarletGestureMotion.CommandReach + 1e-4f, "within 24 px");
                            AssertEqual(true, MathF.Abs(c.Tilt) <= .09f && c.Cast is >= 0 and <= 1 && c.AlphaScale is >= .7f and <= 1, "bounded pose");
                        }
                        AssertEqual(true, MathF.Abs(reduced.OrbY) <= MathF.Abs(full.OrbY) * .5f + 1e-4f, "Reduced halves the displacement");
                        moved |= MathF.Abs(full.OrbX) + MathF.Abs(full.OrbY) > 2;
                    }
                }
                AssertEqual(true, moved, $"act {phase + 1} serial {serial}: she commands her notes");
            }
    }

    [DomainTest("Scarlet notes: closed notes kept for past poses change nothing now and let a past pose replay its motion")]
    private static void ScarletNoteLookback()
    {
        Span<ScarletNote> now = stackalloc ScarletNote[ScarletNotes.Capacity];
        Span<ScarletNote> kept = stackalloc ScarletNote[ScarletNotes.Capacity];
        Span<ScarletNote> then = stackalloc ScarletNote[ScarletNotes.Capacity];
        for (int phase = 0; phase < 2; phase++)
            foreach (int serial in new[] { 1, 2, 3 })
            {
                var plans = ScarletMotionPhrase(phase, serial);
                var v = ScarletVespera(plans[0]);
                for (float age = plans[0].Born; age < plans[^1].End + 70; age += .5f)
                {
                    int a = ScarletNotes.Collect(plans, phase, age, false, v.X, v.Y, now);
                    int b = ScarletNotes.Collect(plans, phase, age, false, v.X, v.Y, kept, ScarletNotes.PastTicks);
                    int expected = 0;
                    foreach (var p in plans) if (p.Source == phase && age >= p.Born && age < p.Fire + ScarletNotes.SpanOf(p) + ScarletNotes.PastTicks) expected++;
                    AssertEqual(Math.Min(expected, ScarletNotes.Capacity), b, $"act {phase + 1} serial {serial} age {age}: closed notes within PastTicks");
                    string at = $"act {phase + 1} serial {serial} age {age}";
                    // Each body's own motion (Crown: Act I, Mantle: Act II), as CrimsonRig.DrawEffigy draws it.
                    ScarletApparitionMotion Motion(float t, ReadOnlySpan<ScarletNote> notes)
                        => phase == 0 ? ScarletGestureMotion.Crown(t, notes) : ScarletGestureMotion.Mantle(t, notes);
                    var motion = Motion(age, now[..a]);
                    AssertEqual(motion, Motion(age, kept[..b]), at + ": motion now");
                    AssertEqual(ScarletBodyMaterial.Apparition(age, now[..a], motion, false, false),
                        ScarletBodyMaterial.Apparition(age, kept[..b], motion, false, false), at + ": material now");
                    // The Crown's pour jolt ends inside every Crown note's window, so a closed note adds no drop.
                    if (phase == 0) for (int i = 0; i < b; i++) AssertEqual(true, kept[i].Broad || kept[i].Span >= ScarletGestureMotion.CrownDropTicks, at + ": jolt inside the window");
                    // The wake's oldest pose (16 ticks, its tendrils 4 more) replays the motion the body really made.
                    foreach (int lag in new[] { 4, 10, 16, 20 })
                    {
                        float t = age - lag;
                        int c = ScarletNotes.Collect(plans, phase, t, false, v.X, v.Y, then);
                        AssertEqual(Motion(t, then[..c]), Motion(t, kept[..b]), $"{at} lag {lag}: past pose");
                    }
                }
            }
    }

    [DomainTest("Scarlet Choir blood: 1 at the struck fingertips on Fire, below white-hot through the warning, returned by the close")]
    private static void ScarletChoirBloodValues()
    {
        Span<CrimsonChoirCue> cues = stackalloc CrimsonChoirCue[16];
        foreach (int serial in new[] { 1, 2, 3 }) // ChoirRakes (both cells), FourHands
            foreach (bool flipped in new[] { false, true })
            {
                var plans = ScarletMotionPhrase(2, serial);
                for (int i = 0; i < plans.Length; i++)
                {
                    var p = plans[i];
                    if (!ScarletNotes.ChoirArms(p, flipped, out int first, out int second)) continue;
                    var one = plans.AsSpan(i, 1);
                    string at = $"serial {serial} pulse {p.Pulse} flip {flipped}";
                    float close = p.Fire + ScarletNotes.SpanOf(p);
                    for (float age = p.Born; age < close; age += .25f)
                    {
                        int c = ScarletNotes.ChoirCues(one, age, flipped, cues);
                        for (int arm = 0; arm < 4; arm++)
                        {
                            var blood = ScarletChoirBlood.Of(cues[..c], arm, age, 1);
                            bool struck = arm == first || arm == second;
                            for (float u = 0; u <= 1.0001f; u += .025f)
                            {
                                float front = blood.At(u);
                                if (!struck) AssertEqual(0f, front, $"{at} age {age}: arm {arm} is not struck");
                                else if (age < p.Fire) AssertEqual(true, front <= ScarletChoirBlood.SendPeak + 1e-6f, $"{at} age {age} u {u}: the warning stays below white-hot");
                                AssertEqual(true, front is >= 0 and <= 1, $"{at}: front in 0..1");
                            }
                        }
                    }
                    foreach (int arm in new[] { first, second })
                    {
                        int c = ScarletNotes.ChoirCues(one, p.Fire - .001f, flipped, cues);
                        ScarletNear(1, ScarletChoirBlood.Of(cues[..c], arm, p.Fire - .001f, 1).Send, 1e-3, at + ": Send is 1 on Fire");
                        c = ScarletNotes.ChoirCues(one, p.Fire, flipped, cues);
                        var fire = ScarletChoirBlood.Of(cues[..c], arm, p.Fire, 1);
                        ScarletNear(1, fire.Flash, 1e-6, at + ": the fingertips ignite on Fire");
                        ScarletNear(1, fire.At(1), 1e-6, at + ": the fingertips are the brightest on Fire");
                        AssertEqual(true, fire.At(ScarletChoirBlood.HandFrom - .05f) < .75f, at + ": only the hand runs white-hot");
                        c = ScarletNotes.ChoirCues(one, close - .25f, flipped, cues);
                        var late = ScarletChoirBlood.Of(cues[..c], arm, close - .25f, 1);
                        AssertEqual(true, late.Return < .01f && late.At(.5f) < .02f, at + ": the blood has returned by the close");
                        c = ScarletNotes.ChoirCues(one, close, flipped, cues);
                        AssertEqual(0, c, at + ": no cue after the close");
                    }
                }
            }
    }
}
