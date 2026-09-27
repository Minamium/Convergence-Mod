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
            // Long draw from the prior finish, a nearly held high guard,
            // acceleration to the middle of contact, then brake/overshoot.
            1 => Reverse(age),
            2 => Heavy(age),
            _ => Downward(age)
        };
    }

    private static float Downward(float t)
    {
        if (t < 3f) return Knot(t, 0, 3, 1.2f, -1.83f, 0, 0);
        if (t < 7f) return Knot(t, 3, 7, -1.83f, -1.58f, 0, .04f);
        if (t < 10.7f) return Knot(t, 7, 10.7f, -1.58f, -.10f, .04f, .55f);
        if (t < 15f) return Knot(t, 10.7f, 15, -.10f, 1.17f, .55f, .04f);
        if (t < 16.2f) return Knot(t, 15, 16.2f, 1.17f, 1.37f, .04f, 0);
        return Knot(t, 16.2f, 18, 1.37f, 1.2f, 0, 0);
    }

    private static float Reverse(float t)
    {
        if (t < 2.3f) return Knot(t, 0, 2.3f, 1.2f, 1.48f, 0, 0);
        if (t < 6f) return Knot(t, 2.3f, 6, 1.48f, 1.32f, 0, -.04f);
        if (t < 9.1f) return Knot(t, 6, 9.1f, 1.32f, .12f, -.04f, -.50f);
        if (t < 13f) return Knot(t, 9.1f, 13, .12f, -1.13f, -.50f, -.04f);
        if (t < 14.2f) return Knot(t, 13, 14.2f, -1.13f, -1.58f, -.04f, 0);
        return Knot(t, 14.2f, 16, -1.58f, -1.5f, 0, 0);
    }

    private static float Heavy(float t)
    {
        if (t < 5f) return Knot(t, 0, 5, -1.5f, -1.94f, 0, 0);
        if (t < 11f) return Knot(t, 5, 11, -1.94f, -1.72f, 0, .04f);
        if (t < 17f) return Knot(t, 11, 17, -1.72f, .08f, .04f, .52f);
        if (t < 22f) return Knot(t, 17, 22, .08f, 1.75f, .52f, .06f);
        if (t < 23.4f) return Knot(t, 22, 23.4f, 1.75f, 2.03f, .06f, 0);
        return Knot(t, 23.4f, 26, 2.03f, 1.2f, 0, 0);
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
