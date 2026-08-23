using System;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly record struct EncounterActivationDecision(bool IsAllowed, string FailureCode)
{
    public static EncounterActivationDecision Allow => new(true, string.Empty);

    public static EncounterActivationDecision Reject(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A rejection requires a machine-readable failure code.", nameof(failureCode));
        }

        return new EncounterActivationDecision(false, failureCode);
    }
}
