namespace Convergence.Common.Encounters.Abstractions;

internal interface IEncounterCommandSink
{
    bool TryStart(
        in EncounterStartCommand command,
        out EncounterSnapshot snapshot,
        out string failureCode);
}
