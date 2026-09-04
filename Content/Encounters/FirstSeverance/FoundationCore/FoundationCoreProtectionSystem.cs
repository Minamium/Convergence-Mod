#nullable enable

using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance.FoundationCore;

internal sealed class FoundationCoreProtectionSystem : ModSystem
{
    private static readonly FirstSeveranceCoreLeaseRegistry Registry = new();

    internal static FirstSeveranceCoreLease? CurrentLease => Registry.Current;

    public override void ClearWorld()
    {
        Registry.ClearWorld();
    }

    public override void OnWorldUnload()
    {
        Registry.ClearWorld();
    }

    public override void Unload()
    {
        Registry.ClearWorld();
    }

    internal static bool TryClaim(
        FoundationCoreTileEntity core,
        ulong encounterSequence,
        FightId fightId)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient
            || !Registry.TryClaim(
                core.ID,
                new TilePoint(core.Position.X, core.Position.Y),
                encounterSequence,
                fightId))
        {
            return false;
        }

        try
        {
            SetProjection(core, FirstSeveranceCoreProtectionState.Preparing);
            return true;
        }
        catch
        {
            Registry.TryRelease(core.ID, fightId);
            throw;
        }
    }

    internal static bool TryEnterActive(int serverTileEntityId, FightId fightId)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient
            || !Registry.TryEnterActive(serverTileEntityId, fightId))
        {
            return false;
        }

        if (TileEntity.ByID.TryGetValue(serverTileEntityId, out TileEntity? entity)
            && entity is FoundationCoreTileEntity core)
        {
            SetProjection(core, FirstSeveranceCoreProtectionState.Active);
            return true;
        }

        Registry.TryRecordMissing(serverTileEntityId);
        return false;
    }

    internal static void RecordMissing(int serverTileEntityId)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Registry.TryRecordMissing(serverTileEntityId);
        }
    }

    internal static bool TryResolveOwnedCore(
        int serverTileEntityId,
        FightId fightId,
        out FoundationCoreTileEntity core)
    {
        FirstSeveranceCoreLease? current = Registry.Current;
        if (current is { IsMissing: false } lease
            && lease.ServerTileEntityId == serverTileEntityId
            && lease.FightId == fightId
            && TileEntity.ByID.TryGetValue(serverTileEntityId, out TileEntity? entity)
            && entity is FoundationCoreTileEntity candidate
            && candidate.Position.X == lease.TopLeft.X
            && candidate.Position.Y == lease.TopLeft.Y
            && candidate.IsTileValidForEntity(candidate.Position.X, candidate.Position.Y))
        {
            core = candidate;
            return true;
        }

        core = null!;
        return false;
    }

    internal static bool TryRelease(int serverTileEntityId, FightId fightId)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            return false;
        }

        FirstSeveranceCoreLease? before = Registry.Current;
        if (!Registry.TryRelease(serverTileEntityId, fightId))
        {
            return false;
        }

        if (before.HasValue
            && TileEntity.ByID.TryGetValue(serverTileEntityId, out TileEntity? entity)
            && entity is FoundationCoreTileEntity core)
        {
            SetProjection(core, FirstSeveranceCoreProtectionState.Idle);
        }

        return true;
    }

    private static void SetProjection(
        FoundationCoreTileEntity core,
        FirstSeveranceCoreProtectionState state)
    {
        core.SetProtectionState(state);
        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendData(MessageID.TileEntitySharing, number: core.ID);
        }
    }
}
