"""Layer Ghost Samurai's attack cues from CC0 recordings.

Replaces the boss's borrowed vanilla sounds. Each cue plays on its hazard's
accepted Fire tick (the shout and chimes on their existing forecast ticks), so
the transient lands on the frame the pixel cut opens. Cues are lower and longer
than Soboro's because the boss's blades are several times larger. Recordings
are read from a local store (--sources) and verified by SHA-256; they are never
committed, and Assets/ATTRIBUTION.md lists their authors, pages and hashes.
Requires numpy, scipy and soundfile (local audio tools, not CI).
"""
import argparse
import json
from pathlib import Path

import numpy as np
import soundfile as sf

from sfx_layers import RATE, layer, load, metrics, render, sha256, thump, write_ogg

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Sounds" / "GhostSamurai"

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
    "zap": ("cc0/elec-FS512471-michael_grinnell-electric_zap.mp3",
            "https://freesound.org/s/512471/ (michael_grinnell, HQ preview)"),
    "war_cry": ("cc0/roar-FS521830-joelcarrsound-war_cry.mp3",
                "https://freesound.org/s/521830/ (joelcarrsound, HQ preview)"),
    "demon_howl": ("cc0/roar-FS257635-Bananaboatman33-demon_giant_howl.mp3",
                   "https://freesound.org/s/257635/ (Bananaboatman33, HQ preview)"),
    "metal_bowl": ("cc0/bell-FS405665-Anthousai-metal_bowl_hit.mp3",
                   "https://freesound.org/s/405665/ (Anthousai, HQ preview)"),
    "singing_bowl": ("cc0/bell-FS271370-inoshirodesign-singing_bowl.mp3",
                     "https://freesound.org/s/271370/ (inoshirodesign, HQ preview)"),
    "temple_bell": ("cc0/bell-FS131348-nahmandub-daitokuji_bell.mp3",
                    "https://freesound.org/s/131348/ (nahmandub, HQ preview)"),
    "wind_whirl": ("cc0/wind-FS719560-DARTEKZ_GAMEZ-wind_whirl.mp3",
                   "https://freesound.org/s/719560/ (DARTEKZ_GAMEZ, HQ preview)"),
    "air_cut": ("cc0/wind-FS60030-qubodup-air_cut.mp3",
                "https://freesound.org/s/60030/ (qubodup, HQ preview)"),
    "concrete_smash": ("cc0/impact-FS522099-magnuswaker-concrete_smash_2.mp3",
                       "https://freesound.org/s/522099/ (magnuswaker, HQ preview)"),
    "stone_crash": ("cc0/impact-FS711657-discofield-stone_crash.mp3",
                    "https://freesound.org/s/711657/ (discofield, HQ preview)"),
    "rock_tumble": ("cc0/impact-FS389618-_stubb-rock_tumble_2.mp3",
                    "https://freesound.org/s/389618/ (_stubb, HQ preview)"),
}

