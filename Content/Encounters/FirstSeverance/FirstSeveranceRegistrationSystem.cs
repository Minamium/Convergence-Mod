using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceRegistrationSystem : ModSystem
{
    public override void PostSetupContent()
    {
        EncounterCatalogSystem.Registry.Register(FirstSeveranceDefinition.Instance);
        EncounterPacketRouter.Routes.Register(FirstSeveranceDefinition.EncounterKey,
            ModContent.GetInstance<FirstSeverancePacketSystem>());
    }
}
