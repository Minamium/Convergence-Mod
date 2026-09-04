#nullable enable

using System;

namespace Convergence.Common.Encounters.Abstractions;

// Factories use this only for an expected, fully validated start rejection that
// occurred before a session was accepted. It never represents a runtime fault.
internal sealed class EncounterStartRejectedException : Exception
{
    public EncounterStartRejectedException(string failureCode)
        : base(failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException(
                "An encounter start rejection requires a stable code.",
                nameof(failureCode));
        }

        FailureCode = failureCode;
    }

    public string FailureCode { get; }
}
