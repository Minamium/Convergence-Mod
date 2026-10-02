using System;
using System.Linq;
using Convergence.Client.Encounters.AzureCathedral;
using Convergence.Common.Raids.Arena;
using Convergence.Content.Encounters.AzureCathedral;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static AzureState AzureAudioState(AzurePhase phase, int stagingAt = -1, bool enraged = false, int wormLife = 1000)
    {
        int phaseAt = phase == AzurePhase.Duet ? -1 : 6000;
        return new AzureState(Guid.Parse("a3f0c5f4-4d1b-4c2e-9d5a-2b0a7e0f1c11"), 9000, 100, 100 + AzureRules.Intro, -1,
            AzureStage.Performance, Array.Empty<AzureMember>(), 8000, 6000, 1000, 1000, phase == AzurePhase.Duet ? 500 : 0,
            wormLife, 3, enraged, phase, phaseAt, stagingAt, stagingAt >= 0 ? (sbyte)1 : (sbyte)0);
    }

    [DomainTest("Azure audio clock plays each cue once even when the visual age regresses")]
    private static void AzureAudioClockPlaysOnce()
    {
        var clock = new AzureCueClock(); var fight = Guid.NewGuid();
        AssertEqual(true, clock.Advance(fight, 90), "first observation starts the Fight");
        AssertEqual(false, clock.Due(1, 7, 100, 8), "a future tick is awaited, not recorded");
        int heard = 0;
        foreach (int age in new[] { 99, 100, 99, 100, 101, 100, 102 })
        {
            AssertEqual(false, clock.Advance(fight, age), "same Fight");
            if (clock.Due(1, 7, 100, 8)) heard++;
        }
        AssertEqual(1, heard, "an age that goes 99->100->99->100 sounds once");
        AssertEqual(102, clock.High, "high-water age never moves backwards");
        AssertEqual(true, clock.Due(1, 8, 100, 8), "another identity at the same tick is another cue");
        AssertEqual(true, clock.Due(2, 7, 100, 8), "another cue kind with the same identity is another cue");
    }

    [DomainTest("Azure audio clock never plays a cue observed past its late window, now or later")]
    private static void AzureAudioClockLateJoin()
    {
        var clock = new AzureCueClock(); var fight = Guid.NewGuid();
        clock.Advance(fight, 5000);
        AssertEqual(false, clock.Due(3, 1, 4000, 20), "a join long after the tick is silent");
        AssertEqual(false, clock.Due(3, 1, 4000, 20), "and is remembered");
        AssertEqual(false, clock.Due(4, 1, 4979, 20), "21 ticks late is too late");
        AssertEqual(true, clock.Due(5, 1, 4981, 20), "19 ticks late is still audible");
        clock.Advance(fight, 5001);
        AssertEqual(false, clock.Due(4, 1, 4979, 20), "a cue that was too late stays silent as the age moves on");
        // Memory is pruned far past every late window; a pruned tick can never come back.
        var busy = new AzureCueClock(); busy.Advance(fight, 0);
        for (int i = 0; i < 400; i++) { busy.Advance(fight, i * 10); busy.Due(1, i, i * 10, 8); }
        busy.Advance(fight, 5000);
        AssertEqual(true, busy.Count < 400, "old entries are pruned");
        AssertEqual(false, busy.Due(1, 3, 30, 30), "a pruned entry does not fire again");
    }

    [DomainTest("Azure audio clock latches state cues once and baselines what is already true")]
    private static void AzureAudioClockLatch()
    {
        var clock = new AzureCueClock(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        clock.Advance(first, 10);
        clock.Baseline(9, 0);
        AssertEqual(false, clock.Latch(9, 0), "a baselined state (join with Liora already down) never sounds");
        AssertEqual(true, clock.Latch(10, 5), "a new state sounds once");
        AssertEqual(false, clock.Latch(10, 5), "and not again, however long the Fight runs");
        for (int age = 11; age < 20000; age += 7) clock.Advance(first, age);
        AssertEqual(false, clock.Latch(10, 5), "latches are never pruned");
        AssertEqual(true, clock.Advance(second, 3), "another Fight starts clean");
        AssertEqual(true, clock.Latch(10, 5), "a new Fight may sound the same state again");
        clock.Clear();
        AssertEqual(true, clock.Advance(second, 3), "Clear forgets the Fight");
    }

    [DomainTest("Azure rush sound and forecast stop during a Duet staging and while devouring or melting")]
    private static void AzureRushIsActiveOnlyWhenTheWormCharges()
    {
        int unlock = 100 + AzureRules.Intro;
        var duet = AzureAudioState(AzurePhase.Duet);
        int chargePhrase = unlock + 0 * AzureRules.PhraseTicks + 10, chorusPhrase = unlock + 2 * AzureRules.PhraseTicks + 10;
        AssertEqual(true, AzureCueRules.RushActive(duet, chargePhrase), "Duet charge phrase");
        AssertEqual(false, AzureCueRules.RushActive(duet, unlock + AzureRules.PhraseTicks + 10), "Duet attack phrase");
        AssertEqual(false, AzureCueRules.RushActive(duet, chorusPhrase), "Duet chorus phrase without enrage");
        AssertEqual(true, AzureCueRules.RushActive(duet, unlock + 3 * AzureRules.PhraseTicks + 10), "Duet second charge phrase");
        AssertEqual(false, AzureCueRules.RushActive(AzureAudioState(AzurePhase.Duet, stagingAt: unlock + 5), chargePhrase + 300),
            "a staging worm that retreated to the floor never dashes");
        AssertEqual(false, AzureCueRules.RushActive(AzureAudioState(AzurePhase.Duet, wormLife: 0), chargePhrase), "dead worm");
        var fury = AzureAudioState(AzurePhase.Fury, stagingAt: unlock + 5, enraged: true);
        int epoch = fury.AttackEpoch;
        AssertEqual(true, AzureCueRules.RushActive(fury, epoch + 10), "Fury charge phrase, although the Duet staging was recorded");
        AssertEqual(true, AzureCueRules.RushActive(fury, epoch + 2 * AzureRules.PhraseTicks + 10), "enraged chorus phrase also dashes");
        AssertEqual(false, AzureCueRules.RushActive(fury, epoch + AzureRules.PhraseTicks + 10), "Fury volley phrase");
        AssertEqual(false, AzureCueRules.RushActive(AzureAudioState(AzurePhase.Devouring), chargePhrase), "Devouring is not Live");
        AssertEqual(false, AzureCueRules.RushActive(AzureAudioState(AzurePhase.Melting), chargePhrase), "Melting is not Live");
        AssertEqual(false, AzureCueRules.RushActive(duet with { Stage = AzureStage.Countdown }, chargePhrase), "not before the Performance");
        // Same serial and side arithmetic as AzureRuntime.Move(): entrance flips every 240 ticks.
        int warn = epoch;
        AssertEqual(0, AzureCueRules.RushSerial(warn, epoch), "first dash");
        AssertEqual(-1, AzureCueRules.RushSide(0), "first entrance is the left");
        AssertEqual(1, AzureCueRules.RushSide(AzureCueRules.RushSerial(epoch + AzureRules.ChargeTicks + 7, epoch)), "second entrance is the right");
        AssertEqual(epoch + AzureRules.ChargeTicks, AzureCueRules.RushWarnTick(epoch, 1), "warning begins every 240 ticks");
        AssertEqual(88, AzureCueRules.RushFollow, "the pass follows the head from contact to the end of the dash");
    }

    [DomainTest("Azure sound moments lead their picture events and keep the authored lengths")]
    private static void AzureSoundMomentsLeadPictures()
    {
        AssertEqual(AzureRules.IceBreak - 24, AzureCueRules.PrisonBreak, "creak (0.4s) before the ice breaks");
        AssertEqual(AzureRules.SwordLight - 76, AzureCueRules.SwordLight, "1.27s bloom before the pillar of light");
        AssertEqual(540 - 60, AzureCueRules.RiftOpen, "1.0s tear ends as the rift opens");
        AssertEqual(610, AzureCueRules.WormArrival, "arrival sounds on its tick");
        AssertEqual(AzureRules.DevourContact - 118, AzureCueRules.DevourRush, "2.0s rush ends on the bite contact");
        AssertEqual(AzureRules.DevourContact - AzureRules.DevourRush, 216, "contact is 216 ticks after the visual rush start");
        AssertEqual(true, AzureCueRules.DevourRush > AzureRules.DevourRush, "the rush sound is later than the picture's rush start");
        AssertEqual(AzureRules.ChargeEnd - AzureRules.ChargeWarning, AzureCueRules.RushFollow, "rush follow window");
        // Chorus countdown: three notes 180/120/60 ticks before the verdict.
        int fire = 5000;
        AssertEqual(fire - 180, AzureCueRules.ChorusTickAt(fire, 0), "first note");
        AssertEqual(fire - 120, AzureCueRules.ChorusTickAt(fire, 1), "second note");
        AssertEqual(fire - 60, AzureCueRules.ChorusTickAt(fire, 2), "third note");
        AssertEqual(3, AzureCueRules.ChorusTickCount, "three notes");
    }

    [DomainTest("Azure lattice sound distinguishes lines from single cuts and ends after the last line")]
    private static void AzureLatticeSoundIndex()
    {
        var field = RaidFieldGeometry.FromGround(8000, 6000);
        var fight = Guid.Parse("ef3f1f8e-15c6-4934-9333-1283314fcb6e");
        for (int pattern = 0; pattern < 2; pattern++)
        {
            var plans = AzureLattice.Create(fight, 3, field, 1000, pattern);
            AssertEqual(true, plans.Length is >= 12 and <= 24, "a lattice has 12-24 lines");
            for (int i = 0; i < plans.Length; i++)
            {
                AssertEqual(true, AzureCueRules.IsLatticeLine(plans[i].Born, plans[i].Fire), "every lattice line is recognised");
                AssertEqual(i, AzureCueRules.LatticeIndex(plans[i].Born, plans[i].Fire), "the line's index from its plan");
            }
            int last = plans.Max(p => p.Fire);
            AssertEqual(1000 + AzureLattice.Warning + (plans.Length - 1) * AzureLattice.Stagger, last, "last line fires last");
            AssertEqual(6, AzureCueRules.LatticeEndDelay, "closing note six ticks after the last line");
        }
        AssertEqual(false, AzureCueRules.IsLatticeLine(2000, 2000 + AzureRules.CutWarning), "a single cut (60 tick warning) is not a lattice line");
        AssertEqual(-1, AzureCueRules.LatticeIndex(2000, 2000 + AzureRules.CutWarning), "single cut has no line index");
        // A lattice of 24 lines is a 3-tick drumbeat for 69 ticks: each line is its own clock identity.
        var clock = new AzureCueClock(); var id = Guid.NewGuid(); clock.Advance(id, 0);
        int heard = 0;
        for (int age = 1000; age < 1000 + 66 + 24 * 3 + 10; age++)
        {
            clock.Advance(id, age);
            for (int line = 0; line < 24; line++) if (clock.Due(25, 1000 + 66 + line * 3, 1000 + 66 + line * 3, 6)) heard++;
        }
        AssertEqual(24, heard, "every line sounds exactly once");
    }
}
