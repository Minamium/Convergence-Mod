#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Geometry;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceArenaIssueSeverity : byte
{
    Warning = 1,
    Error = 2,
}

internal static class FirstSeveranceArenaIssueCodes
{
    public const string ScanIncomplete = "first_severance.arena_scan_incomplete";
    public const string CoreMismatch = "first_severance.arena_core_mismatch";
    public const string RequesterOutOfRange = "first_severance.requester_out_of_range";
    public const string WorldConflict = "first_severance.world_conflict";
    public const string TooFewParticipants = "first_severance.roster_too_small";
    public const string SelectionRequired = "first_severance.roster_selection_required";
    public const string FoundationIncomplete = "first_severance.arena_foundation_incomplete";
    public const string ContainerPresent = "first_severance.arena_container_present";
    public const string AdditionalCorePresent = "first_severance.arena_additional_core_present";
    public const string ForeignTileEntityPresent = "first_severance.arena_foreign_tile_entity_present";
    public const string ProtectedTilePresent = "first_severance.arena_protected_tile_present";
    public const string InteriorSolidWarning = "first_severance.arena_interior_solid_warning";
    public const string LiquidWarning = "first_severance.arena_liquid_warning";
    public const string WireOrActuatorWarning = "first_severance.arena_wire_or_actuator_warning";
    public const string PlatformOrRopeWarning = "first_severance.arena_platform_or_rope_warning";
    public const string SpawnOrHousingWarning = "first_severance.arena_spawn_or_housing_warning";
}

internal readonly record struct FirstSeveranceArenaIssue
{
    public FirstSeveranceArenaIssue(
        string code,
        FirstSeveranceArenaIssueSeverity severity,
        TilePoint? firstCoordinate = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("An Arena issue requires a stable code.", nameof(code));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        Code = code;
        Severity = severity;
        FirstCoordinate = firstCoordinate;
    }

    public string Code { get; }

    public FirstSeveranceArenaIssueSeverity Severity { get; }

    public TilePoint? FirstCoordinate { get; }
}

internal readonly record struct FirstSeveranceArenaScanMetrics
{
    public FirstSeveranceArenaScanMetrics(
        int scannedTileCount,
        int solidInteriorTileCount,
        int liquidTileCount,
        int containerTileCount,
        int foreignTileEntityCount,
        int additionalCoreCount,
        int protectedTileCount,
        int wireOrActuatorTileCount,
        int platformOrRopeTileCount,
        int spawnOrHousingConflictCount,
        int solidFoundationTileCount,
        long scanDurationMicroseconds)
    {
        if (scannedTileCount < 0
            || solidInteriorTileCount < 0
            || liquidTileCount < 0
            || containerTileCount < 0
            || foreignTileEntityCount < 0
            || additionalCoreCount < 0
            || protectedTileCount < 0
            || wireOrActuatorTileCount < 0
            || platformOrRopeTileCount < 0
            || spawnOrHousingConflictCount < 0
            || solidFoundationTileCount < 0
            || scanDurationMicroseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scannedTileCount),
                "Arena scan metrics cannot be negative.");
        }

        ScannedTileCount = scannedTileCount;
        SolidInteriorTileCount = solidInteriorTileCount;
        LiquidTileCount = liquidTileCount;
        ContainerTileCount = containerTileCount;
        ForeignTileEntityCount = foreignTileEntityCount;
        AdditionalCoreCount = additionalCoreCount;
        ProtectedTileCount = protectedTileCount;
        WireOrActuatorTileCount = wireOrActuatorTileCount;
        PlatformOrRopeTileCount = platformOrRopeTileCount;
        SpawnOrHousingConflictCount = spawnOrHousingConflictCount;
        SolidFoundationTileCount = solidFoundationTileCount;
        ScanDurationMicroseconds = scanDurationMicroseconds;
    }

    public int ScannedTileCount { get; }

    public int SolidInteriorTileCount { get; }

    public int LiquidTileCount { get; }

    public int ContainerTileCount { get; }

    public int ForeignTileEntityCount { get; }

    public int AdditionalCoreCount { get; }

    public int ProtectedTileCount { get; }

    public int WireOrActuatorTileCount { get; }

    public int PlatformOrRopeTileCount { get; }

    public int SpawnOrHousingConflictCount { get; }

    public int SolidFoundationTileCount { get; }

    public long ScanDurationMicroseconds { get; }
}

