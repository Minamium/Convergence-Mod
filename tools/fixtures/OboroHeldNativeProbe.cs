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
        CheckHand(mod, engine);
    }

    static void CheckHand(System.Reflection.Assembly mod, System.Reflection.Assembly engine)
    {
        var playerType = engine.GetType("Terraria.Player", true);
        var player = System.Activator.CreateInstance(playerType);
        var anchor = mod.GetType("Convergence.Content.Items.Oboro.OboroHandAnchor", true);
        var capture = anchor.GetMethod("Capture", S);
        var native = playerType.GetMethod("GetFrontHandPosition", I);
        var stretch = native.GetParameters()[0].ParameterType;
        object full = System.Enum.Parse(stretch, "Full");
        var facingField = playerType.GetField("direction", I);
        var gravity = playerType.GetField("gravDir", I);
        float X(object v) => (float)v.GetType().GetField("X").GetValue(v);
        float Y(object v) => (float)v.GetType().GetField("Y").GetValue(v);
        int count = 0;
        float largestGap = 0;
        foreach (int current in new[] { -1, 1 })
        foreach (int facing in new[] { -1, 1 })
        foreach (float grav in new[] { -1f, 1f })
        {
            facingField.SetValue(player, current); gravity.SetValue(player, grav);
            object basis = capture.Invoke(null, new object[] { player, facing });
            Require((int)facingField.GetValue(player) == current, "capture must not mutate facing");
            object center = playerType.GetProperty("MountedCenter", I).GetValue(player);
            facingField.SetValue(player, facing);
            for (int i = 0; i <= 72; i++)
            {
                float angle = i * System.MathF.Tau / 72;
                object actual = native.Invoke(player, new object[] { full, angle - System.MathF.PI / 2 });
                object sample = basis.GetType().GetMethod("At", I).Invoke(basis, new object[] { angle });
                float bx = (float)sample.GetType().GetField("Item1").GetValue(sample);
                float by = (float)sample.GetType().GetField("Item2").GetValue(sample);
                Require(System.MathF.Abs(X(actual) - X(center) - bx) < .0001f
                    && System.MathF.Abs(Y(actual) - Y(center) - by) < .0001f, "native hand basis must reconstruct rotation and mirroring");
                count++;
            }
            foreach (int step in new[] { 0, 1 })
            {
              var motion = mod.GetType("Convergence.Content.Items.Oboro." + (step == 0 ? "OboroFirstSwingMotion" : "OboroSecondSwingMotion"), true);
              int duration = step == 0 ? 18 : 16, ready = step == 0 ? 4 : 3, follow = step == 0 ? 16 : 14;
              for (int frame = 0; frame <= duration; frame++)
              {
                float progress = frame / (float)duration;
                float angle = (float)motion.GetMethod("Angle", S).Invoke(null, new object[] { progress });
                angle = (facing == 1 ? 0 : System.MathF.PI) + facing * angle;
                float weight = (float)motion.GetMethod("HandWeight", S).Invoke(null, new object[] { progress });
                object at = basis.GetType().GetMethod("At", I).Invoke(basis, new object[] { angle });
                float x = X(center) + (float)at.GetType().GetField("Item1").GetValue(at) * weight;
                float y = Y(center) + (float)at.GetType().GetField("Item2").GetValue(at) * weight;
                object grip = System.Activator.CreateInstance(center.GetType(), new object[] { x, y });
                object selected = anchor.GetMethod("Stretch", S).Invoke(null, new[] { player, grip, (object)angle });
                object actual = native.Invoke(player, new[] { selected, (object)(angle - System.MathF.PI / 2) });
                float gap = System.MathF.Sqrt(System.MathF.Pow(X(actual) - x, 2) + System.MathF.Pow(Y(actual) - y, 2));
                largestGap = System.MathF.Max(largestGap, gap);
                // Endpoints retain the centered grip of the next step. Native
                // stretch is discrete: the unchanged third-step150-degree pose
                // has a minimum9.333px gap under inverted gravity (None stretch).
                // Keep active strokes exact; allow only this harmless hilt join.
                Require(gap < (step == 0 ? 9 : 10), "hand stays within the hilt during root transition: step=" + step + " frame=" + frame + " facing=" + facing + " gravity=" + grav + " gap=" + gap + " stretch=" + selected);
                if (frame >= ready && frame <= follow) Require(gap < .0001f, "active stroke must be exactly hand anchored");
              }
            }
        }
        System.Console.WriteLine("PASS " + count + " installed hand-anchor samples: facing, counter-facing, gravity and full rotation");
        System.Console.WriteLine("PASS authored grips: first 4-16F and second 3-14F exact, transition gap <= " + largestGap + " pixels");
    }
}
