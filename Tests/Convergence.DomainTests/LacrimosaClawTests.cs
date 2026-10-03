using System;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static void AssertLacrimosa(bool condition, string context)
    {
        if (!condition) throw new InvalidOperationException(context);
    }

    private static (double CycleRaw, int CycleTicks, double PerSecond) LacrimosaSteadyCycle(float speed = 1)
    {
        var grasps = new int[64];
        int[] raw = LacrimosaClawScore.Timeline(24_000, 0, speed, graspTicks: grasps);
        int count = 0;
        while (count < grasps.Length && grasps[count] > 0) count++;
        AssertLacrimosa(count >= 8, $"the steady run grasps repeatedly ({count})");
        double cycle = 0;
        for (int tick = grasps[3]; tick < grasps[4]; tick++) cycle += raw[tick];
        double total = 0;
        for (int tick = grasps[2]; tick < grasps[count - 1]; tick++) total += raw[tick];
        return (cycle, grasps[4] - grasps[3], DollWeaponBudget.PerSecond(total, grasps[count - 1] - grasps[2]));
    }

    [DomainTest("Lacrimosa claw kata keeps the 1/28 swipe rate and the grasp stays an execution, not a deletion")]
    private static void LacrimosaKataRate()
    {
        float multipliers = 0;
        int ticks = 0;
        for (int stroke = 0; stroke < LacrimosaClawMotion.Strokes; stroke++)
        {
            multipliers += LacrimosaClawMotion.Multiplier(stroke);
            ticks += LacrimosaClawMotion.BaseTicks(stroke);
            AssertEqual(ticks == LacrimosaClawMotion.Offset(stroke) + LacrimosaClawMotion.BaseTicks(stroke), true, "strokes tile the track");
        }
        AssertDollNear(3.0, multipliers, 1e-5, "kata multiplier");
        AssertEqual(LacrimosaClawMotion.KataTicks, ticks, "kata base ticks");
        AssertDollNear(1.0 / 28, multipliers / ticks, 1e-6, "the v1 swipe rate");
        double grasp = LacrimosaClawMotion.ContactMultiplier + LacrimosaClawMotion.CrushMultiplier;
        AssertLacrimosa(grasp < 5, "contact + crush stays below 5x");
        AssertEqual(7700, LacrimosaClawMotion.BaseDamage, "base damage unchanged");
        AssertEqual(NullCantorClawMotion.BaseDamage, LacrimosaClawMotion.BaseDamage, "same base as v1");
        AssertEqual(6545, RitualArmamentRules.ScaledDamage(7700, LacrimosaClawMotion.Multiplier(LacrimosaClawMotion.RakeDown)), "rake hit");
        AssertEqual(10010, RitualArmamentRules.ScaledDamage(7700, LacrimosaClawMotion.Multiplier(LacrimosaClawMotion.Clap)), "clap hit");
        AssertEqual(2310, RitualArmamentRules.ScaledDamage(7700, LacrimosaClawMotion.ContactMultiplier), "grasp contact hit");
        AssertEqual(30800, RitualArmamentRules.ScaledDamage(7700, LacrimosaClawMotion.CrushMultiplier), "crush hit");
    }

    [DomainTest("Lacrimosa claw budget matches the 0.2.x claws per cycle, sustained and over the best cold window")]
    private static void LacrimosaBudget()
    {
        var (cycleRaw, cycleTicks, perSecond) = LacrimosaSteadyCycle();
        AssertLacrimosa(DollWeaponBudget.Within(cycleRaw, DollWeaponBudget.Claw.CycleRaw),
            $"cycle raw {cycleRaw} vs {DollWeaponBudget.Claw.CycleRaw} ({DollWeaponBudget.Deviation(cycleRaw, DollWeaponBudget.Claw.CycleRaw):P2})");
        AssertLacrimosa(DollWeaponBudget.Within(cycleTicks, DollWeaponBudget.Claw.CycleTicks),
            $"cycle ticks {cycleTicks} vs {DollWeaponBudget.Claw.CycleTicks}");
        AssertLacrimosa(DollWeaponBudget.Within(perSecond, DollWeaponBudget.Claw.PerSecond),
            $"sustained {perSecond:F0}/s vs {DollWeaponBudget.Claw.PerSecond:F0}/s ({DollWeaponBudget.Deviation(perSecond, DollWeaponBudget.Claw.PerSecond):P2})");

        // The grasp is player-timed: the best cold window searches every delay after the beads fill.
        var windows = new int[260][];
        for (int delay = 0; delay < windows.Length; delay++) windows[delay] = LacrimosaClawScore.Timeline(DollWeaponBudget.ColdWindowTicks, delay);
        double best = DollWeaponBudget.BestColdWindow(windows.Length, (delay, tick) => windows[delay][tick], out _);
        AssertLacrimosa(DollWeaponBudget.Within(best, DollWeaponBudget.ClawColdWindow),
            $"best cold window {best} vs {DollWeaponBudget.ClawColdWindow} ({DollWeaponBudget.Deviation(best, DollWeaponBudget.ClawColdWindow):P2})");
        double first = DollWeaponBudget.ColdWindow(tick => windows[0][tick]);
        AssertLacrimosa(first <= best && DollWeaponBudget.Within(first, DollWeaponBudget.ClawColdWindow, .05), "grasping as soon as full stays near the baseline");

        // Attack speed shortens strokes but scales the fill with them: the cycle stays in budget at 1.25 and 1.5.
        foreach (float speed in new[] { 1.25f, 1.5f })
        {
            var (_, _, fast) = LacrimosaSteadyCycle(speed);
            // v1 at the same speed: its 28-tick swipe shortened by native attack speed, the 360-tick charge unchanged.
            double v1 = DollWeaponBudget.PerSecond(360.0 / NullCantorClawMotion.Duration(speed) * 7700 + 32340, 402);
            AssertLacrimosa(DollWeaponBudget.Within(fast, v1, .06), $"speed {speed}: {fast:F0}/s vs v1 {v1:F0}/s");
        }
    }

    [DomainTest("Lacrimosa claw strokes are bounded by attack speed and every live window survives the fastest stroke")]
    private static void LacrimosaDurations()
    {
        float[] speeds = { .1f, .25f, .5f, 1f, 1.25f, 1.5f, 2f, 4f, 8f, float.NaN, float.PositiveInfinity };
        for (int stroke = 0; stroke < LacrimosaClawMotion.Strokes; stroke++)
        {
            AssertEqual(LacrimosaClawMotion.BaseTicks(stroke), LacrimosaClawMotion.Duration(stroke, 1), "speed 1 is the base");
            AssertEqual(LacrimosaClawMotion.BaseTicks(stroke), LacrimosaClawMotion.Duration(stroke, float.NaN), "non-finite speed keeps the base");
            foreach (float speed in speeds)
            {
                int duration = LacrimosaClawMotion.Duration(stroke, speed);
                AssertLacrimosa(duration >= LacrimosaClawMotion.MinTicks(stroke) && duration <= LacrimosaClawMotion.MaxTicks, $"stroke {stroke} speed {speed} -> {duration}");
                int first = LacrimosaClawMotion.FirstLiveAge(stroke, duration);
                AssertLacrimosa(first <= duration && LacrimosaClawMotion.Live(stroke, first, duration), $"stroke {stroke} live at {first} of {duration}");
                AssertLacrimosa(!LacrimosaClawMotion.Live(stroke, first - 1, duration), "the first live age is the first");
                int live = 0;
                for (int age = 1; age <= duration; age++) if (LacrimosaClawMotion.Live(stroke, age, duration)) live++;
                AssertLacrimosa(live >= 1, $"stroke {stroke} has a live tick at {duration}");
            }
            AssertEqual(LacrimosaClawMotion.LiveStart(stroke), LacrimosaClawMotion.FirstLiveAge(stroke, LacrimosaClawMotion.BaseTicks(stroke)), "base first live age");
        }
        AssertEqual(12, LacrimosaClawMotion.Duration(LacrimosaClawMotion.RakeDown, 8), "A floor");
        AssertEqual(90, LacrimosaClawMotion.Duration(LacrimosaClawMotion.Clap, .1f), "ceiling");
    }

    [DomainTest("Lacrimosa claw hands flow continuously through A, B, C and back to A")]
    private static void LacrimosaContinuity()
    {
        for (int hand = 0; hand < 2; hand++)
        {
            LacrimosaHand previous = LacrimosaClawMotion.Hand(hand, 0);
            AssertDollNear(previous.Wrist, LacrimosaClawMotion.Hand(hand, LacrimosaClawMotion.KataTicks).Wrist, .01f, "the kata ends at rest");
            AssertDollNear(0, MathF.IEEERemainder(previous.Axis - LacrimosaClawMotion.Hand(hand, 84).Axis, MathF.Tau), 1e-3, "rest axis wraps");
            float worstStep = 0, worstTurn = 0;
            for (int i = 1; i <= 8400; i++)
            {
                LacrimosaHand next = LacrimosaClawMotion.Hand(hand, i * .01f);
                worstStep = MathF.Max(worstStep, Vector2.Distance(previous.Wrist, next.Wrist));
                worstTurn = MathF.Max(worstTurn, MathF.Abs(MathF.IEEERemainder(next.Axis - previous.Axis, MathF.Tau)));
                previous = next;
            }
            AssertLacrimosa(worstStep < 1.6f && worstTurn < .01f, $"hand {hand}: no jump ({worstStep} px, {worstTurn} rad per 0.01 tick)");
            // Velocity matches across every stroke join at the real cadence (attack speed 1) and across C -> A.
            foreach (int join in new[] { 26, 50, 84 })
            {
                Vector2 before = (LacrimosaClawMotion.Hand(hand, join).Wrist - LacrimosaClawMotion.Hand(hand, join - .05f).Wrist) / .05f;
                Vector2 after = (LacrimosaClawMotion.Hand(hand, (join + .05f) % 84).Wrist - LacrimosaClawMotion.Hand(hand, join % 84).Wrist) / .05f;
                AssertLacrimosa(Vector2.Distance(before, after) < 1f, $"hand {hand} velocity at {join}: {before} vs {after}");
            }
        }
        // Raking hands never brake hard: the wrist angle's acceleration stays bounded through each live window.
        foreach ((int hand, float from, float to) in new[] { (0, 9f, 17f), (1, 33f, 40f) })
        {
            float worst = 0;
            for (float t = from; t < to; t += .02f)
            {
                float a0 = Angle(hand, t - .02f), a1 = Angle(hand, t), a2 = Angle(hand, t + .02f);
                worst = MathF.Max(worst, MathF.Abs(a2 - 2 * a1 + a0) / (.02f * .02f));
            }
            AssertLacrimosa(worst < .35f, $"hand {hand} rakes at {worst} rad/tick^2");
        }

        static float Angle(int hand, float t)
        {
            Vector2 w = LacrimosaClawMotion.Hand(hand, t).Wrist;
            return MathF.Atan2(w.Y, w.X);
        }
    }

    [DomainTest("Lacrimosa claw capsules exist only for the live hand, stay within reach and mirror with facing")]
    private static void LacrimosaLiveGeometry()
    {
        float reachRake = 0, reachClap = 0;
        int probes = 0;
        for (int stroke = 0; stroke < LacrimosaClawMotion.Strokes; stroke++)
        {
            for (float baseAge = 0; baseAge <= LacrimosaClawMotion.BaseTicks(stroke); baseAge += .05f)
            for (int hand = 0; hand < 2; hand++)
            {
                bool live = baseAge >= LacrimosaClawMotion.LiveStart(stroke) && baseAge < LacrimosaClawMotion.LiveEnd(stroke);
                bool has = LacrimosaClawMotion.TryCapsule(stroke, hand, baseAge, out Vector2 a, out Vector2 b, out float radius);
                AssertEqual(live && LacrimosaClawMotion.Active(stroke, hand), has, $"capsule of stroke {stroke} hand {hand} at {baseAge}");
                LacrimosaHand frame = LacrimosaClawMotion.Frame(stroke, hand, baseAge);
                if (live && LacrimosaClawMotion.Active(stroke, hand))
                {
                    LacrimosaPose expected = stroke == LacrimosaClawMotion.Clap ? LacrimosaPose.Thrust : LacrimosaPose.Rake;
                    AssertLacrimosa(frame.Pose == expected && frame.Large, $"live stroke {stroke} draws the large {expected}");
                }
                if (!has) continue;
                float reach = MathF.Max(a.Length(), b.Length()) + radius;
                AssertLacrimosa(reach <= LacrimosaClawMotion.MaximumReach, $"reach {reach}");
                // The hits really reach |b| + radius: a 1 px body just inside the round tip is hit, just outside is not.
                Vector2 axis = Vector2.Normalize(b - a), half = new(.5f);
                Vector2 inside = b + axis * (radius - 2), outside = b + axis * (radius + 2);
                AssertLacrimosa(LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, inside - half, inside + half)
                    && !LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, outside - half, outside + half), $"capsule tip of stroke {stroke} at {baseAge}");
                probes++;
                if (stroke == LacrimosaClawMotion.Clap) reachClap = MathF.Max(reachClap, reach);
                else
                {
                    reachRake = MathF.Max(reachRake, reach);
                    AssertDollNear(104, frame.Wrist.Length(), 1.5, "the raking wrist rides its orbit");
                }
            }
        }
        AssertLacrimosa(probes > 100, $"capsule probes {probes}");
        AssertLacrimosa(reachRake > 300 && reachRake < 310, $"rake reach about 305 ({reachRake})");
        AssertLacrimosa(reachClap > 360 && reachClap < 372, $"clap reach about 366 ({reachClap})");
        // The palms meet on the aim line at C's contact: wrists 50 px either side, both hands flat along the aim.
        LacrimosaHand right = LacrimosaClawMotion.Frame(LacrimosaClawMotion.Clap, 0, LacrimosaClawMotion.ClapContact);
        LacrimosaHand left = LacrimosaClawMotion.Frame(LacrimosaClawMotion.Clap, 1, LacrimosaClawMotion.ClapContact);
        AssertDollNear(new Vector2(84, 50), right.Wrist, .5f, "right wrist at contact");
        AssertDollNear(new Vector2(84, -50), left.Wrist, .5f, "left wrist at contact");
        AssertDollNear(0, right.Axis, 1e-3, "right hand flat");
        AssertDollNear(0, left.Axis, 1e-3, "left hand flat");
        AssertLacrimosa(LacrimosaClawMotion.TryCapsule(LacrimosaClawMotion.Clap, 0, LacrimosaClawMotion.ClapContact, out Vector2 ca, out _, out float cr)
            && MathF.Abs(ca.Y) < cr, "the clap capsules overlap the aim line at contact");
        // Facing -1 is the reflection of facing +1; reversed gravity mirrors it again.
        Vector2 local = new(120, -40);
        Vector2 east = LacrimosaClawMotion.ToWorld(local, 0, 1, 1), west = LacrimosaClawMotion.ToWorld(local, MathF.PI, -1, 1);
        AssertDollNear(new Vector2(-east.X, east.Y), west, .01f, "left facing mirrors right facing");
        AssertDollNear(new Vector2(east.X, -east.Y), LacrimosaClawMotion.ToWorld(local, 0, 1, -1), .01f, "reversed gravity mirrors vertically");
        AssertDollNear(MathF.PI - .4f, LacrimosaClawMotion.AxisToWorld(.4f, MathF.PI, -1, 1), 1e-5, "axis mirrors with the wrist");
    }

    [DomainTest("Lacrimosa claw capsules hit a hitbox when the segment comes within the radius, round ends included")]
    private static void LacrimosaCapsuleBox()
    {
        Vector2 a = new(0, 0), b = new(100, 0);
        const float radius = 44;
        // A body big enough to contain the whole capsule (a large boss pressed against the owner) is hit.
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(-500, -400), new Vector2(600, 400)), "a box containing the segment");
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(-20, -10), new Vector2(120, 10)), "a box containing the segment exactly");
        // Beyond the tip within the radius: the round end.
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(140, -5), new Vector2(150, 5)), "40 px past the tip");
        AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(145, -5), new Vector2(150, 5)), "45 px past the tip");
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(130, 30), new Vector2(140, 40)), "the tip's corner 42 px away");
        AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(132, 32), new Vector2(140, 40)), "the tip's corner 45 px away");
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(-40, -5), new Vector2(-30, 5)), "behind the wrist within the radius");
        AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(-60, -5), new Vector2(-50, 5)), "50 px behind the wrist");
        // Beside the segment, and crossing it.
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(40, 43), new Vector2(60, 80)), "43 px beside");
        AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(a, b, radius, new Vector2(40, 45), new Vector2(60, 80)), "45 px beside");
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(new Vector2(-50, 0), new Vector2(50, 0), 0, new Vector2(-10, -10), new Vector2(10, 10)), "crossing with radius 0");
        // A diagonal segment whose nearest feature is a box corner, not an end.
        Vector2 c = new(0, 0), d = new(100, 100);
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(c, d, 15, new Vector2(60, 20), new Vector2(80, 40)), "corner 14.1 px from the diagonal");
        AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(c, d, 14, new Vector2(60, 20), new Vector2(80, 40)), "corner beyond 14 px");
        AssertDollNear(200, LacrimosaClawMotion.SegmentBoxDistanceSquared(c, d, new Vector2(60, 20), new Vector2(80, 40)), .01, "corner distance squared");
        // A point capsule (a == b) is a disc; non-finite or inverted input never hits.
        AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(a, a, 10, new Vector2(8, -2), new Vector2(12, 2)), "point capsule");
        AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(a, new Vector2(float.NaN, 0), 10, new Vector2(-5), new Vector2(5)), "non-finite end");
        AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(a, b, float.PositiveInfinity, new Vector2(-5), new Vector2(5)), "non-finite radius");
        AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(a, b, 10, new Vector2(5), new Vector2(-5)), "inverted box");
        // Rotated cases agree.
        foreach (float angle in new[] { .3f, 1.9f, -2.4f })
        {
            Vector2 ra = LacrimosaClawMotion.Rotate(new Vector2(10, 0), angle), rb = LacrimosaClawMotion.Rotate(new Vector2(160, 0), angle);
            Vector2 tip = LacrimosaClawMotion.Rotate(new Vector2(160 + radius - 3, 0), angle), past = LacrimosaClawMotion.Rotate(new Vector2(160 + radius + 3, 0), angle);
            AssertEqual(true, LacrimosaClawMotion.CapsuleHitsBox(ra, rb, radius, tip - new Vector2(.5f), tip + new Vector2(.5f)), $"rotated tip {angle}");
            AssertEqual(false, LacrimosaClawMotion.CapsuleHitsBox(ra, rb, radius, past - new Vector2(.5f), past + new Vector2(.5f)), $"rotated past {angle}");
        }
        // The scratch drawn for a rake ends where its hits end.
        for (int stroke = LacrimosaClawMotion.RakeDown; stroke <= LacrimosaClawMotion.RakeUp; stroke++)
        {
            AssertDollNear(LacrimosaClawMotion.LiveEnd(stroke) - 1, LacrimosaClawMotion.LastLiveBaseAge(stroke, LacrimosaClawMotion.BaseTicks(stroke)), 1e-4, "last swept base age");
            foreach (float speed in new[] { .5f, 1.25f, 1.5f, 4f })
            {
                int duration = LacrimosaClawMotion.Duration(stroke, speed);
                float last = LacrimosaClawMotion.LastLiveBaseAge(stroke, duration);
                int lastAge = (int)MathF.Round(last * duration / LacrimosaClawMotion.BaseTicks(stroke));
                AssertLacrimosa(LacrimosaClawMotion.Live(stroke, lastAge, duration) && !LacrimosaClawMotion.Live(stroke, lastAge + 1, duration),
                    $"stroke {stroke} speed {speed}: the last live tick is {lastAge}");
            }
        }
    }

    [DomainTest("Lacrimosa claw hands flash only on a rung swap, and never white")]
    private static void LacrimosaFlash()
    {
        AssertLacrimosa(LacrimosaClawMotion.FlashPeak <= .5f, "the flash keeps the art two-tone");
        for (int hand = 0; hand < 2; hand++)
        {
            LacrimosaHand last = LacrimosaClawMotion.Hand(hand, 0);
            float sinceSwap = 99;
            for (float t = .05f; t <= LacrimosaClawMotion.KataTicks; t += .05f)
            {
                LacrimosaHand next = LacrimosaClawMotion.Hand(hand, t);
                sinceSwap = next.Large != last.Large ? 0 : sinceSwap + .05f;
                AssertLacrimosa(next.Flash == 0 || sinceSwap < LacrimosaClawMotion.FlashTicks + .06f, $"kata hand {hand} flashes at {t} without a swap");
                last = next;
            }
        }
        foreach (int side in new[] { 1, -1 })
        for (int hand = 0; hand < 2; hand++)
        {
            LacrimosaHand last = Grasping(hand, 0, side);
            float sinceSwap = 99;
            for (float age = .05f; age <= LacrimosaClawMotion.GraspEnd; age += .05f)
            {
                LacrimosaHand next = Grasping(hand, age, side);
                sinceSwap = next.Large != last.Large ? 0 : sinceSwap + .05f;
                AssertLacrimosa(next.Flash == 0 || sinceSwap < LacrimosaClawMotion.FlashTicks + .06f, $"grasp hand {hand} flashes at {age} without a swap");
                AssertLacrimosa(next.Flash <= LacrimosaClawMotion.FlashPeak, "flash peak");
                last = next;
            }
            AssertEqual(0f, Grasping(hand, LacrimosaClawMotion.GraspContact + .5f, side).Flash, "clenching at contact does not flash");
            AssertLacrimosa(Grasping(hand, LacrimosaClawMotion.GraspArrive + .5f, side).Flash > 0, "the arrival's rung swap flashes");
        }

        static LacrimosaHand Grasping(int hand, float age, int side)
            => LacrimosaClawMotion.GraspHand(hand, age, new Vector2(800, 400), 40, side, new Vector2(150, 260), -.6f, new Vector2(200, 300), -1f);
    }

    [DomainTest("Lacrimosa heart beads fill from hits with tempo, trickle while held, freeze and spend")]
    private static void LacrimosaHeartMeter()
    {
        var heart = new LacrimosaHeart();
        for (int i = 0; i < 40; i++) heart.Tick(true, true, false);
        AssertEqual(10, heart.Units, "passive: 1 unit per 4 ticks");
        foreach ((bool held, bool usable, bool grasping) in new[] { (false, true, false), (true, false, false), (true, true, true) })
        {
            for (int i = 0; i < 40; i++) heart.Tick(held, usable, grasping);
            AssertEqual(10, heart.Units, $"frozen (held {held}, usable {usable}, grasping {grasping})");
        }
        heart.Reset();
        int idle = 0;
        while (!heart.Ready && idle < 5000) { heart.Tick(true, true, false); idle++; }
        AssertEqual(1440, idle, "empty to full in 24 s without a hit");

        heart.Reset();
        int[] expected = { 12, 12, 30, 15, 15, 36, 18, 18 };
        int[] strokes = { 0, 1, 2, 0, 1, 2, 0, 1 };
        for (int i = 0; i < expected.Length; i++)
            AssertEqual(expected[i], heart.Land((ulong)(i + 1), strokes[i], LacrimosaClawMotion.BaseTicks(strokes[i]), (ulong)(i * 25)), $"stroke {i} units");
        AssertEqual(0, heart.Land(8, 1, 24, 200), "a repeated stroke serial adds nothing");
        AssertEqual(156, heart.Units, "units after eight strokes");
        AssertEqual(2, heart.Lit, "lit beads");
        AssertEqual(12, heart.Land(9, 0, 26, 175 + LacrimosaHeart.StreakWindow + 1), "a streak ends 75 ticks after its last hit");
        // Attack speed: shorter strokes fill proportionally less per hit.
        var fast = new LacrimosaHeart();
        AssertEqual(6, fast.Land(1, 0, 13, 0), "a half-length rake fills half");
        AssertEqual(false, heart.Spend(), "a partial meter cannot be spent");
        while (!heart.Ready) heart.Tick(true, true, false);
        AssertEqual(LacrimosaHeart.Beads, heart.Lit, "six beads");
        AssertEqual(0, heart.Land(99, 2, 34, 400), "a full meter caps");
        AssertEqual(true, heart.Spend(), "a full meter is spent");
        AssertEqual(0, heart.Units, "spent empties");
        AssertEqual(0, heart.Streak, "the streak restarts after a grasp");

        // With every stroke connecting, the beads fill in a little under 6 s (v1 charged in 360 ticks of holding).
        var grasps = new int[1];
        LacrimosaClawScore.Timeline(800, 0, 1, graspTicks: grasps);
        AssertLacrimosa(grasps[0] > 300 && grasps[0] < 380, $"first grasp available at {grasps[0]}");
        foreach (float speed in new[] { 1.25f, 1.5f })
        {
            var quick = new int[1];
            LacrimosaClawScore.Timeline(800, 0, speed, graspTicks: quick);
            AssertLacrimosa(Math.Abs(quick[0] - grasps[0]) <= grasps[0] * .03 + 4, $"speed {speed} fills at {quick[0]} vs {grasps[0]}");
        }
    }

    [DomainTest("Lacrimosa combo chains A, B, C and restarts at A after idling or a grasp")]
    private static void LacrimosaCombo()
    {
        var combo = new LacrimosaCombo();
        static int Duration(int stroke) => LacrimosaClawMotion.BaseTicks(stroke);
        AssertEqual(0, combo.Take(0, Duration), "first A");
        AssertEqual(1, combo.Take(26, Duration), "then B");
        AssertEqual(2, combo.Take(50, Duration), "then C");
        AssertEqual(0, combo.Take(84, Duration), "back to A");
        AssertEqual(1, combo.Take(110 + LacrimosaClawMotion.ComboResetTicks, Duration), "B within the reset window");
        AssertEqual(0, combo.Take(155 + 24 + LacrimosaClawMotion.ComboResetTicks + 1, Duration), "A after 45 idle ticks");
        combo.Take(300, Duration);
        combo.Restart();
        AssertEqual(0, combo.Take(310, Duration), "A after a grasp");
        AssertEqual(0, new LacrimosaCombo().Peek(5), "fresh");
        combo.Take(1000, Duration);
        AssertEqual(0, combo.Peek(10), "a clock that went backwards restarts");
    }

    [DomainTest("Lacrimosa grasp phases, crush ellipse, target clamp and fist travel")]
    private static void LacrimosaGrasp()
    {
        int contact = 0, crush = 0, firstContact = -1, firstCrush = -1;
        for (int age = 0; age <= LacrimosaClawMotion.GraspEnd; age++)
        {
            if (LacrimosaClawMotion.ContactLive(age)) { contact++; if (firstContact < 0) firstContact = age; }
            if (LacrimosaClawMotion.CrushLive(age)) { crush++; if (firstCrush < 0) firstCrush = age; }
        }
        AssertEqual(2, contact, "contact ticks");
        AssertEqual(4, crush, "crush ticks");
        AssertLacrimosa(LacrimosaClawMotion.GraspFlight < LacrimosaClawMotion.GraspArrive && LacrimosaClawMotion.GraspArrive < firstContact
            && firstContact < LacrimosaClawMotion.SqueezeBeats[0] && LacrimosaClawMotion.SqueezeBeats[^1] < LacrimosaClawMotion.GraspBrace
            && LacrimosaClawMotion.GraspBrace < firstCrush && firstCrush + crush <= LacrimosaClawMotion.GraspRelease
            && LacrimosaClawMotion.GraspRelease < LacrimosaClawMotion.GraspEnd, "phases are ordered");
        for (int i = 2; i < LacrimosaClawMotion.SqueezeBeats.Length; i++)
            AssertLacrimosa(LacrimosaClawMotion.SqueezeBeats[i] - LacrimosaClawMotion.SqueezeBeats[i - 1]
                <= LacrimosaClawMotion.SqueezeBeats[i - 1] - LacrimosaClawMotion.SqueezeBeats[i - 2], "squeezes tighten");
        AssertEqual(true, LacrimosaClawMotion.GraspBusy(LacrimosaClawMotion.GraspRelease - 1), "busy before release");
        AssertEqual(false, LacrimosaClawMotion.GraspBusy(LacrimosaClawMotion.GraspRelease), "strokes resume at release");

        AssertEqual(true, LacrimosaClawMotion.CrushHits(new Vector2(165, 0), Vector2.Zero), "inside the wide radius");
        AssertEqual(false, LacrimosaClawMotion.CrushHits(new Vector2(167, 0), Vector2.Zero), "outside the wide radius");
        AssertEqual(true, LacrimosaClawMotion.CrushHits(new Vector2(0, 131), Vector2.Zero), "inside the tall radius");
        AssertEqual(false, LacrimosaClawMotion.CrushHits(new Vector2(130, 110), Vector2.Zero), "the corner is outside an ellipse");
        AssertEqual(true, LacrimosaClawMotion.CrushHits(new Vector2(220, 0), new Vector2(60, 60)), "a wide body reaches in");
        AssertEqual(false, LacrimosaClawMotion.CrushHits(new Vector2(float.NaN, 0), Vector2.Zero), "non-finite misses");
        Vector2 origin = new(1000, 1000);
        AssertDollNear(new Vector2(1500, 1000), LacrimosaClawMotion.ClampTarget(origin, new Vector2(1500, 1000)), .01f, "in range");
        AssertDollNear(new Vector2(2120, 1000), LacrimosaClawMotion.ClampTarget(origin, new Vector2(4000, 1000)), .01f, "clamped to 1120");
        AssertDollNear(origin, LacrimosaClawMotion.ClampTarget(origin, new Vector2(float.NaN, 0)), .01f, "non-finite stays home");

        // Fists: smooth travel between the arrival, the slam, the squeezes, the brace and the crush.
        float halfExtent = 40;
        float previous = LacrimosaClawMotion.FistGap(0, halfExtent);
        for (float age = .05f; age <= LacrimosaClawMotion.GraspRelease; age += .05f)
        {
            float gap = LacrimosaClawMotion.FistGap(age, halfExtent);
            AssertLacrimosa(MathF.Abs(gap - previous) < 4f, $"fist gap jumps at {age}");
            previous = gap;
        }
        AssertLacrimosa(LacrimosaClawMotion.FistGap(17, halfExtent) <= halfExtent + 6.5f, "fists press the target at contact");
        AssertLacrimosa(LacrimosaClawMotion.FistGap(39, halfExtent) > LacrimosaClawMotion.FistGap(30, halfExtent) + 10, "the brace parts the fists");
        AssertLacrimosa(LacrimosaClawMotion.FistGap(43, halfExtent) < 10, "the crush closes them");
        AssertLacrimosa(LacrimosaClawMotion.FlightProgress(3) == 0 && LacrimosaClawMotion.FlightProgress(14) == 1
            && LacrimosaClawMotion.FlightProgress(5) > .4f, "fast start, braked arrival");

        // The hands themselves: continuous except where a rung swaps under its flash.
        foreach (int side in new[] { 1, -1 })
        for (int hand = 0; hand < 2; hand++)
        {
            Vector2 center = new(800, 400);
            LacrimosaHand last = Grasping(hand, 0, side);
            for (float age = .1f; age <= LacrimosaClawMotion.GraspRelease; age += .1f)
            {
                LacrimosaHand next = Grasping(hand, age, side);
                float step = Vector2.Distance(last.Wrist, next.Wrist);
                bool swap = next.Pose != last.Pose || next.Large != last.Large;
                AssertLacrimosa(step < (swap ? 14f : 60f), $"hand {hand} side {side} jumps {step} px at {age}");
                last = next;
            }
            LacrimosaHand seated = Grasping(hand, 30, side);
            Vector2 toward = center - seated.Wrist;
            AssertLacrimosa(Vector2.Dot(Vector2.Normalize(toward), new Vector2(MathF.Cos(seated.Axis), MathF.Sin(seated.Axis))) > .99f, "fists point at the target");
            AssertEqual(LacrimosaPose.Clench, seated.Pose, "fists while held");
            AssertDollNear(new Vector2(200, 300), Grasping(hand, LacrimosaClawMotion.GraspRelease, side).Wrist, .5f, "home at release");
        }

        static LacrimosaHand Grasping(int hand, float age, int side)
            => LacrimosaClawMotion.GraspHand(hand, age, new Vector2(800, 400), 40, side, new Vector2(150, 260), -.6f, new Vector2(200, 300), -1f);
    }

    [DomainTest("Lacrimosa claw art lands every hit-shape anchor within one dot of its design anchor")]
    private static void LacrimosaArtFit()
    {
        foreach (LacrimosaSpriteArt art in LacrimosaClawArt.All)
        {
            AssertEqual(6, art.Beads.Length, $"{art.Name}: six bead anchors");
            AssertLacrimosa(art.K is 1 or 2, $"{art.Name}: integer rung");
            AssertLacrimosa(art.Name.EndsWith("_L") == (art.K == 1), $"{art.Name}: the _L sprites are the large k=1 rung");
            AssertLacrimosa(art.Tips.Length >= 4, $"{art.Name}: four or five talon tips");
        }
        foreach (LacrimosaPose pose in new[] { LacrimosaPose.Rake, LacrimosaPose.Thrust, LacrimosaPose.Clench })
        {
            LacrimosaSpriteArt art = LacrimosaClawArt.Sprite(pose, true);
            float design = LacrimosaClawArt.DesignReach(pose, true)!.Value;
            float tolerance = DollSpritePlacement.WorldPerTexel * art.K;
            AssertDollNear(design, LacrimosaClawArt.Reach(pose, art), tolerance, $"{art.Name} reach");
            int tip = LacrimosaClawArt.AxisTip(pose, art);
            if (tip < 0) continue;
            foreach (DollFlip flip in new[] { DollFlip.None, DollFlip.Vertical })
            for (int step = 0; step < 16; step++)
            {
                float axis = step * MathF.Tau / 16 + .07f;
                Vector2 wrist = new(1234.5f, 777.25f);
                Vector2 drawn = LacrimosaClawArt.Anchor(art.Tips[tip], pose, art, wrist, axis, flip);
                Vector2 designed = wrist + new Vector2(MathF.Cos(axis), MathF.Sin(axis)) * design;
                AssertDollNear(designed, drawn, tolerance, $"{art.Name} tip ({flip}, axis {axis})");
                AssertDollNear(wrist, LacrimosaClawArt.Anchor(art.Pivot, pose, art, wrist, axis, flip), .001f, "the cuff sits on the wrist");
            }
        }
        // The live capsules' far ends are the drawn tips, on the frames the hits use.
        for (float baseAge = 9; baseAge < 17; baseAge += .5f)
        {
            LacrimosaHand frame = LacrimosaClawMotion.Frame(LacrimosaClawMotion.RakeDown, 0, baseAge);
            LacrimosaClawMotion.TryCapsule(LacrimosaClawMotion.RakeDown, 0, baseAge, out _, out Vector2 b, out _);
            LacrimosaSpriteArt art = LacrimosaClawArt.Sprite(frame.Pose, frame.Large);
            Vector2 tip = LacrimosaClawArt.Anchor(art.Tips[LacrimosaClawArt.AxisTip(frame.Pose, art)], frame.Pose, art, frame.Wrist, frame.Axis, DollFlip.None);
            AssertDollNear(b, tip, DollSpritePlacement.WorldPerTexel * art.K, "rake capsule tip under the drawn talon");
        }
        AssertEqual(LacrimosaClawArt.Flip(false, 1), DollFlip.None, "right hand unflipped facing right");
        AssertEqual(LacrimosaClawArt.Flip(true, 1), DollFlip.Vertical, "left hand mirrored");
        AssertEqual(LacrimosaClawArt.Flip(false, -1), DollFlip.Vertical, "a left-facing owner mirrors the right hand");
    }
}
