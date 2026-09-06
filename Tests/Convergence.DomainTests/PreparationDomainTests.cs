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
    [DomainTest("Pull roster identity is deterministic")]
    private static void PullRosterIdentityIsDeterministic()
    {
        FirstSeveranceRosterCandidate[] candidates =
        {
            SelectableCandidate(serverWhoAmI: 9, connectionEpoch: 90),
            SelectableCandidate(serverWhoAmI: 2, connectionEpoch: 20),
            SelectableCandidate(serverWhoAmI: 7, connectionEpoch: 70),
        };

        bool created = FirstSeveranceRoster.TryCreate(
            candidates,
            initiatorWhoAmI: 7,
            initiatorConnectionEpoch: 70,
            out FirstSeveranceRoster? roster,
            out string failureCode);
        if (!created || roster is null)
        {
            throw new InvalidOperationException($"Expected a frozen roster, got '{failureCode}'.");
        }

        AssertEqual(3, roster.Count, "roster count");
        AssertEqual(2, roster.Members[0].ServerWhoAmI, "Participant 0 slot");
        AssertEqual(new ParticipantId(0), roster.Members[0].ParticipantId, "Participant 0 identity");
        AssertEqual(7, roster.Members[1].ServerWhoAmI, "Participant 1 slot");
        AssertEqual(new ParticipantId(1), roster.InitiatorParticipantId, "initiator identity");
        AssertEqual(9, roster.Members[2].ServerWhoAmI, "Participant 2 slot");
        AssertEqual(
            false,
            roster.TryResolveCurrentBinding(7, 71, out _),
            "slot reuse with a new epoch");
        AssertEqual(
            true,
            roster.TryResolveCurrentBinding(7, 70, out FirstSeveranceRosterMember initiator),
            "exact initiator binding");
        AssertEqual(new ParticipantId(1), initiator.ParticipantId, "resolved initiator");
    }

    [DomainTest("Pull roster rejects ambiguous membership")]
    private static void PullRosterRejectsAmbiguousMembership()
    {
        FirstSeveranceRosterCandidate[] tooMany =
        {
            SelectableCandidate(0, 10),
            SelectableCandidate(1, 11),
            SelectableCandidate(2, 12),
            SelectableCandidate(3, 13),
            SelectableCandidate(4, 14),
        };
        AssertRosterRejected(
            tooMany,
            initiatorWhoAmI: 0,
            initiatorConnectionEpoch: 10,
            FirstSeveranceArenaIssueCodes.SelectionRequired);

        FirstSeveranceRosterCandidate[] duplicateSlot =
        {
            SelectableCandidate(3, 30),
            SelectableCandidate(3, 31),
        };
        AssertRosterRejected(
            duplicateSlot,
            initiatorWhoAmI: 3,
            initiatorConnectionEpoch: 30,
            "first_severance.roster_candidate_invalid");

        FirstSeveranceRosterCandidate[] tooFew =
        {
            SelectableCandidate(1, 10),
        };
        AssertRosterRejected(
            tooFew,
            initiatorWhoAmI: 1,
            initiatorConnectionEpoch: 10,
            FirstSeveranceArenaIssueCodes.TooFewParticipants);

        FirstSeveranceRosterCandidate[] initiatorOutside =
        {
            SelectableCandidate(1, 10),
            SelectableCandidate(2, 20),
            new FirstSeveranceRosterCandidate(
                ServerWhoAmI: 3,
                ConnectionEpoch: 30,
                IsConnected: true,
                IsEligible: true,
                IsWithinParticipationRegion: false),
        };
        AssertRosterRejected(
            initiatorOutside,
            initiatorWhoAmI: 3,
            initiatorConnectionEpoch: 30,
            "first_severance.roster_initiator_not_eligible");
    }

    [DomainTest("Ready validates exact binding and nonce")]
    private static void ReadyValidatesExactBindingAndNonce()
    {
        FirstSeveranceRoster roster = CreatePreparationRoster();
        FightId fightId = TestFightId("11111111-1111-1111-1111-111111111111");
        var machine = new FirstSeverancePreparationStateMachine(
            fightId,
            roster,
            enteredTick: 100,
            new FirstSeverancePreparationSettings(readyTimeoutTicks: 10));

        FirstSeverancePreparationSnapshot initial = machine.CreateSnapshot();
        AssertEqual(110UL, initial.DeadlineTick, "Ready deadline");
        AssertEqual(true, initial.CombatGateClosed, "preparation combat gate");
        AssertEqual(false, initial.AreAllReady, "initial Ready aggregate");

        FirstSeverancePreparationUpdate first = machine.ApplySetReady(
            new FirstSeveranceSetReadyCommand(2, 20, true, 1, 101));
        AssertEqual(true, first.IsAccepted, "first Ready acceptance");
        AssertEqual(true, first.HasObservableChange, "first Ready change");

        FirstSeverancePreparationUpdate wrongEpoch = machine.ApplySetReady(
            new FirstSeveranceSetReadyCommand(7, 71, true, 99, 108));
        AssertPreparationRejected(
            wrongEpoch,
            "first_severance.preparation_sender_not_bound");

        FirstSeverancePreparationUpdate staleNonce = machine.ApplySetReady(
            new FirstSeveranceSetReadyCommand(2, 20, false, 1, 102));
        AssertPreparationRejected(staleNonce, "first_severance.preparation_stale_nonce");

        AssertEqual(
            true,
            machine.ApplySetReady(new FirstSeveranceSetReadyCommand(7, 70, true, 1, 102))
                .HasObservableChange,
            "second participant Ready");
        AssertEqual(
            true,
            machine.ApplySetReady(new FirstSeveranceSetReadyCommand(9, 90, true, 1, 103))
                .HasObservableChange,
            "third participant Ready");
        AssertEqual(true, machine.CreateSnapshot().AreAllReady, "all Ready aggregate");

        FirstSeverancePreparationUpdate unready = machine.ApplySetReady(
            new FirstSeveranceSetReadyCommand(7, 70, false, 2, 104));
        AssertEqual(true, unready.HasObservableChange, "Ready reversal change");
        AssertEqual(false, machine.CreateSnapshot().AreAllReady, "Ready reversal aggregate");

        FirstSeverancePreparationUpdate live = machine.Advance(
            authorityTick: 109,
            coreIsPresent: true,
            CurrentPreparationConnections());
        AssertEqual(true, live.IsAccepted, "pre-deadline advance");
        AssertEqual(false, live.RequestsEnd, "pre-deadline remains open");

        AssertThrows<ArgumentOutOfRangeException>(
            () => _ = new FirstSeverancePreparationStateMachine(
                fightId,
                roster,
                enteredTick: 0,
                default),
            "default Ready settings");
    }

    [DomainTest("Preparation safety exits are deterministic")]
    private static void PreparationSafetyExitsAreDeterministic()
    {
        FirstSeveranceRoster roster = CreatePreparationRoster();
        var settings = new FirstSeverancePreparationSettings(readyTimeoutTicks: 10);

        var timeout = new FirstSeverancePreparationStateMachine(
            TestFightId("22222222-2222-2222-2222-222222222222"),
            roster,
            enteredTick: 100,
            settings);
        FirstSeverancePreparationUpdate timeoutUpdate = timeout.Advance(
            authorityTick: 110,
            coreIsPresent: true,
            CurrentPreparationConnections());
        AssertPreparationEnded(timeoutUpdate, "first_severance.preparation_timeout");

        var coreLost = new FirstSeverancePreparationStateMachine(
            TestFightId("33333333-3333-3333-3333-333333333333"),
            roster,
            enteredTick: 100,
            settings);
        FirstSeverancePreparationUpdate coreUpdate = coreLost.Advance(
            authorityTick: 101,
            coreIsPresent: false,
            CurrentPreparationConnections());
        AssertPreparationEnded(coreUpdate, "first_severance.preparation_core_removed");

        FirstSeveranceConnectionObservation[] staleConnections =
        {
            new(2, 20, true),
            new(7, 71, true),
            new(9, 90, true),
        };
        var participantLost = new FirstSeverancePreparationStateMachine(
            TestFightId("44444444-4444-4444-4444-444444444444"),
            roster,
            enteredTick: 100,
            settings);
        FirstSeverancePreparationUpdate participantUpdate = participantLost.Advance(
            authorityTick: 101,
            coreIsPresent: true,
            staleConnections);
        AssertPreparationEnded(
            participantUpdate,
            "first_severance.preparation_participant_lost");
    }

    [DomainTest("Preparation cancel and cleanup are exact")]
    private static void PreparationCancelAndCleanupAreExact()
    {
        FirstSeveranceRoster roster = CreatePreparationRoster();
        FightId fightId = TestFightId("55555555-5555-5555-5555-555555555555");
        var machine = new FirstSeverancePreparationStateMachine(
            fightId,
            roster,
            enteredTick: 10,
            new FirstSeverancePreparationSettings(readyTimeoutTicks: 20));

        FirstSeverancePreparationUpdate outsiderCancel = machine.ApplyCancel(
            new FirstSeveranceCancelPreparationCommand(2, 20, 1, 11));
        AssertPreparationRejected(
            outsiderCancel,
            "first_severance.preparation_cancel_not_initiator");

        FirstSeverancePreparationUpdate cancel = machine.ApplyCancel(
            new FirstSeveranceCancelPreparationCommand(7, 70, 1, 12));
        AssertPreparationEnded(cancel, "first_severance.preparation_cancelled");
        AssertEqual(true, machine.IsClosed, "cancel closes preparation");

        FirstSeverancePreparationUpdate afterClose = machine.ApplySetReady(
            new FirstSeveranceSetReadyCommand(2, 20, true, 1, 13));
        AssertPreparationRejected(afterClose, "first_severance.preparation_closed");

        AssertEqual(
            false,
            machine.Cleanup(TestFightId("66666666-6666-6666-6666-666666666666")),
            "foreign Fight cleanup");
        AssertEqual(true, machine.Cleanup(fightId), "exact Fight cleanup");
        AssertEqual(true, machine.Cleanup(fightId), "idempotent exact Fight cleanup");
        AssertPreparationRejected(
            machine.Advance(14, coreIsPresent: true, CurrentPreparationConnections()),
            "first_severance.preparation_cleaned");
    }

    [DomainTest("Core lease cleanup requires the exact Fight")]
    private static void CoreLeaseCleanupRequiresExactFight()
    {
        FightId firstFight = TestFightId("77777777-7777-7777-7777-777777777777");
        FightId secondFight = TestFightId("88888888-8888-8888-8888-888888888888");
        var registry = new FirstSeveranceCoreLeaseRegistry();
        var topLeft = new TilePoint(400, 500);

        AssertEqual(true, registry.TryClaim(12, topLeft, 3, firstFight), "initial Core claim");
        AssertEqual(true, registry.TryClaim(12, topLeft, 3, firstFight), "idempotent Core claim");
        AssertEqual(false, registry.TryClaim(12, topLeft, 4, secondFight), "competing Core claim");
        AssertEqual(
            FirstSeveranceCoreProtectionState.Preparing,
            registry.Current!.Value.ProtectionState,
            "preparation protection state");
        AssertEqual(false, registry.TryEnterActive(12, secondFight), "foreign Fight activation");
        AssertEqual(true, registry.TryEnterActive(12, firstFight), "exact Fight activation");
        AssertEqual(true, registry.IsActivelyProtected(12), "active Core protection");
        AssertEqual(false, registry.TryRelease(12, secondFight), "foreign Fight release");
        AssertEqual(true, registry.IsActivelyProtected(12), "foreign release preserves lease");
        AssertEqual(false, registry.TryRecordMissing(13), "foreign Core missing event");
        AssertEqual(true, registry.TryRecordMissing(12), "exact Core missing event");
        AssertEqual(false, registry.IsActivelyProtected(12), "missing Core projection");
        AssertEqual(true, registry.TryRelease(12, firstFight), "exact Fight release");
        AssertEqual<FirstSeveranceCoreLease?>(null, registry.Current, "released Core lease");
        AssertEqual(true, registry.TryRelease(12, firstFight), "idempotent released cleanup");

        AssertEqual(true, registry.TryClaim(14, topLeft, 5, secondFight), "next Core claim");
        registry.ClearWorld();
        AssertEqual<FirstSeveranceCoreLease?>(null, registry.Current, "World unload cleanup");
    }
}
