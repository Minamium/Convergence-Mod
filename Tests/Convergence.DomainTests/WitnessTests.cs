using System;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

// Last Witness v2 (WitnessRules): budget against the 0.2.x baseline, the score's clocks and continuity, the thrown
// blade's live windows, per-root ledger and swept disc, the judgement's clocks, and the exported art against the design
// anchors and the hit disc.
internal static partial class Program
{
    [DomainTest("Last Witness v2 keeps the 0.2.x budget per score, sustained and over the best cold-press window")]
    private static void WitnessBudget()
    {
        int rogue = RitualArmamentRules.Damage(RitualArmamentKind.Rogue);
        AssertEqual(DollWeaponBudget.Witness.BaseDamage, rogue, "base damage stays");
        AssertDollNear(1, WitnessRules.BladeShares(), 1e-6, "strike + four bites + return share the whole blade");
        AssertDollNear(DollWeaponBudget.Witness.CycleMultiplier, WitnessRules.CycleMultiplier(), 1e-4, "7.08x per score");

        // One score, one target, every hit landing (any contact delay keeps every hit inside the score).
        int cycle = WitnessRules.End + WitnessRules.ReuseGap;
        for (int contact = 1; contact <= WitnessRules.OutboundMaxTicks; contact++)
        {
            double score = 0;
            for (int tick = 0; tick < cycle; tick++) score += WitnessRules.ColdRaw(rogue, contact, tick);
            AssertDollNear(DollWeaponBudget.Witness.CycleRaw, score, .01, $"raw per score, contact after {contact} ticks");
            AssertEqual(true, WitnessRules.Throw + contact + WitnessRules.ReturnTick < WitnessRules.End, "the blade lands inside its score");
        }
        double perScore = DollWeaponBudget.Witness.CycleRaw;
        AssertEqual(true, DollWeaponBudget.Within(perScore, DollWeaponBudget.Witness.CycleRaw), "per score within 3%");
        double sustained = DollWeaponBudget.PerSecond(perScore, cycle);
        AssertEqual(true, DollWeaponBudget.Within(sustained, DollWeaponBudget.Witness.PerSecond),
            $"sustained {sustained:F0}/s with the re-use gap within 3% of {DollWeaponBudget.Witness.PerSecond:F0}/s");
        AssertEqual(true, DollWeaponBudget.Deviation(sustained, DollWeaponBudget.Witness.PerSecond) < 0, "never above the baseline");

        double best = DollWeaponBudget.BestColdWindow(WitnessRules.OutboundMaxTicks,
            (contact, tick) => WitnessRules.ColdRaw(rogue, contact + 1, tick), out _);
        AssertEqual(true, DollWeaponBudget.Within(best, DollWeaponBudget.WitnessColdWindow),
            $"best cold window {best:F0} within 3% of {DollWeaponBudget.WitnessColdWindow:F0}");
        AssertDollNear(DollWeaponBudget.WitnessColdWindow, best, .01, "two scores and the third one's first testimony");

        // Per-hit amounts: shard, strike, bite, return and the stealth judgement.
        int blade = RitualArmamentRules.ScaledDamage(rogue, WitnessRules.BladeMultiplier);
        AssertEqual(2710, RitualArmamentRules.ScaledDamage(rogue, WitnessRules.ShardMultiplier), "testimony shard");
        AssertEqual(52_272, blade, "blade");
        AssertEqual(13_068, RitualArmamentRules.ScaledDamage(blade, WitnessRules.StrikeShare), "strike");
        AssertEqual(6_534, RitualArmamentRules.ScaledDamage(blade, WitnessRules.TurnShare), "Axiom bite");
        AssertEqual(13_068, RitualArmamentRules.ScaledDamage(blade, WitnessRules.ReturnShare), "tear-free return");
        AssertDollNear(DollWeaponBudget.WitnessVerdictRaw, RitualArmamentRules.ScaledDamage(blade, WitnessRules.JudgementShare), 1e-6,
            "stealth judgement");
        AssertDollNear(DollWeaponBudget.WitnessVerdictMultiplier, WitnessRules.JudgementShare * WitnessRules.BladeMultiplier, 1e-4,
            "the judgement adds 3.24x");
    }

