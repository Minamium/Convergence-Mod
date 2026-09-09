using System;
using CalamityMod;
using CalamityMod.CalPlayer;
using Terraria;
using Terraria.Audio;

namespace Convergence.Common.Compatibility.Calamity;

// Raid HP loss deliberately bypasses Hurt (fixed damage and recoverable Down).
// Bridge ONLY Adrenaline: never replay OnHurt, shields, retaliation or death.
internal static class CalamityRaidHit
{
    internal static void Apply(Player player)
    {
        CalamityPlayer calamity = player.Calamity();
        if (!calamity.AdrenalineEnabled || calamity.adrenalineModeActive) return;

        // Calamity's private normal-hit and Nanomachine pause constants are both
        // 60 ticks in the pinned source. Keep the dependency boundary here.
        calamity.adrenalinePauseTimer = (int)Math.Min(int.MaxValue,
            (long)calamity.adrenalinePauseTimer + 60);
        if (calamity.draedonsHeart) return;

        bool wasFull = calamity.adrenaline >= calamity.adrenalineMax;
        calamity.adrenaline = 0;
        calamity.playFullAdrenalineSound = true;
        if (wasFull && !Main.dedServ && player.whoAmI == Main.myPlayer)
            SoundEngine.PlaySound(Main.zenithWorld ? CalamityPlayer.AdrenalineHurtGFB
                : CalamityPlayer.AdrenalineHurtSound, player.Center);
    }
}
