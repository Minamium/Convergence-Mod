using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.AzureCathedral;

internal enum AzureRecoveryReply : byte { None, Revived, NoTarget, Locked, RequiresKit, Invalid }

// Native damage is receiving-player owned; ONLY this exact-Fight authority
// adapter turns a native HP-floor receipt into Down and commits ally recovery.
internal sealed class AzureRecoveryController
{
    private readonly FightId fight;
    private readonly RaidReviveService service;
    private readonly RaidParticipantBinding[] bindings;
    private readonly Guid[] connections;
    private readonly AzureRecoveryState[] health;
    private readonly AzureNativeFloor[] floors;
    private readonly uint[] downNonces, reviveNonces;
    private readonly AzureRecoveryRequest?[] pendingRevives;
    internal bool Failed => service.FailureReason != RaidReviveFailureReason.None;

    internal AzureRecoveryController(Guid id, AzureMember[] roster)
    {
        fight = new(id); bindings = new RaidParticipantBinding[roster.Length];
        connections = new Guid[roster.Length]; health = new AzureRecoveryState[roster.Length];
        floors = new AzureNativeFloor[roster.Length];
        downNonces = new uint[roster.Length]; reviveNonces = new uint[roster.Length];
        pendingRevives = new AzureRecoveryRequest?[roster.Length];
        for (int i = 0; i < roster.Length; i++)
        {
            // Epochs are authority-assigned within this immutable Fight roster;
            // the actual connection GUID is separately checked on every ingress.
            bindings[i] = new(new ParticipantId((byte)i), roster[i].Slot, (ulong)i + 1);
            connections[i] = roster[i].Connection;
            var p = Main.player[roster[i].Slot];
            health[i] = new(1, false, Math.Max(1, p.statLife), p.position.X, p.position.Y, 0, 0);
            floors[i].Reset(p.statLife);
            int kit = ModContent.ItemType<ResuscitationKitItem>();
            if (!p.HasItem(kit)) p.QuickSpawnItem(new EntitySource_Misc("Convergence.AzureRecovery"), kit);
        }
        service = new(fight, RaidReviveSettings.CreateInstantUnlimited(roster.Length), bindings);
    }

    private int Index(int sender, in AzureRecoveryRequest request)
    {
        for (int i = 0; i < bindings.Length; i++)
            if (bindings[i].PlayerSlot == sender && connections[i] == request.Connection
                && request.Nonce != 0 && health[i].Revision == request.HealthRevision
                && Connected(i)) return i;
        return -1;
    }
    private bool Connected(int i)
    {
        var p = Main.player[bindings[i].PlayerSlot];
        return p.active && !p.dead && !p.ghost && p.GetModPlayer<AzureConnection>().Token == connections[i];
    }
    internal bool Receive(int sender, AzureRecoveryRequest request, bool revive)
    {
        if (service.IsCleaned || Failed) return false;
        int i = Index(sender, request); if (i < 0) return false;
        if (!revive)
        {
            // The packet cannot invent damage, an anchor, or health. Terraria's
            // preceding PlayerLifeMana message must already expose the floor.
            if (request.Nonce <= downNonces[i] || Main.player[sender].statLife > 1 || health[i].Downed) return false;
            downNonces[i] = request.Nonce;
            floors[i].Observe(1, nativeLethalReceipt: true);
            return true; // The single authority Tick samples/commits the floor.
        }
        if (request.Nonce <= reviveNonces[i] || health[i].Downed || pendingRevives[i].HasValue) return false;
        reviveNonces[i] = request.Nonce; pendingRevives[i] = request;
        return true;
    }

    internal void Project(AzureMember[] members)
    { for (int i = 0; i < health.Length; i++) members[i] = members[i] with { Recovery = health[i] }; }

