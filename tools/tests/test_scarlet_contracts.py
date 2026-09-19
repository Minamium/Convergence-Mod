"""Source wiring guards, not visual-quality or native gameplay approval."""
from pathlib import Path
import hashlib
import unittest
ROOT = Path(__file__).resolve().parents[2]
CONTENT = ROOT/'Content/Encounters/CrimsonFoundry'
CLIENT = ROOT/'Client/Encounters/CrimsonFoundry'

class ScarletContracts(unittest.TestCase):
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
    def test_runtime_owns_full_cycle_and_resolves_chorus_before_advancement(self):
        text=(CONTENT/'CrimsonRuntime.cs').read_text()
        self.assertIn('CrimsonRhythm.Create',text)
        self.assertNotIn('nextVolley',text)
        self.assertNotIn('score.Events(',text)
        self.assertLess(text.index('if (free < count)'),text.index('Projectile.NewProjectile'))
        self.assertLess(text.index('plans[i].Validate()'),text.index('Projectile.NewProjectile'))
        self.assertIn('cycle.Admit(phraseEnd, recoveryEnd)',text)
        self.assertIn('cycle.TryComplete(age, chorus is not null)',text)
        self.assertIn('ClearChorus(source)',text)
        self.assertIn('if (!TryScheduleChorus()) SchedulePhrase()',text)
        self.assertLess(text.index('TickChorus();'),text.index('cycle.TryComplete('))
        self.assertIn('if (phase < 3 && thresholdLatched',text)
        self.assertIn('phraseEnd - CrimsonRhythm.LookAheadTicks',text)
    def test_damage_one_is_hostile_only_and_keeps_native_hooks(self):
        for name in ('CrimsonGesture.cs','CrimsonActors.cs','CrimsonChorus.cs'):
            text=(CONTENT/name).read_text()
            self.assertIn('Projectile.damage = CrimsonPlaytestTuning.AttackDamage',text)
            self.assertIn('public override void ModifyHitPlayer',text)
            self.assertIn('modifiers.SetMaxDamage(CrimsonPlaytestTuning.AttackDamage)',text)
        for name in ('CrimsonGesture.cs','CrimsonChorus.cs'):
            text=(CONTENT/name).read_text()
            self.assertNotIn('statLife -=',text)
            self.assertNotIn('.Hurt(',text)
            self.assertNotIn('immuneTime = 0',text)
        for path in CONTENT.glob('*Companion*.cs'):
            self.assertNotIn('CrimsonPlaytestTuning.AttackDamage',path.read_text())
    def test_delayed_old_phase_cannot_reactivate_hazards(self):
        actors=(CONTENT/'CrimsonActors.cs').read_text()
        gesture=(CONTENT/'CrimsonGesture.cs').read_text()
        self.assertIn('!next.CanReplace(State)',actors)
        self.assertIn('boss.State.PhaseStart == Plan.Epoch',gesture)
        self.assertIn('boss.State.Fight == Plan.Fight',gesture)
        self.assertIn('Plan.Fight == Guid.Empty || next == Plan',gesture)
        self.assertIn('Main.netMode != NetmodeID.Server',gesture)
        state=(CONTENT/'CrimsonState.cs').read_text()
        self.assertIn('CompletedCycles < old.CompletedCycles',state)
    def test_physical_materials_share_collision_geometry_and_do_not_reintroduce_beams(self):
        visual=(CLIENT/'CrimsonGestureVisuals.cs').read_text()
        gesture=(CONTENT/'CrimsonGesture.cs').read_text()
        self.assertIn('CrimsonTechniqueGeometry.Write',visual)
        self.assertIn('CrimsonTechniqueGeometry.Write',gesture)
        self.assertIn('CrimsonTechniqueGeometry.Intersects',gesture)
        self.assertIn('ScarletMaterials.Strokes',visual)
        for forbidden in ('MagicPixel','PortalBeam','CrimsonEnergy.Add(','Capsule('):
            self.assertNotIn(forbidden,visual)
        self.assertIn('PrimitiveRenderer.RenderTrail',(CLIENT/'ScarletMaterials.cs').read_text())
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
    def test_conductor_stays_near_the_authority_focus_without_changing_final_pose(self):
        runtime=(CONTENT/'CrimsonRuntime.cs').read_text(encoding='utf-8')
        self.assertNotIn('new(f.CenterX, f.Top + 130)', runtime)
        self.assertIn('focus.Y - 260', runtime)
        self.assertIn('age >= poseUntil[3]', runtime)
        self.assertIn('phase == 3 ? focus + new Vector2(170, -200)', runtime)
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
