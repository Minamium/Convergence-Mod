#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Compatibility.Calamity;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Replication;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Debug assist requires console and opted-in dedicated server")]
    private static void DebugAssistAuthorityGate()
    {
        var permit = new FirstSeveranceDebugAssistPermit();
        AssertEqual(false, permit.TryArm(false, true, true, 2, 20, 100), "no SP/client grant");
        AssertEqual(false, permit.TryArm(true, false, true, 2, 20, 100), "normal launch disabled");
        AssertEqual(false, permit.TryArm(true, true, false, 2, 20, 100), "no player chat grant");
        AssertEqual(false, permit.TryArm(true, true, true, 255, 20, 100), "invalid slot");
        AssertEqual(false, permit.TryArm(true, true, true, 2, 0, 100), "invalid epoch");
        AssertEqual(false, permit.TryArm(true, true, true, 2, 20, ulong.MaxValue), "deadline overflow");
        AssertEqual(false, permit.IsArmed, "rejection leaves no permit");
        AssertEqual(true, permit.TryArm(true, true, true, 2, 20, 100), "trusted grant");
        AssertEqual(false, permit.TryArm(true, true, true, 7, 70, 100), "cannot replace pending target");
        permit.Clear();
        AssertEqual(false, permit.IsArmed, "off/unload clears pending");
    }

    [DomainTest("Debug assist permit is single use with exact epoch and deadline")]
    private static void DebugAssistPermitBoundaries()
    {
        var roster = CreatePreparationRoster();
        var fight = TestFightId("3a000000-0000-0000-0000-000000000001");
        var permit = new FirstSeveranceDebugAssistPermit();
        AssertEqual(true, permit.TryArm(true, true, true, 2, 20, 100), "arm");
        AssertEqual(true, permit.Claim(fight, roster, 101) is not null, "matching next roster");
        AssertEqual(true, permit.Claim(fight, roster, 102) is null, "one-shot, no next-fight inheritance");
        AssertEqual(true, permit.TryArm(true, true, true, 2, 21, 100), "different epoch armed");
        AssertEqual(true, permit.Claim(fight, roster, 101) is null, "reused slot cannot inherit");
        AssertEqual(false, permit.IsArmed, "nonmatching roster consumes permit");
        permit.TryArm(true, true, true, 2, 20, 100);
        AssertEqual(true, permit.Claim(fight, roster, 100 + FirstSeveranceDebugAssistPermit.ArmLifetimeTicks) is null, "exact expiry rejected");
        permit.TryArm(true, true, true, 2, 20, 100);
        AssertEqual(true, permit.Claim(FightId.None, roster, 101) is null, "requires real Fight");
    }

    [DomainTest("Debug assist lease is exact Fight/member and cleanup is final")]
    private static void DebugAssistLeaseCleanup()
    {
        var roster = CreatePreparationRoster();
        var fight = TestFightId("3a000000-0000-0000-0000-000000000001");
        var otherFight = TestFightId("3a000000-0000-0000-0000-000000000002");
        var member = roster.Members[0];
        var lease = new FirstSeveranceDebugAssistLease(fight, member);
        AssertEqual(true, lease.Protects(fight, member), "exact binding protected");
        AssertEqual(false, lease.Protects(fight, roster.Members[1]), "operator unaffected");
        AssertEqual(false, lease.Protects(otherFight, member), "other Fight rejected");
        AssertEqual(false, lease.Protects(fight, member with { ConnectionEpoch = member.ConnectionEpoch + 1 }), "rejoin rejected");
        AssertEqual(false, lease.Protects(fight, member with { ParticipantId = new ParticipantId(1) }), "wrong participant rejected");
        lease.Revoke();
        lease.Revoke();
        AssertEqual(false, lease.Protects(fight, member), "terminal/off cleanup idempotent and final");
        AssertEqual(3, roster.Count, "assist never changes roster");
    }

    [DomainTest("Stack uses missing frozen-roster fraction and full gather is free")]
    private static void StackMissingRosterFraction()
    {
        for (int count = 2; count <= 4; count++)
        {
            AssertEqual(0, FirstSeveranceCombatRules.StackDamage(1000, count, count), "full gather zero");
            AssertEqual(1000, FirstSeveranceCombatRules.StackDamage(1000, count, 0), "empty gather full fraction");
            for (int present = 1; present < count; present++)
                AssertEqual((int)Math.Ceiling(1000d * (count - present) / count),
                    FirstSeveranceCombatRules.StackDamage(1000, count, present), "missing fraction");
        }
        AssertEqual(500, FirstSeveranceCombatRules.StackDamage(1000, 2, 1), "one of two missing");
        AssertEqual(250, FirstSeveranceCombatRules.StackDamage(1000, 4, 3), "one of four missing");
        AssertEqual(1, FirstSeveranceCombatRules.StackDamage(1, 4, 3), "round fractional HP upward");
        AssertEqual(int.MaxValue, FirstSeveranceCombatRules.StackDamage(int.MaxValue, 4, 0), "no integer overflow");
    }

    [DomainTest("Prism locks prediction and reverses color order")]
    private static void PrismPredictionAndOrder()
    {
        int[] colors = { 0, 1, 2, 3, 3, 2, 1, 0 };
        for (byte step = 0; step < 8; step++)
        {
            AssertEqual(colors[step], FirstSeveranceAttackPatterns.PrismColorIndex(step), "out-and-back color order");
            var volley = FirstSeveranceAttackPatterns.Create(1, 100, FirstSeveranceSubstate.PylonCheck,
                step, 2, 1000, 2000, 50, -50);
            AssertEqual(FirstSeveranceAttackKind.PursuitPrism, volley.Kind, "prism kind");
            AssertEqual(1, volley.Rays.Count, "one global ray regardless of roster");
            AssertEqual(2, volley.TargetSlot, "authority target retained");
            FirstSeveranceLanceRay ray = volley.Rays[0];
            AssertEqual(true, ray.IsValid, "valid bounded ray");
            AssertEqual(true, Math.Abs(ray.X + ray.DirectionX * 800 - 1220) < 0.01f, "capped X prediction");
            AssertEqual(true, Math.Abs(ray.Y + ray.DirectionY * 800 - 1840) < 0.01f, "capped Y prediction");
            AssertEqual(ray, volley.RayAt(0, volley.FireTick + FirstSeveranceBeamIgnition.FullWidthTicks), "no live retargeting after ignition");
        }
    }

    [DomainTest("Dash and stillness corridors match the four-step sequence")]
    private static void DashStillnessSequence()
    {
        for (byte step = 0; step < 4; step++)
        {
            var volley = FirstSeveranceAttackPatterns.Create(1, 100, FirstSeveranceSubstate.CoreExposure,
                step, 0, 1000, 2000, 20, 0);
            if (step is 1 or 3)
            {
                AssertEqual(FirstSeveranceAttackKind.Stillness, volley.Kind, "steps two/four stop");
                AssertEqual(2, volley.Rays.Count, "two finite curtains");
                foreach (FirstSeveranceLanceRay ray in volley.Rays)
                {
                    AssertEqual(false, ray.Intersects(1000, 2000, 10, 21), "stationary box safe");
                    AssertEqual(false, ray.Intersects(1000, 2200, 10, 21), "vertical movement allowed");
                }
                AssertEqual(true, volley.Rays[0].Intersects(900, 2000, 10, 21), "moving left hits curtain");
                AssertEqual(true, volley.Rays[1].Intersects(1100, 2000, 10, 21), "moving right hits curtain");
                continue;
            }
            bool right = step == 0;
            AssertEqual(right ? FirstSeveranceAttackKind.SweepRight : FirstSeveranceAttackKind.SweepLeft,
                volley.Kind, "mirrored energy charge");
            AssertEqual(false, volley.IsFiring(volley.FireTick - 1), "warning never damages");
            bool hit = false;
            for (ulong tick = volley.StartTick + 1; tick < volley.EndTick; tick++)
            {
                volley = volley.AdvanceCharge(tick, 1000, 2000, 0, 0);
                var ray = volley.RayAt(0, tick);
                AssertEqual(true, ray.IsValid, "bounded compact body");
                AssertEqual(true, ray.Length <= 318, "no legacy full horizontal beam");
                hit |= volley.IsFiring(tick) && ray.Intersects(1000, 2000, 10, 21);
            }
            AssertEqual(true, hit, "stationary focus must evade the charge");
        }
    }

    [DomainTest("Energy charge tracks briefly, locks and cannot tunnel")]
    private static void EnergyChargeMotion()
    {
        var volley = FirstSeveranceAttackPatterns.Create(1, 100, FirstSeveranceSubstate.CoreExposure,
            0, 0, 1000, 2000, 0, 0);
        for (ulong tick = 101; tick < volley.LockTick - 2; tick++)
            volley = volley.AdvanceCharge(tick, 1000, 2000, 0, 0);
        var originalDirection = volley.Rays[0];
        volley = volley.AdvanceCharge(volley.MotionTick + 1, 1000, 2300, 0, 0);
        AssertEqual(true, originalDirection.DirectionY != volley.Rays[0].DirectionY, "actual authority homing, not a cosmetic curve");
        for (ulong tick = volley.MotionTick + 1; tick <= volley.LockTick; tick++)
            volley = volley.AdvanceCharge(tick, 1000, 2300, 0, 0);
        var locked = volley.Rays[0];
        AssertEqual(24ul, volley.FireTick - volley.LockTick, "direction cue precedes damage by 400ms");
        for (ulong tick = volley.LockTick + 1; tick < volley.FireTick; tick++)
        {
            volley = volley.AdvanceCharge(tick, 6000, -2000, -80, 80);
            AssertEqual(locked, volley.Rays[0], "origin and heading freeze throughout readable hold");
            AssertEqual(false, volley.IsFiring(tick), "prelaunch hold cannot damage");
        }
        var projected = volley.ChargeHeadAt(volley.FireTick);
        var next = volley.AdvanceCharge(volley.FireTick, 6000, -2000, -80, 80);
        AssertEqual(locked.DirectionX, next.Rays[0].DirectionX, "no post-lock reversal");
        AssertEqual(locked.DirectionY, next.Rays[0].DirectionY, "no post-lock vertical tracking");
        AssertEqual(true, Math.Abs(projected.X - next.Rays[0].X) < .01f
            && Math.Abs(projected.Y - next.Rays[0].Y) < .01f, "locked client extrapolation matches authority");
        float travelX = next.Rays[0].X - locked.X, travelY = next.Rays[0].Y - locked.Y;
        AssertEqual(true, Math.Abs(MathF.Sqrt(travelX * travelX + travelY * travelY) - 104f) < .01f, "104px per tick charge");
        var collision = next.RayAt(0, next.MotionTick);
        AssertEqual(true, collision.Intersects(locked.X + travelX * .5f, locked.Y + travelY * .5f, 10, 21), "swept body cannot tunnel");
        AssertEqual(false, collision.Intersects(locked.X - locked.DirectionX * 700,
            locked.Y - locked.DirectionY * 700, 10, 21), "long decorative wake is harmless");
        AssertEqual(next, next.AdvanceCharge(next.EndTick, 0, 0, 0, 0), "expired body cannot update");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 100, next.Rays,
            next.Kind, 0, 0, 99), "sample predates attack");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 100, next.Rays,
            next.Kind, 0, 0, next.EndTick), "sample outlives attack");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 100,
            new[] { new FirstSeveranceLanceRay(0, 0, 1, 0) }, motionTick: 100), "beam cannot carry body sample");
    }

    [DomainTest("Attack sequence deadlines and malformed descriptors")]
    private static void AttackSequenceBounds()
    {
        foreach (FirstSeveranceSubstate phase in new[] { FirstSeveranceSubstate.PylonCheck, FirstSeveranceSubstate.CoreExposure })
        {
            ulong lastEnd = 0;
            for (byte step = 0; step < FirstSeveranceAttackPatterns.StepCount(phase); step++)
            {
                ulong start = 100ul + (ulong)(step * FirstSeveranceAttackPatterns.StepCadence(phase));
                var volley = FirstSeveranceAttackPatterns.Create(1, start, phase, step, 0, 1000, 2000, 0, 0);
                AssertEqual((ulong)FirstSeveranceAttackPatterns.StepTicks(phase, step), volley.EndTick - start,
                    "scheduler reserves the actual warning and live duration");
                if (phase == FirstSeveranceSubstate.CoreExposure)
                    AssertEqual(true, lastEnd <= start, "charge/stillness keep separate collision windows");
                else if (step > 0)
                    AssertEqual(12ul, lastEnd - volley.FireTick, "main pursuit keeps twelve ticks of live overlap");
                AssertEqual(true, volley.IsFiring(volley.EndTick - 1), "last active tick");
                AssertEqual(false, volley.IsFiring(volley.EndTick), "end exclusive");
                lastEnd = volley.EndTick;
            }
            AssertEqual(100ul + (ulong)FirstSeveranceAttackPatterns.SequenceTicks(phase), lastEnd, "entire combo fits reserved phase budget");
            int window = phase == FirstSeveranceSubstate.PylonCheck
                ? FirstSeveranceEncounterPlan.Instance.Timing.PylonActiveTicks
                : FirstSeveranceEncounterPlan.Instance.Timing.PenalizedExposureTicks - FirstSeveranceAttackPatterns.ExposureOpeningRestTicks;
            AssertEqual(true, FirstSeveranceAttackPatterns.SequenceTicks(phase) <= window,
                "complete sequence fits shortest phase after initial rest");
        }
        var ray = new FirstSeveranceLanceRay(0, 0, 0, 1);
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 1, new[] { ray },
            FirstSeveranceAttackKind.Stillness, 1, 0), "stillness requires two rays");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 1, new[] { ray },
            (FirstSeveranceAttackKind)99), "unknown attack kind");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 1, new[] { ray },
            FirstSeveranceAttackKind.PursuitPrism, 8, 0), "step bound");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 1, new[] { ray with { HalfWidth = float.NaN } }), "nonfinite width");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 1, new[] { ray with { Length = 4000 } }), "length cap");
        AssertThrows<ArgumentException>(() => FirstSeveranceAttackPatterns.Create(1, 100,
            FirstSeveranceSubstate.Stack, 0, 0, 0, 0, 0, 0), "no attack during stack");
    }

    [DomainTest("Only exact-Fight participant Defeat requests normal death")]
    private static void DefeatDeathEligibility()
    {
        FightId fight = CreateContext(2).FightId;
        AssertEqual(true, FirstSeveranceCombatRules.ShouldExecuteDefeat(4, fight, 4, fight,
            EncounterEndReason.Defeat, true), "exact participant defeat");
        AssertEqual(false, FirstSeveranceCombatRules.ShouldExecuteDefeat(4, fight, 3, fight,
            EncounterEndReason.Defeat, true), "stale terminal");
        AssertEqual(false, FirstSeveranceCombatRules.ShouldExecuteDefeat(4, fight, 4, FightId.None,
            EncounterEndReason.Defeat, true), "wrong Fight");
        AssertEqual(false, FirstSeveranceCombatRules.ShouldExecuteDefeat(4, fight, 4, fight,
            EncounterEndReason.Defeat, false), "outsider or disconnected");
        foreach (EncounterEndReason reason in Enum.GetValues<EncounterEndReason>())
            if (reason != EncounterEndReason.Defeat)
                AssertEqual(false, FirstSeveranceCombatRules.ShouldExecuteDefeat(4, fight, 4, fight,
                    reason, true), "non-defeat never kills");
    }

    [DomainTest("Lance finite corridor geometry")]
    private static void LanceFiniteCorridorGeometry()
    {
        var horizontal = new FirstSeveranceLanceRay(100, 200, 1, 0);
        AssertEqual(true, horizontal.Intersects(300, 200, 10, 20), "inside horizontal beam");
        AssertEqual(true, horizontal.Intersects(300, 264, 10, 20), "touching visible edge");
        AssertEqual(false, horizontal.Intersects(300, 265, 10, 20), "beyond visible edge");
        AssertEqual(false, horizontal.Intersects(89, 200, 10, 20), "behind muzzle");
        AssertEqual(false, horizontal.Intersects(2711, 200, 10, 20), "beyond finite end");
        var vertical = new FirstSeveranceLanceRay(100, 200, 0, -1);
        AssertEqual(true, vertical.Intersects(100, -500, 10, 20), "upward beam");
        AssertEqual(false, vertical.Intersects(155, -500, 10, 20), "outside vertical beam");
        float diagonal = MathF.Sqrt(0.5f);
        var ray = new FirstSeveranceLanceRay(0, 0, diagonal, diagonal);
        AssertEqual(true, ray.Intersects(500, 500, 10, 20), "diagonal target");
        AssertEqual(false, ray.Intersects(500, 650, 10, 20), "diagonal safe lane");
        AssertEqual(FirstSeveranceLanceTuning.SpreadRadius * 2,
            FirstSeveranceLanceTuning.SpreadSeparation, "spread visual/authority boundary");
    }

    [DomainTest("Lance telegraph and active deadlines")]
    private static void LanceTelegraphAndActiveDeadlines()
    {
        var volley = new FirstSeveranceLanceVolley(1, 100, new[] { new FirstSeveranceLanceRay(0, 0, 0, 1) });
        AssertEqual(false, volley.IsFiring(99), "before assignment");
        AssertEqual(142ul, volley.FireTick, "restored fast warning deadline");
        AssertEqual(156ul, volley.EndTick, "unchanged explosive live window");
        AssertEqual(false, volley.IsFiring(volley.FireTick - 1), "last warning tick");
        AssertEqual(true, volley.IsFiring(volley.FireTick), "first firing tick");
        AssertEqual(true, volley.IsFiring(volley.EndTick - 1), "last firing tick");
        AssertEqual(false, volley.IsFiring(volley.EndTick), "end excludes damage");
        AssertEqual(false, FirstSeveranceLanceTuning.IsAttackPhase(FirstSeveranceSubstate.Stack), "stack rest");
        AssertEqual(false, FirstSeveranceLanceTuning.IsAttackPhase(FirstSeveranceSubstate.Spread), "spread rest");
    }

    [DomainTest("Fast combo cadence preserves category breathing")]
    private static void FastComboCadencePreservesCategoryBreathing()
    {
        AssertEqual(42, FirstSeveranceAttackPatterns.StepCadence(FirstSeveranceSubstate.PylonCheck), "prism starts every 0.7s");
        AssertEqual(72, FirstSeveranceAttackPatterns.StepCadence(FirstSeveranceSubstate.CoreExposure), "dash-stop combo starts every 1.2s");
        AssertEqual(376, FirstSeveranceAttackPatterns.SequenceTicks(FirstSeveranceSubstate.PylonCheck), "last sustained beam gets its complete sequence budget");
        AssertEqual(287, FirstSeveranceAttackPatterns.SequenceTicks(FirstSeveranceSubstate.CoreExposure), "four-action sequence includes last tooth's ignition and full hold");
        AssertEqual(60, FirstSeveranceAttackPatterns.SequenceRestTicks, "unchanged extra rest between full combos");
        AssertEqual(60, FirstSeveranceAttackPatterns.ExposureOpeningRestTicks, "unchanged exposure opening rest");
        var prism = FirstSeveranceAttackPatterns.Create(1, 100, FirstSeveranceSubstate.PylonCheck, 0, 0, 1000, 2000, 0, 0);
        var charge = FirstSeveranceAttackPatterns.Create(2, 100, FirstSeveranceSubstate.CoreExposure, 0, 0, 1000, 2000, 0, 0);
        var stillness = FirstSeveranceAttackPatterns.Create(3, 100, FirstSeveranceSubstate.CoreExposure, 1, 0, 1000, 2000, 0, 0);
        AssertEqual(128ul, prism.FireTick, "28-tick prism warning");
        AssertEqual(136ul, stillness.FireTick, "36-tick stillness warning");
        AssertEqual(142ul, charge.FireTick, "42-tick charge windup");
        AssertEqual(118ul, charge.LockTick, "18-tick tracking followed by the retained 24-tick harmless hold");
        var timing = FirstSeveranceEncounterPlan.Instance.Timing;
        AssertEqual(600, timing.SpawnIntroTicks, "ten-second doll capture intro after Ready");
        AssertEqual(90, timing.PylonTelegraphTicks, "pylon opening unchanged");
        AssertEqual(840, timing.PylonActiveTicks, "DPS window unchanged");
        AssertEqual(240, timing.StackTelegraphTicks, "Stack assignment window unchanged");
        AssertEqual(240, timing.SpreadTelegraphTicks, "Spread assignment window unchanged");
        AssertEqual(1080, timing.NormalExposureTicks, "normal exposure unchanged");
        AssertEqual(720, timing.PenalizedExposureTicks, "penalized exposure unchanged");
        AssertEqual(120, timing.ResetTicks, "major reset unchanged");
    }

    [DomainTest("Lance snapshot bounds and immutable rays")]
    private static void LanceSnapshotBoundsAndImmutableRays()
    {
        var rays = new[] { new FirstSeveranceLanceRay(100, 200, 0, 1) };
        var volley = new FirstSeveranceLanceVolley(1, 100, rays);
        rays[0] = default;
        AssertEqual(true, volley.Rays[0].IsValid, "copied locked direction");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(0, 100, rays), "zero serial");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, ulong.MaxValue, rays), "tick overflow");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 100,
            new FirstSeveranceLanceRay[3]), "bounded ray count");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 100,
            new[] { new FirstSeveranceLanceRay(float.NaN, 0, 0, 1) }), "nonfinite origin");
        AssertThrows<ArgumentException>(() => new FirstSeveranceLanceVolley(1, 100,
            new[] { new FirstSeveranceLanceRay(0, 0, 0, 2) }), "nonunit direction");
        var context = CreateContext(2);
        AssertThrows<ArgumentException>(() => new FirstSeveranceCombatProjection(1, context.FightId,
            FirstSeveranceSubstate.Stack, 300, 0, 0, 1000, 1000, 1, 1, 100, -680, 100, 200,
            FirstSeveranceMechanicResult.None, 0, Array.Empty<FirstSeveranceCombatParticipantProjection>(), volley),
            "lance in rest phase");
        AssertThrows<ArgumentException>(() => new FirstSeveranceCombatProjection(1, context.FightId,
            FirstSeveranceSubstate.CoreExposure, volley.EndTick - 1, 0, 0, 1000, 1000, 1, 1, 100, -680, 100, 200,
            FirstSeveranceMechanicResult.None, 0, Array.Empty<FirstSeveranceCombatParticipantProjection>(), volley),
            "lance outliving phase");
    }
}
