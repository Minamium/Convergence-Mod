#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;

namespace Convergence.Content.Encounters.FirstSeverance;

internal enum FirstSeveranceMechanicResult : byte
{
    None = 0,
    StackPassed = 1,
    StackFailed = 2,
    SpreadPassed = 3,
    SpreadFailed = 4,
}

internal readonly record struct FirstSeveranceCombatParticipantProjection(
    ParticipantId ParticipantId,
    int ServerWhoAmI,
    bool IsConnected,
    RaidParticipantCombatState CombatState,
    bool IsReviving,
    ulong DownedDeadlineTick,
    ulong ReviveCompletesTick,
    uint HealthRevision,
    int Life,
    float AnchorX,
    float AnchorY,
    ulong InvulnerabilityUntilTick,
    ulong WeaknessUntilTick,
    ulong ReviveLockoutUntilTick = 0,
    bool DebugAssistProtected = false);

// Cosmetic result recipients sampled BEFORE damage/teleports. Never a hit request.
internal readonly record struct FirstSeveranceMechanicImpact(ParticipantId ParticipantId, float X, float Y, bool Failed);

internal sealed class FirstSeveranceCombatProjection
{
    public FirstSeveranceCombatProjection(
        ulong encounterSequence,
        FightId fightId,
        FirstSeveranceSubstate substate,
        ulong resolveTick,
        int zeroBasedLoopIndex,
        int remainingPylons,
        int bossLife,
        int bossMaximumLife,
        int remainingReviveTokens,
        uint reviveRevision,
        float stackX,
        float stackY,
        float coreX,
        float coreY,
        FirstSeveranceMechanicResult lastMechanicResult,
        uint mechanicRevision,
        IReadOnlyList<FirstSeveranceCombatParticipantProjection> participants,
        FirstSeveranceLanceVolley? lanceVolley = null,
        FirstSeveranceBossPhase bossPhase = FirstSeveranceBossPhase.Sealed,
        ulong bossPhaseStartedTick = 0,
        FirstSeveranceGridVolley? gridVolley = null,
        ulong actionStartedTick = 0, int actionIndex = -1, int completedPhaseCycles = 0,
        ulong mechanicTick = 0, IReadOnlyList<FirstSeveranceMechanicImpact>? mechanicImpacts = null,
        IReadOnlyList<FirstSeveranceLanceVolley>? spreadLances = null,
        FirstSeveranceLanceVolley? carriedLance = null,
        FirstSeveranceCoreCannonVolley? coreCannon = null)
    {
        if (encounterSequence == 0
            || fightId.IsNone
            || substate == FirstSeveranceSubstate.None
            || !Enum.IsDefined(substate)
            || resolveTick == 0
            || zeroBasedLoopIndex < 0
            || remainingPylons < 0
            || bossMaximumLife <= 0
            || bossLife is < 0 || bossLife > bossMaximumLife
            || remainingReviveTokens < 0
            || !float.IsFinite(stackX) || !float.IsFinite(stackY)
            || Math.Abs(stackX) > 1_000_000 || Math.Abs(stackY) > 1_000_000
            || !Enum.IsDefined(bossPhase)
            || !FirstSeveranceChoreography.IsValidStep(bossPhase, substate, actionIndex)
            || actionStartedTick >= resolveTick || completedPhaseCycles is < 0 or > 255
            || (bossPhase != FirstSeveranceBossPhase.Sealed && (bossPhaseStartedTick == 0 || bossPhaseStartedTick >= resolveTick))
            || (bossPhase == FirstSeveranceBossPhase.Final && bossLife >
                (substate == FirstSeveranceSubstate.FinalCoreCheck ? FirstSeveranceFinalCheck.Life(bossMaximumLife) : 0))
            || !float.IsFinite(coreX) || !float.IsFinite(coreY)
            || Math.Abs(coreX) > 1_000_000 || Math.Abs(coreY) > 1_000_000
            || !Enum.IsDefined(lastMechanicResult))
        {
            throw new ArgumentException("The First Severance combat projection is invalid.");
        }

        EncounterSequence = encounterSequence;
        FightId = fightId;
        Substate = substate;
        ResolveTick = resolveTick;
        ZeroBasedLoopIndex = zeroBasedLoopIndex;
        RemainingPylons = remainingPylons;
        BossLife = bossLife;
        BossMaximumLife = bossMaximumLife;
        RemainingReviveTokens = remainingReviveTokens;
        ReviveRevision = reviveRevision;
        StackX = stackX;
        StackY = stackY;
        BossPhase = bossPhase;
        BossPhaseStartedTick = bossPhaseStartedTick;
        ActionStartedTick = actionStartedTick;
        ActionIndex = actionIndex;
        CompletedPhaseCycles = completedPhaseCycles;
        CoreX = coreX;
        CoreY = coreY;
        LastMechanicResult = lastMechanicResult;
        MechanicRevision = mechanicRevision;
        Participants = FirstSeverancePlanCollections.Copy(participants, nameof(participants));
        MechanicTick = mechanicTick;
        MechanicImpacts = FirstSeverancePlanCollections.Copy(mechanicImpacts ?? Array.Empty<FirstSeveranceMechanicImpact>(), nameof(mechanicImpacts));
        if (MechanicImpacts.Count > Participants.Count || MechanicImpacts.Count > 4
            || (MechanicImpacts.Count > 0 && (mechanicTick == 0 || mechanicRevision == 0 || lastMechanicResult == FirstSeveranceMechanicResult.None)))
            throw new ArgumentException("Invalid mechanic presentation result.");
        var impactIds = new HashSet<ParticipantId>();
        foreach (var impact in MechanicImpacts)
        {
            bool member = false;
            foreach (var participant in Participants) member |= participant.ParticipantId == impact.ParticipantId;
            if (!member || !impactIds.Add(impact.ParticipantId) || !float.IsFinite(impact.X) || !float.IsFinite(impact.Y)
                || Math.Abs(impact.X) > 1_000_000 || Math.Abs(impact.Y) > 1_000_000
                || (impact.Failed && lastMechanicResult is not (FirstSeveranceMechanicResult.StackFailed or FirstSeveranceMechanicResult.SpreadFailed))
                || (!impact.Failed && lastMechanicResult == FirstSeveranceMechanicResult.StackFailed))
                throw new ArgumentException("Invalid mechanic presentation target.");
        }
        foreach (var participant in Participants)
            if (participant.CombatState is not (RaidParticipantCombatState.Alive or RaidParticipantCombatState.Downed))
                throw new ArgumentException("First Severance has no eliminated participant state.");
        if (lanceVolley is not null
            && (!FirstSeveranceLanceTuning.IsAttackPhase(substate) || lanceVolley.EndTick > resolveTick
                || (lanceVolley.Kind == FirstSeveranceAttackKind.PursuitPrism && lanceVolley.Rays.Count > Participants.Count)
                || (lanceVolley.SustainedPrism && substate != FirstSeveranceSubstate.PylonCheck)))
            throw new ArgumentException("A lance cannot outlive its attack phase.");
        LanceVolley = lanceVolley;
        if (carriedLance is { } carried
            && (lanceVolley is not { SustainedPrism: true } current || !carried.SustainedPrism
                || substate != FirstSeveranceSubstate.PylonCheck || carried.Serial == current.Serial
                || carried.Step + 1 != current.Step
                || carried.StartTick >= current.StartTick
                || current.StartTick - carried.StartTick < (ulong)FirstSeveranceAttackPatterns.StepCadence(substate)
                || carried.EndTick <= current.StartTick || carried.EndTick > resolveTick
                || carried.Rays.Count > Participants.Count
                || !TryGetParticipantByServerSlot(carried.TargetSlot, out _)))
            throw new ArgumentException("Invalid overlapping main pursuit cast.");
        CarriedLance = carriedLance;
        if (gridVolley is not null && (substate != FirstSeveranceSubstate.Lattice
            || gridVolley.EndTick > resolveTick || lanceVolley is not null || gridVolley.CoreBeams.Count > Participants.Count))
            throw new ArgumentException("A grid cannot outlive its owning stage/window.");
        GridVolley = gridVolley;
        if (coreCannon is { } cannon && (substate is not (FirstSeveranceSubstate.FinalBullets or FirstSeveranceSubstate.FinalCoreCheck)
            || cannon.StartTick < actionStartedTick || cannon.StartTick - actionStartedTick < FirstSeveranceCoreCannonVolley.OpeningTicks
            || cannon.EndTick > resolveTick || !TryGetParticipantByServerSlot(cannon.TargetSlot, out _)
            || cannon.Ray.X != coreX || cannon.Ray.Y != coreY - FirstSeveranceLanceTuning.BossHeightAboveCore))
            throw new ArgumentException("Core cannon must belong to the accepted Final bullet window and roster.");
        CoreCannon = coreCannon;
        if (spreadLances is { Count: > FirstSeveranceSpreadBarrage.Count })
            throw new ArgumentException("Too many Spread pursuit casts.");
        SpreadLances = FirstSeverancePlanCollections.Copy(spreadLances ?? Array.Empty<FirstSeveranceLanceVolley>(), nameof(spreadLances));
        var serials = new HashSet<uint>();
        int previousStep = -1;
        ulong previousStart = 0;
        foreach (var cast in SpreadLances)
        {
            var window = FirstSeveranceSpreadBarrage.Window(substate, actionIndex, actionStartedTick, resolveTick, cast.StartTick);
            bool targetMember = false;
            foreach (var p in Participants) targetMember |= p.ServerWhoAmI == cast.TargetSlot;
            if (window is not { } w || cast.Kind != FirstSeveranceAttackKind.PursuitPrism || cast.SustainedPrism
                || cast.Step >= FirstSeveranceSpreadBarrage.Count || cast.Step <= previousStep
                || cast.StartTick <= previousStart || cast.StartTick < FirstSeveranceSpreadBarrage.Start(w, cast.Step)
                || cast.EndTick > w.ResolveTick - FirstSeveranceSpreadBarrage.Settle
                || cast.Rays.Count > Participants.Count || !targetMember || !serials.Add(cast.Serial)
                || cast.Serial == lanceVolley?.Serial)
                throw new ArgumentException("Invalid Spread pursuit cast.");
            previousStep = cast.Step;
            previousStart = cast.StartTick;
        }
    }

