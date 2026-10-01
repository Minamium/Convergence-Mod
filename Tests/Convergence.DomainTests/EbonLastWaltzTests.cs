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

    [DomainTest("Ebon Last Waltz poses stay inside the sheets and follow cast, pull, fling and twirl")]
    private static void EbonLastWaltzPose()
    {
        AssertEqual(12, EbonLastWaltzRules.FrameCount, "eight Raid cells and four waltz cells");
        for (int idle = 0; idle < 2520; idle += 7)
            foreach (bool moving in new[] { false, true })
                for (int summoned = 0; summoned < 2 * EbonLastWaltzRules.CurtsyTicks; summoned += 9)
                    for (int tick = 0; tick <= EbonLastWaltzRules.Cycle; tick++)
                    {
                        int frame = EbonLastWaltzRules.Frame(tick, moving, idle, summoned);
                        AssertEqual(true, frame >= 0 && frame < EbonLastWaltzRules.FrameCount, "sheet cell bounds");
                    }
        AssertEqual(EbonLastWaltzRules.FrameFloat, EbonLastWaltzRules.Frame(0, false, 0), "idle floats");
        AssertEqual(EbonLastWaltzRules.FrameGlide, EbonLastWaltzRules.Frame(0, true, 0), "glide while following");
        AssertEqual(1, EbonLastWaltzRules.Frame(0, false, 310), "a sway once in a while");
        for (int piece = 0; piece < EbonLastWaltzRules.Flings; piece++)
        {
            int beat = EbonLastWaltzRules.BeatTick(piece), yank = EbonLastWaltzRules.YankTick(piece);
            AssertEqual(EbonLastWaltzRules.FrameCast, EbonLastWaltzRules.Frame(beat, false, 0), "lifts on the beat");
            AssertEqual(EbonLastWaltzRules.FrameCast, EbonLastWaltzRules.Frame(yank - EbonLastWaltzRules.Windup - 1, false, 0), "holds the piece up");
            AssertEqual(EbonLastWaltzRules.FramePull, EbonLastWaltzRules.Frame(yank - EbonLastWaltzRules.Windup, false, 0), "draws the thread back");
            AssertEqual(EbonLastWaltzRules.FramePull, EbonLastWaltzRules.Frame(yank - 1, false, 0), "still drawn back before the yank");
            AssertEqual(EbonLastWaltzRules.FrameFling, EbonLastWaltzRules.Frame(yank, false, 0), "flings at the yank");
            AssertEqual(EbonLastWaltzRules.FrameFling, EbonLastWaltzRules.Frame(yank + EbonLastWaltzRules.PullHold - 1, false, 0), "fling is held");
            AssertEqual(0, EbonLastWaltzRules.SinceYank(yank), "recoil clock starts at the yank");
        }
        // The last piece's fling runs straight into the pointing cell; the others settle into a float.
        for (int piece = 0; piece < EbonLastWaltzRules.Flings - 1; piece++)
            AssertEqual(EbonLastWaltzRules.FrameFloat, EbonLastWaltzRules.Frame(EbonLastWaltzRules.YankTick(piece) + EbonLastWaltzRules.PullHold, false, 0), "floats after the fling");
        AssertEqual(EbonLastWaltzRules.WaltzStart - EbonLastWaltzRules.Anticipation,
            EbonLastWaltzRules.YankTick(EbonLastWaltzRules.Flings - 1) + EbonLastWaltzRules.PullHold, "last fling ends as the pointing begins");
        AssertEqual(-1, EbonLastWaltzRules.SinceYank(EbonLastWaltzRules.YankTick(0) - 1), "no recoil before the first yank");
        AssertEqual(EbonLastWaltzRules.FrameCommand, EbonLastWaltzRules.Frame(EbonLastWaltzRules.WaltzStart - 1, false, 0), "points before the parasol opens");
        AssertEqual(EbonLastWaltzRules.FrameTwirlA, EbonLastWaltzRules.Frame(EbonLastWaltzRules.WaltzStart, false, 0), "twirl opens on the beat");
        AssertEqual(EbonLastWaltzRules.FrameTwirlA, EbonLastWaltzRules.Frame(EbonLastWaltzRules.WaltzStart + 7, true, 0), "first sixteenth still twirl A");
        AssertEqual(EbonLastWaltzRules.FrameTwirlB, EbonLastWaltzRules.Frame(EbonLastWaltzRules.WaltzStart + 8, true, 0), "twirl B on the second sixteenth");
        AssertEqual(EbonLastWaltzRules.FrameTwirlA, EbonLastWaltzRules.Frame(EbonLastWaltzRules.WaltzStart + 15, true, 0), "and back to twirl A");
        int twirlA = 0, twirlB = 0;
        for (int tick = EbonLastWaltzRules.WaltzStart; tick < EbonLastWaltzRules.Cycle - EbonLastWaltzRules.Settle; tick++)
        {
            int frame = EbonLastWaltzRules.Frame(tick, false, 0);
            AssertEqual(true, frame == EbonLastWaltzRules.FrameTwirlA || frame == EbonLastWaltzRules.FrameTwirlB, "only the two twirl poses in the waltz");
            if (frame == EbonLastWaltzRules.FrameTwirlA) twirlA++; else twirlB++;
        }
        AssertEqual(true, twirlA > 0 && twirlB > 0 && Math.Abs(twirlA - twirlB) <= 8, "both twirl poses trade turns");
        AssertEqual(EbonLastWaltzRules.FrameFloat, EbonLastWaltzRules.Frame(EbonLastWaltzRules.Cycle - EbonLastWaltzRules.Settle, false, 0), "settles before the next bar");
        AssertEqual(EbonLastWaltzRules.FrameFloat, EbonLastWaltzRules.Frame(EbonLastWaltzRules.Cycle, false, 0), "settled at the end of the bar");
    }

    [DomainTest("Ebon Last Waltz curtsies when summoned, then goes about her business")]
    private static void EbonLastWaltzCurtsy()
    {
        AssertEqual(EbonLastWaltzRules.FrameCurtsy, EbonLastWaltzRules.Frame(0, false, 0, 0), "curtsy on arrival");
        AssertEqual(EbonLastWaltzRules.FrameCurtsy, EbonLastWaltzRules.Frame(0, true, 0, 10), "the curtsy outranks the glide");
        AssertEqual(EbonLastWaltzRules.FrameCurtsy, EbonLastWaltzRules.Frame(0, false, 310, EbonLastWaltzRules.CurtsyTicks - 1), "the curtsy outranks the sway");
        AssertEqual(EbonLastWaltzRules.FrameFloat, EbonLastWaltzRules.Frame(0, false, 0, EbonLastWaltzRules.CurtsyTicks), "then she floats");
        AssertEqual(EbonLastWaltzRules.FrameGlide, EbonLastWaltzRules.Frame(0, true, 0, EbonLastWaltzRules.CurtsyTicks), "or glides");
        AssertEqual(EbonLastWaltzRules.FrameCast, EbonLastWaltzRules.Frame(EbonLastWaltzRules.BeatTick(0), false, 0, 0), "a target interrupts the curtsy");
        AssertEqual(EbonLastWaltzRules.FrameFloat, EbonLastWaltzRules.Frame(0, false, 0), "no summon clock means no curtsy");
        AssertEqual(true, EbonLastWaltzRules.CurtsyTicks >= 40, "the curtsy outlasts the 40 tick weave-in");
    }

    [DomainTest("Ebon Last Waltz resumes after the first piece when a flickering target comes back")]
    private static void EbonLastWaltzResume()
    {
        int resume = EbonLastWaltzRules.ResumeTick;
        AssertEqual(21, resume, "after the first lift and yank");
        AssertEqual(true, EbonLastWaltzRules.ValidTick(resume), "valid tick");
        AssertEqual(EbonLastWaltzRules.FrameFloat, EbonLastWaltzRules.Frame(resume, false, 0), "no empty throw is shown");
        AssertEqual(false, EbonLastWaltzRules.FlingAt(resume, out _), "no piece on the resume tick");
        for (int tick = 1; tick <= resume; tick++)
            AssertEqual(false, EbonLastWaltzRules.FlingAt(tick, out int skipped) && skipped != 0, "only the first piece is skipped");
        AssertEqual(true, EbonLastWaltzRules.BeatTick(1) > resume, "the second piece still lands on its own beat");
        // From the resume tick one score still flings the other five pieces and opens the parasol once.
        int flings = 0, waltzes = 0, now = resume;
        for (int step = 0; step < EbonLastWaltzRules.Cycle - resume; step++)
        {
            now = EbonLastWaltzRules.Advance(now, true);
            if (EbonLastWaltzRules.FlingAt(now, out _)) flings++;
            if (EbonLastWaltzRules.WaltzAt(now)) waltzes++;
        }
        AssertEqual(EbonLastWaltzRules.Flings - 1, flings, "five pieces after a skipped first");
        AssertEqual(1, waltzes, "the waltz is never skipped");
        AssertEqual(true, EbonLastWaltzRules.RestartCooldown > 0 && EbonLastWaltzRules.Windup < EbonLastWaltzRules.Lift, "timing constants");
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
        // Nominal per 230-tick score on one body: six flings (3.6x) plus at most two contacts on each of six spokes
        // (12 x 0.6 = 7.2x), 10.8x base. REWARDS.md states the two-contact rule this ledger implements.
        AssertEqual(true, MathF.Abs(EbonRewardRules.Spokes * 2 * EbonRewardRules.SpokeMultiplier - 7.2f) < .0001f, "spoke budget");
        AssertEqual(true, MathF.Abs(EbonLastWaltzRules.Flings * EbonRewardRules.FlingMultiplier - 3.6f) < .0001f, "fling budget");
        AssertEqual(true, MathF.Abs(EbonLastWaltzRules.Flings * EbonRewardRules.FlingMultiplier
            + EbonRewardRules.Spokes * 2 * EbonRewardRules.SpokeMultiplier - 10.8f) < .0001f, "nominal per score");
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
