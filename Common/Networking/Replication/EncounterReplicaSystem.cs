#nullable enable

using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Terraria.ModLoader;

namespace Convergence.Common.Networking.Replication;

internal sealed class EncounterReplicaSystem : ModSystem
{
    private readonly EncounterReplica replica = new();

    internal EncounterSnapshot Snapshot => replica.Snapshot;

    internal EncounterSnapshot? LastTerminalSnapshot => replica.LastTerminalSnapshot;

    internal bool ApplyFullSnapshot(in EncounterSnapshot snapshot)
    {
        if (snapshot.Lifecycle == EncounterLifecycle.Idle)
        {
            return replica.ApplyFullSnapshot(snapshot, null);
        }

        if (!EncounterCatalogSystem.Registry.TryGet(
            snapshot.DefinitionKey,
            out EncounterDefinition definition))
        {
            return false;
        }

        return replica.ApplyFullSnapshot(snapshot, definition.TerminationContract);
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
