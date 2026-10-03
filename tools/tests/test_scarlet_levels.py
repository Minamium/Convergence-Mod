"""Scarlet loudness against Graceful Ordeal (ENCOUNTER_SPEC.md#sound-effects, REWARDS.md#levels-against-the-raid).

Every level here is ITU-R BS.1770-4 loudness (the standard's 48 kHz K-weighting run along time, both channels summed):
for a cue, the maximum 400 ms momentary loudness of the shipped file, padded 0.2 s before and 0.5 s after and stepped
10 ms; for the score, the median 3 s short-term loudness (100 ms steps) over the four stages as the client arranges them.
Measured 2026-10-03 (docs/evidence/2026-10-03-scarlet-loudness.json). The recipes' own meter (kit.loudness_stats, which
ran its K-weighting across the two channels) is a documented legacy number only and is not held here.

The tests hold relations, not levels: each Raid group's place against the score at the owner's sliders, each reward
role's place against the Raid cue it answers, and the balance the owner auditioned inside every group. The tables pin
the measured files by digest; a local-only test re-measures them and the score. Single files do not show a fight, so a
second local-only test renders four players' weapons at their real rates over the score and holds a clear band for each
Raid warning. Not hearing approval: the owner's in-game listening is not_run.
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
RULES = ROOT / 'Content/Encounters/CrimsonFoundry/Rewards/CrimsonRewardRules.cs'
# ITU-R BS.1770-4 K-weighting at 48 kHz (the pre-filter shelf, then the RLB high-pass).
ITU_K = [((1.53512485958697, -2.69169618940638, 1.19839281085285), (1.0, -1.69065929318241, 0.73248077421585)),
         ((1.0, -2.0, 1.0), (1.0, -1.99004745483398, 0.99007225036621))]

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
    'BatonLift': -18.49, 'BatonStroke': -20.35, 'Cadence': -14.36, 'CenserBrace': -15.35, 'CenserGrandPour': -14.17,
    'CenserPour': -21.02, 'CenserSummon': -19.36, 'CenserSwing': -16.87, 'ChoirClasp': -12.24, 'CrescentBreak': -20.31,
    'HandSlam': -17.84, 'HymnInhale': -16.81, 'InkBlaze': -15.78, 'InkIgnite': -19.09, 'OrganShot1': -17.25,
    'OrganShot2': -17.51, 'OrganShot3': -17.05, 'OrganShot4': -16.98, 'QuillStick': -17.44, 'QuillThrow': -19.97,
    'ReliquaryOpen': -12.30, 'RiverRelease': -13.33, 'ScoreChord': -12.68, 'ScoreUnseal': -18.55,
    'ScytheSwingHigh': -20.27, 'ScytheSwingLow': -20.35, 'ScytheVolley': -18.50, 'ScytheWhip': -18.87,
    'ScytheWhipBrace': -18.02, 'StaffBarline': -11.94, 'StaffCut': -17.11, 'StaffWindup': -17.57, 'Toll0': -22.25,
    'Toll1': -22.16, 'Toll2': -22.30, 'Toll3': -21.68, 'Toll4': -21.05, 'Toll5': -21.84, 'Toll6': -21.65, 'Toll7': -21.60,
}
REWARD_LEVELS_SHA256 = 'c75948110d3fafd12178586f782c0cfaf7aa3ebe4eeb848de00fdd1dd7edcb2d'
# The same meter on the files the owner auditioned and picked (0.3.77 / 0.3.78), before the 2026-10-03 lift: the
# balance inside each group is held against these.
AUDITIONED = {
    'ScarletActChange': -13.69, 'ScarletCrossflowCharge': -21.70, 'ScarletCrossflowRelease': -13.96,
    'ScarletDown': -20.46, 'ScarletForetell': -25.69, 'ScarletImpact': -20.31, 'ScarletReady': -27.09,
    'ScarletRevive': -18.93, 'ScarletSacrifice': -15.88, 'ScarletSpreadFail': -14.27, 'ScarletSpreadSuccess': -18.68,
    'ScarletSpreadSummon': -17.49, 'ScarletStackFail': -15.55, 'ScarletStackSuccess': -18.22,
    'ScarletStackSummon': -19.27, 'ScarletVictory': -11.82,
    'BatonLift': -18.49, 'BatonStroke': -20.35, 'Cadence': -14.36, 'CenserBrace': -15.35, 'CenserGrandPour': -14.17,
    'CenserPour': -21.02, 'CenserSummon': -19.36, 'CenserSwing': -16.87, 'ChoirClasp': -12.24, 'CrescentBreak': -20.31,
    'HandSlam': -17.84, 'HymnInhale': -16.81, 'InkBlaze': -15.78, 'InkIgnite': -19.09, 'OrganShot1': -17.25,
    'OrganShot2': -17.51, 'OrganShot3': -17.05, 'OrganShot4': -16.98, 'QuillStick': -17.44, 'QuillThrow': -19.97,
    'ReliquaryOpen': -16.15, 'RiverRelease': -13.33, 'ScoreChord': -12.68, 'ScoreUnseal': -18.55,
    'ScytheSwingHigh': -20.27, 'ScytheSwingLow': -20.35, 'ScytheVolley': -18.50, 'ScytheWhip': -18.87,
    'ScytheWhipBrace': -18.02, 'StaffBarline': -11.94, 'StaffCut': -17.11, 'StaffWindup': -17.57, 'Toll0': -22.25,
    'Toll1': -22.16, 'Toll2': -22.30, 'Toll3': -21.68, 'Toll4': -21.05, 'Toll5': -21.84, 'Toll6': -21.65, 'Toll7': -21.60,
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
# Reward roles (REWARDS.md#levels-against-the-raid). The first four sound in play and share one offset.
IN_PLAY = ('Build', 'Shot', 'Windup', 'Release')
ROLES = {
    'Build': {f'Toll{k}' for k in range(8)},
    'Shot': {'ScytheSwingHigh', 'ScytheSwingLow', 'ScytheWhip', 'ScytheVolley', 'CrescentBreak', 'OrganShot', 'BatonStroke',
             'CenserSummon', 'CenserSwing', 'QuillThrow', 'QuillStick'},
    'Windup': {'ScytheWhipBrace', 'StaffWindup', 'HymnInhale', 'BatonLift', 'CenserBrace', 'ScoreUnseal'},
    'Release': {'StaffCut', 'HandSlam', 'InkIgnite', 'CenserPour', 'InkBlaze'},
    'Finale': {'StaffBarline', 'ChoirClasp', 'RiverRelease', 'CenserGrandPour', 'ScoreChord', 'Cadence'},
    'Show': {'ReliquaryOpen'},
}
# The most a file may fall short of its group's common lift (the limiter's 3 dB cap on a transient file), and overshoot it.
BALANCE = (-0.7, 0.15)
# The four-player dense mix (dense_mix below): each warning's median best-band margin, dB. DENSE is the shipped build,
# rendered with DENSE_OFFSETS; DENSE_0380 is 0.3.80 rendered the same way (the Raid's 0.3.77 files, the rewards at
# 0.3.78's offsets with only per-shot cues lower for other players, the score on a linear slider). Every warning keeps at
# least DENSE_FLOOR in every scenario and loses nothing against 0.3.80.
DENSE_OFFSETS = {'in_play': -3.5, 'Finale': -.45, 'Show': 0.0, 'remote': -8.0}
DENSE = {
    'mixed2': {'ScarletForetell': 11.86, 'ScarletStackSummon': 9.10, 'ScarletSpreadSummon': 3.86, 'ScarletCrossflowCharge': 4.10},
    'mixed4': {'ScarletForetell': 11.76, 'ScarletStackSummon': 8.56, 'ScarletSpreadSummon': 3.80, 'ScarletCrossflowCharge': 3.78},
    'censers2': {'ScarletForetell': 10.88, 'ScarletStackSummon': 8.15, 'ScarletSpreadSummon': 3.75, 'ScarletCrossflowCharge': 4.07},
    'censers4': {'ScarletForetell': 10.75, 'ScarletStackSummon': 7.30, 'ScarletSpreadSummon': 3.60, 'ScarletCrossflowCharge': 3.56},
}
# The same four scenarios with the crescent cues in the other player's scythe (CRESCENT_BREAKS): no warning loses more than
# DENSE_CRESCENT_LOSS against DENSE (the most it lost is 0.20 dB in these margins, 0.23 dB with the breaks spread evenly)
# and every one keeps DENSE_FLOOR.
DENSE_CRESCENTS = {
    'mixed2': {'ScarletForetell': 11.86, 'ScarletStackSummon': 9.01, 'ScarletSpreadSummon': 3.85, 'ScarletCrossflowCharge': 4.17},
    'mixed4': {'ScarletForetell': 11.63, 'ScarletStackSummon': 8.48, 'ScarletSpreadSummon': 3.75, 'ScarletCrossflowCharge': 3.86},
    'censers2': {'ScarletForetell': 10.71, 'ScarletStackSummon': 8.07, 'ScarletSpreadSummon': 3.68, 'ScarletCrossflowCharge': 4.11},
    'censers4': {'ScarletForetell': 10.56, 'ScarletStackSummon': 7.37, 'ScarletSpreadSummon': 3.58, 'ScarletCrossflowCharge': 3.42},
}
DENSE_CRESCENT_LOSS = .25
DENSE_0380 = {
    'mixed2': {'ScarletForetell': 0.13, 'ScarletStackSummon': 5.68, 'ScarletSpreadSummon': 3.34, 'ScarletCrossflowCharge': 0.30},
    'mixed4': {'ScarletForetell': 0.03, 'ScarletStackSummon': 4.45, 'ScarletSpreadSummon': 3.31, 'ScarletCrossflowCharge': 0.14},
    'censers2': {'ScarletForetell': -0.49, 'ScarletStackSummon': 4.18, 'ScarletSpreadSummon': 3.17, 'ScarletCrossflowCharge': -0.16},
    'censers4': {'ScarletForetell': -0.51, 'ScarletStackSummon': 3.30, 'ScarletSpreadSummon': 3.16, 'ScarletCrossflowCharge': -0.30},
}
DENSE_FLOOR = 3.0
# The crescent cues (2026-10-04) in the same fight: the other player's scythe also sheds its volley once a measure (the
# Whip's tick 20, four strokes in) and rings CrescentBreak at the offsets below, in ticks from the measure's start. A held
# loop throws nine crescents a measure (the four single ones, one per Over and Under, and the volley's five) and each breaks
# once, so nine breaks and a volley in 100 ticks is the most it can ring: the 8-crescent cap bounds how many fly at once,
# not how many break, and the 6-tick throttle (ScytheInk.BreakCue) would allow sixteen. The first layout is the densest:
# each single breaks 30 ticks after its throw (age 9, at 18-tick strokes) and the volley's five break one by one, 6 ticks
# apart (the closest a break rings), 30 ticks after it is shed, as they do when a swarm takes each to its own target. The
# second spreads the same nine evenly through the measure, a different phase against the warnings.
CRESCENT_BREAKS = (9 + 30, 27 + 30, 45 + 30, 63 + 30) + tuple(4 * 18 + 20 + 30 + 6 * k for k in range(5))
CRESCENT_BREAKS_EVEN = tuple(39 + round(k * 100 / 9) for k in range(9))


def read(path):
    return path.read_text(encoding='utf-8')


def db(gain):
    return 20 * math.log10(gain)


def tml_music_db(volume):
    """tModLoader's music slider (ASoundEffectBasedAudioTrack.ReMapVolumeToMatchXact), dB."""
    return 31.0 * volume - 25.0 - 11.94


