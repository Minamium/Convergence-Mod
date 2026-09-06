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

// Owns cast scheduling and hit ledgers for exactly one combat instance.
// Geometry remains pure/shared; this authority adapter never commits lifecycle changes.
internal sealed class FirstSeveranceAttackController
{
    private readonly FirstSeveranceRoster roster;
    private readonly Vector2 groundCenter;
    private readonly FirstSeveranceEncounterPlan plan;
    private FirstSeveranceGridVolley? gridVolley;
    private uint gridSerial;
    private ulong nextGridTick;
    private readonly HashSet<ParticipantId> gridHitParticipants = new();
    private readonly HashSet<(int Pulse, ParticipantId Participant)> scoreHits = new();
    private readonly HashSet<int> scoreFires = new();
    private FirstSeveranceLanceVolley? lanceVolley;
    private uint lanceSerial;
    private ulong nextLanceTick;
    private byte attackStep;
    private int attackTargetSlot = -1;
    private uint attackSequence;
    private readonly HashSet<ParticipantId> lanceHitParticipants = new();
    private readonly FirstSeveranceRecoveryController recovery;
    private readonly Action<ulong, string> Log;

    internal FirstSeveranceAttackController(FirstSeveranceRoster roster, Vector2 groundCenter,
        FirstSeveranceEncounterPlan plan, FirstSeveranceRecoveryController recovery, Action<ulong, string> log)
    {
        this.roster = roster;
        this.groundCenter = groundCenter;
        this.plan = plan;
        this.recovery = recovery;
        Log = log;
    }

    internal FirstSeveranceLanceVolley? Lance => lanceVolley;
    internal FirstSeveranceGridVolley? Grid => gridVolley;

    internal void EnterSubstate(FirstSeveranceSubstate after, ulong authorityTick)
    {
        lanceVolley = null;
        lanceHitParticipants.Clear();
        scoreHits.Clear();
        scoreFires.Clear();
        nextLanceTick = authorityTick + (after == FirstSeveranceSubstate.PylonCheck
            ? (ulong)plan.Timing.PylonTelegraphTicks : (ulong)FirstSeveranceAttackPatterns.ExposureOpeningRestTicks);
        attackStep = 0;
        attackTargetSlot = -1;
        if (after == FirstSeveranceSubstate.PylonCheck) return;
        gridVolley = null;
        gridHitParticipants.Clear();
        nextGridTick = authorityTick + 60;
    }

    internal void Cleanup()
    {
        lanceVolley = null;
        gridVolley = null;
        lanceHitParticipants.Clear();
        gridHitParticipants.Clear();
        scoreHits.Clear();
        scoreFires.Clear();
        nextLanceTick = nextGridTick = 0;
        attackStep = 0;
        attackTargetSlot = -1;
        attackSequence = 0;
    }

    internal bool UpdateGrid(in FirstSeveranceLoopState state, ulong tick, bool bossDead)
    {
        if (state.Substate != FirstSeveranceSubstate.Lattice || tick >= state.ResolveTick || bossDead)
        {
            bool had = gridVolley is not null;
            gridVolley = null;
            gridHitParticipants.Clear();
            return had;
        }
        bool changed = false;
        if (gridVolley is not null && tick >= gridVolley.EndTick)
        {
            gridVolley = null;
            gridHitParticipants.Clear();
            changed = true;
        }
        if (gridVolley is null && tick >= nextGridTick
            && state.ResolveTick - tick >= FirstSeveranceGridVolley.TelegraphTicks + FirstSeveranceGridVolley.ActiveTicks)
        {
            uint serial = ++gridSerial;
            byte pattern = FirstSeveranceSafeWindows.GridPattern(serial, tick - state.SubstateEnteredTick);
            var beams = new List<FirstSeveranceLanceRay>(roster.Count);
            if (pattern < 4 && serial >= FirstSeveranceGridVolley.CoreSalvoFirstSerial)
                foreach (var member in roster.Members)
                    if (recovery.IsAlive(member.ParticipantId) && recovery.TryGetPlayer(member, out Player target))
                        beams.Add(FirstSeveranceGridVolley.AimCoreBeam(groundCenter.X, groundCenter.Y,
                            target.Center.X, target.Center.Y));
            gridVolley = new(serial, tick, pattern, groundCenter.X, groundCenter.Y, beams);
            nextGridTick = tick + FirstSeveranceGridVolley.CadenceTicks;
            changed = true;
            Log(tick, $"event=GridTelegraph cast={serial} pattern={gridVolley.Pattern} lines={gridVolley.Rays.Count} core_beams={gridVolley.CoreBeams.Count} fire_tick={gridVolley.FireTick} end_tick={gridVolley.EndTick}");
        }
        if (gridVolley is null || !gridVolley.IsFiring(tick)) return changed;
        if (tick == gridVolley.FireTick)
        {
            changed = true;
            Log(tick, $"event=GridFired cast={gridVolley.Serial} pattern={gridVolley.Pattern} core_beams={gridVolley.CoreBeams.Count} fixed_damage={FirstSeveranceCombatRules.BeamDamage} hit_cap=OnePerVolley");
        }
        foreach (var member in roster.Members)
        {
            if (gridHitParticipants.Contains(member.ParticipantId) || !recovery.IsAlive(member.ParticipantId)
                || !recovery.TryGetPlayer(member, out Player player)
                || !gridVolley.Intersects(tick, player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f))
                continue;
            gridHitParticipants.Add(member.ParticipantId); // Crossings cannot double-hit a participant.
            string source = gridVolley.CoreIntersects(tick, player.Center.X, player.Center.Y,
                player.width * .5f, player.height * .5f) ? "CoreSalvo" : "Lattice";
            recovery.ApplyRaidDamage(member, FirstSeveranceCombatRules.BeamDamage, tick, source);
            changed = true;
        }
        return changed;
    }

