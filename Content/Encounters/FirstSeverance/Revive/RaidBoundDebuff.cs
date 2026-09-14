using Terraria;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Revive;

// Display only. Removing this icon cannot grant membership or change a Fight.
public sealed class RaidBoundDebuff : ModBuff
{
    public override string Texture => "Convergence/Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem";
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }
    public override bool RightClick(int buffIndex) => false;
}
