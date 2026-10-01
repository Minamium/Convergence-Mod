using System;
using System.Numerics;
using Convergence.Content.Encounters.EbonManor.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Ebon chandeliers drop on their own beat of the four-beat cycle, never early and never drifting")]
    private static void EbonChandelierBeatSchedule()
    {
        // Chandelier i owns beat (i mod 4): the first drop is the first whole update at or after that beat.
        for (int index = 0; index < 12; index++)
        {
            long first = EbonChandelierRules.DropAt(index, 0);
            AssertEqual((long)Math.Ceiling(index % EbonRewardRules.CycleBeats * (double)EbonRewardRules.BeatTicks - 1e-6), first,
                "first drop of chandelier " + index);

            long previous = first;
            for (int cycle = 1; cycle <= 1000; cycle++)
            {
                long next = EbonChandelierRules.DropAt(index, previous + 1);
                long gap = next - previous;
                AssertEqual(true, gap is 115 or 116, "one drop per four-beat cycle (115.2 ticks)");
                previous = next;
            }
            // 1000 cycles of 115.2 ticks: no accumulated drift.
            AssertEqual(true, Math.Abs(previous - first - 115200) <= 2, "no drift over 1000 cycles");
        }

        // A request is answered by the first update at or after `now`, and never more than one cycle later.
        for (int index = 0; index < 8; index++)
            for (long now = 0; now < 800; now++)
            {
                long drop = EbonChandelierRules.DropAt(index, now);
                AssertEqual(true, drop >= now, "never earlier than asked");
                AssertEqual(true, drop - now <= 116, "at most one cycle away");
                AssertEqual(drop, EbonChandelierRules.DropAt(index, drop), "asking on the drop tick returns that tick");
            }

        // Four chandeliers rain one per beat; the fifth shares the first one's beat.
        long[] ticks = new long[8];
        for (int index = 0; index < ticks.Length; index++) ticks[index] = EbonChandelierRules.DropAt(index, 0);
        for (int index = 1; index < 4; index++)
            AssertEqual(true, ticks[index] - ticks[index - 1] is 28 or 29 or 30, "consecutive chandeliers are one beat apart");
        for (int index = 4; index < 8; index++) AssertEqual(ticks[index - 4], ticks[index], "index 4 and above reuse the beats");
    }

    [DomainTest("Ebon chandelier fall follows the tuned acceleration and the lead matches the fall time")]
    private static void EbonChandelierFall()
    {
        AssertEqual(2f, EbonChandelierRules.FallSpeed(0), "starts at the drop speed");
        AssertEqual(true, MathF.Abs(EbonChandelierRules.FallSpeed(10) - 13f) < .0001f, "gravity 1.1 per tick");
        AssertEqual(30f, EbonChandelierRules.FallSpeed(500), "capped at the maximum");
        float previous = 0;
        for (int age = 0; age < 60; age++)
        {
            float speed = EbonChandelierRules.FallSpeed(age);
            AssertEqual(true, speed >= previous && speed <= 30f, "monotonic and bounded");
            previous = speed;
        }
        AssertEqual(0, EbonChandelierRules.FallTicks(0), "no distance, no fall");
        for (float distance = 1; distance <= 2400; distance += 7.5f)
        {
            int ticks = EbonChandelierRules.FallTicks(distance);
            AssertEqual(true, EbonChandelierRules.FallDistance(ticks) >= distance, "the fall covers the distance");
            AssertEqual(true, EbonChandelierRules.FallDistance(ticks - 1) < distance, "and no tick too many");
        }
        // The spec's 260 px hover (centre to target) is a 19-tick fall from the chandelier's lowest point.
        int hoverFall = EbonChandelierRules.FallTicks(EbonRewardRules.HoverHeight - EbonChandelierRules.BodyHalfHeight);
        AssertEqual(19, hoverFall, "hover fall time");
        Vector2 lead = EbonChandelierRules.Lead(new Vector2(10, -4), hoverFall);
        AssertEqual(true, MathF.Abs(lead.X - 10 * MathF.Min(hoverFall, EbonChandelierRules.MaxLead)) < .001f, "lead is velocity times the fall");
        Vector2 clamped = EbonChandelierRules.Lead(new Vector2(500, -500), 40);
        AssertEqual(true, clamped.X == 220 && clamped.Y == -120, "lead is bounded");
    }

    [DomainTest("Ebon chandelier shatter, reweave and anticipation curves are continuous and end at rest")]
    private static void EbonChandelierCurves()
    {
        AssertEqual(0f, EbonChandelierRules.Scatter(0), "pieces start whole");
        AssertEqual(1f, EbonChandelierRules.Scatter(.2f), "burst peaks at one fifth");
        AssertEqual(0f, EbonChandelierRules.Scatter(1), "pieces end back together");
        float last = 0;
        for (int i = 0; i <= 200; i++)
        {
            float value = EbonChandelierRules.Scatter(i / 200f);
            AssertEqual(true, value >= 0 && value <= 1.0001f, "scatter stays in range");
            if (i <= 40) AssertEqual(true, value >= last - 1e-6f, "bursting out");
            else AssertEqual(true, value <= last + 1e-6f, "flying back");
            last = value;
        }

        // The reel adds up to the full rise, one step per tick of the reweave, starting and ending gently.
        float total = 0;
        for (int age = 1; age <= EbonRewardRules.ReweaveTicks; age++)
        {
            float step = EbonChandelierRules.ReelStep(age);
            AssertEqual(true, step >= 0, "only ever reeled up");
            total += step;
        }
        AssertEqual(true, MathF.Abs(total - EbonChandelierRules.ReelRise) < .01f, "full rise");
        AssertEqual(0f, EbonChandelierRules.ReelStep(0), "nothing before the shatter");
        AssertEqual(true, EbonChandelierRules.ReelStep(1) < EbonChandelierRules.ReelStep(EbonRewardRules.ReweaveTicks / 2), "eases in");
        AssertEqual(true, EbonChandelierRules.ReelStep(EbonRewardRules.ReweaveTicks) < EbonChandelierRules.ReelStep(EbonRewardRules.ReweaveTicks / 2), "eases out");

        // Candles relight one by one, in order, and all are lit before the reweave ends.
        foreach (int count in new[] { 3, 4, 5, 6 })
        {
            float previousStart = -1;
            for (int candle = 0; candle < count; candle++)
            {
                float start = EbonChandelierRules.RelightStart(candle, count);
                AssertEqual(true, start > previousStart, "one after another");
                AssertEqual(true, start + EbonChandelierRules.CatchTicks <= EbonRewardRules.ReweaveTicks, "lit before the reweave ends");
                AssertEqual(0f, EbonChandelierRules.Relit(start - .5f, candle, count), "dark until its turn");
                AssertEqual(1f, EbonChandelierRules.Relit(EbonRewardRules.ReweaveTicks, candle, count), "lit at the end");
                previousStart = start;
            }
        }

        // The cut: the build starts only once a drop is scheduled and peaks on the beat.
        AssertEqual(0f, EbonChandelierRules.Anticipation(-1), "no build before scheduling");
        AssertEqual(0f, EbonChandelierRules.Anticipation(EbonChandelierRules.AnticipationTicks), "no build until the last ticks");
        AssertEqual(1f, EbonChandelierRules.Anticipation(0), "full at the cut");
        float build = 0;
        for (int wait = EbonChandelierRules.AnticipationTicks; wait >= 0; wait--)
        {
            float value = EbonChandelierRules.Anticipation(wait);
            AssertEqual(true, value >= build, "builds monotonically");
            build = value;
        }
        float retract = 0;
        for (int t = 0; t < 60; t++)
        {
            float value = EbonChandelierRules.Retract(t);
            AssertEqual(true, value >= retract, "the severed thread only moves away");
            retract = value;
        }
    }

    [DomainTest("Ebon chandelier idle row is a distinct staggered row behind the owner and the footprint test is exact")]
    private static void EbonChandelierRowAndFootprint()
    {
        for (int facing = -1; facing <= 1; facing += 2)
            for (int count = 1; count <= 10; count++)
            {
                var seen = new System.Collections.Generic.HashSet<(int, int)>();
                for (int index = 0; index < count; index++)
                {
                    Vector2 slot = EbonChandelierRules.IdleSlot(index, count, facing);
                    AssertEqual(true, seen.Add(((int)MathF.Round(slot.X), (int)MathF.Round(slot.Y))), "no two chandeliers share a slot");
                    AssertEqual(true, slot.Y < -100 && slot.Y > -200, "hangs above the owner");
                    AssertEqual(true, MathF.Abs(slot.X) < 360, "stays near the owner");
                }
                if (count > 1)
                {
                    // Neighbours differ in height (small and large alternate) and the row is centred behind the owner.
                    AssertEqual(true, EbonChandelierRules.IdleSlot(0, count, facing).Y != EbonChandelierRules.IdleSlot(1, count, facing).Y,
                        "neighbours alternate in height");
                    float sum = 0;
                    for (int index = 0; index < count; index++) sum += EbonChandelierRules.IdleSlot(index, count, facing).X;
                    AssertEqual(true, MathF.Abs(sum / count + facing * 72) < .01f, "centred 72 px behind the owner");
                }
            }

        var min = new Vector2(100, 100);
        var max = new Vector2(140, 180);
        AssertEqual(true, EbonChandelierRules.CircleTouchesBox(new Vector2(120, 140), 1, min, max), "centre inside");
        AssertEqual(true, EbonChandelierRules.CircleTouchesBox(new Vector2(250, 140), 110, min, max), "reaches the near edge exactly");
        AssertEqual(false, EbonChandelierRules.CircleTouchesBox(new Vector2(251, 140), 110, min, max), "a pixel short");
        // The corner is measured diagonally, not along an axis.
        AssertEqual(false, EbonChandelierRules.CircleTouchesBox(new Vector2(220, 260), 110, min, max), "corner is farther than the radius");
        AssertEqual(true, EbonChandelierRules.CircleTouchesBox(new Vector2(200, 240), 110, min, max), "corner within the radius");
    }
}
