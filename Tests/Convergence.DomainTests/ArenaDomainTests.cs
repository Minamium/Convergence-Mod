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
    [DomainTest("Arena layout is Core-anchored")]
    private static void ArenaLayoutIsCoreAnchored()
    {
        var worldBounds = new TileRectangle(0, 0, 8_400, 2_400);
        var core = new ResolvedFirstSeveranceCoreAnchor(
            new TilePoint(4_200, 1_250),
            BaseY: 1_300,
            ServerTileEntityId: 7);

        bool created = FirstSeveranceArenaBlueprint.Instance.TryCreateLayout(
            core,
            worldBounds,
            out FirstSeveranceArenaLayout? layout,
            out string failureCode);
        if (!created || layout is null)
        {
            throw new InvalidOperationException($"Expected a valid layout, got '{failureCode}'.");
        }

        AssertEqual(new TileRectangle(4_120, 1_230, 160, 70), layout.ArenaBounds, "Arena bounds");
        AssertEqual(new TileRectangle(4_122, 1_232, 156, 66), layout.BarrierBounds, "Barrier bounds");
        AssertEqual(4, layout.Pylons.Count, "Pylon count");
        AssertEqual(new TilePoint(4_134, 1_244), layout.Pylons[0].TilePosition, "NW Pylon");
        AssertEqual(new TilePoint(4_265, 1_285), layout.Pylons[3].TilePosition, "SE Pylon");
    }

    [DomainTest("Arena rejects a World-edge Core")]
    private static void ArenaRejectsWorldEdgeCore()
    {
        var worldBounds = new TileRectangle(0, 0, 320, 141);
        var core = new ResolvedFirstSeveranceCoreAnchor(
            new TilePoint(160, 70),
            BaseY: 140,
            ServerTileEntityId: 1);

        bool created = FirstSeveranceArenaBlueprint.Instance.TryCreateLayout(
            core,
            worldBounds,
            out FirstSeveranceArenaLayout? layout,
            out string failureCode);
        AssertEqual(false, created, "World-edge layout creation");
        AssertEqual<FirstSeveranceArenaLayout?>(null, layout, "rejected Arena layout");
        AssertEqual(
            "first_severance.arena_world_edge_margin_violation",
            failureCode,
            "Arena rejection code");
    }

    [DomainTest("Outsider responses are deterministic")]
    private static void OutsiderResponsesAreDeterministic()
    {
        FirstSeveranceBoundaryRule? outsiderRule = null;
        IReadOnlyList<FirstSeveranceBoundaryRule> rules =
            FirstSeveranceArenaAccessPolicy.Instance.Rules;
        for (int index = 0; index < rules.Count; index++)
        {
            if (rules[index].ViolationKind == FirstSeveranceBoundaryViolationKind.OutsiderInsideBarrier)
            {
                outsiderRule = rules[index];
                break;
            }
        }

        if (outsiderRule is null)
        {
            throw new InvalidOperationException("Outsider boundary rule is missing.");
        }

        IReadOnlyList<FirstSeveranceBoundaryResponse> initial =
            outsiderRule.GetEligibleResponses(continuousTicks: 1, violationCount: 1);
        AssertEqual(2, initial.Count, "initial outsider response count");
        AssertEqual(FirstSeveranceBoundaryResponse.ServerWarning, initial[0], "first outsider response");
        AssertEqual(
            FirstSeveranceBoundaryResponse.SuppressEncounterInteraction,
            initial[1],
            "second outsider response");

        IReadOnlyList<FirstSeveranceBoundaryResponse> escalated =
            outsiderRule.GetEligibleResponses(continuousTicks: 120, violationCount: 3);
        AssertEqual(4, escalated.Count, "escalated outsider response count");
        AssertEqual(
            FirstSeveranceBoundaryResponse.ExcludeFromArena,
            escalated[3],
            "final outsider response");
        AssertEqual(false, FirstSeveranceArenaAccessPolicy.Instance.UsesLethalExclusion, "lethal exclusion");
    }

    [DomainTest("Arena occupant identity rejects slot reuse")]
    private static void ArenaOccupantIdentityRejectsSlotReuse()
    {
        var currentOutsider = new FirstSeveranceArenaOccupant(
            ServerWhoAmI: 8,
            ConnectionEpoch: 42,
            ParticipantId.Invalid);
        var staleOutsider = currentOutsider with { ConnectionEpoch = 0 };
        var sentinel = currentOutsider with { ServerWhoAmI = byte.MaxValue };

        AssertEqual(true, currentOutsider.IsValid, "current outsider identity");
        AssertEqual(false, staleOutsider.IsValid, "stale outsider identity");
        AssertEqual(false, sentinel.IsValid, "server sentinel identity");
        AssertEqual(false, currentOutsider.IsParticipant, "outsider participant flag");
    }

    [DomainTest("Arena warnings remain non-blocking")]
    private static void ArenaWarningsRemainNonBlocking()
    {
        FirstSeveranceArenaLayout layout = CreateValidArenaLayout();
        var metrics = new FirstSeveranceArenaScanMetrics(
            scannedTileCount: layout.ArenaBounds.Width * layout.ArenaBounds.Height,
            solidInteriorTileCount: 2,
            liquidTileCount: 3,
            containerTileCount: 0,
            foreignTileEntityCount: 0,
            additionalCoreCount: 0,
            protectedTileCount: 0,
            wireOrActuatorTileCount: 4,
            platformOrRopeTileCount: 5,
            spawnOrHousingConflictCount: 1,
            solidFoundationTileCount: layout.ArenaBounds.Width,
            scanDurationMicroseconds: 750);
        var firstSolid = new TilePoint(layout.ArenaBounds.Left + 1, layout.ArenaBounds.Top + 1);
        var evidence = new FirstSeveranceArenaScanEvidence(
            FirstFoundationGap: null,
            FirstSolidInterior: firstSolid,
            FirstLiquid: new TilePoint(firstSolid.X + 1, firstSolid.Y),
            FirstContainer: null,
            FirstForeignTileEntity: null,
            FirstAdditionalCore: null,
            FirstProtectedTile: null,
            FirstWireOrActuator: new TilePoint(firstSolid.X + 2, firstSolid.Y),
            FirstPlatformOrRope: new TilePoint(firstSolid.X + 3, firstSolid.Y),
            FirstSpawnOrHousingConflict: new TilePoint(firstSolid.X + 4, firstSolid.Y));
        var survey = new FirstSeveranceArenaSurvey(
            layout,
            coreMatches: true,
            requesterIsWithinRange: true,
            hasWorldConflict: false,
            eligibleCandidateCount: 3,
            metrics,
            evidence);

        FirstSeveranceArenaValidationResult result =
            FirstSeveranceArenaValidator.Instance.Validate(survey);
        AssertEqual(true, result.IsValid, "warning-only Arena validity");
        AssertEqual(string.Empty, result.FirstErrorCode, "warning-only first error");
        AssertEqual(5, result.Issues.Count, "warning count");
        AssertEqual(
            FirstSeveranceArenaIssueCodes.InteriorSolidWarning,
            result.Issues[0].Code,
            "first warning code");
        AssertEqual(firstSolid, result.Issues[0].FirstCoordinate, "first warning coordinate");
        AssertEqual(
            FirstSeveranceArenaIssueCodes.SpawnOrHousingWarning,
            result.Issues[4].Code,
            "last warning code");
        AssertEqual(750L, result.Metrics.ScanDurationMicroseconds, "scan duration evidence");
        var containment = FirstSeveranceArenaValidator.Instance.Validate(survey, FirstSeveranceArenaValidationMode.DevelopmentContainment);
        AssertEqual(false, containment.IsValid, "solid interior blocks a closed flight field");
        AssertEqual(FirstSeveranceArenaIssueCodes.InteriorBlocked, containment.FirstErrorCode, "actionable airspace failure");
    }

    [DomainTest("Arena fatal ordering is stable")]
    private static void ArenaFatalOrderingIsStable()
    {
        FirstSeveranceArenaLayout layout = CreateValidArenaLayout();
        var point = new TilePoint(layout.ArenaBounds.Left, layout.ArenaBounds.Bottom - 1);
        var metrics = new FirstSeveranceArenaScanMetrics(
            scannedTileCount: (layout.ArenaBounds.Width * layout.ArenaBounds.Height) - 1,
            solidInteriorTileCount: 0,
            liquidTileCount: 0,
            containerTileCount: 1,
            foreignTileEntityCount: 1,
            additionalCoreCount: 1,
            protectedTileCount: 1,
            wireOrActuatorTileCount: 0,
            platformOrRopeTileCount: 0,
            spawnOrHousingConflictCount: 0,
            solidFoundationTileCount: layout.ArenaBounds.Width - 1,
            scanDurationMicroseconds: 900);
        var evidence = new FirstSeveranceArenaScanEvidence(
            FirstFoundationGap: point,
            FirstSolidInterior: null,
            FirstLiquid: null,
            FirstContainer: point,
            FirstForeignTileEntity: point,
            FirstAdditionalCore: point,
            FirstProtectedTile: point,
            FirstWireOrActuator: null,
            FirstPlatformOrRope: null,
            FirstSpawnOrHousingConflict: null);
        var survey = new FirstSeveranceArenaSurvey(
            layout,
            coreMatches: false,
            requesterIsWithinRange: false,
            hasWorldConflict: true,
            eligibleCandidateCount: 5,
            metrics,
            evidence);

        FirstSeveranceArenaValidationResult result =
            FirstSeveranceArenaValidator.Instance.Validate(survey);
        string[] expectedCodes =
        {
            FirstSeveranceArenaIssueCodes.ScanIncomplete,
            FirstSeveranceArenaIssueCodes.CoreMismatch,
            FirstSeveranceArenaIssueCodes.RequesterOutOfRange,
            FirstSeveranceArenaIssueCodes.WorldConflict,
            FirstSeveranceArenaIssueCodes.SelectionRequired,
            FirstSeveranceArenaIssueCodes.FoundationIncomplete,
            FirstSeveranceArenaIssueCodes.ContainerPresent,
            FirstSeveranceArenaIssueCodes.AdditionalCorePresent,
            FirstSeveranceArenaIssueCodes.ForeignTileEntityPresent,
            FirstSeveranceArenaIssueCodes.ProtectedTilePresent,
        };

        AssertEqual(false, result.IsValid, "fatal Arena validity");
        AssertEqual(expectedCodes.Length, result.Issues.Count, "fatal issue count");
        AssertEqual(expectedCodes[0], result.FirstErrorCode, "first fatal code");
        for (int index = 0; index < expectedCodes.Length; index++)
        {
            AssertEqual(expectedCodes[index], result.Issues[index].Code, $"fatal issue[{index}]");
            AssertEqual(
                FirstSeveranceArenaIssueSeverity.Error,
                result.Issues[index].Severity,
                $"fatal severity[{index}]");
        }
    }
}
