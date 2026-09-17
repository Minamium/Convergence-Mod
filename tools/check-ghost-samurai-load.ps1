# Requires PowerShell 7+. Runs the installed loader's type validation only;
# does not start Terraria, a server, graphics, or read player/world saves.
param(
    [Parameter(Mandatory=$true)][string]$PackagePath,
    [Parameter(Mandatory=$true)][string]$TModLoaderPath,
    [switch]$ExpectOldFailure,
    [switch]$CheckLifecycle,
    [switch]$CheckSlashArt
)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;

public static class GhostSamuraiLoadCheck
{
    static byte[] ReadModAssembly(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        if (new string(reader.ReadChars(4)) != "TMOD") throw new InvalidDataException("Not a tModLoader package");
        reader.ReadString();
        reader.ReadBytes(280); // hash, signature and package length
        if (reader.ReadString() != "Convergence") throw new InvalidDataException("Expected the Convergence package");
        string version = reader.ReadString();
        int count = reader.ReadInt32();
        if (count < 1 || count > 10000) throw new InvalidDataException("Invalid package entry count");
        long offset = 0, selectedOffset = -1;
        int selectedSize = 0, selectedPacked = 0;
        for (int i = 0; i < count; i++) {
            string name = reader.ReadString();
            int size = reader.ReadInt32(), packed = reader.ReadInt32();
            if (size < 0 || packed < 0 || packed > stream.Length) throw new InvalidDataException("Invalid package entry size");
            if (name == "Convergence.dll") { selectedOffset = offset; selectedSize = size; selectedPacked = packed; }
            offset = checked(offset + packed);
        }
        if (selectedOffset < 0 || selectedSize > 64 * 1024 * 1024 || stream.Position + offset > stream.Length)
            throw new InvalidDataException("Missing or invalid Convergence assembly");
        stream.Position += selectedOffset;
        byte[] bytes = reader.ReadBytes(selectedPacked);
        if (bytes.Length != selectedPacked) throw new EndOfStreamException();
        Console.WriteLine("Package version: " + version);
        if (selectedSize == selectedPacked) return bytes;
        using var input = new MemoryStream(bytes);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        if (output.Length != selectedSize) throw new InvalidDataException("Assembly size mismatch");
        return output.ToArray();
    }

    public static void Run(string package, string loader, bool expectOldFailure, bool checkLifecycle, bool checkSlashArt)
    {
        var resolver = new AssemblyDependencyResolver(loader);
        var context = new AssemblyLoadContext("GhostSamuraiValidation", isCollectible: true);
        context.Resolving += (alc, name) => {
            string resolved = resolver.ResolveAssemblyToPath(name);
            // tML's bundled libraries also use its custom library directory
            // resolver; a normal command-line load context needs that fallback.
            if (resolved == null) {
                string libraries = Path.Combine(Path.GetDirectoryName(loader), "Libraries");
                foreach (string candidate in Directory.EnumerateFiles(libraries, name.Name + ".dll", SearchOption.AllDirectories)) {
                    resolved = candidate;
                    break;
                }
            }
            return resolved == null ? null : alc.LoadFromAssemblyPath(resolved);
        };
        try {
            using var bytes = new MemoryStream(ReadModAssembly(package));
            Assembly assembly = context.LoadFromStream(bytes);
            const string prefix = "Convergence.Client.Encounters.GhostSamurai.";
            string[] names = checkSlashArt ? new[] { "GhostSamuraiVisuals", "GhostSamuraiHazardVisuals", "GhostSamuraiSlashArt" }
                : new[] { "GhostSamuraiVisuals", "GhostSamuraiHazardVisuals" };
            foreach (string name in names) {
                Type type = assembly.GetType(prefix + name, throwOnError: true);
                object instance = Activator.CreateInstance(type, nonPublic: true);
                MethodInfo validate = type.GetMethod("ValidateType", BindingFlags.Instance | BindingFlags.NonPublic);
                bool rejected = false;
                try { validate.Invoke(instance, null); }
                catch (TargetInvocationException ex) when (expectOldFailure && name == "GhostSamuraiVisuals"
                    && ex.InnerException != null && ex.InnerException.Message.Contains("instance fields but InstancePerEntity returns false")) {
                    rejected = true;
                    Console.WriteLine("PASS reproduced old loader rejection: " + name);
                }
                if (expectOldFailure && name == "GhostSamuraiVisuals" && !rejected)
                    throw new Exception("The regression check did not reproduce the old failure");
                if (!rejected) Console.WriteLine("PASS installed tModLoader ValidateType: " + name);
            }
            Type summon = assembly.GetType("Convergence.Content.Encounters.GhostSamurai.GhostSamuraiSummon", throwOnError: true);
            Console.WriteLine("PASS saved item type retained: Convergence/" + summon.Name);
            if (checkLifecycle) {
                const string contentPrefix = "Convergence.Content.Encounters.GhostSamurai.";
                Type leaseType = assembly.GetType(contentPrefix + "GhostSamuraiContainmentPlayer", true);
                object registry = leaseType.GetField("leased", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                MethodInfo add = registry.GetType().GetMethod("Add");
                PropertyInfo count = registry.GetType().GetProperty("Count");
                FieldInfo fight = leaseType.GetField("fight", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo clear = leaseType.GetMethod("ClearAll", BindingFlags.Static | BindingFlags.NonPublic);
                Guid current = Guid.NewGuid(), nextFight = Guid.NewGuid();
                foreach (Guid id in new[] { current, current, nextFight }) {
                    object lease = Activator.CreateInstance(leaseType);
                    fight.SetValue(lease, id); add.Invoke(registry, new[] { lease });
                }
                clear.Invoke(null, new object[] { current });
                clear.Invoke(null, new object[] { current });
                if ((int)count.GetValue(registry) != 1) throw new Exception("Exact-fight cleanup damaged the next fight");
                Type systemType = assembly.GetType(contentPrefix + "GhostSamuraiContainmentSystem", true);
                object system = Activator.CreateInstance(systemType, true);
                // Main.player and ModContent are deliberately not initialized.
                // The production teardown must neither index them nor initialize
                // missing players/types, including a world with no summon.
                foreach (string hook in new[] { "OnWorldUnload", "OnWorldUnload", "Unload" })
                    systemType.GetMethod(hook).Invoke(system, null);
                if ((int)count.GetValue(registry) != 0) throw new Exception("World cleanup left leases");
                Console.WriteLine("PASS exact-fight leases, repeated world/Mod teardown and uninitialized player/type state");
            }
            Console.WriteLine("Only type validation executed; full Mod load and item restoration remain user-owned.");
        }
        finally { context.Unload(); }
    }
}
'@
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$loader = Join-Path (Resolve-Path -LiteralPath $TModLoaderPath).Path 'tModLoader.dll'
[GhostSamuraiLoadCheck]::Run($package, $loader, $ExpectOldFailure.IsPresent, $CheckLifecycle.IsPresent, $CheckSlashArt.IsPresent)
