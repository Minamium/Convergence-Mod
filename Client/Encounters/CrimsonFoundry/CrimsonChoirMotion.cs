using System;
using Microsoft.Xna.Framework;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Read-only accepted attack times. The visual rig never owns a hitbox or timer.
internal readonly record struct CrimsonChoirCue(float Born, float Fire, float End, int Arm, bool Broad = false);
internal readonly record struct CrimsonChoirArm(float Shoulder, float Elbow, float Wrist, float Power, float Burst);

internal static class CrimsonChoirMotion
{
    internal const float Canvas = 1280;
    internal static readonly Vector2 Heart = new(677, 418);
    internal static readonly Vector2[] Shoulders = { new(566, 350), new(755, 355), new(553, 445), new(752, 449) };
    internal static readonly Vector2[] Elbows = { new(452, 343), new(838, 367), new(500, 501), new(819, 477) };
    internal static readonly Vector2[] Wrists = { new(421, 251), new(888, 269), new(415, 552), new(894, 518) };
    internal static readonly Vector2[] Tips = { new(335, 239), new(945, 247), new(376, 616), new(938, 589) };

    internal static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    private static float Out(float x) { x = 1 - Math.Clamp(x, 0, 1); return 1 - x * x * x; }

    internal static CrimsonChoirArm Arm(int index, float age, float charge, float recoil, ReadOnlySpan<CrimsonChoirCue> cues)
    {
        float lift = 0, strike = 0, energy = 0, burst = 0;
        foreach (var cue in cues)
        {
            if ((!cue.Broad && cue.Arm != index) || age < cue.Born || age >= cue.End) continue;
            float warning = Math.Max(1, cue.Fire - cue.Born), p = Math.Clamp((age - cue.Born) / warning, 0, 1);
            float t = age - cue.Fire;
            // Rapid intake -> braked tension -> four-tick unfurl. Continuous at
            // Fire, with shaped recovery completed before descriptor expiry.
            float prepare = .80f * Out(p / .38f) + .12f * Ease((p - .38f) / .43f) + .08f * Ease((p - .81f) / .19f);
            float release = t <= 0 ? 0 : Out(t / 4f);
            float decay = t <= 0 ? 1 : 1 - Ease((t - 6) / Math.Max(7, cue.End - cue.Fire - 6));
            float pulse = t <= 0 ? 0 : Out(t / 1.8f) * MathF.Exp(-t / 11f) * decay;
            lift = Math.Max(lift, prepare * (1 - release) * decay);
            strike = Math.Max(strike, release * decay);
            energy = Math.Max(energy, (prepare * (1 - release) + .7f * release) * decay);
            burst = Math.Max(burst, pulse);
        }
        // Summoning/sacrifice have no attack descriptors: do not invent strikes.
        if (cues.IsEmpty) { lift = charge * .35f; strike = recoil * .45f; energy = charge * .45f; burst = recoil * .3f; }
        float side = (index & 1) == 0 ? -1 : 1, lower = index >= 2 ? -1 : 1;
        return new(side * (MathF.Sin(age * .025f + index * 1.7f) * .065f + lower * (lift * .72f - strike * .80f)),
            side * (MathF.Sin(age * .033f + index * 2.1f) * .09f - lift * .66f + strike * .49f),
            side * (MathF.Sin(age * .046f + index) * .07f + lift * .36f - strike * .54f), energy, burst);
    }
    internal static Vector2 Rotate(Vector2 p, float angle)
    { float s = MathF.Sin(angle), c = MathF.Cos(angle); return new(p.X * c - p.Y * s, p.X * s + p.Y * c); }
    internal static Vector2 Elbow(int arm, CrimsonChoirArm pose)
        => Shoulders[arm] + Rotate(Elbows[arm] - Shoulders[arm], pose.Shoulder);
    internal static Vector2 Wrist(int arm, CrimsonChoirArm pose)
        => Elbow(arm, pose) + Rotate(Wrists[arm] - Elbows[arm], pose.Shoulder + pose.Elbow);
    internal static Vector2 Tip(int arm, CrimsonChoirArm pose)
        => Wrist(arm, pose) + Rotate(Tips[arm] - Wrists[arm], pose.Shoulder + pose.Elbow + pose.Wrist);
    internal static float Weight(Vector2 p, int arm)
    {
        float side = (arm & 1) == 0 ? 640 - p.X : p.X - 640;
        float lateral = Ease((side - 79) / 99), divide = Ease((p.Y - 417) / 48);
        float vertical = arm < 2 ? (1 - divide) * Ease((p.Y - 135) / 55) : divide * (1 - Ease((p.Y - 664) / 73));
        return lateral * vertical;
    }
    internal static Vector2 Skin(Vector2 p, int arm, CrimsonChoirArm pose)
    {
        Vector2 first = Shoulders[arm] + Rotate(p - Shoulders[arm], pose.Shoulder);
        Vector2 forearm = Wrists[arm] - Elbows[arm];
        float elbowWeight = Ease(.30f + Vector2.Dot(p - Elbows[arm], forearm) / forearm.LengthSquared() * 1.4f);
        Vector2 second = Elbow(arm, pose) + Rotate(p - Elbows[arm], pose.Shoulder + pose.Elbow);
        Vector2 fingers = Tips[arm] - Wrists[arm];
        float wristWeight = Ease(.10f + Vector2.Dot(p - Wrists[arm], fingers) / fingers.LengthSquared());
        Vector2 third = Wrist(arm, pose) + Rotate(p - Wrists[arm], pose.Shoulder + pose.Elbow + pose.Wrist);
        return Vector2.Lerp(first, Vector2.Lerp(second, third, wristWeight), elbowWeight);
    }
}
