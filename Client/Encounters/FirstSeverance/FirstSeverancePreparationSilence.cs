using Convergence.Content.Encounters.FirstSeverance;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// A transient scene selection, not a mutation of the user's volume setting.
// Separate from the combat scene so preparation does not activate its sky.
[Autoload(Side = ModSide.Client)]
internal sealed class FirstSeverancePreparationSilence : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    public override int Music => 0;

    public override bool IsSceneEffectActive(Player player)
    {
        if (Main.dedServ || Main.gameMenu || player.whoAmI != Main.myPlayer) return false;
        var state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        return state.Combat is null && state.Preparation is { } preparation
            && preparation.TryGetMemberByServerSlot(player.whoAmI, out _);
    }
}
