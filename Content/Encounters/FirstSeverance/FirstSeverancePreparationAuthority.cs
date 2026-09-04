#nullable enable

using Convergence.Common.Foundation.Identifiers;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeverancePreparationAuthority
{
    private static FirstSeverancePreparationRuntime? current;

    internal static bool TryAttach(FirstSeverancePreparationRuntime runtime)
    {
        if (current is not null && !ReferenceEquals(current, runtime))
        {
            return false;
        }

        current = runtime;
        return true;
    }

    internal static void Detach(FirstSeverancePreparationRuntime runtime)
    {
        if (ReferenceEquals(current, runtime))
        {
            current = null;
        }
    }

    internal static bool TryQueueReady(
        ulong encounterSequence,
        FightId fightId,
        int senderWhoAmI,
        ulong connectionEpoch,
        bool isReady,
        uint requestNonce,
        out string failureCode)
    {
        if (current is null
            || !current.Matches(encounterSequence, fightId))
        {
            failureCode = "first_severance.preparation_not_current";
            return false;
        }

        return current.TryQueueReady(
            senderWhoAmI,
            connectionEpoch,
            isReady,
            requestNonce,
            out failureCode);
    }

    internal static bool TryQueueCancel(
        ulong encounterSequence,
        FightId fightId,
        int senderWhoAmI,
        ulong connectionEpoch,
        uint requestNonce,
        out string failureCode)
    {
        if (current is null
            || !current.Matches(encounterSequence, fightId))
        {
            failureCode = "first_severance.preparation_not_current";
            return false;
        }

        return current.TryQueueCancel(
            senderWhoAmI,
            connectionEpoch,
            requestNonce,
            out failureCode);
    }

    internal static bool TryCreateProjection(
        ulong encounterSequence,
        FightId fightId,
        out FirstSeverancePreparationProjection? projection)
    {
        if (current is null
            || !current.Matches(encounterSequence, fightId))
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

internal sealed class FirstSeverancePreparationAuthoritySystem : ModSystem
{
    public override void ClearWorld()
    {
        FirstSeverancePreparationAuthority.Reset();
    }

    public override void OnWorldUnload()
    {
        FirstSeverancePreparationAuthority.Reset();
    }

    public override void Unload()
    {
        FirstSeverancePreparationAuthority.Reset();
    }
}
