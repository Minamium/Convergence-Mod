#nullable enable

using System;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceCoreProtectionState : byte
{
    Idle = 0,
    Preparing = 1,
    Active = 2,
}

internal readonly record struct FirstSeveranceCoreLease(
    int ServerTileEntityId,
    TilePoint TopLeft,
    ulong EncounterSequence,
    FightId FightId,
    FirstSeveranceCoreProtectionState ProtectionState,
    bool IsMissing)
{
    public bool IsValid => ServerTileEntityId >= 0
        && TopLeft.X >= 0
        && TopLeft.Y >= 0
        && EncounterSequence != 0
        && !FightId.IsNone
        && ProtectionState is FirstSeveranceCoreProtectionState.Preparing
            or FirstSeveranceCoreProtectionState.Active;
}

// The Tile Entity exposes only a synchronized busy/protection projection. This
// exact-Fight registry remains the authority-side owner of the projection lease.
internal sealed class FirstSeveranceCoreLeaseRegistry
{
    private FirstSeveranceCoreLease? current;

    public FirstSeveranceCoreLease? Current => current;

    public bool TryClaim(
        int serverTileEntityId,
        in TilePoint topLeft,
        ulong encounterSequence,
        FightId fightId)
    {
        var candidate = new FirstSeveranceCoreLease(
            serverTileEntityId,
            topLeft,
            encounterSequence,
            fightId,
            FirstSeveranceCoreProtectionState.Preparing,
            IsMissing: false);
        if (!candidate.IsValid)
        {
            return false;
        }

        if (current.HasValue)
        {
            return current.Value == candidate;
        }

        current = candidate;
        return true;
    }

    public bool TryEnterActive(int serverTileEntityId, FightId expectedFightId)
    {
        if (!TryGetExact(serverTileEntityId, expectedFightId, out FirstSeveranceCoreLease lease)
            || lease.IsMissing)
        {
            return false;
        }

        if (lease.ProtectionState == FirstSeveranceCoreProtectionState.Active)
        {
            return true;
        }

        if (lease.ProtectionState != FirstSeveranceCoreProtectionState.Preparing)
        {
            return false;
        }

        current = lease with { ProtectionState = FirstSeveranceCoreProtectionState.Active };
        return true;
    }

    public bool TryRecordMissing(int serverTileEntityId)
    {
        if (!current.HasValue || current.Value.ServerTileEntityId != serverTileEntityId)
        {
            return false;
        }

        if (current.Value.IsMissing)
        {
            return true;
        }

        current = current.Value with { IsMissing = true };
        return true;
    }

    public bool IsActivelyProtected(int serverTileEntityId)
    {
        return current.HasValue
            && current.Value.ServerTileEntityId == serverTileEntityId
            && current.Value.ProtectionState == FirstSeveranceCoreProtectionState.Active
            && !current.Value.IsMissing;
    }

    public bool TryRelease(int serverTileEntityId, FightId expectedFightId)
    {
        if (!current.HasValue)
        {
            return true;
        }

        if (!TryGetExact(serverTileEntityId, expectedFightId, out _))
        {
            return false;
        }

        current = null;
        return true;
    }

    public void ClearWorld()
    {
        current = null;
    }

    private bool TryGetExact(
        int serverTileEntityId,
        FightId expectedFightId,
        out FirstSeveranceCoreLease lease)
    {
        if (current.HasValue
            && current.Value.ServerTileEntityId == serverTileEntityId
            && current.Value.FightId == expectedFightId)
        {
            lease = current.Value;
            return true;
        }

        lease = default;
        return false;
    }
}
