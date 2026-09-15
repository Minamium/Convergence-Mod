#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Read-only actor projection grants a short-lived field capability. No state
// survives the exact Fight/roster, and no remote player gets input acceleration.
public sealed class CrimsonFieldPlayer : ModPlayer
{
    private ulong lastCorrection;
    private bool TryField(out CrimsonBoss? boss)
    {
        boss = null;
        if (Main.gameMenu || !Player.active || Player.dead || Player.ghost) return false;
        boss = CrimsonPackets.Boss;
        if (boss is null || !boss.Fresh || !boss.State.Contains(Player.whoAmI)) return false;
        return Main.netMode == NetmodeID.MultiplayerClient || Array.Exists(boss.State.Members,
            m => m.Slot == Player.whoAmI && m.Connection == Player.GetModPlayer<CrimsonConnection>().Token && !m.Out);
    }
    private bool OwnsMovement => Main.netMode == NetmodeID.SinglePlayer
        || Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer;
    public override void PreUpdateMovement()
    {
        if (!OwnsMovement || !TryField(out var boss)) return;
        var next = boss!.State.Field.ClampBody(Player.position.X + Player.velocity.X, Player.position.Y + Player.velocity.Y, Player.width, Player.height);
        if (next.X != Player.position.X + Player.velocity.X) Player.velocity.X = 0;
        if (next.Y != Player.position.Y + Player.velocity.Y) Player.velocity.Y = 0;
        if (Player.wingsLogic == 0 && Player.controlJump) Player.velocity.Y = MathHelper.Lerp(Player.velocity.Y, -12f, .18f);
    }
    public override void PostUpdateEquips()
    {
        if (!TryField(out _)) return;
        Player.wingTime = Player.wingTimeMax; Player.rocketTime = Player.rocketTimeMax; Player.noFallDmg = true;
    }
    public override void PostUpdate()
    {
        if (!TryField(out var boss) || !(OwnsMovement || Main.netMode == NetmodeID.Server)) return;
        var p = boss!.State.Field.ClampBody(Player.position.X, Player.position.Y, Player.width, Player.height);
        if (Vector2.DistanceSquared(Player.position, new(p.X, p.Y)) > .01f)
        {
            if (p.X != Player.position.X) Player.velocity.X = 0;
            if (p.Y != Player.position.Y) Player.velocity.Y = 0;
            Player.position = new(p.X, p.Y); Player.fallStart = (int)(p.Y / 16);
            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount >= lastCorrection + 12)
            {
                lastCorrection = Main.GameUpdateCount;
                NetMessage.SendData(MessageID.TeleportEntity, number: 0, number2: Player.whoAmI, number3: p.X, number4: p.Y);
            }
        }
        // Explicit owner cadence also carries custom no-wing lift/edge braking.
        if (Main.netMode == NetmodeID.MultiplayerClient && OwnsMovement && Main.GameUpdateCount % 6 == 0)
            NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI);
    }
    public override void OnEnterWorld() => lastCorrection = 0;
}

internal sealed class CrimsonFieldSpawns : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (CrimsonPackets.Boss is { Fresh: true } boss && boss.State.Contains(player.whoAmI))
        { maxSpawns = 0; spawnRate = int.MaxValue; }
    }
}
