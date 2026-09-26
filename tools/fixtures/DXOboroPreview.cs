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
            foreach(int facing in new[]{1,-1})
            foreach(bool bright in new[]{false,true})
            foreach(bool reduced in new[]{false,true})
            foreach(int step in new[]{0,1,2})
            for(int age=0;age<=DXOboroMotion.Duration;age+=2)
            {
                Terraria.Main.GameUpdateCount=(ulong)(step*40+age+100);
                float aim=facing==1?0:MathF.PI;
                float angle=DXOboroMotion.Angle(step,age,aim,facing);
                Vector2 shoulder=new(-16f*facing,-12),hand=new(10f*facing+MathF.Cos(angle)*9,-6+MathF.Sin(angle)*7);
                device.SetRenderTarget(target);device.Clear(bright?new Color(181,195,207):new Color(14,17,31));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                var before=Convergence.Client.Graphics.WorldBatchParameters.Capture(batch);
                DXOboroMaterial.Draw(batch,age,step,aim,facing,hand,reduced);
                if(before!=Convergence.Client.Graphics.WorldBatchParameters.Capture(batch))
                    throw new Exception("DX material changed caller SpriteBatch state");
                Line(batch,pixel,shoulder,hand,5,new Color(213,187,153));
                DXOboroArt.Sword(batch,hand,angle,136,Color.White,facing<0);
                batch.Draw(pixel,hand,null,Color.Gold,0,new(.5f),new Vector2(4),SpriteEffects.None,0);
                batch.End();device.SetRenderTarget(null);
                string name=$"{(facing==1?"right":"left")}-{(bright?"light":"dark")}-{(reduced?"reduced":"normal")}-step{step}-{age:D2}.png";
                using var file=File.Create(Path.Combine(output,name));target.SaveAsPng(file,720,720);frames++;
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
                batch.Draw(peer,Vector2.Zero,null,Color.White,0,new Vector2(peer.Width,peer.Height)*.5f,
                    32f/Math.Max(peer.Width,peer.Height),SpriteEffects.None,0);
                var worldItem=new Terraria.Item{Center=new Vector2(75,0)};
                float rotation=.2f,worldScale=1f;
                hooks.PreDrawInWorld(worldItem,batch,Color.White,Color.White,
                    ref rotation,ref worldScale,0);
                batch.End();device.SetRenderTarget(null);
                using var file=File.Create(Path.Combine(output,$"icons-{(bright?"light":"dark")}.png"));
                target.SaveAsPng(file,720,720);
            }
            Luminance.Core.Graphics.ShaderManager.Clear();
            foreach(var texture in textures.Values)texture.Dispose();textures.Clear();
            Console.WriteLine($"PASS {frames} linked-production DXOboro motion frames plus 2 zoomed icon comparisons (DX inventory, Oboro reference, DX world): all three cuts, both facings, bright/dark, reduced/full, arrival through recovery, and SpriteBatch restoration. Offline only.");
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
        private static ManagedShader shader;
        public static ManagedShader GetShader(string name)=>shader??=new ManagedShader();
        public static void Clear(){shader?.Effect.Dispose();shader=null;}
    }
    public sealed class ManagedShader
    {
        public readonly Effect Effect=new(DXOboroPreview.Device,
            File.ReadAllBytes(Path.Combine(DXOboroPreview.Root,"Assets/AutoloadedEffects/Shaders/DXOboroVeil.fxc")));
        public void TrySetParameter(string name,float value)=>Effect.Parameters[name].SetValue(value);
        public void TrySetParameter(string name,Matrix value)=>Effect.Parameters[name].SetValue(value);
        public void Apply(string pass="AutoloadPass")=>Effect.CurrentTechnique.Passes[pass].Apply();
    }
}
