using System;
using System.Collections.Generic;
using System.IO;
using Convergence.Common.Networking.Protocol;
using Terraria;
using Terraria.ID;

namespace Convergence.Common.Networking;

internal static class EncounterPacketRouter
{
    private const ulong RejectionWindowTicks = 10 * 60;
    private const int RejectionLogLimit = 4;
    private static readonly Dictionary<int, RejectionWindow> RejectionWindows = new();

    public static void Handle(BinaryReader reader, int whoAmI)
    {
        if (!EncounterPacketCodec.TryReadHeader(reader, out EncounterPacketHeader header, out string failureCode))
        {
            WarnRejected(whoAmI, failureCode);
            return;
        }

        if (!Enum.IsDefined(typeof(EncounterPacketType), header.PacketType))
        {
            WarnRejected(whoAmI, "packet.unknown_type");
            return;
        }

        if (header.ProtocolVersion != EncounterProtocol.CurrentVersion)
        {
            WarnRejected(
                whoAmI,
                $"packet.protocol_mismatch:{header.ProtocolVersion}");
            return;
        }

        if (!IsDirectionAllowed(header.PacketType))
        {
            WarnRejected(
                whoAmI,
                $"packet.direction_forbidden:{header.PacketType}:{Main.netMode}");
            return;
        }

        // Typed fixed/bounded decoders are added with Milestone 1. BinaryReader wraps
        // tModLoader's shared receive buffer, so BaseStream.Length is not a safe packet bound.
        // No payload is decoded and no state can mutate until a complete DTO is validated.
        WarnRejected(whoAmI, $"packet.handler_not_implemented:{header.PacketType}");
    }

    internal static void Reset()
    {
        RejectionWindows.Clear();
    }

    private static bool IsDirectionAllowed(EncounterPacketType packetType)
    {
        bool isClientRequest = packetType is EncounterPacketType.RequestActivate
            or EncounterPacketType.RequestSetReady
            or EncounterPacketType.RequestCancel
            or EncounterPacketType.RequestSnapshot;
        bool isServerEvent = packetType is EncounterPacketType.Snapshot
            or EncounterPacketType.StateChanged
            or EncounterPacketType.ParticipantChanged
            or EncounterPacketType.ValidationResult
            or EncounterPacketType.EncounterEnded;

        return Main.netMode switch
        {
            NetmodeID.Server => isClientRequest,
            NetmodeID.MultiplayerClient => isServerEvent,
            _ => false,
        };
    }

    private static void WarnRejected(int whoAmI, string failureCode)
    {
        ulong now = Main.GameUpdateCount;
        if (!RejectionWindows.TryGetValue(whoAmI, out RejectionWindow window)
            || now < window.StartTick
            || now - window.StartTick >= RejectionWindowTicks)
        {
            window = new RejectionWindow(now, 0);
        }

        if (window.LoggedCount >= RejectionLogLimit)
        {
            RejectionWindows[whoAmI] = window;
            return;
        }

        int nextCount = window.LoggedCount + 1;
        string suffix = nextCount == RejectionLogLimit
            ? " Further rejections from this sender are suppressed for this window."
            : string.Empty;
        global::Convergence.ConvergenceMod.Instance.Logger.Warn(
            $"Rejected encounter packet from player={whoAmI}; reason={failureCode}.{suffix}");
        RejectionWindows[whoAmI] = window with { LoggedCount = nextCount };
    }

    private readonly record struct RejectionWindow(ulong StartTick, int LoggedCount);
}
