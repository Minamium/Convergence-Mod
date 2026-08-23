namespace Convergence.Common.Encounters.Abstractions;

internal interface IEncounterRuntime : IEncounterCleanupParticipant
{
    EncounterRuntimeUpdate Tick(in EncounterRuntimeContext context);
}
