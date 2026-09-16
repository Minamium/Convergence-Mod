# Executes the production cutout cache against a delayed-asset CPU fixture.
# No game, graphics device, save or server is started. Pending Asset.Value uses
# a transparent placeholder, matching the installed ReLogic contract.
param([string]$SourcePath = (Join-Path $PSScriptRoot '../Client/Weapons/SpectralSpriteCutouts.cs'))
$ErrorActionPreference = 'Stop'
$production = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $SourcePath).Path)
$production = $production.Replace('namespace Convergence.Client.Weapons;', 'namespace Convergence.Client.Weapons {') + "`n}`n"
$fixture = @'
namespace ReLogic.Content {
 public enum AssetRequestMode { AsyncLoad, ImmediateLoad }
 public sealed class Asset<T> where T:class {
  public T Loaded, Placeholder; public bool Ready;
  public T Value => Ready ? Loaded : Placeholder;
 }
}
namespace Microsoft.Xna.Framework {
 public struct Color {
  public byte R,G,B,A;
  public Color(byte r,byte g,byte b,byte a){R=r;G=g;B=b;A=a;}
  public static Color Transparent => new Color(0,0,0,0);
 }
}
namespace Microsoft.Xna.Framework.Graphics {
 public sealed class Texture2D {
  public readonly int Width,Height;
  public bool Disposed;
  public Color[] Pixels;
  public Texture2D(object device,int width,int height){Width=width;Height=height;Pixels=new Color[width*height];}
  public void GetData(Color[] pixels)=>Pixels.CopyTo(pixels,0);
  public void SetData(Color[] pixels)=>pixels.CopyTo(Pixels,0);
  public void Dispose()=>Disposed=true;
 }
}
namespace Terraria {
 public static class Main {
  public sealed class Graphics { public object GraphicsDevice=new object(); }
  public static Graphics graphics=new Graphics();
  public static System.Collections.Generic.Queue<System.Action> Queue=new();
  public static void QueueMainThreadAction(System.Action action)=>Queue.Enqueue(action);
 }
}
namespace Terraria.ModLoader {
 public enum ModSide { Client }
 public sealed class AutoloadAttribute:System.Attribute { public ModSide Side {get;set;} }
 public class ModSystem { public virtual void Unload(){} }
 public static class ModContent {
  public static int Requests;
  public static ReLogic.Content.Asset<Texture2D> Asset;
  public static ReLogic.Content.Asset<T> Request<T>(string path,ReLogic.Content.AssetRequestMode mode=ReLogic.Content.AssetRequestMode.AsyncLoad) where T:class {
   Requests++; if(mode==ReLogic.Content.AssetRequestMode.ImmediateLoad)Asset.Ready=true;
   return (ReLogic.Content.Asset<T>)(object)Asset;
  }
 }
}
public static class SpectralCacheFixture {
 static void Require(bool value,string label){if(!value)throw new System.Exception(label);}
 static Texture2D Source(){
  var t=new Texture2D(null,16,12);
  for(int i=0;i<t.Pixels.Length;i++) t.Pixels[i]=new Color(0,255,0,255);
  // Twelve pose cells with visible violet cores, plus a low-alpha speck.
  for(int row=0;row<3;row++)for(int col=0;col<4;col++)t.Pixels[(row*4+1)*16+col*4+1]=new Color(160,50,220,255);
  t.Pixels[0]=new Color(150,30,230,4); return t;
 }
 public static void Run(){
  for(int client=0;client<3;client++){
   // Each independent cold client gets an unloaded asset, including late join.
   var source=Source(); var placeholder=new Texture2D(null,1,1);
   Terraria.ModLoader.ModContent.Asset=new ReLogic.Content.Asset<Texture2D>{Loaded=source,Placeholder=placeholder};
   int before=Terraria.ModLoader.ModContent.Requests;
   var first=Convergence.Client.Weapons.SpectralSpriteCutouts.Get("atlas");
   Require(first.Width==16 && first.Height==12,"cold client cached a pending placeholder");
   Require(first.Pixels[0].A==0 && first.Pixels[1].A==0,"matte and faint alpha must clear");
   for(int row=0;row<3;row++)for(int col=0;col<4;col++)Require(first.Pixels[(row*4+1)*16+col*4+1].A==255,"visible pose lost");
   Require(source.Pixels[1].G==255 && !source.Disposed,"shared source must be unchanged");
   Terraria.ModLoader.ModContent.Asset.Ready=true;
   Require(object.ReferenceEquals(first,Convergence.Client.Weapons.SpectralSpriteCutouts.Get("atlas")),"cache reuse");
   Require(Terraria.ModLoader.ModContent.Requests==before+1,"per-frame reload");
   var cache=new Convergence.Client.Weapons.SpectralSpriteCutouts(); cache.Unload(); cache.Unload();
   Require(!first.Disposed,"disposal must be queued");
   var replacement=Convergence.Client.Weapons.SpectralSpriteCutouts.Get("atlas");
   while(Terraria.Main.Queue.Count>0)Terraria.Main.Queue.Dequeue()();
   Require(first.Disposed && !replacement.Disposed && !source.Disposed,"reload disposed wrong texture");
   cache.Unload(); while(Terraria.Main.Queue.Count>0)Terraria.Main.Queue.Dequeue()();
  }
  System.Console.WriteLine("PASS linked production cache:3 cold clients,12 visible poses,matte,cache reuse and captured reload disposal");
  System.Console.WriteLine("CPU fixture only; actual tModLoader/SP/MP rendering remains user-owned.");
 }
}
'@
Add-Type -TypeDefinition ($production + $fixture)
[SpectralCacheFixture]::Run()
