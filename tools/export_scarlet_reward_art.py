"""Export the Scarlet Invocation reward pixel art from the Codex image delivery (no painting).

Inputs are the Codex-recommended candidates in the external delivery folder
asset-deliveries/scarlet-rewards/2026-10-02/alpha (hash-pinned below; selections in
docs/encounters/crimson-foundry/REWARDS.md#art-and-audio). Every step is mechanical and reuses the
Ebon reward recipe (tools/export_ebon_reward_art.py: object cut, dot pitch; tools/export_ebon_art.py:
clean_alpha, and block_mode's palette and majority rule):

- Haze: alpha below 16 becomes 0. Objects are the connected parts of alpha > 128 (parts closer than
  6 px join); each object is cut out alone.
- Dot pitch, per sheet, as Ebon's (edge-phase coherence, 3-12 px). One logical pixel is one measured dot.
- Logical size = round(opaque bbox / pitch). An object larger than its brief limit ("never more than",
  the MaxWidth/MaxHeight of CrimsonRewardSprites) is fitted to the limit by the same majority resample
  (block_mode's rule: per-object 48-colour median cut, majority colour per cell, opaque at half coverage, no
  dither). The lattice may sit at any whole source px against the drawing; each object takes the phase whose cells
  disagree with the fewest of its px (never growing it), because a 1-dot mark such as the seals' three bars survives
  only at some phases. No pixel is repainted and no colour is adjusted; parts are only placed whole.
- Reliquary: the icon is SR01_a's complete casket. The opening-show parts are SR01P_c's body, lid and seal
  (regenerated from SR01_a so the lid fits the body; SR01_a's own parts are drawn at a flatter angle and its lid
  overhangs the body). The lid is centred across the body's front rim, at the height leaving the fewest velvet px
  open plus front px (under the velvet) covered (FFT correlation), then lowest; body and lid are resampled on ONE
  lattice (one phase for both) whose cell makes the closed casket as wide as the icon, so the lid offset is an exact
  whole number of texels. The hinge is the left-most corner of the lid's bottom edge that rests on the body, so the
  opening lid turns on the body. The seal is 11 cells across (SEAL_CELLS), one more than the lattice gives, so its
  three bars stay apart; it is placed on the ring recess.
- Quill: Codex drew the ink bead detached ahead of the nib (REPORT.md notes it); after the resample only
  the quill's largest 8-connected piece is kept, so no flying ink is drawn into the sprite.
- Anchors are measured on the exported textures from colour classes (bone, gold, red, dark) and shapes
  (enclosed holes, lines, geodesic thirds); each rule is written next to its value in report.json.
- Item icons are doubled ("icon 2x", tML draws ModItem.Texture at 1x); buff icons are centred on 16x16 logical and
  doubled (32x32 like vanilla buffs). World bodies keep one texel per logical pixel and are drawn at
  CrimsonRewardSprites.PixelScale with point sampling.

Outputs in Assets/Textures/Items/ScarletRewards (measured 2026-10-03; report.json in the preview
directory holds the full record with each lattice phase, the contact sheet, the anchor sheet and the real-size
mock-ups). Sizes are logical pixels; anchors are texture coordinates: (0,0) is the top-left corner, a texel centre +0.5.
  CrimsonScoreReliquary 64x50 icon 2x (SR01_a complete casket, 50x39 at the 7.82 px pitch, fitted to 32x25)
  ReliquaryBody 32x18   SR01P_c on one lattice of 10.562 px (closed casket as wide as the icon):
                        lid top-left at (1, -7), seal top-left at (8, 5) (ring recess (13.4, 10.18)),
                        mouth (velvet centre) (16.63, 3.14); the closed lid overhangs the body 0 / 1 texels
  ReliquaryLid  30x13   rear hinge (1, 11), resting on the body's top-left corner
  ReliquarySeal 11x11   (SEAL_CELLS)
  SableScythe   64x56   (84x73 at 5.84 px, fitted) grip (5.74, 50.61), hook tip (61.5, 31.5): reach 117.89 px;
                        BladeKnots (tip frame) (0.967, -0.491) (0.969, -0.407) (1.056, -0.199) (1, 0)
  CrimsonSableScythe 64x56 icon 2x (32x28 at 8.92 px)
  CanticleOrgan 43x22   (80x41 at 5.76 px, fitted) grip (5.86, 16.84), heart-gem (17.71, 7.86),
                        pipe mouths (43, 3) (42, 6) (42, 10) (42, 13)
  CanticleShard 11x6    (25x13, fitted)
  BoneHand      20x32   (36x56, fitted) palm (11.5, 12.43)
  CrimsonCanticleOrgan 54x56 icon 2x (39x40, fitted to 27x28; its four barrels merge at this size)
  ScarletBaton  37x38   (at 7.82 px) grip (3.55, 32.7), gem (33.88, 3.19)
  CrimsonBaton  52x52 icon 2x (26x26 at 7.64 px)
  CrimsonEmberCenser 36x64 icon 2x (23x39 at 9.16 px, fitted to 18x32)
  EmberCenser   30x36   (37x45, fitted) ring (14.5, 3), bowl mouth (14.5, 20) on the gold rim -> BowlDrop 34 px,
                        drape ends (3.5, 30) (26, 31)
  CrimsonEmberCenserBuff 32x32 (14x13 on 16x16, 2x)
  BloodinkQuill 25x7    (51x13 at 6.74 px, fitted to 28x7; the detached ink bead cut) nib (25, 3.5)
  CrimsonBloodinkQuill 40x64 icon 2x (33x53, fitted to 20x32)
  SealedScore   24x10   (42x20, fitted to 24x11; its best phase leaves 10 rows) seal (12.68, 4.09)
  CrimsonPact   58x64 icon 2x (35x38 at 10.84 px, fitted to 29x32)
  CrimsonPactBuff 32x32 (15x15, fitted to 14x14 on 16x16, 2x)
Requires Pillow, numpy and scipy (local asset tools, not CI).
"""
import argparse
import heapq
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage, signal

sys.path.insert(0, str(Path(__file__).resolve().parent))
from export_ebon_art import clean_alpha, palette_of, sha256  # noqa: E402  (shared recipe helpers)
from export_ebon_reward_art import COLORS, dot_pitch, double, parts_of  # noqa: E402  (shared recipe helpers)

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Textures" / "Items" / "ScarletRewards"
PREVIEW_DIR = ROOT / ".local" / "scarlet-reward-art"
SANCTUM = ROOT / "Assets" / "Textures" / "Backgrounds" / "ScarletSanctum.png"

# delivery file -> SHA-256 prefix of the exact recommended input (all accepted_by_codex=false: the strict grid and edges, and
# recorded size and shape shortfalls; CODEX_SHORTFALLS)
INPUTS = {
    "alpha/SR01_a.png": "e3ab89dcce56",
    "alpha/SR01P_c.png": "a3624df04da2",
    "alpha/SR02_c.png": "41a498cf43a5",
    "alpha/SR02I_c.png": "53908b6ba7db",
    "alpha/SR03_d.png": "dffc1e82f7de",
    "alpha/SR04_c.png": "89994551b9db",
    "alpha/SR04I_b.png": "e537bedf532f",
    "alpha/SR05_d.png": "c9c9cd000579",
    "alpha/SR06_d.png": "1dd111845c7e",
    "alpha/SR07_b.png": "2a18e8a8f0fd",
}
# nominal sheet grid (columns, rows) and the number of objects expected in it (SR01P_c's fourth cell is empty)
SHEETS = {
    "alpha/SR01_a.png": ((2, 2), 4),
    "alpha/SR01P_c.png": ((2, 2), 3),
    "alpha/SR02_c.png": ((1, 1), 1),
    "alpha/SR02I_c.png": ((1, 1), 1),
    "alpha/SR03_d.png": ((2, 2), 4),
    "alpha/SR04_c.png": ((1, 1), 1),
    "alpha/SR04I_b.png": ((1, 1), 1),
    "alpha/SR05_d.png": ((3, 1), 3),
    "alpha/SR06_d.png": ((3, 1), 3),
    "alpha/SR07_b.png": ((2, 1), 2),
}
# brief "never more than" limits in logical pixels (CrimsonRewardSprites MaxWidth/MaxHeight)
LIMITS = {
    "CrimsonScoreReliquary": (32, 28), "CrimsonSableScythe": (32, 32), "SableScythe": (64, 64),
    "CrimsonCanticleOrgan": (28, 28), "CanticleOrgan": (44, 22), "CanticleShard": (12, 6), "BoneHand": (24, 32),
    "CrimsonBaton": (32, 32), "ScarletBaton": (40, 40),
    "CrimsonEmberCenser": (24, 32), "EmberCenser": (32, 36), "CrimsonEmberCenserBuff": (14, 14),
    "BloodinkQuill": (28, 8), "CrimsonBloodinkQuill": (28, 32), "SealedScore": (24, 12),
    "CrimsonPact": (32, 32), "CrimsonPactBuff": (14, 14),
}
BUFF_CANVAS = 16
PIXEL_SCALE = 2     # CrimsonRewardSprites.PixelScale: screen px per logical pixel
BACKDROPS = {        # contact-sheet grounds: the Scarlet sanctum's dark and mid tones, and a bright surface sky
    "sanctum dark": (36, 18, 14), "sanctum mid": (110, 52, 38), "day sky": (150, 196, 242),
}