def music_gain():
    return float(re.search(r'MusicGain\(double scoreAge\) => ([\d.]+)f \* Ease', read(CONTENT / 'CrimsonInvocation.cs')).group(1))


def music_slider(volume):
    """CrimsonInvocation.MusicSlider evaluated from its own constants (volume <= 0 ? 0 : 10^(a (min(v, cap) - b) / c))."""
    source = ' '.join(read(CONTENT / 'CrimsonInvocation.cs').split())
    match = re.search(r'internal static float MusicSlider\(float volume\) => volume <= 0 \? 0 : '
                      r'MathF\.Pow\(10, ([\d.]+) \* \(Math\.Min\(volume, ([\d.]+)\) - ([\d.]+)\) / ([\d.]+)\);', source)
    slope, cap, top, scale = map(float, match.groups())
    return 0.0 if volume <= 0 else 10 ** (slope * (min(volume, cap) - top) / scale)


def bgm_in_cue_units(music, sound):
    """Where the score's reference sits on the cue files' scale, both as heard at these sliders: the score streams at
    MusicGain times the music curve, cues at the sound slider (linear, as tML's ActiveSound sets them)."""
    return BGM_REFERENCE + db(music_gain()) + db(music_slider(music)) - db(sound)


def files_digest(folder, stems):
    digest = hashlib.sha256()
    for stem in sorted(stems):
        digest.update(f'{stem}.ogg:{hashlib.sha256((folder / f"{stem}.ogg").read_bytes()).hexdigest()}\n'.encode())
    return digest.hexdigest()


