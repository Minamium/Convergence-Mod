using System;
using Convergence.Content.Encounters.EbonManor.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Ebon Last Waltz reserves ten slots, one companion and the budgeted damage")]
    private static void EbonLastWaltzBudget()
    {
        for (int slots = 0; slots < 10; slots++) AssertEqual(false, EbonLastWaltzRules.CanSummon(slots, 0), "no free slot grant");
        AssertEqual(true, EbonLastWaltzRules.CanSummon(10, 0), "exact capacity");
        AssertEqual(false, EbonLastWaltzRules.CanSummon(30, 1), "one companion per owner");
        AssertEqual(10000, EbonLastWaltzRules.ItemDamage, "ten times the summon seed");
        AssertEqual(EbonRewardRules.Damage(EbonRewardKind.Summon) * EbonRewardRules.CompanionDamageScale, EbonLastWaltzRules.ItemDamage, "rules budget");
        AssertEqual(10, EbonLastWaltzRules.Mana, "mana");
        AssertEqual(true, EbonLastWaltzRules.DismissForMissingBuff(true, false), "owner dismissal");
        AssertEqual(false, EbonLastWaltzRules.DismissForMissingBuff(true, true), "buff present");
        AssertEqual(false, EbonLastWaltzRules.DismissForMissingBuff(false, false), "observers never dismiss");
    }

    [DomainTest("Ebon Last Waltz score flings on beats one to six and opens the parasol on beat seven")]
    private static void EbonLastWaltzScore()
    {
        AssertEqual(230, EbonLastWaltzRules.Cycle, "two bars");
        AssertEqual(6, EbonLastWaltzRules.Flings, "six flings");
        int[] expected = { 1, 29, 58, 86, 115, 144, 173, 202 };
        for (int beat = 0; beat < expected.Length; beat++)
            AssertEqual(expected[beat], EbonLastWaltzRules.BeatTick(beat), "beat tick " + beat);
        for (int beat = 1; beat < 8; beat++)
            AssertEqual(true, MathF.Abs(EbonLastWaltzRules.BeatTick(beat) - beat * EbonRewardRules.BeatTicks) <= .5f, "rounded, not accumulated");
        AssertEqual(173, EbonLastWaltzRules.WaltzStart, "waltz opens on beat six");
        AssertEqual(56, EbonLastWaltzRules.SpokeLife, "spokes fold before the loop restarts");
        AssertEqual(true, EbonLastWaltzRules.WaltzStart + EbonLastWaltzRules.SpokeLife < EbonLastWaltzRules.Cycle, "waltz fits the bar");

        int tick = 0, flings = 0, waltzes = 0, gathers = 0, length = 0;
        for (int step = 0; step < EbonLastWaltzRules.Cycle * 3; step++)
        {
            tick = EbonLastWaltzRules.Advance(tick, true);
            AssertEqual(true, tick >= 1 && tick <= EbonLastWaltzRules.Cycle && EbonLastWaltzRules.ValidTick(tick), "active tick bounds");
            if (EbonLastWaltzRules.FlingAt(tick, out int piece))
            {
                flings++;
                AssertEqual(true, piece >= 0 && piece < 6 && EbonLastWaltzRules.YankTick(piece) < EbonLastWaltzRules.WaltzStart, "yank before the waltz");
            }
            if (EbonLastWaltzRules.WaltzAt(tick)) waltzes++;
            if (EbonLastWaltzRules.GatherAt(tick)) gathers++;
            length++;
        }
        AssertEqual(18, flings, "six flings per bar pair");
        AssertEqual(3, waltzes, "one waltz per score");
        AssertEqual(3, gathers, "one gather per waltz");
        AssertEqual(690, length, "loops without a pause");
        AssertEqual(0, EbonLastWaltzRules.Advance(77, false), "no target is idle");
        AssertEqual(1, EbonLastWaltzRules.Advance(EbonLastWaltzRules.Cycle, true), "wraps to the first beat");
        AssertEqual(false, EbonLastWaltzRules.ValidTick(-1), "negative");
        AssertEqual(false, EbonLastWaltzRules.ValidTick(EbonLastWaltzRules.Cycle + 1), "too far");
        AssertEqual(false, EbonLastWaltzRules.ValidTick(float.NaN), "not finite");
    }

    [DomainTest("Ebon Last Waltz poses stay inside the sheet and follow cast, pull and parasol")]
    private static void EbonLastWaltzPose()
    {
        for (int idle = 0; idle < 2520; idle += 7)
            foreach (bool moving in new[] { false, true })
                for (int tick = 0; tick <= EbonLastWaltzRules.Cycle; tick++)
                {
                    int frame = EbonLastWaltzRules.Frame(tick, moving, idle);
                    AssertEqual(true, frame >= 0 && frame < EbonLastWaltzRules.FrameCount, "sheet cell bounds");
                }
        AssertEqual(2, EbonLastWaltzRules.Frame(0, false, 0), "idle floats");
        AssertEqual(3, EbonLastWaltzRules.Frame(0, true, 0), "glide while following");
        AssertEqual(1, EbonLastWaltzRules.Frame(0, false, 310), "a sway once in a while");
        for (int piece = 0; piece < EbonLastWaltzRules.Flings; piece++)
        {
            AssertEqual(4, EbonLastWaltzRules.Frame(EbonLastWaltzRules.BeatTick(piece), false, 0), "lifts on the beat");
            AssertEqual(5, EbonLastWaltzRules.Frame(EbonLastWaltzRules.YankTick(piece), false, 0), "pulls at the yank");
            AssertEqual(0, EbonLastWaltzRules.SinceYank(EbonLastWaltzRules.YankTick(piece)), "recoil clock starts at the yank");
        }
        AssertEqual(-1, EbonLastWaltzRules.SinceYank(EbonLastWaltzRules.YankTick(0) - 1), "no recoil before the first yank");
        AssertEqual(7, EbonLastWaltzRules.Frame(EbonLastWaltzRules.WaltzStart - 1, false, 0), "points before the parasol opens");
        AssertEqual(6, EbonLastWaltzRules.Frame(EbonLastWaltzRules.WaltzStart, false, 0), "parasol opens on the beat");
        AssertEqual(6, EbonLastWaltzRules.Frame(EbonLastWaltzRules.WaltzStart + 40, true, 0), "parasol twirl");
        AssertEqual(2, EbonLastWaltzRules.Frame(EbonLastWaltzRules.Cycle, false, 0), "settles before the next bar");
    }

    [DomainTest("Ebon Last Waltz spokes unfurl, turn exactly once in beat steps and fold")]
    private static void EbonLastWaltzSpokes()
    {
        int life = EbonLastWaltzRules.SpokeLife;
        for (int spoke = 0; spoke < EbonRewardRules.Spokes; spoke++)
        {
            AssertEqual(0f, EbonLastWaltzRules.SpokeLength(spoke, 0), "closed at the start");
            AssertEqual(true, EbonLastWaltzRules.SpokeLength(spoke, life) < .001f, "folded at the end");
            AssertEqual(true, MathF.Abs(EbonLastWaltzRules.SpokeLength(spoke, 30) - EbonLastWaltzRules.SpokeReach) < .001f, "full reach mid waltz");
            float previous = -1;
            for (float age = 0; age <= 18; age += .25f)
            {
                float length = EbonLastWaltzRules.SpokeLength(spoke, age);
                AssertEqual(true, length >= previous - .0001f && length <= EbonLastWaltzRules.SpokeReach + .001f, "unfurls monotonically");
                previous = length;
            }
            float turn = EbonLastWaltzRules.SpokeAngle(spoke, life, 0.3f) - EbonLastWaltzRules.SpokeAngle(spoke, 0, 0.3f);
            AssertEqual(true, MathF.Abs(turn - MathF.Tau) < .0001f, "one full turn around the target");
        }
        // Evenly spaced, and always turning forward with the boss's lilt: fastest on a beat, slowest between.
        for (int spoke = 1; spoke < EbonRewardRules.Spokes; spoke++)
        {
            float gap = EbonLastWaltzRules.SpokeAngle(spoke, 12, 0) - EbonLastWaltzRules.SpokeAngle(spoke - 1, 12, 0);
            AssertEqual(true, MathF.Abs(gap - MathF.Tau / EbonRewardRules.Spokes) < .0001f, "six even spokes");
        }
        float Speed(float age) => EbonLastWaltzRules.SpokeAngle(0, age + .05f, 0) - EbonLastWaltzRules.SpokeAngle(0, age - .05f, 0);
        for (float age = .1f; age < life; age += .1f) AssertEqual(true, Speed(age) > 0, "never turns back");
        AssertEqual(true, Speed(.5f) > Speed(EbonRewardRules.BeatTicks / 2) * 3, "fast on the beat, easing between");
        AssertEqual(true, Speed(EbonRewardRules.BeatTicks) > Speed(EbonRewardRules.BeatTicks * 1.5f) * 3, "fast on the next beat");
        AssertEqual(true, EbonLastWaltzRules.SpokePhase(1234) >= 0 && EbonLastWaltzRules.SpokePhase(1234) < MathF.Tau, "phase in a turn");
    }

    [DomainTest("Ebon Last Waltz spoke ledger strikes a body across a spoke once per beat step")]
    private static void EbonLastWaltzSpokeLedger()
    {
        AssertEqual(29, EbonLastWaltzRules.SpokeRearm(0), "waits for the next beat");
        AssertEqual(36, EbonLastWaltzRules.SpokeRearm(28), "native immunity after a late first-beat hit");
        AssertEqual(58, EbonLastWaltzRules.SpokeRearm(29), "second beat step rearms past the waltz");
        // A body lying across the spoke from any time in the first beat is struck then and again on the next beat: twice.
        for (int firstContact = 0; firstContact < 29; firstContact++)
        {
            int rearm = 0, hits = 0, last = -100;
            for (int age = firstContact; age < EbonLastWaltzRules.SpokeLife; age++)
            {
                if (age < rearm) continue;
                hits++;
                AssertEqual(true, age - last >= EbonRewardRules.SpokeImmunity, "native 8-tick immunity");
                last = age;
                rearm = EbonLastWaltzRules.SpokeRearm(age);
            }
            AssertEqual(2, hits, "once per beat step, twice a waltz");
        }
        // First touched during the second beat: a single contact, never a stream.
        int late = 0, lateRearm = 0;
        for (int age = 30; age < EbonLastWaltzRules.SpokeLife; age++)
        {
            if (age < lateRearm) continue;
            late++;
            lateRearm = EbonLastWaltzRules.SpokeRearm(age);
        }
        AssertEqual(1, late, "one contact in the last beat");
        // Six spokes at x0.6 per contact on one body: at most 7.2x per waltz plus 3.6x of flings.
        AssertEqual(true, MathF.Abs(EbonRewardRules.Spokes * 2 * EbonRewardRules.SpokeMultiplier - 7.2f) < .0001f, "spoke budget");
        AssertEqual(true, MathF.Abs(EbonLastWaltzRules.Flings * EbonRewardRules.FlingMultiplier - 3.6f) < .0001f, "fling budget");
    }

    [DomainTest("Ebon Last Waltz fling accelerates from the yank speed to the cap")]
    private static void EbonLastWaltzFlingSpeed()
    {
        AssertEqual(EbonRewardRules.YankSpeed, EbonLastWaltzRules.FlingSpeed(0), "yank speed");
        AssertEqual(EbonRewardRules.YankSpeed, EbonLastWaltzRules.FlingSpeed(-5), "never below the yank");
        float previous = 0;
        for (int tick = 0; tick < EbonLastWaltzRules.MaxFlight; tick++)
        {
            float speed = EbonLastWaltzRules.FlingSpeed(tick);
            AssertEqual(true, speed >= previous && speed <= EbonRewardRules.YankMax, "accelerates and caps");
            previous = speed;
        }
        AssertEqual(EbonRewardRules.YankMax, EbonLastWaltzRules.FlingSpeed(EbonLastWaltzRules.MaxFlight), "reaches the cap");
        AssertEqual(true, EbonLastWaltzRules.Lift + EbonLastWaltzRules.PullHold < (int)EbonRewardRules.BeatTicks, "recovers before the next beat");
    }
}
