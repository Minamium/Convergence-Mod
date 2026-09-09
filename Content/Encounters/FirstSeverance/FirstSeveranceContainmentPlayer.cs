#nullable enable
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

// A short-lived capability issued only by the exact-Fight runtime/accepted
// replica. Ordinary world join, outsiders and stale state grant no movement power.
public sealed class FirstSeveranceContainmentPlayer : ModPlayer
{
    private FightId owner;
    private FirstSeveranceContainmentBounds bounds;
    private ulong expires, lastCorrection, connectionEpoch;
    private readonly FirstSeveranceMovementSyncCadence movementSync = new();
    private bool Active => !owner.IsNone && Main.GameUpdateCount < expires && Player.active && !Player.dead
        && (Main.netMode == NetmodeID.MultiplayerClient ||
            FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(Player.whoAmI, out ulong current) && current == connectionEpoch);
    // Received velocity already includes the owner's custom lift/edge braking.
    // Never apply that acceleration again to a server/remote simulation.
    private bool OwnsMovement => Main.netMode == NetmodeID.SinglePlayer
        || Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer;

    internal void Refresh(FightId fight, FirstSeveranceContainmentBounds field, ulong epoch = 0)
    {
        if (owner != fight || connectionEpoch != epoch || Main.GameUpdateCount >= expires)
            movementSync.Clear();
        owner = fight;
        bounds = field;
        connectionEpoch = epoch;
        expires = Main.GameUpdateCount + 180;
        if (Main.netMode != NetmodeID.MultiplayerClient) Constrain();
    }

    internal void Clear()
    {
        owner = FightId.None;
        expires = lastCorrection = connectionEpoch = 0;
        movementSync.Clear();
    }

    internal void Clear(FightId fight)
    {
        if (owner == fight) Clear();
    }

    public override void PreUpdateMovement()
    {
        if (!Active || !OwnsMovement) return;
        var next = bounds.ClampBody(Player.position.X + Player.velocity.X, Player.position.Y + Player.velocity.Y, Player.width, Player.height);
        if (next.X != Player.position.X + Player.velocity.X) Player.velocity.X = 0;
        if (next.Y != Player.position.Y + Player.velocity.Y) Player.velocity.Y = 0;
        var raid = Player.GetModPlayer<FirstSeveranceRaidPlayer>();
        // A participant without equipped wings still gets field-supported lift.
        // Equipped wings retain their own acceleration/hover behavior.
        if (!raid.IsRaidDowned && !raid.IsRaidEliminated && Player.wingsLogic == 0 && Player.controlJump)
            Player.velocity.Y = MathHelper.Lerp(Player.velocity.Y, -12f, .18f);
    }

    public override void PostUpdateEquips() => RefillFlight();
    public override void PostUpdate()
    {
        if (!Active) return;
        RefillFlight();
        // Server still authoritatively enforces the field, without driving input.
        if (OwnsMovement || Main.netMode == NetmodeID.Server) Constrain();
    }

    internal void SynchronizeOwnerMovement()
    {
        var raid = Player.GetModPlayer<FirstSeveranceRaidPlayer>();
        bool eligible = Main.netMode == NetmodeID.MultiplayerClient
            && Player.whoAmI == Main.myPlayer && Active
            && !raid.IsRaidDowned && !raid.IsRaidEliminated;
        if (movementSync.TryAdvance(Main.GameUpdateCount, eligible))
            NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI);
    }
    private void RefillFlight()
    {
        if (!Active) return;
        var raid = Player.GetModPlayer<FirstSeveranceRaidPlayer>();
        if (raid.IsRaidDowned || raid.IsRaidEliminated) return;
        Player.wingTime = Player.wingTimeMax;
        Player.rocketTime = Player.rocketTimeMax;
        Player.noFallDmg = true;
    }
    private void Constrain()
    {
        if (!Active) return;
        var p = bounds.ClampBody(Player.position.X, Player.position.Y, Player.width, Player.height);
        Vector2 corrected = new(p.X, p.Y);
        if (Vector2.DistanceSquared(corrected, Player.position) < .01f) return;
        // Two pixels of inward hysteresis prevent repeated edge corrections.
        p = bounds.ClampBody(corrected.X, corrected.Y, Player.width, Player.height, 2);
        corrected = new(p.X, p.Y);
        if (corrected.X != Player.position.X) Player.velocity.X = 0;
        if (corrected.Y != Player.position.Y) Player.velocity.Y = 0;
        Player.position = corrected;
        Player.fallStart = (int)(corrected.Y / 16);
        if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount >= lastCorrection + 12)
        {
            lastCorrection = Main.GameUpdateCount;
            NetMessage.SendData(MessageID.TeleportEntity, number: 0, number2: Player.whoAmI,
                number3: corrected.X, number4: corrected.Y, number5: 0);
            Mod.Logger.Info(System.FormattableString.Invariant($"FirstSeverance event=ContainmentCorrection fight={owner} tick={Main.GameUpdateCount} slot={Player.whoAmI} x={corrected.X:F1} y={corrected.Y:F1} vx={Player.velocity.X:F2} vy={Player.velocity.Y:F2}"));
        }
    }
    public override void OnEnterWorld() => Clear();
    public override void PlayerDisconnect() => Clear();
}
