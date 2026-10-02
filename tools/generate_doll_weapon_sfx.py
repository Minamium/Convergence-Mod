"""Doll reward weapon cues and the companion summon (shared audio basis of the Doll weapon refresh).

Renders Assets/Sounds/Weapons/DollWeapons/<Cue>.ogg. Every cue is one builder function registered
with @cue in CUES (short-term loudness target, group, maximum length, in-game playback volume and
the owner audition text); later weapon changes add their cues here and reuse the building blocks
of tools/doll_sfx_dsp.py (music-box comb tooth on the F minor pentatonic ladder, brass ratchet,
porcelain ring and crack, additive organ pad, shimmer, low thump).

Recordings come only from the pinned SOURCES table: CC0 recordings already attributed for the
Ebon and Cathedral cues, read from the local store (--store, never committed) and project-owned
masters (repo:). Each is SHA-256 checked before use; a store recording must also be listed with
that hash in Assets/ATTRIBUTION.md, a project master by its runtime path.

Loudness is the Ebon scale (generate_ebon_reward_sfx helpers, imported unmodified): BS.1770
K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip.
Deterministic: a cue's random stream is seeded from its name and the Ogg stream serial is pinned
from the file stem (generate_ebon_sfx.write_ogg), so a rerun, or --only, is byte-identical.
Requires numpy, scipy and soundfile (local audio tools, not CI).

Run: py -3.12 tools/generate_doll_weapon_sfx.py [--store DIR] [--only CUE,...] [--output DIR]
     [--preview DIR] [--attribution-section FILE]
"""
import argparse
import hashlib
import html
import io
import json
import os
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import numpy as np
import soundfile as sf
from scipy import signal

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_ebon_sfx as base  # noqa: E402  (shared helpers; its CLI only runs as __main__)
from generate_ebon_sfx import RATE, fades, hp, lp, pan, place, seconds, sha256, speed, trim  # noqa: E402
from generate_ebon_reward_sfx import loudness, master, room, true_peak_db  # noqa: E402
import doll_sfx_dsp as dsp  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Sounds" / "Weapons" / "DollWeapons"
ATTRIBUTION = ROOT / "Assets" / "ATTRIBUTION.md"
SEED_PREFIX = "DollWeapon:"
TICK = 1 / 60
SIXTEENTH = 60 / 218 / 4  # the Doll BGM source tempo (DistantLiturgy, 218 BPM): one sixteenth = 4.13 ticks

# key: (path, origin, SHA-256, author). A path under the local store ('!' = zip member) or 'repo:' + a path
# in this repository. Store recordings are the CC0 files already in the Assets/ATTRIBUTION.md tables.
KENNEY = "sfx-sources/cc0/pack-OGA-Kenney-RPGsounds.zip"
SOURCES = {
    "metal_click": (KENNEY + "!OGG/metalClick.ogg",
                    "Kenney RPG Audio metalClick.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
                    "9851a69d0c613e13bceef08060ecc4148f098ef487927cbebe270d642398a3b3", "Kenney"),
    "metal_latch": (KENNEY + "!OGG/metalLatch.ogg",
                    "Kenney RPG Audio metalLatch.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
                    "ba9ba60b172b3ebc131a940f25793cd2e207aca7af73dc80d637277f060f1708", "Kenney"),
    "doll_summon": ("repo:Assets/Sounds/Weapons/DollTheater/DollSummon.wav",
                    "Convergence project asset doll-theater-0253-dollsummon (0.3.7 articulation revision)",
                    "f4be1a0733e06ad0262d6703a44fc00a0a01dd9a17a3fb1a3381831d634e8534", "Convergence"),
}


def find_store():
    """The local recording store: $CONVERGENCE_AUDIO_STORE, else the nearest ancestor '.claude-tools' that holds
    sfx-sources (the workspace convention); None when absent (CI)."""
    env = os.environ.get("CONVERGENCE_AUDIO_STORE")
    if env:
        return Path(env)
    for parent in ROOT.parents:
        if (parent / ".claude-tools" / "sfx-sources").is_dir():
            return parent / ".claude-tools"
    return None


