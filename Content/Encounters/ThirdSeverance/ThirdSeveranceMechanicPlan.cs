#nullable enable

using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal enum ThirdSeverancePhaseId : byte
{
    None = 0,
    BaseActivation = 1,
    SealRelease = 2,
    PartBreak = 3,
    Coordination = 4,
    PersonalEffigies = 5,
    WeakPointExposure = 6,
    LastStand = 7,
}

internal enum ThirdSeverancePhaseExitRule : byte
{
    PreparationComplete = 1,
    MechanicResolvedOrDeadline = 2,
    AllScheduledMechanicsResolved = 3,
    BossLifeOrDeadline = 4,
    FixedSequenceComplete = 5,
}

internal enum ThirdSeveranceMechanicKind : byte
{
    PylonDpsCheck = 1,
    PartBreakWindow = 2,
    Stack = 3,
    Spread = 4,
    TargetedLine = 5,
    PersonalEffigy = 6,
    WeakPointExposure = 7,
    BossAttackPattern = 8,
}

internal enum ThirdSeveranceTargetRule : byte
{
    None = 0,
    AllParticipants = 1,
    AssignedParticipant = 2,
    DeterministicRandomParticipant = 3,
    FarthestParticipant = 4,
    HighestRecentServerDamageParticipant = 5,
}

internal enum ThirdSeveranceFailureOutcome : byte
{
    None = 0,
    ApplyOverload = 1,
    ApplyRaidDamageDown = 2,
    ApplyParticipantDebuff = 3,
    StrengthenBoss = 4,
    AdvanceHardEnrage = 5,
}

internal enum ThirdSeverancePylonSelectionRule : byte
{
    RosterMappedSymmetricSlots = 1,
}

internal readonly record struct ThirdSeveranceParticipantScaledInt(
    int TwoParticipants,
    int ThreeParticipants,
    int FourParticipants)
{
    public bool IsNonNegative => TwoParticipants >= 0
        && ThreeParticipants >= 0
        && FourParticipants >= 0;

    public bool FitsParticipantCount => IsNonNegative
        && TwoParticipants <= 2
        && ThreeParticipants <= 3
        && FourParticipants <= 4;

    public int GetValue(int participantCount)
    {
        return participantCount switch
        {
            2 => TwoParticipants,
            3 => ThreeParticipants,
            4 => FourParticipants,
            _ => throw new ArgumentOutOfRangeException(
                nameof(participantCount),
                "Third Severance supports exactly two to four participants."),
        };
    }
}

internal abstract class ThirdSeveranceMechanicDefinition
{
    protected ThirdSeveranceMechanicDefinition(
        string key,
        ThirdSeveranceMechanicKind kind,
        int startOffsetTicks,
        int durationTicks,
        ThirdSeveranceTargetRule targetRule,
        ThirdSeveranceFailureOutcome failureOutcome)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A mechanic requires a stable key.", nameof(key));
        }

        if (!Enum.IsDefined(kind) || !Enum.IsDefined(targetRule) || !Enum.IsDefined(failureOutcome))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), "Mechanic enum values must be defined.");
        }

        if (startOffsetTicks < 0 || durationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startOffsetTicks),
                "Mechanic timing must use a non-negative offset and positive duration.");
        }

        Key = key;
        Kind = kind;
        StartOffsetTicks = startOffsetTicks;
        DurationTicks = durationTicks;
        TargetRule = targetRule;
        FailureOutcome = failureOutcome;

        _ = checked(startOffsetTicks + durationTicks);
    }

    public string Key { get; }

    public ThirdSeveranceMechanicKind Kind { get; }

    public int StartOffsetTicks { get; }

    public int DurationTicks { get; }

    public int EndOffsetTicks => StartOffsetTicks + DurationTicks;

    public ThirdSeveranceTargetRule TargetRule { get; }

    public ThirdSeveranceFailureOutcome FailureOutcome { get; }
}

