"""Export the Doll weapon pixel art (DW01-DW05) from the Codex delivery (no painting).

Inputs are the manifest recommendations of the external delivery
asset-deliveries/doll-weapons/2026-10-02/alpha (hash-pinned in INPUTS; the brief is
that folder's BRIEF.md). Every step is mechanical and reuses tools/export_ebon_art.py
(clean_alpha, block_mode, sha256) unmodified:

1. Pin: every input must match its SHA-256.
2. Alpha: below 16 becomes 0, 240 and above becomes 255 (clean_alpha).
3. Objects: 8-connected components of alpha > 128. Specks under 64 px are dropped,
   the rest join when their gap is about 12 px or less. The count is checked per sheet
   and objects are sorted row-major by the brief's cell grid.
4. Dot pitch, per sheet: strong colour or alpha edges inside the objects. The coarse
   pitch is the period (3.0-24.0 px, 0.02 steps) whose edge phases agree best inside
   64 px tiles; that agreement is the reported coherence. It is lifted to the
   fundamental when a whole multiple is as coherent, then refined over whole lines
   within +-3 %. A pitch within 0.02 px of the 24 px ceiling is flagged. Edits use their base sheet's pitch so a family registers
   (DW01B->DW01, DW02E->DW02, DW04V2/V3->DW04); their own pitch is recorded.
5. Palette: every opaque pixel snaps to the fixed Doll palette by OKLab nearest
   (deltaE = 100 x OKLab distance, reported per object). An object whose mean deltaE
   exceeds 6 falls back to a 32-colour median cut (block_mode), reported.
6. Integer reduction by k, the nearest integer of measured dots / brief dots on the
   brief's major axis; a few assets also list the rung the review names. A texel is
   the majority palette colour of its (pitch*k)^2 cell and is opaque at >= 50 %
   coverage. On the outer boundary a cell next to the body's interior with >= 25 %
   coverage and at least half a dot of ink becomes ink, which keeps the one-dot
   outline; features with no interior (thin talons at k = 4) keep their majority
   colour. The report gives the ink share of the outer boundary (target 90 %).
   --reink forces every outer boundary texel to ink and is recorded as a human
   modification.
   Icons fit <= 32x32 logical stored at 2x; the buff fits 16x16 logical stored 32x32.
   They use integer k when it fills >= 70 % of the limit, otherwise a bounded
   fractional majority fit (reported).
7. Frame sets become equal-cell strips registered on their anchor: the key on the
   shaft bottom, the iris on the ring centre, the choristers (one cell for all three
   voices) on the stand tip, the shards on their centroid.
8. Gun parts: DW02E is registered onto DW02 by the integer shift (+-16 px) with the
   best opaque IoU, refined +-8 px around each cut brass region. The cut is DW02's
   non-ivory pixels the edit does not repeat within deltaE 10, opened by 2 dots;
   bodies of >= 4 texels that are >= 70 % brass/ink keep the cut brass connected to
   them and the cut ink within 1.5 dots. Brass fittings separated by an ink row (the
   sight foot, the key stub) join the part they reach within 6 dots. Labels: sight =
   the part with a see-through hole, shroud = aspect >= 4, spring housing = rearmost,
   cylinder = the rest. MeridianGun, MeridianBare and the part seats share one canvas.
9. Anchors are texel coordinates: (0, 0) is the top-left corner, a texel centre is
   +0.5; extremes (muzzle, tips of the silhouette, stand tip, stake point) sit on the
   texel edge. They go to report.json and, with --write-repo, to
   Client/Encounters/FirstSeverance/Weapons/DollArtAnchors.g.cs.
10. The contact sheet shows each sprite at 1x, 8x and in-game size (2 px per texel)
   next to a 20x42 px player silhouette on dark #121017 and bright #bac6d6;
   anchors.png marks every anchor at 8x for review.

Texel rule: 1 texel = 1 dot = 2 world px, point sampled. Size changes only through
integer export rungs (k), never through draw-time scaling of pixel sprites. A second
rung is named <Name>_L (smaller k, larger sprite) or <Name>_S (larger k).

The default run writes only to .local/doll-weapon-art/ (ignored by Git): PNGs,
report.json, contact.png, anchors.png and DollArtAnchors.g.cs.txt (a preview that the
Mod build cannot pick up). --write-repo also writes the runtime PNGs to
Assets/Textures/Items/DollWeapons/ and the generated anchors; that belongs to the
per-weapon art PRs after the owner's art review.
Requires Pillow, numpy and scipy (local asset tools, not CI). Run it with the Python 3.12
that has them (the plain `py -3` may not), pointing PYTHONPATH at a local dependency folder
when they are not installed:
  py -3.12 tools/export_doll_weapon_art.py [--source <delivery folder>]
  py -3.12 tools/export_doll_weapon_art.py --write-repo [--only <output names>]
--only limits what --write-repo puts into the repository to the named outputs (a bare
name also takes its _L/_S rungs, e.g. ClawOpen takes ClawOpen_L), so art can land one
weapon per PR. The generated anchors then cover those outputs plus every runtime output
whose PNG is already in the repository.
The delivery folder (asset-deliveries/doll-weapons/2026-10-02) must hold manifest.json.
"""
import argparse
import hashlib
import io
import json
import math
import os
import platform
import sys
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
import scipy
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage, signal
import PIL

sys.path.insert(0, str(Path(__file__).resolve().parent))
from export_ebon_art import block_mode, clean_alpha, sha256  # noqa: E402  (shared recipe helpers, unmodified)

ROOT = Path(__file__).resolve().parents[1]
PREVIEW_DIR = ROOT / ".local" / "doll-weapon-art"
TEXTURE_REL = Path("Assets") / "Textures" / "Items" / "DollWeapons"
ANCHORS_REL = Path("Client") / "Encounters" / "FirstSeverance" / "Weapons" / "DollArtAnchors.g.cs"
TEXTURE_ASSET_ROOT = "Convergence/Assets/Textures/Items/DollWeapons/"
DELIVERY = Path("asset-deliveries") / "doll-weapons" / "2026-10-02"
DELIVERY_ENV = "CONVERGENCE_DOLL_WEAPON_DELIVERY"
GENERATED_HEADER = "generated by tools/export_doll_weapon_art.py; do not edit"

# delivery file -> SHA-256 of the exact manifest recommendation
INPUTS = {
    "alpha/DW01_a.png": "6c7e04b11e5cf2e30c150cc2cbaa958880f4374aeebaf74c93e4fd2df035f43b",
    "alpha/DW01B_a.png": "e19fd8362268c07ea9db2e07b865bb96d8ee0ef6c6a14749742949ec85daf101",
    "alpha/DW01I_a.png": "22b6d558821348b6d6acf8e8202684c247b46c4e76cb9fdcb69361291f11fe49",
    "alpha/DW02_a.png": "ceaf1423c2f381242d5557edf4d121a85f5afca896979c4d40c857f4ddc41026",
    "alpha/DW02E_a.png": "86aa6a8aade4fc1d429309393ff27d60c6c16d1600668b3d235469a6f55314ef",
    "alpha/DW02K_a.png": "f96923dca52da7c57c94c980d3d4bd5fab967a7163a326c508bd898679d91e1f",
    "alpha/DW02I_a.png": "96239792625ad0b7791fc98713bf504c5fc867a6108c86c2d7f89ee73ddc1ed6",
    "alpha/DW03_b.png": "55658616da6acc0b23df076daf00995cbd7ff56c66b052c7f6866bbffa82b316",
    "alpha/DW03I_a.png": "860d363cb63520cc39dd20de7bb1a64d8493c44d108f760001bcb074cc6d54d1",
    "alpha/DW03A_d.png": "93847c4c11d6cd607aa0c191c4ce2eee644fd53945b4325cbe99d153c45d60d5",
    "alpha/DW03B_c.png": "eeea2e95f4aa9e71765505d0a1d9e78862c3fed326ea094281b6b432123b0056",
    "alpha/DW04_a.png": "f105dbfe715a4b921e15021ea14717744131a502d2b5c188507df6527ed39f11",
    "alpha/DW04V2_a.png": "ff71c603eeaae988e3684a2b4bc5bc69ac9a5e6a4167a9c624626008b6b75a32",
    "alpha/DW04V3_a.png": "dc3f054b9568d18cf129d5d774fc12f624e244d1d6c488084758b84c428b298a",
    "alpha/DW04P_c.png": "75c958cdd35ccd65bb141f195ab15e356fa67c6c813d44406023781efe86bee5",
    "alpha/DW04S_c.png": "fa4fdf2a5bc61c87718e22941544f669017d588c9ea75829465aff49ee878928",
    "alpha/DW04B_a.png": "8e66aa1d211d298e58f7ad337155eb1e8a250e5f692683a18bde7b362f1d0baa",
    "alpha/DW05_b.png": "76a4967fee68e5222e691269ca6a1d7f671f468400c9eb42ff1464879d42573b",
    "alpha/DW05V_a.png": "5c1853e477e4dc97de579ec25c12fde9f101ca5ddc6d7dc47b404308a08eecf4",
    "alpha/DW05S_a.png": "9d6b02f0e072a98b9a6d7d51b4d6a6f809d3e6753cacaa3a4847f347ab89dc4a",
    "alpha/DW05I_a.png": "ac2a79946ca2232d1419eb0d7d905ba2b83d9b89bdeb9475c670483f13e905e0",
}


@dataclass(frozen=True)
class Sheet:
    file: str
    grid: tuple       # the brief's layout (columns, rows)
    count: int        # objects expected
    family: str = ""  # base sheet whose pitch an edit uses


SHEETS = {
    "DW01": Sheet("alpha/DW01_a.png", (2, 1), 2),
    "DW01B": Sheet("alpha/DW01B_a.png", (2, 1), 2, "DW01"),
    "DW01I": Sheet("alpha/DW01I_a.png", (1, 1), 1),
    "DW02": Sheet("alpha/DW02_a.png", (1, 1), 1),
    "DW02E": Sheet("alpha/DW02E_a.png", (1, 1), 1, "DW02"),
    "DW02K": Sheet("alpha/DW02K_a.png", (2, 2), 4),
    "DW02I": Sheet("alpha/DW02I_a.png", (1, 1), 1),
    "DW03": Sheet("alpha/DW03_b.png", (1, 1), 1),
    "DW03I": Sheet("alpha/DW03I_a.png", (1, 1), 1),
    "DW03A": Sheet("alpha/DW03A_d.png", (2, 2), 4),
    "DW03B": Sheet("alpha/DW03B_c.png", (1, 1), 1),
    "DW04": Sheet("alpha/DW04_a.png", (3, 1), 3),
    "DW04V2": Sheet("alpha/DW04V2_a.png", (3, 1), 3, "DW04"),
    "DW04V3": Sheet("alpha/DW04V3_a.png", (3, 1), 3, "DW04"),
    "DW04P": Sheet("alpha/DW04P_c.png", (1, 1), 1),
    "DW04S": Sheet("alpha/DW04S_c.png", (1, 1), 1),
    "DW04B": Sheet("alpha/DW04B_a.png", (1, 1), 1),
    "DW05": Sheet("alpha/DW05_b.png", (1, 1), 1),
    "DW05V": Sheet("alpha/DW05V_a.png", (1, 1), 1),
    "DW05S": Sheet("alpha/DW05S_a.png", (3, 1), 3),
    "DW05I": Sheet("alpha/DW05I_a.png", (1, 1), 1),
}

# k proposed by the cross-weapon review (rule: nearest integer of measured / brief dots)
REVIEW_K = {"DW01": 2, "DW01B": 2, "DW02": 3, "DW02E": 3, "DW02K": 1, "DW03": 2, "DW03A": 2, "DW03B": 1,
            "DW04": 2, "DW04V2": 2, "DW04V3": 2, "DW04P": 1, "DW04S": 2, "DW05": 2, "DW05V": 2, "DW05S": 2}
# second rungs the review names: the claws and the thrown blade also use k = 1 (about double size)
EXTRA_RUNGS = {"DW01": (1,), "DW01B": (1,), "DW05": (1,)}

# the brief's fixed Doll palette (BRIEF.md section 4). A dark socket/shadow tone is added
# only if measurement shows a dark off-palette cluster (see socket_check); none has so far.
PALETTE = (
    ("Ink", "#121017"),
    ("Iron0", "#1d1a22"), ("Iron1", "#302a29"), ("Iron2", "#49404a"),
    ("Porcelain0", "#9c8070"), ("Porcelain1", "#d0b69e"), ("Porcelain2", "#f4e3ce"), ("Porcelain3", "#fcf4e6"),
    ("Pearl0", "#c9c4c9"), ("Pearl1", "#e1dce0"),
    ("Brass0", "#684828"), ("Brass1", "#a07b48"), ("Brass2", "#d5b279"),
    ("Glass", "#301840"),
    ("Ruby0", "#8c141c"), ("Ruby1", "#4c040c"),
)
PALETTE_RGB = np.array([[int(h[i:i + 2], 16) for i in (1, 3, 5)] for _, h in PALETTE], dtype=np.uint8)
INK = 0
IRON = (1, 2, 3)
PORCELAIN = (4, 5, 6, 7)
PEARL = (8, 9)
BRASS = (10, 11, 12)
RUBY = (14, 15)
DARK = (0, 1, 2, 13, 15)
IVORY = PORCELAIN + PEARL

