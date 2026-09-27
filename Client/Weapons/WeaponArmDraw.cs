using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Convergence.Content.Items.Oboro;
using Convergence.Content.Items.DXOboro;

namespace Convergence.Client.Weapons;

// Draw-set-only rotation: armor follows the same sub-tick hand as the weapon.
// Never write a player position, native input flag or authoritative hit root.
[Autoload(Side = ModSide.Client)]
internal sealed class WeaponArmDraw : ModPlayer
{
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.headOnlyRender || Player.dead || !Player.active) return;
        bool oboro = Player.HeldItem.ModItem is Oboro;
        bool soboro = Player.HeldItem.ModItem is DXOboro;
        if (!oboro && !soboro) return;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != Player.whoAmI) continue;
            float rotation;
            if (oboro && p.ModProjectile is OboroHeldProj
                && p.GetGlobalProjectile<OboroHeldVisuals>().TryGetArmPose(p, out rotation))
            { drawInfo.compositeFrontArmRotation = Player.gravDir == -1f ? -rotation : rotation; return; }
            if (soboro && p.ModProjectile is DXOboroCut
                && p.GetGlobalProjectile<DXOboroVisuals>().TryGetArmPose(p, out rotation))
            { drawInfo.compositeFrontArmRotation = Player.gravDir == -1f ? -rotation : rotation; return; }
        }
    }
}
