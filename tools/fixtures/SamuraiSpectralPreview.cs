// Hidden FNA D3D11 preview linked directly to the production Samurai renderers.
// Terraria, Luminance resource management and encounter snapshots are shims.
#nullable disable
using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Convergence.Client.Encounters.GhostSamurai;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.GhostSamurai;

internal static class SamuraiSpectralPreview
{
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)] static extern int SDL_Init(uint flags);
    [DllImport("FNA3D",CallingConvention=CallingConvention.Cdecl)] static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_CreateWindow(string title,int x,int y,int w,int h,uint flags);
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_Quit();
    internal static string Root,LuminancePackage;
    static readonly float[] BodyAges={12.25f,47.5f,53.75f,54.5f,60.75f,72.25f};
    static readonly float[] HazardAges={48.25f,53.75f,54f,55.25f,64.5f};
    internal static GraphicsDevice Device;
    internal static readonly Dictionary<string,Texture2D> Textures=new();
    internal static Texture2D Texture(string path)
    {
        if(Textures.TryGetValue(path,out var found)) return found;
        using var stream=File.OpenRead(Path.Combine(Root,path.Replace("Convergence/","")+".png"));
        var texture=Texture2D.FromStream(Device,stream);
        var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        for(int i=0;i<pixels.Length;i++) pixels[i]=Color.FromNonPremultiplied(pixels[i].ToVector4());
        texture.SetData(pixels);Textures.Add(path,texture);return texture;
    }
    internal static Texture2D Noise(string name)
    {
        if(Textures.TryGetValue(name,out var found))return found;
        using var file=File.OpenRead(LuminancePackage);
        using var reader=new BinaryReader(file);
        if(System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4))!="TMOD")throw new InvalidDataException("TMOD magic");
        reader.ReadString();reader.ReadBytes(276);reader.ReadInt32();reader.ReadString();reader.ReadString();
        int count=reader.ReadInt32(),offset=0,position=-1,length=0,stored=0;
        for(int i=0;i<count;i++)
        {
            string path=reader.ReadString();int size=reader.ReadInt32(),compressed=reader.ReadInt32();
            if(path=="Assets/Noise/"+name+".rawimg"){position=offset;length=size;stored=compressed;}
            offset+=compressed;
        }
        if(position<0)throw new FileNotFoundException("Luminance noise: "+name);
        file.Position+=position;
        using var bytes=new MemoryStream(reader.ReadBytes(stored));
        using Stream data=length==stored?bytes:new DeflateStream(bytes,CompressionMode.Decompress);
        using var raw=new BinaryReader(data);
        if(raw.ReadInt32()!=1)throw new InvalidDataException("rawimg version");
        int width=raw.ReadInt32(),height=raw.ReadInt32();
        var texture=new Texture2D(Device,width,height);texture.SetData(raw.ReadBytes(width*height*4));
        Textures.Add(name,texture);return texture;
    }
    static void Main(string[] args)
    {
        Root=args[0];LuminancePackage=args[1];string native=args[2],output=args[3];
        IntPtr Resolve(string name,System.Reflection.Assembly a,DllImportSearchPath? p)
        {string file=Path.Combine(native,name.EndsWith(".dll")?name:name+".dll");return File.Exists(file)?NativeLibrary.Load(file):IntPtr.Zero;}
        NativeLibrary.SetDllImportResolver(typeof(SamuraiSpectralPreview).Assembly,Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly,Resolve);
        if(SDL_Init(0x20)!=0)throw new Exception("SDL video initialization failed");
        IntPtr window=SDL_CreateWindow("Samurai offline preview",0,0,720,720,FNA3D_PrepareWindowAttributes()|0x8);
        if(window==IntPtr.Zero)throw new Exception("Hidden device surface unavailable");
        try
        {
            using var device=new GraphicsDevice(GraphicsAdapter.DefaultAdapter,GraphicsProfile.HiDef,new PresentationParameters{
                DeviceWindowHandle=window,BackBufferWidth=720,BackBufferHeight=720,BackBufferFormat=SurfaceFormat.Color,
                IsFullScreen=false,DepthStencilFormat=DepthFormat.None,PresentationInterval=PresentInterval.Immediate});
            Device=device;
            using var target=new RenderTarget2D(device,720,720,false,SurfaceFormat.Color,DepthFormat.None);
            using var batch=new SpriteBatch(device);Terraria.Main.spriteBatch=batch;
            using var pixel=new Texture2D(device,1,1);pixel.SetData(new[]{Color.White});
            if(args.Length>4 && bool.Parse(args[4]))
            {
                CutSequences(device,target,batch,output);
                GhostSamuraiCuts.Reset();Luminance.Core.Graphics.ShaderManager.Clear();
                foreach(var texture in Textures.Values)texture.Dispose();Textures.Clear();
                return;
            }
            if(args.Length>7 && bool.Parse(args[7]))
            {
                CinemaSequences(device,target,batch,output);
                GhostSamuraiCuts.Reset();Luminance.Core.Graphics.ShaderManager.Clear();
                foreach(var texture in Textures.Values)texture.Dispose();Textures.Clear();
                return;
            }
            if(args.Length>6 && bool.Parse(args[6]))
            {
                FieldCutSequences(device,target,batch,output);
                GhostSamuraiCuts.Reset();Luminance.Core.Graphics.ShaderManager.Clear();
                foreach(var texture in Textures.Values)texture.Dispose();Textures.Clear();
                return;
            }
            if(args.Length>5 && bool.Parse(args[5]))
            {
                BossMotion(device,target,batch,output);
                GhostSamuraiCuts.Reset();Luminance.Core.Graphics.ShaderManager.Clear();
                foreach(var texture in Textures.Values)texture.Dispose();Textures.Clear();
                return;
            }
            new GhostSamuraiComposite().Load();
            int frames=0,composites=0;
            foreach(float zoom in new[]{1f,.65f})
            foreach(bool reduced in new[]{false,true})
            foreach(int facing in new[]{1,-1})
            foreach(bool light in new[]{false,true})
            foreach(float age in BodyAges)
            {
                Terraria.Main.GameViewMatrix.Scale=zoom;
                Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualConfig.Instance.ReducedEffects=reduced;
                var left=SamuraiRigMotion.Blade(SamuraiAttack.DirectionalSlash,SamuraiPhase.Phase1,age,age,-1,facing,default,false);
                var right=SamuraiRigMotion.Blade(SamuraiAttack.DirectionalSlash,SamuraiPhase.Phase1,age,age,1,facing,default,false);
                float cut=Math.Clamp((age-GhostSamuraiRules.SlashWarning)/6,0,1);
                var pose=new SamuraiRigPose(360,360,age,SamuraiRigMotion.ActionLean(left,right,facing),1,left,right,cut*.3f,cut*70,cut*.2f);
                GhostSamuraiPresentation.Pose=pose;
                var blendBefore=device.BlendState;var depthBefore=device.DepthStencilState;
                var rasterBefore=device.RasterizerState;var viewportBefore=device.Viewport;
                var targetBefore=device.GetRenderTargets();
                Luminance.Core.Graphics.RenderTargetManager.Pulse();
                if(device.BlendState!=blendBefore||device.DepthStencilState!=depthBefore||
                    device.RasterizerState!=rasterBefore||!device.Viewport.Equals(viewportBefore)||
                    device.GetRenderTargets().Length!=targetBefore.Length)
                    throw new Exception("Composite changed caller device state");
                device.SetRenderTarget(target);device.Clear(light?new Color(188,201,212):new Color(17,22,36));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                var state=WorldBatchParameters.Capture(batch);
                GhostSamuraiRigArt.Draw(batch,pose,Vector2.Zero,null,age);
                if(state!=WorldBatchParameters.Capture(batch))throw new Exception("Body changed caller SpriteBatch state");
                batch.End();device.SetRenderTarget(null);
                Save(target,output,$"body-z{zoom:0.00}-{(facing==1?"right":"left")}-{(light?"light":"dark")}-{(reduced?"reduced":"normal")}-{age:00.00}.png");
                frames++;composites++;
            }
            foreach(float zoom in new[]{1f,.65f})
            foreach(bool reduced in new[]{false,true})
            foreach(bool light in new[]{false,true})
            foreach(string shape in new[]{"line","annulus","halfdisc","wave"})
            foreach(float age in HazardAges)
            {
                Terraria.Main.GameViewMatrix.Scale=zoom;
                device.SetRenderTarget(target);device.Clear(light?new Color(188,201,212):new Color(17,22,36));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                var state=WorldBatchParameters.Capture(batch);
                if(shape=="line")GhostSamuraiEnergy.Line(batch,new(150,300),new(570,420),35,age,
                    Math.Clamp(age/GhostSamuraiRules.SlashWarning,0,1),age>=54&&age<64,age-54,64-age,reduced);
                else if(shape=="wave")
                {
                    Vector2 center=new(250+(age-48)*8,425);
                    GhostSamuraiEnergy.Line(batch,center-new Vector2(140,0),center+new Vector2(140,0),
                        32,age,Math.Clamp(age/54,0,1),age>=54&&age<64,age-54,64-age,reduced);
                }
                else
                {
                    var kind=shape=="annulus"?SamuraiShape.OuterSlash:SamuraiShape.FrontalCleave;
                    var hazard=new SamuraiHazard(kind,360,360,1,0,shape=="annulus"?105:0,170,0,54,64,1);
                    var at=new Vector2(360,360);
                    GhostSamuraiEnergy.Field(batch,hazard,at,age,new Rectangle(0,0,720,720),reduced);
                }
                if(state!=WorldBatchParameters.Capture(batch))throw new Exception("Hazard changed caller SpriteBatch state");
                batch.End();device.SetRenderTarget(null);
                if(shape=="annulus"&&age>=54&&age<64)
                    AssertBackground(target,360,360,light?new Color(188,201,212):new Color(17,22,36),"annulus safe hole");
                if(shape=="halfdisc"&&age>=54&&age<64)
                    AssertBackground(target,300,360,light?new Color(188,201,212):new Color(17,22,36),"halfdisc safe side");
                Save(target,output,$"hazard-z{zoom:0.00}-{shape}-{(light?"light":"dark")}-{(reduced?"reduced":"normal")}-{age:00.00}.png");frames++;
            }
            Terraria.Main.GameViewMatrix.Scale=1;
            var diagnostic=new Color[2][];
            for(int live=0;live<2;live++)
            {
                device.SetRenderTarget(target);device.Clear(new Color(17,22,36));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                GhostSamuraiEnergy.Line(batch,new(150,300),new(570,420),35,56,1,live==1,2,8,false);
                batch.End();device.SetRenderTarget(null);
                diagnostic[live]=new Color[720*720];target.GetData(diagnostic[live]);
                Save(target,output,$"diagnostic-line-{(live==1?"live":"warning")}.png");
            }
            int changed=0,warningPixels=0,livePixels=0;
            var background=new Color(17,22,36);
            for(int i=0;i<diagnostic[0].Length;i++)
            {
                if(diagnostic[0][i]!=diagnostic[1][i])changed++;
                if(diagnostic[0][i]!=background)warningPixels++;
                if(diagnostic[1][i]!=background)livePixels++;
            }
            if(changed<5000||livePixels<=warningPixels)
                throw new Exception($"Energy warning/live material collapsed: changed={changed}, warning={warningPixels}, live={livePixels}");
            Console.WriteLine($"ENERGY DIFFERENTIAL same clock/geometry: {changed} changed pixels, warning={warningPixels}, live={livePixels}");
            var modeImages=new Color[2][];
            for(int mode=0;mode<2;mode++)
            {
                Luminance.Core.Graphics.ManagedShader.EnergyModeOverride=mode;
                device.SetRenderTarget(target);device.Clear(background);
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                GhostSamuraiEnergy.Line(batch,new(150,300),new(570,420),35,56,1,true,2,8,false);
                batch.End();device.SetRenderTarget(null);
                modeImages[mode]=new Color[720*720];target.GetData(modeImages[mode]);
                Save(target,output,$"diagnostic-mode-{mode}.png");
            }
            Luminance.Core.Graphics.ManagedShader.EnergyModeOverride=null;
            changed=0;for(int i=0;i<modeImages[0].Length;i++)if(modeImages[0][i]!=modeImages[1][i])changed++;
            if(changed<5000)throw new Exception($"Energy line/field mode selection collapsed: {changed} changed pixels");
            Console.WriteLine($"ENERGY MODE DIFFERENTIAL line vs field: {changed} changed pixels");
            // Exercise the remaining changed material path and teardown visual,
            // not just the static body. No encounter timing is simulated here.
            int extraFrames=0;
            foreach(bool light in new[]{false,true})foreach(bool reduced in new[]{false,true})
            foreach(float deathAge in new[]{24f,52f,78f,96f})
            {
                Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualConfig.Instance.ReducedEffects=reduced;
                device.SetRenderTarget(target);device.Clear(light?new Color(188,201,212):background);
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone);
                var state=WorldBatchParameters.Capture(batch);
                GhostSamuraiRigArt.DrawDeath(batch,GhostSamuraiPresentation.Pose,Vector2.Zero,deathAge);
                GhostSamuraiCuts.Wisp(batch,new(110,220),24,deathAge,Vector2.UnitX,0,reduced);
                if(state!=WorldBatchParameters.Capture(batch))throw new Exception("Ending/wisp changed caller state");
                batch.End();device.SetRenderTarget(null);
                Save(target,output,$"ending-wisp-{(light?"light":"dark")}-{(reduced?"reduced":"normal")}-{deathAge:00}.png");
                frames++;extraFrames++;
            }
            Console.WriteLine($"PASS {extraFrames} additional ending/wisp frames");
            if(Luminance.Core.Graphics.ShaderManager.CompositeDraws!=composites*2)
                throw new Exception($"Composite pass count {Luminance.Core.Graphics.ShaderManager.CompositeDraws} != {composites*2}");
            if(Luminance.Core.Graphics.ManagedRenderTarget.Sizes[1024]<1||
                Luminance.Core.Graphics.ManagedRenderTarget.Sizes[512]<2||
                Luminance.Core.Graphics.ManagedRenderTarget.Sizes[256]<1)
                throw new Exception("Composite target dimensions omitted normal or Reduced sizes");
            foreach(float zoom in new[]{1f,.65f})foreach(bool reduced in new[]{false,true})
            {
                var names=new List<string>();
                foreach(int facing in new[]{1,-1})foreach(bool light in new[]{false,true})
                foreach(float age in BodyAges)
                    names.Add($"body-z{zoom:0.00}-{(facing==1?"right":"left")}-{(light?"light":"dark")}-{(reduced?"reduced":"normal")}-{age:00.00}.png");
                Sheet(device,batch,output,$"contact-body-z{zoom:0.00}-{(reduced?"reduced":"normal")}.png",names,BodyAges.Length,4);
            }
            foreach(float zoom in new[]{1f,.65f})foreach(bool reduced in new[]{false,true})foreach(bool light in new[]{false,true})
            {
                var names=new List<string>();
                foreach(float age in HazardAges)
                foreach(string shape in new[]{"line","annulus","halfdisc","wave"})
                    names.Add($"hazard-z{zoom:0.00}-{shape}-{(light?"light":"dark")}-{(reduced?"reduced":"normal")}-{age:00.00}.png");
                Sheet(device,batch,output,$"contact-hazards-z{zoom:0.00}-{(light?"light":"dark")}-{(reduced?"reduced":"normal")}.png",names,4,HazardAges.Length);
            }
            // Dense release samples show the actual articulated shoulder/hand
            // trajectory; a few disconnected keyframes can hide a discontinuity.
            Terraria.Main.GameViewMatrix.Scale=.65f;
            Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualConfig.Instance.ReducedEffects=false;
            var motionImages=new List<string>();
            for(int sub=0;sub<32;sub++)
            {
                float age=50+sub*.5f;
                var left=SamuraiRigMotion.Blade(SamuraiAttack.DirectionalSlash,SamuraiPhase.Phase1,age,age,-1,1,default,false);
                var right=SamuraiRigMotion.Blade(SamuraiAttack.DirectionalSlash,SamuraiPhase.Phase1,age,age,1,1,default,false);
                var pose=new SamuraiRigPose(360,360,age,SamuraiRigMotion.ActionLean(left,right,1),1,left,right,0,0,0);
                GhostSamuraiPresentation.Pose=pose;
                Luminance.Core.Graphics.RenderTargetManager.Pulse();
                device.SetRenderTarget(target);device.Clear(new Color(17,22,36));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                GhostSamuraiRigArt.Draw(batch,pose,Vector2.Zero,null,age);
                batch.End();device.SetRenderTarget(null);
                string name=$"motion-release-{sub:00}.png";Save(target,output,name);motionImages.Add(name);frames++;composites++;
            }
            Sheet(device,batch,output,"contact-motion-release.png",motionImages,8,4);
            new GhostSamuraiComposite().Unload();
            Luminance.Core.Graphics.ShaderManager.Clear();
            foreach(var texture in Textures.Values)texture.Dispose();Textures.Clear();
            Console.WriteLine($"PASS {frames} linked-production FNA frames ({composites} body/composite, {frames-composites} hazard), {composites*2} composite passes; normal/reduced, light/dark, both facings, fractional warning/live/recovery, SpriteBatch state restored. Offline only.");
        }
        finally{SDL_DestroyWindow(window);SDL_Quit();}
    }
    static void Save(RenderTarget2D target,string output,string name)
    {using var file=File.Create(Path.Combine(output,name));target.SaveAsPng(file,720,720);}
    static void BossMotion(GraphicsDevice device,RenderTarget2D target,SpriteBatch batch,string output)
    {
        new GhostSamuraiComposite().Load();
        Terraria.Main.GameViewMatrix.Scale=.75f;
        Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualConfig.Instance.ReducedEffects=false;
        var combo=new SamuraiComboSnapshot(0,1,true,360,360,360,360,360);
        int frames=0;
        foreach(var clip in new[]{
            (Name:"cleave-right",Attack:SamuraiAttack.FrontalCleaveShockwave,Facing:1,From:88,To:156),
            (Name:"directional-right",Attack:SamuraiAttack.DirectionalSlash,Facing:1,From:36,To:126),
            (Name:"directional-left",Attack:SamuraiAttack.DirectionalSlash,Facing:-1,From:36,To:126)})
        {
            string dir=Path.Combine(output,clip.Name);Directory.CreateDirectory(dir);
            var history=new SamuraiRigHistory();var selected=new List<string>();
            int lastTick=0;
            SamuraiRigPose Pose(float age)
            {
                var left=SamuraiRigMotion.Blade(clip.Attack,SamuraiPhase.Phase1,age,age,-1,clip.Facing,combo,false);
                var right=SamuraiRigMotion.Blade(clip.Attack,SamuraiPhase.Phase1,age,age,1,clip.Facing,combo,false);
                float speed=Math.Max(left.Trail,right.Trail)*70;
                // Raw production step target; the game additionally eases its return.
                var (stepX,stepY)=SamuraiRigMotion.Lunge(left,right,clip.Facing);
                return new(360+stepX,360+stepY,age,SamuraiRigMotion.ActionLean(left,right,clip.Facing),1,left,right,0,speed,0);
            }
            for(int frame=0;frame<=(clip.To-clip.From)*2;frame++)
            {
                float age=clip.From+frame*.5f;
                int tick=(int)MathF.Floor(age);
                while(lastTick<=tick)
                {history.Add(GhostSamuraiPresentation.Fight,1,Pose(lastTick),(ulong)lastTick);lastTick++;}
                var pose=Pose(age);
                Terraria.Main.GameUpdateCount=(ulong)tick;
                GhostSamuraiPresentation.Fraction=age-tick;
                GhostSamuraiPresentation.Pose=pose;
                Luminance.Core.Graphics.RenderTargetManager.Pulse();
                device.SetRenderTarget(target);device.Clear(new Color(17,22,36));
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                var state=WorldBatchParameters.Capture(batch);
                GhostSamuraiRigArt.Draw(batch,pose,Vector2.Zero,history,age);
                if(state!=WorldBatchParameters.Capture(batch))throw new Exception("Boss motion changed SpriteBatch state");
                batch.End();device.SetRenderTarget(null);
                string name=$"frame-{frame:0000}.png";Save(target,dir,name);frames++;
                if(frame%12==0 && selected.Count<24)selected.Add(name);
            }
            Sheet(device,batch,dir,"contact.png",selected,8,(selected.Count+7)/8);
            Console.WriteLine($"{clip.Name}: {(clip.To-clip.From)*2+1} 120fps production-linked frames; {selected.Count} contact samples");
        }
        new GhostSamuraiComposite().Unload();
        Console.WriteLine($"PASS {frames} chronological boss frames at 120fps; actual installed noise and production body/composite/lightning, history supplied; no native gameplay launch.");
    }
    static void CutSequences(GraphicsDevice device,RenderTarget2D target,SpriteBatch batch,string output)
    {
        float[] offsets={-24,-8,-2,-.1f,0,.5f,1,2,3.5f,5,7,9,11.5f,12,15,20,26,28};
        int frames=0;
        foreach(float zoom in new[]{1f,.65f})foreach(bool light in new[]{false,true})foreach(bool reduced in new[]{false,true})
        foreach(string kind in new[]{"slash","vertical","grid","wave","annulus","wind","cleave"})
        {
            var images=new List<string>();var bg=light?new Color(188,201,212):new Color(17,22,36);
            for(int step=0;step<offsets.Length;step++)
            {
                float fire=54,age=fire+offsets[step];
                Terraria.Main.GameViewMatrix.Scale=zoom;
                device.SetRenderTarget(target);device.Clear(bg);
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,Terraria.Main.GameViewMatrix.TransformationMatrix);
                var state=WorldBatchParameters.Capture(batch);
                if(kind=="grid")
                {
                    // Actual production 15+15 grid descriptors, not a 280px
                    // stand-in beam. End/width/spacing are exactly the rules.
                    for(int axis=0;axis<2;axis++)for(int line=0;line<GhostSamuraiRules.GridVerticalLineCount;line++)
                    {
                        var h=GhostSamuraiRules.GridLine(axis==0,line,360,360,0);
                        GhostSamuraiCuts.Slash(batch,h,new(h.X,h.Y),h.Fire+offsets[step],reduced);
                    }
                }
                else if(kind=="annulus"||kind=="cleave"||kind=="wind")
                {
                    var shape=kind=="annulus"?SamuraiShape.OuterSlash:kind=="wind"?SamuraiShape.InnerKamaitachi:SamuraiShape.FrontalCleave;
                    var h=new SamuraiHazard(shape,360,360,1,0,kind=="annulus"?145:0,315,0,54,
                        kind=="wind"?54+GhostSamuraiRules.Phase3KamaitachiDuration:66,1);
                    GhostSamuraiCuts.Field(batch,h,new(360,360),age,new Rectangle(0,0,720,720),reduced);
                }
                else if(kind=="wave")
                {
                    var h=new SamuraiHazard(SamuraiShape.SlashWave,180,360,1,0,SamuraiWaveRules.ChargedSlashWaveWidth,
                        SamuraiWaveRules.ChargedSlashWaveHeight/2,0,54,66,1);
                    GhostSamuraiCuts.Wave(batch,h,new(270+Math.Max(0,offsets[step])*8,360),age,reduced);
                }
                else
                {
                    bool vertical=kind=="vertical";
                    var h=new SamuraiHazard(vertical?SamuraiShape.VerticalSlash:SamuraiShape.Slash,
                        vertical?360:-340,vertical?-200:360,vertical?0:1,vertical?1:0,
                        vertical?1120:GhostSamuraiRules.SlashLength,vertical?SamuraiComboRules.VerticalHalfWidth:GhostSamuraiRules.SlashHalfWidth,
                        0,54,vertical?54+SamuraiComboRules.VerticalLive:54+GhostSamuraiRules.SlashLive,1);
                    GhostSamuraiCuts.Slash(batch,h,new(h.X,h.Y),age,reduced);
                }
                if(state!=WorldBatchParameters.Capture(batch))throw new Exception("Cut changed caller SpriteBatch state");
                batch.End();device.SetRenderTarget(null);
                if(kind=="annulus")AssertBackground(target,360,360,bg,"cut annulus hole");
                if(kind=="cleave")AssertBackground(target,300,360,bg,"cut cleave safe side");
                if(kind=="grid")AssertBackground(target,360+(int)(90*zoom),360+(int)(90*zoom),bg,"grid safe cell");
                if(offsets[step]>=28&&kind!="wind")AssertBackground(target,360,360,bg,"cut residue expires");
                string name=$"cut-{kind}-z{zoom:0.00}-{(light?"light":"dark")}-{(reduced?"reduced":"normal")}-{step:00}.png";
                Save(target,output,name);images.Add(name);frames++;
            }
            Sheet(device,batch,output,$"contact-cut-{kind}-z{zoom:0.00}-{(light?"light":"dark")}-{(reduced?"reduced":"normal")}.png",images,6,3);
        }
        Console.WriteLine($"PASS {frames} production-cut frames: exact grid30 descriptors, slash/vertical/wave/annulus/cleave, forecast-amplify-cut-contract, zoom/full/reduced/light/dark; safe cells/holes/sides, expiry and batch state. Offline only.");
    }
    // The production forecasts and cuts over the production sealed field (backdrop and rim
    // behind, seal above), for readability review rather than flat light/dark plates.
    // The cinematics' world pieces over the production field: the summoning (the death run
    // backwards, then the stance), the victory's falling and planted blades, and the
    // departure after a wipe. Camera, letterbox and titles need the game and are not drawn.
    static void CinemaSequences(GraphicsDevice device,RenderTarget2D target,SpriteBatch batch,string output)
    {
        var field=new SamuraiArenaBounds(560,600-SamuraiArenaBounds.Height/2,SamuraiArenaBounds.Width/2,SamuraiArenaBounds.Height/2);
        new GhostSamuraiComposite().Load();
        Terraria.Main.GameViewMatrix.Scale=1;
        Matrix view=Terraria.Main.GameViewMatrix.TransformationMatrix;
        Vector2 a=Vector2.Transform(new Vector2(field.Left,field.Top),view),b=Vector2.Transform(new Vector2(field.Right,field.Bottom),view);
        var rect=new Vector4(a.X,a.Y,b.X,b.Y);
        SamuraiRigPose Idle(float age)=>new(360,330,age,0,1,new(2.16f,1,0,0),new(.98f,1,0,0),0,0,0);
        int frames=0;
        void Frame(string name,float clock,Action<SpriteBatch> draw)
        {
            var look=new SamuraiFieldLook{Clock=clock,Presence=1,Deploy=1,Phase=0,Camera=new Vector2(360,360)-new Vector2(field.CenterX,field.CenterY)};
            device.SetRenderTarget(target);device.Clear(Color.Black);
            device.BlendState=BlendState.AlphaBlend;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
            SamuraiFieldRenderer.DrawBackdrop(device,view,new Vector2(field.Left,field.Top),look);
            SamuraiFieldRenderer.DrawRim(device,rect,false,look);
            batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone,null,view);
            var state=WorldBatchParameters.Capture(batch);
            draw(batch);
            if(state!=WorldBatchParameters.Capture(batch))throw new Exception($"{name} changed caller SpriteBatch state");
            batch.End();
            device.BlendState=BlendState.AlphaBlend;
            SamuraiFieldRenderer.DrawSeal(device,rect,false,look);
            device.SetRenderTarget(null);
            Save(target,output,name);frames++;
        }
        var summon=new List<string>();
        foreach(float age in new[]{30f,50f,70f,90f,110f,124f,140f,170f})
        {
            string name=$"cinema-summon-{age:000}.png";summon.Add(name);
            Frame(name,4+age/60,batch2=>
            {
                bool stance=SamuraiCinematics.Stance(age);
                var left=SamuraiRigMotion.Blade(SamuraiAttack.Idle,SamuraiPhase.Phase1,age-SamuraiCinematics.StanceStart,age,-1,1,default,stance);
                var right=SamuraiRigMotion.Blade(SamuraiAttack.Idle,SamuraiPhase.Phase1,age-SamuraiCinematics.StanceStart,age,1,1,default,stance);
                var pose=new SamuraiRigPose(360,330,age,0,1,left,right,0,0,0);
                GhostSamuraiPresentation.Pose=pose;
                if(SamuraiCinematics.Manifesting(age))
                {
                    float undone=SamuraiCinematics.ManifestAge(age);
                    if(undone<SamuraiRigMotion.DeathDuration)GhostSamuraiRigArt.DrawDeath(batch2,pose,Vector2.Zero,undone);
                }
                else GhostSamuraiRigArt.Draw(batch2,pose,Vector2.Zero,null,age);
            });
        }
        Sheet(device,batch,output,"contact-cinema-summon.png",summon,4,2);
        var victory=new List<string>();
        foreach(float age in new[]{10f,26f,38f,50f,62f,70f,110f,190f})
        {
            string name=$"cinema-victory-{age:000}.png";victory.Add(name);
            Frame(name,9+age/60,batch2=>
            {
                var pose=Idle(400);GhostSamuraiPresentation.Pose=pose;
                if(age<SamuraiRigMotion.DeathDuration)GhostSamuraiRigArt.DrawDeath(batch2,pose,Vector2.Zero,age,swords:false);
                GhostSamuraiRigArt.DrawFallenBlades(batch2,pose,Vector2.Zero,age,field.Bottom,field.Left,field.Right);
            });
        }
        Sheet(device,batch,output,"contact-cinema-victory.png",victory,4,2);
        var defeat=new List<string>();
        foreach(float t in new[]{20f,60f,90f,120f,150f,190f})
        {
            string name=$"cinema-defeat-{t:000}.png";defeat.Add(name);
            Frame(name,12+t/60,batch2=>
            {
                var pose=Idle(600);GhostSamuraiPresentation.Pose=pose;
                float leaving=SamuraiCinematics.DepartAge(t);
                if(leaving<SamuraiRigMotion.DeathDuration)GhostSamuraiRigArt.DrawDeath(batch2,pose,Vector2.Zero,leaving,departing:true);
            });
        }
        Sheet(device,batch,output,"contact-cinema-defeat.png",defeat,3,2);
        Console.WriteLine($"PASS {frames} cinematic world frames (summoning, planted blades, departure) over the production field. Offline only; camera, letterbox and titles not drawn.");
    }

    static void FieldCutSequences(GraphicsDevice device,RenderTarget2D target,SpriteBatch batch,string output)
    {
        float[] offsets={-24,-8,-2,0,1.5f,5};
        var field=new SamuraiArenaBounds(560,600-SamuraiArenaBounds.Height/2,SamuraiArenaBounds.Width/2,SamuraiArenaBounds.Height/2);
        int frames=0;
        foreach(float phase in new[]{0f,2f})foreach(string kind in new[]{"slash","vertical","grid","wave","annulus","wind","cleave"})
        {
            var images=new List<string>();
            for(int step=0;step<offsets.Length;step++)
            {
                float fire=54,age=fire+offsets[step];
                Terraria.Main.GameViewMatrix.Scale=1;
                Matrix view=Terraria.Main.GameViewMatrix.TransformationMatrix;
                var look=new SamuraiFieldLook{Clock=7.3f+phase*5.6f+step*.05f,Presence=1,Deploy=1,Phase=phase,
                    Camera=new Vector2(360,360)-new Vector2(field.CenterX,field.CenterY)};
                Vector2 a=Vector2.Transform(new Vector2(field.Left,field.Top),view),b=Vector2.Transform(new Vector2(field.Right,field.Bottom),view);
                var rect=new Vector4(a.X,a.Y,b.X,b.Y);
                device.SetRenderTarget(target);device.Clear(Color.Black);
                device.BlendState=BlendState.AlphaBlend;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
                SamuraiFieldRenderer.DrawBackdrop(device,view,new Vector2(field.Left,field.Top),look);
                SamuraiFieldRenderer.DrawRim(device,rect,false,look);
                batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                    DepthStencilState.None,RasterizerState.CullNone,null,view);
                if(kind=="grid")
                    for(int axis=0;axis<2;axis++)for(int line=0;line<GhostSamuraiRules.GridVerticalLineCount;line++)
                    {
                        var h=GhostSamuraiRules.GridLine(axis==0,line,360,360,0);
                        GhostSamuraiCuts.Slash(batch,h,new(h.X,h.Y),h.Fire+offsets[step],false);
                    }
                else if(kind=="annulus"||kind=="cleave"||kind=="wind")
                {
                    var shape=kind=="annulus"?SamuraiShape.OuterSlash:kind=="wind"?SamuraiShape.InnerKamaitachi:SamuraiShape.FrontalCleave;
                    var h=new SamuraiHazard(shape,360,360,1,0,kind=="annulus"?145:0,315,0,54,
                        kind=="wind"?54+GhostSamuraiRules.Phase3KamaitachiDuration:66,1);
                    GhostSamuraiCuts.Field(batch,h,new(360,360),age,new Rectangle(0,0,720,720),false);
                }
                else if(kind=="wave")
                {
                    var h=new SamuraiHazard(SamuraiShape.SlashWave,180,360,1,0,SamuraiWaveRules.ChargedSlashWaveWidth,
                        SamuraiWaveRules.ChargedSlashWaveHeight/2,0,54,66,1);
                    GhostSamuraiCuts.Wave(batch,h,new(270+Math.Max(0,offsets[step])*8,360),age,false);
                }
                else
                {
                    bool vertical=kind=="vertical";
                    var h=new SamuraiHazard(vertical?SamuraiShape.VerticalSlash:SamuraiShape.Slash,
                        vertical?360:-340,vertical?-200:360,vertical?0:1,vertical?1:0,
                        vertical?1120:GhostSamuraiRules.SlashLength,vertical?SamuraiComboRules.VerticalHalfWidth:GhostSamuraiRules.SlashHalfWidth,
                        0,54,vertical?54+SamuraiComboRules.VerticalLive:54+GhostSamuraiRules.SlashLive,1);
                    GhostSamuraiCuts.Slash(batch,h,new(h.X,h.Y),age,false);
                }
                batch.End();
                device.BlendState=BlendState.AlphaBlend;
                SamuraiFieldRenderer.DrawSeal(device,rect,false,look);
                device.SetRenderTarget(null);
                string name=$"field-cut-{kind}-p{phase:0}-{step:00}.png";
                Save(target,output,name);images.Add(name);frames++;
            }
            Sheet(device,batch,output,$"contact-field-cut-{kind}-p{phase:0}.png",images,6,1);
        }
        Console.WriteLine($"PASS {frames} production forecast/cut frames over the production sealed field (backdrop+rim behind, seal above). Offline only.");
    }
    static void AssertBackground(RenderTarget2D target,int x,int y,Color expected,string description)
    {
        var pixel=new Color[1];target.GetData(0,new Rectangle(x,y,1,1),pixel,0,1);
        if(pixel[0]!=expected)throw new Exception($"{description} changed pixel {x},{y}: {pixel[0]} vs {expected}");
    }
    static void Sheet(GraphicsDevice device,SpriteBatch batch,string output,string name,List<string> images,int columns,int rows)
    {
        using var sheet=new RenderTarget2D(device,columns*180,rows*180,false,SurfaceFormat.Color,DepthFormat.None);
        device.SetRenderTarget(sheet);device.Clear(Color.Black);
        batch.Begin(SpriteSortMode.Immediate,BlendState.Opaque,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone);
        for(int i=0;i<images.Count;i++)
        {
            using var stream=File.OpenRead(Path.Combine(output,images[i]));using var texture=Texture2D.FromStream(device,stream);
            batch.Draw(texture,new Rectangle((i%columns)*180,(i/columns)*180,180,180),Color.White);
        }
        batch.End();device.SetRenderTarget(null);
        using var file=File.Create(Path.Combine(output,name));sheet.SaveAsPng(file,columns*180,rows*180);
    }
}

