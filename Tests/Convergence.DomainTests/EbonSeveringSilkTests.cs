using System;
using System.Collections.Generic;
using System.Numerics;
using Convergence.Content.Encounters.EbonManor.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Severing Silk scissors cruise, brake and cover exactly the planned range")]
    private static void SeveringSilkScissorsFlight()
    {
        for (int ticks = EbonSilkMath.BrakeTicks; ticks <= EbonRewardRules.ScissorsLife; ticks++)
        {
            float sum = 0, previous = float.MaxValue;
            for (int i = 0; i < ticks; i++)
            {
                float step = EbonSilkMath.FlightStep(i, ticks);
                sum += step;
                AssertEqual(true, step > 0 && step <= previous + 1e-4f, "steps stay positive and never speed up");
                previous = step;
                if (i < ticks - EbonSilkMath.BrakeTicks)
                    AssertEqual(true, MathF.Abs(step - EbonRewardRules.ScissorsSpeed) < 1e-4f, "cruise runs at the spec speed");
            }
            AssertEqual(true, MathF.Abs(sum - EbonSilkMath.FlightRange(ticks)) < .01f, "braking still covers the planned range");
            AssertEqual(0f, EbonSilkMath.FlightStep(ticks, ticks), "no step after arrival");
            AssertEqual(ticks, EbonSilkMath.FlightTicks(EbonSilkMath.FlightRange(ticks)), "range round-trips through ticks");
            AssertEqual(true, EbonSilkMath.FlightRange(ticks) <= EbonRewardRules.ScissorsSpeed * EbonRewardRules.ScissorsLife,
                "never farther than speed x life");
        }
        float[] lastFour = { 21f, 15f, 9f, 3f };
        for (int j = 0; j < 4; j++)
            AssertEqual(true, MathF.Abs(EbonSilkMath.FlightStep(26 + j, 30) - lastFour[j]) < 1e-3f, "the brake is linear 21/15/9/3 px");
        AssertEqual(EbonRewardRules.ScissorsLife, EbonSilkMath.FlightTicks(100000f), "a far cursor is capped at 30 ticks");
        AssertEqual(EbonSilkMath.BrakeTicks, EbonSilkMath.FlightTicks(0f), "a cursor at the thrower still brakes in");
    }

    [DomainTest("Severing Silk strands tighten six ticks then part together for two live ticks")]
    private static void SeveringSilkSeverTimeline()
    {
        var live = new List<int>();
        var tightening = new List<int>();
        for (int phase = 0; phase < 80; phase++)
        {
            if (EbonSilkMath.IsLive(phase)) live.Add(phase);
            if (EbonSilkMath.Tightening(phase)) tightening.Add(phase);
        }
        AssertEqual(EbonRewardRules.TightenTicks, tightening.Count, "tightens for TightenTicks");
        AssertEqual(1, tightening[0], "tightening starts on the arming tick");
        AssertEqual(2, live.Count, "live for exactly two ticks");
        AssertEqual(EbonRewardRules.TightenTicks + 1, live[0], "live starts right after the tightening");
        AssertEqual(live[0] + 1, live[1], "the two live ticks are consecutive");
        AssertEqual(EbonSilkMath.SeverPhase, live[0], "the parting phase is the first live phase");
        for (int rank = 0; rank < EbonRewardRules.MaxStrands; rank++)
        {
            AssertEqual(true, EbonSilkMath.CascadeAt(rank) >= EbonSilkMath.SeverPhase, "no curl before the parting");
            AssertEqual(true, EbonSilkMath.CascadeAt(rank) + EbonSilkMath.CurlLife <= EbonSilkMath.EndPhase, "the carrier outlives every curl");
            if (rank > 0) AssertEqual(EbonSilkMath.CascadeStep, EbonSilkMath.CascadeAt(rank) - EbonSilkMath.CascadeAt(rank - 1), "ripple spacing");
        }
        AssertEqual(EbonSilkMath.CascadeAt(EbonRewardRules.MaxStrands - 1), EbonSilkMath.CascadeAt(99), "rank is capped");
        AssertEqual(true, EbonSilkMath.ScissorsEndPhase > EbonSilkMath.SeverPhase + EbonSilkMath.LiveTicks,
            "the scissors recover after the snip");
        AssertEqual(true, EbonSilkMath.Retract(0) == 0 && EbonSilkMath.Retract(EbonSilkMath.RecoilTicks) == 1, "halves recoil 0 -> 1");
        float before = 0;
        for (float t = 0; t <= EbonSilkMath.RecoilTicks; t += .25f)
        {
            float r = EbonSilkMath.Retract(t);
            AssertEqual(true, r >= before - 1e-6f, "recoil never reverses");
            before = r;
        }
    }

    [DomainTest("Severing Silk beak opens through the tightening and snaps shut on the parting")]
    private static void SeveringSilkBeak()
    {
        AssertEqual(0f, EbonSilkMath.Gape(-EbonSilkMath.GapeLead - 3), "closed while flying in");
        AssertEqual(true, MathF.Abs(EbonSilkMath.Gape(EbonRewardRules.TightenTicks) - 1) < 1e-4f, "fully open at the parting");
        AssertEqual(true, EbonSilkMath.Gape(EbonRewardRules.TightenTicks + 2) < 1e-4f, "shut within two ticks");
        float previous = 0;
        for (float t = -EbonSilkMath.GapeLead; t <= EbonRewardRules.TightenTicks; t += .25f)
        {
            float g = EbonSilkMath.Gape(t);
            AssertEqual(true, g >= previous - 1e-6f && g >= 0 && g <= 1.0001f, "opens monotonically inside 0..1");
            previous = g;
        }
    }

    [DomainTest("Severing Silk cut point, ripple order, oldest strand and note spread")]
    private static void SeveringSilkOrdering()
    {
        Vector2 a = new(0, 0), b = new(400, 0);
        AssertEqual(EbonSilkMath.CutMin, EbonSilkMath.CutParameter(a, b, new(-900, 50)), "cut keeps off the near end");
        AssertEqual(EbonSilkMath.CutMax, EbonSilkMath.CutParameter(a, b, new(9000, 50)), "cut keeps off the far end");
        AssertEqual(true, MathF.Abs(EbonSilkMath.CutParameter(a, b, new(200, 77)) - .5f) < 1e-4f, "cut is the projection of the scissors");
        AssertEqual(.5f, EbonSilkMath.CutParameter(a, a, new(10, 10)), "a degenerate strand cuts at its middle");

        var rank = new int[4];
        EbonSilkMath.Rank(new float[] { 5, 1, 3, 1 }, rank);
        AssertEqual("3,0,2,1", string.Join(',', rank), "nearest first, ties by index");
        var many = new float[14]; var manyRank = new int[14];
        for (int i = 0; i < many.Length; i++) many[i] = i;
        EbonSilkMath.Rank(many, manyRank);
        AssertEqual(EbonRewardRules.MaxStrands - 1, manyRank[13], "ranks never exceed MaxStrands - 1");

        AssertEqual(2, EbonSilkMath.Oldest(new[] { 300, 120, 40 }, new[] { true, true, true }), "least time left is oldest");
        AssertEqual(1, EbonSilkMath.Oldest(new[] { 300, 120, 40 }, new[] { true, true, false }), "ineligible strands are skipped");
        AssertEqual(-1, EbonSilkMath.Oldest(new[] { 300 }, new[] { false }), "nothing eligible");

        AssertEqual(0, EbonSilkMath.NoteFor(0, 1), "a single strand rings the root");
        AssertEqual(0, EbonSilkMath.NoteFor(0, 2), "two strands: root");
        AssertEqual(EbonRewardRules.NoteCount - 1, EbonSilkMath.NoteFor(1, 2), "two strands: top");
        for (int count = 2; count <= EbonRewardRules.MaxStrands; count++)
        {
            int last = -1;
            for (int r = 0; r < count; r++)
            {
                int note = EbonSilkMath.NoteFor(r, count);
                AssertEqual(true, note >= last && note >= 0 && note < EbonRewardRules.NoteCount, "notes climb inside the arpeggio");
                last = note;
            }
            AssertEqual(EbonRewardRules.NoteCount - 1, last, "the last strand rings the top note");
        }
        AssertEqual(true, EbonSilkMath.Worthy(new(0, 0), new(EbonSilkMath.MinStrand, 0)), "minimum strand");
        AssertEqual(false, EbonSilkMath.Worthy(new(0, 0), new(EbonSilkMath.MinStrand - 1, 0)), "too short is not pinned");
        AssertEqual(false, EbonSilkMath.Worthy(new(0, 0), new(EbonSilkMath.MaxStrand + 1, 0)), "too long is not pinned");
        AssertEqual(false, EbonSilkMath.Worthy(new(0, 0), new(float.NaN, 0)), "non-finite is not pinned");
    }

    [DomainTest("Severing Silk bow settles after the pin and knots sit where strands cross")]
    private static void SeveringSilkBowAndKnots()
    {
        AssertEqual(0f, EbonSilkMath.Settle(0), "a fresh pin is taut");
        AssertEqual(true, MathF.Abs(EbonSilkMath.Settle(120) - 1) < .01f, "the bow settles to its rest sag");
        float peak = 0;
        for (float t = 0; t < 20; t += .25f) peak = MathF.Max(peak, EbonSilkMath.Settle(t));
        AssertEqual(true, peak > 1.3f, "the pin quivers past rest before settling");
        AssertEqual(0f, EbonSilkMath.Sag(0), "no sag without length");
        AssertEqual(true, EbonSilkMath.Sag(100000) <= 7 + 1e-4f, "the sag stays slight");

        Vector2 a = new(0, 0), b = new(400, 200);
        AssertEqual(true, (EbonSilkMath.Bow(a, b, 9, 0) - a).Length() < 1e-4f && (EbonSilkMath.Bow(a, b, 9, 1) - b).Length() < 1e-4f,
            "ends stay pinned however bowed");
        Vector2 straight = EbonSilkMath.Bow(a, b, 0, .5f), bowed = EbonSilkMath.Bow(a, b, 9, .5f);
        AssertEqual(true, (straight - new Vector2(200, 100)).Length() < 1e-4f, "no offset is a straight line");
        AssertEqual(true, MathF.Abs((bowed - straight).Length() - 9) < 1e-3f && bowed.Y >= straight.Y, "the bow sags by its offset, downward");

        // A string leans along the left normal of its ends; ordering them by Flipped makes every strand bow toward gravity.
        for (int step = 0; step < 24; step++)
        {
            float angle = step * MathF.PI / 12;
            Vector2 start = new(0, 0), end = new(MathF.Cos(angle) * 100, MathF.Sin(angle) * 100);
            if (EbonSilkMath.Flipped(start, end)) (start, end) = (end, start);
            Vector2 left = new(-(end.Y - start.Y), end.X - start.X);
            AssertEqual(true, left.Y > -1e-3f, "an oriented string never bows upward");
        }
        AssertEqual(true, EbonSilkMath.Flipped(new(0, 0), new(0, 100)), "a level tie on a downward vertical strand is swapped");
        AssertEqual(false, EbonSilkMath.Flipped(new(0, 100), new(0, 0)), "a level tie on an upward vertical strand bows right");
        AssertEqual(false, EbonSilkMath.Flipped(new(0, 0), new(100, 0)), "rightward strand is already oriented");
        AssertEqual(true, EbonSilkMath.Flipped(new(100, 0), new(0, 0)), "leftward strand is swapped");

        static Vector2[] Line(Vector2 p, Vector2 q, float bow)
        {
            var points = new Vector2[EbonSilkMath.Segments(Vector2.Distance(p, q)) + 1];
            EbonSilkMath.Polyline(p, q, bow, points);
            return points;
        }
        var first = Line(new(0, 0), new(400, 200), 0);
        var second = Line(new(0, 200), new(400, 0), 0);
        AssertEqual(true, EbonSilkMath.Knot(first, second, out Vector2 knot) && (knot - new Vector2(200, 100)).Length() < .01f,
            "straight strands knot at the crossing");
        AssertEqual(true, EbonSilkMath.Knot(Line(new(0, 0), new(400, 200), 6), Line(new(0, 200), new(400, 0), 6), out knot)
            && (knot - new Vector2(200, 100)).Length() < 10, "sagging strands still knot near the crossing");
        AssertEqual(false, EbonSilkMath.Knot(Line(new(0, 0), new(400, 0), 0), Line(new(0, 50), new(400, 50), 0), out _), "parallel strands never knot");
        AssertEqual(false, EbonSilkMath.Knot(Line(new(0, 0), new(100, 0), 0), Line(new(300, -50), new(300, 50), 0), out _), "disjoint strands never knot");
    }

    [DomainTest("Severing Silk hit shapes follow the strand width and the snip radius")]
    private static void SeveringSilkHitShapes()
    {
        Vector2 a = new(0, 0), b = new(400, 0);
        float reach = EbonRewardRules.StrandWidth * .5f;
        AssertEqual(true, EbonRewardRules.BoxTouchesSegment(new(100, reach - 1), new(120, 40), a, b, EbonRewardRules.StrandWidth), "inside the strand width");
        AssertEqual(false, EbonRewardRules.BoxTouchesSegment(new(100, reach + 1), new(120, 40), a, b, EbonRewardRules.StrandWidth), "beyond the strand width");
        AssertEqual(false, EbonRewardRules.BoxTouchesSegment(new(430, -5), new(450, 5), a, b, EbonRewardRules.StrandWidth), "past the end of the strand");
        Vector2 center = new(0, 0);
        AssertEqual(true, EbonSilkMath.CircleTouchesBox(center, EbonRewardRules.ScissorsRadius, new(70, -10), new(100, 10)), "snip reaches a near box");
        AssertEqual(false, EbonSilkMath.CircleTouchesBox(center, EbonRewardRules.ScissorsRadius, new(81, -5), new(100, 5)), "snip stops at its radius");
        AssertEqual(true, EbonSilkMath.CircleTouchesBox(center, EbonRewardRules.ScissorsRadius, new(-5, -5), new(5, 5)), "snip hits a box around its centre");
    }

    [DomainTest("Severing Silk nominal budget is thirty-two times base")]
    private static void SeveringSilkBudget()
    {
        int baseDamage = EbonRewardRules.Damage(EbonRewardKind.Rogue);
        AssertEqual(1800, baseDamage, "rogue base damage");
        int sever = EbonRewardRules.Scaled(baseDamage, EbonRewardRules.SeverMultiplier);
        int snip = EbonRewardRules.Scaled(baseDamage, EbonRewardRules.ScissorsMultiplier);
        AssertEqual(7200, sever, "each strand part is x4.0");
        AssertEqual(3600, snip, "the snip is x2.0");
        AssertEqual(32 * baseDamage, 6 * baseDamage + 6 * sever + snip, "six throws, six strands and the snip on one target");
        AssertEqual(EbonRewardRules.StrandLife, 360, "strand life");
        AssertEqual(10, EbonRewardRules.MaxStrands, "strand cap");
    }
}
