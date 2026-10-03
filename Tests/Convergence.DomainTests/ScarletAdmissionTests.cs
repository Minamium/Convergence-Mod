using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    // CrimsonRuntime's phrase and chorus admission without Terraria: BookPhrase/IssueAt, TryScheduleChorus (Due and
    // Schedule) and SchedulePhrase (Admit), ticking only at the moments the runtime would act. Each record is one
    // admitted phrase or chorus, relative to musicStart.
    private sealed record ScarletAdmitted(int Serial, int IssuedAt, CrimsonRhythmPhrase? Phrase, (int Born, int Fire, int End)? Chorus);
    private static List<ScarletAdmitted> ScarletRunAct(int phase, int unlock, int firstSerial, int cycles)
    {
        var admitted = new List<ScarletAdmitted>();
        int serial = firstSerial, booked = unlock, since = 0, finishAt = -1;
        bool pickup = true;
        int next = CrimsonChoreography.IssueAt(booked, pickup, serial + 1, phase);
        for (int cycle = 0; cycle < cycles; cycle++)
        {
            int issued = 0;
            while (issued < CrimsonActCycle.PhrasesPerCycle)
            {
                int now = next;
                if (phase > 0 && CrimsonChorusRules.Due(phase, since, serial + 1))
                {
                    var chorus = CrimsonChorusRules.Schedule(Math.Max(Math.Max(unlock, now + CrimsonRhythm.LookAheadTicks), finishAt));
                    admitted.Add(new(serial + 1, now, null, chorus));
                    since = 0; booked = chorus.End; pickup = true;
                    next = CrimsonChoreography.IssueAt(booked, pickup, serial + 1, phase);
                    continue;
                }
                var rhythm = CrimsonChoreography.Admit(booked, pickup, now, unlock, ++serial, phase);
                admitted.Add(new(serial, now, rhythm, null));
                since++; issued++;
                foreach (var (hit, second) in CrimsonEnsemble.Notes(rhythm, phase))
                    finishAt = Math.Max(finishAt, Math.Max(rhythm.End,
                        CrimsonEnsemble.NoteEnd(CrimsonEnsemble.Technique(phase, serial, hit.Pulse, second), hit) + CrimsonRhythm.LeaseTicks));
                booked = rhythm.End; pickup = false;
                next = CrimsonChoreography.IssueAt(booked, pickup, serial + 1, phase);
            }
            // An unlatched cycle boundary: once the last owned flight and lease have ended, the runtime books the first
            // bar head that can still open with a pickup and issues it one look-ahead before that pickup's forecast.
            booked = CrimsonChoreography.Admit(-1, true, finishAt, unlock, serial + 1, phase).Start; pickup = true;
            next = CrimsonChoreography.IssueAt(booked, pickup, serial + 1, phase);
            AssertEqual(true, next >= finishAt, "never issued before the cycle completes");
            finishAt = -1;
        }
        return admitted;
    }

    [DomainTest("Scarlet runtime admission starts every booked phrase on its bar head, one look-ahead after it is issued")]
    private static void ScarletAdmissionChain()
    {
        int firstSerial = 0, unlock = CrimsonChoreography.OpeningTicks;
        for (int phase = 0; phase < 4; phase++)
        {
            var run = ScarletRunAct(phase, unlock, firstSerial, 2);
            int phrases = 0, choruses = 0, firstCycleChoruses = 0, lastKey = -1;
            for (int i = 0; i < run.Count; i++)
            {
                var (serial, issuedAt, phrase, chorus) = run[i];
                if (chorus is { } c)
                {
                    choruses++; if (phrases < CrimsonActCycle.PhrasesPerCycle) firstCycleChoruses++;
                    AssertEqual(true, phase > 0, "no chorus in Act I");
                    AssertEqual(false, CrimsonSignatureMoves.IsSignaturePhrase(phase, serial), "a chorus never comes right before a signature phrase");
                    AssertEqual(CrimsonMeter.BarTick(CrimsonMeter.BarAtOrAfter(c.Born)), c.Born, "called on a bar head");
                    AssertEqual(true, lastKey < 0 || c.Born > lastKey, "called after the previous key release");
                    var after = run[i + 1].Phrase!;
                    AssertEqual(c.End, after.Start, "the next phrase starts where the chorus ends");
                    AssertEqual(CrimsonChoreography.Pickup, after.Hits[0].Pulse, "and opens with a pickup crossflow");
                    AssertEqual(c.End, after.Hits[0].Fire, "released on the chorus's last bar head");
                    continue;
                }
                var p = phrase!;
                phrases++;
                AssertEqual(CrimsonRhythm.LookAheadTicks, p.FirstWarning - issuedAt,
                    $"phase {phase} serial {serial}: issued exactly one look-ahead before its first forecast (the curtain's observation)");
                AssertEqual(CrimsonMeter.BarTick(CrimsonMeter.BarAtOrAfter(p.Start)), p.Start, "starts on a bar head");
                if (i == 0) AssertEqual(unlock, p.Hits[0].Fire, "the Act's first release lands on the unlocking downbeat");
                bool boundary = phrases % CrimsonActCycle.PhrasesPerCycle == 1 && phrases > 1;
                if (i > 0 && run[i - 1].Phrase is { } previous && !boundary)
                {
                    AssertEqual(previous.End, p.Start, "no bar inserted between booked phrases");
                    AssertEqual(p.Start, previous.Hits[^1].Fire, "the closer or signature finale releases on this phrase's downbeat");
                    AssertEqual(false, p.Hits[0].Pulse == CrimsonChoreography.Pickup, "and the phrase needs no pickup");
                }
                if (boundary && !CrimsonSignatureMoves.IsSignaturePhrase(phase, serial))
                    AssertEqual(p.Start, p.Hits[0].Fire, "a cycle boundary resumes with a pickup on its downbeat");
                lastKey = p.Hits[^1].Fire;
            }
            AssertEqual(2 * CrimsonActCycle.PhrasesPerCycle, phrases, "two cycles admitted");
            if (phase is 1 or 2) AssertEqual(2, firstCycleChoruses, "two choruses in an Act's first cycle, as before");
            if (phase == 3) AssertEqual(true, choruses >= 3, "Final keeps its chorus every five phrases");
            firstSerial += 2 * CrimsonActCycle.PhrasesPerCycle;
            unlock = run[^1].Phrase!.End + CrimsonMeter.BarTick(4);
        }
    }

    [DomainTest("Scarlet late admission moves a phrase to the next feasible bar head with a pickup")]
    private static void ScarletAdmissionLate()
    {
        int unlock = CrimsonChoreography.OpeningTicks;
        for (int phase = 0; phase < 4; phase++)
        for (int serial = 2; serial <= 7; serial++)
        foreach (int booked in new[] { CrimsonMeter.BarTick(12), CrimsonMeter.BarTick(13), CrimsonMeter.BarTick(40) })
        {
            bool signature = CrimsonSignatureMoves.IsSignaturePhrase(phase, serial);
            int onTime = CrimsonChoreography.IssueAt(booked, false, serial, phase);
            var exact = CrimsonChoreography.Admit(booked, false, onTime, unlock, serial, phase);
            AssertEqual(booked, exact.Start, "on time: the booked bar head");
            AssertEqual(false, exact.Hits[0].Pulse == CrimsonChoreography.Pickup, "and no pickup added");
            for (int late = 1; late <= 400; late += 3)
            {
                int now = onTime + late;
                var moved = CrimsonChoreography.Admit(booked, false, now, unlock, serial, phase);
                AssertEqual(true, moved.FirstWarning >= now + CrimsonRhythm.LookAheadTicks, "every forecast still reaches peers in time");
                AssertEqual(CrimsonMeter.BarTick(CrimsonMeter.BarAtOrAfter(moved.Start)), moved.Start, "on a bar head");
                if (moved.Start == booked) { AssertEqual(exact, moved with { Hits = exact.Hits }, "still the booked phrase"); continue; }
                AssertEqual(true, moved.Start > booked, "moved later, never earlier");
                AssertEqual(!signature, moved.Hits[0].Pulse == CrimsonChoreography.Pickup, "a moved phrase takes a pickup (signature phrases never do)");
                var sooner = CrimsonChoreography.Create(CrimsonMeter.BarTick(CrimsonMeter.BarAtOrAfter(moved.Start) - 1), serial, phase, true);
                if (sooner.Start > booked) AssertEqual(true, sooner.FirstWarning < now + CrimsonRhythm.LookAheadTicks, "the first feasible bar head");
            }
        }
        // Nothing booked (an unlatched cycle boundary): the first feasible bar head after now, with a pickup.
        var free = CrimsonChoreography.Admit(-1, true, 5000, unlock, 13, 1);
        AssertEqual(true, free.FirstWarning >= 5030 && free.Hits[0].Pulse == CrimsonChoreography.Pickup, "free admission opens with a pickup");
        AssertEqual(free.Start, CrimsonChoreography.Admit(-1, true, 5000, unlock, 13, 1).Start, "deterministic");
    }

    [DomainTest("Scarlet choruses come every four or five phrases and never right before a signature phrase")]
    private static void ScarletChorusCadence()
    {
        for (int phase = 1; phase < 4; phase++)
        for (int start = 0; start < 3; start++)
        {
            int since = 0, serial = 12 + start, last = -1;
            for (int n = 0; n < 60; n++)
            {
                if (CrimsonChorusRules.Due(phase, since, serial + 1))
                {
                    AssertEqual(false, CrimsonSignatureMoves.IsSignaturePhrase(phase, serial + 1), "the phrase after a chorus can take its pickup");
                    if (last >= 0) AssertEqual(true, phase == 3 ? since == 5 : since is 4 or 5, $"spacing {since}");
                    since = 0; last = serial; continue;
                }
                serial++; since++;
                AssertEqual(true, since <= 6, "a chorus is never postponed past six phrases");
            }
        }
        AssertEqual(false, CrimsonChorusRules.Due(1, 4, 13), "four phrases are not enough when the fifth slot is open");
        AssertEqual(true, CrimsonChorusRules.Due(1, 4, 14), "but they are when the fifth slot precedes a signature phrase");
        AssertEqual(false, CrimsonChorusRules.Due(1, 5, 15), "never before a signature phrase");
        AssertEqual(true, CrimsonChorusRules.Due(3, 5, 15), "Final has no signature phrases");
    }

    [DomainTest("Scarlet apparition attacks hurt from the unlock tick on the plan clock, before a snapshot reports it")]
    private static void ScarletSourceActiveAtUnlock()
    {
        var actI = ScarletRehearsalState() with { Stage = CrimsonStage.Countdown, Age = 590, UnlockAt = 600 };
        AssertEqual(false, actI.SourceActive(0, 599.5f), "not before the unlock");
        AssertEqual(true, actI.SourceActive(0, 600), "the pickup released on the unlock hurts on a peer whose snapshot still says Countdown");
        AssertEqual(false, actI.SummonVulnerable(0), "while the apparition itself is not yet hittable");
        AssertEqual(false, actI.SourceActive(1, 600), "only the Act's own apparition");
        var actII = ScarletRehearsalState() with { Phase = 1, PhaseStart = 1100, Age = 1300, UnlockAt = 1305 };
        AssertEqual(true, actII.SourceActive(1, 1305), "Act II's pickup hurts on its unlock tick");
        AssertEqual(false, actII.SourceActive(1, 1304), "and not a tick before");
        AssertEqual(false, actII.SourceActive(0, 1305), "the retired apparition stays silent");
        AssertEqual(false, (actII with { Stage = CrimsonStage.Victory }).SourceActive(1, 1400), "a terminal stage silences every source");
        AssertEqual(false, (actII with { Fight = Guid.Empty }).SourceActive(1, 1400), "no Fight, no attack");
    }
}
