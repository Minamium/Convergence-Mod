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

    internal static void RequestSummon(Player player)
    {
        uint request = ++nonce;
        if (request == 0) request = ++nonce;
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Start(player.whoAmI, request);
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
        return ModContent.GetInstance<EncounterCoordinatorSystem>().CommandSink?.TryStart(
            new(sender, GhostSamuraiDefinition.EncounterKey, new TilePoint((int)p.Center.X / 16, (int)p.Center.Y / 16), request),
            out _, out _) == true;
    }

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
            if (Main.GameUpdateCount < connection.NextRequest || request <= connection.LastNonce) return false;
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
            if (!accepted) Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.Convergence.GhostSamurai.SummonRejected"));
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
            ModContent.GetInstance<EncounterReplicaSystem>().ApplyFullSnapshot(new(header.EncounterSequence, header.FightId,
                GhostSamuraiDefinition.EncounterKey, (EncounterLifecycle)lifecycle, header.Revision, tick, entered, active, termination));
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
    public void ApplyIdleSnapshot(in EncounterSnapshot snapshot) { }
    public override void ClearWorld() { nonce = 0; }
    public override void OnWorldUnload() => ClearWorld();
}

// Request history belongs to the current player connection, never a reused slot.
internal sealed class GhostSamuraiSummonPlayer : ModPlayer
{
    internal uint LastNonce;
    internal ulong NextRequest;
    public override void Initialize() { LastNonce = 0; NextRequest = 0; }
}
