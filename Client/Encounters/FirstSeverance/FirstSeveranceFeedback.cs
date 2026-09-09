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
    private readonly FirstSeveranceMechanicVisuals mechanics = new();
    private ulong shardDeadline;
    private int shardBeat = -1;
    private readonly List<ReLogic.Utilities.SlotId> voices = new(24);
    private uint chargeSerial, lockSerial, fireSerial;
    private int curtainBeat = -1;
    private uint gridChargeSerial, gridFireSerial;
    private bool shellBroken;
    private int countdown = -1, resultTicks;
    private int scoreImpactTicks;
    private readonly HashSet<int> scoreSounds = new();
    private readonly HashSet<uint> spreadCharges = new(), spreadFires = new();
    private ulong lastSwordSoundTick;
    private bool failed, stack;
    private float previousEndingAge = -1;

    internal void UpdateEnding(bool victory, float age)
    {
        if (!victory) { previousEndingAge = -1; return; }
        bool Crossed(float at) => previousEndingAge < at && age >= at && age - at < .035f;
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        float gain = reduced ? .45f : .85f;
        if (Crossed(.04f)) Play("PhaseRupture", gain, -.28f);
        if (Crossed(.29f)) Play("RemoteDeparture", gain * .75f, -.15f);
        if (Crossed(.68f)) Play("EnergyLock", gain * .7f, .30f);
        if (Crossed(.795f)) { Play("HandCrushImpact", gain, -.20f); Play("ShellBreak", gain * .7f, .28f); }
        previousEndingAge = age;
    }

    internal float Shake => Math.Max(resultTicks > 0 ? (failed ? 17f : 9f) * MathF.Pow(resultTicks / 32f, 2f) : 0f,
        22f * MathF.Pow(scoreImpactTicks / 24f, 2f));

    internal void Update(FirstSeveranceClientStateSystem state)
    {
        if (Main.dedServ) return;
        if (resultTicks > 0) resultTicks--;
        if (scoreImpactTicks > 0) scoreImpactTicks--;
        voices.RemoveAll(id => !SoundEngine.TryGetActiveSound(id, out var sound) || !sound.IsPlaying);
        var combat = state.Combat;
        mechanics.Update(combat, state.EstimatedAuthorityTick);
        if (combat is null || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) || !local.IsConnected)
        {
            if (previous is not null)
            {
                StopVoices();
                if (state.TerminalMechanic is { } terminal && terminal.FightId == previous.FightId
                    && terminal.MechanicRevision != previous.MechanicRevision)
                    AcceptResult(terminal, state.EstimatedAuthorityTick);
                else mechanics.Reset();
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
            mechanics.Reset();
            mechanics.Update(combat, state.EstimatedAuthorityTick);
            shardDeadline = 0;
            shardBeat = -1;
            resultTicks = 0;
            scoreImpactTicks = 0;
        }
        bool phaseChanged = fresh || previous!.Substate != combat.Substate
            || previous.ZeroBasedLoopIndex != combat.ZeroBasedLoopIndex || previous.ActionStartedTick != combat.ActionStartedTick;
        if (phaseChanged)
        {
            countdown = -1;
            scoreSounds.Clear();
            spreadCharges.Clear();
            spreadFires.Clear();
            lastSwordSoundTick = 0;
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
                FirstSeveranceSubstate.HalfField => "ShellMassLatch",
                FirstSeveranceSubstate.RemoteCrush => "CrushPressure",
                FirstSeveranceSubstate.FinalBullets => "FinalGather",
                FirstSeveranceSubstate.FinalSlicer => null,
                _ => null,
            };
            if (cue is not null && state.EstimatedAuthorityTick < combat.ActionStartedTick + 30) Play(cue, .98f);
        }
        // Do not announce an old result or revive when joining/catching up.
        if (!fresh && previous is { } before)
        {
            if (combat.MechanicRevision != before.MechanicRevision && combat.MechanicRevision > 0)
            {
                AcceptResult(combat, state.EstimatedAuthorityTick);
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
        if ((safe?.Kind == FirstSeveranceSafeMechanic.Stack || combat.Substate == FirstSeveranceSubstate.Stack) && tick < deadline)
        {
            if (shardDeadline != deadline) { shardDeadline = deadline; shardBeat = -1; }
            ulong start = safe?.StartTick ?? combat.ActionStartedTick;
            int beat = FirstSeveranceMechanicVisuals.ShardBeat((double)tick - start, deadline - start);
            if (beat > shardBeat)
            {
                shardBeat = beat;
                if (!fresh)
                {
                    Play("ShellMassLatch", .62f, beat * .012f - .06f);
                    if (beat > 0) Play("ShellMassArc", .36f, beat * .012f - .04f);
                }
            }
        }
        if ((safe is not null || combat.Substate is FirstSeveranceSubstate.Stack or FirstSeveranceSubstate.Spread) && tick < deadline)
        {
            int beat = (int)((deadline - tick + 29) / 30);
            if (beat <= 3 && beat != countdown)
                Play("MechanicTick", .95f);
            countdown = beat;
        }
        foreach (var cast in combat.SpreadLances)
        {
            if (tick >= cast.StartTick && spreadCharges.Add(cast.Serial) && tick < cast.StartTick + 8)
                Play("LanceCharge", .52f, cast.Step * .015f);
            if (tick >= cast.FireTick && spreadFires.Add(cast.Serial) && tick < cast.FireTick + 8)
                Play("LanceFire", .62f);
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
            if (combat.Substate == FirstSeveranceSubstate.HalfField)
            {
                for (int wave = 0; wave < 2; wave++)
                {
                    int brace = FirstSeveranceImpalingSwords.FireBase(wave) - 39;
                    if (age >= brace && scoreSounds.Add(-1100 - wave) && age < brace + 8)
                        Play("IronPressure", .68f);
                }
                foreach (var sword in FirstSeveranceImpalingSwords.At(combat.ActionIndex, age, combat.CoreX, combat.CoreY))
                    if (age >= sword.Fire && age < sword.Fire + 8 && scoreSounds.Add(1000 + sword.Fire))
                    {
                        if (tick >= lastSwordSoundTick + 8)
                        {
                            Play("IronDescent", .72f, sword.Slot % 3 * .025f - .025f);
                            lastSwordSoundTick = tick;
                        }
                    }
            }
            if (combat.Substate == FirstSeveranceSubstate.FinalSlicer)
                for (int pulse = 0; pulse < FirstSeveranceScoreGeometry.SlicerPulses; pulse++)
                {
                    int reveal = FirstSeveranceScoreGeometry.SlicerReveal(pulse);
                    if (age >= reveal && scoreSounds.Add(-1000 - pulse) && age < reveal + 8)
                        Play("LanceCharge", .62f, -.12f + pulse * .12f);
                }
            if (combat.Substate == FirstSeveranceSubstate.RemoteClaws)
            {
                int pulse = (int)age / FirstSeveranceScoreGeometry.FloodInterval;
                if (age % FirstSeveranceScoreGeometry.FloodInterval < 12 && scoreSounds.Add(pulse - 800))
                    Play("HandGather", .98f);
            }
            foreach (var ray in FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex, age, combat.CoreX, combat.CoreY, combat.ActionStartedTick))
            {
                int soundPulse = combat.Substate == FirstSeveranceSubstate.RotatingBlade
                    ? ray.Pulse / FirstSeveranceScoreGeometry.BladeCount : ray.Pulse;
                if (combat.Substate is not (FirstSeveranceSubstate.HalfField or FirstSeveranceSubstate.RemoteCrush or FirstSeveranceSubstate.FinalSlicer)
                    && !ray.Live && ray.Charge >= .65f && ray.Charge < 1 && scoreSounds.Add(soundPulse - 128))
                    Play("ExecutionLock", .98f);
                if (combat.Substate != FirstSeveranceSubstate.HalfField && ray.Live && scoreSounds.Add(soundPulse))
                {
                    Play(combat.Substate switch
                    {
                        FirstSeveranceSubstate.RotatingBlade => "BladeSweep",
                        FirstSeveranceSubstate.RemoteClaws => "HandClasp",
                        FirstSeveranceSubstate.RemoteCrush => "CrushCataclysm",
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

    internal void Draw(SpriteBatch batch, bool reduced) => mechanics.Draw(batch, reduced);

    private void AcceptResult(FirstSeveranceCombatProjection combat, ulong tick)
    {
        if (combat.MechanicImpacts.Count == 0 || tick < combat.MechanicTick || tick - combat.MechanicTick > 30) return;
        stack = combat.LastMechanicResult is FirstSeveranceMechanicResult.StackPassed or FirstSeveranceMechanicResult.StackFailed;
        failed = combat.LastMechanicResult is FirstSeveranceMechanicResult.StackFailed or FirstSeveranceMechanicResult.SpreadFailed;
        mechanics.Accept(combat);
        resultTicks = 32;
        if (stack) Play(failed ? "ShellMassCollapse" : "ShellMassShed", failed ? .88f : .72f);
        else
        {
            bool dissipate = false;
            foreach (var impact in combat.MechanicImpacts) dissipate |= !impact.Failed;
            Play("SpreadExecution", .90f);
            if (dissipate) Play("SpreadDissolve", .68f);
        }
    }

    private void StopVoices()
    {
        foreach (var id in voices)
            if (SoundEngine.TryGetActiveSound(id, out var sound)) sound.Stop();
        voices.Clear();
    }

    internal void Reset(bool unload = false)
    {
        StopVoices();
        previous = null;
        mechanics.Reset(unload);
        previousEndingAge = -1;
        shardDeadline = 0;
        shardBeat = -1;
        resultTicks = 0;
        scoreImpactTicks = 0;
        chargeSerial = lockSerial = fireSerial = 0;
        curtainBeat = -1;
        gridChargeSerial = gridFireSerial = 0;
        shellBroken = false;
        countdown = -1;
        scoreSounds.Clear();
        spreadCharges.Clear();
        spreadFires.Clear();
    }
}
