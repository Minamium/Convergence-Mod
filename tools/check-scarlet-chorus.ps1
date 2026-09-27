# Exact-package publication and native marker codec probe; no game or sockets.
param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$TModLoaderPath
)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Collections.Generic;

public static class ScarletChorusCheck
{
    const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    const BindingFlags Methods = Fields | BindingFlags.Static;
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    record Instruction(int Offset, OpCode Op, object Operand);
    static readonly Dictionary<short, OpCode> codes = typeof(OpCodes).GetFields()
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null))
        .ToDictionary(op => op.Value);
    static List<Instruction> ReadIL(MethodInfo method)
    {
        var result = new List<Instruction>();
        byte[] bytes = method.GetMethodBody().GetILAsByteArray();
        for (int i = 0; i < bytes.Length;) {
            int at = i; short code = bytes[i++];
            if (code == 0xfe) code = (short)(0xfe00 | bytes[i++]);
            OpCode op = codes[code]; object operand = null; int size;
            switch (op.OperandType) {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(bytes, i); break;
                default: size = 4; break;
            }
            if (op.OperandType == OperandType.InlineMethod || op.OperandType == OperandType.InlineField)
                operand = method.Module.ResolveMember(BitConverter.ToInt32(bytes, i));
            result.Add(new(at, op, operand)); i += size;
        }
        return result;
    }
    static void CheckPublication(Assembly mod)
    {
        string prefix = "Convergence.Content.Encounters.CrimsonFoundry.";
        var marker = mod.GetType(prefix + "CrimsonChorus", true);
        var publish = marker.GetMethod("Synchronize", Methods);
        Require(publish != null, "Missing explicit marker publication");
        var sends = ReadIL(publish).Where(i => i.Operand is MethodInfo m
            && m.DeclaringType.FullName == "Terraria.NetMessage" && m.Name == "SendData").ToArray();
        Require(sends.Length == 1, "Marker must use one native publication boundary");
        var runtime = mod.GetType(prefix + "CrimsonRuntime", true);
        foreach (string name in new[] { "TryScheduleChorus", "TickChorus" }) {
            var il = ReadIL(runtime.GetMethod(name, Methods));
            var calls = il.Where(i => Equals(i.Operand, publish)).ToArray();
            Require(calls.Length == 1, name + " must explicitly publish");
            string[] fields = name == "TickChorus" ? new[] { "Resolved", "FailedMask", "ImpactPositions" } : new[] { "timeLeft" };
            foreach (string field in fields) {
                var writes = il.Where(i => i.Op == OpCodes.Stfld && i.Operand is FieldInfo f && f.Name == field).ToArray();
                Require(writes.Length > 0 && writes.All(i => i.Offset < calls[0].Offset), name + " publishes before committing " + field);
            }
        }
        Console.WriteLine("PASS compiled marker publication after spawn initialization and complete verdict commit");
    }
    static byte[] Payload(int count, byte kind, bool resolved, byte failed, Guid fight, float offset = 0)
    {
        using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
        w.Write(fight.ToByteArray()); w.Write((short)3); w.Write(500); w.Write(1);
        w.Write((byte)3); w.Write(kind); w.Write((byte)((1 << count) - 1));
        w.Write(600); w.Write(824); w.Write(880); w.Write(8000); w.Write(6000);
        w.Write(8000f); w.Write(5440f); w.Write(resolved); w.Write(failed);
        w.Write((byte)(resolved ? count : 0));
        if (resolved) for (int i = 0; i < count; i++) { w.Write(8000f + offset + i); w.Write(5440f); }
        return stream.ToArray();
    }
    static void Read(object marker, byte[] data)
    {
        using var r = new BinaryReader(new MemoryStream(data));
        marker.GetType().GetMethod("ReceiveExtraAI").Invoke(marker, new object[] { r });
        Require(r.BaseStream.Position == data.Length, "Native marker did not parse complete payload");
    }
    static byte[] Write(object marker)
    {
        using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
        marker.GetType().GetMethod("SendExtraAI").Invoke(marker, new object[] { w });
        return stream.ToArray();
    }
    public static void Run(string assembly, string loader)
    {
        var resolver = new AssemblyDependencyResolver(loader);
        var context = new AssemblyLoadContext("ScarletChorusChecks", true);
        context.Resolving += (alc, name) => {
            string path = resolver.ResolveAssemblyToPath(name);
            if (path == null) path = Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(loader), "Libraries"),
                name.Name + ".dll", SearchOption.AllDirectories).FirstOrDefault();
            return path == null ? null : alc.LoadFromAssemblyPath(path);
        };
        try {
            var engine = context.LoadFromAssemblyPath(loader);
            var mod = context.LoadFromAssemblyPath(assembly);
            CheckPublication(mod);
            engine.GetType("Terraria.Program", true).GetField("SavePath", Methods)
                .SetValue(null, Path.GetDirectoryName(assembly));
            var type = mod.GetType("Convergence.Content.Encounters.CrimsonFoundry.CrimsonChorus", true);
            var mode = engine.GetType("Terraria.Main", true).GetField("netMode", Methods);
            // SP uses the same non-server decoder acceptance path without logging
            // a fictitious receiving client. This is not a network simulation.
            mode.SetValue(null, 0);
            int cases = 0, rejectedPrefixes = 0;
            foreach (int count in new[] { 1, 4, 8 }) foreach (byte kind in new byte[] { 0, 1 })
            foreach (byte failures in new byte[] { 0, (byte)((1 << count) - 1) }) {
                Guid fight = Guid.NewGuid(); object marker = Activator.CreateInstance(type, true);
                byte[] pending = Payload(count, kind, false, 0, fight), verdict = Payload(count, kind, true, failures, fight);
                Read(marker, pending); Require(Write(marker).SequenceEqual(pending), "Initial native marker round trip");
                Read(marker, verdict); Require(Write(marker).SequenceEqual(verdict), "Resolved native marker round trip");
                Read(marker, verdict); Read(marker, pending);
                Read(marker, Payload(count, kind, true, failures, Guid.NewGuid()));
                Read(marker, Payload(count, kind, true, failures, fight, 24));
                Read(marker, Payload(count, kind, true, (byte)(failures ^ 1), fight));
                Require(Write(marker).SequenceEqual(verdict), "Stale/foreign/conflicting verdict changed accepted state");
                for (int n = 0; n < verdict.Length; n++) {
                    try { Read(marker, verdict.Take(n).ToArray()); throw new Exception("Truncated marker accepted"); }
                    catch (TargetInvocationException ex) when (ex.InnerException is IOException) { rejectedPrefixes++; }
                    Require(Write(marker).SequenceEqual(verdict), "Truncation partially mutated marker");
                }
                // No valid local client/SP object may publish an authority packet.
                var sync = type.GetMethod("Synchronize", Methods);
                sync.Invoke(marker, null); mode.SetValue(null, 1); sync.Invoke(marker, null); mode.SetValue(null, 0);
                object ignored = Activator.CreateInstance(type, true);
                mode.SetValue(null, 2); Read(ignored, verdict);
                Require(!(bool)type.GetField("Resolved", Fields).GetValue(ignored), "Server accepted a client-authored verdict");
                sync.Invoke(ignored, null); // Empty-Fight guard, before native entity access.
                var entity = RuntimeHelpers.GetUninitializedObject(engine.GetType("Terraria.Projectile", true));
                type.GetProperty("Entity", Fields).SetValue(marker, entity);
                sync.Invoke(marker, null); // Inactive native marker cannot publish.
                mode.SetValue(null, 0); cases++;
            }
            Console.WriteLine($"PASS {cases} native marker cases / {rejectedPrefixes} truncated prefixes; solo and4/8, both outcomes, immutable terminal, server authority and sender guards");
            Console.WriteLine("NOT_RUN: socket delivery, draw-layer visibility, sound, latency and actual Host & Play");
        } finally { context.Unload(); }
    }
}
'@
[ScarletChorusCheck]::Run((Resolve-Path -LiteralPath $AssemblyPath).Path,
    (Join-Path (Resolve-Path -LiteralPath $TModLoaderPath).Path 'tModLoader.dll'))
