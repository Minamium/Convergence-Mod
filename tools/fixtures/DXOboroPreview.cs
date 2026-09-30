// Linked production motion, DXOboroArt and SoboroSlashArt on a hidden FNA device.
// A small stick figure stands in for Terraria player draw data; gameplay is not simulated.
// Each frame follows the in-game order: half-resolution pixel layer (crescent + accents),
// point-upscaled CompositePass with outline/glow, then the physical blade on top.
#nullable disable
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Convergence.Client.Weapons;
using Convergence.Content.Items.DXOboro;

internal static class DXOboroPreview
{
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention=CallingConvention.Cdecl)] static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_CreateWindow(string title,int x,int y,int w,int h,uint flags);
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] static extern void SDL_Quit();
    internal static string Root;
    internal static GraphicsDevice Device;
    private const int Size=720;
    private static readonly Dictionary<string,Texture2D> textures = new();
    internal static Texture2D Texture(string path)
    {
        if (textures.TryGetValue(path, out var existing)) return existing;
        using var stream=File.OpenRead(Path.Combine(Root,path.Replace("Convergence/","")+".png"));
        var texture=Texture2D.FromStream(Device,stream);
        var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        for(int i=0;i<pixels.Length;i++) pixels[i]=Color.FromNonPremultiplied(pixels[i].ToVector4());
        texture.SetData(pixels);textures.Add(path,texture);return texture;
    }

    // Stand-in hand basis around the world origin, as OboroHandAnchor supplies in game.
    private static SoboroCutPose Pose(int step,float aim,int facing,int seed)
        =>new(step,aim,facing,new Vector2(0,-9),new Vector2(11,0),new Vector2(0,10),seed);

    static void Main(string[] args)
    {
        Root=args[0];string native=args[1],output=args[2];
        IntPtr Resolve(string name,System.Reflection.Assembly a,DllImportSearchPath? p)
        {string file=Path.Combine(native,name.EndsWith(".dll")?name:name+".dll");return File.Exists(file)?NativeLibrary.Load(file):IntPtr.Zero;}
        NativeLibrary.SetDllImportResolver(typeof(DXOboroPreview).Assembly,Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly,Resolve);
        if(SDL_Init(0x20)!=0)throw new Exception("SDL initialization failed");
        IntPtr window=SDL_CreateWindow("Offline DXOboro",0,0,Size,Size,FNA3D_PrepareWindowAttributes()|0x8);
        if(window==IntPtr.Zero)throw new Exception("Hidden device unavailable");
        try
        {
            using var device=new GraphicsDevice(GraphicsAdapter.DefaultAdapter,GraphicsProfile.HiDef,new PresentationParameters {
                DeviceWindowHandle=window,BackBufferWidth=Size,BackBufferHeight=Size,BackBufferFormat=SurfaceFormat.Color,
                IsFullScreen=false,DepthStencilFormat=DepthFormat.None,PresentationInterval=PresentInterval.Immediate});
            Device=device;
            // World origin sits at the frame centre, like a player in the middle of the screen.
            Terraria.Main.screenPosition=new Vector2(-Size/2,-Size/2);
            using var target=new RenderTarget2D(device,Size,Size,false,SurfaceFormat.Color,DepthFormat.None);
            using var layer=new RenderTarget2D(device,Size/2,Size/2,false,SurfaceFormat.Color,DepthFormat.None);
            using var batch=new SpriteBatch(device);
            using var pixel=new Texture2D(device,1,1);pixel.SetData(new[]{Color.White});
            int frames=0;
            bool oneSequence=args.Length>4 && bool.Parse(args[4]);
            int comboTicks=DXOboroMotion.Duration(0)+DXOboroMotion.Duration(1)+DXOboroMotion.Duration(2);
            if(args.Length < 4 || !bool.Parse(args[3]))
            foreach(int facing in new[]{1,-1})
            foreach(bool bright in new[]{false,true})
            foreach(bool reduced in new[]{false,true})
            for(int frame=0;frame<comboTicks*4;frame++)
            {
                if(oneSequence && (facing!=1 || bright || reduced)) continue;
                float action=(frame*.5f)%comboTicks;
                int step=action<DXOboroMotion.Duration(0)?0
                    :action<DXOboroMotion.Duration(0)+DXOboroMotion.Duration(1)?1:2;
                float start=step==0?0:step==1?DXOboroMotion.Duration(0):DXOboroMotion.Duration(0)+DXOboroMotion.Duration(1);
                float age=action-start;
                int previous=(step+2)%3;
                float previousAge=age+DXOboroMotion.Duration(previous);
                Terraria.Main.GameUpdateCount=(ulong)(frame/2+100);
                float aim=facing==1?0:MathF.PI;
                var pose=Pose(step,aim,facing,step*131+7);
                var residue=Pose(previous,aim,facing,previous*131+7);
                // One synthetic local hit per cut, two ticks into the live window.
                float impactAge=age-(DXOboroMotion.Release(step)+2);
                Vector2 impactAt=pose.Root(DXOboroMotion.Release(step)+2)+pose.Direction(DXOboroMotion.Release(step)+2)*DXOboroMotion.Reach*.9f;
                RenderLayer(device,batch,pixel,layer,pose,age,residue,previousAge,reduced,impactAge>=0,impactAt,impactAge);
                // Raw half-resolution layer samples for material debugging (before outline/glow).
                if(oneSequence && frame%20==0)
                { using var raw=File.Create(Path.Combine(output,$"layer-{frame:D4}.png"));layer.SaveAsPng(raw,layer.Width,layer.Height); }

                float angle=DXOboroMotion.Angle(step,age,aim,facing);
                float armAngle=DXOboroMotion.ArmAngle(step,age,aim,facing);
                Vector2 shoulder=new(-4f*facing,-9);
                Vector2 hand=pose.HandCenter+pose.HandAlong*MathF.Cos(armAngle)+pose.HandAcross*MathF.Sin(armAngle);
                device.SetRenderTarget(target);device.Clear(bright?new Color(181,195,207):new Color(14,17,31));
                DrawFigure(batch,pixel);
                SoboroSlashArt.Composite(device,layer,new Rectangle(0,0,Size,Size),Matrix.Identity,reduced);
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Matrix.Identity);
                Line(batch,pixel,shoulder-Terraria.Main.screenPosition,hand-Terraria.Main.screenPosition,5,new Color(213,187,153));
                DXOboroArt.Sword(batch,hand,angle,DXOboroMotion.BladeLength(step,age),Color.White,facing<0);
                batch.End();device.SetRenderTarget(null);
                string name=$"{(facing==1?"right":"left")}-{(bright?"light":"dark")}-{(reduced?"reduced":"normal")}-{frame:D4}.png";
                using var file=File.Create(Path.Combine(output,name));target.SaveAsPng(file,Size,Size);frames++;
            }
            // A cancelled owner leaves only the bounded residue already recorded, then nothing.
            if(args.Length < 4 || !bool.Parse(args[3]))
            for(int frame=0;frame<6;frame++)
            {
                device.SetRenderTarget(target);device.Clear(new Color(14,17,31));
                DrawFigure(batch,pixel);
                device.SetRenderTarget(null);
                using var file=File.Create(Path.Combine(output,$"cancel-{frame:D2}.png"));
                target.SaveAsPng(file,Size,Size);
            }
            foreach(bool bright in new[]{false,true})
            {
                device.SetRenderTarget(target);device.Clear(bright?new Color(181,195,207):new Color(14,17,31));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,
                    Matrix.CreateScale(3f)*Matrix.CreateTranslation(Size/2,Size/2,0));
                var hooks=new DXOboroItemVisuals();
                hooks.PreDrawInInventory(new Terraria.Item(),batch,new(-75,0),
                    new Rectangle(0,0,768,512),Color.White,Color.White,Vector2.Zero,32f/768f);
                Texture2D peer=Texture("Convergence/Assets/Textures/Items/Oboro/Blade");
                var framed=new Convergence.Client.Graphics.ReadableItemIcon();
                framed.Draw(batch,peer,Vector2.Zero,43.2f,Color.White);
                var mask=Texture("Convergence/Assets/Textures/Items/GhostSamurai/SealedMask");
                framed.Draw(batch,mask,new Vector2(75,0),43.2f,Color.White);
                batch.End();device.SetRenderTarget(null);
                using var file=File.Create(Path.Combine(output,$"icons-{(bright?"light":"dark")}.png"));
                target.SaveAsPng(file,Size,Size);
            }
            Luminance.Core.Graphics.ShaderManager.Clear();
            foreach(var texture in textures.Values)texture.Dispose();textures.Clear();
            Console.WriteLine($"PASS {frames} sequential 120fps linked-production Soboro frames ({(oneSequence ? "right/dark/full" : "both facings, bright/dark, reduced/full")}), cancel/icon outputs. Offline only; no native player or multiplayer acceptance.");
        }
        finally{SDL_DestroyWindow(window);SDL_Quit();}
    }

    private static void RenderLayer(GraphicsDevice device,SpriteBatch batch,Texture2D pixel,RenderTarget2D layer,
        SoboroCutPose pose,float age,SoboroCutPose residue,float residueAge,bool reduced,bool hasImpact,Vector2 impactAt,float impactAge)
    {
        device.SetRenderTarget(layer);device.Clear(Color.Transparent);
        device.BlendState=BlendState.AlphaBlend;device.DepthStencilState=DepthStencilState.None;
        device.RasterizerState=RasterizerState.CullNone;
        Matrix projection=Matrix.CreateOrthographicOffCenter(0,layer.Width,layer.Height,0,-1,1);
        Vector2 origin=Terraria.Main.screenPosition;
        bool drawResidue=SoboroSlashArt.Visible(residue.Step,residueAge);
        if(drawResidue) SoboroSlashArt.DrawCrescent(device,residue,residueAge,origin,projection,reduced);
        if(SoboroSlashArt.Visible(pose.Step,age)) SoboroSlashArt.DrawCrescent(device,pose,age,origin,projection,reduced);
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp,
            DepthStencilState.None,RasterizerState.CullNone,null,Matrix.Identity);
        if(drawResidue) SoboroSlashArt.DrawAccents(batch,pixel,residue,residueAge,origin,reduced,false,Vector2.Zero,-1);
        if(SoboroSlashArt.Visible(pose.Step,age))
            SoboroSlashArt.DrawAccents(batch,pixel,pose,age,origin,reduced,hasImpact,impactAt,impactAge);
        batch.End();
        device.SetRenderTarget(null);
    }

    private static void DrawFigure(SpriteBatch batch,Texture2D pixel)
    {
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
            DepthStencilState.None,RasterizerState.CullNone,null,Matrix.CreateTranslation(Size/2,Size/2,0));
        batch.Draw(pixel,new Vector2(-8,-11),null,new Color(40,46,61),0,Vector2.Zero,new Vector2(16,27),SpriteEffects.None,0);
        batch.Draw(pixel,new Vector2(-7,-27),null,new Color(188,175,163),0,Vector2.Zero,new Vector2(14,14),SpriteEffects.None,0);
        Line(batch,pixel,new(-4,16),new(-7,39),5,new Color(39,42,55));
        Line(batch,pixel,new(4,16),new(9,39),5,new Color(39,42,55));
        batch.End();
    }

    private static void Line(SpriteBatch batch,Texture2D pixel,Vector2 start,Vector2 end,float width,Color color)
    {
        Vector2 d=end-start;
        batch.Draw(pixel,start,null,color,MathF.Atan2(d.Y,d.X),new(0,.5f),new Vector2(d.Length(),width),SpriteEffects.None,0);
    }
}