def load(source, name):
    path = source / name
    digest = sha256(path)
    if not digest.startswith(INPUTS[name]):
        raise ValueError(f"{name}: unexpected input {digest[:12]} (accepted {INPUTS[name]})")
    return clean_alpha(Image.open(path).convert("RGBA")), digest


def trim_offset(image):
    """Crop to the opaque texels and return the crop's top-left offset."""
    alpha = np.asarray(image)[..., 3]
    ys, xs = np.nonzero(alpha)
    box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
    return image.crop(box), box[:2]


class Export:
    """A resampled object: the texture and the map from delivery (sheet) px to texture coordinates."""

    def __init__(self, image, info, origin, cell, offset):
        self.image, self.info = image, info
        self.origin, self.cell, self.offset = origin, cell, offset

    def tex(self, sx, sy):
        return ((sx - self.origin[0]) / self.cell[0] - self.offset[0], (sy - self.origin[1]) / self.cell[1] - self.offset[1])


# ---- phased majority resample ---------------------------------------------------------------------------------
# block_mode's rule (majority colour per cell from a 48-colour median cut of the object, opaque at half coverage, no
# dither) on a lattice whose lines may sit at any whole source px against the drawing: a 1-dot mark (the three-bar
# seals) survives only at some phases, so every object takes the phase whose cells agree best with its own pixels.
# At phase (0, 0) this is block_mode exactly.

class Lattice:
    """The palette and pixel classes of one object, shared by every phase tried on it."""

    def __init__(self, image):
        quant = palette_of(image, COLORS)
        self.pal = np.asarray(quant.getpalette()[:COLORS * 3], dtype=np.uint8).reshape(-1, 3)
        self.quant = np.asarray(quant).astype(np.int64)
        self.opaque = np.asarray(image)[..., 3] >= 128
        self.size = image.size

    def cells(self, cell, start):
        """Cells of `cell` source px whose lines are at round(k * cell), with the object's (0,0) px at lattice px
        `start`. Returns the RGBA cells (from the cell holding `start`), the index of that first cell and the number of
        the object's px that disagree with their cell (opaque against transparent, or another palette colour)."""
        w, h = self.size
        index, lines = [], []
        for axis, n in ((0, w), (1, h)):
            k0 = math.floor(start[axis] / cell[axis])
            while round(k0 * cell[axis]) > start[axis]:
                k0 -= 1
            while round((k0 + 1) * cell[axis]) <= start[axis]:
                k0 += 1
            k1 = k0
            while round(k1 * cell[axis]) < start[axis] + n:
                k1 += 1
            edge = np.array([round(k * cell[axis]) for k in range(k0, k1 + 1)])
            at = np.searchsorted(edge, np.arange(n) + start[axis], side="right") - 1
            index.append((k0, at, edge))
        (kx, cx, ex), (ky, cy, ey) = index
        nx, ny = len(ex) - 1, len(ey) - 1
        cid = cy[:, None] * nx + cx[None, :]
        area = (np.diff(ey)[:, None] * np.diff(ex)[None, :]).ravel()
        solid = np.bincount(cid[self.opaque], minlength=nx * ny)
        votes = np.bincount(cid[self.opaque] * COLORS + self.quant[self.opaque], minlength=nx * ny * COLORS).reshape(nx * ny, COLORS)
        major = votes.argmax(1)
        on = solid / area >= .5
        out = np.zeros((ny * nx, 4), dtype=np.uint8)
        out[on, :3] = self.pal[major[on]]
        out[on, 3] = 255
        drawn = np.where(on, major, -1)[cid]
        errors = int((np.where(self.opaque, self.quant, -1) != drawn).sum())
        return Image.fromarray(out.reshape(ny, nx, 4)), (kx, ky), errors


def phase_search(image, cell):
    """The object resampled at every whole-px phase of its lattice; the one with the fewest disagreeing px wins (ties:
    the smaller shift), never larger than phase (0, 0) is in either axis."""
    lattice = Lattice(image)
    base, _ = trim_offset(lattice.cells(cell, (0, 0))[0])
    best = None
    for py in range(math.ceil(cell[1])):
        for px in range(math.ceil(cell[0])):
            cells, _, errors = lattice.cells(cell, (px, py))
            trimmed, offset = trim_offset(cells)
            if trimmed.width > base.width or trimmed.height > base.height:
                continue
            key = (errors, px * px + py * py)
            if best is None or key < best[0]:
                best = (key, trimmed, offset, (px, py))
            if (px, py) == (0, 0):
                zero = errors
    (errors, _), image, offset, phase = best
    return image, offset, phase, {"lattice_phase_px": list(phase), "disagreeing_px": errors, "disagreeing_px_at_phase_0": zero}


def resample(part, pitch, limit, size=None):
    """Ebon's rule: round(bbox / pitch) logical cells, majority colour, at the best-agreeing lattice phase; fitted (and
    reported) above `limit`. `size` forces the cell count (reported)."""
    w, h = part.image.size
    info = {"source_box": list(part.box)}
    if size is None:
        size = (max(1, round(w / pitch)), max(1, round(h / pitch)))
        info["logical_at_pitch"] = list(size)
        lw, lh = limit
        if size[0] > lw or size[1] > lh:
            fit = max(w / lw, h / lh)
            size = (min(lw, max(1, round(w / fit))), min(lh, max(1, round(h / fit))))
            info["fitted_to"] = list(size)
    else:
        info["cells"] = list(size)
    cell = (w / size[0], h / size[1])
    image, offset, phase, searched = phase_search(part.image, cell)
    info["logical"] = list(image.size)
    info["source_px_per_texel"] = [round(cell[0], 3), round(cell[1], 3)]
    info.update(searched)
    return Export(image, info, (part.box[0] - phase[0], part.box[1] - phase[1]), cell, offset)


def lattice_resample(parts, places, cell, limit_width):
    """Resample `parts` placed at `places` (whole source px in one frame, all >= 0) on the frame's lattice of `cell` px,
    at the common phase whose cells agree best with all of them (ties: the smaller shift), never larger than at phase
    (0, 0) and never wider together than `limit_width`. Returns the exports and each texture's top-left cell."""
    lattices = [Lattice(p.image) for p in parts]

    def at(phase):
        out = []
        for lattice, (x, y) in zip(lattices, places):
            cells, first, errors = lattice.cells((cell, cell), (x + phase[0], y + phase[1]))
            image, offset = trim_offset(cells)
            out.append((image, (first[0] + offset[0], first[1] + offset[1]), errors, offset, (x + phase[0], y + phase[1])))
        return out

    def width(out):
        return max(c[0] + i.width for i, c, *_ in out) - min(c[0] for _, c, *_ in out)

    base = at((0, 0))
    best = None
    for py in range(math.ceil(cell)):
        for px in range(math.ceil(cell)):
            out = base if (px, py) == (0, 0) else at((px, py))
            if any(o[0].width > b[0].width or o[0].height > b[0].height for o, b in zip(out, base)) or width(out) > limit_width:
                continue
            key = (sum(o[2] for o in out), px * px + py * py)
            if best is None or key < best[0]:
                best = (key, out, (px, py))
    (errors, _), out, phase = best
    exports = []
    for part, (image, first, part_errors, offset, start) in zip(parts, out):
        k0 = (first[0] - offset[0], first[1] - offset[1])
        # sheet px -> texture: the part's (0,0) px lies at lattice px `start`, the texture starts at cell `first`
        origin = (part.box[0] - start[0] + k0[0] * cell, part.box[1] - start[1] + k0[1] * cell)
        info = {"source_box": list(part.box), "logical": list(image.size), "lattice_cell": list(first),
                "source_px_per_texel": [round(cell, 3), round(cell, 3)], "lattice_phase_px": list(phase),
                "disagreeing_px": part_errors, "disagreeing_px_at_phase_0": base[len(exports)][2]}
        exports.append((Export(image, info, origin, (cell, cell), offset), first))
    return exports


# ---- colour classes (on delivery or exported RGBA) ------------------------------------------------------------

