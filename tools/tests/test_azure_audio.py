"""Cathedral SFX v2 export and routing checks; not a subjective listening test."""
from pathlib import Path
import re
import struct
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOUNDS = ROOT / 'Assets/Sounds/AzureCathedral'
TABLE = ROOT / 'Client/Encounters/AzureCathedral/AzureAudio.cs'


def vorbis_info(path):
    """Channels, sample rate and length from the Ogg identification header and last granule position."""
    data = path.read_bytes()
    if data[:4] != b'OggS':
        raise AssertionError(f'{path.name}: not an Ogg stream')
    packet = data[27 + data[26]:]
    if packet[:7] != b'\x01vorbis':
        raise AssertionError(f'{path.name}: first packet is not a Vorbis identification header')
    channels, rate = packet[11], struct.unpack('<I', packet[12:16])[0]
    last = data.rfind(b'OggS')
    granule = struct.unpack('<q', data[last + 6:last + 14])[0]
    return channels, rate, granule / rate


def routed_files():
    """Files the AzureAudio cue table plays: Add(AzureCue.X, "AzureCathedral/Name", variants, split, ...)."""
    rows = re.findall(r'Add\(AzureCue\.\w+,\s*"AzureCathedral/(\w+)",\s*(\d+),\s*(\d+),', TABLE.read_text(encoding='utf-8-sig'))
    files = set()
    for name, variants, split in rows:
        count = max(int(variants), int(split))
        files |= {f'{name}{i}' for i in range(1, count + 1)} if count else {name}
    return files


class AzureAudioTests(unittest.TestCase):
    def test_v2_exports_are_stereo_vorbis_cues(self):
        self.assertEqual([], sorted(p.name for p in SOUNDS.glob('*.wav')), 'retired NumPy WAV cues must stay removed')
        clips = sorted(SOUNDS.glob('*.ogg'))
        self.assertEqual(53, len(clips))
        for clip in clips:
            with self.subTest(cue=clip.stem):
                channels, rate, seconds = vorbis_info(clip)
                self.assertEqual((2, 44100), (channels, rate))
                self.assertGreater(seconds, 0.15)
                self.assertLess(seconds, 5.2)

    def test_cue_table_plays_exactly_the_exports(self):
        self.assertEqual({p.stem for p in SOUNDS.glob('*.ogg')}, routed_files())


if __name__ == '__main__':
    unittest.main()
