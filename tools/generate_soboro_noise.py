"""Generate Soboro's tileable slash noise (original, deterministic).

Three independent value-noise FBM fields are stored in R/G/B of one 128x128
texture that tiles seamlessly (lattice periods divide the size). The pixel
slash shader samples it for torn edges, splinters, speed streaks and the
dissolve; the palette quantization happens in the shader, not here.

Usage: python tools/generate_soboro_noise.py [output.png]
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

SIZE = 128
BASE_CELLS = 16  # cells across one tile at the first octave
SEEDS = (1709, 2851, 4441)


def lattice(cells: int, seed: int) -> np.ndarray:
    rng = np.random.default_rng(seed * 7919 + cells)
    return rng.random((cells, cells))


def value_noise(cells: int, seed: int) -> np.ndarray:
    grid = lattice(cells, seed)
    coords = np.arange(SIZE) * cells / SIZE
    i0 = np.floor(coords).astype(int)
    f = coords - i0
    f = f * f * (3 - 2 * f)
    i1 = (i0 + 1) % cells  # periodic wrap keeps the tile seamless
    x0, x1, fx = i0[None, :], i1[None, :], f[None, :]
    y0, y1, fy = i0[:, None], i1[:, None], f[:, None]
    top = grid[y0, x0] * (1 - fx) + grid[y0, x1] * fx
    bottom = grid[y1, x0] * (1 - fx) + grid[y1, x1] * fx
    return top * (1 - fy) + bottom * fy


def fbm(seed: int) -> np.ndarray:
    total = np.zeros((SIZE, SIZE))
    amplitude, norm, cells = 0.5, 0.0, BASE_CELLS
    for octave in range(3):
        total += amplitude * value_noise(cells, seed + octave * 17)
        norm += amplitude
        amplitude *= 0.5
        cells *= 2
    field = total / norm
    # stretch contrast so thresholds in the shader span the whole range
    low, high = np.percentile(field, 1), np.percentile(field, 99)
    return np.clip((field - low) / (high - low), 0, 1)


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    output = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "Assets/Textures/Items/DXOboro/SlashNoise.png"
    channels = [fbm(seed) for seed in SEEDS]
    rgb = np.stack(channels, axis=-1)
    image = Image.fromarray((rgb * 255 + 0.5).astype(np.uint8), "RGB")
    image.save(output, optimize=True)
    print(f"wrote {output} ({SIZE}x{SIZE}, seeds {SEEDS})")


if __name__ == "__main__":
    main()