class Store:
    """Key -> stereo float64 at 44.1 kHz; every read is hash-checked and recorded for the attribution."""

    def __init__(self, root, attribution_text):
        self.root = Path(root) if root else None
        self.attribution = attribution_text
        self.cache, self.hashes, self.used = {}, {}, set()

    def raw(self, key):
        path, _, expected, _ = SOURCES[key]
        if path.startswith("repo:"):
            data = (ROOT / path[5:]).read_bytes()
            listed = f"`{path[5:]}`" in self.attribution
        else:
            if self.root is None:
                raise RuntimeError(f"{key} needs the local recording store (--store or CONVERGENCE_AUDIO_STORE)")
            if "!" in path:
                archive, member = path.split("!", 1)
                data = zipfile.ZipFile(self.root / archive).read(member)
            else:
                data = (self.root / path).read_bytes()
            listed = expected in self.attribution
        found = sha256(data)
        if found != expected:
            raise RuntimeError(f"Source hash mismatch for {key}: {found} != {expected}")
        if not listed:
            raise RuntimeError(f"Source {key} is not recorded in Assets/ATTRIBUTION.md")
        self.hashes[key] = found
        return data

    def get(self, key):
        self.used.add(key)
        if key not in self.cache:
            x, rate = sf.read(io.BytesIO(self.raw(key)), dtype="float64", always_2d=True)
            if x.shape[1] == 1:
                x = np.repeat(x, 2, axis=1)
            if rate != RATE:
                g = np.gcd(rate, RATE)
                x = signal.resample_poly(x, RATE // g, rate // g, axis=0)
            self.cache[key] = x[:, :2]
        return self.cache[key]


def lay(mix, s, key, start, end, at=0.0, gain=0.0, rate=1.0, hp_=None, lp_=None, fade_in=0.002, fade_out=0.02, side=None):
    """Cut [start, end] s of a recording, varispeed/filter/pan it and add it to the mix at `at` s."""
    x = s.get(key)[round(start * RATE):round(end * RATE)].copy()
    if len(x) < 8:
        raise ValueError(f"Window outside {key}: {start}..{end}")
    if rate != 1.0:
        x = speed(x, rate)
    if hp_:
        x = hp(x, hp_)
    if lp_:
        x = lp(x, lp_)
    if side is not None:
        x = pan(x, side)
    return place(mix, fades(x, fade_in, fade_out), at, gain)


# ---------------------------------------------------------------- cue registry
@dataclass(frozen=True)
class Cue:
    build: Callable
    target_lufs: float   # short-term LUFS of the master (the Ebon scale)
    group: str           # audition page section
    max_seconds: float   # hard length budget, checked after encoding
    volume: float        # SoundStyle volume at the call site (the audition plays it at this level)
    description: str     # owner audition text (Japanese)
    glue: float = 1.6    # tanh bus drive before the loudness match (generate_ebon_reward_sfx.soft)


CUES = {}


def cue(name, target_lufs, group, max_seconds, volume, description, glue=1.6):
    def register(build):
        if name in CUES:
            raise ValueError(f"duplicate cue {name}")
        CUES[name] = Cue(build, target_lufs, group, max_seconds, volume, description, glue)
        return build
    return register


# ---------------------------------------------------------------- cues
# Old DollSummon: 0.55 s at -7.8 LUFS short-term, played by RitualWeaponFeedback at min(.95, .325 x 2) = .65,
# i.e. about -11.5 LUFS effective. CompanionSummon is mastered 1 dB hotter and played at .9 (-0.9 dB):
# the same effective level, with every layer of the new phrase audible.
SUMMON_CLICKS = (0.0, 0.075, 0.14)          # the brass key turns: three ratchet teeth, accelerating
SUMMON_LATCH = 0.195                        # the key lets go: latch, the comb starts to turn
SUMMON_PHRASE = (("F5", 0.235), ("Ab5", 0.235 + SIXTEENTH), ("C6", 0.235 + 2 * SIXTEENTH),
                 ("Eb6", 0.235 + 3 * SIXTEENTH))
SUMMON_LANDING = 0.235 + 5 * SIXTEENTH      # F6 lands an eighth after Eb6, with the porcelain chime
SUMMON_LENGTH = 1.26


@cue("CompanionSummon", -10.5, "Companion", 1.3, 0.9,
     "人形の相棒が現れる。真鍮の鍵が3つ巻かれ（ラチェット）、留め金が外れるとオルゴールが F5→A♭5→C6→E♭6 と上がって "
     "F6 に着地し、磁器のチャイムと柔らかなオルガンの息、短いきらめきで終わる。出だしには旧 DollSummon の"
     "「拡がる到着音」を薄く重ねて、相棒の到着だと分かる性格を残している。")
def companion_summon(s, rng):
    mix = seconds(1.6)
    # Arrival: the old cue's diffuse spectral bloom, thinned so the key and the comb read through it.
    lay(mix, s, "doll_summon", 0.0, 0.42, 0.0, -9.5, hp_=320, lp_=7000, fade_in=0.001, fade_out=0.16)
    place(mix, dsp.thump(150, 62, 0.22, rng), 0.0, -17)
    # The brass key on the doll's back: three accelerating ratchet teeth, then the latch lets go.
    place(mix, dsp.ratchet(SUMMON_CLICKS, rng, freq=2450, gains_db=(-3, -2, -1), side=-0.25), 0.0, -8)
    for at, rate in zip(SUMMON_CLICKS, (1.0, 1.06, 0.97)):  # metalClick's second click starts at 0.2711 s
        lay(mix, s, "metal_click", 0.2705, 0.34, at, -5, rate=rate, hp_=1400, side=-0.3, fade_in=0.0005, fade_out=0.03)
    # metalLatch's catch starts at 0.0418 s.
    lay(mix, s, "metal_latch", 0.041, 0.20, SUMMON_LATCH, -10, rate=1.12, hp_=900, side=-0.2, fade_in=0.0005, fade_out=0.08)
    place(mix, pan(dsp.brass_click(rng, 1900, decay=0.007, thud=0.6), -0.2), SUMMON_LATCH, -10)
    # The comb turns: a rising F minor pentatonic phrase, each tooth a little to the right of the last.
    for i, (note, at) in enumerate(SUMMON_PHRASE):
        place(mix, pan(dsp.box_tine(dsp.hz(note), 0.9, rng), -0.15 + 0.1 * i), at, -8 + 0.5 * i)
    # The landing: F6 with Ab6 and C7 struck a hair later (an open F minor chord above the BGM's dense mids).
    place(mix, pan(dsp.box_tine(dsp.hz("F6"), 1.0, rng), 0.15), SUMMON_LANDING, -3)
    place(mix, pan(dsp.box_tine(dsp.hz("Ab6"), 0.8, rng), -0.3), SUMMON_LANDING + 0.004, -12)
    place(mix, pan(dsp.box_tine(dsp.hz("C7"), 0.7, rng), 0.35), SUMMON_LANDING + 0.008, -10)
    # Porcelain chime on the landing (a dry ceramic ring an octave above, never a long bell).
    place(mix, dsp.porcelain_ring(dsp.hz("F7"), 0.5, rng, decay=0.09, side=0.3), SUMMON_LANDING + 0.003, -10)
    place(mix, dsp.porcelain_ring(dsp.hz("C8"), 0.3, rng, decay=0.05, side=-0.3), SUMMON_LANDING + 0.018, -18)
    # A soft organ breath under the phrase: F minor (F3 C4 F4 Ab4) and its wind, swelling into the landing
    # and gone before the shimmer ends.
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F3", "C4", "F4", "Ab4")], 0.86, rng, attack=0.38, release=0.45,
                        harmonics=8, rolloff=1.5, chiff=0.03, breath=0.22)
    place(mix, pad, 0.2, -18)
    # A brief shimmer as the doll settles.
    place(mix, dsp.shimmer(0.6, rng, count=10), SUMMON_LANDING + 0.05, -17)
    return room(mix, 0.14, SUMMON_LENGTH, 0.3)


