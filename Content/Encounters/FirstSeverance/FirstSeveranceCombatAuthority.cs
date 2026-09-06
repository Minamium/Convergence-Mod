#nullable enable

using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeveranceCombatAuthority
{
    private static FirstSeverancePrototypeCombatRuntime? current;

    internal static bool IsActive => Main.netMode != NetmodeID.MultiplayerClient && current is not null;

    internal static bool TryAttach(FirstSeverancePrototypeCombatRuntime runtime)
    {
        if (current is not null && !ReferenceEquals(current, runtime))
        {
            return false;
        }

        current = runtime;
        return true;
    }

    internal static void Detach(FirstSeverancePrototypeCombatRuntime runtime)
    {
        if (ReferenceEquals(current, runtime))
        {
            current = null;
        }
    }

    internal static void RecordActorDeath(NPC npc)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            current?.RecordActorDeath(npc);
    }

    internal static bool ProtectBossPhaseBoundary(NPC npc)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return current?.ProtectBossPhaseBoundary(npc) == true;
        // Mirror the already accepted stage only; this never advances a stage.
        if (ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat is not { } combat)
            return false;
        npc.life = FirstSeveranceBossPhasePlan.Instance.TryGetNext(combat.BossPhase, out var next)
            ? System.Math.Max(1, FirstSeveranceBossPhasePlan.LifeThreshold(combat.BossMaximumLife, next)) : 1;
        return true;
    }

    internal static bool CanHitActor(NPC npc, int playerSlot)
    {
        if (playerSlot < 0 || playerSlot >= Main.maxPlayers || npc.dontTakeDamage)
            return false;
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return current?.CanHitActor(npc, playerSlot) == true;
        return ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat is { } combat
            && combat.TryGetParticipantByServerSlot(playerSlot, out var member)
            && member.IsConnected && member.CombatState == RaidParticipantCombatState.Alive
            && !member.IsReviving;
    }

    internal static bool TryQueueCancel(ulong encounterSequence, FightId fightId,
        int senderWhoAmI, ulong connectionEpoch, uint requestNonce, out string failureCode)
    {
        if (current is null || !current.Matches(encounterSequence, fightId))
        {
            failureCode = "first_severance.combat_not_current";
            return false;
        }
        return current.TryQueueCancel(senderWhoAmI, connectionEpoch, requestNonce, out failureCode);
    }

    internal static bool TryQueuePrototypeDown(
        ulong encounterSequence,
        FightId fightId,
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out string failureCode)
    {
        if (current is null || !current.Matches(encounterSequence, fightId))
        {
            failureCode = "first_severance.combat_not_current";
            return false;
        }

        return current.TryQueuePrototypeDown(
            senderWhoAmI,
            connectionEpoch,
            requestNonce,
            out failureCode);
    }

    internal static bool TryQueueReviveNearest(
        ulong encounterSequence,
        FightId fightId,
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out string failureCode)
    {
        if (current is null || !current.Matches(encounterSequence, fightId))
        {
            failureCode = "first_severance.combat_not_current";
            return false;
        }

        return current.TryQueueReviveNearest(
            senderWhoAmI,
            connectionEpoch,
            requestNonce,
            out failureCode);
    }

    internal static bool TryCreateProjection(
        ulong encounterSequence,
        FightId fightId,
        out FirstSeveranceCombatProjection? projection)
    {
        if (current is null || !current.Matches(encounterSequence, fightId))
        {
            projection = null;
            return false;
        }

        return current.TryCreateProjection(out projection);
    }

    internal static void Reset()
    {
        current = null;
    }
}

internal sealed class FirstSeveranceCombatAuthoritySystem : ModSystem
{
    public override void ClearWorld()
    {
        FirstSeveranceCombatAuthority.Reset();
    }

    public override void OnWorldUnload()
    {
        FirstSeveranceCombatAuthority.Reset();
    }

    public override void Unload()
    {
        FirstSeveranceCombatAuthority.Reset();
    }
}