    [DomainTest("Last Witness v2 testimonies: six fires before the seal, warnings ten ticks ahead, seats from the centre out")]
    private static void WitnessTestimonies()
    {
        int[] fires = { 16, 45, 74, 103, 132, 161 };
        var seats = new bool[WitnessRules.Testimonies];
        for (int i = 0; i < WitnessRules.Testimonies; i++)
        {
            AssertEqual(fires[i], WitnessRules.TestimonyFire(i), "testimony fire tick");
            AssertEqual(fires[i] - 10, WitnessRules.TestimonyWarn(i), "warning ten ticks ahead");
            AssertEqual(true, WitnessRules.TestimonyFire(i) < WitnessRules.Seal, "every testimony speaks before the seal");
            int seat = WitnessRules.SeatOf(i);
            AssertEqual(false, seats[seat], "seats are a permutation");
            seats[seat] = true;
            AssertEqual(i, WitnessRules.Spoken(fires[i]) - 1, "spoken count rises on the fire tick");
            AssertEqual(i, WitnessRules.Spoken(fires[i] - .01f), "and not before");
            // The shard slides out over the warning and pulls back before it leaves.
            AssertDollNear(0, WitnessRules.Slide(WitnessRules.TestimonyWarn(i), i), 1e-4, "starts in the edge");
            float peak = WitnessRules.Slide(WitnessRules.TestimonyFire(i) - 3, i);
            float shot = WitnessRules.Slide(WitnessRules.TestimonyFire(i), i);
            AssertEqual(true, peak > WitnessRules.SlideOut - .6f && shot < peak - 3.5f, "slides out, then pulls back");
        }
        int[] order = { 2, 3, 1, 4, 0, 5 };
        for (int i = 0; i < order.Length; i++) AssertEqual(order[i], WitnessRules.SeatOf(i), "seats fill 3-4-2-5-1-6");
        AssertEqual(6, WitnessRules.Spoken(173), "six by the seal");
        AssertEqual(0, WitnessRules.Spoken(float.NaN), "non-finite age speaks none");
        // Seats lie on the hanging blade's cutting edge between the guard and the tip.
        float guard = (DollArtAnchors.WitnessBlade.GuardX1 - DollArtAnchors.WitnessBlade.Pivot.X) * 2;
        for (int seat = 0; seat < WitnessRules.Testimonies; seat++)
        {
            float along = WitnessRules.SeatAlong(seat);
            AssertEqual(true, along > guard + 4 && along < WitnessRules.HangTip.X - 4, "seat on the blade, off the guard and the tip");
        }
    }

