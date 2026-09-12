using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Doll companion reserves ten slots and cannot duplicate itself")]
    private static void DollSummonBudget()
    {
        for(int slots=0;slots<10;slots++) AssertEqual(false,DollCompanionRules.CanSummon(slots,0),"no free slot grant");
        AssertEqual(true,DollCompanionRules.CanSummon(10,0),"exact capacity");
        AssertEqual(false,DollCompanionRules.CanSummon(30,1),"one companion");
        AssertEqual(RitualArmamentRules.Damage(RitualArmamentKind.Summon)*10,DollCompanionRules.Damage,"ten-slot item budget");
    }
    [DomainTest("Doll companion has bounded cels and one three-shot charge verdict score")]
    private static void DollMotionScore()
    {
        int shots=0;
        var seen=new HashSet<int>();
        for(int tick=0;tick<DollCompanionRules.Cycle;tick++)
        {
            if(DollCompanionRules.NeedleAt(tick)) {shots++;AssertEqual(true,tick<DollCompanionRules.Verdict,"notes precede verdict");}
            int frame=DollCompanionRules.Frame(tick,false,-1,tick);
            seen.Add(frame);
            AssertEqual(true,frame>=0&&frame<DollCompanionRules.FrameCount,"attack cel bounds");
        }
        AssertEqual(3,shots,"three shots, not a shot every rendered frame");
        AssertEqual(32,DollCompanionRules.Frame(DollCompanionRules.Verdict,true,20,0),"release cel at damaging beat");
        for(int tick=0;tick<2520;tick++)
        {
            int idle=DollCompanionRules.Frame(0,false,-1,tick);
            AssertEqual(true,idle>=0&&idle<12,"idle reuses NPC cels");
            int flight=DollCompanionRules.Frame(0,true,-1,tick);
            AssertEqual(true,flight>=20&&flight<24,"four authored floating cels");
            int walk=DollCompanionRules.Frame(0,false,tick%40,tick);
            seen.Add(idle);seen.Add(flight);seen.Add(walk);
            AssertEqual(true,walk>=12&&walk<20,"eight authored walk cels");
        }
        AssertEqual(DollCompanionRules.FrameCount,seen.Count,"all authored cels are reachable");
    }
}
