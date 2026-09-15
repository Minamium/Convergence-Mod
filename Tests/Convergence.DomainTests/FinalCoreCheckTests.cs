using System;
using System.IO;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    private static FirstSeveranceLoopStateMachine AtFinalCheck(int count)
    {
        var loop = CreateStagedExposure(count);
        for (int guard = 0; guard < 160 && loop.State.Substate != FirstSeveranceSubstate.FinalCoreCheck; guard++)
        {
            AssertEqual(false, loop.State.IsTerminal, "cannot win before the core check");
            loop.Advance(new(loop.State.ResolveTick, 0, int.MaxValue));
        }
        AssertEqual(FirstSeveranceSubstate.FinalCoreCheck, loop.State.Substate, "finite score reaches check");
        return loop;
    }

    [DomainTest("Final core check restores five percent exactly once and accepts the deadline hit")]
    private static void FinalCheckSuccess()
    {
        foreach (int count in new[] { 1, 2, 3, 4 })
        {
            var loop = AtFinalCheck(count);
            var start = loop.State;
            AssertEqual(FirstSeveranceFinalCheck.Life(start.BossMaximumLife), start.BossLife, "five percent budget");
            AssertEqual(600ul, start.ResolveTick - start.SubstateEnteredTick, "ten seconds");
            loop.Advance(new(start.LastAuthorityTick + 1, 0, start.BossLife - 1));
            AssertEqual(1, loop.State.BossLife, "real damage, no refilling on update");
            AssertEqual(false, loop.State.IsTerminal, "not a timed automatic victory");
            loop.Advance(new(start.ResolveTick, 0, 1));
            AssertEqual(EncounterEndReason.Victory, loop.State.Termination.EndReason, "deadline is inclusive");
            var ended = loop.State;
            loop.Advance(new(start.ResolveTick + 1, 0, int.MaxValue));
            AssertEqual(ended, loop.State, "terminal is immutable");
        }
        AssertEqual(500000, FirstSeveranceFinalCheck.Life(FirstSeverancePartyScaling.ForCount(1).BossLife), "solo remains available");
        AssertEqual(900000, FirstSeveranceFinalCheck.Life(FirstSeverancePartyScaling.ForCount(3).BossLife), "three-player budget");
        AssertEqual(1300000, FirstSeveranceFinalCheck.Life(FirstSeverancePartyScaling.ForCount(4).BossLife), "four-player budget");
    }

    [DomainTest("Final core check times out as Defeat and never accepts a late killing hit")]
    private static void FinalCheckTimeout()
    {
        foreach (bool late in new[] { false, true })
        {
            var loop = AtFinalCheck(2);
            int life = loop.State.BossLife;
            loop.Advance(new(loop.State.ResolveTick + (late ? 1ul : 0ul), 0, late ? int.MaxValue : 0));
            AssertEqual(EncounterEndReason.Defeat, loop.State.Termination.EndReason, "timeout is a real wipe");
            AssertEqual(life, loop.State.BossLife, "late damage cannot turn timeout into a win");
            AssertEqual(FirstSeveranceTerminalCause.FinalDpsFailed,
                FirstSeveranceTerminationContract.Instance.GetCause(loop.State.Termination), "diagnosable cause");
            AssertEqual(true, FirstSeveranceTerminationContract.IsFeatureOwned(FirstSeveranceTerminalCause.FinalDpsFailed), "normal cleanup ownership");
        }
    }

    [DomainTest("Final check late-join snapshot preserves HP, target and long cannon collision")]
    private static void FinalCheckCodec()
    {
        var loop = AtFinalCheck(2); var s = loop.State; var fight = CreateContext(2).FightId;
        var ray = FirstSeveranceGridVolley.AimCoreBeam(4000, 4000, 4500, 3440);
        var cannon = new FirstSeveranceCoreCannonVolley(10, s.SubstateEnteredTick + 24, 0, ray);
        var players = new[] { new FirstSeveranceCombatParticipantProjection(new ParticipantId(0), 0, true,
            RaidParticipantCombatState.Alive, false, 0, 0, 0, 500, 4500, 3440, 0, 0) };
        FirstSeveranceCombatProjection Projection(int hp) => new(1, fight, s.Substate, s.ResolveTick,
            0, 0, hp, s.BossMaximumLife, 0, 0, 4000, 3120, 4000, 4000,
            FirstSeveranceMechanicResult.None, 0, players, bossPhase:s.BossPhase,
            bossPhaseStartedTick:s.BossPhaseStartedTick, actionStartedTick:s.SubstateEnteredTick,
            actionIndex:s.ActionIndex, coreCannon:cannon);
        AssertEqual(true, Projection(s.BossLife).IsCoreOpen, "Final check exposes native actor");
        AssertEqual(60ul, cannon.EndTick - cannon.FireTick, "one second live beam");
        AssertEqual(true, cannon.Intersects(cannon.EndTick - 1, 4500, 3440, 10, 21), "extended tail is visible and harmful");
        AssertEqual(false, cannon.Intersects(cannon.EndTick, 4500, 3440, 10, 21), "no harm beyond exact end");
        AssertThrows<ArgumentException>(() => Projection(s.BossLife + 1), "no forged refill above check budget");
        var snapshot = new EncounterSnapshot(1, fight, FirstSeveranceIdentity.EncounterKey, EncounterLifecycle.Active,
            4, cannon.FireTick + 30, s.SubstateEnteredTick, 54, EncounterTerminationDescriptor.None);
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        FirstSeverancePacketCodec.WriteSnapshot(writer, snapshot, null, Projection(s.BossLife));
        stream.Position = 0; using var reader = new BinaryReader(stream);
        EncounterPacketCodec.TryReadHeader(reader, out var header, out _);
        EncounterRouteCodec.TryRead(reader, out _);
        AssertEqual(true, FirstSeverancePacketCodec.TryReadSnapshot(reader, header, out _, out _, out var decoded, out _), "check round trip");
        AssertEqual(s.BossLife, decoded!.BossLife, "late join receives remaining HP");
        AssertEqual(cannon.EndTick, decoded.CoreCannon!.EndTick, "late join receives actual cannon window");
    }

    [DomainTest("Doll observer missing buff never dismisses another player's companion")]
    private static void DollObserverPersistence()
    {
        AssertEqual(false, DollCompanionRules.DismissForMissingBuff(false, false), "remote buff unknown");
        AssertEqual(false, DollCompanionRules.DismissForMissingBuff(false, true), "remote buff known");
        AssertEqual(false, DollCompanionRules.DismissForMissingBuff(true, true), "owner sustaining");
        AssertEqual(true, DollCompanionRules.DismissForMissingBuff(true, false), "owner cancellation still works");
    }
}
