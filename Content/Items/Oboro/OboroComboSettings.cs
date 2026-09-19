using System;

namespace Convergence.Content.Items.Oboro;

internal enum OboroMotionPhase { Idle, Windup, Acceleration, Cut, FollowThrough, Transition }

// Angles are radians relative to accepted aim, before facing/direction mirroring.
// Ratios are fractions of the attack duration (24/24/42 ticks before attack speed).
// HoldOffset moves the blade root along aim, NOT the player. Zero preserves reach.
internal readonly record struct OboroComboStep(
    int BaseTicks, float HitStart, float HitEnd, int Direction,
    float StartAngle, float WindupAngle, float CutEndAngle, float ReturnAngle,
    float HoldOffset = 0)
{
    internal OboroMotionPhase Phase(float progress)
    {
        if (progress < HitStart) return OboroMotionPhase.Windup;
        if (progress > HitEnd) return OboroMotionPhase.Transition;
        float live = (progress - HitStart) / (HitEnd - HitStart);
        if (live < .18f) return OboroMotionPhase.Acceleration;
        return live < .62f ? OboroMotionPhase.Cut : OboroMotionPhase.FollowThrough;
    }
}

internal static class OboroComboSettings
{
    internal const int Count = 3;

    // Foundation only: keep the approved curves and combat timings for now.
    // Next pass: author the requested upstroke -> return -> heavy finish here,
    // then tune the shared curve in OboroRules.Offset (visual AND collision).
    internal static OboroComboStep For(int comboIndex) => comboIndex switch
    {
        0 => new(24, .16f, .8f,  1, -1.55f, -1.77f, 1.58f, 1.55f),
        1 => new(24, .16f, .8f, -1, -1.55f, -1.77f, 1.58f, 1.55f),
        2 => new(42, .48f, .8f,  1, -1.55f, -1.77f, 1.58f, MathF.Tau - 1.55f),
        _ => throw new ArgumentOutOfRangeException(nameof(comboIndex))
    };
}
