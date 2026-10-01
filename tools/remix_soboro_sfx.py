"""Layer Soboro's blade and hit cues from CC0 recordings.

The owner selected CC0 recordings for these masters on 2026-10-01. Each cue
layers trimmed, filtered recordings plus a small original sine body, then
matches the loudness of the cue it replaced. Recordings are read from a local
store (--sources) and verified by SHA-256; they are never committed, and
Assets/ATTRIBUTION.md lists their authors, pages and hashes. Timing follows the
accepted cut clock: a cue starts on the release tick, the blade is fastest about
62/52/100 ms later (steps 0/1/2) and the lightning holds until the live window
ends. Requires numpy, scipy and soundfile (local audio tools, not CI).
"""
import argparse
import json
from pathlib import Path

import numpy as np
import soundfile as sf

from sfx_layers import RATE, layer, load, metrics, render, sha256, thump, write_ogg
ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Sounds" / "Weapons" / "Soboro"

# Local store path and origin of every recording the score uses.
SOURCES = {
    "energy_wave": ("cc0/swing-FS724716-greyfeather-sword_slash_energy_wave.mp3",
                    "https://freesound.org/s/724716/ (greyfeather, HQ preview)"),
    "samurai_slash": ("cc0/swing-FS370204-nekoninja-samurai_slash.mp3",
                      "https://freesound.org/s/370204/ (nekoninja, HQ preview)"),
    "swoosh": ("cc0/swing-FS263595-PorkMuncher-swoosh.mp3",
               "https://freesound.org/s/263595/ (PorkMuncher, HQ preview)"),
    "stick_woosh": ("cc0/swing-FS352719-Dalesome-woosh_stick.mp3",
                    "https://freesound.org/s/352719/ (Dalesome, HQ preview)"),
    "swish_short": ("cc0/swishes/swish-10.wav",
                    "https://opengameart.org/content/swishes-sound-pack (artisticdude, swish-10.wav)"),
    "anime_shing": ("cc0/ring-FS529019-Euphrosyyn-anime_shing_sword_2.mp3",
                    "https://freesound.org/s/529019/ (Euphrosyyn, HQ preview)"),
    "anime_ring": ("cc0/ring-FS706204-xkeril-nice_anime_sword_hit.mp3",
                   "https://freesound.org/s/706204/ (xkeril, HQ preview)"),
    "sword_hit": ("cc0/metal-FS442769-qubodup-sword_hit.mp3",
                  "https://freesound.org/s/442769/ (qubodup, HQ preview)"),
    "armor_strike": ("cc0/metal-FS568170-Merrick079-sword_sound_1.mp3",
                     "https://freesound.org/s/568170/ (Merrick079, HQ preview)"),
    "bloody_blade": ("cc0/flesh-FS323526-Kreastricon62-bloody_blade_2.mp3",
                     "https://freesound.org/s/323526/ (Kreastricon62, HQ preview)"),
    "slashkut": ("cc0/flesh-FS35213-Abyssmal-slashkut.mp3",
                 "https://freesound.org/s/35213/ (Abyssmal, HQ preview)"),
    "chop": ("cc0/kenney-rpg/chop.ogg",
             "https://opengameart.org/content/50-rpg-sound-effects (Kenney, RPGsounds_Kenney.zip chop.ogg)"),
    "zap": ("cc0/elec-FS512471-michael_grinnell-electric_zap.mp3",
            "https://freesound.org/s/512471/ (michael_grinnell, HQ preview)"),
}


