#nullable enable

using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking.Replication;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceClientStateSystem : ModSystem
{
    private FirstSeverancePreparationProjection? preparation;
    private uint lastValidationNonce;
    private bool requestedInitialSnapshot;

    internal FirstSeverancePreparationProjection? Preparation => preparation;

    internal FirstSeveranceValidationMessage? LastValidation { get; private set; }

    internal void ApplySnapshot(
        in EncounterSnapshot snapshot,
        FirstSeverancePreparationProjection? incomingPreparation)
    {
        preparation = snapshot.Lifecycle == EncounterLifecycle.Preparing
            && incomingPreparation is not null
            && incomingPreparation.EncounterSequence == snapshot.EncounterSequence
            && incomingPreparation.FightId == snapshot.FightId
                ? incomingPreparation
                : null;
    }

    internal void ApplyValidation(in FirstSeveranceValidationMessage validation)
    {
        if (validation.RequestNonce <= lastValidationNonce)
        {
            return;
        }

        lastValidationNonce = validation.RequestNonce;
        LastValidation = validation;
        if (!validation.IsAccepted && Main.netMode != NetmodeID.Server)
        {
            FirstSeveranceClientActions.ShowRejected(validation.FailureCode);
        }
    }

    public override void OnWorldLoad()
    {
        ResetState();
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            EncounterCoordinatorSystem authority =
                ModContent.GetInstance<EncounterCoordinatorSystem>();
            FirstSeverancePreparationAuthority.TryCreateProjection(
                authority.Snapshot.EncounterSequence,
                authority.Snapshot.FightId,
                out FirstSeverancePreparationProjection? projection);
            ApplySnapshot(authority.Snapshot, projection);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient
            && !requestedInitialSnapshot
            && Main.myPlayer >= 0
            && Main.myPlayer < Main.maxPlayers
            && Main.player[Main.myPlayer].active)
        {
            requestedInitialSnapshot = true;
            FirstSeveranceClientActions.RequestSnapshot();
        }
    }

    public override void OnWorldUnload()
    {
        ResetState();
    }

    public override void Unload()
    {
        ResetState();
    }

    private void ResetState()
    {
        preparation = null;
        lastValidationNonce = 0;
        LastValidation = null;
        requestedInitialSnapshot = false;
        FirstSeveranceClientActions.Reset();
    }
}