# ---------------------------------------------------------------- render
def seed(name):
    return int.from_bytes(hashlib.sha256((SEED_PREFIX + name).encode("utf-8")).digest()[:8], "little")


def render(name, store):
    x = trim(hp(CUES[name].build(store, np.random.default_rng(seed(name))), 28))
    limit = round(CUES[name].max_seconds * RATE)
    if len(x) > limit:
        raise RuntimeError(f"{name}: {len(x) / RATE:.3f} s exceeds its {CUES[name].max_seconds} s budget")
    return x


def render_cue(name, store, output):
    """Master to the cue's target, write the Ogg and, if the Vorbis round trip overshoots -1 dBTP, back the gain off."""
    store.used = set()
    spec = CUES[name]
    path = output / f"{name}.ogg"
    mastered = master(render(name, store), spec.target_lufs, spec.glue)
    for _ in range(8):
        decoded = base.write_ogg(path, mastered)
        peak = true_peak_db(decoded)
        if peak <= -1.0:
            return decoded, sorted(store.used)
        mastered = mastered * 10 ** (-(peak + 1.1) / 20)
    raise RuntimeError(f"{name}: true peak {peak:.2f} dBTP after retries")


def ogg_serial(data):
    return int.from_bytes(data[14:18], "little")


def pinned_serial(stem):
    """The serial generate_ebon_sfx.write_ogg pins for a file stem."""
    return int.from_bytes(hashlib.sha256(stem.encode("utf-8")).digest()[:4], "little")