def classes(rgba):
    a = rgba[..., 3] > 128
    r, g, b = (rgba[..., i].astype(int) for i in range(3))
    lo = np.minimum(np.minimum(r, g), b)
    return {
        "opaque": a,
        "bone": a & (lo >= 135) & (r - b <= 90) & (r >= 170),
        "gold": a & (r >= 140) & (g >= 90) & (r - b >= 70) & (g - b >= 35) & (r - g <= 110),
        "red": a & (r >= 105) & (r - g >= 65) & (r - b >= 45),
        "dark": a & (np.maximum(np.maximum(r, g), b) <= 90),
    }


def centroid(mask):
    ys, xs = np.nonzero(mask)
    return [round(float(xs.mean()) + .5, 2), round(float(ys.mean()) + .5, 2)] if len(xs) else None


def largest(mask, structure=np.ones((3, 3))):
    labels, count = ndimage.label(mask, structure=structure)
    if count == 0:
        return mask
    sizes = ndimage.sum(mask, labels, range(1, count + 1))
    return labels == (int(np.argmax(sizes)) + 1)


def enclosed_hole(mask, near=None):
    """Transparent 4-connected regions not reaching the border; the largest (or the one nearest `near`)."""
    labels, count = ndimage.label(~mask)
    border = set(np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]])).tolist())
    holes = []
    for index in range(1, count + 1):
        if index in border:
            continue
        region = labels == index
        c = centroid(region)
        holes.append((int(region.sum()), c))
    if not holes:
        return None
    if near is not None:
        return min(holes, key=lambda h: (h[1][0] - near[0]) ** 2 + (h[1][1] - near[1]) ** 2)[1]
    return max(holes)[1]


# ---- reliquary ------------------------------------------------------------------------------------------------

def velvet_and_front(body):
    """Velvet: the red region in the body's top third. Front: everything under the velvet in each column (the front
    rim and panel), and under the first row below the velvet where gold spans 20% of the width elsewhere."""
    c = classes(body.rgba)
    h, w = c["opaque"].shape
    labels, count = ndimage.label(ndimage.binary_closing(c["red"], iterations=2))
    velvet = np.zeros_like(c["opaque"])
    for index in range(1, count + 1):
        region = labels == index
        ys = np.nonzero(region)[0]
        if ys.mean() < h * .35 and region.sum() > 200:
            velvet |= region
    velvet = ndimage.binary_fill_holes(velvet)
    rows = np.nonzero(velvet.any(1))[0]
    rim = next(y for y in range(int(rows.max()), h) if c["gold"][y].sum() >= .2 * w)
    below = np.zeros_like(velvet)
    for x in range(w):
        ys = np.nonzero(velvet[:, x])[0]
        below[(ys.max() + 1) if len(ys) else rim:, x] = True
    return velvet, c["opaque"] & below & ~velvet, rim


def place_lid(body, lid):
    """Lid top-left on the body (source px): centred across the body's front rim (the lid's widest row over the rim
    row's extent), then at the height leaving the fewest velvet px open plus front px covered (FFT correlation), then
    lowest."""
    velvet, front, rim = velvet_and_front(body)
    lid_mask = (lid.rgba[..., 3] > 128).astype(float)
    lh, lw = lid_mask.shape
    pad = max(lh, lw) + 8

    def correlate(mask):
        return np.rint(signal.correlate(np.pad(mask.astype(float), pad), lid_mask, mode="valid", method="fft")).astype(int)

    rim_xs = np.nonzero(body.rgba[rim, :, 3] > 128)[0]
    widths = (lid_mask > 0).sum(1)
    band = int(np.argmax(widths))
    band_xs = np.nonzero(lid_mask[band] > 0)[0]
    dx = round((rim_xs.min() + rim_xs.max() + 1) / 2 - (band_xs.min() + band_xs.max() + 1) / 2)
    total = int(velvet.sum())
    cost = (total - correlate(velvet)) + correlate(front)
    j = dx + pad
    column = cost[:, j]
    i = int(np.nonzero(column <= column.min())[0].max())
    dy = i - pad
    open_px = total - int(correlate(velvet)[i, j])
    rows = np.nonzero(velvet.any(1))[0]
    return (dx, dy), {"velvet_rows": [int(rows.min()), int(rows.max())], "rim_row": int(rim), "velvet_px": total,
                      "rim_extent_source_px": [int(rim_xs.min()), int(rim_xs.max()) + 1],
                      "lid_band_extent_source_px": [int(band_xs.min()), int(band_xs.max()) + 1],
                      "velvet_left_open_px": open_px, "front_covered_px": int(cost[i, j]) - open_px, "velvet": velvet}


def ring_recess(body):
    """The seal's ring recess: the gold ring on the front panel (gold grown by 3 px encloses a hole), the hole's centre."""
    c = classes(body.rgba)
    labels, count = ndimage.label(ndimage.binary_dilation(c["gold"], iterations=3), structure=np.ones((3, 3)))
    best = None
    for index in range(1, count + 1):
        ring = labels == index
        filled = ndimage.binary_fill_holes(ring)
        hole = int(filled.sum() - ring.sum())
        if hole > 200 and (best is None or hole > best[0]):
            best = (hole, filled & ~ring)
    if best is None:
        raise ValueError("no ring recess on the body")
    ys, xs = np.nonzero(best[1])
    return float(xs.mean()) + .5, float(ys.mean()) + .5


# The seal's cells: on the parts' lattice the seal is 10 cells across, where its 17 px bar pitch is 1.6 cells and no
# phase keeps the three bars apart (they join into a letter-like glyph); 11 is the fewest cells that keep them.
SEAL_CELLS = 11


def reliquary(parts, icon_width):
    body, lid, seal = parts
    (dx, dy), fit = place_lid(body, lid)
    velvet = fit.pop("velvet")
    # frame: the closed casket (body at (0,0), lid at (dx,dy)), in body-local source px, shifted to start at (0,0)
    x0, y0 = min(0, dx), min(0, dy)
    width = max(body.image.width, dx + lid.image.width) - x0
    cell = width / icon_width
    (body_x, body_cells), (lid_x, lid_cells) = lattice_resample([body, lid], [(-x0, -y0), (dx - x0, dy - y0)], cell, icon_width)
    seal_x = resample(seal, None, None, size=(SEAL_CELLS, SEAL_CELLS))
    lid_offset = [lid_cells[0] - body_cells[0], lid_cells[1] - body_cells[1]]

    def body_tex(px, py):  # body-local source px -> body texture
        return body_x.tex(px + body.box[0], py + body.box[1])
    rx, ry = ring_recess(body)
    recess = body_tex(rx, ry)
    vy, vx = np.nonzero(velvet)
    mouth = body_tex(float(vx.mean()) + .5, float(vy.mean()) + .5)
    sw, sh = seal_x.image.size
    seal_offset = [round(recess[0] - sw / 2), round(recess[1] - sh / 2)]
    hinge = lid_hinge(lid_x.image, body_x.image, lid_offset)
    closed = closed_fit(body_x.image, lid_x.image, lid_offset, hinge)
    common = {"lid_offset": lid_offset, "lid_placement_source_px": [dx, dy], **fit, "lattice_source_px": round(cell, 3),
              "closed_width_logical": icon_width}
    body_x.info.update(common, mouth=[round(mouth[0], 2), round(mouth[1], 2)], seal_recess=[round(recess[0], 2), round(recess[1], 2)],
                       seal_offset=seal_offset, closed=closed)
    lid_x.info.update(common, hinge=hinge, closed=closed,
                      hinge_rule="the left-most corner of the lid's bottom edge that rests on the closed body (a corner of a body texel)")
    seal_x.info.update(seal_offset_on_body=seal_offset, seal_rule="centred on the body's ring recess, rounded to whole texels",
                       seal_cells_rule=f"{SEAL_CELLS} cells: the fewest that keep the three bars apart (10, the lattice's, joins them)")
    return body_x, lid_x, seal_x


def lid_hinge(lid, body, offset):
    """The lid's rear hinge (lid texels): the left-most corner on the lid's bottom edge that rests on the closed body (a
    corner of one of the body's texels), so the lid turns about a point of both drawings."""
    l = np.asarray(lid)[..., 3] > 0
    b = np.asarray(body)[..., 3] > 0

    def on_body(x, y):
        return any(0 <= y + j < b.shape[0] and 0 <= x + i < b.shape[1] and b[y + j, x + i] for i in (-1, 0) for j in (-1, 0))
    corners = []
    for y, x in zip(*np.nonzero(l)):
        if y + 1 == l.shape[0] or not l[y + 1, x]:
            corners += [(int(x), int(y) + 1), (int(x) + 1, int(y) + 1)]
    resting = [c for c in corners if on_body(c[0] + offset[0], c[1] + offset[1])]
    if not resting:
        raise ValueError("the closed lid does not rest on the body")
    return list(min(resting, key=lambda c: (c[0], -c[1])))


