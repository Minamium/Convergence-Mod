namespace Convergence.Common.Encounters.Abstractions;

internal interface IEncounterCleanupParticipant
{
    void Cleanup(in EncounterCleanupContext context);
}
