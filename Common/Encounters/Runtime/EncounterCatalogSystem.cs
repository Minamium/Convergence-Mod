#nullable enable

using System;
using Terraria.ModLoader;

namespace Convergence.Common.Encounters.Runtime;

internal sealed class EncounterCatalogSystem : ModSystem
{
    private static EncounterRegistry? registry;

    internal static EncounterRegistry Registry => registry
        ?? throw new InvalidOperationException("The encounter catalog is not loaded.");

    public override void Load()
    {
        registry = new EncounterRegistry();
    }

    public override void Unload()
    {
        registry?.Clear();
        registry = null;
    }
}
