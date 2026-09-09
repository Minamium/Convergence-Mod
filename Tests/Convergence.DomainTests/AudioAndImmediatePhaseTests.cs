using Convergence.Client.Encounters.FirstSeverance;
using System;
using System.IO;
using System.Text;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Lethal score impact survives cleanup transport without becoming live combat")]
    private static void TerminalScoreAudioTransport()
    {
        var fight = CreateContext(2).FightId;
        var members = new[] { new FirstSeveranceCombatParticipantProjection(new ParticipantId(0), 0,
            true, RaidParticipantCombatState.Downed, false, 0, 0, 1, 1, 4000, 3440, 0, 0) };
        var projection = new FirstSeveranceCombatProjection(1, fight, FirstSeveranceSubstate.RemoteCrush,
            1300, 0, 0, 0, 5_000_000, 0, 0, 4000, 3120, 4000, 4000,
            FirstSeveranceMechanicResult.None, 0, members,
            bossPhase: FirstSeveranceBossPhase.Distant, bossPhaseStartedTick: 100,
            actionStartedTick: 1000, actionIndex: 8);
        AssertEqual(false, projection.IsTerminalPresentationAt(999), "future action cannot be retained");
        AssertEqual(true, projection.IsTerminalPresentationAt(1162), "crush impact without a Stack/Spread verdict is retained");
        AssertEqual(false, projection.IsTerminalPresentationAt(1300), "expired action cannot be retained");
        foreach (ulong tick in new[] { 1162ul, 1300ul })
        {
            var snapshot = new EncounterSnapshot(1, fight, FirstSeveranceIdentity.EncounterKey,
                EncounterLifecycle.Cleanup, 4, tick, 100, 900,
                FirstSeveranceTerminationContract.Instance.Create(FirstSeveranceTerminalCause.AllParticipantsDowned));
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            FirstSeverancePacketCodec.WriteSnapshot(writer, snapshot, null, projection);
            stream.Position = 0;
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            AssertEqual(true, EncounterPacketCodec.TryReadHeader(reader, out var header, out _), "header");
            AssertEqual(true, EncounterRouteCodec.TryRead(reader, out _), "route");
            bool accepted = FirstSeverancePacketCodec.TryReadSnapshot(reader, header, out var actual,
                out _, out var cosmetic, out var failure);
            AssertEqual(true, accepted, "terminal wire accepts bounded score audio: " + failure);
            AssertEqual(EncounterLifecycle.Cleanup, actual.Lifecycle, "no restored Active capability");
            AssertEqual(tick == 1162, cosmetic is not null, "writer suppresses stale score data");
        }
    }

    [DomainTest("Critical audio retries future beats and tolerates bounded clock correction once")]
    private static void CriticalAudioClock()
    {
        var clock = new FirstSeveranceAudioCueClock();
        AssertEqual(false, clock.Take(1, 99, 100), "early snapshot must not consume event");
        AssertEqual(true, clock.Take(1, 100, 100), "play at due tick");
        AssertEqual(false, clock.Take(1, 101, 100), "no duplicate");
        AssertEqual(false, clock.Take(1, 98, 100), "clock rollback is harmless");
        AssertEqual(true, clock.Take(2, 130, 100), "thirty tick catch-up covers short hazard skipped by snapshot");
        AssertEqual(false, clock.Take(3, 131, 100), "do not replay stale audio");
        AssertEqual(false, clock.Take(3, 110, 100), "rewinding does not resurrect expired audio");
        clock.Reset();
        AssertEqual(true, clock.Take(1, 100, 100), "next action has independent ownership");
        AssertEqual(0, FirstSeveranceScoreGeometry.BladeTurn(362), "first orbit ends after 183 ticks");
        AssertEqual(1, FirstSeveranceScoreGeometry.BladeTurn(363), "second orbit audio begins with second visual turn");
        AssertEqual(480, FirstSeveranceChoreography.BladeEnd, "five second total spin used by audio masters");
    }

    [DomainTest("Completed scores allow same-tick HP transitions in every damage action")]
    private static void ImmediateCompletedScoreTransitions()
    {
        foreach (int count in new[] { 1, 2, 3, 4 })
        foreach (var phase in new[] { FirstSeveranceBossPhase.Sealed, FirstSeveranceBossPhase.Unbound, FirstSeveranceBossPhase.Distant })
        foreach (var target in new[] { FirstSeveranceSubstate.CoreExposure, FirstSeveranceSubstate.Lattice,
            FirstSeveranceSubstate.RotatingBlade, FirstSeveranceSubstate.RemoteClaws,
            FirstSeveranceSubstate.HalfField, FirstSeveranceSubstate.RemoteCrush })
        {
            if (phase == FirstSeveranceBossPhase.Sealed && target != FirstSeveranceSubstate.CoreExposure
                || phase == FirstSeveranceBossPhase.Unbound && target is not (FirstSeveranceSubstate.Lattice or FirstSeveranceSubstate.RotatingBlade)
                || phase == FirstSeveranceBossPhase.Distant && target is not (FirstSeveranceSubstate.RemoteClaws or FirstSeveranceSubstate.HalfField or FirstSeveranceSubstate.RemoteCrush)) continue;
            var loop = CreateStagedExposure(count);
            int guard = 0;
            while (loop.State.BossPhase != phase || loop.State.CompletedPhaseCycles == 0 || loop.State.Substate != target)
            {
                AssertEqual(true, ++guard < 250, "bounded traversal to repeated damage action");
                var s = loop.State;
                loop.Advance(new(s.ResolveTick,
                    s.Substate == FirstSeveranceSubstate.PylonCheck ? s.RemainingPylons : 0,
                    s.BossPhase < phase ? int.MaxValue : 0));
            }
            ulong now = loop.State.LastAuthorityTick + 1;
            AssertEqual(true, now < loop.State.ResolveTick, "test hit is before action deadline");
            int untilFloor = loop.State.BossLife - loop.DamageFloor;
            AssertEqual(false, loop.WillChangeStage(untilFloor - 1), "above threshold stays");
            loop.Advance(new(now, 0, untilFloor - 1));
            AssertEqual(phase, loop.State.BossPhase, "one HP above threshold");
            AssertEqual(true, loop.WillChangeStage(1), "runtime suppresses old attack on threshold tick");
            loop.Advance(new(++now, 0, 1));
            AssertEqual(FirstSeveranceSubstate.PhaseTransition, loop.State.Substate, "immediate cinematic during action");
            AssertEqual(now, loop.State.BossPhaseStartedTick, "no deadline wait");
            AssertEqual(0, loop.State.CompletedPhaseCycles, "new phase requires its own first score");
            AssertEqual(false, loop.State.IsTerminal, "even HP zero enters Final rather than Victory");
        }
    }
}
