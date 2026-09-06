using System;
using System.Numerics;

namespace Convergence.Client.Encounters.FirstSeverance;

// Pure presentation math. No target choice, collision, packet or world access.
internal static class FirstSeveranceVisualCurves
{
    internal static float Ease(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        // Float evaluation near 1 can overshoot by a few ulps; light envelopes
        // must remain bounded even before conversion to a packed Color.
        return Math.Clamp(x * x * x * (x * (x * 6f - 15f) + 10f), 0f, 1f);
    }

    internal static float Window(double age, double start, double end)
        => Ease((float)((age - start) / Math.Max(.001, end - start)));

    internal static float Aperture(double tick, ulong start, ulong fire, ulong end)
        => Window(tick, start, fire - 3d) * (1f - Window(tick, end, end + 24d));

    internal static float Emission(double tick, ulong fire, ulong end)
        => Window(tick, fire - 2d, fire + 2d) * (1f - Window(tick, end - 2d, end + 18d));

    internal static float CastPose(double tick, ulong start, ulong fire, ulong end)
        => Window(tick, start, fire - 5d) * (1f - Window(tick, fire + 3d, end + 22d));

    internal static float Recoil(double tick, ulong fire, ulong end)
        => Window(tick, fire - 1d, fire + 3d) * (1f - Window(tick, fire + 4d, end + 18d));

    // A single non-strobing compression before release, not a modulo reset.
    internal static float PreRelease(double tick, double fire, double lead = 18, double decay = 10)
        => Window(tick, fire - lead, fire) * (1f - Window(tick, fire, fire + decay));

    internal static float Cycle(double tick, double period, double offset = 0)
    {
        double phase = tick / period + offset;
        return (float)(phase - Math.Floor(phase));
    }

    internal static float EclosionPry(float age) => Window(age, .08, .36);
    internal static float EclosionPeel(float age) => Window(age, .20, .76);
    internal static float EclosionEmerge(float age) => Window(age, .25, .73);
    internal static float EclosionUnfurl(float age) => Window(age, .53, .97);

    // Two-bone cosmetic rig: continuous shared elbows, no disconnected wrists.
    internal static Vector2 Elbow(Vector2 shoulder, Vector2 wrist, float side)
    {
        Vector2 delta = wrist - shoulder;
        float distance = Math.Clamp(delta.Length(), 86, 584);
        Vector2 direction = delta.LengthSquared() > .001f ? Vector2.Normalize(delta) : Vector2.UnitY;
        float along = (335 * 335 - 250 * 250 + distance * distance) / (2 * distance);
        float height = MathF.Sqrt(Math.Max(0, 335 * 335 - along * along));
        return shoulder + direction * along + new Vector2(-direction.Y, direction.X) * height * side;
    }

    internal static Vector2 Hermite(Vector2 from, Vector2 velocity, Vector2 to, float ticks, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float t2 = t * t, t3 = t2 * t;
        return from * (2 * t3 - 3 * t2 + 1) + velocity * ticks * (t3 - 2 * t2 + t)
            + to * (-2 * t3 + 3 * t2);
    }
}
