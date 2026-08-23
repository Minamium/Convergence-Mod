using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Common.Encounters.Runtime;

internal static class EncounterLifecycleMachine
{
    public static bool CanTransition(EncounterLifecycle current, EncounterLifecycle next)
    {
        return (current, next) switch
        {
            (EncounterLifecycle.Validating, EncounterLifecycle.Preparing) => true,
            (EncounterLifecycle.Preparing, EncounterLifecycle.Active) => true,
            (EncounterLifecycle.Active, EncounterLifecycle.Resolving) => true,
            _ => false,
        };
    }
}
