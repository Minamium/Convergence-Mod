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
    private const int ShardCount = 23;
    private readonly Vector3[] shardCenters = new Vector3[ShardCount];
    private readonly byte[] shardGroups = new byte[Rings*Sectors*2];
    private readonly float[] fractureDistance = new float[(Rings+1)*(Sectors+1)];
    private readonly VertexPositionColor[] shards = new VertexPositionColor[Rings*Sectors*6];
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
        for (int i = 0; i < ShardCount; i++)
        {
            float z = (i + .5f) / ShardCount, a = i * 2.399963f;
            float radial = MathF.Sqrt(1 - z * z);
            shardCenters[i] = new(radial * MathF.Cos(a), radial * MathF.Sin(a), z);
        }
        // The crack network and flying plates share this exact partition.
        for (int i = 0; i < normals.Length; i++)
        {
            ClosestShard(normals[i], out float distance);
            fractureDistance[i] = distance;
        }
        for (int i = 0; i < shardGroups.Length; i++)
            shardGroups[i] = (byte)ClosestShard(Vector3.Normalize(normals[indices[i*3]] +
                normals[indices[i*3+1]] + normals[indices[i*3+2]]), out _);
    }

    private int ClosestShard(Vector3 point, out float edge)
    {
        float first = float.MaxValue, second = float.MaxValue;
        int nearest = 0;
        for (int i = 0; i < ShardCount; i++)
        {
            float d = Vector3.DistanceSquared(point, shardCenters[i]);
            if (d < first) { second = first; first = d; nearest = i; }
            else if (d < second) second = d;
        }
        edge = second - first;
        return nearest;
    }

    internal void Draw(SpriteBatch batch,Vector2 center,float radius,float seconds,float roll,Color tint,float opacity,bool reduced,
        float bore = 0, Vector2 boreAxis = default, bool twinBore = false, float damage = 0,
        FirstSeveranceCoreRupture rupture = default)
    {
        opacity *= rupture.MetalOpacity;
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
            float side = twinBore && along < 0 ? -1 : 1;
            var crater = FirstSeveranceCoreCrater.SampleSide(along * side, cross, bore);
            Vector2 slope = axis * (crater.AlongSlope * side) + across * crater.AcrossSlope;
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
            // Failing metal, not another UI reticle: torn, asymmetric joins
            // rotate with the material and leak white-violet pressure from inside.
            float scar = MathF.Abs(local.Y + .065f * MathF.Sin(local.X * 21 + local.Z * 8));
            float crack = MathF.Pow(1 - Math.Clamp(scar / (.004f + damage * .026f), 0, 1), 2);
            float flicker = .6f + .4f * MathF.Sin(seconds * 19 + local.X * 6) * MathF.Sin(seconds * 31);
            material *= 1 - damage * .38f;
            material += new Vector3(.7f, .34f, 1f) * crack * damage * flicker * (1 - occlusion);
            // Contact arrives from the claws at the left/right limb, then
            // fractures travel inward before the plates accelerate away.
            float arrival = Math.Clamp((rupture.Cracks - (1 - Math.Abs(n.X)) * .6f) * 2.5f, 0, 1);
            float split = 1 - Math.Clamp(fractureDistance[i] / (.005f + arrival * .026f), 0, 1);
            material += new Vector3(.72f, .30f, 1f) * split * arrival * 1.5f;
            Vector2 displaced = p + axis * (crater.Depth * .20f * side);
            displaced *= 1 + damage * .022f * MathF.Sin(seconds * 13 + local.X * 9) * n.Z;
            displaced *= new Vector2(1 - rupture.Compression, 1 + rupture.Compression * .65f);
            vertices[i]=new(new Vector3(origin+displaced*radius,0),
                new Color(new Vector4(Vector3.Clamp(material,Vector3.Zero,Vector3.One),1)*modulation));
        }
        bool bursting = rupture.BurstAge > 0;
        if (bursting)
        {
            float t = rupture.BurstAge / 60f, travel = 1 - MathF.Exp(-t * 6);
            float distance = radius * (reduced ? 1.05f : 2.9f) * travel;
            for (int i = 0; i < shardGroups.Length; i++)
            {
                int group = shardGroups[i];
                Vector3 n = shardCenters[group];
                Vector2 pivot = origin + new Vector2(n.X, n.Y) * radius;
                float angle = (group % 2 == 0 ? 1 : -1) * t * (1.1f + group % 4 * .3f);
                float cosine = MathF.Cos(angle), sine = MathF.Sin(angle);
                Vector2 offset = new Vector2(n.X, n.Y) * distance +
                    new Vector2(MathF.Sin(group * 2.1f) * t * radius * .2f, t*t*radius*(reduced?.4f:1.4f));
                float tilt = MathF.Cos(t * (1.5f + group % 5 * .4f));
                for (int j = 0; j < 3; j++)
                {
                    var v = vertices[indices[i*3+j]];
                    Vector2 local = new Vector2(v.Position.X, v.Position.Y) - pivot;
                    local.X *= tilt;
                    Vector2 rotated = new(local.X*cosine-local.Y*sine, local.X*sine+local.Y*cosine);
                    shards[i*3+j] = new(new Vector3(pivot + offset + rotated, 0), v.Color);
                }
            }
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
            {
                pass.Apply();
                if (bursting) device.DrawUserPrimitives(PrimitiveType.TriangleList, shards, 0, shards.Length/3);
                else device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,vertices,0,vertices.Length,indices,0,indices.Length/3);
            }
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
