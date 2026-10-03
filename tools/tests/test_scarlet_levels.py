"""Scarlet loudness against Graceful Ordeal (ENCOUNTER_SPEC.md#sound-effects, REWARDS.md#levels-against-the-raid).

Every level here is ITU-R BS.1770-4 loudness (the standard's 48 kHz K-weighting run along time, both channels summed):
for a cue, the maximum 400 ms momentary loudness of the shipped file, padded 0.2 s before and 0.5 s after and stepped
10 ms; for the score, the median 3 s short-term loudness (100 ms steps) over the four stages as the client arranges them.
Measured 2026-10-03 (docs/evidence/2026-10-03-scarlet-loudness.json). The recipes' own meter (kit.loudness_stats, which
ran its K-weighting across the two channels) is a documented legacy number only and is not held here.

The tests hold relations, not levels: each Raid group's place against the score at the owner's sliders, each reward
role's place against the Raid cue it answers, and the balance the owner auditioned inside every group. The tables pin
the measured files by digest; a local-only test re-measures them and the score. Not hearing approval: the owner's
in-game listening is not_run.
"""
from pathlib import Path
import hashlib
import importlib.util
import math
import re
import statistics
import unittest

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / 'Client/Encounters/CrimsonFoundry'
CONTENT = ROOT / 'Content/Encounters/CrimsonFoundry'
RAID = ROOT / 'Assets/Sounds/CrimsonFoundry'
SOUNDS = ROOT / 'Assets/Sounds/Weapons/ScarletRewards'
BGM = ROOT / 'Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg'
CUES = CLIENT / 'Rewards/ScarletRewardCues.cs'

# The owner's sliders when the levels were set (tModLoader config.json, 2026-10-02): the reference listening position.
OWNER_MUSIC, OWNER_SOUND = 0.68503934, 0.16071428
# Graceful Ordeal's median short-term loudness over the four stages (Act I after the opening, Acts II/III and Final from
# the bar after their Continue bar, each with one lap of its body), the file at gain 1.
BGM_REFERENCE = -12.26
BGM_SHA256 = '1a00366b16c485fd7f921100fe237d19ac59e689471ed6fb966b605d36fa4519'

