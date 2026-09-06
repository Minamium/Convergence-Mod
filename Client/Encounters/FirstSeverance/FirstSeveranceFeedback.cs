#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// Snapshot-driven, participant-only presentation. Never derives a pass/fail from
// local positions or sound playback. No packets, world actors or gameplay writes.
internal sealed class FirstSeveranceFeedback
{
    private ulong safeCueResolve;
    private const string Root = "Convergence/Assets/Sounds/FirstSeverance/";
    private FirstSeveranceCombatProjection? previous;
    private readonly List<Vector2> impacts = new(4);
    private readonly List<ReLogic.Utilities.SlotId> voices = new(24);
    private uint chargeSerial, lockSerial, fireSerial;
    private int curtainBeat = -1;
    private uint gridChargeSerial, gridFireSerial;
    private bool shellBroken;
    private int countdown = -1, resultTicks;
    private int scoreImpactTicks;
    private readonly HashSet<int> scoreSounds = new();
    private bool failed, stack;
    private float radius;

    internal float Shake => Math.Max(resultTicks > 0 ? (failed ? 17f : 9f) * MathF.Pow(resultTicks / 32f, 2f) : 0f,
        22f * MathF.Pow(scoreImpactTicks / 24f, 2f));

    internal void Update(FirstSeveranceClientStateSystem state)
    {
        if (Main.dedServ) return;
        if (resultTicks > 0) resultTicks--;
        if (scoreImpactTicks > 0) scoreImpactTicks--;
        voices.RemoveAll(id => !SoundEngine.TryGetActiveSound(id, out var sound) || !sound.IsPlaying);
        var combat = state.Combat;
        if (combat is null || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) || !local.IsConnected)
        {
            if (previous is not null)
            {
                StopVoices();
                if (state.LastCombatEndReason is EncounterEndReason.Defeat or EncounterEndReason.Victory)
                    Play(state.LastCombatEndReason == EncounterEndReason.Defeat ? "RaidDefeat" : "RaidVictory", .86f);
            }
            previous = null;
            return;
        }
        bool fresh = previous is null || previous.FightId != combat.FightId;
        if (fresh)
        {
            StopVoices();
            chargeSerial = lockSerial = fireSerial = 0;
            curtainBeat = -1;
            gridChargeSerial = gridFireSerial = 0;
            shellBroken = false;
            countdown = -1;
            safeCueResolve = 0;
            impacts.Clear();
            resultTicks = 0;
            scoreImpactTicks = 0;
        }
        bool phaseChanged = fresh || previous!.Substate != combat.Substate
            || previous.ZeroBasedLoopIndex != combat.ZeroBasedLoopIndex || previous.ActionStartedTick != combat.ActionStartedTick;
        if (phaseChanged)
        {
            countdown = -1;
            scoreSounds.Clear();
            shellBroken = false;
            string? cue = combat.Substate switch
            {
                FirstSeveranceSubstate.SpawnIntro => "RaidDesignation",
                FirstSeveranceSubstate.Stack => "StackSummon",
                FirstSeveranceSubstate.Spread => "SpreadSummon",
                FirstSeveranceSubstate.CoreExposure => "CoreExposure",
                FirstSeveranceSubstate.PhaseTransition => combat.BossPhase == FirstSeveranceBossPhase.Distant ? "RemoteDeparture"
                    : combat.BossPhase == FirstSeveranceBossPhase.Final ? "TerminalEntry" : "PhaseRupture",
                FirstSeveranceSubstate.Lattice => "CoreExposure",
                FirstSeveranceSubstate.RotatingBlade => "BladeGather",
                FirstSeveranceSubstate.HalfField => "HalfFieldCharge",
                FirstSeveranceSubstate.RemoteCrush => "HandCrushGather",
                FirstSeveranceSubstate.FinalBullets => "FinalGather",
                FirstSeveranceSubstate.FinalSlicer => "FinalGather",
                _ => null,
            };
            if (cue is not null && state.EstimatedAuthorityTick < combat.ActionStartedTick + 30) Play(cue, .98f);
        }
        // Do not announce an old result or revive when joining/catching up.
        if (!fresh && previous is { } before)
        {
            if (combat.MechanicRevision != before.MechanicRevision && combat.MechanicRevision > 0)
            {
                stack = combat.LastMechanicResult is FirstSeveranceMechanicResult.StackPassed or FirstSeveranceMechanicResult.StackFailed;
                failed = combat.LastMechanicResult is FirstSeveranceMechanicResult.StackFailed or FirstSeveranceMechanicResult.SpreadFailed;
                Play(failed ? "MechanicFailure" : stack ? "StackRelease" : "SpreadRelease", .82f);
                impacts.Clear();
                if (stack) impacts.Add(new(before.StackX, before.StackY));
                foreach (var member in before.Participants)
                    if (!stack && member.IsConnected)
                        impacts.Add(Main.player[member.ServerWhoAmI].Center);
                radius = stack ? FirstSeveranceLanceTuning.StackRadius : FirstSeveranceLanceTuning.SpreadRadius;
                resultTicks = 32;
            }
            if (combat.Substate == FirstSeveranceSubstate.PylonCheck && before.Substate == combat.Substate
                && combat.RemainingPylons < before.RemainingPylons)
                Play("PylonBreak", .65f);
            bool down = false, revive = false;
            foreach (var member in combat.Participants)
                if (before.TryGetParticipantByServerSlot(member.ServerWhoAmI, out var old))
                {
                    down |= member.CombatState == RaidParticipantCombatState.Downed && old.CombatState != member.CombatState;
                    revive |= member.CombatState == RaidParticipantCombatState.Alive && member.ReviveLockoutUntilTick > old.ReviveLockoutUntilTick;
                }
            if (revive) Play("Revive", .8f);
            else if (down) Play("Downed", .72f);
        }
        ulong tick = state.EstimatedAuthorityTick;
        var safe = FirstSeveranceSafeWindows.At(combat.Substate, combat.ActionIndex,
            combat.ActionStartedTick, tick, combat.CoreX, combat.CoreY);
        if (safe is { } companion && tick < companion.ResolveTick && safeCueResolve != companion.ResolveTick)
        {
            safeCueResolve = companion.ResolveTick;
            countdown = -1;
            if (!fresh || tick < companion.StartTick + 30)
                Play(companion.Kind == FirstSeveranceSafeMechanic.Stack ? "StackSummon" : "SpreadSummon", .98f);
        }
        ulong deadline = safe?.ResolveTick ?? combat.ResolveTick;
        if ((safe is not null || combat.Substate is FirstSeveranceSubstate.Stack or FirstSeveranceSubstate.Spread) && tick < deadline)
        {
            int beat = (int)((deadline - tick + 29) / 30);
            if (beat <= 3 && beat != countdown)
                Play("MechanicTick", .95f);
            countdown = beat;
        }
        if (combat.LanceVolley is { } volley)
        {
            if (chargeSerial != volley.Serial)
            {
                chargeSerial = volley.Serial;
                curtainBeat = -1;
                if (tick >= volley.StartTick && tick < volley.FireTick)
                    Play(volley.IsCharge ? "EnergyGather" : "LanceCharge", .98f);
            }
            if (fireSerial != volley.Serial && tick >= volley.FireTick)
            {
                fireSerial = volley.Serial;
                if (volley.IsFiring(tick) && volley.Kind != FirstSeveranceAttackKind.Stillness)
                    Play(volley.IsCharge ? "EnergyCharge" : "LanceFire", .96f);
            }
            if (volley.Kind == FirstSeveranceAttackKind.Stillness && tick >= volley.FireTick)
            {
                int beat = (int)Math.Min((tick - volley.FireTick) / 4, 3ul);
                if (beat > curtainBeat)
                {
                    curtainBeat = beat;
                    // Four restrained transients for both curtains together, not
                    // fifty competing voices. Late snapshots never catch up a burst.
                    ulong cueTick = volley.FireTick + (ulong)(beat * 4);
                    if (tick < cueTick + 3 && volley.IsFiring(tick))
                        Play("LanceFire", .64f, -.08f + beat * .045f);
                }
            }
            if (volley.IsCharge && lockSerial != volley.Serial && tick >= volley.LockTick)
            {
                lockSerial = volley.Serial;
                if (tick < volley.FireTick) Play("EnergyLock", .98f);
            }
        }
        if (combat.Substate == FirstSeveranceSubstate.PhaseTransition && combat.BossPhase == FirstSeveranceBossPhase.Unbound && !shellBroken
            && tick >= combat.BossPhaseStartedTick + 112)
        {
            shellBroken = true;
            if (tick < combat.BossPhaseStartedTick + 152) Play("ShellBreak", .94f);
        }
        if (combat.GridVolley is { } grid)
        {
            if (gridChargeSerial != grid.Serial)
            {
                gridChargeSerial = grid.Serial;
                if (tick >= grid.StartTick && tick < grid.FireTick) Play("GridCharge", .98f);
            }
            if (gridFireSerial != grid.Serial && tick >= grid.FireTick)
            {
                gridFireSerial = grid.Serial;
                if (grid.IsFiring(tick))
                {
                    // Salvo asset includes the grid impact, mastered as one
                    // pressure-rich voice instead of summing two clipped peaks.
                    Play(grid.CoreBeams.Count > 0 ? "CoreSalvoFire" : "GridFire", 1f);
                }
            }
        }
        double age = (double)tick - combat.ActionStartedTick;
        if (tick < combat.ResolveTick && age >= 0)
        {
            if (combat.Substate == FirstSeveranceSubstate.RemoteClaws)
            {
                int pulse = (int)age / FirstSeveranceScoreGeometry.FloodInterval;
                if (age % FirstSeveranceScoreGeometry.FloodInterval < 12 && scoreSounds.Add(pulse - 800))
                    Play("HandGather", .98f);
            }
            foreach (var ray in FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex, age, combat.CoreX, combat.CoreY))
            {
                int soundPulse = combat.Substate == FirstSeveranceSubstate.RotatingBlade
                    ? ray.Pulse / FirstSeveranceScoreGeometry.BladeCount : ray.Pulse;
                if (!ray.Live && ray.Charge >= .65f && ray.Charge < 1 && scoreSounds.Add(soundPulse - 128))
                    Play("ExecutionLock", .98f);
                if (ray.Live && scoreSounds.Add(soundPulse))
                {
                    Play(combat.Substate switch
                    {
                        FirstSeveranceSubstate.RotatingBlade => "BladeSweep",
                        FirstSeveranceSubstate.RemoteClaws => "HandClasp",
                        FirstSeveranceSubstate.HalfField => "HalfFieldFire",
                        FirstSeveranceSubstate.RemoteCrush => "HandCrushImpact",
                        _ => "FinalSlicerFire",
                    }, .98f);
                    if (combat.Substate == FirstSeveranceSubstate.RemoteCrush) scoreImpactTicks = 24;
                }
            }
            if (combat.Substate == FirstSeveranceSubstate.RotatingBlade && age >= 144 && age < 162 && scoreSounds.Add(-500))
                Play("BladeUnsheathe", .98f);
            if (combat.Substate == FirstSeveranceSubstate.FinalBullets)
                foreach (var bullet in FirstSeveranceScoreGeometry.Bullets(combat.ActionIndex, age, combat.CoreX, combat.CoreY))
                    if (bullet.Live && scoreSounds.Add(bullet.Wave)) Play("FinalBulletRelease", .94f, bullet.Wave * .025f);
        }
        previous = combat;
    }

    private void Play(string name, float volume, float pitch = 0f)
    {
        // No position: raid-critical cues remain audible in a very large arena.
        // Main.soundVolume still applies; music uses Main.musicVolume separately.
        var style = new SoundStyle(Root + name)
        {
            Volume = volume * .80f, Pitch = pitch, MaxInstances = 2,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        };
        voices.Add(SoundEngine.PlaySound(style));
    }

    internal void Draw(SpriteBatch batch, bool reduced)
    {
        if (resultTicks <= 0) return;
        float age = 1f - resultTicks / 32f, fade = 1f - age;
        Color color = failed ? FirstSeveranceBossVisuals.Danger : stack ? FirstSeveranceBossVisuals.Ice : FirstSeveranceBossVisuals.Gold;
        foreach (var center in impacts)
        {
            float shock = 1f - MathF.Pow(1f - age, 3f);
            FirstSeveranceBossVisuals.Ring(batch, center, radius, color * fade, 4f);
            if (stack)
            {
                // A resolved Stack retains the one true circle, not shockwave rings.
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathF.PI * .5f;
                    Vector2 direction = new(MathF.Cos(a), MathF.Sin(a)), tangent = new(-direction.Y, direction.X);
                    Vector2 tip = center + direction * (radius + 12 + shock * 25);
                    FirstSeveranceBossVisuals.Line(batch, tip + direction * 26 + tangent * 18, tip, color * fade, 5);
                    FirstSeveranceBossVisuals.Line(batch, tip + direction * 26 - tangent * 18, tip, color * fade, 5);
                }
                continue;
            }
            FirstSeveranceBossVisuals.Ring(batch, center, 20 + shock * radius, Color.White * fade, reduced ? 2f : 6f);
            if (!reduced)
            {
                FirstSeveranceBossVisuals.Ring(batch, center, radius * (.8f + shock * .4f), color * fade * .7f, 3f);
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * MathF.Tau / 12;
                    Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
                    Vector2 start = center + direction * (radius * .28f + shock * radius * .6f);
                    FirstSeveranceBossVisuals.Line(batch, start, start + direction * (22 + 30 * fade), color * fade, 3f);
                }
            }
        }
    }

    private void StopVoices()
    {
        foreach (var id in voices)
            if (SoundEngine.TryGetActiveSound(id, out var sound)) sound.Stop();
        voices.Clear();
    }

    internal void Reset()
    {
        StopVoices();
        previous = null;
        impacts.Clear();
        resultTicks = 0;
        scoreImpactTicks = 0;
        chargeSerial = lockSerial = fireSerial = 0;
        curtainBeat = -1;
        gridChargeSerial = gridFireSerial = 0;
        shellBroken = false;
        countdown = -1;
        scoreSounds.Clear();
    }
}
