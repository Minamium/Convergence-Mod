#nullable enable

using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeveranceServerCommands
{
    private static ulong[] activationEpochs = [];
    private static uint[] activationNonces = [];

    internal static bool TryActivate(
        int senderWhoAmI,
        in TilePoint requestedAnchor,
        uint requestNonce,
        out EncounterSnapshot snapshot,
        out string failureCode)
    {
        EncounterCoordinatorSystem coordinator =
            ModContent.GetInstance<EncounterCoordinatorSystem>();
        snapshot = coordinator.Snapshot;
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            failureCode = "encounter.not_authority";
            return false;
        }

        if (!TryAcceptActivationNonce(senderWhoAmI, requestNonce, out failureCode))
        {
            return false;
        }

        IEncounterCommandSink? sink = coordinator.CommandSink;
        if (sink is null)
        {
            failureCode = "encounter.world_not_ready";
            return false;
        }

        return sink.TryStart(
            new EncounterStartCommand(
                senderWhoAmI,
                FirstSeveranceDefinition.EncounterKey,
                requestedAnchor,
                requestNonce),
            out snapshot,
            out failureCode);
    }

    internal static bool TrySetReady(
        ulong encounterSequence,
        FightId fightId,
        int senderWhoAmI,
        bool isReady,
        uint requestNonce,
        out string failureCode)
    {
        if (!FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(
                senderWhoAmI,
                out ulong connectionEpoch))
        {
            failureCode = "first_severance.connection_epoch_unavailable";
            return false;
        }

        return FirstSeverancePreparationAuthority.TryQueueReady(
            encounterSequence,
            fightId,
            senderWhoAmI,
            connectionEpoch,
            isReady,
            requestNonce,
            out failureCode);
    }

    internal static bool TryCancel(
        ulong encounterSequence,
        FightId fightId,
        int senderWhoAmI,
        uint requestNonce,
        out string failureCode)
    {
        if (!FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(
                senderWhoAmI,
                out ulong connectionEpoch))
        {
            failureCode = "first_severance.connection_epoch_unavailable";
            return false;
        }

        return FirstSeverancePreparationAuthority.TryQueueCancel(
            encounterSequence,
            fightId,
            senderWhoAmI,
            connectionEpoch,
            requestNonce,
            out failureCode);
    }

    internal static void Reset()
    {
        activationEpochs = [];
        activationNonces = [];
    }

    private static bool TryAcceptActivationNonce(
        int senderWhoAmI,
        uint requestNonce,
        out string failureCode)
    {
        EnsureCapacity();
        if (senderWhoAmI < 0
            || senderWhoAmI >= Main.maxPlayers
            || requestNonce == 0
            || !FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(
                senderWhoAmI,
                out ulong currentEpoch))
        {
            failureCode = "first_severance.activate_sender_invalid";
            return false;
        }

        if (activationEpochs[senderWhoAmI] != currentEpoch)
        {
            activationEpochs[senderWhoAmI] = currentEpoch;
            activationNonces[senderWhoAmI] = 0;
        }

        if (requestNonce <= activationNonces[senderWhoAmI])
        {
            failureCode = "first_severance.activate_nonce_stale";
            return false;
        }

        activationNonces[senderWhoAmI] = requestNonce;
        failureCode = string.Empty;
        return true;
    }

    private static void EnsureCapacity()
    {
        if (activationEpochs.Length != Main.maxPlayers)
        {
            activationEpochs = new ulong[Main.maxPlayers];
            activationNonces = new uint[Main.maxPlayers];
        }
    }
}

internal sealed class FirstSeveranceServerCommandSystem : ModSystem
{
    public override void ClearWorld()
    {
        FirstSeveranceServerCommands.Reset();
    }

    public override void OnWorldUnload()
    {
        FirstSeveranceServerCommands.Reset();
    }

    public override void Unload()
    {
        FirstSeveranceServerCommands.Reset();
    }
}
