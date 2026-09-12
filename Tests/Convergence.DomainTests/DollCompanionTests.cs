using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Doll companion follows mounted and hovering owners in broom mode")]
    private static void DollOwnerLocomotion()
    {
        AssertEqual(true, DollCompanionRules.OwnerRequiresBroom(true, true, 0, 1), "stationary ground mount also rides");
        AssertEqual(true, DollCompanionRules.OwnerRequiresBroom(true, false, 0, 1), "horizontal flying mount");
        AssertEqual(true, DollCompanionRules.OwnerRequiresBroom(false, false, 0, 1), "hovering and jump apex are not ground");
        AssertEqual(true, DollCompanionRules.OwnerRequiresBroom(false, true, -7, 1), "takeoff before feet clear ground");
        AssertEqual(true, DollCompanionRules.OwnerRequiresBroom(false, false, 8, 1), "dismount while falling");
        AssertEqual(true, DollCompanionRules.OwnerRequiresBroom(false, true, 0, -1), "inverted gravity stays airborne");
        AssertEqual(false, DollCompanionRules.OwnerRequiresBroom(false, true, 0, 1), "actual supported unmounted landing");
        int grounded = 0;
        for (int tick = 1; tick <= DollCompanionRules.LandingConfirmTicks; tick++)
        {
            grounded = DollCompanionRules.GroundedTicks(false, grounded);
            AssertEqual(tick < DollCompanionRules.LandingConfirmTicks,
                DollCompanionRules.UseBroom(true, false, grounded, false, true), "stable landing hysteresis");
        }
        AssertEqual(true, DollCompanionRules.UseBroom(false, true, grounded, false, true), "mount takes priority immediately");
        AssertEqual(true, DollCompanionRules.UseBroom(true, false, grounded, false, false), "never enable collision inside tiles");
        AssertEqual(true, DollCompanionRules.UseBroom(false, false, grounded, true, true), "ground-owner catchup retained");
        AssertEqual(false, DollCompanionRules.UseBroom(false, false, grounded, false, true), "ordinary ground walking retained");
        AssertEqual(0, DollCompanionRules.GroundedTicks(true, grounded), "new flight resets landing confirmation");
    }

    [DomainTest("Doll companion reserves ten slots and cannot duplicate itself")]
    private static void DollSummonBudget()
    {
        for(int slots=0;slots<10;slots++) AssertEqual(false,DollCompanionRules.CanSummon(slots,0),"no free slot grant");
        AssertEqual(true,DollCompanionRules.CanSummon(10,0),"exact capacity");
        AssertEqual(false,DollCompanionRules.CanSummon(30,1),"one companion");
        AssertEqual(RitualArmamentRules.Damage(RitualArmamentKind.Summon)*10,DollCompanionRules.Damage,"ten-slot item budget");
    }
    [DomainTest("Doll companion has bounded ground and broom cels and one charge beam score")]
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
        var broomSeen = new HashSet<int>();
        for (int tick = 0; tick < 2520; tick++)
        {
            int flight = DollCompanionRules.BroomFrame(0, tick);
            AssertEqual(true, flight >= 0 && flight < 8, "eight authored seated flight cels");
            broomSeen.Add(flight);
        }
        for (int tick = 1; tick < DollCompanionRules.BroomRecoveryEnd; tick++)
        {
            int cast = DollCompanionRules.BroomFrame(tick, tick);
            AssertEqual(true, cast >= 8 && cast < DollCompanionRules.BroomFrames, "eight authored broom casting cels");
            broomSeen.Add(cast);
        }
        AssertEqual(DollCompanionRules.BroomFrames, broomSeen.Count, "all sixteen broom cels reachable");
        AssertEqual(11, DollCompanionRules.BroomFrame(DollCompanionRules.Verdict, 0), "beam is cast by the pointing hand, not seated-idle cel");
        for (int tick = DollCompanionRules.Verdict; tick < DollCompanionRules.Verdict + DollCompanionRules.BeamTicks; tick++)
            AssertEqual(true, DollCompanionRules.BroomFrame(tick, tick) is 10 or 11, "hand stays extended for sustained beam");
    }

    [DomainTest("Doll companion beam uses one bounded collision and material envelope")]
    private static void DollBeamEnvelope()
    {
        AssertEqual(false, DollCompanionRules.BeamLive(-.01f), "no prefire damage");
        AssertEqual(0f, DollCompanionRules.BeamScale(-.01f), "no prefire attack surface");
        AssertEqual(true, DollCompanionRules.BeamLive(0), "beam first active tick");
        AssertEqual(true, DollCompanionRules.BeamScale(0) > .5f, "explosive opening, not a slow fade-in");
        AssertEqual(1f, DollCompanionRules.BeamScale(4), "full damage geometry matches beam within five ticks");
        AssertEqual(false, DollCompanionRules.BeamLive(DollCompanionRules.BeamTicks), "afterglow cannot damage");
        AssertEqual(0f, DollCompanionRules.BeamScale(DollCompanionRules.BeamTicks), "beam fully shut off at boundary");
        float last = 1;
        for (float age = DollCompanionRules.BeamTicks - 8; age <= DollCompanionRules.BeamTicks + 1; age += .25f)
        {
            float scale = DollCompanionRules.BeamScale(age);
            AssertEqual(true, scale >= 0 && scale <= last, "pressure/collision retreat monotonically together");
            last = scale;
        }
        for (int index = 0; index < 3; index++)
            AssertEqual(true, DollCompanionRules.SigilBirth(index) < DollCompanionRules.NeedleTick(index), "circle exists before its shot");
        AssertEqual(0f, DollCompanionRules.MergeAmount(DollCompanionRules.Merge), "merge starts continuously");
        AssertEqual(1f, DollCompanionRules.MergeAmount(DollCompanionRules.Tension), "merge completes before short held beat");
        AssertEqual(true, DollCompanionRules.ChargeAmount(DollCompanionRules.Verdict - 9)
            < DollCompanionRules.ChargeAmount(DollCompanionRules.Verdict - 2), "final eight ticks sharply accelerate");
        AssertEqual(true, DollCompanionRules.Verdict + DollCompanionRules.BeamTicks + DollCompanionRules.BeamAfterglow
            < DollCompanionRules.Cycle, "beam lifecycle ends before the next attack score");
    }
}
