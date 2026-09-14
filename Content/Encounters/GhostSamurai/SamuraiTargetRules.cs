using System;

namespace Convergence.Content.Encounters.GhostSamurai;

internal readonly record struct SamuraiTarget(int Slot, Guid Connection)
{
    internal static SamuraiTarget None => new(-1, Guid.Empty);
}

internal readonly record struct SamuraiTargetCandidate(int Slot, Guid Connection, bool Active, bool Dead,
    bool Ghost, bool Participant, float DistanceSquared)
{
    internal bool Eligible => Slot is >= 0 and < 255 && Connection != Guid.Empty && Active && !Dead && !Ghost && Participant;
}

internal static class SamuraiTargetRules
{
    internal const float NonTargetDamageMultiplier = .5f;

    internal static SamuraiTarget Select(SamuraiTarget current, ReadOnlySpan<SamuraiTargetCandidate> players)
    {
        SamuraiTarget selected = SamuraiTarget.None;
        float nearest = float.MaxValue;
        foreach (var p in players)
        {
            if (!p.Eligible) continue;
            if (p.Slot == current.Slot && p.Connection == current.Connection) return current;
            if (p.DistanceSquared < nearest)
            { nearest = p.DistanceSquared; selected = new(p.Slot, p.Connection); }
        }
        return selected;
    }

    internal static float DamageMultiplier(bool multiplayer, int lockedTarget, int attacker, bool ownerKnown)
        => multiplayer && lockedTarget is >= 0 and < 255 && ownerKnown && attacker is >= 0 and < 255 && attacker != lockedTarget
            ? NonTargetDamageMultiplier : 1f;
}
