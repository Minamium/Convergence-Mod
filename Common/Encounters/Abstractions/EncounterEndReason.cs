namespace Convergence.Common.Encounters.Abstractions;

internal enum EncounterEndReason : byte
{
    None = 0,
    Cancelled = 1,
    Victory = 2,
    Defeat = 3,
    Invalidated = 4,
    AnchorDestroyed = 5,
    EncounterActorMissing = 6,
    WorldUnload = 7,
    ProtocolFailure = 8,
    InternalFailure = 9,
}
