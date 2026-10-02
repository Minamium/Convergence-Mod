"""Contracts of the Scarlet rig harness (tools/preview-scarlet-rigs.ps1, tools/fixtures/ScarletRig*.cs,
tools/fixtures/ScarletPreviewHost.cs) and its encoder (tools/encode_scarlet_preview.py).

Pure Python and text checks (CI). The WAV reader test needs numpy and is skipped without it. Rendering itself needs
the hidden FNA device and is run by the harness, not here.
"""
from __future__ import annotations

import hashlib
import importlib.util
import pathlib
import re
import struct
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
TOOLS = ROOT / 'tools'
FIXTURES = TOOLS / 'fixtures'
CLIENT = ROOT / 'Client/Encounters/CrimsonFoundry'


def load_encoder():
    spec = importlib.util.spec_from_file_location('encode_scarlet_preview', TOOLS / 'encode_scarlet_preview.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding='utf-8')


class SoundTableTests(unittest.TestCase):
    def setUp(self):
        self.encoder = load_encoder()
        self.table = self.encoder.parse_sound_table(read(CLIENT / 'ScarletSounds.cs'))

    def test_every_cue_of_the_production_table_has_its_file(self):
        cues = self.table['cues']
        self.assertEqual(16, len(cues))
        for name, spec in cues.items():
            self.assertTrue((ROOT / 'Assets/Sounds/CrimsonFoundry' / (spec['file'] + '.ogg')).exists(), name)
            self.assertGreater(spec['lease'], spec['ticks'], name)  # a lease never cuts a cue short
        self.assertEqual(1.0, self.table['gain'])

    def test_the_harness_emits_only_cues_the_game_plays(self):
        scene = read(FIXTURES / 'ScarletRigScene.cs')
        emitted = set(re.findall(r'"(Foretell|Impact|CrossflowCharge|CrossflowRelease)"', scene))
        self.assertEqual({'Foretell', 'Impact', 'CrossflowCharge', 'CrossflowRelease'}, emitted)
        self.assertTrue(emitted <= set(self.table['cues']))
        # The game's choice of cue (CrimsonGestureVisuals.PostUpdateEverything) is what the harness mirrors.
        visuals = read(CLIENT / 'CrimsonGestureVisuals.cs')
        self.assertIn('ScarletCue.CrossflowRelease : ScarletCue.CrossflowCharge', visuals)
        self.assertIn('ScarletCue.Impact : ScarletCue.Foretell', visuals)
        self.assertIn('CrimsonSignatureMoves.CurtainBurning(p) == 0) return;', visuals)
        self.assertIn('CrimsonSignatureMoves.CurtainBurning(p) == 0) continue;', scene)

    def test_voices_follow_max_instances(self):
        table = {'gain': 1.0, 'fade': 8, 'cues': {
            'Hit': {'file': 'x', 'ticks': 30, 'instances': 2, 'limit': 'ReplaceOldest', 'lease': 40},
            'Bell': {'file': 'y', 'ticks': 90, 'instances': 1, 'limit': 'IgnoreNew', 'lease': 100}}}
        events = [{'frame': f, 'tick': f, 'cue': 'Hit'} for f in (0, 5, 10)] + [{'frame': 0, 'tick': 0, 'cue': 'Bell'}, {'frame': 20, 'tick': 20, 'cue': 'Bell'}]
        lengths = {'Hit': 30 * 800, 'Bell': 90 * 800}
        voices = self.encoder.schedule(events, table, lengths)
        hits = [v for v in voices if v['cue'] == 'Hit']
        self.assertEqual(3, len(hits))
        self.assertEqual(10 * 800, hits[0]['end'])          # the oldest hit is stopped hard when the third starts
        self.assertEqual(5 * 800 + 30 * 800, hits[1]['end'])
        self.assertEqual(1, len([v for v in voices if v['cue'] == 'Bell']))  # IgnoreNew drops the second bell

    def test_voice_capacity_and_lease(self):
        table = {'gain': 1.0, 'fade': 8, 'cues': {'Hit': {'file': 'x', 'ticks': 30, 'instances': 99, 'limit': 'ReplaceOldest', 'lease': 10}}}
        voices = self.encoder.schedule([{'frame': i, 'tick': i, 'cue': 'Hit'} for i in range(5)], table, {'Hit': 30 * 800}, capacity=3)
        self.assertEqual(3, len(voices))
        self.assertTrue(all(v['end'] - v['start'] == 10 * 800 for v in voices))

    def test_makeup_gain_is_fixed_until_the_ceiling(self):
        self.assertAlmostEqual(10 ** (12 / 20), self.encoder.makeup_gain(.05))
        self.assertAlmostEqual(10 ** (-1 / 20), self.encoder.makeup_gain(1.0) * 1.0)
        self.assertEqual(24, self.encoder.voice_capacity(read(CLIENT / 'CrimsonGestureVisuals.cs')))

    @unittest.skipUnless(importlib.util.find_spec('numpy'), 'numpy is a local preview dependency')
    def test_reads_the_float_wav_the_harness_writes(self):
        samples = [0.25, -0.5, 1.0, 0.0]
        body = struct.pack('<4f', *samples)
        data = (b'RIFF' + struct.pack('<I', 36 + len(body)) + b'WAVE' + b'fmt ' + struct.pack('<IHHIIHH', 16, 3, 2, 48000, 48000 * 8, 8, 32)
                + b'data' + struct.pack('<I', len(body)) + body)
        with tempfile.TemporaryDirectory() as folder:
            path = pathlib.Path(folder) / 'bgm.wav'
            path.write_bytes(data)
            wav = self.encoder.read_wav(path)
        self.assertEqual((2, 2), wav.shape)
        self.assertEqual(samples, wav.reshape(-1).tolist())


class HarnessContractTests(unittest.TestCase):
    def test_production_files_are_linked_not_copied(self):
        script = read(TOOLS / 'preview-scarlet-rigs.ps1')
        for name in ('ScarletApparitionRig', 'CrimsonChoirRig', 'CrimsonChoirMotion', 'CrimsonEnergy', 'ScarletSorcery',
                     'CrimsonRigMotion', 'CrimsonMusicMixer', 'CrimsonMeter', 'CrimsonSignatureMoves'):
            self.assertIn(f"'{name}'", script)
        self.assertIn("'Client/Graphics/WorldGraphicsScope.cs'", script)
        self.assertIn("Client/Encounters/CrimsonFoundry/Vfx", script)
        for fixture in ('ScarletRigScene.cs', 'ScarletRigGates.cs', 'ScarletPreviewHost.cs'):
            text = read(FIXTURES / fixture)
            self.assertNotIn('class ScarletApparitionRig', text)
            self.assertNotIn('class CrimsonChoirRig', text)
            self.assertNotIn('class CrimsonEnergy', text)

    def test_vespera_performer_is_still_extractable(self):
        # preview-scarlet-rigs.ps1 extracts DrawPerformer verbatim (with poses / pivots) from the first of these files that has it.
        sources = [CLIENT / 'CrimsonRig.Performer.cs', CLIENT / 'CrimsonRig.cs']
        text = next(read(p) for p in sources if p.exists() and 'internal static void DrawPerformer(' in read(p))
        self.assertRegex(text, r'static readonly Rectangle\[\] poses\b')
        self.assertRegex(text, r'static readonly Vector2\[\] pivots\b')
        self.assertIn('\nnamespace ', text)

    def test_no_beat_figure_and_no_thread_language_in_the_harness(self):
        for name in ('ScarletRigScene.cs', 'ScarletRigGates.cs', 'ScarletPreviewHost.cs'):
            text = read(FIXTURES / name)
            for banned in ('baton(', 'engagement(', 'Transfusion(', 'DrawSecondary(', 'BeatTick('):
                self.assertNotIn(banned, text, f'{name}: {banned}')
        # The beat pulse reaches only the backdrop, as in CrimsonSky.
        scene = read(FIXTURES / 'ScarletRigScene.cs')
        self.assertEqual(2, scene.count('CrimsonMeter.Pulse('))

    def test_every_frame_says_it_is_offline(self):
        scene = read(FIXTURES / 'ScarletRigScene.cs')
        self.assertIn('OFFLINE RENDER - NOT A PLAYTEST - IN-GAME ACCEPTANCE NOT_RUN', scene)
        self.assertIn('REVIEW SCALE 0.65 - NOT AN IN-GAME ZOOM', scene)
        encoder = read(TOOLS / 'encode_scarlet_preview.py')
        self.assertIn('not_run', encoder)

    def test_pinned_ink_hashes_match_the_contract_tests_and_the_tree(self):
        gates = read(FIXTURES / 'ScarletRigGates.cs')
        pins = dict(re.findall(r'\["(\w+\.fxc?)"\] = "([0-9a-f]{64})"', gates))
        self.assertEqual(14, len(pins))
        contracts = read(TOOLS / 'tests/test_scarlet_contracts.py')
        self.assertIn(pins['ScarletInk.fx'], contracts)
        self.assertIn(pins['ScarletInk.fxc'], contracts)
        shaders = ROOT / 'Assets/AutoloadedEffects/Shaders'
        for name, expected in pins.items():
            data = (shaders / name).read_bytes()
            if name.endswith('.fx'):
                data = data.replace(b'\r\n', b'\n')
            self.assertEqual(expected, hashlib.sha256(data).hexdigest(), name)

    def test_layer_order_is_the_game_order(self):
        scene = read(FIXTURES / 'ScarletRigScene.cs')
        render = scene[scene.index('internal void Render('):scene.index('private void Backdrop(')]
        order = [render.index(token) for token in ('Backdrop(s, view)', 'RigDriver.Apparition(', 'RigDriver.Vespera(',
                                                   'TrackingBeams(s, view, age)', 'Players(s, view)', 'Mask(view)')]
        self.assertEqual(sorted(order), order)
        beams = scene[scene.index('private void TrackingBeams('):scene.index('private void Standins(')]
        order = [beams.index(token) for token in ('CrossflowSeals(', 'residues.Add(', 'CrimsonEnergy.Draw(batch)', 'foreach (var strike in strikes)')]
        self.assertEqual(sorted(order), order)


if __name__ == '__main__':
    unittest.main()
