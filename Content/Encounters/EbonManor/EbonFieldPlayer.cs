#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.EbonManor;

// Read-only actor projection grants a short-lived field capability. No state
// survives the exact Fight/roster, and no remote player gets input acceleration.
public sealed class EbonFieldPlayer : ModPlayer
{
    private ulong lastCorrection;
    private bool TryField(out EbonBoss? boss)
    {
        boss = null;
        if (Main.gameMenu || !Player.active || Player.dead || Player.ghost) return false;
        boss = EbonPackets.Boss;
        if (boss is null || !boss.Fresh || !boss.State.Contains(Player.whoAmI)) return false;
        return Main.netMode == NetmodeID.MultiplayerClient || Array.Exists(boss.State.Members,
            m => m.Slot == Player.whoAmI && m.Connection == Player.GetModPlayer<EbonConnection>().Token && !m.Out);
    }
    private bool OwnsMovement => Main.netMode == NetmodeID.SinglePlayer
        || Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer;
    public override void PreUpdateMovement()
    {
        if (!OwnsMovement || Player.GetModPlayer<EbonRecoveryPlayer>().IsIncapacitated || !TryField(out var boss)) return;
        var next = EbonRules.Clamp(boss!.State.Field, Player.position.X + Player.velocity.X, Player.position.Y + Player.velocity.Y, Player.width, Player.height);
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
        var p = EbonRules.Clamp(boss!.State.Field, Player.position.X, Player.position.Y, Player.width, Player.height);
        float correction = Vector2.DistanceSquared(Player.position, new(p.X, p.Y));
        // Ordinary gravity/roundoff at the grounded floor is not a teleport.
        if (correction > (Main.netMode == NetmodeID.Server ? 4 : .01f))
        {
            if (p.X != Player.position.X) Player.velocity.X = 0;
            if (p.Y != Player.position.Y) Player.velocity.Y = 0;
            Player.position = new(p.X, p.Y); Player.fallStart = (int)(p.Y / 16);
            if (Main.netMode == NetmodeID.Server && correction > 48 * 48 && Main.GameUpdateCount >= lastCorrection + 60)
            {
                lastCorrection = Main.GameUpdateCount;
                NetMessage.SendData(MessageID.TeleportEntity, number: 0, number2: Player.whoAmI, number3: p.X, number4: p.Y);
                EbonPackets.Log($"event=MajorFieldCorrection fight={boss.State.Fight} slot={Player.whoAmI} distance={MathF.Sqrt(correction):F1}");
            }
        }
        // Explicit owner cadence also carries custom no-wing lift/edge braking.
        if (Main.netMode == NetmodeID.MultiplayerClient && OwnsMovement && Main.GameUpdateCount % 6 == 0)
            NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI);
    }
    public override void OnEnterWorld() => lastCorrection = 0;
}

internal sealed class EbonFieldSpawns : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (EbonPackets.Boss is { Fresh: true } boss && boss.State.Contains(player.whoAmI))
        { maxSpawns = 0; spawnRate = int.MaxValue; }
    }
}
