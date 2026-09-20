using System;
using System.Numerics;

namespace Convergence.Content.Encounters.AzureCathedral;

// Continuous curves, sampled by the authority; body parts inherit the travelled
// path rather than each orbiting its own small target.
internal static class AzureFlight
{
    internal static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        t = Math.Clamp(t, 0, 1); float s = 1 - t;
        return a * (s*s*s) + b * (3*s*s*t) + c * (3*s*t*t) + d * (t*t*t);
    }
    internal static Vector2 Steer(Vector2 velocity, Vector2 delta, float speed, float turn = .045f)
    {
        if (delta.LengthSquared() < 1) return velocity;
        float old = velocity.LengthSquared() < 1 ? MathF.Atan2(delta.Y, delta.X) : MathF.Atan2(velocity.Y, velocity.X);
        float difference = MathF.IEEERemainder(MathF.Atan2(delta.Y, delta.X) - old, MathF.Tau);
        float angle = old + Math.Clamp(difference, -turn, turn);
        float length = velocity.Length() + Math.Clamp(speed - velocity.Length(), -1.2f, 1.2f);
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * length;
    }
}
