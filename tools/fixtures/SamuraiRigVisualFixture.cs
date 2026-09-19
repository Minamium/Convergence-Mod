#nullable disable
// CPU capture of real production transforms. Shaders, Verlet, blending and
// in-game timing are explicitly outside this offline art-layout fixture.
namespace Microsoft.Xna.Framework
{
    public record struct Vector2(float X, float Y)
    {
        public Vector2(float v) : this(v, v) { }
        public static Vector2 Zero => new(0); public static Vector2 UnitX => new(1,0); public static Vector2 UnitY => new(0,1);
        public float LengthSquared() => X*X+Y*Y;
        public float Length() => System.MathF.Sqrt(LengthSquared());
        public static float Distance(Vector2 a,Vector2 b) => (a-b).Length();
        public static float DistanceSquared(Vector2 a,Vector2 b) => (a-b).LengthSquared();
        public static Vector2 Lerp(Vector2 a,Vector2 b,float t) => a+(b-a)*t;
        public static Vector2 operator +(Vector2 a,Vector2 b) => new(a.X+b.X,a.Y+b.Y);
        public static Vector2 operator -(Vector2 a,Vector2 b) => new(a.X-b.X,a.Y-b.Y);
        public static Vector2 operator *(Vector2 a,Vector2 b) => new(a.X*b.X,a.Y*b.Y);
        public static Vector2 operator *(Vector2 a,float b) => new(a.X*b,a.Y*b);
        public static Vector2 operator /(Vector2 a,Vector2 b) => new(a.X/b.X,a.Y/b.Y);
        public static Vector2 operator /(Vector2 a,float b) => new(a.X/b,a.Y/b);
    }
    public record struct Rectangle(int X,int Y,int Width,int Height);
    public record struct Color(float R,float G,float B,float A=255)
    {
        public static Color White => new(255,255,255);
        public static Color operator *(Color c,float t) => new(c.R*t,c.G*t,c.B*t,c.A*t);
        public static Color Lerp(Color a,Color b,float t) => new(a.R+(b.R-a.R)*t,a.G+(b.G-a.G)*t,a.B+(b.B-a.B)*t,a.A+(b.A-a.A)*t);
    }
    public static class MathHelper { public const float Pi=System.MathF.PI, PiOver2=Pi/2; }
}
namespace Microsoft.Xna.Framework.Graphics
{
    using Microsoft.Xna.Framework;
    public enum SpriteEffects { None, FlipHorizontally }
    public class Texture2D { public int Width=1254, Height=1254; }
    public sealed class SpriteBatch
    {
        public readonly System.Text.StringBuilder Text = new();
        public int Calls;
        public void Draw(Texture2D t,Vector2 at,Rectangle r,Color c,float rot,Vector2 origin,Vector2 scale,SpriteEffects effect,float depth)
        {
            if(r.X<0 || r.Y<0 || r.X+r.Width>t.Width || r.Y+r.Height>t.Height || scale.X<=0 || scale.Y<=0) throw new System.Exception("Invalid sprite bounds");
            foreach(float n in new[]{at.X,at.Y,rot,scale.X,scale.Y,c.A}) if(!float.IsFinite(n)) throw new System.Exception("Nonfinite draw");
            Calls++;
            Text.AppendLine(System.FormattableString.Invariant($"{at.X},{at.Y},{r.X},{r.Y},{r.Width},{r.Height},{c.R},{c.G},{c.B},{c.A},{rot},{origin.X},{origin.Y},{scale.X},{scale.Y},{(int)effect}"));
        }
    }
}
namespace ReLogic.Content
{
    public enum AssetRequestMode { ImmediateLoad }
    public class Asset<T> { public T Value; }
}
namespace Terraria
{
    using Microsoft.Xna.Framework;
    public static class Main { public static bool dedServ; }
    public static class Extensions
    {
        public static Vector2 ToRotationVector2(this float a) => new(System.MathF.Cos(a),System.MathF.Sin(a));
        public static float ToRotation(this Vector2 v) => System.MathF.Atan2(v.Y,v.X);
        public static Vector2 RotatedBy(this Vector2 v,float a) => new(v.X*System.MathF.Cos(a)-v.Y*System.MathF.Sin(a),v.X*System.MathF.Sin(a)+v.Y*System.MathF.Cos(a));
        public static Vector2 SafeNormalize(this Vector2 v,Vector2 fallback) => v.LengthSquared()>0 ? v/v.Length() : fallback;
    }
}
namespace Terraria.ModLoader
{
    public enum ModSide { Client }
    public class AutoloadAttribute : System.Attribute { public ModSide Side; }
    public class ModSystem { public virtual void Unload() { } }
    public static class ModContent
    {
        public static T GetInstance<T>() where T:new() => new();
        public static ReLogic.Content.Asset<T> Request<T>(string path,ReLogic.Content.AssetRequestMode mode) where T:new()
        { if(Terraria.Main.dedServ) throw new System.Exception("Server asset request"); return new(){Value=new()}; }
    }
}
namespace Convergence.Client.Encounters.FirstSeverance { public class FirstSeveranceVisualConfig { public bool ReducedEffects; } }
namespace Convergence.Client.Graphics
{
    public class WorldGraphicsScope : System.IDisposable
    {
        public static Microsoft.Xna.Framework.Graphics.SpriteBatch Batch;
        public WorldGraphicsScope(Microsoft.Xna.Framework.Graphics.SpriteBatch batch) { Batch=batch; }
        public void Dispose() { }
    }
}
namespace Convergence.Client.Encounters.GhostSamurai
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    internal static class GhostSamuraiPresentation { internal static bool Talisman(int i,out Vector2 p,out float a) { p=default;a=0;return false; } }
    internal static class GhostSamuraiVisuals { internal static void Stroke(SpriteBatch b,Vector2 a,Vector2 end,float w,Color c) { } }
    internal static class GhostSamuraiMaterials
    {
        internal static void Prepare(float age,float charge,float hit,float death) { }
        internal static void Trails(SpriteBatch batch,in SamuraiRigPose p,SamuraiRigHistory h,double tick) { }
        internal static void Part(Texture2D t,int part,Vector2 at,Rectangle r,Color c,float rot,Vector2 origin,Vector2 scale,bool flip)
            => Convergence.Client.Graphics.WorldGraphicsScope.Batch.Draw(t,at,r,c,rot,origin,scale,flip?SpriteEffects.FlipHorizontally:SpriteEffects.None,0);
    }
}
public static class SamuraiRigVisualFixture
{
    public static string Capture()
    {
        var output=new System.Text.StringBuilder();
        var batch=new Microsoft.Xna.Framework.Graphics.SpriteBatch();
        var zero=Microsoft.Xna.Framework.Vector2.Zero;
        var cases=new (string Name,Convergence.Content.Encounters.GhostSamurai.SamuraiAttack Attack,float Tick,int Facing,float Death)[] {
            ("Idle",0,0,1,-1), ("Moving",0,0,1,-1), ("Direction windup",Convergence.Content.Encounters.GhostSamurai.SamuraiAttack.DirectionalSlash,40,1,-1),
            ("Direction slash",Convergence.Content.Encounters.GhostSamurai.SamuraiAttack.DirectionalSlash,63,1,-1),
            ("Vertical windup",Convergence.Content.Encounters.GhostSamurai.SamuraiAttack.TripleVerticalSlash,38,1,-1),
            ("Vertical cut",Convergence.Content.Encounters.GhostSamurai.SamuraiAttack.TripleVerticalSlash,45,1,-1),
            ("Cleave windup",Convergence.Content.Encounters.GhostSamurai.SamuraiAttack.FrontalCleaveShockwave,128,1,-1),
            ("Cleave cut left",Convergence.Content.Encounters.GhostSamurai.SamuraiAttack.FrontalCleaveShockwave,135,-1,-1),
            ("Dash",Convergence.Content.Encounters.GhostSamurai.SamuraiAttack.Phase2DashSlash,105,1,-1),
            ("Victory: lower blades",0,0,1,18), ("Victory: scatter",0,0,1,48), ("Victory: last wisp",0,0,1,83) };
        foreach(var c in cases) {
            var left=Convergence.Client.Encounters.GhostSamurai.SamuraiRigMotion.Blade(c.Attack,Convergence.Content.Encounters.GhostSamurai.SamuraiPhase.Phase2,c.Tick,200,-1,c.Facing,default,false);
            var right=Convergence.Client.Encounters.GhostSamurai.SamuraiRigMotion.Blade(c.Attack,Convergence.Content.Encounters.GhostSamurai.SamuraiPhase.Phase2,c.Tick,200,1,c.Facing,default,false);
            var p=new Convergence.Client.Encounters.GhostSamurai.SamuraiRigPose(300,265,200,c.Name=="Moving"?.18f:0,.85f,left,right,0,c.Name=="Dash"?80:0,.08f);
            batch.Text.Clear(); batch.Calls=0;
            if(c.Death<0) Convergence.Client.Encounters.GhostSamurai.GhostSamuraiRigArt.Draw(batch,p,zero,null,200);
            else Convergence.Client.Encounters.GhostSamurai.GhostSamuraiRigArt.DrawDeath(batch,p,zero,c.Death);
            if(batch.Calls<15) throw new System.Exception("Missing body layers");
            output.AppendLine("#"+c.Name).Append(batch.Text);
        }
        int before=batch.Calls; Terraria.Main.dedServ=true;
        Convergence.Client.Encounters.GhostSamurai.GhostSamuraiRigArt.Draw(batch,default,zero,null,0);
        Convergence.Client.Encounters.GhostSamurai.GhostSamuraiRigArt.DrawDeath(batch,default,zero,0);
        if(batch.Calls!=before) throw new System.Exception("Dedicated-server drawing");
        return output.ToString();
    }
}