def raid_gain_db():
    return db(float(re.search(r'internal const float Gain = ([\d.]+)f;', read(CLIENT / 'ScarletSounds.cs')).group(1)))


def role_offsets():
    """ScarletRewardCues.<Role>Decibels, in dB (a role may name InPlayDecibels)."""
    text = read(CUES)
    values = {'InPlayDecibels': float(re.search(r'\bInPlayDecibels = (-?[\d.]+)f;', text).group(1))}
    out = {}
    for role in ROLES:
        value = re.search(rf'\b{role}Decibels = (-?[\d.]+f|InPlayDecibels)\b', text).group(1)
        out[role] = values[value] if value in values else float(value.rstrip('f'))
    return out


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


# ---- The four-player dense mix ----------------------------------------------------------------------------------------
# A single file's level cannot show what a fight sounds like: the weapons fire many times a second and other players'
# weapons add up. This renders Graceful Ordeal's song bars 19-23 at the owner's sliders with the local player's organ
# and three other players' weapons firing at their real rates, as ScarletRewardAudio plays them (role offsets, the
# remote offset, the voice pools with replace-oldest or ignore-new, other players 250 px away: tML's positional
# 1 - d/2500 = 0.9), and lays each Raid warning over it at ten places. The measure is the warning's best 1/3-octave
# band against the score and the weapons together, in the 400 ms where the warning is loudest (the median of the ten).
# A model, not a capture: organ shots every UseTicks(Ranged), scythe strokes every StrokeTicks, a quill thrown every
# UseTicks(Rogue) that sticks 6 ticks later, and n censers whose apexes come every ApexInterval / n ticks, cued through
# the owner's sound budget the way CenserVisuals does.
WARNINGS = ('ScarletForetell', 'ScarletStackSummon', 'ScarletSpreadSummon', 'ScarletCrossflowCharge')
# scenario: (censer owners, censers each). Every scenario has the local organ (owner 0), another player's scythe
# (owner 1) and another's quill (owner 3). The scythe's two crescent cues (2026-10-04) join it in a second run of each.
DENSE_SCENARIOS = {'mixed2': ((2,), 2), 'mixed4': ((2,), 4), 'censers2': ((1, 2, 3), 2), 'censers4': ((1, 2, 3), 4)}


