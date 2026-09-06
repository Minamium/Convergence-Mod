#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Geometry;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeverancePylonSlot : byte
{
    NorthWest = 1,
    NorthEast = 2,
    SouthWest = 3,
    SouthEast = 4,
}

// This DTO records what the future server-side resolver found. Its name does not
// claim that the Core or prospective Arena has passed validation.
internal readonly record struct ResolvedFirstSeveranceCoreAnchor(
    TilePoint LogicalCenter,
    int BaseY,
    int ServerTileEntityId)
{
    public bool HasServerTileEntityIdentity => ServerTileEntityId >= 0;
}

internal readonly record struct FirstSeverancePylonPosition(
    FirstSeverancePylonSlot Slot,
    TilePoint TilePosition);

internal sealed class FirstSeveranceArenaLayout
{
    public FirstSeveranceArenaLayout(
        in ResolvedFirstSeveranceCoreAnchor core,
        in TileRectangle worldBounds,
        in TileRectangle arenaBounds,
        in TileRectangle barrierBounds,
        IReadOnlyList<FirstSeverancePylonPosition> pylons)
    {
        if (!core.HasServerTileEntityIdentity)
        {
            throw new ArgumentException("The arena requires a resolved Core Tile Entity identity.", nameof(core));
        }

        if (!worldBounds.IsValid || !arenaBounds.IsValid || !barrierBounds.IsValid)
        {
            throw new ArgumentException("World, Arena, and Barrier bounds must be valid rectangles.");
        }

        var coreBasePoint = new TilePoint(core.LogicalCenter.X, core.BaseY);
        if (worldBounds.Left < 0
            || worldBounds.Top < 0
            || !HasIntSafeEdges(worldBounds)
            || !HasIntSafeEdges(arenaBounds)
            || !ContainsRectangle(worldBounds, arenaBounds)
            || !ContainsRectangleWithInset(
                worldBounds,
                arenaBounds,
                FirstSeveranceArenaBlueprint.WorldEdgeSafetyMarginInTiles)
            || !HasExpectedGeometry(core, arenaBounds, barrierBounds)
            || !worldBounds.Contains(core.LogicalCenter)
            || !worldBounds.Contains(coreBasePoint)
            || !arenaBounds.Contains(core.LogicalCenter))
        {
            throw new ArgumentException(
                "The resolved Core and complete prospective Arena must satisfy World bounds and edge margin.");
        }

        if (pylons is null || pylons.Count != FirstSeveranceArenaBlueprint.RequiredPylonSlotCount)
        {
            throw new ArgumentException("The arena layout requires four logical Pylon slots.", nameof(pylons));
        }

        if (!arenaBounds.Contains(new TilePoint(barrierBounds.Left, barrierBounds.Top))
            || !arenaBounds.Contains(new TilePoint(barrierBounds.Right - 1, barrierBounds.Bottom - 1)))
        {
            throw new ArgumentException("The logical Barrier must be inside the Arena.", nameof(barrierBounds));
        }

        var pylonCopy = new FirstSeverancePylonPosition[pylons.Count];
        var slots = new HashSet<FirstSeverancePylonSlot>();
        for (int index = 0; index < pylons.Count; index++)
        {
            FirstSeverancePylonPosition pylon = pylons[index];
            if (!Enum.IsDefined(pylon.Slot)
                || !slots.Add(pylon.Slot)
                || !barrierBounds.Contains(pylon.TilePosition)
                || !HasExpectedPylonPosition(pylon, arenaBounds))
            {
                throw new ArgumentException(
                    "Pylon slots must be unique, defined, and inside the Barrier.",
                    nameof(pylons));
            }

            pylonCopy[index] = pylon;
        }

        Core = core;
        WorldBounds = worldBounds;
        ArenaBounds = arenaBounds;
        BarrierBounds = barrierBounds;
        Pylons = Array.AsReadOnly(pylonCopy);
    }

    public ResolvedFirstSeveranceCoreAnchor Core { get; }

    public TileRectangle WorldBounds { get; }

    public TileRectangle ArenaBounds { get; }

    // This is a logical movement/admission boundary. It is not a generated Tile wall.
    public TileRectangle BarrierBounds { get; }

    public IReadOnlyList<FirstSeverancePylonPosition> Pylons { get; }

    private static bool HasIntSafeEdges(in TileRectangle bounds)
    {
        long right = (long)bounds.Left + bounds.Width;
        long bottom = (long)bounds.Top + bounds.Height;
        return right <= int.MaxValue && bottom <= int.MaxValue;
    }

