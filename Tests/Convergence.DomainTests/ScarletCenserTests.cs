using System;
using System.Collections.Generic;
using System.Numerics;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;

namespace Convergence.DomainTests;

// Ember Censer (docs/encounters/crimson-foundry/REWARDS.md, "Summon - Ember Censer"): the pendulum and its apexes, the
// four-pour cycle with the braced Grand Pour, the largest-gap phase choice, the falling column, commitment and the
// replicated ai.
internal static partial class Program
{
    private const float CenserDegree = MathF.PI / 180;

    private readonly struct CenserFloorArray : ICenserFloor
    {
        private readonly bool[] solid;
        internal CenserFloorArray(bool[] solid) => this.solid = solid;
        public bool Solid(int sample) => sample < solid.Length && solid[sample];
    }

    [DomainTest("Scarlet censer winds up, then reaches an apex every 32 ticks and braces the Grand Pour")]
    private static void ScarletCenserPendulum()
    {
        AssertEqual(0f, CenserRules.Angle(0), "arrives hanging straight down");
        AssertNear(-25 * CenserDegree, CenserRules.Angle(16), 1e-5f, "the small first apex");
        AssertNear(55 * CenserDegree, CenserRules.Angle(48), 1e-5f, "the second apex, the first pour");
        AssertNear(-55 * CenserDegree, CenserRules.Angle(80), 1e-5f, "second pour");
        AssertNear(55 * CenserDegree, CenserRules.Angle(112), 1e-5f, "third pour");
        AssertNear(-75 * CenserDegree, CenserRules.Angle(144), 1e-5f, "the Grand Pour swings out to 75");
        AssertNear(-75 * CenserDegree, CenserRules.Angle(149.5f), 1e-5f, "and braces there for 6 ticks");
        AssertNear(-75 * CenserDegree, CenserRules.Angle(150), 1e-5f, "it dumps from the apex");
        AssertNear(CenserRules.Angle(48), CenserRules.Angle(182), 1e-5f, "the cycle closes on the first pour's apex");
        AssertEqual(134, CenserRules.Cycle, "a cycle is 32 + 32 + 38 + 32");
        AssertEqual(CrimsonRewardRules.CenserCycleTicks, CenserRules.Cycle, "the parity table's cycle");
        AssertEqual(2 * CrimsonRewardRules.ApexInterval, CrimsonRewardRules.SwingPeriod, "apexes every half period");

        // No hard stops: the angle and its rate are continuous everywhere (wind-up, cycle and the wrap); the bowl
        // comes to rest only at apexes; the angular acceleration stays bounded.
        const float h = .05f;
        float maxAccel = 0;
        for (float c = 0; c < CenserRules.ClockEnd - 2 * h; c += h)
        {
            float v0 = (CenserRules.Angle(c + h) - CenserRules.Angle(c)) / h, v1 = (CenserRules.Angle(c + 2 * h) - CenserRules.Angle(c + h)) / h;
            maxAccel = MathF.Max(maxAccel, MathF.Abs(v1 - v0) / h);
            AssertEqual(true, MathF.Abs(v1 - v0) < .0015f, $"angular velocity is continuous at {c}");
        }
        AssertEqual(true, maxAccel < .012f, $"peak angular acceleration {maxAccel} stays gentle");
        foreach (float mid in new[] { 32f, 64f, 96f, 128f, 166f })
            AssertEqual(true, MathF.Abs(CenserRules.Angle(mid + .5f) - CenserRules.Angle(mid - .5f)) > .03f, $"the bowl sweeps through the bottom at {mid}");
        foreach (float apex in new[] { 16f, 48f, 80f, 112f, 144f })
            AssertEqual(true, MathF.Abs(CenserRules.Angle(apex + .25f) - CenserRules.Angle(apex - .25f)) < .002f, $"rest at the apex {apex}");
        AssertNear(CenserRules.Angle(48.3f), CenserRules.Angle(182.3f), 1e-5f, "the wrap is seamless");
        // The mouth hangs BowlDrop below the ring along the pendulum line.
        AssertNear(CrimsonRewardRules.BowlDrop, Vector2.Distance(CenserRules.Mouth(80), Vector2.Zero), .01f, "bowl BowlDrop below the ring");
        AssertNear(34f, CrimsonRewardRules.BowlDrop, 0, "the measured ring-to-rim distance of EmberCenser.png at the 2 px dot");
        AssertEqual(true, CenserRules.Mouth(48).X > 0 && CenserRules.Mouth(80).X < 0, "pours alternate sides");
    }

