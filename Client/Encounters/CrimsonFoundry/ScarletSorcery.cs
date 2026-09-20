using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Bounded world-space seals, not HUD rings; all clocks are accepted Raid clocks.
internal static class ScarletSorcery
{
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[6];
    internal static void Seal(SpriteBatch batch, Vector2 center, float radius, float flatten, float angle,
        float age, float charge, float alpha, bool black = false, float seed = 0)
    {
        if (radius < .1f || alpha <= .001f || Main.dedServ) return;
        Vector2 u = new Vector2(radius * 2, 0).RotatedBy(angle), v = new Vector2(0, radius * 2 * flatten).RotatedBy(angle);
        Draw(batch, center-u*.5f-v*.5f, u, v, age, new(charge,0,alpha,CrimsonVisuals.Reduced?1:0),
            new(radius*2,radius,seed,black?1:0), "AutoloadPass");
    }
    internal static void Tear(SpriteBatch batch, CrimsonStroke s, float age, float fire, float end, float charge, float alpha, float seed)
    {
        Vector2 a = new(s.A.X,s.A.Y), delta = new(s.B.X-s.A.X,s.B.Y-s.A.Y);
        if (delta.LengthSquared() < .01f || s.Radius <= .001f || alpha <= .001f) return;
        // Pad only the material quad. The white/red blade stays inside the
        // shared capsule; smoke and recoil filaments are harmless decoration.
        Vector2 n = new Vector2(-delta.Y,delta.X) / delta.Length() * (s.Radius + 72);
        Draw(batch,a-n,delta,n*2,age,new(charge,age-fire,alpha,CrimsonVisuals.Reduced?1:0),
            new(delta.Length(),s.Radius,seed,end-fire),age<fire?"TearForecastPass":"TearPass");
    }
    internal static void CrossflowSeals(SpriteBatch batch, in CrimsonGesturePlan p, float age)
    {
        var span = CrimsonChoreography.Side(p, age, true);
        float charge = Math.Clamp((age-p.Born)/(p.Fire-p.Born),0,1);
        float alpha = CrimsonInvocation.Ease((age-p.Born)/7) * (1-CrimsonInvocation.Ease((age-(p.End-15))/15));
        float radius = 80 + charge*105;
        Seal(batch,new(span.A.X,span.A.Y),radius,.28f,MathF.PI/2,age,charge,alpha,false,p.Phrase);
        Seal(batch,new(span.B.X,span.B.Y),radius,.28f,MathF.PI/2,age,charge,alpha,false,p.Phrase+.5f);
        float smoke = CrimsonInvocation.Ease((age-p.Fire-7)/12)*alpha;
        if (smoke>.001f)
            Draw(batch,new(span.B.X-170,span.B.Y-200),new(240,0),new(0,400),age,
                new(charge,1,smoke,CrimsonVisuals.Reduced?1:0),new(240,200,p.Phrase,0),"VaporPass");
    }
    internal static void Flame(SpriteBatch batch, Vector2 from, Vector2 to, float width, float age, float alpha)
    {
        var d=to-from; if(d.LengthSquared()<1 || alpha<=.001f)return;
        var n=new Vector2(-d.Y,d.X)/d.Length()*width;
        Draw(batch,from-n,d,n*2,age,new(1,1,alpha,0),new(d.Length(),width,0,1),"FlamePass");
    }
    private static void Draw(SpriteBatch batch,Vector2 origin,Vector2 u,Vector2 v,float age,Vector4 signal,Vector4 shape,string pass)
    {
        using var scope=new Convergence.Client.Graphics.WorldGraphicsScope(batch);
        var shader=ShaderManager.GetShader("Convergence.ScarletSorcery");
        shader.TrySetParameter("uWorldViewProjection",ScarletMaterials.WorldMatrix);
        shader.TrySetParameter("clock",age/60); shader.TrySetParameter("signal",signal); shader.TrySetParameter("shape",shape);
        shader.TrySetParameter("hue",new Vector3(.82f,.04f,.11f));
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value,1,SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value,2,SamplerState.LinearWrap);
        origin-=Main.screenPosition;
        mesh[0]=V(origin,0,0);mesh[1]=V(origin+u,1,0);mesh[2]=V(origin+v,0,1);
        mesh[3]=mesh[1];mesh[4]=V(origin+u+v,1,1);mesh[5]=mesh[2];
        shader.Apply(pass);Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,mesh,0,2);
        static VertexPositionColorTexture V(Vector2 p,float x,float y)=>new(new Vector3(p,0),Color.White,new(x,y));
    }
}
