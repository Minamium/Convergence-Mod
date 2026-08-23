#nullable enable

using Convergence.Common.Encounters.Abstractions;
using Terraria.ModLoader;

namespace Convergence.Common.Networking.Replication;

internal sealed class EncounterReplicaSystem : ModSystem
{
    private readonly EncounterReplica replica = new();

    internal EncounterSnapshot Snapshot => replica.Snapshot;

    internal EncounterSnapshot? LastTerminalSnapshot => replica.LastTerminalSnapshot;

    internal bool ApplyFullSnapshot(in EncounterSnapshot snapshot)
    {
        return replica.ApplyFullSnapshot(snapshot);
    }

    public override void ClearWorld()
    {
        replica.Reset();
    }

    public override void OnWorldUnload()
    {
        replica.Reset();
    }
}