# name: (seconds, loudness target dB, layers); target is the K-weighted peak 100 ms level.
SCORE = {
    "SamuraiSlash": (0.46, -10.0, [
        layer("samurai_slash", 0.005, 0.300, at=0.000, gain_db=0.0, speed=0.84, hp=110, lp=9000, fade_out=0.12),
        layer("energy_wave", 0.150, 0.420, at=0.000, gain_db=-3.0, speed=0.80, hp=140, fade_in=0.006, fade_out=0.12),
        layer("stick_woosh", 0.100, 0.330, at=0.000, gain_db=-7.0, hp=70, lp=4000, fade_in=0.004, fade_out=0.10),
        layer("anime_shing", 0.060, 0.320, at=0.004, gain_db=-13.0, speed=0.94, hp=2200, fade_in=0.002, fade_out=0.14),
        thump(0.012, -11.0, 105, 52, 0.12),
    ]),
    "SamuraiGrid": (0.72, -9.0, [
        layer("samurai_slash", 0.005, 0.330, at=0.000, gain_db=-2.0, speed=0.9, hp=110, lp=9000, fade_out=0.14),
        layer("samurai_slash", 0.005, 0.300, at=0.034, gain_db=-7.0, speed=0.78, hp=110, lp=7000, fade_out=0.12),
        layer("energy_wave", 0.140, 0.420, at=0.010, gain_db=-4.0, speed=0.86, hp=160, fade_in=0.006, fade_out=0.12),
        layer("zap", 0.000, 0.220, at=0.006, gain_db=-11.0, hp=1500, lp=11000, fade_out=0.08),
        layer("anime_ring", 0.080, 0.700, at=0.050, gain_db=-12.0, hp=1600, lp=12000, fade_in=0.01, fade_out=0.30),
        thump(0.010, -10.0, 100, 48, 0.14),
    ]),
    "SamuraiWave": (0.92, -8.5, [
        layer("stick_woosh", 0.080, 0.480, at=0.000, gain_db=-1.0, speed=0.9, hp=60, lp=5000, fade_in=0.01, fade_out=0.16),
        layer("energy_wave", 0.130, 0.430, at=0.000, gain_db=-2.0, speed=0.74, hp=120, fade_in=0.008, fade_out=0.14),
        layer("samurai_slash", 0.005, 0.300, at=0.010, gain_db=-8.0, speed=0.82, hp=110, lp=8000, fade_out=0.12),
        layer("anime_ring", 0.080, 0.900, at=0.060, gain_db=-10.0, speed=0.94, hp=1400, lp=12000, fade_in=0.01,
              fade_out=0.36),
        thump(0.020, -7.0, 90, 44, 0.20),
    ]),
    "SamuraiCleave": (1.10, -7.5, [
        layer("stick_woosh", 0.060, 0.500, at=0.000, gain_db=0.0, speed=0.84, hp=55, lp=5000, fade_in=0.01, fade_out=0.18),
        layer("energy_wave", 0.120, 0.430, at=0.000, gain_db=-2.0, speed=0.68, hp=110, fade_in=0.008, fade_out=0.16),
        layer("samurai_slash", 0.005, 0.330, at=0.020, gain_db=-6.0, speed=0.78, hp=100, lp=8000, fade_out=0.14),
        layer("anime_shing", 0.040, 0.360, at=0.000, gain_db=-12.0, speed=0.9, hp=2000, fade_in=0.002, fade_out=0.14),
        layer("anime_ring", 0.080, 1.000, at=0.070, gain_db=-9.0, speed=0.9, hp=1300, lp=12000, fade_in=0.01,
              fade_out=0.42),
        layer("zap", 0.000, 0.220, at=0.030, gain_db=-15.0, hp=1600, lp=11000, fade_out=0.06),
        thump(0.030, -5.0, 82, 40, 0.26),
    ]),
    "SamuraiRush": (0.52, -10.0, [
        layer("swoosh", 1.050, 1.250, at=0.000, gain_db=0.0, speed=0.86, hp=90, lp=4500, fade_out=0.08),
        layer("energy_wave", 0.140, 0.420, at=0.000, gain_db=-3.0, speed=1.0, hp=180, fade_in=0.006, fade_out=0.12),
        layer("swish_short", 0.000, 0.110, at=0.030, gain_db=-8.0, speed=0.9, hp=150, fade_out=0.05),
        layer("samurai_slash", 0.005, 0.260, at=0.060, gain_db=-9.0, speed=0.9, hp=140, lp=8000, fade_out=0.12),
    ]),
    # the dash shout: a human war cry deepened by a demon howl and a short slap echo
    "SamuraiShout": (1.25, -10.0, [
        layer("war_cry", 0.040, 1.050, at=0.000, gain_db=0.0, speed=0.92, hp=120, lp=7000, fade_in=0.01, fade_out=0.30),
        layer("demon_howl", 0.020, 1.100, at=0.020, gain_db=-3.0, speed=0.82, hp=60, lp=4000, fade_in=0.01, fade_out=0.35),
        layer("demon_howl", 0.020, 0.800, at=0.110, gain_db=-13.0, speed=0.82, hp=200, lp=2500, fade_in=0.01,
              fade_out=0.30),
        thump(0.030, -12.0, 70, 40, 0.20),
    ]),
    # telegraph chime: a struck bowl ringing out, a quiet temple bell underneath
    "SamuraiChime": (1.60, -13.0, [
        layer("metal_bowl", 0.000, 0.500, at=0.000, gain_db=-2.0, hp=300, fade_in=0.001, fade_out=0.20),
        layer("singing_bowl", 0.030, 1.630, at=0.000, gain_db=0.0, hp=200, fade_in=0.002, fade_out=0.80),
        layer("temple_bell", 0.000, 1.600, at=0.000, gain_db=-7.0, hp=120, lp=6000, fade_in=0.002, fade_out=0.80),
    ]),
    # kamaitachi: a swirling gust brightened into wind blades
    "SamuraiWind": (0.76, -10.0, [
        layer("wind_whirl", 0.400, 1.100, at=0.000, gain_db=-2.0, hp=450, lp=12000, fade_in=0.02, fade_out=0.20,
              eq=(("high", 3000, 8.0, 0.7),)),
        layer("air_cut", 0.080, 0.430, at=0.000, gain_db=-5.0, hp=300, fade_in=0.004, fade_out=0.10),
        layer("air_cut", 0.080, 0.430, at=0.130, gain_db=-8.0, speed=1.12, hp=300, fade_in=0.004, fade_out=0.10),
        layer("energy_wave", 0.150, 0.420, at=0.020, gain_db=-2.0, speed=1.08, hp=1200, fade_in=0.006, fade_out=0.10),
        layer("energy_wave", 0.150, 0.420, at=0.150, gain_db=-5.0, speed=1.18, hp=1400, fade_in=0.006, fade_out=0.10),
    ]),
    # the ground shockwave: a boulder slam, debris and a rolling rubble tail over a sub thump
    "SamuraiShock": (1.00, -9.0, [
        layer("concrete_smash", 0.000, 0.950, at=0.000, gain_db=0.0, hp=40, lp=9000, fade_in=0.001, fade_out=0.35),
        layer("stone_crash", 0.000, 0.800, at=0.010, gain_db=-3.0, hp=150, fade_in=0.001, fade_out=0.30),
        layer("rock_tumble", 0.050, 0.800, at=0.120, gain_db=-9.0, hp=120, fade_in=0.01, fade_out=0.30),
        thump(0.000, -6.0, 70, 35, 0.30),
    ]),
}


