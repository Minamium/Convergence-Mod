"""Layer Cathedral of the White Night's cues from CC0 recordings (SFX v2).

Replaces the seven 2026-09-26 NumPy-only ice/glass cues and the borrowed Doll layers.
Each cue has one meaning (ADR-free presentation change; AzureAudio owns the event
table). Liora's cues are clean crystal and blade, Vitrion's are low glass, chain and
roar, the chorus uses tuned bells (A minor, matching the White Night BGM), and the
lattice keeps the old Doll ChargeRush staccato as its core (owner pick, 2026-10-02).

Sources are resolved by key from a local store (--store, never committed) holding
  sfx-sources/cc0/...            Freesound / OpenGameArt CC0 recordings
  music/libs/VSCO-2-CE-1.1.0/... VSCO 2 Community Edition (CC0)
plus this repository's own Assets/Sounds (repo:) and deterministic original synthesis
(synth:). A ":rev" suffix reverses a source. Every used source is listed with its path,
origin and SHA-256 in the report; Assets/ATTRIBUTION.md records them per cue.
Requires numpy, scipy and soundfile (local audio tools, not CI).
"""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

from sfx_layers import RATE, layer, load, metrics, render, sha256, thump, write_ogg

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Sounds" / "AzureCathedral"
VSCO = Path("music/libs/VSCO-2-CE-1.1.0")
CC0 = Path("sfx-sources/cc0")
VSCO_ORIGIN = "VSCO 2 Community Edition 1.1.0, Versilian Studios, CC0 1.0 (https://versilian-studios.com/vsco-community/)"
PACKS = {
    "kenney-rpg": "https://opengameart.org/content/50-rpg-sound-effects (Kenney, CC0)",
    "swishes": "https://opengameart.org/content/swishes-sound-pack (artisticdude, CC0)",
    "starninjas-attack": "https://opengameart.org/content/sword-attacks (StarNinjas, CC0)",
    "starninjas-clash": "https://opengameart.org/content/sword-clashes (StarNinjas, CC0)",
}


class Store:
    """Key -> recording, in the same search order the audition harness used."""

    def __init__(self, store):
        self.store, self.index, self.cache, self.used = store, {}, {}, set()
        for root in (store / VSCO / "Miscellania Raw", store / VSCO / "Percussion"):
            for path in sorted(root.rglob("*.wav")):
                self.index.setdefault("vsco:" + path.stem, path)
        sounds = ROOT / "Assets" / "Sounds"
        for path in sorted(sounds.rglob("*")):
            if path.suffix.lower() in (".wav", ".ogg"):
                self.index.setdefault("repo:" + path.relative_to(sounds).with_suffix("").as_posix(), path)
        for path in sorted((store / CC0).rglob("*")):
            if path.suffix.lower() in (".wav", ".mp3", ".ogg", ".flac"):
                self.index.setdefault("cc0:" + path.stem, path)

    def __getitem__(self, key):
        if key in self.cache:
            return self.cache[key]
        base, rev = (key[:-4], True) if key.endswith(":rev") else (key, False)
        if base.startswith("synth:"):
            x = synth(base[6:])
        else:
            if base not in self.index:
                raise KeyError(f"missing source {base!r} under {self.store}")
            self.used.add(base)
            x = load(self.index[base])
        if rev:
            x = x[::-1].copy()
        self.cache[key] = x
        return x

    def origin(self, key):
        path = self.index[key]
        if key.startswith("repo:"):
            return "Convergence project asset " + path.relative_to(ROOT).as_posix()
        if key.startswith("vsco:"):
            return VSCO_ORIGIN + " " + path.relative_to(self.store / VSCO).as_posix()
        match = re.search(r"FS(\d+)-([^-]+)-", path.name)
        if match:
            return f"https://freesound.org/s/{match.group(1)}/ ({match.group(2)}, HQ preview)"
        pack = path.relative_to(self.store / CC0).parts[0]
        return f"{PACKS[pack]} {path.name}"


def _rng(name):
    seed = int.from_bytes(name.encode("utf-8")[:8].ljust(8, b"\0"), "little") % (2 ** 32)
    return np.random.default_rng(seed)


def synth(name):
    """Original deterministic support material; parameters are encoded in the name: kind_seconds."""

    kind, _, rest = name.partition("_")
    seconds = float(rest or 1.0)
    n = round(seconds * RATE)
    t = np.arange(n) / RATE
    rng = _rng(name)
    if kind == "riser":
        # band-limited noise whose centre sweeps 300 Hz -> 6 kHz with rising level
        noise = rng.standard_normal((n, 2))
        out = np.zeros_like(noise)
        hop = 1024
        for i in range(0, n, hop):
            u = min(1.0, i / max(1, n - 1))
            fc = 300 * (6000 / 300) ** u
            sos = signal.butter(2, [fc * 0.7, min(RATE / 2 - 100, fc * 1.4)], "bandpass", fs=RATE, output="sos")
            seg = noise[max(0, i - 2048):i + hop]
            out[i:i + hop] = signal.sosfilt(sos, seg, axis=0)[-min(hop, n - i):]
        return out * (t / seconds)[:, None] ** 1.6
    if kind == "shimmer":
        # cluster of high inharmonic partials with slow beating, stereo detuned
        out = np.zeros((n, 2))
        for k in range(14):
            f = 2400 * 2 ** rng.uniform(0, 2.2)
            for ch in range(2):
                ph = rng.uniform(0, 2 * math.pi)
                det = 1 + rng.uniform(-0.003, 0.003)
                out[:, ch] += np.sin(2 * math.pi * f * det * t + ph) * rng.uniform(0.3, 1.0)
        env = np.minimum(1, t / 0.02) * np.exp(-t / (seconds * 0.45))
        return out / 14 * env[:, None]
    if kind == "sub":
        f = 55 * np.exp(-t / (seconds * 0.6)) + 32
        body = np.sin(2 * math.pi * np.cumsum(f) / RATE) * np.minimum(1, t / 0.01) * np.exp(-t / (seconds * 0.4))
        return np.repeat(body[:, None], 2, axis=1)
    if kind == "noise":
        return rng.standard_normal((n, 2)) * np.exp(-t / (seconds * 0.3))[:, None]
    if kind == "drip":
        # a water drop: short upward-gliding sine blip
        out = np.zeros((n, 2))
        f = 900 + 1700 * (t / 0.05).clip(0, 1)
        blip = np.sin(2 * math.pi * np.cumsum(f) / RATE) * np.exp(-t / 0.018) * np.minimum(1, t / 0.0015)
        out += blip[:, None]
        return out
    raise ValueError(f"unknown synth: {name}")


