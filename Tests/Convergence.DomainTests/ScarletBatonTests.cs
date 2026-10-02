using System;
using System.Numerics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;

namespace Convergence.DomainTests;

// Scarlet Baton (docs/encounters/crimson-foundry/REWARDS.md, "Magic - Scarlet Baton"): gesture motion, stroke geometry,
// the shape codec, the writing / ignition / river timelines and their collision spans, the score and commitment.
internal static partial class Program
{
    private static readonly float[] BatonAims = { 0, .5f, -.6f, 1.2f, MathF.PI, 2.6f, -2.2f, -1.5f };

    [DomainTest("Scarlet baton conducts the 4/4 pattern on clicks, back to Down after 90 idle ticks")]
    private static void ScarletBatonGestures()
    {
        AssertEqual(18, CrimsonRewardRules.Anticipation + CrimsonRewardRules.Sweep + CrimsonRewardRules.FollowThrough, "3 + 8 + 7 = one gesture");
        AssertEqual(CrimsonRewardRules.GestureTicks, BatonRules.Duration(BatonRules.Down), "gesture length");
        AssertEqual(CrimsonRewardRules.TuttiTicks, BatonRules.Duration(BatonRules.Tutti), "tutti pose");
        AssertEqual(11, BatonRules.Ictus, "the ictus ends the sweep");
        AssertEqual(5, BatonRules.WriteStart, "the pen writes for 6 ticks of the sweep");
        AssertEqual(BatonRules.Ictus, BatonRules.WriteEnd, "the head reaches the stroke's end on the ictus");
        AssertEqual(17, BatonRules.Written, "the last written point dries 6 ticks later, before the next gesture");
        AssertEqual(true, BatonRules.Written < CrimsonRewardRules.GestureTicks, "a stroke is written within its own gesture");
        AssertEqual(CrimsonRewardRules.TuttiLift, BatonRules.IctusTick(BatonRules.Tutti), "the downbeat at tick 8");

        int gesture = BatonRules.NextGesture(BatonRules.Rest, long.MaxValue);
        int[] bar = { BatonRules.Down, BatonRules.In, BatonRules.Out, BatonRules.Up, BatonRules.Down, BatonRules.In };
        for (int i = 0; i < bar.Length; i++)
        {
            AssertEqual(bar[i], gesture, $"beat {i}");
            gesture = BatonRules.NextGesture(gesture, 0);
        }
        AssertEqual(BatonRules.Out, BatonRules.NextGesture(BatonRules.In, CrimsonRewardRules.BatonIdleReset - 1), "89 idle ticks keep the bar");
        AssertEqual(BatonRules.Down, BatonRules.NextGesture(BatonRules.In, CrimsonRewardRules.BatonIdleReset), "90 idle ticks return to Down");
        AssertEqual(BatonRules.Down, BatonRules.NextGesture(BatonRules.Tutti, 0), "a new score starts on Down");
        AssertEqual(true, BatonRules.Continues(BatonRules.Up, 0) && BatonRules.Continues(BatonRules.Tutti, 1), "a held conductor continues the pose");
        AssertEqual(false, BatonRules.Continues(BatonRules.Up, 20) || BatonRules.Continues(BatonRules.Rest, 0), "a pause starts from rest");

        for (int g = BatonRules.Down; g <= BatonRules.Tutti; g++)
            for (int p = BatonRules.Rest; p <= BatonRules.Tutti; p++)
            {
                float code = BatonRules.EncodeGesture(g, p);
                AssertEqual(true, BatonRules.TryDecodeGesture(code, out int dg, out int dp) && dg == g && dp == p, $"gesture code {g}/{p}");
            }
        foreach (float bad in new[] { float.NaN, -1f, .5f, 30f, float.PositiveInfinity })
            AssertEqual(false, BatonRules.TryDecodeGesture(bad, out _, out _), $"rejects gesture code {bad}");
        AssertThrows<ArgumentOutOfRangeException>(() => BatonRules.EncodeGesture(5, 0), "no sixth gesture");
    }

