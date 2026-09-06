using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static void FullChoreographyAndFinal()
    {
        foreach (int count in new[] { 1, 2, 3, 4 })
        {
            var loop = CreateStagedExposure(count);
            var visited = new HashSet<(FirstSeveranceBossPhase, int)>();
            int limit = 0, finalStacks = 0;
            while (!loop.State.IsTerminal && ++limit < 180)
            {
                var before = loop.State;
                if (before.ActionIndex >= 0) visited.Add((before.BossPhase, before.ActionIndex));
                if (before.BossPhase == FirstSeveranceBossPhase.Final && before.Substate == FirstSeveranceSubstate.Stack) finalStacks++;
                if (before.BossPhase == FirstSeveranceBossPhase.Final)
                    AssertEqual(0, before.BossLife, "terminal survival is truly logical HP zero");
                var result = loop.Advance(new(before.ResolveTick, 0, int.MaxValue));
                AssertEqual(false, result.Disposition == FirstSeveranceLoopUpdateDisposition.Rejected, "valid action deadline");
                if (loop.State.BossPhase != before.BossPhase)
                    for (int i = 0; i < FirstSeveranceChoreography.For(before.BossPhase).Count; i++)
                        AssertEqual(true, visited.Contains((before.BossPhase, i)), "no skipped action despite overkill");
                if (!loop.State.IsTerminal)
                {
                    var sameTick = loop.Advance(new(loop.State.LastAuthorityTick, 0, int.MaxValue));
                    AssertEqual(FirstSeveranceLoopUpdateDisposition.Rejected, sameTick.Disposition, "duplicate tick cannot advance phase");
                }
            }
            AssertEqual(true, loop.State.IsTerminal, "finite final score wins");
            AssertEqual(FirstSeveranceBossPhase.Final, loop.State.BossPhase, "never dies in phases one to three");
            AssertEqual(8, finalStacks, "all eight clockwise gatherings required");
            var terminal = loop.State;
            loop.Advance(new(terminal.LastAuthorityTick + 1, 0, int.MaxValue));
            AssertEqual(terminal, loop.State, "terminal cannot resurrect a phase");
        }
    }

    private static void ClockAndTravelBudgets()
    {
        AssertEqual(640f, FirstSeveranceLanceTuning.SpreadSeparation, "40-tile separation");
        var field = FirstSeveranceContainmentBounds.FromGround(4000, 4000);
        foreach (var phase in new[] { FirstSeveranceBossPhase.Sealed, FirstSeveranceBossPhase.Final })
        {
            var score = FirstSeveranceChoreography.For(phase);
            double previousAngle = -Math.PI;
            int points = 0;
            for (int step = 0; step < score.Count; step++)
            {
                if (score[step].State != FirstSeveranceSubstate.Stack) continue;
                var p = FirstSeveranceChoreography.StackPosition(4000, 4000, phase, step);
                AssertEqual(true, p.X - 112 > field.Left && p.X + 112 < field.Right && p.Y - 112 > field.Top && p.Y + 112 < field.Bottom, "whole valid disc fits");
                double angle = Math.Atan2((p.Y-3440)/300, (p.X-4000)/560);
                if (angle < previousAngle) angle += Math.Tau;
                AssertEqual(true, angle > previousAngle, "clockwise top/right/bottom/left progression");
                previousAngle = angle;
                // Four practical spread stations: x +/-330, y +/-330 around field center.
                // Nearest-neighbor distance 660 > 640; worst next Stack travel is budgeted.
                foreach (int x in new[] {-330,330}) foreach (int y in new[] {-330,330})
                {
                    double distance = Math.Sqrt(Math.Pow(p.X-4000-x,2)+Math.Pow(p.Y-3440-y,2));
                    double ticks = Math.Max(0,distance-112)/10 + 30 + 6;
                    AssertEqual(true, ticks < score[step].Ticks, "10 px/tick + .5s reaction + .1s settle fits");
                }
                points++;
            }
            AssertEqual(phase == FirstSeveranceBossPhase.Final ? 8 : 4, points, "clock point count");
        }
        float travelSpeed=180*MathF.Tau*FirstSeveranceChoreography.BladeTurns/FirstSeveranceChoreography.BladeSpin*1.45f;
        AssertEqual(true, travelSpeed > 10 && travelSpeed < 11, "faster two-turn sweep requires a tighter inner orbit");
        AssertEqual(2, FirstSeveranceScoreGeometry.BladeCount, "two opposite blades");
    }

    private static void ScoreHazardBoundaries()
    {
        foreach (var state in new[] {FirstSeveranceSubstate.RotatingBlade,FirstSeveranceSubstate.RemoteClaws,
            FirstSeveranceSubstate.HalfField,FirstSeveranceSubstate.RemoteCrush,FirstSeveranceSubstate.FinalSlicer})
        {
            int end=state switch { FirstSeveranceSubstate.FinalSlicer => 240,
                FirstSeveranceSubstate.HalfField or FirstSeveranceSubstate.RemoteCrush => 300,
                FirstSeveranceSubstate.RotatingBlade => FirstSeveranceChoreography.BladeEnd + FirstSeveranceChoreography.BladeRecovery, _ => 600 };
            foreach (var ray in FirstSeveranceScoreGeometry.Rays(state,0,0,4000,4000)) AssertEqual(false,ray.Live,"entry warning harmless");
            AssertEqual(0,FirstSeveranceScoreGeometry.Rays(state,0,end,4000,4000).Count,"no lingering hazards after deadline");
            for (int tick=0;tick<end;tick++)
            {
                var rays=FirstSeveranceScoreGeometry.Rays(state,0,tick,4000,4000);
                AssertEqual(true,rays.Count<=17,"bounded geometry");
                foreach(var r in rays) AssertEqual(true,float.IsFinite(r.Ray.X)&&float.IsFinite(r.Ray.Y)&&r.Ray.Length>0,"finite rays");
            }
        }
        AssertEqual(false,FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RotatingBlade,2,179,4000,4000)[0].Live,"last warning tick");
        AssertEqual(true,FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RotatingBlade,2,180,4000,4000)[0].Live,"first live tick");
        AssertEqual(false,FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RotatingBlade,2,480,4000,4000)[0].Live,"rotation end harmless");
        AssertEqual(true,Math.Abs(FirstSeveranceScoreGeometry.BladeAngle(480)-FirstSeveranceScoreGeometry.BladeAngle(180)-2*MathF.Tau)<.001,"two exact revolutions");
        AssertEqual(0,FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RotatingBlade,2,180,4000,4000)[0].Pulse,"first turn hit identity");
        AssertEqual(2,FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RotatingBlade,2,479,4000,4000)[0].Pulse,"second turn may hit again");
        AssertEqual(0f,FirstSeveranceScoreGeometry.BladeExtension(144),"blade held before draw");
        AssertEqual(1f,FirstSeveranceScoreGeometry.BladeExtension(156),"12 tick unsheathing complete before harm");
        AssertEqual(true,FirstSeveranceScoreGeometry.BladeAngle(419)-FirstSeveranceScoreGeometry.BladeAngle(418)
            > 2*(FirstSeveranceScoreGeometry.BladeAngle(181)-FirstSeveranceScoreGeometry.BladeAngle(180)),"angular acceleration");
        var half=FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.HalfField,1,180,4000,4000)[0].Ray;
        AssertEqual(true,half.Intersects(3300,3440,10,21),"highlighted half hit");
        AssertEqual(false,half.Intersects(4700,3440,10,21),"other half safe");
        for(int tick=0;tick<240;tick++)
        {
            var bullets=FirstSeveranceScoreGeometry.Bullets(2,tick,4000,4000);
            AssertEqual(true,bullets.Count<=120,"bounded dense bullets");
            foreach(var b in bullets)
                AssertEqual(b.Live,FirstSeveranceScoreGeometry.BulletHits(b,b.X,b.Y,10,21),"bullet damage only after warning");
        }
        AssertEqual(0,FirstSeveranceScoreGeometry.Bullets(2,240,4000,4000).Count,"no final bullets after action");
        var crushWarning=FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RemoteCrush,8,161,4000,4000)[0];
        var crush=FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RemoteCrush,8,162,4000,4000)[0];
        AssertEqual(false,FirstSeveranceScoreGeometry.RayHits(FirstSeveranceSubstate.RemoteCrush,crushWarning,161,4000,3440,10,21),"closing approach harmless");
        AssertEqual(true,FirstSeveranceScoreGeometry.RayHits(FirstSeveranceSubstate.RemoteCrush,crush,162,4000,3440,10,21),"crush center live at contact");
        AssertEqual(false,crush.Ray.Intersects(4500,3440,10,21),"crush side pocket safe");
        AssertEqual(false,crush.Ray.Intersects(4000,3040,10,21),"crush upper pocket safe");
        AssertEqual(1f,FirstSeveranceScoreGeometry.CrushClosure(162),"hands closed at first damaging tick");
        AssertEqual(false,FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.RemoteCrush,8,180,4000,4000)[0].Live,"crush recovery harmless");
        AssertEqual(true,FirstSeveranceChoreography.IsValidStep(FirstSeveranceBossPhase.Distant,FirstSeveranceSubstate.RemoteCrush,8),"crush is phase-local score step");
        var comb=FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,2,28,4000,4000);
        AssertEqual(16,comb.Count,"sixteen vertical teeth");
        float gap=(comb[0].Ray.X+comb[1].Ray.X)*.5f;
        foreach(var tooth in comb) AssertEqual(false,tooth.Ray.Intersects(gap,3440,10,21),"104 px lane fits standing player");
        AssertEqual(8,FirstSeveranceScoreGeometry.Rays(FirstSeveranceSubstate.FinalSlicer,2,68,4000,4000).Count,"horizontal comb includes the boundary cells");
        AssertEqual(96,FirstSeveranceScoreGeometry.Bullets(2,132,4000,4000).Count,"four overlapping 24-bullet waves");
    }
}
