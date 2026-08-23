namespace Convergence.Common.Encounters.Abstractions;

internal interface IEncounterActivationPolicy
{
    EncounterActivationDecision Evaluate(
        in EncounterStartCommand command,
        EncounterDefinition definition);
}
