using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Convergence.Content.Encounters.EbonManor.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Moonloom Harp string state round trips every rank and rejects malformed values")]
    private static void EbonLoomHarpStringState()
    {
        for (int rank = 0; rank < EbonRewardRules.MaxStrings; rank++)
        foreach (bool last in new[] { false, true })
        {
            float state = EbonLoomHarpRules.Schedule(rank, last);
            AssertTrue(EbonLoomHarpRules.TryDecode(state, out int r, out bool l, out int countdown), "scheduled state decodes");
            AssertEqual(rank, r, "rank survives the sync");
            AssertEqual(last, l, "last flag survives the sync");
            AssertEqual(EbonRewardRules.PluckTick(rank) + 1, countdown, "countdown is the sixteenth step plus the replication tick");
            AssertTrue(EbonLoomHarpRules.Valid(state) && !EbonLoomHarpRules.Live(state), "scheduled is valid and harmless");
        }

        AssertTrue(EbonLoomHarpRules.Valid(0), "idle is valid");
        AssertTrue(!EbonLoomHarpRules.TryDecode(0, out _, out _, out _), "idle is not a schedule");
        AssertTrue(!EbonLoomHarpRules.TryDecode(-1, out _, out _, out _), "plucked is not a schedule");
        AssertTrue(EbonLoomHarpRules.Valid(-1) && EbonLoomHarpRules.Live(-1), "age 1 is live");
        AssertTrue(EbonLoomHarpRules.Valid(-EbonRewardRules.PluckLive) && EbonLoomHarpRules.Live(-EbonRewardRules.PluckLive), "last live age");
        AssertTrue(!EbonLoomHarpRules.Valid(-EbonRewardRules.PluckLive - 1), "past the live window is malformed");
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, .5f, -.5f, 9000f, 1 + (EbonRewardRules.MaxStrings << 6) })
            AssertTrue(!EbonLoomHarpRules.Valid(bad), "malformed state " + bad + " is rejected");
    }

    [DomainTest("Moonloom Harp glissando plucks oldest first one sixteenth apart and spends each string after its live window")]
    private static void EbonLoomHarpGlissando()
    {
        int count = EbonRewardRules.MaxStrings;
        var pluckedAt = new int[count];
        for (int i = 0; i < count; i++)
        {
            // The click lands in tick 0; the string's own update runs later in that same tick.
            float state = EbonLoomHarpRules.Schedule(i, i == count - 1);
            int plucked = -1, spent = -1, live = 0;
            for (int tick = 0; tick < 200 && spent < 0; tick++)
            {
                var step = EbonLoomHarpRules.Advance(state);
                if (step.Spent) { spent = tick; break; }
                AssertTrue(!(step.Plucked && plucked >= 0), "a string plucks once");
                state = step.State;
                if (step.Plucked) plucked = tick;
                if (EbonLoomHarpRules.Live(state)) live++;
            }
            AssertEqual(EbonRewardRules.PluckTick(i) + 1, plucked, "string " + i + " plucks at its sixteenth");
            AssertEqual(EbonRewardRules.PluckLive, live, "live for exactly PluckLive ticks");
            AssertEqual(plucked + EbonRewardRules.PluckLive, spent, "spent right after the live window");
            pluckedAt[i] = plucked;
        }
        for (int i = 1; i < count; i++)
        {
            int gap = pluckedAt[i] - pluckedAt[i - 1];
            AssertTrue(gap == 7 || gap == 8, "a sixteenth is 7.2 ticks, rounded without drift");
        }
        AssertEqual(EbonRewardRules.PluckTick(count - 1) + 1, pluckedAt[count - 1], "the whole run takes about 0.85 s");
        var idle = EbonLoomHarpRules.Advance(0);
        AssertTrue(idle.State == 0 && !idle.Plucked && !idle.Spent, "idle stays idle");
        AssertTrue(EbonLoomHarpRules.Advance(float.NaN).State == 0, "a corrupt state is flushed to idle, never plucked");
    }

    [DomainTest("Moonloom Harp schedule published from the string's own update still carries rank and last to a peer")]
    private static void EbonLoomHarpPublishedSchedule()
    {
        for (int rank = 0; rank < EbonRewardRules.MaxStrings; rank++)
        foreach (bool last in new[] { false, true })
        {
            // The owner can only publish from the string's AI (native Update clears netUpdate first), so what peers
            // receive is the schedule after the click tick's own update has advanced it once.
            var published = EbonLoomHarpRules.Advance(EbonLoomHarpRules.Schedule(rank, last));
            AssertTrue(!published.Plucked && !published.Spent, "the click tick never plucks");
            AssertTrue(EbonLoomHarpRules.TryDecode(published.State, out int r, out bool l, out int countdown), "the published state decodes");
            AssertEqual(rank, r, "rank survives the publish");
            AssertEqual(last, l, "last flag survives the publish");
            AssertEqual(EbonRewardRules.PluckTick(rank), countdown, "the replication tick is already spent");
            AssertTrue(EbonLoomHarpRules.Valid(published.State) && !EbonLoomHarpRules.Live(published.State), "published is valid and harmless");

            // A peer runs the same countdown from receipt; the first string (countdown 0) plucks in its very first update.
            float state = published.State;
            int plucked = -1;
            for (int tick = 0; tick < 200 && plucked < 0; tick++)
            {
                var step = EbonLoomHarpRules.Advance(state);
                state = step.State;
                if (step.Plucked) plucked = tick;
            }
            AssertEqual(countdown, plucked, "a peer plucks after the countdown it received");
        }
    }

    [DomainTest("Moonloom Harp keeps at most eight strings and retires the oldest first")]
    private static void EbonLoomHarpStringLimit()
    {
        AssertEqual("4,1,3,0,2", string.Join(",", EbonLoomHarpRules.OldestFirst(new[] { 300, 120, 450, 120, 5 })), "oldest first, ties by index");
        AssertEqual(0, EbonLoomHarpRules.OldestFirst(Array.Empty<int>()).Length, "no strings, no order");
        for (int idle = 0; idle < EbonRewardRules.MaxStrings; idle++)
            AssertEqual(0, EbonLoomHarpRules.Evictions(idle), "room for one more below the limit");
        AssertEqual(1, EbonLoomHarpRules.Evictions(EbonRewardRules.MaxStrings), "the ninth replaces one");
        AssertEqual(2, EbonLoomHarpRules.Evictions(EbonRewardRules.MaxStrings + 1), "recovers from an overshoot");

        // Twenty arrows in a row: only the newest eight survive and the order of age is preserved.
        var life = new List<int>();
        for (int shot = 0; shot < 20; shot++)
        {
            for (int i = 0; i < life.Count; i++) life[i]--;
            int[] order = EbonLoomHarpRules.OldestFirst(life.ToArray());
            foreach (int index in order.Take(EbonLoomHarpRules.Evictions(life.Count)).OrderByDescending(i => i)) life.RemoveAt(index);
            life.Add(EbonRewardRules.StringLife);
            AssertTrue(life.Count <= EbonRewardRules.MaxStrings, "never more than eight strings");
        }
        AssertEqual(EbonRewardRules.MaxStrings, life.Count, "eight stand after twenty arrows");
        AssertEqual(EbonRewardRules.StringLife - 7, life.Min(), "the survivors are the eight newest arrows");
    }

    [DomainTest("Moonloom Harp fade, ring and draw curves are bounded, monotone and continuous")]
    private static void EbonLoomHarpCurves()
    {
        AssertTrue(EbonLoomHarpRules.Fade(EbonRewardRules.StringLife) == 1 && EbonLoomHarpRules.Fade(EbonRewardRules.StringFade) == 1, "full until the last 60 ticks");
        AssertTrue(EbonLoomHarpRules.Fade(0) == 0 && EbonLoomHarpRules.Fade(-5) == 0, "gone at the end");
        float previous = 0;
        for (int left = 0; left <= EbonRewardRules.StringLife; left++)
        {
            float fade = EbonLoomHarpRules.Fade(left);
            AssertTrue(fade >= previous && fade is >= 0 and <= 1, "fade only brightens with life left");
            previous = fade;
        }

        AssertTrue(EbonLoomHarpRules.Pull(0) == 0 && EbonLoomHarpRules.Pull(EbonLoomHarpRules.RingEnd) == 0, "the string rings before it is drawn");
        AssertTrue(MathF.Abs(EbonLoomHarpRules.Pull(1) - 1) < 1e-5f, "full draw lands on the next shot");
        previous = 0;
        for (int i = 0; i <= 200; i++)
        {
            float pull = EbonLoomHarpRules.Pull(i / 200f);
            AssertTrue(pull >= previous - 1e-6f && pull - previous < .05f, "the draw is smooth and never goes back");
            previous = pull;
        }
        AssertTrue(MathF.Abs(EbonLoomHarpRules.Ring(0, 0) - EbonLoomHarpRules.BowRingPeak) < 1e-5f, "the release rings at its peak");
        AssertTrue(EbonLoomHarpRules.Ring(10, 1) == 0 && EbonLoomHarpRules.Ring(-1, 0) == 0, "a drawn string does not ring and nothing rings before the shot");
        AssertTrue(EbonLoomHarpRules.Ring(6, 0) < EbonLoomHarpRules.Ring(3, 0), "the ring decays");
        AssertTrue(EbonLoomHarpRules.PluckAmplitude(0) == EbonLoomHarpRules.PluckPeak && EbonLoomHarpRules.PluckAmplitude(EbonLoomHarpRules.RingOut) < .1f, "a plucked string rings out inside its window");
        AssertTrue(EbonLoomHarpRules.Quiver(0, 4) == 4 && EbonLoomHarpRules.Quiver(30, 4) < .1f && EbonLoomHarpRules.Quiver(-1, 4) == 0, "a crossing quiver settles");
    }

    [DomainTest("Moonloom Harp pluck band cuts only what the string touches and the budget is thirty-two times base")]
    private static void EbonLoomHarpBandAndBudget()
    {
        Vector2 a = new(0, 0), b = new(400, 0);
        bool Touches(float x0, float y0, float x1, float y1)
            => EbonRewardRules.BoxTouchesSegment(new Vector2(x0, y0), new Vector2(x1, y1), a, b, EbonRewardRules.PluckWidth);
        AssertTrue(Touches(100, -20, 140, 20), "a box the string crosses is cut");
        AssertTrue(Touches(100, 9, 140, 40), "inside the half width (10 px)");
        AssertTrue(!Touches(100, 11, 140, 40), "outside the half width");
        AssertTrue(!Touches(100, -60, 140, -12), "the band is symmetric");
        AssertTrue(!Touches(440, -20, 480, 20), "beyond the end of the string");
        AssertTrue(Touches(-8, -10, 4, 10), "the ends cut too");

        int arrow = EbonRewardRules.Damage(EbonRewardKind.Ranged);
        int pluck = EbonRewardRules.Scaled(arrow, EbonRewardRules.PluckMultiplier);
        AssertEqual(5700, pluck, "a pluck is three times the arrow that laid the string");
        AssertEqual(32 * arrow, EbonRewardRules.MaxStrings * arrow + EbonRewardRules.MaxStrings * pluck, "eight arrows and a glissando into a target they all hit");
        AssertTrue(EbonLoomHarpRules.GlissandoUse > 0 && EbonLoomHarpRules.MinStringLength > 0, "constants sane");
    }
}
