#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Convergence.Content.Encounters.FirstSeverance;

internal sealed class FirstSeveranceResolvedPreparation
{
    public FirstSeveranceResolvedPreparation(
        FoundationCoreTileEntity core,
        FirstSeveranceArenaValidationResult arena,
        FirstSeveranceRoster roster)
    {
        Core = core ?? throw new ArgumentNullException(nameof(core));
        Arena = arena ?? throw new ArgumentNullException(nameof(arena));
        Roster = roster ?? throw new ArgumentNullException(nameof(roster));
        if (!arena.IsValid)
        {
            throw new ArgumentException("Resolved preparation requires a valid Arena.", nameof(arena));
        }
    }

    public FoundationCoreTileEntity Core { get; }

    public FirstSeveranceArenaValidationResult Arena { get; }

    public FirstSeveranceRoster Roster { get; }
}

// Reads Terraria World/player state on server or Single Player only. It performs
// no mutation; a factory must finish this complete resolution before claiming
// the Core or deploying a logical Barrier projection.
internal sealed class FirstSeveranceCoreResolver
{
    internal const int RequesterRangeInTiles = 12;
    internal const int ParticipationRadiusInTiles = 80;

    public static FirstSeveranceCoreResolver Instance { get; } = new();

    private FirstSeveranceCoreResolver()
    {
    }

    public bool TryResolve(
        in AcceptedEncounterStart start,
        out FirstSeveranceResolvedPreparation? resolved,
        out string failureCode)
    {
        resolved = null;
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            failureCode = "encounter.not_authority";
            return false;
        }

        if (!TryResolveCore(start.RequestedAnchor, out FoundationCoreTileEntity core))
        {
            failureCode = "first_severance.arena_core_not_resolved";
            return false;
        }

        if (core.ProtectionState != FirstSeveranceCoreProtectionState.Idle)
        {
            failureCode = "first_severance.core_busy";
            return false;
        }

        Point16 topLeft = core.Position;
        var logicalCenter = new TilePoint(topLeft.X + core.FootprintWidth / 2, topLeft.Y + core.FootprintHeight - 1);
        var resolvedCore = new ResolvedFirstSeveranceCoreAnchor(
            logicalCenter,
            BaseY: topLeft.Y + core.FootprintHeight,
            ServerTileEntityId: core.ID);
        var worldBounds = new TileRectangle(0, 0, Main.maxTilesX, Main.maxTilesY);
        if (!FirstSeveranceArenaBlueprint.Instance.TryCreateLayout(
                resolvedCore,
                worldBounds,
                out FirstSeveranceArenaLayout? layout,
                out failureCode)
            || layout is null)
        {
            return false;
        }

        if (!FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(
                start.RequesterWhoAmI,
                out ulong requesterEpoch))
        {
            failureCode = "first_severance.roster_requester_epoch_unavailable";
            return false;
        }

        IReadOnlyList<FirstSeveranceRosterCandidate> candidates = BuildCandidates(logicalCenter);
        int selectableCandidateCount = CountSelectable(candidates);
        bool requesterInRange = IsPlayerWithinTiles(
            Main.player[start.RequesterWhoAmI],
            logicalCenter,
            RequesterRangeInTiles);
        FirstSeveranceArenaScanMetrics metrics = ScanArena(
            core,
            layout,
            out FirstSeveranceArenaScanEvidence evidence);
        var survey = new FirstSeveranceArenaSurvey(
            layout,
            coreMatches: core.ID == resolvedCore.ServerTileEntityId
                && core.IsTileValidForEntity(topLeft.X, topLeft.Y),
            requesterIsWithinRange: requesterInRange,
            hasWorldConflict: HasWorldConflict(),
            eligibleCandidateCount: selectableCandidateCount,
            metrics,
            evidence);
        // A closed field needs genuinely empty flight space. The broad ground
        // does not need to be a manufactured flat 320-tile foundation.
        FirstSeveranceArenaValidationResult validation =
            FirstSeveranceDevelopmentPolicy.ValidateStartArena(survey);
        if (!validation.IsValid)
        {
            failureCode = validation.FirstErrorCode;
            return false;
        }

        if (!FirstSeveranceRoster.TryCreate(
                candidates,
                start.RequesterWhoAmI,
                requesterEpoch,
                out FirstSeveranceRoster? roster,
                out failureCode,
                allowSoloDebug: FirstSeveranceDevelopmentPolicy.AllowSoloDebugStart)
            || roster is null)
        {
            return false;
        }

