#nullable enable

using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking.Replication;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceClientStateSystem : ModSystem
{
    private FirstSeverancePreparationProjection? preparation;
    private uint lastValidationNonce;
    private bool requestedInitialSnapshot;
    private ulong displayedPreparationSequence;
    private int displayedReadyCount = -1;
    private FirstSeveranceCombatProjection? combat;
    private ulong lastAuthorityTick;
    private ulong receivedAtLocalTick;
    private string? combatEndMessage;
    private ulong showCombatEndUntilTick;
    private ulong lastExecutedDefeatSequence;
    private ulong stackDiagnosticTick;
    private bool hasStackDiagnostic;

    internal FirstSeveranceCombatProjection? Combat => combat;
    internal EncounterEndReason LastCombatEndReason { get; private set; }

    internal string? CombatEndMessage => Main.GameUpdateCount < showCombatEndUntilTick
        ? combatEndMessage : null;

    internal ulong EstimatedAuthorityTick => lastAuthorityTick
        + (Main.GameUpdateCount >= receivedAtLocalTick ? Main.GameUpdateCount - receivedAtLocalTick : 0);

    internal FirstSeverancePreparationProjection? Preparation => preparation;

    internal FirstSeveranceValidationMessage? LastValidation { get; private set; }

    internal void ApplySnapshot(
        in EncounterSnapshot snapshot,
        FirstSeverancePreparationProjection? incomingPreparation,
        FirstSeveranceCombatProjection? incomingCombat)
    {
        lastAuthorityTick = snapshot.AuthorityTick;
        receivedAtLocalTick = Main.GameUpdateCount;
        ApplyCombat(snapshot, incomingCombat);
        FirstSeverancePreparationProjection? nextPreparation =
            snapshot.Lifecycle == EncounterLifecycle.Preparing
            && incomingPreparation is not null
            && incomingPreparation.EncounterSequence == snapshot.EncounterSequence
            && incomingPreparation.FightId == snapshot.FightId
                ? incomingPreparation
                : null;
        preparation = nextPreparation;
        if (Main.netMode == NetmodeID.Server || nextPreparation is null)
        {
            if (nextPreparation is null)
            {
                displayedPreparationSequence = 0;
                displayedReadyCount = -1;
            }

            return;
        }

        int readyCount = CountReady(nextPreparation);
        if (displayedPreparationSequence == nextPreparation.EncounterSequence
            && displayedReadyCount == readyCount)
        {
            return;
        }

        bool firstPreparation = displayedPreparationSequence != nextPreparation.EncounterSequence;
        displayedPreparationSequence = nextPreparation.EncounterSequence;
        displayedReadyCount = readyCount;
        if (firstPreparation && nextPreparation.Members.Count == 1)
            Say("SoloDebugNotice");
        string message = nextPreparation.AreAllReady
            ? Language.GetTextValue(
                "Mods.Convergence.UI.FirstSeverance.AllReady",
                readyCount,
                nextPreparation.Members.Count)
            : Language.GetTextValue(
                "Mods.Convergence.UI.FirstSeverance.PreparationStatus",
                readyCount,
                nextPreparation.Members.Count);
        Main.NewText($"[Convergence] {message}", 86, 210, 229);
    }

    private void ApplyCombat(in EncounterSnapshot snapshot, FirstSeveranceCombatProjection? incoming)
    {
        if (Main.netMode == NetmodeID.Server)
            return;
        FirstSeveranceCombatProjection? previous = combat;
        combat = snapshot.Lifecycle == EncounterLifecycle.Active
            && incoming is not null && incoming.FightId == snapshot.FightId
            && incoming.EncounterSequence == snapshot.EncounterSequence ? incoming : null;

        if (previous is not null && (combat is null || combat.FightId != previous.FightId))
        {
            LastCombatEndReason = snapshot.FightId == previous.FightId
                && snapshot.EncounterSequence == previous.EncounterSequence
                ? snapshot.Termination.EndReason : EncounterEndReason.None;
            ClearCombatPlayers(previous);
            // Only the accepted authority terminal can request this. The owning
            // client executes Terraria's normal death (inventory/respawn and vanilla
            // death synchronization), never a cosmetic dead flag on remote players.
            if (previous.EncounterSequence > lastExecutedDefeatSequence
                && previous.TryGetParticipantByServerSlot(Main.myPlayer, out var local)
                && FirstSeveranceCombatRules.ShouldExecuteDefeat(previous.EncounterSequence, previous.FightId,
                    snapshot.EncounterSequence, snapshot.FightId, snapshot.Termination.EndReason, local.IsConnected))
            {
                lastExecutedDefeatSequence = previous.EncounterSequence;
                Player player = Main.LocalPlayer;
                if (player.active && !player.dead)
                {
                    player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                        "Mods.Convergence.UI.FirstSeverance.DefeatDeathReason", player.name)), 100_000, 0);
                    Mod.Logger.Info($"FirstSeverance event=DefeatDeathApplied seq={snapshot.EncounterSequence} dead={player.dead}");
                    if (!player.dead)
                        Mod.Logger.Warn("FirstSeverance death was cancelled by an external PreKill hook; no hook bypass was attempted.");
                }
            }
            FirstSeveranceTerminationContract contract = FirstSeveranceTerminationContract.Instance;
            string cause = contract.IsValid(snapshot.Termination)
                ? contract.GetCause(snapshot.Termination).ToString() : "Unknown";
            string explanation = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.End" + cause);
            combatEndMessage = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.CombatEnded",
                snapshot.Termination.EndReason.ToString(), explanation);
            showCombatEndUntilTick = Main.GameUpdateCount + 600;
            Say("CombatEnded", snapshot.Termination.EndReason.ToString(), explanation);
            Mod.Logger.Info($"FirstSeverance client_end seq={snapshot.EncounterSequence} fight={snapshot.FightId} reason={snapshot.Termination.EndReason} cause={cause}");
            previous = null;
        }
        if (combat is null)
            return;

        combatEndMessage = null;
        showCombatEndUntilTick = 0;
        LastCombatEndReason = EncounterEndReason.None;

        foreach (FirstSeveranceCombatParticipantProjection participant in combat.Participants)
        {
            Player player = Main.player[participant.ServerWhoAmI];
            if (player.active)
            {
                player.GetModPlayer<FirstSeveranceRaidPlayer>().ApplyProjection(
                    combat.FightId, participant, snapshot.AuthorityTick);
                if (participant.IsConnected && Main.netMode == NetmodeID.MultiplayerClient)
                    player.GetModPlayer<FirstSeveranceContainmentPlayer>().Refresh(combat.FightId,
                        FirstSeveranceContainmentBounds.FromGround(combat.CoreX, combat.CoreY));
            }

            FirstSeveranceCombatParticipantProjection before = default;
            bool hadPrevious = previous is not null
                && previous.TryGetParticipantByServerSlot(participant.ServerWhoAmI, out before);
            if (participant.DebugAssistProtected && (!hadPrevious || !before.DebugAssistProtected))
                Main.NewText("[Convergence DEV] Auxiliary protection active: " + player.name, 190, 170, 110);
            if (participant.CombatState == RaidParticipantCombatState.Downed
                && (!hadPrevious || before.CombatState != RaidParticipantCombatState.Downed))
                Say("ParticipantDowned", player.name);
            else if (hadPrevious && participant.CombatState == RaidParticipantCombatState.Alive
                && participant.ReviveLockoutUntilTick > before.ReviveLockoutUntilTick)
                Say("ParticipantRevived", player.name);
        }

        if (previous is null || previous.Substate != combat.Substate
            || previous.ZeroBasedLoopIndex != combat.ZeroBasedLoopIndex || previous.ActionStartedTick != combat.ActionStartedTick)
        {
            string target = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.FixedStackTarget");
            int seconds = combat.ResolveTick > snapshot.AuthorityTick
                ? (int)((combat.ResolveTick - snapshot.AuthorityTick + 59) / 60) : 0;
            string phaseKey = combat.Substate == FirstSeveranceSubstate.PhaseTransition
                ? combat.BossPhase == FirstSeveranceBossPhase.Distant ? "PhaseRemoteTransition"
                    : combat.BossPhase == FirstSeveranceBossPhase.Final ? "PhaseFinalTransition" : "PhasePhaseTransition"
                : "Phase" + combat.Substate;
            Say(phaseKey, combat.Participants.Count, target, seconds,
                combat.Participants.Count);
        }
        if (combat.MechanicRevision > 0 && previous?.MechanicRevision != combat.MechanicRevision)
            Say("Result" + combat.LastMechanicResult);
    }

    private static void Say(string key, params object[] args)
    {
        Main.NewText("[Convergence] " + Language.GetTextValue(
            "Mods.Convergence.UI.FirstSeverance." + key, args), 86, 210, 229);
    }

    private static void ClearCombatPlayers(FirstSeveranceCombatProjection? previous)
    {
        if (previous is null)
            return;
        foreach (FirstSeveranceCombatParticipantProjection participant in previous.Participants)
            Main.player[participant.ServerWhoAmI].GetModPlayer<FirstSeveranceRaidPlayer>().ClearRaidState();
    }

    internal void ApplyValidation(in FirstSeveranceValidationMessage validation)
    {
        if (validation.RequestNonce <= lastValidationNonce)
        {
            return;
        }

        lastValidationNonce = validation.RequestNonce;
        LastValidation = validation;
        if (!validation.IsAccepted && Main.netMode != NetmodeID.Server)
        {
            FirstSeveranceClientActions.ShowRejected(validation.FailureCode);
        }
    }

    public override void OnWorldLoad()
    {
        ResetState();
    }

    public override void PostUpdatePlayers()
    {
        // This hook runs on MP clients (PostUpdateWorld does not). Publish only
        // our own final Player pose, after every ModPlayer has finished updating.
        if (Main.netMode != NetmodeID.MultiplayerClient || Main.gameMenu
            || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers) return;
        Main.LocalPlayer.GetModPlayer<FirstSeveranceContainmentPlayer>().SynchronizeOwnerMovement();
        if (combat is null || combat.Substate != FirstSeveranceSubstate.Stack
            || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) || !local.IsConnected)
        {
            hasStackDiagnostic = false;
            return;
        }
        ulong tick = Main.GameUpdateCount;
        if (hasStackDiagnostic && tick >= stackDiagnosticTick && tick - stackDiagnosticTick < 30) return;
        hasStackDiagnostic = true;
        stackDiagnosticTick = tick;
        var center = new Vector2(combat.StackX, combat.StackY);
        foreach (var member in combat.Participants)
        {
            Player observed = Main.player[member.ServerWhoAmI];
            Mod.Logger.Info(FormattableString.Invariant($"FirstSeverance event=StackClientSample seq={combat.EncounterSequence} fight={combat.FightId} authority_tick={EstimatedAuthorityTick} snapshot_tick={lastAuthorityTick} resolve_tick={combat.ResolveTick} observer={Main.myPlayer} slot={member.ServerWhoAmI} local={member.ServerWhoAmI == Main.myPlayer} state={member.CombatState} active={observed.active} x={observed.Center.X:F1} y={observed.Center.Y:F1} vx={observed.velocity.X:F2} vy={observed.velocity.Y:F2} target_x={center.X:F1} target_y={center.Y:F1} distance={Vector2.Distance(observed.Center, center):F1}"));
        }
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            EncounterCoordinatorSystem authority =
                ModContent.GetInstance<EncounterCoordinatorSystem>();
            FirstSeverancePreparationAuthority.TryCreateProjection(
                authority.Snapshot.EncounterSequence,
                authority.Snapshot.FightId,
                out FirstSeverancePreparationProjection? projection);
            FirstSeveranceCombatAuthority.TryCreateProjection(
                authority.Snapshot.EncounterSequence,
                authority.Snapshot.FightId,
                out FirstSeveranceCombatProjection? combatProjection);
            ApplySnapshot(authority.Snapshot, projection, combatProjection);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient
            && !requestedInitialSnapshot
            && Main.myPlayer >= 0
            && Main.myPlayer < Main.maxPlayers
            && Main.player[Main.myPlayer].active)
        {
            requestedInitialSnapshot = true;
            FirstSeveranceClientActions.RequestSnapshot();
        }
    }

    public override void OnWorldUnload()
    {
        ResetState();
    }

    public override void Unload()
    {
        ResetState();
    }

    private void ResetState()
    {
        ClearCombatPlayers(combat);
        combat = null;
        combatEndMessage = null;
        showCombatEndUntilTick = 0;
        lastAuthorityTick = 0;
        lastExecutedDefeatSequence = 0;
        hasStackDiagnostic = false;
        stackDiagnosticTick = 0;
        LastCombatEndReason = EncounterEndReason.None;
        receivedAtLocalTick = 0;
        preparation = null;
        lastValidationNonce = 0;
        LastValidation = null;
        requestedInitialSnapshot = false;
        displayedPreparationSequence = 0;
        displayedReadyCount = -1;
        FirstSeveranceClientActions.Reset();
    }

    private static int CountReady(FirstSeverancePreparationProjection projection)
    {
        int count = 0;
        for (int index = 0; index < projection.Members.Count; index++)
        {
            if (projection.Members[index].IsReady)
            {
                count++;
            }
        }

        return count;
    }
}
