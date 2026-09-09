using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Grand magic builds over seconds, merges, charges and sustains without restarting")]
    private static void GrandMagicScore()
    {
        AssertEqual(1, RitualGrandScore.Count(0, true), "one initial circle");
        int previousGap = int.MaxValue;
        for (int i = 1; i < 7; i++)
        {
            int birth = RitualGrandScore.SigilBirth(i), gap = birth - RitualGrandScore.SigilBirth(i - 1);
            AssertEqual(true, gap < previousGap && gap >= 20, "irregular accelerating macro arrivals, not two-tick flashes");
            AssertEqual(i, RitualGrandScore.Count(birth - .001f, true), "not born early");
            AssertEqual(i + 1, RitualGrandScore.Count(birth, true), "persistent added circle");
            AssertEqual(0f, RitualGrandScore.Arrival(birth, birth), "continuous birth");
            AssertEqual(1f, RitualGrandScore.Arrival(birth + 12, birth), "fast materialization brakes");
            previousGap = gap;
        }
        AssertEqual(48, RitualGrandScore.MagicFire - RitualGrandScore.MagicCharge, "0.8 second charge");
        AssertEqual(0f, RitualGrandScore.Merge(RitualGrandScore.MagicMerge), "merge starts continuously");
        AssertEqual(1f, RitualGrandScore.Merge(RitualGrandScore.MagicCharge), "circles meet before charge");
        AssertEqual(0f, RitualGrandScore.BeamOpening(RitualGrandScore.MagicFire, RitualGrandScore.MagicFire), "no beam before release");
        AssertEqual(1f, RitualGrandScore.BeamOpening(36000, RitualGrandScore.MagicFire), "ten minutes of hold never loops back to charge");
        AssertEqual(true, RitualGrandScore.MagicBoltAt(15, 0), "first circle attacks during construction");
        for (int tick = RitualGrandScore.MagicMerge; tick < 500; tick++)
            for (int i = 0; i < 7; i++) AssertEqual(false, RitualGrandScore.MagicBoltAt(tick, i), "no hidden extra bolts during beam");
    }
    [DomainTest("Grand channel resource cadence has no free sustained damage intervals")]
    private static void GrandResourceScore()
    {
        AssertEqual(false, RitualGrandScore.MagicPays(0), "item use already pays initial mana");
        int payments = 0;
        for (int tick = RitualGrandScore.MagicFire; tick < RitualGrandScore.MagicFire + 480; tick++)
            if (RitualGrandScore.MagicPays(tick)) payments++;
        AssertEqual(60, payments, "one payment every eight real ticks, independent of use speed");
        AssertEqual(true, RitualGrandScore.MagicPays(RitualGrandScore.MagicFire), "pay on the release beat");
        for (int t = RitualGrandScore.BatteryLock; t < RitualGrandScore.BatteryFire; t++)
            AssertEqual(-1, RitualGrandScore.BatteryLane(t), "no ammo spent during mechanical lock/charge");
        int bullets = 0;
        for (int t = RitualGrandScore.BatteryFire; t < RitualGrandScore.BatteryFire + 600; t++)
        {
            int lane = RitualGrandScore.BatteryLane(t);
            AssertEqual(true, lane is >= -1 and < 5, "bounded battery lane");
            if (lane >= 0) bullets++;
        }
        AssertEqual(200, bullets, "one paid projectile every three real ticks, not five full budgets");
    }
    [DomainTest("Grand choir has a long assembly and three-second real chorus with a rest seam")]
    private static void GrandChoirScore()
    {
        AssertEqual(180, RitualGrandScore.ChoirReleaseEnd - RitualGrandScore.ChoirFire, "three second sustain");
        AssertEqual(false, RitualGrandScore.ChoirLive(RitualGrandScore.ChoirFire - .001f), "charge harmless");
        AssertEqual(true, RitualGrandScore.ChoirLive(RitualGrandScore.ChoirFire), "chorus begins");
        AssertEqual(false, RitualGrandScore.ChoirLive(RitualGrandScore.ChoirReleaseEnd), "chorus ends");
        AssertEqual(0f, RitualGrandScore.ChoirAssembly(0), "unassembled start");
        AssertEqual(0f, RitualGrandScore.ChoirAssembly(RitualGrandScore.ChoirCycle), "rest before next concert");
        for (int i = 0; i < 40; i++)
            for (int t = RitualGrandScore.ChoirAssemble; t < RitualGrandScore.ChoirCycle; t++)
                AssertEqual(false, RitualGrandScore.ChoirNoteAt(t, i), "individual notes don't multiply combined chorus");
    }
    [DomainTest("Grand witness stacks bounded relics then folds before a single release")]
    private static void GrandWitnessScore()
    {
        AssertEqual(1, RitualGrandScore.WitnessCount(0), "first witness");
        AssertEqual(6, RitualGrandScore.WitnessCount(170), "six witnesses");
        AssertEqual(0f, RitualGrandScore.WitnessFold(RitualGrandScore.WitnessMerge), "continuous collapse start");
        AssertEqual(1f, RitualGrandScore.WitnessFold(194), "fold complete before launch");
        AssertEqual(true, RitualGrandScore.WitnessFire > 180, "multi-second preparation, not another short throw");
        AssertEqual(1f, RitualGrandScore.WitnessStrike(RitualGrandScore.WitnessFire + 5), "fast final acceleration");
    }
}
