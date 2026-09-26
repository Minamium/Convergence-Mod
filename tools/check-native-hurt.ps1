# Offline package/API boundary checks. Does not start a game, world or server.
param(
    [Parameter(Mandatory=$true)][string]$PackagePath,
    [Parameter(Mandatory=$true)][string]$TModLoaderPath,
    [Parameter(Mandatory=$true)][string]$AssemblyOutput
)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

public static class NativeHurtCheck
{
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static void Require(bool test, string message) { if (!test) throw new Exception(message); }
    static byte[] ReadAssembly(string package)
    {
        using var stream = File.OpenRead(package);
        using var reader = new BinaryReader(stream);
        Require(new string(reader.ReadChars(4)) == "TMOD", "Package header");
        reader.ReadString(); reader.ReadBytes(280);
        Require(reader.ReadString() == "Convergence", "Package identity");
        Console.WriteLine("Package: " + reader.ReadString());
        int count = reader.ReadInt32();
        Require(count > 0 && count < 10000, "Package entry count");
        long offset = 0, selected = -1;
        int size = 0, packed = 0;
        for (int i = 0; i < count; i++) {
            string name = reader.ReadString(); int a = reader.ReadInt32(), b = reader.ReadInt32();
            Require(a >= 0 && b >= 0 && b <= stream.Length, "Package entry size");
            if (name == "Convergence.dll") { selected = offset; size = a; packed = b; }
            offset = checked(offset + b);
        }
        Require(selected >= 0 && size <= 64 * 1024 * 1024 && stream.Position + offset <= stream.Length, "Package assembly bounds");
        stream.Position += selected;
        byte[] raw = reader.ReadBytes(packed);
        Require(raw.Length == packed, "Truncated package");
        if (size == packed) return raw;
        using var input = new MemoryStream(raw);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream(); deflate.CopyTo(output);
        Require(output.Length == size, "Assembly decompression size");
        return output.ToArray();
    }
    public static void Run(string package, string loader, string assemblyOutput)
    {
        byte[] bytes = ReadAssembly(package);
        var resolver = new AssemblyDependencyResolver(loader);
        var context = new AssemblyLoadContext("NativeHurtChecks", true);
        context.Resolving += (alc, name) => {
            string path = resolver.ResolveAssemblyToPath(name);
            if (path == null) foreach (string candidate in Directory.EnumerateFiles(
                Path.Combine(Path.GetDirectoryName(loader), "Libraries"), name.Name + ".dll", SearchOption.AllDirectories))
                { path = candidate; break; }
            return path == null ? null : alc.LoadFromAssemblyPath(path);
        };
        try {
            var engine = context.LoadFromAssemblyPath(loader);
            using var input = new MemoryStream(bytes);
            var mod = context.LoadFromStream(input);
            var modifiers = engine.GetType("Terraria.Player+HurtModifiers", true);
            var setMax = modifiers.GetMethod("SetMaxDamage");
            var getDamage = modifiers.GetMethod("GetDamage");
            object h = Activator.CreateInstance(modifiers);
            setMax.Invoke(h, new object[] {499});
            Require((float)getDamage.Invoke(h, new object[] {1000f, 0f, .5f}) == 499, "Native lethal cap");
            setMax.Invoke(h, new object[] {250}); setMax.Invoke(h, new object[] {499});
            Require((float)getDamage.Invoke(h, new object[] {1000f, 0f, .5f}) == 250, "Other Mod lower ceiling");
            h = Activator.CreateInstance(modifiers);
            Require((float)getDamage.Invoke(h, new object[] {120f, 200f, .5f}) == 20, "Native armor");
            var final = modifiers.GetField("FinalDamage");
            object dr = final.GetValue(h);
            var multiply = dr.GetType().GetMethod("op_Multiply", new[] {dr.GetType(), typeof(float)});
            final.SetValue(h, multiply.Invoke(null, new object[] {dr, .5f}));
            Require((float)getDamage.Invoke(h, new object[] {120f, 200f, .5f}) == 10, "Native damage reduction");
            setMax.Invoke(h, new object[] {0});
            Require((float)getDamage.Invoke(h, new object[] {1000f, 0f, .5f}) == 1, "Zero cap is NOT immunity");
            Console.WriteLine("PASS installed HurtModifiers: defense/DR, lethal floor, minimum1 and competing ceilings");

            string azure = "Convergence.Content.Encounters.AzureCathedral.";
            foreach (string name in new[] { "AzureAttack", "AzureWorm", "AzureChorusStrike" }) {
                Type type = mod.GetType(azure + name, false);
                if (type == null || mod.GetType(azure + "AzureRecoveryPlayer", false) == null) continue;
                object actor = Activator.CreateInstance(type, true);
                if (name == "AzureChorusStrike") type.GetField("Budget", Instance).SetValue(actor, 900);
                object[] arguments = { null, Activator.CreateInstance(modifiers) };
                type.GetMethod("ModifyHitPlayer").Invoke(actor, arguments);
                bool rehearsal = (bool)mod.GetType(azure + "AzureRules").GetField("DebugOneDamagePlaytest", Static).GetRawConstantValue();
                if (rehearsal) Require((float)getDamage.Invoke(arguments[1], new object[] {1800f, 0f, .5f}) == 1,
                    "Azure native final cap: " + name);
            }
            Console.WriteLine("PASS packaged Azure attack/contact/verdict final-damage ceilings");
            string scarlet = "Convergence.Content.Encounters.CrimsonFoundry.";
            if (mod.GetType(scarlet + "CrimsonRecoveryPlayer", false) != null) {
                foreach (string name in new[] { "CrimsonAttack", "CrimsonGesture", "CrimsonChorusStrike" }) {
                    Type type = mod.GetType(scarlet + name, true);
                    object actor = Activator.CreateInstance(type, true);
                    string fieldName = name == "CrimsonAttack" ? "Hazard" : name == "CrimsonGesture" ? "Plan" : "Impact";
                    var field = type.GetField(fieldName, Instance);
                    object descriptor = Activator.CreateInstance(field.FieldType);
                    field.FieldType.GetField("<Damage>k__BackingField", Instance).SetValue(descriptor, 900);
                    field.SetValue(actor, descriptor);
                    object[] arguments = { null, Activator.CreateInstance(modifiers) };
                    type.GetMethod("ModifyHitPlayer").Invoke(actor, arguments);
                    bool rehearsal = (bool)mod.GetType(scarlet + "CrimsonPlaytestTuning").GetField("DebugOneDamagePlaytest", Static).GetRawConstantValue();
                    Require((float)getDamage.Invoke(arguments[1], new object[] {1800f, 0f, .5f}) == (rehearsal ? 1 : 900),
                        "Scarlet native final cap: " + name);
                }
                Console.WriteLine("PASS packaged Scarlet attack/gesture/verdict final-damage ceilings");
            }

            // Calibration example, not a simulation of the owner's equipment.
            int beamSource = (int)mod.GetType("Convergence.Content.Encounters.FirstSeverance.FirstSeveranceCombatRules", true)
                .GetField("BeamDamage", Static).GetRawConstantValue();
            h = Activator.CreateInstance(modifiers);
            dr = final.GetValue(h);
            final.SetValue(h, multiply.Invoke(null, new object[] {dr, .5f}));
            Require((float)getDamage.Invoke(h, new object[] {120f, 300f, 1f}) == 1, "Old budget floors against endgame armor");
            Require(beamSource == 500 && (float)getDamage.Invoke(h, new object[] {(float)beamSource, 300f, 1f}) == 100,
                "New source budget retains armor and DR");
            Console.WriteLine("PASS calibration: source500, defense300, effectiveness1, DR50% => 100 before accessory hooks");

            string prefix = "Convergence.Content.Encounters.FirstSeverance.";
            var raid = mod.GetType(prefix + "Revive.FirstSeveranceRaidPlayer", true);
            foreach (string name in new[] {"Revive.FirstSeveranceRaidPlayer", "Revive.RaidBoundDebuff"}) {
                Type type = mod.GetType(prefix + name, true);
                object instance = Activator.CreateInstance(type, true);
                type.GetMethod("ValidateType", Instance).Invoke(instance, null);
            }
            Console.WriteLine("PASS packaged Raid player/buff loader type validation");

            foreach (string name in new[] {
                "Convergence.Content.Encounters.AzureCathedral.AzureRecoveryPlayer",
                "Convergence.Content.Encounters.AzureCathedral.AzureDownedDebuff",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonRecoveryPlayer",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonDownedDebuff",
                "Convergence.Client.Encounters.CrimsonFoundry.CrimsonBossVisuals",
                "Convergence.Client.Encounters.CrimsonFoundry.CrimsonAttackVisuals",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonBoss",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonEffigy",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonCompanion",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonCompanionRay",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonPact",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonPactBuff",
                "Convergence.Client.Encounters.CrimsonFoundry.CrimsonCompanionVisuals",
                "Convergence.Client.Encounters.CrimsonFoundry.CrimsonItemVisuals",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonAttack",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonConductor",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonConnection",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonFieldPlayer",
                "Convergence.Content.Encounters.CrimsonFoundry.CrimsonFieldSpawns" }) {
                Type type = mod.GetType(name, false);
                if (type == null) continue; // Earlier packages remain inspectable.
                object instance = Activator.CreateInstance(type, true);
                type.GetMethod("ValidateType", Instance).Invoke(instance, null);
                Console.WriteLine("PASS packaged loader type: " + type.Name);
            }

            var playerType = engine.GetType("Terraria.Player", true);
            var modPlayer = engine.GetType("Terraria.ModLoader.ModPlayer", true);
            var fightType = mod.GetType("Convergence.Common.Foundation.Identifiers.FightId", true);
            object fightA = Activator.CreateInstance(fightType, new object[] {Guid.NewGuid()});
            object fightB = Activator.CreateInstance(fightType, new object[] {Guid.NewGuid()});
            var registry = raid.GetField("bound", Static).GetValue(null);
            var add = registry.GetType().GetMethod("Add");
            var countProperty = registry.GetType().GetProperty("Count");
            object unusedPlayer = RuntimeHelpers.GetUninitializedObject(playerType);
            playerType.GetField("modPlayers", Instance).SetValue(unusedPlayer, Array.CreateInstance(modPlayer, 0));
            foreach (object fight in new[] {fightA, fightA, fightB}) {
                object instance = Activator.CreateInstance(raid, true);
                // ModPlayer.Player is a getter-only view of ModType.Entity.
                raid.GetProperty("Entity", Instance).SetValue(instance, unusedPlayer);
                raid.GetField("fightId", Instance).SetValue(instance, fight);
                add.Invoke(registry, new[] {instance});
            }
            raid.GetMethod("ClearFight", Static).Invoke(null, new[] {fightA});
            raid.GetMethod("ClearFight", Static).Invoke(null, new[] {fightA});
            Require((int)countProperty.GetValue(registry) == 1, "Exact Fight cleanup");
            var containment = mod.GetType(prefix + "FirstSeveranceContainmentPlayer", true);
            containment.GetMethod("ClearExisting", Static).Invoke(null, new[] {unusedPlayer, fightA});
            raid.GetMethod("ClearAll", Static).Invoke(null, null);
            raid.GetMethod("ClearAll", Static).Invoke(null, null);
            Require((int)countProperty.GetValue(registry) == 0, "Repeated cleanup");
            Console.WriteLine("PASS exact-Fight/repeated Doll teardown with an uninitialized player/type registry");
            File.WriteAllBytes(assemblyOutput, bytes);
            Console.WriteLine("Extracted exact package assembly for codec checks: " + assemblyOutput);
            Console.WriteLine("NOT RUN: actual Hurt hook dispatch with installed Mods, player/world simulation, multiplayer.");
        }
        finally { context.Unload(); }
    }
}
'@
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$loader = Join-Path (Resolve-Path -LiteralPath $TModLoaderPath).Path 'tModLoader.dll'
[NativeHurtCheck]::Run($package, $loader, [IO.Path]::GetFullPath($AssemblyOutput))