OCTAVES = (63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000)


def character(x):
    """Spectral character: centroid, octave-band levels relative to the loudest band, weighted flatness
    (0 = tonal, 1 = noise), side/mid ratio, 10 ms envelope peak time and -20 dB decay time."""
    m = x.mean(axis=1)
    f, p = signal.welch(m, RATE, nperseg=min(4096, len(m)))
    bands = np.array([p[(f >= c / np.sqrt(2)) & (f < min(c * np.sqrt(2), RATE / 2))].sum() for c in OCTAVES])
    bands = 10 * np.log10(bands / bands.max() + 1e-18)
    _, _, z = signal.stft(m, RATE, nperseg=2048)
    power = np.abs(z) ** 2 + 1e-18
    flat = np.exp(np.mean(np.log(power), axis=0)) / np.mean(power, axis=0)
    weight = power.sum(axis=0)
    side = (x[:, 0] - x[:, 1]) / 2
    frames = np.array([np.sqrt(np.mean(m[i:i + 441] ** 2) + 1e-18) for i in range(0, len(m) - 441, 441)])
    db = 20 * np.log10(frames / frames.max())
    top = int(frames.argmax())
    below = np.nonzero(db[top:] < -20)[0]
    return {
        "centroid_hz": round(float((f * p).sum() / (p.sum() + 1e-18))),
        "octave_bands_db": {str(c): round(float(v), 1) for c, v in zip(OCTAVES, bands)},
        "spectral_flatness": round(float((flat * weight).sum() / weight.sum()), 4),
        "side_to_mid_db": round(float(10 * np.log10((side ** 2).sum() / ((m ** 2).sum() + 1e-18) + 1e-18)), 1),
        "peak_ms": top * 10,
        "decay_20db_ms": int(below[0]) * 10 if len(below) else None,
    }


def measure(x):
    return {"seconds": round(len(x) / RATE, 3), "short_term_lufs": round(loudness(x), 2),
            "true_peak_dbfs": round(true_peak_db(x), 2)} | character(x)


def analyse(name, decoded, path, sources_used):
    data = path.read_bytes()
    spec = CUES[name]
    info = measure(decoded) | {
        "target_lufs": spec.target_lufs,
        "max_seconds": spec.max_seconds,
        "volume": spec.volume,
        "effective_lufs": round(loudness(decoded) + 20 * np.log10(spec.volume), 2),
        "sources": sources_used,
        "bytes": len(data),
        "ogg_serial": ogg_serial(data),
        "ogg_sha256": sha256(data),
    }
    if info["seconds"] > spec.max_seconds + 1e-6:
        raise RuntimeError(f"{name}: encoded length {info['seconds']} s exceeds {spec.max_seconds} s")
    return info


# ---------------------------------------------------------------- audition (owner listening page, git-ignored)
# Legacy cue each new cue replaces: (source key, effective in-game volume), for the A/B rows.
LEGACY = {"CompanionSummon": ("doll_summon", 0.65)}
# BGM excerpts to hear the cues in the music: (file, BPM, beat-grid origin s, first beat, beats, cue times within the
# excerpt, label). Whole beats on the measured grid (the cue sheet's tempos), so the bed loops in time.
BGM = (("ObsidianLiturgy", 168.0, -0.008, 268, 44, (2.0, 7.3, 12.1), "第1相（ObsidianLiturgy）"),
       ("DistantLiturgy", 218.0, 0.068, 28, 56, (2.0, 7.3, 12.1), "第3相（DistantLiturgy、全武器の調 F の基準）"))