def intro_score(base_layer, thump, src):
    from sfx_layers import render_layer
    import math

    import numpy as np

    _layer = base_layer


    class _Sources:
        def __getitem__(self, key):
            return src(key)


    def layer(source, start, end, lvl=None, **kw):
        """Harness layer; a window that overruns a recording stops at its end. lvl (dB RMS of the filtered
        cut, fades ignored) replaces gain_db so sustained textures can be balanced by absolute level."""
        end = min(end, len(src(source)) / 44100 - 0.003)
        spec = _layer(source, start, end, **kw)
        if lvl is not None:
            probe = dict(spec, gain_db=0.0, fade_in=0.001, fade_out=0.001)
            x = render_layer(probe, _Sources())
            spec["gain_db"] = lvl - 20 * math.log10(float(np.sqrt(np.mean(x ** 2))) + 1e-9)
        return spec


    IMPACT = "cc0:impact-FS522099-magnuswaker-concrete_smash_2"
    STONE = "cc0:impact-FS711657-discofield-stone_crash"
    ROCK = "cc0:impact-FS389618-_stubb-rock_tumble_2"
    LOWHIT = "cc0:impact-FS541029-AudioPapkin-very_low_impact"
    HOWL = "cc0:roar-FS257635-Bananaboatman33-demon_giant_howl"
    WARCRY = "cc0:roar-FS521830-joelcarrsound-war_cry"
    BOWL = "cc0:bell-FS271370-inoshirodesign-singing_bowl"
    TEMPLE = "cc0:bell-FS131348-nahmandub-daitokuji_bell"
    WHIRL = "cc0:wind-FS719560-DARTEKZ_GAMEZ-wind_whirl"
    WOOSH = "cc0:wind-FS683096-florianreichelt-woosh"
    SWOOSH = "cc0:swing-FS263595-PorkMuncher-swoosh"
    TB_C4 = "vsco:TB_hit_C4_v4_rr1"

    # equal-tempered ratios from the C4 file's strike partial (C5, 525 Hz), to tune bells onto A / D / E / C
    A3, C4, D4, E4, F4, G4, A4 = 0.8409, 1.0, 1.1225, 1.2599, 1.3348, 1.4983, 1.6818

    SCORE = {
        # Liora's floating ice cage: hairline cracks, the diagonal split, then the whole cage drops as glass.
        "PrisonBreak": (2.2, -8.0, [
            layer("vsco:chain_grind", 0.02, 0.30, at=0.00, gain_db=-8.0, speed=0.62, hp=500, lp=7000, fade_in=0.06, fade_out=0.08),
            layer("vsco:brick_scrape2", 0.08, 0.42, lvl=-36, at=0.00, speed=0.80, hp=300, lp=4000, fade_in=0.06, fade_out=0.08),
            layer("vsco:glass_break6", 0.00, 0.14, at=0.13, gain_db=-12.0, speed=1.15, hp=1500, fade_in=0.001, fade_out=0.05),
            layer("vsco:glass_break2", 0.06, 0.20, at=0.27, gain_db=-8.0, hp=1500, fade_in=0.001, fade_out=0.06),
            layer("vsco:glass_break3", 0.07, 0.77, at=0.40, gain_db=0.0, hp=900, fade_in=0.001, fade_out=0.30),
            layer("vsco:glass_break", 0.02, 0.68, at=0.40, gain_db=-3.0, speed=0.80, hp=500, fade_in=0.001, fade_out=0.30),
            layer("vsco:glass_break5", 0.03, 0.92, at=0.43, gain_db=-5.0, speed=0.65, hp=250, fade_in=0.001, fade_out=0.40),
            layer(IMPACT, 0.00, 0.80, at=0.40, gain_db=-7.0, speed=1.4, hp=150, lp=3500, fade_in=0.001, fade_out=0.25),
            layer(STONE, 0.00, 1.20, at=0.40, gain_db=-6.0, speed=1.3, hp=250, lp=6000, fade_in=0.001, fade_out=0.35),
            thump(0.40, -8.0, 150, 58, 0.40),
            # shards raining down after the split
            layer("vsco:glass_break7", 0.45, 1.28, lvl=-40, at=0.75, speed=1.10, hp=2500, fade_in=0.02, fade_out=0.50),
            layer("vsco:glass_break4", 0.30, 0.88, lvl=-43, at=1.00, speed=1.30, hp=2500, fade_in=0.02, fade_out=0.40),
            layer("vsco:glass_break5", 0.40, 0.92, lvl=-46, at=1.20, speed=1.50, hp=3000, fade_in=0.02, fade_out=0.30),
            layer("vsco:glass_break8", 0.50, 1.09, lvl=-48, at=1.45, speed=1.20, hp=3000, fade_in=0.02, fade_out=0.30),
            layer("vsco:sleighbell1_hit_quiet", 0.10, 0.85, lvl=-46, at=0.95, hp=3000, fade_in=0.003, fade_out=0.40),
            layer("vsco:sleighbell1_hit_3", 0.00, 0.99, lvl=-50, at=1.35, hp=3000, fade_in=0.003, fade_out=0.50),
            layer("vsco:vibraring_v1_rr2", 0.03, 2.30, lvl=-40, at=0.42, speed=1.29, hp=300, lp=6000, fade_in=0.002, fade_out=1.30),
        ]),
        # A column of light rises when Liora lifts the sword: bowed-cymbal swell, glock glissando, a bell that blooms.
        "SwordLight": (2.5, -11.0, [
            layer("vsco:susCymb1-cresc-Short_v1", 0.55, 2.10, lvl=-34, at=0.00, hp=300, lp=9000, fade_in=0.40, fade_out=0.50),
            layer("vsco:glock_fx_up_chromatic_fast_01", 0.00, 1.35, lvl=-33, at=0.00, fade_in=0.01, fade_out=0.30),
            layer("synth:riser_1.3", 0.00, 1.30, lvl=-32, at=0.00, hp=600, fade_in=0.05, fade_out=0.05),
            layer(TB_C4, 0.00, 3.10, lvl=-24, at=1.25, speed=E4 * 2, hp=400, fade_in=0.002, fade_out=1.00),
            layer(TB_C4, 0.00, 2.10, lvl=-28, at=1.27, speed=A4, hp=200, fade_in=0.002, fade_out=1.00),
            layer("vsco:BellTree_Stroke4_v1_Sum", 0.80, 3.00, lvl=-38, at=1.10, hp=2500, fade_in=0.01, fade_out=1.00),
            layer("vsco:Triangle6-HitM_v1_rr2_Sum", 0.00, 1.00, lvl=-44, at=1.25, fade_in=0.001, fade_out=0.60),
            thump(1.25, -15.0, 110, 55, 0.50),
        ]),
        # A giant dimensional tear: reversed crash suck-in, glass shreds backwards, rip, low surge.
        "RiftOpen": (2.0, -12.0, [
            layer("vsco:cymbal-crash1_mf_rr2:rev", 8.55, 9.62, at=0.00, gain_db=0.0, hp=250, fade_in=0.30, fade_out=0.02),
            layer("vsco:glass_break5:rev", 0.00, 0.92, at=0.10, gain_db=-3.0, speed=0.9, hp=400, fade_in=0.20, fade_out=0.02),
            layer("vsco:chain_grind:rev", 0.00, 1.00, at=0.05, gain_db=-5.0, speed=0.85, hp=400, lp=8000, fade_in=0.30, fade_out=0.02),
            layer("vsco:zap11:rev", 1.95, 3.00, lvl=-36, at=0.00, speed=1.05, hp=150, lp=5000, fade_in=0.40, fade_out=0.02),
            layer(WHIRL, 0.10, 1.40, gain_db=-6.0, at=0.00, speed=0.55, hp=120, lp=1200, fade_in=0.70, fade_out=0.50),
            layer("cc0:drawKnife3", 0.00, 0.48, at=0.98, gain_db=-1.0, speed=0.60, hp=200, fade_in=0.002, fade_out=0.15),
            layer("cc0:knifeSlice", 0.05, 0.60, at=1.00, gain_db=-4.0, speed=0.75, hp=600, fade_in=0.002, fade_out=0.25),
            layer("vsco:glass_break7", 0.05, 1.28, at=1.00, gain_db=-3.0, speed=0.55, hp=300, fade_in=0.002, fade_out=0.50),
            layer("vsco:metal_hit9", 0.07, 1.50, at=1.00, gain_db=-5.0, speed=0.7, hp=100, lp=5000, fade_in=0.002, fade_out=0.50),
            layer("vsco:zap12", 0.10, 0.80, lvl=-32, at=0.98, speed=0.75, hp=150, lp=6000, fade_in=0.002, fade_out=0.50),
            layer(LOWHIT, 0.60, 1.91, at=0.88, gain_db=-13.0, hp=35, fade_in=0.002, fade_out=0.50),
            thump(1.00, -14.0, 90, 38, 0.80),
            layer("vsco:bassdrum_rub3_v1", 0.60, 2.60, lvl=-42, at=0.00, lp=320, fade_in=1.00, fade_out=0.50),
        ]),
        # The leviathan's head breaks through: deep howl, glass plates groaning, sub underneath.
        "WormArrival": (3.0, -8.0, [
            layer(HOWL, 0.03, 0.75, lvl=-22, at=0.05, speed=0.55, hp=80, lp=3500, fade_in=0.06, fade_out=0.50),
            layer(HOWL, 0.03, 0.75, lvl=-24, at=0.90, speed=0.40, hp=60, lp=3000, fade_in=0.15, fade_out=0.90),
            layer(WARCRY, 0.04, 1.20, lvl=-27, at=0.35, speed=0.45, hp=100, lp=3500, fade_in=0.10, fade_out=1.20),
            layer("vsco:chain_grind", 0.02, 1.00, lvl=-30, at=0.00, speed=0.55, hp=250, lp=6000, fade_in=0.02, fade_out=0.50),
            layer("vsco:brick_scrape", 0.00, 1.14, lvl=-38, at=0.35, speed=0.65, hp=150, lp=3500, fade_in=0.02, fade_out=0.50),
            layer("vsco:gongscrape_mf", 1.30, 3.20, lvl=-40, at=0.60, speed=0.70, hp=100, lp=4000, fade_in=0.10, fade_out=0.80),
            layer("vsco:glass_break8", 0.02, 1.09, at=0.00, gain_db=-2.0, speed=0.55, hp=250, lp=6000, fade_in=0.001, fade_out=0.50),
            layer(STONE, 0.00, 1.40, at=0.00, gain_db=-7.0, speed=0.70, hp=100, lp=5000, fade_in=0.001, fade_out=0.40),
            layer(LOWHIT, 0.85, 1.91, at=0.10, gain_db=-9.0, speed=0.60, hp=30, fade_in=0.002, fade_out=1.00),
            layer("vsco:gongHit_p", 0.00, 3.00, lvl=-40, at=0.00, hp=60, fade_in=0.01, fade_out=1.50),
            thump(0.10, -10.0, 70, 34, 1.80),
        ]),
        # Liora falls: the glass sword cracks, a crystalline sigh, fine shards, a soft weight on the floor.
        "LioraFall": (2.0, -11.0, [
            layer("vsco:glass_break2", 0.06, 0.40, at=0.00, gain_db=-1.0, speed=1.0, hp=1200, fade_in=0.001, fade_out=0.15),
            layer("vsco:glass_break6", 0.00, 0.60, at=0.00, gain_db=-6.0, speed=0.85, hp=700, fade_in=0.001, fade_out=0.25),
            layer("vsco:metal_hit11", 0.00, 0.80, at=0.00, gain_db=-9.0, speed=0.9, hp=300, lp=6000, fade_in=0.001, fade_out=0.35),
            layer(BOWL, 0.03, 2.00, at=0.12, gain_db=0.0, speed=1.26, hp=200, lp=8000, fade_in=0.15, fade_out=1.00),
            layer(WOOSH, 0.30, 1.40, at=0.15, gain_db=-18.0, speed=1.0, hp=400, lp=2200, fade_in=0.25, fade_out=0.60),
            layer("vsco:glock_fx_down_chromatic_fast_01", 0.40, 1.90, lvl=-40, at=0.25, hp=800, fade_in=0.01, fade_out=0.80),
            layer("vsco:glass_break4", 0.10, 0.88, lvl=-44, at=0.50, speed=1.2, hp=2500, fade_in=0.01, fade_out=0.50),
            layer("vsco:glass_break7", 0.30, 1.28, lvl=-46, at=0.80, speed=1.15, hp=3000, fade_in=0.01, fade_out=0.60),
            layer("vsco:sleighbell1_hit_quiet", 0.10, 0.85, lvl=-50, at=1.05, hp=3000, fade_in=0.003, fade_out=0.40),
            layer(IMPACT, 0.00, 0.60, at=0.30, gain_db=-17.0, speed=1.6, hp=100, lp=600, fade_in=0.002, fade_out=0.25),
            thump(0.30, -16.0, 110, 52, 0.30),
        ]),
        # The worm is stopped and withdraws offscreen: a long low groan with creaking glass dying away.
        "WormRetreat": (2.0, -12.0, [
            layer(HOWL, 0.03, 0.75, lvl=-24, at=0.00, speed=0.42, hp=90, lp=2200, fade_in=0.05, fade_out=1.20),
            layer("vsco:gongscrape_pp", 0.20, 1.40, lvl=-38, at=0.00, speed=0.65, hp=80, lp=2500, fade_in=0.10, fade_out=1.00),
            layer("vsco:chain_grind", 0.02, 1.00, at=0.10, gain_db=-8.0, speed=0.50, hp=200, lp=4000, fade_in=0.05, fade_out=0.80),
            layer("vsco:brick_scrape", 0.00, 0.90, lvl=-42, at=0.45, speed=0.60, hp=150, lp=3000, fade_in=0.05, fade_out=0.80),
            layer("vsco:bassdrum_rub1_v1", 1.20, 3.10, lvl=-44, at=0.00, lp=300, fade_in=0.10, fade_out=1.00),
            layer(LOWHIT, 0.85, 1.91, at=0.00, gain_db=-12.0, speed=0.85, hp=30, fade_in=0.002, fade_out=0.50),
            thump(0.00, -16.0, 60, 32, 0.80),
        ]),
        # The worm rushes in from afar: low rumble and wind pressure that swell toward the end.
        "DevourRush": (2.0, -10.0, [
            # stage A: slow bed over the whole cue
            layer("vsco:ambience1", 0.20, 2.20, lvl=-32, lp=450, fade_in=1.90, fade_out=0.05),
            layer("vsco:bassdrum_rub3_v1", 7.40, 9.40, lvl=-34, lp=450, fade_in=1.90, fade_out=0.05),
            layer("vsco:gongscrape_mf", 1.90, 3.90, lvl=-34, hp=100, lp=3500, fade_in=1.90, fade_out=0.05),
            layer(WHIRL, 0.10, 1.40, lvl=-34, at=0.10, speed=0.70, hp=200, lp=3000, fade_in=1.70, fade_out=0.05),
            layer(WOOSH + ":rev", 0.00, 1.03, lvl=-32, at=0.10, speed=0.55, hp=150, lp=2500, fade_in=0.50, fade_out=0.05),
            # stage B: a second wave from 0.8 s
            layer("vsco:ambience1", 2.40, 3.60, lvl=-26, at=0.80, lp=450, fade_in=1.00, fade_out=0.05),
            layer("vsco:gongscrape_mf", 4.50, 5.70, lvl=-28, at=0.80, hp=100, lp=3500, fade_in=1.00, fade_out=0.05),
            layer("vsco:susCymb1-bow-1", 9.70, 10.90, lvl=-30, at=0.80, hp=200, lp=6000, fade_in=1.00, fade_out=0.05),
            # stage C: the final surge
            layer("vsco:bassdrum_rub2_v1", 2.40, 3.05, lvl=-22, at=1.35, lp=500, fade_in=0.55, fade_out=0.05),
            layer("vsco:susCymb1-bow-3", 7.40, 8.05, lvl=-28, at=1.35, hp=200, lp=6000, fade_in=0.55, fade_out=0.05),
            layer("vsco:chaingrindLoop", 1.00, 1.65, lvl=-30, at=1.35, speed=0.8, hp=200, lp=5000, fade_in=0.55, fade_out=0.05),
            layer("synth:riser_2.0", 0.00, 2.00, lvl=-31, hp=400, lp=5000, fade_in=0.10, fade_out=0.05),
        ]),
        # The bite lands: glass crushed under an enormous weight, sub, shards, then near silence.
        "DevourBite": (2.5, -7.0, [
            layer("vsco:glass_break3", 0.05, 0.77, at=0.00, gain_db=-1.0, speed=0.60, hp=300, fade_in=0.001, fade_out=0.35),
            layer("vsco:glass_break5", 0.03, 0.92, at=0.00, gain_db=-2.0, speed=0.50, hp=200, fade_in=0.001, fade_out=0.50),
            layer("vsco:glass_break7", 0.05, 1.28, at=0.04, gain_db=-3.0, speed=0.70, hp=300, fade_in=0.001, fade_out=0.60),
            layer(IMPACT, 0.00, 1.20, at=0.00, gain_db=-3.0, speed=0.75, hp=80, lp=6000, fade_in=0.001, fade_out=0.50),
            layer(STONE, 0.00, 1.50, at=0.01, gain_db=0.0, speed=0.75, hp=120, fade_in=0.001, fade_out=0.50),
            layer(ROCK, 0.00, 1.20, at=0.03, gain_db=-4.0, speed=0.75, hp=100, fade_in=0.001, fade_out=0.45),
            layer("vsco:metal_hit7", 0.00, 2.00, at=0.00, gain_db=-9.0, speed=0.8, hp=80, lp=4000, fade_in=0.001, fade_out=0.60),
            layer(LOWHIT, 0.75, 1.91, at=0.00, gain_db=-8.0, hp=28, fade_in=0.001, fade_out=0.40),
            layer("vsco:BDrumNewhit_v7_rr1_Sum", 0.00, 1.20, at=0.00, gain_db=-12.0, hp=30, lp=400, fade_in=0.001, fade_out=0.50),
            layer(HOWL, 0.03, 1.00, at=0.00, gain_db=-11.0, speed=0.60, hp=100, lp=3000, fade_in=0.002, fade_out=0.40),
            thump(0.00, -10.0, 90, 34, 0.70),
            layer("vsco:glass_break4", 0.10, 0.88, lvl=-46, at=0.50, speed=0.9, hp=1500, fade_in=0.01, fade_out=0.40),
            layer("vsco:vibraring_v1_rr1", 0.05, 1.60, lvl=-52, at=0.70, speed=A4 * 0.75, hp=200, fade_in=0.20, fade_out=1.00),
        ]),
        # Armour of dark indigo spreads head to tail: awakening roar, rising crystal glitter, low weight.
        "FuryAwaken": (3.0, -8.0, [
            layer(LOWHIT, 0.85, 1.91, at=0.00, gain_db=-9.0, hp=30, fade_in=0.002, fade_out=0.40),
            layer(WARCRY, 0.04, 1.90, at=0.10, gain_db=-1.0, speed=0.70, hp=100, lp=6000, fade_in=0.08, fade_out=0.60),
            layer(HOWL, 0.03, 1.80, at=0.25, gain_db=-6.0, speed=0.65, hp=80, lp=3500, fade_in=0.10, fade_out=0.80),
            layer(HOWL, 0.03, 0.95, lvl=-27, at=1.20, speed=0.50, hp=70, lp=3000, fade_in=0.10, fade_out=1.00),
            layer("vsco:glass_break6", 0.00, 0.77, at=0.00, gain_db=-5.0, speed=0.65, hp=300, fade_in=0.001, fade_out=0.40),
            layer("vsco:glock_fx_up_chromatic_med_01", 0.00, 2.60, lvl=-34, at=0.40, hp=900, fade_in=0.05, fade_out=0.50),
            layer("vsco:BellTree_Stroke1_v1_Sum", 0.00, 2.60, lvl=-36, at=0.30, hp=2500, fade_in=0.20, fade_out=0.50),
            layer("synth:shimmer_2.2", 0.00, 2.20, lvl=-30, at=0.70, fade_in=0.50, fade_out=0.60),
            layer(TB_C4, 0.00, 3.00, at=1.30, gain_db=-8.0, speed=D4 * 0.5, hp=60, fade_in=0.002, fade_out=1.00),
            layer("vsco:gongHit_p", 0.00, 3.00, lvl=-42, at=0.00, hp=50, fade_in=0.01, fade_out=1.20),
            thump(0.00, -10.0, 80, 36, 1.00),
        ]),
        # The killing blow on the armoured worm: dead-weight hit, cracks racing through the body, a high held ring.
        "FinalBlow": (2.5, -7.0, [
            layer(LOWHIT, 0.95, 1.91, at=0.00, gain_db=-6.0, hp=28, fade_in=0.001, fade_out=0.40),
            layer(IMPACT, 0.00, 1.20, at=0.00, gain_db=-4.0, speed=0.90, hp=80, lp=6000, fade_in=0.001, fade_out=0.45),
            layer(STONE, 0.00, 1.40, at=0.01, gain_db=-3.0, speed=0.85, hp=120, fade_in=0.001, fade_out=0.40),
            layer("vsco:glass_break3", 0.05, 0.77, at=0.00, gain_db=0.0, speed=0.70, hp=300, fade_in=0.001, fade_out=0.30),
            layer("vsco:glass_break7", 0.05, 1.28, at=0.12, gain_db=-2.0, speed=0.85, hp=400, fade_in=0.001, fade_out=0.35),
            layer("vsco:glass_break2", 0.05, 0.65, at=0.26, gain_db=-3.0, speed=0.95, hp=500, fade_in=0.001, fade_out=0.30),
            layer("vsco:glass_break8", 0.05, 1.09, at=0.42, gain_db=-4.0, speed=1.0, hp=600, fade_in=0.001, fade_out=0.35),
            layer("vsco:glass_break6", 0.05, 0.77, at=0.60, gain_db=-5.0, speed=1.1, hp=900, fade_in=0.001, fade_out=0.30),
            layer("vsco:glass_break4", 0.05, 0.88, at=0.80, gain_db=-7.0, speed=1.2, hp=1500, fade_in=0.001, fade_out=0.40),
            layer(BOWL, 0.03, 3.00, lvl=-31, at=0.05, speed=1.26, hp=200, fade_in=0.50, fade_out=1.00),
            layer(BOWL, 0.03, 5.90, lvl=-36, at=0.05, speed=2.52, hp=400, fade_in=0.60, fade_out=1.00),
            layer(HOWL, 0.03, 1.00, at=0.00, gain_db=-13.0, speed=0.60, hp=100, lp=3000, fade_in=0.002, fade_out=0.40),
            thump(0.00, -12.0, 85, 34, 0.80),
        ]),
        # Hostility is gone: a gentle swell toward the centre, soft wind and a low sway, easing off.
        "MeltRush": (1.6, -12.0, [
            layer(WHIRL, 0.10, 1.40, at=0.00, gain_db=-1.0, speed=0.75, hp=120, lp=1800, fade_in=0.75, fade_out=0.50),
            layer(SWOOSH, 0.40, 1.80, at=0.10, gain_db=-5.0, speed=0.80, hp=100, lp=2500, fade_in=0.70, fade_out=0.50),
            layer("vsco:bassdrum_rub4_v1", 0.50, 2.20, at=0.00, gain_db=-8.0, lp=300, fade_in=0.80, fade_out=0.40),
            layer("vsco:vibraring_v1_rr1", 0.05, 1.60, lvl=-40, at=0.50, speed=A4 * 0.75, hp=200, fade_in=0.25, fade_out=0.60),
            layer("vsco:bubbles4", 0.50, 1.50, lvl=-46, at=0.40, hp=200, lp=3000, fade_in=0.30, fade_out=0.50),
            layer("vsco:Triangle6-HitFM_v1_rr1_Sum", 0.00, 1.30, lvl=-52, at=0.60, fade_in=0.20, fade_out=0.50),
        ]),
        # Glass goes soft and sags: a slow slumping sigh, frost creaking, first drops, a settling bowl.
        "MeltContact": (3.0, -10.0, [
            layer("vsco:glass_break7", 0.05, 1.28, at=0.00, gain_db=-3.0, speed=0.50, hp=200, lp=4500, fade_in=0.08, fade_out=0.90),
            layer("vsco:glass_break4", 0.05, 0.88, at=0.18, gain_db=-6.0, speed=0.55, hp=200, lp=4000, fade_in=0.05, fade_out=0.60),
            layer(WHIRL, 0.10, 1.40, at=0.00, gain_db=-10.0, speed=0.60, hp=300, lp=1500, fade_in=0.50, fade_out=1.00),
            layer(BOWL, 0.03, 3.00, at=0.00, gain_db=-4.0, speed=1.26 * 0.5, hp=120, lp=5000, fade_in=0.05, fade_out=1.40),
            layer(TEMPLE, 0.00, 3.00, at=0.05, gain_db=-8.0, speed=1.025 * 0.5, hp=100, lp=4000, fade_in=0.01, fade_out=1.40),
            layer("vsco:chain_grind", 0.02, 1.00, at=0.50, gain_db=-12.0, speed=0.7, hp=1500, lp=7000, fade_in=0.05, fade_out=0.40),
            layer("vsco:brick_scrape2", 0.08, 1.10, lvl=-46, at=0.90, speed=0.6, hp=800, lp=3000, fade_in=0.05, fade_out=0.50),
            layer("vsco:bubbles2", 0.15, 2.60, lvl=-44, at=0.40, hp=150, lp=3500, fade_in=0.05, fade_out=1.0),
            layer("synth:drip_0.3", 0.00, 0.30, at=0.70, gain_db=-17.0, speed=1.00, fade_in=0.001, fade_out=0.05),
            layer("synth:drip_0.3", 0.00, 0.30, at=1.35, gain_db=-19.0, speed=0.84, fade_in=0.001, fade_out=0.05),
            thump(0.00, -16.0, 80, 40, 0.80),
        ]),
        # The melting floor: drops and tiny creaks, spaced irregularly across the whole body melting.
        "ChainMelt": (3.0, -14.0, [
            layer("vsco:bubbles", 0.15, 2.80, lvl=-40, at=0.00, hp=150, lp=3500, fade_in=0.20, fade_out=0.70),
            layer("vsco:bubbles4", 0.05, 2.10, lvl=-44, at=0.80, speed=1.1, hp=200, lp=4000, fade_in=0.10, fade_out=0.70),
            layer("synth:drip_0.3", 0.00, 0.30, at=0.20, gain_db=-12.0, speed=1.00, fade_in=0.001, fade_out=0.05),
            layer("synth:drip_0.3", 0.00, 0.30, at=0.65, gain_db=-14.0, speed=1.19, fade_in=0.001, fade_out=0.05),
            layer("synth:drip_0.3", 0.00, 0.30, at=1.10, gain_db=-13.0, speed=0.89, fade_in=0.001, fade_out=0.05),
            layer("synth:drip_0.3", 0.00, 0.30, at=1.70, gain_db=-15.0, speed=1.33, fade_in=0.001, fade_out=0.05),
            layer("synth:drip_0.3", 0.00, 0.30, at=2.15, gain_db=-17.0, speed=0.75, fade_in=0.001, fade_out=0.05),
            layer("vsco:chain_grind", 0.02, 0.50, at=0.35, gain_db=-12.0, speed=0.9, hp=1800, lp=7000, fade_in=0.01, fade_out=0.20),
            layer("vsco:chain_grind", 0.40, 0.95, at=1.45, gain_db=-14.0, speed=0.8, hp=1500, lp=6000, fade_in=0.01, fade_out=0.25),
            layer("vsco:brick_scrape2", 0.08, 0.80, lvl=-46, at=0.95, speed=0.7, hp=500, lp=3500, fade_in=0.02, fade_out=0.30),
            layer("vsco:glass_break7", 0.30, 1.20, lvl=-50, at=1.90, speed=0.6, hp=300, lp=3000, fade_in=0.05, fade_out=0.60),
            layer("vsco:bassdrum_rub4_v1", 1.00, 4.00, lvl=-46, at=0.00, lp=250, fade_in=0.80, fade_out=0.90),
        ]),
        # "The glass falls silent": descending bell line in A minor, bell-tree glitter, a long quiet tail.
        "Victory": (5.0, -11.0, [
            layer(TB_C4, 0.00, 2.40, at=0.00, gain_db=-3.0, speed=E4 * 2, hp=300, fade_in=0.002, fade_out=1.20),
            layer(TB_C4, 0.00, 2.60, at=0.55, gain_db=-4.0, speed=C4 * 2, hp=300, fade_in=0.002, fade_out=1.20),
            layer(TB_C4, 0.00, 3.00, at=1.10, gain_db=-4.0, speed=A4, hp=200, fade_in=0.002, fade_out=1.50),
            layer(TB_C4, 0.00, 3.40, at=1.70, gain_db=-4.0, speed=E4, hp=100, fade_in=0.002, fade_out=1.80),
            layer(TB_C4, 0.00, 2.20, at=2.10, gain_db=-3.0, speed=A3, hp=60, fade_in=0.002, fade_out=2.20),
            # glitter: bell-tree strokes, a glock tail on A6, scattered chimes on E / D / A
            layer("vsco:BellTree_Stroke2_v1_Sum", 0.00, 4.50, lvl=-38, at=0.00, hp=2500, fade_in=0.01, fade_out=2.00),
            layer("vsco:glock_fx_down_chromatic_fast_01", 0.30, 5.00, lvl=-42, at=0.00, hp=800, fade_in=0.01, fade_out=1.80),
            layer(BOWL, 0.03, 5.20, lvl=-37, at=0.10, speed=1.26, hp=200, fade_in=0.30, fade_out=3.00),
            layer("vsco:Triangle6-Hit_v1_rr2_Sum", 0.00, 2.60, lvl=-46, at=0.60, fade_in=0.001, fade_out=1.50),
            layer("vsco:sleighbell1_hit_quiet", 0.10, 0.85, lvl=-50, at=1.80, hp=3000, fade_in=0.003, fade_out=0.40),
            layer("vsco:glock_medium_G5", 0.00, 2.20, lvl=-44, at=2.80, speed=1.1225, hp=300, fade_in=0.002, fade_out=1.20),
            layer("vsco:glock_medium_C6", 0.00, 2.40, lvl=-47, at=3.10, speed=1.26, hp=300, fade_in=0.002, fade_out=1.10),
            layer("vsco:glock_medium_G5", 0.00, 2.20, lvl=-49, at=3.50, speed=0.835, hp=300, fade_in=0.002, fade_out=0.90),
            layer("vsco:sleighbell1_hit_3", 0.00, 0.99, lvl=-54, at=3.30, hp=3500, fade_in=0.003, fade_out=0.50),
            layer("vsco:glock_medium_C6", 0.00, 2.40, lvl=-52, at=3.90, speed=1.1225, hp=300, fade_in=0.002, fade_out=0.70),
            thump(0.00, -14.0, 110, 55, 0.50),
        ]),
    }
    return SCORE


