using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Convergence.Client.Graphics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.CrimsonFoundry;

internal static class ScarletClusters
{
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[6];
    internal static void Draw(SpriteBatch batch, in CrimsonGesturePlan p, float age)
    {
        if (Main.dedServ || age < p.Born || age >= p.End) return;
        using var scope = new WorldGraphicsScope(batch);
        var shader = ShaderManager.GetShader("Convergence.ScarletCluster");
        shader.TrySetParameter("uWorldViewProjection", ScarletMaterials.WorldMatrix);
        shader.TrySetParameter("clock", age / 60);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
        float charge = Math.Clamp((age-p.Born)/(p.Fire-p.Born),0,1);
        float fade = CrimsonInvocation.Ease((age-p.Born)/7) * CrimsonInvocation.Ease((p.End-age)/14);
        float release = age < p.Fire ? 0 : MathF.Exp(-(age-p.Fire)/9);
        float size = CrimsonClusters.OrbRadius * (.52f + .48f * CrimsonInvocation.Ease((age-p.Born)/18));
        size *= 1 + (CrimsonVisuals.Reduced ? .008f : .025f) * MathF.Sin((age-p.Born)*.21f)*charge + release*.07f;
        Sphere(V(CrimsonClusters.Emitter(p.Field)),size,fade,charge,release,p.Phrase);
        for (int i=0;i<CrimsonClusters.Count;i++)
        {
            var ray = CrimsonClusters.Ray(p,i);
            Vector2 direction=V(ray.Direction), normal=new(-direction.Y,direction.X);
            if (age < p.Fire + 4)
            {
                float guide=fade*(1-CrimsonInvocation.Ease((age-p.Fire)/4));
                Set(new(charge,0,guide,1),new(ray.Length,ray.Radius,i,0));
                Quad(V(ray.Start)-normal*ray.Radius,direction*ray.Length,normal*ray.Radius*2,"ClusterForecastPass");
            }
            var carrier=CrimsonClusters.Carrier(p,i,age);
            if (carrier.Radius<=.01f) continue;
            Vector2 head=V(carrier.B);
            float glow=carrier.Radius/ray.Radius;
            if (!CrimsonVisuals.Reduced)
            {
                float travelled=CrimsonClusters.Distance(age-p.Fire,ray.Speed);
                float trail=Math.Min(travelled,ray.Radius*3.5f+ray.Speed*2);
                Set(new(1,0,glow*.75f,1),new(trail,ray.Radius,i,0));
                Quad(head-direction*trail-normal*ray.Radius,direction*trail,normal*ray.Radius*2,"ClusterTailPass");
            }
            Sphere(head,carrier.Radius,1,.65f,0,i+p.Phrase*5);
        }
        void Sphere(Vector2 at,float radius,float alpha,float pressure,float impact,float seed)
        {
            Set(new(pressure,impact,alpha,CrimsonVisuals.Reduced?.25f:1),new(0,radius,seed,0));
            float extent=radius*1.4f;
            Quad(at-new Vector2(extent),new(extent*2,0),new(0,extent*2),"AutoloadPass");
        }
        void Set(Vector4 signal,Vector4 shape)
        { shader.TrySetParameter("signal",signal);shader.TrySetParameter("shape",shape); }
        void Quad(Vector2 start,Vector2 u,Vector2 v,string pass)
        {
            start-=Main.screenPosition;
            mesh[0]=Vertex(start,0,0);mesh[1]=Vertex(start+u,1,0);mesh[2]=Vertex(start+v,0,1);
            mesh[3]=mesh[1];mesh[4]=Vertex(start+u+v,1,1);mesh[5]=mesh[2];
            shader.Apply(pass);Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,mesh,0,2);
        }
    }
    private static Vector2 V(CrimsonPoint p)=>new(p.X,p.Y);
    private static VertexPositionColorTexture Vertex(Vector2 p,float x,float y)=>new(new Vector3(p,0),Color.White,new(x,y));
}
