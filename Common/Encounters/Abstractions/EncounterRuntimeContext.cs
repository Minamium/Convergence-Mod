using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly record struct EncounterRuntimeContext(
    ulong EncounterSequence,
    FightId FightId,
    EncounterLifecycle Lifecycle,
    ulong AuthorityTick,
    ulong LifecycleEnteredTick,
    ulong ActiveFightTick);
