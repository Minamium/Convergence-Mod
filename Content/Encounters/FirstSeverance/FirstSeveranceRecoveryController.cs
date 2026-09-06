#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance.Actors;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceCombatIntentKind : byte
{
    Cancel = 0,
    PrototypeDown = 1,
    ReviveNearest = 2,
}

internal readonly record struct FirstSeveranceQueuedCombatIntent(
    FirstSeveranceCombatIntentKind Kind,
    FirstSeveranceRosterMember Member,
    uint RequestNonce);

// Owns bounded recovery intents, server player corrections and the Raid recovery boundary.
// It cannot advance/end the encounter; the orchestrator calls Commit once after all damage.
internal sealed class FirstSeveranceRecoveryController
{
    private const int MaximumPendingIntents = 16;
    private const float ReviveRangePixels = 8f * 16f;
    private readonly FightId fightId;
    private readonly FirstSeveranceRoster roster;
    private readonly Vector2 groundCenter;
    private readonly FirstSeveranceReviveBoundary revive;
    private readonly FirstSeveranceDebugAssistLease? debugAssist;
    private readonly uint[] lastQueuedNonces;
    private readonly bool[] observedConnected;
    private readonly RaidParticipantCombatState[] projectedCombatStates;
    private readonly uint[] healthRevisions;
    private readonly int[] correctedLife;
    private readonly Vector2[] downedPositions;
    private readonly ulong[] projectedReviveLockouts;
    private readonly List<FirstSeveranceQueuedCombatIntent> pendingIntents = new();
    private bool cancelRequested;
    private readonly Action<ulong, string> Log;
    private readonly Func<bool> canAcceptIntent;

    internal FirstSeveranceRecoveryController(FightId fightId, FirstSeveranceRoster roster,
        Vector2 groundCenter, FirstSeveranceDebugAssistLease? debugAssist,
        Action<ulong, string> log, Func<bool> canAcceptIntent)
    {
        this.fightId = fightId;
        this.roster = roster;
        this.groundCenter = groundCenter;
        this.debugAssist = debugAssist;
        Log = log;
        this.canAcceptIntent = canAcceptIntent;
        revive = new FirstSeveranceReviveBoundary(fightId, LogReviveEvent);
        lastQueuedNonces = new uint[roster.Count];
        observedConnected = new bool[roster.Count];
        projectedCombatStates = new RaidParticipantCombatState[roster.Count];
        healthRevisions = new uint[roster.Count];
        correctedLife = new int[roster.Count];
        downedPositions = new Vector2[roster.Count];
        projectedReviveLockouts = new ulong[roster.Count];
        Array.Fill(observedConnected, true);
        Array.Fill(projectedCombatStates, RaidParticipantCombatState.Alive);
    }

    internal bool IsInitialized => revive.IsInitialized;
    internal bool CancelRequested => cancelRequested;
    internal RaidReviveSnapshot? CreateSnapshot() => revive.CreateSnapshot();
    internal EncounterRuntimeUpdate Commit(in EncounterRuntimeContext context) => revive.Tick(context);

    internal bool TryInitialize(out string failure)
    {
        var bindings = new RaidParticipantBinding[roster.Count];
        for (int i = 0; i < roster.Count; i++) bindings[i] = ToReviveBinding(roster.Members[i]);
        return revive.TryInitialize(Array.AsReadOnly(bindings), out failure);
    }

    internal bool CanHitActor(FirstSeveranceRosterMember member)
        => projectedCombatStates[member.ParticipantId.Value] == RaidParticipantCombatState.Alive
            && !Main.player[member.ServerWhoAmI].GetModPlayer<FirstSeveranceRaidPlayer>().IsReviving;

    internal FirstSeveranceCombatParticipantProjection[] CreateParticipants(in RaidReviveSnapshot snapshot)
    {
        var result = new FirstSeveranceCombatParticipantProjection[roster.Count];
        for (int i = 0; i < roster.Count; i++)
            result[i] = CreateParticipantProjection(snapshot, FindReviveParticipant(snapshot, roster.Members[i].ParticipantId), i);
        return result;
    }

