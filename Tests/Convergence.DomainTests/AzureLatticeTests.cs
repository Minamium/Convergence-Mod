using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Convergence.Content.Encounters.AzureCathedral;
using Convergence.Common.Raids.Arena;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Azure lattice forecasts bounded shuffled full-field lines and immutable codec plans")]
    private static void AzureLatticePlans()
    {
        var field=RaidFieldGeometry.FromGround(8000,6000);
        var fight=Guid.Parse("ef3f1f8e-15c6-4934-9333-1283314fcb6e");
        for(int pattern=0;pattern<2;pattern++)for(int seed=0;seed<60;seed++)
        {
            int born=1000+seed*144;
            var plans=AzureLattice.Create(fight,3,field,born,pattern);
            AssertEqual(true,plans.Length is >= 12 and <= AzureLattice.MaxLines,"bounded visible grid");
            AssertEqual(true,plans.SequenceEqual(AzureLattice.Create(fight,3,field,born,pattern)),"same authority seed identical");
            AssertEqual(true,plans.Select(p=>p.Angle).Distinct().Count()==2,"both axes interleaved");
            int switches=0;
            for(int i=0;i<plans.Length;i++)
            {
                var p=plans[i];
                AssertEqual(born+AzureLattice.Warning+i*AzureLattice.Stagger,p.Fire,"staggered order after complete warning");
                AssertEqual(AzureRules.CutLive,p.End-p.Fire,"same existing slash live window");
                AssertEqual(true,field.ClipAxis(p.X,p.Y,MathF.Cos(p.Angle),MathF.Sin(p.Angle),out float first,out float last),"clipped line spans arena");
                AssertEqual(true,last-first>=120 && last-first<4000,"full-field bounded extent");
                using var m=new MemoryStream();using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))p.Write(w);
                m.Position=0;using var r=new BinaryReader(m);AssertEqual(p,AzureAttackPlan.Read(r)!.Value,"native immutable payload");
                if(i>0 && p.Angle!=plans[i-1].Angle)switches++;
            }
            AssertEqual(true,switches>=2,"not all vertical then all horizontal");
            var next=AzureLattice.Create(fight,3,field,born+144,pattern);
            AssertEqual(false,plans.Select(p=>(p.X,p.Y)).SequenceEqual(next.Select(p=>(p.X,p.Y))),"subsequent grid shifts");
        }
    }
    [DomainTest("Azure lattice cadence brackets Duet chorus and leaves Fury transit completely clear")]
    private static void AzureLatticeSchedule()
    {
        var fight=Guid.NewGuid();var field=RaidFieldGeometry.FromGround(8000,6000);
        for(int cycle=0;cycle<3;cycle++)
        {
            int duet=0,fury=0;int[] byPhrase=new int[6];
            for(int clock=0;clock<AzureRules.CycleTicks;clock++)
            {
                int t=cycle*AzureRules.CycleTicks+clock;
                if(AzureLattice.Due(AzurePhase.Duet,t,out int p))
                {
                    duet++;AssertEqual(0,p,"P1 vertical and horizontal");
                    var lines=AzureLattice.Create(fight,3,field,t,p);
                    if(clock<984)AssertEqual(true,lines[^1].End<cycle*AzureRules.CycleTicks+984,"finish before Stack marker");
                    if(clock>2000 && clock<2424)AssertEqual(true,lines[^1].End<cycle*AzureRules.CycleTicks+2424,"finish before Spread marker");
                }
                if(AzureLattice.Due(AzurePhase.Fury,t,out p))
                {
                    fury++;int phrase=clock/480;byPhrase[phrase]++;
                    AssertEqual(clock%480/144==1?0:1,p,"diagonal orthogonal diagonal");
                    var lines=AzureLattice.Create(fight,3,field,t,p);
                    for(int live=t;live<lines[^1].End+AzureRules.CutResidue;live++)
                        AssertEqual(false,AzureRules.Phrase(live,0) is 1 or 4,"no forecast live or residue during slow transit");
                }
                AssertEqual(false,AzureLattice.Due(AzurePhase.Devouring,t,out _),"no protected transition attacks");
                AssertEqual(false,AzureLattice.Due(AzurePhase.Melting,t,out _),"no ending attacks");
            }
            AssertEqual(4,duet,"four chorus brackets per loop");AssertEqual(12,fury,"three grids per eligible phrase");
            for(int i=0;i<6;i++)AssertEqual(i is 1 or 4?0:3,byPhrase[i],"transit exemption");
        }
    }
    [DomainTest("Azure Stack boundary and enlarged Spread overlap resolve party failure correctly")]
    private static void AzureVerdictBoundaries()
    {
        var zero=Vector2.Zero;
        foreach(int count in new[]{1,2,3,4,8})
        {
            byte mask=(byte)((1<<count)-1);var points=new Vector2[count];
            for(int i=0;i<count;i++)points[i]=new(AzureChorusRules.StackRadius,0);
            AssertEqual(true,AzureChorusRules.Resolve(AzureChorusKind.Stack,zero,points,mask,mask).All(d=>d==0),"inclusive edge succeeds");
            points[^1].X+=.1f;
            var damage=AzureChorusRules.Resolve(AzureChorusKind.Stack,zero,points,mask,mask);
            AssertEqual(true,damage.All(d=>d==(int)Math.Ceiling(900d/count)),"outside means party failure, not falling ice success");
            for(int i=0;i<count;i++)points[i]=new(i*AzureChorusRules.SpreadRadius*2,0);
            AssertEqual(true,AzureChorusRules.Resolve(AzureChorusKind.Spread,zero,points,mask,mask).All(d=>d==0),"touching not overlap");
            if(count>1)
            {
                points[1].X-=.1f;damage=AzureChorusRules.Resolve(AzureChorusKind.Spread,zero,points,mask,mask);
                AssertEqual(900,damage[0],"overlap first");AssertEqual(900,damage[1],"overlap second");
                AssertEqual(true,damage.Skip(2).All(d=>d==0),"unrelated members safe");
            }
        }
    }
    [DomainTest("Azure verdict animation waits for authority and late receipt never invents success")]
    private static void AzureVerdictPresentation()
    {
        var clock=new AzureVerdictClock();
        for(int age=1100;age<1300;age++)AssertEqual(-1f,clock.Sample(false,age,1270,1354),"pending holds charged pose");
        AssertEqual(0f,clock.Sample(true,1300,1270,1354),"late verdict still shows impact");
        AssertEqual(7f,clock.Sample(true,1307,1270,1354),"duplicate never restarts");
        var late=new AzureVerdictClock();
        AssertEqual(24f,late.Sample(true,1330,1270,1354),"very late receipt bounded inside owned recovery");
        var early=new AzureVerdictClock();AssertEqual(-2f,early.Sample(true,1268,1270,1354),"no damage animation before deadline");
        AssertEqual(0f,early.Sample(true,1270,1270,1354),"scheduled start");
        var shortest=new AzureVerdictClock();AssertEqual(0f,shortest.Sample(true,1270,1270,1300),"shortest accepted tail never throws");
    }
    [DomainTest("Azure ending sky title music and punctuation follow whole-chain melting")]
    private static void AzureEndingAlignment()
    {
        AssertEqual(1f,AzureRules.MusicGain(0,2000,AzureStage.Victory,2000+AzureRules.MeltContact),"music holds until actual melt");
        for(int t=0;t<=AzureRules.MeltSettled;t++)
        {
            AssertEqual(1f,AzureRules.EndingSky(AzureStage.Victory,t),"cathedral retained through last segment");
            AssertEqual(0f,AzureRules.VictoryTitle(t),"no premature victory title");
        }
        AssertEqual(1f,AzureRules.Melt(AzureRules.MeltSettled,AzureRules.Segments),"tail gone at settled cue");
        AssertEqual(1f,AzureRules.Melt(AzureRules.VictoryCue,0),"victory sound follows head dissolution");
        AssertEqual(true,AzureRules.MeltEnding-AzureRules.VictoryCue>=210,"3.5 second Victory cue fits before owned cleanup");
        AssertEqual(true,AzureRules.VictoryTitle(AzureRules.MeltSettled+25)>.8f,"settled title hold");
        AssertEqual(0f,AzureRules.EndingSky(AzureStage.Victory,AzureRules.MeltEnding),"no sky after cleanup");
        AssertEqual(0f,AzureRules.EndingSky(AzureStage.Defeat,AzureRules.Ending),"short defeat remains bounded");
    }
}
