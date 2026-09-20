using System;
using System.IO;
using Convergence.Content.Encounters.AzureCathedral;
using Convergence.Common.Raids.Arena;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static AzureState AzureExample() => new(Guid.NewGuid(), 500, 180, 660, -1, AzureStage.Countdown,
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
        foreach(var bad in new[]{AzureExample() with{Members=Array.Empty<AzureMember>()},AzureExample() with{UnlockAt=661},AzureExample() with{WormSlot=200}})
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
        AssertEqual(0,AzureRules.Phrase(1000,1000),"first charge");AssertEqual(1,AzureRules.Phrase(1420,1000),"beam phrase");
    }
}
