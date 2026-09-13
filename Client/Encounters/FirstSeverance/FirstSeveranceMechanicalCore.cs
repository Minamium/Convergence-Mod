#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.FirstSeverance;

// Polished 3D material on the established world-space surface pass. No raster
// synthesis, private animation state, gameplay clock or per-frame allocation.
internal sealed class FirstSeveranceMechanicalCore
{
    private const int Rings=32, Sectors=128;
    private readonly Vector3[] normals=new Vector3[(Rings+1)*(Sectors+1)];
    private readonly VertexPositionColor[] vertices=new VertexPositionColor[(Rings+1)*(Sectors+1)];
    private readonly short[] indices=new short[Rings*Sectors*6];
    private BasicEffect? effect;

    internal FirstSeveranceMechanicalCore()
    {
        for(int r=0;r<=Rings;r++)
            for(int s=0;s<=Sectors;s++)
            {
                float elevation=r/(float)Rings*MathHelper.PiOver2,angle=s/(float)Sectors*MathHelper.TwoPi;
                normals[r*(Sectors+1)+s]=new(MathF.Sin(elevation)*MathF.Cos(angle),
                    MathF.Sin(elevation)*MathF.Sin(angle),MathF.Cos(elevation));
            }
        int cursor=0;
        for(int r=0;r<Rings;r++) for(int s=0;s<Sectors;s++)
        {
            int a=r*(Sectors+1)+s,b=a+Sectors+1;
            indices[cursor++]=(short)a; indices[cursor++]=(short)b; indices[cursor++]=(short)(a+1);
            indices[cursor++]=(short)(a+1); indices[cursor++]=(short)b; indices[cursor++]=(short)(b+1);
        }
    }

    internal void Draw(SpriteBatch batch,Vector2 center,float radius,float seconds,float roll,Color tint,float opacity,bool reduced,
        float bore = 0, Vector2 boreAxis = default)
    {
        if(Main.dedServ||opacity<=.001f||radius<1) return;
        // The sphere's surface rotates; the highlight remains in the world
        // lighting frame. This does not spin a flat painted highlight like a coin.
        Matrix turn=Matrix.CreateRotationZ(-roll)*Matrix.CreateRotationY(seconds*(reduced?.22f:.85f))
            *Matrix.CreateRotationX(.52f+MathF.Sin(seconds*.23f)*.13f);
        Vector3 light=Vector3.Normalize(new Vector3(-.42f,-.58f,.82f));
        Vector3 halfLight=Vector3.Normalize(light+Vector3.UnitZ);
        Vector2 origin=center-Main.screenPosition;
        Vector4 modulation=tint.ToVector4()*opacity;
        bore = Math.Clamp(bore, 0, 1);
        Vector2 axis = boreAxis.SafeNormalize(Vector2.UnitY), across = new(-axis.Y, axis.X);
        for(int i=0;i<normals.Length;i++)
        {
            Vector3 n=normals[i],local=Vector3.TransformNormal(n,turn);
            Vector2 p = new(n.X, n.Y);
            float along = Vector2.Dot(p, axis), cross = Vector2.Dot(p, across);
            var crater = FirstSeveranceCoreCrater.SampleSide(along, cross, bore);
            Vector2 slope = axis * crater.AlongSlope + across * crater.AcrossSlope;
            // Inward wall normals catch a different highlight to the polished
            // outside. Oblique projection exposes depth and a heavy raised lip.
            Vector3 surfaceNormal = Vector3.Normalize(n - new Vector3(slope * Math.Max(n.Z, .001f), 0));
            float diffuse=Math.Max(0,Vector3.Dot(surfaceNormal,light));
            float sheen=MathF.Pow(Math.Max(0,Vector3.Dot(surfaceNormal,halfLight)),35);
            float rim=MathF.Pow(1-n.Z,3)*.17f;
            // A restrained equatorial join and one offset inset travel over
            // the surface. Both recede into the limb: never external rails/Xs.
            float join=1-Math.Clamp(Math.Abs(local.Y)/.027f,0,1);
            float inset=1-Math.Clamp(Math.Abs(local.X+.58f)/.023f,0,1);
            float body=.055f+diffuse*.21f+rim;
            Vector3 material=new(body*.89f,body*.94f,body);
            material+=new Vector3(.66f,.68f,.71f)*sheen;
            material=Vector3.Lerp(material,new Vector3(.055f,.048f,.039f),join*.7f+inset*.14f);
            // Fine brass lip catches the same light without becoming neon.
            float edge=(1-Math.Clamp(Math.Abs(Math.Abs(local.Y)-.037f)/.016f,0,1))*.14f*diffuse;
            material+=new Vector3(edge,edge*.84f,edge*.59f);
            float occlusion = crater.Interior;
            material *= 1 - occlusion * .97f;
            material += new Vector3(.022f, .006f, .039f) * occlusion;
            // Cold reflected wall light remains subordinate to the actual jet.
            material += new Vector3(.06f, .018f, .095f) * Math.Max(0, surfaceNormal.Z) * occlusion * bore;
            Vector2 displaced = p + axis * (crater.Depth * .20f);
            vertices[i]=new(new Vector3(origin+displaced*radius,0),
                new Color(new Vector4(Vector3.Clamp(material,Vector3.Zero,Vector3.One),1)*modulation));
        }
        batch.End();
        var device=Main.instance.GraphicsDevice;
        var blend=device.BlendState; var depth=device.DepthStencilState;
        var raster=device.RasterizerState; var sampler=device.SamplerStates[0];
        try
        {
            effect??=new BasicEffect(device){TextureEnabled=false,VertexColorEnabled=true,LightingEnabled=false};
            effect.World=Matrix.Identity; effect.View=Main.GameViewMatrix.TransformationMatrix;
            effect.Projection=Matrix.CreateOrthographicOffCenter(0,device.Viewport.Width,device.Viewport.Height,0,-1,1);
            device.BlendState=BlendState.AlphaBlend; device.DepthStencilState=DepthStencilState.None;
            device.RasterizerState=RasterizerState.CullNone;
            foreach(var pass in effect.CurrentTechnique.Passes)
            { pass.Apply(); device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,vertices,0,vertices.Length,indices,0,indices.Length/3); }
        }
        finally
        {
            device.BlendState=blend; device.DepthStencilState=depth;
            device.RasterizerState=raster; device.SamplerStates[0]=sampler;
            batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                DepthStencilState.None,Main.Rasterizer,null,Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal void Unload()
    {
        var old=effect; effect=null;
        if(old is not null) Main.QueueMainThreadAction(old.Dispose);
    }
}
