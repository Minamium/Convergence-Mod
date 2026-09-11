using System;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Doll presentation keeps joint endpoints continuous and scale finite")]
    private static void DollJointContinuity()
    {
        var before=new FirstSeveranceDollPose();
        var after=new FirstSeveranceDollPose();
        foreach(bool reduced in new[]{false,true})
            for(int frame=0;frame<240;frame++)
            {
                float a=frame/240f,b=(frame+1)/240f;
                before.Body(a*6,a,0,a,a,reduced); after.Body(b*6,b,0,b,b,reduced);
                AssertEqual(17,before.Sprites.Count,"bounded doll and retained restraint rig");
                AssertEqual(5,before.Cords.Count,"bounded real attachment points");
                AssertEqual(true,before.Tilt>.06f && before.Tilt<.40f,"crooked even during reveal");
                for(int i=0;i<before.Sprites.Count;i++)
                {
                    var part=before.Sprites[i];
                    AssertEqual(true,float.IsFinite(part.Scale.X) && part.Scale.X>.1f && part.Scale.X<4,"positive finite authored part scale");
                    AssertEqual(true,Vector2.Distance(part.Position,after.Sprites[i].Position)<7,"no discontinuous joint jump");
                    AssertEqual(true,MathF.Abs(part.Rotation-after.Sprites[i].Rotation)<.06f,"no sudden part rotation");
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

    [DomainTest("Doll presentation capture reconstructs NPC then converges into the existing shell")]
    private static void DollCaptureContinuity()
    {
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
