#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Geometry;

namespace Convergence.Common.Encounters.Abstractions;

internal abstract class EncounterDefinition
{
    protected EncounterDefinition(
        IEncounterTerminationContract terminationContract,
        IReadOnlyList<EncounterTerminationDescriptor> externalTerminations)
    {
        TerminationContract = terminationContract
            ?? throw new ArgumentNullException(nameof(terminationContract));
        ExternalTerminations = new EncounterExternalTerminationMap(
            terminationContract,
            externalTerminations);
    }

    public abstract string Key { get; }

    public abstract EncounterKind Kind { get; }

    public virtual int MinimumParticipants => 1;

    public virtual int MaximumParticipants => 1;

    public virtual ArenaProfile? DefaultArena => null;

    public virtual IReadOnlyList<IEncounterActivationPolicy> ActivationPolicies =>
        Array.Empty<IEncounterActivationPolicy>();

    public IEncounterTerminationContract TerminationContract { get; }

    public EncounterExternalTerminationMap ExternalTerminations { get; }

    public abstract IEncounterRuntimeFactory RuntimeFactory { get; }
}
