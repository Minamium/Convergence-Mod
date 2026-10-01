#nullable enable
using System;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.Client.Encounters.GhostSamurai;

// ArmLead and Reach animate the hand independently of the blade's wrist angle.
// Their defaults keep neutral/terminal poses valid before a replicated action.
internal readonly record struct SamuraiBladeMotion(float Angle, float Size, float Charge, float Trail,
    float ArmLead = 0, float Reach = 0);
internal readonly record struct SamuraiRigPose(float X, float Y, float Age, float Lean, float Scale,
    SamuraiBladeMotion Left, SamuraiBladeMotion Right, float Hit, float Speed, float Lag);
internal readonly record struct SamuraiRigSample(SamuraiRigPose Pose, ulong Tick);

// Presentation only. Fire times come from the unchanged authority rules; the
// four envelopes change articulation, never state duration, position or damage.
internal static class SamuraiRigMotion
{
    internal const int TrailCapacity = 14, TrailLife = 14, BodyEchoes = 6, DeathDuration = 96;
    // Runtime binds the installed Luminance curves. The math fallback keeps the
    // geometry/timing harness independent of Terraria and third-party binaries.
    private static Func<float, float>? cubicCurve, outCurve;
    internal static void BindEasing(Func<float, float> cubic, Func<float, float> easeOut) { cubicCurve = cubic; outCurve = easeOut; }
    internal static void ClearEasing() { cubicCurve = null; outCurve = null; }
    internal static float Clamp(float x) => Math.Clamp(x, 0, 1);
    internal static float Smooth(float x) { x = Clamp(x); return x * x * (3 - 2 * x); }
    internal static float Cubic(float x) { x = Clamp(x); return cubicCurve is not null ? cubicCurve(x) : x < .5f ? 4 * x * x * x : 1 - MathF.Pow(-2 * x + 2, 3) / 2; }
    internal static float Out(float x) => outCurve is not null ? outCurve(Clamp(x)) : 1 - MathF.Pow(1 - Clamp(x), 3);
    internal static float Mix(float a, float b, float t) => a + (b - a) * t;
    internal static float Wrap(float x) => MathF.IEEERemainder(x, MathF.Tau);
    internal static float Angle(float a, float b, float t) => a + Wrap(b - a) * t;
    internal static float Fade(float age) => MathF.Pow(1 - Clamp(age / TrailLife), 2);

    internal static SamuraiBladeMotion Blade(SamuraiAttack attack, SamuraiPhase phase, float tick,
        float age, int side, int facing, in SamuraiComboSnapshot combo, bool transition)
    {
        float idle = side < 0 ? 2.16f : .98f;
        idle += MathF.Sin(age * .034f - side * .9f) * .045f;
        if (transition) return new(Angle(idle, side < 0 ? -2.2f : -.94f, Smooth(tick / 18)), 1, .6f, 0);
        Span<float> fires = stackalloc float[8];
        int count = 0;
        bool heavy = attack is SamuraiAttack.ChargedSlash or SamuraiAttack.GridSlash or SamuraiAttack.FrontalCleaveShockwave;
        float windup = 20;
        switch (attack)
        {
            case SamuraiAttack.DirectionalSlash:
                for (int i = side == facing ? 0 : 1; i < GhostSamuraiRules.DirectionalSlashCount; i += 2)
                    fires[count++] = GhostSamuraiRules.SlashWarning + GhostSamuraiRules.DirectionalSpawnTime(i);
                break;
            case SamuraiAttack.TripleVerticalSlash:
                for (int i = 0; i < SamuraiComboRules.VerticalCount; i++)
                    fires[count++] = SamuraiComboRules.VerticalWindup + i * SamuraiComboRules.VerticalCadence;
                windup = 28;
                break;
            case SamuraiAttack.ChargedSlash:
            case SamuraiAttack.GridSlash:
                bool grid = attack == SamuraiAttack.GridSlash;
                tick -= grid ? 0 : GhostSamuraiRules.ChargeAimTime;
                for (int i = 0; i < GhostSamuraiRules.ChargeCount(phase); i++)
                    fires[count++] = GhostSamuraiRules.ChargeWindup(0, grid) + i * GhostSamuraiRules.ChargedSlashInterval;
                windup = 42;
                break;
            case SamuraiAttack.FrontalCleaveShockwave:
                tick -= SamuraiComboRules.ApproachDuration(combo);
                fires[count++] = SamuraiComboRules.CleaveWindup;
                windup = SamuraiComboRules.CleaveWindup;
                break;
            case SamuraiAttack.Phase2DashSlash:
                for (int i = 0; i < 3; i++) fires[count++] = GhostSamuraiRules.DashApproach + GhostSamuraiRules.DashWarning + i * GhostSamuraiRules.DashCadence;
                break;
            case SamuraiAttack.Phase3CircleAttack:
                for (int i = 0; i < GhostSamuraiRules.Phase3CircleSteps; i++)
                    fires[count++] = GhostSamuraiRules.Phase3CircleTelegraphTime + i * GhostSamuraiRules.Phase3CircleStepInterval;
                break;
            default: return new(idle, 1, 0, 0);
        }
        // Work in a forward-facing basis, then mirror the angles, not the root.
        // Each side keeps its own last cut's endpoint throughout a combo.
        float basisIdle = facing < 0 ? MathF.PI - idle : idle;
        SamuraiBladeMotion result = Sequence(tick, fires[..count], basisIdle, windup,
            reverse: side != facing && attack == SamuraiAttack.DirectionalSlash,
            downwardOnly: attack is SamuraiAttack.TripleVerticalSlash or SamuraiAttack.FrontalCleaveShockwave);
        float size = 1 + result.Charge * (heavy ? .36f : .12f) + result.Trail * .25f;
        if (attack == SamuraiAttack.FrontalCleaveShockwave)
            size = SamuraiComboRules.HorizontalSwordScale(tick);
        if (attack == SamuraiAttack.Phase3CircleAttack && tick >= 2 * GhostSamuraiRules.Phase3CircleStepInterval)
            result = result with { Angle = result.Angle + MathF.Sin(tick * .08f) * result.Charge * .22f };
        return result with { Angle = facing < 0 ? MathF.PI - result.Angle : result.Angle, Size = size };
    }

