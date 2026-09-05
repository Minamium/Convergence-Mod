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
    ulong WeaknessUntilTick);

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
        int stackTargetSlot,
        float coreX,
        float coreY,
        FirstSeveranceMechanicResult lastMechanicResult,
        uint mechanicRevision,
        IReadOnlyList<FirstSeveranceCombatParticipantProjection> participants,
        FirstSeveranceLanceVolley? lanceVolley = null)
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
            || stackTargetSlot is < -1 or >= 255
            || !float.IsFinite(coreX) || !float.IsFinite(coreY)
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
        StackTargetSlot = stackTargetSlot;
        CoreX = coreX;
        CoreY = coreY;
        LastMechanicResult = lastMechanicResult;
        MechanicRevision = mechanicRevision;
        Participants = FirstSeverancePlanCollections.Copy(participants, nameof(participants));
        if (lanceVolley is not null
            && (!FirstSeveranceLanceTuning.IsAttackPhase(substate) || lanceVolley.EndTick > resolveTick))
            throw new ArgumentException("A lance cannot outlive its attack phase.");
        LanceVolley = lanceVolley;
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

    public int StackTargetSlot { get; }

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