    [DomainTest("Scarlet baton motion never stops and continues each gesture from the last pose and speed")]
    private static void ScarletBatonMotion()
    {
        const float dt = 1f / 32;
        for (int g = BatonRules.Down; g <= BatonRules.Tutti; g++)
            for (int previous = BatonRules.Rest; previous <= BatonRules.Tutti; previous++)
                foreach (float aim in BatonAims)
                    foreach (float previousAim in BatonAims)
                    {
                        if (previous == BatonRules.Rest && previousAim != BatonAims[0]) continue;
                        // Only the transitions a conductor can make: the next beat, the tutti, or a fresh start.
                        if (previous != BatonRules.Rest && g != BatonRules.Tutti && g != BatonRules.NextGesture(previous, 0)) continue;
                        int duration = BatonRules.Duration(g);
                        float slowest = float.MaxValue, fastest = 0;
                        for (float t = 0; t + dt <= duration; t += dt)
                        {
                            float speed = Vector2.Distance(BatonRules.Tip(g, previous, previousAim, aim, t), BatonRules.Tip(g, previous, previousAim, aim, t + dt)) / dt;
                            slowest = MathF.Min(slowest, speed); fastest = MathF.Max(fastest, speed);
                        }
                        string context = $"gesture {g} after {previous}, aim {aim} from {previousAim}";
                        AssertEqual(true, slowest >= 1f, $"{context}: never a stop ({slowest:0.00} px/tick)");
                        AssertEqual(true, fastest <= (g == BatonRules.Tutti ? 40 : 24), $"{context}: bounded, even turning to face the other way ({fastest:0.0} px/tick)");
                        // The tip lands on the gesture's ictus on its tick.
                        Vector2 ictus = BatonRules.FrameFor(aim).World(BatonRules.IctusPoint(g));
                        AssertNear(0, Vector2.Distance(ictus, BatonRules.Tip(g, previous, previousAim, aim, BatonRules.IctusTick(g))), .05f, $"{context}: ictus");
                        if (previous == BatonRules.Rest) continue;
                        // Joined to the previous gesture: same pose, same velocity.
                        int before = BatonRules.Duration(previous);
                        Vector2 end = BatonRules.Tip(previous, BatonRules.Rest, 0, previousAim, before);
                        Vector2 start = BatonRules.Tip(g, previous, previousAim, aim, 0);
                        AssertNear(0, Vector2.Distance(end, start), .01f, $"{context}: continuous pose");
                        const float dj = 1f / 256;
                        Vector2 velocityIn = (end - BatonRules.Tip(previous, BatonRules.Rest, 0, previousAim, before - dj)) / dj;
                        Vector2 velocityOut = (BatonRules.Tip(g, previous, previousAim, aim, dj) - start) / dj;
                        AssertNear(0, Vector2.Distance(velocityIn, velocityOut), .1f, $"{context}: continuous velocity");
                    }
        // Mirrored when facing left; tilted toward the aim, at most 40 degrees.
        var right = BatonRules.FrameFor(0);
        var left = BatonRules.FrameFor(MathF.PI);
        AssertNear(0, Vector2.Distance(new Vector2(-40, 14), left.World(new Vector2(40, 14))), 1e-3f, "mirrored");
        AssertNear(0, Vector2.Distance(new Vector2(40, 14), right.World(new Vector2(40, 14))), 1e-3f, "facing right, level");
        AssertNear(40 * MathF.PI / 180, BatonRules.FrameFor(-1.4f).Tilt * -1, 1e-4f, "tilt clamped at 40 degrees up");
    }