internal sealed class ThirdSeverancePylonDpsCheckDefinition : ThirdSeveranceMechanicDefinition
{
    public ThirdSeverancePylonDpsCheckDefinition(
        string key,
        int startOffsetTicks,
        int durationTicks,
        in ThirdSeveranceParticipantScaledInt activePylons,
        ThirdSeverancePylonSelectionRule slotSelectionRule,
        string serverDamageBudgetKey,
        bool usesParticipantAffinity,
        ThirdSeveranceFailureOutcome failureOutcome)
        : base(
            key,
            ThirdSeveranceMechanicKind.PylonDpsCheck,
            startOffsetTicks,
            durationTicks,
            ThirdSeveranceTargetRule.AssignedParticipant,
            failureOutcome)
    {
        if (!activePylons.FitsParticipantCount
            || activePylons.FourParticipants > ThirdSeveranceArenaBlueprint.RequiredPylonSlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(activePylons));
        }

        if (!Enum.IsDefined(slotSelectionRule))
        {
            throw new ArgumentOutOfRangeException(nameof(slotSelectionRule));
        }

        if (string.IsNullOrWhiteSpace(serverDamageBudgetKey))
        {
            throw new ArgumentException("A server-owned damage budget key is required.", nameof(serverDamageBudgetKey));
        }

        ActivePylons = activePylons;
        SlotSelectionRule = slotSelectionRule;
        ServerDamageBudgetKey = serverDamageBudgetKey;
        UsesParticipantAffinity = usesParticipantAffinity;
    }

    public ThirdSeveranceParticipantScaledInt ActivePylons { get; }

    public ThirdSeverancePylonSelectionRule SlotSelectionRule { get; }

    // Future authority code resolves this key to tuned HP. Clients never report DPS success.
    public string ServerDamageBudgetKey { get; }

    public bool UsesParticipantAffinity { get; }
}

internal sealed class ThirdSeverancePartBreakDefinition : ThirdSeveranceMechanicDefinition
{
    public ThirdSeverancePartBreakDefinition(
        string key,
        int startOffsetTicks,
        int durationTicks,
        IReadOnlyList<string> eligiblePartKeys,
        int maximumStrategicBreaks)
        : base(
            key,
            ThirdSeveranceMechanicKind.PartBreakWindow,
            startOffsetTicks,
            durationTicks,
            ThirdSeveranceTargetRule.AllParticipants,
            ThirdSeveranceFailureOutcome.None)
    {
        ArgumentNullException.ThrowIfNull(eligiblePartKeys);

        if (eligiblePartKeys.Count == 0)
        {
            throw new ArgumentException("A part break window requires eligible parts.", nameof(eligiblePartKeys));
        }

        if (maximumStrategicBreaks <= 0 || maximumStrategicBreaks > eligiblePartKeys.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStrategicBreaks));
        }

        EligiblePartKeys = ThirdSeverancePlanCollections.Copy(eligiblePartKeys, nameof(eligiblePartKeys));
        MaximumStrategicBreaks = maximumStrategicBreaks;
    }

    public IReadOnlyList<string> EligiblePartKeys { get; }

    public int MaximumStrategicBreaks { get; }
}

internal sealed class ThirdSeveranceStackDefinition : ThirdSeveranceMechanicDefinition
{
    public ThirdSeveranceStackDefinition(
        string key,
        int startOffsetTicks,
        int durationTicks,
        int radiusInTiles,
        in ThirdSeveranceParticipantScaledInt requiredParticipants,
        int successWeaknessStacks)
        : base(
            key,
            ThirdSeveranceMechanicKind.Stack,
            startOffsetTicks,
            durationTicks,
            ThirdSeveranceTargetRule.DeterministicRandomParticipant,
            ThirdSeveranceFailureOutcome.ApplyRaidDamageDown)
    {
        if (radiusInTiles <= 0
            || !requiredParticipants.FitsParticipantCount
            || successWeaknessStacks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusInTiles));
        }

        RadiusInTiles = radiusInTiles;
        RequiredParticipants = requiredParticipants;
        SuccessWeaknessStacks = successWeaknessStacks;
    }

    public int RadiusInTiles { get; }

    public ThirdSeveranceParticipantScaledInt RequiredParticipants { get; }

    public int SuccessWeaknessStacks { get; }
}

