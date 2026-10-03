using System;
using System.Numerics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;

namespace Convergence.DomainTests;

// Sable Scythe (docs/encounters/crimson-foundry/REWARDS.md, "Melee - Sable Scythe"): the figure-eight measure, its
// flow bounds, the staff, Staff Reap's placement and cut schedule, commitment and replicated-data validation.
internal static partial class Program
{
    private static readonly int[] ScytheMeasure = { 0, 1, 2, 3, 4 };

    [DomainTest("Sable Scythe measure keeps the specified stroke clocks")]
    private static void SableScytheClocks()
    {
        int[] kind = { SableScytheMotion.Over, SableScytheMotion.Under, SableScytheMotion.Over, SableScytheMotion.Under, SableScytheMotion.Whip };
        int[] duration = { 18, 18, 18, 18, 28 }, start = { 5, 5, 5, 5, 14 }, end = { 13, 13, 13, 13, 20 };
        float measure = 0; int ticks = 0;
        foreach (int i in ScytheMeasure)
        {
            AssertEqual(kind[i], SableScytheMotion.Kind(i), $"stroke {i} kind");
            AssertEqual(duration[i], SableScytheMotion.Duration(i), $"stroke {i} duration");
            AssertEqual(start[i], SableScytheMotion.LiveStart(i), $"stroke {i} live start");
            AssertEqual(end[i], SableScytheMotion.LiveEnd(i), $"stroke {i} live end");
            AssertEqual(false, SableScytheMotion.Live(i, start[i]), "the sweep into the live start is not yet live");
            AssertEqual(true, SableScytheMotion.Live(i, start[i] + 1), "live from the start of the window");
            AssertEqual(true, SableScytheMotion.Live(i, end[i]), "live through the end of the window");
            AssertEqual(false, SableScytheMotion.Live(i, end[i] + 1), "half-open after the window");
            AssertEqual((i + 1) % 5, SableScytheMotion.Next(i), "Over, Under, Over, Under, Whip, then the Over again");
            // Each Over and Under throws one crescent; the Whip's lash arc lands once and sheds the volley of five.
            measure += SableScytheMotion.Multiplier(i) + (kind[i] == SableScytheMotion.Whip
                ? CrimsonRewardRules.LashMultiplier + CrimsonRewardRules.VolleyCrescents * CrimsonRewardRules.VolleyMultiplier
                : CrimsonRewardRules.CrescentMultiplier);
            ticks += duration[i];
        }
        AssertNear(.6f, SableScytheMotion.Multiplier(0), 1e-6f, "the Over's and Under's blade");
        AssertNear(1f, SableScytheMotion.Multiplier(4), 1e-6f, "the Whip's blade");
        AssertNear(6.4f, measure, 1e-5f, "a measure is 6.4x");
        AssertNear(CrimsonRewardRules.MeasureMultiplier, measure, 1e-5f, "the rules' measure is the sum of its parts");
        AssertEqual(100, ticks, "a measure is 100 ticks");
        AssertEqual(true, SableScytheMotion.LashLive(4, 15) && SableScytheMotion.LashLive(4, 26) && !SableScytheMotion.LashLive(4, 27),
            "the lash arc is live from the lash until 26");
        AssertEqual(false, SableScytheMotion.LashLive(0, 10), "only the Whip writes a lash arc");
        AssertEqual(5f, SableScytheMotion.DrawAge(0, 6, 0), "the first live tick draws from the live start, never the windup");
        AssertEqual(13f, SableScytheMotion.DrawAge(0, 14, 0), "the first recovery tick draws from the live end");
        AssertNear(2.5f, SableScytheMotion.DrawAge(0, 3, .5f), 1e-6f, "fractional between ticks");
        AssertEqual(13, SableScytheMotion.LastDamage(0), "a release waits for the stroke's live window");
        AssertEqual(26, SableScytheMotion.LastDamage(4), "the Whip's release waits for its lash arc");
    }

