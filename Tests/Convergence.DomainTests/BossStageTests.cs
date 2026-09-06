using System;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static FirstSeveranceLoopStateMachine CreateStagedExposure(int count)
    {
        var loop = new FirstSeveranceLoopStateMachine(FirstSeveranceEncounterPlan.Instance,
            count, 4_000_000, 100, FirstSeveranceBossPhasePlan.Instance);
        loop.Advance(new(loop.State.ResolveTick, 0, 0));
        loop.Advance(new(loop.State.SubstateEnteredTick + 90, FirstSeveranceEncounterPlan.Instance.PylonCount.GetValue(count), 0));
        AssertEqual(FirstSeveranceSubstate.Stack, loop.State.Substate, "cleared pylons");
        loop.Advance(new(loop.State.ResolveTick, 0, 0));
        loop.Advance(new(loop.State.ResolveTick, 0, 0));
        AssertEqual(FirstSeveranceSubstate.CoreExposure, loop.State.Substate, "first exposure");
        return loop;
    }

    [DomainTest("Boss stages transition at half life without skipping the rupture")]
    private static void BossStageThresholdAndOverkill()
    {
        foreach (int count in new[] { 2, 3, 4 })
        {
            var loop = CreateStagedExposure(count);
            ulong tick = loop.State.LastAuthorityTick;
            loop.Advance(new(++tick, 0, 1_999_999));
            AssertEqual(FirstSeveranceBossPhase.Sealed, loop.State.BossPhase, "above half remains sealed");
            AssertEqual(false, loop.WillChangeStage(0), "no premature transition");
            AssertEqual(false, loop.WillChangeStage(1), "first score must finish before transition");
            loop.Advance(new(++tick, 0, 1));
            AssertEqual(FirstSeveranceSubstate.CoreExposure, loop.State.Substate, "half waits at floor");
            loop.Advance(new(++tick, 0, int.MaxValue));
            AssertEqual(2_000_000, loop.State.BossLife, "cannot reduce below floor while waiting");
            while (loop.State.BossPhase == FirstSeveranceBossPhase.Sealed)
                loop.Advance(new(loop.State.ResolveTick, 0, int.MaxValue));
            tick = loop.State.LastAuthorityTick;
            AssertEqual(FirstSeveranceSubstate.PhaseTransition, loop.State.Substate, "completed relay triggers rupture");
            AssertEqual(tick, loop.State.BossPhaseStartedTick, "one authority animation epoch");
            AssertEqual(tick + 360, loop.State.ResolveTick, "six second transformation");
            loop.Advance(new(++tick, 0, int.MaxValue));
            AssertEqual(2_000_000, loop.State.BossLife, "cinematic invulnerability");
            loop.Advance(new(loop.State.ResolveTick, 0, int.MaxValue));
            AssertEqual(FirstSeveranceSubstate.Lattice, loop.State.Substate, "transition ends in grid");
            AssertEqual(2_000_000, loop.State.BossLife, "transition deadline not damageable");
            loop.Advance(new(loop.State.LastAuthorityTick + 1, 0, int.MaxValue));
            AssertEqual(false, loop.State.IsTerminal, "Unbound cannot die or skip its score");
            AssertEqual(1_000_000, loop.State.BossLife, "second phase quarter-HP floor");

            loop = CreateStagedExposure(count);
            loop.Advance(new(loop.State.LastAuthorityTick + 1, 0, int.MaxValue));
            AssertEqual(false, loop.State.IsTerminal, "one huge hit cannot skip transformation");
            AssertEqual(2_000_000, loop.State.BossLife, "overkill clamped at phase floor");
        }
    }

    [DomainTest("Lattice, spread and blade repeat as one phase score")]
    private static void BossStageWindowAndCap()
    {
        var loop = CreateStagedExposure(2);
        loop.Advance(new(loop.State.LastAuthorityTick + 1, 0, 2_000_000));
        while (loop.State.BossPhase == FirstSeveranceBossPhase.Sealed)
            loop.Advance(new(loop.State.ResolveTick, 0, 0));
        ulong epoch = loop.State.BossPhaseStartedTick;
        loop.Advance(new(loop.State.ResolveTick, 0, 0));
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            foreach (var action in FirstSeveranceChoreography.Unbound)
            {
                AssertEqual(action.State, loop.State.Substate, "ordered lattice, spread and blade actions");
                loop.Advance(new(loop.State.ResolveTick, 0, 0));
            }
            AssertEqual(cycle, loop.State.CompletedPhaseCycles, "count full lists, not windows");
            AssertEqual(epoch, loop.State.BossPhaseStartedTick, "stage epoch persists across windows");
            AssertEqual(false, loop.State.IsTerminal, "old eight-window cap does not truncate multi-stage scores");
        }
    }

    [DomainTest("Fixed stack anchor is above the Boss and independent of players")]
    private static void FixedStackWorldAnchor()
    {
        var anchor = FirstSeveranceStackAnchor.FromGround(4000, 4000);
        AssertEqual((4000f, 3120f), anchor, "world coordinate from foundation");
        AssertEqual(true, anchor.Y < 4000 - FirstSeveranceLanceTuning.BossHeightAboveCore,
            "assembly above Boss core");
        var field = FirstSeveranceContainmentBounds.FromGround(4000, 4000);
        AssertEqual(true, anchor.Y - FirstSeveranceLanceTuning.StackRadius > field.Top,
            "complete gathering disc inside reduced field");
        AssertEqual(true, FirstSeveranceStackAnchor.Contains(4112, 3120, anchor.X, anchor.Y), "inclusive edge");
        AssertEqual(false, FirstSeveranceStackAnchor.Contains(4113, 3120, anchor.X, anchor.Y), "outside edge");
        AssertEqual((4000f, 3120f), FirstSeveranceStackAnchor.FromGround(4000, 4000), "no player dependency");
    }

    [DomainTest("Lattice warnings, cells and exact hit deadlines share bounded geometry")]
    private static void LatticeGeometryAndTiming()
    {
        var field = FirstSeveranceContainmentBounds.FromGround(4000, 4000);
        for (byte pattern = 0; pattern < 4; pattern++)
        {
            var grid = new FirstSeveranceGridVolley(1, 100, pattern, 4000, 4000);
            AssertEqual(true, grid.Rays.Count <= FirstSeveranceGridVolley.MaximumLines, "bounded ray budget");
            foreach (var ray in grid.Rays)
                AssertEqual(true, ray.IsValid, "finite full field ray");
            float x = field.Left + 40 + pattern * 40;
            float y = field.Top + 40 + ((pattern * 3) % 4) * 40;
            AssertEqual(false, grid.Intersects(159, x, y, 10, 21), "warning is harmless");
            AssertEqual(true, grid.Intersects(160, x, y, 10, 21), "crossing gives one boolean hit");
            AssertEqual(true, grid.Intersects(179, x, y, 10, 21), "last active tick");
            AssertEqual(false, grid.Intersects(180, x, y, 10, 21), "end tick harmless");
            AssertEqual(false, grid.Intersects(160, x + 80, y + 80, 10, 21), "whole body fits clear cell");
        }
        AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(1, 100, 4, 4000, 4000), "unknown pattern");
        AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(1, ulong.MaxValue, 0, 4000, 4000), "overflow");
        AssertThrows<ArgumentException>(() => new FirstSeveranceGridVolley(1, 100, 0, float.NaN, 4000), "nonfinite field");
    }

    [DomainTest("Stage snapshots reject incompatible or malformed grid state")]
    private static void StageProjectionBounds()
    {
        var fight = CreateContext(2).FightId;
        var grid = new FirstSeveranceGridVolley(1, 100, 0, 4000, 4000);
        FirstSeveranceCombatProjection Project(FirstSeveranceSubstate state,
            FirstSeveranceBossPhase phase, ulong end, ulong phaseStart, float stackX = 4000)
            => new(1, fight, state, end, 0, 0, 2_000_000, 4_000_000, 0, 0,
                stackX, 3120, 4000, 4000, FirstSeveranceMechanicResult.None, 0,
                Array.Empty<FirstSeveranceCombatParticipantProjection>(), null, phase, phaseStart, grid, 100, 0);
        AssertEqual(grid, Project(FirstSeveranceSubstate.Lattice, FirstSeveranceBossPhase.Unbound, 1000, 1).GridVolley,
            "valid bounded lattice");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceSubstate.Lattice, FirstSeveranceBossPhase.Sealed, 1000, 1), "phase mismatch");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceSubstate.Lattice, FirstSeveranceBossPhase.Unbound, 179, 1), "outliving window");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceSubstate.PhaseTransition, FirstSeveranceBossPhase.Unbound, 1000, 1), "attack during cinematic");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceSubstate.Lattice, FirstSeveranceBossPhase.Unbound, 1000, 0), "missing stage epoch");
        AssertThrows<ArgumentException>(() => Project(FirstSeveranceSubstate.Lattice, FirstSeveranceBossPhase.Unbound, 1000, 1, float.NaN), "invalid stack coordinate");
    }
}