internal sealed class ThirdSeveranceSpreadDefinition : ThirdSeveranceMechanicDefinition
{
    public ThirdSeveranceSpreadDefinition(
        string key,
        int startOffsetTicks,
        int durationTicks,
        int minimumSeparationInTiles,
        int markerRadiusInTiles)
        : base(
            key,
            ThirdSeveranceMechanicKind.Spread,
            startOffsetTicks,
            durationTicks,
            ThirdSeveranceTargetRule.AllParticipants,
            ThirdSeveranceFailureOutcome.ApplyParticipantDebuff)
    {
        if (minimumSeparationInTiles <= 0 || markerRadiusInTiles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSeparationInTiles));
        }

        MinimumSeparationInTiles = minimumSeparationInTiles;
        MarkerRadiusInTiles = markerRadiusInTiles;
    }

    public int MinimumSeparationInTiles { get; }

    public int MarkerRadiusInTiles { get; }
}

internal sealed class ThirdSeveranceTargetedLineDefinition : ThirdSeveranceMechanicDefinition
{
    public ThirdSeveranceTargetedLineDefinition(
        string key,
        int startOffsetTicks,
        int durationTicks,
        int telegraphTicks,
        int lineWidthInTiles,
        ThirdSeveranceTargetRule targetRule)
        : base(
            key,
            ThirdSeveranceMechanicKind.TargetedLine,
            startOffsetTicks,
            durationTicks,
            targetRule,
            ThirdSeveranceFailureOutcome.ApplyParticipantDebuff)
    {
        if (targetRule is ThirdSeveranceTargetRule.None or ThirdSeveranceTargetRule.AllParticipants)
        {
            throw new ArgumentOutOfRangeException(nameof(targetRule));
        }

        if (telegraphTicks <= 0 || telegraphTicks >= durationTicks || lineWidthInTiles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegraphTicks));
        }

        TelegraphTicks = telegraphTicks;
        LineWidthInTiles = lineWidthInTiles;
    }

    public int TelegraphTicks { get; }

    public int LineWidthInTiles { get; }
}

internal sealed class ThirdSeverancePersonalEffigyDefinition : ThirdSeveranceMechanicDefinition
{
    public ThirdSeverancePersonalEffigyDefinition(
        string key,
        int startOffsetTicks,
        int durationTicks,
        int ownerDamagePermille,
        int assistDamagePermille,
        int failureStrengthStacks,
        string serverClassResolutionKey,
        IReadOnlyList<string> attackArchetypeKeys)
        : base(
            key,
            ThirdSeveranceMechanicKind.PersonalEffigy,
            startOffsetTicks,
            durationTicks,
            ThirdSeveranceTargetRule.AllParticipants,
            ThirdSeveranceFailureOutcome.StrengthenBoss)
    {
        ArgumentNullException.ThrowIfNull(attackArchetypeKeys);

        if (ownerDamagePermille <= 0
            || assistDamagePermille < 0
            || assistDamagePermille > ownerDamagePermille
            || failureStrengthStacks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerDamagePermille));
        }

        if (string.IsNullOrWhiteSpace(serverClassResolutionKey) || attackArchetypeKeys.Count == 0)
        {
            throw new ArgumentException("Effigies require a server class resolver and attack archetypes.");
        }

        OwnerDamagePermille = ownerDamagePermille;
        AssistDamagePermille = assistDamagePermille;
        FailureStrengthStacks = failureStrengthStacks;
        ServerClassResolutionKey = serverClassResolutionKey;
        AttackArchetypeKeys = ThirdSeverancePlanCollections.Copy(
            attackArchetypeKeys,
            nameof(attackArchetypeKeys));
    }

    public int OwnerDamagePermille { get; }

    public int AssistDamagePermille { get; }

    public int FailureStrengthStacks { get; }

    public string ServerClassResolutionKey { get; }

    public IReadOnlyList<string> AttackArchetypeKeys { get; }
}

