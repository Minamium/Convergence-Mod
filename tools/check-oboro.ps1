# Offline installed-API/type/codec checks. No game, graphics or save is started.
param([Parameter(Mandatory=$true)][string]$AssemblyPath,
      [Parameter(Mandatory=$true)][string]$TModLoaderPath)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
public static class OboroPackageCheck {
 const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static void Require(bool value,string label){if(!value)throw new Exception(label);}
 public static void Run(string dll,string folder){
  string loader=Path.Combine(folder,"tModLoader.dll");var resolver=new AssemblyDependencyResolver(loader);
  var context=new AssemblyLoadContext("OboroPackage",true);
  context.Resolving+=(alc,name)=>{
   string found=resolver.ResolveAssemblyToPath(name);
   if(found==null)foreach(string file in Directory.EnumerateFiles(Path.Combine(folder,"Libraries"),name.Name+".dll",SearchOption.AllDirectories)){found=file;break;}
   return found==null?null:alc.LoadFromAssemblyPath(found);
  };
  try {
   var engine=context.LoadFromAssemblyPath(loader);var mod=context.LoadFromAssemblyPath(dll);
   string prefix="Convergence.Content.Items.Oboro.";
   foreach(string name in new[]{prefix+"Oboro",prefix+"OboroPlayer",prefix+"OboroNpc",prefix+"Zanshin",
    "Convergence.Client.Weapons.OboroItemVisuals","Convergence.Client.Weapons.OboroBuffVisuals"}){
    var t=mod.GetType(name,true);var value=Activator.CreateInstance(t,true);
    t.GetMethod("ValidateType",I).Invoke(value,null);
   }
   Console.WriteLine("PASS six Oboro installed-loader type validations, including saved item identity Convergence/Oboro");
   var player=mod.GetType(prefix+"OboroPlayer",true);var empty=Activator.CreateInstance(player,true);
   player.GetMethod("Initialize",I).Invoke(empty,null);
   player.GetMethod("ClearCombat",I).Invoke(empty,null);player.GetMethod("ClearCombat",I).Invoke(empty,null);
   Console.WriteLine("PASS repeated empty weapon cleanup without initialized player/content state");
   // Main's static paths normally come from launch setup. Point the fixture at
   // its isolated assembly folder; do not invoke a game or create a save.
   engine.GetType("Terraria.Program",true).GetField("SavePath",S).SetValue(null,Path.GetDirectoryName(dll));
   var main=engine.GetType("Terraria.Main",true);var npcType=engine.GetType("Terraria.NPC",true);
   var npc=RuntimeHelpers.GetUninitializedObject(npcType);
   void Set(string field,object value)=>npcType.GetField(field,I).SetValue(npc,value);
   Set("realLife",-1);Set("lifeMax",2400000);Set("lifeRegen",-60);Set("wet",true);
   var g=mod.GetType(prefix+"OboroNpc",true);var global=Activator.CreateInstance(g,true);
   g.GetField("FireRemaining",I).SetValue(global,600);
   var regen=g.GetMethod("UpdateLifeRegen",I);
   main.GetField("netMode",S).SetValue(null,0);object[] args={npc,1};regen.Invoke(global,args);
   Require((int)npcType.GetField("lifeRegen",I).GetValue(npc)==-96460,"stacked wet root DoT");
   main.GetField("netMode",S).SetValue(null,1);Set("lifeRegen",-60);regen.Invoke(global,args);
   Require((int)npcType.GetField("lifeRegen",I).GetValue(npc)==-60,"client must not apply DoT");
   main.GetField("netMode",S).SetValue(null,2);
   var root=RuntimeHelpers.GetUninitializedObject(npcType);npcType.GetField("active",I).SetValue(root,true);
   var npcs=Array.CreateInstance(npcType,200);npcs.SetValue(root,1);main.GetField("npc",S).SetValue(null,npcs);
   Set("realLife",1);regen.Invoke(global,args);
   Require((int)npcType.GetField("lifeRegen",I).GetValue(npc)==-60,"segment must not duplicate root DoT");
   Console.WriteLine("PASS installed NPC regen: exact percent formula, additive other DoT, wet persistence, client and segment exclusion");
   foreach(string typeName in new[]{"OboroRequest","OboroSnapshot","OboroBurst"}){
    var type=mod.GetType(prefix+typeName,true);var read=type.GetMethod("Read",S);
    using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
    if(typeName=="OboroRequest"){w.Write((byte)1);w.Write((ulong)3);w.Write((uint)9);w.Write(.3f);}
    else if(typeName=="OboroSnapshot"){w.Write((byte)0);w.Write((ulong)3);w.Write((uint)1);w.Write((uint)1);w.Write((byte)2);w.Write((ushort)12);w.Write((ushort)42);w.Write(.3f);w.Write((sbyte)1);w.Write((ushort)200);}
    else {w.Write(10f);w.Write(20f);w.Write((byte)5);for(int i=0;i<5;i++)w.Write(i*.4f);}
    byte[] payload=stream.ToArray();long end=stream.Length;w.Write(new byte[128]);stream.Position=0;
    using var reader=new BinaryReader(stream);object decoded=read.Invoke(null,new object[]{reader});Require(stream.Position==end,"shared buffer boundary");
    using var output=new MemoryStream();using var writer=new BinaryWriter(output);type.GetMethod("Write",I).Invoke(decoded,new object[]{writer});
    Require(Convert.ToHexString(output.ToArray())==Convert.ToHexString(payload),"exact round-trip");
    for(int size=0;size<payload.Length;size++){
     using var truncated=new MemoryStream(payload,0,size);using var r=new BinaryReader(truncated);bool rejected=false;
     try{read.Invoke(null,new object[]{r});}catch(TargetInvocationException ex)when(ex.InnerException is IOException){rejected=true;}
     Require(rejected,"truncated body accepted");
    }
    Console.WriteLine("PASS compiled "+typeName+": round-trip, shared buffer and all "+payload.Length+" truncations");
   }
  }finally{context.Unload();}
 }
}
'@
[OboroPackageCheck]::Run((Resolve-Path -LiteralPath $AssemblyPath).Path,(Resolve-Path -LiteralPath $TModLoaderPath).Path)
