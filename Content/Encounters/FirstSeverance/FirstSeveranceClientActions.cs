#nullable enable

using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Networking.Replication;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeveranceClientActions
{
    private static uint nextRequestNonce;

    internal static void InteractWithCore(int tileX, int tileY)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            return;
        }

        EncounterSnapshot snapshot = CurrentSnapshot();
        if (snapshot.Lifecycle == EncounterLifecycle.Preparing
            && string.Equals(
                snapshot.DefinitionKey,
                FirstSeveranceDefinition.EncounterKey,
                System.StringComparison.Ordinal))
        {
            if (!TryGetPreparation(snapshot, out FirstSeverancePreparationProjection? preparation)
                || preparation is null
                || !preparation.TryGetMemberByServerSlot(
                    Main.myPlayer,
                    out FirstSeverancePreparationMemberSnapshot member))
            {
                Main.NewText("[Convergence] Preparation snapshot is not ready yet.", 235, 180, 90);
                RequestSnapshot();
                return;
            }

            if (ModContent.GetInstance<FirstSeveranceClientStateSystem>().EstimatedAuthorityTick < preparation.ReadyOpensTick)
                return;
            RequestSetReady(snapshot, !member.IsReady);
            return;
        }

        if (snapshot.Lifecycle is EncounterLifecycle.Validating
            or EncounterLifecycle.Active
            or EncounterLifecycle.Resolving)
        {
            Main.NewText("[Convergence] The current encounter request is already in progress.", 235, 180, 90);
            return;
        }

        RequestActivate(new TilePoint(tileX, tileY));
    }

    internal static void RequestCancel()
    {
        if (Main.netMode == NetmodeID.Server)
        {
            return;
        }

        EncounterSnapshot snapshot = CurrentSnapshot();
        if (snapshot.Lifecycle is not (EncounterLifecycle.Preparing or EncounterLifecycle.Active)
            || snapshot.FightId.IsNone)
        {
            Main.NewText("[Convergence] There is no active Raid to cancel.", 235, 180, 90);
            return;
        }

        uint nonce = NextNonce();
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (!FirstSeveranceServerCommands.TryCancel(
                    snapshot.EncounterSequence,
                    snapshot.FightId,
                    Main.myPlayer,
                    nonce,
                    out string failureCode))
            {
                ShowRejected(failureCode);
            }

            return;
        }

        ModPacket packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        FirstSeverancePacketCodec.WriteCancelRequest(
            packet,
            RequestHeader(EncounterPacketType.RequestCancel, snapshot),
            nonce);
        packet.Send();
    }

    internal static void RequestSnapshot()
        => Convergence.Common.Networking.EncounterSnapshotTransport.RequestSnapshot();

    internal static void RequestPrototypeDown()
    {
        if (!TryGetActiveCombatSnapshot(out EncounterSnapshot snapshot))
        {
            Main.NewText("[Convergence] First Severance combat is not active.", 235, 180, 90);
            return;
        }

        uint nonce = NextNonce();
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (!FirstSeveranceServerCommands.TryPrototypeDown(
                    snapshot.EncounterSequence,
                    snapshot.FightId,
                    Main.myPlayer,
                    nonce,
                    out string failureCode))
            {
                ShowRejected(failureCode);
            }

            return;
        }

        ModPacket packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        FirstSeverancePacketCodec.WritePrototypeDownRequest(
            packet,
            RequestHeader(EncounterPacketType.RequestPrototypeDown, snapshot),
            nonce);
        packet.Send();
    }

    internal static void RequestReviveNearest()
    {
        if (!TryGetActiveCombatSnapshot(out EncounterSnapshot snapshot))
        {
            Main.NewText("[Convergence] First Severance combat is not active.", 235, 180, 90);
            return;
        }

        uint nonce = NextNonce();
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (!FirstSeveranceServerCommands.TryReviveNearest(
                    snapshot.EncounterSequence,
                    snapshot.FightId,
                    Main.myPlayer,
                    nonce,
                    out string failureCode))
            {
                ShowRejected(failureCode);
            }

            return;
        }

        ModPacket packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        FirstSeverancePacketCodec.WriteReviveNearestRequest(
            packet,
            RequestHeader(EncounterPacketType.RequestReviveNearest, snapshot),
            nonce);
        // Publish this local player's selected slot before the custom request.
        // The server still checks the resulting inventory/held-item state itself.
        NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer,
            number2: Main.LocalPlayer.selectedItem);
        NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
        packet.Send();
    }

    internal static void Reset()
    {
        nextRequestNonce = 0;
    }

    private static void RequestActivate(in TilePoint requestedAnchor)
    {
        uint nonce = NextNonce();
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (!FirstSeveranceServerCommands.TryActivate(
                    Main.myPlayer,
                    requestedAnchor,
                    nonce,
                    out _,
                    out string failureCode))
            {
                ShowRejected(failureCode);
            }

            return;
        }

        ModPacket packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        FirstSeverancePacketCodec.WriteActivateRequest(
            packet,
            new EncounterPacketHeader(
                EncounterProtocol.CurrentVersion,
                EncounterPacketType.RequestActivate,
                EncounterSequence: 0,
                FightId: FightId.None,
                Revision: 0),
            requestedAnchor,
            nonce);
        packet.Send();
    }

    private static void RequestSetReady(
        in EncounterSnapshot snapshot,
        bool isReady)
    {
        uint nonce = NextNonce();
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (!FirstSeveranceServerCommands.TrySetReady(
                    snapshot.EncounterSequence,
                    snapshot.FightId,
                    Main.myPlayer,
                    isReady,
                    nonce,
                    out string failureCode))
            {
                ShowRejected(failureCode);
            }

            return;
        }

        ModPacket packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        FirstSeverancePacketCodec.WriteReadyRequest(
            packet,
            RequestHeader(EncounterPacketType.RequestSetReady, snapshot),
            isReady,
            nonce);
        packet.Send();
    }

    private static EncounterSnapshot CurrentSnapshot()
    {
        return Main.netMode == NetmodeID.SinglePlayer
            ? ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot
            : ModContent.GetInstance<EncounterReplicaSystem>().Snapshot;
    }

    private static bool TryGetActiveCombatSnapshot(out EncounterSnapshot snapshot)
    {
        snapshot = CurrentSnapshot();
        return Main.netMode != NetmodeID.Server
            && snapshot.Lifecycle == EncounterLifecycle.Active
            && !snapshot.FightId.IsNone
            && string.Equals(
                snapshot.DefinitionKey,
                FirstSeveranceDefinition.EncounterKey,
                System.StringComparison.Ordinal);
    }

    private static bool TryGetPreparation(
        in EncounterSnapshot snapshot,
        out FirstSeverancePreparationProjection? preparation)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            return FirstSeverancePreparationAuthority.TryCreateProjection(
                snapshot.EncounterSequence,
                snapshot.FightId,
                out preparation);
        }

        preparation = ModContent.GetInstance<FirstSeveranceClientStateSystem>().Preparation;
        return preparation is not null
            && preparation.EncounterSequence == snapshot.EncounterSequence
            && preparation.FightId == snapshot.FightId;
    }

    private static EncounterPacketHeader RequestHeader(
        EncounterPacketType packetType,
        in EncounterSnapshot snapshot)
    {
        return new EncounterPacketHeader(
            EncounterProtocol.CurrentVersion,
            packetType,
            snapshot.EncounterSequence,
            snapshot.FightId,
            snapshot.Revision);
    }

    private static uint NextNonce()
    {
        nextRequestNonce = nextRequestNonce == uint.MaxValue
            ? 1
            : nextRequestNonce + 1;
        return nextRequestNonce;
    }

    internal static void ShowRejected(string failureCode)
    {
        string? preparationReason = failureCode switch
        {
            "first_severance.roster_player_not_alive" => "PlayerUnavailable",
            FirstSeveranceArenaIssueCodes.SelectionRequired => "TooManyPlayers",
            "first_severance.preparation_field_deploying" => "Deploying",
            _ => null,
        };
        if (preparationReason is not null)
        {
            Main.NewText("[Convergence] " + Language.GetTextValue(
                "Mods.Convergence.UI.PreparationDeployment." + preparationReason), 235, 170, 110);
            return;
        }
        string? reviveReason = failureCode switch
        {
            "first_severance.revive_sender_not_alive" or "revive.reviver_not_alive" => "ReviveNotAlive",
            "first_severance.revive_kit_not_selected" => "ReviveSelectKit",
            "first_severance.revive_no_downed_ally_in_range" => "ReviveNoTarget",
            "first_severance.revive_target_recovery_locked" or "revive.target_recovery_locked" => "ReviveLocked",
            "revive.target_already_reserved" or "revive.target_not_revivable" => "ReviveTargetUnavailable",
            _ => null,
        };
        if (reviveReason is not null)
        {
            Main.NewText("[Convergence] " + Language.GetTextValue(
                "Mods.Convergence.UI.FirstSeverance." + reviveReason), 235, 170, 110);
            return;
        }
        string message = Language.GetTextValue(
            "Mods.Convergence.UI.FirstSeverance.RequestRejected",
            failureCode);
        Main.NewText($"[Convergence] {message}", 235, 110, 110);
    }
}
