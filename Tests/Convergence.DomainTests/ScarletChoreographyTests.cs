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
            AssertEqual(3,p.Hits.Count,"two basic releases and one paired release");
            AssertEqual((int)Math.Round(b[4]),p.Hits[2].Warning,"four beats complete");
            AssertEqual((int)Math.Round(b[6]),p.Hits[2].Fire,"two beat charge");
            AssertEqual((int)Math.Round(b[8]),p.Hits[2].End,"two beat amplification and collapse");
            AssertEqual(p.End,p.Hits[2].End,"next bar starts without extra dead time");
        }
        AssertEqual(CrimsonTechnique.SpatialRift,CrimsonChoreography.Technique(1,2,0),"Act II alternates spatial cuts");
        AssertEqual(CrimsonTechnique.TrackingBeam,CrimsonChoreography.Technique(1,3,0),"Act II retains pursuit contrast");
        AssertEqual(CrimsonTechnique.SideBeams,CrimsonChoreography.Technique(1,2,2),"every phrase closes with broad sides");
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
    [DomainTest("Scarlet orthogonal diagonal lead forecasts and paired curtains leave a complete body safe")]
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
            var left=CrimsonChoreography.Side(p,p.Fire,true,-1);var right=CrimsonChoreography.Side(p,p.Fire,true,1);
            AssertEqual(p.Target.Y,left.A.Y,"seal beside locked player position");AssertEqual(f.Bottom,left.B.Y,"bottom edge");
            AssertEqual(f.Top,CrimsonChoreography.Side(p,p.Fire,true,-1,true).B.Y,"upper branch reaches top edge");
            AssertEqual(true,left.A.X-left.Radius>=f.Left&&right.A.X+right.Radius<=f.Right,"not clipped outside");
            AssertEqual(true,right.A.X-right.Radius-(left.A.X+left.Radius)>=192,"player-sized center refuge");
            for(float t=p.Fire;t<p.End;t+=.5f) {
                var live=CrimsonChoreography.Side(p,t,false,-1);
                AssertEqual(true,live.Radius<=left.Radius&&live.Radius>=0,"shared visible collision envelope");
            }
        }
        var resting=CrimsonChoreography.ClampParticipant(f,f.CenterX,f.Bottom-42,20,42);
        AssertEqual((f.CenterX,f.Bottom-42),resting,"ordinary support floor is not displaced upward");
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
}
