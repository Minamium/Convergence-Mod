#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Terraria.ModLoader;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterActivationPolicyCatalogSystem : ModSystem
{
    private static List<IEncounterActivationPolicy>? policies;

    internal static IReadOnlyList<IEncounterActivationPolicy> Policies => policies
        ?? throw new InvalidOperationException("The activation policy catalog is not loaded.");

    internal static void Register(IEncounterActivationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        (policies ?? throw new InvalidOperationException("The activation policy catalog is not loaded."))
            .Add(policy);
    }

    public override void Load()
    {
        policies = new List<IEncounterActivationPolicy>
        {
            AuthorityActivationPolicy.Instance,
        };
    }

    public override void Unload()
    {
        policies?.Clear();
        policies = null;
    }
}
