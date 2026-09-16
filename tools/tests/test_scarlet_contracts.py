"""Source-wiring guards; these do not substitute for a native tModLoader load."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
CONTENT = ROOT / 'Content/Encounters/CrimsonFoundry'
CLIENT = ROOT / 'Client/Encounters/CrimsonFoundry'


class ScarletContracts(unittest.TestCase):
    def test_native_floor_and_boss_bar_are_wired(self):
        effigy = (CONTENT / 'CrimsonEffigy.cs').read_text()
        actors = (CONTENT / 'CrimsonActors.cs').read_text()
        self.assertIn('public override void ModifyIncomingHit', effigy)
        self.assertIn('modifiers.SetMaxDamage', effigy)
        self.assertIn('public override bool CheckDead()', effigy)
        for source in (effigy, actors):
            self.assertIn('NPC.BossBar = ModContent.GetInstance<CrimsonBossBar>()', source)
        bar = (CONTENT / 'CrimsonBossBar.cs').read_text()
        self.assertIn('boss.State.BarLife', bar)
        self.assertIn('boss.State.BarMax', bar)
        self.assertNotIn('override bool PreDraw', bar)

    def test_runtime_consumes_phrases_not_legacy_single_event_cooldown(self):
        source = (CONTENT / 'CrimsonRuntime.cs').read_text()
        self.assertIn('CrimsonRhythm.Create', source)
        self.assertNotIn('nextVolley', source)
        self.assertNotIn('score.Events(', source)
        self.assertLess(source.index('if (free < count)'), source.index('Projectile.NewProjectile'))
        self.assertLess(source.index('plans[i].Validate()'), source.index('Projectile.NewProjectile'))
        self.assertIn('phaseStart, serial, (byte)i, (byte)source', source)
        self.assertIn('CrimsonPhaseRules.Victory(phase, defeated, performerDefeated, allOut)', source)
        self.assertIn('nextPhrase = phraseEnd - CrimsonRhythm.LookAheadTicks', source)
        self.assertIn('new CrimsonGestureSource(plan)', source)
        self.assertNotIn('ModContent.ProjectileType<CrimsonAttack>()', source)

    def test_delayed_old_phase_cannot_reactivate_hazards(self):
        actors = (CONTENT / 'CrimsonActors.cs').read_text()
        gesture = (CONTENT / 'CrimsonGesture.cs').read_text()
        self.assertIn('!next.CanReplace(State)', actors)
        self.assertIn('boss.State.PhaseStart == Plan.Epoch', gesture)
        self.assertIn('boss.State.Fight == Plan.Fight', gesture)
        self.assertIn('Plan.Fight == Guid.Empty || next == Plan', gesture)
        self.assertIn('Main.netMode != NetmodeID.Server', gesture)
        self.assertIn('boss!.State.SourceActive(Plan.Source', gesture)
        visuals = (CLIENT / 'CrimsonGestureVisuals.cs').read_text()
        self.assertIn('gesture.TryBoss(out var owner)', visuals)
        self.assertIn('age < p.Born', visuals)

    def test_public_name_does_not_rename_stable_item_or_encounter_ids(self):
        for locale in ('en-US', 'ja-JP'):
            source = (ROOT / f'Localization/CrimsonFoundry/{locale}.hjson').read_text()
            self.assertIn('CrimsonConductor', source)
            self.assertNotIn('Crimson Invocation', source)
            self.assertIn('RaidTitle:', source)
        self.assertIn('EncounterKey = "crimson_foundry"', (CONTENT / 'CrimsonDefinition.cs').read_text())
        self.assertIn('Mods.Convergence.CrimsonFoundry.RaidTitle', (CLIENT / 'CrimsonVisuals.cs').read_text())

    def test_physical_geometry_is_shared_and_native_damage_is_not_emulated(self):
        gesture = (CONTENT / 'CrimsonGesture.cs').read_text()
        visuals = (CLIENT / 'CrimsonGestureVisuals.cs').read_text()
        self.assertIn('CrimsonTechniqueGeometry.Write', gesture)
        self.assertIn('CrimsonTechniqueGeometry.Write', visuals)
        self.assertIn('CrimsonTechniqueGeometry.Intersects', gesture)
        self.assertIn('Projectile.hostile =', gesture)
        self.assertNotIn('statLife', gesture)
        self.assertNotIn('Player.Hurt', gesture)
        self.assertNotIn('PortalBeam', visuals)
        self.assertNotIn('CrimsonEnergy.Add(', visuals)
        self.assertIn('CrimsonEnergy.AddCore', visuals)

    def test_physical_body_path_is_projected_on_both_native_actor_types(self):
        for file in ('CrimsonActors.cs', 'CrimsonEffigy.cs'):
            self.assertIn('CrimsonGesture.ProjectMotion(NPC', (CONTENT / file).read_text())
        gesture = (CONTENT / 'CrimsonGesture.cs').read_text()
        self.assertIn('plan.Body(age)', gesture)
        self.assertIn('npc.velocity = Vector2.Zero', gesture)
        self.assertIn('CrimsonTechniqueGeometry.LimitTravel', (CONTENT / 'CrimsonRuntime.cs').read_text())
        rig = (CLIENT / 'CrimsonRig.cs').read_text()
        for species in range(3):
            self.assertIn(f'species == {species}', rig)
        self.assertIn('CrimsonTechnique.MantleRush', rig)

    def test_scarlet_sky_is_separate_and_does_not_mutate_the_world(self):
        sky = (CLIENT / 'CrimsonSky.cs').read_text()
        self.assertIn('CrimsonSky : CustomSky', sky)
        self.assertIn('Autoload(Side = ModSide.Client)', sky)
        self.assertIn('MiscTexturesRegistry.TurbulentNoise', sky)
        self.assertNotIn('HollowCathedral', sky)
        self.assertNotIn('Main.time =', sky)
        self.assertNotIn('Main.raining =', sky)
        self.assertIn('SkyManager.Instance.Deactivate(CrimsonSky.Key)', sky)
        self.assertIn('OnWorldUnload()', sky)

    def test_doll_attendant_requires_exact_doll_preparation_at_spawn_and_draw(self):
        source = (ROOT / 'Content/Encounters/FirstSeverance/Actors/FirstSeveranceDollAttendant.cs').read_text()
        self.assertIn('snapshot.DefinitionKey != FirstSeveranceIdentity.EncounterKey', source)
        self.assertIn('prep.FightId == snapshot.FightId', source)
        self.assertIn('prep.EncounterSequence == snapshot.EncounterSequence', source)
        self.assertIn('prep.GroundX - core.GroundCenter.X', source)
        self.assertIn('!OwnsDollPreparation(core)', source)
        self.assertIn('FirstSeveranceDollAttendant.OwnsDollPreparation(core)', source)
        self.assertIn('&& !onStage) Retire(NPC)', source)