    internal bool Tick(AzureState state, AzureMember[] members)
    {
        ulong tick = (ulong)state.Age;
        bool changed = false;
        var before = service.CreateSnapshot();
        for (int i = 0; i < bindings.Length; i++)
        {
            if (!Connected(i) || members[i].Out)
            {
                if (before.Participants[i].IsConnected)
                {
                    service.Apply(new RaidParticipantDisconnectedCommand(fight, bindings[i], tick));
                    changed = true;
                }
                pendingRevives[i] = null;
                continue;
            }
            var player = Main.player[bindings[i].PlayerSlot];
            if (!health[i].Downed && floors[i].Observe(player.statLife))
            {
                var result = service.Apply(new AuthoritativeParticipantDownedCommand(fight, bindings[i], tick));
                if (result.HasObservableChange)
                {
                    var anchor = AzureRules.Clamp(state.Field, player.position.X, player.position.Y, player.width, player.height);
                    health[i] = health[i] with { Revision = health[i].Revision + 1, Downed = true, Life = 1,
                        X = anchor.X, Y = anchor.Y, ImmunityUntil = 0 };
                    player.statLife = 1; changed = true;
                    AzurePackets.Log($"event=Downed fight={fight.Value} age={state.Age} slot={player.whoAmI} health_revision={health[i].Revision} anchor={anchor.X:F1},{anchor.Y:F1} basis=NativeHpFloor");
                }
            }
        }

        // Downs/disconnects precede one stable-ID revive batch and one terminal
        // commit. A simultaneous lethal hit cannot revive another participant.
        var starts = new List<RaidReviveStartCommand>();
        var senders = new List<int>();
        for (int i = 0; i < bindings.Length; i++)
        {
            if (pendingRevives[i] is not { } request) continue;
            pendingRevives[i] = null;
            var p = Main.player[bindings[i].PlayerSlot];
            var reply = ValidateRevive(i, request, state.Age, out int target);
            if (reply != AzureRecoveryReply.None) { AzurePackets.RecoveryReply(p.whoAmI, reply); continue; }
            starts.Add(new(fight, bindings[i], bindings[target].ParticipantId, request.Nonce, tick));
            senders.Add(i);
        }
        if (starts.Count > 0)
        {
            var results = service.ApplyStartBatch(starts);
            for (int i = 0; i < senders.Count; i++)
            {
                bool accepted = results.IsAccepted && results.CommandResults[i].IsAccepted;
                AzurePackets.RecoveryReply(bindings[senders[i]].PlayerSlot,
                    accepted ? AzureRecoveryReply.Revived : AzureRecoveryReply.Invalid);
            }
        }
        var commit = service.CommitTick(fight, tick);
        foreach (var e in commit.Events)
        {
            if (e.Kind != RaidReviveEventKind.ParticipantRevived) continue;
            int i = e.Subject.Value; var p = Main.player[bindings[i].PlayerSlot];
            int life = Math.Max(1, (int)Math.Ceiling(p.statLifeMax2 * e.RestoredLifeRatio));
            health[i] = health[i] with { Revision = health[i].Revision + 1, Downed = false, Life = life,
                ImmunityUntil = (int)e.InvulnerabilityUntilTick, LockoutUntil = (int)e.ReviveLockoutUntilTick };
            p.statLife = life;
            floors[i].Reset(life);
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.PlayerLifeMana, number: p.whoAmI);
            AzurePackets.Log($"event=Revived fight={fight.Value} age={state.Age} slot={p.whoAmI} by={bindings[e.Actor.Value].PlayerSlot} health_revision={health[i].Revision} life={life} lockout_until={health[i].LockoutUntil}");
            changed = true;
        }
        Project(members);
        return changed || commit.HasObservableChange;
    }

    private AzureRecoveryReply ValidateRevive(int i, AzureRecoveryRequest request, int age, out int target)
    {
        target = -1;
        if (!Connected(i) || health[i].Downed || health[i].Revision != request.HealthRevision) return AzureRecoveryReply.Invalid;
        var p = Main.player[bindings[i].PlayerSlot];
        if (p.HeldItem.type != ModContent.ItemType<ResuscitationKitItem>()) return AzureRecoveryReply.RequiresKit;
        float distance = 128 * 128; bool locked = false;
        for (int j = 0; j < health.Length; j++)
        {
            if (j == i || !Connected(j) || !health[j].Downed) continue;
            float d = Vector2.DistanceSquared(p.Center, Main.player[bindings[j].PlayerSlot].Center);
            if (d > 128 * 128) continue;
            if (age < health[j].LockoutUntil) { locked = true; continue; }
            if (d <= distance) { target = j; distance = d; }
        }
        return target >= 0 ? AzureRecoveryReply.None : locked ? AzureRecoveryReply.Locked : AzureRecoveryReply.NoTarget;
    }
    internal void Cleanup()
    {
        service.TryCleanup(fight);
        Array.Clear(pendingRevives);
        AzureRecoveryPlayer.ClearFight(fight.Value);
    }
}
