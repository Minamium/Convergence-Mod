#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance.Actors;
using Convergence.Content.Encounters.FirstSeverance.FoundationCore;
using Convergence.Content.Encounters.FirstSeverance.Development;
using Convergence.Content.Encounters.FirstSeverance.Revive;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.FirstSeverance;

// Passive window accounting from observations. Never owns or advances the loop.
internal sealed class FirstSeveranceCombatTelemetry
{
    private readonly FirstSeverancePartyScaling partyScaling;
    private readonly FirstSeveranceEncounterPlan plan;
    private FirstSeveranceDamageWindow? damageWindow;
    private FirstSeveranceSubstate damageWindowPhase;
    private int damageWindowLoop;
    private int damageWindowFloor;
    private ulong nextProgressTick;
    private ulong combatStartedTick;
    private int[] progressPylonSlots = Array.Empty<int>();
    private int[] progressPylonLife = Array.Empty<int>();
    private bool progressObservationMissing;
    private long totalPylonDamage;
    private long totalCoreDamage;
    private ulong totalPylonOpenTicks;
    private ulong totalCoreOpenTicks;
    private readonly FirstSeveranceActorSet actors;
    private readonly int participantCount;
    private readonly Action<ulong, string> Log;
    private FirstSeveranceLoopState currentState;
    private bool isStarted;

    internal FirstSeveranceCombatTelemetry(FirstSeveranceActorSet actors, FirstSeverancePartyScaling partyScaling,
        int participantCount, FirstSeveranceEncounterPlan plan, Action<ulong, string> log)
    {
        this.actors = actors;
        this.partyScaling = partyScaling;
        this.participantCount = participantCount;
        this.plan = plan;
        Log = log;
    }

    internal void Start(in FirstSeveranceLoopState state, ulong tick)
    {
        currentState = state;
        combatStartedTick = tick;
        isStarted = true;
    }

    internal void BeginDamageProgress(in FirstSeveranceLoopState state, int damageFloor)
    {
        currentState = state;
        try
        {
            if (state.Substate != FirstSeveranceSubstate.PylonCheck && !FirstSeveranceBossPhasePlan.IsDamageState(state.Substate))
                return;
            damageWindowFloor = state.Substate == FirstSeveranceSubstate.PylonCheck ? 0 : damageFloor;
            if (state.Substate != FirstSeveranceSubstate.PylonCheck && state.BossLife <= damageWindowFloor) return;
            damageWindowPhase = state.Substate;
            damageWindowLoop = state.ZeroBasedLoopIndex + 1;
            progressObservationMissing = false;
            ulong open = state.SubstateEnteredTick;
            int life = state.BossLife - damageWindowFloor;
            if (state.Substate == FirstSeveranceSubstate.PylonCheck)
            {
                open += (ulong)plan.Timing.PylonTelegraphTicks;
                progressPylonSlots = actors.CapturePylonSlots();
                progressPylonLife = new int[progressPylonSlots.Length];
                Array.Fill(progressPylonLife, partyScaling.PylonLife);
                life = progressPylonSlots.Length * partyScaling.PylonLife;
            }
            damageWindow = new(life, open, state.ResolveTick);
            nextProgressTick = open + 120;
            Log(state.SubstateEnteredTick, $"event=DamageWindowStarted phase={damageWindowPhase} loop={damageWindowLoop} open_tick={open} close_tick={state.ResolveTick} target_hp={life} boss_life={state.BossLife} boss_max_life={state.BossMaximumLife} penalized={state.IsPenalizedExposure}");
        }
        catch (Exception) { damageWindow = null; } // Diagnostics never drive combat.
    }

    internal void ObservePylonProgress(ulong tick, bool damageWindowOpen)
    {
        try
        {
            if (damageWindow is null || damageWindowPhase != FirstSeveranceSubstate.PylonCheck || !damageWindowOpen)
                return;
            for (int index = 0; index < progressPylonSlots.Length; index++)
            {
                if (progressPylonLife[index] == 0)
                    continue;
                int slot = progressPylonSlots[index];
                if (actors.WasPylonKilled(slot))
                {
                    progressPylonLife[index] = 0;
                    Log(tick, $"event=PylonDestroyed loop={damageWindowLoop} pylon={index + 1} open_elapsed_ticks={tick - damageWindow.OpenTick}");
                }
                else if (actors.TryObservePylonLife(slot, out int life))
                    // Only confirmed OnKill may represent completion; disappearance is not a kill.
                    progressPylonLife[index] = Math.Clamp(life, 1, partyScaling.PylonLife);
                else
                    progressObservationMissing = true;
            }
        }
        catch (Exception) { progressObservationMissing = true; }
    }

