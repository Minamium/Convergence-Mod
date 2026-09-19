#nullable enable
using System;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.Client.Encounters.GhostSamurai;

internal readonly record struct SamuraiBladeMotion(float Angle, float Size, float Charge, float Trail);
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
        float size = heavy ? 1 + result.Charge * (attack == SamuraiAttack.FrontalCleaveShockwave ? 1.1f : .4f) : 1;
        if (attack == SamuraiAttack.FrontalCleaveShockwave && tick >= SamuraiComboRules.CleaveWindup)
            size = SamuraiComboRules.HorizontalSwordScale(tick);
        if (attack == SamuraiAttack.Phase3CircleAttack && tick >= 2 * GhostSamuraiRules.Phase3CircleStepInterval)
            result = result with { Angle = result.Angle + MathF.Sin(tick * .08f) * result.Charge * .22f };
        return result with { Angle = facing < 0 ? MathF.PI - result.Angle : result.Angle, Size = size };
    }

    internal static SamuraiBladeMotion Sequence(float tick, ReadOnlySpan<float> fires, float idle,
        float windup, bool reverse = false, bool downwardOnly = false)
    {
        float previous = idle;
        for (int i = 0; i < fires.Length; i++)
        {
            bool back = !downwardOnly && ((i & 1) != 0) != reverse;
            float start = back ? 1.30f : -2.20f, end = back ? -2.20f : 1.30f;
            float from = i == 0 ? idle : previous;
            float begin = Math.Max(i == 0 ? 0 : fires[i - 1] + 12, fires[i] - windup);
            if (tick < fires[i])
            {
                float charge = Cubic((tick - begin) / Math.Max(1, fires[i] - 4 - begin));
                return new(Mix(from, start, charge), 1, charge, 0);
            }
            float local = tick - fires[i];
            float over = end + MathF.Sign(end - start) * .16f;
            if (local < 6)
                return new(Mix(start, over, Cubic(local / 6)), 1, 1, 1);
            if (local < 12)
                return new(Mix(over, end, Out((local - 6) / 6)), 1, 1 - Smooth((local - 6) / 6), 1 - (local - 6) / 6);
            previous = end;
        }
        float recovery = Smooth((tick - fires[^1] - 12) / 18);
        return new(Mix(previous, idle, recovery), 1, 0, 0);
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
        => new(Angle(a.Angle, b.Angle, t), Mix(a.Size, b.Size, t), Mix(a.Charge, b.Charge, t), Mix(a.Trail, b.Trail, t));
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
