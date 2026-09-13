"""Export the exact first in-game NPC cel; no repainting or generated artwork."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / 'Assets/Textures/NPCs/DollTheater/DollAttendant.png'
with Image.open(source) as atlas:
    if atlas.size != (32, 624):
        raise ValueError('Review the NPC cel layout before exporting')
    atlas.crop((0, 0, 32, 52)).resize((96, 156), Image.Resampling.NEAREST).save(
        ROOT / 'docs/media/doll-npc.png')
