using Terraria.ModLoader;

namespace Convergence.Common.Compatibility.Calamity;

// Resolve registered public content, never the dependency's internal Instance field.
// The required Calamity dependency must register its true-melee class before item defaults.
internal static class CalamityTrueMelee
{
    internal static DamageClass Damage => ModContent.Find<DamageClass>("CalamityMod/TrueMeleeDamageClass");
}
