using System;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceRuntimeFactory : IEncounterRuntimeFactory
{
    public static FirstSeveranceRuntimeFactory Instance { get; } = new();

    private FirstSeveranceRuntimeFactory()
    {
    }

    public IEncounterRuntime Create(
        EncounterDefinition definition,
        in EncounterRuntimeCreationContext context)
    {
        if (definition is not FirstSeveranceDefinition)
        {
            throw new ArgumentException(
                "The First Severance runtime requires its matching definition.",
                nameof(definition));
        }

        return new FirstSeveranceBootstrapRuntime(
            context.FightId,
            FirstSeveranceEncounterPlan.Instance,
            InertFirstSeveranceWorldAdapter.Instance);
    }
}
