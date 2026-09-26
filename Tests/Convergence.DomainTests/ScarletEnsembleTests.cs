using System;
using System.IO;
using System.Linq;
using Convergence.Content.Encounters.CrimsonFoundry;
using Convergence.Common.Raids.Arena;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet covenant selects twenty current-HP targets with stable ties")]
    private static void ScarletCovenantSelection()
    {
        var candidates = Enumerable.Range(0, 26).Select(i => new CrimsonCovenantTarget(i, (i / 2 + 1) * 100)).ToArray();
        Span<int> slots = stackalloc int[20];
        AssertEqual(20, CrimsonCovenantRules.Select(candidates, slots), "bounded batch");
        AssertEqual("24,25,22,23,20,21,18,19,16,17,14,15,12,13,10,11,8,9,6,7", string.Join(",", slots.ToArray()), "highest current HP, slot tie break");
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
        AssertEqual(0f, CrimsonEnsemble.Absorption(CrimsonEnsemble.SacrificeStart), "bound before sacrifice");
        AssertEqual(1f, CrimsonEnsemble.Absorption(CrimsonEnsemble.SacrificeComplete), "all three consumed before unlock");
        AssertEqual(true, CrimsonEnsemble.FinalRelease > CrimsonEnsemble.SacrificeComplete, "giant appears only after full convergence");
        AssertEqual(1f, CrimsonEnsemble.ConductorAbsorption(CrimsonEnsemble.SacrificeStart), "Vespera consumed before sacrifices");
        AssertEqual(1f, CrimsonEnsemble.RetreatDissolve(CrimsonEnsemble.ActRelease), "old apparition dissolved before next reveal");
        AssertEqual(1f, CrimsonEnsemble.VictoryMelt(150), "melt finishes within unchanged cleanup lease");
    }
    [DomainTest("Scarlet covenant concentrates smoothly and starts another cast before its previous live tail ends")]
    private static void ScarletCovenantConcentration()
    {
        for(int n=1;n<=20;n++)
        {
            AssertEqual(true, CrimsonCovenantRules.Scale(n)>=.8f && CrimsonCovenantRules.Scale(n)<=2.61f, "bounded size");
            AssertEqual(true, CrimsonCovenantRules.DamageFactor(n)>=1 && CrimsonCovenantRules.DamageFactor(n)<=4, "bounded multiplier");
            if(n<20) AssertEqual(true, CrimsonCovenantRules.Scale(n)>CrimsonCovenantRules.Scale(n+1)
                && CrimsonCovenantRules.DamageFactor(n)>CrimsonCovenantRules.DamageFactor(n+1), "fewer targets concentrate");
            foreach(float width in new[]{20f,100f,600f,2400f,4000f})
                AssertEqual(true,CrimsonCovenantRules.HalfSpan(width,n)>width*.5f,"seals enclose entire enemy width");
        }
        AssertEqual(true,CrimsonCovenantRules.Cycle<CrimsonCovenantRules.Duration,"overlapping cast lifetimes");
        AssertEqual(.82f,CrimsonCovenantRules.Scale(20),"slightly smaller minimum");
        AssertEqual(4f,CrimsonCovenantRules.DamageFactor(1),"single-target per-hit factor");
    }
    [DomainTest("Scarlet covenant count codec accepts harmless empty state and rejects invalid or truncated counts")]
    private static void ScarletCovenantCountCodec()
    {
        foreach(byte n in new byte[]{0,1,4,10,20})
        { using var r=new BinaryReader(new MemoryStream(new[]{n}));AssertEqual(n,CrimsonCovenantRules.ReadBatchCount(r),"bounded count"); }
        foreach(var bytes in new[]{new byte[]{21},new byte[]{255},Array.Empty<byte>()})
        {
            using var r=new BinaryReader(new MemoryStream(bytes));bool rejected=false;
            try { CrimsonCovenantRules.ReadBatchCount(r); }
            catch(Exception e) when(e is IOException or InvalidDataException) {rejected=true;}
            AssertEqual(true,rejected,"malformed batch rejected before mutation");
        }
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
