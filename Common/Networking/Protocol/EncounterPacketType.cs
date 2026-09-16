namespace Convergence.Common.Networking.Protocol;

internal enum EncounterPacketType : byte
{
    RequestActivate = 1,
    RequestSetReady = 2,
    RequestCancel = 3,
    RequestSnapshot = 4,
    RequestPrototypeDown = 5,
    RequestReviveNearest = 6,
    RequestRaidHurtResult = 7,
    RequestWeaponUse = 8,

    Snapshot = 64,
    StateChanged = 65,
    ParticipantChanged = 66,
    ValidationResult = 67,
    EncounterEnded = 68,
    RaidHit = 69,
    WeaponState = 70,
    WeaponBurst = 71,
}