def liora_score(layer, thump, src):
    D, E, F, G, A = 1.122, 1.26, 1.335, 1.498, 1.682
    VIB = 1.0234

    CRESC_S = "vsco:susCymb1-cresc-Short_v1"      # swells -44 dB -> 0 dB, peak at 1.52 s
    CRESC_M = "vsco:susCymb1-cresc-Median_v1"     # swells to a peak at 3.6 s
    BOW2 = "vsco:susCymb1-bow-2"                  # bowed cymbal, swell to 1.1 s
    BOW3 = "vsco:susCymb1-bow-3"                  # long bowed cymbal, plateau 1-6.5 s then decay
    VIBR = "vsco:vibraring3:rev"                  # vibraring reversed = F-ish swell into the cut
    GDOWN_REV = "vsco:glock_fx_down_chromatic_fast_03:rev"  # reversed descending gliss = rising swell
    TRI6ROLL = "vsco:Triangle6-Roll_v2_rr1_Sum"
    TRI3ROLL = "vsco:Triangle3-Roll_v2_rr1_Sum"
    SHING = "cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2"
    RING = "cc0:ring-FS706204-xkeril-nice_anime_sword_hit"
    AIRCUT = "cc0:wind-FS60030-qubodup-air_cut"
    ENERGY = "cc0:swing-FS724716-greyfeather-sword_slash_energy_wave"
    HIT442 = "cc0:metal-FS442769-qubodup-sword_hit"
    SAMURAI = "cc0:swing-FS370204-nekoninja-samurai_slash"
    SLASHKUT = "cc0:flesh-FS35213-Abyssmal-slashkut"


    def sparkles(times, gains, notes, srcs, length=0.16):
        """Short crystalline grains that accelerate and swell (gathering glitter)."""
        out = []
        for i, (t, g, n) in enumerate(zip(times, gains, notes)):
            src = srcs[i % len(srcs)]
            out.append(layer(src, 0.0, length, at=t, gain_db=g, speed=n, hp=1500, fade_in=0.001, fade_out=0.09))
        return out


    SCORE = {
        # ---------------------------------------------------------------- icicle fan
        "FanCharge": (1.0, -16.0, [
            layer(CRESC_S, 0.45, 1.45, at=0.0, gain_db=-2.0, hp=350, fade_in=0.25, fade_out=0.02),
            layer(BOW2, 0.10, 1.10, at=0.0, gain_db=-5.0, speed=E, hp=500, fade_in=0.30, fade_out=0.03),
            layer(VIBR, 1.15, 2.14, at=0.0, gain_db=-5.0, speed=VIB, hp=150, fade_in=0.20, fade_out=0.02),
            layer(GDOWN_REV, 5.85, 6.78, at=0.0, gain_db=-8.0, hp=900, fade_in=0.05, fade_out=0.02),
            layer("synth:riser_1.0", 0.0, 0.99, at=0.0, gain_db=-9.0, hp=1500, fade_in=0.30, fade_out=0.02),
        ]),
        "FanRelease1": (0.6, -11.0, [
            layer(SHING, 0.070, 0.500, at=0.000, gain_db=-3.0, hp=1600, fade_in=0.002, fade_out=0.20),
            layer(AIRCUT, 0.120, 0.430, at=0.000, gain_db=0.0, hp=300, lp=3500, fade_in=0.003, fade_out=0.12),
            layer("vsco:glass_break5", 0.020, 0.420, at=0.000, gain_db=-9.0, hp=2500, lp=14000, fade_in=0.001, fade_out=0.15),
            layer("vsco:glock_fx_up_chromatic_fast_01", 0.28, 0.95, at=0.010, gain_db=-8.0, hp=900, fade_in=0.003, fade_out=0.25),
            layer("vsco:Marimba_hit_Outrigger_G4_loud_01", 0.0, 0.40, at=0.000, gain_db=2.0, hp=150, fade_in=0.001, fade_out=0.22),
            layer("cc0:drawKnife3", 0.10, 0.44, at=0.000, gain_db=-3.0, hp=200, lp=3000, fade_in=0.002, fade_out=0.15),
            thump(0.000, -3.0, 190, 80, 0.20),
        ]),
        "FanRelease2": (0.6, -11.0, [
            layer(RING, 0.080, 0.520, at=0.000, gain_db=-5.0, speed=1.08, hp=1500, fade_in=0.002, fade_out=0.22),
            layer(SHING, 0.070, 0.490, at=0.000, gain_db=-4.0, speed=1.12, hp=1800, fade_in=0.002, fade_out=0.18),
            layer(ENERGY, 0.150, 0.420, at=0.000, gain_db=-2.0, speed=1.10, hp=200, lp=4000, fade_in=0.004, fade_out=0.12),
            layer("vsco:glass_break3", 0.050, 0.440, at=0.000, gain_db=-9.0, hp=2500, lp=14000, fade_in=0.001, fade_out=0.15),
            layer("vsco:glock_fx_up_chromatic_fast_02", 0.34, 1.00, at=0.010, gain_db=-8.0, hp=900, fade_in=0.003, fade_out=0.25),
            layer("vsco:Xylo_Medium_G4_ff_01_far", 0.0, 0.40, at=0.000, gain_db=3.0, hp=150, fade_in=0.001, fade_out=0.22),
            layer("cc0:drawKnife3", 0.10, 0.44, at=0.000, gain_db=-4.0, speed=1.06, hp=200, lp=3000, fade_in=0.002, fade_out=0.15),
            thump(0.000, -3.0, 175, 75, 0.20),
        ]),
        "FanRelease3": (0.6, -11.0, [
            layer("cc0:knifeSlice2", 0.000, 0.420, at=0.000, gain_db=-3.0, speed=0.94, hp=300, lp=6000, fade_in=0.002, fade_out=0.18),
            layer(SHING, 0.070, 0.490, at=0.002, gain_db=-7.0, speed=0.94, hp=1800, fade_in=0.002, fade_out=0.20),
            layer(AIRCUT, 0.120, 0.430, at=0.000, gain_db=-1.0, speed=0.92, hp=250, lp=3500, fade_in=0.003, fade_out=0.12),
            layer("vsco:glass_break2", 0.050, 0.440, at=0.000, gain_db=-9.0, hp=2500, lp=14000, fade_in=0.001, fade_out=0.15),
            layer("vsco:glock_fx_up_pentatonic_med_02", 0.05, 0.75, at=0.005, gain_db=-8.0, hp=900, fade_in=0.003, fade_out=0.25),
            layer("vsco:Marimba_hit_Outrigger_G4_loud_01", 0.0, 0.40, at=0.000, gain_db=2.0, speed=0.94, hp=150, fade_in=0.001, fade_out=0.22),
            layer("cc0:drawKnife3", 0.10, 0.44, at=0.000, gain_db=-3.0, speed=0.94, hp=200, lp=3000, fade_in=0.002, fade_out=0.15),
            thump(0.000, -3.0, 160, 70, 0.20),
        ]),

        # ---------------------------------------------------------------- glass rain
        "RainCharge": (1.07, -17.0, [
            layer(TRI6ROLL, 0.0, 1.05, at=0.0, gain_db=-8.0, hp=2500, fade_in=0.90, fade_out=0.02),
            layer("vsco:BellTree_Stroke3_v1_Sum:rev", 4.55, 5.60, at=0.0, gain_db=-3.0, hp=2500, fade_in=0.10, fade_out=0.02),
            layer(BOW2, 0.05, 1.12, at=0.0, gain_db=0.0, hp=300, fade_in=0.40, fade_out=0.03),
            layer(VIBR, 1.20, 2.14, at=0.0, gain_db=-4.0, speed=VIB, hp=150, fade_in=0.30, fade_out=0.02),
            layer("synth:shimmer_1.07:rev", 0.0, 1.06, at=0.0, gain_db=-9.0, fade_in=0.20, fade_out=0.02),
            *sparkles([0.10, 0.34, 0.52, 0.66, 0.77, 0.86, 0.94],
                      [-20.0, -17.0, -14.0, -12.0, -10.0, -8.0, -6.0],
                      [1.0, G, E, D, G, 1.0, E],
                      ["vsco:glock_medium_G6", "vsco:glock_medium_C7", "vsco:Xylo_Medium_C7_ff_01_far"]),
        ]),
        "RainRelease1": (0.9, -12.0, [
            layer("vsco:glass_break7", 0.08, 0.93, at=0.000, gain_db=-5.0, hp=1800, lp=15000, fade_in=0.001, fade_out=0.30),
            layer("vsco:glock_fx_down_chromatic_fast_02", 0.20, 1.10, at=0.000, gain_db=-4.0, hp=700, fade_in=0.003, fade_out=0.45),
            layer("vsco:Triangle3-Hit_v2_rr1_Sum", 0.0, 0.85, at=0.000, gain_db=-8.0, hp=2500, fade_in=0.001, fade_out=0.40),
            layer("vsco:TB_hit_C5_v4_rr1", 0.0, 0.70, at=0.000, gain_db=-8.0, hp=250, lp=6000, fade_in=0.001, fade_out=0.40),
            layer("vsco:Marimba_hit_Outrigger_G4_loud_01", 0.0, 0.50, at=0.000, gain_db=0.0, hp=150, fade_in=0.001, fade_out=0.30),
            layer("cc0:swish-10", 0.0, 0.12, at=0.000, gain_db=-8.0, speed=0.8, hp=400, fade_out=0.06),
            thump(0.000, -6.0, 150, 65, 0.24),
        ]),
        "RainRelease2": (0.9, -12.0, [
            layer("vsco:glass_break3", 0.0, 0.76, at=0.000, gain_db=-1.0, hp=1800, lp=15000, fade_in=0.001, fade_out=0.30),
            layer("vsco:glock_fx_down_pentatonic_med_01", 0.15, 1.05, at=0.000, gain_db=-4.0, speed=D, hp=700, fade_in=0.003, fade_out=0.45),
            layer("vsco:Triangle6-Hit_v2_rr1_Sum", 0.0, 0.85, at=0.000, gain_db=-7.0, hp=2500, fade_in=0.001, fade_out=0.40),
            layer("vsco:TB_hit_G4_v4_rr1", 0.0, 0.70, at=0.000, gain_db=-8.0, hp=250, lp=6000, fade_in=0.001, fade_out=0.40),
            layer("vsco:Xylo_Medium_G4_ff_01_far", 0.0, 0.50, at=0.000, gain_db=-6.0, hp=150, fade_in=0.001, fade_out=0.30),
            layer("cc0:swish-12", 0.0, 0.07, at=0.000, gain_db=-7.0, speed=0.8, hp=400, fade_out=0.04),
            thump(0.000, -9.0, 145, 62, 0.22),
        ]),
        "RainRelease3": (0.9, -12.0, [
            layer("vsco:glass_break6", 0.0, 0.77, at=0.000, gain_db=-2.0, speed=0.92, hp=1800, lp=15000, fade_in=0.001, fade_out=0.30),
            layer("vsco:glock_fx_down_chromatic_fast_04", 0.20, 1.10, at=0.000, gain_db=-4.0, speed=1.0, hp=700, fade_in=0.003, fade_out=0.45),
            layer("vsco:Triangle3-Hit_v2_rr2_Sum", 0.0, 0.85, at=0.000, gain_db=-8.0, hp=2500, fade_in=0.001, fade_out=0.40),
            layer("vsco:TB_hit_F5_v3_rr1", 0.0, 0.70, at=0.000, gain_db=-7.0, hp=250, lp=6000, fade_in=0.001, fade_out=0.40),
            layer("vsco:Marimba_hit_Outrigger_G4_loud_01", 0.0, 0.50, at=0.000, gain_db=-7.0, speed=0.89, hp=150, fade_in=0.001, fade_out=0.30),
            layer("cc0:swish-11", 0.0, 0.10, at=0.000, gain_db=-7.0, speed=0.8, hp=400, fade_out=0.05),
            thump(0.000, -8.0, 140, 60, 0.24),
        ]),

        # ---------------------------------------------------------------- sword beam
        "BeamCharge": (1.6, -14.0, [
            layer(CRESC_M, 2.15, 3.75, at=0.0, gain_db=-2.0, hp=300, fade_in=0.20, fade_out=0.02),
            layer(BOW2, 0.0, 1.2, at=0.4, gain_db=-2.0, hp=300, fade_in=0.30, fade_out=0.03),
            layer("synth:riser_1.6", 0.0, 1.59, at=0.0, gain_db=-8.0, hp=800, fade_in=0.40, fade_out=0.02),
            layer("synth:sub_1.6:rev", 0.0, 1.55, at=0.0, gain_db=-24.0, hp=40, lp=300, fade_in=0.40, fade_out=0.08),
            layer(VIBR, 0.50, 2.10, at=0.0, gain_db=-5.0, speed=VIB, hp=150, fade_in=0.30, fade_out=0.02),
            layer("synth:shimmer_1.6:rev", 0.0, 1.59, at=0.0, gain_db=-9.0, fade_in=0.30, fade_out=0.02),
            layer(GDOWN_REV, 5.60, 6.78, at=0.35, gain_db=-8.0, hp=900, fade_in=0.10, fade_out=0.02),
        ]),
        "BeamFire": (1.0, -9.0, [
            layer("vsco:cymbal-crashshort_v1", 0.0, 0.95, at=0.000, gain_db=0.0, hp=400, fade_in=0.001, fade_out=0.40),
            layer("vsco:susCymb1-hit-bell_fff", 0.0, 1.00, at=0.000, gain_db=-4.0, hp=350, fade_in=0.001, fade_out=0.50),
            layer("vsco:glass_break5", 0.0, 0.50, at=0.000, gain_db=-5.0, hp=2500, lp=15000, fade_in=0.001, fade_out=0.25),
            layer("vsco:TB_hit_C5_v4_rr1", 0.0, 1.00, at=0.000, gain_db=-5.0, hp=300, lp=6000, fade_in=0.001, fade_out=0.50),
            layer(RING, 0.080, 0.900, at=0.020, gain_db=-8.0, hp=1400, lp=12000, fade_in=0.004, fade_out=0.40),
            layer(ENERGY, 0.150, 0.420, at=0.000, gain_db=-3.0, speed=0.82, hp=250, fade_in=0.004, fade_out=0.15),
            layer("vsco:glock_fx_up_chromatic_fast_02", 0.34, 1.20, at=0.030, gain_db=-9.0, hp=900, fade_in=0.003, fade_out=0.45),
            thump(0.000, -5.0, 165, 55, 0.32),
        ]),
        "BeamSweep": (3.0, -15.0, [
            layer(BOW3, 6.0, 9.0, at=0.0, gain_db=6.0, hp=250, fade_in=0.15, fade_out=1.60),
            layer("synth:shimmer_3.0", 0.0, 2.99, at=0.0, gain_db=-14.0, fade_in=0.05, fade_out=1.50, width=1.4),
            layer(TRI3ROLL, 0.0, 2.90, at=0.0, gain_db=-12.0, hp=4000, fade_in=0.30, fade_out=1.50),
            layer("vsco:susCymb1-scrape2_v1", 0.30, 3.30, at=0.0, gain_db=-14.0, hp=1500, fade_in=0.15, fade_out=1.40),
            layer("vsco:glock_medium_C6", 0.0, 1.8, at=0.050, gain_db=-12.0, speed=G, hp=500, fade_in=0.001, fade_out=1.00),
            layer("vsco:glock_medium_G6", 0.0, 1.8, at=1.100, gain_db=-16.0, speed=D, hp=500, fade_in=0.001, fade_out=1.00),
        ]),

        # ---------------------------------------------------------------- spatial cut
        "CutCharge": (1.0, -17.0, [
            layer("vsco:susCymb1-scrape1_v1", 0.20, 1.20, at=0.0, gain_db=-4.0, speed=1.34, hp=1800, fade_in=0.55, fade_out=0.03),
            layer(BOW2, 0.10, 1.10, at=0.0, gain_db=2.0, speed=A, hp=400, fade_in=0.45, fade_out=0.03),
            layer(TRI6ROLL, 0.0, 1.0, at=0.0, gain_db=-6.0, hp=4500, fade_in=0.85, fade_out=0.03),
            layer(GDOWN_REV, 6.00, 6.78, at=0.20, gain_db=-8.0, speed=1.26, hp=1800, fade_in=0.05, fade_out=0.02),
            layer("synth:riser_1.0", 0.0, 0.99, at=0.0, gain_db=-10.0, hp=3500, fade_in=0.35, fade_out=0.02),
        ]),
        "CutRelease1": (0.5, -10.0, [
            layer("cc0:knifeSlice", 0.075, 0.330, at=0.000, gain_db=-2.0, hp=500, fade_in=0.002, fade_out=0.20),
            layer(SHING, 0.070, 0.470, at=0.000, gain_db=-5.0, speed=1.20, hp=2200, fade_in=0.002, fade_out=0.20),
            layer(AIRCUT, 0.050, 0.350, at=0.000, gain_db=-5.0, speed=1.15, hp=500, fade_in=0.003, fade_out=0.10),
            layer("vsco:glass_break6", 0.0, 0.30, at=0.000, gain_db=-8.0, hp=3000, lp=15000, fade_in=0.001, fade_out=0.10),
            layer("vsco:Xylo_Medium_G4_ff_01_far", 0.0, 0.30, at=0.000, gain_db=1.0, hp=200, fade_in=0.001, fade_out=0.18),
            layer("cc0:drawKnife3", 0.10, 0.40, at=0.000, gain_db=-4.0, speed=1.1, hp=250, lp=3500, fade_in=0.002, fade_out=0.12),
            thump(0.000, -5.0, 170, 80, 0.14),
        ]),
        "CutRelease2": (0.5, -10.0, [
            layer("cc0:drawKnife2", 0.050, 0.440, at=0.000, gain_db=0.0, hp=300, fade_in=0.002, fade_out=0.20),
            layer(HIT442, 0.0, 0.40, at=0.000, gain_db=-7.0, speed=1.10, hp=1500, fade_in=0.001, fade_out=0.20),
            layer(AIRCUT, 0.050, 0.350, at=0.000, gain_db=-5.0, speed=1.25, hp=500, fade_in=0.003, fade_out=0.10),
            layer("vsco:glass_break2", 0.040, 0.340, at=0.000, gain_db=-8.0, hp=3000, lp=15000, fade_in=0.001, fade_out=0.10),
            layer("vsco:Marimba_hit_Outrigger_G4_loud_01", 0.0, 0.30, at=0.000, gain_db=4.0, speed=1.12, hp=200, fade_in=0.001, fade_out=0.18),
            layer("cc0:drawKnife3", 0.10, 0.40, at=0.000, gain_db=-4.0, speed=1.2, hp=250, lp=3500, fade_in=0.002, fade_out=0.12),
            thump(0.000, -5.0, 165, 78, 0.14),
        ]),

        # ---------------------------------------------------------------- slash lattice
        "LatticeVolley": (1.2, -10.0, [
            layer("vsco:glass_break7", 0.0, 1.10, at=0.000, gain_db=-2.0, hp=1500, lp=15000, fade_in=0.001, fade_out=0.45),
            layer("vsco:glass_break5", 0.0, 0.70, at=0.040, gain_db=-5.0, hp=2000, lp=15000, fade_in=0.001, fade_out=0.30),
            layer("cc0:knifeSlice2", 0.0, 0.40, at=0.000, gain_db=-3.0, hp=600, fade_in=0.002, fade_out=0.18),
            layer("cc0:knifeSlice2", 0.0, 0.40, at=0.050, gain_db=-7.0, speed=1.12, hp=800, fade_in=0.002, fade_out=0.18),
            layer("cc0:knifeSlice2", 0.0, 0.40, at=0.100, gain_db=-10.0, speed=1.26, hp=900, fade_in=0.002, fade_out=0.18),
            layer("vsco:TB_hit_C5_v4_rr1", 0.0, 1.10, at=0.000, gain_db=-5.0, hp=300, lp=6000, fade_in=0.001, fade_out=0.60),
            layer("vsco:susCymb1-hit-bell_fff", 0.0, 1.10, at=0.000, gain_db=-7.0, hp=400, fade_in=0.001, fade_out=0.60),
            layer("vsco:BrakeDrum1_Hammer_v1_Sum", 0.0, 0.60, at=0.000, gain_db=-8.0, hp=250, fade_in=0.001, fade_out=0.30),
            layer("vsco:glock_fx_down_chromatic_fast_02", 0.20, 1.20, at=0.020, gain_db=-8.0, hp=800, fade_in=0.003, fade_out=0.50),
            thump(0.000, -5.0, 150, 52, 0.34),
        ]),
        "LatticeTick1": (0.25, -20.0, [
            layer("vsco:Xylo_Medium_C7_ff_01_far", 0.0, 0.20, at=0.000, gain_db=0.0, hp=700, fade_in=0.0005, fade_out=0.10),
            layer("vsco:Claves1_Hit_v3_rr1_Sum", 0.0, 0.08, at=0.000, gain_db=-4.0, hp=500, fade_in=0.0005, fade_out=0.04),
            layer("vsco:glass_break6", 0.0, 0.06, at=0.000, gain_db=-14.0, hp=3500, fade_in=0.0005, fade_out=0.04),
            layer("vsco:Xylo_Medium_G4_ff_01_far", 0.0, 0.07, at=0.000, gain_db=-5.0, hp=300, fade_in=0.0005, fade_out=0.04),
        ]),
        "LatticeTick2": (0.25, -20.0, [
            layer("vsco:glock_medium_C7", 0.0, 0.20, at=0.000, gain_db=0.0, speed=1.0, hp=800, fade_in=0.0005, fade_out=0.10),
            layer("vsco:Claves1_Hit_v3_rr2_Sum", 0.0, 0.08, at=0.000, gain_db=-4.0, hp=500, fade_in=0.0005, fade_out=0.04),
            layer("vsco:glass_break4", 0.0, 0.06, at=0.000, gain_db=-14.0, hp=3500, fade_in=0.0005, fade_out=0.04),
            layer("vsco:Marimba_hit_Outrigger_G4_loud_01", 0.0, 0.07, at=0.000, gain_db=-5.0, hp=300, fade_in=0.0005, fade_out=0.04),
        ]),
        "LatticeTick3": (0.25, -20.0, [
            layer("vsco:Xylo_Medium_G6_ff_01_far", 0.0, 0.20, at=0.000, gain_db=0.0, speed=1.0, hp=700, fade_in=0.0005, fade_out=0.10),
            layer("vsco:Claves1_Hit_v2_rr1_Sum", 0.0, 0.08, at=0.000, gain_db=-4.0, hp=500, fade_in=0.0005, fade_out=0.04),
            layer("vsco:glass_break8", 0.0, 0.06, at=0.000, gain_db=-14.0, hp=3500, fade_in=0.0005, fade_out=0.04),
            layer("vsco:Xylo_Medium_G4_ff_01_far", 0.0, 0.07, at=0.000, gain_db=-5.0, speed=1.12, hp=300, fade_in=0.0005, fade_out=0.04),
        ]),

        # ---------------------------------------------------------------- Liora is hit
        "LioraHit1": (0.2, -18.0, [
            layer("vsco:Marimba_hit_Outrigger_G4_loud_01", 0.0, 0.18, at=0.000, gain_db=0.0, speed=1.12, hp=250, fade_in=0.0008, fade_out=0.10),
            layer("vsco:glock_medium_G5", 0.0, 0.18, at=0.000, gain_db=-4.0, speed=1.0, hp=800, fade_in=0.0008, fade_out=0.11),
            layer("vsco:glass_break6", 0.0, 0.10, at=0.000, gain_db=-14.0, hp=3000, lp=11000, fade_in=0.0008, fade_out=0.06),
        ]),
        "LioraHit2": (0.2, -18.0, [
            layer("vsco:Xylo_Medium_G4_ff_01_far", 0.0, 0.18, at=0.000, gain_db=0.0, speed=1.12, hp=250, fade_in=0.0008, fade_out=0.10),
            layer("vsco:glock_medium_C6", 0.0, 0.18, at=0.000, gain_db=-4.0, speed=1.26, hp=800, fade_in=0.0008, fade_out=0.11),
            layer("vsco:glass_break4", 0.0, 0.10, at=0.000, gain_db=-14.0, hp=3000, lp=11000, fade_in=0.0008, fade_out=0.06),
        ]),
    }
    return SCORE


