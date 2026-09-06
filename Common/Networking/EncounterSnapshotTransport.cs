using System.Collections.Generic;
using System.IO;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Networking.Replication;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Common.Networking;

internal sealed class EncounterSnapshotTransport : ModSystem
{
    private static readonly Dictionary<int, (ulong Tick, int Count)> Requests = new();

    public override void PostUpdateWorld()
    {
        if (Main.netMode != NetmodeID.Server) return;
        var authority = ModContent.GetInstance<EncounterCoordinatorSystem>();
        while (authority.TryTakePublishedSnapshot(out var snapshot)) SendSnapshot(snapshot, -1);
    }

    internal static void RequestSnapshot()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        var packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
        EncounterPacketCodec.WriteHeader(packet, new(EncounterProtocol.CurrentVersion,
            EncounterPacketType.RequestSnapshot, 0, FightId.None, 0));
        packet.Send();
    }

    internal static bool HandleRequest(int sender, out string failure)
    {
        failure = "packet.snapshot_sender_invalid";
        if (sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active) return false;
        ulong now = Main.GameUpdateCount;
        Requests.TryGetValue(sender, out var window);
        if (now < window.Tick || now - window.Tick >= 60) window = (now, 0);
        failure = "packet.snapshot_rate_limited";
        if (window.Count >= 4) return false;
        Requests[sender] = (window.Tick, window.Count + 1);
        SendSnapshot(ModContent.GetInstance<EncounterCoordinatorSystem>().Snapshot, sender);
        failure = string.Empty;
        return true;
    }

    private static void SendSnapshot(in EncounterSnapshot snapshot, int client)
    {
        if (snapshot.Lifecycle == EncounterLifecycle.Idle)
        {
            var packet = global::Convergence.ConvergenceMod.Instance.GetPacket();
            EncounterRouteCodec.WriteHeader(packet, new(EncounterProtocol.CurrentVersion,
                EncounterPacketType.Snapshot, snapshot.EncounterSequence, snapshot.FightId, snapshot.Revision), string.Empty);
            packet.Write(snapshot.AuthorityTick);
            packet.Send(client);
        }
        else if (EncounterPacketRouter.Routes.TryGet(snapshot.DefinitionKey, out var handler))
            handler.PublishSnapshot(snapshot, client);
        else
            global::Convergence.ConvergenceMod.Instance.Logger.Error("Missing packet adapter for registered encounter: " + snapshot.DefinitionKey);
    }

    internal static bool ApplyIdle(BinaryReader reader, in EncounterPacketHeader header, out string failure)
    {
        ulong tick = reader.ReadUInt64();
        failure = "packet.idle_header_invalid";
        if (!header.FightId.IsNone) return false;
        var snapshot = EncounterSnapshot.CreateIdle(header.EncounterSequence, header.Revision, tick);
        if (ModContent.GetInstance<EncounterReplicaSystem>().ApplyFullSnapshot(snapshot))
            foreach (var handler in EncounterPacketRouter.Routes.Handlers) handler.ApplyIdleSnapshot(snapshot);
        failure = string.Empty;
        return true;
    }

    public override void ClearWorld() => Requests.Clear();
    public override void OnWorldUnload() => Requests.Clear();
    public override void Unload() => Requests.Clear();
}

internal sealed class EncounterSnapshotPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (Player.whoAmI == Main.myPlayer) EncounterSnapshotTransport.RequestSnapshot();
    }
}
