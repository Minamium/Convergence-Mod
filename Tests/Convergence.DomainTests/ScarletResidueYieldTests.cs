using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Common.Raids.Arena;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

// Signature residue under the forecast, yielding where the next beat is safe (presentation only): which residue
// strokes the move's next note keeps dangerous, and how fast the others dry.
internal static partial class Program
{
    private const int ScarletYieldPhrase = 3; // a signature phrase (serial % 3 == 0)

    private static bool[] ScarletYieldHolds(in CrimsonGesturePlan plan, ReadOnlySpan<CrimsonGesturePlan> phrase, out CrimsonStroke[] residue)
    {
        residue = new CrimsonStroke[CrimsonSignatureMoves.MaximumStrokes];
        int count = CrimsonTechniqueGeometry.Write(plan, plan.End - 1, residue, false);
        Array.Resize(ref residue, count);
        var next = new CrimsonStroke[CrimsonSignatureMoves.MaximumStrokes];
        int follower = ScarletResidueYield.Successor(plan, phrase);
        int forecast = follower >= 0 ? ScarletResidueYield.Forecast(phrase[follower], next) : 0;
        var holds = new bool[count];
        for (int i = 0; i < count; i++) holds[i] = ScarletResidueYield.Holds(residue[i], next.AsSpan(0, forecast));
        return holds;
    }

    private static CrimsonGesturePlan[] ScarletYieldPhraseOf(int phase, float observedX = 0, int curtainMask = 0)
    {
        var plans = new CrimsonGesturePlan[CrimsonChoreography.BasicNotes];
        for (int note = 0; note < plans.Length; note++)
            plans[note] = ScarletSignaturePlan(phase, ScarletYieldPhrase, note, observedX, curtainMask: curtainMask);
        return plans;
    }

