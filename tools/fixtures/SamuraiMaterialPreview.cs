// Actual FNA D3D11 and distributable effects, with production part transforms
// captured by preview-samurai-rig.ps1. This is not Terraria or an in-game test.
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal static class SamuraiMaterialPreview
{
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)] static extern int SDL_Init(uint flags);
    [DllImport("FNA3D",CallingConvention=CallingConvention.Cdecl)] static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_CreateWindow(string title,int x,int y,int w,int h,uint flags);
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_Quit();
    static void Main(string[] args)
    {
        string root=args[0],lumi=args[1],native=args[2],output=args[3];
        IntPtr Resolve(string name,System.Reflection.Assembly a,DllImportSearchPath? p)
        { string file=Path.Combine(native,name.EndsWith(".dll")?name:name+".dll"); return File.Exists(file)?NativeLibrary.Load(file):IntPtr.Zero; }
        NativeLibrary.SetDllImportResolver(typeof(SamuraiMaterialPreview).Assembly,Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly,Resolve);
        if(SDL_Init(0x20)!=0) throw new Exception("SDL video initialization failed");
        uint windowFlags=FNA3D_PrepareWindowAttributes();
        IntPtr window=SDL_CreateWindow("Offline material QA",0,0,600,600,windowFlags|0x8); // hidden, never shown
        if(window==IntPtr.Zero) throw new Exception("Hidden device surface unavailable");
        try {
            using var device=new GraphicsDevice(GraphicsAdapter.DefaultAdapter,GraphicsProfile.HiDef,new PresentationParameters {
                DeviceWindowHandle=window,BackBufferWidth=600,BackBufferHeight=600,BackBufferFormat=SurfaceFormat.Color,
                IsFullScreen=false,DepthStencilFormat=DepthFormat.None,PresentationInterval=PresentInterval.Immediate });
            using var target=new RenderTarget2D(device,600,600,false,SurfaceFormat.Color,DepthFormat.None);
            using var batch=new SpriteBatch(device);
            Texture2D Load(string path,bool premultiply=false) {
                using var stream=File.OpenRead(path); var t=Texture2D.FromStream(device,stream);
                if(premultiply) { var colors=new Color[t.Width*t.Height];t.GetData(colors);for(int i=0;i<colors.Length;i++) colors[i]=Color.FromNonPremultiplied(colors[i].ToVector4());t.SetData(colors); }
                return t;
            }
            using var atlas=Load(Path.Combine(root,"Assets/Textures/GhostSamurai/VioletRig.png"),true);
            using var noise=Load(Path.Combine(lumi,"Assets/Noise/TurbulentNoise.png"));
            using var veins=Load(Path.Combine(lumi,"Assets/Noise/DendriticNoiseZoomedOut.png"));
            using var spirit=new Effect(device,File.ReadAllBytes(Path.Combine(root,"Assets/AutoloadedEffects/Shaders/SamuraiSpirit.fxc")));
            using var mist=new Effect(device,File.ReadAllBytes(Path.Combine(root,"Assets/AutoloadedEffects/Shaders/SamuraiMist.fxc")));
            using var ribbon=new Effect(device,File.ReadAllBytes(Path.Combine(root,"Assets/AutoloadedEffects/Shaders/SamuraiRibbon.fxc")));
            var matrix=Matrix.CreateOrthographicOffCenter(0,600,600,0,-1,1);
            var vertices=new VertexPositionColorTexture[6];
            int index=0;
            foreach(string section in File.ReadAllText(Path.Combine(root,".local/samurai-rig-draws.txt")).Split('#').Skip(1)) {
                string[] lines=section.Trim().Split('\n');
                foreach(bool bright in new[]{false,true}) {
                    device.SetRenderTarget(target); device.Clear(bright?new Color(165,182,194):new Color(24,30,42));
                    foreach(string line in lines.Skip(1)) {
                        float[] v=line.Trim().Split(',').Select(s=>float.Parse(s,CultureInfo.InvariantCulture)).ToArray();
                        var at=new Vector2(v[0],v[1]);var source=new Rectangle((int)v[2],(int)v[3],(int)v[4],(int)v[5]);
                        var color=new Color((byte)Math.Clamp(v[6],0,255),(byte)Math.Clamp(v[7],0,255),(byte)Math.Clamp(v[8],0,255),(byte)Math.Clamp(v[9],0,255));
                        var origin=new Vector2(v[11],v[12]);var scale=new Vector2(v[13],v[14]);bool flip=v[15]>0;
                        if(v[16]<0) {
                            batch.Begin(SpriteSortMode.Immediate,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone);
                            batch.Draw(atlas,at,source,color,v[10],origin,scale,flip?SpriteEffects.FlipHorizontally:SpriteEffects.None,0);batch.End();continue;
                        }
                        spirit.Parameters["clock"].SetValue(v[17]/60);spirit.Parameters["charge"].SetValue(v[18]);spirit.Parameters["hit"].SetValue(v[19]);spirit.Parameters["dissolution"].SetValue(v[20]);
                        spirit.Parameters["part"].SetValue(v[16]);spirit.Parameters["uWorldViewProjection"].SetValue(matrix);
                        spirit.Parameters["texel"].SetValue(new Vector2(1f/atlas.Width,1f/atlas.Height));
                        spirit.Parameters["region"].SetValue(new Vector4(source.X/(float)atlas.Width,source.Y/(float)atlas.Height,source.Width/(float)atlas.Width,source.Height/(float)atlas.Height));
                        for(int i=0;i<6;i++) {
                            int corner=i switch{0=>0,1=>2,2=>1,3=>1,4=>2,_=>3};var unit=new Vector2(corner%2,corner/2);
                            Vector2 local=(unit*new Vector2(source.Width,source.Height)-origin)*scale;
                            Vector2 pos=at+Vector2.Transform(local,Matrix.CreateRotationZ(v[10]));
                            vertices[i]=new(new Vector3(pos,0),color,new Vector2((source.X+(flip?1-unit.X:unit.X)*source.Width)/atlas.Width,(source.Y+unit.Y*source.Height)/atlas.Height));
                        }
                        device.BlendState=BlendState.AlphaBlend;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
                        spirit.CurrentTechnique.Passes[0].Apply();device.Textures[0]=atlas;device.Textures[1]=noise;device.Textures[2]=veins;
                        device.SamplerStates[0]=SamplerState.LinearClamp;device.SamplerStates[1]=device.SamplerStates[2]=SamplerState.LinearWrap;
                        device.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,2);
                    }
                    device.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,$"rig-{index:D3}-{(bright?"light":"dark")}.png"));target.SaveAsPng(file,600,600);
                }
                index++;
            }
            // Isolated diagnostic geometry for the two procedural materials.
            // Actual metaball composition/primitive submission remains a game check.
            using var mask=new Texture2D(device,64,64);var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float a=Math.Clamp(1-Vector2.Distance(new(x,y),new(32,32))/32,0,1);pixels[y*64+x]=Color.White*a;}
            mask.SetData(pixels);
            for(int frame=0;frame<6;frame++) {
                device.SetRenderTarget(target);device.Clear(new Color(24,30,42));
                mist.Parameters["clock"].SetValue(frame*.3f);mist.Parameters["screenSize"].SetValue(new Vector2(600));mist.Parameters["worldOffset"].SetValue(Vector2.Zero);
                batch.Begin(SpriteSortMode.Immediate,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone,mist);
                device.Textures[1]=noise;device.SamplerStates[1]=SamplerState.LinearWrap;
                batch.Draw(mask,new Rectangle(90,80,420,200),Color.White);batch.End();
                ribbon.Parameters["clock"].SetValue(frame*.3f);ribbon.Parameters["opacity"].SetValue(1-frame*.1f);ribbon.Parameters["completionScale"].SetValue(1f);ribbon.Parameters["uWorldViewProjection"].SetValue(matrix);
                // Ribbon declares float3 UV. Width in z is1 here, matching a
                // normalized diagnostic strip, not the production tip history.
                var strip=new RibbonVertex[6];int[] order={0,2,1,1,2,3};
                for(int i=0;i<6;i++){int k=order[i];strip[i]=new(){Position=new(60+(k%2)*480,380+(k/2)*70,0),Color=Color.White,UV=new(k%2,k/2,1)};}
                ribbon.CurrentTechnique.Passes[0].Apply();device.Textures[1]=noise;device.SamplerStates[1]=SamplerState.LinearWrap;
                device.DrawUserPrimitives(PrimitiveType.TriangleList,strip,0,2);
                device.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,$"materials-{frame:D2}.png"));target.SaveAsPng(file,600,600);
            }
            Console.WriteLine($"PASS {index*2} actual shader rig frames and 6 material diagnostic frames. Offline only; no game, multiplayer or frame-time claim.");
        }
        finally { SDL_DestroyWindow(window);SDL_Quit(); }
    }
    struct RibbonVertex : IVertexType
    {
        public Vector3 Position;public Color Color;public Vector3 UV;
        public VertexDeclaration VertexDeclaration=>declaration;
        static readonly VertexDeclaration declaration=new(new VertexElement(0,VertexElementFormat.Vector3,VertexElementUsage.Position,0),new VertexElement(12,VertexElementFormat.Color,VertexElementUsage.Color,0),new VertexElement(16,VertexElementFormat.Vector3,VertexElementUsage.TextureCoordinate,0));
    }
}
