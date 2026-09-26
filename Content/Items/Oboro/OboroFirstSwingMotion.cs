using System;

namespace Convergence.Content.Items.Oboro;

// 上段から前方下へ落とす袈裟斬り。Fは速度補正前の22F。
internal static class OboroFirstSwingMotion
{
    internal const float ReadyEnd = 5, AccelerationEnd = 9, CutEnd = 14, FollowEnd = 19;
    internal const float AccelerationAngle = -25, FollowAngle = 85;
    internal const float PeakSpeed = 34, ExitSpeed = 4, ReturnSpeed = 1; // 度/F

    internal static float Frame(float progress) => Math.Clamp(progress, 0, 1) * OboroComboSettings.For(0).TotalFrames;
    internal static OboroMotionPhase Phase(float progress)
    {
        float f = Frame(progress);
        return f < ReadyEnd ? OboroMotionPhase.Windup : f < AccelerationEnd ? OboroMotionPhase.Acceleration
            : f < CutEnd ? OboroMotionPhase.Cut : f < FollowEnd ? OboroMotionPhase.FollowThrough : OboroMotionPhase.Transition;
    }
    internal static float Angle(float progress)
    {
        float f = Frame(progress);
        var step = OboroComboSettings.For(0);
        float degrees;
        // 接点速度を共有するHermite補間。上で止め、9F付近で最速、下で減速。
        if (f < ReadyEnd) degrees = Segment(f, 0, ReadyEnd, step.StartDegrees, step.WindupDegrees, 0, 0);
        else if (f < AccelerationEnd) degrees = Segment(f, ReadyEnd, AccelerationEnd, step.WindupDegrees, AccelerationAngle, 0, PeakSpeed);
        else if (f < CutEnd) degrees = Segment(f, AccelerationEnd, CutEnd, AccelerationAngle, step.EndDegrees, PeakSpeed, ExitSpeed);
        else if (f < FollowEnd) degrees = Segment(f, CutEnd, FollowEnd, step.EndDegrees, FollowAngle, ExitSpeed, ReturnSpeed);
        else degrees = Segment(f, FollowEnd, step.TotalFrames, FollowAngle, OboroComboSettings.For(1).StartDegrees, ReturnSpeed, 0);
        return degrees * MathF.PI / 180;
    }
    // 刀の根元をプレイヤー中心から実際の手へ移す。引いた手が斬撃で前に出る。
    // 最初と最後は既存の持ち位置に接続。プレイヤー本体を移動させない。
    internal static float HandWeight(float progress)
    {
        float f = Frame(progress);
        if (f < ReadyEnd) return OboroRules.Ease(f / ReadyEnd);
        return f < FollowEnd ? 1 : 1 - OboroRules.Ease((f - FollowEnd) / (OboroComboSettings.For(0).TotalFrames - FollowEnd));
    }
    private static float Segment(float frame, float start, float end, float a, float b, float speedA, float speedB)
    {
        float span = end - start, t = Math.Clamp((frame - start) / span, 0, 1), t2 = t * t, t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * a + (t3 - 2 * t2 + t) * span * speedA
            + (-2 * t3 + 3 * t2) * b + (t3 - t2) * span * speedB;
    }
}

// Native hand APIの3サンプルから作る回転基底。描画とサーバー判定が同じ値を使う。
internal readonly record struct OboroHandBasis(float X, float Y, float AlongX, float AlongY, float AcrossX, float AcrossY)
{
    internal (float X, float Y) At(float angle) => (X + MathF.Cos(angle) * AlongX + MathF.Sin(angle) * AcrossX,
        Y + MathF.Cos(angle) * AlongY + MathF.Sin(angle) * AcrossY);
}
