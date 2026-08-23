namespace Convergence.Common.Encounters.Abstractions;

internal interface IEncounterCleanupRegistrar
{
    void Register(IEncounterCleanupParticipant participant);
}
