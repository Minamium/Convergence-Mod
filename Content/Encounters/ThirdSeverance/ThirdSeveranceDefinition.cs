#nullable enable

using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal sealed class ThirdSeveranceDefinition : EncounterDefinition
{
    private static readonly IEncounterActivationPolicy[] FeatureActivationPolicies =
    {
        ThirdSeveranceAvailabilityPolicy.Instance,
    };

    public const string EncounterKey = "third_severance";

    public static ThirdSeveranceDefinition Instance { get; } = new();

    private ThirdSeveranceDefinition()
    {
    }

    public override string Key => EncounterKey;

    public override EncounterKind Kind => EncounterKind.Raid;

    public override int MinimumParticipants => 2;

    public override int MaximumParticipants => 4;

    public override ArenaProfile? DefaultArena => ThirdSeveranceEncounterPlan.Instance.Arena.Profile;

    internal ThirdSeveranceEncounterPlan Plan => ThirdSeveranceEncounterPlan.Instance;

    public override IReadOnlyList<IEncounterActivationPolicy> ActivationPolicies =>
        FeatureActivationPolicies;

    public override IEncounterRuntimeFactory RuntimeFactory => ThirdSeveranceRuntimeFactory.Instance;
}
