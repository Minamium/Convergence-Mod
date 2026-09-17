"""Import only the exact owner-approved background, without a lossy conversion.

Usage: python tools/import_scarlet_background.py /path/to/approved.png
No download, replacement artwork, commit or push is performed by this helper.
"""
from __future__ import annotations
import argparse
import hashlib
from pathlib import Path

APPROVED_SHA256 = '94b77c968991bf52b14504bb11095c417dbf4dd2abb3bc378da779da39400a2d'
DESTINATION = Path('Assets/Textures/Backgrounds/ScarletSanctum.png')

def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    args = parser.parse_args()
    data = args.source.read_bytes()
    if not data.startswith(b'\x89PNG\r\n\x1a\n'):
        parser.error('Expected the original approved PNG file')
    digest = hashlib.sha256(data).hexdigest()
    if digest != APPROVED_SHA256:
        parser.error(f'Artwork identity mismatch: {digest}; refusing to substitute another image')
    root = Path(__file__).resolve().parents[1]
    destination = root / DESTINATION
    if destination.exists() and destination.read_bytes() != data:
        parser.error('Different artwork already exists at the destination')
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)
    print(f'Imported {DESTINATION.as_posix()} unchanged; sha256={digest}')

if __name__ == '__main__':
    main()
