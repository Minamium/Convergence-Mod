using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal sealed class ThirdSeveranceAvailabilityPolicy : IEncounterActivationPolicy
{
    public static ThirdSeveranceAvailabilityPolicy Instance { get; } = new();

    private ThirdSeveranceAvailabilityPolicy()
    {
    }

    public EncounterActivationDecision Evaluate(
        in EncounterStartCommand command,
        EncounterDefinition definition)
    {
        _ = command;
        _ = definition;
        return EncounterActivationDecision.Reject("third_severance.activation_not_implemented");
    }
}