internal readonly record struct FirstSeveranceArenaScanEvidence(
    TilePoint? FirstFoundationGap,
    TilePoint? FirstSolidInterior,
    TilePoint? FirstLiquid,
    TilePoint? FirstContainer,
    TilePoint? FirstForeignTileEntity,
    TilePoint? FirstAdditionalCore,
    TilePoint? FirstProtectedTile,
    TilePoint? FirstWireOrActuator,
    TilePoint? FirstPlatformOrRope,
    TilePoint? FirstSpawnOrHousingConflict);

internal sealed class FirstSeveranceArenaSurvey
{
    public FirstSeveranceArenaSurvey(
        FirstSeveranceArenaLayout layout,
        bool coreMatches,
        bool requesterIsWithinRange,
        bool hasWorldConflict,
        int eligibleCandidateCount,
        in FirstSeveranceArenaScanMetrics metrics,
        in FirstSeveranceArenaScanEvidence evidence)
    {
        if (eligibleCandidateCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eligibleCandidateCount));
        }

        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        CoreMatches = coreMatches;
        RequesterIsWithinRange = requesterIsWithinRange;
        HasWorldConflict = hasWorldConflict;
        EligibleCandidateCount = eligibleCandidateCount;
        Metrics = metrics;
        Evidence = evidence;
    }

    public FirstSeveranceArenaLayout Layout { get; }

    public bool CoreMatches { get; }

    public bool RequesterIsWithinRange { get; }

    public bool HasWorldConflict { get; }

    public int EligibleCandidateCount { get; }

    public FirstSeveranceArenaScanMetrics Metrics { get; }

    public FirstSeveranceArenaScanEvidence Evidence { get; }
}

internal sealed class FirstSeveranceArenaValidationResult
{
    public FirstSeveranceArenaValidationResult(
        FirstSeveranceArenaLayout layout,
        IReadOnlyList<FirstSeveranceArenaIssue> issues,
        in FirstSeveranceArenaScanMetrics metrics)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Issues = FirstSeverancePlanCollections.Copy(issues, nameof(issues));
        Metrics = metrics;

        bool isValid = true;
        for (int index = 0; index < Issues.Count; index++)
        {
            if (Issues[index].Severity == FirstSeveranceArenaIssueSeverity.Error)
            {
                isValid = false;
                break;
            }
        }

        IsValid = isValid;
    }

    public bool IsValid { get; }

    public FirstSeveranceArenaLayout Layout { get; }

    public IReadOnlyList<FirstSeveranceArenaIssue> Issues { get; }

    public FirstSeveranceArenaScanMetrics Metrics { get; }

    public string FirstErrorCode
    {
        get
        {
            for (int index = 0; index < Issues.Count; index++)
            {
                if (Issues[index].Severity == FirstSeveranceArenaIssueSeverity.Error)
                {
                    return Issues[index].Code;
                }
            }

            return string.Empty;
        }
    }
}

internal sealed class FirstSeveranceArenaValidator
{
    public static FirstSeveranceArenaValidator Instance { get; } = new();

    private FirstSeveranceArenaValidator()
    {
    }

