#nullable enable

using System;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Instant unlimited recovery accepts eight members without widening token mode")]
    private static void EightMemberInstantSettingsBounds()
    {
        AssertEqual(true, RaidReviveSettings.CreateInstantUnlimited(8).IsValid, "eight-member instant policy");
        AssertEqual(false, (RaidReviveSettings.CreateInitial(4) with { ParticipantCount = 8, InitialTokenCount = 7 }).IsValid,
            "token policy remains capped at four");
        AssertThrows<ArgumentOutOfRangeException>(() => RaidReviveSettings.CreateInitial(5),
            "historical initial policy rejects five");
        AssertThrows<ArgumentOutOfRangeException>(() => RaidReviveSettings.CreateInstantUnlimited(9),
            "instant policy rejects nine");
    }

    [DomainTest("Eight-member instant Down revive lockout all-Down and exact cleanup")]
    private static void EightMemberInstantRecoveryLifecycle()
    {
        TestContext context = CreateContext(8, RaidReviveSettings.CreateInstantUnlimited(8));
        AssertEqual(8, context.Service.CreateSnapshot().Participants.Count, "frozen eight-member roster");
        AssertEqual(0, context.Service.RemainingTokenCount, "instant mode has no shared tokens");

        AssertApplied(Down(context, 7, 10));
        AssertEqual(0ul, ParticipantSnapshot(context, 7).DownedDeadlineTick, "Down has no timeout");
        AssertApplied(StartRevive(context, 0, 7, 1, 10));
        AssertApplied(context.Service.CommitTick(context.FightId, 10));
        AssertEqual(RaidParticipantCombatState.Alive, ParticipantSnapshot(context, 7).CombatState,
            "instant revive completes on its authority tick");
        AssertEqual(3_610ul, ParticipantSnapshot(context, 7).ReviveLockoutUntilTick,
            "recipient lockout lasts sixty seconds");

        AssertApplied(Down(context, 7, 20));
        AssertEqual("revive.target_recovery_locked", StartRevive(context, 0, 7, 2, 3_609).FailureCode,
            "recipient cannot revive one tick early");
        AssertApplied(StartRevive(context, 0, 7, 2, 3_610));
        AssertApplied(context.Service.CommitTick(context.FightId, 3_610));
        AssertEqual(7_210ul, ParticipantSnapshot(context, 7).ReviveLockoutUntilTick,
            "successful repeat revive renews the recipient lockout");

        for (int index = 0; index < 8; index++) AssertApplied(Down(context, index, 4_000));
        AssertEqual(RaidReviveFailureReason.None, context.Service.FailureReason,
            "all-Down waits for the authority commit");
        AssertApplied(context.Service.CommitTick(context.FightId, 4_000));
        AssertEqual(RaidReviveFailureReason.AllParticipantsDowned, context.Service.FailureReason,
            "all eight Down ends the Raid");

        FightId staleFight = FightId.FromWire(Guid.Parse("88888888-8888-8888-8888-888888888888"));
        AssertEqual(false, context.Service.TryCleanup(staleFight), "another Fight cannot clean this roster");
        AssertEqual(true, context.Service.TryCleanup(context.FightId), "exact Fight cleans roster");
        AssertEqual(true, context.Service.TryCleanup(context.FightId), "exact cleanup is idempotent");
        AssertEqual(0, context.Service.CreateSnapshot().Participants.Count, "cleanup clears eight participants");
        AssertEqual(0, context.Service.CreateSnapshot().Channels.Count, "cleanup leaves no channels");
    }
}
