using Convergence.Common.Encounters.Runtime;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceRegistrationSystem : ModSystem
{
    public override void PostSetupContent()
    {
        EncounterCatalogSystem.Registry.Register(FirstSeveranceDefinition.Instance);
    }
}
