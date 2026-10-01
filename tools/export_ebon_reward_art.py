"""Export the Ebon Manor reward pixel art from the Codex image delivery (no painting).

Inputs are the Codex-recommended candidates in the external delivery folder
asset-deliveries/ebon-rewards/2026-10-02/alpha (hash-pinned below; selections in
docs/encounters/ebon-manor/ASSET_BRIEF.md). Every step is mechanical and reuses
tools/export_ebon_art.py (clean_alpha, block_mode):

- Haze: alpha below 16 becomes 0. Objects are the connected parts of alpha > 128
  (parts closer than 6 px join; specks under 64 px are dropped), so drawings that
  cross the nominal cell lines stay whole; each object is cut out alone.
- Dot pitch, per sheet: strong colour or alpha edges (RGB L1 >= 48) inside the
  objects; the pitch is the period (3-12 px, 0.02 steps) whose edge phases agree
  best inside 64 px tiles. The edge-spacing mode and the manifest estimated_dot_px
  (a shading-run mode, 2 px everywhere) are recorded as cross-checks only.
- The logical pixel is the whole number of measured dots nearest the brief's 8 px
  (n = round(8 / pitch)): sheets drawn with ~4 px dots take 2 dots per logical pixel.
- Logical size = round(opaque bbox / logical pitch); block_mode then takes the
  majority colour of each logical cell (per-object 48-colour median cut, no
  dither) and makes it opaque when at least half its source pixels are opaque.
  No pixel is repainted and no colour is adjusted; parts are only placed whole.
- Icons larger than 32x32 logical (buffs 16x16) are reported and fitted by the
  same majority resample at the allowed size. "Icon 2x" textures repeat each
  logical pixel as 2x2 texels; "1x" textures keep one texel per logical pixel
  (the code draws them at 2x with point sampling).

Outputs in Assets/Textures/Items/EbonRewards (measured 2026-10-02; report.json
in the preview directory holds the full record). Anchors are texture coordinates:
(0,0) is the top-left corner and a texel centre is +0.5.
  EbonHatbox      64x60 icon 2x (38x36 logical at the pitch, fitted to 32x30)
  HatboxBody      33x26   HatboxLid 38x19   HatboxBow 23x19
  ShearUpper      131x25  pivot hole centre (50.21, 15.00)
  ShearLower      132x29  pivot hole centre (51.42, 12.83)
  EbonMoonshear   64x62 icon 2x (34x33, fitted to 32x31)
  LoomHarp        18x56   pegs (1.50, 2.00) and (1.50, 54.00), grip (14.19, 28.33)
  NeedleArrow     35x8    EbonLoomHarp 24x60 icon 2x
  EbonThimble     54x64 icon 2x (30x36, fitted to 27x32)
  Furniture       384x64  eight 48x64 cells, eyelets at cell x 23.5-24.0, rows 12-22
  Piano           76x57   eyelet (65.50, 0)
  Chandelier      41x68   wick tops (5.5, 40) (9.5, 22) (33.5, 22) (36.5, 40)
  ChandelierSmall 34x49   wick tops (5.0, 25) (31.0, 24)
  EbonChandelierPole 62x64 icon 2x (59x61, fitted to 31x32)
  ChandelierFlame 16x14   two 8x14 frames, flames bottom-aligned
  ChandelierCrystal 12x26 EbonChandelierBuff 32x32 (24x25, fitted to 15x16)
  Spool           11x18   EbonSeveringSilk 64x48 icon 2x (36x27, fitted to 32x24)
  ScissorsClosed / ScissorsOpen 35x18, shared beak hinge (26.0, 8.0)
  EbonLastWaltz   56x64 icon 2x (29x34, fitted to 28x32)
  EbonLastWaltzBuff 32x32 (23x24, fitted to 16x16)
  NoiretteWaltz   192x64  four 48x64 cells, feet on row 61 (Noirette.png row 58)
Requires Pillow, numpy and scipy (local asset tools, not CI).
"""
import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from export_ebon_art import block_mode, clean_alpha, sha256  # noqa: E402  (shared recipe helpers)

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Textures" / "Items" / "EbonRewards"
PREVIEW_DIR = ROOT / ".local" / "ebon-reward-art"
NOIRETTE = ROOT / "Assets" / "Textures" / "EbonManor" / "Noirette.png"

