namespace Convergence.Common.Encounters.Abstractions;

internal interface IEncounterRuntimeFactory
{
    IEncounterRuntime Create(
        EncounterDefinition definition,
        in EncounterRuntimeCreationContext context);
}
