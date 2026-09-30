using System;

namespace Convergence.Content.Items.DXOboro;

// Accepted tick windows stay discrete. These fractional knots describe the
// connected hand/blade performance between simulation samples.
internal static class DXOboroMotion
{
    internal const int MaximumDuration = 26;
    internal const float Reach = 290f;
    internal const float Width = 22f;
    internal static int Duration(int step) => step switch { 1 => 16, 2 => 26, _ => 18 };
    internal static int Release(int step) => step switch { 1 => 6, 2 => 11, _ => 7 };
    internal static int LiveEnd(int step) => step switch { 1 => 13, 2 => 22, _ => 15 };
    internal static bool Live(int step, int age) => age >= Release(step) && age < LiveEnd(step);

    internal static float BladeLength(int step, float age)
    {
        float extended = step switch { 1 => 190f, 2 => 230f, _ => 188f };
        int release = Release(step), liveEnd = LiveEnd(step), duration = Duration(step);
        if (age <= release) return 140f;
        if (age < liveEnd) return 140f + (extended - 140f) * Smooth((age - release) / 2.5f);
        return extended - (extended - 140f) * Smooth((age - liveEnd) / (duration - liveEnd));
    }

    internal static float Angle(int step, float age, float aim, int facing)
        => aim + facing * Offset(step, age);

    // The elbow follows the same accepted action with a restrained angular
    // delay. Blade and collision retain Angle; this only poses the native hand.
    internal static float ArmAngle(int step, float age, float aim, int facing)
    {
        float now = Offset(step, age);
        float prior = Offset(step, MathF.Max(0f, age - .42f));
        return aim + facing * (now - .34f * (now - prior));
    }

    internal static float Offset(int step, float age)
    {
        age = Math.Clamp(age, 0f, Duration(step));
        return step switch
        {
            // One flowing kata: the cut, a rounded turn and the next cut's
            // preparation share momentum. Live-window endpoints and peak speed
            // keep the accepted sweep; only the dead stops at the ends are gone.
            1 => Reverse(age),
            2 => Heavy(age),
            _ => Downward(age)
        };
    }

    // The previous finisher's momentum carries the blade around the back and
    // over the head (2.90 - 2pi is the finisher's exit pose), easing slightly at
    // the crest before the accelerating cut, then gliding into the lower turn.
    private static float Downward(float t)
    {
        if (t < 4.2f) return Knot(t, 0, 4.2f, -3.3832f, -2.30f, .32f, .26f);
        if (t < 7f) return Knot(t, 4.2f, 7, -2.30f, -1.58f, .26f, .20f);
        if (t < 10.7f) return Knot(t, 7, 10.7f, -1.58f, -.10f, .20f, .55f);
        if (t < 15f) return Knot(t, 10.7f, 15, -.10f, 1.17f, .55f, .21f);
        return Knot(t, 15, 18, 1.17f, 1.62f, .21f, .09f);
    }

    // A rounded turn below the body instead of a brake, then the rising cut
    // continues past the guard toward the finisher's high coil.
    private static float Reverse(float t)
    {
        if (t < 2.6f) return Knot(t, 0, 2.6f, 1.62f, 1.76f, .09f, 0);
        if (t < 6f) return Knot(t, 2.6f, 6, 1.76f, 1.32f, 0, -.21f);
        if (t < 9.1f) return Knot(t, 6, 9.1f, 1.32f, .12f, -.21f, -.50f);
        if (t < 13f) return Knot(t, 9.1f, 13, .12f, -1.13f, -.50f, -.26f);
        return Knot(t, 13, 16, -1.13f, -1.84f, -.26f, -.13f);
    }

    // The high coil keeps moving forward instead of holding, the cut releases
    // at full weight, and the follow-through keeps travelling around the back.
    private static float Heavy(float t)
    {
        if (t < 3.2f) return Knot(t, 0, 3.2f, -1.84f, -2.04f, -.13f, 0);
        if (t < 7.5f) return Knot(t, 3.2f, 7.5f, -2.04f, -1.93f, 0, .05f);
        if (t < 11f) return Knot(t, 7.5f, 11, -1.93f, -1.72f, .05f, .13f);
        if (t < 17f) return Knot(t, 11, 17, -1.72f, .08f, .13f, .52f);
        if (t < 22f) return Knot(t, 17, 22, .08f, 1.75f, .52f, .32f);
        return Knot(t, 22, 26, 1.75f, 2.90f, .32f, .32f);
    }

    // Hermite tangents are radians per tick, so each adjacent beat has the
    // same pose and speed. No modulo reset occurs at impact or combo wrap.
    private static float Knot(float t, float first, float last, float a, float b, float speedA, float speedB)
    {
        float span = last - first, u = Math.Clamp((t - first) / span, 0f, 1f);
        float u2 = u * u, u3 = u2 * u;
        return (2 * u3 - 3 * u2 + 1) * a + (u3 - 2 * u2 + u) * span * speedA
            + (-2 * u3 + 3 * u2) * b + (u3 - u2) * span * speedB;
    }

    private static float Smooth(float x) { x = Math.Clamp(x, 0f, 1f); return x * x * (3f - 2f * x); }
}