# delivery file -> SHA-256 prefix of the exact recommended input
INPUTS = {
    "alpha/ER01_d.png": "e001b0893322",
    "alpha/ER02_c.png": "313653eda52f",
    "alpha/ER02I_c.png": "ae0d79e95d7f",
    "alpha/ER03_d.png": "7c8331de8854",
    "alpha/ER04_a.png": "8a2bbc42f540",
    "alpha/ER05_d.png": "3e1fb1c16651",
    "alpha/ER06_d.png": "164586769ae0",
    "alpha/ER07_d.png": "b6b2b9f19a5a",
    "alpha/ER08_b.png": "1849a66cbac6",
    "alpha/ER09_c.png": "83f16ba39a18",
    "alpha/ER10_a.png": "7db6f9f20464",
}
# nominal sheet grid (columns, rows) and the number of objects expected in it
SHEETS = {
    "alpha/ER01_d.png": ((2, 2), 4),
    "alpha/ER02_c.png": ((1, 2), 2),
    "alpha/ER02I_c.png": ((1, 1), 1),
    "alpha/ER03_d.png": ((3, 1), 3),
    "alpha/ER04_a.png": ((1, 1), 1),
    "alpha/ER05_d.png": ((4, 2), 8),
    "alpha/ER06_d.png": ((1, 1), 1),
    "alpha/ER07_d.png": ((3, 2), 7),
    "alpha/ER08_b.png": ((4, 1), 4),
    "alpha/ER09_c.png": ((2, 1), 2),
    "alpha/ER10_a.png": ((4, 2), 4),
}

NOMINAL_DOT = 8.0   # the brief's output px per logical pixel
OPAQUE = 128
JOIN = 6            # px; parts closer than this belong to one object
SPECK = 64          # px; smaller opaque parts are dropped as residue
EDGE = 48           # RGB L1 difference that counts as a drawn edge
PERIODS = np.arange(3.0, 12.0001, 0.02)
TILE = 64
COLORS = 48
ICON_MAX, BUFF_MAX = 32, 16
CELL = (48, 64)     # Furniture / Noirette cells (logical)


def load(source, name):
    path = source / name
    digest = sha256(path)
    if not digest.startswith(INPUTS[name]):
        raise ValueError(f"{name}: unexpected input {digest[:12]} (accepted {INPUTS[name]})")
    return clean_alpha(Image.open(path).convert("RGBA")), digest


class Part:
    """One connected drawing of a sheet, cut out alone (soft fringe kept, neighbours removed)."""

    def __init__(self, rgba, region, opaque):
        ys, xs = np.nonzero(opaque)
        self.box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
        x0, y0, x1, y1 = self.box
        cut = rgba[y0:y1, x0:x1].copy()
        cut[~region[y0:y1, x0:x1]] = 0
        self.rgba = cut
        self.image = Image.fromarray(cut)
        self.centre = (float(xs.mean()), float(ys.mean()))
        self.area = int(len(xs))


