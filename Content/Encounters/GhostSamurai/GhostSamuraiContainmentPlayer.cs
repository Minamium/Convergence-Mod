#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Networking.Replication;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

// Connection-local, expiring movement capability. The authority admits living
// players on entering the field; only that local owner predicts its edge clamp.
public sealed class GhostSamuraiContainmentPlayer : ModPlayer
{
    // Track only instances that actually received a lease. Teardown must never
    // index ModPlayer arrays belonging to unused/uninitialized Main.player slots.
    private static readonly HashSet<GhostSamuraiContainmentPlayer> leased = new();
    internal Guid Connection { get; private set; } = Guid.NewGuid();
    private GhostSamuraiBoss? boss;
    private Guid fight;
    private ulong expires, lastCorrection, lastSent;
    private Vector2 lastSafe;
    private bool hasSafe;
    internal bool BoundTo(GhostSamuraiBoss owner) => ReferenceEquals(boss, owner) && Active;
    private bool Active => boss is not null && fight != Guid.Empty && boss.Fight == fight
        && boss.NPC.active && ReferenceEquals(boss.NPC.ModNPC, boss) && boss.Arena.IsValid
        && Main.GameUpdateCount < expires && Player.active && !Player.dead && !Player.ghost && FightActive(boss);
    private bool OwnsMovement => Main.netMode == NetmodeID.SinglePlayer
        || Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer;
    internal static bool FightActive(GhostSamuraiBoss owner)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return owner.Runtime is not null && owner.Runtime.Matches(owner);
        var state = ModContent.GetInstance<EncounterReplicaSystem>().Snapshot;
        return owner.ProjectionFresh && state.FightId.Value == owner.Fight && state.DefinitionKey == GhostSamuraiDefinition.EncounterKey
            && state.Lifecycle is EncounterLifecycle.Validating or EncounterLifecycle.Preparing or EncounterLifecycle.Active;
    }
    internal void Refresh(GhostSamuraiBoss owner)
    {
        if (!FightActive(owner) || !owner.Arena.IsValid) return;
        if (!ReferenceEquals(boss, owner) || fight != owner.Fight) { Clear(); boss = owner; fight = owner.Fight; }
        leased.Add(this);
        expires = Main.GameUpdateCount + 45;
        if (Main.netMode != NetmodeID.MultiplayerClient) Constrain();
    }
    internal void PredictEntry()
    {
        if (!OwnsMovement || Main.netMode != NetmodeID.MultiplayerClient || Player.dead || Player.ghost) return;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is GhostSamuraiBoss owner && FightActive(owner)
                && (BoundTo(owner) || owner.Arena.Contains(Player.Center.X, Player.Center.Y)))
            { Refresh(owner); return; }
        Clear();
    }
    internal void Clear(Guid owner) { if (fight == owner) Clear(); }
    internal void Clear() { leased.Remove(this); boss = null; fight = Guid.Empty; expires = lastCorrection = lastSent = 0; hasSafe = false; }
    internal static void ClearAll(Guid? owner = null)
    {
        foreach (var instance in leased.ToArray())
            if (!owner.HasValue || instance.fight == owner.Value) instance.Clear();
    }
    private void ResetConnection() { Clear(); Connection = Guid.NewGuid(); }
    public override void Initialize() => ResetConnection();
    public override void OnEnterWorld() => ResetConnection();
    public override void PlayerDisconnect() => ResetConnection();
    public override void UpdateDead() => Clear();
    public override void PreUpdateMovement()
    {
        if (!Active || !OwnsMovement) return;
        var next = boss!.Arena.ClampBody(Player.position.X + Player.velocity.X, Player.position.Y + Player.velocity.Y, Player.width, Player.height);
        if (next.X != Player.position.X + Player.velocity.X) Player.velocity.X = 0;
        if (next.Y != Player.position.Y + Player.velocity.Y) Player.velocity.Y = 0;
    }
    public override void PostUpdate()
    {
        if (!Active) { Clear(); return; }
        if (OwnsMovement || Main.netMode == NetmodeID.Server) Constrain();
        if (Main.netMode == NetmodeID.MultiplayerClient && OwnsMovement && Main.GameUpdateCount >= lastSent + 6)
        { lastSent = Main.GameUpdateCount; NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI); }
    }
    private void Constrain()
    {
        if (!Active) return;
        var p = boss!.Arena.ClampBody(Player.position.X, Player.position.Y, Player.width, Player.height);
        Vector2 corrected = new(p.X, p.Y);
        if (Vector2.DistanceSquared(corrected, Player.position) < .01f)
        {
            if (!Collision.SolidCollision(Player.position, Player.width, Player.height)) { lastSafe = Player.position; hasSafe = true; }
            return;
        }
        p = boss.Arena.ClampBody(corrected.X, corrected.Y, Player.width, Player.height, 2);
        corrected = new(p.X, p.Y);
        // A recall/third-party teleport may project onto solid terrain at the
        // border. Prefer the last clear in-field position; never carve the world.
        if (hasSafe && Collision.SolidCollision(corrected, Player.width, Player.height)
            && !Collision.SolidCollision(lastSafe, Player.width, Player.height)) corrected = lastSafe;
        if (corrected.X != Player.position.X) Player.velocity.X = 0;
        if (corrected.Y != Player.position.Y) Player.velocity.Y = 0;
        Player.position = corrected;
        Player.fallStart = (int)(corrected.Y / 16);
        if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount >= lastCorrection + 12)
        {
            lastCorrection = Main.GameUpdateCount;
            NetMessage.SendData(MessageID.TeleportEntity, number: 0, number2: Player.whoAmI,
                number3: corrected.X, number4: corrected.Y, number5: 0);
        }
    }
}

internal sealed class GhostSamuraiContainmentSystem : ModSystem
{
    public override void PostUpdateEverything()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient && !Main.gameMenu && Main.LocalPlayer.active)
            Main.LocalPlayer.GetModPlayer<GhostSamuraiContainmentPlayer>().PredictEntry();
    }
    public override void OnWorldUnload() => GhostSamuraiContainmentPlayer.ClearAll();
    public override void Unload() => GhostSamuraiContainmentPlayer.ClearAll();
}