    [DomainTest("Scarlet censer pours from the second apex on and every fourth pour is grand")]
    private static void ScarletCenserPourCycle()
    {
        var starts = new List<int>();
        var grand = new List<bool>();
        int clock = 0;
        float multiplier = 0;
        for (int tick = 0; tick <= 48 + 3 * 134; tick++)
        {
            AssertEqual(true, clock >= 0 && clock < CenserRules.ClockEnd, "the clock stays small");
            if (CenserRules.PourStarts(clock, out int index))
            {
                starts.Add(tick);
                grand.Add(index == 3);
                AssertEqual(true, CenserRules.TryPour(clock, out var pour) && pour.Age == 0 && pour.Index == index, "a pour starts live");
                multiplier += pour.Multiplier;
            }
            if (tick < 48) AssertEqual(false, CenserRules.TryPour(clock, out _), "nothing pours in the wind-up");
            clock = CenserRules.Advance(clock);
        }
        AssertEqual(48, starts[0], "the first pour at the second apex");
        int[] gaps = { 32, 32, 38, 32 };
        for (int i = 1; i < starts.Count; i++) AssertEqual(gaps[(i - 1) % 4], starts[i] - starts[i - 1], $"pour {i + 1}");
        for (int i = 0; i < grand.Count; i++) AssertEqual((i + 1) % 4 == 0, grand[i], $"pour {i + 1} grand only every fourth");
        AssertNear(3 * CrimsonRewardRules.CenserCycleMultiplier + 1, multiplier, 1e-4f, "three cycles of 5.2x and the next first pour");
        AssertEqual(48, CenserRules.Advance(181), "the clock wraps by one cycle");
        AssertEqual(48, CenserRules.Advance(47), "out of the wind-up");

        // Live windows: 16 ticks, the Grand Pour 20.
        AssertEqual(true, CenserRules.TryPour(63, out var last) && last.Age == 15 && !last.Grand, "a pour is live for 16 ticks");
        AssertEqual(false, CenserRules.TryPour(64, out _), "then it is over");
        AssertEqual(true, CenserRules.TryPour(169, out var dump) && dump.Grand && dump.Age == 19 && dump.Radius == 56 && dump.Fall == 60,
            "the curtain is live for 20 ticks, radius 56, falling 60 px/tick");
        AssertEqual(false, CenserRules.TryPour(170, out _), "and ends");
        AssertNear(2.2f, dump.Multiplier, 1e-6f, "x2.2");

        // Cues: the windup 10 ticks before each normal pour; the Grand Pour's windup is its 6-tick brace.
        clock = 0;
        var swings = new List<int>();
        var braces = new List<int>();
        for (int tick = 0; tick < 48 + 134; tick++)
        {
            if (CenserRules.SwingCue(clock)) swings.Add(tick);
            if (CenserRules.BraceStarts(clock)) braces.Add(tick);
            clock = CenserRules.Advance(clock);
        }
        AssertEqual("38,70,102,172", string.Join(",", swings), "CenserSwing leads each normal pour by 10");
        AssertEqual("144", string.Join(",", braces), "CenserBrace when the Grand Pour reaches its apex");
        AssertEqual(true, CenserRules.Bracing(146) && !CenserRules.Bracing(150), "the brace holds until the dump");
    }

    [DomainTest("Scarlet censer bowl tips 35 degrees inward through each pour and rights itself between")]
    private static void ScarletCenserTip()
    {
        AssertEqual(0f, CenserRules.Tip(30), "no tip in the wind-up");
        for (int clock = 48; clock < 182; clock++)
        {
            if (!CenserRules.TryPour(clock, out var pour)) continue;
            float tip = CenserRules.Tip(clock);
            AssertNear(pour.Side * 35 * CenserDegree, tip, 1e-5f, $"tipped through the pour at {clock}");
            // Inward: the tip turns the same way as the apex the pour leaves from.
            AssertEqual(MathF.Sign(CenserRules.Angle(pour.Start)), MathF.Sign(tip), $"inward at {clock}");
        }
        AssertNear(35 * CenserDegree, CenserRules.Tip(48), 1e-5f, "fully tipped at the apex");
        AssertEqual(0f, CenserRules.Tip(48 + 16 + 8 + 1), "upright again between pours");
        for (float c = 40; c < 182; c += .05f)
            AssertEqual(true, MathF.Abs(CenserRules.Tip(c + .05f) - CenserRules.Tip(c)) < .04f, $"the tip moves smoothly at {c}");
    }