    [DomainTest("Scarlet baton strokes are centred on the cursor and steered by its sweep")]
    private static void ScarletBatonStrokeGeometry()
    {
        AssertEqual(new Vector2(500, 0), BatonRules.ClampCursor(Vector2.Zero, new Vector2(500, 0)), "inside 900 px");
        AssertNear(900, Vector2.Distance(new Vector2(10, 10), BatonRules.ClampCursor(new Vector2(10, 10), new Vector2(2000, 1500))), .01f, "clamped to 900 px");
        for (int g = BatonRules.Down; g <= BatonRules.Up; g++)
            foreach (int facing in new[] { 1, -1 })
            {
                var clean = BatonRules.Steer(g, facing, Vector2.Zero);
                Vector2 direction = BatonRules.ChordDirection(g, facing);
                AssertNear(90, clean.Half.Length(), 1e-3f, $"gesture {g}: half of a 180 px chord");
                AssertNear(0, Vector2.Distance(Vector2.Normalize(clean.Half), direction), 1e-4f, $"gesture {g}: a still cursor does not turn the chord");
                AssertEqual(BatonRules.CleanBend(g, facing), clean.Bend, $"gesture {g}: the clean mark's own bow");
                AssertEqual(0, clean.Skew, $"gesture {g}: no lean without a sweep");
                AssertEqual(-BatonRules.ChordDirection(g, 1).X, BatonRules.ChordDirection(g, -1).X, $"gesture {g}: mirrored when facing left");
                AssertEqual(-BatonRules.CleanBend(g, 1), BatonRules.CleanBend(g, -1), $"gesture {g}: its bow mirrors too");
            }
        var random = new Random(11);
        for (int i = 0; i < 4000; i++)
        {
            int g = random.Next(0, 4), facing = random.Next(0, 2) == 0 ? -1 : 1;
            Vector2 sweep = new(random.Next(-400, 400), random.Next(-400, 400));
            var s = BatonRules.Steer(g, facing, sweep);
            Vector2 d = BatonRules.ChordDirection(g, facing);
            float turned = MathF.Abs(BatonRules.SignedAngle(d, Vector2.Normalize(s.Half))) * 180 / MathF.PI;
            AssertEqual(true, turned <= CrimsonRewardRules.StrokeTurnDegrees + 1e-3f, "turns at most 35 degrees");
            if (sweep.Length() > 1 && turned > .5f)
                AssertEqual(true, MathF.Sign(BatonRules.SignedAngle(d, Vector2.Normalize(s.Half))) == MathF.Sign(BatonRules.SignedAngle(d, Vector2.Normalize(sweep))), "turns toward the sweep");
            AssertEqual(true, Math.Abs(s.Bend) <= CrimsonRewardRules.StrokeBendMax && Math.Abs(s.Skew) <= BatonRules.SkewMax, "bend and skew bounded");
            AssertNear(90, s.Half.Length(), 1e-3f, "the chord stays 180 px");
            var curve = BatonRules.Curve(new Vector2(1000, 800), s.Half, s.Bend, s.Skew);
            AssertNear(0, Vector2.Distance((curve.A + curve.B) * .5f, new Vector2(1000, 800)), 1e-3f, "centred on the cursor");
            Vector2 n = BatonRules.Perp(Vector2.Normalize(s.Half));
            AssertNear(s.Bend, Vector2.Dot(curve.Point(.5f) - new Vector2(1000, 800), n), 1e-2f, "the bend is the curve's bulge");
        }
        // A full sweep across the chord bends it the most; a held cursor gives the clean bow.
        var bent = BatonRules.Steer(BatonRules.Down, 1, BatonRules.Perp(BatonRules.ChordDirection(BatonRules.Down, 1)) * 200);
        AssertEqual(CrimsonRewardRules.StrokeBendMax, bent.Bend, "a wide sweep across bends it fully");
        // Every stroke stays rounder than the river's strip, so no ink folds on itself.
        foreach (int bend in new[] { -64, -32, 0, 32, 64 })
            foreach (int skew in new[] { -40, -20, 0, 20, 40 })
            {
                var c = BatonRules.Curve(Vector2.Zero, new Vector2(90, 0), bend, skew);
                float radius = float.MaxValue;
                for (int k = 0; k <= 400; k++)
                {
                    float s = k / 400f;
                    Vector2 d1 = 2 * (1 - s) * (c.K - c.A) + 2 * s * (c.B - c.K), d2 = 2 * (c.A - 2 * c.K + c.B);
                    float cross = MathF.Abs(d1.X * d2.Y - d1.Y * d2.X);
                    if (cross > 1e-6f) radius = MathF.Min(radius, MathF.Pow(d1.Length(), 3) / cross);
                }
                AssertEqual(true, radius >= BatonRules.MinCurveRadius, $"bend {bend} skew {skew}: radius {radius:0}");
            }
    }

