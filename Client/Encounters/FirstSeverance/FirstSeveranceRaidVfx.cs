#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// A frame-local Doll Raid command list, not a global particle/actor framework.
// No gameplay state, retained Fight tails or GPU resources: Luminance owns the assets.
internal static class FirstSeveranceRaidVfx
{
    private readonly record struct DrawCommand(string Pass, Vector2 Origin, Vector2 Direction,
        float Length, float HalfWidth, Color Color, Vector4 Signal, float Time, float Seed, bool Reduced,
        Vector4 Pulse, float FlowOffset, bool Portal, Vector4 Ceremony);
    private static readonly DrawCommand[] commands = new DrawCommand[1024];
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[6];
    private static int count;
    private static bool collecting;
    internal static void BeginFrame() { count = 0; collecting = true; }
    internal static void Reset() { count = 0; collecting = false; }
    private readonly struct LocalBatch : IDisposable
    {
        private readonly SpriteBatch batch;
        private readonly bool owns;
        internal LocalBatch(SpriteBatch batch) { this.batch=batch; owns=!collecting; if(owns) BeginFrame(); }
        public void Dispose() { if(owns) EndFrame(batch); }
    }

    internal static void Beam(SpriteBatch batch, Vector2 origin, Vector2 direction, float length, float halfWidth,
        double age, float charge, float energy, float opacity, Color color, bool reduced,
        bool confined = false, bool mouth = true, float release = 0,
        double fireAge = double.NaN, double endAge = double.NaN)
    {
        if (Main.dedServ || opacity <= .001f || length <= 0 || halfWidth <= 0) return;
        using var scope=new LocalBatch(batch);
        float live = energy > .001f ? 1 : 0;
        Vector4 signal = new(Math.Clamp(charge,0,1), live, Math.Clamp(opacity,0,1), Math.Clamp(release,0,1));
        if (double.IsFinite(fireAge))
        {
            Vector4 ceremony = new((float)Math.Clamp(age-fireAge,-600,600),
                double.IsFinite(endAge)?(float)Math.Clamp(age-endAge,0,60):0,0,0);
            if (live == 0)
            {
                float t=Math.Clamp((ceremony.X+8)/7,0,1);
                float dip=1-.84f*t*t*(3-2*t);
                Add(batch,"ForecastDustPass",origin,direction,length,halfWidth,color,
                    signal with { Z=signal.Z*dip },age,reduced);
                Add(batch,"PortalForecastPass",origin,direction,length,halfWidth,color,
                    signal,age,reduced,portal:true,ceremony:ceremony);
            }
            else
            {
                if (!confined)
                    Add(batch,"PortalCoronaPass",origin,direction,length,halfWidth+Math.Min(24,halfWidth*.3f),
                        color,signal with { Z=signal.Z*(reduced?.3f:1) },age,reduced,portal:true,ceremony:ceremony);
                Add(batch,"AutoloadPass",origin,direction,length,halfWidth,color,
                    signal,age,reduced,portal:true,ceremony:ceremony);
            }
            if (mouth)
                Add(batch,"PortalMouthPass",origin-direction*110,direction,220,
                    live>0?Math.Min(150,22+halfWidth*.85f):24,color,signal,age,reduced,
                    portal:true,ceremony:ceremony);
            return;
        }
        // Untimed verdict rays and lattice forecasts retain the accepted 0.2.71
        // material. In particular, lattice never enters the portal-jet branch.
        if (live == 0)
        {
            // Sparse glints describe the accepted FUTURE footprint. Keep its
            // width before clamping the axis; never fill the warning rectangle.
            Add(batch, "ForecastDustPass", origin, direction, length, halfWidth,
                color, signal, age, reduced);
            halfWidth = Math.Min(halfWidth, 3f);
        }
        // Small gaps cannot be washed out by glow. Dense teeth/flood bands use no
        // outside corona. Other beams retain a low-intensity decorative skirt.
        if (!confined && live > 0)
            Add(batch, "CoronaPass", origin, direction, length, halfWidth + Math.Min(34,halfWidth*.6f),
                color, signal with { Z = signal.Z * (reduced ? .35f : 1) }, age, reduced);
        Add(batch, live > 0 ? "AutoloadPass" : "ForecastPass", origin, direction, length, halfWidth,
            color, signal, age, reduced);
        if (mouth)
        {
            float radius = live > 0 ? Math.Min(170,12+halfWidth*.85f+release*18) : 16;
            Add(batch, "MouthPass", origin-direction*150, direction, 300, radius,
                color, signal, age, reduced);
        }
    }

