#nullable enable
using Terraria;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Explicitly opt into tML's standard/fancy boss-bar renderer. No custom combat
// text or new texture is needed; the loader supplies its default icon fallback.
public sealed class CrimsonBossBar : ModBossBar
{
    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax,
        ref float shield, ref float shieldMax)
    {
        int slot = info.npcIndexToAimAt;
        if (slot < 0 || slot >= Main.maxNPCs || !Main.npc[slot].active) return false;
        CrimsonBoss? boss = Main.npc[slot].ModNPC as CrimsonBoss;
        if (boss is null && Main.npc[slot].ModNPC is CrimsonEffigy effigy) effigy.TryBoss(out boss);
        if (boss is null || !boss.Fresh || boss.State.Stage != CrimsonStage.Performance) return false;
        // All tracked NPCs report the same focused encounter bar, including a
        // dormant child selected by vanilla during a phase transition.
        life = boss.State.BarLife; lifeMax = boss.State.BarMax; shield = shieldMax = 0;
        return lifeMax > 0 && life > 0;
    }
}
