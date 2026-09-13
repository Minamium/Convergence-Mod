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
    private uint coreSalvoOrdinal;
    private ulong nextGridTick;
    private readonly HashSet<ParticipantId> gridHitParticipants = new();
    private readonly HashSet<(int Pulse, ParticipantId Participant)> scoreHits = new();
    private readonly HashSet<int> scoreFires = new();
    private readonly FirstSeveranceLanceLedger lances = new();
    private FirstSeveranceLanceVolley? lanceVolley => lances.Current;
    private uint lanceSerial;
    private ulong nextLanceTick;
    private byte attackStep;
    private int attackTargetSlot = -1;
    private uint attackSequence;
    private readonly FirstSeveranceRecoveryController recovery;
    private readonly FirstSeveranceSpreadBarrageRuntime spread = new();
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
    internal FirstSeveranceLanceVolley? CarriedLance => lances.Carried;
    internal FirstSeveranceGridVolley? Grid => gridVolley;
    internal IReadOnlyList<FirstSeveranceLanceVolley> SpreadLances => spread.Casts;
    internal bool UpdateSpread(in FirstSeveranceLoopState state, ulong tick)
        => spread.Update(state, tick, roster, recovery, ref lanceSerial, Log);

    internal void EnterSubstate(FirstSeveranceSubstate after, ulong authorityTick)
    {
        spread.Clear();
        lances.Clear();
        scoreHits.Clear();
        scoreFires.Clear();
        nextLanceTick = authorityTick + (after == FirstSeveranceSubstate.PylonCheck
            ? (ulong)plan.Timing.PylonTelegraphTicks : (ulong)FirstSeveranceAttackPatterns.ExposureOpeningRestTicks);
        attackStep = 0;
        attackTargetSlot = -1;
        if (after == FirstSeveranceSubstate.PylonCheck) return;
        gridVolley = null;
        gridHitParticipants.Clear();
        nextGridTick = authorityTick + FirstSeveranceGridVolley.OpeningTicks;
    }

    internal void Cleanup()
    {
        spread.Clear();
        lances.Clear();
        gridVolley = null;
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
            && state.ResolveTick - tick >= FirstSeveranceGridVolley.DurationTicks)
        {
            uint serial = ++gridSerial;
            byte pattern = FirstSeveranceSafeWindows.GridPattern(serial, tick - state.SubstateEnteredTick);
            var beams = new List<FirstSeveranceLanceRay>(1);
            int coreTargetSlot = -1;
            if (pattern < 4 && serial >= FirstSeveranceGridVolley.CoreSalvoFirstSerial)
            {
                var targets = new List<Player>(roster.Count);
                foreach (var member in roster.Members)
                    if (recovery.IsAlive(member.ParticipantId) && recovery.TryGetPlayer(member, out Player target))
                        targets.Add(target);
                if (targets.Count > 0)
                {
                    Player target = targets[FirstSeveranceGridVolley.CoreTargetIndex(coreSalvoOrdinal++, targets.Count)];
                    coreTargetSlot = target.whoAmI;
                    beams.Add(FirstSeveranceGridVolley.AimCoreBeam(groundCenter.X, groundCenter.Y,
                        target.Center.X, target.Center.Y));
                }
            }
            gridVolley = new(serial, tick, pattern, groundCenter.X, groundCenter.Y, beams);
            nextGridTick = tick + FirstSeveranceGridVolley.CadenceTicks;
            changed = true;
            Log(tick, $"event=GridTelegraph cast={serial} pattern={gridVolley.Pattern} lines={gridVolley.Rays.Count} core_beams={gridVolley.CoreBeams.Count} core_target_slot={coreTargetSlot} fire_tick={gridVolley.FireTick} end_tick={gridVolley.EndTick}");
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
            bool hadVolley = lanceVolley is not null || lances.Carried is not null;
            lances.Clear();
            return hadVolley;
        }

        bool changed = lances.Retire(tick);

        // Never truncate a new telegraph at the phase boundary. Do not catch up
        // missed casts in a burst; scheduling is relative to the actual start.
        int neededTicks = attackStep == 0 ? FirstSeveranceAttackPatterns.SequenceTicks(state.Substate)
            : FirstSeveranceAttackPatterns.StepTicks(state.Substate, attackStep);
        if (lances.CanStart && tick >= nextLanceTick && state.ResolveTick - tick >= (ulong)neededTicks)
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
                FirstSeveranceLanceVolley created;
                if (state.Substate == FirstSeveranceSubstate.PylonCheck)
                {
                    var aims = new List<FirstSeverancePrismTarget>(targets.Count);
                    foreach (Player participant in targets)
                        aims.Add(new(participant.whoAmI, participant.Center.X, participant.Center.Y,
                            participant.velocity.X, participant.velocity.Y));
                    // One simultaneous, locked ray per standing participant. A crossed
                    // group of rays is still capped to one hit per player per step.
                    created = FirstSeveranceAttackPatterns.CreatePrism(++lanceSerial, tick, attackStep, aims, sustainedPrism:true);
                }
                else
                    created = FirstSeveranceAttackPatterns.Create(++lanceSerial, tick,
                        state.Substate, attackStep, target.whoAmI, target.Center.X, target.Center.Y,
                        target.velocity.X, target.velocity.Y);
                lances.Start(created, tick);
                nextLanceTick = tick + (ulong)FirstSeveranceAttackPatterns.StepCadence(state.Substate);
                attackStep++;
                if (attackStep >= FirstSeveranceAttackPatterns.StepCount(state.Substate))
                {
                    attackStep = 0;
                    nextLanceTick += (ulong)FirstSeveranceAttackPatterns.SequenceRestTicks;
                }
                changed = true; // Publish the locked aim immediately, not on the 30-tick heartbeat.
                string assignedSlots = created.Kind == FirstSeveranceAttackKind.PursuitPrism
                    ? string.Join(",", targets.ConvertAll(player => player.whoAmI)) : target.whoAmI.ToString();
                string curtain = created.Kind == FirstSeveranceAttackKind.Stillness
                    ? $" pattern=ContinuousBands half_width={FirstSeveranceAttackPatterns.StillnessBeamHalfWidth}"
                    : string.Empty;
                Log(tick, $"event=LanceTelegraph cast={lanceSerial} kind={created.Kind} step={created.Step + 1} target_slot={target.whoAmI} target_slots={assignedSlots} rays={created.Rays.Count} fire_tick={created.FireTick} end_tick={created.EndTick} carried_cast={lances.Carried?.Serial ?? 0}{curtain}");
            }
        }

        if (lanceVolley is { IsCharge: true } body)
        {
            Player focus = Main.player[body.TargetSlot];
            // No target replacement mid-charge. If the focus is Down, pursue its
            // last anchor and finish the same bounded trajectory, never an outsider.
            lances.UpdateMotion(body.AdvanceCharge(tick, focus.Center.X, focus.Center.Y,
                focus.velocity.X, focus.velocity.Y));
            changed |= (tick < body.LockTick && tick % 4ul == 0)
                || tick == body.LockTick;
            if (tick == body.LockTick)
                Log(tick, $"event=EnergyChargeLocked cast={body.Serial} speed=104 target_slot={body.TargetSlot}");
        }
        // Older cast first, each with its own hit set. A new forecast cannot
        // erase or rearm its still-firing predecessor.
        changed |= ApplyLance(lances.Carried, tick);
        changed |= ApplyLance(lanceVolley, tick);
        return changed;
    }

    private bool ApplyLance(FirstSeveranceLanceVolley? volley, ulong tick)
    {
        if (volley is null || !volley.IsFiring(tick)) return false;
        bool changed = false;
        if (tick == volley.FireTick)
        {
            changed = true;
            Log(tick, $"event=LanceFired cast={volley.Serial} rays={volley.Rays.Count} end_tick={volley.EndTick}");
        }
        foreach (FirstSeveranceRosterMember member in roster.Members)
        {
            if (lances.HasHit(volley.Serial, member.ParticipantId) || !recovery.IsAlive(member.ParticipantId)
                || !recovery.TryGetPlayer(member, out Player player))
                continue;
            // Contact-style energy charges respect legitimate engine/dash i-frames.
            // Stack/Spread remain authority percentage mechanics, not dodgeable hits.
            if (volley.IsCharge && player.immune && player.immuneTime > 0)
                continue;
            bool hit = volley.Kind == FirstSeveranceAttackKind.Stillness
                && FirstSeveranceCurtainComb.Intersects(volley, tick,
                    player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f);
            for (int index = 0; !hit && volley.Kind != FirstSeveranceAttackKind.Stillness
                && index < volley.Rays.Count; index++)
            {
                FirstSeveranceLanceRay ray = volley.RayAt(index, tick);
                hit = ray.Intersects(player.Center.X, player.Center.Y, player.width * .5f, player.height * .5f);
            }
            if (hit)
            {
                // One hit per step, including overlapping curtains or moving blades.
                lances.MarkHit(volley.Serial, member.ParticipantId);
                recovery.ApplyRaidDamage(member, FirstSeveranceCombatRules.AttackDamage(
                    volley.Kind, player.statLifeMax2), tick, volley.Kind.ToString());
                changed = true;
            }
        }
        return changed;
    }

    internal void UpdateScoreHazards(in FirstSeveranceLoopState state, ulong tick)
    {
        if (!FirstSeveranceScoreGeometry.HasHazards(state.Substate) || tick >= state.ResolveTick) return;
        double age = tick - state.SubstateEnteredTick;
        var rays = FirstSeveranceScoreGeometry.Rays(state.Substate, state.ActionIndex, age, groundCenter.X, groundCenter.Y, state.SubstateEnteredTick);
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