    [DomainTest("Scarlet censer phase choice puts a newcomer in the largest gap and never moves the others")]
    private static void ScarletCenserPhase()
    {
        AssertEqual(0, CenserRules.ChooseStart(ReadOnlySpan<int>.Empty), "alone: the whole wind-up");
        Span<int> mine = stackalloc int[16];
        Span<int> theirs = stackalloc int[256];
        var random = new Random(11);
        for (int trial = 0; trial < 300; trial++)
        {
            int n = 1 + random.Next(4);
            int[] others = new int[n];
            for (int i = 0; i < n; i++) others[i] = random.Next(CenserRules.ClockEnd);
            int count = 0;
            foreach (int o in others) count += CenserRules.Pours(o, -CenserRules.StartWindow, CenserRules.WindUp + CenserRules.PhaseHorizon + CenserRules.StartWindow, theirs[count..]);
            int chosen = CenserRules.ChooseStart(others);
            AssertEqual(true, chosen >= 0 && chosen < CenserRules.StartWindow, "a start in the first swing");
            int best = CenserRules.Score(chosen, theirs[..count], mine);
            for (int s = 0; s < CenserRules.StartWindow; s++)
                AssertEqual(true, CenserRules.Score(s, theirs[..count], mine) <= best, $"trial {trial}: no start is further from the others than {chosen}");
        }

        // A second censer over a target already pouring steps between its pours.
        int second = CenserRules.ChooseStart(new[] { 48 });
        int theirCount = CenserRules.Pours(48, -32, 48 + CenserRules.PhaseHorizon + 32, theirs);
        AssertEqual(true, CenserRules.Score(second, theirs[..theirCount], mine) >= 14, "at least 14 ticks from every pour of the first (16 is half an apex interval)");

        // Four censers arrive one after another; every pour keeps clear of the others.
        var clocks = new List<int>();
        for (int arrival = 0; arrival < 4; arrival++)
        {
            int start = CenserRules.ChooseStart(clocks.ToArray());
            for (int i = 0; i < clocks.Count; i++) clocks[i] = CenserRules.After(clocks[i], 0); // never re-phased
            clocks.Add(start);
            for (int tick = 0; tick < 23; tick++)
                for (int i = 0; i < clocks.Count; i++) clocks[i] = CenserRules.Advance(clocks[i]);
        }
        var times = new List<int>();
        foreach (int c in clocks)
        {
            int k = CenserRules.Pours(c, 0, 2 * CenserRules.Cycle, theirs);
            for (int i = 0; i < k; i++) times.Add(theirs[i]);
        }
        times.Sort();
        int tightest = int.MaxValue;
        for (int i = 1; i < times.Count; i++) tightest = Math.Min(tightest, times[i] - times[i - 1]);
        AssertEqual(true, tightest >= 5, $"four censers step from one to the next ({tightest} ticks at the closest)");

        // Pour times agree with the clock run forward.
        int n2 = CenserRules.Pours(100, 0, 300, theirs);
        int clock = 100;
        var run = new List<int>();
        for (int t = 0; t < 300; t++) { if (CenserRules.PourStarts(clock, out _)) run.Add(t); clock = CenserRules.Advance(clock); }
        AssertEqual(string.Join(",", run), string.Join(",", theirs[..n2].ToArray()), "pour times match the clock");
    }

