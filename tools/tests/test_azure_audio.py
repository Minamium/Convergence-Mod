"""Shipped ice/glass PCM and routing checks; not a subjective listening test."""
import array
import math
from pathlib import Path
import re
import unittest
import wave

ROOT = Path(__file__).resolve().parents[2]


class AzureAudioTests(unittest.TestCase):
    def test_seven_owned_exports_have_headroom_and_clean_ends(self):
        clips = sorted((ROOT / 'Assets/Sounds/AzureCathedral').glob('*.wav'))
        self.assertEqual(7, len(clips))
        for clip in clips:
            with self.subTest(cue=clip.stem), wave.open(str(clip), 'rb') as wav:
                self.assertEqual((2, 2, 44100), (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()))
                self.assertLessEqual(wav.getnframes() / wav.getframerate(), 2.65)
                pcm = array.array('h', wav.readframes(wav.getnframes()))
                self.assertLess(max(abs(v) for v in pcm) / 32768, .8)
                self.assertLess(abs(sum(pcm) / len(pcm)) / 32768, .0001)
                self.assertGreater(math.sqrt(sum(v*v for v in pcm) / len(pcm)) / 32768, .05)
                self.assertEqual([0, 0], list(pcm[:2]))
                self.assertEqual([0, 0], list(pcm[-2:]))

    def test_every_new_cue_has_an_encounter_route(self):
        source = (ROOT / 'Client/Encounters/AzureCathedral/AzureVisuals.cs').read_text(encoding='utf-8-sig')
        routed = set(re.findall(r'PlayAzure\("([^"]+)"', source))
        self.assertEqual({p.stem for p in (ROOT / 'Assets/Sounds/AzureCathedral').glob('*.wav')}, routed)


if __name__ == '__main__':
    unittest.main()
