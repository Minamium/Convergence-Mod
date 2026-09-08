#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// The very same finger chains feed collision and fractional client drawing.
// Decorative crescents, pressure rings and debris never become damage volumes.
internal readonly record struct CantorClawPose(Vector2 Palm, float Angle, float Scale, float Curl, int Mirror);
internal readonly record struct CantorClawSegment(Vector2 Start, Vector2 End, float Radius);

internal static class NullCantorClawMotion
{
    internal const int BaseDamage = 7700;
    internal const int SwingTicks = 28;
    internal const int ChargeTicks = 360;
    internal const int CrushTicks = 72;
    internal const int CrushCloseTick = 28;
    internal const int CrushImpactTick = 40;
    internal const int CrushEndHitTick = 44;
    internal const float CrushMultiplier = 4.2f;
    internal const float TargetRange = 1120;
    internal const float SweepStart = .20f, SweepEnd = .72f;
    internal const float MaximumReach = 560;
    internal const float CrushRadiusX = 166, CrushRadiusY = 132;

    internal static float Smooth(float t)
    {
        if (!float.IsFinite(t)) return 0;
        t = Math.Clamp(t, 0, 1);
        return Math.Clamp(t * t * t * (10 + t * (-15 + t * 6)), 0, 1);
    }
    internal static float Envelope(float t, float rise, float hold, float end)
        => Smooth(t / rise) * (1 - Smooth((t - hold) / (end - hold)));
    internal static int Duration(float speed) => Math.Clamp((int)MathF.Round(SwingTicks /
        Math.Clamp(float.IsFinite(speed) ? speed : 1, .25f, 4)), 10, 90);
    internal static bool SwingLive(float p) => float.IsFinite(p) && p >= SweepStart && p < SweepEnd;
    internal static bool CrushLive(float age) => float.IsFinite(age) && age >= CrushImpactTick && age < CrushEndHitTick;
    internal static float SwingAngle(float p, int hand)
    {
        p = float.IsFinite(p) ? Math.Clamp(p, 0, 1) : 0;
        float stroke = Smooth((p - SweepStart) / (SweepEnd - SweepStart));
        float anticipation = p < SweepStart ? .20f * MathF.Pow(MathF.Sin(p / SweepStart * MathF.PI), 2) : 0;
        return (hand == 0 ? 1 : -1) * (-2.15f + 4.30f * stroke - anticipation);
    }
    internal static CantorClawPose SwingPose(float p, int hand)
    {
        float growth = Envelope(p, .24f, .57f, 1);
        float scale = .68f + 1.62f * growth;
        float angle = SwingAngle(p, hand);
        float curl = .18f + .72f * Smooth((p - .22f) / .45f);
        return new(Unit(angle) * (74 + 54 * scale), angle, scale, curl, hand == 0 ? 1 : -1);
    }
    internal static CantorClawPose CrushPose(float age, int hand)
    {
        float emerge = Smooth(age / 18), close = Smooth((age - CrushCloseTick) / (CrushImpactTick - CrushCloseTick));
        float depart = Smooth((age - 49) / 23);
        float side = hand == 0 ? -1 : 1;
        return new(new Vector2(side * (420 - 266 * close + 90 * depart), -24 * MathF.Sin(close * MathF.PI)),
            hand == 0 ? .12f * (1 - close) : MathF.PI - .12f * (1 - close),
            (1.2f + 1.9f * emerge) * (1 - .24f * depart), .12f + .88f * close, hand == 0 ? 1 : -1);
    }
    internal static Vector2 Joint(in CantorClawPose pose, int finger, int joint)
    {
        finger = Math.Clamp(finger, 0, 4); joint = Math.Clamp(joint, 0, 3);
        Vector2 p = finger switch
        {
            0 => new(-22, -28), 1 => new(8, -29), 2 => new(23, -8), 3 => new(18, 17), _ => new(-3, 33),
        };
        float angle = finger switch { 0 => -.86f, 1 => -.38f, 2 => -.07f, 3 => .23f, _ => .54f };
        for (int part = 0; part < joint; part++)
        {
            float length = (finger == 0 ? 33 : finger == 2 ? 42 : 37) - part * 3;
            float bend = finger < 2 ? 1 : -1;
            angle += part * bend * (.22f + pose.Curl * .50f);
            p += Unit(angle) * length;
        }
        p.Y *= pose.Mirror;
        return pose.Palm + Rotate(p * pose.Scale, pose.Angle);
    }
    internal static CantorClawSegment Segment(in CantorClawPose pose, int finger, int segment)
        => new(Joint(pose, finger, segment), Joint(pose, finger, segment + 1),
            (segment == 2 ? 6 : 10) * pose.Scale);
    internal static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    internal static Vector2 Rotate(Vector2 p, float angle)
        => new(p.X * MathF.Cos(angle) - p.Y * MathF.Sin(angle), p.X * MathF.Sin(angle) + p.Y * MathF.Cos(angle));
    internal static bool Finite(Vector2 p) => float.IsFinite(p.X) && float.IsFinite(p.Y);
    internal static Vector2 ClampTarget(Vector2 origin, Vector2 requested)
    {
        if (!Finite(origin) || !Finite(requested)) return origin;
        Vector2 delta = requested - origin;
        return origin + delta * Math.Min(1, TargetRange / Math.Max(.001f, delta.Length()));
    }
    internal static bool CrushHits(Vector2 delta, Vector2 halfSize)
    {
        // Exact ellipse vs AABB: test the nearest point, not an expanded square.
        float x = Math.Max(0, Math.Abs(delta.X) - halfSize.X) / CrushRadiusX;
        float y = Math.Max(0, Math.Abs(delta.Y) - halfSize.Y) / CrushRadiusY;
        return x * x + y * y <= 1;
    }
}

// One charge belongs to the player, not to a stack/clone of the item.
// Holding recharges in real game ticks. Swapping freezes rather than refills it.
internal sealed class NullCantorClawCharge
{
    internal int Ticks { get; private set; }
    internal bool Ready => Ticks == NullCantorClawMotion.ChargeTicks;
    internal void Tick(bool held, bool usable, bool crushing)
    {
        if (held && usable && !crushing && !Ready) Ticks++;
    }
    internal bool TrySpend()
    {
        if (!Ready) return false;
        Ticks = 0; return true;
    }
    internal void Reset() => Ticks = 0;
}
