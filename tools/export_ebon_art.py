"""Export Ebon Manor runtime textures from the Codex image delivery (no painting).

Inputs are the owner-selected built-in generations and background removals in
the external delivery folder (see asset-deliveries BRIEF/DECISIONS and
docs/encounters/ebon-manor/ASSET_BRIEF.md). Every step is mechanical:

- Noirette sheet: the established NPC recipe, one nearest-neighbour sample per
  logical pixel (1536x1024 -> 192x128), then binary alpha at 128.
- Invitation icon: the generated clusters do not sit on an exact integer grid,
  so each logical cell takes the most frequent colour of its opaque source
  pixels (fixed palette) and is opaque when at least half covered.
- Painted sprites: alpha below 16 becomes 0 (removes the faint haze and hidden
  colour fringes), 240 and above becomes 255; bounding-box crop, Lanczos resize.
- Backdrops keep the generated pixels (RGB), only re-encoded as PNG.
Requires Pillow and numpy (local asset tools, not CI).
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Textures" / "EbonManor"

# delivery file -> SHA-256 of the exact accepted input
INPUTS = {
    "alpha/EM01_b.png": "e7b5b3ef6b5d",
    "alpha/EM02_a.png": "be39b0755f9d",
    "alpha/EM03_a.png": "387bd6a1efdd",
    "alpha/EM03_c.png": "01db1673a45c",
    "alpha/EM04_b.png": "bc6001311fdb",
    "raw/EM05_b.png": "f6f0f29d53b5",
    "raw/EM05F_a.png": "6b259560563f",
    "alpha/EM06_b.png": "387a49b8b057",
    "alpha/EM07_b.png": "76ab7704b1eb",
}

# EM02 cells (4x2, left to right, top to bottom); cell 3 (porcelain doll) is excluded by the owner
PROPS = ["Armchair", "Candelabra", "Portrait", None, "Clock", "Birdcage", "Mirror", "MusicBox"]
PROP_CELL = 176      # exported cell size (px); each prop fits within PROP_FIT
PROP_FIT = 160


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def load(source, name):
    path = source / name
    digest = sha256(path)
    if not digest.startswith(INPUTS[name]):
        raise ValueError(f"{name}: unexpected input {digest[:12]} (accepted {INPUTS[name]})")
    return Image.open(path).convert("RGBA"), digest


def clean_alpha(image):
    rgba = np.asarray(image).copy()
    alpha = rgba[..., 3]
    alpha[alpha < 16] = 0
    alpha[alpha >= 240] = 255
    rgba[alpha == 0, :3] = 0
    return Image.fromarray(rgba)


def hard_alpha(image):
    rgba = np.asarray(image).copy()
    opaque = rgba[..., 3] >= 128
    rgba[..., 3] = np.where(opaque, 255, 0)
    rgba[~opaque, :3] = 0
    return Image.fromarray(rgba)


def crop_to_content(image):
    alpha = np.asarray(image)[..., 3]
    ys, xs = np.nonzero(alpha)
    return image.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


def fit(image, size):
    scale = min(size / image.width, size / image.height)
    return image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)


def palette_of(image, colors):
    opaque = image.copy()
    rgba = np.asarray(opaque)
    rgb = Image.fromarray(np.where(rgba[..., 3:4] >= 128, rgba[..., :3], 0).astype(np.uint8))
    return rgb.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)


def block_mode(image, width, height, colors=28):
    """Downscale pixel art: majority colour per logical cell, coverage >= 1/2 is opaque."""
    rgba = np.asarray(image)
    quant = np.asarray(palette_of(image, colors))
    pal = np.asarray(palette_of(image, colors).getpalette()[:colors * 3], dtype=np.uint8).reshape(-1, 3)
    out = np.zeros((height, width, 4), dtype=np.uint8)
    bw, bh = image.width / width, image.height / height
    for y in range(height):
        for x in range(width):
            x0, x1 = round(x * bw), round((x + 1) * bw)
            y0, y1 = round(y * bh), round((y + 1) * bh)
            a = rgba[y0:y1, x0:x1, 3]
            if (a >= 128).mean() < 0.5:
                continue
            q = quant[y0:y1, x0:x1][a >= 128]
            out[y, x, :3] = pal[np.bincount(q, minlength=len(pal)).argmax()]
            out[y, x, 3] = 255
    return Image.fromarray(out)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", type=Path, required=True, help="asset-deliveries/ebon-manor/2026-10-01 (never committed)")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR)
    parser.add_argument("--preview", type=Path, required=True, help="local comparison sheet directory")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    args.preview.mkdir(parents=True, exist_ok=True)
    report, outputs = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__)), "inputs": {}}, {}

    sheet, report["inputs"]["alpha/EM01_b.png"] = load(args.source, "alpha/EM01_b.png")
    # The established NPC recipe (Liora): one nearest sample per logical pixel keeps
    # the generated outline/ribbon colours; alpha is made binary to drop the haze.
    outputs["Noirette.png"] = hard_alpha(sheet.resize((192, 128), Image.NEAREST))
    # comparison only: majority colour per cell (muddier ribbons in review)
    block_mode(sheet, 192, 128).save(args.preview / "Noirette-majority.png")

    icon, report["inputs"]["alpha/EM07_b.png"] = load(args.source, "alpha/EM07_b.png")
    outputs["BlackInvitation.png"] = block_mode(crop_to_content(clean_alpha(icon)), 44, 44, colors=16)

    props, report["inputs"]["alpha/EM02_a.png"] = load(args.source, "alpha/EM02_a.png")
    atlas = Image.new("RGBA", (PROP_CELL * sum(1 for p in PROPS if p), PROP_CELL))
    column, layout = 0, {}
    cw, ch = props.width // 4, props.height // 2
    for index, name in enumerate(PROPS):
        if name is None:
            continue
        cell = props.crop(((index % 4) * cw, (index // 4) * ch, (index % 4 + 1) * cw, (index // 4 + 1) * ch))
        art = fit(crop_to_content(clean_alpha(cell)), PROP_FIT)
        x = column * PROP_CELL + (PROP_CELL - art.width) // 2
        y = (PROP_CELL - art.height) // 2
        atlas.alpha_composite(art, (x, y))
        layout[name] = {"cell": column, "size": [art.width, art.height]}
        column += 1
    outputs["ManorProps.png"] = clean_alpha(atlas)
    report["prop_layout"] = layout

    for name, file, width in (("ChandelierWide.png", "alpha/EM03_a.png", 300), ("ChandelierTall.png", "alpha/EM03_c.png", 240)):
        art, report["inputs"][file] = load(args.source, file)
        art = crop_to_content(clean_alpha(art))
        outputs[name] = clean_alpha(art.resize((width, round(art.height * width / art.width)), Image.LANCZOS))

    shears, report["inputs"]["alpha/EM04_b.png"] = load(args.source, "alpha/EM04_b.png")
    halves = []
    for side in range(2):
        half = crop_to_content(clean_alpha(shears.crop((side * shears.width // 2, 0, (side + 1) * shears.width // 2, shears.height))))
        halves.append(half.resize((360, round(half.height * 360 / half.width)), Image.LANCZOS))
    strip = Image.new("RGBA", (360, max(h.height for h in halves) * 2))
    strip.alpha_composite(halves[0], (0, 0))
    strip.alpha_composite(halves[1], (0, strip.height // 2))
    outputs["Shears.png"] = clean_alpha(strip)

    for name, file in (("ManorHall.png", "raw/EM05_b.png"), ("ManorHallFinal.png", "raw/EM05F_a.png")):
        art, report["inputs"][file] = load(args.source, file)
        outputs[name] = art.convert("RGB")
    frame, report["inputs"]["alpha/EM06_b.png"] = load(args.source, "alpha/EM06_b.png")
    outputs["ManorFrame.png"] = clean_alpha(frame)

    report["outputs"] = {}
    for name, image in outputs.items():
        target = args.output / name
        image.save(target, optimize=True)
        report["outputs"][name] = {"size": list(image.size), "sha256": sha256(target)}
    (args.preview / "ebon-art-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["outputs"], indent=2))


if __name__ == "__main__":
    main()
