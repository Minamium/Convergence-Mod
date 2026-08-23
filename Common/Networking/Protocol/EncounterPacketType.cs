namespace Convergence.Common.Networking.Protocol;

internal enum EncounterPacketType : byte
{
    RequestActivate = 1,
    RequestSetReady = 2,
    RequestCancel = 3,
    RequestSnapshot = 4,

    Snapshot = 64,
    StateChanged = 65,
    ParticipantChanged = 66,
    ValidationResult = 67,
    EncounterEnded = 68,
}

