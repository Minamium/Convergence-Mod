using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterCleanupScope : IEncounterCleanupRegistrar
{
    private readonly List<IEncounterCleanupParticipant> participants = new();
    private bool isSealed;

    public bool IsComplete => isSealed && participants.Count == 0;

    public void Register(IEncounterCleanupParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);

        if (isSealed)
        {
            throw new InvalidOperationException("Cleanup registration is closed.");
        }

        if (participants.Contains(participant))
        {
            throw new InvalidOperationException("A cleanup participant cannot be registered twice.");
        }

        participants.Add(participant);
    }

    public void CleanupPending(
        in EncounterCleanupContext context,
        Action<Exception> onFailure)
    {
        isSealed = true;

        for (int index = participants.Count - 1; index >= 0; index--)
        {
            try
            {
                participants[index].Cleanup(context);
                participants.RemoveAt(index);
            }
            catch (Exception exception)
            {
                SafeReport(onFailure, exception);
            }
        }
    }

    private static void SafeReport(Action<Exception> reporter, Exception exception)
    {
        try
        {
            reporter(exception);
        }
        catch
        {
            // Failure reporting must never prevent remaining cleanup participants.
        }
    }
}
