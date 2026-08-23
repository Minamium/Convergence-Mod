using Convergence.Common.Encounters.Runtime;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal sealed class ThirdSeveranceRegistrationSystem : ModSystem
{
    public override void PostSetupContent()
    {
        EncounterCatalogSystem.Registry.Register(ThirdSeveranceDefinition.Instance);
    }
}
