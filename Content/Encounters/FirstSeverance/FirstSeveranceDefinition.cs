#nullable enable

using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Content.Encounters.FirstSeverance.Development;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceDefinition : EncounterDefinition
{
    private static readonly IEncounterActivationPolicy[] FeatureActivationPolicies =
    {
        FirstSeveranceAvailabilityPolicy.Instance,
        FirstSeveranceProgressionActivationPolicy.Instance,
    };

    public const string EncounterKey = FirstSeveranceIdentity.EncounterKey;
    public static int MinimumRosterSize => FirstSeveranceDevelopmentPolicy.MinimumParticipants;
    public const int MaximumRosterSize = FirstSeveranceRoster.MaximumCount;

    public static FirstSeveranceDefinition Instance { get; } = new();

    private FirstSeveranceDefinition()
        : base(
            FirstSeveranceTerminationContract.Instance,
            FirstSeveranceTerminationContract.Instance.ExternalTerminations)
    {
    }

    public override string Key => EncounterKey;

    public override EncounterKind Kind => EncounterKind.Raid;

    public override int MinimumParticipants => MinimumRosterSize;

    public override int MaximumParticipants => MaximumRosterSize;

    public override ArenaProfile? DefaultArena => FirstSeveranceEncounterPlan.Instance.Arena.Profile;

    internal FirstSeveranceEncounterPlan Plan => FirstSeveranceEncounterPlan.Instance;

    public override IReadOnlyList<IEncounterActivationPolicy> ActivationPolicies =>
        FeatureActivationPolicies;

    public override IEncounterRuntimeFactory RuntimeFactory => FirstSeveranceRuntimeFactory.Instance;
}
