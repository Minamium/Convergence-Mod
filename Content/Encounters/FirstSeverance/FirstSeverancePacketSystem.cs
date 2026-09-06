#nullable enable

using System.Collections.Generic;
using System.IO;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Networking;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Networking.Replication;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeverancePacketSystem : ModSystem, IEncounterPacketHandler
{
    private const ulong FastRequestWindowTicks = 60;
    private const ulong ActivationWindowTicks = 10 * 60;
    private static readonly Dictionary<RequestRateKey, RequestRateWindow> RequestWindows = new();

    public override void Load()
    {
        EncounterPacketRouter.Register(EncounterPacketType.RequestActivate, this);
        EncounterPacketRouter.Register(EncounterPacketType.RequestSetReady, this);
        EncounterPacketRouter.Register(EncounterPacketType.RequestCancel, this);
        EncounterPacketRouter.Register(EncounterPacketType.RequestSnapshot, this);
        EncounterPacketRouter.Register(EncounterPacketType.RequestPrototypeDown, this);
        EncounterPacketRouter.Register(EncounterPacketType.RequestReviveNearest, this);
        EncounterPacketRouter.Register(EncounterPacketType.Snapshot, this);
        EncounterPacketRouter.Register(EncounterPacketType.ValidationResult, this);
    }

    public bool TryHandle(
        BinaryReader reader,
        int whoAmI,
        in EncounterPacketHeader header,
        out string failureCode)
    {
        return header.PacketType switch
        {
            EncounterPacketType.RequestActivate => HandleActivate(
                reader,
                whoAmI,
                header,
                out failureCode),
            EncounterPacketType.RequestSetReady => HandleReady(
                reader,
                whoAmI,
                header,
                out failureCode),
            EncounterPacketType.RequestCancel => HandleCancel(
                reader,
                whoAmI,
                header,
                out failureCode),
            EncounterPacketType.RequestSnapshot => HandleSnapshotRequest(
                whoAmI,
                out failureCode),
            EncounterPacketType.RequestPrototypeDown => HandlePrototypeDown(
                reader,
                whoAmI,
                header,
                out failureCode),
            EncounterPacketType.RequestReviveNearest => HandleReviveNearest(
                reader,
                whoAmI,
                header,
                out failureCode),
            EncounterPacketType.Snapshot => HandleSnapshot(
                reader,
                header,
                out failureCode),
            EncounterPacketType.ValidationResult => HandleValidation(
                reader,
                out failureCode),
            _ => Reject("first_severance.packet_type_unhandled", out failureCode),
        };
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            return;
        }

        EncounterCoordinatorSystem authority =
            ModContent.GetInstance<EncounterCoordinatorSystem>();
        while (authority.TryTakePublishedSnapshot(out EncounterSnapshot snapshot))
        {
            SendSnapshot(snapshot, toClient: -1);
        }
    }

    public override void ClearWorld()
    {
        RequestWindows.Clear();
    }

    public override void OnWorldUnload()
    {
        RequestWindows.Clear();
    }

    public override void Unload()
    {
        RequestWindows.Clear();
    }

    private static bool HandleActivate(
        BinaryReader reader,
        int whoAmI,
        in EncounterPacketHeader header,
        out string failureCode)
    {
        // Even a rate-limited/stale request must consume its bounded payload.
        // tML gives us a shared receive stream, not a stream we may drain to EOF.
        if (!FirstSeverancePacketCodec.TryReadActivateRequest(
                reader, out TilePoint requestedAnchor, out uint requestNonce, out failureCode))
            return false;

        if (!IsCurrentPlayer(whoAmI)
            || header.EncounterSequence != 0
            || !header.FightId.IsNone
            || header.Revision != 0)
        {
            return Reject("first_severance.activate_header_invalid", out failureCode);
        }

        if (!TryConsumeRate(whoAmI, header.PacketType, ActivationWindowTicks, 3))
        {
            return Reject("first_severance.activate_rate_limited", out failureCode);
        }

        bool accepted = FirstSeveranceServerCommands.TryActivate(
            whoAmI,
            requestedAnchor,
            requestNonce,
            out _,
            out string startFailureCode);
        SendValidation(whoAmI, requestNonce, accepted, startFailureCode);
        failureCode = string.Empty;
        return true;
    }

    private static bool HandleReady(
        BinaryReader reader,
        int whoAmI,
        in EncounterPacketHeader header,
        out string failureCode)
    {
        if (!FirstSeverancePacketCodec.TryReadReadyRequest(
                reader, out bool isReady, out uint requestNonce, out failureCode))
            return false;

        if (!IsLiveHeader(header) || !IsCurrentPlayer(whoAmI))
        {
            return Reject("first_severance.ready_header_invalid", out failureCode);
        }

        if (!TryConsumeRate(whoAmI, header.PacketType, FastRequestWindowTicks, 12))
        {
            return Reject("first_severance.ready_rate_limited", out failureCode);
        }

        bool accepted = FirstSeveranceServerCommands.TrySetReady(
            header.EncounterSequence,
            header.FightId,
            whoAmI,
            isReady,
            requestNonce,
            out string readyFailureCode);
        SendValidation(whoAmI, requestNonce, accepted, readyFailureCode);
        failureCode = string.Empty;
        return true;
    }

    private static bool HandleCancel(
        BinaryReader reader,
        int whoAmI,
        in EncounterPacketHeader header,
        out string failureCode)
    {
        if (!FirstSeverancePacketCodec.TryReadCancelRequest(
                reader, out uint requestNonce, out failureCode))
            return false;

        if (!IsLiveHeader(header) || !IsCurrentPlayer(whoAmI))
        {
            return Reject("first_severance.cancel_header_invalid", out failureCode);
        }

        if (!TryConsumeRate(whoAmI, header.PacketType, FastRequestWindowTicks, 4))
        {
            return Reject("first_severance.cancel_rate_limited", out failureCode);
        }

        bool accepted = FirstSeveranceServerCommands.TryCancel(
            header.EncounterSequence,
            header.FightId,
            whoAmI,
            requestNonce,
            out string cancelFailureCode);
        SendValidation(whoAmI, requestNonce, accepted, cancelFailureCode);
        failureCode = string.Empty;
        return true;
    }

    private static bool HandleSnapshotRequest(int whoAmI, out string failureCode)
    {
        if (!IsCurrentPlayer(whoAmI))
        {
            return Reject("first_severance.snapshot_sender_invalid", out failureCode);
        }

        if (!TryConsumeRate(
                whoAmI,
                EncounterPacketType.RequestSnapshot,
                FastRequestWindowTicks,
                4))
        {
            return Reject("first_severance.snapshot_rate_limited", out failureCode);
        }

        SendSnapshot(
            ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot,
            whoAmI);
        failureCode = string.Empty;
        return true;
    }

    private static bool HandlePrototypeDown(
        BinaryReader reader,
        int whoAmI,
        in EncounterPacketHeader header,
        out string failureCode)
    {
        if (!FirstSeverancePacketCodec.TryReadPrototypeDownRequest(
                reader, out uint requestNonce, out failureCode))
            return false;

        if (!IsLiveHeader(header) || !IsCurrentPlayer(whoAmI))
        {
            return Reject("first_severance.prototype_down_header_invalid", out failureCode);
        }

        if (!TryConsumeRate(whoAmI, header.PacketType, FastRequestWindowTicks, 4))
        {
            return Reject("first_severance.prototype_down_rate_limited", out failureCode);
        }

        bool accepted = FirstSeveranceServerCommands.TryPrototypeDown(
            header.EncounterSequence,
            header.FightId,
            whoAmI,
            requestNonce,
            out string downFailureCode);
        SendValidation(whoAmI, requestNonce, accepted, downFailureCode);
        failureCode = string.Empty;
        return true;
    }

    private static bool HandleReviveNearest(
        BinaryReader reader,
        int whoAmI,
        in EncounterPacketHeader header,
        out string failureCode)
    {
        if (!FirstSeverancePacketCodec.TryReadReviveNearestRequest(
                reader, out uint requestNonce, out failureCode))
            return false;

        if (!IsLiveHeader(header) || !IsCurrentPlayer(whoAmI))
        {
            return Reject("first_severance.revive_header_invalid", out failureCode);
        }

        if (!TryConsumeRate(whoAmI, header.PacketType, FastRequestWindowTicks, 8))
        {
            return Reject("first_severance.revive_rate_limited", out failureCode);
        }

        bool accepted = FirstSeveranceServerCommands.TryReviveNearest(
            header.EncounterSequence,
            header.FightId,
            whoAmI,
            requestNonce,
            out string reviveFailureCode);
        // A queued instant revive receives its result after the authority tick revalidation.
        if (!accepted)
            SendValidation(whoAmI, requestNonce, false, reviveFailureCode);
        failureCode = string.Empty;
        return true;
    }

    private static bool HandleSnapshot(
        BinaryReader reader,
        in EncounterPacketHeader header,
        out string failureCode)
    {
        if (!FirstSeverancePacketCodec.TryReadSnapshot(
                reader,
                header,
                out EncounterSnapshot snapshot,
                out FirstSeverancePreparationProjection? preparation,
                out FirstSeveranceCombatProjection? combat,
                out failureCode))
        {
            return false;
        }

        EncounterReplicaSystem replica = ModContent.GetInstance<EncounterReplicaSystem>();
        if (replica.ApplyFullSnapshot(snapshot))
        {
            ModContent.GetInstance<FirstSeveranceClientStateSystem>()
                .ApplySnapshot(snapshot, preparation, combat);
        }

        failureCode = string.Empty;
        return true;
    }

    private static bool HandleValidation(BinaryReader reader, out string failureCode)
    {
        if (!FirstSeverancePacketCodec.TryReadValidation(
                reader,
                out FirstSeveranceValidationMessage validation,
                out failureCode))
        {
            return false;
        }

        ModContent.GetInstance<FirstSeveranceClientStateSystem>()
            .ApplyValidation(validation);
        failureCode = string.Empty;
        return true;
    }

    private static void SendSnapshot(in EncounterSnapshot snapshot, int toClient)
    {
        FirstSeverancePreparationAuthority.TryCreateProjection(
            snapshot.EncounterSequence,
            snapshot.FightId,
            out FirstSeverancePreparationProjection? preparation);
        FirstSeveranceCombatAuthority.TryCreateProjection(
            snapshot.EncounterSequence,
            snapshot.FightId,
            out FirstSeveranceCombatProjection? combat);
        ModPacket packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        FirstSeverancePacketCodec.WriteSnapshot(packet, snapshot, preparation, combat);
        packet.Send(toClient);
    }

    internal static void SendValidation(
        int toClient,
        uint requestNonce,
        bool isAccepted,
        string failureCode)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (!isAccepted)
                FirstSeveranceClientActions.ShowRejected(failureCode);
            return;
        }
        EncounterSnapshot snapshot =
            ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot;
        ModPacket packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        FirstSeverancePacketCodec.WriteValidation(
            packet,
            snapshot,
            new FirstSeveranceValidationMessage(
                requestNonce,
                isAccepted,
                isAccepted
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(failureCode)
                        ? "first_severance.request_rejected"
                        : failureCode));
        packet.Send(toClient);
    }

    private static bool IsCurrentPlayer(int whoAmI)
    {
        return whoAmI >= 0
            && whoAmI < Main.maxPlayers
            && Main.player[whoAmI].active;
    }

    private static bool IsLiveHeader(in EncounterPacketHeader header)
    {
        return header.EncounterSequence != 0 && !header.FightId.IsNone;
    }

    private static bool TryConsumeRate(
        int whoAmI,
        EncounterPacketType packetType,
        ulong windowTicks,
        int maximumRequests)
    {
        var key = new RequestRateKey(whoAmI, packetType);
        ulong now = Main.GameUpdateCount;
        if (!RequestWindows.TryGetValue(key, out RequestRateWindow window)
            || now < window.StartTick
            || now - window.StartTick >= windowTicks)
        {
            window = new RequestRateWindow(now, 0);
        }

        if (window.RequestCount >= maximumRequests)
        {
            RequestWindows[key] = window;
            return false;
        }

        RequestWindows[key] = window with { RequestCount = window.RequestCount + 1 };
        return true;
    }

    private static bool Reject(string code, out string failureCode)
    {
        failureCode = code;
        return false;
    }

    private readonly record struct RequestRateKey(
        int SenderWhoAmI,
        EncounterPacketType PacketType);

    private readonly record struct RequestRateWindow(
        ulong StartTick,
        int RequestCount);
}
