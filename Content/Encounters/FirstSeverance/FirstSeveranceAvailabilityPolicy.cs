using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceAvailabilityPolicy : IEncounterActivationPolicy
{
    public static FirstSeveranceAvailabilityPolicy Instance { get; } = new();

    private FirstSeveranceAvailabilityPolicy()
    {
    }

    public EncounterActivationDecision Evaluate(
        in EncounterStartCommand command,
        EncounterDefinition definition)
    {
        _ = command;
        _ = definition;
        // Slice 3 permits validation and Ready preparation only. The preparation
        // runtime keeps its combat gate closed and cannot enter Active.
        return EncounterActivationDecision.Allow;
    }
}
