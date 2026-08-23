using System;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal sealed class ThirdSeveranceRuntimeFactory : IEncounterRuntimeFactory
{
    public static ThirdSeveranceRuntimeFactory Instance { get; } = new();

    private ThirdSeveranceRuntimeFactory()
    {
    }

    public IEncounterRuntime Create(
        EncounterDefinition definition,
        in EncounterRuntimeCreationContext context)
    {
        if (definition is not ThirdSeveranceDefinition)
        {
            throw new ArgumentException(
                "The Third Severance runtime requires its matching definition.",
                nameof(definition));
        }

        return new ThirdSeveranceBootstrapRuntime(
            context.FightId,
            ThirdSeveranceEncounterPlan.Instance,
            InertThirdSeveranceWorldAdapter.Instance);
    }
}
