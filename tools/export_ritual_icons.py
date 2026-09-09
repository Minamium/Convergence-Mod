"""Mechanical alpha-preserving export of text-generated weapon artwork.

No generation, recoloring, background removal or compositing of old weapon art.
Originals stay outside the repository. Pass NAME=original.png for each new image.
"""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("sources", nargs="+", help="NAME=full path to generated PNG")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--preview", type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    entries = []
    sheet = Image.new("RGB", (len(args.sources) * 220, 180), (18, 20, 31))
    draw = ImageDraw.Draw(sheet)
    for ordinal, pair in enumerate(args.sources):
        name, source = pair.split("=", 1)
        if name not in {"PaleMeridian", "LacunaTestament", "ChoirOfTheUnmade", "LastWitness"}:
            raise ValueError("Unexpected asset name")
        raw = Image.open(source)
        if "A" not in raw.getbands():
            raise ValueError(f"{name}: generated file has no alpha")
        raw = raw.convert("RGBA")
        alpha = raw.getchannel("A")
        if alpha.getextrema() != (0, 255):
            raise ValueError(f"{name}: expected transparent background and opaque material")
        bounds = alpha.getbbox()
        cropped = raw.crop(bounds)
        outputs = []
        for size, fill, suffix in ((128, 116, ""), (512, 464, "_Apparatus")):
            fitted = cropped.copy()
            fitted.thumbnail((fill, fill), Image.Resampling.LANCZOS)
            result = Image.new("RGBA", (size, size), (0, 0, 0, 0))
            result.alpha_composite(fitted, ((size - fitted.width) // 2, (size - fitted.height) // 2))
            path = args.output / f"{name}{suffix}.png"
            result.save(path)
            outputs.append({"file": path.name, "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                            "visible_bounds": result.getchannel("A").getbbox()})
            if size == 128:
                sheet.paste(result, (ordinal * 220 + 46, 8), result)
                small = result.resize((40, 40), Image.Resampling.LANCZOS)
                sheet.paste(small, (ordinal * 220 + 4, 82), small)
                draw.text((ordinal * 220 + 6, 150), name, fill=(220, 220, 232))
        entries.append({"name": name, "original_sha256": hashlib.sha256(Path(source).read_bytes()).hexdigest(),
                        "original_size": raw.size, "original_bounds": bounds, "outputs": outputs})
    if args.preview:
        args.preview.parent.mkdir(parents=True, exist_ok=True)
        sheet.save(args.preview)
    print(json.dumps(entries, indent=2))


if __name__ == "__main__":
    main()