RAID_LEVELS = {
    'ScarletActChange': -12.18, 'ScarletCrossflowCharge': -20.19, 'ScarletCrossflowRelease': -12.45,
    'ScarletDown': -18.31, 'ScarletForetell': -19.32, 'ScarletImpact': -15.39, 'ScarletReady': -24.92,
    'ScarletRevive': -16.77, 'ScarletSacrifice': -14.45, 'ScarletSpreadFail': -12.65, 'ScarletSpreadSuccess': -17.14,
    'ScarletSpreadSummon': -15.96, 'ScarletStackFail': -14.02, 'ScarletStackSuccess': -16.68,
    'ScarletStackSummon': -17.74, 'ScarletVictory': -10.32,
}
RAID_LEVELS_SHA256 = '6e4c8c4591496530bda25a004f1e49a4bf6cecb662e8789716cd70a9bdeaf882'
REWARD_LEVELS = {
    'BatonLift': -18.49, 'BatonStroke': -19.50, 'Cadence': -14.36, 'CenserBrace': -15.35, 'CenserGrandPour': -14.17,
    'CenserPour': -19.48, 'CenserSummon': -18.52, 'CenserSwing': -16.02, 'ChoirClasp': -12.24, 'HandSlam': -16.91,
    'HymnInhale': -16.81, 'InkBlaze': -14.21, 'InkIgnite': -17.55, 'OrganShot1': -16.35, 'OrganShot2': -16.55,
    'OrganShot3': -16.16, 'OrganShot4': -16.17, 'QuillStick': -16.97, 'QuillThrow': -19.07, 'ReliquaryOpen': -12.30,
    'RiverRelease': -13.33, 'ScoreChord': -12.68, 'ScoreUnseal': -18.55, 'ScytheSwingHigh': -19.37,
    'ScytheSwingLow': -19.50, 'ScytheWhip': -17.99, 'ScytheWhipBrace': -18.02, 'StaffBarline': -11.94,
    'StaffCut': -15.66, 'StaffWindup': -17.57, 'Toll0': -21.39, 'Toll1': -21.27, 'Toll2': -21.44, 'Toll3': -20.81,
    'Toll4': -20.18, 'Toll5': -20.96, 'Toll6': -20.79, 'Toll7': -20.73,
}
REWARD_LEVELS_SHA256 = '763707660de9e78e275eb7790d4c5eec22aed1d1148ece08d955306277fc234e'
# The same meter on the files the owner auditioned and picked (0.3.77 / 0.3.78), before the 2026-10-03 lift: the
# balance inside each group is held against these.
AUDITIONED = {
    'ScarletActChange': -13.69, 'ScarletCrossflowCharge': -21.70, 'ScarletCrossflowRelease': -13.96,
    'ScarletDown': -20.46, 'ScarletForetell': -25.69, 'ScarletImpact': -20.31, 'ScarletReady': -27.09,
    'ScarletRevive': -18.93, 'ScarletSacrifice': -15.88, 'ScarletSpreadFail': -14.27, 'ScarletSpreadSuccess': -18.68,
    'ScarletSpreadSummon': -17.49, 'ScarletStackFail': -15.55, 'ScarletStackSuccess': -18.22,
    'ScarletStackSummon': -19.27, 'ScarletVictory': -11.82,
    'BatonLift': -18.49, 'BatonStroke': -20.35, 'Cadence': -14.36, 'CenserBrace': -15.35, 'CenserGrandPour': -14.17,
    'CenserPour': -21.02, 'CenserSummon': -19.36, 'CenserSwing': -16.87, 'ChoirClasp': -12.24, 'HandSlam': -17.84,
    'HymnInhale': -16.81, 'InkBlaze': -15.78, 'InkIgnite': -19.09, 'OrganShot1': -17.25, 'OrganShot2': -17.51,
    'OrganShot3': -17.05, 'OrganShot4': -16.98, 'QuillStick': -17.44, 'QuillThrow': -19.97, 'ReliquaryOpen': -16.15,
    'RiverRelease': -13.33, 'ScoreChord': -12.68, 'ScoreUnseal': -18.55, 'ScytheSwingHigh': -20.27,
    'ScytheSwingLow': -20.35, 'ScytheWhip': -18.87, 'ScytheWhipBrace': -18.02, 'StaffBarline': -11.94,
    'StaffCut': -17.11, 'StaffWindup': -17.57, 'Toll0': -22.25, 'Toll1': -22.16, 'Toll2': -22.30, 'Toll3': -21.68,
    'Toll4': -21.05, 'Toll5': -21.84, 'Toll6': -21.65, 'Toll7': -21.60,
}
# Raid groups (ENCOUNTER_SPEC.md#sound-effects) and where each sits against the score at the owner's sliders, dB
# (the group's median, as played): (low, high).
RAID_GROUPS = {
    'impact': ({'ScarletImpact'}, (-2, 0)),
    'foretell': ({'ScarletForetell'}, (-6, -4)),
    'big': ({'ScarletCrossflowRelease', 'ScarletActChange', 'ScarletSacrifice', 'ScarletVictory'}, (1, 3)),
    'chorus': ({'ScarletStackSummon', 'ScarletStackSuccess', 'ScarletStackFail', 'ScarletSpreadSummon',
                'ScarletSpreadSuccess', 'ScarletSpreadFail'}, (-3, -1)),
    'status': ({'ScarletDown', 'ScarletRevive', 'ScarletReady'}, (-5, -3)),
    'charge': ({'ScarletCrossflowCharge'}, None),  # rides with the release it swells into
}
# Reward roles (REWARDS.md#levels-against-the-raid).
ROLES = {
    'Build': {f'Toll{k}' for k in range(8)},
    'Shot': {'ScytheSwingHigh', 'ScytheSwingLow', 'ScytheWhip', 'OrganShot', 'BatonStroke', 'CenserSummon', 'CenserSwing',
             'QuillThrow', 'QuillStick'},
    'Windup': {'ScytheWhipBrace', 'StaffWindup', 'HymnInhale', 'BatonLift', 'CenserBrace', 'ScoreUnseal'},
    'Release': {'StaffCut', 'HandSlam', 'InkIgnite', 'CenserPour', 'InkBlaze'},
    'Finale': {'StaffBarline', 'ChoirClasp', 'RiverRelease', 'CenserGrandPour', 'ScoreChord', 'Cadence'},
    'Show': {'ReliquaryOpen'},
}
# The most a file may fall short of its group's common lift (the limiter's 3 dB cap on a transient file), and overshoot it.
BALANCE = (-0.7, 0.15)


def read(path):
    return path.read_text(encoding='utf-8')


def db(gain):
    return 20 * math.log10(gain)


def tml_music_db(volume):
    """tModLoader's music slider (ASoundEffectBasedAudioTrack.ReMapVolumeToMatchXact), dB."""
    return 31.0 * volume - 25.0 - 11.94