    private static bool ContainsRectangle(
        in TileRectangle outer,
        in TileRectangle inner)
    {
        long outerRight = (long)outer.Left + outer.Width;
        long outerBottom = (long)outer.Top + outer.Height;
        long innerRight = (long)inner.Left + inner.Width;
        long innerBottom = (long)inner.Top + inner.Height;
        return inner.Left >= outer.Left
            && inner.Top >= outer.Top
            && innerRight <= outerRight
            && innerBottom <= outerBottom;
    }

    private static bool ContainsRectangleWithInset(
        in TileRectangle outer,
        in TileRectangle inner,
        int inset)
    {
        long safeLeft = (long)outer.Left + inset;
        long safeTop = (long)outer.Top + inset;
        long safeRight = (long)outer.Left + outer.Width - inset;
        long safeBottom = (long)outer.Top + outer.Height - inset;
        long innerRight = (long)inner.Left + inner.Width;
        long innerBottom = (long)inner.Top + inner.Height;
        return inset >= 0
            && safeLeft <= safeRight
            && safeTop <= safeBottom
            && inner.Left >= safeLeft
            && inner.Top >= safeTop
            && innerRight <= safeRight
            && innerBottom <= safeBottom;
    }

    private static bool HasExpectedGeometry(
        in ResolvedFirstSeveranceCoreAnchor core,
        in TileRectangle arenaBounds,
        in TileRectangle barrierBounds)
    {
        return arenaBounds.Width == FirstSeveranceArenaBlueprint.WidthInTiles
            && arenaBounds.Height == FirstSeveranceArenaBlueprint.HeightInTiles
            && (long)arenaBounds.Left + (arenaBounds.Width / 2) == core.LogicalCenter.X
            && (long)arenaBounds.Top + arenaBounds.Height == core.BaseY
            && barrierBounds.Left == arenaBounds.Left + FirstSeveranceArenaBlueprint.BarrierInsetInTiles
            && barrierBounds.Top == arenaBounds.Top + FirstSeveranceArenaBlueprint.BarrierInsetInTiles
            && barrierBounds.Width == arenaBounds.Width
                - (FirstSeveranceArenaBlueprint.BarrierInsetInTiles * 2)
            && barrierBounds.Height == arenaBounds.Height
                - (FirstSeveranceArenaBlueprint.BarrierInsetInTiles * 2);
    }

    private static bool HasExpectedPylonPosition(
        in FirstSeverancePylonPosition pylon,
        in TileRectangle arenaBounds)
    {
        int westX = arenaBounds.Left + FirstSeveranceArenaBlueprint.PylonInsetInTiles;
        int eastX = arenaBounds.Right - FirstSeveranceArenaBlueprint.PylonInsetInTiles - 1;
        int northY = arenaBounds.Top + FirstSeveranceArenaBlueprint.PylonInsetInTiles;
        int southY = arenaBounds.Bottom - FirstSeveranceArenaBlueprint.PylonInsetInTiles - 1;
        TilePoint expected = pylon.Slot switch
        {
            FirstSeverancePylonSlot.NorthWest => new TilePoint(westX, northY),
            FirstSeverancePylonSlot.NorthEast => new TilePoint(eastX, northY),
            FirstSeverancePylonSlot.SouthWest => new TilePoint(westX, southY),
            FirstSeverancePylonSlot.SouthEast => new TilePoint(eastX, southY),
            _ => default,
        };

        return pylon.TilePosition == expected;
    }
}

internal sealed class FirstSeveranceArenaBlueprint
{
    public const int WidthInTiles = 160;
    public const int HeightInTiles = 70;
    public const int BarrierInsetInTiles = 2;
    public const int PylonInsetInTiles = 14;
    public const int WorldEdgeSafetyMarginInTiles = 20;
    public const int RequiredPylonSlotCount = 4;

    public static FirstSeveranceArenaBlueprint Instance { get; } = new();

    private FirstSeveranceArenaBlueprint()
    {
    }

    public ArenaProfile Profile => new(WidthInTiles, HeightInTiles);

