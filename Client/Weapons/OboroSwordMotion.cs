using System;
using Convergence.Content.Items.Oboro;

namespace Convergence.Client.Weapons;

// 表示専用の手首。霊刃の軸/到達距離とサーバーの判定は既存のPoseのまま。
// 肩→腕→柄→刀身を分離し、刀と腕を一本の棒のように回さない。
internal static class OboroSwordMotion
{
    internal static float Wrist(int step, float progress)
    {
        float f = progress * OboroComboSettings.For(step).TotalFrames;
        return step switch
        {
            0 => Curve(f, 4, 8, 12, 18, .32f, -.24f),
            1 => Curve(f, 3, 6, 10, 16, -.32f, .26f),
            _ => Curve(f, 14, 18, 22, 26, .42f, -.30f)
        };
    }
    private static float Curve(float f, float load, float release, float cut, float end, float drawn, float through)
    {
        // 構えで手首を引く→腕が先行→切っ先が追い越す→手首を戻して次段へ。
        if (f < load) return drawn * OboroRules.Ease(f / Math.Min(load, 8));
        if (f < release) return drawn + (through - drawn) * OboroRules.Ease((f - load) / (release - load));
        if (f < cut) return through;
        return through * (1 - OboroRules.Ease((f - cut) / (end - cut)));
    }

    internal static OboroBladePose AtHand(OboroBladePose slash, float bodyX, float bodyY, OboroHandBasis hand, bool swinging)
    {
        if (!swinging) return slash with { Length = OboroSwingPresentation.SwordLength };
        float arm = slash.Angle + slash.Facing * Wrist(slash.Step, slash.Progress);
        var grip = hand.At(arm);
        return slash with { X = bodyX + grip.X, Y = bodyY + grip.Y, Length = OboroSwingPresentation.SwordLength };
    }
}