def music_gain():
    return float(re.search(r'MusicGain\(double scoreAge\) => ([\d.]+)f \* Ease', read(CONTENT / 'CrimsonInvocation.cs')).group(1))


def bgm_in_cue_units(music, sound):
    """Where the score's reference sits on the cue files' scale, both as heard at these sliders: the score streams at
    MusicGain times the music curve, cues at the sound slider (linear, as tML's ActiveSound sets them)."""
    return BGM_REFERENCE + db(music_gain()) + (tml_music_db(music) - tml_music_db(1)) - db(sound)


def files_digest(folder, stems):
    digest = hashlib.sha256()
    for stem in sorted(stems):
        digest.update(f'{stem}.ogg:{hashlib.sha256((folder / f"{stem}.ogg").read_bytes()).hexdigest()}\n'.encode())
    return digest.hexdigest()


def raid_gain_db():
    return db(float(re.search(r'internal const float Gain = ([\d.]+)f;', read(CLIENT / 'ScarletSounds.cs')).group(1)))


def role_offsets():
    """ScarletRewardCues.<Role>Decibels, in dB."""
    text = read(CUES)
    return {role: float(re.search(rf'\b{role}Decibels = (-?[\d.]+)f', text).group(1)) for role in ROLES}


def reward_files():
    """Runtime file stem -> role, from ScarletRewardCues.All."""
    text = read(CUES)
    body = text[text.index('internal static readonly ScarletCue[] All'):]
    body = body[:body.index('};')]
    out = {}
    for quoted, named, role, files in re.findall(r"new\((?:\"(\w+)\"|(\w+)), '[AB]', \w+, (\w+), \d+, [\w.]+, [\d.]+f(?:, Files: (\d+))?\)", body):
        name, role, files = quoted or named, 'Shot' if role == 'OneShot' else role, int(files or 1)
        for i in range(files):
            out[name if files == 1 else f'{name}{i + 1}'] = (name, role)
    return out


def played():
    """File stem -> its level as played (the file plus the Raid gain or its reward role's offset)."""
    raid, offsets = raid_gain_db(), role_offsets()
    out = {stem: level + raid for stem, level in RAID_LEVELS.items()}
    for stem, (_, role) in reward_files().items():
        out[stem] = REWARD_LEVELS[stem] + offsets[role]
    return out


def numeric_audio_stack():
    return all(importlib.util.find_spec(name) for name in ('numpy', 'scipy', 'soundfile'))


