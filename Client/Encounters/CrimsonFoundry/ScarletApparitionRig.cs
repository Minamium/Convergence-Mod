#nullable enable
using System;
using Convergence.Client.Graphics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Species-specific continuous skin + travelling material. No gameplay positions
// or random state are produced here. Also renders in the final avatar's local RT.
internal static class ScarletApparitionRig
{
    private const int Columns = 48, Rows = 48, Segments = 36;
    private static readonly Texture2D?[] bodies = new Texture2D?[2];
    private static readonly VertexPositionColorTexture[] grid = new VertexPositionColorTexture[(Columns+1)*(Rows+1)];
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[Columns*Rows*6];
    private static readonly VertexPositionColorTexture[] strip = new VertexPositionColorTexture[Segments*6];
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private static readonly Vector2[] hooks = { new(.08f,.13f), new(.88f,.08f), new(.11f,.73f), new(.80f,.80f) };

    internal static void Load()
    {
        if (Main.dedServ) return;
        bodies[0] = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/EmberCrown", AssetRequestMode.ImmediateLoad).Value;
        bodies[1] = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/SableMantle", AssetRequestMode.ImmediateLoad).Value;
    }
    internal static void Unload() => Array.Clear(bodies);

    internal static void Draw(SpriteBatch batch, int species, Vector2 center, float height, float age,
        float charge, float recoil, float alpha, bool flip = false, float rotation = 0,
        float dissolve = 0, float melt = 0, Matrix? projection = null, bool local = false, float cut = 0)
    {
        if (Main.dedServ || species is <0 or >1 || bodies[species] is not { } texture || alpha <= .001f || dissolve >= 1) return;
        bool reduced = CrimsonVisuals.Reduced;
        Vector2 root = center - (local ? Vector2.Zero : Main.screenPosition);
        float tilt = rotation + MathF.Sin(age*.017f + species)*.027f;
        float pulse = MathF.Pow(.5f+.5f*MathF.Sin(age*.12f), 5);
        var shader = ShaderManager.GetShader("Convergence.ScarletApparitions");
        using var scope = new WorldGraphicsScope(batch);
        shader.TrySetParameter("uWorldViewProjection", projection ?? ScarletMaterials.WorldMatrix);
        shader.TrySetParameter("clock", age/60);
        shader.TrySetParameter("species", (float)species);
        shader.TrySetParameter("signal", new Vector4(charge,recoil,alpha,reduced?.48f:1));
        shader.TrySetParameter("ceremony", new Vector2(dissolve,melt));
        shader.TrySetParameter("cut",cut);
        shader.SetTexture(texture,0,SamplerState.LinearClamp);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value,1,SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value,2,SamplerState.LinearWrap);

        if (species == 0)
        {
            // Crown is a venting censer: upright furnace plumes, not Choir wings.
            for (int k=0;k<(reduced?4:9);k++)
            {
                float side=(k-4)/4f;
                Vector2 a=new(.50f+side*.16f,.25f+MathF.Abs(side)*.08f);
                Vector2 d=a+new Vector2(side*(.13f+charge*.10f),-.21f-charge*.10f-recoil*.15f);
                d.X+=MathF.Sin(age*.052f+k*2.4f)*.045f;
                Ribbon(a,a+new Vector2(-side*.08f,-.09f),d+new Vector2(.035f*MathF.Sin(age*.04f+k),.11f),d,
                    .032f+charge*.025f+recoil*.03f,k*1.73f,.72f,0);
            }
        }
        else
        {
            // Mantle hooks pull long, asymmetric afterimages through space.
            for(int k=0;k<4;k++)
            {
                Vector2 now=Skin(hooks[k],age),past=Skin(hooks[k],age-11),old=Skin(hooks[k],age-24);
                float side=k%2==0?-1:1;
                Vector2 tail=old+new Vector2(side*.12f,.12f+MathF.Sin(age*.022f+k)*.06f);
                Ribbon(now,past,old,tail,.023f+charge*.025f+recoil*.05f,k+11,.65f,1);
                if(!reduced) Ribbon(now,now+new Vector2(side*.17f,-.08f),tail+new Vector2(side*.12f,.07f),tail,
                    .055f+charge*.018f,k+7,.24f,0);
            }
        }

        for(int y=0;y<=Rows;y++) for(int x=0;x<=Columns;x++)
        {
            Vector2 uv=new((float)x/Columns,(float)y/Rows);
            Vector2 at=Map(Skin(uv,age));
            grid[y*(Columns+1)+x]=new(new(at,0),Color.White,uv);
        }
        int n=0;
        for(int y=0;y<Rows;y++) for(int x=0;x<Columns;x++)
        {
            int a=y*(Columns+1)+x,b=a+1,c=a+Columns+1,d=c+1;
            mesh[n++]=grid[a];mesh[n++]=grid[b];mesh[n++]=grid[c];
            mesh[n++]=grid[b];mesh[n++]=grid[d];mesh[n++]=grid[c];
        }
        shader.Apply("AuraPass"); Submit(mesh,n);
        shader.Apply(); Submit(mesh,n);

