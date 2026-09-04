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
        // The development build admits Core validation and Ready. Its composing
        // runtime then starts the explicitly scoped combat experiment (ADR-0009).
        return EncounterActivationDecision.Allow;
    }
}
