using System;

namespace Convergence.Content.Items.DXOboro;

// One normalized clock drives blade pose, native collision, and all visible material.
internal static class DXOboroMotion
{
    internal const int Duration = 30;
    internal const float Reach = 290f;
    internal const float Width = 22f;
    internal const int ReleaseFrame = 12;
    internal const int LiveEndFrame = 23;

    internal static bool Live(int age) => age >= ReleaseFrame && age < LiveEndFrame;

    internal static float Angle(int step, float age, float aim, int facing)
    {
        float start = step switch { 1 => 1.35f, 2 => -1.8f, _ => -1.55f };
        float end = step switch { 1 => -1.35f, 2 => 1.9f, _ => 1.25f };
        // Arrival is brisk, then a short held tension beat. The slash accelerates
        // into its contact arc and leaves the wrist with a small recoil.
        float offset;
        if (age < 5f) offset = Lerp(start * .57f, start, EaseOut(age / 5f));
        else if (age < ReleaseFrame) offset = start + MathF.Sin((age - 5f) * .6f) * .018f;
        else if (age < 22f) offset = Lerp(start, end, EaseIn((age - ReleaseFrame) / 10f));
        else offset = Lerp(end, end - MathF.Sign(end - start) * .13f, EaseOut((age - 22f) / 8f));
        return aim + facing * offset;
    }

    internal static float EaseOut(float x) { x = Math.Clamp(x, 0, 1); return 1 - (1 - x) * (1 - x) * (1 - x); }
    internal static float EaseIn(float x) { x = Math.Clamp(x, 0, 1); return x * x * x; }
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
