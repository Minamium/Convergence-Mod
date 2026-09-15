using System;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Shared fractional action clock; performer, apparition, orb and particles
// sample these envelopes rather than starting independent looping animations.
internal static class CrimsonRigMotion
{
    internal static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    internal static float Charge(float ticksUntilFire) => Ease((60 - ticksUntilFire) / 52);
    internal static float Recoil(float sinceFire) => sinceFire < 0 ? 0 : MathF.Exp(-sinceFire / 11) * MathF.Sin(Math.Min(sinceFire / 5, MathF.PI));
}