def worm_score(layer, thump, src):
    HOWL = "cc0:roar-FS257635-Bananaboatman33-demon_giant_howl"
    LOWIMP = "cc0:impact-FS541029-AudioPapkin-very_low_impact"
    CONC = "cc0:impact-FS522099-magnuswaker-concrete_smash_2"
    STONE = "cc0:impact-FS711657-discofield-stone_crash"
    WOOSH = "cc0:wind-FS683096-florianreichelt-woosh"
    WHIRL = "cc0:wind-FS719560-DARTEKZ_GAMEZ-wind_whirl"
    SWOOSH = "cc0:swing-FS263595-PorkMuncher-swoosh"
    BELL = "cc0:bell-FS405665-Anthousai-metal_bowl_hit"
    BOWL = "cc0:bell-FS271370-inoshirodesign-singing_bowl"

    SCORE = {
        # 1.67s telegraph: low growl swells, glass/chain creak climbs in pitch, peak just before contact
        "RushWarn": (1.67, -14.0, [
            layer(LOWIMP, 0.20, 1.40, at=0.00, gain_db=-17.0, speed=0.9, hp=35, lp=400, fade_in=1.00, fade_out=0.05),
            layer(HOWL, 0.10, 0.75, at=0.05, gain_db=-10.0, speed=0.5, hp=60, lp=1400, fade_in=0.90, fade_out=0.05),
            layer(HOWL, 0.10, 0.75, at=0.80, gain_db=-4.0, speed=0.62, hp=90, lp=2200, fade_in=0.55, fade_out=0.05),
            layer("vsco:gongscrape_mf", 1.00, 2.65, at=0.00, gain_db=9.0, speed=0.8, hp=120, lp=3500, fade_in=1.30, fade_out=0.05),
            layer("vsco:brick_scrape", 0.00, 1.10, at=0.30, gain_db=20.0, speed=0.7, hp=80, lp=3000, fade_in=0.50, fade_out=0.05),
            layer("vsco:brick_scrape2", 0.00, 1.19, at=0.85, gain_db=19.0, speed=0.9, hp=100, lp=4500, fade_in=0.40, fade_out=0.05),
            layer("vsco:chain_grind", 0.00, 0.50, at=0.45, gain_db=-9.0, speed=0.62, hp=500, lp=4500, fade_in=0.20, fade_out=0.12),
            layer("vsco:chain_grind", 0.00, 0.50, at=0.88, gain_db=-8.0, speed=0.85, hp=700, lp=6500, fade_in=0.15, fade_out=0.10),
            layer("vsco:chain_grind", 0.00, 0.42, at=1.25, gain_db=-1.0, speed=1.12, hp=900, lp=8000, fade_in=0.10, fade_out=0.02),
            layer("synth:riser_1.5", 0.00, 1.50, at=0.12, gain_db=-5.0, hp=200, fade_in=0.50, fade_out=0.05),
            layer("synth:shimmer_0.6", 0.00, 0.50, at=1.10, gain_db=-9.0, hp=2500, fade_in=0.40, fade_out=0.05),
        ]),
        # contact: smashing-pressure onset, then wind + glass body dragging past + howl + sub
        "RushPass": (1.40, -9.0, [
            thump(0.000, -11.0, 90, 38, 0.45),
            layer(LOWIMP, 0.00, 1.30, at=0.000, gain_db=-13.0, speed=1.0, hp=30, lp=500, fade_in=0.004, fade_out=0.40),
            layer(CONC, 0.000, 0.700, at=0.000, gain_db=-8.0, speed=0.9, hp=35, lp=1200, fade_in=0.002, fade_out=0.25),
            layer(SWOOSH, 1.04, 1.50, at=0.000, gain_db=2.0, speed=0.8, hp=120, lp=6000, fade_in=0.004, fade_out=0.20),
            layer(HOWL, 0.080, 0.750, at=0.020, gain_db=-1.0, speed=0.62, hp=70, lp=4500, fade_in=0.012, fade_out=0.30),
            layer(WOOSH, 0.300, 1.600, at=0.040, gain_db=-5.0, speed=0.85, hp=40, lp=1500, fade_in=0.060, fade_out=0.50),
            layer("vsco:brick_scrape2", 0.000, 1.190, at=0.000, gain_db=22.0, speed=0.9, hp=100, lp=5000, fade_in=0.002, fade_out=0.50),
            layer("vsco:glass_break5", 0.030, 0.900, at=0.005, gain_db=-6.0, speed=0.55, hp=250, lp=9000, fade_in=0.002, fade_out=0.40),
            layer("vsco:chain_grind", 0.000, 0.500, at=0.010, gain_db=-6.0, speed=0.75, hp=500, lp=7000, fade_in=0.005, fade_out=0.20),
            layer("vsco:gongscrape_mf", 1.00, 2.20, at=0.100, gain_db=12.0, speed=0.75, hp=120, lp=3500, fade_in=0.10, fade_out=0.50),
        ]),
        # 0.4s inhale (reversed glass chime sweep + low swell), then a glittering swarm launch with a long tail
        "MissileVolley": (1.50, -11.0, [
            layer("vsco:glass_break7:rev", 0.30, 1.28, at=0.00, gain_db=-10.0, speed=0.9, hp=700, lp=11000, fade_in=0.20, fade_out=0.02),
            layer(WOOSH, 0.40, 0.80, at=0.00, gain_db=-6.0, speed=1.0, hp=60, lp=1500, fade_in=0.25, fade_out=0.02),
            layer("synth:riser_0.4", 0.00, 0.40, at=0.00, gain_db=-10.0, hp=300, fade_in=0.20, fade_out=0.015),
            # launch at 0.40
            thump(0.400, -9.0, 130, 55, 0.25),
            layer(SWOOSH, 1.03, 1.83, at=0.400, gain_db=-3.0, speed=1.1, hp=250, lp=10000, fade_in=0.006, fade_out=0.40),
            layer("cc0:swish-10", 0.00, 0.125, at=0.400, gain_db=-4.0, speed=1.0, hp=300, fade_out=0.04),
            layer("cc0:swish-11", 0.00, 0.104, at=0.425, gain_db=-5.0, speed=1.2, hp=400, fade_out=0.04),
            layer("cc0:swish-12", 0.00, 0.076, at=0.450, gain_db=-5.0, speed=0.9, hp=400, fade_out=0.04),
            layer("cc0:swish-13", 0.00, 0.070, at=0.480, gain_db=-6.0, speed=1.1, hp=500, fade_out=0.04),
            layer("vsco:glass_break8", 0.05, 0.60, at=0.400, gain_db=-5.0, speed=0.8, hp=800, fade_in=0.002, fade_out=0.30),
            layer("vsco:tamb2_rollSlow", 0.05, 1.10, at=0.420, gain_db=3.0, hp=3000, fade_in=0.02, fade_out=0.55),
            layer("vsco:glock_fx_up_chromatic_fast_02", 0.05, 1.05, at=0.420, gain_db=0.0, hp=1200, fade_in=0.01, fade_out=0.60),
            layer("vsco:Triangle6-HitFM_v2_rr1_Sum", 0.00, 1.00, at=0.405, gain_db=-1.0, hp=2500, fade_in=0.002, fade_out=0.60),
            layer("synth:shimmer_1.0", 0.00, 1.00, at=0.450, gain_db=-12.0, hp=1800, fade_out=0.50),
            layer(SWOOSH, 1.50, 1.83, at=0.620, gain_db=-8.0, speed=1.25, hp=500, lp=11000, fade_in=0.02, fade_out=0.25),
            layer(WHIRL, 0.30, 1.10, at=0.450, gain_db=-1.0, speed=1.5, hp=600, lp=6000, fade_in=0.03, fade_out=0.45),
        ]),
    }


    def _hit(name, glass, g_start, g_end, speed, pot, p_start, p_end, f0, gain_pot=-7.0):
        return (0.25, -18.0, [
            layer(glass, g_start, g_end, at=0.000, gain_db=-4.0, speed=speed, hp=500, lp=9000, fade_in=0.001, fade_out=0.12),
            layer(pot, p_start, p_end, at=0.000, gain_db=gain_pot, speed=speed * 0.85, hp=150, lp=4500, fade_in=0.001, fade_out=0.06),
            layer(BELL, 0.000, 0.300, at=0.002, gain_db=-17.0, speed=1.15 + 0.1 * (speed - 0.75), hp=800, fade_in=0.001, fade_out=0.20),
            thump(0.000, -9.0, f0, 70, 0.10),
        ])


    SCORE["WormHit1"] = _hit("1", "vsco:glass_break2", 0.050, 0.300, 0.75, "cc0:metalPot1", 0.080, 0.185, 150)
    SCORE["WormHit2"] = _hit("2", "vsco:glass_break4", 0.015, 0.265, 0.70, "cc0:metalPot3", 0.055, 0.190, 135)
    SCORE["WormHit3"] = _hit("3", "vsco:glass_break6", 0.010, 0.260, 0.80, "cc0:metalPot1", 0.085, 0.188, 165)
    return SCORE