def load_bgm(name, start, length):
    """Excerpt of a Doll BGM resampled to 44.1 kHz (the masters are 48 kHz); checked against the original
    by cross-correlation so the excerpt keeps its speed and pitch (the audition plays at the real tempo)."""
    x, rate = sf.read(str(ROOT / "Assets" / "Music" / f"{name}.ogg"), always_2d=True, dtype="float64")
    seg = x[round(start * rate):round((start + length) * rate), :2]
    out = signal.resample_poly(seg, RATE // np.gcd(rate, RATE), rate // np.gcd(rate, RATE), axis=0)
    back = signal.resample_poly(out, rate // np.gcd(rate, RATE), RATE // np.gcd(rate, RATE), axis=0)[:len(seg)]
    a, b = seg.mean(axis=1)[rate:rate * 3], back.mean(axis=1)[rate:rate * 3]
    corr = signal.correlate(a, b, mode="full", method="fft")
    lag = int(corr.argmax()) - (len(b) - 1)
    r = float(np.dot(a, b) / np.sqrt(np.dot(a, a) * np.dot(b, b)))
    if lag != 0 or r < 0.999:
        raise RuntimeError(f"BGM resample check failed for {name}: lag {lag}, r {r:.5f}")
    return fades(out, 0.004, 0.004), {"source_rate": rate, "lag": lag, "r": round(r, 6)}


def overlay(bed, cue_x, times, gain):
    mix = bed.copy()
    for t in times:
        place(mix, cue_x * gain, t)
    return mix


def preview(directory, samples, report, store):
    directory.mkdir(parents=True, exist_ok=True)

    def wav(name, x):
        peak = np.abs(x).max()
        if peak > 0.999:
            raise RuntimeError(f"audition clip {name} clips ({peak:.3f})")
        sf.write(str(directory / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        return f"{name}.wav"

    rows, extra = [], {}
    for name, x in samples.items():
        spec = CUES[name]
        clips = {"new": wav(name, x * spec.volume)}
        if name in LEGACY:
            key, volume = LEGACY[name]
            old = store.get(key) * volume
            clips["old"] = wav(name + "-old", old)
            gap = seconds(0.9)
            clips["ab"] = wav(name + "-ab", np.concatenate((old, gap, x * spec.volume, gap, old, gap, x * spec.volume)))
            extra[name] = {"legacy": measure(store.get(key)) | {"volume": volume,
                                                                "effective_lufs": round(loudness(store.get(key)) + 20 * np.log10(volume), 2)}}
            for bgm, bpm, origin, first, beats, times, label in BGM:
                start, length = origin + first * 60 / bpm, beats * 60 / bpm
                bed, check = load_bgm(bgm, start, length)
                new, old = overlay(bed, x, times, spec.volume), overlay(bed, store.get(key), times, volume)
                # One common gain for the bed and both versions: relative levels stay the in-game ones.
                trim_db = min(0.0, 20 * np.log10(0.97 / max(np.abs(new).max(), np.abs(old).max(), np.abs(bed).max())))
                g = 10 ** (trim_db / 20)
                clips[f"bgm-{bgm}"] = wav(f"{name}-{bgm}", new * g)
                clips[f"bgm-{bgm}-old"] = wav(f"{name}-{bgm}-old", old * g)
                clips[f"bed-{bgm}"] = wav(f"bgm-{bgm}", bed * g)
                extra[name][bgm] = check | {"start": round(start, 4), "length": round(length, 4), "bpm": bpm,
                                            "beats": beats, "cue_times": list(times), "mix_trim_db": round(trim_db, 2)}
        rows.append((name, clips))
    page(directory, rows, report, extra)
    return extra


def page(directory, rows, report, extra):
    def audio(src, loop=False):
        return f"<audio controls preload='none' {'loop ' if loop else ''}src='{html.escape(src)}'></audio>"

    def metrics(r):
        bands = " / ".join(f"{v:+.0f}" for v in r["octave_bands_db"].values())
        return (f"{r['seconds']:.2f} s ・ 短時間 {r['short_term_lufs']:.1f} LUFS ・ 実効 {r['effective_lufs']:.1f} LUFS（音量 {r['volume']}）"
                f" ・ トゥルーピーク {r['true_peak_dbfs']:.1f} dBTP ・ 重心 {r['centroid_hz']} Hz<br>"
                f"<small>オクターブ帯 63 Hz〜16 kHz（最大帯比 dB）: {bands} ・ 平坦度 {r['spectral_flatness']}"
                f" ・ ピーク {r['peak_ms']} ms ・ -20 dB 減衰 {r['decay_20db_ms']} ms</small>")

    blocks = []
    for name, clips in rows:
        r = report[name]
        body = [f"<h2>{html.escape(name)}</h2><p>{html.escape(CUES[name].description)}</p>",
                "<table>",
                f"<tr><td><b>新</b> {html.escape(name)}</td><td>{audio(clips['new'])}</td><td>{metrics(r)}</td></tr>"]
        if "old" in clips:
            old = extra[name]["legacy"]
            body.append(f"<tr><td><b>旧</b> DollSummon</td><td>{audio(clips['old'])}</td><td>{metrics(old)}</td></tr>")
            body.append(f"<tr><td><b>A/B</b> 旧→新→旧→新</td><td>{audio(clips['ab'])}</td><td><small>ゲーム内の音量（旧 .65、新 {CUES[name].volume}）で並べています。</small></td></tr>")
        body.append("</table>")
        bgm_rows = [(bgm, label) for bgm, *_, label in BGM if f"bgm-{bgm}" in clips]
        if bgm_rows:
            body.append("<h3>BGM の中で</h3><table>")
            for bgm, label in bgm_rows:
                e = extra[name][bgm]
                times = "、".join(f"{t:g}" for t in e["cue_times"])
                body.append(f"<tr><td>{html.escape(label)}<br><small>{e['start']:.1f} 秒から {e['beats']} 拍（{e['bpm']:g} BPM）。"
                            f"{times} 秒に鳴らす。クリップしないよう3本とも同じだけ {-e['mix_trim_db']:.1f} dB 下げています。</small></td>"
                            f"<td>新 {audio(clips[f'bgm-{bgm}'])}<br>旧 {audio(clips[f'bgm-{bgm}-old'])}</td>"
                            f"<td>BGM だけ（ループ） {audio(clips[f'bed-{bgm}'], loop=True)}</td></tr>")
            body.append("</table>")
        blocks.append("\n".join(body))
    (directory / "index.html").write_text(f"""<!doctype html><html lang='ja'><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'><title>人形の相棒 召喚音</title>
<style>body{{font:15px system-ui,'Yu Gothic UI',sans-serif;background:#15121a;color:#ece4dc;margin:24px;max-width:1100px}}
td{{padding:8px 12px;vertical-align:top;border-bottom:1px solid #2c2633}}small{{color:#a99fb0}}table{{border-collapse:collapse;width:100%}}
h1,h2,h3{{font-weight:600}}h2{{margin-top:1.6em;color:#eed9c4}}audio{{height:32px;width:280px}}p{{line-height:1.6}}</style>
<h1>人形の相棒 — 新しい召喚音</h1>
<p>変わるのは召喚の瞬間の音だけです（同じきっかけで1回鳴る）。旧 DollSummon と、ゲーム内の音量で聴き比べてください。
BGM の行は、実際の Doll の BGM（44.1 kHz に変換し、元の速さと音程のまま）に、ゲーム内の音量で重ねています（効果音と音楽の音量設定はどちらも 100%）。</p>
<p><small>調は全武器共通の F マイナー・ペンタトニック（F A♭ B♭ C E♭）で、BGM に合わせた音程の補正はしていません（0 セント）。
第3相の BGM はこの調の基準ですが、第1相は約2半音低い調で鳴っているので、そこでのなじみ方も確かめてください。</small></p>
{''.join(blocks)}
</html>""", encoding="utf-8", newline="\n")


# ---------------------------------------------------------------- attribution
def attribution_section(report, hashes):
    table = "\n".join(f"| {k} | {SOURCES[k][0].replace('repo:', '')} | {SOURCES[k][1]} | `{hashes[k]}` |" for k in sorted(hashes))
    head = f"""### Doll weapon audio foundation and companion summon — 2026-10-02

The shared audio basis of the Doll reward weapon refresh and the companion's new summon cue. [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns the windows, filters, pitches, gains, loudness targets and source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring, additive flue organ, shimmer, low thump); both reuse the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) unmodified. The two Kenney recordings are the CC0 1.0 files already recorded in the Ebon Manor reward audio table of this register; they stay in the local store, are SHA-256 verified before use and are not committed. The project-owned `DollSummon.wav` (recorded in this register as doll-theater-0253-dollsummon; current bytes from the 0.3.7 weapon articulation revision) is layered thinly at the start so the companion keeps its arrival character. Loudness follows the Ebon scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip. The audition page and report stay in the git-ignored `.local`.

| Key | Store or repository file | Source | Source SHA256 |
|---|---|---|---|
{table}
"""
    blocks = []
    for name, r in report.items():
        used = r["sources"]
        external = [k for k in used if not SOURCES[k][0].startswith("repo:")]
        masters = [Path(SOURCES[k][0]).stem for k in used if SOURCES[k][0].startswith("repo:")]
        authors = sorted({SOURCES[k][3] for k in external}, key=str.lower)
        creators = "".join((f"recordings by {' and '.join(authors)}; " if authors else "",
                            f"project master{'s' if len(masters) > 1 else ''} {', '.join(masters)} by Convergence; " if masters else "",
                            "synthesis and layering by Convergence with owner-directed Claude assistance"))
        blocks.append(f"""- Runtime file: `Assets/Sounds/Weapons/DollWeapons/{name}.ogg`
- Asset ID: doll-weapon-sfx-{name.lower()}-20261002
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue ({r['seconds']:.2f} s)
- Creator: {creators}
- Creation/acquisition date: 2026-10-02
- Source type: {'public-domain' if external else 'original'}
- Source work and URL: {', '.join(used) + ' in the table above as selected by the cue recipe; remaining layers original synthesis' if used else 'none; original NumPy synthesis'}
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy {np.__version__}, SciPy {__import__('scipy').__version__}, soundfile {sf.__version__}/libsndfile {sf.__libsndfile_version__} Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness {r['short_term_lufs']:.1f} LUFS (played at volume {r['volume']}: {r['effective_lufs']:.1f} LUFS effective), true peak {r['true_peak_dbfs']:.1f} dBFS; pinned Ogg serial
- License and redistribution terms: {'CC0 1.0 recordings and project-owned masters; the layered cue follows the existing project asset terms' if external else 'original project asset under the existing project terms'}
- Required attribution: {'none required by CC0; retain the table above as courtesy credit' if external else 'none; retain this provenance'}
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, length, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `{r['ogg_sha256']}`
""")
    return head + "\n" + "\n".join(blocks)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--store", type=Path, help="local recording store (default: $CONVERGENCE_AUDIO_STORE or the "
                                                    "nearest ancestor .claude-tools); never committed")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR, help="Ogg output folder")
    parser.add_argument("--preview", type=Path, help="write the owner audition page, WAVs and report here (.local)")
    parser.add_argument("--attribution-section", type=Path, help="write the Assets/ATTRIBUTION.md section here")
    parser.add_argument("--only", help="comma-separated cue names; each cue is seeded by its own name, so the bytes "
                                       "equal a full run")
    args = parser.parse_args()
    names = [n.strip() for n in args.only.split(",")] if args.only else list(CUES)
    unknown = [n for n in names if n not in CUES]
    if unknown:
        parser.error("unknown cue(s): " + ", ".join(unknown))
    store = Store(args.store or find_store(), ATTRIBUTION.read_text(encoding="utf-8"))
    args.output.mkdir(parents=True, exist_ok=True)
    samples, report = {}, {}
    for name in names:
        samples[name], used = render_cue(name, store, args.output)
        report[name] = analyse(name, samples[name], args.output / f"{name}.ogg", used)
    record = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__).read_bytes()),
              "dsp_sha256": sha256(Path(dsp.__file__).read_bytes()),
              "libraries": {"numpy": np.__version__, "scipy": __import__("scipy").__version__,
                            "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
              "sources": {k: {"path": SOURCES[k][0], "origin": SOURCES[k][1], "sha256": store.hashes[k]} for k in sorted(store.hashes)},
              "cues": report}
    if args.preview:
        record["audition"] = preview(args.preview, samples, report, store)
        (args.preview / "doll-weapon-sfx-report.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    if args.attribution_section:
        args.attribution_section.write_text(attribution_section(report, store.hashes), encoding="utf-8", newline="\n")
    print(json.dumps({k: {kk: v[kk] for kk in ("seconds", "short_term_lufs", "effective_lufs", "true_peak_dbfs", "peak_ms",
                                              "centroid_hz", "bytes")} for k, v in report.items()}, indent=1))


if __name__ == "__main__":
    main()