def parts_of(image, grid, expected):
    rgba = np.asarray(image)
    opaque = rgba[..., 3] > OPAQUE
    labels, count = ndimage.label(ndimage.binary_dilation(opaque, iterations=JOIN), structure=np.ones((3, 3)))
    parts, dropped = [], 0
    for index in range(1, count + 1):
        region = labels == index
        solid = opaque & region
        if solid.sum() < SPECK:
            dropped += int(solid.sum() > 0)
            continue
        parts.append(Part(rgba, region, solid))
    if len(parts) != expected:
        raise ValueError(f"found {len(parts)} objects, expected {expected}")
    cw, ch = image.width / grid[0], image.height / grid[1]
    for part in parts:
        part.cell = (int(part.centre[0] // cw), int(part.centre[1] // ch))
        part.cell_origin = (part.cell[0] * cw, part.cell[1] * ch)
    parts.sort(key=lambda p: (p.cell[1], p.cell[0], p.box[0]))
    return parts, dropped


def edge_maps(rgba):
    rgb = rgba[..., :3].astype(np.int32)
    opaque = rgba[..., 3] > OPAQUE
    dx = np.abs(np.diff(rgb, axis=1)).sum(-1)
    dy = np.abs(np.diff(rgb, axis=0)).sum(-1)
    ex = ((dx >= EDGE) & opaque[:, 1:] & opaque[:, :-1]) | (opaque[:, 1:] != opaque[:, :-1])
    ey = ((dy >= EDGE) & opaque[1:, :] & opaque[:-1, :]) | (opaque[1:, :] != opaque[:-1, :])
    return ex, ey


def tile_coherence(edges, axis):
    """Sum over 64 px tiles of |mean phase| x count for every candidate period (the grid may drift)."""
    total, weight = np.zeros_like(PERIODS), 0
    h, w = edges.shape
    for y0 in range(0, h, TILE):
        for x0 in range(0, w, TILE):
            ys, xs = np.nonzero(edges[y0:y0 + TILE, x0:x0 + TILE])
            pos = (xs if axis == 1 else ys).astype(float) + 1
            if len(pos) < 12:
                continue
            total += np.abs(np.exp(2j * np.pi * pos[None, :] / PERIODS[:, None]).mean(1)) * len(pos)
            weight += len(pos)
    return total, weight


def spacing_histogram(edges, axis):
    lines = edges if axis == 1 else edges.T
    gaps = []
    for line in lines:
        xs = np.nonzero(line)[0]
        if len(xs) > 1:
            d = np.diff(xs)
            gaps.extend(d[d >= 3].tolist())
    return np.bincount(gaps, minlength=16)[:16] if gaps else np.zeros(16, dtype=int)


def dot_pitch(parts):
    total, weight, spacing = np.zeros_like(PERIODS), 0, np.zeros(16, dtype=int)
    for part in parts:
        ex, ey = edge_maps(part.rgba)
        for axis, edges in ((1, ex), (0, ey)):
            t, w = tile_coherence(edges, axis)
            total, weight = total + t, weight + w
            spacing = spacing + spacing_histogram(edges, axis)
    spectrum = total / max(weight, 1)
    best = int(spectrum.argmax())
    dot = float(PERIODS[best])
    dots = max(1, round(NOMINAL_DOT / dot))
    return {
        "dot_px": round(dot, 2),
        "coherence": round(float(spectrum[best]), 3),
        "dots_per_logical": dots,
        "logical_px": round(dot * dots, 2),
        "edge_spacing_mode_px": int(spacing.argmax()),
        "edges": int(weight),
    }


def trim(image):
    alpha = np.asarray(image)[..., 3]
    ys, xs = np.nonzero(alpha)
    return image.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))


def resample(part, pitch, limit=None):
    """round(bbox / pitch) logical cells, majority colour; fitted (and reported) above `limit`."""
    w, h = part.image.size
    size = (max(1, round(w / pitch)), max(1, round(h / pitch)))
    info = {"source_box": list(part.box), "logical_at_pitch": list(size)}
    if limit is not None:
        lw, lh = limit if isinstance(limit, tuple) else (limit, limit)
        if size[0] > lw or size[1] > lh:
            fit = max(w / lw, h / lh)
            size = (min(lw, max(1, round(w / fit))), min(lh, max(1, round(h / fit))))
            info["fitted_to"] = list(size)
    image = trim(block_mode(part.image, size[0], size[1], colors=COLORS))
    info["logical"] = list(image.size)
    return image, info


def double(image):
    return image.resize((image.width * 2, image.height * 2), Image.NEAREST)


def centred(image, size):
    canvas = Image.new("RGBA", size)
    canvas.alpha_composite(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return canvas


def alpha_of(image):
    return np.asarray(image)[..., 3] > 0


def enclosed_holes(image):
    """Transparent 4-connected regions that do not reach the texture border."""
    clear = ~alpha_of(image)
    labels, count = ndimage.label(clear)
    border = set(np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]])).tolist())
    holes = []
    for index in range(1, count + 1):
        if index in border:
            continue
        ys, xs = np.nonzero(labels == index)
        holes.append({"centre": [round(float(xs.mean()) + .5, 2), round(float(ys.mean()) + .5, 2)],
                      "area": int(len(xs)), "box": [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]})
    return holes


def source_hole(part, image):
    """The pivot hole measured in the delivery (alpha <= 128 enclosed), mapped to texture coordinates."""
    clear = part.rgba[..., 3] <= OPAQUE
    labels, count = ndimage.label(clear)
    border = set(np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]])).tolist())
    width = part.image.width
    found = []
    for index in range(1, count + 1):
        if index in border:
            continue
        ys, xs = np.nonzero(labels == index)
        if len(xs) >= 16 and .25 < xs.mean() / width < .6:
            found.append((len(xs), float(xs.mean()) + .5, float(ys.mean()) + .5))
    if not found:
        return None
    area, cx, cy = max(found)
    sx, sy = image.width / part.image.width, image.height / part.image.height
    return [round(cx * sx, 2), round(cy * sy, 2)]


