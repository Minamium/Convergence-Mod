using System;
using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterCleanupWork
{
    public EncounterCleanupWork(
        EncounterCleanupScope scope,
        EncounterCleanupContext context,
        ulong nextAttemptTick)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Context = context;
        NextAttemptTick = nextAttemptTick;
    }

    public EncounterCleanupScope Scope { get; }

    public EncounterCleanupContext Context { get; }

    public ulong NextAttemptTick { get; set; }
}
