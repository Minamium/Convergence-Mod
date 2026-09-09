using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Server-wide preparation includes distant players without silent eligibility filtering")]
    private static void ServerWidePreparationRoster()
    {
        for (int count = 1; count <= 4; count++)
        {
            var candidates = new FirstSeveranceRosterCandidate[count];
            for (int i = 0; i < count; i++) candidates[i] = new(i, (ulong)i + 10, true, true, i == 0);
            AssertEqual(true, FirstSeveranceRoster.TryCreate(candidates, 0, 10, out var roster, out _,
                allowSoloDebug: true, requireAllConnected: true), "all players admitted even outside old radius");
            AssertEqual(count, roster!.Count, "exact server denominator");
            candidates[count - 1] = candidates[count - 1] with { IsEligible = false };
            AssertEqual(false, FirstSeveranceRoster.TryCreate(candidates, 0, 10, out _, out var failure,
                allowSoloDebug: true, requireAllConnected: true), "dead or ghost blocks entire preparation");
            AssertEqual("first_severance.roster_player_not_alive", failure, "explicit unavailable reason");
            candidates[count - 1] = candidates[count - 1] with { IsEligible = true, ConnectionEpoch = 0 };
            AssertEqual(false, FirstSeveranceRoster.TryCreate(candidates, 0, 10, out _, out _,
                allowSoloDebug: true, requireAllConnected: true), "missing epoch cannot silently remove a joiner");
        }
        var five = new FirstSeveranceRosterCandidate[5];
        for (int i = 0; i < five.Length; i++) five[i] = new(i, (ulong)i + 10, true, true, false);
        AssertEqual(false, FirstSeveranceRoster.TryCreate(five, 0, 10, out _, out var reason,
            allowSoloDebug: true, requireAllConnected: true), "capacity is rejected, never truncated");
        AssertEqual(FirstSeveranceArenaIssueCodes.SelectionRequired, reason, "explicit capacity reason");
    }

    [DomainTest("Preparation deployment finishes before Ready accepts exact bound members")]
    private static void PreparationDeploymentTiming()
    {
        var roster = CreatePreparationRoster();
        var machine = new FirstSeverancePreparationStateMachine(CreateContext(3).FightId, roster, 100,
            FirstSeverancePreparationSettings.Default, FirstSeverancePreparationTimeline.DeploymentTicks);
        AssertEqual(280ul, machine.ReadyOpensTick, "three-second deployment");
        AssertEqual(3880ul, machine.DeadlineTick, "full Ready timeout starts after deployment");
        var member = roster.Members[0];
        var early = machine.ApplySetReady(new(member.ServerWhoAmI, member.ConnectionEpoch, true, 1, 279));
        AssertEqual(false, early.IsAccepted, "no early Ready");
        AssertEqual(false, machine.CreateSnapshot().Members[0].IsReady, "no early count mutation");
        foreach (var m in roster.Members)
            AssertEqual(true, machine.ApplySetReady(new(m.ServerWhoAmI, m.ConnectionEpoch, true, 1, 280)).IsAccepted,
                "same nonce remains usable after rejected early Ready");
        AssertEqual(true, machine.CreateSnapshot().AreAllReady, "all server players, not only nearby ones");
        AssertEqual(true, machine.ApplySetReady(new(member.ServerWhoAmI, member.ConnectionEpoch, false, 2, 281)).IsAccepted,
            "Ready remains reversible before runtime activation");
        AssertEqual(false, machine.CreateSnapshot().AreAllReady, "unready prevents start");
        machine.Cleanup(machine.FightId);
        AssertEqual(false, machine.ApplySetReady(new(member.ServerWhoAmI, member.ConnectionEpoch, true, 3, 282)).IsAccepted,
            "cleaned session cannot accept delayed Ready");
    }

    [DomainTest("Preparation membership changes cannot launch an obsolete Ready roster")]
    private static void PreparationMembershipChanges()
    {
        var roster = CreatePreparationRoster();
        var current = new System.Collections.Generic.List<FirstSeveranceConnectionObservation>();
        foreach (var m in roster.Members) current.Add(new(m.ServerWhoAmI, m.ConnectionEpoch, true));
        AssertEqual(true, roster.MatchesConnected(current), "all three live bindings");
        current.Add(new(20, 200, true));
        AssertEqual(false, roster.MatchesConnected(current), "new player prevents old all-Ready start");
        current.RemoveAt(3);
        current[2] = current[2] with { IsConnected = false };
        AssertEqual(false, roster.MatchesConnected(current), "disconnect does not shrink denominator");
        current[2] = current[2] with { IsConnected = true, ConnectionEpoch = 91 };
        AssertEqual(false, roster.MatchesConnected(current), "slot reuse cannot inherit Ready");
        current[2] = current[0];
        AssertEqual(false, roster.MatchesConnected(current), "duplicate observation cannot fake count");
    }
}