    internal bool UpdateLances(in FirstSeveranceLoopState state, ulong tick, bool phaseComplete)
    {
        if (phaseComplete || !FirstSeveranceLanceTuning.IsAttackPhase(state.Substate)
            || tick >= state.ResolveTick)
        {
            bool hadVolley = lanceVolley is not null;
            lanceVolley = null;
            lanceHitParticipants.Clear();
            return hadVolley;
        }

        bool changed = false;
        if (lanceVolley is not null && tick >= lanceVolley.EndTick)
        {
            lanceVolley = null;
            lanceHitParticipants.Clear();
            changed = true;
        }

        // Never truncate a new telegraph at the phase boundary. Do not catch up
        // missed casts in a burst; scheduling is relative to the actual start.
        int neededTicks = attackStep == 0 ? FirstSeveranceAttackPatterns.SequenceTicks(state.Substate)
            : FirstSeveranceAttackPatterns.StepTicks(state.Substate, attackStep);
        if (lanceVolley is null && tick >= nextLanceTick && state.ResolveTick - tick >= (ulong)neededTicks)
        {
            var targets = new List<Player>(roster.Count);
            int first = (int)(attackSequence % (uint)roster.Count);
            for (int offset = 0; offset < roster.Count; offset++)
            {
                FirstSeveranceRosterMember member = roster.Members[(first + offset) % roster.Count];
                if (recovery.IsAlive(member.ParticipantId) && recovery.TryGetPlayer(member, out Player player))
                    targets.Add(player);
            }
            if (targets.Count > 0)
            {
                Player target = attackStep == 0 ? targets[0]
                    : targets.Find(player => player.whoAmI == attackTargetSlot) ?? targets[0];
                if (attackStep == 0)
                    attackSequence++;
                attackTargetSlot = target.whoAmI;
                if (state.Substate == FirstSeveranceSubstate.PylonCheck)
                {
                    var aims = new List<FirstSeverancePrismTarget>(targets.Count);
                    foreach (Player participant in targets)
                        aims.Add(new(participant.whoAmI, participant.Center.X, participant.Center.Y,
                            participant.velocity.X, participant.velocity.Y));
                    // One simultaneous, locked ray per standing participant. A crossed
                    // group of rays is still capped to one hit per player per step.
                    lanceVolley = FirstSeveranceAttackPatterns.CreatePrism(++lanceSerial, tick, attackStep, aims);
                }
                else
                    lanceVolley = FirstSeveranceAttackPatterns.Create(++lanceSerial, tick,
                        state.Substate, attackStep, target.whoAmI, target.Center.X, target.Center.Y,
                        target.velocity.X, target.velocity.Y);
                nextLanceTick = tick + (ulong)FirstSeveranceAttackPatterns.StepCadence(state.Substate);
                attackStep++;
                if (attackStep >= FirstSeveranceAttackPatterns.StepCount(state.Substate))
                {
                    attackStep = 0;
                    nextLanceTick += (ulong)FirstSeveranceAttackPatterns.SequenceRestTicks;
                }
                lanceHitParticipants.Clear();
                changed = true; // Publish the locked aim immediately, not on the 30-tick heartbeat.
                string assignedSlots = lanceVolley.Kind == FirstSeveranceAttackKind.PursuitPrism
                    ? string.Join(",", targets.ConvertAll(player => player.whoAmI)) : target.whoAmI.ToString();
                string curtain = lanceVolley.Kind == FirstSeveranceAttackKind.Stillness
                    ? $" pattern=CenterOut teeth={lanceVolley.Rays.Count * FirstSeveranceCurtainComb.LaneCount} last_fire_tick={lanceVolley.FireTick + FirstSeveranceCurtainComb.StaggerTicks} end_tick={lanceVolley.EndTick}"
                    : string.Empty;
                Log(tick, $"event=LanceTelegraph cast={lanceSerial} kind={lanceVolley.Kind} step={lanceVolley.Step + 1} target_slot={target.whoAmI} target_slots={assignedSlots} rays={lanceVolley.Rays.Count} fire_tick={lanceVolley.FireTick}{curtain}");
            }
        }

        if (lanceVolley is { IsCharge: true } body)
        {
            Player focus = Main.player[body.TargetSlot];
            // No target replacement mid-charge. If the focus is Down, pursue its
            // last anchor and finish the same bounded trajectory, never an outsider.
            lanceVolley = body.AdvanceCharge(tick, focus.Center.X, focus.Center.Y,
                focus.velocity.X, focus.velocity.Y);
            changed |= (tick < body.LockTick && tick % 4ul == 0)
                || tick == body.LockTick;
            if (tick == body.LockTick)
                Log(tick, $"event=EnergyChargeLocked cast={body.Serial} speed=104 target_slot={body.TargetSlot}");
        }
        if (lanceVolley is null || !lanceVolley.IsFiring(tick))
            return changed;
        if (tick == lanceVolley.FireTick)
        {
            changed = true;
            Log(tick, $"event=LanceFired cast={lanceVolley.Serial} rays={lanceVolley.Rays.Count}");
        }
        foreach (FirstSeveranceRosterMember member in roster.Members)
        {
            if (lanceHitParticipants.Contains(member.ParticipantId) || !recovery.IsAlive(member.ParticipantId)
                || !recovery.TryGetPlayer(member, out Player player))
                continue;
            // Contact-style energy charges respect legitimate engine/dash i-frames.
            // Stack/Spread remain authority percentage mechanics, not dodgeable hits.
            if (lanceVolley.IsCharge && player.immune && player.immuneTime > 0)
                continue;
            bool hit = lanceVolley.Kind == FirstSeveranceAttackKind.Stillness
                && FirstSeveranceCurtainComb.Intersects(lanceVolley, tick,
                    player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f);
            for (int index = 0; !hit && lanceVolley.Kind != FirstSeveranceAttackKind.Stillness
                && index < lanceVolley.Rays.Count; index++)
            {
                FirstSeveranceLanceRay ray = lanceVolley.RayAt(index, tick);
                hit = ray.Intersects(player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f);
            }
            if (hit)
            {
                // One hit per step, including overlapping curtains or moving blades.
                lanceHitParticipants.Add(member.ParticipantId);
                recovery.ApplyRaidDamage(member, FirstSeveranceCombatRules.AttackDamage(
                    lanceVolley.Kind, player.statLifeMax2), tick, lanceVolley.Kind.ToString());
                changed = true;
            }
        }
        return changed;
    }