    internal void Cleanup(in EncounterCleanupContext context)
    {
        if (context.FightId != fightId) throw new InvalidOperationException("Recovery cleanup has the wrong Fight.");
        debugAssist?.Revoke(); // Clear protection before any terminal player death.
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            if (TryGetPlayer(member, out Player player))
            {
                player.GetModPlayer<FirstSeveranceRaidPlayer>().ClearRaidState();
                if (context.EndReason == EncounterEndReason.Defeat)
                    Log(Main.GameUpdateCount, $"event=DefeatDeathOrdered participant={member.ParticipantId.Value} slot={member.ServerWhoAmI}");
            }
        }

        revive.Cleanup(context);
        Array.Clear(projectedReviveLockouts);
        pendingIntents.Clear();
        Array.Clear(lastQueuedNonces);
    }

    internal bool TryQueueCancel(int senderWhoAmI, ulong connectionEpoch,
        uint requestNonce, out string failureCode)
    {
        if (!TryValidateIntent(senderWhoAmI, connectionEpoch, requestNonce,
                out FirstSeveranceRosterMember member, out failureCode))
            return false;
        if (member.ParticipantId != roster.InitiatorParticipantId)
        {
            failureCode = "first_severance.cancel_not_initiator";
            return false;
        }
        QueueIntent(FirstSeveranceCombatIntentKind.Cancel, member, requestNonce);
        return true;
    }

    internal bool TryQueuePrototypeDown(
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out string failureCode)
    {
        if (!TryValidateIntent(
                senderWhoAmI,
                connectionEpoch,
                requestNonce,
                out FirstSeveranceRosterMember member,
                out failureCode))
        {
            return false;
        }

        if (!TryGetReviveParticipant(member.ParticipantId, out RaidParticipantReviveSnapshot state)
            || state.CombatState != RaidParticipantCombatState.Alive)
        {
            failureCode = "first_severance.prototype_down_not_alive";
            return false;
        }

        QueueIntent(FirstSeveranceCombatIntentKind.PrototypeDown, member, requestNonce);
        failureCode = string.Empty;
        return true;
    }

    internal bool TryQueueReviveNearest(
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out string failureCode)
    {
        if (!TryValidateIntent(
                senderWhoAmI,
                connectionEpoch,
                requestNonce,
                out FirstSeveranceRosterMember member,
                out failureCode))
        {
            return false;
        }

        if (!TryValidateRevive(member, Main.GameUpdateCount, out _, out failureCode))
            return false;

        QueueIntent(FirstSeveranceCombatIntentKind.ReviveNearest, member, requestNonce);
        failureCode = string.Empty;
        return true;
    }

    private bool TryValidateIntent(
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out FirstSeveranceRosterMember member,
        out string failureCode)
    {
        if (!canAcceptIntent())
        {
            member = default;
            failureCode = "first_severance.combat_not_available";
            return false;
        }

        if (pendingIntents.Count >= MaximumPendingIntents)
        {
            member = default;
            failureCode = "first_severance.combat_intent_queue_full";
            return false;
        }

        if (!roster.TryResolveCurrentBinding(senderWhoAmI, connectionEpoch, out member))
        {
            failureCode = "first_severance.combat_sender_not_bound";
            return false;
        }

        int index = member.ParticipantId.Value;
        ParticipantId pendingParticipant = member.ParticipantId;
        if (pendingIntents.Exists(intent => intent.Member.ParticipantId == pendingParticipant))
        {
            failureCode = "first_severance.combat_request_pending";
            return false;
        }
        if (requestNonce == 0 || requestNonce <= lastQueuedNonces[index])
        {
            failureCode = "first_severance.combat_nonce_stale";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    private void QueueIntent(
        FirstSeveranceCombatIntentKind kind,
        FirstSeveranceRosterMember member,
        uint requestNonce)
    {
        pendingIntents.Add(new FirstSeveranceQueuedCombatIntent(kind, member, requestNonce));
        lastQueuedNonces[member.ParticipantId.Value] = requestNonce;
    }

    internal void ApplyPendingIntents(ulong authorityTick)
    {
        if (pendingIntents.Count == 0)
        {
            return;
        }

        pendingIntents.Sort(CompareIntents);
        for (int index = 0; index < pendingIntents.Count; index++)
        {
            FirstSeveranceQueuedCombatIntent intent = pendingIntents[index];
            RaidParticipantBinding binding = ToReviveBinding(intent.Member);
            if (intent.Kind == FirstSeveranceCombatIntentKind.Cancel)
            {
                cancelRequested = true;
                break;
            }
            if (!TryGetPlayer(intent.Member, out Player sender) || sender.dead)
                continue;
            if (intent.Kind == FirstSeveranceCombatIntentKind.PrototypeDown)
            {
                Log(authorityTick, $"event=DownRequested source=DebugCommand participant={intent.Member.ParticipantId.Value} slot={intent.Member.ServerWhoAmI}");
                revive.Apply(new AuthoritativeParticipantDownedCommand(
                    fightId,
                    binding,
                    authorityTick));
            }
        }
    }

    internal void ApplyConnectionChanges(ulong authorityTick)
    {
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            bool connected = FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(
                    member.ServerWhoAmI,
                    out ulong currentEpoch)
                && currentEpoch == member.ConnectionEpoch
                && Main.player[member.ServerWhoAmI].active;
            if (connected || !observedConnected[index])
            {
                continue;
            }

            observedConnected[index] = false;
            revive.Apply(new RaidParticipantDisconnectedCommand(
                fightId,
                ToReviveBinding(member),
                authorityTick));
        }
    }

    internal void ApplyPendingRevives(ulong authorityTick)
    {
        var starts = new List<RaidReviveStartCommand>(roster.Count);
        foreach (FirstSeveranceQueuedCombatIntent intent in pendingIntents)
        {
            if (intent.Kind != FirstSeveranceCombatIntentKind.ReviveNearest)
                continue;
            if (!TryValidateRevive(intent.Member, authorityTick, out ParticipantId target,
                    out string failureCode))
            {
                FirstSeverancePacketSystem.SendValidation(intent.Member.ServerWhoAmI,
                    intent.RequestNonce, false, failureCode);
                continue;
            }
            starts.Add(new RaidReviveStartCommand(fightId, ToReviveBinding(intent.Member),
                target, intent.RequestNonce, authorityTick));
        }
        if (starts.Count > 0)
        {
            RaidReviveStartBatchResult result = revive.ApplyStartBatch(starts);
            for (int index = 0; index < starts.Count; index++)
            {
                // Both the ingress list and the domain batch are ordered by stable reviver ID.
                RaidReviveCommandResult command = result.IsAccepted ? result.CommandResults[index]
                    : RaidReviveCommandResult.Rejected(result.FailureCode);
                FirstSeverancePacketSystem.SendValidation(starts[index].Reviver.PlayerSlot,
                    starts[index].RequestNonce, command.IsAccepted, command.FailureCode);
            }
        }
        pendingIntents.Clear();
    }

    internal void SynchronizeRevivePlayers(ulong authorityTick)
    {
        RaidReviveSnapshot? snapshot = revive.CreateSnapshot();
        if (snapshot is null)
        {
            return;
        }

        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            RaidParticipantReviveSnapshot participant =
                FindReviveParticipant(snapshot.Value, member.ParticipantId);
            if (!TryGetPlayer(member, out Player player))
            {
                projectedCombatStates[index] = participant.CombatState;
                continue;
            }

            RaidParticipantCombatState before = projectedCombatStates[index];
            bool lifeChanged = false;
            if (participant.CombatState == RaidParticipantCombatState.Downed
                && before == RaidParticipantCombatState.Alive)
            {
                downedPositions[index] = player.position;
                SetCorrectedLife(index, player, 1);
                lifeChanged = true;
            }
            else if (participant.CombatState == RaidParticipantCombatState.Alive
                && (before == RaidParticipantCombatState.Downed
                    || participant.ReviveLockoutUntilTick > projectedReviveLockouts[index]))
            {
                int restoredLife = Math.Max(1, (int)Math.Ceiling(player.statLifeMax2 * 0.35f));
                SetCorrectedLife(index, player, restoredLife);
                lifeChanged = true;
            }

            FirstSeveranceCombatParticipantProjection projection =
                CreateParticipantProjection(snapshot.Value, participant, index);
            player.GetModPlayer<FirstSeveranceRaidPlayer>().ApplyProjection(
                fightId, projection, authorityTick);
            player.GetModPlayer<FirstSeveranceContainmentPlayer>().Refresh(fightId,
                FirstSeveranceContainmentBounds.FromGround(groundCenter.X, groundCenter.Y), member.ConnectionEpoch);

            if (lifeChanged && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.PlayerLifeMana, number: member.ServerWhoAmI);
            }

            projectedCombatStates[index] = participant.CombatState;
            projectedReviveLockouts[index] = participant.ReviveLockoutUntilTick;
        }
    }

    private FirstSeveranceCombatParticipantProjection CreateParticipantProjection(
        in RaidReviveSnapshot snapshot, in RaidParticipantReviveSnapshot participant, int index)
    {
        ulong completesTick = 0;
        foreach (RaidReviveChannelSnapshot channel in snapshot.Channels)
            if (channel.Reviver == participant.Binding.ParticipantId)
                completesTick = channel.CompletesTick;
        return new FirstSeveranceCombatParticipantProjection(
            participant.Binding.ParticipantId, participant.Binding.PlayerSlot,
            participant.IsConnected, participant.CombatState, completesTick > 0,
            participant.DownedDeadlineTick, completesTick, healthRevisions[index],
            Math.Max(1, correctedLife[index]), downedPositions[index].X, downedPositions[index].Y,
            participant.InvulnerabilityUntilTick, participant.WeaknessUntilTick,
            participant.ReviveLockoutUntilTick,
            debugAssist?.Protects(fightId, roster.Members[index]) == true
                && participant.IsConnected && participant.CombatState == RaidParticipantCombatState.Alive);
    }

    private void SetCorrectedLife(int index, Player player, int life)
    {
        player.statLife = Math.Clamp(life, 1, player.statLifeMax2);
        correctedLife[index] = player.statLife;
        healthRevisions[index]++;
    }

    internal void ApplyRaidDamage(FirstSeveranceRosterMember member, int damage, ulong authorityTick, string source)
    {
        if (damage <= 0 || !TryGetPlayer(member, out Player player) || player.dead
            || !TryGetReviveParticipant(member.ParticipantId, out RaidParticipantReviveSnapshot state)
            || state.CombatState != RaidParticipantCombatState.Alive
            || authorityTick < state.InvulnerabilityUntilTick)
            return;

        if (debugAssist?.Protects(fightId, member) == true)
        {
            Log(authorityTick, $"event=DebugAssistWouldHit source={source} participant={member.ParticipantId.Value} damage={damage} life_before={player.statLife}");
            return;
        }

        Log(authorityTick, $"event=RaidDamage source={source} participant={member.ParticipantId.Value} slot={member.ServerWhoAmI} damage={damage} life_before={player.statLife} max_life={player.statLifeMax2} lethal={damage >= player.statLife}");

        // Experimental encounter-owned damage, not an interception of another Mod's hit.
        // This path never sends a lethal HP value or invokes Terraria's death hooks.
        if (damage >= player.statLife)
            revive.Apply(new AuthoritativeParticipantDownedCommand(
                fightId, ToReviveBinding(member), authorityTick));
        else
            SetCorrectedLife(member.ParticipantId.Value, player, player.statLife - damage);
    }

    internal void GrantPrototypeReviveKits()
    {
        int itemType = ModContent.ItemType<ResuscitationKitItem>();
        var source = new EntitySource_Misc("Convergence.FirstSeverancePrototype");
        for (int index = 0; index < roster.Count; index++)
        {
            if (TryGetPlayer(roster.Members[index], out Player player)
                && !player.HasItem(itemType))
            {
                player.QuickSpawnItem(source, itemType);
            }
        }
    }

    private bool TryValidateRevive(
        FirstSeveranceRosterMember reviver,
        ulong authorityTick,
        out ParticipantId target,
        out string failureCode)
    {
        target = ParticipantId.Invalid;
        failureCode = "first_severance.revive_sender_not_alive";
        if (!TryGetPlayer(reviver, out Player reviverPlayer) || reviverPlayer.dead
            || !IsAlive(reviver.ParticipantId))
        {
            return false;
        }
        if (reviverPlayer.HeldItem.type != ModContent.ItemType<ResuscitationKitItem>()
            || reviverPlayer.HeldItem.stack < 1)
        {
            failureCode = "first_severance.revive_kit_not_selected";
            return false;
        }

        RaidReviveSnapshot? snapshot = revive.CreateSnapshot();
        failureCode = "first_severance.revive_no_downed_ally_in_range";
        if (snapshot is null)
        {
            return false;
        }

        float nearestDistanceSquared = ReviveRangePixels * ReviveRangePixels;
        for (int index = 0; index < snapshot.Value.Participants.Count; index++)
        {
            RaidParticipantReviveSnapshot candidate = snapshot.Value.Participants[index];
            bool reserved = false;
            foreach (RaidReviveChannelSnapshot channel in snapshot.Value.Channels)
                reserved |= channel.Target == candidate.Binding.ParticipantId;
            if (candidate.Binding.ParticipantId == reviver.ParticipantId
                || reserved
                || candidate.CombatState != RaidParticipantCombatState.Downed
                || !candidate.IsConnected
                || !roster.TryGet(candidate.Binding.ParticipantId, out FirstSeveranceRosterMember member)
                || !TryGetPlayer(member, out Player targetPlayer))
            {
                continue;
            }

            float distanceSquared = Vector2.DistanceSquared(
                reviverPlayer.Center,
                targetPlayer.Center);
            if (candidate.DownedDeadlineTick != 0 && candidate.DownedDeadlineTick <= authorityTick)
                continue;
            if (candidate.ReviveLockoutUntilTick > authorityTick)
            {
                if (distanceSquared <= ReviveRangePixels * ReviveRangePixels)
                    failureCode = "first_severance.revive_target_recovery_locked";
                continue;
            }
            if (distanceSquared <= nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                target = member.ParticipantId;
            }
        }

        if (target.IsValid)
            failureCode = string.Empty;
        return target.IsValid;
    }

    private bool TryGetReviveParticipant(
        ParticipantId participantId,
        out RaidParticipantReviveSnapshot participant)
    {
        RaidReviveSnapshot? snapshot = revive.CreateSnapshot();
        if (snapshot is not null)
        {
            for (int index = 0; index < snapshot.Value.Participants.Count; index++)
            {
                if (snapshot.Value.Participants[index].Binding.ParticipantId == participantId)
                {
                    participant = snapshot.Value.Participants[index];
                    return true;
                }
            }
        }

        participant = default;
        return false;
    }

    internal bool IsAlive(ParticipantId participantId)
    {
        return TryGetReviveParticipant(participantId, out RaidParticipantReviveSnapshot participant)
            && participant.CombatState == RaidParticipantCombatState.Alive;
    }

    internal bool TryGetPlayer(FirstSeveranceRosterMember member, out Player player)
    {
        if (member.ServerWhoAmI >= 0
            && member.ServerWhoAmI < Main.maxPlayers
            && Main.player[member.ServerWhoAmI].active
            && FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(member.ServerWhoAmI, out ulong epoch)
            && epoch == member.ConnectionEpoch)
        {
            player = Main.player[member.ServerWhoAmI];
            return true;
        }

        player = null!;
        return false;
    }

    private static RaidParticipantBinding ToReviveBinding(
        FirstSeveranceRosterMember member)
    {
        return new RaidParticipantBinding(
            member.ParticipantId,
            member.ServerWhoAmI,
            member.ConnectionEpoch);
    }

    private static RaidParticipantReviveSnapshot FindReviveParticipant(
        in RaidReviveSnapshot snapshot,
        ParticipantId participantId)
    {
        for (int index = 0; index < snapshot.Participants.Count; index++)
        {
            if (snapshot.Participants[index].Binding.ParticipantId == participantId)
            {
                return snapshot.Participants[index];
            }
        }

        throw new InvalidOperationException("The revive snapshot lost a roster participant.");
    }

    private static bool IsReviver(
        in RaidReviveSnapshot snapshot,
        ParticipantId participantId)
    {
        for (int index = 0; index < snapshot.Channels.Count; index++)
        {
            if (snapshot.Channels[index].Reviver == participantId)
            {
                return true;
            }
        }

        return false;
    }

    private static int CompareIntents(
        FirstSeveranceQueuedCombatIntent left,
        FirstSeveranceQueuedCombatIntent right)
    {
        int kind = left.Kind.CompareTo(right.Kind);
        if (kind != 0)
        {
            return kind;
        }

        int participant = left.Member.ParticipantId.Value.CompareTo(
            right.Member.ParticipantId.Value);
        return participant != 0
            ? participant
            : left.RequestNonce.CompareTo(right.RequestNonce);
    }

    private void LogReviveEvent(RaidReviveEvent entry)
    {
        Log(entry.AuthorityTick, $"event={entry.Kind} participant={entry.Subject.Value} actor={entry.Actor.Value} cancel={entry.CancelReason} failure={entry.FailureReason} elimination={entry.EliminationReason} recovery_lockout_until={entry.ReviveLockoutUntilTick}");
    }
}