    public ulong EncounterSequence { get; }

    public FightId FightId { get; }

    public FirstSeveranceSubstate Substate { get; }

    public ulong ResolveTick { get; }

    public int ZeroBasedLoopIndex { get; }

    public int RemainingPylons { get; }

    public int BossLife { get; }

    public int BossMaximumLife { get; }

    public int RemainingReviveTokens { get; }

    public uint ReviveRevision { get; }

    public float StackX { get; }
    public float StackY { get; }
    public FirstSeveranceBossPhase BossPhase { get; }
    public ulong BossPhaseStartedTick { get; }
    public ulong ActionStartedTick { get; }
    public int ActionIndex { get; }
    public int CompletedPhaseCycles { get; }
    public bool IsHpGated => (BossPhase == FirstSeveranceBossPhase.Final
            && (Substate != FirstSeveranceSubstate.FinalCoreCheck || BossLife == 0))
        || (FirstSeveranceBossPhasePlan.Instance.TryGetNext(BossPhase, out var next)
            && BossLife <= FirstSeveranceBossPhasePlan.LifeThreshold(BossMaximumLife, next));
    public bool IsCoreOpen => !IsHpGated && FirstSeveranceBossPhasePlan.IsDamageState(Substate);
    public FirstSeveranceGridVolley? GridVolley { get; }
    public FirstSeveranceCoreCannonVolley? CoreCannon { get; }

