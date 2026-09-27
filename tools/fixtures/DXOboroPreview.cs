// Linked production motion, DXOboroArt and DXOboroMaterial on a hidden FNA device.
// A small stick arm stands in for Terraria player draw data; gameplay is not simulated.
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
    static void Main(string[] args)
    {
        Root=args[0];string native=args[1],output=args[2];
        IntPtr Resolve(string name,System.Reflection.Assembly a,DllImportSearchPath? p)
        {string file=Path.Combine(native,name.EndsWith(".dll")?name:name+".dll");return File.Exists(file)?NativeLibrary.Load(file):IntPtr.Zero;}
        NativeLibrary.SetDllImportResolver(typeof(DXOboroPreview).Assembly,Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly,Resolve);
        if(SDL_Init(0x20)!=0)throw new Exception("SDL initialization failed");
        IntPtr window=SDL_CreateWindow("Offline DXOboro",0,0,720,720,FNA3D_PrepareWindowAttributes()|0x8);
        if(window==IntPtr.Zero)throw new Exception("Hidden device unavailable");
        try
        {
            using var device=new GraphicsDevice(GraphicsAdapter.DefaultAdapter,GraphicsProfile.HiDef,new PresentationParameters {
                DeviceWindowHandle=window,BackBufferWidth=720,BackBufferHeight=720,BackBufferFormat=SurfaceFormat.Color,
                IsFullScreen=false,DepthStencilFormat=DepthFormat.None,PresentationInterval=PresentInterval.Immediate});
            Device=device;
            using var target=new RenderTarget2D(device,720,720,false,SurfaceFormat.Color,DepthFormat.None);
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
                float age=action-(step==0?0:step==1?DXOboroMotion.Duration(0)
                    :DXOboroMotion.Duration(0)+DXOboroMotion.Duration(1));
                Terraria.Main.GameUpdateCount=(ulong)(frame/2+100);
                float aim=facing==1?0:MathF.PI;
                float angle=DXOboroMotion.Angle(step,age,aim,facing);
                float armAngle=DXOboroMotion.ArmAngle(step,age,aim,facing);
                Vector2 shoulder=new(-4f*facing,-9),handCenter=new(0,-9),handAlong=new(11,0),handAcross=new(0,10);
                Vector2 hand=handCenter+handAlong*MathF.Cos(armAngle)+handAcross*MathF.Sin(armAngle);
                device.SetRenderTarget(target);device.Clear(bright?new Color(181,195,207):new Color(14,17,31));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                batch.Draw(pixel,new Vector2(-8,-11),null,new Color(40,46,61),0,Vector2.Zero,
                    new Vector2(16,27),SpriteEffects.None,0);
                batch.Draw(pixel,new Vector2(-7,-27),null,new Color(188,175,163),0,Vector2.Zero,
                    new Vector2(14,14),SpriteEffects.None,0);
                Line(batch,pixel,new(-4,16),new(-7,39),5,new Color(39,42,55));
                Line(batch,pixel,new(4,16),new(9,39),5,new Color(39,42,55));
                var before=Convergence.Client.Graphics.WorldBatchParameters.Capture(batch);
                DXOboroMaterial.Draw(batch,age,step,aim,facing,handCenter,handAlong,handAcross,reduced);
                if(before!=Convergence.Client.Graphics.WorldBatchParameters.Capture(batch))
                    throw new Exception("DX material changed caller SpriteBatch state");
                Line(batch,pixel,shoulder,hand,5,new Color(213,187,153));
                DXOboroArt.Sword(batch,hand,angle,DXOboroMotion.BladeLength(step,age),Color.White,facing<0);
                DXOboroMaterial.BladeAndFracture(batch,age,step,aim,facing,hand,reduced);
                if(before!=Convergence.Client.Graphics.WorldBatchParameters.Capture(batch))
                    throw new Exception("DX blade/fracture changed caller SpriteBatch state");
                batch.Draw(pixel,hand,null,Color.Gold,0,new(.5f),new Vector2(4),SpriteEffects.None,0);
                batch.End();device.SetRenderTarget(null);
                string name=$"{(facing==1?"right":"left")}-{(bright?"light":"dark")}-{(reduced?"reduced":"normal")}-{frame:D4}.png";
                using var file=File.Create(Path.Combine(output,name));target.SaveAsPng(file,720,720);frames++;
            }
            // A cancelled owner has no persistent ribbon or stale blade frame.
            // Synthetic native-hit samples exercise the same bounded impact
            // material, not a claim that a native callback was delivered.
            if(args.Length < 4 || !bool.Parse(args[3]))
            foreach(bool reduced in new[]{false,true})
            for(int frame=0;frame<15;frame++)
            {
                device.SetRenderTarget(target);device.Clear(new Color(14,17,31));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                var before=Convergence.Client.Graphics.WorldBatchParameters.Capture(batch);
                DXOboroMaterial.Impact(batch,Vector2.Zero,.2f,frame*.5f,2,reduced);
                if(before!=Convergence.Client.Graphics.WorldBatchParameters.Capture(batch))
                    throw new Exception("DX impact changed caller SpriteBatch state");
                batch.End();device.SetRenderTarget(null);
                using var file=File.Create(Path.Combine(output,$"impact-{(reduced?"reduced":"normal")}-{frame:D2}.png"));
                target.SaveAsPng(file,720,720);
            }
            if(args.Length < 4 || !bool.Parse(args[3]))
            for(int frame=0;frame<6;frame++)
            {
                device.SetRenderTarget(target);device.Clear(new Color(14,17,31));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                batch.Draw(pixel,new Vector2(-8,-11),null,new Color(40,46,61),0,Vector2.Zero,
                    new Vector2(16,27),SpriteEffects.None,0);
                batch.End();device.SetRenderTarget(null);
                using var file=File.Create(Path.Combine(output,$"cancel-{frame:D2}.png"));
                target.SaveAsPng(file,720,720);
            }
            foreach(bool bright in new[]{false,true})
            {
                device.SetRenderTarget(target);device.Clear(bright?new Color(181,195,207):new Color(14,17,31));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,
                    Matrix.CreateScale(3f)*Matrix.CreateTranslation(360,360,0));
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
                target.SaveAsPng(file,720,720);
            }
            Luminance.Core.Graphics.ShaderManager.Clear();
            foreach(var texture in textures.Values)texture.Dispose();textures.Clear();
            Console.WriteLine($"PASS {frames} sequential 120fps linked-production Soboro frames ({(oneSequence ? "right/dark/full" : "both facings, bright/dark, reduced/full")}), cancel/icon outputs. SpriteBatch restoration verified. Offline only; no native player or multiplayer acceptance.");
        }
        finally{SDL_DestroyWindow(window);SDL_Quit();}
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
        public static PreviewView GameViewMatrix=new();
    }
    public sealed class GraphicsDeviceManager { public GraphicsDevice GraphicsDevice=>DXOboroPreview.Device; }
    public sealed class PreviewView { public Matrix TransformationMatrix=>Matrix.CreateTranslation(360,360,0); }
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
        public void TrySetParameter(string name,float value)=>Effect.Parameters[name].SetValue(value);
        public void TrySetParameter(string name,Matrix value)=>Effect.Parameters[name].SetValue(value);
        public void Apply(string pass="AutoloadPass")=>Effect.CurrentTechnique.Passes[pass].Apply();
    }
}