OPAQUE = 128            # alpha > 128 is opaque
TALONS = 5              # talon tips of an open hand; a pose with fewer is reported
SPECK = 64              # px; smaller opaque components are residue
JOIN = 6                # px dilation radius: parts whose gap is ~12 px or less join
EDGE = 48               # RGB L1 difference that counts as a drawn edge
PITCH_MIN, PITCH_MAX = 3.0, 24.0   # px; the search range of the dot pitch
PERIODS = np.round(np.arange(PITCH_MIN, PITCH_MAX + 0.0001, 0.02), 2)
TILE = 64
SUBHARMONIC = 0.85      # a multiple this coherent is the fundamental
REFINE = 0.03           # line refinement window (+-3 %)
REFINE_STEP = 0.005
LOW_COHERENCE = 0.30
MEAN_DE_LIMIT = 6.0
FALLBACK_COLORS = 32
OFF_PALETTE_DE = 8.0
SOCKET_SHARE = 0.01
ICON_LIMIT, BUFF_LIMIT = 32, 16
ICON_FILL = 0.70
INK_MIN_DOTS = 0.5      # ink pixels (in dots) that make a boundary cell ink
EIGHT = np.ones((3, 3), dtype=bool)
FOUR = ndimage.generate_binary_structure(2, 1)
DARK_BG, BRIGHT_BG = (0x12, 0x10, 0x17), (0xba, 0xc6, 0xd6)


def round_half_up(value):
    return int(math.floor(value + 0.5))


def r2(value, digits=2):
    return round(float(value), digits)


def disk(radius):
    span = np.arange(-radius, radius + 1)
    return np.hypot(span[None, :], span[:, None]) <= radius


# ---------------------------------------------------------------- colour

def oklab(rgb):
    """sRGB (..., 3) 0-255 -> OKLab (..., 3)."""
    c = np.asarray(rgb, dtype=np.float64) / 255.0
    lin = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    m1 = np.array([[0.4122214708, 0.5363325363, 0.0514459929],
                   [0.2119034982, 0.6806995451, 0.1073969566],
                   [0.0883024619, 0.2817188376, 0.6299787005]])
    m2 = np.array([[0.2104542553, 0.7936177850, -0.0040720468],
                   [1.9779984951, -2.4285922050, 0.4505937099],
                   [0.0259040371, 0.7827717662, -0.8086757660]])
    return np.cbrt(lin @ m1.T) @ m2.T


PALETTE_LAB = oklab(PALETTE_RGB)


def snap(rgba):
    """Palette index (int16, -1 where not opaque) and deltaE (float32, nan) per pixel."""
    opaque = rgba[..., 3] > OPAQUE
    idx = np.full(opaque.shape, -1, dtype=np.int16)
    de = np.full(opaque.shape, np.nan, dtype=np.float32)
    if not opaque.any():
        return idx, de
    rgb = rgba[..., :3][opaque].astype(np.int64)
    packed = (rgb[:, 0] << 16) | (rgb[:, 1] << 8) | rgb[:, 2]
    unique, inverse = np.unique(packed, return_inverse=True)
    colours = np.stack([(unique >> 16) & 255, (unique >> 8) & 255, unique & 255], axis=1)
    distance = np.linalg.norm(oklab(colours)[:, None, :] - PALETTE_LAB[None, :, :], axis=2)
    nearest = distance.argmin(axis=1)
    idx[opaque] = nearest[inverse].astype(np.int16)
    de[opaque] = (distance.min(axis=1) * 100.0)[inverse].astype(np.float32)
    return idx, de


