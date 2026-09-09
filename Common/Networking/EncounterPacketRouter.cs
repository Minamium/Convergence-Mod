using System;
using System.Collections.Generic;
using System.IO;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Encounters.Runtime;
using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace Convergence.Common.Networking;

internal static class EncounterPacketRouter
{
    private const ulong RejectionWindowTicks = 10 * 60;
    private const int RejectionLogLimit = 4;
    private static readonly Dictionary<int, RejectionWindow> RejectionWindows = new();
    internal static EncounterPacketRoutes Routes { get; } = new();

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

        try
        {
            // Snapshot repair addresses the active session, not whichever feature requested it.
            if (header.PacketType == EncounterPacketType.RequestSnapshot)
            {
                if (!EncounterSnapshotTransport.HandleRequest(whoAmI, out failureCode)) WarnRejected(whoAmI, failureCode);
                return;
            }
            if (!EncounterRouteCodec.TryRead(reader, out string key))
            {
                WarnRejected(whoAmI, "packet.route_invalid");
                return;
            }
            if (key.Length == 0 && header.PacketType == EncounterPacketType.Snapshot)
            {
                if (!EncounterSnapshotTransport.ApplyIdle(reader, header, out failureCode)) WarnRejected(whoAmI, failureCode);
                return;
            }
            if (!Routes.TryGet(key, out IEncounterPacketHandler handler))
            {
                WarnRejected(whoAmI, "packet.route_unknown");
                return;
            }
            if (Main.netMode == NetmodeID.Server && header.PacketType != EncounterPacketType.RequestActivate
                && !EncounterPacketRoutes.MatchesSession(key, header, ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot))
            {
                WarnRejected(whoAmI, "packet.route_session_mismatch");
                return;
            }
            if (!handler.TryHandle(reader, whoAmI, header, out failureCode))
            {
                WarnRejected(whoAmI, failureCode);
            }
        }
        catch (EndOfStreamException)
        {
            WarnRejected(whoAmI, $"packet.truncated_payload:{header.PacketType}");
        }
        catch (IOException)
        {
            WarnRejected(whoAmI, $"packet.payload_io_failure:{header.PacketType}");
        }
        catch (Exception exception)
        {
            global::Convergence.ConvergenceMod.Instance.Logger.Error(
                $"Encounter packet handler failed safely for type={header.PacketType} "
                + $"player={whoAmI}.",
                exception);
            WarnRejected(whoAmI, $"packet.handler_failure:{header.PacketType}");
        }
    }

    internal static void Reset()
    {
        RejectionWindows.Clear();
        Routes.Clear();
    }

    private static bool IsDirectionAllowed(EncounterPacketType packetType)
    {
        bool isClientRequest = packetType is EncounterPacketType.RequestActivate
            or EncounterPacketType.RequestSetReady
            or EncounterPacketType.RequestCancel
            or EncounterPacketType.RequestSnapshot
            or EncounterPacketType.RequestPrototypeDown
            or EncounterPacketType.RequestReviveNearest;
        bool isServerEvent = packetType is EncounterPacketType.Snapshot
            or EncounterPacketType.StateChanged
            or EncounterPacketType.ParticipantChanged
            or EncounterPacketType.ValidationResult
            or EncounterPacketType.EncounterEnded
            or EncounterPacketType.RaidHit;

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
