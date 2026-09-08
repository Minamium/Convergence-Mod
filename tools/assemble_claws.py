"""One-use feature-branch assembly. Removed before the final branch commit."""
from pathlib import Path
import hashlib
import json
import subprocess
from PIL import Image, ImageOps

root = Path(__file__).resolve().parents[1]
def edit(path, old, new):
    target = root / path
    text = target.read_text(encoding='utf-8')
    if old not in text:
        raise RuntimeError(f'Expected baseline not present: {path}')
    target.write_text(text.replace(old, new, 1), encoding='utf-8', newline='\n')

# Full RGBA derivative of the project's existing native-resolution P3 rig.
# Preserve its original bytes and do not quantize the output.
im = Image.open(root / 'Assets/Textures/NPCs/NullCantorRigAtlas.png').convert('RGBA')
hand = im.crop((955, 0, 1254, 650)); hand.thumbnail((85, 156), Image.Resampling.LANCZOS)
canvas = Image.new('RGBA', (192, 192))
left = hand.rotate(-40, Image.Resampling.BICUBIC, expand=True)
right = ImageOps.mirror(hand).rotate(40, Image.Resampling.BICUBIC, expand=True)
canvas.alpha_composite(left, (6, 16)); canvas.alpha_composite(right, (192-right.width-6, 16))
ring = im.crop((960, 792, 1254, 1093)).resize((60, 62), Image.Resampling.LANCZOS)
canvas.alpha_composite(ring, (66, 89))
image_path = 'Assets/Textures/Items/RitualArmaments/NullCantorClaws.png'
canvas.resize((128, 128), Image.Resampling.LANCZOS).save(root/image_path, optimize=True)
image_hash = hashlib.sha256((root/image_path).read_bytes()).hexdigest()

edit('Client/Encounters/FirstSeverance/NullRefrainVisuals.cs',
     '=> entity.ModItem is IRitualArmament;', '=> entity.ModItem is IRitualArmament && entity.ModItem is not NullRefrain;')
edit('Tests/Convergence.DomainTests/Directory.Build.targets', '<ItemGroup>', '<ItemGroup>\n    <Compile Include="../../Content/Encounters/FirstSeverance/Rewards/NullCantorClawMotion.cs" Link="Domain/NullCantorClawMotion.cs" />')
edit('build.txt', 'version = 0.2.28', 'version = 0.2.29')

texts = {
'en-US': ('Null Refrain — Cantor\'s Claws',
    'Manifest the distant arms of the Null Cantor\nLeft click: alternate enormous expanding true-melee claws\nHold this weapon for 6 seconds to charge one execution\nRight click: both hands crush the clicked location after a short windup\nThe charge pauses while unequipped or incapacitated; death clears it\nTrade this raid weapon for another class form at a Work Bench'),
'ja-JP': ('断唱・虚掌',
    '虚無の詠唱者の双腕を顕現する\n左クリック：巨大化する双爪を左右交互に振る直接近接攻撃\n装備中6秒で圧壊のチャージが完了\n右クリック：予備動作の後、指定地点を両手で挟み潰す\n持ち替え・行動不能中はチャージ停止。死亡で消失\n作業台で他クラスのレイド武器へ交換可能')}
for culture,(name,tip) in texts.items():
    path = root/f'Localization/RitualArmaments/{culture}.hjson'
    data = json.loads(path.read_text(encoding='utf-8'))
    data['Mods']['Convergence']['RitualArmaments']['NullRefrain']['Name'] = name
    data['Mods']['Convergence']['RitualArmaments']['NullRefrain']['Tooltip'] = tip
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

