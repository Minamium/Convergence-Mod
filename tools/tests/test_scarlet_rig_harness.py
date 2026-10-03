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
        for name in ('ScarletApparitionRig', 'CrimsonChoirRig', 'CrimsonChoirMotion', 'CrimsonRig.Performer', 'CrimsonEnergy',
                     'ScarletSorcery', 'CrimsonRigMotion', 'CrimsonMusicMixer', 'CrimsonMeter', 'CrimsonSignatureMoves'):
            self.assertIn(f"'{name}'", script)
        self.assertIn("'Client/Graphics/WorldGraphicsScope.cs'", script)
        self.assertIn("Client/Encounters/CrimsonFoundry/Vfx", script)
        for fixture in ('ScarletRigScene.cs', 'ScarletRigGates.cs', 'ScarletPreviewHost.cs'):
            text = read(FIXTURES / fixture)
            self.assertNotIn('class ScarletApparitionRig', text)
            self.assertNotIn('class CrimsonChoirRig', text)
            self.assertNotIn('class CrimsonEnergy', text)
            self.assertNotIn('void DrawPerformer(', text)

    def test_vespera_performer_is_linked_unchanged(self):
        # CrimsonRig.Performer.cs (DrawPerformer with poses / pivots, the plan-list Signal) is linked, never extracted;
        # the harness's part of the partial class only calls the production loader. The boss half stays unlinked.
        script = read(TOOLS / 'preview-scarlet-rigs.ps1')
        self.assertIn("'CrimsonRig.Performer'", script)
        self.assertIn("'Client/Encounters/CrimsonFoundry/CrimsonRig.Performer.cs'", script)
        self.assertNotIn("'CrimsonRig',", script)
        self.assertNotIn('CrimsonRigPerformerExtract.g.cs" />', script)
        performer = read(CLIENT / 'CrimsonRig.Performer.cs')
        for member in ('internal static void DrawPerformer(', 'static readonly Rectangle[] poses', 'static readonly Vector2[] pivots',
                       'private static void LoadPerformer()'):
            self.assertIn(member, performer)
            self.assertIn(member, script)  # the build refuses a performer file without them
        self.assertIn('ReadOnlySpan<CrimsonGesturePlan> gestures', performer)
        host = read(FIXTURES / 'ScarletPreviewHost.cs')
        part = host[host.index('internal static partial class CrimsonRig'):host.index('internal static class CrimsonVisuals')]
        self.assertEqual(1, part.count('=>'))
        self.assertIn('internal static void LoadPreviewPerformer() => LoadPerformer();', part)
        for fixture in ('ScarletRigScene.cs', 'ScarletRigGates.cs', 'ScarletPreviewHost.cs'):
            self.assertNotIn('CrimsonRigPerformerExtract', read(FIXTURES / fixture))

    def test_driver_calls_the_production_plan_list_functions(self):
        # The signal, the Choir cues and the notes are S1's plan-list functions over the live gestures, wired as
        # CrimsonRig.DrawEffigy wires them; no harness copy of their derivation remains.
        scene = read(FIXTURES / 'ScarletRigScene.cs')
        mirror = scene[scene.index('internal static class RigMirror'):scene.index('internal static class RigDriver')]
        self.assertIn('=> CrimsonRig.Signal(Live(plans, age), ReadOnlySpan<CrimsonChorusPlan>.Empty, source, age);', mirror)
        self.assertIn('=> ScarletNotes.ChoirCues(Live(plans, age), age, flipped, cues);', mirror)
        # The body's notes read ScarletCueFrame.Known (the plans whose aim is known), as DrawEffigy does; the signal and the cues read every plan.
        self.assertIn('=> ScarletNotes.Collect(Known(plans, age), source, age, flipped, RigScene.Conductor.X, RigScene.Conductor.Y, notes, lookback);', mirror)
        self.assertIn('if (Alive(p, age) && ScarletNotes.AimKnown(p, age >= p.Born)) known.Add(p);', mirror)
        for copied in ('CrimsonRigMotion.Charge(', 'CrimsonRigMotion.Recoil(', 'new CrimsonChoirCue(', 'Step % 4'):
            self.assertNotIn(copied, mirror)
        driver = scene[scene.index('internal static class RigDriver'):scene.index('internal enum RigLayers')]
        rig = read(CLIENT / 'CrimsonRig.cs')
        effigy = rig[rig.index('internal static bool DrawEffigy('):rig.index('internal static int ChoirCues(')]
        for call in ('ScarletGestureMotion.Crown(age, notes[..noted]) : ScarletGestureMotion.Mantle(age, notes[..noted])',
                     'ScarletBodyMaterial.Choir(age, notes[..noted], reduced)', 'ScarletGestureMotion.Heave'):
            self.assertIn(call, effigy)
            self.assertIn(call, driver)
        self.assertIn('ScarletBodyMaterial.Apparition(age, notes[..noted], motion, flipped, reduced)', effigy)
        self.assertIn('ScarletBodyMaterial.Apparition(age, notes[..noted], motion, s.Flipped, reduced)', driver)
        self.assertIn('CrimsonRig.DrawConductor(', driver)
        self.assertIn('index == 2 ? 0 : ScarletNotes.PastTicks', driver)
        self.assertIn('effigy.State.Index == 2 ? 0 : ScarletNotes.PastTicks', effigy)

    def test_gates_cannot_hide_behind_each_other(self):
        gates = read(FIXTURES / 'ScarletRigGates.cs')
        # G2's safe zones include every displayed footprint (basic phrases too), and its second part reads the
        # composited light over safe ground against the current silhouette, independent of G1's range.
        zones = gates[gates.index('private bool[] SafeZones('):gates.index('private bool[] Capsules(')]
        self.assertNotIn('IsSignature', zones)
        self.assertIn('var silhouette = BodyMask(s, view, Plain, 4);', gates)
        self.assertIn('Status(inZones == 0 && hands.Outside == 0 && seenOk)', gates)
        # G3 gates every pass alone as well as the pooled percentile.
        self.assertIn('outPass <= limitOutside && inPass <= limitInside', gates)
        # G5 is local: 100 px tiles near the body on every warning tick, against the plain picture.
        g5 = gates[gates.index('private void G5()'):gates.index('private void G6()')]
        self.assertIn('const int Tile = 100, MinimumPixels = 150;', g5)
        self.assertIn('for (int tick = third.Born; tick < third.Fire; tick++)', g5)
        self.assertIn('worst >= .9f', g5)

    def test_baselines_are_explicit_and_carry_provenance(self):
        gates = read(FIXTURES / 'ScarletRigGates.cs')
        conductor = read(FIXTURES / 'ScarletConductorGates.cs')
        script = read(TOOLS / 'preview-scarlet-rigs.ps1')
        self.assertIn('[switch]$WriteBaseline', script)
        self.assertIn("if ($WriteBaseline) { $options += @('--write-baseline', 'on') }", script)
        self.assertIn('internal static class RigBaseline', gates)
        self.assertIn('rev-parse HEAD', gates)
        for text, gate in ((gates, '"g8"'), (conductor, '"s4"')):
            self.assertIn(f'RigBaseline.Refuse(root, dir, {gate}, out manifest)', text)
            self.assertIn(f'RigBaseline.Write(root, dir, {gate},', text)
        # G11 and the Vespera half of G8v compare against references that live in the harness, so they hold under any phrase
        # timing: main's ScarletInkStroke (verbatim, renamed) and the pre-S4 boss-path drawing (TodayVespera).
        reference = read(FIXTURES / 'ScarletInkReference.cs')
        self.assertIn('internal sealed class ScarletInkStrokeReference', reference)
        self.assertIn('new ScarletInkStrokeReference().Draw(view, r.Assets, p);', gates)
        self.assertIn('production.AsSpan().SequenceEqual(reference)', gates)
        self.assertIn("'tools/fixtures/ScarletInkReference.cs'", script)
        self.assertIn('private Color[] TodayVespera(', conductor)
        self.assertIn('compare ? Hash(TodayVespera(s, tick, reduced)) : null', conductor)
        # Nothing is written just because a baseline is missing.
        self.assertNotIn('Directory.EnumerateFiles(dir, "*.rgba.gz").Any()', gates)
        self.assertNotIn('bool compare = File.Exists(file);', conductor)

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
        # The crossflow's seals lie under the forecast and the ink for their whole life (owner 2026-10-04), as in the game.
        self.assertIn('if (p.Technique == CrimsonTechnique.SideBeams && age < p.End) ScarletSorcery.CrossflowSeals(batch, p, age);', beams)
        self.assertNotIn('sealsOver', beams)

    def test_g11_compares_the_tracking_beams_and_measures_the_crossflow_band(self):
        gates = read(FIXTURES / 'ScarletRigGates.cs')
        g11 = gates[gates.index('private void G11()'):gates.index('// ---- G12')]
        self.assertIn('q.Technique == CrimsonTechnique.TrackingBeam', g11)
        self.assertIn('new ScarletInkStrokeReference().Draw(view, r.Assets, p);', gates)
        crossflow = g11[g11.index('private List<object> Crossflow('):]
        for token in ('"wallL"', '"wallR"', 'outsideAlong', 'outsideAcross', 'columnAtRightCut', 'columnAtLeftCut', 'atRight >= .85f && atLeft >= .85f', 'sha256'):
            self.assertIn(token, crossflow)
        self.assertIn('Status(hashOk && frameOk && crossflowOk)', g11)

    def test_g6v_reports_the_rests_between_phrases_and_gates_only_blinks(self):
        gates = read(FIXTURES / 'ScarletConductorGates.cs')
        self.assertIn('int blink = dipsNow.Count(d => d.Length <= 8)', gates)
        self.assertIn('steady &= blink == 0;', gates)
        for token in ('rests = rests.Count', 'restTicks', 'restIdleTicks', 'longerDips'):
            self.assertIn(token, gates)


if __name__ == '__main__':
    unittest.main()
