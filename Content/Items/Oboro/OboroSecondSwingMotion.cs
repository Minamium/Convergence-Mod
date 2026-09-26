using System;

namespace Convergence.Content.Items.Oboro;

// 下の振り抜きを受け、短い弧で上へ返す。Fは速度補正前の18F。
internal static class OboroSecondSwingMotion
{
    internal const float ReadyEnd = 3, AccelerationEnd = 6, CutEnd = 11, FollowEnd = 15;
    internal const float AccelerationAngle = 60, FollowAngle = -55;
    internal const float PeakSpeed = -33, ExitSpeed = -4, ReturnSpeed = -2; // 度/F

    internal static float Frame(float progress) => Math.Clamp(progress, 0, 1) * OboroComboSettings.For(1).TotalFrames;
    internal static OboroMotionPhase Phase(float progress)
    {
        float f = Frame(progress);
        return f < ReadyEnd ? OboroMotionPhase.Windup : f < AccelerationEnd ? OboroMotionPhase.Acceleration
            : f < CutEnd ? OboroMotionPhase.Cut : f < FollowEnd ? OboroMotionPhase.FollowThrough : OboroMotionPhase.Transition;
    }
    internal static float Angle(float progress)
    {
        float f = Frame(progress);
        var step = OboroComboSettings.For(1);
        float degrees;
        // 1段目終端+90度から小さく引き、逆向きの142度の弧で返す。
        // 1段目より短い構え・加速区間。大振りにせず手首を返すテンポ。
        // Hermiteの接点速度を共有し、急加速後も刀を瞬間移動させない。
        if (f < ReadyEnd) degrees = Segment(f, 0, ReadyEnd, step.StartDegrees, step.WindupDegrees, 0, 0);
        else if (f < AccelerationEnd) degrees = Segment(f, ReadyEnd, AccelerationEnd, step.WindupDegrees, AccelerationAngle, 0, PeakSpeed);
        else if (f < CutEnd) degrees = Segment(f, AccelerationEnd, CutEnd, AccelerationAngle, step.EndDegrees, PeakSpeed, ExitSpeed);
        else if (f < FollowEnd) degrees = Segment(f, CutEnd, FollowEnd, step.EndDegrees, FollowAngle, ExitSpeed, ReturnSpeed);
        else degrees = Segment(f, FollowEnd, step.TotalFrames, FollowAngle, OboroComboSettings.For(2).StartDegrees, ReturnSpeed, 0);
        return degrees * MathF.PI / 180;
    }
    // 入口は1段目の低い姿勢、出口は3段目の上段構えに接続する。
    // nativeの手に沿って根元を動かす。プレイヤー本体は移動しない。
    internal static float HandWeight(float progress)
    {
        float f = Frame(progress);
        if (f < ReadyEnd) return OboroRules.Ease(f / ReadyEnd);
        return f < FollowEnd ? 1 : 1 - OboroRules.Ease((f - FollowEnd) / (OboroComboSettings.For(1).TotalFrames - FollowEnd));
    }
    // 時間を等分する掃引では、急減速区間の途中が端点平均より速い。
    // 2段目だけ密度を2倍にし、細い敵の抜けを防ぐ。既存の上限64は維持。
    internal static int SweepSamples(float start, float end, float rootTravel)
        => Math.Clamp((int)MathF.Ceiling(Math.Max(Math.Abs(Angle(end) - Angle(start)) / .0175f, rootTravel / 2)), 1, 64);
    private static float Segment(float frame, float start, float end, float a, float b, float speedA, float speedB)
    {
        float span = end - start, t = Math.Clamp((frame - start) / span, 0, 1), t2 = t * t, t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * a + (t3 - 2 * t2 + t) * span * speedA
            + (-2 * t3 + 3 * t2) * b + (t3 - t2) * span * speedB;
    }
}
