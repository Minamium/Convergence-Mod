using System;

namespace Convergence.Content.Items.Oboro;

// 大きな袈裟斬りのフィニッシュ。角度・根元・判定・演出が同じ30Fを読む。
internal static class OboroThirdSwingMotion
{
    internal const float PullEnd = 8, HoldEnd = 16, AccelerationEnd = 20, CutEnd = 25;
    internal const float AccelerationAngle = -30, FollowAngle = 110;
    internal const float PeakSpeed = 55, ExitSpeed = 7; // 度/F。他2段より大きい解放速度。
    internal const float PullDistance = -4; // 根元だけを引く。プレイヤーは移動しない。

    internal static float Frame(float progress) => Math.Clamp(progress, 0, 1) * OboroComboSettings.For(2).TotalFrames;
    internal static OboroMotionPhase Phase(float progress)
    {
        float f = Frame(progress);
        return f < PullEnd ? OboroMotionPhase.Windup : f < HoldEnd ? OboroMotionPhase.Charge
            : f < AccelerationEnd ? OboroMotionPhase.Acceleration : f < CutEnd ? OboroMotionPhase.Cut : OboroMotionPhase.FollowThrough;
    }
    internal static float Angle(float progress)
    {
        float f = Frame(progress);
        var step = OboroComboSettings.For(2);
        float degrees;
        if (f < PullEnd) degrees = Segment(f, 0, PullEnd, step.StartDegrees, step.WindupDegrees, 0, 0);
        else if (f < HoldEnd) degrees = step.WindupDegrees; // 8Fは静止。溜めと解放を明確に分ける。
        else if (f < AccelerationEnd) degrees = Segment(f, HoldEnd, AccelerationEnd, step.WindupDegrees, AccelerationAngle, 0, PeakSpeed);
        else if (f < CutEnd) degrees = Segment(f, AccelerationEnd, CutEnd, AccelerationAngle, step.EndDegrees, PeakSpeed, ExitSpeed);
        else degrees = Segment(f, CutEnd, step.TotalFrames, step.EndDegrees, FollowAngle, ExitSpeed, 0);
        // 終端で無理に180度回して戻さない。次の1段目の無害な構えで受け継ぐ。
        return degrees * MathF.PI / 180;
    }
    internal static float HandWeight(float progress)
    {
        float f = Frame(progress);
        if (f < PullEnd) return OboroRules.Ease(f / PullEnd);
        return f < CutEnd ? 1 : 1 - OboroRules.Ease((f - CutEnd) / (OboroComboSettings.For(2).TotalFrames - CutEnd));
    }
    internal static float Forward(float progress)
    {
        float f = Frame(progress), distance = OboroComboSettings.For(2).ForwardDistance;
        if (f < PullEnd) return PullDistance * OboroRules.Ease(f / PullEnd);
        if (f < HoldEnd) return PullDistance;
        if (f < AccelerationEnd) return PullDistance + (distance - PullDistance) * OboroRules.Ease((f - HoldEnd) / (AccelerationEnd - HoldEnd));
        return distance * (1 - OboroRules.Ease((f - AccelerationEnd) / (OboroComboSettings.For(2).TotalFrames - AccelerationEnd)));
    }
    // 急加速・減速の曲線を時間で掃引する。上限は既存の64分割。
    internal static int SweepSamples(float start, float end, float rootTravel)
        => Math.Clamp((int)MathF.Ceiling(Math.Max(Math.Abs(Angle(end) - Angle(start)) / .012f, rootTravel / 2)), 1, 64);

    private static float Segment(float frame, float start, float end, float a, float b, float speedA, float speedB)
    {
        float span = end - start, t = Math.Clamp((frame - start) / span, 0, 1), t2 = t * t, t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * a + (t3 - 2 * t2 + t) * span * speedA
            + (-2 * t3 + 3 * t2) * b + (t3 - t2) * span * speedB;
    }
}