    [DomainTest("Last Witness v2 pose flows from the hang through the lift and whip into the thrown spin")]
    private static void WitnessPoseContinuity()
    {
        WitnessPose previous = WitnessRules.Pose(0);
        AssertDollNear(WitnessRules.HangBeta, previous.Beta, 1e-6, "hangs 21 degrees above the aim");
        AssertDollNear(WitnessRules.HangRadius, previous.Radius, 1e-6, "94 px from the hand");
        for (float age = .25f; age < WitnessRules.Throw; age += .25f)
        {
            WitnessPose pose = WitnessRules.Pose(age);
            AssertEqual(true, MathF.Abs(pose.Beta - previous.Beta) <= .13f, $"angle continuous at {age}");
            AssertEqual(true, MathF.Abs(pose.Radius - previous.Radius) <= 2.6f, $"radius continuous at {age}");
            previous = pose;
        }
        WitnessPose release = WitnessRules.Pose(WitnessRules.Throw - 1e-3f);
        AssertDollNear(0, release.Beta, 2e-3, "leaves along the aim");
        AssertDollNear(WitnessRules.ReleaseRadius, release.Radius, 1e-2, "leaves 92 px out");
        float rate = (WitnessRules.Pose(WitnessRules.Throw - .001f).Beta - WitnessRules.Pose(WitnessRules.Throw - .101f).Beta) / .1f;
        AssertDollNear(WitnessRules.CruiseSpin, rate, .01, "the whip leaves at the thrown spin (no brake)");
        // The whip accelerates monotonically from the hold.
        float last = WitnessRules.Pose(WitnessRules.WhipStart).Beta, lastStep = 0;
        for (float age = WitnessRules.WhipStart + .5f; age < WitnessRules.Throw; age += .5f)
        {
            float beta = WitnessRules.Pose(age).Beta, step = beta - last;
            AssertEqual(true, step >= lastStep - 1e-5f, "the whip only accelerates");
            last = beta; lastStep = step;
        }
        AssertDollNear(WitnessRules.LiftBeta, WitnessRules.Pose(WitnessRules.LiftEnd).Beta, 1e-5, "lifted over the shoulder");
        // The arm follows through after the throw and is back on the hang when the score ends.
        AssertDollNear(0, WitnessRules.ArmBeta(WitnessRules.Throw), 1e-5, "arm continuous at the throw");
        AssertEqual(true, WitnessRules.ArmBeta(228) > .4f, "arm follows through past the aim");
        AssertDollNear(WitnessRules.HangBeta, WitnessRules.ArmBeta(WitnessRules.End), 1e-5, "arm back on the hang for the next score");
        for (float age = WitnessRules.Throw; age < WitnessRules.End; age += .25f)
            AssertEqual(true, MathF.Abs(WitnessRules.ArmBeta(age + .25f) - WitnessRules.ArmBeta(age)) < .1f, "arm continuous");
        // Each testimony kicks the blade back a little and lets it settle.
        AssertEqual(true, WitnessRules.Pose(WitnessRules.TestimonyFire(2) + 1.5f).Radius < WitnessRules.HangRadius - 4, "kick");
        AssertDollNear(WitnessRules.HangRadius, WitnessRules.Pose(WitnessRules.TestimonyFire(2) + 14).Radius, .05, "settled");
        // Facing mirrors the angle and the edge.
        Vector2 root = new(100, 200);
        Vector2 right = WitnessRules.BalancePoint(root, 0, 1, WitnessRules.Pose(0));
        Vector2 left = WitnessRules.BalancePoint(root, MathF.PI, -1, WitnessRules.Pose(0));
        AssertDollNear(right.Y, left.Y, 1e-3, "mirrored height");
        AssertDollNear(right.X - root.X, root.X - left.X, 1e-3, "mirrored reach");
        AssertEqual(true, right.Y < root.Y - 30, "raised above the line of fire");
        Vector2 edgeRight = WitnessRules.BladeToWorld(Vector2.Zero, 0, 1, new Vector2(0, 10));
        Vector2 edgeLeft = WitnessRules.BladeToWorld(Vector2.Zero, MathF.PI, -1, new Vector2(0, 10));
        AssertDollNear(edgeRight.Y, edgeLeft.Y, 1e-4, "the cutting edge faces down on both sides");
        // The hanging blade's pommel stays clear of the player's body.
        float pommel = (DollArtAnchors.WitnessBlade.Pivot.X - DollArtAnchors.WitnessBlade.Pommel.X) * 2;
        AssertEqual(true, WitnessRules.HangRadius - pommel >= 20, "pommel off the player");
    }

