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
    private readonly HashSet<ReLogic.Utilities.SlotId> impactTails = new();
    private readonly List<(ReLogic.Utilities.SlotId Id, ulong End, int Release)> timedVoices = new(24);
    private readonly FirstSeveranceAudioCueClock criticalClock = new();
    private FirstSeveranceCombatProjection? pendingResult;
    private ReLogic.Utilities.SlotId orbitVoice;
    private static readonly int SecondTurnTick = FindSecondTurnTick();
    private readonly List<(ReLogic.Utilities.SlotId Id, string Name, ulong CheckAt)> audioChecks = new(16);
    private uint chargeSerial, lockSerial, fireSerial;
    private uint gridChargeSerial, gridFireSerial;
    private uint cannonChargeSerial, cannonFireSerial;
    private bool shellBroken;
    private int countdown = -1, resultTicks;
    private int scoreImpactTicks;
    private int captureCueMask;
    private readonly HashSet<int> scoreSounds = new();
    private readonly HashSet<uint> spreadCharges = new(), spreadFires = new();
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

    internal float Shake => Math.Max(resultTicks > 0 ? (stack ? (failed ? 17f : 9f) : 19f) * MathF.Pow(resultTicks / 32f, 2f) : 0f,
        22f * MathF.Pow(scoreImpactTicks / 24f, 2f));

    internal void Update(FirstSeveranceClientStateSystem state)
    {
        if (Main.dedServ) return;
        if (resultTicks > 0) resultTicks--;
        if (scoreImpactTicks > 0) scoreImpactTicks--;
        CheckAudioVoices();
        UpdateTimedVoices(state.EstimatedAuthorityTick);
        voices.RemoveAll(id =>
        {
            if (SoundEngine.TryGetActiveSound(id, out var sound) && sound.IsPlaying) return false;
            impactTails.Remove(id);
            return true;
        });
        var combat = state.Combat;
        mechanics.Update(combat, state.EstimatedAuthorityTick);
        if (combat is null || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) || !local.IsConnected)
        {
            if (previous is not null)
            {
                StopVoices(preserveImpacts: true);
                // Cleanup can arrive instead of the short-lived active impact
                // snapshot. Use its accepted authority tick, not a guessed defeat cause.
                if (state.TerminalCombat is { } ended && ended.FightId == previous.FightId
                    && ended.Substate is FirstSeveranceSubstate.HalfField or FirstSeveranceSubstate.RemoteCrush)
                {
                    if (ended.ActionStartedTick != previous.ActionStartedTick || ended.Substate != previous.Substate)
                        criticalClock.Reset();
                    PlayCriticalAction(ended, state.TerminalAuthorityTick);
                }
                if (state.TerminalMechanic is { } terminal && terminal.FightId == previous.FightId
                    && terminal.MechanicRevision != previous.MechanicRevision)
                    AcceptResult(terminal, state.TerminalAuthorityTick);
                else if (pendingResult is { } pending && pending.FightId == previous.FightId)
                    AcceptResult(pending, Math.Max(state.TerminalAuthorityTick, pending.MechanicTick));
                else mechanics.Reset();
                if (state.LastCombatEndReason is EncounterEndReason.Defeat or EncounterEndReason.Victory)
                    Play(state.LastCombatEndReason == EncounterEndReason.Defeat ? "RaidDefeat" : "RaidVictory", .86f);
            }
            previous = null;
            pendingResult = null;
            return;
        }
        bool fresh = previous is null || previous.FightId != combat.FightId;
        if (fresh)
        {
            StopVoices();
            chargeSerial = lockSerial = fireSerial = 0;
            gridChargeSerial = gridFireSerial = cannonChargeSerial = cannonFireSerial = 0;
            shellBroken = false;
            countdown = -1;
            safeCueResolve = 0;
            mechanics.Reset();
            mechanics.Update(combat, state.EstimatedAuthorityTick);
            shardDeadline = 0;
            shardBeat = -1;
            resultTicks = 0;
            scoreImpactTicks = 0;
            pendingResult = null;
        }
        bool phaseChanged = fresh || previous!.Substate != combat.Substate
            || previous.ZeroBasedLoopIndex != combat.ZeroBasedLoopIndex || previous.ActionStartedTick != combat.ActionStartedTick;
        if (phaseChanged)
        {
            captureCueMask=0;
            // An HP-gated transition may interrupt an action before its planned
            // end. Retire only that action's sounds, not accepted verdict tails.
            for (int i = 0; i < timedVoices.Count; i++)
                timedVoices[i] = (timedVoices[i].Id,
                    Math.Min(timedVoices[i].End, state.EstimatedAuthorityTick + (ulong)(6 + timedVoices[i].Release)),
                    timedVoices[i].Release);
            countdown = -1;
            scoreSounds.Clear();
            criticalClock.Reset();
            if (SoundEngine.TryGetActiveSound(orbitVoice, out var orbit)) orbit.Stop();
            spreadCharges.Clear();
            spreadFires.Clear();
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
                FirstSeveranceSubstate.RotatingBlade => "PrismBeamCharge",
                FirstSeveranceSubstate.HalfField => "PrismBeamCharge",
                FirstSeveranceSubstate.RemoteCrush => null, // owned by the critical cue clock
                FirstSeveranceSubstate.FinalBullets => "FinalGather",
                FirstSeveranceSubstate.FinalSlicer => null,
                _ => null,
            };
            if (cue is not null && state.EstimatedAuthorityTick < combat.ActionStartedTick + 30)
                PlayTimed(cue, .98f, state.EstimatedAuthorityTick, combat.ResolveTick);
        }
        if(combat.Substate==FirstSeveranceSubstate.SpawnIntro)
        {
            float captureAge=FirstSeveranceDollCapture.IntroAge(state.EstimatedAuthorityTick,combat.ActionStartedTick,combat.ResolveTick);
            CaptureCue(1,.22f,"ShellMassLatch",.62f);
            CaptureCue(2,.52f,"RemoteDeparture",.55f);
            CaptureCue(4,.858f,"CoreExposure",.76f);
            void CaptureCue(int bit,float at,string name,float gain)
            {
                if(captureAge<at||(captureCueMask&bit)!=0) return;
                captureCueMask|=bit;
                // Do not replay missed cinematic sounds on a late snapshot.
                if(captureAge-at<.025f) PlayTimed(name,gain,state.EstimatedAuthorityTick,combat.ResolveTick);
            }
        }
        // Do not announce an old result or revive when joining/catching up.
        if (!fresh && previous is { } before)
        {
            if (combat.MechanicRevision != before.MechanicRevision && combat.MechanicRevision > 0)
            {
                pendingResult = combat;
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
        if (pendingResult is { } result && tick >= result.MechanicTick)
        {
            AcceptResult(result, tick);
            pendingResult = null;
        }
        PlayCriticalAction(combat, tick);
        var safe = FirstSeveranceSafeWindows.At(combat.Substate, combat.ActionIndex,
            combat.ActionStartedTick, tick, combat.CoreX, combat.CoreY);
        if (safe is { } companion && tick < companion.ResolveTick && safeCueResolve != companion.ResolveTick)
        {
            safeCueResolve = companion.ResolveTick;
            countdown = -1;
            if (!fresh || tick < companion.StartTick + 30)
                PlayTimed(companion.Kind == FirstSeveranceSafeMechanic.Stack ? "StackSummon" : "SpreadSummon", .98f, tick, companion.ResolveTick);
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
                    PlayTimed("ShellMassLatch", 1f, tick, deadline, beat * .012f - .06f);
                    if (beat > 0) PlayTimed("ShellMassArc", .60f, tick, deadline, beat * .012f - .04f);
                }
            }
        }
        if ((safe is not null || combat.Substate is FirstSeveranceSubstate.Stack or FirstSeveranceSubstate.Spread) && tick < deadline)
        {
            int beat = (int)((deadline - tick + 29) / 30);
            if (beat <= 3 && beat != countdown)
                PlayTimed("MechanicTick", safe?.Kind == FirstSeveranceSafeMechanic.Stack
                    || combat.Substate == FirstSeveranceSubstate.Stack ? .64f : .95f, tick, deadline);
            countdown = beat;
        }
        foreach (var cast in combat.SpreadLances)
        {
            if (tick >= cast.StartTick && spreadCharges.Add(cast.Serial) && tick < cast.StartTick + 8)
                PlayTimed("LanceCharge", .52f, tick, cast.FireTick, cast.Step * .015f);
            if (tick >= cast.FireTick && spreadFires.Add(cast.Serial) && tick < cast.FireTick + 8)
                PlayTimed("LanceFire", .62f, tick, cast.EndTick + 6);
        }
        if (combat.LanceVolley is { } volley)
        {
            if (chargeSerial != volley.Serial)
            {
                chargeSerial = volley.Serial;
                if (tick >= volley.StartTick && tick < volley.FireTick)
                    PlayTimed(volley.IsCharge ? "BeamGather" : volley.Kind == FirstSeveranceAttackKind.Stillness
                        ? "PrismBeamCharge" : "LanceCharge", .98f, tick, volley.FireTick);
            }
            if (fireSerial != volley.Serial && tick >= volley.FireTick)
            {
                fireSerial = volley.Serial;
                if (volley.IsFiring(tick))
                    // One pressure envelope for the pair of continuous bands.
                    PlayTimed(volley.IsCharge ? "EnergyCharge" : volley.Kind == FirstSeveranceAttackKind.Stillness
                        ? "CurtainFire" : "LanceFire", .96f, tick, volley.EndTick + 6);
            }
            if (volley.IsCharge && lockSerial != volley.Serial && tick >= volley.LockTick)
            {
                lockSerial = volley.Serial;
                if (tick < volley.FireTick) PlayTimed("BeamLock", .72f, tick, volley.FireTick);
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
                if (tick >= grid.StartTick && tick < grid.FireTick) PlayTimed("GridCharge", .98f, tick, grid.FireTick);
            }
            if (gridFireSerial != grid.Serial && tick >= grid.FireTick)
            {
                gridFireSerial = grid.Serial;
                if (grid.IsFiring(tick))
                {
                    // Salvo asset includes the grid impact, mastered as one
                    // pressure-rich voice instead of summing two clipped peaks.
                    PlayTimed(grid.CoreBeams.Count > 0 ? "CoreSalvoFire" : "GridFire", 1f, tick, grid.EndTick + 6);
                }
            }
        }
        if (combat.CoreCannon is { } cannon)
        {
            if (cannonChargeSerial != cannon.Serial)
            {
                cannonChargeSerial = cannon.Serial;
                if (tick >= cannon.StartTick && tick < cannon.FireTick) PlayTimed("GridCharge", .98f, tick, cannon.FireTick);
            }
            if (cannonFireSerial != cannon.Serial && tick >= cannon.FireTick)
            {
                cannonFireSerial = cannon.Serial;
                if (tick < cannon.EndTick) PlayTimed("CoreSalvoFire", 1f, tick, cannon.EndTick + 6);
            }
        }
        double age = (double)tick - combat.ActionStartedTick;
        if (tick < combat.ResolveTick && age >= 0)
        {
            if (combat.Substate == FirstSeveranceSubstate.FinalSlicer)
                for (int pulse = 0; pulse < FirstSeveranceScoreGeometry.SlicerPulses; pulse++)
                {
                    int reveal = FirstSeveranceScoreGeometry.SlicerReveal(pulse);
                    if (age >= reveal && scoreSounds.Add(-1000 - pulse) && age < reveal + 8)
                        PlayTimed("LanceCharge", .62f, tick,
                            combat.ActionStartedTick + (ulong)FirstSeveranceScoreGeometry.SlicerFire(combat.ActionIndex), -.12f + pulse * .12f);
                }
            if (combat.Substate == FirstSeveranceSubstate.RemoteClaws)
            {
                int pulse = (int)age / FirstSeveranceScoreGeometry.FloodInterval;
                if (age % FirstSeveranceScoreGeometry.FloodInterval < 12 && scoreSounds.Add(pulse - 800))
                    PlayTimed("PrismBeamCharge", .98f, tick,
                        combat.ActionStartedTick + (ulong)(pulse * FirstSeveranceScoreGeometry.FloodInterval + FirstSeveranceScoreGeometry.FloodFireTick));
            }
            foreach (var ray in FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex, age, combat.CoreX, combat.CoreY, combat.ActionStartedTick))
            {
                int soundPulse = combat.Substate == FirstSeveranceSubstate.RotatingBlade
                    ? ray.Pulse / FirstSeveranceScoreGeometry.BladeCount : ray.Pulse;
                if (combat.Substate is not (FirstSeveranceSubstate.HalfField or FirstSeveranceSubstate.RemoteCrush or FirstSeveranceSubstate.FinalSlicer)
                    && !ray.Live && ray.Charge >= .65f && ray.Charge < 1 && scoreSounds.Add(soundPulse - 128))
                    PlayTimed("BeamLock", .78f, tick, Math.Min(combat.ResolveTick, tick + 18));
                if (combat.Substate is not (FirstSeveranceSubstate.HalfField or FirstSeveranceSubstate.RotatingBlade or FirstSeveranceSubstate.RemoteCrush)
                    && ray.Live && scoreSounds.Add(soundPulse))
                {
                    ulong soundEnd = combat.Substate == FirstSeveranceSubstate.RemoteClaws
                        ? combat.ActionStartedTick + (ulong)(ray.Pulse * FirstSeveranceScoreGeometry.FloodInterval + FirstSeveranceScoreGeometry.FloodFadeTick)
                        : combat.ActionStartedTick + (ulong)(FirstSeveranceScoreGeometry.SlicerEnd(combat.ActionIndex)
                            + ray.Pulse * FirstSeveranceScoreGeometry.SlicerCadence(combat.ActionIndex) + 6);
                    PlayTimed(combat.Substate switch
                    {
                        FirstSeveranceSubstate.RemoteClaws => "FloodFire",
                        _ => "LanceFire",
                    }, .98f, tick, Math.Min(combat.ResolveTick + 6, soundEnd));
                }
            }
            if (combat.Substate == FirstSeveranceSubstate.RotatingBlade && age >= 144 && age < 162 && scoreSounds.Add(-500))
                PlayTimed("PrismBeamCharge", .90f, tick, combat.ActionStartedTick + FirstSeveranceChoreography.BladeWindup);
            if (combat.Substate == FirstSeveranceSubstate.FinalBullets)
                foreach (var bullet in FirstSeveranceScoreGeometry.Bullets(combat.ActionIndex, age, combat.CoreX, combat.CoreY))
                    if (bullet.Live && scoreSounds.Add(bullet.Wave)) PlayTimed("FinalBulletRelease", .94f, tick, combat.ResolveTick + 6, bullet.Wave * .025f);
        }
        previous = combat;
    }

    private static int FindSecondTurnTick()
    {
        for (int age = FirstSeveranceChoreography.BladeWindup; age < FirstSeveranceChoreography.BladeEnd; age++)
            if (FirstSeveranceScoreGeometry.BladeTurn(age) == 1) return age;
        throw new InvalidOperationException("The two-turn audio requires a second blade turn.");
    }

    private void PlayCriticalAction(FirstSeveranceCombatProjection combat, ulong tick)
    {
        void Cue(int key, int offset, string name, float gain = 1.25f, bool impact = true, int endOffset = 0)
        {
            ulong due = combat.ActionStartedTick + (ulong)offset;
            if (!criticalClock.Take(key, tick, due)) return;
            var voice = PlayCritical(name, combat, tick, due, gain, impact);
            if (name == "PrismBeamSustain")
            {
                if (SoundEngine.TryGetActiveSound(orbitVoice, out var old)) old.Stop();
                orbitVoice = voice;
            }
            if (endOffset > 0)
                TrackTimed(voice, name, combat.ActionStartedTick + (ulong)endOffset);
        }
        if (combat.Substate == FirstSeveranceSubstate.HalfField)
            for (int wave = 0; wave < 2; wave++)
            {
                int strike = FirstSeveranceImpalingSwords.FireBase(wave);
                Cue(wave * 3, strike - 39, "PrismBeamCharge", .95f, false, strike);
                // Two emission accents per field wave, not 28 competing voices.
                Cue(wave * 3 + 1, strike, "PrismBeamFire", .95f, false, strike + 50);
                Cue(wave * 3 + 2, strike + 12, "PrismBeamFire", .70f, false, strike + 50);
            }
        else if (combat.Substate == FirstSeveranceSubstate.RemoteCrush)
        {
            Cue(0, 0, "CrushPressure", 1.1f, false);
            ulong due = combat.ActionStartedTick + FirstSeveranceScoreGeometry.CrushImpactTick;
            if (criticalClock.Take(1, tick, due))
            {
                PlayCritical("CrushCataclysm", combat, tick, due);
                scoreImpactTicks = 24;
            }
        }
        else if (combat.Substate == FirstSeveranceSubstate.RotatingBlade)
        {
            Cue(0, FirstSeveranceChoreography.BladeWindup, "PrismBeamSustain", .95f, false,
                FirstSeveranceChoreography.BladeEnd);
            Cue(1, FirstSeveranceChoreography.BladeWindup, "PrismBeamFire", 1.1f, false,
                FirstSeveranceChoreography.BladeWindup + 30);
            Cue(2, SecondTurnTick, "PrismBeamFire", 1.1f, false, SecondTurnTick + 30);
            if (tick >= combat.ActionStartedTick + FirstSeveranceChoreography.BladeEnd
                && SoundEngine.TryGetActiveSound(orbitVoice, out var orbit)) orbit.Stop();
        }
    }

    private ReLogic.Utilities.SlotId PlayCritical(string name, FirstSeveranceCombatProjection combat,
        ulong tick, ulong due, float gain = 1.25f, bool finishOnTerminal = true)
    {
        var id = Play(name, gain);
        bool accepted = SoundEngine.TryGetActiveSound(id, out var sound) && sound.IsPlaying;
        if (accepted && finishOnTerminal) impactTails.Add(id);
        ModContent.GetInstance<FirstSeveranceClientStateSystem>().Mod.Logger.Info(FormattableString.Invariant(
            $"FirstSeverance event=AudioCue seq={combat.EncounterSequence} fight={combat.FightId} cue={name} action={combat.Substate} tick={tick} due={due} late_ticks={tick - due} accepted={accepted} gain={FirstSeverancePresentationTiming.CueGain(name, gain):F3} slider={Main.soundVolume:F3} focused={Main.instance.IsActive}"));
        if (accepted && audioChecks.Count < 32) audioChecks.Add((id, name, Main.GameUpdateCount + 2));
        return id;
    }

    private void CheckAudioVoices()
    {
        for (int i = audioChecks.Count - 1; i >= 0; i--)
        {
            var check = audioChecks[i];
            if (Main.GameUpdateCount < check.CheckAt) continue;
            bool tracked = SoundEngine.TryGetActiveSound(check.Id, out var sound);
            float deviceGain = tracked && sound!.Sound is { IsDisposed: false } output ? output.Volume : 0;
            ModContent.GetInstance<FirstSeveranceClientStateSystem>().Mod.Logger.Info(FormattableString.Invariant(
                $"FirstSeverance event=AudioVoice cue={check.Name} playing={tracked && sound!.IsPlaying} device_gain={deviceGain:F3} slider={Main.soundVolume:F3}"));
            audioChecks.RemoveAt(i);
        }
    }

    private ReLogic.Utilities.SlotId Play(string name, float volume, float pitch = 0f)
    {
        // No position: raid-critical cues remain audible in a very large arena.
        // Main.soundVolume still applies; music uses Main.musicVolume separately.
        const string beamRoot = Root + "Beams/";
        string path = name switch
        {
            "LanceCharge" => beamRoot + "PortalCharge",
            "LanceFire" => beamRoot + "PortalFire",
            "CurtainFire" => beamRoot + "CurtainFire",
            "BeamGather" => beamRoot + "ChargeGather",
            "BeamLock" => beamRoot + "ChargeLock",
            "EnergyCharge" => beamRoot + "ChargeRush",
            "GridCharge" => beamRoot + "GridCharge",
            "GridFire" => beamRoot + "GridFire",
            "CoreSalvoFire" => beamRoot + "CoreSalvoFire",
            "PrismBeamCharge" => beamRoot + "WideCharge",
            "PrismBeamFire" => beamRoot + "WideFire",
            "PrismBeamSustain" => beamRoot + "BeamSustain",
            "FloodFire" => beamRoot + "FloodFire",
            "SpreadExecution" => beamRoot + "SpreadRay",
            "SpreadDissolve" => beamRoot + "SpreadScatter",
            _ => Root + name,
        };
        var style = new SoundStyle(path)
        {
            Volume = FirstSeverancePresentationTiming.CueGain(name, volume), Pitch = pitch, MaxInstances = 2,
            IsLooped = name == "PrismBeamSustain",
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        };
        var id = SoundEngine.PlaySound(style);
        voices.Add(id);
        return id;
    }

    private void PlayTimed(string name, float volume, ulong tick, ulong endTick, float pitch = 0f)
    {
        if (tick >= endTick) return;
        var id = Play(name, volume, pitch);
        TrackTimed(id, name, endTick);
    }

    private void TrackTimed(ReLogic.Utilities.SlotId id, string name, ulong endTick)
    {
        if (timedVoices.Count >= 64)
        {
            if (SoundEngine.TryGetActiveSound(timedVoices[0].Id, out var oldest)) oldest.Stop();
            timedVoices.RemoveAt(0);
        }
        int release = FirstSeverancePresentationTiming.BeamReleaseTicks(name);
        timedVoices.Add((id, endTick + (ulong)release, release));
    }

    private void UpdateTimedVoices(ulong tick)
    {
        for (int i = timedVoices.Count - 1; i >= 0; i--)
        {
            var voice = timedVoices[i];
            if (!SoundEngine.TryGetActiveSound(voice.Id, out var sound) || !sound.IsPlaying)
            { timedVoices.RemoveAt(i); continue; }
            if (tick >= voice.End)
            { sound.Stop(); timedVoices.RemoveAt(i); continue; }
            // ActiveSound.Volume is a multiplier, not the SoundStyle gain.
            sound.Volume = Math.Min(sound.Volume, FirstSeverancePresentationTiming.VoiceFade(tick, voice.End, voice.Release));
        }
    }

    internal void Draw(SpriteBatch batch, bool reduced) => mechanics.Draw(batch, reduced);

    private void AcceptResult(FirstSeveranceCombatProjection combat, ulong tick)
    {
        if (tick < combat.MechanicTick || combat.LastMechanicResult == FirstSeveranceMechanicResult.None) return;
        if (tick - combat.MechanicTick > 60)
        {
            ModContent.GetInstance<FirstSeveranceClientStateSystem>().Mod.Logger.Info($"FirstSeverance event=AudioCueExpired seq={combat.EncounterSequence} result={combat.LastMechanicResult} late_ticks={tick - combat.MechanicTick}");
            return;
        }
        stack = combat.LastMechanicResult is FirstSeveranceMechanicResult.StackPassed or FirstSeveranceMechanicResult.StackFailed;
        failed = combat.LastMechanicResult is FirstSeveranceMechanicResult.StackFailed or FirstSeveranceMechanicResult.SpreadFailed;
        mechanics.Accept(combat);
        resultTicks = 32;
        if (stack) PlayCritical(failed ? "ShellMassCollapse" : "ShellMassShed", combat, tick, combat.MechanicTick);
        else
        {
            bool dissipate = false;
            foreach (var impact in combat.MechanicImpacts) dissipate |= !impact.Failed;
            Play("SpreadExecution", .90f);
            if (dissipate) Play("SpreadDissolve", .68f);
        }
    }

    private void StopVoices(bool preserveImpacts = false)
    {
        timedVoices.Clear();
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            var id = voices[i];
            if (preserveImpacts && impactTails.Contains(id)) continue;
            if (SoundEngine.TryGetActiveSound(id, out var sound)) sound.Stop();
            impactTails.Remove(id);
            voices.RemoveAt(i);
        }
        if (!preserveImpacts) audioChecks.Clear();
    }

    internal void Reset(bool unload = false)
    {
        StopVoices();
        previous = null;
        pendingResult = null;
        criticalClock.Reset();
        mechanics.Reset(unload);
        previousEndingAge = -1;
        shardDeadline = 0;
        shardBeat = -1;
        resultTicks = 0;
        scoreImpactTicks = 0;
        chargeSerial = lockSerial = fireSerial = 0;
        gridChargeSerial = gridFireSerial = cannonChargeSerial = cannonFireSerial = 0;
        shellBroken = false;
        countdown = -1;
        scoreSounds.Clear();
        spreadCharges.Clear();
        spreadFires.Clear();
    }
}