    internal static void GridRibbon(SpriteBatch batch, FirstSeveranceGridPulse pulse,
        double age, Color color, bool reduced)
    {
        var ray = pulse.Bounds;
        // Clipping never remaps the packet or changes its noise seed. Head and
        // taper are material-space geometry, shared with the authority collision.
        Add(batch, "RibbonPass", new(ray.X, ray.Y), new(ray.DirectionX, ray.DirectionY),
            ray.Length, ray.HalfWidth, color, new(1, 1, 1, 0), age, reduced,
            new(pulse.StartU, pulse.EndU, FirstSeveranceGridPulse.TailFraction, FirstSeveranceGridPulse.HeadFraction),
            pulse.FlowOffset, new(pulse.Track.X, pulse.Track.Y));
    }

    internal static void Orb(SpriteBatch batch, Vector2 center, Vector2 velocity, float radius,
        double age, Color color, float opacity, bool live, bool reduced)
    {
        Vector2 dir=velocity.LengthSquared()>.001f ? Vector2.Normalize(velocity) : Vector2.UnitX;
        using var scope=new LocalBatch(batch);
        Vector4 signal=new(.8f,live?1:0,opacity,0);
        if(live && !reduced) {
            float length=Math.Clamp(velocity.Length()*9+radius*3,35,180);
            Add(batch,"WakePass",center-dir*length,dir,length,radius*1.8f,color,
                signal with { Z=opacity*.52f },age,false);
        }
        // Luminous nucleus is no smaller than the fixed hit radius.
        Add(batch,live?"OrbPass":"PressurePass",center-dir*radius,dir,radius*2,radius,
            color,signal with { Z=opacity*(live?1:.58f) },age,reduced);
        if(!reduced) Add(batch,"FlarePass",center-dir*radius*3,dir,radius*6,radius*3,
            color,signal with { Z=opacity*.13f },age,false);
    }

    internal static void Charge(SpriteBatch batch, Vector2 center, Vector2 direction, double age,
        float tension, float release, float opacity, Color color, bool reduced, float scale=1)
    {
        float breadth=(120+tension*85)*scale;
        using var scope=new LocalBatch(batch);
        Vector4 signal=new(tension,release,opacity,release);
        Add(batch,"PressurePass",center-direction*breadth,direction,breadth*2,breadth*.65f,
            color,signal,age,reduced);
        Add(batch,"MouthPass",center-direction*100*scale,direction,200*scale,(32+tension*32)*scale,
            color,signal,age,reduced);
        if(release>.001f) Flare(batch,center,age,release*opacity,color,reduced,scale);
        // Bounded local in-flow sparks stop at the source; no glyph or ring.
        if(!reduced && tension>.02f)
            Sparks(batch,center,direction,age,tension,opacity,color,true,scale);
    }

    internal static void Flare(SpriteBatch batch, Vector2 center, double age, float opacity,
        Color color, bool reduced, float scale=1)
        => Add(batch,"FlarePass",center-Vector2.UnitX*220*scale,Vector2.UnitX,440*scale,170*scale,
            color,new(1,1,opacity*(reduced?.12f:1),1),age,reduced);

    internal static void Rift(SpriteBatch batch, Vector2 center, Vector2 axis, float length, float halfWidth,
        double age, float aperture, float opacity, Color color, bool reduced)
        => Add(batch,"RiftPass",center-axis*length*.5f,axis,length,halfWidth,
            color,new(aperture,0,opacity,0),age,reduced);

    internal static void Pressure(SpriteBatch batch, Vector2 center, Vector2 axis, float length, float halfWidth,
        double age,float tension,float opacity,Color color,bool reduced)
        => Add(batch,"PressurePass",center-axis*length*.5f,axis,length,halfWidth,color,
            new(tension,0,opacity,0),age,reduced);

    internal static void Sparks(SpriteBatch batch, Vector2 center, Vector2 axis, double age,
        float strength,float opacity,Color color,bool inward,float scale=1)
    {
        Vector2 normal=new(-axis.Y,axis.X);
        using var scope=new LocalBatch(batch);
        int total=18;
        for(int i=0;i<total;i++) {
            float seed=Seed(i+1), t=(float)((age*(.022+seed*.016)+seed)%1);
            float motion=inward?1-t*t*t:t*(2-t);
            float a=seed*MathHelper.TwoPi;
            Vector2 direction=axis*MathF.Cos(a)+normal*MathF.Sin(a)*.7f;
            Vector2 p=center+direction*(28+motion*(110+seed*145))*scale;
            float fade=MathF.Sin(t*MathF.PI)*opacity*strength;
            Add(batch,"WakePass",p-direction*(14+35*t)*scale,direction, (14+35*t)*scale,
                (1.2f+seed*1.4f)*scale,color,new(0,0,fade,0),age,false);
        }
    }

