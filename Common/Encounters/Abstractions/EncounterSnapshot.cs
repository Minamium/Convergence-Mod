using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Encounters.Abstractions;

internal readonly record struct EncounterSnapshot(
    ulong EncounterSequence,
    FightId FightId,
    string DefinitionKey,
    EncounterLifecycle Lifecycle,
    uint Revision,
    ulong AuthorityTick,
    ulong LifecycleEnteredTick,
    ulong ActiveFightTick,
    EncounterEndReason EndReason)
{
    public static EncounterSnapshot Idle => CreateIdle(0, 0, 0);

    public bool IsTerminal => Lifecycle == EncounterLifecycle.Cleanup
        && EndReason != EncounterEndReason.None;

    public static EncounterSnapshot CreateIdle(
        ulong encounterSequence,
        uint revision,
        ulong authorityTick)
    {
        return new EncounterSnapshot(
            encounterSequence,
            FightId.None,
            string.Empty,
            EncounterLifecycle.Idle,
            revision,
            authorityTick,
            authorityTick,
            0,
            EncounterEndReason.None);
    }
}