def pivot_hole(image):
    """The pivot: the enclosed hole between 25% and 60% of the length (the loop is further left)."""
    holes = [h for h in enclosed_holes(image) if .25 < h["centre"][0] / image.width < .6]
    return max(holes, key=lambda h: h["area"]) if holes else None


def colour_mask(image, kind):
    rgba = np.asarray(image).astype(np.int32)
    r, g, b, a = rgba[..., 0], rgba[..., 1], rgba[..., 2], rgba[..., 3] > 0
    if kind == "ivory":  # warm light candle wax; crystals are cooler, gold is warmer
        return a & (np.minimum(np.minimum(r, g), b) >= 170) & (r - b >= 20) & (r - b <= 45)
    if kind == "rose":
        return a & (r >= 110) & (r - g >= 40) & (r - b >= 25)
    raise ValueError(kind)


def wick_tops(image):
    """Top-most ivory texels of each candle (ivory 8-connected clusters of 5+ texels, 3+ rows tall)."""
    labels, count = ndimage.label(colour_mask(image, "ivory"), structure=np.ones((3, 3)))
    tops = []
    for index in range(1, count + 1):
        ys, xs = np.nonzero(labels == index)
        if len(xs) < 5 or ys.max() - ys.min() < 2:
            continue
        row = xs[ys == ys.min()]
        tops.append([round(float(row.mean()) + .5, 2), int(ys.min())])
    return sorted(tops)


def eyelet(alpha, x_offset=0):
    """The top-most opaque point: centre of the top opaque row, at that row's top edge."""
    ys, xs = np.nonzero(alpha)
    row = xs[ys == ys.min()]
    return [round(float(row.mean()) + .5 + x_offset, 2), int(ys.min())]


def harp_anchors(image):
    alpha = alpha_of(image)
    h = alpha.shape[0]
    ys, xs = np.nonzero(alpha)
    pegs = []
    for upper in (True, False):
        half = (ys < h / 2) if upper else (ys >= h / 2)
        left = xs[half].min()
        near = half & (xs <= left + 2) & ((ys <= ys[half].min() + 3) if upper else (ys >= ys[half].max() - 3))
        pegs.append([round(float(xs[near].mean()) + .5, 2), round(float(ys[near].mean()) + .5, 2)])
    gy, gx = np.nonzero(colour_mask(image, "rose"))
    grip = [round(float(gx.mean()) + .5, 2), round(float(gy.mean()) + .5, 2)] if len(gx) else None
    return {"peg_top": pegs[0], "peg_bottom": pegs[1], "grip": grip, "grip_rose_texels": int(len(gx))}


def register(closed, opened):
    """Integer shift of the open scissors that best overlaps the closed ones on the loop half."""
    a, b = alpha_of(closed), alpha_of(opened)
    best = None
    for dy in range(-4, 5):
        for dx in range(-4, 5):
            ca = np.zeros((40 + a.shape[0], 40 + a.shape[1]), bool)
            cb = np.zeros_like(ca)
            ca[20:20 + a.shape[0], 20:20 + a.shape[1]] = a
            cb[20 + dy:20 + dy + b.shape[0], 20 + dx:20 + dx + b.shape[1]] = b
            left = slice(0, 20 + int(a.shape[1] * .45))
            inter = (ca[:, left] & cb[:, left]).sum()
            union = (ca[:, left] | cb[:, left]).sum()
            score = inter / max(union, 1)
            if best is None or score > best[0]:
                best = (score, dx, dy)
    return best


def beak_hinge(opened):
    """Left-most column right of the loops where the open beak shows two separate blades."""
    alpha = alpha_of(opened)
    w = alpha.shape[1]
    for x in range(int(w * .45), w):
        column = alpha[:, x]
        runs = np.flatnonzero(np.diff(np.concatenate([[0], column.astype(int), [0]])))
        if len(runs) >= 4:
            starts, ends = runs[0::2], runs[1::2]
            return [float(x), round((ends[0] + starts[1]) / 2, 2)]
    return None


