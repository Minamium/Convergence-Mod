#nullable enable

using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceDefinition : EncounterDefinition
{
    private static readonly IEncounterActivationPolicy[] FeatureActivationPolicies =
    {
        FirstSeveranceAvailabilityPolicy.Instance,
    };

    public const string EncounterKey = "first_severance";

    public static FirstSeveranceDefinition Instance { get; } = new();

    private FirstSeveranceDefinition()
    {
    }

    public override string Key => EncounterKey;

    public override EncounterKind Kind => EncounterKind.Raid;

    public override int MinimumParticipants => 2;

    public override int MaximumParticipants => 4;

    public override ArenaProfile? DefaultArena => FirstSeveranceEncounterPlan.Instance.Arena.Profile;

    internal FirstSeveranceEncounterPlan Plan => FirstSeveranceEncounterPlan.Instance;

    public override IReadOnlyList<IEncounterActivationPolicy> ActivationPolicies =>
        FeatureActivationPolicies;

    public override IEncounterRuntimeFactory RuntimeFactory => FirstSeveranceRuntimeFactory.Instance;
}
