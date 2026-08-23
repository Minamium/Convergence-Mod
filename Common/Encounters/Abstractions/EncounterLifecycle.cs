namespace Convergence.Common.Encounters.Abstractions;

internal enum EncounterLifecycle : byte
{
    Idle = 0,
    Validating = 1,
    Preparing = 2,
    Active = 3,
    Resolving = 4,
    Cleanup = 5,
}