# Attack cadences from the encounter rules (60 ticks = 1 s) with
# GhostSamuraiAudio's volumes and chime pitches. A listening aid, not a capture.
def sequences():
    def chimes(born, fire, gain=0.7):
        return [(born + k * (fire - born) / 3, "SamuraiChime", gain, k * .15) for k in range(3)]
    dash = []
    for k in range(3):
        fire = 24 + 78 + k * 141
        dash += [(fire - 78, "SamuraiShout", .9, 0), (fire, "SamuraiRush", .9, 0)]
    return {
        "directional": [(t, "SamuraiSlash", .9, 0) for t in (54, 66, 102, 114, 150, 162, 198, 210)],
        "charged": chimes(48, 180) + [(180, "SamuraiWave", 1, 0)] + chimes(240, 270) + [(270, "SamuraiWave", 1, 0)]
        + chimes(312, 360) + [(360, "SamuraiWave", 1, 0)],
        # the spec's 900 px example: grid fires at 84, the first wave at 108, followers 90/180 later
        "grid": chimes(0, 108) + [(84, "SamuraiGrid", .95, 0), (108, "SamuraiWave", 1, 0),
                                  (198, "SamuraiWave", 1, 0), (288, "SamuraiWave", 1, 0)],
        "dash": dash,
        "cleave": [(0, "SamuraiChime", .65, -.25), (24, "SamuraiChime", .8, .3), (96, "SamuraiChime", .65, .5),
                   (114, "SamuraiCleave", 1, 0), (150, "SamuraiShock", .8, 0)],
        "circle": [(36, "SamuraiSlash", .9, 0), (96, "SamuraiSlash", .9, 0), (156, "SamuraiWind", .9, 0),
                   (216, "SamuraiWind", .9, 0)],
    }


def sequence_mix(samples, events):
    from scipy import signal as _signal
    lead = 0.2
    end = max(tick / 60 + len(samples[cue]) / RATE for tick, cue, _, _ in events) + 0.3
    mix = np.zeros((round((lead + end) * RATE), 2))
    for tick, cue, gain, pitch in events:
        x = samples[cue]
        if pitch:
            up = round(1000 / 2 ** pitch)
            x = _signal.resample_poly(x, up, 1000, axis=0)
        start = round((lead + tick / 60) * RATE)
        mix[start:start + len(x)] += x[:len(mix) - start] * gain
    peak = float(np.max(np.abs(mix)))
    return mix * min(1.0, 0.95 / max(peak, 1e-9))


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
    report = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__)),
              "layers_sha256": sha256(Path(__file__).with_name("sfx_layers.py")),
              "libraries": {"numpy": np.__version__, "scipy": __import__("scipy").__version__,
                            "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
              "sources": {key: {"path": SOURCES[key][0], "origin": SOURCES[key][1], "sha256": hashes[key]}
                          for key in sorted(needed)},
              "cues": {}}
    for name, entry in SCORE.items():
        master = render(entry, sources)
        target = args.output / f"{name}.ogg"
        decoded = write_ogg(target, master)
        sf.write(str(args.preview / f"{name}.wav"), master.astype(np.float32), RATE, subtype="PCM_16")
        report["cues"][name] = metrics(decoded) | {
            "ogg": target.relative_to(ROOT).as_posix() if target.is_relative_to(ROOT) else str(target),
            "ogg_sha256": sha256(target),
            "sources": sorted({part["source"] for part in entry[2] if "source" in part}),
        }
    samples = {name: sf.read(str(args.output / f"{name}.ogg"), always_2d=True, dtype="float64")[0] for name in SCORE}
    if all(cue in samples for events in sequences().values() for _, cue, _, _ in events):
        for name, events in sequences().items():
            sf.write(str(args.preview / f"Samurai-{name}.wav"), sequence_mix(samples, events).astype(np.float32),
                     RATE, subtype="PCM_16")
    for key in sorted(needed):
        if sha256(args.sources / SOURCES[key][0]) != hashes[key]:
            raise RuntimeError(f"Source changed during render: {key}")
    (args.preview / "samurai-sfx-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["cues"], indent=2))


if __name__ == "__main__":
    main()