internal sealed class ThirdSeveranceWeakPointExposureDefinition : ThirdSeveranceMechanicDefinition
{
    public ThirdSeveranceWeakPointExposureDefinition(
        string key,
        int startOffsetTicks,
        int durationTicks,
        string weakPointKey,
        string serverDamageBudgetKey,
        ThirdSeveranceFailureOutcome failureOutcome)
        : base(
            key,
            ThirdSeveranceMechanicKind.WeakPointExposure,
            startOffsetTicks,
            durationTicks,
            ThirdSeveranceTargetRule.AllParticipants,
            failureOutcome)
    {
        if (string.IsNullOrWhiteSpace(weakPointKey) || string.IsNullOrWhiteSpace(serverDamageBudgetKey))
        {
            throw new ArgumentException("Weak point and damage budget keys are required.");
        }

        WeakPointKey = weakPointKey;
        ServerDamageBudgetKey = serverDamageBudgetKey;
    }

    public string WeakPointKey { get; }

    public string ServerDamageBudgetKey { get; }
}

internal sealed class ThirdSeveranceBossAttackPatternDefinition : ThirdSeveranceMechanicDefinition
{
    public ThirdSeveranceBossAttackPatternDefinition(
        string key,
        int startOffsetTicks,
        int durationTicks,
        string patternKey,
        ThirdSeveranceTargetRule targetRule,
        ThirdSeveranceFailureOutcome failureOutcome)
        : base(
            key,
            ThirdSeveranceMechanicKind.BossAttackPattern,
            startOffsetTicks,
            durationTicks,
            targetRule,
            failureOutcome)
    {
        if (string.IsNullOrWhiteSpace(patternKey))
        {
            throw new ArgumentException("An attack pattern requires a stable key.", nameof(patternKey));
        }

        PatternKey = patternKey;
    }

    public string PatternKey { get; }
}

internal sealed class ThirdSeverancePhaseDefinition
{
    // A phase-entry count is deliberately bounded. The authority executor must
    // apply the plan's LoopExhaustionOutcome before exceeding this per-phase cap.
    public const int MaximumSupportedVisits = 16;

    public ThirdSeverancePhaseDefinition(
        ThirdSeverancePhaseId id,
        string bossFormKey,
        int maximumDurationTicks,
        ThirdSeverancePhaseExitRule exitRule,
        ThirdSeverancePhaseId nextPhaseOnResolution,
        ThirdSeverancePhaseId nextPhaseOnSoftFailure,
        int maximumVisits,
        bool canBeInterruptedByLastStand,
        IReadOnlyList<ThirdSeveranceMechanicDefinition> mechanics)
    {
        ArgumentNullException.ThrowIfNull(mechanics);

        if (id == ThirdSeverancePhaseId.None || !Enum.IsDefined(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (string.IsNullOrWhiteSpace(bossFormKey))
        {
            throw new ArgumentException("A phase requires a boss form key.", nameof(bossFormKey));
        }

        if (maximumDurationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDurationTicks));
        }

        if (maximumVisits is <= 0 or > MaximumSupportedVisits)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVisits));
        }

        if (!Enum.IsDefined(exitRule))
        {
            throw new ArgumentOutOfRangeException(nameof(exitRule));
        }

        for (int index = 0; index < mechanics.Count; index++)
        {
            if (mechanics[index].EndOffsetTicks > maximumDurationTicks)
            {
                throw new ArgumentException(
                    $"Mechanic '{mechanics[index].Key}' exceeds phase '{id}'.",
                    nameof(mechanics));
            }
        }

        Id = id;
        BossFormKey = bossFormKey;
        MaximumDurationTicks = maximumDurationTicks;
        ExitRule = exitRule;
        NextPhaseOnResolution = nextPhaseOnResolution;
        NextPhaseOnSoftFailure = nextPhaseOnSoftFailure;
        MaximumVisits = maximumVisits;
        CanBeInterruptedByLastStand = canBeInterruptedByLastStand;
        Mechanics = ThirdSeverancePlanCollections.Copy(mechanics, nameof(mechanics));
    }

    public ThirdSeverancePhaseId Id { get; }

    public string BossFormKey { get; }

    public int MaximumDurationTicks { get; }

    public ThirdSeverancePhaseExitRule ExitRule { get; }

    public ThirdSeverancePhaseId NextPhaseOnResolution { get; }

    public ThirdSeverancePhaseId NextPhaseOnSoftFailure { get; }

    public int MaximumVisits { get; }

    public bool CanBeInterruptedByLastStand { get; }

    public IReadOnlyList<ThirdSeveranceMechanicDefinition> Mechanics { get; }
}
