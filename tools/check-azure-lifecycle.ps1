# Exact-package/native-API regression; no game, server or save is opened.
param(
    [Parameter(Mandatory=$true)][string]$PackagePath,
    [Parameter(Mandatory=$true)][string]$TModLoaderPath,
    [switch]$ExpectOldFailure
)
$ErrorActionPreference='Stop'
$bootstrap=@'
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
public static class AzureLifecycleCheck
{
    static byte[] AssemblyBytes(string path)
    {
        using var s=File.OpenRead(path);using var r=new BinaryReader(s);
        if(new string(r.ReadChars(4))!="TMOD")throw new InvalidDataException("package header");
        r.ReadString();r.ReadBytes(280);
        if(r.ReadString()!="Convergence")throw new InvalidDataException("package name");
        Console.WriteLine("Convergence package: "+r.ReadString());
        int count=r.ReadInt32();if(count<1 || count>10000)throw new InvalidDataException("entry count");
        long offset=0,selected=-1;int size=0,packed=0;
        for(int i=0;i<count;i++)
        {
            string name=r.ReadString();int length=r.ReadInt32(),compressed=r.ReadInt32();
            if(length<0 || compressed<0 || compressed>s.Length)throw new InvalidDataException("entry size");
            if(name=="Convergence.dll"){selected=offset;size=length;packed=compressed;}
            offset=checked(offset+compressed);
        }
        if(selected<0 || size>64*1024*1024 || s.Position+offset>s.Length)throw new InvalidDataException("assembly missing");
        s.Position+=selected;byte[] bytes=r.ReadBytes(packed);
        if(size==packed)return bytes;
        using var input=new MemoryStream(bytes);using var z=new DeflateStream(input,CompressionMode.Decompress);using var output=new MemoryStream();
        z.CopyTo(output);if(output.Length!=size)throw new InvalidDataException("assembly length");return output.ToArray();
    }
    public static void Run(string package,string loader,bool old)
    {
        var context=new AssemblyLoadContext("AzureLifecycleProbe",true);
        var resolver=new AssemblyDependencyResolver(loader);
        context.Resolving+=(alc,name)=>{
            string path=resolver.ResolveAssemblyToPath(name);
            if(path==null)foreach(var file in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(loader),"Libraries"),name.Name+".dll",SearchOption.AllDirectories)){path=file;break;}
            return path==null?null:alc.LoadFromAssemblyPath(path);
        };
        try
        {
            var engine=context.LoadFromAssemblyPath(loader);
            using var bytes=new MemoryStream(AssemblyBytes(package));var mod=context.LoadFromStream(bytes);
            engine.GetType("Terraria.Program",true).GetField("SavePath",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).SetValue(null,Path.GetDirectoryName(package));
            AzureLifecycleNativeProbe.Run(mod,engine,old);
        }
        finally{context.Unload();}
    }
}
'@
Add-Type -TypeDefinition ($bootstrap + [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fixtures/AzureLifecycleNativeProbe.cs')))
[AzureLifecycleCheck]::Run((Resolve-Path -LiteralPath $PackagePath).Path,(Resolve-Path -LiteralPath $TModLoaderPath).Path,$ExpectOldFailure)
