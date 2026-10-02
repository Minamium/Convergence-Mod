#nullable enable
using System;
using Convergence.Client.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// A held Doll controller's client presentation, posing its owner's composite arms at draw time.
// Rotations use the SetCompositeArmFront/Back convention (radians, aim - pi/2) for an upright player at
// the frame's sub-tick `fraction`; return false once the controller no longer poses this player (its
// registration is then dropped). Implementations validate their own projectile identity and owner.
internal interface IDollArmPose
{
    bool TryGetArmPose(Player player, float fraction, out float front, out float? back);
}

// Draw-set-only arm rotation, like WeaponArmDraw: the composite arms follow the same sub-tick aim as the
// weapon drawn by DollWeaponLayer. Owner AI still calls SetCompositeArmFront/Back every tick, which enables
// the arms and lets peers see the pose. Never writes a player position, input flag or hit root. Unused
// until a Doll weapon registers a pose.
[Autoload(Side = ModSide.Client)]
internal sealed class DollWeaponArmDraw : ModPlayer
{
    private static readonly IDollArmPose?[] poses = new IDollArmPose?[Main.maxPlayers + 1];
    private static bool warned;

    internal static void Set(int player, IDollArmPose pose)
    {
        if ((uint)player < (uint)poses.Length) poses[player] = pose;
    }

    internal static void Release(int player, IDollArmPose pose)
    {
        if ((uint)player < (uint)poses.Length && ReferenceEquals(poses[player], pose)) poses[player] = null;
    }

    internal static void ClearAll() => Array.Clear(poses);

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.headOnlyRender || Player.dead || !Player.active) return;
        int slot = Player.whoAmI;
        if ((uint)slot >= (uint)poses.Length || poses[slot] is not { } pose) return;
        float front;
        float? back;
        try
        {
            if (!pose.TryGetArmPose(Player, WeaponDrawClock.Fraction, out front, out back))
            {
                poses[slot] = null;
                return;
            }
        }
        catch (Exception exception)
        {
            poses[slot] = null;
            if (!warned)
            {
                warned = true;
                ConvergenceMod.Instance.Logger.Warn($"A Doll weapon arm pose threw and was dropped (later drops are silent): {exception}");
            }
            return;
        }
        float sign = Player.gravDir == -1f ? -1f : 1f;
        if (float.IsFinite(front)) drawInfo.compositeFrontArmRotation = front * sign;
        if (back is { } rotation && float.IsFinite(rotation)) drawInfo.compositeBackArmRotation = rotation * sign;
    }

    public override void Unload()
    {
        ClearAll();
        warned = false;
    }
}