    internal static SamuraiBladeMotion Sequence(float tick, ReadOnlySpan<float> fires, float idle,
        float windup, bool reverse = false, bool downwardOnly = false)
    {
        if (fires.IsEmpty) return new(idle, 1, 0, 0);
        float previous = idle;
        for (int i = 0; i < fires.Length; i++)
        {
            bool back = !downwardOnly && ((i & 1) != 0) != reverse;
            float start = back ? 1.65f : -2.70f, end = back ? -2.70f : 1.65f;
            float from = i == 0 ? idle : previous;
            float fire = fires[i];
            float begin = Math.Max(i == 0 ? 0 : fires[i - 1] + 12, fire - windup);
            float releaseStart = fire - 4;
            float arrivalEnd = Math.Min(releaseStart - 4, begin + 18);
            float priorReach = i == 0 ? 0 : 16 * (1 - Smooth((begin - fires[i - 1] - 12) / 12));
            float sign = MathF.Sign(end - start);
            if (tick < releaseStart)
            {
                // Arrival reaches a readable high silhouette; the accepted long
                // cleave charge breathes while the wrist continues to coil.
                float arrival = Out((tick - begin) / Math.Max(1, arrivalEnd - begin));
                if (tick < arrivalEnd)
                    return new(Mix(from, start - sign * .13f, arrival), 1,
                        arrival * .83f, 0, -sign * .16f * arrival, Mix(priorReach, 27, arrival));
                float tension = Clamp((tick - arrivalEnd) / Math.Max(1, releaseStart - arrivalEnd));
                float breath = MathF.Sin((tick - arrivalEnd) * .42f) * .023f * MathF.Sin(MathHelperPi * tension);
                return new(start - sign * .13f + sign * .13f * Smooth(tension) + breath, 1,
                    .83f + .17f * tension, 0, -sign * (.16f - .08f * tension), 27 + 7 * tension);
            }
            float swing = tick - releaseStart;
            float over = end + sign * .24f;
            // The blade crosses its forward/contact angle at the authoritative
            // fire tick, halfway through this eight-tick whip, with high speed.
            if (swing < 8)
            {
                float t = Cubic(swing / 8);
                float speed = MathF.Sin(MathHelperPi * Clamp(swing / 8));
                return new(Mix(start, end, t), 1, 1 - .28f * t,
                    speed, sign * (-.08f + .18f * t + .25f * speed), 34 + 12 * speed);
            }
            float local = swing - 8;
            if (local < 4)
                return new(Mix(end, over, Out(local / 4)), 1,
                    .72f * (1 - Smooth(local / 4)), 0,
                    sign * (.10f - .23f * Smooth(local / 4)), 34 - 8 * Smooth(local / 4));
            if (local < 8)
                return new(Mix(over, end, Smooth((local - 4) / 4)), 1, 0,
                    0, -sign * .13f * (1 - Smooth((local - 4) / 4)),
                    26 - 10 * Smooth((local - 4) / 4));
            if (i + 1 < fires.Length)
            {
                float nextBegin = Math.Max(fire + 12, fires[i + 1] - windup);
                if (tick < nextBegin)
                    return new(end, 1, 0, 0, 0, 16 * (1 - Smooth((tick - fire - 12) / 12)));
            }
            previous = end;
        }
        float recovery = Smooth((tick - fires[^1] - 12) / 20);
        return new(Mix(previous, idle, recovery), 1, 0, 0, 0, 16 * (1 - recovery));
    }

    private const float MathHelperPi = MathF.PI;

