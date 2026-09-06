#nullable enable

using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance.Development;

// Pure, one-shot authorization. The command adapter supplies trusted console context;
// no client packet can arm this permit. The preparation runtime owns the claimed lease.
internal sealed class FirstSeveranceDebugAssistPermit
{
    internal const ulong ArmLifetimeTicks = 10 * 60 * 60;
    private int slot = -1;
    private ulong epoch;
    private ulong expiresTick;

    internal bool IsArmed => slot >= 0;

    internal bool TryArm(bool dedicatedServer, bool launchOptIn, bool consoleCaller,
        int playerSlot, ulong connectionEpoch, ulong tick)
    {
        if (!dedicatedServer || !launchOptIn || !consoleCaller || IsArmed
            || playerSlot is < 0 or >= 255 || connectionEpoch == 0
            || tick > ulong.MaxValue - ArmLifetimeTicks)
            return false;
        slot = playerSlot;
        epoch = connectionEpoch;
        expiresTick = tick + ArmLifetimeTicks;
        return true;
    }

    internal FirstSeveranceDebugAssistLease? Claim(FightId fightId,
        FirstSeveranceRoster roster, ulong tick)
    {
        int requestedSlot = slot;
        ulong requestedEpoch = epoch;
        ulong deadline = expiresTick;
        Clear(); // Even a non-matching/expired next pull consumes the permit.
        if (fightId.IsNone || tick >= deadline
            || !roster.TryResolveCurrentBinding(requestedSlot, requestedEpoch, out var member))
            return null;
        return new FirstSeveranceDebugAssistLease(fightId, member);
    }

    internal void Clear()
    {
        slot = -1;
        epoch = 0;
        expiresTick = 0;
    }
}

internal sealed class FirstSeveranceDebugAssistLease
{
    internal FirstSeveranceDebugAssistLease(FightId owner, FirstSeveranceRosterMember member)
    {
        Owner = owner;
        Member = member;
    }

    internal FightId Owner { get; }
    internal FirstSeveranceRosterMember Member { get; }
    internal bool IsRevoked { get; private set; }

    internal bool Protects(FightId fightId, in FirstSeveranceRosterMember member)
        => !IsRevoked && !Owner.IsNone && fightId == Owner && Member.IsValid && member == Member;

    internal void Revoke() => IsRevoked = true;
}
