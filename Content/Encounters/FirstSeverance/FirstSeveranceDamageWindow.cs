using System;

namespace Convergence.Content.Encounters.FirstSeverance;

// Diagnostic HP progress only: never supplies damage or transition decisions.
internal sealed class FirstSeveranceDamageWindow
{
    private ulong previousSampleTick;
    private int previousSampleDamage;

    internal FirstSeveranceDamageWindow(int startingLife, ulong openTick, ulong closeTick)
    {
        if (startingLife <= 0 || closeTick <= openTick)
            throw new ArgumentOutOfRangeException(nameof(startingLife));
        StartingLife = startingLife;
        OpenTick = openTick;
        CloseTick = closeTick;
        previousSampleTick = openTick;
    }

    internal int StartingLife { get; }
    internal ulong OpenTick { get; }
    internal ulong CloseTick { get; }

    internal FirstSeveranceDamageSample Sample(ulong tick, int remainingLife)
    {
        ulong at = Math.Clamp(tick, OpenTick, CloseTick);
        int remaining = Math.Clamp(remainingLife, 0, StartingLife);
        int damage = StartingLife - remaining;
        ulong elapsed = at - OpenTick;
        ulong interval = at >= previousSampleTick ? at - previousSampleTick : 0;
        ulong left = CloseTick - at;
        var sample = new FirstSeveranceDamageSample(
            elapsed, left, damage, remaining,
            100d * damage / StartingLife,
            Rate(damage, elapsed),
            Rate(Math.Max(0, damage - previousSampleDamage), interval),
            remaining == 0 ? 0d : left > 0 ? Rate(remaining, left) : null);
        if (at >= previousSampleTick)
        {
            previousSampleTick = at;
            previousSampleDamage = damage;
        }
        return sample;
    }

    internal static double Rate(long damage, ulong ticks) => ticks == 0 ? 0 : damage * 60d / ticks;
}

internal readonly record struct FirstSeveranceDamageSample(
    ulong ElapsedTicks,
    ulong RemainingTicks,
    int EffectiveDamage,
    int RemainingLife,
    double ProgressPercent,
    double WindowDps,
    double RecentDps,
    double? RequiredDps);
