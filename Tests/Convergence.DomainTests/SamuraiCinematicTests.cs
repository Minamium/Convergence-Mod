using System;
using Convergence.Client.Encounters.GhostSamurai;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Samurai summon and change-of-form cuts stay inside the authority's harmless windows")]
    private static void SamuraiCinematicWindows()
    {
        AssertEqual(true, GhostSamuraiRules.IntroActive(1) && GhostSamuraiRules.IntroActive(GhostSamuraiRules.IntroTime), "intro covers its ticks");
        AssertEqual(false, GhostSamuraiRules.IntroActive(GhostSamuraiRules.IntroTime + 1), "intro ends");
        AssertEqual(true, GhostSamuraiRules.Untouchable(0, 0) && GhostSamuraiRules.Untouchable(900, 1), "untouchable windows");
        AssertEqual(false, GhostSamuraiRules.Untouchable(GhostSamuraiRules.IntroTime + 1, 0), "touchable after the intro");
        AssertEqual(GhostSamuraiRules.IntroTime, SamuraiCinematics.Duration(SamuraiCut.Summon), "summon cut is the intro");
        AssertEqual(GhostSamuraiRules.TransitionTime, SamuraiCinematics.Duration(SamuraiCut.Phase), "phase cut is the transition");
        // The first forecast can appear only after the camera and HUD are home.
        AssertEqual(true, GhostSamuraiRules.IntroTime + GhostSamuraiRules.AttackIntervalPhase1 > SamuraiCinematics.SummonDuration, "first attack after the summon cut");
        AssertEqual(true, SamuraiCinematics.ManifestEnd < SamuraiCinematics.StanceStart && SamuraiCinematics.StanceEnd < GhostSamuraiRules.IntroTime,
            "manifest, stance and shout fit the intro");
        foreach (SamuraiCut cut in new[] { SamuraiCut.Summon, SamuraiCut.Phase, SamuraiCut.Victory, SamuraiCut.Defeat })
        {
            int end = SamuraiCinematics.Duration(cut);
            AssertEqual(true, end > 0, "cut has a length");
            foreach (var curve in new Func<SamuraiCut, float, float>[] { SamuraiCinematics.Bars, SamuraiCinematics.Camera, SamuraiCinematics.Title })
            {
                AssertEqual(0f, curve(cut, 0), $"{cut} starts at rest");
                AssertEqual(0f, curve(cut, end), $"{cut} is home at its end");
                AssertEqual(0f, curve(cut, end + 30), $"{cut} stays home");
                for (float t = -10; t <= end + 10; t += .5f)
                {
                    float v = curve(cut, t);
                    AssertEqual(true, float.IsFinite(v) && v >= 0 && v <= 1, $"{cut} curve bounded");
                }
            }
            AssertEqual(1f, SamuraiCinematics.Camera(cut, end * .5f), $"{cut} camera arrives");
            AssertEqual(true, SamuraiCinematics.Title(cut, end * .6f) > .9f, $"{cut} title shows");
        }
        AssertEqual(0f, SamuraiCinematics.Bars(SamuraiCut.None, 10), "no cut, no bars");
        AssertEqual((float)SamuraiRigMotion.DeathDuration, SamuraiCinematics.ManifestAge(0), "nothing before the summoning");
        AssertEqual(0f, SamuraiCinematics.ManifestAge(SamuraiCinematics.ManifestEnd), "whole at the end of the summoning");
    }

    [DomainTest("Samurai victory blades leave the hands and plant their tips in the seal's floor")]
    private static void SamuraiFallenBlades()
    {
        const float length = 145, floor = 1000, left = 0, right = 2560;
        foreach (int side in new[] { -1, 1 })
        foreach (float bodyX in new[] { 20f, 1280, 2540 })
        {
            float handX = bodyX + side * 80, handY = 600, angle0 = side < 0 ? 2.16f : .98f;
            var held = SamuraiCinematics.FallenBlade(SamuraiCinematics.BladeRelease, handX, handY, angle0, length, side, bodyX, floor, left, right);
            AssertEqual((handX, handY, angle0), held, "held until release");
            float lastX = handX, lastY = handY;
            for (float t = SamuraiCinematics.BladeRelease; t <= SamuraiCinematics.BladeLand + 60; t += .25f)
            {
                var (x, y, a) = SamuraiCinematics.FallenBlade(t, handX, handY, angle0, length, side, bodyX, floor, left, right);
                AssertEqual(true, float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(a), "finite blade");
                AssertEqual(true, MathF.Abs(x - lastX) < 40 && MathF.Abs(y - lastY) < 60, "continuous fall");
                lastX = x; lastY = y;
            }
            foreach (float t in new[] { (float)SamuraiCinematics.BladeLand, SamuraiCinematics.BladeLand + 4, SamuraiCinematics.BladeLand + 200 })
            {
                var (x, y, a) = SamuraiCinematics.FallenBlade(t, handX, handY, angle0, length, side, bodyX, floor, left, right);
                float tipX = x + MathF.Cos(a) * length, tipY = y + MathF.Sin(a) * length;
                AssertEqual(true, MathF.Abs(tipY - floor - SamuraiCinematics.BladeBuried) < .01f, "tip planted in the floor");
                AssertEqual(true, tipX >= left + 48 - .01f && tipX <= right - 48 + .01f, "planted inside the seal");
                AssertEqual(true, MathF.Sin(a) > .95f, "blade stands point down");
            }
        }
    }
}