section = '''## Null Cantor's Claws — accepted melee redesign, 0.2.29

This section supersedes the sword/echo row and melee budget below. Internal item identity `NullRefrain`, the accepted Victory drop and all one-for-one exchanges are unchanged. Other four weapons, Boss attacks, Raid timing, recovery, music and protocol23 are untouched. The old sword projectile remains only as an unused legacy type; the item cannot fire it.

**Left click:** alternate independently articulated left/right five-finger claws using the actual P3 rig material. The hand expands from0.68x to2.30x during the stroke, then retracts; the whole attack stays inside560 world pixels of the player. Base duration28ticks, bounded10–90 after native true-melee speed. Only the palm and swept finger capsules damage, once per logical NPC root per swipe. No homing echo projectiles: this is the user's replacement true-melee design. Calamity's registered `TrueMeleeDamageClass` is resolved through the compatibility adapter, without using its internal singleton.

**Right click:** one charge after360 real game ticks while holding a usable claw. Holding an attack also recharges; unequipping/incapacitation pauses it, execution pauses it, death/world entry clears it. The charge belongs to the player, so extra item copies cannot duplicate it. A click spends it once and fixes a world coordinate within1120px. Both hands emerge, close during ticks28–40, and apply one4.2x ordinary-melee strike during40–44; the rest of the72-tick sequence is harmless. The166x132px damage ellipse is forecast. No forced NPC/player movement, literal instant kill, invulnerability bypass, homing after target lock, or attack-speed reduction of the six-second charge. Native NPC defenses and damage hooks remain in effect.

The new presentation uses native-resolution P3 palm/bone/talon regions, independently moving finger joints, broad layered violet/white crescents with negative-space interiors, connected fingertip wakes, dislodged dark shards and expanding broken pressure rings. The remote strike closes on a dark center before a vertical flare and ring release. Bright remnants never increase hit range. Both hands and major crescents remain under Reduced Effects; secondary shards and shake are reduced/disabled. Weapon sounds use a separate identifier and tracked, bounded voices; P3 masters are reused unchanged. No global pause, forced zoom or white-screen fill.

Initial base damage7700 gives16500 nominal unmodified swipe damage/sec at28ticks; execution damage32340, before native armor/crit/gear/hooks. Including its72-tick occupation and360-tick refill gives about18.2k nominal raw output/sec under continuous perfect contact. This is a budgeting calculation, not measured Calamity DPS or a claim of superiority over every final weapon. Tune `NullCantorClawMotion` from the user/Codex comparison, without changing Boss HP.

The native128x128 RGBA inventory icon is composed from the existing original P3 hand and palm atlas, not cropped from the concept board and not32-color quantized. The original atlas remains unchanged; runtime limbs keep the native source detail. Its exact derivative record is in `Assets/ATTRIBUTION.md`.

Design references: the user's annotated P3-arm sketch and approved dual-claw board; Calamity [Earth's growing true-melee silhouette](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/EarthHoldout.cs), [Ark's staged release](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/ArkOfTheCosmos_BlastAttack.cs), HotOG [Parasanguine's articulated cadence](https://github.com/TohruKobayashi/CalamityHunt/blob/5c2825e64c660384500decafa7702793dc4b48dc/Content/Projectiles/Weapons/Melee/ParasanguineHeld.cs), and WotG [Avatar's finger-chain rendering](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.RightArm.cs). These are fixed source design references, not video/playback verification or permission to import their materials. The implementation and all art used are project-authored; no foreign shader, texture, audio or code is copied.

Native projectile ownership is unchanged. Shared pure geometry covers both fractional rendering and collision; one root ledger prevents five fingers multiplying damage on the same enemy. Draw/audio lifetime is client-only. On death, Down, item change or world unload the attack or its client resources are released. No new Encounter packet or authority rule is introduced.

'''
edit('docs/encounters/first-severance/WEAPONS.md', '## Scope and acquisition', section+'## Scope and acquisition')
edit('docs/STATUS.md', 'Development **0.2.28**, protocol **23**.',
     'Development **0.2.29**, protocol **23**. Null Refrain is now the user-requested giant dual-claw weapon with alternating true-melee sweeps and a six-second charged remote crush; other four weapons and Boss behavior remain unchanged. [Weapon specification](encounters/first-severance/WEAPONS.md#null-cantors-claws--accepted-melee-redesign-0229) owns the revised controls, presentation and tuning.\n\nRetained Boss baseline:')
edit('docs/STATUS.md', '## Verification state\n',
     '## Verification state\n\n[Dual-claw evidence](evidence/2026-09-09-null-cantor-claws.json) records this branch\'s automatic checks and their limits. Actual game audiovisual acceptance and matched DPS remain user/Codex-owned. The tModLoader compile probe excludes the unchanged native Rogue adapter; it is not a full Calamity integration build.\n')
edit('docs/STATUS.md', '## Next change\n',
     '## Next change\n\nUser/Codex-owned0.2.29: reload the claw branch; confirm giant alternating hands, normal true-melee bonuses, single six-second charge use, clicked-location compression, both clients\' silhouettes and the new full-color icon. No FPS/log-collection system or unrelated Boss retuning is added.\n')
# One owning specification contains the full weapon story; other docs only link to it.
for name in ['docs/encounters/first-severance/VISUAL_SPEC.md','docs/AUDIO_CUE_SHEET.md']:
    path=root/name
    with path.open('a',encoding='utf-8') as f:
        link = 'WEAPONS.md' if name.endswith('VISUAL_SPEC.md') else 'encounters/first-severance/WEAPONS.md'
        f.write(f'\nThe0.2.29 [dual-claw weapon specification]({link}) owns the new P3-derived hand animation and separately grouped weapon playback. Boss art, timing and audio master files are unchanged.\n')
record=f'''### Null Cantor claw inventory icon — 0.2.29

- Runtime file: `{image_path}`
- Asset ID: null-cantor-claws-icon-2026-09-09
- Asset type: weapon texture
- Creator: project original P3 rig with ImageGen assistance; independent project-authored icon composition
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: existing `Assets/Textures/NPCs/NullCantorRigAtlas.png`, original record retained; no third-party input
- Tool/model/version: Pillow11.3.0, RGBA crop, reflection, bicubic rotation and Lanczos128x128 export
- Human modifications: user-directed P3 dual-claw design; agent composed the inventory-only derivative; original atlas unchanged
- License and redistribution terms: existing project asset terms remain undecided; development branch only, no publication approval
- Required attribution: retain original P3 rig provenance and this derivative record
- Reviewer and review date: source alpha/region inspection and output decode,2026-09-09; in-game acceptance pending
- Notes: no palette reduction; SHA256 `{image_hash}`. Runtime hand animation samples the original high-resolution atlas, not this inventory icon. Recipe is retained in the feature-branch assembly commit history and the external task working files.

'''
edit('Assets/ATTRIBUTION.md','## Records\n','## Records\n\n'+record)
(root/'docs/evidence/2026-09-09-null-cantor-claws.json').write_text(json.dumps({'build':'0.2.29','checks':'pending branch verification','not_run':['full Calamity integration build','game audiovisual acceptance','DPS comparison']},indent=2)+'\n')
subprocess.run(['python3','tools/docs_catalog.py','--write'],cwd=root,check=True)
print('CLAW_ICON_SHA256='+image_hash)
