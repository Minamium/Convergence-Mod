using System;
using System.IO;
using Convergence.Content.Encounters.AzureCathedral;
using Convergence.Common.Raids.Arena;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static AzureState AzureExample() => new(Guid.NewGuid(), 500, 180, 180+AzureRules.Intro, -1, AzureStage.Countdown,
        new[] { new AzureMember(0, Guid.NewGuid(), true, false) }, 8000, 6000,
        AzureRules.Life(1,false), AzureRules.Life(1,true), AzureRules.Life(1,false), AzureRules.Life(1,true), 7, false);
    private static AzureState? AzureRead(AzureState s)
    {
        using var memory=new MemoryStream(); using(var writer=new BinaryWriter(memory,System.Text.Encoding.UTF8,true))s.WriteEnvelope(writer);
        memory.Position=0; using var reader=new BinaryReader(memory); return AzureState.ReadEnvelope(reader);
    }
    [DomainTest("Azure default native actor envelope is safe before ownership and on despawn")]
    private static void AzureEmpty()
    {
        AssertEqual(true,AzureRead(default) is null,"unowned actor");
        bool bad=false;try {using var r=new BinaryReader(new MemoryStream(new byte[]{2}));AzureState.ReadEnvelope(r);}catch(InvalidDataException){bad=true;}
        AssertEqual(true,bad,"strict presence");
    }
    [DomainTest("Azure bounded roster and clocks roundtrip for 1 through 8 connected players")]
    private static void AzureRosterCodec()
    {
        for(int n=1;n<=8;n++)
        {
            var members=new AzureMember[n];for(byte i=0;i<n;i++)members[i]=new(i,Guid.NewGuid(),i%2==0,false);
            var source=AzureExample() with{Members=members,GirlMax=AzureRules.Life(n,false),WormMax=AzureRules.Life(n,true)};
            var result=AzureRead(source)!.Value;
            AssertEqual(n,result.Members.Length,"bounded roster");AssertEqual(source.Fight,result.Fight,"fight retained");
            AssertEqual(source.Members[^1],result.Members[^1],"connection retained");
        }
        foreach(var bad in new[]{AzureExample() with{Members=Array.Empty<AzureMember>()},AzureExample() with{UnlockAt=180+AzureRules.Intro+1},AzureExample() with{WormSlot=200}})
        {bool rejected=false;try{AzureRead(bad);}catch(InvalidDataException){rejected=true;}AssertEqual(true,rejected,"invalid state rejected");}
    }
    [DomainTest("Azure stale snapshots cannot reopen terminal state or replace another fight")]
    private static void AzureConvergence()
    {
        var a=AzureExample();
        AssertEqual(false,(a with{Age=499}).CanReplace(a),"stale age");
        AssertEqual(false,(a with{Fight=Guid.NewGuid()}).CanReplace(a),"different fight");
        AssertEqual(false,(a with{MusicStart=181}).CanReplace(a),"music epoch");
        AssertEqual(false,(a with{WormSlot=8}).CanReplace(a),"actor binding retained");
        var ended=a with{Age=1000,Stage=AzureStage.Victory,EndAt=1000};
        AssertEqual(false,(ended with{Stage=AzureStage.Performance,Age=1001}).CanReplace(ended),"cannot revive terminal");
        AssertEqual(true,(ended with{Age=1001}).CanReplace(ended),"terminal converges");
    }
    [DomainTest("Azure hazard codec bounds warning time geometry and lifetime")]
    private static void AzureHazardCodec()
    {
        var h=new AzureAttackPlan(Guid.NewGuid(),2,AzureAttackKind.MouthBeam,700,796,1036,8000,6000,.5f,3200,66,360);
        AzureAttackPlan? Read(AzureAttackPlan value)
        {using var m=new MemoryStream();using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))value.Write(w);m.Position=0;using var r=new BinaryReader(m);return AzureAttackPlan.Read(r);}
        AssertEqual(h,Read(h)!.Value,"immutable geometry");
        foreach(var bad in new[]{h with{X=float.NaN},h with{Width=float.PositiveInfinity},h with{Fire=710},h with{End=1100},h with{Girl=200},h with{Kind=(AzureAttackKind)9}})
        {bool rejected=false;try{Read(bad);}catch(InvalidDataException){rejected=true;}AssertEqual(true,rejected,"invalid hazard");}
        AssertEqual(true,Read(default) is null,"unowned projectile");
    }
    [DomainTest("Azure full beam growth and contraction has no invisible damaging onset or end")]
    private static void AzureBeamEnvelope()
    {
        AssertEqual(0f,AzureRules.Envelope(-1,240),"before");AssertEqual(0f,AzureRules.Envelope(0,240),"initial needle");
        AssertEqual(1f,AzureRules.Envelope(9,240),"fully grown");AssertEqual(0f,AzureRules.Envelope(240,240),"end");
        float previous=0;
        for(float t=0;t<=240;t+=.125f)
        {float x=AzureRules.Envelope(t,240);AssertEqual(true,x>=0 && x<=1 && Math.Abs(x-previous)<.03,"continuous eighth-tick curve");previous=x;}
    }
    [DomainTest("Azure solo scales without changing other encounters and floor has no teleport offset")]
    private static void AzureScalingAndFloor()
    {
        AssertEqual(7200000,AzureRules.Life(1,true)+AzureRules.Life(1,false),"solo total");
        AssertEqual(34500000,AzureRules.Life(8,true)+AzureRules.Life(8,false),"eight total");
        var f=RaidFieldGeometry.FromGround(8000,6000);var p=AzureRules.Clamp(f,8000,6000-42,20,42);
        AssertEqual(5958f,p.Y,"floor aligned");
        AssertEqual(false,AzureRules.Completed(0,1),"both actors required");AssertEqual(true,AzureRules.Completed(0,0),"both defeated");
        AssertEqual(0,AzureRules.Phrase(1000,1000),"first charge");AssertEqual(1,AzureRules.Phrase(1480,1000),"beam phrase");
    }
    [DomainTest("Azure native defeat is idempotent and a retired chain cannot abort the survivor")]
    private static void AzureSurvivor()
    {
        foreach(bool first in new[]{false,true})
        {
            var d=new AzureDefeats();AssertEqual(false,d.RequiresChain(false),"unsummoned intro");
            AssertEqual(true,d.RequiresChain(true),"living chain required");AssertEqual(true,d.Mark(first),"first defeat");
            for(int i=0;i<9;i++)AssertEqual(false,d.Mark(first),"duplicate native CheckDead suppressed");
            AssertEqual(!first,d.RequiresChain(true),"worm required iff alive");
            AssertEqual(false,d.Girl && d.Worm,"surviving actor keeps fight active");
            AssertEqual(true,d.Mark(!first),"other actor can die later");AssertEqual(true,d.Girl && d.Worm,"both required to win");
        }
    }
    [DomainTest("Azure chorus resolves living announced members only and solo is playable")]
    private static void AzureChorusDamage()
    {
        var zero=System.Numerics.Vector2.Zero;
        var points=new[]{zero,new System.Numerics.Vector2(700,0),new System.Numerics.Vector2(0,10)};
        var stack=AzureChorusRules.Resolve(AzureChorusKind.Stack,zero,points,7,7);
        AssertEqual(300,stack[0],"one third missing");AssertEqual(300,stack[1],"all living share failure");
        var death=AzureChorusRules.Resolve(AzureChorusKind.Stack,zero,points,7,5);
        AssertEqual(0,death[0],"dead player not required");AssertEqual(0,death[1],"dead not damaged");
        var spread=AzureChorusRules.Resolve(AzureChorusKind.Spread,zero,points,7,7);
        AssertEqual(900,spread[0],"overlapping first");AssertEqual(0,spread[1],"isolated safe");AssertEqual(900,spread[2],"overlapping second");
        foreach(var kind in new[]{AzureChorusKind.Stack,AzureChorusKind.Spread})
            AssertEqual(0,AzureChorusRules.Resolve(kind,zero,new[]{zero},1,1)[0],"solo succeeds");
        AssertEqual(0,AzureChorusRules.Resolve(AzureChorusKind.Spread,zero,points,1,7)[0],"unannounced outsiders excluded");
        AssertEqual(false,AzureChorusRules.VerdictCanReplace(true,1,false,0),"cannot clear verdict");
        AssertEqual(false,AzureChorusRules.VerdictCanReplace(true,1,true,2),"cannot alter verdict");
        AssertEqual(true,AzureChorusRules.VerdictCanReplace(true,1,true,1),"duplicate converges");
    }
    [DomainTest("Azure chorus immutable envelope bounds deadline geometry and membership")]
    private static void AzureChorusCodec()
    {
        AzureChorusPlan? Read(AzureChorusPlan value)
        {using var m=new MemoryStream();using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))value.Write(w);m.Position=0;using var r=new BinaryReader(m);return AzureChorusPlan.Read(r);}
        var p=new AzureChorusPlan(Guid.NewGuid(),4,AzureChorusKind.Stack,3,1000,1270,1354,new(8000,6000));
        AssertEqual(p,Read(p)!.Value,"roundtrip");AssertEqual(true,Read(default) is null,"unowned safe");
        foreach(var bad in new[]{p with{Members=0},p with{Girl=200},p with{Fire=1000},p with{End=2000},p with{Center=new(float.NaN,0)}})
        {bool rejected=false;try{Read(bad);}catch(InvalidDataException){rejected=true;}AssertEqual(true,rejected,"invalid chorus rejected");}
    }
    [DomainTest("Azure flight has bounded turning and continuous broad windup endpoints")]
    private static void AzureBroadFlight()
    {
        var a=new System.Numerics.Vector2(1500,-200);var b=new System.Numerics.Vector2(1300,-1000);
        var c=new System.Numerics.Vector2(-2300,-300);var d=new System.Numerics.Vector2(-1660,0);
        AssertEqual(a,AzureFlight.Curve(a,b,c,d,0),"curve start");AssertEqual(d,AzureFlight.Curve(a,b,c,d,1),"curve end");
        var previous=a;
        for(int tick=1;tick<=100;tick++)
        {var current=AzureFlight.Curve(a,b,c,d,tick/100f);AssertEqual(true,System.Numerics.Vector2.Distance(previous,current)<64,"no snap across windup");previous=current;}
        var v=new System.Numerics.Vector2(48,0);var next=AzureFlight.Steer(v,new(-1000,0),34);
        AssertEqual(true,Math.Abs(Math.Atan2(next.Y,next.X))<=.04501,"large turn cannot oscillate instantly");
        AssertEqual(true,AzureRules.ChorusPhrase(2) && AzureRules.ChorusPhrase(5),"dedicated stack/spread windows");
    }
}