def closed_fit(body, lid, offset, hinge):
    """How the closed lid sits on the body (texels): the lid's widest row against the body's widest row under the lid,
    and the hinge's distance to the nearest body texel."""
    b = np.asarray(body)[..., 3] > 0
    l = np.asarray(lid)[..., 3] > 0
    band = np.nonzero(l[int(np.argmax(l.sum(1)))])[0] + offset[0]
    under = [y for y in range(max(0, offset[1]), min(b.shape[0], offset[1] + l.shape[0])) if b[y].any()]
    row = max(under, key=lambda y: (int(np.ptp(np.nonzero(b[y])[0])), y))
    xs = np.nonzero(b[row])[0]
    ys, bx = np.nonzero(b)
    hx, hy = hinge[0] + offset[0], hinge[1] + offset[1]
    gap = float(np.min(np.hypot(np.clip(hx, bx, bx + 1) - hx, np.clip(hy, ys, ys + 1) - hy)))
    return {"lid_widest_row": [int(band.min()), int(band.max()) + 1], "body_row_under_lid": [int(row), int(xs.min()), int(xs.max()) + 1],
            "overhang_left": int(xs.min() - band.min()), "overhang_right": int(band.max() - xs.max()), "hinge_gap_to_body": round(gap, 2)}


# ---- weapon anchors (exported texture space) ------------------------------------------------------------------

def line_fit(mask):
    ys, xs = np.nonzero(mask)
    pts = np.stack([xs + .5, ys + .5], 1)
    mean = pts.mean(0)
    _, _, vt = np.linalg.svd(pts - mean)
    d = vt[0]
    if d[0] < 0:
        d = -d
    return mean, d


def scythe_anchors(image):
    rgba = np.asarray(image)
    c = classes(rgba)
    h, w = c["opaque"].shape
    yy, xx = np.mgrid[0:h, 0:w]
    # the haft: dark texels in the lower-left half, as one straight line
    haft = c["dark"] & (xx < w * .5) & (yy > h * .4)
    mean, d = line_fit(haft)
    # the butt: the opaque texel farthest down the haft line; the cap: gold texels along it from the butt
    pts = np.stack([xx + .5, yy + .5], -1)
    along = (pts - mean) @ d
    across = np.abs((pts - mean) @ np.array([-d[1], d[0]]))
    on_axis = c["opaque"] & (across <= 2.5)
    butt = float(along[on_axis].min())
    # the cap: the 4-connected gold piece nearest the butt (the haft's dashed highlight dots are separate pieces)
    labels, _ = ndimage.label(c["gold"])
    near = c["gold"] & (across <= 3) & (along <= butt + 4)
    cap = labels == labels[near][np.argmin(along[near])] if near.any() else np.zeros_like(near)
    cap_end = float(along[cap].max()) if cap.any() else butt
    grip_s = cap_end + 1.5
    grip = mean + d * grip_s
    # the hook tip: the lowest opaque texel in the right fifth (the end of the downswept blade)
    right = c["opaque"] & (xx >= w * .8)
    ty = int(yy[right].max())
    tx = float(xx[right & (yy == ty)].mean())
    tip = np.array([tx + .5, ty + .5])
    # the blade: bone and red (cutting edge) texels, the largest 8-connected piece after a 1-texel close
    blade = largest(ndimage.binary_closing(c["bone"] | c["red"], structure=np.ones((3, 3))) & c["opaque"])
    head = mean + d * float(along[c["dark"] & (across <= 2.5)].max())  # top end of the haft
    by, bx = np.nonzero(blade)
    root_i = int(np.argmin((bx + .5 - head[0]) ** 2 + (by + .5 - head[1]) ** 2))
    tip_i = int(np.argmin((bx + .5 - tip[0]) ** 2 + (by + .5 - tip[1]) ** 2))
    dist = geodesic(blade, (by[root_i], bx[root_i]))
    total = dist[by[tip_i], bx[tip_i]]
    knots = []
    for k in range(3):
        target = total * k / 3
        band = blade & (np.abs(dist - target) <= max(1.5, total / 12))
        knots.append(np.array(centroid(band)))
    knots.append(tip)
    span = tip - grip
    length = float(np.hypot(*span))
    tilt = math.atan2(span[1], span[0])
    cs, sn = math.cos(-tilt), math.sin(-tilt)
    frame = [[round((cs * (k[0] - grip[0]) - sn * (k[1] - grip[1])) / length, 3),
              round((sn * (k[0] - grip[0]) + cs * (k[1] - grip[1])) / length, 3)] for k in knots]
    frame[-1] = [1.0, 0.0]
    return {"grip": [round(float(grip[0]), 2), round(float(grip[1]), 2)], "butt_cap_texels": int(cap.sum()), "hook_tip": [round(float(tip[0]), 2), round(float(tip[1]), 2)],
            "reach_px": round(length * PIXEL_SCALE, 2), "blade_knots_texture": [[round(float(k[0]), 2), round(float(k[1]), 2)] for k in knots],
            "blade_knots_tip_frame": frame, "haft_axis": [round(float(d[0]), 3), round(float(d[1]), 3)],
            "grip_rule": "haft line (dark texels, lower-left half) 1.5 texels past the gold butt cap (4-connected gold piece at the butt)",
            "tip_rule": "lowest opaque texel in the right fifth", "knot_rule": "blade (bone + red) geodesic thirds from the texel nearest the haft head"}


def geodesic(mask, start):
    """Dijkstra distance (8-connected, diagonal sqrt 2) from `start` inside `mask`."""
    dist = np.full(mask.shape, np.inf)
    dist[start] = 0
    steps = [(-1, 0, 1), (1, 0, 1), (0, -1, 1), (0, 1, 1), (-1, -1, 1.4142), (-1, 1, 1.4142), (1, -1, 1.4142), (1, 1, 1.4142)]
    heap = [(0.0, start)]
    while heap:
        d0, (y, x) = heapq.heappop(heap)
        if d0 > dist[y, x]:
            continue
        for dy, dx, cost in steps:
            ny, nx = y + dy, x + dx
            if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny, nx] and d0 + cost < dist[ny, nx]:
                dist[ny, nx] = d0 + cost
                heapq.heappush(heap, (d0 + cost, (ny, nx)))
    return dist


def organ_anchors(image):
    rgba = np.asarray(image)
    c = classes(rgba)
    h, w = c["opaque"].shape
    yy, xx = np.mgrid[0:h, 0:w]
    # pipes: bone runs down the column at 85% of the length; each mouth is that pipe's right edge, mid-row
    col = int(w * .85)
    bone = c["bone"][:, col]
    runs = np.flatnonzero(np.diff(np.concatenate([[0], bone.astype(int), [0]])))
    pipes = [(int(a), int(b)) for a, b in zip(runs[0::2], runs[1::2])]
    mouths = []
    for a, b in pipes:
        rows = c["opaque"][a:b]
        right = int(np.nonzero(rows.any(0))[0].max()) + 1
        mouths.append([float(right), round((a + b) / 2, 2)])
    gem = centroid(largest(c["red"] & (xx > w * .2) & (xx < w * .6)))
    handle = c["opaque"] & (xx < w * .3) & (yy > h * .55)
    return {"grip": centroid(handle), "gem": gem, "mouths": mouths, "pipe_rows": pipes,
            "grip_rule": "centroid of the handle (opaque, left 30%, lower 45%)", "mouth_rule": "bone runs at 85% length; right edge, mid-row",
            "gem_rule": "largest red piece in the frame (20-60% of the length)"}


def hand_anchors(image):
    c = classes(np.asarray(image))
    h, w = c["opaque"].shape
    rows = np.nonzero(c["bone"].any(1))[0]
    top, bottom = int(rows.min()), int(rows.max())
    palm = c["bone"].copy()
    palm[:top] = False
    palm[int(top + .35 * (bottom - top)) + 1:] = False
    return {"palm": centroid(palm), "bone_rows": [top, bottom], "palm_rule": "bone texels in the top 35% of the bone (under the cuff)"}


def baton_anchors(image):
    c = classes(np.asarray(image))
    h, w = c["opaque"].shape
    yy, xx = np.mgrid[0:h, 0:w]
    grip = centroid(largest(c["gold"] & (xx < w * .4) & (yy > h * .6)))
    gem = centroid(largest(c["red"] & (xx > w * .6) & (yy < h * .4)))
    return {"grip": grip, "gem": gem, "grip_rule": "largest gold piece in the lower-left (the teardrop grip)",
            "gem_rule": "largest red piece in the upper-right"}


