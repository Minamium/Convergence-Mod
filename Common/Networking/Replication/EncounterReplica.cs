#nullable enable

using Convergence.Common.Encounters.Abstractions;

namespace Convergence.Common.Networking.Replication;

internal sealed class EncounterReplica
{
    private ulong? terminalSequence;

    public EncounterSnapshot Snapshot { get; private set; } = EncounterSnapshot.Idle;

    public EncounterSnapshot? LastTerminalSnapshot { get; private set; }

    public bool ApplyFullSnapshot(in EncounterSnapshot incoming)
    {
        if (!HasValidShape(incoming) || !IsNewerThanCurrent(incoming))
        {
            return false;
        }

        if (terminalSequence == incoming.EncounterSequence
            && incoming.Lifecycle is not EncounterLifecycle.Cleanup
            and not EncounterLifecycle.Idle)
        {
            return false;
        }

        if (terminalSequence == incoming.EncounterSequence
            && incoming.Lifecycle == EncounterLifecycle.Cleanup
            && LastTerminalSnapshot.HasValue
            && incoming.FightId != LastTerminalSnapshot.Value.FightId)
        {
            return false;
        }

        if (incoming.EncounterSequence == Snapshot.EncounterSequence
            && Snapshot.Lifecycle != EncounterLifecycle.Idle
            && incoming.Lifecycle == EncounterLifecycle.Idle
            && terminalSequence != incoming.EncounterSequence)
        {
            return false;
        }

        if (incoming.EncounterSequence > Snapshot.EncounterSequence)
        {
            terminalSequence = null;
        }

        if (incoming.IsTerminal)
        {
            terminalSequence = incoming.EncounterSequence;
            LastTerminalSnapshot = incoming;
        }

        Snapshot = incoming;
        return true;
    }

    public void Reset()
    {
        Snapshot = EncounterSnapshot.Idle;
        LastTerminalSnapshot = null;
        terminalSequence = null;
    }

    private bool IsNewerThanCurrent(in EncounterSnapshot incoming)
    {
        if (incoming.EncounterSequence != Snapshot.EncounterSequence)
        {
            return incoming.EncounterSequence > Snapshot.EncounterSequence;
        }

        if (!Snapshot.FightId.IsNone
            && !incoming.FightId.IsNone
            && incoming.FightId != Snapshot.FightId)
        {
            return false;
        }

        if (incoming.Revision != Snapshot.Revision)
        {
            return incoming.Revision > Snapshot.Revision;
        }

        return incoming.AuthorityTick > Snapshot.AuthorityTick;
    }

    private static bool HasValidShape(in EncounterSnapshot snapshot)
    {
        if (snapshot.Lifecycle == EncounterLifecycle.Idle)
        {
            return snapshot.FightId.IsNone
                && string.IsNullOrEmpty(snapshot.DefinitionKey)
                && snapshot.EndReason == EncounterEndReason.None;
        }

        if (snapshot.EncounterSequence == 0
            || snapshot.FightId.IsNone
            || string.IsNullOrWhiteSpace(snapshot.DefinitionKey))
        {
            return false;
        }

        if (snapshot.Lifecycle == EncounterLifecycle.Cleanup)
        {
            return snapshot.IsTerminal;
        }

        return snapshot.EndReason == EncounterEndReason.None;
    }
}
