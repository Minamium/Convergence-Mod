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
        foreach (int count in new[] { 2, 3, 4, 5, 6 }) // the final small body has two candles, the large one four
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

    [DomainTest("Ebon chandelier order follows summon order, not the projectile slot, and a newcomer never reshuffles the row")]
    private static void EbonChandelierSummonOrder()
    {
        // The owner's stamp is positive and strictly increasing with the update count.
        int last = 0;
        for (ulong tick = 0; tick < 5000; tick += 7)
        {
            int stamp = EbonChandelierRules.SummonOrder(tick);
            AssertEqual(true, stamp > last, "a later summon gets a later stamp");
            last = stamp;
        }
        AssertEqual(1, EbonChandelierRules.SummonOrder(0), "the first update stamps 1");
        AssertEqual((int)EbonChandelierRules.SummonOrderWrap, EbonChandelierRules.SummonOrder((ulong)(EbonChandelierRules.SummonOrderWrap - 1)), "the last stamp");
        AssertEqual(1, EbonChandelierRules.SummonOrder((ulong)EbonChandelierRules.SummonOrderWrap), "wraps to the first");

        // Slots 900, 12 and 400 were summoned in that order: the slot number does not decide the rank.
        long first = EbonChandelierRules.RosterKey(100, 900), second = EbonChandelierRules.RosterKey(160, 12), third = EbonChandelierRules.RosterKey(190, 400);
        AssertEqual(true, first < second && second < third, "summon order, not slot order");
        AssertEqual(true, EbonChandelierRules.RosterKey(100, 3) < EbonChandelierRules.RosterKey(100, 4), "the slot only breaks a tie");
        // A chandelier whose stamp has not arrived yet sorts after every stamped one, even from a lower slot,
        // and keeps that place once its stamp (the newest) arrives, so nothing already out is renumbered.
        long unstamped = EbonChandelierRules.RosterKey(0, 1);
        AssertEqual(true, unstamped > third && unstamped > EbonChandelierRules.RosterKey((int)EbonChandelierRules.SummonOrderWrap, 1023), "unstamped sorts last");
        AssertEqual(true, EbonChandelierRules.RosterKey(250, 1) > third, "the newest stamp is also last");

        // Ranks as the minion computes them: how many of the owner's keys are smaller.
        long[] row = { third, first, second, EbonChandelierRules.RosterKey(250, 7) };
        int[] expected = { 2, 0, 1, 3 };
        for (int i = 0; i < row.Length; i++)
        {
            int rank = 0;
            foreach (long key in row) if (key < row[i]) rank++;
            AssertEqual(expected[i], rank, "rank of chandelier " + i);
        }
        // Removing the oldest renumbers the later ones down by one; the order among them is unchanged.
        long[] shorter = { third, second, EbonChandelierRules.RosterKey(250, 7) };
        int[] after = { 1, 0, 2 };
        for (int i = 0; i < shorter.Length; i++)
        {
            int rank = 0;
            foreach (long key in shorter) if (key < shorter[i]) rank++;
            AssertEqual(after[i], rank, "rank after the oldest leaves, chandelier " + i);
        }
    }

    [DomainTest("Ebon chandelier hover row spreads the cascade across the target in drop order and keeps every fall on the target")]
    private static void EbonChandelierHoverRow()
    {
        const float reach = 60;
        for (int count = 1; count <= 16; count++)
        {
            var slots = new System.Collections.Generic.HashSet<int>();
            for (int ordinal = 0; ordinal < count; ordinal++)
            {
                int slot = EbonChandelierRules.HoverSlot(ordinal, count);
                AssertEqual(true, slot >= 0 && slot < count, "inside the row");
                AssertEqual(true, slots.Add(slot), "no two chandeliers hang at the same place");
                AssertEqual(true, MathF.Abs(EbonChandelierRules.HoverSpread(slot, count, reach)) < reach, "inside the reach");
                // Chandeliers that share a beat (ordinal and ordinal + 4) hang side by side.
                if (ordinal >= EbonRewardRules.CycleBeats)
                    AssertEqual(slot, EbonChandelierRules.HoverSlot(ordinal - EbonRewardRules.CycleBeats, count) + 1, "a repeated beat is the next slot");
                // The first cycle sweeps one way: later beats are farther along the row.
                if (ordinal > 0 && ordinal < EbonRewardRules.CycleBeats)
                    AssertEqual(true, slot > EbonChandelierRules.HoverSlot(ordinal - 1, count), "the cascade sweeps across the target");
            }

            float sum = 0;
            for (int slot = 0; slot < count; slot++)
            {
                sum += EbonChandelierRules.HoverSpread(slot, count, reach);
                if (slot > 0)
                {
                    float pitch = EbonChandelierRules.HoverSpread(slot, count, reach) - EbonChandelierRules.HoverSpread(slot - 1, count, reach);
                    AssertEqual(true, MathF.Abs(pitch - 2 * reach / count) < .001f, "equal shares");
                }
                // Neighbouring slots hang at different heights, except for a lone chandelier, which keeps the spec height.
                if (count > 1) AssertEqual(true, EbonChandelierRules.HoverRaise(slot, count) != EbonChandelierRules.HoverRaise(slot + 1, count), "neighbours alternate in height");
            }
            AssertEqual(true, MathF.Abs(sum) < .01f, "centred on the target");
        }
        AssertEqual(0f, EbonChandelierRules.HoverSpread(EbonChandelierRules.HoverSlot(0, 1), 1, reach), "a lone chandelier hangs straight above");
        AssertEqual(0f, EbonChandelierRules.HoverRaise(1, 1), "and at the spec height");

        // Four chandeliers fall left to right in drop order: 0 at the left edge of the row, 3 at the right.
        AssertEqual(true, EbonChandelierRules.HoverSpread(EbonChandelierRules.HoverSlot(0, 4), 4, reach) < 0
            && EbonChandelierRules.HoverSpread(EbonChandelierRules.HoverSlot(3, 4), 4, reach) > 0, "sweeps from left to right");

        // A fall is straight and its 40 px foot must still overlap the hitbox: the row never reaches farther
        // than the foot's half-width beyond the target's edge, and is bounded for huge targets.
        for (float width = 0; width <= 600; width += 5)
        {
            float row = EbonChandelierRules.HoverReach(width);
            AssertEqual(true, row <= width * .5f + 20 && row >= EbonChandelierRules.MinSpread && row <= EbonChandelierRules.MaxSpread, "reach for width " + width);
        }
        AssertEqual(EbonChandelierRules.MaxSpread, EbonChandelierRules.HoverReach(5000), "a huge boss does not spread the row without bound");
    }
}