namespace ReLogic.Content { public enum AssetRequestMode{ImmediateLoad} public sealed class Asset<T>{public T Value;} }
namespace Terraria
{
    public static class Main
    {
        public static bool dedServ;public static int screenWidth=720,screenHeight=720;
        public static ulong GameUpdateCount;public static Vector2 screenPosition;
        public static SpriteBatch spriteBatch;public static GraphicsDeviceManager instance=new();
        public static PreviewView GameViewMatrix=new();
        public static void QueueMainThreadAction(Action action)=>action();
    }
    public sealed class GraphicsDeviceManager { public GraphicsDevice GraphicsDevice=>SamuraiSpectralPreview.Device; }
    public sealed class PreviewView
    {
        public float Scale=1;
        public Matrix TransformationMatrix=>Matrix.CreateTranslation(-360,-360,0)*Matrix.CreateScale(Scale)*Matrix.CreateTranslation(360,360,0);
    }
    public static class PreviewVectorExtensions
    {
        public static Vector2 RotatedBy(this Vector2 v,float r)=>new(v.X*MathF.Cos(r)-v.Y*MathF.Sin(r),v.X*MathF.Sin(r)+v.Y*MathF.Cos(r));
        public static Vector2 ToRotationVector2(this float r)=>new(MathF.Cos(r),MathF.Sin(r));
        public static float ToRotation(this Vector2 v)=>MathF.Atan2(v.Y,v.X);
        public static Vector2 SafeNormalize(this Vector2 v,Vector2 fallback)=>v.LengthSquared()<.0001f?fallback:Vector2.Normalize(v);
    }
}
namespace Terraria.ModLoader
{
    public enum ModSide{Client}
    [AttributeUsage(AttributeTargets.Class)] public sealed class AutoloadAttribute:Attribute{public ModSide Side{get;set;}}
    public class ModSystem{public virtual void Load(){}public virtual void Unload(){}public virtual void ClearWorld(){}public virtual void OnWorldUnload(){}}
    public static class ModContent
    {
        public static ReLogic.Content.Asset<T> Request<T>(string path,ReLogic.Content.AssetRequestMode mode)
            =>new(){Value=(T)(object)SamuraiSpectralPreview.Texture(path)};
        public static T GetInstance<T>() where T:class=>
            (T)(object)Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualConfig.Instance;
    }
}
namespace Convergence.Client.Encounters.FirstSeverance
{public sealed class FirstSeveranceVisualConfig{public static readonly FirstSeveranceVisualConfig Instance=new();public bool ReducedEffects;}}
namespace Convergence
{public sealed class ConvergenceMod{public static readonly ConvergenceMod Instance=new();public readonly PreviewLogger Logger=new();}public sealed class PreviewLogger{public void Warn(string message)=>Console.Error.WriteLine(message);}}
namespace Convergence.Client.Encounters.GhostSamurai
{
    internal static class GhostSamuraiPresentation
    {internal static SamuraiRigPose Pose;internal static readonly Guid Fight=Guid.NewGuid();internal static float Fraction=1;internal static bool TryPose(out SamuraiRigPose pose,out Guid fight){pose=Pose;fight=Fight;return true;}internal static bool Talisman(int i,out Vector2 tether,out float angle){tether=default;angle=0;return false;}}
    internal static class GhostSamuraiVisuals
    {
        static Texture2D? pixel;
        internal static void Stroke(SpriteBatch b,Vector2 a,Vector2 c,float width,Color color)
        {
            Vector2 d=c-a;if(d.LengthSquared()<.001f||width<=0)return;
            if(pixel is null){pixel=new Texture2D(b.GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});}
            b.Draw(pixel,a,null,color,MathF.Atan2(d.Y,d.X),new Vector2(0,.5f),new Vector2(d.Length(),width),SpriteEffects.None,0);
        }
    }
}
namespace Luminance.Assets
{
    public static class MiscTexturesRegistry
    {
        public static readonly ReLogic.Content.Asset<Texture2D> TurbulentNoise=new(){Value=SamuraiSpectralPreview.Noise("TurbulentNoise")};
        public static readonly ReLogic.Content.Asset<Texture2D> DendriticNoiseZoomedOut=new(){Value=SamuraiSpectralPreview.Noise("DendriticNoiseZoomedOut")};
        public static readonly ReLogic.Content.Asset<Texture2D> WavyBlotchNoise=new(){Value=SamuraiSpectralPreview.Noise("WavyBlotchNoise")};
    }
}
namespace Luminance.Core.Graphics
{
    public static class ShaderManager
    {
        static readonly Dictionary<string,ManagedShader> shaders=new();
        public static int CompositeDraws;
        public static ManagedShader GetShader(string name)
        {if(!shaders.TryGetValue(name,out var s))shaders[name]=s=new ManagedShader(name);return s;}
        public static void Clear(){foreach(var s in shaders.Values)s.Effect.Dispose();shaders.Clear();}
    }
    public sealed class ManagedShader
    {
        public static float? EnergyModeOverride;
        public readonly Effect Effect;readonly Texture[] textures=new Texture[4];readonly SamplerState[] samplers=new SamplerState[4];readonly string name;
        public ManagedShader(string name)
        {
            this.name=name;
            string path=Path.Combine(SamuraiSpectralPreview.Root,"Assets/AutoloadedEffects/Shaders/"+name.Split('.')[1]+".fxc");
            Effect=new Effect(SamuraiSpectralPreview.Device,File.ReadAllBytes(path));
        }
        public void TrySetParameter(string name,float value)=>Effect.Parameters[name]?.SetValue(value);
        public void TrySetParameter(string name,Vector2 value)=>Effect.Parameters[name]?.SetValue(value);
        public void TrySetParameter(string name,Vector4 value)=>Effect.Parameters[name]?.SetValue(value);
        public void TrySetParameter(string name,Matrix value)=>Effect.Parameters[name]?.SetValue(value);
        public void SetTexture(Texture texture,int slot,SamplerState sampler){textures[slot]=texture;samplers[slot]=sampler;}
        public void Apply(string pass="AutoloadPass")
        {
            if(name=="Convergence.SamuraiComposite")ShaderManager.CompositeDraws++;
            if(name=="Convergence.SamuraiEnergy")
            {
                if(EnergyModeOverride.HasValue)Effect.Parameters["mode"].SetValue(EnergyModeOverride.Value);
            }
            Effect.CurrentTechnique.Passes[pass].Apply();var d=SamuraiSpectralPreview.Device;
            for(int i=0;i<textures.Length;i++)if(textures[i]!=null){d.Textures[i]=textures[i];d.SamplerStates[i]=samplers[i];}
        }
    }
    public sealed class ManagedRenderTarget:IDisposable
    {
        public static readonly Dictionary<int,int> Sizes=new(){{1024,0},{512,0},{256,0}};
        readonly Func<int,int,RenderTarget2D> factory;RenderTarget2D target;
        readonly List<RenderTarget2D> retired=new();
        public ManagedRenderTarget(bool resize,Func<int,int,RenderTarget2D> factory,bool auto){this.factory=factory;}
        public RenderTarget2D Target=>target;public bool IsDisposed=>target?.IsDisposed??true;
        public bool IsUninitialized=>target==null;
        public void Recreate(int w,int h){if(target!=null)retired.Add(target);target=factory(w,h);Sizes[target.Width]++;}
        public void Dispose(){target?.Dispose();foreach(var old in retired)old.Dispose();retired.Clear();}
    }
    public static class RenderTargetManager
    {public static event Action RenderTargetUpdateLoopEvent;public static void Pulse()=>RenderTargetUpdateLoopEvent?.Invoke();}
    public sealed record PrimitiveSettings(Func<float,float> Width,Func<float,Color> Color,bool Smoothen,ManagedShader Shader);
    // Offline replacement for Luminance's tessellator only: production supplies
    // the exact history points, width/color curves, uniforms and SamuraiRibbon
    // shader. The installed native tessellator is not exercised by this fixture.
    public static class PrimitiveRenderer
    {
        struct TrailVertex:IVertexType
        {
            public Vector3 Position;public Color Color;public Vector3 UV;
            public VertexDeclaration VertexDeclaration=>Declaration;
            static readonly VertexDeclaration Declaration=new(
                new VertexElement(0,VertexElementFormat.Vector3,VertexElementUsage.Position,0),
                new VertexElement(12,VertexElementFormat.Color,VertexElementUsage.Color,0),
                new VertexElement(16,VertexElementFormat.Vector3,VertexElementUsage.TextureCoordinate,0));
        }
        static readonly TrailVertex[] strip=new TrailVertex[SamuraiRigMotion.TrailCapacity*6];
        public static void RenderTrail(List<Vector2> points,PrimitiveSettings settings,int count)
        {
            int end=Math.Min(count-2,points.Count-2);if(end<1)return;
            int used=0;
            for(int i=0;i<end;i++)
            {
                Edge(i,out var a,out var b);Edge(i+1,out var c,out var d);
                strip[used++]=a;strip[used++]=b;strip[used++]=c;
                strip[used++]=c;strip[used++]=b;strip[used++]=d;
            }
            var device=SamuraiSpectralPreview.Device;
            settings.Shader.TrySetParameter("uWorldViewProjection",
                Matrix.CreateTranslation(-Terraria.Main.screenPosition.X,-Terraria.Main.screenPosition.Y,0)*
                Terraria.Main.GameViewMatrix.TransformationMatrix*
                Matrix.CreateOrthographicOffCenter(0,device.Viewport.Width,device.Viewport.Height,0,-1,1));
            settings.Shader.Apply();device.DrawUserPrimitives(PrimitiveType.TriangleList,strip,0,used/3);
            void Edge(int i,out TrailVertex a,out TrailVertex b)
            {
                float u=i/(float)(count-1);
                Vector2 tangent=points[Math.Min(i+1,count-1)]-points[Math.Max(i-1,0)];
                tangent=tangent.LengthSquared()<.0001f?Vector2.UnitX:Vector2.Normalize(tangent);
                Vector2 normal=new(-tangent.Y,tangent.X);
                float width=settings.Width(u);
                Color color=settings.Color(u);
                Vector2 p=points[i];
                a=new(){Position=new(p-normal*width,0),Color=color,UV=new(u,0,1)};
                b=new(){Position=new(p+normal*width,0),Color=color,UV=new(u,1,1)};
            }
        }
    }
}
