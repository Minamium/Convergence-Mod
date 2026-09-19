// Links the production pose/history AND OboroArt renderer. Only engine asset/config
// access is substituted. The small shoulder/hand diagram is not Terraria player art.
#nullable disable
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Convergence.Client.Weapons;
using Convergence.Content.Items.Oboro;

internal static class OboroMotionPreview
{
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention=CallingConvention.Cdecl)] static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_CreateWindow(string title,int x,int y,int w,int h,uint flags);
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] static extern void SDL_Quit();
    internal static string Root;
    internal static GraphicsDevice Device;
    internal static bool Reduced;
    private static readonly Dictionary<string,Texture2D> textures = new();
    internal static Texture2D Texture(string path)
    {
        if (textures.TryGetValue(path, out var existing)) return existing;
        using var stream=File.OpenRead(Path.Combine(Root,path.Replace("Convergence/","")+".png"));
        var texture=Texture2D.FromStream(Device,stream);
        var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        for(int i=0;i<pixels.Length;i++)
        {
            pixels[i]=Color.FromNonPremultiplied(pixels[i].ToVector4());
        }
        texture.SetData(pixels);textures.Add(path,texture);return texture;
    }
    static void Main(string[] args)
    {
        Root=args[0];string native=args[1],output=args[2];
        IntPtr Resolve(string name,System.Reflection.Assembly a,DllImportSearchPath? p)
        {string file=Path.Combine(native,name.EndsWith(".dll")?name:name+".dll");return File.Exists(file)?NativeLibrary.Load(file):IntPtr.Zero;}
        NativeLibrary.SetDllImportResolver(typeof(OboroMotionPreview).Assembly,Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly,Resolve);
        if(SDL_Init(0x20)!=0)throw new Exception("SDL initialization failed");
        IntPtr window=SDL_CreateWindow("Offline Oboro motion",0,0,640,640,FNA3D_PrepareWindowAttributes()|0x8);
        if(window==IntPtr.Zero)throw new Exception("Hidden device unavailable");
        try
        {
            using var device=new GraphicsDevice(GraphicsAdapter.DefaultAdapter,GraphicsProfile.HiDef,new PresentationParameters {
                DeviceWindowHandle=window,BackBufferWidth=640,BackBufferHeight=640,BackBufferFormat=SurfaceFormat.Color,
                IsFullScreen=false,DepthStencilFormat=DepthFormat.None,PresentationInterval=PresentInterval.Immediate});
            Device=device;
            using var target=new RenderTarget2D(device,640,640,false,SurfaceFormat.Color,DepthFormat.None);
            using var batch=new SpriteBatch(device);
            using var pixel=new Texture2D(device,1,1);pixel.SetData(new[]{Color.White});
            Terraria.GameContent.TextureAssets.MagicPixel=new(){Value=pixel};
            // Same documented Cubic InOut formula for harmless entry only.
            // Production binds the installed Luminance implementation instead.
            OboroSwingPresentation.FirstEntryEase=t=>t<.5f?4*t*t*t:1-MathF.Pow(-2*t+2,3)/2;
            int frames=0;
            foreach(int facing in new[]{1,-1})
            foreach(bool bright in new[]{false,true})
            foreach(bool reduced in new[]{false,true})
            {
                Reduced=reduced;var visual=new OboroSwingPresentation();
                var hand=new OboroHandBasis(-4*facing,-2,10,3*facing,-3*facing,10);
                for(int frame=0;frame<=28;frame++)
                {
                    Terraria.Main.GameUpdateCount=(ulong)frame+100;
                    bool second=frame>=18;float age=second?frame-18:frame;
                    var view=new OboroSnapshot(0,1,1,second?2u:1u,second?(byte)1:(byte)0,(ushort)age,second?(ushort)16:(ushort)18,
                        facing==1?0:MathF.PI,(sbyte)facing,0);
                    visual.Update(view,age,true,true,0,0,facing,Terraria.Main.GameUpdateCount,hand);
                    device.SetRenderTarget(target);device.Clear(bright?new Color(176,190,204):new Color(15,18,32));
                    batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone,
                        null,Matrix.CreateScale(.46f)*Matrix.CreateTranslation(320,320,0));
                    OboroArt.Afterimages(batch,visual);OboroArt.Swing(batch,visual.Pose,true);
                    OboroArt.Line(batch,new(-10,-18),new(10,-18),4,Color.Gray);
                    OboroArt.Line(batch,new(0,-12),new(0,20),10,Color.Gray);
                    var shoulder=new Vector2(-4*facing,-2);var grip=new Vector2(visual.Pose.X,visual.Pose.Y);
                    OboroArt.Line(batch,shoulder,grip,4,new Color(255,188,109));
                    batch.Draw(pixel,grip,null,Color.Cyan,0,new(.5f),new Vector2(4),SpriteEffects.None,0);
                    batch.End();device.SetRenderTarget(null);
                    string name=$"{(facing==1?"right":"left")}-{(bright?"light":"dark")}-{(reduced?"reduced":"normal")}-{frame:D2}.png";
                    using var file=File.Create(Path.Combine(output,name));target.SaveAsPng(file,640,640);frames++;
                }
            }
            new SpectralSpriteCutouts().Unload();
            foreach(var texture in textures.Values)texture.Dispose();textures.Clear();
            Console.WriteLine($"PASS {frames} production-renderer frames: first cut, second-step handoff, both facings, bright/dark, reduced/full. Offline only.");
        }
        finally{SDL_DestroyWindow(window);SDL_Quit();}
    }
}

// Minimal engine seams for the linked production renderer, not gameplay substitutes.
namespace ReLogic.Content { public enum AssetRequestMode{ImmediateLoad} public sealed class Asset<T>{public T Value;} }
namespace Terraria
{
    public static class Main
    {
        public static Vector2 screenPosition;public static ulong GameUpdateCount;
        public static GraphicsDeviceManager graphics=new();
        public static void QueueMainThreadAction(Action action)=>action();
    }
    public sealed class GraphicsDeviceManager { public GraphicsDevice GraphicsDevice=>OboroMotionPreview.Device; }
    public static class PreviewVectorExtensions
    {
        public static Vector2 Size(this Texture2D t)=>new(t.Width,t.Height);
        public static float ToRotation(this Vector2 v)=>MathF.Atan2(v.Y,v.X);
        public static Vector2 ToRotationVector2(this float r)=>new(MathF.Cos(r),MathF.Sin(r));
    }
}
namespace Terraria.GameContent { public static class TextureAssets { public static ReLogic.Content.Asset<Texture2D> MagicPixel; } }
namespace Terraria.ModLoader
{
    public enum ModSide { Client }
    public sealed class AutoloadAttribute:Attribute { public ModSide Side{get;set;} }
    public class ModSystem { public virtual void Unload(){} }
    public static class ModContent
    {
        public static ReLogic.Content.Asset<T> Request<T>(string path,ReLogic.Content.AssetRequestMode mode)=>new(){Value=(T)(object)OboroMotionPreview.Texture(path)};
        public static T GetInstance<T>() where T:new()=>new();
    }
}
namespace Convergence.Client.Encounters.FirstSeverance { public sealed class FirstSeveranceVisualConfig { public bool ReducedEffects=>OboroMotionPreview.Reduced; } }
