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
        for(int phrase=1;phrase<=12;phrase++) for(int i=0;i<4;i++) {
            AssertEqual(CrimsonTechnique.SpatialRift,CrimsonEnsemble.Technique(1,phrase,i,false),"every Act II basic note is a spatial cut");
            AssertEqual(CrimsonTechnique.ChoirRakes,CrimsonEnsemble.Technique(2,phrase,i,false),"every Act III basic note is a Choir rake volley");
        }
        for(int phase=0;phase<3;phase++) for(int phrase=1;phrase<=12;phrase++)
            AssertEqual(CrimsonTechnique.SideBeams,CrimsonEnsemble.Technique(phase,phrase,4,false),"all Act I–III phrases retain closing crossflow");
        for(int phrase=1;phrase<=12;phrase++) {
            var pair=CrimsonEnsemble.Pair(phrase);
            for(int i=0;i<4;i++) {
                AssertEqual(pair.First,CrimsonEnsemble.Technique(3,phrase,i,false),"Final first family unchanged");
                AssertEqual(pair.Second,CrimsonEnsemble.Technique(3,phrase,i,true),"Final second family unchanged");
            }
            AssertEqual(CrimsonTechnique.ClusterVolley,CrimsonEnsemble.Technique(3,phrase,4,false),"Final closes with clusters");
        }
        var hit=CrimsonChoreography.Create(score,score.IntroTicks,1,false).Hits[0];
        AssertEqual(hit.Fire+CrimsonSpatialCuts.LiveTicks,CrimsonEnsemble.NoteEnd(CrimsonTechnique.ChoirRakes,hit),"rakes keep the cut live window");
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
    [DomainTest("Scarlet Choir rakes rotate clockwise, bow and hook within wide forecast corridors")]
    private static void ScarletChoirRakes()
    {
        var p=TechniqueExample(CrimsonTechnique.ChoirRakes) with {End=612,LastEnd=633};
        Span<CrimsonStroke> warning=stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        Span<CrimsonStroke> live=stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        AssertEqual(16,(int)CrimsonTechnique.ChoirRakes,"append-only technique ID");
        AssertEqual(2,CrimsonTechniqueGeometry.Owner(CrimsonTechnique.ChoirRakes),"Choir owns rakes");
        AssertEqual(false,p.Aimed,"rakes have no player target identity");
        AssertEqual(true,CrimsonChoirRakes.Spacing-2*CrimsonChoirRakes.Radius>2*(42+CrimsonChoirRakes.Radius),"parallel corridors exceed two full player/capsule footprints");
        for(int phrase=1;phrase<=6;phrase++) for(byte pulse=0;pulse<4;pulse++) {
            var note=p with {Phrase=phrase,Pulse=pulse};
            note.Validate();
            int count=CrimsonTechniqueGeometry.Write(note,note.Fire,warning,true);
            AssertEqual(CrimsonChoirRakes.MaximumStrokes,count,"all three full curves are forecast");
            var f=note.Field;
            var first=warning[0].A;
            AssertEqual(true,pulse switch {
                0=>first.Y<f.CenterY&&Math.Abs(first.Y-(f.Top+CrimsonChoirRakes.EdgeInset))<.01f,
                1=>first.X>f.CenterX&&Math.Abs(first.X-(f.Right-CrimsonChoirRakes.EdgeInset))<.01f,
                2=>first.Y>f.CenterY&&Math.Abs(first.Y-(f.Bottom-CrimsonChoirRakes.EdgeInset))<.01f,
                _=>first.X<f.CenterX&&Math.Abs(first.X-(f.Left+CrimsonChoirRakes.EdgeInset))<.01f
            },"clockwise edge entry");
            var midpoint=warning[15].B;
            var tip=warning[31].B;
            var chord=CrimsonPoint.Lerp(first,tip,.5f);
            AssertEqual(true,(midpoint-chord).LengthSquared>2500,"bowed path visibly departs from a straight beam");
            AssertEqual(true,(warning[32].A-warning[0].A).LengthSquared>=CrimsonChoirRakes.Spacing*CrimsonChoirRakes.Spacing-.1f,"second claw keeps its lane");
            AssertEqual(true,(warning[64].A-warning[32].A).LengthSquared>=CrimsonChoirRakes.Spacing*CrimsonChoirRakes.Spacing-.1f,"third claw keeps its lane");
            foreach(var stroke in warning[..count]) {
                foreach(var point in new[]{stroke.A,stroke.B})
                    AssertEqual(true,point.X-stroke.Radius>=f.Left&&point.X+stroke.Radius<=f.Right
                        &&point.Y-stroke.Radius>=f.Top&&point.Y+stroke.Radius<=f.Bottom,"forecast capsule contained in arena");
            }
            AssertEqual(0,CrimsonTechniqueGeometry.Write(note,note.Fire,live),"zero-width ignition");
            for(float age=note.Fire+.25f;age<note.End;age+=.25f) {
                int visible=CrimsonTechniqueGeometry.Write(note,age,live);
                AssertEqual(true,visible>0&&visible<=count,$"bounded travelling extension age={age} visible={visible} forecast={count}");
                foreach(var stroke in live[..visible]) {
                    AssertEqual(true,stroke.Radius>0&&stroke.Radius<=CrimsonChoirRakes.Radius,"tapered live width");
                    foreach(var point in new[]{stroke.A,stroke.B})
                        AssertEqual(true,point.X-stroke.Radius>=f.Left&&point.X+stroke.Radius<=f.Right
                            &&point.Y-stroke.Radius>=f.Top&&point.Y+stroke.Radius<=f.Bottom,"live capsule contained in arena");
                }
                if(phrase==1&&(age-note.Fire)%1==.25f) {
                    int perCut=visible/CrimsonChoirRakes.Cuts;
                    for(int cut=0;cut<CrimsonChoirRakes.Cuts;cut++) for(int segment=0;segment<perCut;segment++) {
                        var stroke=live[cut*perCut+segment];
                        var forecast=warning[cut*CrimsonChoirRakes.SegmentsPerCut+segment];
                        var axis=stroke.B-stroke.A;
                        var normal=new CrimsonPoint(-axis.Y,axis.X)*(1/MathF.Sqrt(axis.LengthSquared));
                        var center=CrimsonPoint.Lerp(stroke.A,stroke.B,.5f);
                        for(int side=-1;side<=1;side++) {
                            var point=center+normal*(side*stroke.Radius);
                            AssertEqual(true,CrimsonTechniqueGeometry.Intersects(forecast,point.X-1,point.Y-1,2,2),"full live sweep lies inside announced capsule");
                        }
                    }
                }
            }
            AssertEqual(0,CrimsonTechniqueGeometry.Write(note,note.End,live),"exclusive live end");
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
