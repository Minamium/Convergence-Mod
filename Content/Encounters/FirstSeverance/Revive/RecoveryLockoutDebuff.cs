using Terraria;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.Revive;

public sealed class RecoveryLockoutDebuff : ModBuff
{
    public override string Texture =>
        "Convergence/Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = false;
    }

    public override bool RightClick(int buffIndex) => false;
}
