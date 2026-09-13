"""Export/routing contracts only; subjective audition and runtime mix are separate."""
import array
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
                self.assertLessEqual(peak, .79)
                self.assertGreater(rms, .06)
                self.assertLessEqual(rms, .176)
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

    def test_feedback_routes_every_new_asset_without_changing_shared_masters(self):
        text = (ROOT/'Client/Encounters/FirstSeverance/FirstSeveranceFeedback.cs').read_text(encoding='utf-8-sig')
        routed = set(re.findall(r'=> beamRoot \+ "([^"]+)"',text))
        self.assertEqual({p.stem for p in ASSETS.glob('*.wav')}, routed)
        self.assertNotIn('weaponRoot',text)
        self.assertIn('MaxInstances = 2',text)
        self.assertIn('PrismBeamSustain',text)
        self.assertIn('FirstSeveranceChoreography.BladeEnd);',text)
        self.assertIn('voice.End - tick',text)
        self.assertNotIn('curtainBeat',text)


if __name__ == '__main__': unittest.main()