    // Pure two-bone solution shared by renderer and domain checks. The hand path
    // stays inside the 78+76 reach, so these are true fixed-length segments.
    internal static (float X, float Y) SolveElbow(float sx, float sy, float hx, float hy, int side, float scale)
    {
        float upper = 78 * scale, lower = 76 * scale;
        float dx = hx - sx, dy = hy - sy;
        float distance = MathF.Max(.001f, MathF.Sqrt(dx * dx + dy * dy));
        float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
        float height = MathF.Sqrt(MathF.Max(0, upper * upper - along * along));
        float ux = dx / distance, uy = dy / distance;
        return (sx + ux * along + uy * height * side,
            sy + uy * along - ux * height * side);
    }

    internal static float ActionLean(SamuraiBladeMotion left, SamuraiBladeMotion right, int facing)
    {
        // Weight transfers through the shoulders instead of rotating two arms
        // on an otherwise motionless PNG. Both handed cuts share this envelope.
        var blade = left.Trail > right.Trail ? left : right;
        if (left.Trail == right.Trail && facing < 0) blade = left;
        float angle = facing < 0 ? MathF.PI - blade.Angle : blade.Angle;
        return facing * (-.16f * Math.Max(left.Charge, right.Charge)
            + blade.Trail * (.27f + .23f * MathF.Cos(angle)));
    }

    // A display-only step into each cut: the body draws back while the wrist
    // coils, drives forward and dips through the whip, then settles. The
    // presentation smooths the return; NPC.Center and every hazard are unchanged.
    internal static (float X, float Y) Lunge(SamuraiBladeMotion left, SamuraiBladeMotion right, int facing)
    {
        float charge = Math.Max(left.Charge, right.Charge), trail = Math.Max(left.Trail, right.Trail);
        return (facing * (22 * trail - 7 * charge), 6 * trail - 2 * charge);
    }

    internal static float DashCompression(SamuraiAttack attack, float timer)
    {
        if (attack != SamuraiAttack.Phase2DashSlash) return 1;
        float local = timer % GhostSamuraiRules.DashCadence - GhostSamuraiRules.DashApproach - GhostSamuraiRules.DashWarning;
        return 1 - .065f * Smooth((local + 10) / 6) * (1 - Out(local / 5));
    }

    internal static SamuraiRigPose Interpolate(in SamuraiRigPose a, in SamuraiRigPose b, float fraction)
    {
        float t = Clamp(fraction);
        return new(Mix(a.X, b.X, t), Mix(a.Y, b.Y, t), Mix(a.Age, b.Age, t), Mix(a.Lean, b.Lean, t), Mix(a.Scale, b.Scale, t),
            LerpBlade(a.Left, b.Left, t), LerpBlade(a.Right, b.Right, t), Mix(a.Hit, b.Hit, t), Mix(a.Speed, b.Speed, t), Mix(a.Lag, b.Lag, t));
    }
    private static SamuraiBladeMotion LerpBlade(SamuraiBladeMotion a, SamuraiBladeMotion b, float t)
        => new(Angle(a.Angle, b.Angle, t), Mix(a.Size, b.Size, t), Mix(a.Charge, b.Charge, t), Mix(a.Trail, b.Trail, t),
            Mix(a.ArmLead, b.ArmLead, t), Mix(a.Reach, b.Reach, t));
}

// Bounded world-space tick history. Rendering can sample it repeatedly without
// advancing any animation or allocating a new trace. A new owner never inherits it.
internal sealed class SamuraiRigHistory
{
    private readonly SamuraiRigSample[] history = new SamuraiRigSample[SamuraiRigMotion.TrailCapacity];
    private int head;
    internal int Count { get; private set; }
    internal Guid Fight { get; private set; }
    internal int Slot { get; private set; } = -1;
    internal SamuraiRigSample At(int index) => history[(head + index) % history.Length];
    internal void Clear() { Fight = Guid.Empty; Slot = -1; Count = head = 0; Array.Clear(history); }
    internal void Add(Guid fight, int slot, SamuraiRigPose pose, ulong tick)
    {
        if (fight == Guid.Empty || !float.IsFinite(pose.X) || !float.IsFinite(pose.Y)) { Clear(); return; }
        if (Fight != fight || Slot != slot) Clear();
        Fight = fight; Slot = slot;
        if (Count > 0)
        {
            var last = At(Count - 1);
            if (tick <= last.Tick) return;
            float dx = last.Pose.X - pose.X, dy = last.Pose.Y - pose.Y;
            if (dx * dx + dy * dy > 320 * 320 || tick - last.Tick > SamuraiRigMotion.TrailLife) Count = head = 0;
        }
        while (Count > 0 && tick - At(0).Tick >= SamuraiRigMotion.TrailLife) { head = (head + 1) % history.Length; Count--; }
        if (Count == history.Length) { head = (head + 1) % history.Length; Count--; }
        history[(head + Count++) % history.Length] = new(pose, tick);
    }
}
