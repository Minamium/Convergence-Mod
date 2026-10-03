using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet two-bar composition keeps sparse notes between bar-head crossflows and selects each Act's techniques")]
    private static void ScarletEightBeatComposition()
    {
        for(int age=CrimsonChoreography.OpeningTicks;age<54000;age+=137) for(int serial=1;serial<=3;serial++) {
            var p=CrimsonChoreography.Create(age,serial,0,false);
            int first=ScarletFirstBarAtOrAfter(age)*CrimsonMeter.BeatsPerBar;
            AssertEqual(CrimsonMeter.BeatTick(first),p.Start,"phrase begins on its bar head");
            AssertEqual(CrimsonMeter.BeatTick(first+8),p.End,"two bars");
            AssertEqual(4,p.Hits.Count,serial==3?"four signature steps":"three notes and the closing crossflow");
            int strikes=0;
            foreach(var hit in p.Hits) if(hit.Fire<p.End) strikes++;
            AssertEqual(3,strikes,"three strikes inside the phrase; the fourth release belongs to the next downbeat");
            AssertEqual(p.End,p.Hits[^1].Fire,"the closer or the final step lands on the next downbeat");
            var next=CrimsonChoreography.Create(p.End,serial+1,0,false);
            AssertEqual(p.End,next.Start,"the next phrase starts on that downbeat");
            AssertEqual(true,next.FirstWarning>=p.Hits[^1].Fire+20,"after a beat of release, no forecast stacks on it");
        }
        for(int phrase=1;phrase<=12;phrase++) for(int i=0;i<4;i++) {
            bool signature=phrase%3==0;
            AssertEqual(signature?CrimsonTechnique.CinderCurtain:CrimsonTechnique.TrackingBeam,CrimsonEnsemble.Technique(0,phrase,i,false),"Act I notes are tracking beams except the signature curtain");
            AssertEqual(signature?CrimsonTechnique.ShroudRope:CrimsonTechnique.SpatialRift,CrimsonEnsemble.Technique(1,phrase,i,false),"Act II notes are spatial cuts except the signature rope");
            AssertEqual(signature?CrimsonTechnique.FourHands:CrimsonTechnique.ChoirRakes,CrimsonEnsemble.Technique(2,phrase,i,false),"Act III notes are Choir rake volleys except the signature hands");
        }
        for(int phase=0;phase<3;phase++) for(int phrase=1;phrase<=12;phrase++) {
            AssertEqual(CrimsonTechnique.SideBeams,CrimsonEnsemble.Technique(phase,phrase,CrimsonChoreography.Closer,false),"closers are the seal crossflow");
            AssertEqual(CrimsonTechnique.SideBeams,CrimsonEnsemble.Technique(phase,phrase,CrimsonChoreography.Pickup,false),"pickups are the seal crossflow");
            bool closes=false;
            foreach(var hit in CrimsonChoreography.Create(900,phrase,phase,false).Hits) closes|=hit.Pulse==CrimsonChoreography.Closer;
            AssertEqual(phrase%3!=0,closes,"every ordinary Act I-III phrase closes with the crossflow; a signature move's final step takes its place");
        }
        for(int phrase=1;phrase<=12;phrase++) {
            var pair=CrimsonEnsemble.Pair(phrase);
            for(int i=0;i<4;i++) {
                AssertEqual(pair.First,CrimsonEnsemble.Technique(3,phrase,i,false),"Final first family unchanged");
                AssertEqual(pair.Second,CrimsonEnsemble.Technique(3,phrase,i,true),"Final second family unchanged");
            }
            AssertEqual(CrimsonTechnique.ClusterVolley,CrimsonEnsemble.Technique(3,phrase,CrimsonChoreography.Closer,false),"Final closes with clusters");
            AssertEqual(CrimsonTechnique.ClusterVolley,CrimsonEnsemble.Technique(3,phrase,CrimsonChoreography.Pickup,false),"the Final drop's pickup is the cluster orb");
        }
        var hit0=CrimsonChoreography.Create(CrimsonChoreography.OpeningTicks,1,2,false).Hits[0];
        AssertEqual(hit0.Fire+CrimsonSpatialCuts.LiveTicks,CrimsonEnsemble.NoteEnd(CrimsonTechnique.ChoirRakes,hit0),"rakes keep the cut live window");
    }
    [DomainTest("Scarlet every phrase opens on a bar head, spans two bars and warns for one measured beat")]
    private static void ScarletPhrasesOnBarHeads()
    {
        for(int earliest=0;earliest<=3000;earliest++) {
            int phase=earliest%4, serial=earliest%7+1; bool pickup=earliest%2==1;
            var p=CrimsonChoreography.Create(earliest,serial,phase,pickup);
            bool takesPickup=pickup&&!CrimsonSignatureMoves.IsSignaturePhrase(phase,serial);
            int bar=Math.Max(ScarletFirstBarAtOrAfter(earliest),takesPickup?1:0);
            AssertEqual(CrimsonMeter.BarTick(bar),p.Start,"start is the first bar head at or after earliest");
            AssertEqual(true,p.Start>=earliest&&p.Start-earliest<=113,"never earlier than asked, at most one bar later");
            AssertEqual(CrimsonMeter.BarTick(bar+2),p.End,"end is two bars later");
            AssertEqual(225,p.End-p.Start,"two bars are exactly 225 ticks, whatever the bar");
            AssertEqual(CrimsonRhythmKind.Groove,p.Kind,"groove only");
            AssertEqual(takesPickup?5:4,p.Hits.Count,"notes per phrase");
            AssertEqual(takesPickup,p.Hits[0].Pulse==CrimsonChoreography.Pickup,"a pickup leads the phrase");
            AssertEqual(takesPickup?p.Start-p.Hits[0].Fire+p.Hits[0].Warning:p.Hits[0].Warning,p.FirstWarning,"first forecast");
            foreach(var hit in p.Hits) {
                int warning=hit.Fire-hit.Warning;
                if(CrimsonChoreography.IsCrossflow(hit.Pulse)) {
                    AssertEqual(true,warning is 56 or 57,"crossflow charges for two beats");
                    AssertEqual(true,hit.End-hit.Fire is 56 or 57,"crossflow collapses over two beats");
                }
                else AssertEqual(true,warning is 28 or 29,"a note's warning is one beat: 28 or 29 ticks");
            }
        }
        AssertThrows<ArgumentOutOfRangeException>(()=>CrimsonChoreography.Create(-1,1,0,false),"negative earliest");
        AssertThrows<ArgumentOutOfRangeException>(()=>CrimsonChoreography.Create(0,-1,0,false),"negative serial");
    }
    [DomainTest("Scarlet opening and summon land on bar heads, and Act I's first crossflow releases on the unlock downbeat")]
    private static void ScarletOpeningBars()
    {
        AssertEqual(900,CrimsonChoreography.OpeningTicks,"eight bars before Act I unlocks");
        AssertEqual(450,CrimsonChoreography.SummonAt,"Ember Crown emerges on bar four");
        AssertEqual(CrimsonMeter.BarTick(CrimsonMeter.OpeningBars),CrimsonChoreography.OpeningTicks,"opening is a whole number of bars");
        AssertEqual(CrimsonMeter.BarTick(CrimsonMeter.SummonBar),CrimsonChoreography.SummonAt,"summon is a whole number of bars");
        var open=CrimsonChoreography.Create(CrimsonChoreography.OpeningTicks,1,0,true);
        AssertEqual(CrimsonChoreography.OpeningTicks,open.Start,"first phrase starts the moment Act I unlocks");
        AssertEqual(CrimsonChoreography.OpeningTicks,open.Hits[0].Fire,"its pickup crossflow releases on that downbeat");
        AssertEqual(true,open.FirstWarning>CrimsonChoreography.SummonAt,"the seals bloom after Ember Crown has emerged");
        AssertEqual(CrimsonChoreography.OpeningTicks,CrimsonChoreography.Create(CrimsonChoreography.OpeningTicks-30,1,0,true).Start,"an earlier request lands on the same bar head");
        // Acts II/III unlock two bars after the change and Final five: the pickup's charge stays inside the protected transition.
        foreach(int bars in new[]{CrimsonArrangement.TransitionBars(1),CrimsonArrangement.TransitionBars(3)}) {
            int change=CrimsonMeter.BarTick(40), unlock=CrimsonMeter.BarTick(40+bars);
            var first=CrimsonChoreography.Create(unlock,13,bars==5?3:1,true);
            AssertEqual(unlock,first.Hits[0].Fire,"the new Act's first release is its combat downbeat");
            AssertEqual(true,first.FirstWarning-CrimsonRhythm.LookAheadTicks>=change,"issued after the change, inside its epoch");
            AssertEqual(true,first.FirstWarning>change+CrimsonEnsemble.ActRelease,"the seals bloom after the stop");
        }
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
        foreach(float x in new[]{f.Left+100,f.Left+470,f.CenterX,f.Right-300,f.Right-100}) {
            p=p with {Target=new(x,f.CenterY)};
            var (right,left)=CrimsonChoreography.Seals(p);
            var band=CrimsonChoreography.Side(p,p.Fire,true);
            AssertEqual(right,band.A,"the forecast band starts at the right seal centre");
            AssertEqual(left,band.B,"and ends at the left seal centre");
            AssertEqual(p.Target.Y,right.Y,"seals beside the locked player position");
            AssertEqual(right.Y,left.Y,"horizontal stream");
            AssertEqual(940f,right.X-left.X,"seals 940 px apart");
            AssertEqual(true,left.X>=f.Left&&right.X<=f.Right,"both seal centres inside the field, at most on a wall");
            AssertEqual(true,p.Target.X>=left.X&&p.Target.X<=right.X,"the captured position lies between the seals");
            float previousFront=right.X;
            for(float t=p.Fire;t<p.End;t+=.25f) {
                var live=CrimsonChoreography.Side(p,t,false);
                if(live.Radius<=0) continue;
                AssertEqual(true,live.Radius<=CrimsonChoreography.SideHalfWidth,"no wider than the band");
                AssertEqual(true,live.A.X+live.Radius<=right.X+.01f,"the round end stops at the right seal centre");
                AssertEqual(true,live.B.X-live.Radius>=left.X-.01f,"and never passes the left seal centre");
                AssertEqual(true,live.B.X<=live.A.X,"flows right to left");
                AssertEqual(true,live.B.X-live.Radius<=previousFront+.01f,"the front only advances leftward");
                previousFront=live.B.X-live.Radius;
            }
            var full=CrimsonChoreography.Side(p,p.Fire+30,false);
            AssertEqual(CrimsonChoreography.SideHalfWidth,full.Radius,"full width between the seals");
            AssertEqual(right.X,full.A.X+full.Radius,"full stream ends exactly at the right seal centre");
            AssertEqual(left.X,full.B.X-full.Radius,"and exactly at the left seal centre");
        }
        var resting=CrimsonChoreography.ClampParticipant(f,f.CenterX,f.Bottom-42,20,42);
        AssertEqual((f.CenterX,f.Bottom-42),resting,"ordinary support floor is not displaced upward");
    }
    [DomainTest("Scarlet crossflow collision is the drawn stream: inside its forecast band, ends sunk in the seals, wall bodies reached")]
    private static void ScarletCrossflowContainment()
    {
        var f=Convergence.Common.Raids.Arena.RaidFieldGeometry.FromGround(8000,6000);
        var p=TechniqueExample(CrimsonTechnique.SideBeams) with {End=656,LastEnd=656};
        Span<CrimsonStroke> strokes=stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach(float x in new[]{f.Left+100,f.Left+300,f.CenterX,f.Right-250,f.Right-100}) foreach(float y in new[]{f.Top+100,f.CenterY,f.Bottom-100}) {
            var plan=p with {Target=new(x,y)};
            var (right,left)=CrimsonChoreography.Seals(plan);
            // Collision (CrimsonTechniqueGeometry.Write, what CrimsonGesture collides and ScarletInk draws) at every live tick.
            for(int tick=plan.Fire;tick<plan.End;tick++) {
                int count=CrimsonTechniqueGeometry.Write(plan,tick,strokes);
                for(int i=0;i<count;i++) {
                    var s=strokes[i];
                    // A 1 px probe just outside the band (beyond a seal centre, above or below) is never hit.
                    foreach(var (px,py) in new[]{(right.X+.6f,right.Y),(left.X-1.6f,left.Y),(s.A.X,right.Y-CrimsonChoreography.SideHalfWidth-1.6f),(s.B.X,right.Y+CrimsonChoreography.SideHalfWidth+.6f)})
                        AssertEqual(false,CrimsonTechniqueGeometry.Intersects(s,px,py,1,1),$"nothing collides outside the forecast band tick={tick-plan.Fire}");
                    // The capsule's extent is inside the seal-to-seal band: the drawn ink (radius + its anti-aliased margin)
                    // ends inside the seals, whose ellipse is 52 px wide either side of the centre at full charge.
                    AssertEqual(true,s.A.X+s.Radius<=right.X+.01f&&s.B.X-s.Radius>=left.X-.01f,"capsule between the seal centres");
                    AssertEqual(true,s.A.X+s.Radius+10<=right.X+.28f*185&&s.B.X-s.Radius-10>=left.X-.28f*185,"ink margin stays inside the seal ellipses");
                }
            }
        }
        // Against a wall the pair slides until a seal centre sits on the wall, so a body pressed into it is still in the stream.
        foreach(bool atLeft in new[]{true,false}) {
            var plan=p with {Target=new(atLeft?f.Left+100:f.Right-100,f.CenterY)};
            var (right,left)=CrimsonChoreography.Seals(plan);
            AssertEqual(atLeft?f.Left:f.Right,atLeft?left.X:right.X,"the wall-side seal centre is on the wall");
            int count=CrimsonTechniqueGeometry.Write(plan,plan.Fire+30,strokes);
            float bodyX=atLeft?f.Left:f.Right-20;
            float reach=0;
            for(float dy=0;dy<=200;dy+=.5f)
                if(ScarletHits(strokes[..count],bodyX,right.Y-21+dy)||ScarletHits(strokes[..count],bodyX,right.Y-21-dy)) reach=dy;
            AssertEqual(true,ScarletHits(strokes[..count],bodyX,right.Y-21),"a body against the wall on the stream's line is hit");
            AssertEqual(true,reach>=92,$"a wall body has to leave the line by {reach} px (161 px in the open)");
            float open=0, middle=(left.X+right.X)*.5f-10;
            for(float dy=0;dy<=200;dy+=.5f) if(ScarletHits(strokes[..count],middle,right.Y-21+dy)) open=dy;
            AssertEqual(true,open>=160,$"in the open the stream is 280 px tall plus the body ({open})");
        }
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
