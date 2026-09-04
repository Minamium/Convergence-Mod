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
        if (snapshot.Lifecycle != EncounterLifecycle.Preparing
            || snapshot.FightId.IsNone)
        {
            Main.NewText("[Convergence] There is no preparation to cancel.", 235, 180, 90);
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
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            return;
        }

        EncounterSnapshot snapshot = CurrentSnapshot();
        ModPacket packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        EncounterPacketCodec.WriteHeader(
            packet,
            RequestHeader(EncounterPacketType.RequestSnapshot, snapshot));
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
        string message = Language.GetTextValue(
            "Mods.Convergence.UI.FirstSeverance.ActivationRejected",
            failureCode);
        Main.NewText($"[Convergence] {message}", 235, 110, 110);
    }
}
