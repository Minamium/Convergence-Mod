using System;
using Convergence.Client.Encounters.GhostSamurai;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Samurai rig every attack has finite continuous blade angles in both facings")]
    private static void RigContinuous()
    {
        var combo = new SamuraiComboSnapshot(0, 1, true, 100, 100, 100, 100, 200);
        foreach (SamuraiAttack attack in Enum.GetValues<SamuraiAttack>())
        foreach (SamuraiPhase phase in Enum.GetValues<SamuraiPhase>())
        foreach (int facing in new[] { -1, 1 })
        foreach (int side in new[] { -1, 1 })
        {
            float previous = SamuraiRigMotion.Blade(attack, phase, 0, 0, side, facing, combo, false).Angle;
            for (int sub = 1; sub <= 6000; sub++)
            {
                float tick = sub / 10f;
                var pose = SamuraiRigMotion.Blade(attack, phase, tick, tick, side, facing, combo, false);
                AssertEqual(true, float.IsFinite(pose.Angle) && pose.Size is >= 1 and <= 2.101f, "finite bounded sword");
                AssertEqual(true, Math.Abs(SamuraiRigMotion.Wrap(pose.Angle - previous)) < .19f, $"no angle seam {attack} at {tick}");
                previous = pose.Angle;
            }
        }
    }
    [DomainTest("Samurai rig directional blades alternate on the existing eight fire ticks")]
    private static void RigFireAlignment()
    {
        for (int step = 0; step < GhostSamuraiRules.DirectionalSlashCount; step++)
        {
            int side = step % 2 == 0 ? 1 : -1;
            int fire = GhostSamuraiRules.DirectionalSpawnTime(step) + GhostSamuraiRules.SlashWarning;
            var before = SamuraiRigMotion.Blade(SamuraiAttack.DirectionalSlash, SamuraiPhase.Phase1, fire - .01f, fire, side, 1, default, false);
            var cut = SamuraiRigMotion.Blade(SamuraiAttack.DirectionalSlash, SamuraiPhase.Phase1, fire, fire, side, 1, default, false);
            AssertEqual(0f, before.Trail, "no active trail during warning");
            AssertEqual(1f, cut.Trail, "active trail on authority fire");
        }
        foreach (int fire in new[] { 42, 108, 174 })
            AssertEqual(1f, SamuraiRigMotion.Blade(SamuraiAttack.TripleVerticalSlash, SamuraiPhase.Phase1, fire, fire, 1, 1, default, false).Trail, "vertical authority fire");
    }
    [DomainTest("Samurai rig combo endpoints hold instead of resetting to neutral")]
    private static void RigConnectedCombos()
    {
        float[] fires = { 54, 102, 150, 198 };
        for (int i = 0; i < 3; i++)
        {
            float end = SamuraiRigMotion.Sequence(fires[i] + 12, fires, .98f, 20).Angle;
            float nextStart = SamuraiRigMotion.Sequence(fires[i + 1] - 20, fires, .98f, 20).Angle;
            AssertEqual(true, Math.Abs(end - nextStart) < .001f, "connected last endpoint");
            AssertEqual(true, Math.Abs(end - .98f) > .2f, "not neutral between cuts");
        }
        AssertEqual(.98f, SamuraiRigMotion.Sequence(240, fires, .98f, 20).Angle, "final recovery ends");
    }
    [DomainTest("Samurai rig tick history is bounded and renderer sampling is frame-rate independent")]
    private static void RigHistoryBounds()
    {
        var history = new SamuraiRigHistory(); var fight = Guid.NewGuid();
        for (ulong tick = 1; tick <= 1000; tick++)
        {
            var pose = new SamuraiRigPose(tick * 2, 200, tick, 0, 1, new(.5f, 1, 0, 1), new(1, 1, 0, 1), 0, 2, 0);
            history.Add(fight, 7, pose, tick);
            int count = history.Count;
            for (int frame = 0; frame < 8; frame++)
            {
                SamuraiRigMotion.Interpolate(pose, pose with { X = pose.X + 2 }, frame / 8f);
                history.Add(fight, 7, pose, tick); // duplicate update cannot add samples
            }
            AssertEqual(count, history.Count, "draw count cannot change history");
            AssertEqual(true, count <= 14, "bounded samples");
        }
        AssertEqual(14, history.Count, "full bounded cache");
        float last = 1;
        for (int age = 0; age <= 14; age++)
        { float opacity = SamuraiRigMotion.Fade(age); AssertEqual(true, opacity <= last && opacity >= 0, "natural fade"); last = opacity; }
        AssertEqual(0f, last, "expired completely");
    }
    [DomainTest("Samurai rig clears history on fight replacement slot reuse teleport and unload")]
    private static void RigHistoryIdentity()
    {
        var history = new SamuraiRigHistory(); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var pose = new SamuraiRigPose(100, 100, 5, 0, 1, new(1, 1, 0, 0), new(1, 1, 0, 0), 0, 0, 0);
        history.Add(a, 4, pose, 10); history.Add(a, 4, pose, 11);
        history.Add(b, 4, pose, 12); AssertEqual(1, history.Count, "same slot new fight");
        history.Add(b, 5, pose, 13); AssertEqual(1, history.Count, "new actor slot");
        history.Add(b, 5, pose with { X = 900 }, 14); AssertEqual(1, history.Count, "no teleport trail");
        history.Add(b, 5, pose with { X = 900 }, 13); AssertEqual(1, history.Count, "no stale tick append");
        history.Clear(); history.Clear(); AssertEqual(0, history.Count, "idempotent cleanup");
    }
}