    [DomainTest("Scarlet baton shape codec packs bend and skew exactly and rejects invalid ai")]
    private static void ScarletBatonShapeCodec()
    {
        for (int bend = -64; bend <= 64; bend++)
            for (int skew = -40; skew <= 40; skew += 3)
            {
                float packed = BatonRules.PackShape(bend, skew);
                AssertEqual(true, packed == MathF.Floor(packed) && packed >= 0 && packed < 1 << 24, "an exact integer");
                AssertEqual(true, BatonRules.TryUnpackShape(packed, out int b, out int k) && b == bend && k == skew, "round trip");
            }
        AssertEqual(false, BatonRules.TryUnpackShape(CrimsonRewardRules.PackPair(0, 41, BatonRules.ShapeHalf), out _, out _), "skew beyond 40");
        foreach (float bad in new[] { float.NaN, -1f, 2.5f, 129f * 129f, float.NegativeInfinity })
            AssertEqual(false, BatonRules.TryUnpackShape(bad, out _, out _), $"rejects shape {bad}");
        AssertThrows<ArgumentOutOfRangeException>(() => BatonRules.PackShape(65, 0), "bend beyond 64");
        AssertEqual(true, BatonRules.ValidHalf(new Vector2(0, 90)) && BatonRules.ValidHalf(new Vector2(63.64f, 63.64f)), "a 90 px half-chord");
        foreach (var bad in new[] { Vector2.Zero, new Vector2(100, 0), new Vector2(float.NaN, 90), new Vector2(0, float.PositiveInfinity) })
            AssertEqual(false, BatonRules.ValidHalf(bad), $"rejects half-chord {bad}");
    }

