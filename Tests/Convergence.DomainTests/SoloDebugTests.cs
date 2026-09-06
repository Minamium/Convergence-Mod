using System;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Solo debug admission is authority-opted-in with unchanged invalid roster rejection")]
    private static void SoloDebugAdmission()
    {
        AssertEqual(1, FirstSeveranceDevelopmentPolicy.MinimumFor(true), "enabled admission");
        AssertEqual(2, FirstSeveranceDevelopmentPolicy.MinimumFor(false), "release admission");
        AssertEqual(FirstSeveranceDevelopmentPolicy.AllowSoloDebugStart ? 1 : 2,
            FirstSeveranceDevelopmentPolicy.MinimumParticipants, "compiled policy agrees");
        foreach (bool enabled in new[] { false, true })
        {
            for (int count = 0; count <= 5; count++)
            {
                var candidates = new FirstSeveranceRosterCandidate[count];
                for (int i = 0; i < count; i++) candidates[i] = SelectableCandidate(i, (ulong)i + 10);
                bool accepted = FirstSeveranceRoster.TryCreate(candidates, 0, 10, out var roster, out _, enabled);
                AssertEqual(count >= (enabled ? 1 : 2) && count <= 4, accepted, "bounded roster admission");
                if (accepted) AssertEqual(count, roster!.Count, "no synthetic member");
            }
        }
        var candidate = new[] { SelectableCandidate(0, 10) };
        AssertEqual(false, FirstSeveranceRoster.TryCreate(candidate, 0, 10, out _, out _), "implicit opt-in forbidden");
        AssertEqual(false, FirstSeveranceRoster.TryCreate(candidate, 0, 11, out _, out _, true), "stale initiator rejected even in debug");
        var layout = CreateValidArenaLayout();
        var metrics = new FirstSeveranceArenaScanMetrics(layout.ArenaBounds.Width * layout.ArenaBounds.Height,
            0, 0, 0, 0, 0, 0, 0, 0, 0, layout.ArenaBounds.Width, 1);
        var survey = new FirstSeveranceArenaSurvey(layout, true, true, false, 1, metrics, default);
        AssertEqual(FirstSeveranceArenaIssueCodes.TooFewParticipants,
            FirstSeveranceArenaValidator.Instance.Validate(survey).FirstErrorCode, "release arena still needs two");
        AssertEqual(true, FirstSeveranceArenaValidator.Instance.Validate(survey, allowSoloDebug: true).IsValid, "solo arena only with opt-in");
        var invalid = new FirstSeveranceArenaSurvey(layout, false, true, false, 1, metrics, default);
        AssertEqual(FirstSeveranceArenaIssueCodes.CoreMismatch,
            FirstSeveranceArenaValidator.Instance.Validate(invalid, allowSoloDebug: true).FirstErrorCode, "debug never skips Core/field checks");
    }

    [DomainTest("One-player Ready remains manual and one Down ends the Raid on the authority tick")]
    private static void SoloReadyAndDefeat()
    {
        var context = CreateContext(1, RaidReviveSettings.CreateInstantUnlimited(1));
        FirstSeveranceRoster.TryCreate(new[] { SelectableCandidate(0, 100) }, 0, 100, out var roster, out _, true);
        var preparation = new FirstSeverancePreparationStateMachine(context.FightId, roster!, 100,
            FirstSeverancePreparationSettings.Default);
        AssertEqual(false, preparation.CreateSnapshot().AreAllReady, "solo is not auto-Ready");
        AssertEqual(false, preparation.ApplySetReady(new(0, 99, true, 1, 101)).IsAccepted, "stale sender still rejected");
        AssertEqual(true, preparation.ApplySetReady(new(0, 100, true, 1, 101)).IsAccepted, "sole real player can Ready");
        AssertEqual(true, preparation.CreateSnapshot().AreAllReady, "one actual ready member starts");
        var boundary = new FirstSeveranceReviveBoundary(context.FightId);
        AssertEqual(true, boundary.TryInitialize(context.Roster, out _), "actual feature recovery boundary accepts solo roster");
        AssertEqual(true, boundary.Apply(new AuthoritativeParticipantDownedCommand(context.FightId, context.Roster[0], 102)).IsAccepted, "authority Down accepted");
        var end = boundary.Tick(new EncounterRuntimeContext(1, context.FightId, EncounterLifecycle.Active, 102, 101, 1));
        AssertEqual(false, end.RequestedTermination.IsNone, "same committed tick ends; no thirty-second solo limbo");
        AssertEqual(EncounterEndReason.Defeat, end.RequestedTermination.EndReason, "normal Defeat death/cleanup route");
        AssertEqual(FirstSeveranceTerminalCause.AllParticipantsDowned,
            FirstSeveranceTerminationContract.Instance.GetCause(end.RequestedTermination), "no special gameplay terminal");
    }

    [DomainTest("Initial admission and Ready revalidation share the compiled solo policy")]
    private static void SoloStartRevalidation()
    {
        var layout = CreateValidArenaLayout();
        var metrics = new FirstSeveranceArenaScanMetrics(layout.ArenaBounds.Width * layout.ArenaBounds.Height,
            0, 0, 0, 0, 0, 0, 0, 0, 0, layout.ArenaBounds.Width, 1);
        for (int count = 0; count <= 5; count++)
        {
            var survey = new FirstSeveranceArenaSurvey(layout, true, true, false, count, metrics, default);
            bool expected = count >= FirstSeveranceDevelopmentPolicy.MinimumParticipants && count <= 4;
            AssertEqual(expected, FirstSeveranceDevelopmentPolicy.ValidateStartArena(survey).IsValid, "initial scan");
            // Production CoreResolver calls the same non-optional policy again
            // after Ready, using a newly scanned survey, not the first result.
            var fresh = new FirstSeveranceArenaSurvey(layout, true, true, false, count, metrics, default);
            AssertEqual(expected, FirstSeveranceDevelopmentPolicy.ValidateStartArena(fresh).IsValid, "Ready revalidation");
        }
        int admittedCount = FirstSeveranceDevelopmentPolicy.MinimumParticipants;
        var removed = new FirstSeveranceArenaSurvey(layout, false, true, false, admittedCount, metrics, default);
        AssertEqual(FirstSeveranceArenaIssueCodes.CoreMismatch,
            FirstSeveranceDevelopmentPolicy.ValidateStartArena(removed).FirstErrorCode, "Core removed after Ready remains rejected");
        var conflict = new FirstSeveranceArenaSurvey(layout, true, true, true, admittedCount, metrics, default);
        AssertEqual(FirstSeveranceArenaIssueCodes.WorldConflict,
            FirstSeveranceDevelopmentPolicy.ValidateStartArena(conflict).FirstErrorCode, "new world conflict remains rejected");
    }

    [DomainTest("Raid owner movement heartbeat is bounded and clears on capability loss")]
    private static void RaidMovementHeartbeat()
    {
        var cadence = new FirstSeveranceMovementSyncCadence();
        int sent = 0;
        for (ulong tick = 0; tick < 60; tick++)
        {
            if (cadence.TryAdvance(tick, true)) sent++;
            AssertEqual(false, cadence.TryAdvance(tick, true), "never sends twice in one tick");
        }
        AssertEqual(10, sent, "stationary as well as moving owners publish at 10 Hz");
        AssertEqual(false, cadence.TryAdvance(60, false), "no capability, Down, outsider or non-owner never sends");
        AssertEqual(true, cadence.TryAdvance(61, true), "revival/re-entry publishes immediately");
        cadence.Clear();
        AssertEqual(true, cadence.TryAdvance(62, true), "new Fight clears previous cadence");
        AssertEqual(true, cadence.TryAdvance(0, true), "world clock reset is safe");
        AssertEqual(false, cadence.TryAdvance(1, true), "bounded after rollback");
        AssertEqual(true, cadence.TryAdvance(ulong.MaxValue, true), "no addition overflow");
        AssertEqual(true, cadence.TryAdvance(0, true), "clock wrap does not stall movement sync");
    }

    [DomainTest("Solo debug keeps the two-player workload with one real Stack participant")]
    private static void SoloDebugWorkload()
    {
        var solo = FirstSeverancePartyScaling.ForCount(1);
        var two = FirstSeverancePartyScaling.ForCount(2);
        AssertEqual((byte)1, solo.ParticipantCount, "actual roster preserved in NPC extra AI");
        AssertEqual(two.BossLife, solo.BossLife, "no solo HP rebalance");
        AssertEqual(two.PylonLife, solo.PylonLife, "no solo Pylon HP rebalance");
        AssertEqual(2, FirstSeveranceEncounterPlan.Instance.PylonCount.GetValue(1), "two Pylons remain");
        AssertEqual(0, FirstSeveranceCombatRules.StackDamage(1000, 1, 1), "actual participant gathered");
        AssertEqual(1000, FirstSeveranceCombatRules.StackDamage(1000, 1, 0), "missed beacon still dangerous");
        AssertEqual(true, RaidReviveSettings.CreateInstantUnlimited(1).IsValid, "one-member generic recovery is valid");
        AssertThrows<ArgumentOutOfRangeException>(() => RaidReviveSettings.CreateInitial(1), "legacy token-based initial policy unchanged");
        AssertThrows<ArgumentOutOfRangeException>(() => RaidReviveSettings.CreateInstantUnlimited(0), "empty recovery forbidden");
    }

    [DomainTest("Terminal HUD cinematics expire and clear without any future packet")]
    private static void TerminalPresentationLifetime()
    {
        var timeline = new FirstSeveranceEndingTimeline();
        foreach (var reason in Enum.GetValues<EncounterEndReason>())
        {
            bool terminal = reason is EncounterEndReason.Victory or EncounterEndReason.Defeat;
            timeline.Begin(reason, true, 1000);
            AssertEqual(terminal, timeline.IsActive(1000), "only success/failure can hide HUD");
            if (terminal)
            {
                int duration = reason == EncounterEndReason.Victory ? 210 : 180;
                AssertEqual(duration, timeline.DurationTicks, "bounded presentation duration");
                AssertEqual(0f, timeline.Age(1000), "starts at zero");
                AssertEqual(true, timeline.Age(1000, .5) > 0, "fractional smooth transition");
                AssertEqual(true, timeline.IsActive((ulong)(1000 + duration - 1)), "last active tick");
                AssertEqual(false, timeline.IsActive((ulong)(1000 + duration)), "HUD restored without another snapshot");
                AssertEqual(false, timeline.IsActive(999), "world-clock rollback cannot preserve ending");
                AssertEqual(false, timeline.IsActive(ulong.MaxValue), "expiry does not overflow");
                timeline.Clear();
                AssertEqual(false, timeline.IsActive(1000), "new Fight/unload clears HUD request");
            }
            timeline.Begin(reason, false, 1000);
            AssertEqual(false, timeline.IsActive(1000), "outsiders do not receive cinematic UI suppression");
        }
    }
}
