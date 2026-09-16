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
        self.assertLess(source.index('if (free < required)'), source.index('Projectile.NewProjectile'))
        self.assertIn('phaseStart, serial, hit.Accent', source)
        self.assertIn('CrimsonPhaseRules.Victory(phase, defeated, performerDefeated, allOut)', source)

    def test_delayed_old_phase_cannot_reactivate_hazards(self):
        source = (CONTENT / 'CrimsonActors.cs').read_text()
        self.assertIn('Hazard.Epoch == boss.State.PhaseStart', source)
        self.assertIn('State.SourceActive(Hazard.Source', source)
        self.assertIn('!next.CanReplace(State)', source)
        visuals = (CLIENT / 'CrimsonVisuals.cs').read_text()
        self.assertIn('h.Epoch != boss.State.PhaseStart', visuals)
        self.assertIn('age < h.Born', visuals)

    def test_public_name_does_not_rename_stable_item_or_encounter_ids(self):
        for locale in ('en-US', 'ja-JP'):
            source = (ROOT / f'Localization/CrimsonFoundry/{locale}.hjson').read_text()
            self.assertIn('CrimsonConductor', source)
            self.assertNotIn('Crimson Invocation', source)
            self.assertIn('RaidTitle:', source)
        self.assertIn('EncounterKey = "crimson_foundry"', (CONTENT / 'CrimsonDefinition.cs').read_text())
        self.assertIn('Mods.Convergence.CrimsonFoundry.RaidTitle', (CLIENT / 'CrimsonVisuals.cs').read_text())
