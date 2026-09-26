using System;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.Client.Encounters.FirstSeverance;

// A material projection, not another encounter flag. The first complete Distant
// cycle necessarily contains the crush, so late peers need no remembered event.
internal readonly record struct FirstSeveranceCoreRupture(bool Struck, float Age)
{
    internal const int BurstDelay = 18;
    internal const int SettledAge = 120;
    internal static FirstSeveranceCoreRupture Settled => new(true, SettledAge);

    internal static FirstSeveranceCoreRupture At(FirstSeveranceBossPhase phase,
        FirstSeveranceSubstate action, int completedCycles, double actionAge)
    {
        if (phase == FirstSeveranceBossPhase.Final ||
            phase == FirstSeveranceBossPhase.Distant && completedCycles > 0)
            return Settled;
        if (phase != FirstSeveranceBossPhase.Distant || action != FirstSeveranceSubstate.RemoteCrush ||
            actionAge < FirstSeveranceScoreGeometry.CrushImpactTick)
            return default;
        return new(true, (float)Math.Clamp(actionAge - FirstSeveranceScoreGeometry.CrushImpactTick, 0, SettledAge));
    }

    internal float Cracks => Struck ? Smooth(Age / 14) : 0;
    internal float BurstAge => Struck ? Math.Max(0, Age - BurstDelay) : 0;
    internal float MetalOpacity => Struck ? 1 - Smooth((Age - 35) / 50) : 1;
    internal float Energy => Struck ? Smooth((Age - BurstDelay) / 12) : 0;
    internal float Compression => Struck ? (.12f * MathF.Exp(-Age / 5) + .07f * Cracks)
        * (1 - Smooth((Age - BurstDelay) / 12)) : 0;
    internal float Flash => Struck ? .6f * MathF.Exp(-Age / 4) +
        (Age >= BurstDelay ? MathF.Exp(-(Age - BurstDelay) / 6) : 0) : 0;
    private static float Smooth(float t) { t = Math.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
}