    internal void UpdateScoreHazards(in FirstSeveranceLoopState state, ulong tick)
    {
        if (!FirstSeveranceScoreGeometry.HasHazards(state.Substate) || tick >= state.ResolveTick) return;
        double age = tick - state.SubstateEnteredTick;
        var rays = FirstSeveranceScoreGeometry.Rays(state.Substate, state.ActionIndex, age, groundCenter.X, groundCenter.Y);
        var bullets = state.Substate == FirstSeveranceSubstate.FinalBullets
            ? FirstSeveranceScoreGeometry.Bullets(state.ActionIndex, age, groundCenter.X, groundCenter.Y) : null;
        bool lethal = state.Substate == FirstSeveranceSubstate.RemoteCrush;
        foreach (var ray in rays)
            if (ray.Live && scoreFires.Add(ray.Pulse))
                Log(tick, $"event=ScoreAttackFired boss_phase={state.BossPhase} attack={state.Substate} action={state.ActionIndex} pulse={ray.Pulse} damage_kind={(lethal ? "LethalToDown" : "Fixed")} fixed_damage={(lethal ? 0 : FirstSeveranceScoreGeometry.FixedDamage)}");
        foreach (var member in roster.Members)
        {
            if (!recovery.IsAlive(member.ParticipantId) || !recovery.TryGetPlayer(member, out Player player)) continue;
            int pulse = -1;
            foreach (var ray in rays)
                if (ray.Live && !scoreHits.Contains((ray.Pulse, member.ParticipantId))
                    && FirstSeveranceScoreGeometry.RayHits(state.Substate, ray, age,
                        player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f))
                { pulse = ray.Pulse; break; }
            if (bullets is not null)
                foreach (var bullet in bullets)
                    if (!scoreHits.Contains((bullet.Wave, member.ParticipantId))
                        && FirstSeveranceScoreGeometry.BulletHits(bullet, player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f))
                    { pulse = bullet.Wave; break; }
            if (pulse < 0) continue;
            scoreHits.Add((pulse, member.ParticipantId));
            recovery.ApplyRaidDamage(member, lethal ? Math.Max(player.statLife, player.statLifeMax2)
                : FirstSeveranceScoreGeometry.FixedDamage, tick, state.Substate.ToString());
        }
    }
}
