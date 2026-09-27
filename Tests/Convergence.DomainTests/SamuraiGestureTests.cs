using System;
using Convergence.Client.Encounters.GhostSamurai;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Samurai gesture envelopes join across arrival contact overshoot and repeated strokes")]
    private static void GestureJoins()
    {
        float[] fires = { 54, 102, 150, 198 };
        foreach (float boundary in new[] { 0f, 34f, 46f, 50f, 54f, 58f, 62f, 66f, 78f, 82f,
                     94f, 98f, 102f, 106f, 110f, 114f })
        {
            var before = SamuraiRigMotion.Sequence(boundary - .0001f, fires, .98f, 20);
            var after = SamuraiRigMotion.Sequence(boundary + .0001f, fires, .98f, 20);
            AssertEqual(true, Math.Abs(SamuraiRigMotion.Wrap(after.Angle - before.Angle)) < .005f,
                $"angle at {boundary}");
            AssertEqual(true, Math.Abs(after.Charge - before.Charge) < .005f &&
                Math.Abs(after.Trail - before.Trail) < .005f &&
                Math.Abs(after.ArmLead - before.ArmLead) < .005f &&
                Math.Abs(after.Reach - before.Reach) < .005f, $"envelope at {boundary}");
        }
    }

    [DomainTest("Samurai gesture hand has finite mirrored fixed-length two-bone articulation")]
    private static void GestureIk()
    {
        foreach (float scale in new[] { .65f, 1f, 1.2f })
        foreach (int side in new[] { -1, 1 })
        for (int i = 0; i <= 264; i++)
        {
            float tick = i / 2f;
            var blade = SamuraiRigMotion.Blade(SamuraiAttack.FrontalCleaveShockwave, SamuraiPhase.Phase1,
                tick, tick, side, 1, default, false);
            float radius = (86 + blade.Reach) * scale;
            float hx = MathF.Cos(blade.Angle + blade.ArmLead) * radius;
            float hy = MathF.Sin(blade.Angle + blade.ArmLead) * radius;
            var elbow = SamuraiRigMotion.SolveElbow(0, 0, hx, hy, side, scale);
            float upper = MathF.Sqrt(elbow.X * elbow.X + elbow.Y * elbow.Y);
            float lower = MathF.Sqrt((hx - elbow.X) * (hx - elbow.X) + (hy - elbow.Y) * (hy - elbow.Y));
            AssertEqual(true, float.IsFinite(elbow.X) && float.IsFinite(elbow.Y), "finite elbow");
            AssertEqual(true, Math.Abs(upper - 78 * scale) < .002f &&
                Math.Abs(lower - 76 * scale) < .002f, "fixed upper/lower limb lengths");
        }
        var right = SamuraiRigMotion.SolveElbow(0, 0, 110, 0, 1, 1);
        var left = SamuraiRigMotion.SolveElbow(0, 0, 110, 0, -1, 1);
        AssertEqual(true, Math.Abs(right.X - left.X) < .001f &&
            Math.Abs(right.Y + left.Y) < .001f, "mirrored elbow bend");
    }

    [DomainTest("Samurai cleave blade size stays continuous at accepted fire in both facings")]
    private static void GestureCleaveSize()
    {
        float fire = SamuraiComboRules.ApproachDuration(default) + SamuraiComboRules.CleaveWindup;
        foreach (int facing in new[] { -1, 1 })
        foreach (int side in new[] { -1, 1 })
        {
            var before = SamuraiRigMotion.Blade(SamuraiAttack.FrontalCleaveShockwave, SamuraiPhase.Phase1,
                fire - .0001f, fire, side, facing, default, false);
            var after = SamuraiRigMotion.Blade(SamuraiAttack.FrontalCleaveShockwave, SamuraiPhase.Phase1,
                fire + .0001f, fire, side, facing, default, false);
            AssertEqual(true, Math.Abs(after.Size - before.Size) < .002f, "size at authority fire");
            AssertEqual(true, after.Trail > .99f && before.Trail > .99f,
                "trail remains attached through authority fire");
        }
    }
}
