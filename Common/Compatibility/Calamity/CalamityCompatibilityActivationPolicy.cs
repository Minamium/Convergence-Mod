using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Common.Compatibility.Calamity;

internal sealed class CalamityCompatibilityActivationPolicy : IEncounterActivationPolicy
{
    public static CalamityCompatibilityActivationPolicy Instance { get; } = new();

    private CalamityCompatibilityActivationPolicy()
    {
    }

    public EncounterActivationDecision Evaluate(
        in EncounterStartCommand command,
        EncounterDefinition definition)
    {
        _ = command;
        _ = definition;
        return CalamityCompatibilitySystem.IsSupported
            ? EncounterActivationDecision.Allow
            : EncounterActivationDecision.Reject("compatibility.calamity_unsupported");
    }
}
