using System;
using System.Numerics;
using Convergence.Content.Encounters.FirstSeverance;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Development HP scales from the frozen two/three/four-player roster")]
    private static void DevelopmentPartyHp()
    {
        var boss = new[] { 5_000_000, 9_000_000, 13_000_000 };
        var pylon = new[] { 300_000, 500_000, 600_000 };
        for (int count = 2; count <= 4; count++)
        {
            var tuning = FirstSeverancePartyScaling.ForCount(count);
            AssertEqual((byte)count, tuning.ParticipantCount, "frozen roster count");
            AssertEqual(boss[count - 2], tuning.BossLife, "Boss tuning");
            AssertEqual(pylon[count - 2], tuning.PylonLife, "Pylon tuning");
            var loop = new FirstSeveranceLoopStateMachine(FirstSeveranceEncounterPlan.Instance,
                count, tuning.BossLife, 100, FirstSeveranceBossPhasePlan.Instance);
            loop.Advance(new(loop.State.ResolveTick, 0, 0));
            loop.Advance(new(loop.State.SubstateEnteredTick + 90, count, 0));
            loop.Advance(new(loop.State.ResolveTick, 0, 0));
            loop.Advance(new(loop.State.ResolveTick, 0, 0));
            loop.Advance(new(loop.State.LastAuthorityTick + 1, 0, int.MaxValue));
            AssertEqual(tuning.BossLife / 2, loop.State.BossLife, "scaled half-HP floor");
            AssertEqual(FirstSeveranceSubstate.CoreExposure, loop.State.Substate, "scaled overkill waits for the full score");
        }
        foreach (int count in new[] { -1, 0, 5, 255 })
            AssertThrows<ArgumentOutOfRangeException>(() => FirstSeverancePartyScaling.ForCount(count), "invalid party count");
    }

    [DomainTest("Core salvos begin on grid three with bounded immutable directions")]
    private static void CoreSalvoBounds()
    {
        var ray = FirstSeveranceGridVolley.AimCoreBeam(4000, 4000, 5000, 3440);
        var rays = new[] { ray, ray, ray, ray };
        var grid = new FirstSeveranceGridVolley(3, 100, 0, 4000, 4000, rays);
        rays[0] = ray with { DirectionX = 0, DirectionY = 1 };
        AssertEqual(1f, grid.CoreBeams[0].DirectionX, "defensive copy, never retarget live beams");
        AssertEqual(4, grid.CoreBeams.Count, "every player supported");
        for (uint serial = 1; serial < 3; serial++)
        {
            AssertEqual(0, new FirstSeveranceGridVolley(serial, 100, 0, 4000, 4000).CoreBeams.Count, "first two are grid only");
            AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(serial, 100, 0, 4000, 4000, rays), "early salvo");
        }
        AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(3, 100, 0, 4000, 4000, new[] { ray, ray, ray, ray, ray }), "bounded allocation");
        foreach (var bad in new[] { ray with { X = 4001 }, ray with { HalfWidth = 71 }, ray with { Length = 2900 },
            ray with { DirectionY = float.NaN }, ray with { DirectionX = 0 } })
            AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(3, 100, 0, 4000, 4000, new[] { bad }), "malformed core ray");
        AssertEqual(true, FirstSeveranceGridVolley.AimCoreBeam(4000, 4000, 4000, 3440).IsValid, "overlapping center has finite fallback");
    }

    [DomainTest("Core salvos use the full warning and shared active collision window")]
    private static void CoreSalvoGeometry()
    {
        var ray = FirstSeveranceGridVolley.AimCoreBeam(4000, 4000, 5000, 3440);
        var grid = new FirstSeveranceGridVolley(3, 100, 0, 4000, 4000, new[] { ray });
        AssertEqual(false, grid.CoreIntersects(159, 4500, 3440, 10, 21), "whole warning harmless");
        AssertEqual(true, grid.CoreIntersects(160, 4500, 3533, 10, 21), "144px beam inclusive edge");
        AssertEqual(false, grid.CoreIntersects(160, 4500, 3534, 10, 21), "outside thick corridor");
        AssertEqual(true, grid.Intersects(179, 4500, 3440, 10, 21), "union includes core beam");
        AssertEqual(false, grid.CoreIntersects(180, 4500, 3440, 10, 21), "cooling light harmless");
        AssertEqual(false, grid.CoreIntersects(160, 3989, 3440, 10, 21), "no attack behind source");
    }

    [DomainTest("Eclosion pose curves and connected arm joints are continuous")]
    private static void EclosionContinuity()
    {
        foreach (Func<float, float> curve in new Func<float, float>[] { EclosionPry, EclosionPeel, EclosionEmerge, EclosionUnfurl })
        {
            AssertEqual(0f, curve(0), "closed pose endpoint");
            AssertEqual(1f, curve(1), "open pose endpoint");
            float previous = 0;
            for (int n = 0; n <= 2000; n++)
            {
                float value = curve(n / 2000f);
                AssertEqual(true, float.IsFinite(value) && value >= previous - .00001f && value <= 1, "bounded monotonic pose");
                AssertEqual(true, Math.Abs(value - previous) < .005f, "no one-frame switch");
                previous = value;
            }
        }
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 oldJoint = default;
            for (int n = 0; n <= 360; n++)
            {
                float age = n / 360f, spread = EclosionUnfurl(age), pry = EclosionPry(age);
                Vector2 shoulder = new(side * (150 + 60 * spread), 12);
                Vector2 wrist = Vector2.Lerp(new(side * (150 + 305 * pry), -75 - 160 * pry), new(side * 620, 110), spread);
                Vector2 joint = Elbow(shoulder, wrist, side);
                AssertEqual(true, Math.Abs(Vector2.Distance(joint, shoulder) - 335) < .01f, "upper arm length");
                AssertEqual(true, Math.Abs(Vector2.Distance(joint, wrist) - 250) < .01f, "forearm length");
                if (n > 0) AssertEqual(true, Vector2.Distance(joint, oldJoint) < 18, "joint cannot snap");
                oldJoint = joint;
            }
        }
    }
}
