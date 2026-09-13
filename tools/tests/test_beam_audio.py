"""Export/routing contracts only; subjective audition and runtime mix are separate."""
import array
import hashlib
import math
from pathlib import Path
import re
import unittest
import wave

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'Assets/Sounds/FirstSeverance/Beams'


class BeamAudioTests(unittest.TestCase):
    def test_owned_exports_are_pcm_with_headroom_and_clean_ends(self):
        paths = sorted(ASSETS.glob('*.wav'))
        self.assertEqual(15, len(paths))
        for path in paths:
            with self.subTest(cue=path.stem), wave.open(str(path), 'rb') as wav:
                self.assertEqual((2, 2, 44100), (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()))
                samples = array.array('h', wav.readframes(wav.getnframes()))
                peak = max(abs(n) for n in samples) / 32768
                rms = math.sqrt(sum(n*n for n in samples)/len(samples))/32768
                self.assertLessEqual(peak, .82)
                self.assertGreater(rms, .06)
                self.assertLessEqual(rms, .281)
                self.assertLess(abs(sum(samples)/len(samples))/32768, .0001)
                if path.stem != 'BeamSustain':
                    self.assertEqual([0,0], list(samples[:2]))
                    self.assertEqual([0,0], list(samples[-2:]))
                else:
                    # Periodic noise needn't meet at a constant value, but its wrap
                    # slope must be no larger than ordinary in-buffer slopes.
                    wrap = max(abs(samples[c]-samples[-2+c]) for c in (0,1))
                    delta = sorted(abs(samples[i]-samples[i-2]) for i in range(2,len(samples)))
                    self.assertLessEqual(wrap,delta[int(len(delta)*.999)])

    def test_release_overlaps_and_original_spread_needle_are_preserved(self):
        needle = ROOT/'Assets/Sounds/FirstSeverance/SpreadExecution.wav'
        self.assertEqual('44484f4970caaa194524c6363aa5aeb96ce2b518058f67dd96769ae26efb4220',
                         hashlib.sha256(needle.read_bytes()).hexdigest())
        rate = 44100
        # Export-only overlap checks at the current per-group feedback gain. No bus
        # normalization/limiting: a future master must fit the actual voices.
        scenarios = [
            [('PortalCharge', i*.7, .98, 28/60) for i in range(8)] +
            [('PortalFire', i*.7+28/60, .96, 1.) for i in range(8)],
            [('BeamSustain', 0., .95, 2.4), ('WideFire', 0., 1.1, .5),
             ('WideFire', 1.25, 1.1, .5)],
            [('WideFire', 0., .95, 50/60), ('WideFire', .2, .70, 38/60)],
            [('SpreadRay', 0., .90, .50), ('SpreadScatter', 0., .68, .46)],
        ]
        for events in scenarios:
            with self.subTest(events=events):
                bus = array.array('f', [0.]) * (rate*7*2)
                for name, start, gain, duration in events:
                    release = .3 if name in ('PortalFire','WideFire') else 0
                    duration += release
                    level = .65 if name in ('SpreadRay','SpreadScatter') else .8 if name=='BeamSustain' else .88
                    with wave.open(str(ASSETS/(name+'.wav')), 'rb') as wav:
                        samples = array.array('h', wav.readframes(round(duration*rate)))
                    offset = round(start*rate)*2
                    for i, sample in enumerate(samples):
                        fade = min(1., max(0., (duration-(i//2)/rate)/(.1+release)))
                        bus[offset+i] += sample/32768*min(1,gain*level)*fade
                self.assertLess(max(abs(n) for n in bus), .99)

    def test_feedback_routes_every_new_asset_without_changing_shared_masters(self):
        text = (ROOT/'Client/Encounters/FirstSeverance/FirstSeveranceFeedback.cs').read_text(encoding='utf-8-sig')
        routed = set(re.findall(r'=> beamRoot \+ "([^"]+)"',text))
        self.assertEqual({p.stem for p in ASSETS.glob('*.wav')}, routed)
        self.assertNotIn('weaponRoot',text)
        self.assertIn('MaxInstances = 2',text)
        self.assertIn('PrismBeamSustain',text)
        self.assertIn('FirstSeveranceChoreography.BladeEnd);',text)
        self.assertIn('FirstSeverancePresentationTiming.VoiceFade(tick, voice.End, voice.Release)',text)
        self.assertNotIn('curtainBeat',text)


if __name__ == '__main__': unittest.main()
