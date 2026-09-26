// Hidden FNA/D3D11 rendering of the shipped .fxc. Offline material QA only.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Convergence.Client.Encounters.FirstSeverance;

internal static class DollCorePreview
{
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
    [DllImport("FNA3D", CallingConvention=CallingConvention.Cdecl)] private static extern uint FNA3D_PrepareWindowAttributes();
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] private static extern IntPtr SDL_CreateWindow(string title,int x,int y,int w,int h,uint flags);
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] private static extern void SDL_DestroyWindow(IntPtr window);
    [DllImport("SDL2", CallingConvention=CallingConvention.Cdecl)] private static extern void SDL_Quit();

    private static void Main(string[] args)
    {
        string root=args[0], luminancePackage=args[1], native=args[2], output=args[3];
        IntPtr Resolve(string name,System.Reflection.Assembly assembly,DllImportSearchPath? path)
        {
            string file=Path.Combine(native,name.EndsWith(".dll")?name:name+".dll");
            return File.Exists(file)?NativeLibrary.Load(file):IntPtr.Zero;
        }
        NativeLibrary.SetDllImportResolver(typeof(DollCorePreview).Assembly,Resolve);
        NativeLibrary.SetDllImportResolver(typeof(GraphicsDevice).Assembly,Resolve);
        if(SDL_Init(0x20)!=0) throw new Exception("SDL video initialization failed");
        IntPtr window=SDL_CreateWindow("Doll Core offline material",0,0,512,512,FNA3D_PrepareWindowAttributes()|0x8);
        if(window==IntPtr.Zero) throw new Exception("Hidden device surface unavailable");
        try
        {
            using var device=new GraphicsDevice(GraphicsAdapter.DefaultAdapter,GraphicsProfile.HiDef,new PresentationParameters {
                DeviceWindowHandle=window,BackBufferWidth=512,BackBufferHeight=512,
                BackBufferFormat=SurfaceFormat.Color,DepthStencilFormat=DepthFormat.None,
                PresentationInterval=PresentInterval.Immediate });
            using var target=new RenderTarget2D(device,512,512,false,SurfaceFormat.Color,DepthFormat.None);
            using var effect=new Effect(device,File.ReadAllBytes(Path.Combine(root,"Assets/AutoloadedEffects/Shaders/DollCoreEnergy.fxc")));
            using var batch = new SpriteBatch(device);
            Terraria.Main.instance.GraphicsDevice = device;
            var metal = new FirstSeveranceMechanicalCore();
            Texture2D Load(string name)
            {
                using var file=File.OpenRead(luminancePackage);
                using var reader=new BinaryReader(file);
                if(System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4))!="TMOD") throw new InvalidDataException("TMOD magic");
                reader.ReadString();reader.ReadBytes(276);reader.ReadInt32();reader.ReadString();reader.ReadString();
                int count=reader.ReadInt32(),offset=0,position=-1,length=0,stored=0;
                for(int i=0;i<count;i++)
                {
                    string path=reader.ReadString();int size=reader.ReadInt32(),compressed=reader.ReadInt32();
                    if(path=="Assets/Noise/"+name+".rawimg") {position=offset;length=size;stored=compressed;}
                    offset+=compressed;
                }
                if(position<0) throw new FileNotFoundException("Luminance noise: "+name);
                file.Position+=position;
                using var bytes=new MemoryStream(reader.ReadBytes(stored));
                using Stream data=length==stored?bytes:new DeflateStream(bytes,CompressionMode.Decompress);
                using var raw=new BinaryReader(data);
                if(raw.ReadInt32()!=1) throw new InvalidDataException("Raw image version");
                int width=raw.ReadInt32(),height=raw.ReadInt32();
                var texture=new Texture2D(device,width,height);
                texture.SetData(raw.ReadBytes(width*height*4));
                return texture;
            }
            using var cloud=Load("WavyBlotchNoise");
            using var flow=Load("TurbulentNoise");
            using var branch=Load("DendriticNoiseZoomedOut");
            var matrix=Matrix.CreateOrthographicOffCenter(0,512,512,0,-1,1);
            var quad=new VertexPositionColorTexture[6];
            void Draw(float radius,int pass)
            {
                float x=256-radius,y=256-radius,s=radius*2;
                quad[0]=new(new Vector3(x,y,0),Color.White,Vector2.Zero);
                quad[1]=new(new Vector3(x,y+s,0),Color.White,Vector2.UnitY);
                quad[2]=new(new Vector3(x+s,y,0),Color.White,Vector2.UnitX);
                quad[3]=quad[2];quad[4]=quad[1];
                quad[5]=new(new Vector3(x+s,y+s,0),Color.White,Vector2.One);
                effect.CurrentTechnique.Passes[pass].Apply();
                device.Textures[1]=cloud;device.Textures[2]=flow;device.Textures[3]=branch;
                device.SamplerStates[1]=SamplerState.LinearWrap;
                device.SamplerStates[2]=SamplerState.LinearWrap;
                device.SamplerStates[3]=SamplerState.LinearWrap;
                device.DrawUserPrimitives(PrimitiveType.TriangleList,quad,0,2);
            }
            // Clock continuity, settled material, Reduced Effects, and directed
            // single/twin muzzle opening against two field luminances.
            var cases=new List<(string name,float time,float intensity,bool reduced,float bore,bool twin)>{
                ("emerge",.15f,.8f,false,0,false),
                ("settled-a",2f,0,false,0,false),
                ("settled-b",3.4f,0,false,0,false),
                ("pressure",5f,1,false,0,false),
                ("reduced",5f,.45f,true,0,false),
                ("bore",6f,.7f,false,1,false),
                ("twin-bore",6f,.7f,false,1,true)
            };
            for(int frame=0;frame<=16;frame++)
                cases.Add(($"sequence-{frame:00}",frame*.25f,0,false,0,false));
            var ruptureAges = new Dictionary<string, float>();
            foreach (float age in new float[]{-1,0,1,3,6,9,12,14,17,18,19,20,22,24,28,32,38,44,50,60,72,85,100,120})
                foreach (bool reduced in new[]{false,true})
                {
                    string key=$"rupture-{age+1:000}-{(reduced?"reduced":"full")}";
                    ruptureAges[key]=age;
                    cases.Add((key,10+age/60,0,reduced,0,false));
                }
            foreach(var c in cases) foreach(bool bright in new[]{false,true})
            {
                device.SetRenderTarget(target);
                device.Clear(bright?new Color(172,184,196):new Color(22,26,40));
                device.BlendState=BlendState.AlphaBlend;
                device.DepthStencilState=DepthStencilState.None;
                device.RasterizerState=RasterizerState.CullNone;
                effect.Parameters["uWorldViewProjection"].SetValue(matrix);
                effect.Parameters["clock"].SetValue(c.time);
                bool rupturing=ruptureAges.TryGetValue(c.name,out float ruptureAge);
                var pose = ruptureAge < 0 ? default : new FirstSeveranceCoreRupture(true,ruptureAge);
                effect.Parameters["signal"].SetValue(new Vector4(rupturing?pose.Energy:1,
                    rupturing?pose.Flash:c.intensity,c.reduced?1:0,c.bore));
                effect.Parameters["boreAxis"].SetValue(Vector2.UnitX);
                effect.Parameters["twinBore"].SetValue(c.twin?1f:0f);
                Draw(66*(c.reduced?1.23f:1.36f),1);
                Draw(66,0);
                if (rupturing)
                {
                    batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                        DepthStencilState.None,RasterizerState.CullNone,null,Matrix.Identity);
                    metal.Draw(batch,new Vector2(256),66,c.time,.3f,Color.White,1,c.reduced,rupture:pose);
                    batch.End();
                }
                device.SetRenderTarget(null);
                using var file=File.Create(Path.Combine(output,$"{c.name}-{(bright?"light":"dark")}.png"));
                target.SaveAsPng(file,512,512);
            }
            Console.WriteLine($"PASS {cases.Count*2} compiled-material GPU frames; offline only.");
            metal.Unload();
        }
        finally { SDL_DestroyWindow(window); SDL_Quit(); }
    }
}