    // Projected hook tip in the aim frame (shoulder at the origin), facing right.
    private static Vector2 ScytheTip(int stroke, float age) => SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, age));

    [DomainTest("Sable Scythe strokes join with matched pose and speed")]
    private static void SableScytheJoins()
    {
        // (from, to): Over -> Under, Under -> Over, Under -> Whip, Whip -> Over.
        foreach (var (a, b) in new[] { (0, 1), (1, 2), (3, 4), (4, 0) })
        {
            float end = SableScytheMotion.Duration(a);
            SablePose x = SableScytheMotion.Pose(a, end), y = SableScytheMotion.Pose(b, 0);
            AssertNear(0, SableScytheMotion.Wrap(x.Spin - y.Spin), 1e-4f, $"{a}->{b} spin");
            AssertNear(0, SableScytheMotion.Wrap(x.Roll - y.Roll), 1e-3f, $"{a}->{b} roll");
            AssertNear(0, Vector2.Distance(x.Hand, y.Hand), 1e-3f, $"{a}->{b} hand");
            AssertNear(0, Vector2.Distance(ScytheTip(a, end), ScytheTip(b, 0)), .05f, $"{a}->{b} tip");
            const float h = .002f;
            AssertNear(Rate(a, end - h, p => p.Spin), Rate(b, h, p => p.Spin), .01f, $"{a}->{b} spin speed");
            AssertNear(Rate(a, end - h, p => p.Roll), Rate(b, h, p => p.Roll), .01f, $"{a}->{b} roll speed");
            AssertNear(0, Vector2.Distance(TipVelocity(a, end - h), TipVelocity(b, h)), .6f, $"{a}->{b} tip velocity");
        }
    }

    private static float Rate(int stroke, float t, Func<SablePose, float> value, float h = .001f)
        => (value(SableScytheMotion.Pose(stroke, t + h)) - value(SableScytheMotion.Pose(stroke, t - h))) / (2 * h);
    private static Vector2 TipVelocity(int stroke, float t, float h = .001f) => (ScytheTip(stroke, t + h) - ScytheTip(stroke, t - h)) / (2 * h);

    [DomainTest("Sable Scythe hook tip never slows under 3 px/tick or turns tighter than 24 px")]
    private static void SableScytheTipFlow()
    {
        const float h = .01f;
        foreach (int stroke in ScytheMeasure)
        {
            int duration = SableScytheMotion.Duration(stroke);
            for (float t = h; t <= duration - h; t += .02f)
            {
                Vector2 p0 = ScytheTip(stroke, t - h), p1 = ScytheTip(stroke, t), p2 = ScytheTip(stroke, t + h);
                Vector2 v = (p2 - p0) / (2 * h), a = (p2 - 2 * p1 + p0) / (h * h);
                float speed = v.Length(), cross = MathF.Abs(v.X * a.Y - v.Y * a.X);
                AssertEqual(true, speed >= CrimsonRewardRules.MinTipSpeed, $"stroke {stroke} at {t:0.00}: tip speed {speed:0.0}");
                float radius = cross < 1e-6f ? float.MaxValue : speed * speed * speed / cross;
                AssertEqual(true, radius >= CrimsonRewardRules.MinTurnRadius, $"stroke {stroke} at {t:0.00}: turn radius {radius:0.0}");
            }
        }
    }

    [DomainTest("Sable Scythe keeps its angular speed through rolls and the draw-back, under 0.3 rad/tick^2")]
    private static void SableScytheAngularBounds()
    {
        const float h = .01f;
        foreach (int stroke in ScytheMeasure)
        {
            int duration = SableScytheMotion.Duration(stroke), kind = SableScytheMotion.Kind(stroke);
            float peak = 0, rollMin = float.MaxValue, drawMin = float.MaxValue, accel = 0;
            for (float t = h; t <= duration - h; t += .02f)
            {
                float spin = Rate(stroke, t, p => p.Spin, h), roll = Rate(stroke, t, p => p.Roll, h);
                float spin2 = Second(stroke, t, p => p.Spin, h), roll2 = Second(stroke, t, p => p.Roll, h);
                float w = SableScytheMotion.AngularSpeed(spin, roll);
                accel = MathF.Max(accel, SableScytheMotion.AngularAcceleration(spin, roll, spin2, roll2));
                bool live = t >= SableScytheMotion.LiveStart(stroke) && t <= SableScytheMotion.LiveEnd(stroke);
                if (live) peak = MathF.Max(peak, w);
                else if (kind != SableScytheMotion.Whip) rollMin = MathF.Min(rollMin, w);
                if (kind == SableScytheMotion.Whip && t >= CrimsonRewardRules.WhipDrawStart && t <= CrimsonRewardRules.WhipDrawEnd) drawMin = MathF.Min(drawMin, w);
            }
            AssertEqual(true, accel < CrimsonRewardRules.MaxAngularAcceleration, $"stroke {stroke}: peak angular acceleration {accel:0.000}");
            if (kind == SableScytheMotion.Whip)
                AssertEqual(true, drawMin >= CrimsonRewardRules.DrawBackSpeedFloor * peak, $"draw-back {drawMin:0.000} vs peak {peak:0.000}");
            else
                AssertEqual(true, rollMin >= CrimsonRewardRules.RollSpeedFloor * peak, $"stroke {stroke} roll {rollMin:0.000} vs peak {peak:0.000}");
        }
    }

    private static float Second(int stroke, float t, Func<SablePose, float> value, float h)
        => (value(SableScytheMotion.Pose(stroke, t + h)) - 2 * value(SableScytheMotion.Pose(stroke, t)) + value(SableScytheMotion.Pose(stroke, t - h))) / (h * h);

    [DomainTest("Sable Scythe cuts down through the aim in the Over, rises in the Under and lashes in the Whip")]
    private static void SableScytheFigureEight()
    {
        foreach (int stroke in ScytheMeasure)
        {
            int kind = SableScytheMotion.Kind(stroke);
            int start = SableScytheMotion.LiveStart(stroke), end = SableScytheMotion.LiveEnd(stroke);
            // The cut through the aim is the crossing farthest in front (the Whip's lash arc curls back near the reaper).
            float farthest = 0, direction = 0;
            for (float t = start; t < end; t += .05f)
            {
                Vector2 a = ScytheTip(stroke, t), b = ScytheTip(stroke, t + .05f);
                if (MathF.Sign(a.Y) == MathF.Sign(b.Y) || a.X <= farthest) continue;
                farthest = a.X; direction = MathF.Sign(b.Y - a.Y);
            }
            AssertEqual(true, farthest > CrimsonRewardRules.ScytheReach * .8f, $"stroke {stroke} crosses the aim at reach during its live window");
            AssertEqual(kind == SableScytheMotion.Under ? -1f : 1f, direction, $"stroke {stroke} crosses the aim the right way");
        }
        // The Over's hand rises (the high slash floats), the Under's sinks (the low slash sinks).
        AssertEqual(true, SableScytheMotion.Pose(0, 18).Hand.Y < SableScytheMotion.Pose(0, 0).Hand.Y - 10, "the hand rises through the Over");
        AssertEqual(true, SableScytheMotion.Pose(1, 18).Hand.Y > SableScytheMotion.Pose(1, 0).Hand.Y + 10, "the hand sinks through the Under");
        // The grip stays near the shoulder, within about the native front arm's reach (the arm points at it).
        foreach (int stroke in ScytheMeasure)
            for (float t = 0; t <= SableScytheMotion.Duration(stroke); t += .25f)
                AssertEqual(true, SableScytheMotion.Pose(stroke, t).Hand.Length() <= 20.5f, $"stroke {stroke} grip within reach at {t}");
        for (float t = 0; t <= 56; t += .25f)
            AssertEqual(true, SableScytheMotion.ReleasePose(t).Hand.Length() <= 20.5f, $"release grip within reach at {t}");
        // The Under mirrors the Over across the aim: same spin, plane rolled by pi.
        for (float t = 0; t <= 18; t += .5f)
        {
            Vector2 over = ScytheTip(0, t), under = ScytheTip(1, t);
            AssertNear(over.X, under.X, .05f, "the Under mirrors the Over (x)");
            AssertNear(-over.Y, under.Y, .05f, "the Under mirrors the Over (y)");
        }
        // The Whip's arm thrusts 24 px forward through the lash; the hook stays behind the head while drawn back.
        AssertNear(CrimsonRewardRules.WhipThrust, SableScytheMotion.Pose(4, 20).Hand.X - SableScytheMotion.Pose(4, 14).Hand.X, .01f, "the Whip's thrust");
        for (float t = CrimsonRewardRules.WhipDrawStart; t <= CrimsonRewardRules.WhipDrawEnd; t += .5f)
        {
            Vector2 tip = ScytheTip(4, t);
            AssertEqual(true, tip.Y < -40 && tip.X < 10, $"the hook is behind the head while drawn back ({tip.X:0},{tip.Y:0})");
        }
        // The Whip's lash is flat: its live sweep is wider than it is tall.
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        for (float t = CrimsonRewardRules.WhipLiveStart; t <= CrimsonRewardRules.WhipLiveEnd; t += .25f)
        {
            Vector2 tip = ScytheTip(4, t);
            minX = MathF.Min(minX, tip.X); maxX = MathF.Max(maxX, tip.X); minY = MathF.Min(minY, tip.Y); maxY = MathF.Max(maxY, tip.Y);
        }
        AssertEqual(true, maxX - minX > maxY - minY, "the lash sweeps flat through the aim");
    }

    [DomainTest("Sable Scythe blade is three capsules along the hook, at the reach from the grip")]
    private static void SableScytheBlade()
    {
        AssertEqual(4, SableScytheMotion.BladeKnots.Length, "three capsules need four knots");
        AssertNear(13, SableScytheMotion.BladeRadius, 1e-6f, "26 px wide");
        AssertEqual(SableScytheMotion.BladeKnots[^1], Vector2.UnitX, "the last knot is the hook tip");
        var pose = new SablePose(.3f, 0, new Vector2(5, -3));
        AssertNear(CrimsonRewardRules.ScytheReach, Vector2.Distance(pose.Hand, SableScytheMotion.Tip(pose)), 1e-3f, "reach in a cutting plane");
        Span<Vector2> knots = stackalloc Vector2[4];
        SableScytheMotion.Blade(pose, knots);
        Vector2 tip = SableScytheMotion.Tip(pose);
        AssertEqual(true, SableScytheMotion.BladeTouches(knots, tip - new Vector2(4), tip + new Vector2(4)), "a box on the hook tip is cut");
        AssertEqual(false, SableScytheMotion.BladeTouches(knots, pose.Hand - new Vector2(4), pose.Hand + new Vector2(4)), "the haft at the grip does not hurt");
        // Edge-on mid-roll the hook projects onto the aim axis; in the Under plane it mirrors.
        var rolled = new SablePose(.6f, MathF.PI / 2, Vector2.Zero);
        AssertNear(0, SableScytheMotion.Tip(rolled).Y, 1e-3f, "edge-on, the tip lies on the aim axis");
        var under = new SablePose(.6f, MathF.PI, Vector2.Zero);
        AssertNear(-SableScytheMotion.Tip(new SablePose(.6f, 0, Vector2.Zero)).Y, SableScytheMotion.Tip(under).Y, 1e-3f, "the Under plane mirrors");
        // Facing mirrors across the aim; the aim rotates the frame.
        Vector2 local = new(10, 4);
        AssertEqual(true, Vector2.Distance(SableScytheMotion.ToWorld(local, 0, 1), local) < 1e-4f, "aim 0, facing right");
        AssertEqual(true, Vector2.Distance(SableScytheMotion.ToWorld(local, MathF.PI, -1), new Vector2(-10, 4)) < 1e-4f, "facing left keeps down down");
        AssertEqual(-1, SableScytheMotion.Facing(MathF.PI * .9f), "facing follows the aim");
    }

    [DomainTest("Sable Scythe staff lines last 360 ticks, then drain one per 30")]
    private static void SableScytheStaffLife()
    {
        AssertEqual(3, SableScytheMotion.LinesAt(3, 0), "fresh");
        AssertEqual(3, SableScytheMotion.LinesAt(3, 389), "still whole until the first drain");
        AssertEqual(2, SableScytheMotion.LinesAt(3, 390), "one drained after 390");
        AssertEqual(0, SableScytheMotion.LinesAt(3, 450), "all drained");
        AssertEqual(5, SableScytheMotion.LinesAt(9, 10), "capped at five");
        AssertEqual(510, SableScytheMotion.StaffLife(5), "360 + up to 150 drain");
        AssertEqual(0, SableScytheMotion.LinesAt(5, SableScytheMotion.StaffLife(5)), "the carrier retires with its last line");
        // The hanging staff: five lines 8 px apart, middle first, 40 px behind the shoulder.
        float[] rows = new float[5];
        for (int i = 0; i < 5; i++) rows[i] = SableScytheMotion.HangingLine(i, 1).Y;
        Array.Sort(rows);
        for (int i = 1; i < 5; i++) AssertNear(8, rows[i] - rows[i - 1], 1e-6f, "8 px apart");
        AssertEqual(0f, SableScytheMotion.HangingLine(0, 1).Y, "the first line is the middle");
        AssertEqual(-40f, SableScytheMotion.HangingLine(2, 1).X, "behind a right-facing reaper");
        AssertEqual(40f, SableScytheMotion.HangingLine(2, -1).X, "behind a left-facing reaper");
    }

    [DomainTest("Sable Scythe Staff Reap closes on the nearest target within 96 px of the cursor")]
    private static void SableScythePlacement()
    {
        Vector2 player = new(1000, 1000);
        // Cursor clamped within 720 px.
        AssertNear(720, Vector2.Distance(player, SableScytheMotion.ClampCursor(player, player + new Vector2(2000, 0))), 1e-3f, "cursor clamp");
        Vector2[] min = { new(1300, 950), new(1380, 900), new(2500, 900) }, max = { new(1340, 1010), new(1500, 1100), new(2600, 1000) };
        var placed = SableScytheMotion.Place(player, new Vector2(1355, 1000), min, max);
        AssertEqual(0, placed.Target, "the nearest hitbox to C wins");
        AssertEqual(new Vector2(1320, 980), placed.Center, "the staff centres on the target");
        AssertNear(15, placed.Spacing, 1e-4f, "spacing h / 4");
        var none = SableScytheMotion.Place(player, new Vector2(1000, 600), min, max);
        AssertEqual(-1, none.Target, "nothing within 96 px");
        AssertEqual(new Vector2(1000, 600), none.Center, "the staff centres on C");
        AssertEqual(36f, none.Spacing, "open spacing 36");
        AssertEqual(36f, SableScytheMotion.Place(player, new Vector2(1440, 1000), min, max).Spacing, "a 200 px tall target caps at 36");
        AssertEqual(14f, CrimsonRewardRules.StaffSpacing(20), "a tiny target floors at 14");
        // Every line crosses a target at least 56 px tall; a smaller one takes the middle three.
        foreach (float height in new[] { 56f, 80f, 144f, 400f })
        {
            float s = CrimsonRewardRules.StaffSpacing(height);
            AssertEqual(true, 2 * s <= height * .5f + 1e-3f, $"all five rows cross a {height} px target");
        }
        float small = CrimsonRewardRules.StaffSpacing(30);
        AssertEqual(true, small <= 15 + 1e-3f && 2 * small > 15, "a 30 px target takes the middle three");
    }

    [DomainTest("Sable Scythe Staff Reap fires the middle line first, outward on sixteenths, with the barline only at five")]
    private static void SableScytheCutSchedule()
    {
        var staff = new SableStaffPlacement(new Vector2(2000, 800), 20, 0);
        int[] fire = { 16, 23, 30, 37, 44 };
        float[] rows = { 0, -1, 1, -2, 2 };
        for (int k = 0; k < 5; k++)
        {
            SableCutPlan cut = SableScytheMotion.Cut(k, staff, 1500);
            AssertEqual(fire[k], cut.Fire, $"line {k} fires at 16 + S(k)");
            AssertNear(800 + rows[k] * 20, cut.Start.Y, 1e-4f, $"line {k} row");
            AssertNear(840, MathF.Abs(cut.Length), 1e-4f, "840 px long");
            float near = MathF.Min(cut.Start.X, cut.Start.X + cut.Length), far = MathF.Max(cut.Start.X, cut.Start.X + cut.Length);
            AssertEqual(true, MathF.Abs(near - 1580) < 1e-3f && MathF.Abs(far - 2420) < 1e-3f, "C.x +- 420");
            bool fromPlayer = MathF.Abs(cut.Start.X - 1580) < 1e-3f;
            AssertEqual(k % 2 == 0, fromPlayer, $"line {k} alternates its side, the middle from the player's side");
        }
        SableCutPlan bar = SableScytheMotion.Cut(5, staff, 1500);
        AssertEqual(51, bar.Fire, "the Final Barline at 16 + S(5)");
        AssertNear(4 * 20 + 48, bar.Length, 1e-4f, "height 4s + 48");
        AssertNear(800, bar.Start.Y + bar.Length * .5f, 1e-4f, "the pair is centred on the staff");
        AssertEqual(6, SableScytheMotion.Cuts(5), "five lines and the barline");
        AssertEqual(4, SableScytheMotion.Cuts(4), "no barline below five lines");
        AssertEqual(0, SableScytheMotion.Cuts(0), "no lines, no release");
        // Every part of a cast has dried within 90 ticks of the cast.
        for (int lines = 1; lines <= 5; lines++)
            for (int k = 0; k < SableScytheMotion.Cuts(lines); k++)
            {
                int f = k == 5 ? CrimsonRewardRules.BarlineTick : CrimsonRewardRules.StaffLineTick(k);
                AssertEqual(true, f + SableScytheMotion.CutEnd(k, lines) <= CrimsonRewardRules.StaffCutLife, $"cut {k} of {lines} retires within 90");
            }
        // A full staff dries together.
        AssertEqual(SableScytheMotion.CutEnd(0, 5) + 16, SableScytheMotion.CutEnd(4, 5) + 44, "a full staff's lines retire together");
        AssertEqual(1f, SableScytheMotion.LineScarFade(40, 0, 0, 5), "a full staff holds its scars until the barline dries");
        AssertNear(.5f, SableScytheMotion.LineScarFade(20, 0, 0, 2), 1e-4f, "a partial line dries on its own clock");
    }

    [DomainTest("Sable Scythe cuts are live behind their head for 10 ticks, opening over 3 (draw equals collide)")]
    private static void SableScytheCutLive()
    {
        var line = new SableCutPlan(new Vector2(0, 0), 840, 0, 16);
        Vector2 Box(float x) => new(x, -5);
        bool Hits(float x, float t) => SableScytheMotion.CutTouches(line, t, Box(x) - new Vector2(5, 0), Box(x) + new Vector2(5, 10));
        AssertEqual(false, Hits(600, 1), "not ahead of the head");
        AssertEqual(true, Hits(100, 2), "behind the head, opening");
        AssertEqual(true, Hits(800, 12), "the far end is live 10 ticks after the head passed it");
        AssertEqual(false, Hits(100, 13), "the near end dried after 10 ticks");
        AssertEqual(false, Hits(800, 15), "the whole line dried");
        AssertEqual(0f, SableScytheMotion.LiveRadius(12, 0), "a point opens from nothing");
        AssertNear(12, SableScytheMotion.LiveRadius(12, 3), 1e-5f, "fully open after 3 ticks");
        var bar = new SableCutPlan(new Vector2(0, 0), 128, 5, 51);
        AssertEqual(true, SableScytheMotion.CutTouches(bar, 4, new Vector2(10, 100), new Vector2(20, 110)), "the barline pair cuts its lower half after it fell");
        AssertEqual(false, SableScytheMotion.CutTouches(bar, 4, new Vector2(31, 60), new Vector2(40, 70)), "beside the pair, out of its 16 px reach");
        AssertEqual(true, SableScytheMotion.BarlineLive(15) && !SableScytheMotion.BarlineLive(16), "fall 3, live 12");
    }

    [DomainTest("Sable Scythe nominal: a full staff pays, a one-line release is worth less than swinging")]
    private static void SableScytheNominal()
    {
        AssertEqual(56, SableScytheMotion.ReleaseTicks(5), "the full release holds until 56");
        AssertEqual(24, SableScytheMotion.ReleaseTicks(1), "one line: released 8 ticks after it fires");
        float swing = CrimsonRewardRules.PerTick(3600, CrimsonRewardRules.MeasureMultiplier, CrimsonRewardRules.MeasureTicks);
        float oneLine = CrimsonRewardRules.PerTick(3600, CrimsonRewardRules.StaffReleaseMultiplier(1), SableScytheMotion.ReleaseTicks(1));
        float full = CrimsonRewardRules.PerTick(3600, CrimsonRewardRules.MeasureMultiplier + CrimsonRewardRules.StaffReleaseMultiplier(5),
            CrimsonRewardRules.MeasureTicks + SableScytheMotion.ReleaseTicks(5));
        AssertEqual(true, oneLine < swing, "a one-line release is worth less than swinging");
        AssertNear(286, full, .5f, "12.4x in 156 ticks");
        for (int lines = 1; lines < 5; lines++)
            AssertEqual(true, CrimsonRewardRules.StaffReleaseMultiplier(lines) / SableScytheMotion.ReleaseTicks(lines)
                < CrimsonRewardRules.StaffReleaseMultiplier(5) / SableScytheMotion.ReleaseTicks(5), $"{lines} lines pay less per tick than five");
        // The release pose whips forward at 16 and ends still (a held follow-through).
        AssertEqual(true, SableScytheMotion.Tip(SableScytheMotion.ReleasePose(12)).Y < -60, "drawn high behind before the whip");
        AssertEqual(true, SableScytheMotion.Tip(SableScytheMotion.ReleasePose(20)).X > 100, "whipped forward after 16");
    }

    [DomainTest("Sable Scythe combo cycles the measure and resets after 60 idle ticks")]
    private static void SableScytheCombo()
    {
        var combo = new SableCombo();
        ulong now = 50;
        for (int expected = 0; expected < 7; expected++)
        {
            int stroke = combo.Take(now);
            AssertEqual(expected % 5, stroke, "Over, Under, Over, Under, Whip, wrapping");
            now += (ulong)SableScytheMotion.Duration(stroke);
        }
        now += 60;
        AssertEqual(2, combo.Take(now), "60 idle ticks after a stroke's end keep the measure");
        now += (ulong)SableScytheMotion.Duration(2) + 61;
        AssertEqual(0, combo.Take(now), "61 idle ticks restart on the Over");
        combo.Take(now + 18);
        combo.Reset();
        AssertEqual(0, combo.Take(now + 40), "an item change, a release or death restarts on the Over");
    }

    [DomainTest("Sable Scythe commitment: unstarted parts die on Down or death, builds survive Down and swaps")]
    private static void SableScytheCommitment()
    {
        AssertEqual(false, SableScytheMotion.CutSurvives(-3, false), "an unstarted cut dies with a Down or dead owner");
        AssertEqual(true, SableScytheMotion.CutSurvives(-3, true), "an unstarted cut waits for a usable owner, item swaps included");
        AssertEqual(true, SableScytheMotion.CutSurvives(0, false), "a started cut finishes its window");
        AssertEqual(true, SableScytheMotion.StaffSurvives(true, false), "the staff survives Down and item swaps");
        AssertEqual(false, SableScytheMotion.StaffSurvives(true, true), "death clears the staff");
        AssertEqual(false, SableScytheMotion.StaffSurvives(false, false), "leaving clears the staff");
        AssertEqual(false, SableScytheMotion.HeldSurvives(false, true), "held strokes stop when the owner cannot act");
        AssertEqual(false, SableScytheMotion.HeldSurvives(true, false), "held strokes stop with an item swap");
    }

    [DomainTest("Sable Scythe projectiles reject invalid ai")]
    private static void SableScytheValidation()
    {
        AssertEqual(true, SableScytheMotion.ValidStroke(4, -2.5f, 28, 5, 1), "a valid stroke");
        foreach (var bad in new[] { (5f, 0f, 0f, -1f, 0f), (1.5f, 0f, 0f, -1f, 0f), (0f, float.NaN, 0f, -1f, 0f), (0f, 0f, 30f, -1f, 0f),
                     (0f, 0f, -1f, -1f, 0f), (0f, 0f, 0f, 6f, 0f), (0f, 0f, 0f, -2f, 0f), (0f, 0f, 0f, .5f, 0f), (0f, 0f, 0f, 0f, float.PositiveInfinity) })
            AssertEqual(false, SableScytheMotion.ValidStroke(bad.Item1, bad.Item2, bad.Item3, bad.Item4, bad.Item5), $"stroke {bad}");
        AssertEqual(true, SableScytheMotion.ValidStaff(5, 510, 0), "a valid staff");
        AssertEqual(false, SableScytheMotion.ValidStaff(0, 0, 0), "a staff carries at least one line");
        AssertEqual(false, SableScytheMotion.ValidStaff(6, 0, 0), "at most five");
        AssertEqual(false, SableScytheMotion.ValidStaff(2, float.NaN, 0), "finite age");
        AssertEqual(true, SableScytheMotion.ValidRelease(1, 3, 40, 4 * 32 + 20, -1), "a valid release");
        AssertEqual(false, SableScytheMotion.ValidRelease(1, 0, 0, -1, 0), "a release spends at least one line");
        AssertEqual(false, SableScytheMotion.ValidRelease(1, 5, 58, -1, 0), "a release ends by 56");
        AssertEqual(false, SableScytheMotion.ValidRelease(1, 5, 0, 160, 0), "packed stroke out of range");
        AssertEqual(true, SableScytheMotion.ValidCut(-840, 2 + 8 * 3, -30), "a valid line");
        AssertEqual(true, SableScytheMotion.ValidCut(4 * 36 + 48, 5 + 8 * 5, -51), "a valid barline");
        AssertEqual(false, SableScytheMotion.ValidCut(4 * 36 + 48, 5 + 8 * 4, -51), "a barline needs five lines");
        AssertEqual(false, SableScytheMotion.ValidCut(840, 3 + 8 * 3, 0), "line 3 needs four lines");
        AssertEqual(false, SableScytheMotion.ValidCut(500, 0 + 8 * 1, 0), "a line is 840 px");
        AssertEqual(false, SableScytheMotion.ValidCut(840, 0 + 8 * 1, -60), "waits at most until the barline");
        AssertEqual(false, SableScytheMotion.ValidCut(840, 0 + 8 * 1, 91), "retired within 90");
        AssertEqual(false, SableScytheMotion.ValidCut(float.NaN, 8, 0), "finite length");
    }
}
