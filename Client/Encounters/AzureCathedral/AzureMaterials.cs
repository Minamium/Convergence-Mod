using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.AzureCathedral;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.AzureCathedral;

internal static class AzureMaterials
{
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[6];
    private static readonly AzureWorm[] parts = new AzureWorm[AzureRules.Segments + 1];
    internal static void Quad(ManagedShader shader, Vector2 center, Vector2 size, float rotation, Vector4 uv, Color color, string pass = "AutoloadPass")
    {
        Vector2 dx = rotation.ToRotationVector2() * size.X * .5f, dy = new Vector2(-MathF.Sin(rotation), MathF.Cos(rotation)) * size.Y * .5f;
        Vector2 a = center - dx - dy, b = center - dx + dy, c = center + dx - dy, d = center + dx + dy;
        vertices[0] = new(new(a, 0), color, new(uv.X, uv.Y)); vertices[1] = new(new(b, 0), color, new(uv.X, uv.Y + uv.W));
        vertices[2] = new(new(c, 0), color, new(uv.X + uv.Z, uv.Y)); vertices[3] = vertices[2]; vertices[4] = vertices[1];
        vertices[5] = new(new(d, 0), color, new(uv.X + uv.Z, uv.Y + uv.W));
        shader.Apply(pass); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
    }
    internal static ManagedShader Begin(bool screen = false)
    {
        var device = Main.instance.GraphicsDevice;
        device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
        var shader = ShaderManager.GetShader("Convergence.AzureGlass");
        var matrix = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1);
        shader.TrySetParameter("uWorldViewProjection", screen ? matrix : Main.GameViewMatrix.TransformationMatrix * matrix);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        return shader;
    }
    internal static void Worm(AzureWorm head, AzureBoss girl, SpriteBatch batch, Vector2 screen, Matrix? projection=null, bool silhouette=false)
    {
        Array.Clear(parts);
        foreach (NPC n in Main.ActiveNPCs) if (n.ModNPC is AzureWorm a && a.Fight == head.Fight) parts[a.Index] = a;
        float age = AzureVisuals.RenderAge(girl);
        float presence = AzureWormPresentation.Presence(age, girl.State.MusicStart);
        bool melting=girl.State.Phase==AzurePhase.Melting;
        if (girl.State.WormLife <= 0 && !melting) presence *= .16f;
        if (girl.State.EndAt >= 0 && !melting) presence *= 1 - AzureRules.Ease((age - girl.State.EndAt) / 150);
        var art = ModContent.Request<Texture2D>("Convergence/Assets/Textures/AzureCathedral/Vitrion").Value;
        var furyArt = ModContent.Request<Texture2D>("Convergence/Assets/Textures/AzureCathedral/VitrionFury").Value;
        using var scope = new WorldGraphicsScope(batch);
        var shader = Begin(); shader.SetTexture(art, 0, SamplerState.PointClamp); shader.TrySetParameter("clock", age / 60);
        shader.SetTexture(furyArt,2,SamplerState.PointClamp);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value,3,SamplerState.LinearWrap);
        if(projection is { } matrix)shader.TrySetParameter("uWorldViewProjection",matrix*Matrix.CreateOrthographicOffCenter(0,Main.instance.GraphicsDevice.Viewport.Width,Main.instance.GraphicsDevice.Viewport.Height,0,-1,1));
        shader.TrySetParameter("silhouette",silhouette?1f:0f);
        AzureWormPresentation.Wakes(shader,parts,girl,age,screen,presence,silhouette);
        for (int i = AzureRules.Segments; i >= 0; i--)
        {
            var part = parts[i]; if (part is null) continue;
            int cell = i == 0 ? 0 : i == AzureRules.Segments ? 3 : i % 3 == 0 ? 2 : 1;
            Vector4 uv = new(cell % 2 * .5f, cell / 2 * .5f, .5f, .5f);
            // Authored part spines sit at y=.54 / .48 of their cells. The shader
            // mirrors around that spine, not a guessed atlas midpoint.
            float spine = cell < 2 ? .542f : .48f;
            float angle = part.NPC.rotation, scale = i == 0 ? 292 : i == AzureRules.Segments ? 205 : 188;
            // Tail art points away from the head; the barrel root overlaps its previous joint.
            if (i == AzureRules.Segments) angle += MathHelper.Pi;
            float flex = AzureVisuals.Reduced ? 0 : MathF.Sin(age * .020f - i * .22f) * .018f;
            shader.TrySetParameter("region", uv);
            shader.TrySetParameter("spine",spine);
            shader.TrySetParameter("furySpine",cell<2?.610f:.485f);
            float fury=girl.State.Phase==AzurePhase.Devouring?AzureRules.FuryReveal(age-girl.State.PhaseAt,i):girl.State.Enraged?1:0;
            shader.TrySetParameter("fury",fury);
            float melt=melting?AzureRules.Melt(age-girl.State.EndAt,i):0;
            shader.TrySetParameter("dissolve",melt);
            float charge=Math.Clamp(head.NPC.ai[2],0,1)+(girl.State.Enraged?.6f:0)
                +AzureWormPresentation.ArrivalLight(age,girl.State.MusicStart,i)*.7f;
            shader.TrySetParameter("signal", new Vector4(presence, charge, AzureVisuals.Reduced ? 1 : 0, i));
            Vector2 sag=new(0,melt*melt*(60+i*2));
            var size=new Vector2(scale*(1-melt*.32f),scale*(1+flex+melt*.45f));
            float jaw=girl.State.Phase==AzurePhase.Devouring?AzureRules.JawOpening(age-girl.State.PhaseAt):fury*(.20f+.08f*MathF.Sin(age*.075f));
            // The head carapace remains one rigid silhouette. Only separate
            // mouth/mandible sprites articulate; never cut the head image in half.
            Quad(shader, part.NPC.Center + sag - screen,size,angle,uv,Color.White);
            if(!silhouette)Quad(shader,part.NPC.Center+sag-screen,size,angle,uv,Color.White,"WormGlowPass");
            if(i==0)Mouth(shader,part.NPC.Center+sag-screen,angle,jaw,age,melt,presence,silhouette);
        }
        shader.TrySetParameter("silhouette",0f);shader.TrySetParameter("dissolve",0f);
        Array.Clear(parts);
    }
    private static void Mouth(ManagedShader shader,Vector2 center,float rotation,float opening,float age,float melt,float presence,bool silhouette)
    {
        const string path="Convergence/Assets/Textures/AzureCathedral/";
        float shrink=1-melt*.32f;
        Vector2 axis=rotation.ToRotationVector2(),normal=new(-axis.Y,axis.X);
        Vector2 mouth=center+axis*(AzureRules.MouthReach*shrink);
        shader.TrySetParameter("signal",new Vector4(presence,opening,AzureVisuals.Reduced?1:0,0));
        shader.SetTexture(ModContent.Request<Texture2D>(path+"VitrionMouth").Value,0,SamplerState.PointClamp);
        Quad(shader,mouth,new Vector2(72,28+opening*104)*shrink,rotation,new(0,0,1,1),Color.White*opening,"PartPass");
        shader.SetTexture(ModContent.Request<Texture2D>(path+"VitrionMouthClosed").Value,0,SamplerState.PointClamp);
        Quad(shader,mouth,new Vector2(76,62)*shrink,rotation,new(0,0,1,1),Color.White*(1-opening),"PartPass");
        shader.SetTexture(ModContent.Request<Texture2D>(path+"VitrionMandible").Value,0,SamplerState.PointClamp);
        for(int half=0;half<2;half++)
        {
            float sign=half==0?-1:1;
            // One original pincer mirrored about the throat, with an authored
            // hinge at (0.155,0.45), so roots do not slide when the jaws rotate.
            float tremble=AzureVisuals.Reduced?0:MathF.Sin(age*.23f)*opening*.009f;
            float angle=rotation+sign*(.24f+opening*.60f+tremble);
            Vector2 size=new Vector2(112,75)*shrink;
            Vector2 pivot=center+axis*(99*shrink)+normal*(sign*15*shrink);
            Vector2 offset=new Vector2(size.X*.345f,-sign*size.Y*.05f).RotatedBy(angle);
            Quad(shader,pivot+offset,size,angle,half==0?new(0,0,1,1):new(0,1,1,-1),Color.White,"PartPass");
        }
        if(opening>.03f && !silhouette)
        {
            shader.TrySetParameter("signal",new Vector4(presence*opening*(1-melt)*.26f,0,0,9));
            Quad(shader,mouth+axis*32,new(190,110),rotation,new(0,0,1,1),Color.White,"FrostPass");
        }
    }
    internal static void Frost(SpriteBatch batch, AzureBoss girl, float age)
    {
        var state=girl.State;var field=state.Field;
        float presence=AzureRules.Ease(age/120)*(state.EndAt<0?1:AzureRules.EndingSky(state.Stage,age-state.EndAt));
        using var scope=new WorldGraphicsScope(batch);var shader=Begin();shader.TrySetParameter("clock",age/60);
        for(int layer=0;layer<3;layer++)
        {
            shader.TrySetParameter("signal",new Vector4(presence*(AzureVisuals.Reduced?.18f:.32f),1,layer,layer*1.71f));
            Quad(shader,new Vector2(field.CenterX,field.Bottom-18-layer*24)-Main.screenPosition,new(field.Right-field.Left,155+layer*42),0,new(0,0,1,1),Color.White,"FrostPass");
        }
        foreach(NPC npc in Main.ActiveNPCs)
            if(npc.ModNPC is AzureWorm w && w.Fight==state.Fight && w.Index%(AzureVisuals.Reduced?6:3)==0)
            {
                float opacity=presence*(state.Enraged?.36f:.24f);
                shader.TrySetParameter("signal",new Vector4(opacity,0,w.Index*.13f,w.Index));
                Quad(shader,npc.Center-Main.screenPosition,new(360,210),npc.rotation,new(0,0,1,1),Color.White,"FrostPass");
            }
    }
    internal static void Effect(SpriteBatch batch, string pass, Vector2 center, Vector2 size, float rotation, float age, Vector4 signal)
    {
        using var scope = new WorldGraphicsScope(batch);
        var shader=Begin();shader.TrySetParameter("clock",age/60);shader.TrySetParameter("signal",signal);
        Quad(shader,center-Main.screenPosition,size,rotation,new(0,0,1,1),Color.White,pass);
    }
    internal static void Shard(SpriteBatch batch, Vector2 a, Vector2 b, float radius, float age, float alpha)
    {
        if (Vector2.DistanceSquared(a,b)<1) return;
        using var scope = new WorldGraphicsScope(batch);
        var shader = Begin(); shader.TrySetParameter("clock", age / 60); shader.TrySetParameter("signal", new Vector4(alpha, 0, 0, 0));
        Quad(shader, (a+b)*.5f-Main.screenPosition, new(Vector2.Distance(a,b),radius*2), (b-a).ToRotation(), new(0,0,1,1), Color.White,"ShardPass");
    }
    internal static void EnergyBolt(SpriteBatch batch,Vector2 a,Vector2 b,float radius,float age,float seed)
    {
        if(radius<=.01f)return;
        using var scope=new WorldGraphicsScope(batch);var shader=Begin();
        float length=Vector2.Distance(a,b)+100,height=radius*6+24;
        shader.TrySetParameter("clock",age/60);shader.TrySetParameter("signal",new Vector4(1,radius,AzureVisuals.Reduced?1:0,seed));
        shader.TrySetParameter("orbSize",new Vector2(length,height));
        Quad(shader,(a+b)*.5f-Main.screenPosition,new(length,height),(b-a).ToRotation(),new(0,0,1,1),Color.White,"EnergyOrbPass");
    }
    internal static void Slash(SpriteBatch batch,Vector2 a,Vector2 b,float radius,float age,float born,float fire,float end)
    {
        if(Vector2.DistanceSquared(a,b)<1)return;
        using var scope=new WorldGraphicsScope(batch);
        var shader=ShaderManager.GetShader("Convergence.ScarletSorcery");
        shader.TrySetParameter("uWorldViewProjection",Main.GameViewMatrix.TransformationMatrix*Matrix.CreateOrthographicOffCenter(0,Main.instance.GraphicsDevice.Viewport.Width,Main.instance.GraphicsDevice.Viewport.Height,0,-1,1));
        shader.TrySetParameter("clock",age/60);
        shader.TrySetParameter("signal",new Vector4(Math.Clamp((age-born)/(fire-born),0,1),age-fire,AzureRules.Ease((age-born)/5),AzureVisuals.Reduced?1:0));
        shader.TrySetParameter("shape",new Vector4(Vector2.Distance(a,b),radius,born*.17f,end-fire));
        shader.TrySetParameter("cutTint",new Vector3(.035f,.58f,1));
        shader.TrySetParameter("cutCore",new Vector3(.84f,.98f,1));
        shader.TrySetParameter("cutHot",new Vector3(.22f,.80f,1));
        shader.TrySetParameter("cutSmoke",new Vector3(.035f,.12f,.17f));
        shader.TrySetParameter("cutForecast",new Vector3(.83f,.95f,1));
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value,1,SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value,2,SamplerState.LinearWrap);
        // Same material, wave/recoil and harmless smoke as Vespera; only palette differs.
        Quad(shader,(a+b)*.5f-Main.screenPosition,new(Vector2.Distance(a,b),(radius+72)*2),(b-a).ToRotation(),new(0,0,1,1),Color.White,age<fire?"TearForecastPass":"TearPass");
    }
}
