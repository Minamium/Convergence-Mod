#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Content.Encounters.FirstSeverance;

internal static class FirstSeverancePreparationTimeline
{
    internal const int DeploymentTicks = 180;
    internal const int ReadyHoldTicks = 45;
}

internal sealed class FirstSeverancePreparationProjection
{
    public FirstSeverancePreparationProjection(
        ulong encounterSequence,
        FightId fightId,
        int serverTileEntityId,
        in TilePoint coreTopLeft,
        in TileRectangle arenaBounds,
        ulong enteredTick,
        ulong deadlineTick,
        bool combatGateClosed,
        IReadOnlyList<FirstSeverancePreparationMemberSnapshot> members)
    {
        if (encounterSequence == 0
            || fightId.IsNone
            || serverTileEntityId < 0
            || coreTopLeft.X < 0
            || coreTopLeft.Y < 0
            || !arenaBounds.IsValid
            || arenaBounds.Width != FirstSeveranceArenaBlueprint.WidthInTiles
            || arenaBounds.Height != FirstSeveranceArenaBlueprint.HeightInTiles
            || deadlineTick <= enteredTick)
        {
            throw new ArgumentException("Preparation projection identity or geometry is invalid.");
        }

        IReadOnlyList<FirstSeverancePreparationMemberSnapshot> memberCopy =
            FirstSeverancePlanCollections.Copy(members, nameof(members));
        if (memberCopy.Count is < FirstSeveranceRoster.MinimumCount
            or > FirstSeveranceRoster.MaximumCount)
        {
            throw new ArgumentOutOfRangeException(nameof(members));
        }

        var slots = new HashSet<int>();
        for (int index = 0; index < memberCopy.Count; index++)
        {
            FirstSeverancePreparationMemberSnapshot member = memberCopy[index];
            if (member.ParticipantId != new ParticipantId(checked((byte)index))
                || member.ServerWhoAmI is < 0
                    or >= FirstSeveranceArenaOccupant.TerrariaPlayerSlotCount
                || member.ConnectionEpoch == 0
                || !slots.Add(member.ServerWhoAmI))
            {
                throw new ArgumentException(
                    "Preparation projection members require contiguous IDs and unique bindings.",
                    nameof(members));
            }
        }

        EncounterSequence = encounterSequence;
        FightId = fightId;
        ServerTileEntityId = serverTileEntityId;
        CoreTopLeft = coreTopLeft;
        ArenaBounds = arenaBounds;
        EnteredTick = enteredTick;
        DeadlineTick = deadlineTick;
        CombatGateClosed = combatGateClosed;
        Members = memberCopy;
    }

    public ulong EncounterSequence { get; }

    public FightId FightId { get; }

    public int ServerTileEntityId { get; }

    public TilePoint CoreTopLeft { get; }

    public TileRectangle ArenaBounds { get; }

    public ulong EnteredTick { get; }
    public ulong ReadyOpensTick => EnteredTick + FirstSeverancePreparationTimeline.DeploymentTicks;
    public float GroundX => (ArenaBounds.Left + ArenaBounds.Width / 2) * 16f;
    public float GroundY => ArenaBounds.Bottom * 16f;

    public ulong DeadlineTick { get; }

    public bool CombatGateClosed { get; }

    public IReadOnlyList<FirstSeverancePreparationMemberSnapshot> Members { get; }

    public bool AreAllReady
    {
        get
        {
            for (int index = 0; index < Members.Count; index++)
            {
                if (!Members[index].IsReady)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool TryGetMemberByServerSlot(
        int serverWhoAmI,
        out FirstSeverancePreparationMemberSnapshot member)
    {
        for (int index = 0; index < Members.Count; index++)
        {
            if (Members[index].ServerWhoAmI == serverWhoAmI)
            {
                member = Members[index];
                return true;
            }
        }

        member = default;
        return false;
    }
}
