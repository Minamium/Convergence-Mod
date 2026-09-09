#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Chat;
using Terraria.Localization;
using Microsoft.Xna.Framework;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeverancePreparationIntentKind : byte
{
    Cancel = 1,
    SetReady = 2,
}

internal readonly record struct FirstSeveranceQueuedPreparationIntent(
    FirstSeverancePreparationIntentKind Kind,
    ParticipantId ParticipantId,
    int SenderWhoAmI,
    ulong ConnectionEpoch,
    bool IsReady,
    uint RequestNonce);

internal sealed class FirstSeverancePreparationRuntime : IEncounterRuntime
{
    private const int MaximumPendingIntents = 16;

    private readonly ulong encounterSequence;
    private readonly FightId fightId;
    private readonly FirstSeveranceArenaValidationResult arena;
    private readonly FirstSeveranceRoster roster;
    private readonly int serverTileEntityId;
    private readonly TilePoint coreTopLeft;
    private readonly uint[] lastQueuedNonces;
    private readonly List<FirstSeveranceQueuedPreparationIntent> pendingIntents = new();
    private FirstSeverancePreparationStateMachine? preparation;
    private FirstSeverancePrototypeCombatRuntime? combat;
    private FirstSeveranceDebugAssistLease? debugAssist;
    private bool isAttached;
    private bool isCleaned;
    private ulong allReadySince;

    public FirstSeverancePreparationRuntime(
        ulong encounterSequence,
        FightId fightId,
        FirstSeveranceResolvedPreparation resolved)
    {
        if (encounterSequence == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(encounterSequence));
        }

        if (fightId.IsNone)
        {
            throw new ArgumentException("A preparation runtime requires a Fight ID.", nameof(fightId));
        }

        ArgumentNullException.ThrowIfNull(resolved);
        this.encounterSequence = encounterSequence;
        this.fightId = fightId;
        arena = resolved.Arena;
        roster = resolved.Roster;
        serverTileEntityId = resolved.Core.ID;
        coreTopLeft = new TilePoint(resolved.Core.Position.X, resolved.Core.Position.Y);
        lastQueuedNonces = new uint[roster.Count];