    [DomainTest("Last Witness v2 Axiom turns close two revolutions with rising spin and bite on each half turn")]
    private static void WitnessAxiomTurns()
    {
        AssertDollNear(0, WitnessRules.TurnAngle(0), 1e-6, "starts at 0");
        AssertDollNear(4 * MathF.PI, WitnessRules.TurnAngle(WitnessRules.TurnTicks), 1e-3, "two revolutions");
        AssertDollNear(WitnessRules.CruiseSpin, WitnessRules.TurnSpin(0), 1e-6, "continuous with the outbound spin");
        AssertEqual(true, WitnessRules.TurnPeak <= .75f, "peak spin under the pixel strobe bound");
        float sum = 0, previous = 0;
        for (int t = 0; t < WitnessRules.TurnTicks; t++)
        {
            float step = WitnessRules.SpinStep(WitnessPhase.Turn, t);
            AssertEqual(true, step >= previous - 1e-6f, "spin only rises through the turns");
            sum += step; previous = step;
        }
        AssertDollNear(4 * MathF.PI, sum, 1e-3, "tick steps close the two revolutions exactly");
        int bites = 0;
        for (int t = 0; t <= WitnessRules.ReturnTick; t++)
        {
            int bite = WitnessRules.TurnWindow(t);
            if (bite < 0) continue;
            AssertEqual(bites, bite, "bites in order");
            AssertEqual(t, WitnessRules.TurnBiteTick(bite), "bite tick table");
            float angle = WitnessRules.TurnAngle(t), half = MathF.Round(angle / MathF.PI);
            AssertEqual(true, MathF.Abs(angle - half * MathF.PI) <= .35f && half == bite + 1, "each bite on a half turn");
            bites++;
        }
        AssertEqual(WitnessRules.TurnBites, bites, "four bites");
        AssertEqual(true, WitnessRules.ReturnTick > WitnessRules.TurnBiteTick(3), "tears free after the last bite");
        AssertDollNear(WitnessRules.TurnPeak, WitnessRules.ReturnSpin(0), 1e-6, "return starts at the peak");
        AssertDollNear(WitnessRules.CruiseSpin, WitnessRules.ReturnSpin(WitnessRules.ReturnEase), 1e-6, "eases back to cruise");
        AssertDollNear(WitnessRules.StrikeShare, WitnessRules.Share(WitnessPhase.Outbound), 1e-6, "strike share");
        AssertDollNear(WitnessRules.TurnShare, WitnessRules.Share(WitnessPhase.Turn), 1e-6, "bite share");
        AssertDollNear(WitnessRules.ReturnShare, WitnessRules.Share(WitnessPhase.Return), 1e-6, "return share");
    }

