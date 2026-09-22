using System;
using System.IO;
using Convergence.Content.Encounters.CrimsonFoundry;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Scarlet clusters keep every swept carrier inside its frozen forecast and field")]
    private static void ScarletClusterGeometry()
    {
        Span<CrimsonStroke> lanes=stackalloc CrimsonStroke[CrimsonClusters.Count];
        for (int phrase=1;phrase<=12;phrase++)
        {
            var p=TechniqueExample(CrimsonTechnique.ClusterVolley) with { Phrase=phrase, End=696, LastEnd=696 };
            p.Validate();
            int large=0,medium=0,small=0;
            for (int i=0;i<CrimsonClusters.Count;i++)
            {
                var ray=CrimsonClusters.Ray(p,i);
                if(ray.Radius==58)large++;else if(ray.Radius==32)medium++;else if(ray.Radius==16)small++;
                var guide=new CrimsonStroke(ray.Start,ray.Start+ray.Direction*ray.Length,ray.Radius);
                AssertEqual(true,ray.Length>0 && ray.Start.Finite && guide.B.Finite,"finite lanes");
                AssertEqual(true,guide.B.X-ray.Radius>=p.Field.Left-.01f && guide.B.X+ray.Radius<=p.Field.Right+.01f
                    && guide.B.Y-ray.Radius>=p.Field.Top-.01f && guide.B.Y+ray.Radius<=p.Field.Bottom+.01f,"exit stays inside arena");
                for(float age=p.Fire;age<p.End;age+=.5f)
                {
                    var head=CrimsonClusters.Carrier(p,i,age);
                    AssertEqual(true,head.Radius<=ray.Radius,"never wider than forecast");
                    if(head.Radius<=0)continue;
                    var normal=new CrimsonPoint(-ray.Direction.Y,ray.Direction.X);
                    for(int sign=-1;sign<=1;sign+=2)
                    {
                        var edge=head.B+normal*(sign*head.Radius);
                        AssertEqual(true,CrimsonTechniqueGeometry.Intersects(guide,edge.X-.1f,edge.Y-.1f,.2f,.2f),"forecast contains complete moving head");
                    }
                    AssertEqual(true,(head.B-head.A).LengthSquared<=ray.Speed*ray.Speed+.1f,"no teleport jump/tunnelling step");
                }
                AssertEqual(0f,CrimsonClusters.Carrier(p,i,p.Fire).Radius,"harmless ignition");
                AssertEqual(0f,CrimsonClusters.Carrier(p,i,p.End).Radius,"no lingering damage");
            }
            AssertEqual("5/10/10",$"{large}/{medium}/{small}","three sizes in five bouquets");
            // Guaranteed route halfway between two neighbouring bouquets near
            // the source: a full player footprint fits, not just a point.
            var gap=CrimsonClusters.Emitter(p.Field)+CrimsonPoint.Polar((p.Phrase%11)*.137f+MathF.PI/5,400);
            int count=CrimsonClusters.Write(p,p.Fire,lanes,true);
            for(int i=0;i<count;i++)AssertEqual(false,CrimsonTechniqueGeometry.Intersects(lanes[i],gap.X-12,gap.Y-23,24,46),"player-sized announced corridor");
        }
    }
    [DomainTest("Scarlet cluster codec bounds its long flight and cycle waits for it while next bar overlaps")]
    private static void ScarletClusterTiming()
    {
        var score=ScarletRecordedScore();
        var cycle=new CrimsonActCycle();int finish=0;
        for(int serial=1;serial<=12;serial++)
        {
            var rhythm=CrimsonChoreography.Create(score,8000+serial*250,serial,true);
            var hit=rhythm.Hits[4];
            int end=CrimsonEnsemble.NoteEnd(CrimsonTechnique.ClusterVolley,hit);
            var next=CrimsonChoreography.Create(score,rhythm.End,serial+1,true);
            AssertEqual(true,end>next.Hits[0].Warning,"flight can cross next warning");
            AssertEqual(CrimsonClusters.FlightTicks,end-hit.Fire,"fixed complete flight");
            AssertEqual(true,hit.Fire-hit.Warning>=CrimsonRhythm.MinimumWarningTicks,"two measured warning beats");
            for(int note=0;note<9;note++)
            {
                var h=rhythm.Hits[note%5];
                var technique=CrimsonEnsemble.Technique(3,serial,note%5,note>=5);
                bool aimed=technique is CrimsonTechnique.TrackingBeam or CrimsonTechnique.SpatialRift;
                var plan=TechniqueExample(technique) with {
                    Source=3,Step=(byte)note,Steps=9,Pulse=(byte)note,Phrase=serial,
                    Begin=rhythm.Start-30,Born=h.Warning,Fire=h.Fire,End=CrimsonEnsemble.NoteEnd(technique,h),
                    FirstFire=rhythm.Hits[0].Fire,LastEnd=end,
                    TargetSlot=aimed?(short)0:(short)-1,TargetConnection=aimed?Guid.NewGuid():Guid.Empty
                };
                plan.Validate();
            }
            finish=end+CrimsonRhythm.LeaseTicks;cycle.Admit(rhythm.End,finish);
        }
        AssertEqual(false,cycle.TryComplete(finish-1,false),"no completion while previous carriers own their tail");
        AssertEqual(true,cycle.TryComplete(finish,false),"completion after final flight lease");
        var p=TechniqueExample(CrimsonTechnique.ClusterVolley) with {End=696,LastEnd=696};
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);using var reader=new BinaryReader(stream);
        p.Write(writer);stream.Position=0;AssertEqual(p,CrimsonGesturePlan.Read(reader),"cluster round trip");
        foreach(var bad in new[]{p with{Source=2},p with{End=697,LastEnd=697},p with{TargetSlot=0,TargetConnection=Guid.NewGuid()}})
        {
            bool rejected=false;try{bad.Validate();}catch(InvalidDataException){rejected=true;}
            AssertEqual(true,rejected,"reject ownership/oversized flight/retarget identity");
        }
    }
}