    public FirstSeveranceArenaValidationResult Validate(FirstSeveranceArenaSurvey survey)
    {
        ArgumentNullException.ThrowIfNull(survey);

        FirstSeveranceArenaScanMetrics metrics = survey.Metrics;
        FirstSeveranceArenaScanEvidence evidence = survey.Evidence;
        var issues = new List<FirstSeveranceArenaIssue>();
        long expectedScanCount = (long)survey.Layout.ArenaBounds.Width
            * survey.Layout.ArenaBounds.Height;

        AddWhen(
            metrics.ScannedTileCount != expectedScanCount,
            FirstSeveranceArenaIssueCodes.ScanIncomplete,
            FirstSeveranceArenaIssueSeverity.Error,
            null,
            issues);
        AddWhen(
            !survey.CoreMatches,
            FirstSeveranceArenaIssueCodes.CoreMismatch,
            FirstSeveranceArenaIssueSeverity.Error,
            survey.Layout.Core.LogicalCenter,
            issues);
        AddWhen(
            !survey.RequesterIsWithinRange,
            FirstSeveranceArenaIssueCodes.RequesterOutOfRange,
            FirstSeveranceArenaIssueSeverity.Error,
            survey.Layout.Core.LogicalCenter,
            issues);
        AddWhen(
            survey.HasWorldConflict,
            FirstSeveranceArenaIssueCodes.WorldConflict,
            FirstSeveranceArenaIssueSeverity.Error,
            null,
            issues);
        AddWhen(
            survey.EligibleCandidateCount < FirstSeveranceRoster.MinimumCount,
            FirstSeveranceArenaIssueCodes.TooFewParticipants,
            FirstSeveranceArenaIssueSeverity.Error,
            null,
            issues);
        AddWhen(
            survey.EligibleCandidateCount > FirstSeveranceRoster.MaximumCount,
            FirstSeveranceArenaIssueCodes.SelectionRequired,
            FirstSeveranceArenaIssueSeverity.Error,
            null,
            issues);
        AddWhen(
            metrics.SolidFoundationTileCount != survey.Layout.ArenaBounds.Width,
            FirstSeveranceArenaIssueCodes.FoundationIncomplete,
            FirstSeveranceArenaIssueSeverity.Error,
            evidence.FirstFoundationGap,
            issues);
        AddWhen(
            metrics.ContainerTileCount > 0,
            FirstSeveranceArenaIssueCodes.ContainerPresent,
            FirstSeveranceArenaIssueSeverity.Error,
            evidence.FirstContainer,
            issues);
        AddWhen(
            metrics.AdditionalCoreCount > 0,
            FirstSeveranceArenaIssueCodes.AdditionalCorePresent,
            FirstSeveranceArenaIssueSeverity.Error,
            evidence.FirstAdditionalCore,
            issues);
        AddWhen(
            metrics.ForeignTileEntityCount > 0,
            FirstSeveranceArenaIssueCodes.ForeignTileEntityPresent,
            FirstSeveranceArenaIssueSeverity.Error,
            evidence.FirstForeignTileEntity,
            issues);
        AddWhen(
            metrics.ProtectedTileCount > 0,
            FirstSeveranceArenaIssueCodes.ProtectedTilePresent,
            FirstSeveranceArenaIssueSeverity.Error,
            evidence.FirstProtectedTile,
            issues);

        AddWhen(
            metrics.SolidInteriorTileCount > 0,
            FirstSeveranceArenaIssueCodes.InteriorSolidWarning,
            FirstSeveranceArenaIssueSeverity.Warning,
            evidence.FirstSolidInterior,
            issues);
        AddWhen(
            metrics.LiquidTileCount > 0,
            FirstSeveranceArenaIssueCodes.LiquidWarning,
            FirstSeveranceArenaIssueSeverity.Warning,
            evidence.FirstLiquid,
            issues);
        AddWhen(
            metrics.WireOrActuatorTileCount > 0,
            FirstSeveranceArenaIssueCodes.WireOrActuatorWarning,
            FirstSeveranceArenaIssueSeverity.Warning,
            evidence.FirstWireOrActuator,
            issues);
        AddWhen(
            metrics.PlatformOrRopeTileCount > 0,
            FirstSeveranceArenaIssueCodes.PlatformOrRopeWarning,
            FirstSeveranceArenaIssueSeverity.Warning,
            evidence.FirstPlatformOrRope,
            issues);
        AddWhen(
            metrics.SpawnOrHousingConflictCount > 0,
            FirstSeveranceArenaIssueCodes.SpawnOrHousingWarning,
            FirstSeveranceArenaIssueSeverity.Warning,
            evidence.FirstSpawnOrHousingConflict,
            issues);

        return new FirstSeveranceArenaValidationResult(
            survey.Layout,
            issues.AsReadOnly(),
            metrics);
    }

    private static void AddWhen(
        bool condition,
        string code,
        FirstSeveranceArenaIssueSeverity severity,
        TilePoint? coordinate,
        List<FirstSeveranceArenaIssue> issues)
    {
        if (condition)
        {
            issues.Add(new FirstSeveranceArenaIssue(code, severity, coordinate));
        }
    }
}
