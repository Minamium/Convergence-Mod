// Exercises the actual compiled types against installed tML without starting a game.
public static class OboroHeldNativeProbe
{
    const System.Reflection.BindingFlags I = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
    const System.Reflection.BindingFlags S = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
    static void Require(bool value, string label) { if (!value) throw new System.Exception(label); }
    public static void Run(System.Reflection.Assembly mod, System.Reflection.Assembly engine)
    {
        var heldType = mod.GetType("Convergence.Content.Items.Oboro.OboroHeldProj", true);
        var projectileType = engine.GetType("Terraria.Projectile", true);
        object Create(out object projectile)
        {
            var held = System.Activator.CreateInstance(heldType);
            projectile = System.Activator.CreateInstance(projectileType);
            heldType.GetProperty("Entity", I).SetValue(held, projectile);
            projectileType.GetProperty("ModProjectile", I).SetValue(projectile, held);
            heldType.GetMethod("SetDefaults", I).Invoke(held, null);
            projectileType.GetField("active", I).SetValue(projectile, true);
            return held;
        }
        var first = Create(out var native);
        var second = Create(out var replacement);
        Require((bool)heldType.GetMethod("ShouldUpdatePosition", I).Invoke(first, null) == false, "no velocity integration");
        Require((bool)heldType.GetMethod("CanDamage", I).Invoke(first, null) == false, "no parallel hit path");
        Require((bool)heldType.GetMethod("CanCutTiles", I).Invoke(first, null) == false, "no tile side effects");
        foreach (string field in new[] { "friendly", "hostile", "tileCollide" })
            Require(!(bool)projectileType.GetField(field, I).GetValue(native), "harmless " + field);
        var layers = new System.Collections.Generic.List<int>[5];
        for (int i = 0; i < layers.Length; i++) layers[i] = new();
        heldType.GetMethod("DrawBehind", I).Invoke(first, new object[] { 42, layers[0], layers[1], layers[2], layers[3], layers[4] });
        for (int i = 0; i < layers.Length; i++)
            Require(layers[i].Count == (i == 3 ? 1 : 0), "exactly one draw layer");
        Require(layers[3][0] == 42, "correct over-player projectile index");
        var generation = heldType.GetField("ConnectionGeneration", I);
        generation.SetValue(first, 0x123456789ABCDEF0UL);
        using var stream = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(stream);
        using var reader = new System.IO.BinaryReader(stream);
        heldType.GetMethod("SendExtraAI", I).Invoke(first, new object[] { writer });
        Require(stream.Length == 8, "identity is full ulong, never float");
        var net = engine.GetType("Terraria.Main", true).GetField("netMode", S);
        int previousNetMode = (int)net.GetValue(null);
        try
        {
            net.SetValue(null, 1); stream.Position = 0;
            heldType.GetMethod("ReceiveExtraAI", I).Invoke(second, new object[] { reader });
            Require((ulong)generation.GetValue(first) == (ulong)generation.GetValue(second), "remote identity exact");
            generation.SetValue(second, 99UL);
            net.SetValue(null, 2); stream.Position = 0;
            heldType.GetMethod("ReceiveExtraAI", I).Invoke(second, new object[] { reader });
            Require((ulong)generation.GetValue(second) == 99UL && stream.Position == 8, "server consumes but rejects owner identity claims");
        }
        finally { net.SetValue(null, previousNetMode); }

        var playerType = mod.GetType("Convergence.Content.Items.Oboro.OboroPlayer", true);
        var state = System.Activator.CreateInstance(playerType);
        var snapshotType = mod.GetType("Convergence.Content.Items.Oboro.OboroSnapshot", true);
        object snapshot = System.Activator.CreateInstance(snapshotType, new object[] {
            (byte)0, 7UL, 1U, 1U, (byte)0, (ushort)0, (ushort)24, 0f, (sbyte)1, (ushort)0 });
        playerType.GetField("View", I).SetValue(state, snapshot);
        generation.SetValue(first, 7UL); generation.SetValue(second, 7UL);
        var bind = playerType.GetMethod("BindHeld", I);
        bool Bind(object held) => (bool)bind.Invoke(state, new[] { held });
        Require(Bind(first) && Bind(first) && !Bind(second), "one holdout, idempotent bind");
        playerType.GetMethod("ReleaseHeld", I).Invoke(state, new[] { second });
        Require(!Bind(second), "old kill cannot release another instance");
        // Native projectile-slot reuse replaces ModProjectile on the same object.
        projectileType.GetProperty("ModProjectile", I).SetValue(native, second);
        Require(Bind(second), "slot reuse invalidates cached identity");
        generation.SetValue(second, 8UL);
        Require(!(bool)playerType.GetProperty("HasHeld", I).GetValue(state), "connection replacement invalidates holdout");
        playerType.GetMethod("ReleaseHeld", I).Invoke(state, new[] { second });
        playerType.GetMethod("ClearCombat", I).Invoke(state, null);
        System.Console.WriteLine("PASS native held projectile defaults, full identity replication, one-per-player binding, stale cleanup and slot/connection reuse");
    }
}
