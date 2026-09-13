"""Mechanical alpha-preserving inventory export of the approved generated art."""
import argparse
from pathlib import Path
from PIL import Image

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--input',type=Path,required=True)
parser.add_argument('--output',type=Path,default=Path('Assets/Textures/Items/TheaterDoll.png'))
args=parser.parse_args()
im=Image.open(args.input).convert('RGBA')
if im.getextrema()[3][0]!=0: raise ValueError('A genuine transparent source is required')
art=im.crop(im.getchannel('A').getbbox())
art.thumbnail((40,44),Image.Resampling.LANCZOS)
out=Image.new('RGBA',(44,48));out.alpha_composite(art,((44-art.width)//2,(48-art.height)//2))
args.output.parent.mkdir(parents=True,exist_ok=True)
out.save(args.output)
print(f'{args.output}: {out.size}, painted bounds {out.getchannel("A").getbbox()}')
