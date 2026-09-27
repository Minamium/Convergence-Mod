using System;

namespace Convergence.Content.Items.DXOboro;

// One normalized clock drives blade pose, native collision, and all visible material.
internal static class DXOboroMotion
{
    internal const int MaximumDuration = 26;
    internal const float Reach = 290f;
    internal const float Width = 22f;
    internal static int Duration(int step) => step switch { 1 => 16, 2 => 26, _ => 18 };
    internal static int Release(int step) => step switch { 1 => 6, 2 => 11, _ => 7 };
    internal static int LiveEnd(int step) => step switch { 1 => 13, 2 => 22, _ => 15 };
    internal static float BladeLength(int step, float age)
    {
        float extended = step switch { 1 => 190f, 2 => 230f, _ => 188f };
        return 140f + (extended - 140f) * EaseOut((age - Release(step)) / 2.5f);
    }

    internal static bool Live(int step, int age) => age >= Release(step) && age < LiveEnd(step);

    internal static float Angle(int step, float age, float aim, int facing)
    {
        float start = step switch { 1 => 1.2f, 2 => -1.65f, _ => -1.5f };
        float end = step switch { 1 => -1.1f, 2 => 1.75f, _ => 1.15f };
        int release = Release(step), liveEnd = LiveEnd(step), duration = Duration(step);
        // Arrival is brisk, then a short held tension beat. The slash accelerates
        // into its contact arc and leaves the wrist with a small recoil.
        float offset;
        if (age < release - 2f) offset = Lerp(start * .61f, start, EaseOut(age / (release - 2f)));
        else if (age < release) offset = start + MathF.Sin((age - release + 2f) * 2.1f) * .012f;
        else if (age < liveEnd) offset = Lerp(start, end, EaseIn((age - release) / (liveEnd - release)));
        else offset = Lerp(end, end - MathF.Sign(end - start) * (step == 2 ? .23f : .14f),
            EaseOut((age - liveEnd) / (duration - liveEnd)));
        return aim + facing * offset;
    }

    internal static float EaseOut(float x) { x = Math.Clamp(x, 0, 1); return 1 - (1 - x) * (1 - x) * (1 - x); }
    internal static float EaseIn(float x) { x = Math.Clamp(x, 0, 1); return x * x * x; }
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
