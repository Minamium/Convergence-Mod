using System;

namespace Convergence.Content.Items.Oboro;

internal enum OboroMotionPhase { Idle, Windup, Acceleration, Cut, FollowThrough, Transition, Charge }

// 調整用データ。角度は右向き・照準方向を0度とした「度」、Fは攻撃速度補正前。
// ヒット区間は0始まり [開始F, 終了F)。例: 4,13なら4～12Fが有効。
// 前進量は刀の根元を照準方向へ移す距離。プレイヤー自体は移動しない。
internal readonly record struct OboroComboStep(
    int TotalFrames, float StartDegrees, float WindupDegrees, float EndDegrees,
    int HitStartFrame, int HitEndFrame, float ForwardDistance)
{
    private const float DegreesToRadians = MathF.PI / 180;
    internal float StartAngle => StartDegrees * DegreesToRadians;
    internal float WindupAngle => WindupDegrees * DegreesToRadians;
    internal float CutEndAngle => EndDegrees * DegreesToRadians;
    internal float HitStart => HitStartFrame / (float)TotalFrames;
    internal float HitEnd => HitEndFrame / (float)TotalFrames;
    internal OboroMotionPhase Phase(float progress)
    {
        if (progress < HitStart) return OboroMotionPhase.Windup;
        if (progress >= HitEnd) return OboroMotionPhase.Transition;
        float live = (progress - HitStart) / (HitEnd - HitStart);
        if (live < .18f) return OboroMotionPhase.Acceleration;
        return live < .62f ? OboroMotionPhase.Cut : OboroMotionPhase.FollowThrough;
    }
}

internal static class OboroComboSettings
{
    internal const int Count = 3;

    // 基本値の調整場所。各段の緩急は対応するSwingMotionクラスで調整する。
    internal static OboroComboStep For(int comboIndex) => comboIndex switch
    {
        //            総F   開始    引き    終点   Hit開始 終了 前進px
        0 => new(     22,   -65,  -100,     65,       5,  15,     0), // 上段からの袈裟斬り
        1 => new(     18,    90,   102,    -40,       3,  12,     0), // 小さな切り返し
        2 => new(     30,   -65,  -130,     90,      16,  26,     8), // 大きな振り抜き
        _ => throw new ArgumentOutOfRangeException(nameof(comboIndex))
    };
}