    [DomainTest("Last Witness v2 release, outbound cap and the swept spinning disc")]
    private static void WitnessFlightGeometry()
    {
        AssertEqual(true, WitnessRules.ReleaseCancels(173.99f), "release cancels before the seal");
        AssertEqual(false, WitnessRules.ReleaseCancels(WitnessRules.Seal), "the sentence is passed at the seal");
        AssertEqual(true, WitnessRules.ReleaseCancels(float.NaN), "a broken age cancels");
        AssertEqual(WitnessRules.OutboundMinTicks, WitnessRules.OutboundCap(0), "minimum outbound");
        AssertEqual(10, WitnessRules.OutboundCap(340), "cursor distance");
        AssertEqual(WitnessRules.OutboundMaxTicks, WitnessRules.OutboundCap(5000), "918 px cap");
        AssertEqual(WitnessRules.OutboundMaxTicks, WitnessRules.OutboundCap(float.NaN), "non-finite distance");
        AssertEqual(WitnessRules.OutboundMinTicks, WitnessRules.OutboundCap(-50), "negative distance");

        float r = WitnessRules.SpinRadius;
        Vector2 min = new(100, -20), max = new(140, 20);
        AssertEqual(true, WitnessRules.SweptDiscTouchesBox(new(120, 0), new(120, 0), r, min, max), "centre inside");
        AssertEqual(true, WitnessRules.SweptDiscTouchesBox(new(100 - r + .01f, 0), new(100 - r + .01f, 0), r, min, max), "tangent from the left");
        AssertEqual(false, WitnessRules.SweptDiscTouchesBox(new(100 - r - .5f, 0), new(100 - r - .5f, 0), r, min, max), "just outside");
        AssertEqual(true, WitnessRules.SweptDiscTouchesBox(new(120, -300), new(120, 300), r, min, max), "swept through");
        AssertEqual(true, WitnessRules.SweptDiscTouchesBox(new(0, -70), new(260, -70), r, min, max), "grazes over the top");
        AssertEqual(false, WitnessRules.SweptDiscTouchesBox(new(0, -80), new(260, -80), r, min, max), "passes above");
        // Diagonal corner case: the closest approach is to the box corner mid-segment.
        Vector2 corner = max;
        Vector2 a = corner + new Vector2(-100, 100) + Vector2.Normalize(new Vector2(1, 1)) * (r - 1);
        Vector2 b = a + new Vector2(200, -200);
        AssertEqual(true, WitnessRules.SweptDiscTouchesBox(a, b, r, min, max), "corner within the radius mid-sweep");
        Vector2 a2 = corner + new Vector2(-100, 100) + Vector2.Normalize(new Vector2(1, 1)) * (r + 1);
        AssertEqual(false, WitnessRules.SweptDiscTouchesBox(a2, a2 + new Vector2(200, -200), r, min, max), "corner just outside mid-sweep");
        AssertEqual(false, WitnessRules.SweptDiscTouchesBox(new(float.NaN, 0), new(120, 0), r, min, max), "NaN rejected");
        AssertEqual(false, WitnessRules.SweptDiscTouchesBox(new(120, 0), new(120, 0), float.PositiveInfinity, min, max), "infinite radius rejected");
        AssertEqual(false, WitnessRules.SweptDiscTouchesBox(new(120, 0), new(120, 0), r, max, min), "inverted box rejected");
        // A testimony shard updates twice a tick: it leaves at 44 px/tick and seeks at 36.
        AssertEqual(2, WitnessRules.ShardUpdates, "shard updates per tick (extraUpdates 1, as in 0.2.x)");
        AssertDollNear(44, WitnessRules.ShardLaunch * WitnessRules.ShardUpdates, 1e-6, "launch px per tick");
    }

    // The native hit pass of one tick after the blade's AI (Advance, then Settle): every root inside the disc that the
    // ledger still allows is paid the tick's share and booked.
    private static void WitnessLedgerTick(WitnessBladeLedger ledger, int age, int limit, int[] inside, double[] paid)
    {
        ledger.Advance(age, limit);
        ledger.Settle(age);
        if (!ledger.Live(age)) return;
        foreach (int root in inside)
        {
            if (!ledger.CanHit(root)) continue;
            paid[root] += ledger.StruckShare;
            ledger.Book(root, age);
        }
    }

