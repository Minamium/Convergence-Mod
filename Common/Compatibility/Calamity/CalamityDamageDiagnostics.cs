using CalamityMod;
using Terraria;

namespace Convergence.Common.Compatibility.Calamity;

// Read-only owner diagnostics. Never clear, cap or emulate the accessory buffer.
// Public fields: source evidence in research/2026-09-14-implementation-review.md.
internal static class CalamityDamageDiagnostics
{
    internal static (bool Equipped, double Buffer) ReadChalice(Player player)
    {
        var calamity = player.Calamity();
        return (calamity.chaliceOfTheBloodGod, calamity.chaliceBleedoutBuffer);
    }
}
