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
            0 => Curve(f, OboroFirstSwingMotion.ReadyEnd, OboroFirstSwingMotion.AccelerationEnd,
                OboroFirstSwingMotion.CutEnd, OboroComboSettings.For(0).TotalFrames, -.32f, .24f),
            1 => Curve(f, OboroSecondSwingMotion.ReadyEnd, OboroSecondSwingMotion.AccelerationEnd,
                OboroSecondSwingMotion.CutEnd, OboroComboSettings.For(1).TotalFrames, .26f, -.20f),
            _ => Curve(f, OboroThirdSwingMotion.HoldEnd, OboroThirdSwingMotion.AccelerationEnd,
                OboroThirdSwingMotion.CutEnd, OboroComboSettings.For(2).TotalFrames, -.42f, .30f)
        };
    }
    private static float Curve(float f, float load, float release, float cut, float end, float drawn, float through)
    {
        // Hand leads the launch; the heavy tip overtakes it during the cut.
        // The wrist then flexes in the opposite direction before relaxing.
        // This changes arm acting, not the shared sword axis or damage volume.
        if (f < load) return drawn * OboroRules.Ease(f / Math.Min(load, 6));
        float lead = load + (release - load) * .68f;
        if (f < lead) return drawn + (through - drawn) * OboroRules.Ease((f - load) / (lead - load));
        float recoil = -through * .58f;
        if (f < cut) return through + (recoil - through) * OboroRules.Ease((f - lead) / (cut - lead));
        return recoil * (1 - OboroRules.Ease((f - cut) / (end - cut)));
    }

    internal static OboroBladePose AtHand(OboroBladePose slash, float bodyX, float bodyY, OboroHandBasis hand, bool swinging)
    {
        if (!swinging) return slash with { Length = OboroSwingPresentation.SwordLength };
        float arm = slash.Angle + slash.Facing * Wrist(slash.Step, slash.Progress);
        var grip = hand.At(arm);
        return slash with { X = bodyX + grip.X, Y = bodyY + grip.Y, Length = OboroSwingPresentation.SwordLength };
    }
}