    [DomainTest("Scarlet censer column falls to the floor from the mouth and collides exactly where it is drawn")]
    private static void ScarletCenserColumn()
    {
        AssertEqual(true, CenserRules.TryPour(53, out var pour) && pour.Start == 48 && pour.Age == 5, "the first pour, five ticks in");
        Span<float> emitted = stackalloc float[CenserRules.MaxColumnPoints];
        AssertEqual(6, CenserRules.Emissions(pour, 53, emitted), "one point per tick since the pour started, the mouth last");
        AssertEqual(7, CenserRules.Emissions(pour, 53.5f, emitted), "drawing between ticks adds the moving mouth");
        AssertEqual(53.5f, emitted[6], "the newest point is the mouth now");
        AssertEqual(CenserRules.Mouth(53), CenserRules.Stream(pour, 53, 53, 600), "the column's top rides the mouth");
        AssertEqual(CenserRules.Mouth(48) + new Vector2(0, 200), CenserRules.Stream(pour, 48, 53, 600), "the head falls 40 px/tick");
        AssertEqual(CenserRules.Mouth(48) + new Vector2(0, 120 - 24 * CenserRules.RestLift), CenserRules.Stream(pour, 48, 53, 120),
            "to the floor, resting a little above it so the foot stays on it");
        var late = pour with { Age = 15 };
        AssertEqual(600f, CenserRules.FallDistance(late, 48, 48 + 20), "600 px at most");
        AssertEqual(true, CenserRules.Landed(pour, 48, 53, 120) && !CenserRules.Landed(pour, 50, 53, 200), "landed points rest on the floor");
        AssertEqual(false, CenserRules.Landed(late, 48, 68, 600), "open air: no floor, nothing rests");
        Span<float> depths = stackalloc float[CenserRules.MaxColumnPoints];
        int all = CenserRules.Emissions(pour, 53, emitted);
        for (int k = 0; k < all; k++) depths[k] = 100;
        AssertEqual(2, CenserRules.Bottom(pour, emitted[..all], depths[..all], 53), "older points pool into the newest resting one");
        for (int k = 0; k < all; k++) depths[k] = 600;
        AssertEqual(0, CenserRules.Bottom(pour, emitted[..all], depths[..all], 53), "nothing rests: the whole column falls");
        AssertNear(16, CenserRules.StreamRadius(pour, 53, 53), 1e-4f, "the lip opens from the bowl (2 ticks burnt)");
        AssertNear(24, CenserRules.StreamRadius(pour, 52, 53), 1e-4f, "radius 24 a tick below the mouth");
        AssertNear(1, CrimsonRewardRules.InkOpen(CenserRules.Ignition(48, 53)), 1e-6f, "open after three ticks");

        // The floor: the first solid sample (every 8 px) after free air, at most the scan limit.
        var floor = new bool[CenserRules.FloorSamples + 1];
        for (int k = 10; k < floor.Length; k++) floor[k] = true;
        AssertEqual(80f, CenserRules.FloorDepth(new CenserFloorArray(floor), 600), "the floor 80 px down");
        AssertEqual(600f, CenserRules.FloorDepth(new CenserFloorArray(floor), 40), "only as far as it can fall (none found yet)");
        var inside = new bool[CenserRules.FloorSamples + 1];
        inside[0] = inside[1] = true; inside[7] = true;
        AssertEqual(56f, CenserRules.FloorDepth(new CenserFloorArray(inside), 600), "a mouth inside a wall pours through it");
        AssertEqual(600f, CenserRules.FloorDepth(new CenserFloorArray(new bool[CenserRules.FloorSamples + 1]), 600), "open air: 600 px");
        AssertEqual(75, CenserRules.FloorSamples, "sampled every 8 px to 600");

        // Draw equals collide: the capsule test agrees with dense sampling of the drawn (tapered) shape.
        var points = new Vector2[CenserRules.MaxColumnPoints];
        var radii = new float[CenserRules.MaxColumnPoints];
        var times = new float[CenserRules.MaxColumnPoints];
        var floors = new float[CenserRules.MaxColumnPoints];
        Array.Fill(floors, 150);
        int n = CenserRules.Column(pour, 53, floors, points, radii, times, out bool rests);
        AssertEqual(true, rests, "the stream reaches a floor 150 px down");
        AssertEqual(CenserRules.Mouth(53), points[0], "the column's top is the mouth");
        AssertNear(16, radii[0], 1e-4f, "the lip");
        // The foot lies on the stream's own line, at the rest height above the floor: no kink into the floor.
        Vector2 foot = points[n - 1], above = points[n - 2];
        AssertNear(CenserRules.Mouth(49).Y + 150 - 24 * CenserRules.RestLift, foot.Y, 1e-3f, "the foot rests above the floor");
        Vector2 under = CenserRules.Mouth(49) + new Vector2(0, CenserRules.FallDistance(pour, 49, 53));
        float cross = (foot.X - above.X) * (under.Y - above.Y) - (foot.Y - above.Y) * (under.X - above.X);
        AssertNear(0, cross, 1e-2f, "the foot is on the stream's line");
        AssertEqual(true, foot.Y > above.Y, "below the last falling point");
        Array.Fill(floors, 600);
        AssertEqual(6, CenserRules.Column(pour, 53, floors, points, radii, times, out rests), "open air: every point falls");
        AssertEqual(false, rests, "nothing rests");
        AssertEqual(CenserRules.Mouth(48) + new Vector2(0, 200), points[5], "the head 200 px down");
        Array.Fill(floors, 150);
        n = CenserRules.Column(pour, 53, floors, points, radii, times, out _);
        var random = new Random(5);
        for (int trial = 0; trial < 3000; trial++)
        {
            Vector2 min = new(random.Next(-120, 120), random.Next(-40, 260)), size = new(random.Next(2, 40), random.Next(2, 40));
            bool exact = CenserRules.ColumnTouches(points.AsSpan(0, n), radii.AsSpan(0, n), min, min + size);
            float margin = float.MaxValue;
            for (int k = 1; k < n; k++)
                for (int s = 0; s <= 64; s++)
                {
                    float u = s / 64f;
                    Vector2 p = Vector2.Lerp(points[k - 1], points[k], u);
                    margin = MathF.Min(margin, CrimsonRewardRules.BoxDistance(p, min, min + size) - (radii[k - 1] + (radii[k] - radii[k - 1]) * u));
                }
            if (margin < -1.5f) AssertEqual(true, exact, $"trial {trial}: inside the drawn column collides");
            if (margin > .5f) AssertEqual(false, exact, $"trial {trial}: outside the drawn column never collides");
        }
    }

