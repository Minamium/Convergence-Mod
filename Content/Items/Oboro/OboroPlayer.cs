using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Items.Oboro;

public sealed partial class OboroPlayer : ModPlayer
{
    internal OboroSnapshot View;
    internal ulong ReceivedAt;
    internal uint RequestNonce, LastNonce;
    private ulong generation;
    private uint revision;
    private readonly OboroTiming timing = new();
    private int age => timing.Age;
    private int duration => timing.Duration;
    private int step => timing.Step;
    private int zanshin => timing.Zanshin;
    private int facing = 1;
    private float aim;
    private Item swingItem;
    private bool rightHeld;
    private readonly HashSet<ulong> struck = new();
    private readonly Dictionary<ulong, OboroWounds> wounds = new();
    internal static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    internal float VisualAge => View.Duration == 0 ? 0 : Math.Min(View.Duration, View.Age + Math.Min(12UL, Main.GameUpdateCount - ReceivedAt));
    internal bool SwingVisible => View.Duration > 0 && VisualAge < View.Duration;
    internal int ZanshinRemaining => Authority ? zanshin : Math.Max(0, View.Zanshin - (int)Math.Min(300UL, Main.GameUpdateCount - ReceivedAt));
    internal bool Usable => Player.active && !Player.dead && !Player.noItems && !Player.CCed;
    internal bool Holding => Player.HeldItem.type == ModContent.ItemType<Oboro>();
    public override void Initialize()
    {
        generation = 0; View = default; revision = RequestNonce = LastNonce = 0;
        timing.Initialize(); facing = 1; aim = 0;
        ReceivedAt = 0; rightHeld = false;
        wounds.Clear(); struck.Clear(); swingItem = null;
    }
    public override void OnEnterWorld()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        Initialize();
        OboroPackets.Request(Player, OboroAction.Hello, 0);
    }
    internal void EnsureGeneration() { if (generation == 0) generation = OboroPackets.NextGeneration(); }
    internal void Handle(OboroRequest request)
    {
        if (!Authority) return;
        EnsureGeneration();
        if (request.Action == OboroAction.Hello) { Publish(Player.whoAmI); return; }
        if (!Usable || !Holding || request.Generation != generation || request.Nonce <= LastNonce) return;
        LastNonce = request.Nonce;
        if (request.Action == OboroAction.Zanshin)
        {
            var result = timing.Toggle(Main.GameUpdateCount);
            if (result == OboroToggle.Detonate) Detonate();
            else if (result == OboroToggle.Started) Publish();
            return;
        }
        if (!timing.TryBegin(Main.GameUpdateCount, Player.GetAttackSpeed(Player.HeldItem.DamageType))) return;
        aim = request.Aim; facing = MathF.Cos(aim) < 0 ? -1 : 1;
        swingItem = Player.HeldItem.Clone(); struck.Clear();
        Publish();
    }
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        bool press = Main.mouseRight && !rightHeld; rightHeld = Main.mouseRight;
        if (press && Usable && Holding && !Main.gamePaused && Main.hasFocus && !Main.playerInventory
            && !Main.mapFullscreen && !Player.mouseInterface && !Main.blockInput && !Main.drawingPlayerChat)
            OboroPackets.Request(Player, OboroAction.Zanshin, 0);
    }
    public override void PostUpdateEquips()
    {
        if (ZanshinRemaining <= 0 || !Player.active || Player.dead) return;
        Player.statDefense += OboroRules.DefenseBonus; Player.moveSpeed += OboroRules.MovementBonus;
        Player.AddBuff(ModContent.BuffType<Zanshin>(), ZanshinRemaining, quiet: true);
    }
    public override void PostUpdate()
    {
        if (!Authority) return;
        EnsureGeneration();
        if (!Player.active || Player.dead) { ClearCombat(); return; }
        if (timing.TickZanshin()) Detonate();
        PruneWounds();
        if (duration > 0)
        {
            if (!Usable || !Holding) { timing.CancelSwing(); Publish(); }
            else
            {
                ResolveSwing();
                if (timing.AdvanceSwing(Main.GameUpdateCount)) Publish();
                else if (age % 6 == 0) Publish();
            }
        }
        else
        {
            if (!Holding) timing.CancelSwing();
            if (zanshin > 0 && zanshin % 15 == 0) Publish();
        }
    }
    public override void UpdateDead() { if (Authority) ClearCombat(); }
    public override void PlayerDisconnect()
    {
        if (!Authority) return;
        ClearCombat(); generation = 0; LastNonce = 0;
    }
    internal void ClearCombat()
    {
        bool changed = duration != 0 || zanshin != 0 || wounds.Count != 0;
        ClearWounds(); timing.Clear(); struck.Clear();
        if (changed) Publish();
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) { if (Authority) Publish(toWho); }
    internal void Publish(int toWho = -1)
    {
        EnsureGeneration();
        View = new((byte)Player.whoAmI, generation, ++revision, timing.Serial, (byte)step, (ushort)age,
            (ushort)duration, aim, (sbyte)facing, (ushort)zanshin);
        ReceivedAt = Main.GameUpdateCount;
        OboroPackets.Publish(View, toWho);
    }
}