    [DomainTest("Scarlet baton writing is live behind its bead and collides exactly where it is drawn")]
    private static void ScarletBatonWriting()
    {
        var curve = BatonRules.Curve(new Vector2(400, 300), new Vector2(90, 0), 20, 10);
        Span<Vector2> points = stackalloc Vector2[BatonRules.MaxStrokeSamples];
        Span<float> along = stackalloc float[BatonRules.MaxStrokeSamples];
        int n = BatonRules.SampleStroke(curve, points, along);
        float length = along[n - 1];
        AssertEqual(true, n >= 16 && n <= BatonRules.MaxStrokeSamples, $"sampled densely ({n})");
        for (int i = 1; i < n; i++) AssertEqual(true, Vector2.Distance(points[i - 1], points[i]) <= BatonRules.StrokeSpacing + 1e-3f, "at most 12 px apart");
        AssertNear(0, Vector2.Distance(points[0], curve.A), 1e-3f, "starts at A");
        AssertNear(0, Vector2.Distance(points[n - 1], curve.B), 1e-3f, "ends at B");

        Span<BatonInkSpan> spans = stackalloc BatonInkSpan[2];
        AssertEqual(0, BatonRules.DormantSpans(4.9f, length, 0, spans), "nothing before the pen starts");
        AssertEqual(1, BatonRules.DormantSpans(8, length, 0, spans), "only live ink while the first ticks are written");
        AssertEqual(BatonLook.Live, spans[0].Look, "fresh ink is live");
        AssertNear(length * .5f, spans[0].To, 1e-3f, "the head is halfway at tick 8");
        AssertNear(0, spans[0].TimeAt(spans[0].To), 1e-4f, "the point under the bead has just ignited");
        AssertNear(3, spans[0].TimeAt(0), 1e-4f, "the first point ignited 3 ticks ago");
        AssertEqual(true, spans[0].Bead, "led by its bead");
        AssertEqual(2, BatonRules.DormantSpans(14, length, 0, spans), "dried behind, live ahead");
        AssertEqual(BatonLook.Dormant, spans[0].Look, "the dried part");
        AssertNear(length * .5f, spans[0].To, 1e-3f, "points live for 6 ticks after the head passes");
        AssertNear(CrimsonRewardRules.WriteLive, spans[1].TimeAt(spans[1].From), 1e-3f, "the live part's tail is 6 ticks old");
        AssertEqual(false, spans[1].Bead, "no bead once written");
        AssertEqual(1, BatonRules.DormantSpans(BatonRules.Written, length, 1, spans), "a written stroke is one dormant path");
        AssertEqual(BatonLook.Dormant, spans[0].Look, "dormant");
        AssertEqual(1f, spans[0].Warmth, "a full score warms its lips");

        // Draw equals collide: a box just inside the drawn rim is hit, one 2 px beyond it is not.
        BatonRules.DormantSpans(13, length, 0, spans);
        var live = spans[1];
        float u = (live.From + live.To) * .5f;
        Vector2 at = Lerp(points, along, n, u), normal = BatonRules.Perp(Vector2.Normalize(curve.B - curve.A));
        float radius = live.Radius * CrimsonRewardRules.InkOpen(live.TimeAt(u));
        foreach (float sign in new[] { 1f, -1f })
        {
            Vector2 edge = at + normal * sign * radius;
            AssertEqual(true, BatonRules.SpanTouchesBox(points[..n], along[..n], ReadOnlySpan<bool>.Empty, n, live, edge + normal * sign * -.5f - new Vector2(.1f), edge + normal * sign * -.5f + new Vector2(.1f)), "inside the rim");
            Vector2 outside = at + normal * sign * (radius + 2.5f);
            AssertEqual(false, BatonRules.SpanTouchesBox(points[..n], along[..n], ReadOnlySpan<bool>.Empty, n, live, outside - new Vector2(.1f), outside + new Vector2(.1f)), "outside the rim");
        }
        // The unwritten end does not hurt yet.
        BatonRules.DormantSpans(7, length, 0, spans);
        AssertEqual(false, BatonRules.SpanTouchesBox(points[..n], along[..n], ReadOnlySpan<bool>.Empty, n, spans[0], curve.B - new Vector2(4), curve.B + new Vector2(4)), "ahead of the head");
        // A tapered capsule never reaches more than 1 px beyond its drawn radius.
        AssertEqual(true, BatonRules.TaperTouchesBox(new Vector2(0, 0), new Vector2(100, 0), 0, 20, new Vector2(50, 10), new Vector2(51, 11)), "r = 10 at the middle");
        AssertEqual(false, BatonRules.TaperTouchesBox(new Vector2(0, 0), new Vector2(100, 0), 0, 20, new Vector2(50, 11.5f), new Vector2(51, 12)), "but not 11.5");
    }