def chorus_score(layer, thump, src):
    def tone(key, start, dur, at, speed=1.0, gain_db=0.0, fade_in=0.002, tail=0.6, **kw):
        """A cut whose *output* length is dur seconds (clamped to the source), decaying over the last `tail` share."""
        avail = len(src(key)) / 44100 - 0.001
        end = min(start + dur * speed, avail)
        dur = (end - start) / speed
        return layer(key, start, end, at=at, gain_db=gain_db, speed=speed,
                     fade_in=fade_in, fade_out=dur * tail, **kw)


    GLOCK_C = "vsco:glock_medium_C5"     # sounds C6
    GLOCK_G = "vsco:glock_medium_G4"     # sounds G5
    GLOCK_G6 = "vsco:glock_medium_G5"    # sounds G6
    VIB = "vsco:vibraring1"              # 341 Hz
    VIB2 = "vsco:vibraring3"
    GONG = "vsco:cymb_gong"              # 340 Hz fundamental
    TB_C = "vsco:TB_hit_C4_v4_rr1"
    TB_G = "vsco:TB_hit_G4_v4_rr1"

    SCORE = {
        # ---- Stack call: notes fall and bunch up, landing on a low bell (gather) ----
        "StackCall": (2.0, -12.0, [
            # converging shimmer swell that resolves into the low strike
            tone("synth:shimmer_1.3:rev", 0.0, 1.2, 0.05, gain_db=-12.0, fade_in=0.4, tail=0.15, hp=1500),
            # descending glock notes with shrinking gaps: E6 C6 A5 E5
            tone(GLOCK_C, 0.0, 0.55, 0.00, speed=1.2515, gain_db=1.0),
            tone(GLOCK_C, 0.0, 0.55, 0.26, speed=1.0, gain_db=1.0),
            tone(GLOCK_G, 0.0, 0.60, 0.46, speed=1.117, gain_db=1.0),
            tone(GLOCK_G, 0.0, 0.70, 0.62, speed=0.837, gain_db=2.0),
            # mid-register bells follow the same fall: E5 -> A4 -> E4
            tone(VIB, 0.0, 0.7, 0.10, speed=1.93, gain_db=-3.0, hp=300),
            tone(VIB, 0.0, 0.7, 0.36, speed=1.29, gain_db=-2.0, hp=250),
            tone(VIB2, 0.0, 0.8, 0.56, speed=0.967, gain_db=-1.0, hp=200),
            # the low bell everything falls into
            tone(VIB, 0.0, 1.05, 0.88, speed=0.645, gain_db=-1.0, hp=180, fade_in=0.002, tail=0.8),
            tone(GONG, 0.0, 1.05, 0.88, speed=0.645, gain_db=-9.0, hp=150, lp=2500, tail=0.8),
            tone(TB_C, 0.0, 1.05, 0.88, speed=0.8, gain_db=-8.0, hp=200, lp=3500, tail=0.8),
            thump(0.88, -15.0, 110, 55, 0.5),
        ]),
        # ---- Spread call: notes rise and open outward, bright and airy (scatter) ----
        "SpreadCall": (2.0, -12.0, [
            tone("synth:riser_1.1", 0.0, 1.1, 0.0, gain_db=-14.0, fade_in=0.3, tail=0.2, hp=800, lp=9000),
            # ascending glock A5 C6 E6 A6 with widening gaps
            tone(GLOCK_G, 0.0, 0.60, 0.00, speed=1.117, gain_db=-6.0),
            tone(GLOCK_C, 0.0, 0.60, 0.16, speed=1.0, gain_db=-6.0),
            tone(GLOCK_C, 0.0, 0.65, 0.36, speed=1.2515, gain_db=-5.0),
            tone(GLOCK_G6, 0.0, 0.85, 0.62, speed=1.115, gain_db=-5.0),
            # mid bells rise A4 C5 E5 A5 under them
            tone(VIB, 0.0, 0.8, 0.00, speed=1.29, gain_db=-6.0, hp=250),
            tone(VIB, 0.0, 0.8, 0.18, speed=1.53, gain_db=-7.0, hp=250),
            tone(VIB2, 0.0, 0.9, 0.40, speed=1.93, gain_db=-7.0, hp=250),
            tone(VIB, 0.0, 1.1, 0.66, speed=2.58, gain_db=-6.0, hp=350, tail=0.8),
            # opening bell tree flourish and shimmer ring-out
            tone("vsco:BellTree_Stroke3_v1_Sum", 0.8, 1.0, 0.90, gain_db=-9.0, hp=1200, fade_in=0.05, tail=0.7, width=1.4),
            tone("synth:shimmer_1.3", 0.0, 1.1, 0.88, gain_db=-12.0, tail=0.8),
            tone("vsco:susCymb1-bow-2", 0.2, 1.3, 0.70, gain_db=-10.0, hp=400, lp=6000, fade_in=0.25, tail=0.7),
        ]),
        # ---- Countdown tick: a small clear crystal bell + wooden click, tuned to A5 ----
        # The game raises pitch per tick: recommend 0, +0.25, +0.583 octaves (A, C, E).
        "ChorusTick1": (0.5, -16.0, [
            tone(GLOCK_G, 0.0, 0.42, 0.0, speed=1.117, gain_db=0.0, tail=0.9, hp=500),
            tone(VIB, 0.0, 0.42, 0.0, speed=2.58, gain_db=-8.0, hp=500, tail=0.9),
            tone("vsco:Claves1_Hit_v2_rr1_Sum", 0.0, 0.10, 0.0, speed=1.4, gain_db=-8.0, hp=600, tail=0.8),
        ]),
        "ChorusTick2": (0.5, -16.0, [
            tone(GLOCK_C, 0.0, 0.42, 0.0, speed=0.835, gain_db=0.0, tail=0.9, hp=500),
            tone(VIB2, 0.0, 0.42, 0.0, speed=2.58, gain_db=-8.0, hp=500, tail=0.9),
            tone("vsco:Claves1_Hit_v2_rr2_Sum", 0.0, 0.10, 0.0, speed=1.45, gain_db=-8.0, hp=600, tail=0.8),
        ]),
        "ChorusTick3": (0.5, -16.0, [
            tone(GLOCK_G6, 0.0, 0.42, 0.0, speed=0.557, gain_db=0.0, tail=0.9, hp=500),
            tone(VIB, 0.05, 0.42, 0.0, speed=2.6, gain_db=-9.0, hp=500, tail=0.9),
            tone("vsco:Claves1_Hit_v3_rr1_Sum", 0.0, 0.10, 0.0, speed=1.35, gain_db=-8.0, hp=600, tail=0.8),
        ]),
        # ---- Stack success: consonant open chord resolves, soft glass lets go ----
        "StackHold": (1.5, -12.0, [
            tone(VIB, 0.0, 1.4, 0.0, speed=0.645, gain_db=-1.0, hp=120, tail=0.8),    # A3
            tone(VIB2, 0.0, 1.3, 0.0, speed=0.967, gain_db=-3.0, hp=150, tail=0.8),   # E4
            tone(VIB, 0.0, 1.2, 0.0, speed=1.29, gain_db=-4.0, hp=200, tail=0.8),     # A4
            tone(TB_G, 0.0, 1.4, 0.0, speed=0.75, gain_db=-8.0, hp=250, lp=4000, tail=0.8),
            tone(GLOCK_G, 0.0, 1.2, 0.02, speed=1.117, gain_db=-7.0, tail=0.8),       # A5
            tone(GLOCK_C, 0.0, 1.1, 0.07, speed=1.2515, gain_db=-8.0, tail=0.8),      # E6
            # glass lets go gently: lowpassed, slowed, never the star
            tone("vsco:glass_break5", 0.0, 0.9, 0.0, speed=0.8, gain_db=-8.0, hp=500, lp=6500, tail=0.7),
            thump(0.0, -9.0, 100, 60, 0.4),
            tone("synth:shimmer_1.2", 0.0, 1.2, 0.05, gain_db=-14.0, tail=0.8),
        ]),
        # ---- Stack failure: ice jaws crush, shatter, low blow, dissonant low bells ----
        "StackShatter": (1.5, -8.0, [
            tone("cc0:impact-FS522099-magnuswaker-concrete_smash_2", 0.0, 1.2, 0.0, gain_db=-6.0, hp=60, lp=9000, tail=0.6),
            tone("cc0:impact-FS541029-AudioPapkin-very_low_impact", 0.62, 1.3, 0.0, speed=0.95, gain_db=-9.0, hp=40, tail=0.6),
            tone("cc0:impact-FS711657-discofield-stone_crash", 0.0, 1.3, 0.0, speed=0.85, gain_db=-1.0, hp=150, tail=0.6),
            tone("vsco:glass_break7", 0.05, 1.2, 0.0, speed=0.75, gain_db=1.0, hp=300, lp=9000, tail=0.5),
            tone("vsco:glass_break3", 0.0, 0.7, 0.07, speed=0.7, gain_db=-1.0, hp=200, lp=8000, tail=0.6),
            tone("vsco:metal_hit9", 0.0, 1.2, 0.0, speed=0.85, gain_db=-2.0, hp=150, tail=0.7),
            # dissonant low bells (minor second apart)
            tone(VIB, 0.0, 1.2, 0.0, speed=0.60, gain_db=-4.0, hp=160, tail=0.8),
            tone(VIB2, 0.0, 1.2, 0.02, speed=0.64, gain_db=-4.0, hp=160, tail=0.8),
            thump(0.0, -9.0, 95, 45, 0.45),
        ]),
        # ---- Spread success: the rifts evaporate into air ----
        "SpreadFade": (1.2, -14.0, [
            tone("vsco:susCymb1-bow-2", 0.2, 1.1, 0.0, gain_db=-3.0, hp=350, lp=7000, fade_in=0.03, tail=0.8),
            tone("synth:shimmer_1.2", 0.0, 1.1, 0.0, gain_db=-10.0, tail=0.85),
            tone(GLOCK_G6, 0.0, 1.1, 0.0, speed=1.115, gain_db=-7.0, tail=0.9),   # A6
            tone(GLOCK_C, 0.0, 1.0, 0.09, speed=1.2515, gain_db=-9.0, tail=0.9),  # E6
            tone(VIB, 0.0, 1.1, 0.0, speed=2.58, gain_db=-5.0, hp=300, tail=0.9), # A5
            tone("vsco:BellTree_Stroke3_v1_Sum", 0.8, 0.9, 0.0, gain_db=-12.0, hp=1500, tail=0.85, fade_in=0.04),
        ]),
        # ---- Spread failure: a blade of light thrusts up through the rift ----
        "SpreadPierce": (1.2, -8.0, [
            tone("cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2", 0.03, 1.0, 0.0, speed=0.95, gain_db=-4.0, hp=1500, tail=0.7),
            tone("cc0:swing-FS724716-greyfeather-sword_slash_energy_wave", 0.14, 0.30, 0.0, speed=0.9, gain_db=-3.0, hp=200, fade_in=0.003, tail=0.5),
            tone("cc0:swing-FS370204-nekoninja-samurai_slash", 0.005, 0.60, 0.0, speed=0.85, gain_db=-3.0, hp=100, lp=9000, tail=0.5),
            tone("vsco:glass_break6", 0.0, 0.75, 0.02, speed=0.85, gain_db=-5.0, hp=500, lp=10000, tail=0.6),
            tone("cc0:impact-FS522099-magnuswaker-concrete_smash_2", 0.0, 0.9, 0.0, speed=1.2, gain_db=-4.0, hp=60, lp=6000, tail=0.6),
            tone(GLOCK_C, 0.0, 1.0, 0.0, speed=2.0, gain_db=-8.0, tail=0.8),
            tone(VIB, 0.0, 1.0, 0.0, speed=1.53, gain_db=-8.0, hp=250, tail=0.8),
            thump(0.0, -6.0, 120, 55, 0.35),
        ]),
        # ---- Downed: ice swallows the player — a dull blow, then creaking frost closing in ----
        "Downed": (1.5, -12.0, [
            tone("cc0:impact-FS541029-AudioPapkin-very_low_impact", 0.62, 1.2, 0.0, gain_db=-11.0, hp=40, tail=0.7),
            tone("cc0:impact-FS522099-magnuswaker-concrete_smash_2", 0.0, 0.8, 0.0, gain_db=-5.0, hp=70, lp=3000, tail=0.6),
            tone("vsco:glass_break4", 0.0, 1.2, 0.0, speed=0.6, gain_db=-1.0, hp=300, lp=6000, tail=0.6),
            tone("vsco:chain_grind", 0.0, 1.0, 0.1, speed=0.7, gain_db=-2.0, hp=400, lp=6000, fade_in=0.05, tail=0.5),
            tone("vsco:susCymb1-scrape1_v1", 0.3, 1.2, 0.05, speed=0.7, gain_db=-4.0, hp=300, lp=5000, fade_in=0.05, tail=0.7),
            tone(VIB, 0.0, 1.3, 0.0, speed=0.75, gain_db=-4.0, hp=180, tail=0.8),
            tone("vsco:glock_fx_down_chromatic_fast_02", 0.4, 1.0, 0.04, gain_db=-8.0, hp=800, tail=0.8),
            thump(0.0, -15.0, 90, 48, 0.4),
        ]),
        # ---- Revived: thaw drips, then warm bells climb ----
        "Revived": (2.0, -12.0, [
            tone("vsco:bubbles2", 0.15, 0.8, 0.0, gain_db=-8.0, hp=250, tail=0.5, fade_in=0.01),
            tone("synth:drip_0.3", 0.0, 0.3, 0.12, gain_db=-8.0, speed=1.0, tail=0.5),
            tone("synth:drip_0.3", 0.0, 0.3, 0.30, gain_db=-11.0, speed=0.84, tail=0.5),
            tone("vsco:glass_break2", 0.0, 0.6, 0.0, speed=0.8, gain_db=-12.0, hp=500, lp=6000, tail=0.6),
            # warm rising bells A3 E4 A4 C5 E5
            tone(VIB, 0.0, 1.0, 0.25, speed=0.645, gain_db=-3.0, hp=120, tail=0.7),
            tone(VIB2, 0.0, 1.0, 0.50, speed=0.967, gain_db=-4.0, hp=150, tail=0.7),
            tone(VIB, 0.0, 1.0, 0.72, speed=1.29, gain_db=-4.0, hp=200, tail=0.7),
            tone(VIB2, 0.0, 1.0, 0.92, speed=1.53, gain_db=-4.0, hp=200, tail=0.7),
            tone(VIB, 0.0, 1.0, 1.10, speed=1.93, gain_db=-3.0, hp=250, tail=0.8),
            # glock shimmer on the top, bright resolution
            tone(GLOCK_G, 0.0, 0.8, 0.72, speed=1.117, gain_db=-3.0, tail=0.8),
            tone(GLOCK_C, 0.0, 0.8, 1.10, speed=1.2515, gain_db=-2.0, tail=0.8),
            tone(GLOCK_G6, 0.0, 0.8, 1.10, speed=1.115, gain_db=-5.0, tail=0.8),
            # warm low body that lifts and fades
            thump(0.25, -18.0, 110, 70, 0.8),
            tone("vsco:susCymb1-bow-2", 0.2, 1.3, 0.55, gain_db=-12.0, hp=300, lp=4500, fade_in=0.4, tail=0.6),
        ]),
    }
    return SCORE


