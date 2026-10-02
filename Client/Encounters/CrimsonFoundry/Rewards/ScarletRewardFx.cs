#nullable enable
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Client.Weapons;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// Small shared client helpers for the reward visuals: who owns what, Reduced Effects, shake and particles.
internal static class ScarletRewardFx
{
    internal static bool Reduced => CrimsonVisuals.Reduced;
    internal static float Fraction => WeaponDrawClock.Fraction;
    internal static bool Local(int owner) => owner == Main.myPlayer;

    // An ink style for a projectile's owner; ScarletRewardInk applies the remote opacity (0.5 dormant, 0.85 live).
    internal static ScarletInkStyle Ink(int owner, ScarletInkLook look, float seed, float opacity = 1, bool fire = false, float warmth = 0)
        => new(look, Local(owner), seed, opacity, fire, warmth, owner);

    // Shake comes only from the local player's own weapon, honours the shake switch and is off under Reduced Effects.
    internal static void Shake(int owner, Vector2 at, float strength, Vector2? direction = null)
    {
        if (Main.dedServ || !Local(owner) || Reduced || !ModContent.GetInstance<CrimsonVisualConfig>().ScreenShake || strength <= 0) return;
        if (direction is { } along && along.LengthSquared() > 1e-4f)
            ScreenShakeSystem.StartShakeAtPoint(at, strength, shakeDirection: Vector2.Normalize(along), angularVariance: .35f,
                shakeStrengthDissipationIncrement: .6f);
        else ScreenShakeSystem.StartShakeAtPoint(at, strength, shakeStrengthDissipationIncrement: .6f);
    }

    internal static bool Particle(ScarletParticleKind kind, int owner, Vector2 at, Vector2 velocity, float life, float size, float seed)
        => !Main.dedServ && ScarletRewardInk.Particles.Spawn(kind, owner, Local(owner), at, velocity, life, size, Reduced, seed);
}
