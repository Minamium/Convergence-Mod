using System;
using System.IO;
using System.Linq;
using Convergence.Content.Encounters.CrimsonFoundry;
using Convergence.Common.Raids.Arena;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet covenant selects ten current-HP targets with stable ties")]
    private static void ScarletCovenantSelection()
    {
        var candidates = Enumerable.Range(0, 16).Select(i => new CrimsonCovenantTarget(i, (i / 2 + 1) * 100)).ToArray();
        Span<int> slots = stackalloc int[10];
        AssertEqual(10, CrimsonCovenantRules.Select(candidates, slots), "bounded batch");
        AssertEqual("14,15,12,13,10,11,8,9,6,7", string.Join(",", slots.ToArray()), "highest current HP, slot tie break");
        AssertEqual(0, CrimsonCovenantRules.Select(Span<CrimsonCovenantTarget>.Empty, slots), "empty targets");
        AssertEqual(0f, CrimsonCovenantRules.Opening(CrimsonCovenantRules.ChargeTicks), "charge harmless");
        AssertEqual(1f, CrimsonCovenantRules.Opening(CrimsonCovenantRules.ChargeTicks + 7), "connected amplification");
        AssertEqual(0f, CrimsonCovenantRules.Opening(CrimsonCovenantRules.Duration), "closed tail");
    }
    [DomainTest("Scarlet final bindings are equally spaced and emergence precedes unlock")]
    private static void ScarletEnsembleGeometry()
    {
        var field = RaidFieldGeometry.FromGround(8000, 6000);
        var a = CrimsonEnsemble.Binding(field, 0); var b = CrimsonEnsemble.Binding(field, 1); var c = CrimsonEnsemble.Binding(field, 2);
        AssertEqual(true, a.Y < field.CenterY && b.Y > field.CenterY && c.Y > field.CenterY && b.X > a.X && c.X < a.X, "top/right-bottom/left-bottom");
        AssertEqual(true, Math.Abs((a-b).LengthSquared-(b-c).LengthSquared)<1, "equilateral bindings");
        AssertEqual(true, a.Y - 177 > field.Top && b.Y + 177 < field.Bottom, "child seals fit inside the black field mask");
        AssertEqual(0f, CrimsonEnsemble.Emergence(CrimsonEnsemble.ActRelease, false), "gate before summon");
        AssertEqual(1f, CrimsonEnsemble.Emergence(CrimsonEnsemble.Transition(2), false), "act visible before attacks");
        AssertEqual(1f, CrimsonEnsemble.Emergence(CrimsonEnsemble.Transition(3), true), "giant visible before attacks");
        AssertEqual(0f, CrimsonEnsemble.Absorption(CrimsonEnsemble.FinalRelease), "bound before sacrifice");
        AssertEqual(1f, CrimsonEnsemble.Absorption(CrimsonEnsemble.SacrificeComplete), "all three consumed before unlock");
    }
    [DomainTest("Scarlet final cycles three two-family overlaps without removing warning beats")]
    private static void ScarletEnsembleComposition()
    {
        var score = ScarletRecordedScore();
        for (int phrase = 1; phrase <= 12; phrase++)
        {
            var pair = CrimsonEnsemble.Pair(phrase);
            AssertEqual(true, pair.First != pair.Second, "distinct simultaneous families");
            var rhythm = CrimsonChoreography.Create(score, 4000 + phrase * 233, phrase, true);
            AssertEqual(9, rhythm.Hits.Count + CrimsonChoreography.BasicNotes, "bounded final notes");
            for (int i=0;i<4;i++)
            {
                AssertEqual(pair.First, CrimsonEnsemble.Technique(3, phrase, i, false), "primary");
                AssertEqual(pair.Second, CrimsonEnsemble.Technique(3, phrase, i, true), "simultaneous secondary");
                AssertEqual(true, rhythm.Hits[i].Fire - rhythm.Hits[i].Warning >= CrimsonRhythm.MinimumWarningTicks, "both use full beat");
            }
            AssertEqual(CrimsonTechnique.ClusterVolley, CrimsonEnsemble.Technique(3,phrase,4,false), "Final closing volley");
        }
    }
    [DomainTest("Scarlet failed chorus preserves bounded immutable world positions")]
    private static void ScarletChorusResultPositions()
    {
        var positions = new[] { new CrimsonPoint(8100,5700), new CrimsonPoint(7800,5200) };
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream); using var reader = new BinaryReader(stream);
        CrimsonChorusImpactPositions.Write(writer,positions); stream.Position=0;
        AssertEqual(true, positions.AsSpan().SequenceEqual(CrimsonChorusImpactPositions.Read(reader,true,3)), "round trip final coordinates");
        foreach (byte count in new byte[] { 0,1,9,255 })
        {
            using var bad = new BinaryReader(new MemoryStream(new[] {count}));
            bool rejected=false;
            try { CrimsonChorusImpactPositions.Read(bad,true,3); } catch(InvalidDataException) {rejected=true;}
            AssertEqual(true,rejected,"roster/count malformed");
        }
        using var premature = new BinaryReader(new MemoryStream(new byte[]{2}));
        bool blocked=false;
        try { CrimsonChorusImpactPositions.Read(premature,false,3); } catch(InvalidDataException) {blocked=true;}
        AssertEqual(true,blocked,"positions only after authority verdict");
    }
}