    [DomainTest("Scarlet censer finishes a live pour for an unusable owner but never starts one")]
    private static void ScarletCenserCommitment()
    {
        AssertEqual(false, CenserRules.Continues(30, false), "the wind-up stops at once");
        AssertEqual(false, CenserRules.Continues(48, false), "a pour that has not started never starts");
        AssertEqual(true, CenserRules.Continues(49, false), "a live pour finishes its window");
        AssertEqual(true, CenserRules.Continues(63, false), "to its last live tick");
        AssertEqual(false, CenserRules.Continues(64, false), "then the censer leaves");
        AssertEqual(true, CenserRules.Continues(150 + 19, false), "the Grand Pour too");
        for (int c = 0; c < CenserRules.ClockEnd; c++) AssertEqual(true, CenserRules.Continues(c, true), "a usable owner with a target swings on");
    }

    [DomainTest("Scarlet censer rejects invalid ai and keeps a peer clock that only leads a stale packet")]
    private static void ScarletCenserReplication()
    {
        AssertEqual(true, CenserRules.ValidAi(0, -1, 0), "a fresh censer");
        AssertEqual(true, CenserRules.ValidAi(2, 199, 181), "the largest values");
        foreach (var (a, b, c) in new[] { (3f, -1f, 0f), (-1f, -1f, 0f), (1.5f, -1f, 0f), (2f, 200f, 0f), (2f, -2f, 0f), (2f, 3.5f, 0f),
                     (2f, 0f, 182f), (2f, 0f, -1f), (2f, 0f, 2.5f), (float.NaN, 0f, 0f), (2f, float.PositiveInfinity, 0f), (2f, 0f, float.NaN) })
            AssertEqual(false, CenserRules.ValidAi(a, b, c), $"rejects ({a}, {b}, {c})");
        AssertEqual(true, CenserRules.KeepLocal(60, 55), "a packet 5 ticks old");
        AssertEqual(true, CenserRules.KeepLocal(49, 180), "across the wrap");
        AssertEqual(false, CenserRules.KeepLocal(55, 60), "behind the owner: adopt");
        AssertEqual(false, CenserRules.KeepLocal(80, 60), "too far ahead: adopt");
        AssertEqual(false, CenserRules.KeepLocal(30, 40), "in the wind-up as well");
        AssertEqual(true, CenserRules.RosterKey(5, 900) < CenserRules.RosterKey(6, 1), "summon order first");
        AssertEqual(true, CenserRules.RosterKey(0, 1) > CenserRules.RosterKey(int.MaxValue - 1, 1023), "unstamped sorts last");
    }

    [DomainTest("Scarlet censer warmth builds toward the Grand Pour without a pulse")]
    private static void ScarletCenserWarmth()
    {
        AssertEqual(CenserRules.WarmthIdle, CenserRules.Warmth(0), "smouldering on arrival");
        AssertNear(CenserRules.WarmthSwing, CenserRules.Warmth(48), 1e-5f, "warm after the first swing");
        float previous = CenserRules.Warmth(0);
        for (float c = .5f; c <= 48 + 96; c += .5f)
        {
            float w = CenserRules.Warmth(c);
            AssertEqual(true, w >= previous - 1e-5f, $"never cools before the Grand Pour ({c})");
            previous = w;
        }
        AssertEqual(1f, CenserRules.Warmth(48 + 99), "ember-gold through the brace");
        AssertEqual(true, CenserRules.Warmth(48 + 130) < .6f, "settles after the curtain");
        for (float c = 0; c < 182; c += .25f)
            AssertEqual(true, MathF.Abs(CenserRules.Warmth(c + .25f) - CenserRules.Warmth(c)) < .05f, $"continuous at {c}");
    }
}
