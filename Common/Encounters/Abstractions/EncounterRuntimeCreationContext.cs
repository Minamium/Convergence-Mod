using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly record struct EncounterRuntimeCreationContext(
    ulong EncounterSequence,
    FightId FightId,
    AcceptedEncounterStart Start,
    IEncounterCleanupRegistrar CleanupRegistrar);
