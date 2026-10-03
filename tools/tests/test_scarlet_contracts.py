"""Source wiring guards, not visual-quality or native gameplay approval."""
from pathlib import Path
import hashlib
import re
import unittest
ROOT = Path(__file__).resolve().parents[2]
CONTENT = ROOT/'Content/Encounters/CrimsonFoundry'
CLIENT = ROOT/'Client/Encounters/CrimsonFoundry'

class ScarletContracts(unittest.TestCase):
    def test_chorus_publishes_the_committed_verdict_not_an_out_of_tick_flag(self):
        text=(CONTENT/'CrimsonChorus.cs').read_text()
        sync=text[text.index('internal void Synchronize()'):text.index('public override void ReceiveExtraAI')]
        self.assertIn('Main.netMode == NetmodeID.Server',sync)
        self.assertIn('Plan.Fight != Guid.Empty && Projectile.active',sync)
        self.assertIn('NetMessage.SendData(MessageID.SyncProjectile',sync)
        resolve=text[text.index('private void TickChorus()'):text.index('private void ClearChorus')]
        self.assertLess(resolve.index('marker.ImpactPositions = positions'),resolve.index('marker.Synchronize();'))
        self.assertLess(resolve.index('marker.FailedMask |='),resolve.index('marker.Synchronize();'))
        self.assertNotIn('marker.Projectile.netUpdate',resolve)
        self.assertIn('ChorusReplicaReceived',text)
        visual=(CLIENT/'CrimsonChorusVisuals.cs').read_text()
        self.assertIn('event=ChorusDrawn',visual)
        self.assertIn('drawnResults.Clear()',visual)

    def test_native_floor_and_boss_bar_are_wired(self):
        for name in ('CrimsonEffigy.cs','CrimsonActors.cs'):
            text=(CONTENT/name).read_text()
            self.assertIn('NPC.BossBar = ModContent.GetInstance<CrimsonBossBar>()',text)
            self.assertIn('public override void ModifyIncomingHit',text)
            self.assertIn('modifiers.SetMaxDamage',text)
            self.assertIn('DamageFloor',text)
        bar=(CONTENT/'CrimsonBossBar.cs').read_text()
        self.assertIn('boss.State.BarLife',bar)
        self.assertIn('boss.State.BarMax',bar)
        self.assertNotIn('override bool PreDraw',bar)
    def test_terminal_endings_survive_the_cap_and_downed_rosters_wait(self):
        text=(CONTENT/'CrimsonRuntime.cs').read_text()
        flat=' '.join(text.split())
        self.assertIn('cancelled || ending < 0 && age > 60 * 60 * 15',text)
        self.assertIn('stage is CrimsonStage.Countdown or CrimsonStage.Performance && HasStandingMember()',flat)
        helper=text[text.index('private bool HasStandingMember()'):text.index('private void SchedulePhrase()')]
        self.assertIn('!m.Out && !m.Recovery.Downed',helper)
    def test_runtime_owns_full_cycle_and_resolves_chorus_before_advancement(self):
        text=(CONTENT/'CrimsonRuntime.cs').read_text()
        self.assertIn('CrimsonChoreography.Create',text)
        self.assertNotIn('nextVolley',text)
        self.assertNotIn('score.Events(',text)
        self.assertLess(text.index('if (free < count)'),text.index('Projectile.NewProjectile'))
        self.assertLess(text.index('plans[i].Validate()'),text.index('Projectile.NewProjectile'))
        self.assertIn('cycle.Admit(phraseEnd, recoveryEnd)',text)
        self.assertIn('cycle.TryComplete(age, chorus is not null)',text)
        self.assertIn('ClearChorus(source, preserveVerdict)',text)
        self.assertIn('if (!TryScheduleChorus()) SchedulePhrase()',text)
        self.assertLess(text.index('TickChorus();'),text.index('cycle.TryComplete('))
        self.assertIn('if (phase < 3 && thresholdLatched',text)
        # A phrase is booked on a bar head and issued one look-ahead before its first forecast.
        self.assertIn('BookPhrase(phraseEnd - musicStart, false)',text)
        self.assertIn('.FirstWarning - CrimsonRhythm.LookAheadTicks',text)
        self.assertIn('while (musicStart + rhythm.FirstWarning < age + CrimsonRhythm.LookAheadTicks)',text)
    def test_gameplay_clock_is_the_shared_128_bpm_grid_not_a_recorded_score(self):
        self.assertFalse((ROOT/'Assets/Music/CrimsonFoundry/Score.json').exists())
        self.assertFalse((CONTENT/'CrimsonScore.cs').exists())
        self.assertTrue((ROOT/'Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg').exists())
        for folder in (CONTENT,CLIENT):
            for path in folder.rglob('*.cs'):
                text=path.read_text(encoding='utf-8')
                # Whole word: the reward box CrimsonScoreReliquary is not the retired CrimsonScore beat map.
                self.assertIsNone(re.search(r'\bCrimsonScore\b',text),f'{path.name} still reads the retired recorded beat map')
                for stale in ('Score.json','CrimsonRegistration.Score','score.Events(','NextBeats(score'):
                    self.assertNotIn(stale,text,f'{path.name} still reads the retired recorded beat map')
        meter=(CONTENT/'CrimsonMeter.cs').read_text(encoding='utf-8')
        self.assertIn('BeatSamples = 22500',meter)
        self.assertIn('(beat * 225 + 4) / 8',meter)
        self.assertIn('internal static class CrimsonArrangement',meter)
        for portable in ('CrimsonMeter.cs','CrimsonRhythm.cs','CrimsonChorusRules.cs'):
            self.assertNotIn('using Terraria',(CONTENT/portable).read_text(encoding='utf-8'))
        mixer=(CLIENT/'CrimsonMusicMixer.cs').read_text(encoding='utf-8')
        for dependency in ('using Terraria','Microsoft.Xna','NVorbis'):
            self.assertNotIn(dependency,mixer)
        project=(ROOT/'Tests/Convergence.DomainTests/Convergence.DomainTests.csproj').read_text(encoding='utf-8')
        self.assertIn('Content/Encounters/CrimsonFoundry/CrimsonMeter.cs',project)
        self.assertIn('Client/Encounters/CrimsonFoundry/CrimsonMusicMixer.cs',project)
        self.assertNotIn('CrimsonScore',project)
        self.assertNotIn('Score.json',project)
        choreography=(CONTENT/'CrimsonChoreography.cs').read_text(encoding='utf-8')
        self.assertIn('internal static CrimsonRhythmPhrase Create(int earliest, int serial, int phase, bool pickup)',choreography)
        self.assertIn('CrimsonMeter.EighthTick(first + eighth)',choreography)
        self.assertIn('CrimsonMeter.BarTick(CrimsonMeter.OpeningBars)',choreography)
        runtime=(CONTENT/'CrimsonRuntime.cs').read_text(encoding='utf-8')
        self.assertIn('unlockAt = musicStart + CrimsonChoreography.OpeningTicks',runtime)
        rules=(CONTENT/'CrimsonChorusRules.cs').read_text(encoding='utf-8')
        self.assertIn('CrimsonMeter.BarAtOrAfter(earliest) * CrimsonMeter.BeatsPerBar',rules)
        chorus=(CONTENT/'CrimsonChorus.cs').read_text(encoding='utf-8')
        self.assertIn('CrimsonChorusRules.Schedule(earliest)',chorus)
        self.assertIn('BookPhrase(plan.End - musicStart, true);',chorus)

    def test_act_changes_are_booked_for_a_bar_head_and_unlock_on_the_arrangement_grid(self):
        text=(CONTENT/'CrimsonRuntime.cs').read_text(encoding='utf-8')
        flat=' '.join(text.split())
        self.assertIn('pendingAdvance = -1;',flat)
        # An Act change waits for a bar head; nothing else completes or schedules meanwhile.
        self.assertIn('if (pendingAdvance < 0 && cycle.TryComplete(age, chorus is not null))',flat)
        self.assertIn('pendingAdvance = musicStart + CrimsonMeter.BarTick(CrimsonMeter.BarAtOrAfter(age - musicStart));',flat)
        self.assertIn('if (pendingAdvance >= 0 && age >= pendingAdvance && summons[phase] is { } retired)',flat)
        self.assertIn('{ pendingAdvance = -1; AdvancePhase(retired); }',flat)
        self.assertIn('if (pendingAdvance < 0 && !cycle.Full && age >= nextPhrase',flat)
        self.assertLess(flat.index('pendingAdvance = musicStart + CrimsonMeter.BarTick'),flat.index('AdvancePhase(retired)'))
        advance=flat[flat.index('private void AdvancePhase('):]
        self.assertLess(advance.index('phase++; phaseStart = age;'),advance.index('CrimsonArrangement.TransitionBars(phase)'))
        self.assertIn('unlockAt = musicStart + CrimsonMeter.BarTick(CrimsonMeter.BarAt(age - musicStart) + CrimsonArrangement.TransitionBars(phase));',advance)
        self.assertNotIn('CrimsonEnsemble.Transition(phase)',advance[:advance.index('SpawnSummon')])
        self.assertIn('BookPhrase(unlockAt - musicStart, true);',advance)

    def test_music_is_one_dynamic_voice_fed_by_the_mixer_and_never_reads_a_score_file(self):
        audio=(CLIENT/'CrimsonAudio.cs').read_text(encoding='utf-8')
        for rule in ('DynamicSoundEffectInstance','CrimsonMusicMixer','Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg',
                     'mixer.Render(','voice.SubmitBuffer(','mixer.SetStage(','mixer.End(',
                     'CrimsonMeter.BarAt(state.PhaseStart - state.MusicStart)'):
            self.assertIn(rule,audio)
        for retired in ('Score.json','CrimsonScore','CrimsonRegistration','SoundEffectInstance voice','LoopStart'):
            self.assertNotIn(retired,audio)
        # The dedicated server must never create an audio device.
        self.assertRegex(audio,r'\[Autoload\(Side = ModSide\.Client\)\]\s*internal sealed class CrimsonAudio : ModSystem')

    def test_damage_one_is_hostile_only_and_keeps_native_hooks(self):
        # All hostile sources, including failed chorus, now share the owner-gated
        # mapping. Numeric/success-zero behavior is checked by the domain suite.
        for name, source in (('CrimsonGesture.cs', 'Plan'),
                             ('CrimsonActors.cs', 'Hazard'),
                             ('CrimsonChorus.cs', 'Impact')):
            text=(CONTENT/name).read_text()
            self.assertIn(f'Projectile.damage = CrimsonPlaytestTuning.NativeSourceDamage({source}.Damage)',text)
            self.assertIn('public override void ModifyHitPlayer',text)
            self.assertIn(f'modifiers.SetMaxDamage(CrimsonPlaytestTuning.NativeFinalDamageLimit({source}.Damage))',text)
            self.assertNotIn('statLife -=',text)
            self.assertNotIn('.Hurt(',text)
            self.assertNotIn('immuneTime = 0',text)
        for path in CONTENT.glob('*Companion*.cs'):
            text=path.read_text()
            for mapping in ('AttackDamage', 'NativeSourceDamage', 'NativeFinalDamageLimit'):
                self.assertNotIn('CrimsonPlaytestTuning.'+mapping,text)
    def test_delayed_old_phase_cannot_reactivate_hazards(self):
        actors=(CONTENT/'CrimsonActors.cs').read_text()
        gesture=(CONTENT/'CrimsonGesture.cs').read_text()
        self.assertIn('!next.CanReplace(State)',actors)
        self.assertIn('boss.State.PhaseStart == Plan.Epoch',gesture)
        self.assertIn('boss.State.Fight == Plan.Fight',gesture)
        self.assertIn('Plan.Fight != Guid.Empty && next != Plan',gesture)
        self.assertIn('Main.netMode == NetmodeID.Server',gesture)
        self.assertIn('CrimsonTrackingBeam.CanAccept',gesture)
        self.assertIn('Token == Plan.TargetConnection',gesture)
        self.assertIn('!AimLocked',gesture)
        state=(CONTENT/'CrimsonState.cs').read_text()
        self.assertIn('CompletedCycles < old.CompletedCycles',state)
    def test_materials_share_collision_geometry_and_single_beam_replaces_decks(self):
        visual=(CLIENT/'CrimsonGestureVisuals.cs').read_text()
        gesture=(CONTENT/'CrimsonGesture.cs').read_text()
        self.assertIn('CrimsonTechniqueGeometry.Write',visual)
        self.assertIn('CrimsonTechniqueGeometry.Write',gesture)
        self.assertIn('CrimsonTechniqueGeometry.Intersects',gesture)
        self.assertIn('ScarletMaterials.Strokes',visual)
        for forbidden in ('MagicPixel','Capsule('):
            self.assertNotIn(forbidden,visual)
        self.assertIn('PrimitiveRenderer.RenderTrail',(CLIENT/'ScarletMaterials.cs').read_text())
        runtime=(CONTENT/'CrimsonRuntime.cs').read_text()
        self.assertIn('techniques[source] = CrimsonTechnique.TrackingBeam',runtime)
        self.assertNotIn('CrimsonTechniqueGeometry.Select(source',runtime)
        # Notes use the owner-approved Scarlet set (test_scarlet_audio owns the details).
        self.assertNotIn('FirstSeverance/Beams/',visual)
        self.assertIn('ScarletCue.Impact',visual)
        self.assertIn('gesture.EffectivePlan(age, true)',visual)
        self.assertIn('fieldBeam: true',visual)
        energy=(CLIENT/'CrimsonEnergy.cs').read_text()
        self.assertIn('PortalForecastPass',energy)
        self.assertIn('ForecastDustPass',energy)
    def test_approved_background_is_explicitly_gated_and_never_substituted(self):
        sky=(CLIENT/'CrimsonSky.cs').read_text()
        self.assertIn('ScarletSanctum',sky)
        self.assertIn('scarlet.background_asset_missing',sky)
        self.assertIn('sky.Available && ScarletArticulation.Participant',sky)
        self.assertIn('SkyManager.Instance.Deactivate(CrimsonSky.Key)',sky)
        self.assertNotIn('HollowCathedral',sky)
        self.assertNotIn('MagicPixel',sky)
        self.assertNotIn('Main.time =',sky)
        image=ROOT/'Assets/Textures/Backgrounds/ScarletSanctum.png'
        if image.exists():
            self.assertEqual('802d1f6393ae6f0919214e3de535c1b38cc8e740161fcb98e5ba7f71c5a7e1ef',hashlib.sha256(image.read_bytes()).hexdigest())
        self.assertIn('ScarletSanctum.rawimg', sky)
    def test_conductor_is_fixed_and_does_not_follow_aimed_gestures(self):
        runtime=(CONTENT/'CrimsonRuntime.cs').read_text(encoding='utf-8')
        self.assertNotIn('new(f.CenterX, f.Top + 130)', runtime)
        self.assertIn('CrimsonChoreography.Conductor(f)', runtime)
        self.assertIn('actor.NPC.Center = new(fixedCenter.X, fixedCenter.Y)', runtime)
        self.assertNotIn('age >= poseUntil[3]', runtime)
        self.assertIn('if (source == 3) return false', (CONTENT/'CrimsonGesture.cs').read_text())
    def test_floor_noise_is_not_repaid_by_small_native_teleports(self):
        text=(CONTENT/'CrimsonFieldPlayer.cs').read_text()
        self.assertIn('CrimsonChoreography.ClampParticipant',text)
        self.assertIn('48 * 48',text)
        self.assertIn('event=MajorFieldCorrection',text)
    def test_forecast_and_attack_materials_are_separate_and_energy_restores_batch(self):
        shader=(ROOT/'Assets/AutoloadedEffects/Shaders/ScarletRibbon.fx').read_text(encoding='utf-8')
        self.assertIn('float4 Forecast(VO i)', shader)
        self.assertIn('if(motion.x>.5 && signal.y>.5) return Forecast(i)', shader)
        energy=(CLIENT/'CrimsonEnergy.cs').read_text(encoding='utf-8')
        self.assertIn('using var scope = new ScarletGraphicsScope(batch)', energy)
        self.assertNotIn('batch.Begin(', energy)
    def test_secondary_motion_is_cosmetic_and_metaballs_are_bounded(self):
        rig=(CLIENT/'ScarletArticulation.cs').read_text()
        self.assertIn('PushdownAutomata',rig)
        self.assertIn('VerletSimulations.VerletSimulation',rig)
        self.assertIn('PiecewiseCurve',rig)
        self.assertIn('CutsceneManager.QueueCutscene',rig)
        self.assertIn('info.ShakeStrength = 0',rig)
        for p in CONTENT.glob('*.cs'):
            self.assertNotIn('VerletSimulations',p.read_text())
        atmosphere=(CLIENT/'ScarletAtmosphere.cs').read_text()
        self.assertIn('96 - particles.ActiveParticleCount',atmosphere)
        self.assertIn('DrawnManually => true',atmosphere)
        self.assertIn('p.ExtraInfo[0] >= p.ExtraInfo[1]',atmosphere)
        self.assertIn('particles.Epoch != plan.Epoch',atmosphere)
    def test_presentation_restores_actual_batch_and_gpu_bindings(self):
        text=(ROOT/'Client/Graphics/WorldGraphicsScope.cs').read_text()
        self.assertIn('WorldBatchParameters.Capture(batch)',text)
        self.assertIn('batchParameters.Restore(batch)',text)
        self.assertIn('device.SetVertexBuffers(bindings)',text)
        self.assertIn('device.Indices = indices',text)
        self.assertIn('device.ScissorRectangle = scissor',text)
        for slot in range(4):
            self.assertIn(f'device.Textures[{slot}] = t{slot}',text)
            self.assertIn(f'device.SamplerStates[{slot}] = s{slot}',text)
    def test_trail_support_point_uv_and_long_tail_are_wired(self):
        material=(CLIENT/'ScarletMaterials.cs').read_text()
        self.assertIn('submittedPoints.Add(points[^1] + (points[^1] - points[^2]))',material)
        self.assertIn('PrimitiveRenderer.RenderTrail(submittedPoints, settings!, submittedPoints.Count)',material)
        self.assertIn('u * trailCompletionScale',material)
        shader=(ROOT/'Assets/AutoloadedEffects/Shaders/ScarletRibbon.fx').read_text()
        self.assertIn('o.U.x*=trailCompletionScale',shader)
        for name in ('CrimsonRuntime.cs','CrimsonGesture.cs'):
            self.assertIn('CrimsonRhythm.LeaseTicks',(CONTENT/name).read_text())
        self.assertIn('cycle.FinishAt',(CONTENT/'CrimsonChorus.cs').read_text())
        visual=(CLIENT/'CrimsonGestureVisuals.cs').read_text()
        for name in ('WarningOpacity','LiveOpacity','SampleAge'):
            self.assertIn('ScarletGesturePresentation.'+name,visual)
    def test_public_name_preserves_stable_item_and_encounter_ids(self):
        for locale in ('en-US','ja-JP'):
            text=(ROOT/f'Localization/CrimsonFoundry/{locale}.hjson').read_text(encoding='utf-8')
            self.assertIn('CrimsonConductor',text)
            self.assertNotIn('Crimson Invocation',text)
            self.assertIn('CinematicCamera',text)
        self.assertIn('EncounterKey = "crimson_foundry"',(CONTENT/'CrimsonDefinition.cs').read_text())
    def test_doll_attendant_keeps_exact_doll_preparation_ownership(self):
        text=(ROOT/'Content/Encounters/FirstSeverance/Actors/FirstSeveranceDollAttendant.cs').read_text()
        for rule in ('snapshot.DefinitionKey != FirstSeveranceIdentity.EncounterKey','prep.FightId == snapshot.FightId',
                     'prep.EncounterSequence == snapshot.EncounterSequence','prep.GroundX - core.GroundCenter.X',
                     '!OwnsDollPreparation(core)','&& !onStage) Retire(NPC)'):
            self.assertIn(rule,text)

    def test_sacrifice_validates_all_native_owners_before_retiring_any(self):
        text=(CONTENT/'CrimsonRuntime.cs').read_text()
        body=text[text.index('private bool SacrificeSummons()'):text.index('private void SpawnSummon')]
        guard='child.NPC.ModNPC != child || !Matches(child)'
        self.assertLess(body.index(guard),body.index('child.NPC.active = false'))
        self.assertNotIn('StrikeNPC',body)
        self.assertNotIn('checkDead',body)
        self.assertIn('defeated == CrimsonInvocation.AllDefeated',body)
        self.assertIn('!SacrificeSummons()) return End(EncounterEndReason.EncounterActorMissing)',text)

    def test_companion_uses_owner_target_incarnation_and_resolved_tail_is_harmless(self):
        companion=(CONTENT/'CrimsonCompanion.cs').read_text()
        for rule in ('CrimsonCovenantRules.Select','CrimsonCovenantIncarnation','InstancePerEntity => true','Projectile.owner == Main.myPlayer'):
            self.assertIn(rule,companion)
        chorus=(CONTENT/'CrimsonChorus.cs').read_text()
        self.assertIn('CrimsonChorusImpactPositions.Read',chorus)
        self.assertIn('ImpactPositions.AsSpan().SequenceEqual',chorus)
        self.assertIn('TryBoss(Plan, out var boss, Resolved)',chorus)
        self.assertIn('!CrimsonChorus.TryBoss(Impact.Plan, out var boss)',chorus)

    def test_ready_pill_matches_doll_and_does_not_follow_player_or_ui_scale(self):
        doll=(ROOT/'Client/Encounters/FirstSeverance/FirstSeverancePreparationVisuals.cs').read_text()
        scarlet=(CLIENT/'CrimsonVisuals.cs').read_text()
        self.assertIn('new Rectangle(width / 2 - 100, 64, 200, 36)',doll)
        self.assertIn('new Rectangle(view.Width / 2 - 100, 64, 200, 36)',scarlet)
        self.assertIn('InterfaceScaleType.None',scarlet)
        self.assertNotIn('Math.Clamp(pos.X - 78',scarlet)
        self.assertNotIn('Main.UIScale',scarlet)
        self.assertIn('if (m.Ready && !p.dead)',scarlet)

    def test_cluster_flight_uses_actual_end_and_fixed_conductor_can_overlap_next_bar(self):
        text=(CONTENT/'CrimsonRuntime.cs').read_text()
        self.assertIn('last[source] = Math.Max(last[source], musicStart + CrimsonEnsemble.NoteEnd(technique, note))',text)
        self.assertIn('begins[source] = phase == 3 ? age : Math.Max(age, poseUntil[source])',text)
        self.assertIn('plan.LastEnd + CrimsonRhythm.LeaseTicks',text)
        self.assertIn('cycle.Admit(phraseEnd, recoveryEnd)',text)
        visual=(CLIENT/'CrimsonGestureVisuals.cs').read_text()
        self.assertIn('cluster.TryBoss(out var parent) && parent == boss',visual)
        self.assertIn('ScarletClusters.Draw(batch, cluster.Plan, age)',visual)

    def test_signature_moves_share_one_terraria_free_geometry_and_one_curtain_observation(self):
        moves=(CONTENT/'CrimsonSignatureMoves.cs').read_text(encoding='utf-8')
        for dependency in ('using Terraria','Microsoft.Xna'):
            self.assertNotIn(dependency,moves)
        technique=(CONTENT/'CrimsonTechnique.cs').read_text(encoding='utf-8')
        self.assertIn('ClusterVolley, ChoirRakes,',technique)
        self.assertIn('CinderCurtain, ShroudRope, FourHands',technique)
        self.assertIn('CrimsonSignatureMoves.Write(p, age, destination, forecast)',technique)
        self.assertIn('IsSignature && Pulse >= CrimsonChoreography.BasicNotes',technique)
        self.assertIn('CrimsonSignatureMoves.IsSignaturePhrase(phase, phrase)',(CONTENT/'CrimsonChoreography.cs').read_text(encoding='utf-8'))
        self.assertIn('CrimsonSignatureMoves.LiveTicks(technique)',(CONTENT/'CrimsonEnsemble.cs').read_text(encoding='utf-8'))
        runtime=' '.join((CONTENT/'CrimsonRuntime.cs').read_text(encoding='utf-8').split())
        # The curtain observes every eligible member once for all four notes (a column mask in Target); the other moves are unaimed.
        self.assertIn('else if (technique == CrimsonTechnique.CinderCurtain)',runtime)
        self.assertIn('foreach (var member in eligible)',runtime)
        self.assertIn('occupied |= 1 << CrimsonSignatureMoves.CurtainColumn(field, Main.player[member.Slot].Center.X);',runtime)
        self.assertIn('aim = CrimsonSignatureMoves.CurtainTarget(occupied);',runtime)
        self.assertNotIn('var walker',runtime)
        self.assertIn('crimson.gesture_curtain_mask',technique)
        self.assertIn('CrimsonTechnique.ChoirRakes or CrimsonTechnique.ShroudRope or CrimsonTechnique.FourHands',runtime)
        self.assertIn('techniques[source] = CrimsonTechnique.TrackingBeam',runtime)
        visual=(CLIENT/'CrimsonGestureVisuals.cs').read_text(encoding='utf-8')
        self.assertIn('!p.Aimed && !p.IsRift && !p.IsSignature',visual)
        self.assertIn('CrimsonSignatureMoves.ResidueTicks(p.Technique)',visual)
        self.assertIn('fieldBeam: true',visual)
        # A full crowd mask can leave a curtain note nothing to burn: no cue, shake or embers; the strike is felt at a burning column.
        self.assertIn('CrimsonSignatureMoves.CurtainBurning(p) == 0) return;',visual)
        self.assertIn('CrimsonSignatureMoves.CurtainImpact(p, Main.LocalPlayer.Center.X)',visual)
        self.assertIn('strokes[i * count / budget].B',(CLIENT/'ScarletAtmosphere.cs').read_text(encoding='utf-8'))
        self.assertIn('Content/Encounters/CrimsonFoundry/CrimsonSignatureMoves.cs',(ROOT/'Tests/Convergence.DomainTests/Convergence.DomainTests.csproj').read_text(encoding='utf-8'))
        self.assertIn('public const ushort CurrentVersion = 80;',(ROOT/'Common/Networking/Protocol/EncounterProtocol.cs').read_text(encoding='utf-8'))

    def test_covenant_follows_live_target_width_and_uses_the_same_scale_for_damage_geometry(self):
        text=(CONTENT/'CrimsonCompanion.cs').read_text()
        ray=text.split('public sealed class CrimsonCompanionRay')[1]
        self.assertNotIn('Projectile.ai[0] <= CrimsonCovenantRules.ChargeTicks',ray)
        self.assertIn('target.GetGlobalNPC<CrimsonCovenantIncarnation>().Value == incarnation',ray)
        self.assertIn('Projectile.Center = target.Center',ray)
        self.assertIn('CrimsonCovenantRules.HalfSpan(target.width, BatchCount)',ray)
        self.assertIn('else { Projectile.Kill(); return; }',ray)
        self.assertIn('48 * Size * Opening',ray)
        self.assertIn('writer.Write(BatchCount)',ray)
        self.assertIn('ray.Size',(CLIENT/'CrimsonCompanionVisuals.cs').read_text())
        self.assertIn('CrimsonCovenantRules.DamageFactor(count)',text)

    def test_terminal_melt_is_visual_only_and_final_draw_does_not_duplicate_native_sacrifices(self):
        rig=(CLIENT/'CrimsonRig.cs').read_text()
        self.assertIn('ScarletInvocationScene.Victory',rig)
        self.assertIn('if (boss!.State.Phase == 3) return false',rig)
        self.assertIn('material.TrySetParameter("ceremony", new Vector2(dissolve, melt))',rig)
        material=(ROOT/'Assets/AutoloadedEffects/Shaders/ScarletSurface.fx').read_text()
        self.assertIn('float2 uv=i.U*region.zw+region.xy',material)
        self.assertIn('material.TrySetParameter("frameSpan"',rig)
        self.assertNotIn('(i.U-region.xy)/region.zw',material)
        runtime=(CONTENT/'CrimsonRuntime.cs').read_text()
        self.assertIn('ending + 150',runtime)
        self.assertIn('CrimsonEnsemble.SacrificeComplete',runtime)

    def test_field_beam_live_uses_scarlet_ink_while_forecast_and_seals_stay_original(self):
        visual=(CLIENT/'CrimsonGestureVisuals.cs').read_text(encoding='utf-8')
        beams=visual[visual.index('private static void DrawTrackingBeams'):visual.index('private static void DrawSources')]
        # Forecasts keep the portal energy; the two crossflow seals and the rift tears keep ScarletSorcery.
        self.assertIn('CrimsonEnergy.Add(',beams)
        self.assertIn('if (age < p.Fire) ScarletSorcery.CrossflowSeals(batch, p, age);',beams)
        self.assertIn('ScarletSorcery.Tear(',beams)
        # A live crossflow's seals are drawn after the ink, over the stream's ends (protocol80).
        self.assertIn('else sealsOver.Add(p);',beams)
        self.assertLess(beams.index('ink.Draw(view, ScarletVfxHost.Assets, strike)'),beams.index('foreach (var p in sealsOver) ScarletSorcery.CrossflowSeals(batch, p, age);'))
        sorcery=(CLIENT/'ScarletSorcery.cs').read_text(encoding='utf-8')
        self.assertIn('var (right, left) = CrimsonChoreography.Seals(p);',sorcery)
        # Live strike and residue never reach CrimsonEnergy: they are collected before it and drawn after it (over forecasts).
        self.assertLess(beams.index('ScarletInkStroke.Owns(p, age)'),beams.index('CrimsonEnergy.Add('))
        self.assertLess(beams.index('CrimsonEnergy.Draw(batch)'),beams.index('ink.Draw(view, ScarletVfxHost.Assets, strike)'))
        self.assertIn('ScarletInkStroke.ResidueTicks',beams)
        self.assertIn('using var scope = new ScarletGraphicsScope(batch)',beams)
        # Only the two field-beam techniques; the forecast pass of ScarletInk is not used anywhere in game code.
        stroke=(CLIENT/'Vfx/ScarletInkStroke.cs').read_text(encoding='utf-8')
        self.assertIn('plan.Technique is CrimsonTechnique.TrackingBeam or CrimsonTechnique.SideBeams',stroke)
        self.assertIn('"AutoloadPass"',stroke)
        self.assertIn('"ResiduePass"',stroke)
        self.assertNotIn('"ForecastPass"',stroke)
        # Residue stays inside the projectile lease (LastEnd + ResidueTicks + 4).
        rhythm=(CONTENT/'CrimsonRhythm.cs').read_text(encoding='utf-8')
        self.assertIn('internal const int ResidueTicks = 24;',rhythm)
        self.assertIn('const int CloseTicks = 8, ResidueTicks = 24;',stroke)
        self.assertIn('LeaseTicks = ResidueTicks + 4',rhythm)
        # Production host: Luminance shader by the autoload name, noise from the registry, CrimsonEnergy's transform.
        host=(CLIENT/'ScarletVfxHost.cs').read_text(encoding='utf-8')
        self.assertIn('ShaderManager.GetShader("Convergence." + name)',host)
        for noise in ('WavyBlotchNoise','TurbulentNoise','DendriticNoiseZoomedOut'):
            self.assertIn(f'MiscTexturesRegistry.{noise}.Value',host)
        self.assertIn('Main.GameViewMatrix.TransformationMatrix',host)
        self.assertIn('device.Viewport.Width, device.Viewport.Height',host)
        self.assertIn('CrimsonVisuals.Reduced',host)
        energy=(CLIENT/'CrimsonEnergy.cs').read_text(encoding='utf-8')
        self.assertIn('Main.GameViewMatrix.TransformationMatrix * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1)',energy)
        self.assertIn('DrawTrackingBeams(boss!, batch, age)',visual) # the fractional RenderAge drives it
        self.assertIn('ScarletVfxHost.View(age)',beams)
        # The Vfx foundation is linked into the offline preview: it must not name Terraria, tModLoader or Luminance.
        for path in (CLIENT/'Vfx').glob('*.cs'):
            code=path.read_text(encoding='utf-8')
            for forbidden in ('using Terraria','using Luminance','ModContent.','ShaderManager.','MiscTexturesRegistry.','Main.instance'):
                self.assertNotIn(forbidden,code,f'{path.name} must stay Terraria-free ({forbidden})')
        shader=(ROOT/'Assets/AutoloadedEffects/Shaders/ScarletInk.fx').read_text(encoding='utf-8')
        for pass_name in ('AutoloadPass','ForecastPass','ResiduePass'):
            self.assertIn(f'pass {pass_name}',shader)