    [DomainTest("Scarlet baton score keeps eight strokes and dries the oldest away without a flash")]
    private static void ScarletBatonScore()
    {
        for (int dormant = 0; dormant <= 9; dormant++)
            AssertEqual(dormant >= 8 ? dormant - 7 : 0, BatonRules.Evictions(dormant), $"{dormant} dormant");
        AssertEqual(1f, BatonRules.NaturalOpacity(420), "full until the last 60 ticks");
        AssertEqual(.5f, BatonRules.NaturalOpacity(450), "fading over the last 60");
        AssertEqual(0f, BatonRules.NaturalOpacity(480), "gone at 480");
        for (int clock = 0; clock <= CrimsonRewardRules.StrokeLife; clock += 7)
        {
            float before = BatonRules.NaturalOpacity(clock);
            float after = BatonRules.DryOpacity(BatonRules.DryAwayStart(before));
            AssertEqual(true, after <= before + 1e-4f && before - after <= 1f / CrimsonRewardRules.StrokeDryAway + 1e-4f, $"no flash at {clock}");
        }
        AssertEqual(0, BatonRules.DryAwayStart(1), "a fresh stroke takes the full 24 ticks");
        AssertEqual(0f, BatonRules.DryOpacity(CrimsonRewardRules.StrokeDryAway), "dried away in 24 ticks");
        AssertEqual(64, CrimsonRewardRules.BatonMana * CrimsonRewardRules.MaxStrokes, "64 mana per score");
        AssertNear(4, CrimsonRewardRules.BatonBuildMultiplier(8), 1e-4f, "eight written strokes, 0.5x each");
        AssertNear(19, CrimsonRewardRules.TuttiMultiplier(8), 1e-4f, "eight ignitions and the river");
        AssertNear(14, CrimsonRewardRules.TuttiMultiplier(7) + 1.75f, 1e-4f, "no river below eight");
    }

    [DomainTest("Scarlet baton tutti ignites in writing order and only a full score raises the river")]
    private static void ScarletBatonTutti()
    {
        for (int rank = 0; rank < CrimsonRewardRules.MaxStrokes; rank++)
        {
            // The owner schedules in its item use; the cast tick's own stroke update counts once (clock 0).
            var state = CrimsonStrokeState.Scheduled(BatonRules.Countdown(rank), rank, 5).Tick();
            int clock = 0;
            while (state.Kind == CrimsonStrokeKind.Scheduled) { clock++; state = state.Tick(); }
            AssertEqual(CrimsonRewardRules.IgniteTick(rank), clock, $"rank {rank} ignites on its tick");
            AssertEqual(8 + CrimsonRewardRules.S(rank), clock, $"rank {rank} at 8 + S({rank})");
        }
        AssertEqual(true, BatonRules.Countdown(CrimsonRewardRules.MaxStrokes - 1) <= CrimsonStrokeState.MaxCountdown, "the last countdown fits 7 bits");
        for (int n = 0; n <= 9; n++) AssertEqual(n == 8, BatonRules.RiverFor(n), $"the river only at exactly eight ({n})");
        // The ignition swells from 8 to 24 in 3 ticks: the material's opening lands exactly on it.
        AssertNear(8, BatonRules.IgniteRadiusAt(0), 1e-4f, "radius 8 at ignition");
        AssertNear(16, BatonRules.IgniteRadiusAt(1.5f), 1e-4f, "halfway");
        AssertNear(24, BatonRules.IgniteRadiusAt(3), 1e-4f, "24 after 3 ticks");
        AssertNear(24, BatonRules.IgniteRadiusAt(12), 1e-4f, "then a river");
        AssertEqual(17, BatonRules.IgniteEnd, "swell 3, live 14");
        var ignited = BatonRules.IgnitedSpan(16, 200, 0, false);
        AssertEqual(BatonLook.Live, ignited.Look, "live through tick 16");
        AssertEqual(BatonLook.Residue, BatonRules.IgnitedSpan(17, 200, 0, false).Look, "then a scar");
        AssertNear(1, BatonRules.ScarFade(17, 3, false), 1e-4f, "the scar starts fresh");
        AssertNear(0, BatonRules.ScarFade(17 + 24, 3, false), 1e-4f, "and dries in 24 ticks");
        // A full score holds every scar until the river has run, then dries with it.
        AssertEqual(71, CrimsonRewardRules.RiverTick, "the river at 8 + S(9)");
        AssertEqual(38, BatonRules.RiverDryStart, "head 24, live 14");
        AssertEqual(true, CrimsonRewardRules.RiverTick + BatonRules.RiverEnd <= CrimsonRewardRules.RiverMaxLife, "the river ends within 135 ticks of the cast");
        for (int rank = 0; rank < 8; rank++)
        {
            AssertEqual(CrimsonRewardRules.RiverTick + BatonRules.RiverEnd, CrimsonRewardRules.IgniteTick(rank) + BatonRules.StrokeEnd(rank, true), $"rank {rank} dries with the river");
            float hold = CrimsonRewardRules.RiverTick + BatonRules.RiverDryStart - CrimsonRewardRules.IgniteTick(rank);
            AssertNear(1, BatonRules.ScarFade(hold, rank, true), 1e-4f, $"rank {rank} held until the river dries");
            AssertNear(.5f, BatonRules.ScarFade(hold + 12, rank, true), 1e-4f, $"rank {rank} drying with it");
        }
        AssertEqual(-72, BatonRules.RiverSpawnAge, "spawned in the cast's item use, 0 on tick 71");
    }

