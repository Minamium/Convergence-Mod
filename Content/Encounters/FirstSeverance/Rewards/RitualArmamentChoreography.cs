#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// One animation/shot geometry for owner spawning, replicas and client apparatus.
// Enlarging an ornamental assembly does not change a projectile's damage budget.
internal static class RitualArmamentChoreography
{
    internal const int ChoirCycle = 108;
    internal const int VerdictLock = 16, VerdictHit = 28, VerdictEndHit = 31, VerdictDuration = 56;
    internal const float VerdictRadius = 185;
    internal static float Smooth(float t) => NullCantorClawMotion.Smooth(t);
    internal static float Pulse(float t, float rise, float release, float end)
        => NullCantorClawMotion.Envelope(t, rise, release, end);
    internal static float Response(float rate, float dt)
        => !float.IsFinite(dt) || dt <= 0 ? 0 : 1 - MathF.Exp(-rate * Math.Min(dt, 4));
    internal static Vector2 CannonRoot(int lane) => Math.Clamp(lane, 0, 2) switch
    {
        0 => new(-116, -134), 1 => new(-192, -8), _ => new(-108, 118),
    };
    internal static Vector2 CannonMuzzle(int lane) => CannonRoot(lane) + new Vector2(224, 0);
    internal static Vector2 LensMuzzle(int lane) => new(148 + Math.Clamp(lane, 0, 2) * 57,
        (Math.Clamp(lane, 0, 2) - 1) * 58 - 18);
    internal static float MagicRayShare(int cast) => cast % 4 == 3 ? 1.20f : .60f;
    internal static float RangedRayShare(int shot) => RitualArmamentRules.RangedMultiplier(shot) / 3;
    internal static float ChoirAssembly(float age)
    {
        float phase = Mod(age, ChoirCycle);
        return Smooth((phase - 52) / 27) * (1 - Smooth((phase - 98) / 10));
    }
    internal static int ChoirShotAt(int phase, int ordinal)
    {
        int stagger = Math.Abs(ordinal % 6) * 2;
        return phase == 8 + stagger ? 0 : phase == 44 + stagger ? 1 : phase == 88 ? 2 : -1;
    }
    internal static Vector2 ChoirSeat(int ordinal, int count)
    {
        count = Math.Clamp(count, 1, 40); ordinal = Math.Clamp(ordinal, 0, count - 1);
        float x = (ordinal - (count - 1) * .5f) * Math.Min(72, 560f / count);
        return new(x, Math.Abs(x) * .23f);
    }
    internal static float Mod(float value, float period) => ((value % period) + period) % period;
    internal static Vector2 Triangle(int corner, float radius, float angle = -.5f * MathF.PI)
        => NullCantorClawMotion.Unit(angle + corner * MathF.Tau / 3) * radius;
    internal static bool VerdictLive(float age) => age >= VerdictHit && age < VerdictEndHit;
    // SAT triangle vs AABB: a large drawn triangle has its real finite triangular footprint.
    internal static bool TriangleHits(Vector2 relativeCenter, Vector2 halfSize, float radius)
    {
        if (!RitualArmamentRules.Finite(relativeCenter) || !RitualArmamentRules.Finite(halfSize)
            || halfSize.X < 0 || halfSize.Y < 0 || !float.IsFinite(radius) || radius <= 0) return false;
        Span<Vector2> v = stackalloc Vector2[3];
        for (int i = 0; i < 3; i++) v[i] = Triangle(i, radius);
        for (int axis = 0; axis < 5; axis++)
        {
            Vector2 n;
            if (axis < 2) n = axis == 0 ? Vector2.UnitX : Vector2.UnitY;
            else { Vector2 edge = v[(axis - 1) % 3] - v[axis - 2]; n = new(-edge.Y, edge.X); }
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < 3; i++) { float d = Vector2.Dot(v[i], n); lo = Math.Min(lo, d); hi = Math.Max(hi, d); }
            float center = Vector2.Dot(relativeCenter, n), extent = Math.Abs(n.X) * halfSize.X + Math.Abs(n.Y) * halfSize.Y;
            if (center + extent < lo || center - extent > hi) return false;
        }
        return true;
    }
    // Presentation-only world-space hand parked pose; same endpoints for every swipe.
    internal static CantorClawPose ParkedHand(int hand, float time, int facing = 1)
    {
        float side = hand == 0 ? -1 : 1;
        float a = hand == 0 ? 2.4f : .74f;
        return new(new(side * 31 * facing, 3 + MathF.Sin(time * .033f + hand) * 2),
            facing < 0 ? MathF.PI - a : a, .44f, .24f + .035f * MathF.Sin(time * .027f + hand), (hand == 0 ? 1 : -1) * facing);
    }
    internal static CantorClawPose WorldPose(in CantorClawPose local, float aim, int facing)
        => new(NullCantorClawMotion.Rotate(new(local.Palm.X, local.Palm.Y * facing), aim),
            aim + local.Angle * facing, local.Scale, local.Curl, local.Mirror * facing);
    internal static CantorClawPose Mix(in CantorClawPose a, in CantorClawPose b, float amount)
    {
        amount = Smooth(amount);
        float angle = a.Angle + MathF.IEEERemainder(b.Angle - a.Angle, MathF.Tau) * amount;
        return new(Vector2.Lerp(a.Palm, b.Palm, amount), angle, a.Scale + (b.Scale - a.Scale) * amount,
            a.Curl + (b.Curl - a.Curl) * amount, b.Mirror);
    }
    internal static CantorClawPose PresentedHand(float progress, int activeHand, int hand, float aim, int facing, float time)
    {
        CantorClawPose rest = ParkedHand(hand, time, facing);
        if (hand != activeHand)
        {
            // The opposite hand breathes and subtly braces; no fixed .91 end-of-swing pose.
            return rest with { Curl = rest.Curl + .13f * Pulse(progress, .20f, .68f, 1) };
        }
        CantorClawPose pose = WorldPose(NullCantorClawMotion.SwingPose(progress, hand), aim, facing);
        if (progress < NullCantorClawMotion.SweepStart)
            return Mix(rest, pose, progress / NullCantorClawMotion.SweepStart);
        if (progress > NullCantorClawMotion.SweepEnd)
            return Mix(pose, rest, (progress - NullCantorClawMotion.SweepEnd) / (1 - NullCantorClawMotion.SweepEnd));
        return pose; // Exact shared collision pose throughout the active swing.
    }
}
