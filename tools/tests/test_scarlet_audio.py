"""Scarlet sound-set wiring and provenance guards, not hearing or in-game mix approval."""
import hashlib
import math
import re
import struct
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / 'Client/Encounters/CrimsonFoundry'
CONTENT = ROOT / 'Content/Encounters/CrimsonFoundry'
SOUNDS = ROOT / 'Assets/Sounds/CrimsonFoundry'
RECORD = '### Scarlet Invocation recorded audio — 2026-10-02'
# The reward companion keeps its own Doll beam sounds; it is outside the Raid's sound set.
COMPANION = 'CrimsonCompanionVisuals.cs'
# The Scarlet reward weapons play their own cues, outside the Raid's sound set (docs/encounters/crimson-foundry/REWARDS.md).
REWARD_AUDIO = 'ScarletRewardAudio.cs'


def read(path):
    return path.read_text(encoding='utf-8')


def scarlet_sources():
    return sorted(list(CLIENT.rglob('*.cs')) + list(CONTENT.rglob('*.cs')))


def specs():
    """(cue enum name, file stem, lease ticks) from ScarletSounds.cs, in declaration order."""
    text = read(CLIENT / 'ScarletSounds.cs')
    enum = re.search(r'internal enum ScarletCue : byte\s*\{([^}]*)\}', text).group(1)
    names = [n.strip() for n in enum.split(',') if n.strip()]
    rows = re.findall(r'new\("(\w+)", (\d+), (\d+), SoundLimitBehavior\.(\w+)\)', text)
    return names, [(stem, int(ticks), int(instances), limit) for stem, ticks, instances, limit in rows]


def vorbis_seconds(path):
    """Length from the last Ogg page's granule position and the Vorbis identification header."""
    data = path.read_bytes()
    rate = struct.unpack_from('<I', data, data.index(b'\x01vorbis') + 12)[0]
    granule = struct.unpack_from('<q', data, data.rindex(b'OggS') + 6)[0]
    return granule / rate


