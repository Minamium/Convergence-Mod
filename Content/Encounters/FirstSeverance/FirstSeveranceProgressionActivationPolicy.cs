using Convergence.Common.Compatibility.Calamity;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceProgressionActivationPolicy : IEncounterActivationPolicy
{
    public static FirstSeveranceProgressionActivationPolicy Instance { get; } = new();

    private FirstSeveranceProgressionActivationPolicy()
    {
    }

    public EncounterActivationDecision Evaluate(
        in EncounterStartCommand command,
        EncounterDefinition definition)
    {
        _ = command;
        _ = definition;
        CalamityFirstSeveranceGateResult gate =
            CalamityCompatibilitySystem.EvaluateFirstSeveranceGate();
        return gate.IsAllowed
            ? EncounterActivationDecision.Allow
            : EncounterActivationDecision.Reject(gate.FailureCode);
    }
}
