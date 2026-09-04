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
        return EncounterActivationDecision.Reject("first_severance.activation_not_implemented");
    }
}