    internal void PublishDamageProgress(in FirstSeveranceLoopState state, ulong tick)
    {
        currentState = state;
        try
        {
            if (damageWindow is null)
                return;
            bool gated = damageWindowPhase != FirstSeveranceSubstate.PylonCheck && state.BossLife <= damageWindowFloor;
            bool ended = gated || state.Substate != damageWindowPhase || state.IsTerminal || tick >= damageWindow.CloseTick;
            if (ended || tick >= nextProgressTick)
            {
                int remaining = DamageProgressRemainingLife();
                string outcome = gated ? "HpGateReached" : remaining == 0 ? "Cleared"
                    : state.Substate == FirstSeveranceSubstate.PhaseTransition ? "PhaseTransition"
                    : tick >= damageWindow.CloseTick ? "Deadline" : "Interrupted";
                WriteDamageProgress(tick, ended, ended ? outcome : "Open");
                nextProgressTick = tick + 120;
            }
        }
        catch (Exception) { } // Never change an accepted gameplay transition for logging.
    }

    private int DamageProgressRemainingLife()
    {
        if (FirstSeveranceBossPhasePlan.IsDamageState(damageWindowPhase))
            return Math.Max(0, currentState.BossLife - damageWindowFloor);
        int remaining = 0;
        foreach (int life in progressPylonLife)
            remaining += life;
        return remaining;
    }

    private void WriteDamageProgress(ulong tick, bool end, string outcome)
    {
        if (damageWindow is null)
            return;
        FirstSeveranceDamageSample sample = damageWindow.Sample(tick, DamageProgressRemainingLife());
        string required = sample.RequiredDps?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "unavailable";
        string pylons = damageWindowPhase == FirstSeveranceSubstate.PylonCheck
            ? string.Join(",", progressPylonLife) : "none";
        string kind = end ? "DamageWindowEnded" : "DamageProgress";
        Log(tick, FormattableString.Invariant($"event={kind} phase={damageWindowPhase} loop={damageWindowLoop} outcome={outcome} hp_observation={(progressObservationMissing ? "Incomplete" : "Complete")} open_seconds={sample.ElapsedTicks / 60d:F2} remaining_seconds={sample.RemainingTicks / 60d:F2} effective_damage={sample.EffectiveDamage} target_hp_start={damageWindow.StartingLife} target_hp_remaining={sample.RemainingLife} window_progress_pct={sample.ProgressPercent:F2} window_dps={sample.WindowDps:F1} recent_dps={sample.RecentDps:F1} required_dps_by_deadline={required} pylon_hp={pylons} boss_life={currentState.BossLife} boss_remaining_pct={100d * currentState.BossLife / currentState.BossMaximumLife:F2} overload={currentState.Overload}"));
        if (!end)
            return;
        if (damageWindowPhase == FirstSeveranceSubstate.PylonCheck)
        {
            totalPylonDamage += sample.EffectiveDamage;
            totalPylonOpenTicks += sample.ElapsedTicks;
        }
        else
        {
            totalCoreDamage += sample.EffectiveDamage;
            totalCoreOpenTicks += sample.ElapsedTicks;
        }
        damageWindow = null; // Final sample and totals are emitted once, including terminal cleanup.
        progressPylonSlots = Array.Empty<int>();
        progressPylonLife = Array.Empty<int>();
    }

    internal void FinishDamageProgress(ulong tick, string cause)
    {
        try
        {
            if (!isStarted)
                return;
            WriteDamageProgress(tick, end: true, outcome: cause);
            double coreDps = FirstSeveranceDamageWindow.Rate(totalCoreDamage, totalCoreOpenTicks);
            double pylonDps = FirstSeveranceDamageWindow.Rate(totalPylonDamage, totalPylonOpenTicks);
            ulong elapsed = tick >= combatStartedTick ? tick - combatStartedTick : 0;
            Log(tick, FormattableString.Invariant($"event=CombatDpsSummary cause={cause} participants={participantCount} elapsed_seconds={elapsed / 60d:F2} completed_exposures={currentState.CompletedExposures} overload={currentState.Overload} core_effective_damage={totalCoreDamage} core_open_seconds={totalCoreOpenTicks / 60d:F2} core_window_dps={coreDps:F1} pylon_effective_damage={totalPylonDamage} pylon_open_seconds={totalPylonOpenTicks / 60d:F2} pylon_window_dps={pylonDps:F1} boss_life={currentState.BossLife} boss_max_life={currentState.BossMaximumLife} boss_remaining_pct={100d * currentState.BossLife / currentState.BossMaximumLife:F2} dps_basis=AuthorityNetHpLoss"));
        }
        catch (Exception) { }
        finally
        {
            damageWindow = null;
            progressPylonSlots = Array.Empty<int>();
            progressPylonLife = Array.Empty<int>();
        }
    }
}
