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
                AssertEqual(17,before.Sprites.Count,"bounded segmented rig including two visible hip caps");
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
                    AssertEqual(true,Vector2.Distance(end,before.Sprites[l].Position)<.001f,"upper and lower meet at sphere center");
                }
            }
    }
}
