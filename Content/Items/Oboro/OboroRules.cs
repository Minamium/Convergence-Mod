using System;
using System.Collections.Generic;

namespace Convergence.Content.Items.Oboro;

internal static class OboroRules
{
    internal const int BaseDamage = 8400, LightTicks = 24, HeavyTicks = 42, ComboResetTicks = 90;
    internal const int ZanshinTicks = 300, FireTicks = 600, MaximumMarks = 5;
    internal const int DefenseBonus = 30;
    internal const float MovementBonus = .1f, Reach = 560, BladeWidth = 34, HeavyMultiplier = 2.2f, PhantomMultiplier = .7f;
    internal static int Duration(int step, float attackSpeed) => Math.Clamp(
        (int)MathF.Round((step == 2 ? HeavyTicks : LightTicks) / Math.Clamp(attackSpeed, .5f, 3)), 8, 84);
    internal static float Windup(int step) => step == 2 ? .48f : .16f;
    internal static bool Live(int step, float progress) => progress >= Windup(step) && progress <= .8f;
    internal static float Offset(int step, float progress)
    {
        float windup = Windup(step), p = Math.Clamp(progress, 0, 1);
        float arc;
        if (p < windup) arc = -1.55f - .22f * Ease(p / windup);
        else if (p < .8f)
        {
            float t = (p - windup) / (.8f - windup);
            // Connected Hermite segments: gather, burst through the cut, release.
            // The same curve drives the server's swept hit test and the visible blade.
            if (t < .18f) t = Hermite(t / .18f, 0, .08f, 0, .9f * .18f);
            else if (t < .62f) t = Hermite((t - .18f) / .44f, .08f, .9f, .9f * .44f, .6f * .44f);
            else t = Hermite((t - .62f) / .38f, .9f, 1, .6f * .38f, 0);
            arc = -1.77f + 3.35f * t;
        }
        else
        {
            // Light cuts hand off to the reverse cut. The heavy cut follows through
            // over the shoulder to the first stance (equivalent modulo a full turn).
            float t = (p - .8f) / .2f;
            float end = step == 2 ? MathF.Tau - 1.55f : 1.55f;
            arc = step == 2 ? 1.58f + (end - 1.58f) * Ease(t)
                : t < .2f ? 1.58f + .1f * Ease(t / .2f)
                : 1.68f + (end - 1.68f) * Ease((t - .2f) / .8f);
        }
        return step == 1 ? -arc : arc;
    }
    internal static float Ease(float t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
    private static float Hermite(float t, float start, float end, float startSpeed, float endSpeed)
        => (2 * t * t * t - 3 * t * t + 1) * start + (t * t * t - 2 * t * t + t) * startSpeed
            + (-2 * t * t * t + 3 * t * t) * end + (t * t * t - t * t) * endSpeed;
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
