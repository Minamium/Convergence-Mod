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
        ulong actionStartedTick = 0, int actionIndex = -1, int completedPhaseCycles = 0)
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
            || (bossPhase == FirstSeveranceBossPhase.Final && bossLife != 0)
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
        foreach (var participant in Participants)
            if (participant.CombatState is not (RaidParticipantCombatState.Alive or RaidParticipantCombatState.Downed))
                throw new ArgumentException("First Severance has no eliminated participant state.");
        if (lanceVolley is not null
            && (!FirstSeveranceLanceTuning.IsAttackPhase(substate) || lanceVolley.EndTick > resolveTick
                || (lanceVolley.Kind == FirstSeveranceAttackKind.PursuitPrism && lanceVolley.Rays.Count > Participants.Count)))
            throw new ArgumentException("A lance cannot outlive its attack phase.");
        LanceVolley = lanceVolley;
        if (gridVolley is not null && (substate != FirstSeveranceSubstate.Lattice
            || gridVolley.EndTick > resolveTick || lanceVolley is not null || gridVolley.CoreBeams.Count > Participants.Count))
            throw new ArgumentException("A grid cannot outlive its owning stage/window.");
        GridVolley = gridVolley;
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
    public bool IsHpGated => BossPhase == FirstSeveranceBossPhase.Final
        || (FirstSeveranceBossPhasePlan.Instance.TryGetNext(BossPhase, out var next)
            && BossLife <= FirstSeveranceBossPhasePlan.LifeThreshold(BossMaximumLife, next));
    public bool IsCoreOpen => !IsHpGated && FirstSeveranceBossPhasePlan.IsDamageState(Substate);
    public FirstSeveranceGridVolley? GridVolley { get; }

    public float CoreX { get; }

    public float CoreY { get; }

    public FirstSeveranceMechanicResult LastMechanicResult { get; }

    public uint MechanicRevision { get; }

    public IReadOnlyList<FirstSeveranceCombatParticipantProjection> Participants { get; }

    public FirstSeveranceLanceVolley? LanceVolley { get; }

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
