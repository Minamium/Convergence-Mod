using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Raids.Revive;

internal readonly record struct RaidParticipantReviveSnapshot(
    RaidParticipantBinding Binding,
    bool IsConnected,
    RaidParticipantCombatState CombatState,
    ulong DownedDeadlineTick,
    ulong DisconnectDeadlineTick,
    ulong InvulnerabilityUntilTick,
    ulong WeaknessUntilTick,
    ulong ReviveLockoutUntilTick = 0);

internal readonly record struct RaidReviveChannelSnapshot(
    ParticipantId Reviver,
    ParticipantId Target,
    uint ChannelNonce,
    ulong StartedTick,
    ulong CompletesTick);

internal readonly record struct RaidReviveSnapshot(
    FightId FightId,
    uint Revision,
    int InitialTokenCount,
    int RemainingTokenCount,
    int ReservedTokenCount,
    RaidReviveFailureReason FailureReason,
    IReadOnlyList<RaidParticipantReviveSnapshot> Participants,
    IReadOnlyList<RaidReviveChannelSnapshot> Channels);

// The future ModPlayer adapter consumes this projection every authority tick.
// A stale or slot-reused binding receives no projection from the service.
internal readonly record struct RaidParticipantControlProjection(
    bool IsDowned,
    bool IsEliminated,
    bool IsReviving,
    bool SuppressMovement,
    bool SuppressItemUse,
    bool SuppressCombat,
    bool SuppressIncomingDamage,
    bool HasReviveWeakness);