def lattice_score(layer, thump, src):
    CR = "repo:FirstSeverance/Beams/ChargeRush"
    SHING = "cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2"


    def slice_cue(cr0, speed, gl, gl0, sh0, shsp, lp_hz, gg=-4.0, sg=-8.0):
        return (0.2, -6.5, [
            layer(CR, cr0, cr0 + 0.055, at=0.0, gain_db=0.0, speed=speed, hp=110, fade_in=0.002, fade_out=0.03, drive=5.0),
            layer(CR, cr0 + 0.31, cr0 + 0.37, at=0.0, gain_db=-8.0, speed=speed * 0.75, lp=900, fade_in=0.002, fade_out=0.03),
            layer(gl, gl0, gl0 + 0.055, at=0.0, gain_db=gg, speed=1.0, hp=2200, lp=min(lp_hz, 8000), fade_in=0.001, fade_out=0.03),
            layer(gl, gl0 + 0.05, gl0 + 0.21, at=0.04, gain_db=gg - 16, hp=3000, lp=lp_hz, fade_in=0.03, fade_out=0.09),
            layer(SHING, sh0, sh0 + 0.06, at=0.0, gain_db=sg, speed=shsp, hp=2500, fade_in=0.001, fade_out=0.035),
            thump(0.0, -11.0, 150, 70, 0.05),
        ])


    SCORE = {
        "LatticeVolleyA": (1.1, -7.0, [
            layer(CR, 0.00, 0.30, at=0.0, gain_db=0.0, speed=0.92, hp=60, fade_in=0.002, fade_out=0.20),
            layer(CR, 0.10, 0.70, at=0.0, gain_db=-7.0, speed=0.85, lp=1800, fade_in=0.004, fade_out=0.70),
            layer("vsco:glass_break5", 0.02, 0.90, at=0.0, gain_db=-7.0, hp=2000, fade_in=0.001, fade_out=0.55),
            layer(SHING, 0.0, 1.0, at=0.0, gain_db=-9.0, speed=0.95, hp=2200, fade_in=0.001, fade_out=0.6),
            layer("vsco:glass_break7", 0.05, 1.0, at=0.03, gain_db=-12.0, hp=3000, fade_in=0.002, fade_out=0.6, width=1.5),
            thump(0.0, -1.0, 110, 42, 0.45),
        ]),
        "LatticeSliceA1": slice_cue(0.00, 1.00, "vsco:glass_break5", 0.03, 0.00, 1.00, 12000, 1.0, 10.0),
        "LatticeSliceA2": slice_cue(0.02, 1.06, "vsco:glass_break3", 0.08, 0.04, 1.06, 10000, 2.0, 7.0),
        "LatticeSliceA3": slice_cue(0.04, 0.95, "vsco:glass_break", 0.02, 0.00, 0.94, 14000, 1.0, 8.0),
        "LatticeSliceA4": slice_cue(0.01, 1.12, "vsco:glass_break8", 0.06, 0.04, 1.03, 5500, 3.0, 11.0),
        "LatticeEndA": (1.0, -10.0, [
            layer(CR, 0.00, 0.25, at=0.0, gain_db=-2.0, speed=0.9, hp=60, fade_in=0.002, fade_out=0.15),
            layer("vsco:glass_break8", 0.05, 1.0, at=0.0, gain_db=-5.0, hp=2000, fade_in=0.001, fade_out=0.6),
            layer("vsco:glass_break7", 0.30, 1.1, at=0.15, gain_db=-8.0, hp=2500, fade_in=0.002, fade_out=0.5, width=1.5),
            layer(SHING, 0.0, 1.1, at=0.0, gain_db=-10.0, speed=0.9, hp=2200, fade_in=0.001, fade_out=0.7),
            layer("vsco:glass_break2", 0.1, 0.64, at=0.3, gain_db=-14.0, hp=3500, fade_in=0.002, fade_out=0.4),
            thump(0.0, -4.0, 120, 50, 0.35),
        ]),
    }
    return SCORE


