using System;
using System.IO;
using Convergence.Content.Encounters.AzureCathedral;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Azure doubled HP projection accepts every supported roster and rejects over-budget maxima")]
    private static void AzureDoubledHpCodec()
    {
        for(int count=1;count<=AzureRules.Members;count++)
        {
            var members=new AzureMember[count];
            for(byte i=0;i<count;i++)members[i]=new(i,Guid.NewGuid(),true,false);
            int girl=AzureRules.Life(count,false),worm=AzureRules.Life(count,true);
            var state=AzureExample() with { Members=members,GirlMax=girl,GirlLife=girl,WormMax=worm,WormLife=worm };
            var read=AzureRead(state)!.Value;
            AssertEqual(girl,read.GirlMax,"scaled Liora HP transmitted");
            AssertEqual(worm,read.WormMax,"scaled Duet shared pool transmitted");
            var fury=state with { Stage=AzureStage.Performance,Age=3000,StagingAt=2000,CeremonySide=1,
                Phase=AzurePhase.Fury,PhaseAt=2600,Enraged=true,GirlLife=0,WormLife=AzureRules.FuryLife(worm) };
            AssertEqual(AzureRules.FuryLife(worm),AzureRead(fury)!.Value.WormLife,"Fury refill fits bounded codec at every roster size");
        }
        foreach(var invalid in new[] {
            AzureExample() with { GirlMax=AzureRules.Life(AzureRules.Members,false)+1 },
            AzureExample() with { WormMax=AzureRules.Life(AzureRules.Members,true)+1 } })
        {
            bool rejected=false;
            try { AzureRead(invalid); } catch(InvalidDataException) { rejected=true; }
            AssertEqual(true,rejected,"over-budget HP remains rejected");
        }
    }
}
