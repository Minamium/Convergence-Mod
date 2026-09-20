using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet eight beat composition preserves four basic beats then two warning and two broad beam beats")]
    private static void ScarletEightBeatComposition()
    {
        var score=ScarletRecordedScore();
        for(int age=score.IntroTicks;age<54000;age+=137) {
            var p=CrimsonChoreography.Create(score,age,1,false);
            var b=CrimsonRhythm.NextBeats(score,Math.Max(0,age-.499999d),9);
            AssertEqual(5,p.Hits.Count,"four consecutive releases and one crossflow");
            for(int i=0;i<4;i++) {
                AssertEqual((int)Math.Round(b[i]),p.Hits[i].Warning,"forecast starts on beat including bar start");
                AssertEqual((int)Math.Round(b[i+1]),p.Hits[i].Fire,"one beat later strike");
                AssertEqual(true,p.Hits[i].Fire-p.Hits[i].Warning>=CrimsonRhythm.MinimumWarningTicks,"minimum warning at all score/loop positions");
            }
            AssertEqual((int)Math.Round(b[4]),p.Hits[4].Warning,"four beats complete");
            AssertEqual((int)Math.Round(b[6]),p.Hits[4].Fire,"two beat charge");
            AssertEqual((int)Math.Round(b[8]),p.Hits[4].End,"two beat amplification and collapse");
            var next=CrimsonChoreography.Create(score,p.End,2,false);
            AssertEqual(p.End,next.Hits[0].Warning,"no blank beat after beam tail");
            AssertEqual(p.End,p.Hits[4].End,"next bar starts without extra dead time");
        }
        AssertEqual(CrimsonTechnique.SpatialRift,CrimsonChoreography.Technique(1,2,0),"Act II alternates spatial cuts");
        AssertEqual(CrimsonTechnique.TrackingBeam,CrimsonChoreography.Technique(1,3,0),"Act II retains pursuit contrast");
        AssertEqual(CrimsonTechnique.SideBeams,CrimsonChoreography.Technique(1,2,4),"every phrase closes with crossflow");
        for(int i=0;i<4;i++) AssertEqual(CrimsonTechnique.SpatialGrid,CrimsonChoreography.Technique(2,2,i),"four offset cuts in Act III");
    }
    [DomainTest("Scarlet presentation opens from orb to girl before backdrop and summons")]
    private static void ScarletOpeningOrder()
    {
        AssertEqual(0f,CrimsonChoreography.Reveal(-1),"preparation contains only orb");
        AssertEqual(0f,CrimsonChoreography.Backdrop(250),"no cathedral before materialization");
        AssertEqual(1f,CrimsonChoreography.Reveal(250),"girl precedes world change");
        AssertEqual(true,CrimsonChoreography.Seal(CrimsonChoreography.SummonAt+CrimsonInvocation.MusicLeadTicks)>.9f,"full seal before summon");
        AssertEqual(0f,CrimsonChoreography.Seal(CrimsonChoreography.OpeningTicks+60),"no lingering startup seal");
    }
    [DomainTest("Scarlet capped lead and horizontal seal-to-seal crossflow share bounded collision")]
    private static void ScarletAimedGeometry()
    {
        var p=TechniqueExample(CrimsonTechnique.SideBeams) with {End=656,LastEnd=656};
        var f=p.Field;var center=new CrimsonPoint(f.CenterX,f.CenterY);
        AssertEqual(center+new CrimsonPoint(220,-160),CrimsonChoreography.Predict(f,center,new(50,-50)),"same capped lead as Doll");
        for(int i=0;i<4;i++) {
            var axis=CrimsonChoreography.Direction(i,0);
            AssertEqual(true,Math.Abs(axis.LengthSquared-1)<.0001f,"normalized four axes");
        }
        foreach(float x in new[]{f.Left+100,f.CenterX,f.Right-100}) {
            p=p with {Target=new(x,f.CenterY)};
            var full=CrimsonChoreography.Side(p,p.Fire,true);
            AssertEqual(p.Target.Y,full.A.Y,"seals beside locked player position");
            AssertEqual(full.A.Y,full.B.Y,"horizontal stream");
            AssertEqual(true,full.A.X>full.B.X,"right emitter into left receiver");
            AssertEqual(true,full.B.X>=f.Left&&full.A.X<=f.Right,"both seals inside field");
            for(float t=p.Fire;t<p.End;t+=.5f) {
                var live=CrimsonChoreography.Side(p,t,false);
                AssertEqual(full.A,live.A,"fixed right emitter");
                AssertEqual(true,live.B.X<=full.A.X&&live.B.X>=full.B.X,"front grows leftward");
                AssertEqual(true,live.Radius<=full.Radius&&live.Radius>=0,"shared visible collision envelope");
            }
        }
        var resting=CrimsonChoreography.ClampParticipant(f,f.CenterX,f.Bottom-42,20,42);
        AssertEqual((f.CenterX,f.Bottom-42),resting,"ordinary support floor is not displaced upward");
    }
    [DomainTest("Scarlet successive full-field lattices shift predictably and leave complete player corridors")]
    private static void ScarletShiftedLattice()
    {
        var p=TechniqueExample(CrimsonTechnique.SpatialGrid) with {End=612};
        Span<CrimsonStroke> cuts=stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        float prior=-1;
        for(byte note=0;note<4;note++) {
            var n=p with {Pulse=note};
            int count=CrimsonTechniqueGeometry.Write(n,n.Fire,cuts,true);
            AssertEqual(true,count>=12&&count<=18,"bounded full-field vertical and horizontal lanes");
            AssertEqual(true,prior!=cuts[0].A.X,"next note moves the lattice");prior=cuts[0].A.X;
            var f=n.Field;
            foreach(var s in cuts[..count]) {
                bool vertical=s.A.X==s.B.X;
                AssertEqual(true,vertical?Math.Abs(s.B.Y-s.A.Y)==f.Bottom-f.Top:Math.Abs(s.B.X-s.A.X)==f.Right-f.Left,"edge to edge");
            }
            AssertEqual(true,CrimsonSpatialCuts.Spacing-CrimsonSpatialCuts.Radius*2>42,"full player can leave the next line");
        }
    }
    [DomainTest("Scarlet chorus result payload bounds and terminal monotonicity")]
    private static void ScarletChorusResult()
    {
        foreach(byte[] bytes in new[]{new byte[]{},new byte[]{1},new byte[]{2,0},new byte[]{0,1},new byte[]{1,8}}) {
            bool rejected=false;
            try { using var r=new BinaryReader(new MemoryStream(bytes));CrimsonChorusRules.ReadVerdict(r,7); }
            catch(Exception e) when(e is IOException or InvalidDataException){rejected=true;}
            AssertEqual(true,rejected,"truncated, forged or out-of-roster verdict rejected");
        }
        using var good=new BinaryReader(new MemoryStream(new byte[]{1,5}));
        AssertEqual((true,(byte)5),CrimsonChorusRules.ReadVerdict(good,7),"bounded failure mask");
        AssertEqual(false,CrimsonChorusRules.CanReplaceVerdict(true,5,false,0),"no unresolved rollback");
        AssertEqual(false,CrimsonChorusRules.CanReplaceVerdict(true,5,true,3),"no changed terminal result");
        AssertEqual(true,CrimsonChorusRules.CanReplaceVerdict(true,5,true,5),"idempotent duplicate");
    }
    [DomainTest("Scarlet unowned native actor envelopes are harmless without weakening owned DTO validation")]
    private static void ScarletUnownedActorEnvelope()
    {
        using var bytes=new MemoryStream();
        using(var w=new BinaryWriter(bytes,System.Text.Encoding.UTF8,true)) {
            default(CrimsonState).WriteEnvelope(w);
            default(CrimsonEffigyState).WriteEnvelope(w);
        }
        AssertEqual(2L,bytes.Length,"one presence byte per unowned actor, no null roster");
        bytes.Position=0;using var r=new BinaryReader(bytes);
        AssertEqual(true,CrimsonState.ReadEnvelope(r) is null,"unowned boss ignored");
        AssertEqual(true,CrimsonEffigyState.ReadEnvelope(r) is null,"unowned effigy ignored");
        var owned=new CrimsonEffigyState(Guid.NewGuid(),3,2,500);
        using var payload=new MemoryStream();
        using(var w=new BinaryWriter(payload,System.Text.Encoding.UTF8,true))owned.WriteEnvelope(w);
        payload.Position=0;using var reader=new BinaryReader(payload);
        AssertEqual(owned,CrimsonEffigyState.ReadEnvelope(reader)!.Value,"owned state still round trips");
        using var badPresence=new BinaryReader(new MemoryStream(new byte[]{2}));
        bool malformed=false;try{CrimsonEffigyState.ReadEnvelope(badPresence);}catch(InvalidDataException){malformed=true;}
        AssertEqual(true,malformed,"nonboolean presence byte rejected");
        for(int n=0;n<payload.Length;n++) {
            bool rejected=false;
            try { using var cut=new BinaryReader(new MemoryStream(payload.ToArray(),0,n));CrimsonEffigyState.ReadEnvelope(cut); }
            catch(IOException){rejected=true;}
            AssertEqual(true,rejected,"owned truncation never treated as an unowned actor");
        }
    }
}