# name: (seconds, loudness target dB, layers). The loudness target is the
# K-weighted peak 100 ms level of the finished cue.
SCORE = {
    "CutDown": (0.30, -10.0, [
        layer("energy_wave", 0.150, 0.420, at=0.000, gain_db=0.0, hp=180, fade_in=0.006, fade_out=0.10),
        layer("samurai_slash", 0.005, 0.230, at=0.004, gain_db=-5.0, hp=140, lp=9000, fade_out=0.10),
        layer("swoosh", 0.020, 0.200, at=0.010, gain_db=-7.0, hp=110, lp=3000, fade_out=0.06),
        layer("anime_shing", 0.060, 0.260, at=0.000, gain_db=-14.0, hp=2500, fade_in=0.002, fade_out=0.12),
        layer("zap", 0.020, 0.160, at=0.018, gain_db=-17.0, hp=1800, lp=11000, fade_out=0.05),
    ]),
    "CutReverse": (0.26, -10.5, [
        layer("energy_wave", 0.150, 0.400, at=0.000, gain_db=0.0, speed=1.12, hp=200,
              fade_in=0.006, fade_out=0.09),
        layer("swoosh", 1.070, 1.240, at=0.004, gain_db=-9.0, speed=1.08, hp=130, lp=3200, fade_out=0.06),
        layer("swish_short", 0.000, 0.110, at=0.020, gain_db=-12.0, hp=150, fade_out=0.05),
        layer("anime_shing", 0.070, 0.230, at=0.000, gain_db=-15.0, speed=1.06, hp=2800,
              fade_in=0.002, fade_out=0.10),
        layer("zap", 0.060, 0.170, at=0.012, gain_db=-18.0, hp=2000, lp=11000, fade_out=0.04),
    ]),
    "CutHeavy": (0.72, -8.5, [
        layer("anime_shing", 0.040, 0.300, at=0.000, gain_db=-12.0, hp=2200, fade_in=0.002, fade_out=0.12),
        layer("stick_woosh", 0.040, 0.420, at=0.000, gain_db=-2.0, hp=70, lp=5000, fade_in=0.01,
              fade_out=0.14),
        layer("energy_wave", 0.100, 0.420, at=0.000, gain_db=-3.0, speed=0.86, hp=150,
              fade_in=0.01, fade_out=0.12),
        layer("samurai_slash", 0.005, 0.300, at=0.055, gain_db=-10.0, speed=0.92, hp=120, lp=8000,
              fade_out=0.12),
        layer("anime_ring", 0.080, 0.720, at=0.100, gain_db=-12.0, hp=1600, lp=12000, fade_in=0.01,
              fade_out=0.30),
        layer("zap", 0.000, 0.220, at=0.030, gain_db=-15.0, hp=1600, lp=11000, fade_out=0.06),
        thump(0.085, -8.0, 95, 48, 0.16),
    ]),
    "HitMetal": (0.34, -12.0, [
        layer("sword_hit", 0.000, 0.330, at=0.000, gain_db=-6.0, hp=900, lp=8000, fade_in=0.001, fade_out=0.16),
        layer("armor_strike", 0.065, 0.330, at=0.000, gain_db=0.0, hp=160, lp=11000, fade_in=0.002,
              fade_out=0.12),
        layer("chop", 0.042, 0.160, at=0.000, gain_db=-2.0, hp=200, lp=3000, fade_in=0.001, fade_out=0.05),
        thump(0.000, -9.0, 110, 60, 0.08),
    ]),
    "HitOrganic": (0.26, -12.5, [
        layer("bloody_blade", 0.135, 0.400, at=0.000, gain_db=0.0, hp=90, lp=9000, fade_in=0.002,
              fade_out=0.10),
        layer("slashkut", 0.060, 0.200, at=0.000, gain_db=-8.0, hp=800, lp=8000, fade_in=0.002,
              fade_out=0.07),
        layer("chop", 0.040, 0.200, at=0.000, gain_db=-4.0, hp=140, lp=3500, fade_in=0.001, fade_out=0.07),
        thump(0.005, -7.0, 120, 55, 0.09),
    ]),
}


def combo(samples):
    # Two ideal 60-tick combos: releases at 7/24/45 and native contact 3/3/6
    # ticks later, with DXOboroAudio's volumes. A cadence aid, not a game capture.
    strokes = (("CutDown", "HitOrganic", 7, 3, 0.72, 0.52),
               ("CutReverse", "HitMetal", 24, 3, 0.68, 0.52),
               ("CutHeavy", "HitMetal", 45, 6, 0.86, 0.62))
    lead = 0.15
    mix = np.zeros((round((lead + 2 + 1.0) * RATE), 2))
    for cycle in range(2):
        for cut, hit, release, contact, cut_gain, hit_gain in strokes:
            for cue, tick, gain in ((cut, release, cut_gain), (hit, release + contact, hit_gain)):
                x = samples[cue]
                start = round((lead + (cycle * 60 + tick) / 60) * RATE)
                mix[start:start + len(x)] += x * gain
    peak = float(np.max(np.abs(mix)))
    return mix * min(1.0, 0.95 / peak)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--sources", type=Path, required=True,
                        help="local store holding the cc0/ recordings (never committed)")
    parser.add_argument("--preview", type=Path, required=True, help="local WAV/report directory")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR)
    args = parser.parse_args()
    needed = {part["source"] for _, _, layers in SCORE.values() for part in layers if "source" in part}
    sources, hashes = {}, {}
    for key in sorted(needed):
        path = args.sources / SOURCES[key][0]
        if not path.is_file():
            parser.error(f"missing source recording: {path}")
        hashes[key] = sha256(path)
        sources[key] = load(path)
    args.preview.mkdir(parents=True, exist_ok=True)
    samples, report = {}, {"recipe": "tools/" + Path(__file__).name,
                           "recipe_sha256": sha256(Path(__file__)),
                           "layers_sha256": sha256(Path(__file__).with_name("sfx_layers.py")),
                           "libraries": {"numpy": np.__version__, "scipy": __import__("scipy").__version__,
                                         "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
                           "sources": {key: {"path": SOURCES[key][0], "origin": SOURCES[key][1],
                                             "sha256": hashes[key]} for key in sorted(needed)},
                           "cues": {}}
    for name in SCORE:
        master = render(SCORE[name], sources)
        target = args.output / f"{name}.ogg"
        samples[name] = write_ogg(target, master)
        sf.write(str(args.preview / f"{name}.wav"), master.astype(np.float32), RATE, subtype="PCM_16")
        report["cues"][name] = metrics(samples[name]) | {
            "ogg": target.relative_to(ROOT).as_posix() if target.is_relative_to(ROOT) else str(target),
            "ogg_sha256": sha256(target),
            "sources": sorted({part["source"] for part in SCORE[name][2] if "source" in part}),
        }
    sf.write(str(args.preview / "Soboro-combo.wav"), combo(samples).astype(np.float32), RATE, subtype="PCM_16")
    for key in sorted(needed):
        if sha256(args.sources / SOURCES[key][0]) != hashes[key]:
            raise RuntimeError(f"Source changed during render: {key}")
    (args.preview / "soboro-sfx-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["cues"], indent=2))


if __name__ == "__main__":
    main()