    [DomainTest("Last Witness v2 per-root ledger: one blade per root over strike, four bites and the return, re-armed per window")]
    private static void WitnessBladeLedgerWindows()
    {
        // Live ticks and re-arms: outbound and return every tick, the turns only on the bites; each bite and the
        // tear-free return open a new window, nothing else does.
        for (int t = 0; t <= WitnessRules.ReturnTick; t++)
        {
            bool bite = WitnessRules.TurnWindow(t) >= 0;
            AssertEqual(bite, WitnessBladeLedger.Live(WitnessPhase.Turn, t), $"turn tick {t} live only on a bite");
            AssertEqual(bite, WitnessBladeLedger.Rearms(WitnessPhase.Turn, t), $"turn tick {t} re-arms only on a bite");
            AssertEqual(true, WitnessBladeLedger.Live(WitnessPhase.Outbound, t) && WitnessBladeLedger.Live(WitnessPhase.Return, t), "outbound and return live");
            AssertEqual(false, WitnessBladeLedger.Rearms(WitnessPhase.Outbound, t), "outbound never re-arms");
            AssertEqual(t == 0, WitnessBladeLedger.Rearms(WitnessPhase.Return, t), "the return re-arms once, as it tears free");
        }

        const int limit = WitnessRules.OutboundMaxTicks, flight = 80, contact = 9;
        int[] none = Array.Empty<int>();
        // One stationary target inside the disc from the contact to the end of the flight: exactly one blade (1.0x).
        {
            var ledger = new WitnessBladeLedger();
            var paid = new double[4];
            for (int age = 1; age <= flight; age++) WitnessLedgerTick(ledger, age, limit, age >= contact ? new[] { 0 } : none, paid);
            AssertDollNear(1, paid[0], 1e-6, "strike + four bites + return = the whole blade on a target that never leaves the disc");
            AssertEqual(WitnessPhase.Return, ledger.Phase, "torn free");
            AssertEqual(contact + WitnessRules.ReturnTick, ledger.PhaseStart, "tears free 22 ticks after the contact");
        }
        // Two roots struck on the same contact tick both take the strike share (not a bite), then every bite and the return.
        {
            var ledger = new WitnessBladeLedger();
            var paid = new double[4];
            for (int age = 1; age <= contact; age++) WitnessLedgerTick(ledger, age, limit, age == contact ? new[] { 1, 2 } : none, paid);
            AssertEqual(WitnessPhase.Turn, ledger.Phase, "the first contact starts the turns");
            AssertDollNear(WitnessRules.StrikeShare, paid[1], 1e-6, "first root: strike");
            AssertDollNear(WitnessRules.StrikeShare, paid[2], 1e-6, "second root on the contact tick: strike as well");
            for (int age = contact + 1; age <= flight; age++) WitnessLedgerTick(ledger, age, limit, new[] { 1, 2 }, paid);
            AssertDollNear(1, paid[1], 1e-6, "first root: one blade");
            AssertDollNear(1, paid[2], 1e-6, "second root: one blade");
        }
        // A root that enters the disc between the second and third bite takes the last two bites and the return (two of
        // its segments inside together are paid once); a root met on the way home takes the return share once.
        {
            var ledger = new WitnessBladeLedger();
            var paid = new double[4];
            for (int age = 1; age <= flight; age++)
            {
                int turn = age - contact;
                var inside = new System.Collections.Generic.List<int>();
                if (age >= contact) inside.Add(0);
                if (turn >= 14) { inside.Add(1); inside.Add(1); }
                if (turn >= WitnessRules.ReturnTick + 3 && turn <= WitnessRules.ReturnTick + 6) inside.Add(2);
                WitnessLedgerTick(ledger, age, limit, inside.ToArray(), paid);
            }
            AssertDollNear(1, paid[0], 1e-6, "the struck root");
            AssertDollNear(2 * WitnessRules.TurnShare + WitnessRules.ReturnShare, paid[1], 1e-6, "a late root with two segments: bites 3 and 4 and the return, once each");
            AssertDollNear(WitnessRules.ReturnShare, paid[2], 1e-6, "a root met on the way home: the return share once");
        }
        // Without any contact the blade stops at its cap and turns in the air: the bites still land on whatever drifts in.
        {
            var ledger = new WitnessBladeLedger();
            var paid = new double[4];
            for (int age = 1; age <= flight; age++) WitnessLedgerTick(ledger, age, 10, age > 10 ? new[] { 3 } : none, paid);
            AssertEqual(WitnessPhase.Return, ledger.Phase, "stopped at the cap, turned and tore free");
            AssertEqual(11 + WitnessRules.ReturnTick, ledger.PhaseStart, "the turns start the tick after the cap");
            AssertDollNear(4 * WitnessRules.TurnShare + WitnessRules.ReturnShare, paid[3], 1e-6, "no strike without a contact");
        }
    }

