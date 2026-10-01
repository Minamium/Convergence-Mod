using System;
using System.Numerics;
using Convergence.Content.Encounters.EbonManor.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Moonshear kata keeps the specified stroke clocks")]
    private static void MoonshearStrokeClocks()
    {
        int[] duration = { 18, 16, 18, 30 }, release = { 7, 6, 7, 14 }, liveEnd = { 14, 12, 14, 20 };
        for (int stroke = 0; stroke < MoonshearMotion.Strokes; stroke++)
        {
            AssertEqual(duration[stroke], MoonshearMotion.Duration(stroke), "stroke duration");
            AssertEqual(release[stroke], MoonshearMotion.Release(stroke), "stroke release");
            AssertEqual(liveEnd[stroke], MoonshearMotion.LiveEnd(stroke), "stroke live end");
            AssertEqual(false, MoonshearMotion.Live(stroke, release[stroke] - 1), "no hit before release");
            AssertEqual(true, MoonshearMotion.Live(stroke, release[stroke]), "live at release");
            AssertEqual(false, MoonshearMotion.Live(stroke, liveEnd[stroke]), "half-open live window");
            AssertEqual(true, MoonshearMotion.Duration(stroke) <= MoonshearMotion.MaximumDuration, "bounded lifetime");
        }
        AssertEqual(true, MoonshearMotion.Cuts(MoonshearMotion.Down, true) && !MoonshearMotion.Cuts(MoonshearMotion.Down, false), "A cuts with the upper blade");
        AssertEqual(true, MoonshearMotion.Cuts(MoonshearMotion.Rise, false) && !MoonshearMotion.Cuts(MoonshearMotion.Rise, true), "B cuts with the lower blade");
        AssertEqual(true, MoonshearMotion.Cuts(MoonshearMotion.Wide, true) && !MoonshearMotion.Cuts(MoonshearMotion.Wide, false), "C cuts with the upper blade");
        AssertEqual(true, MoonshearMotion.Cuts(MoonshearMotion.Snip, true) && MoonshearMotion.Cuts(MoonshearMotion.Snip, false), "D cuts with both");
        AssertEqual(true, MathF.Abs(MoonshearMotion.Multiplier(MoonshearMotion.Snip) - 2.2f) < 1e-6f, "snip multiplier");
        AssertEqual(false, MoonshearMotion.Marks(MoonshearMotion.Snip), "only A-C leave marks");
        int kata = 0; float budget = 0;
        for (int stroke = 0; stroke < MoonshearMotion.Strokes; stroke++) { kata += MoonshearMotion.Duration(stroke); budget += MoonshearMotion.Multiplier(stroke); }
        AssertEqual(82, kata, "full kata ticks");
        AssertEqual(true, MathF.Abs(budget - 5.2f) < 1e-5f, "nominal kata budget");
    }

    [DomainTest("Moonshear strokes flow into each other without a brake or snap")]
    private static void MoonshearFlow()
    {
        for (int stroke = 0; stroke < MoonshearMotion.Strokes; stroke++)
        {
            int next = (stroke + 1) % MoonshearMotion.Strokes, duration = MoonshearMotion.Duration(stroke);
            // The pose (both blades) and angular speed at the end equal the next stroke's start; D wraps 2pi to A.
            float axisGap = MathF.IEEERemainder(MoonshearMotion.Axis(stroke, duration) - MoonshearMotion.Axis(next, 0), MathF.Tau);
            AssertEqual(true, MathF.Abs(axisGap) < 1e-4f, "adjacent axis pose");
            AssertEqual(true, MathF.Abs(MoonshearMotion.Open(stroke, duration) - MoonshearMotion.Open(next, 0)) < 1e-4f, "adjacent opening");
            for (int b = 0; b < 2; b++)
            {
                bool upper = b == 0;
                float exit = Speed(stroke, duration - .001f, upper), entry = Speed(next, .001f, upper);
                AssertEqual(true, MathF.Abs(exit - entry) < .01f, "blade momentum carries across the join");
                float peak = 0, jump = 0;
                for (int i = 1; i < duration * 20; i++)
                {
                    float t = i / 20f;
                    float acceleration = (MoonshearMotion.Blade(stroke, t + .05f, upper) - 2 * MoonshearMotion.Blade(stroke, t, upper)
                        + MoonshearMotion.Blade(stroke, t - .05f, upper)) / (.05f * .05f);
                    peak = MathF.Max(peak, MathF.Abs(acceleration));
                }
                for (int i = 1; i <= duration * 20; i++)
                {
                    float t = i / 20f;
                    jump = MathF.Max(jump, MathF.Abs(MoonshearMotion.Blade(stroke, t, upper) - MoonshearMotion.Blade(stroke, t - .05f, upper)));
                }
                AssertEqual(true, peak < .3f, "peak angular acceleration under 0.3 rad/tick^2");
                AssertEqual(true, jump < .08f, "no pose jump at 120 fps");
            }
            AssertEqual(true, MathF.Abs(MoonshearMotion.AxisSpeed(stroke, duration) - MoonshearMotion.AxisSpeed(next, 0)) < .01f,
                "analytic axis speed matches across the join");
            for (int i = 0; i <= duration * 10; i++)
            {
                float t = i / 10f;
                float arm = MoonshearMotion.ArmAngle(stroke, t, .2f, 1), axis = MoonshearMotion.AxisAngle(stroke, t, .2f, 1);
                AssertEqual(true, float.IsFinite(arm) && MathF.Abs(arm - axis) < .24f, "connected wrist");
                AssertEqual(true, MoonshearMotion.Open(stroke, t) > -.12f, "blades never cross far");
            }
            float mirrored = MoonshearMotion.BladeAngle(stroke, 5, MathF.PI, -1, true);
            AssertEqual(true, MathF.Abs(MathF.IEEERemainder(mirrored - (MathF.PI - MoonshearMotion.Blade(stroke, 5, true)), MathF.Tau)) < 1e-4f,
                "facing mirrors the pose");
        }

        static float Speed(int stroke, float t, bool upper)
            => (MoonshearMotion.Blade(stroke, t + .001f, upper) - MoonshearMotion.Blade(stroke, t - .001f, upper)) / .002f;
    }

    [DomainTest("Moonshear blades fan open for A-C and snap shut at the Snip's full reach")]
    private static void MoonshearOpening()
    {
        for (int stroke = 0; stroke < MoonshearMotion.Snip; stroke++)
            for (int age = MoonshearMotion.Release(stroke); age <= MoonshearMotion.LiveEnd(stroke); age++)
                AssertEqual(true, MoonshearMotion.Open(stroke, age) > .2f, "twin-blade opening during the live cut");
        // A descends with the upper blade, B rises with the lower, C sweeps wider than A.
        float aSweep = MoonshearMotion.Blade(0, 14, true) - MoonshearMotion.Blade(0, 7, true);
        float bSweep = MoonshearMotion.Blade(1, 12, false) - MoonshearMotion.Blade(1, 6, false);
        float cSweep = MoonshearMotion.Blade(2, 14, true) - MoonshearMotion.Blade(2, 7, true);
        AssertEqual(true, aSweep > 2, "A falls through a long diagonal");
        AssertEqual(true, bSweep < -2, "B rises in reverse");
        AssertEqual(true, cSweep > aSweep + .1f, "C is the widest of the three");
        int snip = MoonshearMotion.Snip, close = MoonshearMotion.SnipClose;
        AssertEqual(true, MoonshearMotion.Open(snip, 12) > .8f, "wide open behind");
        float behind = MathF.IEEERemainder(MoonshearMotion.Axis(snip, 7), MathF.Tau);
        AssertEqual(true, MathF.Abs(behind) > MathF.PI / 2, "the open shears pass behind the body");
        AssertEqual(true, MathF.Abs(MoonshearMotion.Open(snip, close)) < 1e-4f, "shut at the close tick");
        AssertEqual(true, close >= MoonshearMotion.Release(snip) && close < MoonshearMotion.LiveEnd(snip), "the close is live");
        AssertEqual(true, MathF.Abs(MathF.IEEERemainder(MoonshearMotion.Axis(snip, close), MathF.Tau)) < 1e-4f, "closes on the aim");
        AssertEqual(true, MoonshearMotion.Thrust(snip, close) > MoonshearMotion.Thrust(snip, 10) + 10, "drives forward into the close");
        AssertEqual(0f, MoonshearMotion.Thrust(snip, 0), "no thrust at entry");
        AssertEqual(0f, MoonshearMotion.Thrust(snip, MoonshearMotion.Duration(snip)), "no thrust at exit");
        AssertEqual(0f, MoonshearMotion.Thrust(0, 10), "A-C keep the pivot at the hand");
    }

    [DomainTest("Moonshear draw age keeps live and recovery honest")]
    private static void MoonshearDrawAge()
    {
        AssertEqual(7f, MoonshearMotion.DrawAge(0, 7, 0), "a live tick never shows the wind-up");
        AssertEqual(14f, MoonshearMotion.DrawAge(0, 14, 0), "a recovery tick never shows the live pose");
        AssertEqual(18f, MoonshearMotion.DrawAge(0, 18, 1), "the end pose is drawn before the next stroke");
        AssertEqual(true, MathF.Abs(MoonshearMotion.DrawAge(1, 3, .5f) - 2.5f) < 1e-6f, "fractional between ticks");
    }

    [DomainTest("Moonshear combo cycles A-D and resets after idle")]
    private static void MoonshearComboCycle()
    {
        var combo = new MoonshearCombo();
        ulong now = 100;
        for (int expected = 0; expected < 6; expected++)
        {
            int stroke = combo.Take(now);
            AssertEqual(expected % 4, stroke, "kata order wraps from D to A");
            now += (ulong)MoonshearMotion.Duration(stroke);
        }
        now += 80;
        AssertEqual(2, combo.Take(now), "80 idle ticks after a stroke's end keep the combo");
        now += (ulong)MoonshearMotion.Duration(2) + 81;
        AssertEqual(0, combo.Take(now), "81 idle ticks restart at A");
        combo.Take(now + 20);
        combo.Reset();
        AssertEqual(0, combo.Take(now + 40), "item change or death restarts at A");
    }

    [DomainTest("Moonshear marks cap, refresh, expire and never transfer between NPCs")]
    private static void MoonshearMarkLedger()
    {
        var marks = new MoonshearMarks(8);
        for (int i = 0; i < 7; i++) marks.Add(3, 42, (ulong)(10 + i));
        AssertEqual(5, marks.Count(3), "at most five marks");
        AssertEqual(false, marks.Expire(3, 42, 16 + 360), "alive until MarkLife after the newest");
        AssertEqual(true, marks.Expire(3, 42, 16 + 361), "expires MarkLife ticks after the newest mark");
        AssertEqual(0, marks.Count(3), "expired marks are gone");
        marks.Add(3, 42, 1000);
        marks.Add(3, 42, 1300);
        AssertEqual(false, marks.Expire(3, 42, 1500), "a new mark refreshes the whole stack");
        AssertEqual(true, marks.Expire(3, -1, 1500), "a dead NPC clears its marks");
        marks.Add(4, 42, 10);
        AssertEqual(1, marks.Add(4, 77, 11), "a reused slot starts fresh");
        AssertEqual(0, marks.Take(4, 42), "another NPC type cannot pop them");
        marks.Add(5, 9, 10); marks.Add(5, 9, 11);
        AssertEqual(true, marks.Any(), "marks are tracked");
        AssertEqual(2, marks.Take(5, 9), "a cut takes every mark");
        AssertEqual(0, marks.Count(5), "taken marks are spent");
        AssertEqual(0, marks.Add(99, 1, 1), "out-of-range slots are ignored");
        marks.Add(6, 1, 1); marks.Clear();
        AssertEqual(false, marks.Any(), "clear empties the ledger");
    }

    [DomainTest("Moonshear Cut Line runs forecast, travel, tear then pops on its clock")]
    private static void MoonshearCutClock()
    {
        AssertEqual(18, MoonshearMotion.TravelStart, "forecast ticks");
        AssertEqual(30, MoonshearMotion.TearStart, "travel ticks");
        AssertEqual(40, MoonshearMotion.TearEnd, "tear ticks");
        AssertEqual(false, MoonshearMotion.TearLive(29), "harmless while the shears race");
        AssertEqual(true, MoonshearMotion.TearLive(30) && MoonshearMotion.TearLive(39), "tear live for ten ticks");
        AssertEqual(false, MoonshearMotion.TearLive(40), "tear closes");
        AssertEqual(44, MoonshearMotion.PopTick(1), "one pop per four ticks");
        AssertEqual(130, MoonshearMotion.Cooldown, "cooldown 90 ticks after the tear");
        AssertEqual(0f, MoonshearMotion.Travel(18), "shears leave at the travel");
        AssertEqual(1f, MoonshearMotion.Travel(30), "shears arrive as the tear opens");
        AssertEqual(true, MoonshearMotion.Forecast(9) > .3f && MoonshearMotion.Forecast(9) < .7f, "chalk grows over the forecast");
        var end = MoonshearMotion.ClampLine(Vector2.Zero, new Vector2(3000, 0), Vector2.UnitX);
        AssertEqual(true, MathF.Abs(end.X - 900) < .01f, "cut range is clamped to 900 px");
        end = MoonshearMotion.ClampLine(Vector2.Zero, new Vector2(1, 1), -Vector2.UnitX);
        AssertEqual(true, end.X < -47, "a cursor on the hand cuts along the aim");
        AssertEqual(.5f, MoonshearMotion.Along(new Vector2(450, 30), Vector2.Zero, new Vector2(900, 0)), "pops are ordered along the line");
        float nominal = EbonRewardRules.CutMultiplier + EbonRewardRules.MaxMarks * EbonRewardRules.PopMultiplier;
        AssertEqual(true, MathF.Abs(nominal - 5.5f) < 1e-5f, "nominal five-mark cut");
    }
}
