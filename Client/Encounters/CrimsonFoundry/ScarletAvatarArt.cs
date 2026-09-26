using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// A fourth anatomy assembled in local space: abyssal mantle, exposed articulated
// hands, detached crown, a suspended living nucleus and arterial support ribs.
// All accepted cues come from the exact active owner; no animation owns damage.
internal static class ScarletAvatarArt
{
    private const int Segments=40;
    private static readonly VertexPositionColorTexture[] strip=new VertexPositionColorTexture[Segments*6];
    private static readonly VertexPositionColorTexture[] quad=new VertexPositionColorTexture[6];

    internal static void Draw(SpriteBatch batch,Vector2 center,Matrix projection,float age,
        float emergence,float alpha,float dissolve,float melt)
    {
        if(Main.dedServ || emergence<=0 || alpha<=.001f || dissolve>=1)return;
        float size=.45f+.55f*emergence;
        float charge=.25f,recoil=0;
        var boss=CrimsonPackets.Boss;
        Span<CrimsonChoirCue> cues=stackalloc CrimsonChoirCue[16];
        int cueCount=0;
        if(boss is not null && boss.State.Phase==3)
        {
            (charge,recoil)=CrimsonRig.Signal(boss,-1,age);
            cueCount=CrimsonRig.ChoirCues(boss,age,cues,true);
        }
        float heartbeat=MathF.Pow(.5f+.5f*MathF.Sin(age*.10f),5);
        float inhale=.04f*MathF.Sin(age*.02f);
        Vector2 heart=center+new Vector2(0,-25+melt*170)*size;

        // Smoke occupies the gaps behind anatomy. No fullscreen white wash.
        Energy(false);
        ScarletApparitionRig.Draw(batch,1,center+new Vector2(0,-12)*size,1160*size,age,
            charge,recoil,alpha*.90f,rotation:-.025f+inhale,dissolve:dissolve,melt:melt,
            projection:projection,local:true,cut:1);
        // Four actual shoulder/elbow/wrist chains, not oscillating the whole body.
        CrimsonChoirRig.Draw(batch,center+new Vector2(0,115)*size,1100*size,age,
            charge,recoil,alpha,false,.06f+inhale,dissolve,melt,true,cues[..cueCount],projection,Vector2.Zero);
        ScarletApparitionRig.Draw(batch,0,center+new Vector2(0,48)*size,810*size,age+17,
            charge,recoil,alpha,rotation:-.045f,dissolve:dissolve,melt:melt,
            projection:projection,local:true,cut:1);
        Energy(true);

        void Energy(bool foreground)
        {
            var shader=ShaderManager.GetShader("Convergence.ScarletAvatarAnatomy");
            using var scope=new WorldGraphicsScope(batch);
            shader.TrySetParameter("uWorldViewProjection",projection);
            shader.TrySetParameter("clock",age/60);
            shader.TrySetParameter("signal",new Vector4(charge,recoil,alpha,CrimsonVisuals.Reduced?.45f:1));
            shader.TrySetParameter("ceremony",new Vector2(dissolve,melt));
            shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value,0,SamplerState.LinearWrap);
            shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value,1,SamplerState.LinearWrap);
            if(!foreground)
            {
                Patch(center+new Vector2(0,-20)*size,new Vector2(645,625)*size,"ShroudPass");
                for(int side=-1;side<=1;side+=2)
                    for(int k=0;k<(CrimsonVisuals.Reduced?2:4);k++)
                    {
                        Vector2 end=center+new Vector2(side*(330+k*46+MathF.Sin(age*.02f+k)*32),300+k*65)*size;
                        Vector2 root=heart+new Vector2(side*38,-110)*size;
                        Ribbon(root,root+new Vector2(side*(220+k*30),-290+k*35)*size,
                            end+new Vector2(side*130,-190)*size,end,(48+k*9)*size,k+side*3,.35f,false);
                    }
                return;
            }
            // Each side braces at a different angle: organs held in tension.
            for(int side=-1;side<=1;side+=2)
                for(int k=0;k<4;k++)
                {
                    float drift=MathF.Sin(age*(.022f+k*.002f)+side+k)*18;
                    Vector2 root=heart+new Vector2(side*(105+k*7),-140+k*58)*size;
                    Vector2 tip=heart+new Vector2(side*(230+k*30+charge*28-recoil*32),-175+k*106+drift)*size;
                    Ribbon(root,root+new Vector2(side*180,-70)*size,tip+new Vector2(side*95,-30)*size,tip,
                        (13+charge*8+recoil*12)*size,k+side*7,.65f,true);
                    Ribbon(tip,Vector2.Lerp(tip,heart,.35f)+new Vector2(0,90)*size,
                        heart+new Vector2(side*60,45)*size,heart,(8+charge*9)*size,k+3,.48f,false);
                }
            shader.TrySetParameter("shape",new Vector4(heartbeat,charge,recoil,0));
            Patch(heart,new Vector2(285,320)*size,"NucleusPass");
            // Broken crossing meridians are anatomical bands, not a target UI.
            for(int k=0;k<3;k++)
            {
                float swing=MathF.Sin(age*.024f+k*2.1f);
                Vector2 a=heart+new Vector2(-154,-80+k*68)*size;
                Vector2 d=heart+new Vector2(156,85-k*52)*size;
                Ribbon(a,a+new Vector2(20,-120-swing*50)*size,d+new Vector2(-35,90+swing*40)*size,d,
                    (4+charge*4)*size,k*6+age*.001f,.68f,true);
            }
            if(!CrimsonVisuals.Reduced)
                for(int k=0;k<24;k++)
                {
                    float life=(age*(.004f+k%4*.0005f)+k*.618034f)%1;
                    float angle=k*2.399963f+age*.005f;
                    Vector2 p=heart+new Vector2(MathF.Cos(angle)*(80+life*340),MathF.Sin(angle)*(75+life*360))*size;
                    shader.TrySetParameter("shape",new Vector4(MathF.Sin(life*MathF.PI)*(.3f+charge*.45f+recoil*.45f),k,0,0));
                    Patch(p,new Vector2(4,16+recoil*18)*size,"SparkPass");
                }