def cue_table():
    """Cue name -> (audience, role, the owner's voices), from ScarletRewardCues.All."""
    text = read(CUES)
    body = text[text.index('internal static readonly ScarletCue[] All'):]
    body = body[:body.index('};')]
    out = {}
    for quoted, named, audience, role, voices in re.findall(r"new\((?:\"(\w+)\"|(\w+)), '[AB]', (\w+), (\w+), (\d+),", body):
        out[quoted or named] = (audience, 'Shot' if role == 'OneShot' else role, int(voices))
    return out


def remote_decibels():
    return float(re.search(r'RemoteCueDecibels = (-?[\d.]+)f?;', read(RULES)).group(1))


def dense_events(scenario, end, tick, crescents=None):
    """(owner, cue, file, start sample) for one scenario; owner 0 is the local player. With crescents (a layout of break
    offsets, see CRESCENT_BREAKS), the other player's scythe also rings its two crescent cues at their real rates."""
    rules = read(RULES)
    const = lambda name: int(re.search(rf'\b{name} = (\d+)', rules).group(1))  # noqa: E731
    uses = rules[rules.index('internal static int UseTicks('):]
    use = lambda kind: int(re.search(rf'CrimsonRewardKind\.{kind} => (\d+),', uses).group(1))  # noqa: E731
    events = []

    def every(owner, cue, period, first, file_of):
        k, t = 0, first * tick
        while t < end:
            events.append((owner, cue, file_of(k), t))
            t += period * tick
            k += 1

    every(0, 'OrganShot', use('Ranged'), 1, lambda k: f'OrganShot{k % 4 + 1}')
    every(1, 'ScytheSwingHigh', const('StrokeTicks'), 4, lambda k: 'ScytheSwingHigh' if k % 2 == 0 else 'ScytheSwingLow')
    if crescents is not None:
        measure = 4 * const('StrokeTicks') + const('WhipTicks')
        start = 4 + 4 * const('StrokeTicks')  # the scythe's first stroke above is at 4; the Whip follows four strokes
        every(1, 'ScytheVolley', measure, start + const('VolleyAge'), lambda k: 'ScytheVolley')
        for offset in crescents:
            every(1, 'CrescentBreak', measure, 4 + offset, lambda k: 'CrescentBreak')
    every(3, 'QuillThrow', use('Rogue'), 10, lambda k: 'QuillThrow')
    every(3, 'QuillStick', use('Rogue'), 16, lambda k: 'QuillStick')
    owners, n = DENSE_SCENARIOS[scenario]
    apex, grand_every, brace = const('ApexInterval'), const('GrandEvery'), const('GrandBrace')
    lead, swing_gap, pour_gap, grand_gap = const('SwingCueLead'), const('SwingCueInterval'), const('PourCueInterval'), const('GrandCueInterval')
    for owner in owners:
        last = {'swing': -999, 'pour': -999, 'grand': -999, 'brace': -999}
        k, t = 0, (7 if len(owners) == 1 else 5 + 3 * owner)
        while t * tick < end:
            cues = []
            if (k // n) % grand_every == grand_every - 1:
                if t - brace - last['brace'] >= swing_gap and t - last['grand'] >= grand_gap:
                    cues.append(('CenserBrace', t - brace))
                    last['brace'] = t - brace
                if t - last['grand'] >= grand_gap:
                    cues.append(('CenserGrandPour', t))
                    last['grand'] = t
            else:
                if t - lead - last['swing'] >= swing_gap:
                    cues.append(('CenserSwing', t - lead))
                    last['swing'] = t - lead
                if t - last['pour'] >= pour_gap:
                    cues.append(('CenserPour', t))
                    last['pour'] = t
            events += [(owner, cue, cue, int(at * tick)) for cue, at in cues if 0 <= at * tick < end]
            k += 1
            t += apex / n
    return sorted(events, key=lambda e: e[3])


def dense_mix(scenario, raid=RAID, sounds=SOUNDS, role_db=None, remote_db=None, every_remote=True, slider=None, crescents=None):
    """Each warning's median best-band margin (dB) over the score and the weapons in one scenario. The defaults are the
    shipped files and code; the arguments let the evidence render 0.3.80 the same way."""
    import numpy as np
    import soundfile as sf
    from scipy import signal

    rate, bar, tick = 48000, 90000, 800
    role_db = role_offsets() if role_db is None else role_db
    remote_db = remote_decibels() if remote_db is None else remote_db
    slider = music_slider(OWNER_MUSIC) if slider is None else slider
    cache = {}

    def clip(folder, stem):
        if (folder, stem) not in cache:
            x, sr = sf.read(str(folder / f'{stem}.ogg'), always_2d=True, dtype='float64')
            assert sr == rate
            cache[folder, stem] = x
        return cache[folder, stem]

    song, _ = sf.read(str(BGM), always_2d=True, dtype='float64')
    music = song[19 * bar:24 * bar] * music_gain() * slider
    n = len(music)
    table = cue_table()
    voices, active = [], {}
    for owner, cue, stem, start in dense_events(scenario, n, tick, crescents):
        audience, role, own = table[cue]
        remote = owner != 0
        db = role_db[role] + (remote_db if remote and (every_remote or audience == 'Shot') else 0)
        gain = 10 ** (db / 20) * (.9 if remote else 1) * OWNER_SOUND
        key = stem + (':peer' if remote else '')
        limit = (1 if audience == 'Shot' else own) if remote else own
        replace = audience == 'Shot' if remote else True
        playing = [v for v in active.get(key, []) if voices[v][2] > start]
        if len(playing) >= limit:
            if not replace:
                active[key] = playing
                continue
            oldest = min(playing, key=lambda v: voices[v][1])
            voices[oldest][2] = start
            playing.remove(oldest)
        voices.append([stem, start, start + len(clip(sounds, stem)), gain])
        active[key] = playing + [len(voices) - 1]
    masker = music.copy()
    for stem, s0, e0, gain in voices:
        m = min(e0 - s0, n - s0)
        if m > 0:
            masker[s0:s0 + m] += clip(sounds, stem)[:m] * gain

    def kpower(x):
        for b, a in ITU_K:
            x = signal.lfilter(b, a, x, axis=0)
        return np.concatenate([[0.0], np.cumsum(np.sum(x ** 2, axis=1))])

    centres = [c for c in 1000 * 2 ** (np.arange(-17, 14) / 3) if 40 <= c <= 16000]
    bands = [signal.butter(4, [c * 2 ** (-1 / 6), min(c * 2 ** (1 / 6), 23000)], btype='band', fs=rate, output='sos') for c in centres]

    def band_energy(x, a, b):
        mono, pre = x[:, 0] + x[:, 1], min(4800, a)
        return np.array([np.mean(signal.sosfilt(sos, mono[a - pre:b])[pre:] ** 2) + 1e-20 for sos in bands])

    win, out = round(.4 * rate), {}
    for warning in WARNINGS:
        x = clip(raid, warning) * 10 ** (raid_gain_db() / 20) * OWNER_SOUND
        best = []
        for k in range(10):
            a = bar + k * 30011
            m = min(len(x), n - a)
            sig = np.zeros_like(music)
            sig[a:a + m] = x[:m]
            energy = kpower(sig)
            starts = np.arange(a, a + max(1, len(x) - win // 2), 480)
            starts = starts[starts + win < len(energy) - 1]
            w0 = int(starts[int(np.argmax(energy[starts + win] - energy[starts]))])
            best.append(float((10 * np.log10(band_energy(sig, w0, w0 + win) / band_energy(masker, w0, w0 + win))).max()))
        out[warning] = float(np.median(best))
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
        # MusicSlider's own constants, read from its source, against tML's curve: a drifted slope, cap or scale fails
        # here (the domain suite runs the compiled method against the same formula).
        for v in (0.05, 0.25, 0.5, OWNER_MUSIC, 0.9, 1.0):
            self.assertAlmostEqual(tml_music_db(v) - tml_music_db(1), db(music_slider(v)), places=4, msg=f'slider {v}')
        self.assertEqual(1.0, music_slider(1.5), 'never past a full slider')
        self.assertEqual(0.0, music_slider(0.0), 'a muted slider silences the score')
        self.assertEqual(0.0, music_slider(-0.1))
        self.assertAlmostEqual(-6.47, db(music_slider(OWNER_MUSIC)) - db(OWNER_MUSIC), delta=.01,
                               msg="the owner's slider moves the score 6.5 dB against the linear path")

    def test_the_level_tables_measure_the_shipped_files(self):
        self.assertEqual(sorted(RAID_LEVELS), sorted(p.stem for p in RAID.glob('*.ogg')), 'every Raid cue is measured')
        self.assertEqual(sorted(REWARD_LEVELS), sorted(reward_files()), 'every reward file is measured')
        self.assertEqual(sorted(AUDITIONED), sorted({**RAID_LEVELS, **REWARD_LEVELS}))
        message = 'a measured file changed: re-measure it and re-check the relations (REWARDS.md#levels-against-the-raid)'
        self.assertEqual(RAID_LEVELS_SHA256, files_digest(RAID, RAID_LEVELS), message)
        self.assertEqual(REWARD_LEVELS_SHA256, files_digest(SOUNDS, REWARD_LEVELS), message)
        self.assertEqual(BGM_SHA256, hashlib.sha256(BGM.read_bytes()).hexdigest(), 'the score changed: re-measure BGM_REFERENCE')
        # The dense mix was rendered with these offsets: change one and re-render DENSE (and the evidence).
        offsets = role_offsets()
        self.assertEqual(DENSE_OFFSETS, {'in_play': offsets['Shot'], 'Finale': offsets['Finale'], 'Show': offsets['Show'],
                                         'remote': remote_decibels()}, 'the dense mix is stale: re-render it')

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
        offsets = role_offsets()
        self.assertEqual({offsets['Shot']}, {offsets[role] for role in IN_PLAY}, 'the cues that sound in play share one offset')
        self.assertTrue(-7 <= med['Shot'] - impact <= -5, 'one-shots about 6 dB under the Raid impact')
        self.assertLess(max(by_role['Shot']), foretell, 'every one-shot under the Raid foretell')
        self.assertLess(max(by_role['Build']), min(by_role['Shot']), 'tolls under every one-shot')
        self.assertTrue(-3 <= med['Windup'] - foretell <= -1, 'windups and braces about 2 dB under the Raid foretell')
        self.assertTrue(-7 <= med['Release'] - impact <= -5, 'cascade parts about 6 dB under the Raid impact')
        self.assertLessEqual(max(level[s] for s, (_, r) in files.items() if r in IN_PLAY), foretell + 1,
                             'no cue that sounds in play more than 1 dB over the Raid foretell')
        self.assertTrue(-2 <= med['Finale'] - release <= 0, 'finales about 1 dB under the crossflow release')
        self.assertLessEqual(max(by_role['Finale']), release + .5, 'no finale over the crossflow release')
        self.assertTrue(-3 <= level['ReliquaryOpen'] - victory <= -1, 'the reliquary show about 2 dB under the Raid Victory')
        # Offsets never boost a file past its level: a role that had to rise was re-rendered with the lift baked in.
        for role, offset in offsets.items():
            self.assertLessEqual(offset, 0, role)
        self.assertLessEqual(remote_decibels(), -6, "other players' cues stay well under the local player's")
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
        # Every reward file but the reliquary's show is the auditioned file; only the offsets moved.
        for stem, (_, role) in files.items():
            if role != 'Show':
                self.assertEqual(AUDITIONED[stem], REWARD_LEVELS[stem], stem)
        self.assertEqual({offsets['Shot']}, {offsets[role] for role in IN_PLAY}, 'the in-play cues keep their auditioned balance')

    @unittest.skipUnless(numeric_audio_stack(), 'numpy, scipy and soundfile are local audio tools, not CI')
    def test_the_tables_match_a_fresh_measurement(self):
        import numpy as np
        import soundfile as sf
        from scipy import signal

        rate = 48000

        def power(x):
            for b, a in ITU_K:
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

    @unittest.skipUnless(numeric_audio_stack(), 'numpy, scipy and soundfile are local audio tools, not CI')
    def test_a_four_player_fight_leaves_every_warning_a_clear_band(self):
        # The local organ and three other players' scythe, quill and censers at their real rates over the score: each
        # Raid warning keeps a 1/3-octave band DENSE_FLOOR over everything else, and no less than in 0.3.80.
        for scenario in DENSE_SCENARIOS:
            margins = dense_mix(scenario)
            for warning in WARNINGS:
                with self.subTest(scenario=scenario, warning=warning):
                    self.assertAlmostEqual(DENSE[scenario][warning], margins[warning], delta=.02)
                    self.assertGreaterEqual(margins[warning], DENSE_FLOOR)
                    self.assertGreaterEqual(margins[warning], DENSE_0380[scenario][warning])

    @unittest.skipUnless(numeric_audio_stack(), 'numpy, scipy and soundfile are local audio tools, not CI')
    def test_the_crescent_cues_leave_the_four_player_fight_clear(self):
        # ScytheVolley and CrescentBreak, rung by the other player's scythe at the most a held loop allows (a volley and nine
        # breaks in each 100-tick measure, the volley's five 6 ticks apart; other players 8 dB lower with one voice each),
        # take no more than DENSE_CRESCENT_LOSS from any warning and leave every one at least DENSE_FLOOR. The same nine
        # breaks spread evenly through the measure meet the same bounds (their margins are not pinned, only bounded).
        for scenario in DENSE_SCENARIOS:
            for layout, breaks in (('densest', CRESCENT_BREAKS), ('even', CRESCENT_BREAKS_EVEN)):
                margins = dense_mix(scenario, crescents=breaks)
                for warning in WARNINGS:
                    with self.subTest(scenario=scenario, layout=layout, warning=warning):
                        if layout == 'densest':
                            self.assertAlmostEqual(DENSE_CRESCENTS[scenario][warning], margins[warning], delta=.02)
                        self.assertGreaterEqual(margins[warning], DENSE_FLOOR)
                        self.assertGreaterEqual(margins[warning], DENSE[scenario][warning] - DENSE_CRESCENT_LOSS)
                        self.assertGreaterEqual(margins[warning], DENSE_0380[scenario][warning])


if __name__ == '__main__':
    unittest.main()
