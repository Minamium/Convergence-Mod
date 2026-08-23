using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly record struct EncounterCleanupContext(
    ulong EncounterSequence,
    FightId FightId,
    EncounterEndReason EndReason);