    public float CoreX { get; }

    public float CoreY { get; }

    public FirstSeveranceMechanicResult LastMechanicResult { get; }

    public uint MechanicRevision { get; }
    internal bool IsTerminalPresentationAt(ulong tick)
        => (MechanicTick == tick && MechanicImpacts.Count > 0)
            || (Substate is FirstSeveranceSubstate.HalfField or FirstSeveranceSubstate.RemoteCrush
                && tick >= ActionStartedTick && tick < ResolveTick);
    public ulong MechanicTick { get; }
    public IReadOnlyList<FirstSeveranceMechanicImpact> MechanicImpacts { get; }

    public IReadOnlyList<FirstSeveranceCombatParticipantProjection> Participants { get; }

    public FirstSeveranceLanceVolley? LanceVolley { get; }
    public FirstSeveranceLanceVolley? CarriedLance { get; }
    public IReadOnlyList<FirstSeveranceLanceVolley> SpreadLances { get; }

    public bool TryGetParticipantByServerSlot(
        int serverWhoAmI,
        out FirstSeveranceCombatParticipantProjection participant)
    {
        for (int index = 0; index < Participants.Count; index++)
        {
            if (Participants[index].ServerWhoAmI == serverWhoAmI)
            {
                participant = Participants[index];
                return true;
            }
        }

        participant = default;
        return false;
    }
}