class ScarletAudioContracts(unittest.TestCase):
    def test_every_cue_maps_to_one_existing_file_and_its_lease_covers_the_file(self):
        names, rows = specs()
        self.assertEqual(16, len(names))
        self.assertEqual(len(names), len(rows))
        root = re.search(r'Root = "Convergence/(Assets/Sounds/CrimsonFoundry/)"', read(CLIENT / 'ScarletSounds.cs'))
        self.assertIsNotNone(root)
        self.assertEqual(SOUNDS, ROOT / root.group(1).rstrip('/'))
        for name, (stem, ticks, instances, limit) in zip(names, rows):
            with self.subTest(cue=name):
                # Enum order and table order must agree: each row is the cue's own file.
                self.assertEqual('Scarlet' + name, stem)
                path = SOUNDS / (stem + '.ogg')
                self.assertTrue(path.is_file(), path)
                self.assertEqual(1, len(list(SOUNDS.glob(stem + '.*'))), 'a second extension would make the asset path ambiguous')
                self.assertGreaterEqual(ticks, math.ceil(vorbis_seconds(path) * 60 - 1e-6))
                self.assertIn(limit, ('ReplaceOldest', 'IgnoreNew'))
                self.assertTrue(1 <= instances <= 3)
        shipped = {p.stem for p in SOUNDS.iterdir() if p.is_file()}
        self.assertEqual({stem for stem, *_ in rows}, shipped, 'every shipped Scarlet sound is referenced, nothing else')

    def test_retained_doll_cues_in_scarlet_code_exist(self):
        visuals = read(CLIENT / 'CrimsonVisuals.cs')
        names = re.findall(r'Cue\("(\w+)",', visuals)
        self.assertTrue(names)
        for name in names:
            self.assertTrue((ROOT / f'Assets/Sounds/FirstSeverance/{name}.wav').is_file(), name)
        companion = read(CLIENT / COMPANION)
        for name in re.findall(r'Play\("(\w+)",', companion):
            self.assertTrue((ROOT / f'Assets/Sounds/FirstSeverance/Beams/{name}.wav').is_file(), name)
        # No other Scarlet file builds its own sound path. The reward weapons' player is a set of its own
        # (Assets/Sounds/Weapons/ScarletRewards, guarded by test_scarlet_rewards.py, which forbids it this folder).
        builders = {p.name for p in scarlet_sources() if 'new SoundStyle(' in read(p)}
        self.assertEqual({'ScarletSounds.cs', 'CrimsonVisuals.cs', COMPANION, REWARD_AUDIO}, builders)

    def test_scarlet_code_no_longer_borrows_doll_beam_sounds(self):
        for path in scarlet_sources():
            if path.name == COMPANION:
                continue
            with self.subTest(file=path.name):
                text = read(path)
                self.assertNotIn('FirstSeverance/Beams', text)
                for borrowed in ('"ChargeLock"', '"PortalFire"', '"PortalCharge"', '"WideCharge"', '"WideFire"', '"ChargeRush"',
                                 '"StackRelease"', '"SpreadExecution"', '"SpreadDissolve"', '"StackSummon"', '"SpreadSummon"', 'RaidVictory'):
                    self.assertNotIn(borrowed, text)
        for retired in ('Foretell', 'CrownRupture', 'SilkCleave', 'ThornRend', 'ScarletRelease'):
            self.assertFalse((SOUNDS / f'{retired}.wav').exists(), retired)

    def test_each_moment_is_wired_to_its_cue(self):
        gesture = ' '.join(read(CLIENT / 'CrimsonGestureVisuals.cs').split())
        self.assertIn('bool crossflow = p.Technique is CrimsonTechnique.SideBeams or CrimsonTechnique.ClusterVolley;', gesture)
        # An ordinary note sounds only its strike; a signature move is announced on its first and final steps.
        self.assertIn('bool announced = crossflow || p.IsSignature && (p.Pulse == 0 || p.Pulse == CrimsonChoreography.SignatureClimax);', gesture)
        self.assertIn('if (!impact && !announced) return;', gesture)
        self.assertIn('var cue = crossflow ? impact ? ScarletCue.CrossflowRelease : ScarletCue.CrossflowCharge'
                      ' : impact ? ScarletCue.Impact : ScarletCue.Foretell;', gesture)
        self.assertIn('Cue(p.Born, false); Cue(p.Fire, true);', gesture)
        # One voice per musical event, and no pitch offset on a tuned set.
        self.assertIn('if (voiced.Add((p.Phrase, tick, cue))) voices.Play(cue);', gesture)
        self.assertNotIn('Pitch', gesture)
        chorus = ' '.join(read(CLIENT / 'CrimsonChorusVisuals.cs').split())
        self.assertIn('bool failed = impact && marker.FailedMask != 0;', chorus)
        for cue in ('StackFail', 'StackSuccess', 'StackSummon', 'SpreadFail', 'SpreadSuccess', 'SpreadSummon'):
            self.assertIn('ScarletCue.' + cue, chorus)
        visuals = ' '.join(read(CLIENT / 'CrimsonVisuals.cs').split())
        self.assertIn('Crossed(boss.State.PhaseStart + CrimsonEnsemble.ActRelease)) { voices.Play(ScarletCue.ActChange);', visuals)
        self.assertIn('if (Crossed(flash - ScarletSounds.SacrificeFlashTicks)) voices.Play(ScarletCue.Sacrifice);', visuals)
        # The Victory bell follows the score's own cut (picked from its write head, ahead of the
        # speakers); the grid beat is only the fallback when no score streams for the Fight.
        self.assertIn('CrimsonMeter.BeatTick(CrimsonMeter.BeatAtOrAfter(age - boss.State.MusicStart))', visuals)
        self.assertIn('if (victoryAt >= 0 && CrimsonAudio.VictoryCut(fight) switch { ScoreCut.Heard => true,'
                      ' ScoreCut.Absent => age >= victoryAt, _ => age - endingAt >= VictoryWaitTicks, })'
                      ' { voices.Play(ScarletCue.Victory); victoryAt = -1; }', visuals)
        self.assertEqual(1, visuals.count('voices.Play(ScarletCue.Victory)'))
        audio = ' '.join(read(CLIENT / 'CrimsonAudio.cs').split())
        self.assertIn('mixer.End(state.Stage == CrimsonStage.Victory, CrimsonMusicMixer.NextBeat(cursor));', audio)
        self.assertIn('return self.Audible() + ChunkFrames / 2 >= self.mixer.EndAt ? ScoreCut.Heard : ScoreCut.Pending;', audio)
        for cue in ('Ready', 'Down', 'Revive'):
            self.assertIn(f'if ({cue.lower()}) voices.Play(ScarletCue.{cue});', visuals)
        # Single player shares the runtime's member array, so transitions compare a copy.
        self.assertIn('roster.Clear(); roster.AddRange(state.Members);', visuals)
        sounds = read(CLIENT / 'ScarletSounds.cs')
        self.assertIn('SacrificeFlashTicks = 57', sounds)

    def test_voices_have_an_owner_fade_out_and_never_play_on_a_dedicated_server(self):
        sounds = ' '.join(read(CLIENT / 'ScarletSounds.cs').split())
        self.assertIn('if (Main.dedServ || voices.Count >= capacity || leaseTicks <= 0) return;', sounds)
        self.assertIn('LeaseMargin = FadeTicks + 2', sounds)
        self.assertIn('Volume = Gain', sounds)
        self.assertRegex(sounds, r'FadeTicks = ([6-9]|\d{2,});')
        for name in ('CrimsonVisuals.cs', 'CrimsonGestureVisuals.cs', 'CrimsonChorusVisuals.cs'):
            with self.subTest(file=name):
                text = ' '.join(read(CLIENT / name).split())
                self.assertRegex(text, r'\[Autoload\(Side = ModSide\.Client\)\] internal sealed class \w+ : ModSystem')
                self.assertIn('voices.Update();', text)
                self.assertIn('private void Teardown() { voices.Stop(); Reset(); }', text)
                reset = text[text.index('private void Reset()'):text.index('private void Teardown()')]
                self.assertIn('voices.Release();', reset)
                self.assertNotIn('Stop()', reset)
                for hook in ('OnWorldUnload', 'ClearWorld', 'Unload'):
                    self.assertRegex(text, rf'public override void {hook}\(\) (=> |\{{ )Teardown\(\)')
        chorus = read(CLIENT / 'CrimsonChorusVisuals.cs')
        self.assertNotIn('age + 100', chorus)

    def test_attribution_records_every_file_and_its_sources(self):
        text = read(ROOT / 'Assets/ATTRIBUTION.md')
        self.assertIn(RECORD, text)
        start = text.index(RECORD)
        end = text.index('\n### ', start + len(RECORD))
        record = text[start:end]
        _, rows = specs()
        for stem, *_ in rows:
            with self.subTest(file=stem):
                path = SOUNDS / f'{stem}.ogg'
                entry = f'- Runtime file: `Assets/Sounds/CrimsonFoundry/{stem}.ogg`'
                self.assertIn(entry, record)
                block = record[record.index(entry):] + '\n\n'
                block = block[:block.index('\n\n')]
                self.assertIn(f'- SHA256: `{hashlib.sha256(path.read_bytes()).hexdigest()}`', block)
                self.assertIn('- Source type: public-domain', block)
                keys = re.search(r'- Source work and URL: (.+) in the table above', block).group(1).split(', ')
                for key in keys:
                    self.assertRegex(record, rf'\n\| {re.escape(key)} \| .+ \| `[0-9a-f]{{64}}` \|')
        self.assertEqual(len(rows), record.count('- Runtime file:'))
        # The recipe's source catalog (names, measured pitch, shift table) is hashed with it.
        self.assertRegex(record, r'source catalog `catalog\.json` `[0-9a-f]{64}`')
        # The VSCO readme's credit request includes its homepage link.
        self.assertIn('https://versilian-studios.com/vsco-community/', record)
        for retired in ('CrimsonFoundry/Foretell.wav', 'CrownRupture.wav', 'SilkCleave.wav', 'ThornRend.wav', 'CrimsonFoundry/ScarletRelease.wav'):
            self.assertNotIn(retired, text)


if __name__ == '__main__':
    unittest.main()
