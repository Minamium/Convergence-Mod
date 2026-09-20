#nullable enable
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

namespace Convergence.Content.Encounters.AzureCathedral;

internal sealed class AzurePackets : ModSystem, IEncounterPacketHandler
{
    private static uint nonce;
    private static AzureState replica;
    private static short replicaActor=-1;
    private static ulong replicaAt;
    internal static EncounterSnapshot Snapshot => Main.netMode == NetmodeID.MultiplayerClient
        ? ModContent.GetInstance<EncounterReplicaSystem>().Snapshot : ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot;
    internal static AzureBoss? Boss
    {
        get
        {
            foreach (NPC n in Main.ActiveNPCs)
                if (n.ModNPC is AzureBoss boss && Snapshot.DefinitionKey == AzureDefinition.EncounterKey)
                {
                    if(Main.netMode==NetmodeID.MultiplayerClient && n.whoAmI==replicaActor && replica.Fight==Snapshot.FightId.Value)
                        boss.ApplyProjection(replica,replicaAt);
                    if(boss.State.Fight==Snapshot.FightId.Value)return boss;
                }
            return null;
        }
    }
    internal static void Log(string text) => global::Convergence.ConvergenceMod.Instance.Logger.Info("AzureCathedral " + text);
    internal static void Summon(int tileX, int tileY)
    {
        uint id = ++nonce; if (id == 0) id = ++nonce;
        if (Main.netMode == NetmodeID.SinglePlayer) { Start(Main.myPlayer, id, new(tileX, tileY)); return; }
        var packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        EncounterRouteCodec.WriteHeader(packet, new(EncounterProtocol.CurrentVersion, EncounterPacketType.RequestActivate, 0, FightId.None, 0), AzureDefinition.EncounterKey);
        packet.Write(id); packet.Write(tileX); packet.Write(tileY); packet.Send();
    }
    internal static void Ready(bool ready, bool cancel = false)
    {
        var actor = Boss; if (actor is null) return;
        var member = Array.Find(actor.State.Members, m => m.Slot == Main.myPlayer);
        if (member.Connection == Guid.Empty) return;
        if (Main.netMode == NetmodeID.SinglePlayer) { actor.Runtime?.Request(Main.myPlayer, member.Connection, ready, cancel); return; }
        var state = Snapshot;
        var packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        EncounterRouteCodec.WriteHeader(packet, new(EncounterProtocol.CurrentVersion,
            cancel ? EncounterPacketType.RequestCancel : EncounterPacketType.RequestSetReady,
            state.EncounterSequence, state.FightId, state.Revision), AzureDefinition.EncounterKey);
        packet.Write(member.Connection.ToByteArray()); packet.Write(++nonce); packet.Write(ready); packet.Send();
    }
    private static bool Start(int sender, uint request, TilePoint anchor)
    {
        Player p = Main.player[sender];
        var sink = ModContent.GetInstance<EncounterCoordinatorSystem>().CommandSink;
        string failure = "azure.coordinator_unavailable";
        bool success = sink is not null && sink.TryStart(new(sender, AzureDefinition.EncounterKey,
            anchor, request), out _, out failure);
        Log($"event=Summon accepted={success} sender={sender} reason={failure}");
        if (!success && Main.netMode == NetmodeID.SinglePlayer)
            Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.Convergence.AzureCathedral.Rejected") + " " + failure);
        return success;
    }
    public bool TryHandle(BinaryReader reader, int sender, in EncounterPacketHeader header, out string failureCode)
    {
        failureCode = "azure.packet_invalid";
        if (header.PacketType == EncounterPacketType.RequestActivate)
        {
            uint id = reader.ReadUInt32(); int x = reader.ReadInt32(), y = reader.ReadInt32();
            if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active
                || Main.player[sender].dead || id == 0 || !header.FightId.IsNone || header.EncounterSequence != 0 || header.Revision != 0) return false;
            var connection = Main.player[sender].GetModPlayer<AzureConnection>();
            if (Main.GameUpdateCount < connection.NextRequest || id <= connection.LastNonce) return false;
            connection.NextRequest = Main.GameUpdateCount + 45; connection.LastNonce = id;
            bool accepted = Start(sender, id, new TilePoint(x, y));
            var response = global::Convergence.ConvergenceMod.Instance.GetPacket();
            EncounterRouteCodec.WriteHeader(response, new(EncounterProtocol.CurrentVersion, EncounterPacketType.ValidationResult, 0, FightId.None, 0), AzureDefinition.EncounterKey);
            response.Write(accepted); response.Send(sender);
        }
        else if (header.PacketType is EncounterPacketType.RequestSetReady or EncounterPacketType.RequestCancel)
        {
            byte[] token = reader.ReadBytes(16); uint id = reader.ReadUInt32(); bool ready = AzureState.Presence(reader);
            if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active || token.Length != 16) return false;
            var connection = Main.player[sender].GetModPlayer<AzureConnection>();
            if (header.FightId != Snapshot.FightId || header.EncounterSequence != Snapshot.EncounterSequence
                || header.Revision > Snapshot.Revision || id == 0) return false;
            if (id <= connection.LastNonce || Main.GameUpdateCount < connection.NextRequest) return false;
            connection.LastNonce = id; connection.NextRequest = Main.GameUpdateCount + 6;
            if (Boss?.Runtime?.Request(sender, new Guid(token), ready, header.PacketType == EncounterPacketType.RequestCancel) != true) return false;
        }
        else if (header.PacketType == EncounterPacketType.ValidationResult)
        {
            bool accepted = AzureState.Presence(reader);
            if (Main.netMode != NetmodeID.MultiplayerClient) return false;
            if (!accepted) Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.Convergence.AzureCathedral.Rejected"));
        }
        else if (header.PacketType == EncounterPacketType.Snapshot)
        {
            var lifecycle = (EncounterLifecycle)reader.ReadByte(); ulong tick = reader.ReadUInt64(), entered = reader.ReadUInt64(), active = reader.ReadUInt64();
            var reason = (EncounterEndReason)reader.ReadByte();
            short actor=reader.ReadInt16();var projection=AzureState.ReadEnvelope(reader);
            if (Main.netMode != NetmodeID.MultiplayerClient || !Enum.IsDefined(lifecycle) || !Enum.IsDefined(reason)
                || header.FightId.IsNone || header.EncounterSequence == 0 || entered > tick || active > tick
                || (lifecycle == EncounterLifecycle.Cleanup) != (reason != EncounterEndReason.None)
                || actor is < -1 or >=200 || (projection is { } p ? p.Fight!=header.FightId.Value || actor<0 : actor!=-1)) return false;
            bool accepted=ModContent.GetInstance<EncounterReplicaSystem>().ApplyFullSnapshot(new(header.EncounterSequence, header.FightId,
                AzureDefinition.EncounterKey, lifecycle, header.Revision, tick, entered, active,
                reason == EncounterEndReason.None ? EncounterTerminationDescriptor.None : AzureTermination.End(reason)));
            if(accepted && projection is { } next && (replica.Fight!=next.Fight || next.CanReplace(replica)))
            {replica=next;replicaActor=actor;replicaAt=Main.GameUpdateCount;}
        }
        else return false;
        failureCode = string.Empty; return true;
    }
    public void PublishSnapshot(in EncounterSnapshot snapshot, int toClient)
    {
        if (Main.netMode != NetmodeID.Server) return;
        var packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        EncounterRouteCodec.WriteHeader(packet, new(EncounterProtocol.CurrentVersion, EncounterPacketType.Snapshot,
            snapshot.EncounterSequence, snapshot.FightId, snapshot.Revision), AzureDefinition.EncounterKey);
        packet.Write((byte)snapshot.Lifecycle); packet.Write(snapshot.AuthorityTick); packet.Write(snapshot.LifecycleEnteredTick);
        packet.Write(snapshot.ActiveFightTick); packet.Write((byte)snapshot.EndReason);
        // A queued terminal snapshot must never carry the next Fight's actor.
        var boss=Boss;if(boss is not null && boss.State.Fight!=snapshot.FightId.Value)boss=null;
        packet.Write((short)(boss?.NPC.whoAmI??-1));(boss?.State??default).WriteEnvelope(packet);
        packet.Send(toClient);
    }
    public void ApplyIdleSnapshot(in EncounterSnapshot snapshot) {replica=default;replicaActor=-1;replicaAt=0;}
    public override void ClearWorld() {nonce=0;replica=default;replicaActor=-1;replicaAt=0;}
}
