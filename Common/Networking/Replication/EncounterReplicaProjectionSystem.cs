using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Common.Networking.Replication;

internal sealed class EncounterReplicaProjectionSystem : ModSystem
{
    public override void PostUpdateWorld()
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
        {
            return;
        }

        EncounterCoordinatorSystem authority = ModContent.GetInstance<EncounterCoordinatorSystem>();
        EncounterReplicaSystem replica = ModContent.GetInstance<EncounterReplicaSystem>();

        while (authority.TryTakePublishedSnapshot(out EncounterSnapshot published))
        {
            replica.ApplyFullSnapshot(published);
        }

        replica.ApplyFullSnapshot(authority.Snapshot);
    }
}