FAMILIES = (intro_score, liora_score, worm_score, chorus_score, lattice_score)
# The auditioned lattice candidate "A" ships under the plain names; the first v2 tick set is retired.
LATTICE_NAMES = {"LatticeVolleyA": "LatticeVolley", "LatticeEndA": "LatticeEnd",
                 **{f"LatticeSliceA{i}": f"LatticeSlice{i}" for i in range(1, 5)}}
RETIRED = {"LatticeVolley", "LatticeTick1", "LatticeTick2", "LatticeTick3"}


def score(store):
    out = {}
    for family in FAMILIES:
        part = family(layer, thump, lambda key: store[key])
        if family is liora_score:
            part = {name: entry for name, entry in part.items() if name not in RETIRED}
        for name, entry in part.items():
            name = LATTICE_NAMES.get(name, name)
            if name in out:
                raise ValueError(f"duplicate cue {name}")
            out[name] = entry
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--store", type=Path, required=True,
                        help="local folder holding sfx-sources/cc0 and music/libs/VSCO-2-CE-1.1.0 (never committed)")
    parser.add_argument("--preview", type=Path, required=True, help="local WAV/report directory")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR)
    args = parser.parse_args()
    store = Store(args.store)
    cues = score(store)
    args.preview.mkdir(parents=True, exist_ok=True)
    report = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__)),
              "layers_sha256": sha256(Path(__file__).with_name("sfx_layers.py")),
              "libraries": {"numpy": np.__version__, "scipy": __import__("scipy").__version__,
                            "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
              "sources": {}, "cues": {}}
    for name, entry in cues.items():
        master = render(entry, store)
        target = args.output / f"{name}.ogg"
        decoded = write_ogg(target, master)
        sf.write(str(args.preview / f"{name}.wav"), master.astype(np.float32), RATE, subtype="PCM_16")
        keys = sorted({part["source"].removesuffix(":rev") for part in entry[2] if "source" in part
                       and not part["source"].startswith("synth:")})
        report["cues"][name] = metrics(decoded) | {
            "ogg": target.relative_to(ROOT).as_posix() if target.is_relative_to(ROOT) else str(target),
            "ogg_sha256": sha256(target), "sources": keys}
    for key in sorted(store.used):
        path = store.index[key]
        report["sources"][key] = {"path": path.relative_to(store.store).as_posix() if path.is_relative_to(store.store)
                                  else path.relative_to(ROOT).as_posix(),
                                  "origin": store.origin(key), "sha256": sha256(path)}
    (args.preview / "azure-sfx-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"{len(report['cues'])} cues, {len(report['sources'])} recordings")


if __name__ == "__main__":
    main()