    public bool TryCreateLayout(
        in ResolvedFirstSeveranceCoreAnchor resolvedCore,
        in TileRectangle worldBounds,
        out FirstSeveranceArenaLayout? layout,
        out string failureCode)
    {
        layout = null;

        if (!resolvedCore.HasServerTileEntityIdentity)
        {
            failureCode = "first_severance.arena_core_not_resolved";
            return false;
        }

        if (!TryGetRectangleEdges(
                worldBounds,
                out int worldLeft,
                out int worldTop,
                out int worldRight,
                out int worldBottom)
            || worldLeft < 0
            || worldTop < 0)
        {
            failureCode = "first_severance.arena_world_bounds_invalid";
            return false;
        }

        if (!ContainsPoint(
                worldLeft,
                worldTop,
                worldRight,
                worldBottom,
                resolvedCore.LogicalCenter))
        {
            failureCode = "first_severance.arena_core_center_outside_world";
            return false;
        }

        var coreBasePoint = new TilePoint(resolvedCore.LogicalCenter.X, resolvedCore.BaseY);
        if (!ContainsPoint(worldLeft, worldTop, worldRight, worldBottom, coreBasePoint))
        {
            failureCode = "first_severance.arena_core_base_outside_world";
            return false;
        }

        int arenaLeft;
        int arenaTop;
        int arenaRight;
        int arenaBottom;
        try
        {
            checked
            {
                // The resolved Core is the horizontal origin and lower foundation
                // anchor. The 160x70 playable volume extends upward from BaseY.
                arenaLeft = resolvedCore.LogicalCenter.X - (WidthInTiles / 2);
                arenaTop = resolvedCore.BaseY - HeightInTiles;
                arenaRight = arenaLeft + WidthInTiles;
                arenaBottom = arenaTop + HeightInTiles;
            }
        }
        catch (OverflowException)
        {
            failureCode = "first_severance.arena_bounds_overflow";
            return false;
        }

        long safeWorldLeft = (long)worldLeft + WorldEdgeSafetyMarginInTiles;
        long safeWorldTop = (long)worldTop + WorldEdgeSafetyMarginInTiles;
        long safeWorldRight = (long)worldRight - WorldEdgeSafetyMarginInTiles;
        long safeWorldBottom = (long)worldBottom - WorldEdgeSafetyMarginInTiles;
        if (arenaLeft < safeWorldLeft
            || arenaTop < safeWorldTop
            || arenaRight > safeWorldRight
            || arenaBottom > safeWorldBottom)
        {
            failureCode = "first_severance.arena_world_edge_margin_violation";
            return false;
        }

        var arenaBounds = new TileRectangle(
            arenaLeft,
            arenaTop,
            WidthInTiles,
            HeightInTiles);
        if (!arenaBounds.Contains(resolvedCore.LogicalCenter))
        {
            failureCode = "first_severance.arena_core_center_outside_arena";
            return false;
        }

        var barrierBounds = new TileRectangle(
            arenaBounds.Left + BarrierInsetInTiles,
            arenaBounds.Top + BarrierInsetInTiles,
            arenaBounds.Width - (BarrierInsetInTiles * 2),
            arenaBounds.Height - (BarrierInsetInTiles * 2));

        var pylons = Array.AsReadOnly(
            new[]
            {
                CreatePylonPosition(FirstSeverancePylonSlot.NorthWest, arenaBounds),
                CreatePylonPosition(FirstSeverancePylonSlot.NorthEast, arenaBounds),
                CreatePylonPosition(FirstSeverancePylonSlot.SouthWest, arenaBounds),
                CreatePylonPosition(FirstSeverancePylonSlot.SouthEast, arenaBounds),
            });

        layout = new FirstSeveranceArenaLayout(
            resolvedCore,
            worldBounds,
            arenaBounds,
            barrierBounds,
            pylons);
        failureCode = string.Empty;
        return true;
    }

    private static bool TryGetRectangleEdges(
        in TileRectangle bounds,
        out int left,
        out int top,
        out int right,
        out int bottom)
    {
        left = bounds.Left;
        top = bounds.Top;
        right = 0;
        bottom = 0;

        if (!bounds.IsValid)
        {
            return false;
        }

        try
        {
            checked
            {
                right = bounds.Left + bounds.Width;
                bottom = bounds.Top + bounds.Height;
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        return true;
    }

    private static bool ContainsPoint(
        int left,
        int top,
        int right,
        int bottom,
        in TilePoint point)
    {
        return point.X >= left
            && point.X < right
            && point.Y >= top
            && point.Y < bottom;
    }

    private static FirstSeverancePylonPosition CreatePylonPosition(
        FirstSeverancePylonSlot slot,
        in TileRectangle bounds)
    {
        int westX = bounds.Left + PylonInsetInTiles;
        int eastX = bounds.Right - PylonInsetInTiles - 1;
        int northY = bounds.Top + PylonInsetInTiles;
        int southY = bounds.Bottom - PylonInsetInTiles - 1;

        TilePoint position = slot switch
        {
            FirstSeverancePylonSlot.NorthWest => new TilePoint(westX, northY),
            FirstSeverancePylonSlot.NorthEast => new TilePoint(eastX, northY),
            FirstSeverancePylonSlot.SouthWest => new TilePoint(westX, southY),
            FirstSeverancePylonSlot.SouthEast => new TilePoint(eastX, southY),
            _ => throw new ArgumentOutOfRangeException(nameof(slot)),
        };

        return new FirstSeverancePylonPosition(slot, position);
    }
}