def noirette_baseline():
    alpha = np.asarray(Image.open(NOIRETTE).convert("RGBA"))[..., 3] > 0
    rows = []
    for col in range(4):
        ys, xs = np.nonzero(alpha[0:64, col * 48:(col + 1) * 48])
        rows.append({"bottom_row": int(ys.max()), "top_row": int(ys.min()), "width": int(xs.max() - xs.min() + 1)})
    return {"sha256": sha256(NOIRETTE), "top_row_cells": rows}


def contact_sheet(outputs, path):
    font = ImageFont.load_default(size=22)
    tiles = []
    for name, image in outputs.items():
        big = image.resize((image.width * 4, image.height * 4), Image.NEAREST)
        pad, label = 12, f"{name}  {image.width}x{image.height}"
        width = max(big.width * 2 + pad * 3, int(font.getlength(label)) + pad * 2)
        tile = Image.new("RGB", (width, big.height + pad * 2 + 30), (60, 60, 64))
        for i, colour in enumerate(((22, 22, 30), (214, 210, 198))):
            bg = Image.new("RGBA", big.size, colour + (255,))
            bg.alpha_composite(big)
            tile.paste(bg.convert("RGB"), (pad + i * (big.width + pad), pad + 30))
        ImageDraw.Draw(tile).text((pad, 6), label, fill=(240, 236, 226), font=font)
        tiles.append(tile)
    width, rows, row, x = 3300, [], [], 0
    for tile in tiles:
        if row and x + tile.width > width:
            rows.append(row)
            row, x = [], 0
        row.append(tile)
        x += tile.width + 8
    rows.append(row)
    height = sum(max(t.height for t in r) + 8 for r in rows)
    sheet = Image.new("RGB", (width, height), (40, 40, 44))
    y = 0
    for r in rows:
        x = 0
        for tile in r:
            sheet.paste(tile, (x, y))
            x += tile.width + 8
        y += max(t.height for t in r) + 8
    sheet.save(path, optimize=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", type=Path, required=True, help="asset-deliveries/ebon-rewards/2026-10-02 (never committed)")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR)
    parser.add_argument("--preview", type=Path, default=PREVIEW_DIR, help="local contact sheet and report directory")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    args.preview.mkdir(parents=True, exist_ok=True)
    report = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__)),
              "coordinates": "texture space, (0,0) top-left corner, texel centre +0.5",
              "inputs": {}, "sheets": {}, "outputs": {}, "deviations": []}
    manifest = json.loads((args.source / "manifest.json").read_text(encoding="utf-8"))
    manifest_dots = {f["file"]: f.get("alpha_check", {}).get("estimated_dot_px") for f in manifest["files"]}
    outputs, notes = {}, {}

    sheets = {}
    for name, (grid, expected) in SHEETS.items():
        image, report["inputs"][name] = load(args.source, name)
        parts, dropped = parts_of(image, grid, expected)
        pitch = dot_pitch(parts)
        pitch["manifest_estimated_dot_px"] = manifest_dots.get(name)
        pitch["objects"] = [{"cell": list(p.cell), "box": list(p.box)} for p in parts]
        pitch["dropped_specks"] = dropped
        report["sheets"][name] = pitch
        sheets[name] = (parts, pitch["logical_px"])

    def put(name, image, **info):
        outputs[name] = image
        notes[name] = info

    def icon(name, part, pitch, limit=ICON_MAX):
        image, info = resample(part, pitch, limit)
        if "fitted_to" in info:
            report["deviations"].append(f"{name}: {info['logical_at_pitch'][0]}x{info['logical_at_pitch'][1]} logical at the "
                                        f"measured pitch exceeds {limit}x{limit}; fitted to {info['logical'][0]}x{info['logical'][1]}")
        return image, info

    # ER01 hatbox: complete box (icon), body, lid, bow
    parts, p = sheets["alpha/ER01_d.png"]
    image, info = icon("EbonHatbox", parts[0], p)
    put("EbonHatbox.png", double(image), scale="icon 2x", **info)
    for part, name in zip(parts[1:], ("HatboxBody.png", "HatboxLid.png", "HatboxBow.png")):
        image, info = resample(part, p)
        put(name, image, scale="1x", **info)

    # ER02 shear halves (1x) with their pivot holes; ER02I icon
    parts, p = sheets["alpha/ER02_c.png"]
    for part, name in zip(parts, ("ShearUpper.png", "ShearLower.png")):
        image, info = resample(part, p)
        hole = pivot_hole(image)
        put(name, image, scale="1x", **info, pivot_hole=hole["centre"] if hole else None, pivot_hole_box=hole["box"] if hole else None,
            pivot_hole_from_source=source_hole(part, image), enclosed_holes=enclosed_holes(image))
        if hole is None:
            report["deviations"].append(f"{name}: the pivot hole closed in the export; use pivot_hole_from_source")
    parts, p = sheets["alpha/ER02I_c.png"]
    image, info = icon("EbonMoonshear", parts[0], p)
    put("EbonMoonshear.png", double(image), scale="icon 2x", **info)

    # ER03 bow (pegs, grip), needle arrow, icon
    parts, p = sheets["alpha/ER03_d.png"]
    image, info = resample(parts[0], p)
    put("LoomHarp.png", image, scale="1x", **info, **harp_anchors(image))
    image, info = resample(parts[1], p)
    put("NeedleArrow.png", image, scale="1x", **info)
    image, info = icon("EbonLoomHarp", parts[2], p)
    put("EbonLoomHarp.png", double(image), scale="icon 2x", **info)

    parts, p = sheets["alpha/ER04_a.png"]
    image, info = icon("EbonThimble", parts[0], p)
    put("EbonThimble.png", double(image), scale="icon 2x", **info)

    # ER05 furniture: 8 cells of 48x64, each prop centred, eyelet = top-most opaque point
    parts, p = sheets["alpha/ER05_d.png"]
    names = ["armchair", "candelabra", "portrait", "clock", "birdcage", "mirror", "music box", "cello"]
    atlas, props = Image.new("RGBA", (CELL[0] * 8, CELL[1])), []
    for index, (part, prop) in enumerate(zip(parts, names)):
        image, info = resample(part, p, CELL)
        if "fitted_to" in info:
            report["deviations"].append(f"Furniture {prop}: {info['logical_at_pitch']} logical exceeds 48x64; fitted to {info['logical']}")
        x, y = index * CELL[0] + (CELL[0] - image.width) // 2, (CELL[1] - image.height) // 2
        atlas.alpha_composite(image, (x, y))
        top = eyelet(alpha_of(image), x)
        props.append({"prop": prop, "cell": index, "offset": [x - index * CELL[0], y], **info,
                      "eyelet": [top[0], top[1] + y], "eyelet_in_cell": [round(top[0] - index * CELL[0], 2), top[1] + y]})
    put("Furniture.png", atlas, scale="1x", cells=props)

    parts, p = sheets["alpha/ER06_d.png"]
    image, info = resample(parts[0], p)
    put("Piano.png", image, scale="1x", eyelet=eyelet(alpha_of(image)), **info)

    # ER07 chandeliers (wick tops), pole icon, flames, crystal, buff
    parts, p = sheets["alpha/ER07_d.png"]
    for part, name in zip(parts[:2], ("Chandelier.png", "ChandelierSmall.png")):
        image, info = resample(part, p)
        put(name, image, scale="1x", eyelet=eyelet(alpha_of(image)), wick_tops=wick_tops(image), **info)
    image, info = icon("EbonChandelierPole", parts[2], p)
    put("EbonChandelierPole.png", double(image), scale="icon 2x", **info)
    flames = [resample(part, p) for part in parts[3:5]]
    fw, fh = max(f[0].width for f in flames), max(f[0].height for f in flames)
    strip, frames = Image.new("RGBA", (fw * 2, fh)), []
    for index, (image, info) in enumerate(flames):
        x, y = index * fw + (fw - image.width) // 2, fh - image.height
        strip.alpha_composite(image, (x, y))
        frames.append({"frame": index, "offset": [x - index * fw, y], **info})
    put("ChandelierFlame.png", strip, scale="1x", frame_size=[fw, fh], frames=frames,
        base="flames bottom-aligned and horizontally centred in equal cells")
    image, info = resample(parts[5], p)
    put("ChandelierCrystal.png", image, scale="1x", eyelet=eyelet(alpha_of(image)), **info)
    image, info = icon("EbonChandelierBuff", parts[6], p, BUFF_MAX)
    put("EbonChandelierBuff.png", double(centred(image, (BUFF_MAX, BUFF_MAX))), scale="buff 16x16 at 2x", **info)

    # ER08 spool, icon, scissors closed/open on one canvas with the hinge aligned
    parts, p = sheets["alpha/ER08_b.png"]
    image, info = resample(parts[0], p)
    put("Spool.png", image, scale="1x", **info)
    image, info = icon("EbonSeveringSilk", parts[1], p)
    put("EbonSeveringSilk.png", double(image), scale="icon 2x", **info)
    (closed, closed_info), (opened, open_info) = resample(parts[2], p), resample(parts[3], p)
    score, dx, dy = register(closed, opened)
    x0, y0 = min(0, dx), min(0, dy)
    size = (max(closed.width, dx + opened.width) - x0, max(closed.height, dy + opened.height) - y0)
    canvas_closed, canvas_open = Image.new("RGBA", size), Image.new("RGBA", size)
    canvas_closed.alpha_composite(closed, (-x0, -y0))
    canvas_open.alpha_composite(opened, (dx - x0, dy - y0))
    hinge = beak_hinge(canvas_open)
    pivot = [hinge[0], hinge[1]] if hinge else None
    common = {"loop_registration_iou": round(float(score), 3), "pivot": pivot,
              "pivot_rule": "left-most column where the open beak shows two blades; loops registered to the closed pose"}
    put("ScissorsClosed.png", canvas_closed, scale="1x", offset=[-x0, -y0], **closed_info, **common)
    put("ScissorsOpen.png", canvas_open, scale="1x", offset=[dx - x0, dy - y0], **open_info, **common)

    parts, p = sheets["alpha/ER09_c.png"]
    image, info = icon("EbonLastWaltz", parts[0], p)
    put("EbonLastWaltz.png", double(image), scale="icon 2x", **info)
    image, info = icon("EbonLastWaltzBuff", parts[1], p, BUFF_MAX)
    put("EbonLastWaltzBuff.png", double(centred(image, (BUFF_MAX, BUFF_MAX))), scale="buff 16x16 at 2x", **info)

    # ER10 companion poses: Noirette.png cells (48x64), feet on one common row, cell-relative x kept
    parts, p = sheets["alpha/ER10_a.png"]
    poses = [(part, *resample(part, p)) for part in parts]
    baseline = noirette_baseline()
    noir_bottom = max(c["bottom_row"] for c in baseline["top_row_cells"]) + 1
    tallest = max(image.height for _, image, _ in poses)
    bottom = min(CELL[1], max(noir_bottom, tallest))
    sheet, cells = Image.new("RGBA", (CELL[0] * 4, CELL[1])), []
    for index, (part, image, info) in enumerate(poses):
        x = round((part.box[0] - part.cell_origin[0]) / p)
        shift = 0
        if x + image.width > CELL[0]:
            shift = CELL[0] - image.width - x
        elif x < 0:
            shift = -x
        overflow = max(0, image.width - CELL[0])
        if overflow:
            report["deviations"].append(f"NoiretteWaltz pose {index + 1}: {image.width} wide, {overflow} px clipped by the 48 px cell")
        crop = image.crop((overflow // 2, max(0, image.height - bottom), overflow // 2 + min(image.width, CELL[0]), image.height))
        sheet.alpha_composite(crop, (index * CELL[0] + max(0, x + shift), bottom - crop.height))
        cells.append({"pose": index + 1, **info, "x": max(0, x + shift), "x_shift_to_fit": shift,
                      "top_row": bottom - crop.height, "clipped_top_rows": image.height - crop.height})
    feet = bottom - 1
    if feet != noir_bottom - 1:
        report["deviations"].append(f"NoiretteWaltz feet on row {feet}, Noirette.png on row {noir_bottom - 1}: "
                                    f"draw {feet - noir_bottom + 1} logical px higher to share the baseline (no top clipping)")
    put("NoiretteWaltz.png", sheet, scale="1x", cells=cells, feet_row=feet, noirette=baseline)

    for name, image in outputs.items():
        target = args.output / name
        image.save(target, optimize=True)
        report["outputs"][name] = {"size": list(image.size), "sha256": sha256(target), **notes[name]}
    for name, sheet_info in report["sheets"].items():
        if sheet_info["dots_per_logical"] > 1:
            report["deviations"].append(f"{name}: drawn dots {sheet_info['dot_px']} px; {sheet_info['dots_per_logical']} dots per logical pixel "
                                        f"({sheet_info['logical_px']} px)")
    contact_sheet(outputs, args.preview / "contact.png")
    (args.preview / "report.json").write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    for name, entry in report["outputs"].items():
        print(f"{name}: {entry['size'][0]}x{entry['size'][1]}")
    print("\n".join(report["deviations"]))


if __name__ == "__main__":
    main()
