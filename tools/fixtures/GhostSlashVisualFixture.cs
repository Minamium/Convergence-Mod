// Minimal CPU SpriteBatch contract: record the actual submitted sprite corners,
// not a reimplementation of the presentation's layout or authority geometry.
namespace Microsoft.Xna.Framework
{
    public record struct Vector2(float X, float Y)
    {
        public Vector2(float value) : this(value, value) { }
        public float LengthSquared() => X * X + Y * Y;
        public float Length() => System.MathF.Sqrt(LengthSquared());
        public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float b) => new(a.X * b, a.Y * b);
        public static Vector2 operator /(Vector2 a, float b) => new(a.X / b, a.Y / b);
    }
    public record struct Rectangle(int X, int Y, int Width, int Height)
    {
        public int Left => X; public int Right => X + Width; public int Top => Y; public int Bottom => Y + Height;
        public Vector2 Center => new(X + Width / 2, Y + Height / 2);
        public static Rectangle Intersect(Rectangle a, Rectangle b)
        {
            int x = System.Math.Max(a.Left,b.Left), y = System.Math.Max(a.Top,b.Top);
            return new(x,y,System.Math.Max(0,System.Math.Min(a.Right,b.Right)-x),System.Math.Max(0,System.Math.Min(a.Bottom,b.Bottom)-y));
        }
    }
    public record struct Color(float R, float G, float B, float A = 255)
    {
        public static Color White => new(255,255,255);
        public static Color operator *(Color c, float f) => new(c.R*f,c.G*f,c.B*f,c.A*f);
    }
    public static class MathHelper { public const float Pi = System.MathF.PI, PiOver2 = Pi/2; }
}
namespace Microsoft.Xna.Framework.Graphics
{
    using Microsoft.Xna.Framework;
    public enum SpriteEffects { None }
    public sealed class Texture2D { public int Width = 2172, Height = 724; public string Name; }
    public sealed class SpriteBatch
    {
        public readonly System.Collections.Generic.List<(Vector2[] Corners, string Asset)> Draws = new();
        public void Draw(Texture2D texture, Vector2 position, Rectangle source, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float depth)
        {
            if(source.Width <= 0 || source.Height <= 0 || source.Left < 0 || source.Right > texture.Width || source.Bottom > texture.Height) throw new System.Exception("Invalid source rectangle");
            var corners = new Vector2[4];
            for(int i=0;i<4;i++) {
                float x=((i%2)*source.Width-origin.X)*scale.X, y=((i/2)*source.Height-origin.Y)*scale.Y;
                corners[i]=position+new Vector2(x*System.MathF.Cos(rotation)-y*System.MathF.Sin(rotation),x*System.MathF.Sin(rotation)+y*System.MathF.Cos(rotation));
                if(!float.IsFinite(corners[i].X) || !float.IsFinite(corners[i].Y)) throw new System.Exception("Invalid draw corner");
            }
            Draws.Add((corners,texture.Name));
        }
    }
}
namespace ReLogic.Content
{
    public enum AssetRequestMode { AsyncLoad, ImmediateLoad }
    public sealed class Asset<T> { public T Value; }
}
namespace Terraria { public static class Main { public static bool dedServ; } }
namespace Terraria.ModLoader
{
    public enum ModSide { Client }
    public sealed class AutoloadAttribute : System.Attribute { public ModSide Side; }
    public class ModSystem { public virtual void Unload() { } }
    public static class ModContent
    {
        public static int Requests;
        public static ReLogic.Content.Asset<T> Request<T>(string path, ReLogic.Content.AssetRequestMode mode)
        {
            if(Terraria.Main.dedServ || mode != ReLogic.Content.AssetRequestMode.ImmediateLoad) throw new System.Exception("Server or pending asset load");
            Requests++; return new() { Value=(T)(object)new Microsoft.Xna.Framework.Graphics.Texture2D { Name=path } };
        }
    }
}
namespace Convergence.Client.Encounters.GhostSamurai
{
    // Existing exact-volume vector markers are untouched; only new texture calls
    // are captured here. This test does not assert rasterization or alpha blending.
    internal static class GhostSamuraiVisuals
    {
        internal static void Stroke(Microsoft.Xna.Framework.Graphics.SpriteBatch batch, Microsoft.Xna.Framework.Vector2 a, Microsoft.Xna.Framework.Vector2 b, float width, Microsoft.Xna.Framework.Color color) { }
    }
}
public static class GhostSlashVisualFixture
{
    public static void Run()
    {
        var batch=new Microsoft.Xna.Framework.Graphics.SpriteBatch();
        var center=new Microsoft.Xna.Framework.Vector2(900,650);
        var view=new Microsoft.Xna.Framework.Rectangle(100,100,1700,1100);
        int cases=0, quads=0;
        foreach(bool reduced in new[]{false,true})
        foreach(var shape in new[]{Convergence.Content.Encounters.GhostSamurai.SamuraiShape.InnerSlash,Convergence.Content.Encounters.GhostSamurai.SamuraiShape.OuterSlash,Convergence.Content.Encounters.GhostSamurai.SamuraiShape.InnerKamaitachi,Convergence.Content.Encounters.GhostSamurai.SamuraiShape.OuterKamaitachi})
        {
            bool outer=shape is Convergence.Content.Encounters.GhostSamurai.SamuraiShape.OuterSlash or Convergence.Content.Encounters.GhostSamurai.SamuraiShape.OuterKamaitachi;
            var h=new Convergence.Content.Encounters.GhostSamurai.SamuraiHazard(shape,center.X,center.Y,1,0,outer?300:0,1000,0,60,100,200);
            foreach(float age in new[]{0,59,60,61,75,99.9f}) {
                batch.Draws.Clear();
                Convergence.Client.Encounters.GhostSamurai.GhostSamuraiCircleVisuals.Draw(batch,h,center,age,view,reduced);
                Check(h.Live(age)?batch.Draws.Count>0:batch.Draws.Count==0,"circle live/forecast material");
                Check(batch.Draws.Count<=320,"circle draw-call bound");
                foreach(var draw in batch.Draws) {
                    if(outer) Check(DistanceToQuad(center,draw.Corners)>=h.Length-.02f,"texture interior crosses safe hole");
                    foreach(var p in draw.Corners) {
                    float distance=(p-center).Length();
                    Check(distance<=h.Radius+.02f && (!outer || distance>=h.Length-.02f),"circle/hole containment");
                    Inside(p,view);
                    }
                }
                quads+=batch.Draws.Count; cases++;
            }
        }
        foreach(int facing in new[]{-1,1}) foreach(bool reduced in new[]{false,true}) {
            var h=Convergence.Content.Encounters.GhostSamurai.SamuraiComboRules.Cleave(center.X,center.Y,facing,0);
            foreach(float age in new[]{h.Fire-1,h.Fire,h.Fire+4,h.End-.1f}) {
                batch.Draws.Clear();
                Convergence.Client.Encounters.GhostSamurai.GhostSamuraiComboVisuals.DrawCleave(batch,h,center,age,true,view,reduced);
                Check(h.Live(age)?batch.Draws.Count>0:batch.Draws.Count==0,"cleave live/forecast material");
                foreach(var draw in batch.Draws) foreach(var p in draw.Corners) { Check((p.X-center.X)*facing>=-.02f,"cleave back safety"); Inside(p,view); }
                quads+=batch.Draws.Count; cases++;
            }
        }
        for(int rotation=0;rotation<16;rotation++) foreach(bool reduced in new[]{false,true}) {
            float angle=rotation*System.MathF.Tau/16;
            var d=new Microsoft.Xna.Framework.Vector2(System.MathF.Cos(angle),System.MathF.Sin(angle));
            var n=new Microsoft.Xna.Framework.Vector2(-d.Y,d.X);
            var h=new Convergence.Content.Encounters.GhostSamurai.SamuraiHazard(Convergence.Content.Encounters.GhostSamurai.SamuraiShape.SlashWave,center.X,center.Y,d.X,d.Y,64,240,0,60,150,60);
            batch.Draws.Clear(); Convergence.Client.Encounters.GhostSamurai.GhostSamuraiWaveVisuals.Draw(batch,h,center,60,true,reduced);
            foreach(var draw in batch.Draws) foreach(var p in draw.Corners) {
                var r=p-center; Check(System.Math.Abs(Microsoft.Xna.Framework.Vector2.Dot(r,d))<=h.Length/2+.02f && System.Math.Abs(Microsoft.Xna.Framework.Vector2.Dot(r,n))<=h.Radius+.02f,"wave OBB containment");
            }
            Check(batch.Draws.Count==24,"wave bound"); quads+=batch.Draws.Count; cases++;
        }
        foreach(var art in System.Enum.GetValues<Convergence.Client.Encounters.GhostSamurai.SamuraiSlashArt>()) {
            batch.Draws.Clear();
            Convergence.Client.Encounters.GhostSamurai.GhostSamuraiSlashArt.Strip(batch,art,new(-500,-800),new(2100,1900),68,1,view);
            Check(batch.Draws.Count==1,"clipped strip missing"); foreach(var p in batch.Draws[0].Corners) Inside(p,view);
            cases++; quads++;
        }
        int requests=Terraria.ModLoader.ModContent.Requests; Check(requests==5,"five lazy assets");
        batch.Draws.Clear(); Terraria.Main.dedServ=true;
        Convergence.Client.Encounters.GhostSamurai.GhostSamuraiSlashArt.Strip(batch,Convergence.Client.Encounters.GhostSamurai.SamuraiSlashArt.Normal,new(0,0),new(10,0),4,1);
        Check(batch.Draws.Count==0 && requests==Terraria.ModLoader.ModContent.Requests,"server graphics guard"); Terraria.Main.dedServ=false;
        Check(Convergence.Client.Encounters.GhostSamurai.GhostSamuraiSlashArt.Energy(59,60,70)==0 && Convergence.Client.Encounters.GhostSamurai.GhostSamuraiSlashArt.Energy(70,60,70)==0,"live lifetime");
        Check(Convergence.Client.Encounters.GhostSamurai.GhostSamuraiSlashArt.Energy(69.99f,60,70)>=.42f,"end visibility");
        new Convergence.Client.Encounters.GhostSamurai.GhostSamuraiSlashArt().Unload();
        Convergence.Client.Encounters.GhostSamurai.GhostSamuraiSlashArt.Strip(batch,Convergence.Client.Encounters.GhostSamurai.SamuraiSlashArt.Normal,new(0,0),new(10,0),4,1);
        Check(Terraria.ModLoader.ModContent.Requests==requests+1,"reload cache reset");
        System.Console.WriteLine($"PASS {cases} linked-production visual cases / {quads} sprite quads; bounds, UVs, forecasts, reduced effects, cache and dedicated guard");
        System.Console.WriteLine("CPU geometry capture only; actual game/SP/MP rasterization, readability and performance remain not_run.");
    }
    private static void Inside(Microsoft.Xna.Framework.Vector2 p, Microsoft.Xna.Framework.Rectangle r) => Check(p.X>=r.Left-.02f && p.X<=r.Right+.02f && p.Y>=r.Top-.02f && p.Y<=r.Bottom+.02f,"field/viewport containment");
    private static float DistanceToQuad(Microsoft.Xna.Framework.Vector2 p, Microsoft.Xna.Framework.Vector2[] corners)
    {
        var u=corners[1]-corners[0]; var v=corners[2]-corners[0]; var r=p-corners[0];
        float x=System.Math.Clamp(Microsoft.Xna.Framework.Vector2.Dot(r,u)/u.LengthSquared(),0,1);
        float y=System.Math.Clamp(Microsoft.Xna.Framework.Vector2.Dot(r,v)/v.LengthSquared(),0,1);
        return (p-(corners[0]+u*x+v*y)).Length();
    }
    private static void Check(bool ok,string label) { if(!ok) throw new System.Exception(label); }
}