    [DomainTest("Scarlet baton river runs through the strokes in writing order and skips runs over 900 px")]
    private static void ScarletBatonRiver()
    {
        // Eight strokes along a line; the gap after the third is over 900 px.
        Span<BatonCurve> strokes = stackalloc BatonCurve[8];
        float x = 0;
        for (int i = 0; i < 8; i++)
        {
            if (i == 3) x += 1100;
            strokes[i] = BatonRules.Curve(new Vector2(x + 90, 400 + (i % 2) * 60), new Vector2(90, 0), i % 2 == 0 ? 30 : -30, 0);
            x += 300;
        }
        Span<Vector2> points = stackalloc Vector2[CrimsonRewardRules.RiverMaxVertices];
        Span<float> along = stackalloc float[CrimsonRewardRules.RiverMaxVertices];
        Span<bool> breaks = stackalloc bool[CrimsonRewardRules.RiverMaxVertices];
        int n = BatonRules.BuildRiver(strokes, points, along, breaks, out float length);
        AssertEqual(true, n > 8 && n <= CrimsonRewardRules.RiverMaxVertices, $"bounded ({n})");
        int breakCount = 0;
        for (int i = 0; i < n; i++)
        {
            if (breaks[i]) breakCount++;
            if (i > 0) AssertEqual(true, along[i] >= along[i - 1], "arc length never runs back");
        }
        AssertEqual(1, breakCount, "one omitted run");
        AssertNear(0, Vector2.Distance(points[0], strokes[0].A), 1e-3f, "starts with the first stroke");
        AssertNear(0, Vector2.Distance(points[n - 1], strokes[7].B), 1e-3f, "ends with the last");
        AssertNear(length, along[n - 1], 1e-3f, "length over the included parts");
        // The omitted run adds no length (the head jumps it), and every stroke end and start is on the river.
        float total = 0;
        Span<Vector2> sp = stackalloc Vector2[BatonRules.MaxStrokeSamples];
        Span<float> sa = stackalloc float[BatonRules.MaxStrokeSamples];
        for (int i = 0; i < 8; i++)
        {
            int k = BatonRules.SampleStroke(strokes[i], sp, sa);
            total += sa[k - 1];
            AssertEqual(true, Contains(points[..n], strokes[i].A) && Contains(points[..n], strokes[i].B), $"stroke {i} is on the river");
        }
        AssertEqual(true, length > total && length < total + 7 * 300, "strokes plus the included runs");
        // Worst case: every run 880 px long (724 across, 500 up or down) still fits the vertex budget.
        for (int i = 0; i < 8; i++) strokes[i] = BatonRules.Curve(new Vector2(i * 904 + 90, 300 + (i % 2) * 500), new Vector2(90, 0), 64, 40);
        n = BatonRules.BuildRiver(strokes, points, along, breaks, out length);
        int worstBreaks = 0;
        for (int i = 0; i < n; i++) if (breaks[i]) worstBreaks++;
        AssertEqual(true, n <= CrimsonRewardRules.RiverMaxVertices && n > 16 && worstBreaks == 0, $"a long river within budget ({n}, {worstBreaks} breaks)");
        AssertNear(0, Vector2.Distance(points[n - 1], strokes[7].B), 1e-3f, "and it still ends at the last stroke");

        // The head covers the path in 24 ticks; each point is live 14 ticks after it passes; then it all dries.
        Span<BatonInkSpan> spans = stackalloc BatonInkSpan[2];
        AssertEqual(0, BatonRules.RiverSpans(-1, length, spans), "nothing while waiting");
        AssertEqual(1, BatonRules.RiverSpans(12, length, spans), "live behind the head");
        AssertNear(length * .5f, spans[0].To, 1e-2f, "halfway at tick 12");
        AssertNear(0, spans[0].TimeAt(spans[0].To), 1e-3f, "the head has just ignited");
        AssertEqual(2, BatonRules.RiverSpans(30, length, spans), "a held scar behind the live stretch");
        AssertEqual(BatonLook.Residue, spans[0].Look, "scar");
        AssertNear(1, spans[0].Time0, 1e-4f, "held at full until the river dries");
        AssertNear(14, spans[1].TimeAt(spans[1].From), 1e-3f, "each point live for 14 ticks");
        AssertEqual(1, BatonRules.RiverSpans(50, length, spans), "drying together");
        AssertNear(.5f, spans[0].Time0, 1e-4f, "half dried 12 ticks after the last point");
        AssertEqual(0, BatonRules.RiverSpans(BatonRules.RiverEnd, length, spans), "gone at 62");
    }

