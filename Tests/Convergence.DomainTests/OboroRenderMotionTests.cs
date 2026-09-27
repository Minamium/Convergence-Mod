using System;
using Convergence.Client.Weapons;
using Convergence.Content.Items.Oboro;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Oboro draw samples continuous accepted curve without advancing combat or history")]
    private static void OboroDrawProjection()
    {
        var hand = new OboroHandBasis(-4,-2,10,3,-3,10);
        for (byte step=0;step<3;step++)
        {
            var visual=new OboroSwingPresentation();
            var v=OboroSample with { Step=step, Duration=(ushort)OboroComboSettings.For(step).TotalFrames, Aim=0, Facing=1 };
            for (int age=1;age<v.Duration;age++)
            {
                visual.Update(v,age,true,true,0,0,1,(ulong)age+100,hand);
                var accepted=visual.Pose; int count=visual.Count;
                for(int i=0;i<=8;i++)
                {
                    visual.PrepareDraw(i/8f);
                    AssertEqual(accepted,visual.Pose,"drawing never mutates accepted pose");
                    AssertEqual(count,visual.Count,"render frequency cannot multiply trails");
                    float p=Math.Max(0,age-1+i/8f)/v.Duration;
                    if(OboroRules.Live(step,p)!=OboroRules.Live(step,accepted.Progress))p=accepted.Progress;
                    AssertEqual(true,MathF.Abs(visual.RenderPose.Angle-OboroRules.Offset(step,p))<.0001f,"actual curve, no linear-angle approximation");
                    var grip=hand.At(visual.RenderArmAngle);
                    AssertEqual(true,MathF.Abs(grip.X-visual.RenderSword.X)<.0001f && MathF.Abs(grip.Y-visual.RenderSword.Y)<.0001f,"fractional grip attached");
                }
            }
            visual.Clear();visual.PrepareDraw(.5f);
            AssertEqual(default(OboroBladePose),visual.RenderSword,"cancel leaves no interpolated ghost sword");
        }
    }

    [DomainTest("Oboro sub-tick history clips its leading sample and keeps bounded continuous fade")]
    private static void OboroSubTickHistory()
    {
        var visual=new OboroSwingPresentation();
        var v=OboroSample with { Step=0,Duration=22,Aim=0,Facing=1 };
        for(int age=0;age<=10;age++)visual.Update(v,age,true,true,0,0,1,(ulong)age+100);
        visual.PrepareDraw(.5f);
        var lead=visual.RenderEcho(visual.Count-1);
        AssertEqual(visual.RenderPose,lead.Pose,"leading trail follows fractional sword");
        AssertEqual(visual.RenderNow,lead.At,"no future trail sample ahead of sword");
        AssertEqual(true,OboroSwingPresentation.Fade(2.5,0)>OboroSwingPresentation.Fade(2.6,0),"continuous not ticked residue");
    }
}
