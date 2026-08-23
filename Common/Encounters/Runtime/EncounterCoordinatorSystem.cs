#nullable enable

using System;
using Convergence.Common.Encounters.Abstractions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterCoordinatorSystem : ModSystem
{
    private EncounterCoordinator? coordinator;

    internal EncounterSnapshot Snapshot => coordinator?.Snapshot ?? EncounterSnapshot.Idle;

    internal IEncounterCommandSink? CommandSink => coordinator;

    internal EncounterSnapshot? LastTerminalSnapshot => coordinator?.LastTerminalSnapshot;

    internal bool TryTakePublishedSnapshot(out EncounterSnapshot snapshot)
    {
        if (coordinator is null)
        {
            snapshot = default;
            return false;
        }

        return coordinator.TryTakePublishedSnapshot(out snapshot);
    }

    public override void ClearWorld()
    {
        coordinator?.Reset(EncounterEndReason.WorldUnload);
        coordinator = null;
    }

    public override void OnWorldLoad()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            coordinator = new EncounterCoordinator(
                EncounterCatalogSystem.Registry,
                EncounterActivationPolicyCatalogSystem.Policies,
                ReportRuntimeException);
        }
    }

    public override void OnWorldUnload()
    {
        coordinator?.Reset(EncounterEndReason.WorldUnload);
        coordinator = null;
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            coordinator?.Tick();
        }
    }

    private void ReportRuntimeException(Exception exception)
    {
        Mod.Logger.Error("Encounter runtime failure. Cleanup was requested.", exception);
    }
}