def censer_anchors(image, part, export):
    c = classes(np.asarray(image))
    h, w = c["opaque"].shape
    yy, xx = np.mgrid[0:h, 0:w]
    ring = enclosed_hole(c["opaque"] & (yy < h * .25))
    ring_rule = "the enclosed hole of the top ring"
    if ring is None:  # the hole closed in the resample: measure it in the delivery and map it
        src = enclosed_hole(part.rgba[..., 3] > 128)
        ring = [round(v, 2) for v in export.tex(src[0] - .5 + part.box[0], src[1] - .5 + part.box[1])]
        ring_rule = "the top ring's hole measured in the delivery (closed in the export)"
    # the rim: the first row in the middle third where saturated gold (blue <= 90) spans a third of the width; the bone
    # thorns' light-tan shading also passes the general gold test and would put the rim among the thorns
    rim_gold = c["gold"] & (np.asarray(image)[..., 2] <= 90)
    band = next(y for y in range(int(h * .3), int(h * .75)) if rim_gold[y].sum() >= w / 3)
    mouth = [ring[0], float(band)]  # on the rim, straight under the ring the censer hangs from
    drapes = []
    for side in (xx < w * .3, xx >= w * .7):
        red = c["red"] & side
        ys, xs = np.nonzero(red)
        low = ys == ys.max()
        drapes.append([round(float(xs[low].mean()) + .5, 2), float(ys.max() + 1)])
    return {"ring": ring, "mouth": mouth, "drape_left": drapes[0], "drape_right": drapes[1],
            "bowl_drop_px": round(float(np.hypot(mouth[0] - ring[0], mouth[1] - ring[1])) * PIXEL_SCALE, 2),
            "ring_rule": ring_rule, "mouth_rule": "on the top of the gold rim band (first row in the middle third with saturated gold, blue <= 90, over a third of the width), under the ring",
            "drape_rule": "lowest red texels in the outer 30% each side"}


def without_detached(image):
    """Keep the largest 8-connected piece; report the dropped texels (the quill's ink bead Codex drew off the nib)."""
    rgba = np.asarray(image).copy()
    alpha = rgba[..., 3] > 0
    keep = largest(alpha)
    dropped = alpha & ~keep
    ys, xs = np.nonzero(dropped)
    rgba[dropped] = 0
    trimmed, _ = trim_offset(Image.fromarray(rgba))
    return trimmed, {"detached_texels_dropped": int(dropped.sum()),
                     "detached_box": [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1] if len(xs) else None}


def quill_anchors(image):
    alpha = np.asarray(image)[..., 3] > 0
    ys, xs = np.nonzero(alpha)
    right = xs == xs.max()
    return {"nib": [float(xs.max() + 1), round(float(ys[right].mean()) + .5, 2)], "nib_rule": "right edge of the right-most texels (the nib tip)"}


def score_anchors(image):
    c = classes(np.asarray(image))
    return {"seal": centroid(largest(c["red"])), "seal_rule": "largest red piece (the wax seal)"}


def buff(image):
    return double(centred_on(image, (BUFF_CANVAS, BUFF_CANVAS)))


