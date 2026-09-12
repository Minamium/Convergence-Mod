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
        => CastTension(tick, start, fire) * (1f - Window(tick, end, end + 24d));

    // Shared by the sphere depression and its connected beam throat. Fast
    // opening -> tension hold -> pressure expansion -> smooth resealing.
    internal static float CoreBore(double tick, ulong start, ulong fire, ulong end)
    {
        float opening = .72f * Arrive(tick - start, 10)
            + .28f * Window(tick, fire - 12d, fire);
        return opening * (1 - Window(tick, end, end + 20d));
    }

    internal static float Emission(double tick, ulong fire, ulong end)
        => Window(tick, fire - 2d, fire + 2d) * (1f - Window(tick, end - 2d, end + 18d));

    internal static float CastPose(double tick, ulong start, ulong fire, ulong end)
        => CastTension(tick, start, fire) * (1f - Window(tick, fire + 3d, end + 22d));

    internal static float Recoil(double tick, ulong fire, ulong end)
        => ReleaseImpulse(tick, fire, Math.Min(24, end - fire + 18d));

    internal static float Arrive(double age, double duration)
    {
        float remaining = 1 - Math.Clamp((float)(age / Math.Max(.001, duration)), 0, 1);
        return 1 - remaining * remaining * remaining * remaining;
    }

    // Rapid assembly -> a visible elastic settling beat -> last-moment loading.
    // Normalized to the EXISTING warning window; never moves its fire tick.
    internal static float CastTension(double tick, double start, double fire)
    {
        double span = Math.Max(1, fire - start), age = tick - start;
        return .68f * Arrive(age, Math.Min(9, span * .22))
            - .12f * Window(age, span * .22, span * .58)
            + .44f * Window(age, span * .78, span);
    }

    internal static float ReleaseImpulse(double tick, double fire, double recovery = 20)
        => Arrive(tick - fire, 1.5) * (1 - Window(tick, fire + 2, fire + recovery));

    // Changing emission must not multiply the entire elapsed time: that makes
    // the same fiber jump hundreds of radians on a late/long charge.
    internal static float FlowPhase(double age, float rate, float emission)
        => (float)(age % 36000) * rate + Ease(emission) * 1.4f;

    // A single non-strobing compression before release, not a modulo reset.
    internal static float PreRelease(double tick, double fire, double lead = 18, double decay = 10)
        => Window(tick, fire - lead, fire) * (1f - Window(tick, fire, fire + decay));

    internal static float Cycle(double tick, double period, double offset = 0)
    {
        double phase = tick / period + offset;
        return (float)(phase - Math.Floor(phase));
    }

    internal static float EclosionPry(float age) => .76f * Window(age, .08, .30) + .24f * Window(age, .31, .36);
    internal static float EclosionPeel(float age) => .18f * Window(age, .20, .255) + .82f * Window(age, .38, .76);
    internal static float EclosionEmerge(float age) => .16f * Window(age, .25, .30) + .84f * Window(age, .43, .73);
    internal static float EclosionUnfurl(float age) => .84f * Window(age, .53, .83) + .16f * Window(age, .88, .97);

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
