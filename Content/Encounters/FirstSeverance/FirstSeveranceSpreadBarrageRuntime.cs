#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Common.Foundation.Identifiers;
using Terraria;

namespace Convergence.Content.Encounters.FirstSeverance;

// Exact-Fight authority owner. Up to four immutable casts, one hit per cast/member;
// neither client positions nor presentation clocks can resolve their damage.
internal sealed class FirstSeveranceSpreadBarrageRuntime
{
    private readonly List<FirstSeveranceLanceVolley> casts = new(FirstSeveranceSpreadBarrage.Count);
    private readonly HashSet<(uint, ParticipantId)> hits = new();
    private ulong windowStart;
    private int nextStep;
    internal IReadOnlyList<FirstSeveranceLanceVolley> Casts => casts;

    internal void Clear() { casts.Clear(); hits.Clear(); windowStart = 0; nextStep = 0; }

    internal bool Update(in FirstSeveranceLoopState state, ulong tick, FirstSeveranceRoster roster,
        FirstSeveranceRecoveryController recovery, ref uint serial, Action<ulong, string> log)
    {
        var window = FirstSeveranceSpreadBarrage.Window(state.Substate, state.ActionIndex,
            state.SubstateEnteredTick, state.ResolveTick, tick);
        if (window is not { } w) { bool had = casts.Count > 0; Clear(); return had; }
        if (windowStart != w.StartTick) { Clear(); windowStart = w.StartTick; }
        bool changed = false;
        int shotCount = FirstSeveranceSpreadBarrage.ShotCount(w);
        if (nextStep < shotCount && tick >= FirstSeveranceSpreadBarrage.Start(w, nextStep))
        {
            // Skip missed steps instead of catch-up bursts. Never compress a warning.
            while (nextStep + 1 < shotCount
                && tick >= FirstSeveranceSpreadBarrage.Start(w, nextStep + 1)) nextStep++;
            if (w.ResolveTick - tick >= FirstSeveranceSpreadBarrage.CastTicks + FirstSeveranceSpreadBarrage.Settle)
            {
                var targets = new List<FirstSeverancePrismTarget>(roster.Count);
                foreach (var member in roster.Members)
                    if (recovery.IsAlive(member.ParticipantId) && recovery.TryGetPlayer(member, out Player p))
                        targets.Add(new(p.whoAmI, p.Center.X, p.Center.Y, p.velocity.X, p.velocity.Y));
                if (targets.Count > 0)
                {
                    var cast = FirstSeveranceAttackPatterns.CreatePrism(++serial, tick, (byte)nextStep, targets);
                    casts.Add(cast);
                    changed = true;
                    log(tick, $"event=SpreadPrismTelegraph cast={cast.Serial} step={nextStep + 1} rays={cast.Rays.Count} fire_tick={cast.FireTick} end_tick={cast.EndTick} spread_resolve={w.ResolveTick}");
                }
            }
            nextStep++;
        }
        foreach (var cast in casts)
        {
            if (!cast.IsFiring(tick)) continue;
            if (tick == cast.FireTick) log(tick, $"event=SpreadPrismFired cast={cast.Serial} step={cast.Step + 1}");
            foreach (var member in roster.Members)
            {
                if (hits.Contains((cast.Serial, member.ParticipantId)) || !recovery.IsAlive(member.ParticipantId)
                    || !recovery.TryGetPlayer(member, out Player p)) continue;
                foreach (var ray in cast.Rays)
                    if (ray.Intersects(p.Center.X, p.Center.Y, p.width * .5f, p.height * .5f))
                    {
                        hits.Add((cast.Serial, member.ParticipantId));
                        recovery.ApplyRaidDamage(member, FirstSeveranceCombatRules.BeamDamage, tick, "SpreadPrism");
                        changed = true;
                        break;
                    }
            }
        }
        return changed;
    }
}
