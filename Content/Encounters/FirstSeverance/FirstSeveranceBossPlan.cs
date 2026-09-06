#nullable enable

using System;

namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeveranceBossKeys
{
    public const string Actor = "null_cantor";
}

internal enum FirstSeveranceBossVisualState : byte
{
    Shielded = 1,
    Exposed = 2,
}

internal sealed class FirstSeveranceBossPlan
{
    public FirstSeveranceBossPlan(string actorKey, bool usesPersistentLifePool)
    {
        if (string.IsNullOrWhiteSpace(actorKey))
        {
            throw new ArgumentException("A Boss plan requires an actor key.", nameof(actorKey));
        }

        if (!usesPersistentLifePool)
        {
            throw new ArgumentException(
                "First Severance requires one persistent Boss life pool.",
                nameof(usesPersistentLifePool));
        }

        ActorKey = actorKey;
        UsesPersistentLifePool = usesPersistentLifePool;
    }

    public static FirstSeveranceBossPlan CreateDefault()
    {
        return new FirstSeveranceBossPlan(
            FirstSeveranceBossKeys.Actor,
            usesPersistentLifePool: true);
    }

    public string ActorKey { get; }

    public bool UsesPersistentLifePool { get; }

    public bool CanTakeDamage(FirstSeveranceSubstate substate)
    {
        return FirstSeveranceBossPhasePlan.IsDamageState(substate);
    }

    public FirstSeveranceBossVisualState GetVisualState(FirstSeveranceSubstate substate)
    {
        return CanTakeDamage(substate)
            ? FirstSeveranceBossVisualState.Exposed
            : FirstSeveranceBossVisualState.Shielded;
    }
}
