using System;
using System.Numerics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using F = Convergence.Content.Encounters.CrimsonFoundry.Rewards.SableCrescentFlight;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.DomainTests;

// Sable Scythe crescents (docs/encounters/crimson-foundry/REWARDS.md, "Melee - Sable Scythe", Crescents): the throws and
// the volley, the body (draw equals collide), the steering bounds, reaching targets without orbiting, target choice and
// chaining over hitboxes, the live cap and throw spacing, engraving one line per stroke, the lifetime, the replicated
// state and the Melee budget.
internal static partial class Program
{
    private static float Degrees(float radians) => radians * 180 / MathF.PI;

    [DomainTest("Sable Scythe crescents leave at age 9 off the aim, and the lash arc sheds a fanned volley of five at 20")]
    private static void SableCrescentThrows()
    {
        AssertEqual(9, R.CrescentThrowAge, "Over and Under throw at age 9");
        AssertEqual(20, R.VolleyAge, "the volley leaves at Whip age 20, as the lash finishes writing");
        AssertNear(14, Degrees(F.ThrowHeading(0, 1, F.Over)), 1e-4f, "the Over's crescent leaves 14 degrees down");
        AssertNear(-14, Degrees(F.ThrowHeading(0, 1, F.Under)), 1e-4f, "the Under's leaves 14 degrees up");
        AssertNear(180 - 14, Degrees(F.ThrowHeading(MathF.PI, -1, F.Over)), 1e-3f, "mirrored when facing left: still down");
        // The cut travels down through the aim in the Over (+y in the aim frame) and up in the Under: the crescent leans
        // toward the side the cut is moving, so consecutive crescents cross on the target.
        foreach (var (stroke, kind) in new[] { (0, F.Over), (1, F.Under) })
        {
            Vector2 a = SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, R.CrescentThrowAge)),
                b = SableScytheMotion.Tip(SableScytheMotion.Pose(stroke, R.CrescentThrowAge + .05f));
            AssertEqual(MathF.Sign(b.Y - a.Y), MathF.Sign(F.ThrowHeading(0, 1, kind)), $"stroke {stroke} throws toward its cut's travel");
            AssertEqual(true, a.X > R.ScytheReach * .9f && MathF.Abs(a.Y) < 10, $"stroke {stroke} throws from the hook tip at the aim crossing");
        }
        float[] written = { 14.6f, 15.8f, 17f, 18.2f, 19.4f };
        int[] hold = { 10, 8, 6, 8, 10 };
        for (int k = 0; k < 5; k++)
        {
            AssertNear(written[k], F.VolleyWritten(k), 1e-4f, $"volley crescent {k} leaves the arc point written at {written[k]}");
            AssertEqual(hold[k], F.Hold(F.FirstVolley + k), $"volley crescent {k} holds its heading {hold[k]} ticks");
        }
        AssertEqual(6, F.Hold(F.Over), "an Over's crescent holds 6 ticks");
        AssertEqual(14f, F.LaunchSpeed(F.Under), "14 px/tick");
        AssertEqual(10f, F.LaunchSpeed(F.FirstVolley + 3), "volley 10 px/tick");
        // The fan opens the way the arc was written: neighbours stay neighbours and their paths do not cross as they leave.
        Span<Vector2> arc = stackalloc Vector2[25];
        for (int i = 0; i < arc.Length; i++) arc[i] = SableScytheMotion.Tip(SableScytheMotion.Pose(4, R.WhipLiveStart + i / 4f));
        float[] headings = new float[5];
        Vector2[] births = new Vector2[5];
        for (int k = 0; k < 5; k++)
        {
            headings[k] = F.VolleyHeading(0, k, arc[0], arc[^1]);
            births[k] = F.ArcPoint(arc, F.VolleyWritten(k), 4);
            AssertNear(0, Vector2.Distance(births[k], SableScytheMotion.Tip(SableScytheMotion.Pose(4, F.VolleyWritten(k)))), 1.5f, $"volley {k} on the written arc");
        }
        AssertNear(0, headings[2], 1e-5f, "the middle crescent flies along the aim");
        for (int k = 0; k < 4; k++) AssertNear(16, MathF.Abs(Degrees(headings[k + 1] - headings[k])), 1e-3f, "16 degrees apart");
        AssertEqual(MathF.Sign(arc[0].Y - arc[^1].Y), MathF.Sign(headings[0]), "the first crescent leans toward the side where the arc began");
        for (int i = 0; i < 5; i++)
            for (int j = i + 1; j < 5; j++)
                AssertEqual(false, PathsCross(births[i], headings[i], births[j], headings[j], 400), $"volley {i} and {j} do not cross as they leave");
        // Apex and centre: born with the apex on the point.
        Vector2 heading = F.Unit(.7f), apex = new(100, 50);
        AssertNear(0, Vector2.Distance(apex, F.Apex(F.CenterFromApex(apex, heading, F.Over), heading, F.Over)), 1e-4f, "apex round trip");
        // One held measure: Over 9, Under 27, Over 45, Under 63, the volley's five at 92.
        Span<int> ticks = stackalloc int[16];
        int count = F.MeasureThrows(ticks);
        AssertEqual("9,27,45,63,92,92,92,92,92", string.Join(",", ticks[..count].ToArray()), "a measure's throws");
    }

    private static bool PathsCross(Vector2 a, float ha, Vector2 b, float hb, float length)
    {
        Vector2 da = F.Unit(ha) * length, db = F.Unit(hb) * length, ab = b - a;
        float cross = da.X * db.Y - da.Y * db.X;
        if (MathF.Abs(cross) < 1e-6f) return false;
        float t = (ab.X * db.Y - ab.Y * db.X) / cross, u = (ab.X * da.Y - ab.Y * da.X) / cross;
        return t > 0 && t < 1 && u > 0 && u < 1;
    }

    [DomainTest("Sable Scythe crescent body is the drawn arc: seven samples, six capsules, opened over 3 ticks")]
    private static void SableCrescentBody()
    {
        Span<Vector2> at = stackalloc Vector2[R.CrescentSamples];
        Span<float> radius = stackalloc float[R.CrescentSamples];
        Vector2 center = new(500, 300), heading = Vector2.UnitX;
        F.Body(center, heading, F.Over, 1, at, radius);
        AssertNear(0, Vector2.Distance(at[F.ApexSample], F.Apex(center, heading, F.Over)), 1e-4f, "the middle sample is the apex");
        AssertNear(0, Vector2.Distance(at[0], center + new Vector2(-8, -30)), 1e-4f, "a tip lies h/2 behind the centre, w/2 across");
        AssertNear(0, Vector2.Distance(at[^1], center + new Vector2(-8, 30)), 1e-4f, "the other tip");
        AssertNear(9, radius[F.ApexSample], 1e-5f, "R at the apex");
        AssertNear(9 * .35f, radius[0], 1e-5f, "R (1 - 0.65) at the tips");
        F.Body(center, heading, F.FirstVolley, 1, at, radius);
        AssertNear(22, MathF.Abs(at[0].Y - center.Y), 1e-4f, "a volley crescent is 44 px wide");
        AssertNear(7, radius[F.ApexSample], 1e-5f, "and R 7");
        // Collision: only the capsules (each with its smaller end's radius), nothing ahead of the apex's rim.
        F.Body(center, heading, F.Over, 1, at, radius);
        Vector2 apex = at[F.ApexSample];
        AssertEqual(true, F.Touches(center, F.Over, at, radius, apex + new Vector2(2, -2), apex + new Vector2(4, 2)), "the apex cuts");
        AssertEqual(false, F.Touches(center, F.Over, at, radius, apex + new Vector2(12, -2), apex + new Vector2(16, 2)), "beyond the apex's radius it does not");
        AssertEqual(false, F.Touches(center, F.Over, at, radius, center + new Vector2(-30, -4), center + new Vector2(-26, 4)), "behind the chord it does not");
        AssertEqual(true, F.Touches(center, F.Over, at, radius, at[0] - new Vector2(1), at[0] + new Vector2(1)), "a tip cuts");
        F.Body(center, heading, F.Over, R.InkOpen(0), at, radius);
        AssertEqual(false, F.Touches(center, F.Over, at, radius, apex - new Vector2(1), apex + new Vector2(1)), "nothing before it opens");
        AssertNear(47, F.Broadphase(F.Over), 1e-4f, "broadphase w/2 + R + 8");
    }

    // One crescent flown against a box (centre, half size, velocity) until it touches, returning the hit age (or -1),
    // the tightest curve radius and the largest turn per update while held.
    private static (float Hit, float Curve, float HeldTurn, float MaxSpeed) FlyAt(int kind, Vector2 apex, float heading, Vector2 target, Vector2 half, Vector2 targetVelocity)
    {
        Vector2 f = F.Unit(heading), c = F.CenterFromApex(apex, f, kind), v = f * F.LaunchSpeed(kind);
        float age = 0, dt = F.Dt, curve = float.MaxValue, held = 0, maxSpeed = 0;
        Span<Vector2> at = stackalloc Vector2[R.CrescentSamples];
        Span<float> radius = stackalloc float[R.CrescentSamples];
        while (age < R.CrescentLife)
        {
            age += dt; target += targetVelocity * dt;
            Vector2 next = F.Steer(v, c, age, kind, F.AimPoint(c, target, targetVelocity, v.Length()), dt);
            float turn = MathF.Abs(F.Wrap(MathF.Atan2(next.Y, next.X) - MathF.Atan2(v.Y, v.X)));
            AssertEqual(true, turn <= F.TurnLimit(age, kind) * dt + 1e-5f, $"kind {kind} age {age}: the turn never exceeds w");
            if (age <= F.Hold(kind)) held = MathF.Max(held, turn);
            if (turn > 1e-6f) curve = MathF.Min(curve, next.Length() * dt / turn);
            maxSpeed = MathF.Max(maxSpeed, next.Length());
            v = next; c += v * dt;
            F.Body(c, Vector2.Normalize(v), kind, R.InkOpen(age), at, radius);
            if (F.Touches(c, kind, at, radius, target - half, target + half)) return (age, curve, held, maxSpeed);
        }
        return (-1, curve, held, maxSpeed);
    }

    [DomainTest("Sable Scythe crescents steer within 0.16 rad/tick, curve no tighter than 75 px and hold the throw first")]
    private static void SableCrescentSteering()
    {
        AssertNear(75, F.TightestCurve, 1e-3f, "12 / 0.16");
        AssertEqual(0f, F.TurnLimit(6, F.Over), "nothing turns while the heading is held");
        AssertNear(.16f, F.TurnLimit(16, F.Over), 1e-6f, "full rate 10 ticks after the hold");
        AssertNear(.08f, F.TurnLimit(11, F.Over), 1e-6f, "eased in between");
        AssertNear(20, F.CruiseFor(0), 1e-5f, "cruise 20 px/tick");
        AssertNear(12, F.CruiseFor(MathF.PI * .5f), 1e-5f, "12 px/tick in a hard turn");
        AssertNear(12, F.CruiseFor(MathF.PI), 1e-5f, "and behind");
        // Lead: up to 8 ticks of the target's velocity.
        AssertNear(0, Vector2.Distance(new Vector2(1000, 40), F.AimPoint(Vector2.Zero, new Vector2(1000, 0), new Vector2(0, 5), 20)), 1e-3f, "lead capped at 8 ticks");
        AssertNear(0, Vector2.Distance(new Vector2(100, 25), F.AimPoint(Vector2.Zero, new Vector2(100, 0), new Vector2(0, 5), 20)), 1e-3f, "lead distance / speed");
        // No target: straight on, easing to 20.
        Vector2 v = new(10, 0);
        for (float age = .5f; age < 30; age += .5f) v = F.Steer(v, Vector2.Zero, age, F.Over, null, F.Dt);
        AssertNear(0, v.Y, 1e-4f, "without a target it flies straight");
        AssertNear(20, v.Length(), .1f, "and eases to 20");
        // A sweep of throws at every kind and target placement: the turn bound (asserted per update inside FlyAt), the
        // tightest curve, the held heading and the speed cap.
        var rng = new Random(11);
        Span<Vector2> arc = stackalloc Vector2[25];
        for (int i = 0; i < arc.Length; i++) arc[i] = SableScytheMotion.Tip(SableScytheMotion.Pose(4, R.WhipLiveStart + i / 4f));
        for (int trial = 0; trial < 600; trial++)
        {
            int kind = trial % F.Kinds;
            float angle = (float)(rng.NextDouble() * 2 * Math.PI), distance = 100 + (float)rng.NextDouble() * 700;
            Vector2 target = F.Unit(angle) * distance, moving = trial % 3 == 0 ? new Vector2((float)rng.NextDouble() * 8 - 4, (float)rng.NextDouble() * 8 - 4) : Vector2.Zero;
            var (apex, heading) = ThrowOf(kind, arc);
            var result = FlyAt(kind, apex, heading, target, new Vector2(4), moving);
            AssertEqual(true, result.Curve >= F.TightestCurve - .5f, $"trial {trial}: tightest curve {result.Curve:0.0} px");
            AssertEqual(true, result.HeldTurn < 1e-5f, $"trial {trial}: the throw's heading is held");
            AssertEqual(true, result.MaxSpeed <= R.CrescentCruise + 1e-3f, $"trial {trial}: at most 20 px/tick");
        }
    }

    private static (Vector2 Apex, float Heading) ThrowOf(int kind, ReadOnlySpan<Vector2> arc)
    {
        if (!F.IsVolley(kind))
            return (SableScytheMotion.Tip(SableScytheMotion.Pose(kind == F.Under ? 1 : 0, R.CrescentThrowAge)), F.ThrowHeading(0, 1, kind));
        int k = F.VolleyIndex(kind);
        return (F.ArcPoint(arc, F.VolleyWritten(k), 4), F.VolleyHeading(0, k, arc[0], arc[^1]));
    }

    [DomainTest("Sable Scythe crescents reach still targets in front of the reaper without orbiting them")]
    private static void SableCrescentReach()
    {
        Span<Vector2> arc = stackalloc Vector2[25];
        for (int i = 0; i < arc.Length; i++) arc[i] = SableScytheMotion.Tip(SableScytheMotion.Pose(4, R.WhipLiveStart + i / 4f));
        int flights = 0;
        for (int degrees = -75; degrees <= 75; degrees += 15)
            for (int distance = 150; distance <= 700; distance += 50)
                foreach (float half in new[] { 12f, 40f })
                    for (int kind = 0; kind < F.Kinds; kind++)
                    {
                        var (apex, heading) = ThrowOf(kind, arc);
                        Vector2 target = F.Unit(degrees * MathF.PI / 180) * distance;
                        var result = FlyAt(kind, apex, heading, target, new Vector2(half), Vector2.Zero);
                        AssertEqual(true, result.Hit > 0, $"kind {kind}: a {half * 2} px target {distance} px out at {degrees} degrees is reached");
                        flights++;
                    }
        AssertEqual(true, flights > 1500, "the sweep covers the cone");
        // The turning circle: a point behind the beam and inside the tightest circle is held past; ahead of it, never.
        AssertEqual(true, F.InsideTurn(new Vector2(-10, 60), 0, 1.75f), "behind the beam, inside: hold");
        AssertEqual(false, F.InsideTurn(new Vector2(10, 60), 0, 1.4f), "ahead of the beam: turn");
        AssertEqual(false, F.InsideTurn(new Vector2(-10, 200), 0, 1.6f), "outside the circle: turn");
    }

    [DomainTest("Sable Scythe crescent targets: nearest the cursor, else a forward cone, then chains over hitboxes")]
    private static void SableCrescentTargets()
    {
        Vector2 player = new(1000, 1000);
        AssertNear(960, Vector2.Distance(player, F.ClampCursor(player, player + new Vector2(5000, 0))), 1e-3f, "C within 960 px");
        Vector2[] min = { new(1600, 960), new(1180, 900), new(900, 1200), new(1250, 1300) };
        Vector2[] max = { new(1700, 1040), new(1220, 960), new(940, 1240), new(1290, 1340) };
        bool[] none = new bool[4];
        Vector2 at = new(1150, 1000), heading = Vector2.UnitX;
        AssertEqual(0, F.Acquire(new Vector2(1500, 1000), at, heading, min, max, none), "the hitbox nearest C within 240 px wins, not the nearest to the crescent");
        AssertEqual(1, F.Acquire(new Vector2(1000, 400), at, heading, min, max, none), "nothing near C: the nearest within 400 px in the 75 degree cone");
        AssertEqual(1, F.Acquire(new Vector2(1000, 400), at, heading, min, max, new[] { false, false, true, true }), "the cone ignores what is behind");
        AssertEqual(3, F.Acquire(new Vector2(1000, 400), at, heading, min, max, new[] { false, true, false, false }), "a hit root is skipped for the next in the cone");
        AssertEqual(-1, F.Acquire(new Vector2(1000, 400), at, heading, min, max, new[] { false, true, false, true }), "nothing else qualifies");
        AssertEqual(-1, F.Acquire(new Vector2(1000, 400), at, -heading, min, max, new[] { false, false, true, false }), "behind the crescent and far from C: none");
        // Chain: the nearest within 320 px of the crescent in any direction, not the one it hit.
        Vector2 hit = new(1240, 1050);
        AssertEqual(3, F.Chain(hit, min, max, new[] { false, true, false, false }), "the nearest other hitbox within 320 px");
        AssertEqual(-1, F.Chain(new Vector2(3000, 3000), min, max, none), "nothing within 320 px: it breaks");
        AssertEqual(true, F.InAcquireRange(new Vector2(1500, 1000), at, min[0], max[0]), "pre-filter: near C");
        AssertEqual(false, F.InAcquireRange(new Vector2(0, 0), new Vector2(0, 0), min[0], max[0]), "pre-filter: neither");
        AssertEqual(true, F.InKeepRange(new Vector2(0, 0), new Vector2(1400, 0), new Vector2(1500, 10)), "kept within 1,400 px");
        AssertEqual(false, F.InKeepRange(new Vector2(0, 0), new Vector2(1401, 0), new Vector2(1500, 10)), "lost beyond it");
    }

    [DomainTest("Sable Scythe crescents: a held loop peaks at exactly 8 in flight, throws keep 18 ticks apart, one line per stroke")]
    private static void SableCrescentCapAndLedger()
    {
        // Measures back to back: every crescent lives 70 ticks at most.
        Span<int> measure = stackalloc int[16];
        int perMeasure = F.MeasureThrows(measure);
        int peak = 0;
        for (int tick = 0; tick < 1000; tick++)
        {
            int live = 0;
            for (int m = 0; m * R.MeasureTicks <= tick; m++)
                for (int i = 0; i < perMeasure; i++)
                {
                    int thrown = m * R.MeasureTicks + measure[i];
                    if (thrown <= tick && tick < thrown + R.CrescentLife) live++;
                }
            peak = Math.Max(peak, live);
        }
        AssertEqual(R.MaxCrescents, peak, "a held loop's natural maximum is the cap");
        AssertEqual(false, F.MustBreakOldest(7), "eight may fly");
        AssertEqual(true, F.MustBreakOldest(8), "a ninth breaks the oldest");
        // Spacing on the player: swapping items cannot throw faster than the figure eight.
        var ledger = new SableThrowLedger();
        AssertEqual(true, ledger.TryThrow(100), "the first throw");
        AssertEqual(false, ledger.TryThrow(117), "17 ticks later: too soon");
        AssertEqual(true, ledger.TryThrow(118), "18 ticks later");
        // Engraving: once per stroke, however many of its parts land, and however late its crescents do.
        int a = ledger.BeginStroke(), b = ledger.BeginStroke();
        AssertEqual(true, ledger.TryEngrave(a), "the first part of stroke a engraves");
        AssertEqual(false, ledger.TryEngrave(a), "its crescents then engrave nothing more");
        AssertEqual(true, ledger.TryEngrave(b), "stroke b engraves its own line");
        for (int i = 0; i < R.CrescentKeptStrokes; i++) ledger.BeginStroke();
        int c = ledger.BeginStroke();
        AssertEqual(false, ledger.TryEngrave(b), "a stroke forgotten 16 strokes later fails closed");
        AssertEqual(false, ledger.TryEngrave(-1), "no serial");
        ledger.Reset();
        AssertEqual(false, ledger.TryEngrave(c), "death forgets every stroke");
        AssertEqual(true, ledger.TryThrow(0), "and the spacing");
        for (int i = 0; i < 2000; i++) AssertEqual(true, ledger.BeginStroke() is >= 0 and < R.CrescentSerials, "serials wrap within 0..1023");
    }

    [DomainTest("Sable Scythe crescents live 70 ticks, break into a 6-tick close and reject invalid ai")]
    private static void SableCrescentLifetime()
    {
        AssertEqual(70f, F.End(0), "70 ticks unbroken");
        AssertEqual(16f, F.End(10), "a break at 10 closes by 16");
        AssertEqual(70f, F.End(66), "a late break still ends at 70");
        AssertEqual(6f, F.Remaining(10, 10), "the close starts at the break");
        AssertEqual(true, F.Done(70, 0) && !F.Done(69.5f, 0), "done at 70");
        AssertEqual(11, F.BreakAgeAt(10.5f), "a break is recorded in whole ticks");
        AssertEqual(1, F.BreakAgeAt(0), "at least 1");
        Vector2 v = new(20, 0);
        for (int i = 0; i < 12; i++) v = F.Brake(v, F.Dt);
        AssertNear(6, v.Length(), 1e-3f, "it slows to 30% over its close");
        // Replicated state: ai = (target slot or -1, kind + 8 x break age, age).
        for (int kind = 0; kind < F.Kinds; kind++)
            foreach (int broke in new[] { 0, 1, 37, 70 })
            {
                float state = F.State(kind, broke);
                AssertEqual(kind, F.KindOf(state), "kind round trip");
                AssertEqual(broke, F.BreakAgeOf(state), "break age round trip");
                AssertEqual(true, F.Valid(-1, state, 35.5f, new Vector2(10, 0)), $"state {state} is valid");
            }
        AssertEqual(true, F.Valid(199, 566, 71, new Vector2(16, 0)), "the bounds themselves");
        foreach (var bad in new[] { (-2f, 0f, 0f, 0f), (200f, 0f, 0f, 0f), (1.5f, 0f, 0f, 0f), (0f, 7f, 0f, 0f), (0f, 567f, 0f, 0f), (0f, 2.5f, 0f, 0f),
                     (0f, 0f, -.5f, 0f), (0f, 0f, 71.5f, 0f), (0f, 0f, 0f, 16.5f), (0f, float.NaN, 0f, 0f), (0f, 0f, float.PositiveInfinity, 0f) })
            AssertEqual(false, F.Valid(bad.Item1, bad.Item2, bad.Item3, new Vector2(bad.Item4, 0)), $"invalid {bad}");
        AssertEqual(false, F.Valid(0, 0, 0, new Vector2(float.NaN, 0)), "a non-finite velocity");
    }

    [DomainTest("Sable Scythe is ordinary Melee with the measure split 53% blade, 6% lash arc, 41% crescents")]
    private static void SableCrescentBudget()
    {
        AssertNear(6.4f, R.MeasureMultiplier, 1e-5f, "the measure keeps its 6.4x");
        AssertNear(3.8f, R.MeasureCloseMultiplier, 1e-5f, "blade and lash arc");
        AssertNear(2.6f, R.MeasureCrescentMultiplier, 1e-5f, "crescents: 4 x 0.4 + 5 x 0.2");
        AssertNear(.53f, (4 * R.StrokeMultiplier + R.WhipMultiplier) / R.MeasureMultiplier, .005f, "53% blade");
        AssertNear(.06f, R.LashMultiplier / R.MeasureMultiplier, .005f, "6% lash arc");
        AssertNear(.41f, R.MeasureCrescentMultiplier / R.MeasureMultiplier, .005f, "41% crescents");
        int loop = R.MeasureTicks + SableScytheMotion.ReleaseTicks(5);
        AssertNear(286, R.PerTick(3600, R.MeasureMultiplier + R.StaffReleaseMultiplier(5), loop), .5f, "12.4x in 156 ticks");
        float range = R.PerTick(3600, R.MeasureCrescentMultiplier + R.StaffReleaseMultiplier(5), loop);
        AssertNear(198, range, .5f, "from range (crescents only, plus the release): 8.6x in 156 ticks");
        AssertNear(.69f, range / R.PerTick(3600, R.MeasureMultiplier + R.StaffReleaseMultiplier(5), loop), .005f, "69% of the close figure");
        AssertNear(.4f, F.Multiplier(F.Over), 1e-6f, "an Over/Under crescent x0.4");
        AssertNear(.2f, F.Multiplier(F.FirstVolley + 4), 1e-6f, "a volley crescent x0.2");
        AssertEqual(3, R.CrescentRoots, "at most 3 roots each");
        AssertEqual(.5f, R.CrescentKnockback, "crescents carry half the knockback");
    }
}
