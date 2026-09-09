using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Native weapon ownership, not a Raid request or outcome authority. Only the
// owning player samples input, spends resources and emits child shots.
internal static class RitualChannel
{
    internal static bool Valid(Projectile p, Player owner, int itemType) => RitualTargeting.ValidState(p)
        && RitualArmamentItems.Usable(owner) && !owner.noItems && !owner.CCed && owner.HeldItem.type == itemType;
    internal static void Hold(Projectile p, Player owner, float turnRate)
    {
        Vector2 axis = RitualArmamentItems.Aim(p.velocity, owner.direction);
        if (p.owner == Main.myPlayer)
        {
            Vector2 desired = RitualArmamentItems.Aim(Main.MouseWorld - owner.MountedCenter, owner.direction);
            float turn = MathHelper.WrapAngle(desired.ToRotation() - axis.ToRotation());
            axis = axis.RotatedBy(Math.Clamp(turn, -turnRate, turnRate)); p.velocity = axis;
            if ((int)p.ai[0] % 6 == 0 && Math.Abs(turn) > .002f || (int)p.ai[0] % 30 == 0) p.netUpdate = true;
        }
        p.Center = owner.RotatedRelativePoint(owner.MountedCenter, true);
        p.rotation = axis.ToRotation(); p.timeLeft = 2;
        owner.ChangeDir(axis.X >= 0 ? 1 : -1); owner.heldProj = p.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.itemRotation = (axis * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, p.rotation - MathHelper.PiOver2);
    }
    internal static void Stop(Projectile p)
    {
        if (p.ai[2] < 0) return;
        p.ai[2] = -1; p.friendly = false; p.netUpdate = true;
    }
    internal static bool Fade(Projectile p)
    {
        if (p.ai[2] >= 0) return false;
        p.friendly = false; p.ai[2]--; p.timeLeft = 2;
        if (p.ai[2] <= -20) p.Kill();
        return true;
    }
}
