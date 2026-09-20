using System;
using System.Collections.Generic;

namespace Convergence.Content.Items.Oboro;

internal static class OboroRules
{
    internal const int BaseDamage = 8400;
    internal const int HoldRefreshTicks = 6, HoldTimeoutTicks = 30;
    internal const int ZanshinTicks = 300, FireTicks = 600, MaximumMarks = 5;
    internal const int DefenseBonus = 30;
    internal const float MovementBonus = .1f, Reach = 560, BladeWidth = 34, HeavyMultiplier = 2.2f, PhantomMultiplier = .7f;
    internal static int Duration(int step, float attackSpeed) => Math.Clamp(
        (int)MathF.Round(OboroComboSettings.For(step).TotalFrames / Math.Clamp(attackSpeed, .5f, 3)), 8, 84);
    internal static float Windup(int step) => OboroComboSettings.For(step).HitStart;
    internal static bool Live(int step, float progress)
        => progress >= Windup(step) && progress < OboroComboSettings.For(step).HitEnd;
    internal static float Offset(int step, float progress)
    {
        if (step == 0) return OboroFirstSwingMotion.Angle(progress);
        if (step == 1) return OboroSecondSwingMotion.Angle(progress);
        return OboroThirdSwingMotion.Angle(progress);
    }
    // 照準の受け渡しは溜めに入る前に完了。命中開始時刻とは分離する。
    internal static float EntryEnd(int step) => step == 2
        ? OboroThirdSwingMotion.PullEnd / OboroComboSettings.For(2).TotalFrames : Windup(step);
    internal static float Ease(float t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
    internal static (float X, float Y) RootOffset(int step, float progress, float aim, float bladeAngle, OboroHandBasis hand)
    {
        float forward = step == 2 ? OboroThirdSwingMotion.Forward(progress) : OboroComboSettings.For(step).ForwardDistance;
        var at = hand.At(bladeAngle);
        float weight = step switch { 0 => OboroFirstSwingMotion.HandWeight(progress),
            1 => OboroSecondSwingMotion.HandWeight(progress), _ => OboroThirdSwingMotion.HandWeight(progress) };
        return (MathF.Cos(aim) * forward + at.X * weight, MathF.Sin(aim) * forward + at.Y * weight);
    }
    internal static int FireDps(int maximumLife) => (int)Math.Min(int.MaxValue / 4L, 200L + Math.Max(0, maximumLife) / 50L);
    internal static float PhantomAngle(float incoming, int index) => incoming + MathF.PI / 2 + (index - 2) * .17f;
}

internal readonly record struct OboroMark(float Angle, int Damage);
internal sealed class OboroWounds(int slot, ulong generation)
{
    internal readonly int Slot = slot;
    internal readonly ulong Generation = generation;
    internal readonly List<OboroMark> Marks = new(OboroRules.MaximumMarks);
    internal bool Add(float angle, int damage)
    {
        if (Marks.Count >= OboroRules.MaximumMarks || !float.IsFinite(angle) || damage <= 0) return false;
        Marks.Add(new(angle, damage)); return true;
    }
}