        Vector2 heart=species==0?new(.5f,.30f):new(.527f,.428f);
        if(cut<.5f)
        {
            shader.TrySetParameter("shape",new Vector4(pulse,charge,recoil,0));
            Patch(Skin(heart,age),new Vector2(species==0?.24f:.17f),"HeartPass");
        }
        for(int k=0;k<4;k++)
        {
            Vector2 tip=species==0?new(.28f+k*.15f,.12f+MathF.Sin(k*3)*.05f):Skin(hooks[k],age);
            Vector2 start=Skin(heart,age),delta=tip-start;
            Ribbon(start,start+new Vector2(delta.X*.32f,delta.Y*.1f),tip-delta*.17f,tip,
                .008f+charge*.011f+recoil*.017f,k*3.7f,.28f+charge*.35f+recoil*.65f,1);
        }
        if(!reduced)
            for(int k=0;k<16;k++)
            {
                float life=(age*(.004f+k%3*.001f)+k*.618034f)%1;
                float side=k%2==0?-1:1;
                Vector2 at=species==0?new(.50f+side*(.12f+life*.28f),.26f-life*.51f)
                    :Vector2.Lerp(Skin(heart,age),Skin(hooks[k%4],age),life);
                at+=new Vector2(MathF.Sin(k*3.4f+life*5)*.028f,MathF.Sin(life*8+k)*.025f);
                shader.TrySetParameter("shape",new Vector4(MathF.Sin(life*MathF.PI)*(.3f+charge*.7f),k,0,0));
                Patch(at,new(.006f,.019f),"SparkPass");
            }

        Vector2 Skin(Vector2 uv,float t)
        {
            Vector2 pivot=new(.5f,.39f),p=uv-pivot;
            float lateral=Ease((MathF.Abs(p.X)-.05f)/.2f),side=p.X<0?-1:1;
            if(species==0)
            {
                float top=1-Ease((uv.Y-.40f)/.13f);
                float gape=(charge*.055f-recoil*.085f+MathF.Sin(t*.027f)*.012f)*top;
                p=Rotate(p,side*gape*lateral);
                p.X+=side*(charge*.037f+recoil*.034f)*lateral*top;
                float cloth=Ease((uv.Y-.43f)/.38f);
                p.X+=MathF.Sin(t*.030f-uv.Y*15+uv.X*6)*.027f*cloth;
                p.Y+=MathF.Sin(t*.039f+uv.X*8)*.016f*cloth;
            }
            else
            {
                float lower=Ease((uv.Y-.44f)/.18f);
                float lag=lower*.8f+(side<0?.6f:0);
                float move=MathF.Sin(t*.033f-lag)*.085f;
                // Hooks brace quickly, suspend briefly, then whip on discharge.
                float angle=side*(move+charge*.19f-recoil*.36f)*(1-lower*1.7f)*lateral;
                p=Rotate(p,angle);
                p+=new Vector2(MathF.Sin(t*.042f-uv.Y*9+side)*.030f,MathF.Sin(t*.028f+uv.X*11)*.028f)*lateral;
                p*=new Vector2(1+charge*.055f-recoil*.055f,1-recoil*.04f);
            }
            p.X*=1-melt*uv.Y*.7f;p.Y+=melt*uv.Y*uv.Y*.6f;
            return pivot+p;
        }
        Vector2 Map(Vector2 uv)
        {
            Vector2 p=(uv-new Vector2(.5f))*height;
            if(flip)p.X=-p.X;
            return root+Rotate(p,tilt);
        }
        void Ribbon(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float radius,float seed,float opacity,float filament)
        {
            shader.TrySetParameter("shape",new Vector4(seed,opacity*(1-dissolve),filament,0));
            int count=0;
            for(int k=0;k<Segments;k++)
            {
                float t=(float)k/Segments,next=(float)(k+1)/Segments;
                var a0=V(t,-1);var a1=V(t,1);var b0=V(next,-1);var b1=V(next,1);
                strip[count++]=a0;strip[count++]=a1;strip[count++]=b0;strip[count++]=a1;strip[count++]=b1;strip[count++]=b0;
            }
            shader.Apply("RibbonPass");Submit(strip,count);
            VertexPositionColorTexture V(float t,float side)
            {
                float s=1-t;
                Vector2 at=s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;
                Vector2 tangent=3*s*s*(b-a)+6*s*t*(c-b)+3*t*t*(d-c);
                if(tangent.LengthSquared()<.000001f)tangent=Vector2.UnitY;
                tangent.Normalize();
                Vector2 normal=new(-tangent.Y,tangent.X);
                float taper=(.12f+.88f*MathF.Sin(t*MathF.PI))*(1-Ease((t-.83f)/.17f));
                return new(new(Map(at+normal*radius*side*taper),0),Color.White,new(t,(side+1)*.5f));
            }
        }
        void Patch(Vector2 p,Vector2 radius,string pass)
        {
            var a=new VertexPositionColorTexture(new(Map(p-radius),0),Color.White,new(0,0));
            var b=new VertexPositionColorTexture(new(Map(p+new Vector2(radius.X,-radius.Y)),0),Color.White,new(1,0));
            var c=new VertexPositionColorTexture(new(Map(p+new Vector2(-radius.X,radius.Y)),0),Color.White,new(0,1));
            var d=new VertexPositionColorTexture(new(Map(p+radius),0),Color.White,new(1,1));
            quad[0]=a;quad[1]=b;quad[2]=c;quad[3]=b;quad[4]=d;quad[5]=c;
            shader.Apply(pass);Submit(quad,6);
        }
    }
    private static float Ease(float x) { x=Math.Clamp(x,0,1);return x*x*(3-2*x); }
    private static Vector2 Rotate(Vector2 p,float a)=>new(p.X*MathF.Cos(a)-p.Y*MathF.Sin(a),p.X*MathF.Sin(a)+p.Y*MathF.Cos(a));
    private static void Submit(VertexPositionColorTexture[] v,int n)=>Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,v,0,n/3);
}