    [DomainTest("Last Witness v2 judgement: forecast auras, stakes land on the lock, edges write, corners close onto the live triangle")]
    private static void WitnessJudgementClocks()
    {
        AssertDollNear(WitnessRules.StakeDrop, WitnessRules.StakeHeight(0), 1e-4, "swords wait 300 px up");
        AssertDollNear(WitnessRules.StakeDrop, WitnessRules.StakeHeight(WitnessRules.StakeFallStart), 1e-4, "until the fall");
        AssertDollNear(0, WitnessRules.StakeHeight(RitualArmamentChoreography.VerdictLock), 1e-4, "staked on the lock");
        float height = WitnessRules.StakeHeight(WitnessRules.StakeFallStart), fall = 0;
        for (float t = WitnessRules.StakeFallStart + .5f; t <= RitualArmamentChoreography.VerdictLock; t += .5f)
        {
            float next = WitnessRules.StakeHeight(t);
            AssertEqual(true, height - next >= fall - 1e-4f, "falls point-first, accelerating");
            fall = height - next; height = next;
        }
        AssertDollNear(0, WitnessRules.SwordReveal(WitnessRules.SwordAppear), 1e-6, "swords appear from 4");
        AssertDollNear(1, WitnessRules.SwordReveal(WitnessRules.StakeFallStart), 1e-6, "fully shown by the fall");
        AssertDollNear(0, WitnessRules.EdgeWrite(WitnessRules.EdgeWriteStart), 1e-6, "edges start at 17");
        AssertDollNear(1, WitnessRules.EdgeWrite(WitnessRules.EdgeWriteStart + WitnessRules.EdgeWriteTicks), 1e-6, "written by 22");
        AssertDollNear(WitnessRules.JudgementCorner, WitnessRules.JudgementRadius(22), 1e-4, "corners at 265 until the closing");
        AssertDollNear(RitualArmamentChoreography.VerdictRadius, WitnessRules.JudgementRadius(RitualArmamentChoreography.VerdictHit), 1e-3,
            "drawn corners meet the 185 px footprint exactly when it goes live");
        AssertEqual(false, RitualArmamentChoreography.VerdictLive(RitualArmamentChoreography.VerdictLock), "the lock is harmless");
        AssertEqual(true, RitualArmamentChoreography.VerdictLive(RitualArmamentChoreography.VerdictHit), "live at 28");
        AssertEqual(false, RitualArmamentChoreography.VerdictLive(RitualArmamentChoreography.VerdictEndHit), "harmless from 31");
        AssertDollNear(1, WitnessRules.JudgementFade(RitualArmamentChoreography.VerdictEndHit), 1e-6, "full until the hit ends");
        AssertEqual(true, WitnessRules.JudgementCool <= 24, "the residue cools within 24 ticks (shared rule)");
        AssertDollNear(0, WitnessRules.JudgementFade(RitualArmamentChoreography.VerdictEndHit + WitnessRules.JudgementCool), 1e-6, "cooled");
        AssertEqual(true, RitualArmamentChoreography.VerdictEndHit + WitnessRules.JudgementCool <= RitualArmamentChoreography.VerdictDuration,
            "cooled before the projectile ends");
        // The black eye: shut until the hit, at most EyeRadius, shut again ten ticks after the hit.
        AssertDollNear(0, WitnessRules.ExecutionEye(RitualArmamentChoreography.VerdictHit - .5f), 1e-6, "no eye before the hit");
        float widest = 0;
        for (float t = RitualArmamentChoreography.VerdictHit; t <= RitualArmamentChoreography.VerdictDuration; t += .25f)
            widest = MathF.Max(widest, WitnessRules.ExecutionEye(t));
        AssertDollNear(1, widest, .02, "opens fully");
        AssertDollNear(1, WitnessRules.ExecutionEye(RitualArmamentChoreography.VerdictHit + WitnessRules.EyeOpen), 1e-6, "open at the end of the hit");
        int shut = RitualArmamentChoreography.VerdictHit + WitnessRules.EyeOpen + WitnessRules.EyeHold + WitnessRules.EyeClose;
        AssertEqual(RitualArmamentChoreography.VerdictHit + 10, shut, "shut ten ticks after the hit");
        AssertDollNear(0, WitnessRules.ExecutionEye(shut), 1e-6, "shut");
        AssertEqual(true, WitnessRules.EyeRadius * 2 <= RitualArmamentChoreography.VerdictRadius * .25f, "a small eye: at most a quarter of the footprint across");
        AssertDollNear(0, WitnessRules.ExecutionEye(float.NaN), 1e-6, "non-finite age");
        AssertDollNear(1, WitnessRules.Withdraw(WitnessRules.WithdrawStart + WitnessRules.WithdrawTicks), 1e-6, "withdrawn by 46");
        AssertEqual(true, WitnessRules.WithdrawStart + WitnessRules.WithdrawTicks <= RitualArmamentChoreography.VerdictDuration,
            "the swords are gone before the projectile ends");
    }