namespace ReLogic.Content { public enum AssetRequestMode{ImmediateLoad} public sealed class Asset<T>{public T Value;} }
namespace Terraria
{
    public sealed class Item { public object ModItem;public Vector2 Center; }
    public static class Main
    {
        public static Vector2 screenPosition;public static ulong GameUpdateCount;
        public static GraphicsDeviceManager instance=new();
    }
    public sealed class GraphicsDeviceManager { public GraphicsDevice GraphicsDevice=>DXOboroPreview.Device; }
    public static class PreviewVectorExtensions
    {
        public static Vector2 Size(this Texture2D t)=>new(t.Width,t.Height);
        public static Vector2 ToRotationVector2(this float r)=>new(MathF.Cos(r),MathF.Sin(r));
    }
}
namespace Terraria.ModLoader
{
    public enum ModSide { Client }
    public sealed class AutoloadAttribute:Attribute { public ModSide Side{get;set;} }
    public class GlobalItem
    {
        public virtual bool AppliesToEntity(Terraria.Item item,bool lateInstantiation)=>false;
        public virtual bool PreDrawInInventory(Terraria.Item item,SpriteBatch batch,Vector2 position,Rectangle frame,
            Color drawColor,Color itemColor,Vector2 origin,float scale)=>true;
        public virtual bool PreDrawInWorld(Terraria.Item item,SpriteBatch batch,Color lightColor,Color alphaColor,
            ref float rotation,ref float scale,int whoAmI)=>true;
    }
    public static class ModContent
    {
        public static ReLogic.Content.Asset<T> Request<T>(string path,ReLogic.Content.AssetRequestMode mode)
            =>new(){Value=(T)(object)DXOboroPreview.Texture(path)};
    }
}
namespace Convergence.Content.Items.DXOboro { public sealed class DXOboro {} }
namespace Luminance.Core.Graphics
{
    public static class ShaderManager
    {
        private static readonly Dictionary<string,ManagedShader> shaders=new();
        public static ManagedShader GetShader(string name)
        { if(!shaders.TryGetValue(name,out var s))shaders[name]=s=new ManagedShader(name);return s; }
        public static void Clear(){foreach(var s in shaders.Values)s.Effect.Dispose();shaders.Clear();}
    }
    public sealed class ManagedShader
    {
        public readonly Effect Effect;
        public ManagedShader(string name)=>Effect=new(DXOboroPreview.Device,
            File.ReadAllBytes(Path.Combine(DXOboroPreview.Root,"Assets/AutoloadedEffects/Shaders/"+name.Split('.')[^1]+".fxc")));
        public void TrySetParameter(string name,float value)=>Effect.Parameters[name]?.SetValue(value);
        public void TrySetParameter(string name,Vector2 value)=>Effect.Parameters[name]?.SetValue(value);
        public void TrySetParameter(string name,Matrix value)=>Effect.Parameters[name]?.SetValue(value);
        public void SetTexture(Texture2D texture,int index,SamplerState sampler)
        { DXOboroPreview.Device.Textures[index]=texture;DXOboroPreview.Device.SamplerStates[index]=sampler; }
        public void Apply(string pass="AutoloadPass")=>Effect.CurrentTechnique.Passes[pass].Apply();
    }
}