            void Patch(Vector2 at,Vector2 half,string pass)
            {
                var a=new VertexPositionColorTexture(new(at-half,0),Color.White,new(0,0));
                var b=new VertexPositionColorTexture(new(at+new Vector2(half.X,-half.Y),0),Color.White,new(1,0));
                var c=new VertexPositionColorTexture(new(at+new Vector2(-half.X,half.Y),0),Color.White,new(0,1));
                var d=new VertexPositionColorTexture(new(at+half,0),Color.White,new(1,1));
                quad[0]=a;quad[1]=b;quad[2]=c;quad[3]=b;quad[4]=d;quad[5]=c;
                shader.Apply(pass);Submit(quad,6);
            }
            void Ribbon(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float radius,float seed,float opacity,bool bone)
            {
                shader.TrySetParameter("shape",new Vector4(seed,opacity,bone?1:0,0));
                int n=0;
                for(int k=0;k<Segments;k++)
                {
                    float t=(float)k/Segments,nt=(float)(k+1)/Segments;
                    var al=V(t,-1);var ar=V(t,1);var bl=V(nt,-1);var br=V(nt,1);
                    strip[n++]=al;strip[n++]=ar;strip[n++]=bl;strip[n++]=ar;strip[n++]=br;strip[n++]=bl;
                }
                shader.Apply("ArteryPass");Submit(strip,n);
                VertexPositionColorTexture V(float t,float side)
                {
                    float s=1-t;
                    Vector2 p=s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;
                    Vector2 tangent=3*s*s*(b-a)+6*s*t*(c-b)+3*t*t*(d-c);
                    if(tangent.LengthSquared()<.001f)tangent=Vector2.UnitY;
                    tangent.Normalize();
                    Vector2 normal=new(-tangent.Y,tangent.X);
                    float taper=(.20f+.8f*MathF.Sin(t*MathF.PI))*(1-MathF.Pow(t,6));
                    p+=normal*(radius*taper*side);
                    p.X=heart.X+(p.X-heart.X)*(1-melt*.55f);
                    p.Y+=melt*(60+t*t*170)*size;
                    return new(new(p,0),Color.White,new(t,(side+1)*.5f));
                }
            }
        }
    }
    private static void Submit(VertexPositionColorTexture[] v,int n)=>Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,v,0,n/3);
}