    // Deterministic fractional noise: no Main.rand, actor spawning or draw-time clocks.
    internal static float Seed(int n) { float f=MathF.Sin(n*71.37f+13.1f)*951.135f; return f-MathF.Floor(f); }

    private static void Add(SpriteBatch batch,string pass,Vector2 origin,Vector2 direction,float length,
        float halfWidth,Color color,Vector4 signal,double age,bool reduced,
        Vector4 pulse = default, float flowOffset = 0, Vector2? seedOrigin = null,
        bool portal = false, Vector4 ceremony = default)
    {
        if(Main.dedServ || signal.Z<=.001f || length<=0 || halfWidth<=0) return;
        bool immediate=!collecting;
        if(count==commands.Length) Flush(batch); // Never silently drop a danger footprint.
        var stableOrigin=seedOrigin ?? origin;
        float seed=(stableOrigin.X*.00017f+stableOrigin.Y*.00031f)%11;
        commands[count++]=new(pass,origin,direction,length,halfWidth,color,signal,(float)(age%216000)/60,seed,reduced,pulse,flowOffset,portal,ceremony);
        if(immediate) Flush(batch);
    }

    internal static void EndFrame(SpriteBatch batch)
    {
        try { Flush(batch); } finally { Reset(); }
    }
    internal static void Flush(SpriteBatch batch)
    {
        if(Main.dedServ || count==0) { count=0; return; }
        ManagedShader legacy=ShaderManager.GetShader("Convergence.RaidEnergy");
        ManagedShader portal=ShaderManager.GetShader("Convergence.PortalBeam");
        var device=Main.instance.GraphicsDevice;
        batch.End();
        var blend=device.BlendState; var depth=device.DepthStencilState; var raster=device.RasterizerState;
        var t1=device.Textures[1];var t2=device.Textures[2];var t3=device.Textures[3];
        var s1=device.SamplerStates[1];var s2=device.SamplerStates[2];var s3=device.SamplerStates[3];
        try {
            device.BlendState=BlendState.AlphaBlend;device.DepthStencilState=DepthStencilState.None;
            device.RasterizerState=RasterizerState.CullNone;
            var transform=Main.GameViewMatrix.TransformationMatrix*
                Matrix.CreateOrthographicOffCenter(0,device.Viewport.Width,device.Viewport.Height,0,-1,1);
            legacy.TrySetParameter("uWorldViewProjection",transform);
            portal.TrySetParameter("uWorldViewProjection",transform);
            legacy.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value,1,SamplerState.LinearWrap);
            legacy.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value,2,SamplerState.LinearWrap);
            legacy.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value,3,SamplerState.LinearWrap);
            for(int i=0;i<count;i++) {
                ref readonly var c=ref commands[i];
                ManagedShader shader=c.Portal?portal:legacy;
                shader.TrySetParameter("beamColor",c.Color.ToVector3());
                shader.TrySetParameter("signal",c.Signal);
                shader.TrySetParameter("shape",new Vector4(c.Length,c.HalfWidth,c.Seed,c.Reduced?.25f:1));
                shader.TrySetParameter("clock",c.Time);
                shader.TrySetParameter("pulse",c.Pulse);
                shader.TrySetParameter("flowOffset",c.FlowOffset);
                shader.TrySetParameter("ceremony",c.Ceremony);
                Quad(c.Origin,c.Direction,c.Length,c.HalfWidth);
                shader.Apply(c.Pass);
                device.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,2);
            }
        }
        finally {
            count=0;
            device.Textures[1]=t1;device.Textures[2]=t2;device.Textures[3]=t3;
            device.SamplerStates[1]=s1;device.SamplerStates[2]=s2;device.SamplerStates[3]=s3;
            device.BlendState=blend;device.DepthStencilState=depth;device.RasterizerState=raster;
            batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                DepthStencilState.None,Main.Rasterizer,null,Main.GameViewMatrix.TransformationMatrix);
        }
    }
    private static void Quad(Vector2 origin,Vector2 direction,float length,float halfWidth)
    {
        Vector2 normal=new(-direction.Y,direction.X);
        Vector2 a=origin-Main.screenPosition-normal*halfWidth,b=a+normal*halfWidth*2;
        Vector2 c=a+direction*length,d=b+direction*length;
        vertices[0]=new(new Vector3(a,0),Color.White,new(0,0));
        vertices[1]=new(new Vector3(b,0),Color.White,new(0,1));
        vertices[2]=new(new Vector3(c,0),Color.White,new(1,0));
        vertices[3]=vertices[2];vertices[4]=vertices[1];
        vertices[5]=new(new Vector3(d,0),Color.White,new(1,1));
    }
}
