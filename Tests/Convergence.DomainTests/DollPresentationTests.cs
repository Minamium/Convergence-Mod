using System;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Doll presentation authored frames preserve atlas and attachment contracts")]
    private static void DollFrameContracts()
    {
        foreach(bool hand in new[]{false,true})
        {
            var pivot=FirstSeveranceDollFrames.Pivot(hand);
            for(int i=0;i<8;i++)
            {
                var uv=FirstSeveranceDollFrames.Region(hand,i);
                AssertEqual(true,uv.X>=0&&uv.Y>=0&&uv.X+uv.Width<=uv.Width*4&&uv.Y+uv.Height<=uv.Height*2,"in atlas");
                AssertEqual(true,pivot.X>0&&pivot.X<uv.Width&&pivot.Y>0&&pivot.Y<uv.Height,"fixed registered pivot");
            }
        }
        var seen=new bool[8];
        for(int i=0;i<240;i++)
        {
            float seconds=i/60f;
            int frame=FirstSeveranceDollFrames.Body(seconds,0,false); seen[frame]=true;
            foreach(bool reduced in new[]{false,true})
            {
                int claw=FirstSeveranceDollFrames.Hand(seconds,MathF.Sin(seconds),MathF.Cos(seconds),reduced);
                AssertEqual(true,claw>=0&&claw<8,"bounded claw sample");
            }
        }
        foreach(bool used in seen) AssertEqual(true,used,"every authored torso frame is played");
        AssertEqual(7,FirstSeveranceDollFrames.Hand(0,1,1,false),"full strike closes grip");
        AssertEqual(0,FirstSeveranceDollFrames.Body(99,0,true),"reduced ambient animation stays quiet");
    }

    [DomainTest("Doll presentation keeps joint endpoints continuous and scale finite")]
    private static void DollJointContinuity()
    {
        var before=new FirstSeveranceDollPose();
        var after=new FirstSeveranceDollPose();
        foreach(bool reduced in new[]{false,true})
            for(int frame=0;frame<720;frame++)
            {
                float a=frame/720f,b=(frame+1)/720f;
                before.Body(a*6,a,0,a,a,reduced); after.Body(b*6,b,0,b,b,reduced);
                AssertEqual(17,before.Sprites.Count,"bounded doll and retained restraint rig");
                AssertEqual(5,before.Cords.Count,"bounded real attachment points");
                AssertEqual(true,before.Tilt>.04f && before.Tilt<.45f,"bounded crooked sway, never upright");
                for(int i=0;i<before.Sprites.Count;i++)
                {
                    var part=before.Sprites[i];
                    AssertEqual(true,float.IsFinite(part.Scale.X) && part.Scale.X>.1f && part.Scale.X<4,"positive finite authored part scale");
                    AssertEqual(true,Vector2.Distance(part.Position,after.Sprites[i].Position)<4,"continuous joints at fractional 120Hz samples");
                    AssertEqual(true,MathF.Abs(part.Rotation-after.Sprites[i].Rotation)<.025f,"no sudden part rotation");
                    if(FirstSeveranceDollPose.Flexible(part))
                    {
                        AssertEqual(0f,FirstSeveranceDollPose.FlexOffset(part,part.Pivot.Y,a*6,reduced),"flexible root is anchored");
                        int height=FirstSeveranceDollPose.Region(part).Height;
                        for(int row=0;row<=12;row++)
                        {
                            float y=height*row/12f;
                            float delta=FirstSeveranceDollPose.FlexOffset(part,y,b*6,reduced)-FirstSeveranceDollPose.FlexOffset(part,y,a*6,reduced);
                            AssertEqual(true,MathF.Abs(delta)<1,"continuous flexible surface at shared row boundary");
                        }
                    }
                }
                // Upper-arm bottom must exactly meet the forearm's authored top,
                // and forearm bottom the corresponding hand's wrist pivot.
                CheckBone(3,4,0); CheckBone(3,4,1);
                CheckBone(4,5,0); CheckBone(4,5,1);
                void CheckBone(int upper,int lower,int occurrence)
                {
                    int u=-1,l=-1,seenU=0,seenL=0;
                    for(int i=0;i<before.Sprites.Count;i++)
                    {
                        if(before.Sprites[i].Cell==upper && seenU++==occurrence) u=i;
                        if(before.Sprites[i].Cell==lower && seenL++==occurrence) l=i;
                    }
                    var part=before.Sprites[u];
                    Vector2 end=part.Position+Vector2.Transform((FirstSeveranceDollPose.Anchors(upper).Bottom-part.Pivot)*part.Scale,
                        Matrix3x2.CreateRotation(part.Rotation));
                    AssertEqual(true,Vector2.Distance(end,before.Sprites[l].Position)<.001f,"adjacent limbs meet at shared joint center");
                }
            }
    }

    [DomainTest("Doll presentation retained shell masks preserve a closed full surface")]
    private static void RetainedShellSurface()
    {
        int size=FirstSeveranceShellSurface.MaskSize;
        AssertEqual(true,size>=1024,"microfractures are not downsampled to 256px");
        var counts=new int[8];
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            int sector=FirstSeveranceShellSurface.Sector(x,y,size);
            AssertEqual(true,sector>=0&&sector<8,"exactly one petal for every source pixel");
            counts[sector]++;
        }
        foreach(int count in counts) AssertEqual(true,count>size*size/12,"no empty/missing petal");
        for(int tick=0;tick<360;tick++)
        {
            var closed=FirstSeveranceShellSurface.Size(tick);
            AssertEqual(true,closed.X>480&&closed.Y>570,"stable visible preparation/combat footprint");
        }
    }

    [DomainTest("Doll presentation capture reconstructs NPC then converges into the existing shell")]
    private static void DollCaptureContinuity()
    {
        AssertEqual(0f,FirstSeveranceDollCapture.IntroAge(110,200,800),"before accepted intro clamps at intact NPC");
        AssertEqual(.5f,FirstSeveranceDollCapture.IntroAge(500,200,800),"accepted ten-second intro halfway");
        AssertEqual(1f,FirstSeveranceDollCapture.IntroAge(999,200,800),"late snapshot never restarts capture");
        AssertEqual(0f,FirstSeveranceDollCapture.Arrival(0),"no intake flash during initial hold");
        AssertEqual(0f,FirstSeveranceDollCapture.Arrival(1),"no lingering flash after intro");
        Vector2 foot=new(321,654), core=foot-new Vector2(0,506);
        var coverage=new int[32*52];
        for(int i=0;i<FirstSeveranceDollCapture.Count;i++)
        {
            var start=FirstSeveranceDollCapture.Sample(i,0,foot,core,false);
            AssertEqual(1f,start.Scale,"intact source scale");
            AssertEqual(0f,start.Rotation,"intact source orientation");
            AssertEqual(1f,start.Opacity,"intact source opacity");
            AssertEqual(foot+new Vector2(start.X+start.Width*.5f-16,start.Y+start.Height*.5f-50),start.Position,"exact NPC feet/pivot");
            for(int y=start.Y;y<start.Y+start.Height;y++) for(int x=start.X;x<start.X+start.Width;x++) coverage[y*32+x]++;
            foreach(bool reduced in new[]{false,true})
            {
                var end=FirstSeveranceDollCapture.Sample(i,1,foot,core,reduced);
                AssertEqual(true,Vector2.Distance(core,end.Position)<.001f,"every shard reaches fixed destination");
                AssertEqual(0f,end.Opacity,"no stranded sprite after capture");
                for(int frame=0;frame<720;frame++)
                {
                    var a=FirstSeveranceDollCapture.Sample(i,frame/720f,foot,core,reduced);
                    var b=FirstSeveranceDollCapture.Sample(i,(frame+1)/720f,foot,core,reduced);
                    AssertEqual(true,Vector2.Distance(a.Position,b.Position)<8,"continuous separation, pause, and accelerating suction");
                    AssertEqual(true,float.IsFinite(a.Scale)&&a.Scale>0&&a.Opacity>=0&&a.Opacity<=1,"finite bounded fragment state");
                }
            }
        }
        foreach(int count in coverage) AssertEqual(1,count,"exact nonoverlapping whole-frame partition");
    }
}
