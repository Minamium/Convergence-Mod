"""Render captured production part transforms for offline layout inspection.

This is a software QA view, not an asset edit or an in-game/shader screenshot.
Run preview-samurai-rig.ps1 first. Requires Pillow; writes only to .local.
"""
from pathlib import Path
import math
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
atlas = Image.open(ROOT / 'Assets/Textures/GhostSamurai/VioletRig.png').convert('RGBA')
panels = []
for section in (ROOT / '.local/samurai-rig-draws.txt').read_text(encoding='utf-8-sig').split('#')[1:]:
    lines = section.strip().splitlines()
    panel = Image.new('RGBA', (600, 600), '#202734')
    draw = ImageDraw.Draw(panel)
    draw.text((20, 20), lines[0], fill='white')
    draw.line((300, 60, 300, 580), fill='#303849')
    draw.line((20, 265, 580, 265), fill='#303849')
    for line in lines[1:]:
        x,y,sx,sy,w,h,r,g,b,a,rot,ox,oy,kx,ky,flip = list(map(float, line.split(',')))[:16]
        if a <= 0:
            continue
        tile = atlas.crop((int(sx), int(sy), int(sx+w), int(sy+h)))
        if flip:
            tile = tile.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        # Sprite tint is premultiplied; Pillow compositing expects straight RGB.
        rgba = list(tile.split())
        for i, tint in enumerate((r,g,b)):
            rgba[i] = rgba[i].point(lambda v, tint=tint: int(v * min(1, tint/a)))
        rgba[3] = rgba[3].point(lambda v: int(v * min(1, a/255)))
        tile = Image.merge('RGBA', rgba)
        c,s = math.cos(rot), math.sin(rot)
        inverse = (c/kx, s/kx, ox-(c*x+s*y)/kx, -s/ky, c/ky, oy+(s*x-c*y)/ky)
        raster = tile.transform(panel.size, Image.Transform.AFFINE, inverse, Image.Resampling.BICUBIC)
        panel.alpha_composite(raster)
    panels.append(panel)
sheet = Image.new('RGB', (1800, math.ceil(len(panels)/3)*600+45), '#101520')
ImageDraw.Draw(sheet).text((20, 15), 'OFFLINE PART LAYOUT - production transforms; GPU shaders / Verlet / trails not rendered', fill='white')
for i, panel in enumerate(panels):
    sheet.paste(panel, ((i%3)*600, (i//3)*600+45))
sheet.save(ROOT / '.local/samurai-rig-preview.png')
print('Rendered .local/samurai-rig-preview.png')
