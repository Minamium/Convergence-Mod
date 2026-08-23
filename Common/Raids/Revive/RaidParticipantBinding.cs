using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Raids.Revive;

// ParticipantId is stable for one Fight. PlayerSlot is reusable, so every
// authority-side binding also carries a connection epoch allocated by the adapter.
internal readonly record struct RaidParticipantBinding(
    ParticipantId ParticipantId,
    int PlayerSlot,
    ulong ConnectionEpoch)
{
    public const int MaximumPlayerSlotExclusive = byte.MaxValue;

    public bool IsValid => ParticipantId.IsValid
        && PlayerSlot is >= 0 and < MaximumPlayerSlotExclusive
        && ConnectionEpoch != 0;
}