    [DomainTest("Scarlet signature residue: a curtain column holds exactly when the next note burns the same column")]
    private static void ScarletResidueYieldCurtain()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        for (int mask = 1; mask <= CrimsonSignatureMoves.MaximumCurtainMask; mask++)
        {
            var phrase = ScarletYieldPhraseOf(0, curtainMask: mask);
            for (int note = 0; note < phrase.Length; note++)
            {
                var holds = ScarletYieldHolds(phrase[note], phrase, out var residue);
                AssertEqual(CrimsonSignatureMoves.CurtainBurning(phrase[note]), residue.Length, "one residue stroke per burning column");
                int nextSafe = note + 1 < phrase.Length ? CrimsonSignatureMoves.CurtainSafe(mask, ScarletYieldPhrase, note + 1) : -1;
                for (int i = 0; i < residue.Length; i++)
                {
                    int column = CrimsonSignatureMoves.CurtainColumn(f, residue[i].A.X);
                    bool burnsNext = note + 1 < phrase.Length && (nextSafe >> column & 1) == 0;
                    // The 2 px seam between neighbouring burning columns never counts as covering a column.
                    AssertEqual(burnsNext, holds[i], $"mask {mask} note {note} column {column}");
                }
            }
        }
    }

    [DomainTest("Scarlet signature residue: every shroud line yields because the next note uses the other comb")]
    private static void ScarletResidueYieldRope()
    {
        var phrase = ScarletYieldPhraseOf(1);
        for (int note = 0; note < phrase.Length; note++)
        {
            var holds = ScarletYieldHolds(phrase[note], phrase, out var residue);
            AssertEqual(CrimsonSignatureMoves.RopeLines, residue.Length, "five lines");
            foreach (bool hold in holds) AssertEqual(false, hold, $"note {note}: the next comb is elsewhere");
            if (note + 1 == phrase.Length) continue;
            var next = new CrimsonStroke[CrimsonSignatureMoves.MaximumStrokes];
            int count = ScarletResidueYield.Forecast(phrase[note + 1], next);
            float nearest = float.MaxValue;
            foreach (var line in residue)
                for (int i = 0; i < count; i++) nearest = MathF.Min(nearest, ScarletResidueYield.AxisDistance(line, next[i]));
            AssertEqual(CrimsonSignatureMoves.RopeCombShift, nearest, "the two combs are 112 px apart");
        }
    }

    [DomainTest("Scarlet signature residue: the fingers of the shared struck quarter hold and the other three yield")]
    private static void ScarletResidueYieldHands()
    {
        var f = RaidFieldGeometry.FromGround(8000, 6000);
        var phrase = ScarletYieldPhraseOf(2);
        for (int note = 0; note < phrase.Length; note++)
        {
            var holds = ScarletYieldHolds(phrase[note], phrase, out var residue);
            AssertEqual(2 * CrimsonSignatureMoves.HandsClaws, residue.Length, "two slammed quarters of three fingers");
            var next = note + 1 < phrase.Length ? CrimsonSignatureMoves.HandsStruck(ScarletYieldPhrase, note + 1) : (First: -1, Second: -1);
            int held = 0;
            for (int i = 0; i < residue.Length; i++)
            {
                int quarter = (int)MathF.Floor((residue[i].A.X - f.Left) / CrimsonSignatureMoves.QuarterWidth);
                AssertEqual(quarter == next.First || quarter == next.Second, holds[i], $"note {note} finger {i} in quarter {quarter}");
                if (holds[i]) held++;
            }
            AssertEqual(note + 1 < phrase.Length ? CrimsonSignatureMoves.HandsClaws : 0, held, $"note {note}: consecutive notes share one struck quarter");
        }
    }

    [DomainTest("Scarlet signature residue: the yield fades to nothing in six ticks, holding strokes keep the full residue")]
    private static void ScarletResidueYieldTiming()
    {
        AssertEqual(6, ScarletResidueYield.YieldTicks, "six ticks");
        float previous = 2;
        for (int tick = 0; tick <= 12; tick++)
        {
            AssertEqual(1f, ScarletResidueYield.Factor(true, tick), $"holding stroke at +{tick}");
            float factor = ScarletResidueYield.Factor(false, tick);
            AssertEqual(true, factor >= 0 && factor <= 1 && factor <= previous, $"monotone yield at +{tick}");
            previous = factor;
        }
        AssertEqual(1f, ScarletResidueYield.Factor(false, 0), "the residue starts exactly where the live strike ended");
        AssertEqual(.5f, ScarletResidueYield.Factor(false, 3), "half way");
        AssertEqual(0f, ScarletResidueYield.Factor(false, ScarletResidueYield.YieldTicks), "gone at End + 6");
    }

    [DomainTest("Scarlet signature residue: only the move's next note is a successor, and the switch covers signature moves only")]
    private static void ScarletResidueYieldScope()
    {
        foreach (int phase in new[] { 0, 1, 2 })
        {
            var phrase = ScarletYieldPhraseOf(phase, observedX: 8000);
            for (int note = 0; note < phrase.Length; note++)
                AssertEqual(note + 1 < phrase.Length ? note + 1 : -1, ScarletResidueYield.Successor(phrase[note], phrase), $"phase {phase} note {note}");
            var other = ScarletSignaturePlan(phase, ScarletYieldPhrase + 3, 1, 8000);
            AssertEqual(false, ScarletResidueYield.Follows(phrase[0], other), "another phrase");
            AssertEqual(false, ScarletResidueYield.Follows(phrase[0], phrase[0] with { Fight = Guid.NewGuid(), Pulse = 1 }), "another Fight");
            AssertEqual(false, ScarletResidueYield.Follows(phrase[0], phrase[1] with { Epoch = phrase[1].Epoch + 1 }), "another epoch");
            AssertEqual(false, ScarletResidueYield.Follows(phrase[1], phrase[0]), "an earlier note");
        }
        var crossflow = ScarletSignaturePlan(0, ScarletYieldPhrase, 3, 8000) with { Technique = CrimsonTechnique.SideBeams, Pulse = 4 };
        AssertEqual(false, ScarletResidueYield.Follows(ScarletSignaturePlan(0, ScarletYieldPhrase, 3, 8000), crossflow), "the crossflow is not the move's next note");
        bool enabled = ScarletResidueYield.Enabled;
        try
        {
            ScarletResidueYield.Enabled = true;
            AssertEqual(true, ScarletResidueYield.Applies(ScarletSignaturePlan(1, ScarletYieldPhrase, 0, 8000)), "signature move");
            AssertEqual(false, ScarletResidueYield.Applies(crossflow), "a basic field beam keeps the approved residue");
            ScarletResidueYield.Enabled = false;
            AssertEqual(false, ScarletResidueYield.Applies(ScarletSignaturePlan(1, ScarletYieldPhrase, 0, 8000)), "switched off");
        }
        finally { ScarletResidueYield.Enabled = enabled; }
        AssertEqual(true, ScarletResidueYield.Enabled, "the shipped default is on");
    }

    [DomainTest("Scarlet signature residue: axis distance is exact for crossing, parallel and separated capsules")]
    private static void ScarletResidueYieldGeometry()
    {
        CrimsonStroke S(float ax, float ay, float bx, float by, float r = 10) => new(new(ax, ay), new(bx, by), r);
        AssertEqual(0f, ScarletResidueYield.AxisDistance(S(0, 0, 100, 100), S(0, 100, 100, 0)), "crossing");
        AssertEqual(30f, ScarletResidueYield.AxisDistance(S(0, 0, 100, 0), S(0, 30, 100, 30)), "parallel");
        AssertEqual(50f, ScarletResidueYield.AxisDistance(S(0, 0, 100, 0), S(150, 0, 200, 0)), "collinear gap");
        AssertEqual(5f, ScarletResidueYield.AxisDistance(S(0, 0, 100, 0), S(50, 5, 50, 80)), "T junction");
        AssertEqual(true, ScarletResidueYield.Holds(S(0, 0, 0, 100, 128), new[] { S(127, 0, 127, 100, 10) }), "inside the wider radius");
        AssertEqual(false, ScarletResidueYield.Holds(S(0, 0, 0, 100, 128), new[] { S(256, 0, 256, 100, 130) }), "a 2 px seam");
        AssertEqual(false, ScarletResidueYield.Holds(S(0, 0, 0, 100, 128), ReadOnlySpan<CrimsonStroke>.Empty), "no next note");
    }
}
