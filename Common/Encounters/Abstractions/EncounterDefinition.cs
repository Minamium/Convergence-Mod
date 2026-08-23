#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Geometry;

namespace Convergence.Common.Encounters.Abstractions;

internal abstract class EncounterDefinition
{
    public abstract string Key { get; }

    public abstract EncounterKind Kind { get; }

    public virtual int MinimumParticipants => 1;

    public virtual int MaximumParticipants => 1;

    public virtual ArenaProfile? DefaultArena => null;

    public virtual IReadOnlyList<IEncounterActivationPolicy> ActivationPolicies =>
        Array.Empty<IEncounterActivationPolicy>();

    public abstract IEncounterRuntimeFactory RuntimeFactory { get; }
}