        resolved = new FirstSeveranceResolvedPreparation(core, validation, roster);
        failureCode = string.Empty;
        return true;
    }

    private static bool TryResolveCore(
        in TilePoint requestedAnchor,
        out FoundationCoreTileEntity core)
    {
        core = null!;
        if (!WorldGen.InWorld(requestedAnchor.X, requestedAnchor.Y, 1))
        {
            return false;
        }

        Tile tile = Main.tile[requestedAnchor.X, requestedAnchor.Y];
        if (!tile.HasTile
            || !FoundationCoreTileEntity.IsCoreType(tile.TileType)
            || !TileEntity.TryGet(
                requestedAnchor.X,
                requestedAnchor.Y,
                out FoundationCoreTileEntity found))
        {
            return false;
        }

        core = found;
        return true;
    }

    private static IReadOnlyList<FirstSeveranceRosterCandidate> BuildCandidates(
        in TilePoint logicalCenter)
    {
        var candidates = new List<FirstSeveranceRosterCandidate>(
            FirstSeveranceRoster.MaximumCount + 1);
        for (int slot = 0; slot < Main.maxPlayers; slot++)
        {
            Player player = Main.player[slot];
            if (!player.active
                || !FirstSeveranceConnectionEpochSystem.TryGetCurrentEpoch(
                    slot,
                    out ulong epoch))
            {
                continue;
            }

            candidates.Add(new FirstSeveranceRosterCandidate(
                slot,
                epoch,
                IsConnected: true,
                IsEligible: !player.dead && !player.ghost,
                IsWithinParticipationRegion: IsPlayerWithinTiles(
                    player,
                    logicalCenter,
                    ParticipationRadiusInTiles)));
        }

        return candidates.AsReadOnly();
    }

    private static int CountSelectable(IReadOnlyList<FirstSeveranceRosterCandidate> candidates)
    {
        int count = 0;
        for (int index = 0; index < candidates.Count; index++)
        {
            if (candidates[index].IsSelectable)
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsPlayerWithinTiles(
        Player player,
        in TilePoint point,
        int maximumDistanceInTiles)
    {
        Vector2 center = player.Center;
        long playerTileX = (long)Math.Floor(center.X / 16f);
        long playerTileY = (long)Math.Floor(center.Y / 16f);
        long deltaX = playerTileX - point.X;
        long deltaY = playerTileY - point.Y;
        long maximum = maximumDistanceInTiles;
        return (deltaX * deltaX) + (deltaY * deltaY) <= maximum * maximum;
    }

    private static bool HasWorldConflict()
    {
        if (Main.invasionType > 0)
        {
            return true;
        }

        for (int index = 0; index < Main.maxNPCs; index++)
        {
            NPC npc = Main.npc[index];
            if (npc.active && npc.boss)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool RevalidateForCombat(FoundationCoreTileEntity core, FirstSeveranceArenaLayout layout, int count, out string failureCode)
    {
        var metrics = ScanArena(core, layout, out var evidence);
        var survey = new FirstSeveranceArenaSurvey(layout, core.IsTileValidForEntity(core.Position.X, core.Position.Y),
            true, HasWorldConflict(), count, metrics, evidence);
        var result = FirstSeveranceDevelopmentPolicy.ValidateStartArena(survey);
        failureCode = result.FirstErrorCode;
        return result.IsValid;
    }

    private static FirstSeveranceArenaScanMetrics ScanArena(
        FoundationCoreTileEntity core,
        FirstSeveranceArenaLayout layout,
        out FirstSeveranceArenaScanEvidence evidence)
    {
        long started = Stopwatch.GetTimestamp();
        TileRectangle bounds = layout.ArenaBounds;
        int scanned = 0;
        int solidInterior = 0;
        int liquids = 0;
        int containers = 0;
        int foreignTileEntities = 0;
        int additionalCores = 0;
        int protectedTiles = 0;
        int wireOrActuator = 0;
        int platformOrRope = 0;
        int spawnOrHousing = 0;
        int solidFoundation = 0;
        TilePoint? firstFoundationGap = null;
        TilePoint? firstSolid = null;
        TilePoint? firstLiquid = null;
        TilePoint? firstContainer = null;
        TilePoint? firstForeignTileEntity = null;
        TilePoint? firstAdditionalCore = null;
        TilePoint? firstProtected = null;
        TilePoint? firstWire = null;
        TilePoint? firstPlatform = null;
        TilePoint? firstSpawnOrHousing = null;

        for (int x = bounds.Left; x < bounds.Right; x++)
        {
            if (WorldGen.SolidTileAllowBottomSlope(x, layout.Core.BaseY))
            {
                solidFoundation++;
            }
            else
            {
                firstFoundationGap ??= new TilePoint(x, layout.Core.BaseY);
            }

            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                scanned++;
                Tile tile = Main.tile[x, y];
                var coordinate = new TilePoint(x, y);

                if (tile.LiquidAmount > 0)
                {
                    liquids++;
                    firstLiquid ??= coordinate;
                }

                if (tile.RedWire
                    || tile.BlueWire
                    || tile.GreenWire
                    || tile.YellowWire
                    || tile.HasActuator)
                {
                    wireOrActuator++;
                    firstWire ??= coordinate;
                }

                if (!tile.HasTile)
                {
                    continue;
                }

                int type = tile.TileType;
                if (type >= 0
                    && type < Main.tileSolid.Length
                    && Main.tileSolid[type]
                    && !Main.tileSolidTop[type]
                    && !tile.IsActuated)
                {
                    solidInterior++;
                    firstSolid ??= coordinate;
                }

                if (type >= 0
                    && type < TileID.Sets.IsAContainer.Length
                    && TileID.Sets.IsAContainer[type])
                {
                    containers++;
                    firstContainer ??= coordinate;
                }

                if (FoundationCoreTileEntity.IsCoreType(type)
                    && TileObjectData.IsTopLeft(x, y)
                    && (x != core.Position.X || y != core.Position.Y))
                {
                    additionalCores++;
                    firstAdditionalCore ??= coordinate;
                }

                if ((type >= 0 && type < Main.tileDungeon.Length && Main.tileDungeon[type])
                    || type == TileID.LihzahrdBrick)
                {
                    protectedTiles++;
                    firstProtected ??= coordinate;
                }

                if ((type >= 0
                        && type < TileID.Sets.Platforms.Length
                        && TileID.Sets.Platforms[type])
                    || IsRope(type))
                {
                    platformOrRope++;
                    firstPlatform ??= coordinate;
                }
            }
        }

        foreach (KeyValuePair<Point16, TileEntity> pair in TileEntity.ByPosition)
        {
            var position = new TilePoint(pair.Key.X, pair.Key.Y);
            if (!bounds.Contains(position) || pair.Value.ID == core.ID)
            {
                continue;
            }

            if (pair.Value is FoundationCoreTileEntity)
            {
                // A valid additional Core was already counted by its top-left Tile.
                continue;
            }

            foreignTileEntities++;
            firstForeignTileEntity ??= position;
        }

        var worldSpawn = new TilePoint(Main.spawnTileX, Main.spawnTileY);
        if (bounds.Contains(worldSpawn))
        {
            spawnOrHousing++;
            firstSpawnOrHousing = worldSpawn;
        }

        for (int index = 0; index < Main.maxNPCs; index++)
        {
            NPC npc = Main.npc[index];
            if (!npc.active || !npc.townNPC || npc.homeless)
            {
                continue;
            }

            var home = new TilePoint(npc.homeTileX, npc.homeTileY);
            if (bounds.Contains(home))
            {
                spawnOrHousing++;
                firstSpawnOrHousing ??= home;
            }
        }

        long durationMicroseconds = Math.Max(
            0,
            Stopwatch.GetElapsedTime(started).Ticks / 10);
        evidence = new FirstSeveranceArenaScanEvidence(
            firstFoundationGap,
            firstSolid,
            firstLiquid,
            firstContainer,
            firstForeignTileEntity,
            firstAdditionalCore,
            firstProtected,
            firstWire,
            firstPlatform,
            firstSpawnOrHousing);
        return new FirstSeveranceArenaScanMetrics(
            scanned,
            solidInterior,
            liquids,
            containers,
            foreignTileEntities,
            additionalCores,
            protectedTiles,
            wireOrActuator,
            platformOrRope,
            spawnOrHousing,
            solidFoundation,
            durationMicroseconds);
    }

    private static bool IsRope(int tileType)
    {
        return tileType is TileID.Rope
            or TileID.SilkRope
            or TileID.VineRope
            or TileID.WebRope;
    }
}
