#nullable enable
using CalamityMod;
using CalamityMod.Items.Weapons.Rogue;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Common.Compatibility.Calamity;

// Keep all Calamity types/extensions here. Standard RogueWeapon hooks retain
// armor/prefix/stealth consumption behavior; no reflection or private API.
public abstract class CalamityRogueArmament : RogueWeapon
{
    protected static DamageClass RogueClass => ModContent.GetInstance<RogueDamageClass>();
    protected static bool HasStealthStrike(Player player) => player.Calamity().StealthStrikeAvailable();
    protected static void MarkStealthStrike(int index, bool enabled)
    {
        if (index >= 0 && index < Main.maxProjectiles)
            Main.projectile[index].Calamity().stealthStrike = enabled;
    }
}

internal static class CalamityRogueArmamentDamage
{
    internal static DamageClass Class => ModContent.GetInstance<RogueDamageClass>();
    internal static void Mark(int index, bool stealth)
    {
        if (index >= 0 && index < Main.maxProjectiles) Main.projectile[index].Calamity().stealthStrike = stealth;
    }
}