    [DomainTest("Last Witness v2 exported art lands within one dot of its design anchors and the thrown blade fits its hit disc")]
    private static void WitnessArtFit()
    {
        const float dot = DollSpritePlacement.WorldPerTexel;
        static Vector2 Local(Vector2 anchor, Vector2 pivot) => (anchor - pivot) * DollSpritePlacement.WorldPerTexel;
        // One rung, hanging and thrown: the k = 2 blade the presentation draws (WitnessBladeArt).
        AssertEqual(2, WitnessBladeArt.K, "the blade is drawn on the k = 2 rung, hanging and thrown");
        AssertEqual(DollArtAnchors.WitnessBlade.K, WitnessBladeArt.K, "WitnessBladeArt reads the WitnessBlade anchors");
        AssertEqual(true, DollArtAnchors.WitnessBlade.Texture.EndsWith("/" + WitnessBladeArt.Name, StringComparison.Ordinal), "and draws that texture");
        AssertDollNear(WitnessRules.HangTip, WitnessBladeArt.Tip, dot, "drawn tip");
        AssertDollNear(WitnessRules.HangEye, WitnessBladeArt.Eye, dot, "drawn eye");
        AssertDollNear(WitnessRules.HangTip, Local(DollArtAnchors.WitnessBlade.Tip, DollArtAnchors.WitnessBlade.Pivot), dot, "exported tip");
        // The spinning blade against its hit disc: the disc reaches at least 0.8 of the drawn tip and never past it, and
        // the spin arc is drawn on the disc's rim, inside the shared band.
        AssertEqual(true, WitnessBladeArt.ArcInBand(WitnessRules.SpinRadius, WitnessBladeArt.TipReach),
            $"the 56 px disc covers 0.8-1.0 of the drawn tip's {WitnessBladeArt.TipReach:F1} px reach");
        AssertDollNear(WitnessRules.SpinRadius, WitnessBladeArt.ArcRadius, 1e-6, "the spin arc shows the hit disc");
        AssertEqual(true, WitnessBladeArt.TipReach - WitnessRules.SpinRadius <= 4 * dot, "the drawn tip overhangs the disc by at most four dots");
        // The k = 1 rung would draw the tip more than twice the disc out: it fails the band, which is why it is not drawn.
        float large = Local(DollArtAnchors.WitnessBlade_L.Tip, DollArtAnchors.WitnessBlade_L.Pivot).Length();
        AssertEqual(false, WitnessBladeArt.ArcInBand(WitnessRules.SpinRadius, large), $"the k = 1 tip at {large:F0} px does not fit the disc");
        AssertDollNear(WitnessRules.ShardPoint, Local(DollArtAnchors.WitnessShards.Points[0], DollArtAnchors.WitnessShards.Pivot), dot, "shard point");
        AssertDollNear(WitnessRules.SwordGuard, Local(DollArtAnchors.WitnessSword.Guard, DollArtAnchors.WitnessSword.StakePoint), dot, "sword guard");
        AssertDollNear(WitnessRules.SwordLength, DollArtAnchors.WitnessSword.Height * dot, dot, "sword length");
        AssertDollNear(DollArtAnchors.WitnessSword.Width / 2f, DollArtAnchors.WitnessSword.StakePoint.X, .01, "the stake point is the sword's centre line");
        // The seats sit on the hanging blade's lower outline (the cutting edge).
        float edgeRow = 9f;
        AssertDollNear(WitnessRules.SeatEdge, (edgeRow + .5f - DollArtAnchors.WitnessBlade.Pivot.Y) * dot, dot, "seat edge on the outline");
    }
}
