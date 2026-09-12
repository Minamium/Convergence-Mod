using System;
using System.IO;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Networking.Replication;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.GhostSamurai;

internal sealed class GhostSamuraiPackets : ModSystem, IEncounterPacketHandler
{
    private static uint nonce;
    private static ulong nextBlockedLog;
    private ulong observedSequence;

    internal static void Log(string details)
        => global::Convergence.ConvergenceMod.Instance.Logger.Info($"GhostSamurai {details}");

    internal static void LogBlocked(Player player, in EncounterSnapshot state, string reason)
    {
        if (player.whoAmI != Main.myPlayer || Main.GameUpdateCount < nextBlockedLog) return;
        nextBlockedLog = Main.GameUpdateCount + 120;
        Log($"event=SummonBlocked side={Main.netMode} slot={player.whoAmI} reason={reason} seq={state.EncounterSequence} fight={state.FightId.Value} lifecycle={state.Lifecycle} revision={state.Revision}");
    }

    internal static void RequestSummon(Player player)
    {
        uint request = ++nonce;
        if (request == 0) request = ++nonce;
        Log($"event=SummonRequested side={Main.netMode} slot={player.whoAmI} nonce={request}");
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (!Start(player.whoAmI, request)) ShowRejected();
            return;
        }
        var packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        EncounterRouteCodec.WriteHeader(packet, new(EncounterProtocol.CurrentVersion,
            EncounterPacketType.RequestActivate, 0, FightId.None, 0), GhostSamuraiDefinition.EncounterKey);
        packet.Write(request);
        packet.Send();
    }

    private static bool Start(int sender, uint request)
    {
        Player p = Main.player[sender];
        var coordinator = ModContent.GetInstance<EncounterCoordinatorSystem>();
        var snapshot = coordinator.Snapshot;
        string failure = "encounter.coordinator_not_ready";
        bool accepted = coordinator.CommandSink is { } sink && sink.TryStart(
            new(sender, GhostSamuraiDefinition.EncounterKey, new TilePoint((int)p.Center.X / 16, (int)p.Center.Y / 16), request),
            out snapshot, out failure);
        Log($"event={(accepted ? "SummonAccepted" : "SummonRejected")} slot={sender} nonce={request} reason={(accepted ? "none" : failure)} seq={snapshot.EncounterSequence} fight={snapshot.FightId.Value} lifecycle={snapshot.Lifecycle}");
        return accepted;
    }

    private static void ShowRejected()
        => Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.Convergence.GhostSamurai.SummonRejected"));

    public bool TryHandle(BinaryReader reader, int sender, in EncounterPacketHeader header, out string failureCode)
    {
        failureCode = "ghost_samurai.packet_invalid";
        if (header.PacketType == EncounterPacketType.RequestActivate)
        {
            uint request = reader.ReadUInt32(); // Fully consume bounded input before checking sender/state.
            if (Main.netMode != NetmodeID.Server || request == 0 || sender < 0 || sender >= Main.maxPlayers
                || !Main.player[sender].active || Main.player[sender].dead
                || !header.FightId.IsNone || header.EncounterSequence != 0 || header.Revision != 0) return false;
            var connection = Main.player[sender].GetModPlayer<GhostSamuraiSummonPlayer>();
            if (Main.GameUpdateCount < connection.NextRequest)
            { failureCode = "ghost_samurai.summon_rate_limited"; return false; }
            if (request <= connection.LastNonce)
            { failureCode = "ghost_samurai.summon_stale_nonce"; return false; }
            connection.NextRequest = Main.GameUpdateCount + 60;
            connection.LastNonce = request;
            bool accepted = Start(sender, request);
            var response = global::Convergence.ConvergenceMod.Instance.GetPacket();
            EncounterRouteCodec.WriteHeader(response, new(EncounterProtocol.CurrentVersion,
                EncounterPacketType.ValidationResult, 0, FightId.None, 0), GhostSamuraiDefinition.EncounterKey);
            response.Write(accepted);
            response.Send(sender);
        }
        else if (header.PacketType == EncounterPacketType.ValidationResult)
        {
            bool accepted = reader.ReadBoolean();
            if (Main.netMode != NetmodeID.MultiplayerClient) return false;
            Log($"event=SummonResponse accepted={accepted}");
            if (!accepted) ShowRejected();
        }
        else if (header.PacketType == EncounterPacketType.Snapshot)
        {
            byte lifecycle = reader.ReadByte();
            ulong tick = reader.ReadUInt64(), entered = reader.ReadUInt64(), active = reader.ReadUInt64();
            byte reason = reader.ReadByte();
            if (Main.netMode != NetmodeID.MultiplayerClient || header.FightId.IsNone || header.EncounterSequence == 0
                || !Enum.IsDefined((EncounterLifecycle)lifecycle) || !Enum.IsDefined((EncounterEndReason)reason)
                || entered > tick || ((EncounterLifecycle)lifecycle == EncounterLifecycle.Cleanup) != (reason != 0)) return false;
            var termination = reason == 0 ? EncounterTerminationDescriptor.None : GhostSamuraiTermination.End((EncounterEndReason)reason);
            var replica = ModContent.GetInstance<EncounterReplicaSystem>();
            var previous = replica.Snapshot;
            bool applied = replica.ApplyFullSnapshot(new(header.EncounterSequence, header.FightId,
                GhostSamuraiDefinition.EncounterKey, (EncounterLifecycle)lifecycle, header.Revision, tick, entered, active, termination));
            if (applied)
            {
                observedSequence = header.EncounterSequence;
                if (previous.EncounterSequence != header.EncounterSequence || previous.Lifecycle != (EncounterLifecycle)lifecycle)
                    Log($"event=ClientLifecycle seq={header.EncounterSequence} fight={header.FightId.Value} lifecycle={(EncounterLifecycle)lifecycle} revision={header.Revision} reason={(EncounterEndReason)reason}");
            }
        }
        else return false;
        failureCode = string.Empty;
        return true;
    }

    public void PublishSnapshot(in EncounterSnapshot snapshot, int toClient)
    {
        if (Main.netMode != NetmodeID.Server) return;
        var packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        EncounterRouteCodec.WriteHeader(packet, new(EncounterProtocol.CurrentVersion, EncounterPacketType.Snapshot,
            snapshot.EncounterSequence, snapshot.FightId, snapshot.Revision), GhostSamuraiDefinition.EncounterKey);
        packet.Write((byte)snapshot.Lifecycle); packet.Write(snapshot.AuthorityTick);
        packet.Write(snapshot.LifecycleEnteredTick); packet.Write(snapshot.ActiveFightTick); packet.Write((byte)snapshot.EndReason);
        packet.Send(toClient);
    }
    public void ApplyIdleSnapshot(in EncounterSnapshot snapshot)
    {
        if (observedSequence == 0 || observedSequence != snapshot.EncounterSequence) return;
        Log($"event=ClientIdle seq={snapshot.EncounterSequence} revision={snapshot.Revision}");
        observedSequence = 0;
    }
    public override void ClearWorld() { nonce = 0; nextBlockedLog = 0; observedSequence = 0; }
    public override void OnWorldUnload() => ClearWorld();
}

// Request history belongs to the current player connection, never a reused slot.
internal sealed class GhostSamuraiSummonPlayer : ModPlayer
{
    internal uint LastNonce;
    internal ulong NextRequest;
    public override void Initialize() { LastNonce = 0; NextRequest = 0; }
}