    [DomainTest("Scarlet baton commitment: Down kills unstarted parts, death clears the build, live parts finish")]
    private static void ScarletBatonCommitment()
    {
        foreach (var kind in new[] { CrimsonStrokeKind.Dormant, CrimsonStrokeKind.Scheduled, CrimsonStrokeKind.Live, CrimsonStrokeKind.Residue })
            AssertEqual(true, BatonRules.Survives(kind, false, false), $"{kind} stays while usable");
        AssertEqual(true, BatonRules.Survives(CrimsonStrokeKind.Dormant, false, true), "the build survives Down");
        AssertEqual(false, BatonRules.Survives(CrimsonStrokeKind.Scheduled, false, true), "an unstarted ignition dies on Down");
        AssertEqual(true, BatonRules.Survives(CrimsonStrokeKind.Live, false, true), "a burning stroke finishes through Down");
        AssertEqual(false, BatonRules.Survives(CrimsonStrokeKind.Dormant, true, true), "death clears the build");
        AssertEqual(false, BatonRules.Survives(CrimsonStrokeKind.Residue, true, true), "and the stroke drying away");
        AssertEqual(true, BatonRules.Survives(CrimsonStrokeKind.Live, true, true), "a burning stroke finishes through death");
        AssertEqual(false, BatonRules.RiverSurvives(-5, false, true), "a waiting river dies on Down");
        AssertEqual(false, BatonRules.RiverSurvives(-5, true, true), "and on death");
        AssertEqual(true, BatonRules.RiverSurvives(0, true, true), "a running river finishes");
    }

    private static Vector2 Lerp(ReadOnlySpan<Vector2> points, ReadOnlySpan<float> along, int n, float u)
    {
        for (int i = 1; i < n; i++)
            if (along[i] >= u) return Vector2.Lerp(points[i - 1], points[i], (u - along[i - 1]) / Math.Max(1e-5f, along[i] - along[i - 1]));
        return points[n - 1];
    }

    private static bool Contains(ReadOnlySpan<Vector2> points, Vector2 point)
    {
        foreach (var p in points) if (Vector2.Distance(p, point) < 1e-2f) return true;
        return false;
    }
}