class ScarletLevels(unittest.TestCase):
    def test_the_score_follows_tmls_music_slider(self):
        # The Raid streams its own score, so it applies tML's music curve itself (normalised at a full slider, where
        # the calibrated MusicGain holds); a linear slider left it 6.5 dB too loud at the owner's 0.685.
        audio = read(CLIENT / 'CrimsonAudio.cs')
        self.assertEqual(2, audio.count('voice.Volume = gain * CrimsonInvocation.MusicSlider(Main.musicVolume);'))
        self.assertNotRegex(audio, r'\*\s*Main\.musicVolume\s*;', 'no linear slider path is left')
        invocation = ' '.join(read(CONTENT / 'CrimsonInvocation.cs').split())
        self.assertIn('internal static float MusicSlider(float volume) => volume <= 0 ? 0 : MathF.Pow(10, 31 * (Math.Min(volume, 1) - 1) / 20);',
                      invocation)
        for v in (0.05, 0.25, 0.5, OWNER_MUSIC, 0.9, 1.0):
            ours = db(10 ** (31 * (min(v, 1) - 1) / 20))
            self.assertAlmostEqual(tml_music_db(v) - tml_music_db(1), ours, places=9, msg=f'slider {v}')
        self.assertAlmostEqual(-6.47, (tml_music_db(OWNER_MUSIC) - tml_music_db(1)) - db(OWNER_MUSIC), delta=.01,
                               msg="the owner's slider moves the score 6.5 dB against the linear path")

    def test_the_level_tables_measure_the_shipped_files(self):
        self.assertEqual(sorted(RAID_LEVELS), sorted(p.stem for p in RAID.glob('*.ogg')), 'every Raid cue is measured')
        self.assertEqual(sorted(REWARD_LEVELS), sorted(reward_files()), 'every reward file is measured')
        self.assertEqual(sorted(AUDITIONED), sorted({**RAID_LEVELS, **REWARD_LEVELS}))
        message = 'a measured file changed: re-measure it and re-check the relations (REWARDS.md#levels-against-the-raid)'
        self.assertEqual(RAID_LEVELS_SHA256, files_digest(RAID, RAID_LEVELS), message)
        self.assertEqual(REWARD_LEVELS_SHA256, files_digest(SOUNDS, REWARD_LEVELS), message)
        self.assertEqual(BGM_SHA256, hashlib.sha256(BGM.read_bytes()).hexdigest(), 'the score changed: re-measure BGM_REFERENCE')

    def test_raid_groups_sit_at_their_place_against_the_score(self):
        level = played()
        reference = bgm_in_cue_units(OWNER_MUSIC, OWNER_SOUND)
        self.assertAlmostEqual(-14.32, reference, delta=.01)
        for group, (members, window) in RAID_GROUPS.items():
            if window is None:
                continue
            with self.subTest(group=group):
                at = statistics.median(level[m] - reference for m in members)
                self.assertGreaterEqual(at, window[0])
                self.assertLessEqual(at, window[1])
        self.assertEqual(set(RAID_LEVELS), set().union(*(m for m, _ in RAID_GROUPS.values())), 'every Raid cue has a group')
        self.assertLessEqual(max(level[m] for m in RAID_LEVELS) - reference, 5, 'no Raid cue stands more than 5 dB over the score')
        # Impact reads over the warning that precedes it, and the charge swells into a louder release.
        self.assertGreater(level['ScarletImpact'], level['ScarletForetell'] + 2)
        self.assertGreater(level['ScarletCrossflowRelease'], level['ScarletCrossflowCharge'] + 5)

    def test_reward_roles_sit_against_the_raid_cues_they_answer(self):
        level = played()
        files = reward_files()
        self.assertEqual(ROLES, {role: {name for name, r in files.values() if r == role} for role in ROLES}, 'role groups')
        by_role = {role: [level[stem] for stem, (_, r) in files.items() if r == role] for role in ROLES}
        med = {role: statistics.median(v) for role, v in by_role.items()}
        impact, foretell = level['ScarletImpact'], level['ScarletForetell']
        release, victory = level['ScarletCrossflowRelease'], level['ScarletVictory']
        self.assertTrue(-3 <= med['Shot'] - impact <= -1, 'one-shots about 2 dB under the Raid impact')
        self.assertLess(max(by_role['Shot']), impact, 'no one-shot over the Raid impact')
        self.assertLess(max(by_role['Build']), min(by_role['Shot']), 'tolls under every one-shot')
        self.assertTrue(-1 <= med['Windup'] - foretell <= 1, 'windups and braces about the Raid foretell')
        self.assertTrue(-2 <= med['Release'] - impact <= 0, 'cascade parts about 1 dB under the Raid impact')
        self.assertTrue(-2 <= med['Finale'] - release <= 0, 'finales about 1 dB under the crossflow release')
        self.assertLessEqual(max(by_role['Finale']), release + .5, 'no finale over the crossflow release')
        self.assertTrue(-3 <= level['ReliquaryOpen'] - victory <= -1, 'the reliquary show about 2 dB under the Raid Victory')
        # Offsets never boost a file past its level: a role that had to rise was re-rendered with the lift baked in.
        for role, offset in role_offsets().items():
            self.assertLessEqual(offset, 0, role)
        rules = read(ROOT / 'Content/Encounters/CrimsonFoundry/Rewards/CrimsonRewardRules.cs')
        self.assertLessEqual(float(re.search(r'RemoteShotDecibels = (-?\d+)', rules).group(1)), 0)
        for name in ('ScoreThrowDecibels', 'PartialScoreDecibels'):
            self.assertLessEqual(float(re.search(rf'{name} = (-?[\d.]+)', read(CUES)).group(1)), 0, name)

    def test_each_group_keeps_the_balance_the_owner_auditioned(self):
        files = reward_files()
        groups = {f'raid {g}': members for g, (members, _) in RAID_GROUPS.items()}
        groups.update({f'reward {role}': {stem for stem, (_, r) in files.items() if r == role} for role in ROLES})
        offsets = role_offsets()
        # One offset per group, so inside a group only the file's own change can move the balance.
        moved = {stem: level - AUDITIONED[stem] for stem, level in {**RAID_LEVELS, **REWARD_LEVELS}.items()}
        for name, members in groups.items():
            common = statistics.median(moved[m] for m in members)
            for m in members:
                with self.subTest(group=name, file=m):
                    self.assertGreaterEqual(moved[m] - common, BALANCE[0])
                    self.assertLessEqual(moved[m] - common, BALANCE[1])
        # The charge keeps its auditioned step under the release it swells into.
        self.assertAlmostEqual(moved['ScarletCrossflowCharge'], moved['ScarletCrossflowRelease'], delta=.1)
        # Windups and finales keep their files; only their offsets moved.
        for stem, (_, role) in files.items():
            if role in ('Windup', 'Finale'):
                self.assertEqual(AUDITIONED[stem], REWARD_LEVELS[stem], stem)
        self.assertEqual(offsets['Shot'], offsets['Build'], 'tolls keep their auditioned step under the one-shots')

    @unittest.skipUnless(numeric_audio_stack(), 'numpy, scipy and soundfile are local audio tools, not CI')
    def test_the_tables_match_a_fresh_measurement(self):
        import numpy as np
        import soundfile as sf
        from scipy import signal

        rate = 48000
        itu = [((1.53512485958697, -2.69169618940638, 1.19839281085285), (1.0, -1.69065929318241, 0.73248077421585)),
               ((1.0, -2.0, 1.0), (1.0, -1.99004745483398, 0.99007225036621))]

        def power(x):
            for b, a in itu:
                x = signal.lfilter(b, a, x, axis=0)
            return np.concatenate([[0.0], np.cumsum(np.sum(x ** 2, axis=1))])

        def windows(energy, win_s, hop_s):
            win, hop = round(win_s * rate), round(hop_s * rate)
            starts = np.arange(0, len(energy) - win, hop)
            return -0.691 + 10 * np.log10(np.maximum((energy[starts + win] - energy[starts]) / win, 1e-12))

        def m_max(x):
            x = np.concatenate([np.zeros((round(.2 * rate), 2)), x, np.zeros((round(.5 * rate), 2))])
            return float(windows(power(x), .4, .01).max())

        for folder, levels in ((RAID, RAID_LEVELS), (SOUNDS, REWARD_LEVELS)):
            for stem, expected in levels.items():
                with self.subTest(file=stem):
                    x, sr = sf.read(str(folder / f'{stem}.ogg'), always_2d=True, dtype='float64')
                    self.assertEqual((rate, 2), (sr, x.shape[1]))
                    self.assertAlmostEqual(expected, m_max(x), delta=.02)

        # The score's reference, rendered as CrimsonMusicMixer arranges it (whole bars, the equal-power blend into a
        # non-consecutive bar, Final's swelling riser).
        meter = read(CONTENT / 'CrimsonMeter.cs')
        arrangement = meter[meter.index('class CrimsonArrangement'):]

        def table(name):
            body = arrangement[arrangement.index(f'{name} ='):]
            body = body[:body.index('};')]
            return [[-1 if v == 'Continue' else int(v) for v in re.findall(r'Continue|\d+', row)] for row in re.findall(r'\{([^{}]*)\}', body)]

        entries, bodies = table('Entries'), table('Bodies')
        opening = int(re.search(r'OpeningBars = (\d+)', meter).group(1))
        mixer = read(CLIENT / 'CrimsonMusicMixer.cs')
        fade = int(re.search(r'FadeFrames = (\d+)', mixer).group(1))
        self.assertIn('float g = .12f + .88f * MathF.Pow(within / (CrimsonMeter.BeatSamples * 2f), 1.6f);', mixer)
        song, sr = sf.read(str(BGM), always_2d=True, dtype='float64')
        self.assertEqual(rate, sr)
        bar = 90000
        pooled = []
        for stage in range(4):
            seq = (entries[0][opening:] if stage == 0 else entries[stage][1:]) + bodies[stage]
            prev = entries[0][opening - 1] if stage == 0 else None
            out = np.zeros((len(seq) * bar, 2))
            for k, sb in enumerate(seq):
                seg = song[sb * bar:(sb + 1) * bar].copy()
                if prev is not None and prev + 1 != sb:
                    t = (np.arange(fade) + .5) / fade
                    seg[:fade] = seg[:fade] * np.sin(t * np.pi / 2)[:, None] + song[(prev + 1) * bar:(prev + 1) * bar + fade] * np.cos(t * np.pi / 2)[:, None]
                if stage == 3 and k == 1:  # CrimsonArrangement.Swells(3, 2): the bar after the Continue bar
                    w = np.arange(2 * 22500) / (2 * 22500.0)
                    seg[:2 * 22500] *= (.12 + .88 * w ** 1.6)[:, None]
                out[k * bar:(k + 1) * bar] = seg
                prev = sb
            pooled.append(windows(power(out), 3.0, .1))
        self.assertAlmostEqual(BGM_REFERENCE, float(np.median(np.concatenate(pooled))), delta=.02)


if __name__ == '__main__':
    unittest.main()