def centred_on(image, size):
    canvas = Image.new("RGBA", size)
    canvas.alpha_composite(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return canvas


# ---- review images (local) ------------------------------------------------------------------------------------

def screen_size(name, image, notes):
    """How large the texture is on screen at zoom 1: icons as drawn by tML (1 px per texel), bodies at PixelScale."""
    return image if notes[name]["scale"] != "1x" else image.resize((image.width * PIXEL_SCALE, image.height * PIXEL_SCALE), Image.NEAREST)


def sanctum_patch(size, seed=0):
    art = Image.open(SANCTUM).convert("RGBA")
    scale = max(1920 / art.width, 1080 / art.height) * 1.10  # CrimsonSky: the painting covers the screen at 1.10x
    big = art.resize((round(art.width * scale), round(art.height * scale)), Image.BILINEAR)
    x = (big.width - size[0]) // 2 + (seed * 137) % max(1, big.width - size[0]) // 3
    y = (big.height - size[1]) // 2
    return big.crop((x, y, x + size[0], y + size[1]))


def contact_sheet(outputs, notes, path):
    font = ImageFont.load_default(size=16)
    grounds = list(BACKDROPS.items())
    tiles = []
    for name, image in outputs.items():
        real = screen_size(name, image, notes)
        for zoom in (1, 2):
            shown = real.resize((real.width * zoom, real.height * zoom), Image.NEAREST)
            pad = 10
            cols = len(grounds) + 1
            tile = Image.new("RGB", (cols * (shown.width + pad) + pad, shown.height + 2 * pad + 22), (28, 24, 26))
            for i in range(cols):
                ground = sanctum_patch((shown.width + 8, shown.height + 8), i).convert("RGBA") if i == cols - 1 else \
                    Image.new("RGBA", (shown.width + 8, shown.height + 8), grounds[i][1] + (255,))
                ground.alpha_composite(shown, (4, 4))
                tile.paste(ground.convert("RGB"), (pad + i * (shown.width + pad) - 4, 22 + pad - 4))
            ImageDraw.Draw(tile).text((pad, 4), f"{name} {image.width}x{image.height} ({notes[name]['scale']}) screen x{zoom}",
                                      fill=(236, 230, 220), font=font)
            tiles.append(tile)
    width, rows, row, x = 2400, [], [], 0
    for tile in tiles:
        if row and x + tile.width > width:
            rows.append(row)
            row, x = [], 0
        row.append(tile)
        x += tile.width + 6
    rows.append(row)
    height = sum(max(t.height for t in r) + 6 for r in rows) + 30
    sheet = Image.new("RGB", (width, height), (18, 14, 16))
    ImageDraw.Draw(sheet).text((8, 6), "Scarlet reward art at real screen size (x1) and x2; grounds: " + ", ".join(BACKDROPS) + ", sanctum painting",
                               fill=(236, 230, 220), font=font)
    y = 30
    for r in rows:
        x = 0
        for tile in r:
            sheet.paste(tile, (x, y))
            x += tile.width + 6
        y += max(t.height for t in r) + 6
    sheet.save(path, optimize=True)


def blit(canvas, texture, position, rotation=0.0, origin=(0, 0), scale=(PIXEL_SCALE, PIXEL_SCALE), flip_x=False, flip_y=False):
    """SpriteBatch.Draw on a PIL canvas with point sampling: drawn point `local` lands at position + R(rotation)(scale
    (local - origin)) and shows the texel mirrored inside the texture for a flip (the origin stays in drawn space)."""
    tex = np.asarray(texture.convert("RGBA"))
    th, tw = tex.shape[:2]
    c, s = math.cos(rotation), math.sin(rotation)
    corners = [(x, y) for x in (0, tw) for y in (0, th)]
    world = [(position[0] + c * scale[0] * (x - origin[0]) - s * scale[1] * (y - origin[1]),
              position[1] + s * scale[0] * (x - origin[0]) + c * scale[1] * (y - origin[1])) for x, y in corners]
    x0, x1 = max(0, int(min(p[0] for p in world)) - 1), min(canvas.width, int(max(p[0] for p in world)) + 2)
    y0, y1 = max(0, int(min(p[1] for p in world)) - 1), min(canvas.height, int(max(p[1] for p in world)) + 2)
    if x1 <= x0 or y1 <= y0:
        return
    yy, xx = np.mgrid[y0:y1, x0:x1]
    dx, dy = xx + .5 - position[0], yy + .5 - position[1]
    lx = (c * dx + s * dy) / scale[0] + origin[0]
    ly = (-s * dx + c * dy) / scale[1] + origin[1]
    u = np.floor(tw - lx if flip_x else lx).astype(int)
    v = np.floor(th - ly if flip_y else ly).astype(int)
    inside = (lx >= 0) & (lx < tw) & (ly >= 0) & (ly < th) & (u >= 0) & (u < tw) & (v >= 0) & (v < th)
    layer = np.zeros((y1 - y0, x1 - x0, 4), np.uint8)
    layer[inside] = tex[v[inside], u[inside]]
    patch = canvas.crop((x0, y0, x1, y1))
    patch.alpha_composite(Image.fromarray(layer))
    canvas.paste(patch, (x0, y0))


def stand_in(canvas, centre, facing=1):
    """A 20x42 stand-in player (the hitbox) with a head, so the weapons can be judged against it."""
    d = ImageDraw.Draw(canvas)
    x, y = round(centre[0] - 10), round(centre[1] - 21)
    d.rectangle((x, y + 12, x + 19, y + 41), fill=(70, 84, 120, 255), outline=(20, 22, 34, 255))
    d.rectangle((x + 3, y, x + 16, y + 13), fill=(222, 178, 140, 255), outline=(40, 26, 20, 255))
    eye = x + (12 if facing > 0 else 6)
    d.rectangle((eye, y + 5, eye + 1, y + 7), fill=(20, 20, 30, 255))


def dummy(canvas, centre, size=(40, 52)):
    d = ImageDraw.Draw(canvas)
    d.rectangle((centre[0] - size[0] // 2, centre[1] - size[1] // 2, centre[0] + size[0] // 2, centre[1] + size[1] // 2),
                fill=(96, 96, 112, 255), outline=(210, 210, 230, 255))


def mark(canvas, point, colour=(80, 255, 240, 255)):
    d = ImageDraw.Draw(canvas)
    x, y = round(point[0]), round(point[1])
    d.line((x - 3, y, x + 3, y), fill=colour)
    d.line((x, y - 3, x, y + 3), fill=colour)


def hand_at(centre, angle):
    """Rough front hand: shoulder 6 px above the centre, a 12 px arm toward `angle`."""
    return (centre[0] + 12 * math.cos(angle), centre[1] - 6 + 12 * math.sin(angle))


def anchor_sheet(outputs, notes, path):
    """Every world body at 8x with its measured anchors marked."""
    font = ImageFont.load_default(size=14)
    k, tiles = 8, []
    for name, image in outputs.items():
        if notes[name]["scale"] != "1x":
            continue
        n = notes[name]
        big = Image.new("RGBA", (image.width * k + 16, image.height * k + 34), (40, 22, 20, 255))
        big.alpha_composite(image.resize((image.width * k, image.height * k), Image.NEAREST), (8, 26))
        d = ImageDraw.Draw(big)
        d.text((8, 6), name, fill=(240, 236, 226, 255), font=font)
        points = [n[key] for key in ("grip", "hook_tip", "gem", "palm", "ring", "mouth", "drape_left", "drape_right", "nib", "seal",
                                      "hinge", "seal_recess") if n.get(key)]
        points += n.get("mouths", []) + n.get("blade_knots_texture", [])[:-1]
        for x, y in points:
            px, py = 8 + x * k, 26 + y * k
            d.line((px - 6, py, px + 6, py), fill=(80, 255, 240, 255), width=2)
            d.line((px, py - 6, px, py + 6), fill=(80, 255, 240, 255), width=2)
        tiles.append(big)
    rows = [tiles[i:i + 4] for i in range(0, len(tiles), 4)]
    sheet = Image.new("RGBA", (max(sum(t.width + 8 for t in r) for r in rows), sum(max(t.height for t in r) + 8 for r in rows)), (18, 14, 16, 255))
    y = 0
    for r in rows:
        x = 0
        for t in r:
            sheet.alpha_composite(t, (x, y))
            x += t.width + 8
        y += max(t.height for t in r) + 8
    sheet.convert("RGB").save(path, optimize=True)


def mockups(outputs, notes, path):
    """Held and world poses at real screen size beside a 20x42 player, placed the way the client code places them."""
    T = {name[:-4]: image for name, image in outputs.items()}
    N = {name[:-4]: n for name, n in notes.items()}
    W, H = 1360, 540
    canvas = sanctum_patch((W, H), 3)
    d = ImageDraw.Draw(canvas)
    font = ImageFont.load_default(size=12)
    ground, ground2 = 250, 520

    def label(x, y, text):
        d.text((x, y), text, fill=(250, 240, 220, 255), font=font)

    # scythe: grip in the hand, its grip -> hook-tip line turned to three directions (no roll)
    n = N["SableScythe"]
    grip, tip = n["grip"], n["hook_tip"]
    tilt = math.atan2(tip[1] - grip[1], tip[0] - grip[0])
    label(10, 6, "Sable Scythe: grip in hand; tip at -70 / 0 / +50 deg")
    for i, angle in enumerate((-1.22, 0.0, .87)):
        centre = (60 + i * 110, ground - 21)
        stand_in(canvas, centre)
        blit(canvas, T["SableScythe"], hand_at(centre, angle), angle - tilt, grip)
    # organ: aimed right, up-right and left (flipped vertically), a shard leaving the top mouth
    n = N["CanticleOrgan"]
    label(360, 6, "Canticle Organ: 0 / -35 deg / left (flipped)")
    for i, angle in enumerate((0.0, -.61, math.pi)):
        flip = math.cos(angle) < 0
        centre = (390 + i * 115, ground - 21)
        stand_in(canvas, centre, -1 if flip else 1)
        hand = hand_at(centre, angle)
        origin = (n["grip"][0], T["CanticleOrgan"].height - n["grip"][1]) if flip else n["grip"]
        blit(canvas, T["CanticleOrgan"], hand, angle, origin, flip_y=flip)
        if i == 0:
            m = n["mouths"][0]
            blit(canvas, T["CanticleShard"], (hand[0] + (m[0] - n["grip"][0]) * 2 + 16, hand[1] + (m[1] - n["grip"][1]) * 2), 0,
                 (T["CanticleShard"].width / 2, T["CanticleShard"].height / 2))
    # baton: grip in the hand, its grip -> gem line along the arm
    n = N["ScarletBaton"]
    axis = math.atan2(n["gem"][1] - n["grip"][1], n["gem"][0] - n["grip"][0])
    label(740, 6, "Scarlet Baton: -60 / -10 deg / left")
    for i, angle in enumerate((-1.05, -.17, math.pi + .3)):
        centre = (770 + i * 95, ground - 21)
        left = math.cos(angle) < 0
        stand_in(canvas, centre, -1 if left else 1)
        origin = (n["grip"][0], T["ScarletBaton"].height - n["grip"][1]) if left else n["grip"]
        blit(canvas, T["ScarletBaton"], hand_at(centre, angle), angle + axis if left else angle - axis, origin, flip_y=left)
    # reliquary: closed 70 px above the head, and opening (lid at -40 and -110 deg on its hinge, the seal's halves falling)
    body, lid, seal = T["ReliquaryBody"], T["ReliquaryLid"], T["ReliquarySeal"]
    nb = N["ReliquaryBody"]
    lo, so, hinge = nb["lid_offset"], nb["seal_offset"], N["ReliquaryLid"]["hinge"]
    fx0, fy0 = min(0, lo[0]), min(0, lo[1])
    fw, fh = max(body.width, lo[0] + lid.width) - fx0, max(body.height, lo[1] + lid.height) - fy0
    label(1040, 6, "Reliquary: closed / lid 40 / lid 110 deg (+ = hinge)")
    for i, degrees in enumerate((0, 40, 110)):
        opened = degrees > 0
        centre = (1075 + i * 105, ground - 21)
        stand_in(canvas, centre)
        mid = (centre[0], centre[1] - 21 - 70)
        top_left = (mid[0] - fw * PIXEL_SCALE / 2 - fx0 * PIXEL_SCALE, mid[1] - fh * PIXEL_SCALE / 2 - fy0 * PIXEL_SCALE)  # body (0,0)
        blit(canvas, body, top_left)
        pivot = (top_left[0] + (lo[0] + hinge[0]) * PIXEL_SCALE, top_left[1] + (lo[1] + hinge[1]) * PIXEL_SCALE)
        blit(canvas, lid, pivot, -math.radians(degrees), hinge)
        mark(canvas, pivot)
        if not opened:
            blit(canvas, seal, (top_left[0] + so[0] * PIXEL_SCALE, top_left[1] + so[1] * PIXEL_SCALE))
            continue
        for side in (-1, 1):
            half = seal.crop((0, 0, seal.width // 2, seal.height) if side < 0 else (seal.width // 2, 0, seal.width, seal.height))
            at = (top_left[0] + (so[0] + seal.width / 2) * PIXEL_SCALE + side * 9, top_left[1] + (so[1] + seal.height / 2) * PIXEL_SCALE + 6)
            blit(canvas, half, at, side * .5, (half.width / 2, half.height / 2))
        mark(canvas, (top_left[0] + nb["mouth"][0] * 2, top_left[1] + nb["mouth"][1] * 2))
    # quill: thrown right and left (flipped vertically, nib first), stuck nib-first in a target; the sealed score
    n = N["BloodinkQuill"]
    label(10, 276, "Bloodink Quill: thrown right / left, stuck nib-first; sealed score")
    stand_in(canvas, (40, ground2 - 21))
    blit(canvas, T["BloodinkQuill"], (160, 420), -.15, n["nib"])
    blit(canvas, T["BloodinkQuill"], (110, 350), math.pi + .2, (n["nib"][0], T["BloodinkQuill"].height - n["nib"][1]), flip_y=True)
    dummy(canvas, (270, ground2 - 26))
    blit(canvas, T["BloodinkQuill"], (262, ground2 - 34), .35, n["nib"])
    # the sealed score turned right and left (flipped vertically, as QuillVisuals draws it); + = the seal anchor, mirrored
    # with the drawing (QuillArt.Body.Point)
    score, seal_at = T["SealedScore"], N["SealedScore"]["seal"]
    centre_of = (score.width / 2, score.height / 2)
    for at, rotation in (((135, 470), 0.0), ((215, 445), math.pi - .4)):
        flip = math.cos(rotation) < 0
        blit(canvas, score, at, rotation, centre_of, flip_y=flip)
        local = (seal_at[0] - centre_of[0], (centre_of[1] - seal_at[1]) if flip else (seal_at[1] - centre_of[1]))
        c, s_ = math.cos(rotation), math.sin(rotation)
        mark(canvas, (at[0] + PIXEL_SCALE * (c * local[0] - s_ * local[1]), at[1] + PIXEL_SCALE * (s_ * local[0] + c * local[1])))
    # bone hand: palm on the target's centre; the mirrored hand still falling
    n = N["BoneHand"]
    label(360, 276, "Bone hand: palm on target / mirrored, falling")
    dummy(canvas, (410, ground2 - 26))
    blit(canvas, T["BoneHand"], (410, ground2 - 26), 0, n["palm"])
    dummy(canvas, (530, ground2 - 20), (24, 40))
    blit(canvas, T["BoneHand"], (530, ground2 - 20 - 70), 0, (T["BoneHand"].width - n["palm"][0], n["palm"][1]), flip_x=True)
    # censer: the sprite turns about its mouth, BowlDrop under the ring along the pendulum (the code's placement);
    # idle at slot 0 (40 px behind, 70 px up) at rest and at the 55 deg apex, then tipped 35 deg into a pour
    n = N["EmberCenser"]
    drop, mouth = n["bowl_drop_px"], n["mouth"]
    label(640, 276, f"Ember Censer: idle slot 0 at rest / 55 deg apex / pouring (ring->mouth {drop} px; + = pivot)")
    for i, angle in enumerate((0.0, math.radians(55))):
        centre = (720 + i * 150, ground2 - 21)
        stand_in(canvas, centre)
        pivot = (centre[0] - 40, centre[1] - 70)
        blit(canvas, T["EmberCenser"], (pivot[0] + drop * math.sin(angle), pivot[1] + drop * math.cos(angle)), -angle, mouth)
        mark(canvas, pivot)
    dummy(canvas, (1120, ground2 - 26), (60, 52))
    pivot = (1100, ground2 - 52 - 120)
    angle, turn = math.radians(40), math.radians(35)
    blit(canvas, T["EmberCenser"], (pivot[0] + drop * math.sin(angle), pivot[1] + drop * math.cos(angle)), -(angle + turn), mouth)
    mark(canvas, pivot)
    d.line((0, ground, W, ground), fill=(30, 18, 14, 255))
    d.line((0, ground2, W, ground2), fill=(30, 18, 14, 255))
    canvas.convert("RGB").save(path, optimize=True)
    canvas.resize((W * 2, H * 2), Image.NEAREST).convert("RGB").save(path.with_name(path.stem + "-x2.png"), optimize=True)


KINDS = {
    "CrimsonScoreReliquary": "Scarlet Score Reliquary item icon", "ReliquaryBody": "reliquary body for the opening show",
    "ReliquaryLid": "reliquary lid for the opening show", "ReliquarySeal": "reliquary wax seal for the opening show",
    "CrimsonSableScythe": "Sable Scythe item icon", "SableScythe": "held Sable Scythe",
    "CrimsonCanticleOrgan": "Canticle Organ item icon", "CanticleOrgan": "held Canticle Organ", "CanticleShard": "Canticle Organ bone shard",
    "BoneHand": "Hymn of Hands bone hand", "CrimsonBaton": "Scarlet Baton item icon", "ScarletBaton": "held Scarlet Baton",
    "CrimsonEmberCenser": "Ember Censer item icon", "EmberCenser": "Ember Censer minion (unlit crown censer)",
    "CrimsonEmberCenserBuff": "Ember Censer buff icon", "BloodinkQuill": "thrown Bloodink Quill",
    "CrimsonBloodinkQuill": "Bloodink Quill item icon", "SealedScore": "Sealed Score stealth projectile",
    "CrimsonPact": "Scarlet Covenant item icon", "CrimsonPactBuff": "Scarlet Covenant buff icon",
}
EDIT_INPUTS = {"SR01P_c": "SR01_a", "SR02I_c": "SR02_c", "SR04I_b": "SR04_c"}
# Why Codex did not accept each candidate (delivery REPORT.md and manifest.json notes): every one misses the strict 8 px
# grid and leaves edge residue; the recorded size, layout and shape shortfalls follow.
_GRID = "the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining"
CODEX_SHORTFALLS = {
    "SR01_a": _GRID + "; the complete casket is larger than 32x28 (fitted here), the body cell's margin is under 10%, and its own "
              "parts do not reassemble into the casket (SR01P_c's are used)",
    "SR01P_c": _GRID + "; the parts are not at SR01_a's scale and their reassembly was not checked by Codex (the exporter places them)",
    "SR02_c": _GRID,
    "SR02I_c": _GRID + "; 3.75 logical px wider than 32 at 8 px (32x28 at its measured pitch)",
    "SR03_d": _GRID + "; the gun, hand and icon are larger than their limits (fitted here), and the hand is turned three-quarter "
              "front rather than strictly side-on",
    "SR04_c": _GRID,
    "SR04I_b": _GRID,
    "SR05_d": _GRID + "; the icon, censer and buff are larger than their limits (fitted here), and the icon shows 3 of the briefed 5 thorns",
    "SR06_d": _GRID + "; the quill, bottle and score are larger than their limits (fitted here), an ink bead is drawn detached ahead of "
              "the nib (cut here), and the score's marks are more prominent than the briefed tiny faint ones",
    "SR07_b": _GRID + "; the letter and buff are larger than their limits (fitted here)",
}
# What the export could not keep (every lattice phase tried; fixing it needs a new drawing, not a repaint).
EXPORT_SHORTFALLS = {
    "CrimsonCanticleOrgan": "at 27x28 the icon's four diagonal barrels and rib frame merge into one mottled bone mass at every lattice phase",
    "CrimsonScoreReliquary": "at 32x25 the seal's three bars do not survive, so it reads as a plain round seal",
}


def attribution(report, path):
    """The Assets/ATTRIBUTION.md section for every exported texture, from the report."""
    versions = f"Pillow {Image.__version__}, NumPy {np.__version__} and SciPy {__import__('scipy').__version__}"
    lines = []
    for name, out in report["outputs"].items():
        stem = name[:-4]
        sheet = "alpha/" + out["source"].split(" ")[0] + ".png"
        pitch = report["sheets"][sheet]["dot_px"]
        w, h = out["size"]
        lw, lh = out["logical"]
        scale = out["scale"]
        if scale == "1x":
            kind = f"{w}x{h} {KINDS[stem]}, one texel per logical pixel (drawn at 2x with point sampling)"
        elif scale.startswith("buff"):
            kind = f"{w}x{h} {KINDS[stem]} ({lw}x{lh} logical centred on 16x16, each logical pixel 2x2)"
        else:
            kind = f"{w}x{h} {KINDS[stem]} ({lw}x{lh} logical, each logical pixel 2x2)"
        candidate = out["source"].split(" ")[0]
        steps = [f"measured dot pitch {pitch} px"]
        resample = "by majority colour (48-colour median cut of the object, opaque at half coverage, no dither)"
        phase = f"at the lattice phase (+{out['lattice_phase_px'][0]}, +{out['lattice_phase_px'][1]}) source px, the one whose cells disagree with the fewest of its px"
        if "lattice_source_px" in out:
            steps.append(f"resampled with the other reliquary part on one lattice of {out['lattice_source_px']} source px per logical pixel "
                         f"(the closed casket as wide as the icon) {resample}, {phase} (searched for both parts together)")
        elif stem == "ReliquarySeal":
            steps.append(f"resampled to {lw}x{lh} cells, one more than the reliquary lattice's {round(max(out['source_box'][2] - out['source_box'][0], out['source_box'][3] - out['source_box'][1]) / report['outputs']['ReliquaryBody.png']['lattice_source_px'])} "
                         f"(the fewest that keep its three bars apart), {resample}, {phase}")
        elif "fitted_to" in out:
            limit = LIMITS[stem]
            steps.append(f"{out['logical_at_pitch'][0]}x{out['logical_at_pitch'][1]} logical at that pitch exceeds the brief's {limit[0]}x{limit[1]}, "
                         f"so it was fitted to {out['fitted_to'][0]}x{out['fitted_to'][1]} cells {resample}, {phase}")
        else:
            steps.append(f"resampled to round(bbox / pitch) = {out['logical_at_pitch'][0]}x{out['logical_at_pitch'][1]} logical cells {resample}, {phase}")
        nominal = out.get("fitted_to") or out.get("logical_at_pitch") or out.get("cells")
        if nominal and out["logical"] != nominal and not out.get("detached_texels_dropped"):
            steps.append(f"{lw}x{lh} after the empty edge cells")
        if out.get("detached_texels_dropped"):
            n = out["detached_texels_dropped"]
            steps.append(f"the {n} texel{'s' if n != 1 else ''} of the ink bead drawn detached ahead of the nib {'were' if n != 1 else 'was'} cut off (largest piece kept)")
        if scale.startswith("buff"):
            steps.append("centred on 16x16 and doubled")
        elif scale != "1x":
            steps.append("doubled")
        edit = f" Codex made it by editing its own {EDIT_INPUTS[candidate]} (no other input)." if candidate in EDIT_INPUTS else ""
        lines += [
            f"- Runtime file: `Assets/Textures/Items/ScarletRewards/{name}`",
            f"- Asset ID: scarlet-reward-art-{stem.lower()}-20261002",
            f"- Asset type: {kind}",
            "- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief",
            "- Creation/acquisition date: 2026-10-02",
            "- Source type: generated",
            "- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input",
            f"- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with {versions}",
            f"- Human modifications: Codex-recommended candidate {out['source']}.{edit} Generated on a transparent background; the alpha file is a byte copy of raw. "
            f"Mechanical export: alpha below 16 to 0, object cut out alone, {', '.join(steps)}. No repaint or recolour. Source "
            f"`asset-deliveries/scarlet-rewards/2026-10-02/{sheet}` SHA256 `{report['inputs'][sheet]}`.",
            "- License and redistribution terms: existing project original-asset terms; no third-party art license implied",
            "- Required attribution: preserve project provenance and generation disclosure",
            f"- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: {CODEX_SHORTFALLS[candidate]}); "
            f"Claude export, contact-sheet and real-size mock-up review 2026-10-03{' (' + EXPORT_SHORTFALLS[stem] + ')' if stem in EXPORT_SHORTFALLS else ''}; "
            "owner selection and in-game acceptance not_run",
            "- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; "
            "selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`",
            f"- SHA256: `{out['sha256']}`",
            "",
        ]
    path.write_text("\n".join(lines), encoding="utf-8", newline="\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", type=Path, required=True, help="asset-deliveries/scarlet-rewards/2026-10-02 (never committed)")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR)
    parser.add_argument("--preview", type=Path, default=PREVIEW_DIR, help="local contact sheet and report directory")
    parser.add_argument("--attribution-section", type=Path, help="write the Assets/ATTRIBUTION.md records here")
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

    def put(name, export, scale, image=None, **info):
        outputs[name + ".png"] = image if image is not None else export.image
        notes[name + ".png"] = {"scale": scale, **export.info, **info}
        if "fitted_to" in export.info:
            fitted, logical = export.info["fitted_to"], export.info["logical"]
            report["deviations"].append(f"{name}: {export.info['logical_at_pitch'][0]}x{export.info['logical_at_pitch'][1]} logical at the "
                                        f"measured pitch exceeds {LIMITS[name][0]}x{LIMITS[name][1]}; fitted to {fitted[0]}x{fitted[1]}"
                                        + (f" ({logical[0]}x{logical[1]} drawn)" if logical != fitted else ""))

    def body(sheet, index, name):
        parts, p = sheets[sheet]
        return resample(parts[index], p, LIMITS[name])

    # SR01 / SR01P reliquary: SR01_a's complete casket is the icon; SR01P_c's parts share one lattice
    icon = body("alpha/SR01_a.png", 0, "CrimsonScoreReliquary")
    put("CrimsonScoreReliquary", icon, "icon 2x", double(icon.image), source="SR01_a top-left (complete casket)")
    parts, _ = sheets["alpha/SR01P_c.png"]
    box, lid, seal = reliquary(parts, icon.image.width)
    put("ReliquaryBody", box, "1x", source="SR01P_c top-left (body)")
    put("ReliquaryLid", lid, "1x", source="SR01P_c top-right (lid)")
    put("ReliquarySeal", seal, "1x", source="SR01P_c bottom-left (wax seal)")
    report["deviations"].append(f"ReliquarySeal: {SEAL_CELLS}x{SEAL_CELLS} cells, one more than the parts' lattice gives, so its three bars stay apart")

    # SR02 / SR02I scythe
    held = body("alpha/SR02_c.png", 0, "SableScythe")
    put("SableScythe", held, "1x", source="SR02_c (held)", **scythe_anchors(held.image))
    icon = body("alpha/SR02I_c.png", 0, "CrimsonSableScythe")
    put("CrimsonSableScythe", icon, "icon 2x", double(icon.image), source="SR02I_c")

    # SR03 organ: gun, shard, hand, icon
    gun = body("alpha/SR03_d.png", 0, "CanticleOrgan")
    put("CanticleOrgan", gun, "1x", source="SR03_d top-left (held, facing right)", **organ_anchors(gun.image))
    shard = body("alpha/SR03_d.png", 1, "CanticleShard")
    put("CanticleShard", shard, "1x", source="SR03_d top-right (bone shard)")
    hand = body("alpha/SR03_d.png", 2, "BoneHand")
    put("BoneHand", hand, "1x", source="SR03_d bottom-left (bone hand, palm down)", **hand_anchors(hand.image))
    icon = body("alpha/SR03_d.png", 3, "CrimsonCanticleOrgan")
    put("CrimsonCanticleOrgan", icon, "icon 2x", double(icon.image), source="SR03_d bottom-right (icon)")

    # SR04 / SR04I baton
    baton = body("alpha/SR04_c.png", 0, "ScarletBaton")
    put("ScarletBaton", baton, "1x", source="SR04_c (held)", **baton_anchors(baton.image))
    icon = body("alpha/SR04I_b.png", 0, "CrimsonBaton")
    put("CrimsonBaton", icon, "icon 2x", double(icon.image), source="SR04I_b")

    # SR05 censer: icon, minion, buff
    parts, _ = sheets["alpha/SR05_d.png"]
    icon = body("alpha/SR05_d.png", 0, "CrimsonEmberCenser")
    put("CrimsonEmberCenser", icon, "icon 2x", double(icon.image), source="SR05_d left (icon)")
    minion = body("alpha/SR05_d.png", 1, "EmberCenser")
    put("EmberCenser", minion, "1x", source="SR05_d middle (unlit crown censer)", **censer_anchors(minion.image, parts[1], minion))
    mark = body("alpha/SR05_d.png", 2, "CrimsonEmberCenserBuff")
    put("CrimsonEmberCenserBuff", mark, "buff 16x16 at 2x", buff(mark.image), source="SR05_d right (buff)")

    # SR06 quill: projectile, icon, sealed score
    quill = body("alpha/SR06_d.png", 0, "BloodinkQuill")
    quill.image, cut = without_detached(quill.image)
    quill.info["logical"] = list(quill.image.size)
    put("BloodinkQuill", quill, "1x", source="SR06_d left (quill, nib right)", **cut, **quill_anchors(quill.image))
    icon = body("alpha/SR06_d.png", 1, "CrimsonBloodinkQuill")
    put("CrimsonBloodinkQuill", icon, "icon 2x", double(icon.image), source="SR06_d middle (icon)")
    score = body("alpha/SR06_d.png", 2, "SealedScore")
    put("SealedScore", score, "1x", source="SR06_d right (rolled score)", **score_anchors(score.image))

    # SR07 covenant: icon, buff
    icon = body("alpha/SR07_b.png", 0, "CrimsonPact")
    put("CrimsonPact", icon, "icon 2x", double(icon.image), source="SR07_b left (icon)")
    mark = body("alpha/SR07_b.png", 1, "CrimsonPactBuff")
    put("CrimsonPactBuff", mark, "buff 16x16 at 2x", buff(mark.image), source="SR07_b right (buff)")

    report["deviations"] += [f"{stem}: {text}" for stem, text in EXPORT_SHORTFALLS.items()]
    for name, image in outputs.items():
        target = args.output / name
        image.save(target, optimize=True)
        report["outputs"][name] = {"size": list(image.size), "sha256": sha256(target), **notes[name]}
    contact_sheet(outputs, notes, args.preview / "contact.png")
    anchor_sheet(outputs, notes, args.preview / "anchors.png")
    mockups(outputs, notes, args.preview / "mockups.png")
    (args.preview / "report.json").write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    if args.attribution_section:
        attribution(report, args.attribution_section)
    for name, entry in report["outputs"].items():
        print(f"{name}: {entry['size'][0]}x{entry['size'][1]}")
    print("\n".join(report["deviations"]))


if __name__ == "__main__":
    main()
