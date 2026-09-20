using System;
using System.IO;
using Convergence.Content.Encounters.AzureCathedral;
using Convergence.Common.Raids.Arena;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Azure devouring requires both HP gates in either order and refills only once")]
    private static void AzureDevouringGates()
    {
        for(int members=1;members<=8;members++)
        {
            int maximum=AzureRules.Life(members,true),floor=AzureRules.WormFloor(maximum);
            AssertEqual(maximum/5,floor,"twenty percent");
            AssertEqual(false,AzureRules.CanDevour(AzurePhase.Duet,1,floor,maximum),"girl still fighting");
            AssertEqual(false,AzureRules.CanDevour(AzurePhase.Duet,0,floor+1,maximum),"worm not weakened");
            AssertEqual(true,AzureRules.CanDevour(AzurePhase.Duet,0,floor,maximum),"both gates");
            AssertEqual(false,AzureRules.CanDevour(AzurePhase.Fury,0,0,maximum),"cannot replay consumption");
            AssertEqual(false,AzureRules.CanRefill(AzurePhase.Devouring,2000,2000+AzureRules.Devouring-1),"protected scene complete first");
            AssertEqual(true,AzureRules.CanRefill(AzurePhase.Devouring,2000,2000+AzureRules.Devouring),"single refill deadline");
            AssertEqual(false,AzureRules.CanRefill(AzurePhase.Fury,2000,9999),"duplicate update no healing");
        }
    }
    [DomainTest("Azure phase and full projection reject regression malformed epochs and every truncated prefix")]
    private static void AzurePhaseCodec()
    {
        var a=AzureExample() with{Stage=AzureStage.Performance,Age=2100,GirlLife=0,WormLife=AzureRules.WormFloor(AzureRules.Life(1,true)),Phase=AzurePhase.Devouring,PhaseAt=2000};
        var b=a with{Age=2480,Phase=AzurePhase.Fury,PhaseAt=2480,WormLife=a.WormMax,Enraged=true};
        var c=b with{Age=3000,Phase=AzurePhase.Melting,PhaseAt=3000,Stage=AzureStage.Victory,EndAt=3000,WormLife=0};
        AssertEqual(true,b.CanReplace(a),"refill advances phase not old life");AssertEqual(false,a.CanReplace(b),"no old phase");
        AssertEqual(false,(b with{Age=2500,PhaseAt=2490}).CanReplace(b),"no clock restart");
        AssertEqual(false,(b with{Age=2500,GirlLife=1}).CanReplace(b),"no stale resurrection");
        AssertEqual(false,(b with{Age=2501,WormLife=b.WormMax}).CanReplace(b with{Age=2500,WormLife=b.WormMax-1}),"no duplicate refill");
        AssertEqual(true,c.CanReplace(b),"committed victory retained through melt");
        foreach(var state in new[]{a,b,c})
        {
            AssertEqual(state.Phase,AzureRead(state)!.Value.Phase,"roundtrip");
            using var m=new MemoryStream();using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))state.WriteEnvelope(w);
            byte[] bytes=m.ToArray();for(int count=0;count<bytes.Length;count++)
            {bool rejected=false;try{using var r=new BinaryReader(new MemoryStream(bytes,0,count));AzureState.ReadEnvelope(r);}catch(Exception e)when(e is EndOfStreamException or InvalidDataException){rejected=true;}AssertEqual(true,rejected,"truncated projection");}
        }
        foreach(var invalid in new[]{a with{PhaseAt=2200},a with{GirlLife=1},b with{Enraged=false},c with{Stage=AzureStage.Performance},a with{Phase=(AzurePhase)9}})
        {bool bad=false;try{AzureRead(invalid);}catch(InvalidDataException){bad=true;}AssertEqual(true,bad,"invalid phase rejected");}
    }
    [DomainTest("Azure fast braking slow consumption and melting are continuous and bounded")]
    private static void AzureCeremonyCurves()
    {
        float prior=0;for(float t=0;t<=AzureRules.Devouring;t+=.125f)
        {float p=AzureRules.DevourTravel(t);AssertEqual(true,p>=prior && p-prior<.004f,"continuous monotone approach");prior=p;}
        AssertEqual(.86f,AzureRules.DevourTravel(AzureRules.DevourSlow),"slow approach still has readable distance");
        AssertEqual(true,AzureRules.Silhouette(AzureRules.DevourContact),"contact only");
        AssertEqual(false,AzureRules.Silhouette(AzureRules.DevourContact+14),"no persistent blackout");
        for(int i=0;i<=AzureRules.Segments;i++)
        {AssertEqual(0f,AzureRules.Melt(0,i),"whole body on arrival");AssertEqual(1f,AzureRules.Melt(AzureRules.MeltEnding,i),"every segment gone before cleanup");}
        AssertEqual(0f,AzureRules.MusicGain(-1,-1,AzureStage.Ready,200),"silent ready");
        AssertEqual(1f,AzureRules.MusicGain(100,-1,AzureStage.Performance,400),"full battle mix");
        AssertEqual(0f,AzureRules.MusicGain(100,1000,AzureStage.Victory,1420),"fade complete before native handoff");
        float gain=1;for(int i=1000;i<=1420;i++){float next=AzureRules.MusicGain(100,1000,AzureStage.Victory,i);AssertEqual(true,next<=gain,"no ending swell");gain=next;}
    }
    [DomainTest("Azure frost volley has a bounded short warning and recipient while Liora throat stays inside the jet")]
    private static void AzureVolleyAndBell()
    {
        var p=new AzureAttackPlan(Guid.NewGuid(),3,AzureAttackKind.FrostBolt,1000,1024,1194,8000,6000,.5f,6000,13,300,254,199);
        using var m=new MemoryStream();using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))p.Write(w);m.Position=0;using var r=new BinaryReader(m);
        AssertEqual(p,AzureAttackPlan.Read(r)!.Value,"full volley plan");
        foreach(float radius in new[]{.1f,3f,30f,66f})
            for(float d=0;d<=320;d+=2)
            {float width=AzureRules.ThroatRadius(d,radius);AssertEqual(true,width>0 && width<=radius+1e-4,"collision follows visible bell");}
        AssertEqual(66f,AzureRules.ThroatRadius(260,66),"full jet downstream");
    }
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
        AssertEqual(2640000,AzureRules.Life(1,true)+AzureRules.Life(1,false),"solo total");
        AssertEqual(12650000,AzureRules.Life(8,true)+AzureRules.Life(8,false),"eight total");
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
    [DomainTest("Azure worm HP is one twentieth and Fury shares all parts while Duet stays head-only")]
    private static void AzureFuryVulnerability()
    {
        for(int n=1;n<=8;n++)
        {
            int hp=AzureRules.Life(n,true);
            AssertEqual((4800000+(n-1)*2600000)/20,hp,"worm budget only");
            AssertEqual(2400000+(n-1)*1300000,AzureRules.Life(n,false),"Liora unchanged");
            for(int part=0;part<=AzureRules.Segments;part++)
            {
                AssertEqual(part==0,AzureRules.WormDamageable(AzurePhase.Duet,part,true,hp,hp),"duet armor");
                AssertEqual(true,AzureRules.WormDamageable(AzurePhase.Fury,part,true,hp,hp),"body and tail exposed");
                AssertEqual(false,AzureRules.WormDamageable(AzurePhase.Duet,part,true,AzureRules.WormFloor(hp),hp),"floor stops incoming hits");
                foreach(var phase in new[]{AzurePhase.Duet,AzurePhase.Devouring,AzurePhase.Fury,AzurePhase.Melting})
                    AssertEqual(false,AzureRules.WormDamageable(phase,part,false,hp,hp),"protected/ended scene");
            }
        }
    }
    [DomainTest("Azure diagonal flight crosses slowly without stopping and consumption stages away from Liora")]
    private static void AzureSlowCrossingAndBite()
    {
        var a=new System.Numerics.Vector2(-1650,-620);var b=new System.Numerics.Vector2(1650,650);
        for(int i=AzureRules.VolleyApproach;i<AzureRules.VolleyApproach+AzureRules.VolleyTransit;i++)
        {
            float step=System.Numerics.Vector2.Distance(System.Numerics.Vector2.Lerp(a,b,AzureRules.VolleyProgress(i)),System.Numerics.Vector2.Lerp(a,b,AzureRules.VolleyProgress(i+1)));
            AssertEqual(true,step>8 && step<11,"six-second passage never freezes");
        }
        AssertEqual(true,AzureRules.VolleyFire-AzureRules.VolleyWarning>AzureRules.VolleyApproach && AzureRules.VolleyFire<AzureRules.VolleyApproach+AzureRules.VolleyTransit,"fire while crossing");
        var center=System.Numerics.Vector2.Zero;
        foreach(var from in new[]{new System.Numerics.Vector2(1,0),new System.Numerics.Vector2(-50,-20),new System.Numerics.Vector2(1800,-800)})
        {
            var velocity=new System.Numerics.Vector2(35,15);
            var start=AzureFlight.DevourPosition(from,velocity,center,AzureRules.DevourRush);
            var slow=AzureFlight.DevourPosition(from,velocity,center,AzureRules.DevourSlow);
            var contact=AzureFlight.DevourPosition(from,velocity,center,AzureRules.DevourContact);
            AssertEqual(true,start.Length()>2200,"even a near starting head first retreats");
            AssertEqual(true,slow.Length()-AzureRules.MouthReach>260,"visible gap at time dilation");
            AssertEqual(true,Math.Abs(contact.Length()-AzureRules.MouthReach)<.01,"visible mouth touches girl at silhouette");
            var previous=from;
            for(float t=.125f;t<=AzureRules.Devouring;t+=.125f)
            {
                var p=AzureFlight.DevourPosition(from,velocity,center,t);
                AssertEqual(true,System.Numerics.Vector2.Distance(p,previous)<6,"continuous retreat/rush/slow/overrun");previous=p;
            }
        }
        AssertEqual(true,AzureRules.JawOpening(AzureRules.DevourSlow)>.99,"open through slow approach");
        AssertEqual(0f,AzureRules.JawOpening(AzureRules.DevourContact+10),"closed just after bite");
        var staged=AzureFlight.DevourStaging(new(1,0),center);
        var stationary=AzureFlight.DevourPosition(staged,System.Numerics.Vector2.Zero,center,30);
        AssertEqual(true,float.IsFinite(stationary.X)&&float.IsFinite(stationary.Y),"already staged stationary head has a finite retreat tangent");
    }
    [DomainTest("Azure cut and moving segment emitter codecs retain bounded targets and harmless residue")]
    private static void AzureCutsAndEmitters()
    {
        AzureAttackPlan? Read(AzureAttackPlan p)
        {using var m=new MemoryStream();using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))p.Write(w);m.Position=0;using var r=new BinaryReader(m);return AzureAttackPlan.Read(r);}
        var cut=new AzureAttackPlan(Guid.NewGuid(),2,AzureAttackKind.GlacialCut,1000,1060,1072,8000,6000,.62f,4000,7,340);
        AssertEqual(cut,Read(cut)!.Value,"cut replay");
        var bolt=cut with{Kind=AzureAttackKind.FrostBolt,Fire=1024,End=1194,Emitter=199,Target=254,Length=10000};
        AssertEqual(bolt,Read(bolt)!.Value,"live native emitter binding");
        foreach(var bad in new[]{bolt with{Emitter=-1},bolt with{Emitter=200},bolt with{Target=255},cut with{Emitter=0},cut with{Length=5000}})
        {bool rejected=false;try{Read(bad);}catch(InvalidDataException){rejected=true;}AssertEqual(true,rejected,"bounded identity");}
        AssertEqual(0f,AzureRules.CutWidth(0),"zero-width initial emergence");
        AssertEqual(1f,AzureRules.CutReach(2),"connected two-tick slash sweep");
        AssertEqual(0f,AzureRules.CutWidth(AzureRules.CutLive),"smoke residue never damages");
    }
}