def palette_stats(rgba, idx, de):
    """Snap error of one object: mean/p95/max deltaE, tone usage, the largest off-palette clusters."""
    opaque = idx >= 0
    values = de[opaque]
    usage = np.bincount(idx[opaque], minlength=len(PALETTE)) / max(1, opaque.sum())
    far = opaque & (de > OFF_PALETTE_DE)
    clusters = []
    if far.any():
        q = (rgba[..., :3][far].astype(np.int64) // 16) * 16 + 8
        packed = (q[:, 0] << 16) | (q[:, 1] << 8) | q[:, 2]
        unique, counts = np.unique(packed, return_counts=True)
        for i in np.argsort(-counts, kind="stable")[:3]:
            colour = np.array([(unique[i] >> 16) & 255, (unique[i] >> 8) & 255, unique[i] & 255])
            lab = oklab(colour)
            clusters.append({"rgb": "#%02x%02x%02x" % tuple(int(v) for v in colour),
                             "share": r2(counts[i] / opaque.sum(), 4),
                             "lightness": r2(lab[0], 3),
                             "nearest": PALETTE[int(np.linalg.norm(PALETTE_LAB - lab, axis=1).argmin())][1]})
    return {"mean_de": r2(values.mean()), "p95_de": r2(np.percentile(values, 95)), "max_de": r2(values.max()),
            "off_palette_share": r2(far.sum() / max(1, opaque.sum()), 4), "off_palette_clusters": clusters,
            "usage": {PALETTE[i][1]: r2(u, 4) for i, u in enumerate(usage) if u > 0}}


def socket_check(stats):
    """A dark off-palette cluster lighter than ink and >= 1 % of the object asks for a socket tone."""
    ink_l = float(PALETTE_LAB[INK][0])
    return [c for c in stats["off_palette_clusters"]
            if c["lightness"] < 0.32 and c["lightness"] > ink_l + 0.03 and c["share"] >= SOCKET_SHARE]


def render(texels):
    out = np.zeros(texels.shape + (4,), dtype=np.uint8)
    opaque = texels >= 0
    out[opaque, :3] = PALETTE_RGB[texels[opaque]]
    out[opaque, 3] = 255
    return Image.fromarray(out, "RGBA")


def png_bytes(image):
    buffer = io.BytesIO()
    image.save(buffer, format="PNG", optimize=True)
    return buffer.getvalue()


# ---------------------------------------------------------------- objects

@dataclass
class Obj:
    sheet: str
    index: int
    cell: tuple
    box: tuple            # sheet px (x0, y0, x1, y1), exclusive
    rgba: np.ndarray      # crop with neighbours removed
    idx: np.ndarray = None
    de: np.ndarray = None
    palette: dict = None

    @property
    def size(self):
        return self.box[2] - self.box[0], self.box[3] - self.box[1]

    @property
    def opaque(self):
        return self.rgba[..., 3] > OPAQUE


def find_objects(rgba, grid, expected, name="sheet"):
    """8-connected alpha > 128 components; specks dropped; gaps of ~12 px or less joined."""
    opaque = rgba[..., 3] > OPAQUE
    labels, count = ndimage.label(opaque, structure=EIGHT)
    sizes = np.bincount(labels.ravel(), minlength=count + 1)[1:]
    kept = np.isin(labels, np.nonzero(sizes >= SPECK)[0] + 1)
    residue = opaque & ~kept
    dropped = {"count": int((sizes < SPECK).sum()), "area": int(residue.sum())}
    joined, n = ndimage.label(ndimage.binary_dilation(kept, structure=disk(JOIN)), structure=EIGHT)
    cw, ch = rgba.shape[1] / grid[0], rgba.shape[0] / grid[1]
    objects = []
    for index in range(1, n + 1):
        region = joined == index
        solid = kept & region
        ys, xs = np.nonzero(solid)
        if not len(xs):
            continue
        box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
        x0, y0, x1, y1 = box
        crop = rgba[y0:y1, x0:x1].copy()
        crop[~(region[y0:y1, x0:x1] & ~residue[y0:y1, x0:x1])] = 0
        centre = (float(xs.mean()), float(ys.mean()))
        cell = (min(grid[0] - 1, int(centre[0] // cw)), min(grid[1] - 1, int(centre[1] // ch)))
        objects.append(Obj(name, 0, cell, box, crop))
    if len(objects) != expected:
        raise ValueError(f"{name}: found {len(objects)} objects, expected {expected}")
    objects.sort(key=lambda o: (o.cell[1], o.cell[0], o.box[0], o.box[1]))
    for i, obj in enumerate(objects):
        obj.index = i
    cells = [o.cell for o in objects]
    crossing = [list(o.box) for o in objects
                if int(o.box[0] // cw) != int((o.box[2] - 1) // cw) or int(o.box[1] // ch) != int((o.box[3] - 1) // ch)]
    return objects, dropped, len(set(cells)) == len(cells), crossing


def prepare_objects(rgba, grid, expected, name="sheet"):
    """Objects of a cleaned sheet with their palette indices, snap error and palette stats."""
    objects, dropped, unique_cells, crossing = find_objects(rgba, grid, expected, name)
    idx, de = snap(rgba)
    for obj in objects:
        x0, y0, x1, y1 = obj.box
        obj.idx = np.where(obj.opaque, idx[y0:y1, x0:x1], -1).astype(np.int16)
        obj.de = np.where(obj.opaque, de[y0:y1, x0:x1], np.nan).astype(np.float32)
        obj.palette = palette_stats(obj.rgba, obj.idx, obj.de)
    return objects, idx, dropped, unique_cells, crossing


# ---------------------------------------------------------------- dot pitch

def edge_maps(rgba):
    rgb = rgba[..., :3].astype(np.int32)
    opaque = rgba[..., 3] > OPAQUE
    dx = np.abs(np.diff(rgb, axis=1)).sum(-1)
    dy = np.abs(np.diff(rgb, axis=0)).sum(-1)
    ex = ((dx >= EDGE) & opaque[:, 1:] & opaque[:, :-1]) | (opaque[:, 1:] != opaque[:, :-1])
    ey = ((dy >= EDGE) & opaque[1:, :] & opaque[:-1, :]) | (opaque[1:, :] != opaque[:-1, :])
    return ex, ey


def _tile_spectrum(edges, axis, periods):
    """Sum over 64 px tiles of |mean edge phase| x count (the grid may drift between tiles)."""
    phase = np.exp(2j * np.pi * (np.arange(TILE)[None, :] + 1) / periods[:, None])
    total, weight = np.zeros(len(periods)), 0
    h, w = edges.shape
    for y0 in range(0, h, TILE):
        for x0 in range(0, w, TILE):
            pos = np.nonzero(edges[y0:y0 + TILE, x0:x0 + TILE])[1 if axis == 1 else 0]
            if len(pos) < 12:
                continue
            total += np.abs(phase @ np.bincount(pos, minlength=TILE)[:TILE])
            weight += len(pos)
    return total, weight


def _line_spectrum(edges, axis, periods):
    """Sum over whole lines of |sum of edge phases| (long baseline for the refinement)."""
    lines = edges if axis == 1 else edges.T
    used = lines[lines.sum(axis=1) >= 4].astype(np.float64)
    if not len(used):
        return np.zeros(len(periods)), 0
    phase = np.exp(2j * np.pi * (np.arange(lines.shape[1])[None, :] + 1) / periods[:, None])
    return np.abs(phase @ used.T).sum(axis=1), float(used.sum())


def _spectrum(crops, periods, kind):
    total, weight = np.zeros(len(periods)), 0
    for rgba in crops:
        ex, ey = edge_maps(rgba)
        for axis, edges in ((1, ex), (0, ey)):
            t, w = kind(edges, axis, periods)
            total, weight = total + t, weight + w
    return total / max(weight, 1), weight


def spacing_mode(crops):
    gaps = np.zeros(33, dtype=np.int64)
    for rgba in crops:
        for axis, edges in zip((1, 0), edge_maps(rgba)):
            lines = edges if axis == 1 else edges.T
            for line in lines:
                xs = np.nonzero(line)[0]
                if len(xs) > 1:
                    d = np.diff(xs)
                    d = d[(d >= 3) & (d <= 32)]
                    gaps += np.bincount(d, minlength=33)[:33]
    return int(gaps.argmax())


def measure_pitch(crops):
    """Edge-phase coherence pitch (see module notes); returns the record and the used pitch."""
    spectrum, edges = _spectrum(crops, PERIODS, _tile_spectrum)
    best = int(spectrum.argmax())
    coarse, lifted_from = float(PERIODS[best]), None
    for m in range(2, 6):
        target = m * PERIODS[best]
        if target > PERIODS[-1] + 1e-9:
            break
        near = np.abs(PERIODS - target) <= 0.06 * m
        if spectrum[near].max() >= SUBHARMONIC * spectrum[best]:
            coarse, lifted_from = float(PERIODS[near][spectrum[near].argmax()]), float(PERIODS[best])
    coherence = float(spectrum[int(np.abs(PERIODS - coarse).argmin())])
    window = np.arange(coarse * (1 - REFINE), coarse * (1 + REFINE), REFINE_STEP)
    line, _ = _spectrum(crops, window, _line_spectrum)
    peak = int(line.argmax())
    at_edge = peak in (0, len(window) - 1)
    refined = float(window[peak])
    if not at_edge:
        y0, y1, y2 = line[peak - 1:peak + 2]
        curve = y0 - 2 * y1 + y2
        if curve < 0:
            refined += 0.5 * (y0 - y2) / curve * REFINE_STEP
    used = coarse if at_edge else refined
    record = {"coarse_px": r2(coarse), "coherence": r2(coherence, 3), "refined_px": r2(refined, 3),
              "line_coherence": r2(line[peak], 3), "refine_at_window_edge": bool(at_edge),
              "lifted_from_px": r2(lifted_from) if lifted_from else None,
              "at_search_ceiling": bool(coarse >= PERIODS[-1] - 0.02),
              "edge_spacing_mode_px": spacing_mode(crops), "edges": int(edges), "dot_px": r2(used, 3)}
    return record, used


# ---------------------------------------------------------------- reduction

@dataclass
class Grid:
    ox: float
    oy: float
    cx: float
    cy: float
    nx: int
    ny: int

    def lines(self, axis):
        o, c, n = (self.ox, self.cx, self.nx) if axis == 0 else (self.oy, self.cy, self.ny)
        return np.floor(o + np.arange(n + 1) * c + 0.5).astype(np.int64)

    def texel(self, point, offset=(0, 0)):
        """Continuous canvas px -> texel coordinates of the trimmed output."""
        return [r2((point[0] - self.ox) / self.cx - offset[0]), r2((point[1] - self.oy) / self.cy - offset[1])]

    def column(self, x_px, offset=(0, 0)):
        """Texel-centre x of the column holding canvas pixel column x_px."""
        return r2(math.floor((x_px + 0.5 - self.ox) / self.cx) + 0.5 - offset[0])


def object_grid(size, dot_px, k):
    w, h = size
    nx, ny = max(1, round_half_up(w / (dot_px * k))), max(1, round_half_up(h / (dot_px * k)))
    return Grid(0.0, 0.0, w / nx, h / ny, nx, ny)


def exterior_of(transparent):
    """Transparent cells 4-connected to the outside."""
    padded = np.pad(transparent, 1, constant_values=True)
    labels, _ = ndimage.label(padded, structure=FOUR)
    return (labels == labels[0, 0])[1:-1, 1:-1]


def reduce_cells(idx, grid, dot_area, reink=False, ink_rule="interior"):
    """Majority palette colour per grid cell; the ink rule keeps the outer outline."""
    h, w = idx.shape
    xl, yl = grid.lines(0), grid.lines(1)
    col = np.searchsorted(xl, np.arange(w), side="right") - 1
    row = np.searchsorted(yl, np.arange(h), side="right") - 1
    col[(np.arange(w) < xl[0]) | (np.arange(w) >= xl[-1])] = -1
    row[(np.arange(h) < yl[0]) | (np.arange(h) >= yl[-1])] = -1
    ys, xs = np.nonzero(idx >= 0)
    cells_x, cells_y = col[xs], row[ys]
    inside = (cells_x >= 0) & (cells_y >= 0)
    n = len(PALETTE)
    key = (cells_y[inside] * grid.nx + cells_x[inside]) * n + idx[ys[inside], xs[inside]]
    counts = np.bincount(key, minlength=grid.nx * grid.ny * n).reshape(grid.ny, grid.nx, n)
    area = np.outer(np.diff(yl), np.diff(xl)).astype(np.float64)
    coverage = counts.sum(axis=2) / np.maximum(area, 1)
    majority = counts.argmax(axis=2).astype(np.int16)
    opaque = coverage >= 0.5
    exterior = exterior_of(~opaque)
    near_exterior = ndimage.binary_dilation(exterior, structure=FOUR)
    near_opaque = ndimage.binary_dilation(opaque, structure=FOUR)
    candidates = (opaque & near_exterior) | (~opaque & exterior & near_opaque & (coverage >= 0.25))
    if ink_rule == "interior":
        interior = opaque & ~near_exterior
        candidates &= ndimage.binary_dilation(ndimage.binary_dilation(interior, structure=FOUR), structure=FOUR)
    elif ink_rule == "none":
        candidates &= False
    has_ink = counts[..., INK] >= INK_MIN_DOTS * dot_area
    to_ink = candidates & has_ink
    texels = np.where(opaque, majority, -1).astype(np.int16)
    texels[to_ink] = INK
    final_opaque = texels >= 0
    final_exterior = exterior_of(~final_opaque)
    boundary = final_opaque & ndimage.binary_dilation(final_exterior, structure=FOUR)
    share_before = float((texels[boundary] == INK).mean()) if boundary.any() else 1.0
    if reink:
        texels[boundary] = INK
    stats = {"ink_rule_texels": int(to_ink.sum()), "ink_rule_added_opacity": int((to_ink & ~opaque).sum()),
             "boundary_texels": int(boundary.sum()), "ink_boundary_share": r2(share_before, 3),
             "reinked": bool(reink)}
    return texels, stats


def trim(texels):
    ys, xs = np.nonzero(texels >= 0)
    if not len(xs):
        return texels, (0, 0)
    x0, y0 = int(xs.min()), int(ys.min())
    return texels[y0:int(ys.max()) + 1, x0:int(xs.max()) + 1], (x0, y0)


def source_ink_share(idx, dot_px):
    """Share of ink on the outer boundary at the dot level (k = 1) for the retention figure."""
    texels, _ = reduce_cells(idx, object_grid(idx.shape[::-1], dot_px, 1), dot_px * dot_px)
    opaque = texels >= 0
    boundary = opaque & ndimage.binary_dilation(exterior_of(~opaque), structure=FOUR)
    return float((texels[boundary] == INK).mean()) if boundary.any() else 1.0


def rule_k(dots, brief):
    """k = nearest integer of measured dots / brief dots on the brief's major axis."""
    axis = 0 if brief[0] > brief[1] else 1 if brief[1] > brief[0] else int(dots[1] > dots[0])
    ratio = dots[axis] / brief[axis]
    return max(1, round_half_up(ratio)), ratio


# ---------------------------------------------------------------- measurements

def components(mask, structure=EIGHT, min_area=1):
    """Connected components of at least min_area pixels, as full-size masks, in label order."""
    labels, n = ndimage.label(mask, structure=structure)
    sizes = np.bincount(labels.ravel(), minlength=n + 1)
    out = []
    for i, box in enumerate(ndimage.find_objects(labels), start=1):
        if box is None or sizes[i] < min_area:
            continue
        part = np.zeros(mask.shape, dtype=bool)
        part[box] = labels[box] == i
        out.append(part)
    return out


def centroid(mask):
    ys, xs = np.nonzero(mask)
    return float(xs.mean()) + 0.5, float(ys.mean()) + 0.5


def bbox(mask):
    ys, xs = np.nonzero(mask)
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def enclosed_holes(opaque, min_area=1):
    """Transparent regions fully enclosed by opaque pixels, largest first: (area, centre, box, mask)."""
    holes = ndimage.binary_fill_holes(opaque) & ~opaque
    found = []
    for part in components(holes, FOUR, min_area):
        found.append((int(part.sum()), centroid(part), bbox(part), part))
    found.sort(key=lambda h: (-h[0], h[1]))
    return found


def fit_circle(mask):
    """Least-squares (Kasa) circle through the pixel centres of mask: centre and radius."""
    ys, xs = np.nonzero(mask)
    x, y = xs + 0.5, ys + 0.5
    a = np.column_stack([x, y, np.ones_like(x)])
    sol, *_ = np.linalg.lstsq(a, x * x + y * y, rcond=None)
    cx, cy = sol[0] / 2, sol[1] / 2
    return (float(cx), float(cy)), float(math.sqrt(max(0.0, sol[2] + cx * cx + cy * cy)))


def in_group(idx, group):
    return np.isin(idx, group if isinstance(group, tuple) else (group,))


def claw_anchors(idx, dot_px):
    """Wrist cuff pivot, six beads (2 rows x 3), bead-plate centroid and talon tips, in crop px."""
    h, w = idx.shape
    xx = np.arange(w)[None, :]
    result, notes = {}, []
    iron = in_group(idx, IRON)
    cuff_parts = components(iron & (xx < 0.2 * w), EIGHT, max(1, int(dot_px * dot_px)))
    pivot = None
    if cuff_parts:
        cuff = max(cuff_parts, key=lambda p: int(p.sum()))
        x0, y0, x1, y1 = bbox(cuff)
        cy = centroid(cuff)[1]
        pivot = (x0 + 0.5, cy)
        band = in_group(idx, BRASS) & ndimage.binary_dilation(cuff, structure=disk(max(1, int(dot_px))))
        result["cuff"] = {"box": [x0, y0, x1, y1], "min_x": x0, "centroid_y": cy,
                          "brass_band": list(bbox(band)) if band.any() else None}
    else:
        notes.append("no iron cuff found in the leftmost 20 % of columns")
    brass = in_group(idx, BRASS)
    dark = in_group(idx, DARK)
    dot_area = dot_px * dot_px
    best = None
    for plate in components(brass, EIGHT, int(4 * dot_area)):
        beads = []
        for area, centre, box, part in enclosed_holes(plate, max(1, int(2 * dot_area))):
            if (dark & part).sum() >= 0.4 * area:
                beads.append({"centre": centre, "area": area})
        if beads:  # beads are alike; small dark pockets of the plate border are not beads
            largest = max(b["area"] for b in beads)
            beads = [b for b in beads if b["area"] >= 0.35 * largest]
        if best is None or len(beads) > len(best[1]) or (len(beads) == len(best[1]) and plate.sum() > best[0].sum()):
            best = (plate, beads)
    if best is not None and best[1]:
        plate, beads = best
        filled = ndimage.binary_fill_holes(plate)
        result["plate"] = {"box": list(bbox(plate)), "centroid": centroid(filled)}
        result["beads"] = order_rows([b["centre"] for b in beads], dot_px)
        if len(beads) != 6:
            notes.append(f"found {len(beads)} beads, expected 6")
    else:
        notes.append("no brass bead plate with enclosed dark beads")
        result["beads"] = []
    if pivot is not None:
        result["pivot"] = pivot
        result["tips"] = talon_tips(in_group(idx, IVORY), pivot, dot_px)
        if len(result["tips"]) < TALONS:
            notes.append(f"found {len(result['tips'])} talon tips, expected {TALONS}; tips are ordered by angle, so an "
                         f"index is not a fixed finger")
    return result, notes


def order_rows(points, dot_px):
    """Group points into rows (gap > 1.5 dots in y), each row sorted by x."""
    pts = sorted(points, key=lambda p: (p[1], p[0]))
    rows, row = [], []
    for p in pts:
        if row and p[1] - row[-1][1] > 1.5 * dot_px:
            rows.append(row)
            row = []
        row.append(p)
    if row:
        rows.append(row)
    return [p for r in rows for p in sorted(r, key=lambda q: q[0])]


def talon_tips(ivory, pivot, dot_px, limit=TALONS):
    """Talon tips: local maxima of the ivory radius around the pivot (prominence >= 3 dots and
    >= 55 % of the farthest), at most `limit` of them (the farthest ones), returned sorted by
    atan2(dy, dx) from the pivot in image coordinates (y down): the topmost tip comes first.

    The index is an angular rank, not a finger identity. A pose that shows fewer tips (a
    clenched or thrusting hand hides or merges some) shifts every later index, so a check
    must pair tips with design talons by distance rather than by index.
    """
    ys, xs = np.nonzero(ivory)
    if not len(xs):
        return []
    dx, dy = xs + 0.5 - pivot[0], ys + 0.5 - pivot[1]
    radius, angle = np.hypot(dx, dy), np.arctan2(dy, dx)
    bins = 720
    b = ((angle + np.pi) / (2 * np.pi) * bins).astype(np.int64) % bins
    order = np.lexsort((-radius, b))
    first = np.r_[True, b[order][1:] != b[order][:-1]]
    chosen = order[first]
    rmax = np.zeros(bins)
    arg = np.full(bins, -1)
    rmax[b[chosen]] = radius[chosen]
    arg[b[chosen]] = chosen
    peaks, _ = signal.find_peaks(np.concatenate([rmax, rmax, rmax]), prominence=3 * dot_px, distance=4)
    peaks = sorted({int(p - bins) for p in peaks if bins <= p < 2 * bins})
    keep = [p for p in peaks if rmax[p] >= 0.55 * rmax.max()]
    keep = sorted(keep, key=lambda p: (-rmax[p], p))[:limit]
    tips = [(float(xs[arg[p]]) + 0.5, float(ys[arg[p]]) + 0.5) for p in keep]
    return sorted(tips, key=lambda t: math.atan2(t[1] - pivot[1], t[0] - pivot[0]))


def mouth_of(idx, opaque, dot_px):
    """The mouth: the largest dark cluster inside the head (upper 45 % of the figure, not touching
    the outline) in the lower head band (22-45 %). Also counts the other dark clusters in the head
    and the eye-like ones among them (compact, at least a dot, 10-32 % down)."""
    h, w = idx.shape
    yy = np.arange(h)[:, None]
    dark = in_group(idx, DARK + RUBY) & (yy < 0.45 * h)
    outside = ndimage.binary_dilation(~opaque, structure=EIGHT)
    clusters = [p for p in components(dark, EIGHT, max(1, int(0.25 * dot_px * dot_px))) if not (p & outside).any()]
    band = [p for p in clusters if 0.22 * h <= centroid(p)[1] <= 0.45 * h]
    mouth = max(band, key=lambda p: (int(p.sum()), centroid(p))) if band else None
    eyes = 0
    for p in clusters:
        if p is mouth:
            continue
        x0, y0, x1, y1 = bbox(p)
        bw, bh = x1 - x0, y1 - y0
        if (p.sum() >= dot_px * dot_px and 0.5 <= bw / bh <= 2 and p.sum() >= 0.5 * bw * bh
                and 0.10 * h <= centroid(p)[1] <= 0.32 * h):
            eyes += 1
    return (centroid(mouth) if mouth is not None else None), len(clusters) - (mouth is not None), eyes


def dark_near(texels, point, radius=1):
    """Whether a dark (or ruby) texel lies within `radius` texels of a texel-space point."""
    x, y = int(math.floor(point[0])), int(math.floor(point[1]))
    window = texels[max(0, y - radius):y + radius + 1, max(0, x - radius):x + radius + 1]
    return bool(in_group(window, DARK + RUBY).any())


def texel_extreme(texels, side):
    """Silhouette anchors on the output texels (edge or centre conventions documented per anchor)."""
    ys, xs = np.nonzero(texels >= 0)
    if side == "right":
        x = xs.max()
        return [float(x + 1), r2(ys[xs == x].mean() + 0.5)]
    if side == "left":
        x = xs.min()
        return [float(x), r2(ys[xs == x].mean() + 0.5)]
    if side == "bottom":
        y = ys.max()
        return [r2(xs[ys == y].mean() + 0.5), float(y + 1)]
    if side == "top":
        y = ys.min()
        return [r2(xs[ys == y].mean() + 0.5), float(y)]
    raise ValueError(side)


def texel_centroid(texels):
    ys, xs = np.nonzero(texels >= 0)
    return [r2(xs.mean() + 0.5), r2(ys.mean() + 0.5)]


def output_holes(texels):
    holes = enclosed_holes(texels >= 0)
    return [{"centre": [r2(c[0]), r2(c[1])], "area": a, "radius": r2(math.sqrt(a / math.pi))}
            for a, c, _, _ in holes[:3]]


# ---------------------------------------------------------------- gun parts

def shifted(array, dx, dy, fill):
    """array translated by (dx, dy) (content moves right/down), filled outside."""
    out = np.full_like(array, fill)
    h, w = array.shape[:2]
    ys, yd = slice(max(0, dy), min(h, h + dy)), slice(max(0, -dy), min(h, h - dy))
    xs, xd = slice(max(0, dx), min(w, w + dx)), slice(max(0, -dx), min(w, w - dx))
    out[ys, xs] = array[yd, xd]
    return out


def register_shift(base_opaque, edit_opaque, search=16):
    """Integer translation of the edit (content moved by +dx, +dy) that best overlaps the base (opaque IoU)."""
    corr = signal.fftconvolve(base_opaque.astype(np.float64), edit_opaque[::-1, ::-1].astype(np.float64), mode="full")
    h, w = edit_opaque.shape
    a, b = float(base_opaque.sum()), float(edit_opaque.sum())
    best = None
    for dy in range(-search, search + 1):
        for dx in range(-search, search + 1):
            inter = round(float(corr[dy + h - 1, dx + w - 1]))
            iou = inter / (a + b - inter)
            if best is None or iou > best[0] + 1e-12:
                best = (iou, dx, dy)
    return best


def colour_match(base_lab, base_opaque, edit_lab, edit_opaque, limit=10.0):
    distance = np.linalg.norm(base_lab - edit_lab, axis=-1) * 100.0
    return base_opaque & edit_opaque & (distance <= limit)


def lab_of(rgba):
    return oklab(rgba[..., :3])


def cut_gun_parts(base_rgba, base_idx, edit_rgba, dot_px, texel_px, search=16, refine=8):
    """The parts DW02 has and DW02E lacks (module notes, step 8).

    base/edit are the cleaned sheets (same canvas); returns {label: part} with sheet px
    masks, the registration record and the notes.
    """
    notes = []
    base_opaque, edit_opaque = base_rgba[..., 3] > OPAQUE, edit_rgba[..., 3] > OPAQUE
    iou, dx, dy = register_shift(base_opaque, edit_opaque, search)
    base_lab, edit_lab = lab_of(base_rgba), lab_of(edit_rgba)
    pad = search + refine + 1
    padded_lab = np.pad(edit_lab, ((pad, pad), (pad, pad), (0, 0)))
    padded_opaque = np.pad(edit_opaque, pad)

    def edit_window(y0, y1, x0, x1, sx, sy):
        """The edit translated by (sx, sy), seen through the base window [y0:y1, x0:x1]."""
        ys, xs = slice(y0 - sy + pad, y1 - sy + pad), slice(x0 - sx + pad, x1 - sx + pad)
        return padded_lab[ys, xs], padded_opaque[ys, xs]

    h, w = base_opaque.shape
    match = colour_match(base_lab, base_opaque, *edit_window(0, h, 0, w, dx, dy))
    # brass parts with their dark outline and shading; ivory never belongs to a part
    wanted = base_opaque & ~in_group(base_idx, IVORY)
    core = wanted & ~match
    margin = round_half_up(3 * dot_px)
    local = []
    brass_core = core & in_group(base_idx, BRASS)
    for region in components(brass_core, EIGHT, int(2 * dot_px * dot_px)):
        x0, y0, x1, y1 = bbox(region)
        x0, y0 = max(0, x0 - margin), max(0, y0 - margin)
        x1, y1 = min(w, x1 + margin), min(h, y1 + margin)
        best = None
        for ly in range(dy - refine, dy + refine + 1):
            for lx in range(dx - refine, dx + refine + 1):
                m = colour_match(base_lab[y0:y1, x0:x1], base_opaque[y0:y1, x0:x1], *edit_window(y0, y1, x0, x1, lx, ly))
                score = int(m.sum())
                if best is None or score > best[0]:
                    best = (score, lx, ly, m)
        _, lx, ly, m = best
        core[y0:y1, x0:x1] = wanted[y0:y1, x0:x1] & ~m
        local.append({"window": [x0, y0, x1, y1], "shift": [lx, ly]})
    # sliver filter: a 2-dot opening keeps only part bodies; each body then regains the
    # brass connected to it (thin posts, stubs, a long shroud) and the ink within 1.5 dots
    size = max(2, round_half_up(2 * dot_px))
    opened = ndimage.binary_opening(core, structure=np.ones((size, size), dtype=bool))
    min_area = int(4 * texel_px * texel_px)
    brass_core = core & in_group(base_idx, BRASS)
    ink_core = core & in_group(base_idx, (INK,))
    reach_px = 1.5 * dot_px
    reach = disk(max(1, round_half_up(reach_px)))
    groups, rejected = [], []
    for body in sorted(components(opened, EIGHT, min_area), key=lambda b: -int(b.sum())):
        brass_ink = float(in_group(base_idx[body], BRASS + (INK,)).mean())
        brass_share = float(in_group(base_idx[body], BRASS).mean())
        if brass_ink < 0.7 or brass_share < 0.25:
            rejected.append({"box": list(bbox(body)), "area": int(body.sum()), "brass_ink_share": r2(brass_ink, 3)})
            continue
        brass = ndimage.binary_propagation(body & brass_core, structure=EIGHT, mask=brass_core)
        for group in groups:  # bodies joined by cut brass are one part
            if (group["brass"] & brass).any():
                group["body"] |= body
                group["brass"] |= brass
                break
        else:
            groups.append({"body": body, "brass": brass})
    # the cut ink within 1.5 dots goes to the nearest part
    owner = np.full(core.shape, -1, dtype=np.int16)
    nearest = np.full(core.shape, np.inf)
    for i, group in enumerate(groups):
        distance = ndimage.distance_transform_edt(~(group["body"] | group["brass"]))
        closer = ink_core & (distance <= reach_px) & (distance < nearest)
        owner[closer] = i
        nearest = np.where(closer, distance, nearest)
    parts, taken = [], np.zeros_like(core)
    for i, group in enumerate(groups):
        mask = (group["body"] | group["brass"] | (owner == i)) & ~taken
        taken |= mask
        parts.append({"mask": mask})
    # brass fittings cut off by an ink or iron row (a sight foot, a key stub on a thin post)
    # join the part they reach within 6 dots through the gun's non-ivory pixels, the one
    # whose centre is nearest; the cut brass on that path (the post) and the ink around it
    # come along. Slivers (thinner than 1.5 dots) and brass the edit still has within the
    # local search (a drifted body fitting) stay on the bare body.
    steps = round_half_up(6 * dot_px)
    reach_of = [ndimage.binary_dilation(p["mask"], structure=EIGHT, iterations=steps, mask=wanted) for p in parts]
    edit_idx, _ = snap(edit_rgba)
    edit_brass = ndimage.binary_dilation(in_group(shifted(edit_idx, dx, dy, -1), BRASS),
                                         structure=np.ones((2 * refine + 1, 2 * refine + 1), dtype=bool))
    attached, unattached = [], []
    for piece in components(brass_core & ~taken, EIGHT, int(dot_px * dot_px)):
        if ndimage.distance_transform_edt(piece).max() < 0.75 * dot_px or (piece & taken).any():
            continue
        box = list(bbox(piece))
        if (piece & edit_brass).sum() >= 0.5 * piece.sum():
            continue
        near = [i for i, area in enumerate(reach_of) if (area & piece).any()]
        if not near:
            unattached.append({"box": box, "area": int(piece.sum())})
            continue
        target = min(near, key=lambda i: math.dist(centroid(piece), centroid(parts[i]["mask"])))
        path = ndimage.binary_dilation(piece, structure=EIGHT, iterations=steps, mask=wanted) & reach_of[target] & brass_core
        brass = piece | path
        add = (brass | (ink_core & ndimage.binary_dilation(brass, structure=reach))) & ~taken
        parts[target]["mask"] = parts[target]["mask"] | add
        taken |= add
        attached.append({"box": box, "area": int(piece.sum()), "to_part": target})
    for part in parts:
        mask = part["mask"]
        x0, y0, x1, y1 = bbox(mask)
        holes = enclosed_holes(mask, max(1, int(2 * dot_px * dot_px)))
        part.update({"box": [x0, y0, x1, y1], "area": int(mask.sum()),
                     "brass_ink_share": r2(in_group(base_idx[mask], BRASS + (INK,)).mean(), 3),
                     "hole_area": holes[0][0] if holes else 0, "aspect": r2((x1 - x0) / max(1, y1 - y0), 2)})
    if len(parts) != 4:
        notes.append(f"parts cut found {len(parts)} parts of >= 4 texels, expected 4")
    if unattached:
        notes.append(f"brass fittings left on the bare body: {unattached}")
    labelled = label_parts(parts, base_opaque, notes)
    return labelled, {"shift": [dx, dy], "iou": r2(iou, 4), "local": local, "opening_px": size,
                      "rejected_components": rejected, "attached_fittings": attached}, notes


def label_parts(parts, base_opaque, notes):
    """sight: enclosed hole; shroud: aspect >= 4; cylinder: the largest left; housing: rearmost."""
    remaining = list(parts)
    labelled = {}
    with_hole = [p for p in remaining if p["hole_area"] > 0]
    if with_hole:
        labelled["sight"] = max(with_hole, key=lambda p: (p["hole_area"], -p["box"][1]))
        remaining = [p for p in remaining if p is not labelled["sight"]]
    long = [p for p in remaining if p["aspect"] >= 4]
    if long:
        labelled["shroud"] = max(long, key=lambda p: (p["aspect"], p["area"]))
        remaining = [p for p in remaining if p is not labelled["shroud"]]
    if remaining:
        housing = min(remaining, key=lambda p: (p["box"][0], p["box"][1]))
        rest = [p for p in remaining if p is not housing]
        if rest:
            labelled["cylinder"] = max(rest, key=lambda p: (p["area"], -p["box"][0]))
            remaining = [p for p in rest if p is not labelled["cylinder"]]
            labelled["spring_housing"] = housing
        else:
            labelled["cylinder"] = housing
            remaining = []
    for part in remaining:
        notes.append(f"unlabelled part at {part['box']}")
    for name in PART_ORDER:
        if name not in labelled:
            notes.append(f"part '{name}' not found")
    ys, xs = np.nonzero(base_opaque)
    gx0, gx1 = xs.min(), xs.max() + 1
    for name, part in labelled.items():
        x0, y0, x1, y1 = part["box"]
        part["centre_x_fraction"] = r2(((x0 + x1) / 2 - gx0) / (gx1 - gx0), 3)
    if "cylinder" in labelled and "spring_housing" in labelled and labelled["spring_housing"]["box"][0] > labelled["cylinder"]["box"][0]:
        notes.append("spring housing is not the rearmost part")
    if "sight" in labelled:
        tops = [p["box"][1] for p in labelled.values()]
        if labelled["sight"]["box"][1] > min(tops):
            notes.append("sight is not the topmost part")
    return labelled


PART_ORDER = ("cylinder", "shroud", "sight", "spring_housing")

# how each anchor is measured ("source": on the delivery pixels, mapped to texels; "texel": on the output)
ANCHOR_RULES = {
    "Claw*.pivot": "source: leftmost column of the iron wrist cuff (iron in the leftmost 20 %), texel centre; y = cuff centroid",
    "Claw*.beads": "source: dark holes enclosed by the brass plate (the brass component with most of them), "
                   ">= 2 dots and >= 35 % of the largest; 2 rows x 3, row-major",
    "Claw*.grab": "source: centroid of the filled bead plate",
    "Claw*.tips": "source: up to 5 local maxima (prominence >= 3 dots, >= 55 % of the farthest) of the ivory "
                  "radius around the pivot, sorted by atan2(dy, dx) in image coordinates, so the topmost first. The "
                  "index is an angular rank, not a finger identity: a pose with fewer than 5 tips (warned) shifts the "
                  "later indices, so pair tips with design talons by distance, not by index",
    "MeridianGun.muzzle": "texel: right edge of the rightmost column; y = mean opaque row of the last 3 columns + 0.5",
    "MeridianGun.grip": "source: centroid of the iron below the body line (median bottom of the rear 40 %) in x 10-40 %",
    "MeridianGun.key_seat": "texel: top edge and centre of the housing's topmost brass row",
    "MeridianGun.seats": "texel: top-left of each part in the gun canvas, order cylinder, shroud, sight, spring_housing",
    "MeridianKey.shaft_bottom": "texel: centre of the lowest opaque row, bottom edge; every frame registered on it",
    "LacunaBook.hole": "source: largest enclosed see-through region, centroid; radius sqrt(area / pi)",
    "LacunaBook.clasp": "source: centroid of the largest brass cluster right of 70 % of the width",
    "LacunaIris.centre": "source: least-squares circle through the brass ring; frames registered on it",
    "LacunaIris.aperture_radii": "source: 0 closed; #1d1a22/ink core at the centre (frames 2-3); see-through hole (frame 4)",
    "LacunaGreatIris": "source: brass circle fit (centre, ring_radius), see-through hole, outer radius of the filled silhouette",
    "Chorister*.stand_tip": "texel: centre of the lowest opaque row, bottom edge; one cell for all voices",
    "Chorister*.mouths": "source: largest dark cluster inside the head band 22-45 % of the height, per frame",
    "ChoirOrgan.mouth": "source: largest see-through hole in the lower half; case_top_row = first row >= 80 % of the "
                        "widest row; pipe_top_row = first opaque row",
    "ChoirBaton": "source: pommel = largest porcelain cluster in the lower-left quarter; grip = iron centroid; "
                  "texel: tip = opaque texel furthest to the upper right",
    "WitnessBlade": "source: eye = see-through hole, guard = the brass cluster with the tallest extent; texel: tip and "
                    "pommel = right/left edge at the mean row, pivot = opaque centroid",
    "WitnessSword": "texel: stake_point = bottom edge centre, pommel = top edge centre, guard = row with the most brass",
    "WitnessShards": "texel: pivot = opaque centroid (registration), points = right edge at the mean row",
}


# ---------------------------------------------------------------- strips

def strip(frames, anchors, cell=None):
    """Equal cells registered on one anchor per frame (texel coords); integer offsets, residual reported.

    frames: texel arrays (-1 transparent); anchors: (x, y) per frame. cell, if given, is
    (width, height, anchor_x, anchor_y) shared with other strips of a family.
    """
    if cell is None:
        cell = strip_cell(frames, anchors)
    width, height, ax, ay = cell
    sheet = np.full((height, width * len(frames)), -1, dtype=np.int16)
    placed = []
    for i, (texels, (x, y)) in enumerate(zip(frames, anchors)):
        ox, oy = round_half_up(ax - x), round_half_up(ay - y)
        h, w = texels.shape
        if ox < 0 or oy < 0 or ox + w > width or oy + h > height:
            raise ValueError("strip cell too small for a frame")
        view = sheet[oy:oy + h, i * width + ox:i * width + ox + w]
        view[texels >= 0] = texels[texels >= 0]
        placed.append({"frame": i, "offset": [ox, oy], "residual": [r2(x + ox - ax, 3), r2(y + oy - ay, 3)]})
    return sheet, {"frame_size": [width, height], "frames": len(frames), "anchor": [r2(ax), r2(ay)], "placed": placed}


def strip_cell(frames, anchors):
    ax = max(x for x, _ in anchors)
    ay = max(y for _, y in anchors)
    width = max(round_half_up(ax - x) + t.shape[1] for t, (x, _) in zip(frames, anchors))
    height = max(round_half_up(ay - y) + t.shape[0] for t, (_, y) in zip(frames, anchors))
    return width, height, ax, ay


# ---------------------------------------------------------------- exporter

@dataclass
class Output:
    name: str
    texels: np.ndarray            # logical texels (-1 transparent)
    info: dict
    anchors: dict = field(default_factory=dict)
    stored_scale: int = 1         # icons and buff are stored at 2x
    runtime: bool = True
    image: Image.Image = None     # set when the colours do not come from texels (fallback)

    def stored(self):
        image = self.image if self.image is not None else render(self.texels)
        if self.stored_scale != 1:
            image = image.resize((image.width * self.stored_scale, image.height * self.stored_scale), Image.NEAREST)
        return image


def is_delivery(path):
    """A delivery folder holds manifest.json (Exporter.load requires it)."""
    return (Path(path) / "manifest.json").is_file()


def find_delivery():
    """The delivery folder, or None: $CONVERGENCE_DOLL_WEAPON_DELIVERY when set (it must be a delivery
    folder; there is no fallback then), else the nearest parent folder of the repository holding one."""
    env = os.environ.get(DELIVERY_ENV)
    if env:
        return Path(env) if is_delivery(env) else None
    for parent in ROOT.parents:
        candidate = parent / DELIVERY
        if is_delivery(candidate):
            return candidate
    return None


class Exporter:
    def __init__(self, source, reink=False):
        self.source = Path(source)
        self.reink = reink
        self.sheets, self.outputs, self.warnings = {}, [], []
        self.report = {
            "recipe": "tools/" + Path(__file__).name,
            "recipe_sha256": sha256(Path(__file__)),
            "helpers": {"tools/export_ebon_art.py": sha256(Path(__file__).with_name("export_ebon_art.py"))},
            "versions": {"python": platform.python_version(), "numpy": np.__version__, "scipy": scipy.__version__,
                         "pillow": PIL.__version__},
            "delivery": DELIVERY.as_posix().split("asset-deliveries/")[-1],
            "coordinates": "texture space, (0,0) top-left corner, texel centre +0.5; 'edge' anchors sit on a texel edge",
            "texel_rule": "1 texel = 1 dot = 2 world px, PointClamp; size only through integer export rungs k",
            "palette": [{"name": n, "hex": h} for n, h in PALETTE],
            "anchor_rules": ANCHOR_RULES,
            "socket_tone": None,
            "human_modifications": ["--reink: every outer boundary texel forced to ink"] if reink else [],
            "inputs": {}, "manifest": {}, "sheets": {}, "outputs": {}, "warnings": self.warnings,
        }

    # ---- inputs

    def load(self):
        manifest_path = self.source / "manifest.json"
        if not manifest_path.is_file():
            raise FileNotFoundError(f"{manifest_path}: not a delivery folder (manifest.json is required)")
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        recommendations = manifest.get("recommendations")
        if not recommendations:
            raise ValueError(f"{manifest_path}: no recommendations to check the pinned inputs against")
        self.report["delivery"] = manifest.get("batch", self.report["delivery"])
        for key, sheet in SHEETS.items():
            path = self.source / sheet.file
            digest = sha256(path)
            if digest != INPUTS[sheet.file]:
                raise ValueError(f"{sheet.file}: unexpected input {digest[:12]} (pinned {INPUTS[sheet.file][:12]})")
            rec = recommendations.get(key)
            if rec is None or rec.get("file") != sheet.file:
                self.warnings.append(f"{key}: pinned {sheet.file} is not the manifest recommendation")
            if rec is not None:
                self.report["manifest"][key] = {"candidate": rec.get("candidate"), "accepted_by_codex": rec.get("accepted_by_codex")}
            self.report["inputs"][sheet.file] = digest
            with Image.open(path) as image:
                rgba = np.asarray(clean_alpha(image.convert("RGBA"))).copy()
            objects, idx, dropped, unique_cells, crossing = prepare_objects(rgba, sheet.grid, sheet.count, key)
            pitch, used = measure_pitch([o.rgba for o in objects])
            self.sheets[key] = {"rgba": rgba, "idx": idx, "objects": objects, "own_dot_px": used}
            entry = {"file": sheet.file, "size": [rgba.shape[1], rgba.shape[0]], "grid": list(sheet.grid),
                     "objects": [{"cell": list(o.cell), "box": list(o.box)} for o in objects],
                     "dropped_specks": dropped, "pitch": pitch}
            if not unique_cells:
                self.warnings.append(f"{key}: two objects share one brief cell")
            if crossing:
                self.warnings.append(f"{key}: objects cross a brief cell border: {crossing}")
            if pitch["coherence"] < LOW_COHERENCE:
                self.warnings.append(f"{key}: pitch coherence {pitch['coherence']} < {LOW_COHERENCE}; check the 8x contact sheet "
                                     f"(edge-spacing mode {pitch['edge_spacing_mode_px']} px)")
            if pitch["at_search_ceiling"]:
                self.warnings.append(f"{key}: pitch {pitch['coarse_px']} px sits at the {PITCH_MAX:g} px search ceiling; the true dot may be larger")
            mode = pitch["edge_spacing_mode_px"]
            if min(abs(mode - used), abs(mode - 2 * used)) > 1.5 and pitch["coherence"] < 0.5:
                self.warnings.append(f"{key}: edge-spacing mode {mode} px disagrees with the pitch {r2(used)} px")
            self.report["sheets"][key] = entry
        for key, sheet in SHEETS.items():
            entry = self.report["sheets"][key]
            if sheet.family:
                base = self.sheets[sheet.family]["own_dot_px"]
                own = self.sheets[key]["own_dot_px"]
                self.sheets[key]["dot_px"] = base
                entry["pitch"]["family"] = sheet.family
                entry["pitch"]["own_dot_px"] = r2(own, 3)
                entry["pitch"]["dot_px"] = r2(base, 3)
                if abs(own / base - 1) > 0.05:
                    self.warnings.append(f"{key}: own pitch {r2(own)} px differs {r2((own / base - 1) * 100, 1)} % from the "
                                         f"{sheet.family} family pitch {r2(base)} px; resampled at the family pitch")
            else:
                self.sheets[key]["dot_px"] = self.sheets[key]["own_dot_px"]
        socket = []
        for key, data in self.sheets.items():
            for obj in data["objects"]:
                if obj.palette["mean_de"] > MEAN_DE_LIMIT:
                    self.warnings.append(f"{key} object {obj.index}: mean deltaE {obj.palette['mean_de']} > {MEAN_DE_LIMIT}; "
                                         f"median-cut fallback")
                socket += [dict(c, sheet=key, object=obj.index) for c in socket_check(obj.palette)]
        self.report["socket_tone"] = ({"added": False, "reason": "no dark off-palette cluster lighter than ink covers "
                                       f">= {SOCKET_SHARE:.0%} of an object"} if not socket else
                                      {"added": False, "candidates": socket})
        if socket:
            self.warnings.append(f"dark off-palette clusters may need a socket tone: {socket}")

    # ---- helpers

    def dot(self, key):
        return self.sheets[key]["dot_px"]

    def obj(self, key, index=0):
        return self.sheets[key]["objects"][index]

    def rungs(self, key, objs, brief):
        dot = self.dot(key)
        k, ratio = max((rule_k((o.size[0] / dot, o.size[1] / dot), brief) for o in objs), key=lambda kr: kr[1])
        rungs = [k]
        for extra in EXTRA_RUNGS.get(key, ()) + ((REVIEW_K[key],) if key in REVIEW_K else ()):
            if extra not in rungs:
                rungs.append(extra)
        review = REVIEW_K.get(key)
        if review is not None and review != k:
            self.warnings.append(f"{key}: rule k = {k} (major-axis ratio {r2(ratio)}) differs from the review's k = {review}; "
                                 f"both rungs exported")
        return rungs, {"rule_k": k, "major_axis_ratio": r2(ratio, 3), "review_k": review}

    @staticmethod
    def rung_name(name, k, primary):
        if k == primary:
            return name
        return name + ("_L" if k < primary else "_S")

    def reduce_object(self, key, obj, k):
        dot = self.dot(key)
        grid = object_grid(obj.size, dot, k)
        texels, stats = reduce_cells(obj.idx, grid, dot * dot, self.reink)
        texels, offset = trim(texels)
        stats["source_ink_boundary_share"] = r2(source_ink_share(obj.idx, dot), 3)
        stats["ink_boundary_retention"] = r2(stats["ink_boundary_share"] / max(1e-6, stats["source_ink_boundary_share"]), 3)
        image = None
        if obj.palette["mean_de"] > MEAN_DE_LIMIT:
            full = block_mode(Image.fromarray(obj.rgba, "RGBA"), grid.nx, grid.ny, colors=FALLBACK_COLORS)
            image = full.crop((offset[0], offset[1], offset[0] + texels.shape[1], offset[1] + texels.shape[0]))
            stats["fallback"] = f"median cut, {FALLBACK_COLORS} colours"
        info = {"sheet": key, "object": obj.index, "k": k, "dot_px": r2(dot, 3), "cell_px": [r2(grid.cx, 3), r2(grid.cy, 3)],
                "dots_at_pitch": [r2(obj.size[0] / dot, 1), r2(obj.size[1] / dot, 1)],
                "source_box": list(obj.box), "palette": obj.palette, "reduction": stats}
        return texels, grid, offset, info, image

    def add(self, name, texels, info, anchors=None, stored_scale=1, runtime=True, image=None):
        info = dict(info)
        h, w = texels.shape
        info["texels"] = [w, h]
        if stored_scale == 1:
            info["world_px"] = [2 * w, 2 * h]
        info["png_size"] = [w * stored_scale, h * stored_scale]
        self.outputs.append(Output(name, texels, info, anchors or {}, stored_scale, runtime, image))

    # ---- assets

    def export_simple(self, name, key, brief, anchors_fn=None, index=0):
        obj = self.obj(key, index)
        rungs, kinfo = self.rungs(key, [obj], brief)
        for k in rungs:
            texels, grid, offset, info, image = self.reduce_object(key, obj, k)
            anchors = anchors_fn(obj, texels, grid, offset, info) if anchors_fn else {}
            self.add(self.rung_name(name, k, rungs[0]), texels, dict(info, brief_dots=list(brief), **kinfo), anchors, image=image)

    def export_claws(self):
        for key, poses in (("DW01", (("ClawOpen", (56, 48)), ("ClawRake", (52, 48)))),
                           ("DW01B", (("ClawClench", (40, 40)), ("ClawThrust", (64, 28))))):
            for index, (name, brief) in enumerate(poses):
                obj = self.obj(key, index)
                found, notes = claw_anchors(obj.idx, self.dot(key))
                for note in notes:
                    self.warnings.append(f"{name}: {note}")

                def anchors(obj, texels, grid, offset, info, found=found):
                    out = {}
                    if "pivot" in found:
                        out["pivot"] = [grid.column(found["cuff"]["min_x"], offset),
                                        grid.texel((0, found["cuff"]["centroid_y"]), offset)[1]]
                        out["tips"] = [grid.texel(t, offset) for t in found["tips"]]
                    out["beads"] = [grid.texel(b, offset) for b in found["beads"]]
                    if "plate" in found:
                        out["grab"] = grid.texel(found["plate"]["centroid"], offset)
                    info["claw"] = {"beads_found": len(found["beads"]), "tips_found": len(found.get("tips", [])),
                                    "cuff_brass_band": found.get("cuff", {}).get("brass_band")}
                    return out

                self.export_simple(name, key, brief, anchors, index)

    def export_icon(self, name, key, limit, brief):
        obj = self.obj(key)
        dot = self.dot(key)
        w, h = obj.size
        dots = max(w, h) / dot
        k = max(1, math.ceil(dots / limit - 1e-9))
        grid = object_grid(obj.size, dot, k)
        while max(grid.nx, grid.ny) > limit:
            k += 1
            grid = object_grid(obj.size, dot, k)
        fill = max(grid.nx, grid.ny) / limit
        fit = {"rule": "integer", "k": k, "fill": r2(fill, 3)}
        if fill < ICON_FILL:
            scale = max(w, h) / limit
            nx, ny = min(limit, max(1, round_half_up(w / scale))), min(limit, max(1, round_half_up(h / scale)))
            grid = Grid(0.0, 0.0, w / nx, h / ny, nx, ny)
            fit = {"rule": "fractional majority fit", "dots_per_texel": r2(scale / dot, 3), "integer_fill": r2(fill, 3)}
            self.warnings.append(f"{name}: integer k = {k} fills {r2(fill * 100)} % of {limit}; fractional fit at "
                                 f"{fit['dots_per_texel']} dots per texel")
        texels, stats = reduce_cells(obj.idx, grid, dot * dot, self.reink)
        texels, offset = trim(texels)
        if limit == BUFF_LIMIT:
            canvas = np.full((limit, limit), -1, dtype=np.int16)
            h2, w2 = texels.shape
            y0, x0 = (limit - h2) // 2, (limit - w2) // 2
            canvas[y0:y0 + h2, x0:x0 + w2] = texels
            texels = canvas
        info = {"sheet": key, "object": 0, "dot_px": r2(dot, 3), "dots_at_pitch": [r2(w / dot, 1), r2(h / dot, 1)],
                "limit": limit, "fit": fit, "brief_dots": list(brief), "logical": [int(texels.shape[1]), int(texels.shape[0])],
                "stored": "2x", "source_box": list(obj.box), "palette": obj.palette, "reduction": stats,
                "holes": output_holes(texels)}
        self.add(name, texels, info, {}, stored_scale=2)

    def export_gun(self):
        base = self.sheets["DW02"]
        edit = self.sheets["DW02E"]
        dot = self.dot("DW02")
        obj = self.obj("DW02")
        rungs, kinfo = self.rungs("DW02", [obj], (88, 32))
        k = rungs[0]
        if self.rungs("DW02E", [self.obj("DW02E")], (88, 32))[0][0] != k:
            self.warnings.append("DW02E: the bare gun's own k differs; it uses the gun's k and grid")
        parts, registration, notes = cut_gun_parts(base["rgba"], base["idx"], edit["rgba"], dot, dot * k)
        for note in notes:
            self.warnings.append(f"MeridianParts: {note}")
        dx, dy = registration["shift"]
        edit_idx = shifted(edit["idx"], dx, dy, -1)
        bx0, by0, bx1, by1 = obj.box
        gx = object_grid(obj.size, dot, k)
        ex0, ey0, ex1, ey1 = bbox(edit_idx >= 0)
        i0 = min(0, math.floor((ex0 - bx0) / gx.cx))
        j0 = min(0, math.floor((ey0 - by0) / gx.cy))
        i1 = max(gx.nx, math.ceil((ex1 - bx0) / gx.cx))
        j1 = max(gx.ny, math.ceil((ey1 - by0) / gx.cy))
        grid = Grid(bx0 + i0 * gx.cx, by0 + j0 * gx.cy, gx.cx, gx.cy, i1 - i0, j1 - j0)
        gun_only = np.full_like(base["idx"], -1)
        gun_only[by0:by1, bx0:bx1] = obj.idx
        gun, gun_stats = reduce_cells(gun_only, grid, dot * dot, self.reink)
        bare, bare_stats = reduce_cells(edit_idx, grid, dot * dot, self.reink)
        union = (gun >= 0) | (bare >= 0)
        ux0, uy0, ux1, uy1 = bbox(union)
        offset = (ux0, uy0)
        gun, bare = gun[uy0:uy1, ux0:ux1], bare[uy0:uy1, ux0:ux1]
        part_texels, seats, part_info = [], {}, {}
        for name in PART_ORDER:
            if name not in parts:
                continue
            mask = parts[name]["mask"]
            texels, stats = reduce_cells(np.where(mask, base["idx"], -1).astype(np.int16), grid, dot * dot, self.reink)
            texels = texels[uy0:uy1, ux0:ux1]
            texels, (sx, sy) = trim(texels)
            part_texels.append((name, texels))
            seats[name] = [sx, sy]
            part_info[name] = {"seat": [sx, sy], "texels": [int(texels.shape[1]), int(texels.shape[0])],
                               "source_box": parts[name]["box"], "brass_ink_share": parts[name]["brass_ink_share"],
                               "aspect": parts[name]["aspect"], "centre_x_fraction": parts[name]["centre_x_fraction"],
                               "hole_area_px": parts[name]["hole_area"], "reduction": stats}
            if parts[name]["brass_ink_share"] < 0.7:
                self.warnings.append(f"MeridianParts {name}: only {parts[name]['brass_ink_share']} brass/ink")
        # anchors on the shared canvas
        to = lambda p: grid.texel(p, offset)  # noqa: E731  (sheet px -> canvas texels)
        anchors = {"muzzle": muzzle(gun)}
        grip = grip_of(obj.idx, dot)
        if grip is not None:
            anchors["grip"] = to((grip[0] + bx0, grip[1] + by0))
        else:
            self.warnings.append("MeridianGun: no iron grip found below the body line")
        if "spring_housing" in parts:
            for name, texels in part_texels:
                if name == "spring_housing":
                    anchors["key_seat"] = key_seat(texels, seats[name])
        if "key_seat" not in anchors:
            self.warnings.append("MeridianGun: key seat not found (no spring housing part)")
        anchors["seats"] = [seats[n] for n in PART_ORDER if n in seats]
        common = dict(kinfo, brief_dots=[88, 32], k=k, dot_px=r2(dot, 3), cell_px=[r2(grid.cx, 3), r2(grid.cy, 3)],
                      dots_at_pitch=[r2(obj.size[0] / dot, 1), r2(obj.size[1] / dot, 1)],
                      canvas="MeridianGun, MeridianBare and the part seats share one canvas",
                      registration=registration, part_order=list(PART_ORDER))
        self.add("MeridianGun", gun, dict(common, sheet="DW02", source_box=list(obj.box), palette=obj.palette,
                                           reduction=gun_stats), anchors)
        self.add("MeridianBare", bare, dict(common, sheet="DW02E", palette=self.obj("DW02E").palette,
                                             reduction=bare_stats), anchors)
        # parts strip: equal cells, each part at its cell's top-left; seats are in gun texels
        if part_texels:
            cw = max(t.shape[1] for _, t in part_texels)
            ch = max(t.shape[0] for _, t in part_texels)
            sheet = np.full((ch, cw * len(part_texels)), -1, dtype=np.int16)
            for i, (_, texels) in enumerate(part_texels):
                view = sheet[:texels.shape[0], i * cw:i * cw + texels.shape[1]]
                view[texels >= 0] = texels[texels >= 0]
            self.add("MeridianParts", sheet, dict(common, sheet="DW02", frame_size=[cw, ch],
                                                  frames=len(part_texels), parts=part_info),
                     {"frame_width": cw, "frame_height": ch, "seats": anchors["seats"],
                      "parts": [name for name, _ in part_texels]})
            check = compose(bare, part_texels, seats)
            agree = reconstruction(gun, check)
            self.report["gun_reconstruction"] = agree
            self.add("MeridianAssembledCheck", check, {"sheet": "DW02E+DW02", "preview_only": True, **agree}, runtime=False)
            if agree["opaque_iou"] < 0.9:
                self.warnings.append(f"MeridianBare + parts reassemble with opaque IoU {agree['opaque_iou']} against MeridianGun")

    def export_key(self):
        key = "DW02K"
        objs = self.sheets[key]["objects"]
        rungs, kinfo = self.rungs(key, objs, (22, 26))
        for k in rungs:
            frames, anchors, infos = [], [], []
            for obj in objs:
                texels, grid, offset, info, _ = self.reduce_object(key, obj, k)
                frames.append(texels)
                anchors.append(texel_extreme(texels, "bottom"))
                infos.append(info)
            sheet, meta = strip(frames, anchors)
            self.add(self.rung_name("MeridianKey", k, rungs[0]), sheet,
                     dict(kinfo, sheet=key, k=k, dot_px=r2(self.dot(key), 3), brief_dots=[22, 26], strip=meta,
                          registration="shaft bottom: centre of the lowest opaque row (bottom edge)", frames_info=infos),
                     {"frame_width": meta["frame_size"][0], "frame_height": meta["frame_size"][1], "shaft_bottom": meta["anchor"]})

    def export_iris(self):
        key = "DW03A"
        objs = self.sheets[key]["objects"]
        dot = self.dot(key)
        rungs, kinfo = self.rungs(key, objs, (30, 30))
        measured = []
        for i, obj in enumerate(objs):
            centre, radius = fit_circle(in_group(obj.idx, BRASS))
            aperture, how = 0.0, "closed"
            if i in (1, 2):
                core = in_group(obj.idx, (1, INK))
                parts = components(core, EIGHT, 1)
                cx, cy = int(centre[0]), int(centre[1])
                hit = [p for p in parts if p[min(cy, p.shape[0] - 1), min(cx, p.shape[1] - 1)]]
                if not hit and parts:
                    hit = [min(parts, key=lambda p: math.dist(centroid(p), centre))]
                if hit:
                    aperture, how = math.sqrt(hit[0].sum() / math.pi), "dark #1d1a22 core"
            elif i == 3:
                holes = enclosed_holes(obj.opaque, int(dot * dot))
                if holes:
                    aperture, how = math.sqrt(holes[0][0] / math.pi), "see-through hole"
                else:
                    self.warnings.append("LacunaIris frame 4: no see-through hole")
            measured.append((centre, radius, aperture, how))
        for k in rungs:
            frames, anchors, details = [], [], []
            for obj, (centre, radius, aperture, how) in zip(objs, measured):
                texels, grid, offset, info, _ = self.reduce_object(key, obj, k)
                frames.append(texels)
                anchors.append(grid.texel(centre, offset))
                details.append({"ring_centre": grid.texel(centre, offset), "ring_radius": r2(radius / grid.cx),
                                "aperture_radius": r2(aperture / grid.cx), "aperture_from": how,
                                "holes_in_output": output_holes(texels), **info})
            sheet, meta = strip(frames, anchors)
            self.add(self.rung_name("LacunaIris", k, rungs[0]), sheet,
                     dict(kinfo, sheet=key, k=k, dot_px=r2(dot, 3), brief_dots=[30, 30], strip=meta,
                          registration="ring centre (least-squares circle through the brass)", frames_info=details),
                     {"frame_width": meta["frame_size"][0], "frame_height": meta["frame_size"][1], "centre": meta["anchor"],
                      "ring_radius": r2(float(np.mean([d["ring_radius"] for d in details]))),
                      "aperture_radii": [d["aperture_radius"] for d in details]})

    def export_choristers(self):
        dot = self.dot("DW04")
        rungs, kinfo = self.rungs("DW04", self.sheets["DW04"]["objects"], (22, 38))
        for k in rungs:
            sets = []
            for key in ("DW04", "DW04V2", "DW04V3"):
                frames, anchors, mouths, details = [], [], [], []
                for obj in self.sheets[key]["objects"]:
                    texels, grid, offset, info, _ = self.reduce_object(key, obj, k)
                    mouth, extra, eyes = mouth_of(obj.idx, obj.opaque, dot)
                    if mouth is None:
                        self.warnings.append(f"{key} frame {obj.index}: no mouth cluster found")
                    if eyes:
                        self.warnings.append(f"{key} frame {obj.index}: {eyes} eye-like dark cluster(s) in the head")
                    point = grid.texel(mouth, offset) if mouth else None
                    visible = point is not None and dark_near(texels, point)
                    if point is not None and not visible:
                        self.warnings.append(f"{self.rung_name(f'Chorister{len(sets)}', k, rungs[0])} frame {obj.index}: "
                                             f"the mouth is thinner than a texel at k = {k} and does not show")
                    frames.append(texels)
                    anchors.append(texel_extreme(texels, "bottom"))
                    mouths.append(point)
                    details.append(dict(info, other_head_dark_clusters=extra, eye_like_clusters=eyes, mouth_visible=visible))
                sets.append((key, frames, anchors, mouths, details))
            all_frames = [f for s in sets for f in s[1]]
            all_anchors = [a for s in sets for a in s[2]]
            cell = strip_cell(all_frames, all_anchors)
            for voice, (key, frames, anchors, mouths, details) in enumerate(sets):
                sheet, meta = strip(frames, anchors, cell)
                mouth_cell = [None if m is None else [r2(m[0] + p["offset"][0]), r2(m[1] + p["offset"][1])]
                              for m, p in zip(mouths, meta["placed"])]
                self.add(self.rung_name(f"Chorister{voice}", k, rungs[0]), sheet,
                         dict(kinfo, sheet=key, k=k, dot_px=r2(dot, 3), brief_dots=[22, 38], strip=meta,
                              registration="stand spike tip: centre of the lowest opaque row (bottom edge); one cell for all voices",
                              frames_info=details),
                         {"frame_width": meta["frame_size"][0], "frame_height": meta["frame_size"][1],
                          "stand_tip": meta["anchor"], "mouths": [m for m in mouth_cell if m is not None]})

    def export_shards(self):
        key = "DW05S"
        objs = self.sheets[key]["objects"]
        rungs, kinfo = self.rungs(key, objs, (16, 8))
        for k in rungs:
            frames, anchors, points, infos = [], [], [], []
            for obj in objs:
                texels, grid, offset, info, _ = self.reduce_object(key, obj, k)
                frames.append(texels)
                anchors.append(texel_centroid(texels))
                points.append(texel_extreme(texels, "right"))
                infos.append(info)
            sheet, meta = strip(frames, anchors)
            point_cell = [[r2(p[0] + m["offset"][0]), r2(p[1] + m["offset"][1])] for p, m in zip(points, meta["placed"])]
            self.add(self.rung_name("WitnessShards", k, rungs[0]), sheet,
                     dict(kinfo, sheet=key, k=k, dot_px=r2(self.dot(key), 3), brief_dots=[16, 8], strip=meta,
                          registration="opaque centroid (pivot)", frames_info=infos),
                     {"frame_width": meta["frame_size"][0], "frame_height": meta["frame_size"][1], "pivot": meta["anchor"],
                      "points": point_cell})

    def run(self):
        self.load()
        self.export_claws()
        self.export_icon("NullRefrainIcon", "DW01I", ICON_LIMIT, (30, 30))
        self.export_gun()
        self.export_key()
        self.export_icon("PaleMeridianIcon", "DW02I", ICON_LIMIT, (30, 30))
        self.export_simple("LacunaBook", "DW03", (30, 38), book_anchors(self))
        self.export_icon("LacunaTestamentIcon", "DW03I", ICON_LIMIT, (24, 28))
        self.export_iris()
        self.export_simple("LacunaGreatIris", "DW03B", (60, 60), great_iris_anchors(self))
        self.export_choristers()
        self.export_simple("ChoirOrgan", "DW04P", (88, 72), organ_anchors(self))
        self.export_simple("ChoirBaton", "DW04S", (32, 32), baton_anchors)
        self.export_icon("ChoirOfTheUnmadeIcon", "DW04S", ICON_LIMIT, (32, 32))
        self.export_icon("ChoirOfTheUnmadeBuff", "DW04B", BUFF_LIMIT, (16, 16))
        self.export_simple("WitnessBlade", "DW05", (72, 18), blade_anchors(self))
        self.export_simple("WitnessSword", "DW05V", (12, 64), sword_anchors)
        self.export_shards()
        self.export_icon("LastWitnessIcon", "DW05I", ICON_LIMIT, (30, 30))
        for output in self.outputs:
            stats = output.info.get("reduction", {})
            share = stats.get("ink_boundary_share")
            if share is not None and share < 0.9 and not output.name.endswith("Check"):
                self.warnings.append(f"{output.name}: ink on {r2(share * 100, 1)} % of the outer boundary (target 90 %)")
        return self


# ---------------------------------------------------------------- per-asset anchors

def book_anchors(exporter):
    def anchors(obj, texels, grid, offset, info):
        out = {}
        holes = enclosed_holes(obj.opaque, int(exporter.dot("DW03") ** 2))
        if holes:
            area, centre, _, _ = holes[0]
            out["hole"] = grid.texel(centre, offset)
            out["hole_radius"] = r2(math.sqrt(area / math.pi) / grid.cx)
        else:
            exporter.warnings.append("LacunaBook: no see-through hole")
        w = obj.idx.shape[1]
        clasps = [p for p in components(in_group(obj.idx, BRASS), EIGHT, int(exporter.dot("DW03") ** 2))
                  if centroid(p)[0] > 0.7 * w]
        if clasps:
            clasp = max(clasps, key=lambda p: int(p.sum()))
            out["clasp"] = grid.texel(centroid(clasp), offset)
        else:
            exporter.warnings.append("LacunaBook: no right-edge brass clasp")
        info["holes_in_output"] = output_holes(texels)
        return out
    return anchors


def great_iris_anchors(exporter):
    def anchors(obj, texels, grid, offset, info):
        centre, radius = fit_circle(in_group(obj.idx, BRASS))
        out = {"centre": grid.texel(centre, offset), "ring_radius": r2(radius / grid.cx)}
        holes = enclosed_holes(obj.opaque, int(exporter.dot("DW03B") ** 2))
        if holes:
            area, hole_centre, _, _ = holes[0]
            out["hole"] = grid.texel(hole_centre, offset)
            out["hole_radius"] = r2(math.sqrt(area / math.pi) / grid.cx)
        else:
            exporter.warnings.append("LacunaGreatIris: no see-through hole")
        filled = ndimage.binary_fill_holes(obj.opaque)
        out["outer_radius"] = r2(math.sqrt(filled.sum() / math.pi) / grid.cx)
        info["holes_in_output"] = output_holes(texels)
        return out
    return anchors


def organ_anchors(exporter):
    def anchors(obj, texels, grid, offset, info):
        out = {}
        h = obj.idx.shape[0]
        holes = [hole for hole in enclosed_holes(obj.opaque, int(exporter.dot("DW04P") ** 2)) if hole[1][1] > 0.5 * h]
        if holes:
            area, centre, _, _ = holes[0]
            out["mouth"] = grid.texel(centre, offset)
            out["mouth_radius"] = r2(math.sqrt(area / math.pi) / grid.cx)
        else:
            exporter.warnings.append("ChoirOrgan: no see-through mouth in the lower half")
        widths = (texels >= 0).sum(axis=1)
        out["case_top_row"] = int(np.nonzero(widths >= 0.8 * widths.max())[0][0])
        out["pipe_top_row"] = int(np.nonzero(widths > 0)[0][0])
        info["holes_in_output"] = output_holes(texels)
        return out
    return anchors


def baton_anchors(obj, texels, grid, offset, info):
    h, w = obj.idx.shape
    out = {}
    porcelain = [p for p in components(in_group(obj.idx, PORCELAIN), EIGHT, 4)
                 if centroid(p)[0] < 0.5 * w and centroid(p)[1] > 0.5 * h]
    if porcelain:
        out["pommel"] = grid.texel(centroid(max(porcelain, key=lambda p: int(p.sum()))), offset)
    iron = in_group(obj.idx, IRON)
    if iron.any():
        out["grip"] = grid.texel(centroid(iron), offset)
    ys, xs = np.nonzero(texels >= 0)
    best = int(np.argmax(xs - ys))
    out["tip"] = [r2(xs[best] + 0.5), r2(ys[best] + 0.5)]
    return out


def blade_anchors(exporter):
    def anchors(obj, texels, grid, offset, info):
        out = {}
        holes = enclosed_holes(obj.opaque, int(exporter.dot("DW05") ** 2))
        if holes:
            area, centre, _, _ = holes[0]
            out["eye"] = grid.texel(centre, offset)
            out["eye_radius"] = r2(math.sqrt(area / math.pi) / grid.cx)
        else:
            exporter.warnings.append("WitnessBlade: no see-through eye hole")
        out["tip"] = texel_extreme(texels, "right")
        out["pommel"] = texel_extreme(texels, "left")
        out["pivot"] = texel_centroid(texels)
        brass = components(in_group(obj.idx, BRASS), EIGHT, int(exporter.dot("DW05") ** 2))
        if brass:
            guard = max(brass, key=lambda p: (bbox(p)[3] - bbox(p)[1], int(p.sum())))
            x0, _, x1, _ = bbox(guard)
            out["guard_x0"] = grid.texel((x0, 0), offset)[0]
            out["guard_x1"] = grid.texel((x1, 0), offset)[0]
            out["guard"] = grid.texel(centroid(guard), offset)
        info["holes_in_output"] = output_holes(texels)
        return out
    return anchors


def sword_anchors(obj, texels, grid, offset, info):
    """Stake point (bottom edge), pommel (top edge), guard = the row with the most brass (span centre)."""
    out = {"stake_point": texel_extreme(texels, "bottom"), "pommel": texel_extreme(texels, "top")}
    brass_rows = in_group(texels, BRASS).sum(axis=1)
    if brass_rows.any():
        row = int(brass_rows.argmax())
        xs = np.nonzero(texels[row] >= 0)[0]
        out["guard"] = [r2((xs.min() + xs.max() + 1) / 2), r2(row + 0.5)]
        out["guard_row"] = row
    return out


def muzzle(gun):
    """Right edge of the rightmost opaque column; y = mean opaque row of the last 3 columns + 0.5."""
    ys, xs = np.nonzero(gun >= 0)
    right = xs.max()
    near = xs >= right - 2
    return [float(right + 1), r2(ys[near].mean() + 0.5)]


def grip_of(idx, dot_px):
    """Centroid of the iron below the body line (median bottom of the rear 40 %) in x 10-40 % (crop px)."""
    h, w = idx.shape
    opaque = idx >= 0
    columns = np.arange(int(0.4 * w))
    bottoms = [np.nonzero(opaque[:, c])[0].max() for c in columns if opaque[:, c].any()]
    if not bottoms:
        return None
    line = float(np.median(bottoms))
    yy, xx = np.arange(h)[:, None], np.arange(w)[None, :]
    grip = in_group(idx, IRON + (INK,)) & (yy > line + dot_px) & (xx >= 0.10 * w) & (xx < 0.40 * w)
    iron = grip & in_group(idx, IRON)
    if not iron.any():
        return None
    return centroid(iron)


def key_seat(housing, seat):
    """Top centre of the housing's topmost brass texels (top edge), in gun canvas texels."""
    brass = in_group(housing, BRASS)
    ys, xs = np.nonzero(brass if brass.any() else housing >= 0)
    top = ys.min()
    row = xs[ys == top]
    return [r2((row.min() + row.max() + 1) / 2 + seat[0]), float(top + seat[1])]


def compose(bare, parts, seats):
    out = bare.copy()
    for name, texels in parts:
        sx, sy = seats[name]
        h, w = texels.shape
        view = out[sy:sy + h, sx:sx + w]
        view[texels >= 0] = texels[texels >= 0]
    return out


def reconstruction(gun, check):
    a, b = gun >= 0, check >= 0
    both = a & b
    return {"opaque_iou": r2((a & b).sum() / max(1, (a | b).sum()), 3),
            "same_tone_share": r2((gun[both] == check[both]).mean() if both.any() else 0.0, 3)}


# ---------------------------------------------------------------- contact sheet

def silhouette():
    """A plain 20x42 px player stand-in (head, body, legs)."""
    image = Image.new("RGBA", (20, 42), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    fill, line = (138, 147, 166, 255), (42, 46, 56, 255)
    draw.ellipse((4, 0, 15, 11), fill=fill, outline=line)
    draw.rectangle((3, 11, 16, 31), fill=fill, outline=line)
    draw.rectangle((4, 31, 9, 41), fill=fill, outline=line)
    draw.rectangle((10, 31, 15, 41), fill=fill, outline=line)
    return image


def contact_sheet(outputs, path):
    try:
        font = ImageFont.load_default(size=18)
    except TypeError:
        font = ImageFont.load_default()
    figure = silhouette()
    tiles = []
    for output in outputs:
        image = output.stored()
        game_scale = 2 if output.stored_scale == 1 else 1
        big = image.resize((image.width * 8, image.height * 8), Image.NEAREST)
        game = image.resize((image.width * game_scale, image.height * game_scale), Image.NEAREST)
        pad, gap = 12, 16
        inner_h = max(image.height, big.height, figure.height, game.height)
        panel_w = pad + image.width + gap + big.width + gap + figure.width + 6 + game.width + pad
        panels = []
        for colour in (DARK_BG, BRIGHT_BG):
            panel = Image.new("RGBA", (panel_w, inner_h + 2 * pad), colour + (255,))
            x = pad
            panel.alpha_composite(image, (x, pad))
            x += image.width + gap
            panel.alpha_composite(big, (x, pad))
            x += big.width + gap
            base = pad + inner_h
            panel.alpha_composite(figure, (x, base - figure.height))
            panel.alpha_composite(game, (x + figure.width + 6, base - game.height))
            panels.append(panel)
        label = (f"{output.name}  {image.width}x{image.height} px"
                 + (f"  k={output.info['k']}" if "k" in output.info else "")
                 + (f"  world {output.info['world_px'][0]}x{output.info['world_px'][1]}" if "world_px" in output.info else "")
                 + ("  (preview only)" if not output.runtime else ""))
        width = max(panel_w * 2 + 8, int(font.getlength(label)) + 16)
        tile = Image.new("RGBA", (width, panels[0].height + 34), (58, 58, 64, 255))
        ImageDraw.Draw(tile).text((8, 6), label, fill=(240, 236, 226, 255), font=font)
        tile.alpha_composite(panels[0], (0, 34))
        tile.alpha_composite(panels[1], (panel_w + 8, 34))
        tiles.append(tile)
    width = max(4600, max(t.width for t in tiles))
    rows, row, x = [], [], 0
    for tile in tiles:
        if row and x + tile.width > width:
            rows.append(row)
            row, x = [], 0
        row.append(tile)
        x += tile.width + 10
    rows.append(row)
    height = sum(max(t.height for t in r) + 10 for r in rows)
    sheet = Image.new("RGB", (width, height), (36, 36, 40))
    y = 0
    for r in rows:
        x = 0
        for tile in r:
            sheet.paste(tile.convert("RGB"), (x, y))
            x += tile.width + 10
        y += max(t.height for t in r) + 10
    path.write_bytes(png_bytes(sheet))


MARKERS = ((255, 64, 64), (64, 220, 96), (64, 160, 255), (255, 210, 40), (255, 96, 255), (40, 230, 230), (255, 150, 60))
RADIUS_OF = {"hole": "hole_radius", "centre": "ring_radius", "mouth": "mouth_radius", "eye": "eye_radius"}
PER_FRAME = ("mouths", "points", "aperture_radii")


def anchor_sheet(outputs, path, scale=8):
    """Review aid: each sprite with anchors at 8x with its anchors marked (frames repeat strip anchors)."""
    try:
        font = ImageFont.load_default(size=16)
    except TypeError:
        font = ImageFont.load_default()
    tiles = []
    for output in outputs:
        anchors = {k: v for k, v in output.anchors.items() if k not in ("seats",) or output.name.startswith("MeridianGun")}
        points = {k: v for k, v in anchors.items()
                  if isinstance(v, list) and v and all(isinstance(p, (int, float, list)) for p in v)}
        if not points or output.stored_scale != 1:
            continue
        image = render(output.texels)
        big = image.resize((image.width * scale, image.height * scale), Image.NEAREST)
        pad, legend = 24, 22
        tile = Image.new("RGBA", (big.width + 2 * pad, big.height + 2 * pad + legend * (len(points) + 1)), (88, 92, 104, 255))
        tile.alpha_composite(big, (pad, pad + legend))
        draw = ImageDraw.Draw(tile)
        draw.text((6, 4), output.name, fill=(250, 246, 236, 255), font=font)
        frame_w = anchors.get("frame_width")
        frames = image.width // frame_w if isinstance(frame_w, int) and frame_w else 1
        origin = (pad, pad + legend)

        def mark(x, y, colour, radius=None):
            px, py = origin[0] + x * scale, origin[1] + y * scale
            draw.line((px - 6, py, px + 6, py), fill=colour, width=2)
            draw.line((px, py - 6, px, py + 6), fill=colour, width=2)
            if radius:
                r = radius * scale
                draw.ellipse((px - r, py - r, px + r, py + r), outline=colour, width=2)

        for n, (key, value) in enumerate(points.items()):
            colour = MARKERS[n % len(MARKERS)] + (255,)
            draw.text((6, pad + legend + big.height + 6 + n * legend), key, fill=colour, font=font)
            if all(isinstance(v, (int, float)) for v in value):
                if len(value) != 2:
                    continue
                radius = anchors.get(RADIUS_OF.get(key, ""), None)
                repeat = frames if frame_w else 1
                for i in range(repeat):
                    mark(value[0] + i * (frame_w or 0), value[1], colour, radius if isinstance(radius, float) else None)
            else:
                for i, point in enumerate(value):
                    shift = i * frame_w if (frame_w and key in PER_FRAME) else 0
                    mark(point[0] + shift, point[1], colour)
        if "aperture_radii" in anchors and "centre" in anchors:
            for i, radius in enumerate(anchors["aperture_radii"]):
                cx, cy = anchors["centre"]
                mark(cx + i * frame_w, cy, MARKERS[-1] + (255,), radius or None)
        tiles.append(tile)
    width = max(2400, max(t.width for t in tiles))
    rows, row, x = [], [], 0
    for tile in tiles:
        if row and x + tile.width > width:
            rows.append(row)
            row, x = [], 0
        row.append(tile)
        x += tile.width + 10
    rows.append(row)
    sheet = Image.new("RGB", (width, sum(max(t.height for t in r) + 10 for r in rows)), (36, 36, 40))
    y = 0
    for r in rows:
        x = 0
        for tile in r:
            sheet.paste(tile.convert("RGB"), (x, y))
            x += tile.width + 10
        y += max(t.height for t in r) + 10
    path.write_bytes(png_bytes(sheet))


# ---------------------------------------------------------------- generated anchors

def pascal(name):
    return "".join(part[:1].upper() + part[1:] for part in name.split("_"))


def cs_float(value):
    text = f"{float(value):.3f}".rstrip("0").rstrip(".")
    return (text if text not in ("-0", "") else "0") + "f"


def cs_vector(point):
    return f"new({cs_float(point[0])}, {cs_float(point[1])})"


ANCHOR_NOTES = {"tips": "Ordered by angle around Pivot (topmost first); an index is not a finger, and a pose can show "
                        "fewer than 5 tips."}


def anchors_cs(outputs, recipe_sha):
    lines = ["// <auto-generated>", f"// {GENERATED_HEADER}", f"// recipe sha256 {recipe_sha}", "// </auto-generated>",
             "using Vector2 = System.Numerics.Vector2;", "",
             "namespace Convergence.Client.Encounters.FirstSeverance.Weapons;", "",
             "/// <summary>Texel-space anchors of the exported Doll weapon art: (0, 0) is the top-left corner,",
             "/// a texel centre is +0.5, one texel is one dot = 2 world px.</summary>",
             "internal static class DollArtAnchors", "{",
             f"    internal const string TextureRoot = \"{TEXTURE_ASSET_ROOT}\";"]
    for output in outputs:
        if not output.runtime:
            continue
        info = output.info
        lines += ["", f"    internal static class {output.name}", "    {",
                  f"        internal const string Texture = TextureRoot + \"{output.name}\";",
                  f"        internal const int Width = {info['png_size'][0]};",
                  f"        internal const int Height = {info['png_size'][1]};"]
        if "k" in info:
            lines.append(f"        internal const int K = {info['k']};")
        if output.stored_scale != 1:
            lines.append(f"        internal const int StoredScale = {output.stored_scale};")
        for key, value in output.anchors.items():
            name = pascal(key)
            if isinstance(value, bool):
                continue
            if key in ANCHOR_NOTES:
                lines.append(f"        // {ANCHOR_NOTES[key]}")
            if isinstance(value, int):
                lines.append(f"        internal const int {name} = {value};")
            elif isinstance(value, float):
                lines.append(f"        internal const float {name} = {cs_float(value)};")
            elif isinstance(value, list) and len(value) == 2 and all(isinstance(v, (int, float)) for v in value):
                lines.append(f"        internal static readonly Vector2 {name} = {cs_vector(value)};")
            elif isinstance(value, list) and value and all(isinstance(v, str) for v in value):
                items = ", ".join(f"\"{v}\"" for v in value)
                lines.append(f"        internal static readonly string[] {name} = {{ {items} }};")
            elif isinstance(value, list) and all(isinstance(v, list) for v in value):
                items = ", ".join(cs_vector(v) for v in value)
                lines.append(f"        internal static readonly Vector2[] {name} = {{ {items} }};")
            elif isinstance(value, list):
                items = ", ".join(cs_float(v) for v in value)
                lines.append(f"        internal static readonly float[] {name} = {{ {items} }};")
        lines.append("    }")
    lines.append("}")
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------- main

def select_outputs(outputs, only=None):
    """The runtime outputs that --only names (all of them when None). A bare name also takes its
    _L/_S rungs (ClawOpen takes ClawOpen_L). A name that matches nothing is an error."""
    runtime = [o for o in outputs if o.runtime]
    if only is None:
        return runtime
    chosen, unknown = [], []
    for name in only:
        hits = [o for o in runtime if o.name in (name, name + "_L", name + "_S")]
        if not hits:
            unknown.append(name)
        chosen += [o for o in hits if o not in chosen]
    if unknown:
        raise ValueError(f"--only names no runtime output: {', '.join(unknown)}; outputs: "
                         + ", ".join(o.name for o in runtime))
    return [o for o in runtime if o in chosen]


def write_outputs(exporter, preview, write_repo=False, root=ROOT, only=None):
    """Write the preview folder; with write_repo also the runtime PNGs and the generated anchors under `root`.

    Nothing outside `preview` is touched unless write_repo is set. `only` (names, see select_outputs)
    limits the repository PNGs; the generated anchors then cover those plus every runtime output whose
    PNG is already in the repository, so per-weapon art PRs build on each other.
    """
    if only is not None and not write_repo:
        raise ValueError("only needs write_repo")
    chosen = {o.name for o in select_outputs(exporter.outputs, only)}  # fails before anything is written
    texture_dir, anchors_path = root / TEXTURE_REL, root / ANCHORS_REL
    preview.mkdir(parents=True, exist_ok=True)
    report = exporter.report
    for output in exporter.outputs:
        data = png_bytes(output.stored())
        (preview / f"{output.name}.png").write_bytes(data)
        entry = dict(output.info)
        entry["anchors"] = output.anchors
        entry["runtime"] = output.runtime
        entry["path"] = (TEXTURE_REL / f"{output.name}.png").as_posix() if output.runtime else None
        entry["sha256"] = hashlib.sha256(data).hexdigest()
        report["outputs"][output.name] = entry
        if write_repo and output.name in chosen:
            texture_dir.mkdir(parents=True, exist_ok=True)
            (texture_dir / f"{output.name}.png").write_bytes(data)
    (preview / "DollArtAnchors.g.cs.txt").write_bytes(anchors_cs(exporter.outputs, report["recipe_sha256"]).encode("utf-8"))
    if write_repo:
        landed = [o for o in exporter.outputs
                  if o.runtime and (o.name in chosen or (texture_dir / f"{o.name}.png").is_file())]
        anchors_path.parent.mkdir(parents=True, exist_ok=True)
        anchors_path.write_bytes(anchors_cs(landed, report["recipe_sha256"]).encode("utf-8"))
    contact_sheet(exporter.outputs, preview / "contact.png")
    anchor_sheet(exporter.outputs, preview / "anchors.png")
    text = json.dumps(report, indent=2, ensure_ascii=False) + "\n"
    (preview / "report.json").write_bytes(text.encode("utf-8"))
    return report


def summary(report):
    lines = []
    for key, sheet in report["sheets"].items():
        p = sheet["pitch"]
        lines.append(f"{key}: pitch {p['dot_px']} px (coarse {p['coarse_px']}, coherence {p['coherence']}, "
                     f"line {p['refined_px']}{', family ' + p['family'] if 'family' in p else ''})")
    for name, out in report["outputs"].items():
        size = out["png_size"]
        lines.append(f"{name}: {size[0]}x{size[1]} px" + (f" k={out['k']}" if "k" in out else "")
                     + (f" world {out['world_px'][0]}x{out['world_px'][1]}" if "world_px" in out else ""))
    return "\n".join(lines + ["", "warnings:"] + report["warnings"])


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", type=Path, default=None,
                        help=f"asset-deliveries/doll-weapons/2026-10-02 (never committed); default: ${DELIVERY_ENV} "
                             "or the nearest parent folder holding it")
    parser.add_argument("--preview", type=Path, default=PREVIEW_DIR, help="local output directory (default .local/doll-weapon-art)")
    parser.add_argument("--write-repo", action="store_true",
                        help="also write runtime PNGs to Assets/Textures/Items/DollWeapons and the generated anchors "
                             "(art PRs only, after the owner's review)")
    parser.add_argument("--only", nargs="+", metavar="NAME",
                        help="with --write-repo: write only these outputs (a bare name also takes its _L/_S rungs; "
                             "comma-separated is fine); the generated anchors cover them plus the PNGs already in the "
                             "repository")
    parser.add_argument("--reink", action="store_true", help="force every outer boundary texel to ink (recorded)")
    args = parser.parse_args(argv)
    only = [name for part in args.only for name in part.split(",") if name] if args.only else None
    if only is not None and not args.write_repo:
        parser.error("--only needs --write-repo")
    source = args.source or find_delivery()
    if source is None or not is_delivery(source):
        parser.error(f"delivery folder with manifest.json not found; pass --source or set {DELIVERY_ENV}")
    exporter = Exporter(source, reink=args.reink).run()
    try:
        report = write_outputs(exporter, args.preview, args.write_repo, only=only)
    except ValueError as error:
        parser.error(str(error))
    print(summary(report))
    if args.write_repo:
        print(f"\nrepository: {len(select_outputs(exporter.outputs, only))} PNGs in {TEXTURE_REL.as_posix()} "
              f"and {ANCHORS_REL.as_posix()}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
