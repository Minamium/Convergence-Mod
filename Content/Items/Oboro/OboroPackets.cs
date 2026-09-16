using System;
using System.IO;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Networking.Replication;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Items.Oboro;

internal sealed class OboroPackets : ModSystem, IEncounterPacketHandler
{
    private const string Route = "oboro";
    private static ulong nextGeneration;
    private readonly ulong[] helloAfter = new ulong[256];
    internal static Action<OboroBurst> DisplayBurst;
    internal static ulong NextGeneration() => ++nextGeneration;
    public override void PostSetupContent() => EncounterPacketRouter.Routes.Register(Route, this);
    internal static void Request(Player player, OboroAction action, float angle)
    {
        var state = player.GetModPlayer<OboroPlayer>();
        if (OboroPlayer.Authority && state.View.Generation == 0) state.Publish();
        var request = new OboroRequest(action, state.View.Generation, ++state.RequestNonce, angle);
        if (Main.netMode == NetmodeID.SinglePlayer) { state.Handle(request); return; }
        if (Main.netMode != NetmodeID.MultiplayerClient || player.whoAmI != Main.myPlayer) return;
        var packet = Packet(EncounterPacketType.RequestWeaponUse); request.Write(packet); packet.Send();
    }
    private static ModPacket Packet(EncounterPacketType type)
    {
        var packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        EncounterRouteCodec.WriteHeader(packet, new(EncounterProtocol.CurrentVersion, type, 0, FightId.None, 0), Route);
        return packet;
    }
    internal static void Publish(OboroSnapshot state, int toWho)
    {
        if (Main.netMode != NetmodeID.Server) return;
        var packet = Packet(EncounterPacketType.WeaponState); state.Write(packet); packet.Send(toWho);
    }
    internal static void Burst(OboroBurst burst)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) { DisplayBurst?.Invoke(burst); return; }
        if (Main.netMode != NetmodeID.Server) return;
        var packet = Packet(EncounterPacketType.WeaponBurst); burst.Write(packet); packet.Send();
    }
    public bool TryHandle(BinaryReader reader, int sender, in EncounterPacketHeader header, out string failureCode)
    {
        failureCode = "oboro.packet_invalid";
        if (!header.FightId.IsNone || header.EncounterSequence != 0 || header.Revision != 0) return false;
        if (header.PacketType == EncounterPacketType.RequestWeaponUse)
        {
            var request = OboroRequest.Read(reader);
            // tML supplies a shared buffer, not a stream ending at this packet.
            if (Main.netMode != NetmodeID.Server
                || sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active) return false;
            if (request.Action == OboroAction.Hello)
            {
                if (Main.GameUpdateCount < helloAfter[sender]) return false;
                helloAfter[sender] = Main.GameUpdateCount + 60;
            }
            Main.player[sender].GetModPlayer<OboroPlayer>().Handle(request);
        }
        else if (header.PacketType == EncounterPacketType.WeaponState)
        {
            var state = OboroSnapshot.Read(reader);
            if (Main.netMode != NetmodeID.MultiplayerClient) return false;
            var player = Main.player[state.Player].GetModPlayer<OboroPlayer>();
            if (state.NewerThan(player.View)) { player.View = state; player.ReceivedAt = Main.GameUpdateCount; }
        }
        else if (header.PacketType == EncounterPacketType.WeaponBurst)
        {
            var burst = OboroBurst.Read(reader);
            if (Main.netMode != NetmodeID.MultiplayerClient) return false;
            DisplayBurst?.Invoke(burst);
        }
        else return false;
        failureCode = string.Empty; return true;
    }
    public void PublishSnapshot(in EncounterSnapshot snapshot, int toClient) { }
    public void ApplyIdleSnapshot(in EncounterSnapshot snapshot) { }
    public override void ClearWorld() { Array.Clear(helloAfter); }
    public override void OnWorldUnload() => ClearWorld();
    public override void Unload() { nextGeneration = 0; DisplayBurst = null; }
}
