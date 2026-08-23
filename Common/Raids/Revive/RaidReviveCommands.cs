using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Raids.Revive;

// This command is created only after a server/SP damage adapter has determined
// that an eligible participant would otherwise die. It is never a client claim.
internal readonly record struct AuthoritativeParticipantDownedCommand(
    FightId FightId,
    RaidParticipantBinding Participant,
    ulong AuthorityTick);

// Reviver is resolved from the packet sender by an authority adapter. A client
// may select Target, but cannot supply or replace its authoritative binding.
internal readonly record struct RaidReviveStartCommand(
    FightId FightId,
    RaidParticipantBinding Reviver,
    ParticipantId Target,
    uint RequestNonce,
    ulong AuthorityTick);

internal readonly record struct RaidReviveInterruptCommand(
    FightId FightId,
    RaidParticipantBinding Reviver,
    uint ExpectedChannelNonce,
    RaidReviveCancelReason Reason,
    ulong AuthorityTick);

internal readonly record struct RaidParticipantDisconnectedCommand(
    FightId FightId,
    RaidParticipantBinding Participant,
    ulong AuthorityTick);

// Rejoin identity is assigned by the server roster adapter. ConnectionEpoch must
// increase; it is not accepted from an untrusted packet payload.
internal readonly record struct RaidParticipantRejoinedCommand(
    FightId FightId,
    ParticipantId ParticipantId,
    int NewPlayerSlot,
    ulong NewConnectionEpoch,
    ulong AuthorityTick);
