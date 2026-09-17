using System;
using System.Collections.Generic;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet rehearsal reduces party HP to one quarter and preserves all four health budgets")]
    private static void ScarletRehearsalBudgets()
    {
        int[] members = { 1, 2, 4, 8 }, totals = { 3000000, 5000000, 9000000, 17000000 };
        for (int i = 0; i < members.Length; i++)
        {
            int life = CrimsonInvocation.TargetLife(members[i]);
            AssertEqual(totals[i], life * 4, "total does not refill at Final");
            AssertEqual(life / 5, CrimsonPhaseRules.RetreatLife(life), "twenty-percent retreat");
            AssertEqual(life * 8 / 5, CrimsonPhaseRules.BarMaximum(3, life), "Final includes retained apparitions");
        }
        AssertEqual(1, CrimsonPlaytestTuning.AttackDamage, "all hostile source tuning");
    }
    [DomainTest("Scarlet phase cycle cannot complete before all notes recovery tails and chorus resolve")]
    private static void ScarletCycleCompletion()
    {
        var cycle = new CrimsonActCycle();
        for (int i = 0; i < 11; i++) cycle.Admit(112 * (i + 1), 112 * (i + 1) + 6);
        AssertEqual(false, cycle.TryComplete(99999, false), "eleven phrases never a complete repertoire");
        cycle.Admit(1344, 1350);
        AssertEqual(false, cycle.TryComplete(1344, false), "finish musical bar plus recovery");
        AssertEqual(false, cycle.TryComplete(1350, true), "chorus still owns its result");
        AssertEqual(true, cycle.TryComplete(1350, false), "complete at the safe boundary");
        AssertEqual(1, cycle.Completed, "one authority completion");
        AssertEqual(false, cycle.TryComplete(1351, false), "no replay");
        AssertEqual(0, cycle.Issued, "next complete cycle starts fresh");
        cycle.Reset(); AssertEqual(0, cycle.Completed, "new phase is not already unlocked");
    }
    [DomainTest("Scarlet phase cycles reject overflow and do not shorten an existing tail")]
    private static void ScarletCycleBounds()
    {
        var cycle = new CrimsonActCycle();
        for (int i = 0; i < 12; i++) cycle.Admit(i + 1, 500 - i);
        AssertEqual(500, cycle.FinishAt, "later admissions cannot truncate recovery");
        bool rejected = false;
        try { cycle.Admit(501, 510); } catch (InvalidOperationException) { rejected = true; }
        AssertEqual(true, rejected, "finite repertoire capacity");
        AssertEqual(false, cycle.TryComplete(499, false), "last future recovery remains");
        AssertEqual(true, cycle.TryComplete(500, false), "bounded completion");
    }
    private static CrimsonState ScarletRehearsalState() => new(
        Guid.Parse("a60d3295-60ad-47cd-86a7-12d971774e1c"), 1200, 120, -1, CrimsonStage.Performance,
        new[] { new CrimsonMember(0, Guid.Parse("748e1cb4-bea0-4c98-967d-0e87c32ad877"), true, false) },
        8000, 6000, Phase: 0, UnlockAt: 600, Target: 0,
        TargetLife: 750000, Life0: 150000, Life1: 750000, Life2: 750000, Life3: 750000);
    [DomainTest("Scarlet Final first-cycle floor blocks early kills without disabling attack sources")]
    private static void ScarletFinalFirstCycle()
    {
        var state = ScarletRehearsalState() with { Phase = 3, PhaseStart = 500, FinalStart = 500, UnlockAt = 650 };
        for (int source = 0; source < 4; source++) AssertEqual(1, state.DamageFloor(source), "hold every performer at one HP");
        AssertEqual(true, state.Vulnerable(state.Age), "performance source remains active");
        var released = state with { Age = 5000, CompletedCycles = 1 };
        for (int source = 0; source < 4; source++) AssertEqual(0, released.DamageFloor(source), "normal death after full ensemble");
        AssertEqual(true, released.CanReplace(state), "accept completion");
        AssertEqual(false, (state with { Age = 5001 }).CanReplace(released), "stale completion cannot relock Final");
    }
    [DomainTest("Scarlet cycle projection round trips and rejects truncated or negative completion data")]
    private static void ScarletCycleWire()
    {
        var state = ScarletRehearsalState() with { CompletedCycles = 2 };
        using var stream = new MemoryStream();
        using (var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) state.Write(w);
        byte[] bytes = stream.ToArray();
        using (var reader = new BinaryReader(new MemoryStream(bytes)))
        {
            var read = CrimsonState.Read(reader);
            AssertEqual(2, read.CompletedCycles, "completion field");
            AssertEqual(state.Fight, read.Fight, "same Fight");
            AssertEqual(state.Life0, read.Life0, "same health");
        }
        for (int n = 0; n < bytes.Length; n++)
        {
            bool rejected = false;
            try { using var r = new BinaryReader(new MemoryStream(bytes, 0, n)); CrimsonState.Read(r); }
            catch (Exception ex) when (ex is IOException or InvalidDataException) { rejected = true; }
            AssertEqual(true, rejected, "every truncated packet rejected");
        }
        using var invalid = new MemoryStream();
        using (var w = new BinaryWriter(invalid, System.Text.Encoding.UTF8, true)) (state with { CompletedCycles = -1 }).Write(w);
        bool bad = false;
        try { using var r = new BinaryReader(new MemoryStream(invalid.ToArray())); CrimsonState.Read(r); }
        catch (InvalidDataException) { bad = true; }
        AssertEqual(true, bad, "negative generation rejected");
    }
    [DomainTest("Scarlet twelve-phrase repertoire includes every apparition technique and varied drum calls")]
    private static void ScarletCompleteRepertoire()
    {
        var score = ScarletRecordedScore();
        for (int source = 0; source < 3; source++)
        {
            var techniques = new HashSet<CrimsonTechnique>(); var rhythms = new HashSet<string>();
            int earliest = score.IntroTicks; var cycle = new CrimsonActCycle();
            for (int serial = 0; serial < CrimsonActCycle.PhrasesPerCycle; serial++)
            {
                techniques.Add(CrimsonTechniqueGeometry.Select(source, serial));
                var phrase = CrimsonRhythm.Create(score, earliest, serial, false);
                var offsets = new List<int>();
                foreach (var hit in phrase.Hits) offsets.Add(hit.Warning - phrase.Start);
                rhythms.Add(string.Join(",", offsets));
                cycle.Admit(phrase.End, phrase.End + 6); earliest = phrase.End;
                AssertEqual(false, cycle.TryComplete(phrase.End, false), "cannot skip the current recovery");
            }
            AssertEqual(3, techniques.Count, "complete species repertoire");
            AssertEqual(true, rhythms.Count >= 4, "not one equal-spacing pattern");
        }
    }
}