        // This is intentionally the final constructor action. A successful claim
        // is therefore followed immediately by returning a cleanup-owning runtime.
        if (!FoundationCoreProtectionSystem.TryClaim(
                resolved.Core,
                encounterSequence,
                fightId))
        {
            throw new EncounterStartRejectedException("first_severance.core_busy");
        }
    }

    internal bool Matches(ulong candidateSequence, FightId candidateFightId)
    {
        return !isCleaned
            && encounterSequence == candidateSequence
            && fightId == candidateFightId;
    }

    internal bool TryQueueReady(
        int senderWhoAmI,
        ulong connectionEpoch,
        bool isReady,
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

        pendingIntents.Add(new FirstSeveranceQueuedPreparationIntent(
            FirstSeverancePreparationIntentKind.SetReady,
            member.ParticipantId,
            senderWhoAmI,
            connectionEpoch,
            isReady,
            requestNonce));
        lastQueuedNonces[member.ParticipantId.Value] = requestNonce;
        failureCode = string.Empty;
        return true;
    }

    internal bool TryQueueCancel(
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

        if (member.ParticipantId != roster.InitiatorParticipantId)
        {
            failureCode = "first_severance.preparation_cancel_not_initiator";
            return false;
        }

        pendingIntents.Add(new FirstSeveranceQueuedPreparationIntent(
            FirstSeverancePreparationIntentKind.Cancel,
            member.ParticipantId,
            senderWhoAmI,
            connectionEpoch,
            IsReady: false,
            requestNonce));
        lastQueuedNonces[member.ParticipantId.Value] = requestNonce;
        failureCode = string.Empty;
        return true;
    }

    internal bool TryCreateProjection(
        out FirstSeverancePreparationProjection? projection)
    {
        if (isCleaned || preparation is null)
        {
            projection = null;
            return false;
        }

        FirstSeverancePreparationSnapshot snapshot = preparation.CreateSnapshot();
        projection = new FirstSeverancePreparationProjection(
            encounterSequence,
            fightId,
            serverTileEntityId,
            coreTopLeft,
            arena.Layout.ArenaBounds,
            snapshot.EnteredTick,
            snapshot.DeadlineTick,
            snapshot.CombatGateClosed,
            snapshot.Members);
        return true;
    }

    public EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context)
    {
        if (isCleaned)
        {
            throw new InvalidOperationException("A cleaned preparation runtime cannot be ticked.");
        }

        if (context.EncounterSequence != encounterSequence || context.FightId != fightId)
        {
            throw new InvalidOperationException("Preparation runtime context has the wrong identity.");
        }

        return context.Lifecycle switch
        {
            EncounterLifecycle.Validating => EnterPreparing(context.AuthorityTick),
            EncounterLifecycle.Preparing => TickPreparing(context.AuthorityTick),
            EncounterLifecycle.Active => TickActive(context),
            _ => EncounterRuntimeUpdate.End(
                FirstSeveranceTerminationContract.Instance.Create(
                    FirstSeveranceTerminalCause.RuntimeInvariantBroken)),
        };
    }

    public void Cleanup(in EncounterCleanupContext context)
    {
        if (isCleaned)
        {
            return;
        }

        if (context.EncounterSequence != encounterSequence || context.FightId != fightId)
        {
            throw new InvalidOperationException("Preparation cleanup has the wrong identity.");
        }

        combat?.Cleanup(context);
        foreach (var member in roster.Members)
            Main.player[member.ServerWhoAmI].GetModPlayer<FirstSeveranceContainmentPlayer>().Clear(fightId);
        FirstSeveranceDebugAssistSystem.Release(debugAssist);
        debugAssist = null;
        preparation?.Cleanup(fightId);
        pendingIntents.Clear();
        Array.Clear(lastQueuedNonces);
        if (isAttached)
        {
            FirstSeverancePreparationAuthority.Detach(this);
            isAttached = false;
        }

        if (!FoundationCoreProtectionSystem.TryRelease(serverTileEntityId, fightId))
        {
            throw new InvalidOperationException("Exact-Fight Foundation Core cleanup was rejected.");
        }

        isCleaned = true;
    }

    private EncounterRuntimeUpdate EnterPreparing(ulong authorityTick)
    {
        if (preparation is not null || isAttached)
        {
            return EncounterRuntimeUpdate.End(
                FirstSeveranceTerminationContract.Instance.Create(
                    FirstSeveranceTerminalCause.RuntimeInvariantBroken));
        }

        debugAssist = FirstSeveranceDebugAssistSystem.Claim(fightId, roster);
        preparation = new FirstSeverancePreparationStateMachine(
            fightId,
            roster,
            authorityTick,
            debugAssist is not null
                ? new FirstSeverancePreparationSettings(10 * 60 * 60)
                : FirstSeverancePreparationSettings.Default,
            FirstSeverancePreparationTimeline.DeploymentTicks);
        if (!FirstSeverancePreparationAuthority.TryAttach(this))
        {
            return EncounterRuntimeUpdate.End(
                FirstSeveranceTerminationContract.Instance.Create(
                    FirstSeveranceTerminalCause.RuntimeInvariantBroken));
        }

        isAttached = true;
        RefreshPreparationField(gather: true);
        ModContent.GetInstance<FirstSeveranceClientStateSystem>().Mod.Logger.Info(
            $"FirstSeverance event=PreparationStarted seq={encounterSequence} fight={fightId} participants={roster.Count} ready_opens={preparation.ReadyOpensTick} slots={string.Join(',', System.Linq.Enumerable.Select(roster.Members, m => m.ServerWhoAmI))}");
        return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Preparing);
    }

    private EncounterRuntimeUpdate TickPreparing(ulong authorityTick)
    {
        if (preparation is null || !isAttached)
        {
            return EncounterRuntimeUpdate.End(
                FirstSeveranceTerminationContract.Instance.Create(
                    FirstSeveranceTerminalCause.RuntimeInvariantBroken));
        }

        bool hasObservableChange = false;
        // No late join, disconnect, death or slot replacement may silently shrink
        // the group behind an already displayed Ready denominator.
        if (!ServerRosterMatches())
        {
            ModContent.GetInstance<FirstSeveranceClientStateSystem>().Mod.Logger.Info(
                $"FirstSeverance event=PreparationCancelled seq={encounterSequence} reason=RosterChanged tick={authorityTick}");
            var message = NetworkText.FromKey("Mods.Convergence.UI.PreparationDeployment.RosterChanged");
            if (Main.netMode == NetmodeID.Server) ChatHelper.BroadcastChatMessage(message, Color.Orange);
            else Main.NewText(message.ToString(), Color.Orange);
            return EncounterRuntimeUpdate.End(FirstSeveranceTerminationContract.Instance.Create(FirstSeveranceTerminalCause.UserCancelled));
        }
        RefreshPreparationField(gather: false);
        if (pendingIntents.Count > 0)
        {
            pendingIntents.Sort(CompareIntents);
            for (int index = 0; index < pendingIntents.Count; index++)
            {
                FirstSeveranceQueuedPreparationIntent intent = pendingIntents[index];
                FirstSeverancePreparationUpdate applied = intent.Kind switch
                {
                    FirstSeverancePreparationIntentKind.Cancel => preparation.ApplyCancel(
                        new FirstSeveranceCancelPreparationCommand(
                            intent.SenderWhoAmI,
                            intent.ConnectionEpoch,
                            intent.RequestNonce,
                            authorityTick)),
                    FirstSeverancePreparationIntentKind.SetReady => preparation.ApplySetReady(
                        new FirstSeveranceSetReadyCommand(
                            intent.SenderWhoAmI,
                            intent.ConnectionEpoch,
                            intent.IsReady,
                            intent.RequestNonce,
                            authorityTick)),
                    _ => FirstSeverancePreparationUpdate.Reject(
                        "first_severance.preparation_intent_invalid"),
                };

                hasObservableChange |= applied.HasObservableChange;
                if (applied.IsAccepted && intent.Kind == FirstSeverancePreparationIntentKind.SetReady)
                    ModContent.GetInstance<FirstSeveranceClientStateSystem>().Mod.Logger.Info(
                        $"FirstSeverance event=PreparationReady seq={encounterSequence} slot={intent.SenderWhoAmI} ready={intent.IsReady} tick={authorityTick}");
                if (applied.RequestsEnd)
                {
                    pendingIntents.Clear();
                    return EncounterRuntimeUpdate.End(applied.RequestedTermination);
                }
            }

            pendingIntents.Clear();
        }

        bool coreIsPresent = FoundationCoreProtectionSystem.TryResolveOwnedCore(
            serverTileEntityId,
            fightId,
            out _);
        FirstSeverancePreparationUpdate update = preparation.Advance(
            authorityTick,
            coreIsPresent,
            ObserveConnections());
        if (update.RequestsEnd)
        {
            return EncounterRuntimeUpdate.End(update.RequestedTermination);
        }

        hasObservableChange |= update.HasObservableChange;
        bool allReady = preparation.CreateSnapshot().AreAllReady;
        if (!allReady) allReadySince = 0;
        else if (allReadySince == 0) allReadySince = authorityTick;
        if (allReady && authorityTick >= allReadySince + FirstSeverancePreparationTimeline.ReadyHoldTicks)
        {
            combat = new FirstSeverancePrototypeCombatRuntime(
                encounterSequence,
                fightId,
                roster,
                serverTileEntityId,
                coreTopLeft,
                arena.Layout,
                debugAssist);
            if (!combat.TryStart(authorityTick, out _))
            {
                return EncounterRuntimeUpdate.End(
                    FirstSeveranceTerminationContract.Instance.Create(
                        FirstSeveranceTerminalCause.RuntimeInvariantBroken));
            }

            preparation.Cleanup(fightId);
            preparation = null;
            FirstSeverancePreparationAuthority.Detach(this);
            isAttached = false;
            pendingIntents.Clear();
            return EncounterRuntimeUpdate.TransitionTo(EncounterLifecycle.Active);
        }

        return hasObservableChange || authorityTick % 30 == 0
            ? EncounterRuntimeUpdate.ObservableChange()
            : EncounterRuntimeUpdate.None;
    }

    private EncounterRuntimeUpdate TickActive(in EncounterRuntimeContext context)
    {
        return combat?.Tick(context)
            ?? EncounterRuntimeUpdate.End(
                FirstSeveranceTerminationContract.Instance.Create(
                    FirstSeveranceTerminalCause.RuntimeInvariantBroken));
    }

    private bool ServerRosterMatches()
    {
        var connections = new List<FirstSeveranceConnectionObservation>(roster.Count + 1);
        for (int slot = 0; slot < Main.maxPlayers; slot++)
        {
            Player player = Main.player[slot];
            if (!player.active) continue;
            if (player.dead || player.ghost || !FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(slot, out ulong epoch)) return false;
            connections.Add(new(slot, epoch, true));
        }
        return roster.MatchesConnected(connections);
    }

    private void RefreshPreparationField(bool gather)
    {
        Vector2 ground = new(arena.Layout.Core.LogicalCenter.X * 16f, arena.Layout.Core.BaseY * 16f);
        var field = FirstSeveranceContainmentBounds.FromGround(ground.X, ground.Y);
        foreach (var member in roster.Members)
        {
            Player player = Main.player[member.ServerWhoAmI];
            if (gather)
            {
                Vector2 destination = ground + new Vector2((member.ParticipantId.Value - (roster.Count - 1) * .5f) * 96 - player.width * .5f, -160 - player.height);
                if (Main.netMode == NetmodeID.Server) RemoteClient.CheckSection(player.whoAmI, destination, 1);
                player.Teleport(destination, 1);
                player.velocity = Vector2.Zero;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.TeleportEntity, number: 0, number2: player.whoAmI,
                        number3: destination.X, number4: destination.Y, number5: 1);
            }
            player.GetModPlayer<FirstSeveranceContainmentPlayer>().Refresh(fightId, field, member.ConnectionEpoch);
        }
    }

    private bool TryValidateIntent(
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out FirstSeveranceRosterMember member,
        out string failureCode)
    {
        if (isCleaned || preparation is null || !isAttached || preparation.IsClosed)
        {
            member = default;
            failureCode = "first_severance.preparation_not_available";
            return false;
        }

        if (pendingIntents.Count >= MaximumPendingIntents)
        {
            member = default;
            failureCode = "first_severance.preparation_intent_queue_full";
            return false;
        }

        if (!roster.TryResolveCurrentBinding(senderWhoAmI, connectionEpoch, out member))
        {
            failureCode = "first_severance.preparation_sender_not_bound";
            return false;
        }

        int participantIndex = member.ParticipantId.Value;
        if (requestNonce == 0 || requestNonce <= lastQueuedNonces[participantIndex])
        {
            failureCode = "first_severance.preparation_stale_nonce";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    private FirstSeveranceConnectionObservation[] ObserveConnections()
    {
        var observations = new FirstSeveranceConnectionObservation[roster.Count];
        for (int index = 0; index < roster.Count; index++)
        {
            FirstSeveranceRosterMember member = roster.Members[index];
            bool connected = FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(
                member.ServerWhoAmI,
                out ulong currentEpoch);
            observations[index] = new FirstSeveranceConnectionObservation(
                member.ServerWhoAmI,
                currentEpoch,
                connected && Main.player[member.ServerWhoAmI].active);
        }

        return observations;
    }

    private static int CompareIntents(
        FirstSeveranceQueuedPreparationIntent left,
        FirstSeveranceQueuedPreparationIntent right)
    {
        int kindOrder = left.Kind.CompareTo(right.Kind);
        if (kindOrder != 0)
        {
            return kindOrder;
        }

        int participantOrder = left.ParticipantId.Value.CompareTo(right.ParticipantId.Value);
        return participantOrder != 0
            ? participantOrder
            : left.RequestNonce.CompareTo(right.RequestNonce);
    }
}
