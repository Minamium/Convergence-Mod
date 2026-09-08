"""One-shot, branch-scoped source assembly; removed from the resulting commit."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys

ROOT = Path.cwd()

def replace(path, old, new, count=1):
    p = ROOT / path
    s = p.read_text(encoding='utf-8')
    if s.count(old) != count:
        raise RuntimeError(f'{path}: expected {count} exact matches, got {s.count(old)}')
    p.write_text(s.replace(old, new), encoding='utf-8')

art = ROOT / 'Assets/Textures/Items/RitualArmaments'
parts = [ROOT / f'tools/.ritual-lacuna.{i}' for i in range(2)]
magic = b''.join(p.read_bytes() for p in parts)
assert hashlib.sha1(b'blob ' + str(len(magic)).encode() + b'\0' + magic).hexdigest() == '10e2ab6703e045736651a1b600a5be7a104036da'
(art / 'LacunaTestament.png').write_bytes(magic)
for p in parts:
    p.unlink()

items = 'Content/Encounters/FirstSeverance/Rewards/RitualArmaments.cs'
replace(items, 'internal const string TexturePath = "Convergence/Assets/Textures/Items/RitualArmaments";',
              'internal const string TexturePath = "Convergence/Assets/Textures/Items/RitualArmaments/NullRefrain";')
replace(items, 'public override string Texture => RitualArmamentItems.TexturePath;',
              'public override string Texture => "Convergence/Assets/Textures/Items/RitualArmaments/" + Name;', count=2)
replace(items, 'Projectile.NewProjectile(source, player.MountedCenter, aim * 19,',
              'Projectile.NewProjectile(source, player.MountedCenter + aim * 136, aim * 19,')

visual = 'Client/Encounters/FirstSeverance/NullRefrainVisuals.cs'
replace(visual, 'private static Asset<Texture2D>? atlas;', 'private static readonly Asset<Texture2D>?[] textures = new Asset<Texture2D>?[6];')
p = ROOT / visual
s = p.read_text()
a = s.index('    internal static Texture2D Atlas =>')
b = s.index('    internal static Color ColorFor', a)
s = s[:a] + '''    private static readonly string[] Names = { "NullRefrain", "PaleMeridian", "LacunaTestament", "ChoirOfTheUnmade", "LastWitness", "ChoirSentinel" };
    // Pivots were measured in the approved concept crops, independently of export resolution.
    private static readonly Vector2[] DesignSizes = { new(279, 49), new(274, 57), new(110, 116), new(175, 43), new(100, 109), new(54, 100) };
    internal static Texture2D Texture(int row) => (textures[row] ??= ModContent.Request<Texture2D>(
        "Convergence/Assets/Textures/Items/RitualArmaments/" + Names[row])).Value;
    internal static void Unload() => Array.Clear(textures);
    internal static Rectangle Source(int row) => Texture(row).Bounds;
''' + s[b:]
s = s.replace('batch.Draw(Atlas, world - Main.screenPosition, source, tint, angle,\n            pivot ?? new Vector2(source.Width * .5f, source.Height * .5f),',
'''batch.Draw(Texture(row), world - Main.screenPosition, source, tint, angle,
            pivot is { } measured ? measured * source.Size() / DesignSizes[row] : source.Size() * .5f,''')
s = s.replace('spriteBatch.Draw(RitualArmamentArt.Atlas, position, source,',
              'spriteBatch.Draw(RitualArmamentArt.Texture(row), position, source,')
assert 'Texture2D Atlas' not in s and 'Draw(Atlas' not in s and 'Art.Atlas' not in s
marker = '    internal static void Line(SpriteBatch batch, Vector2 a, Vector2 b, Color color, float width)'
method = '''    internal static void Blade(SpriteBatch batch, Vector2 world, float angle, float length, Color tint, float open)
    {
        Texture2D image = Texture(0);
        float scale = length / image.Width;
        Vector2 origin = new(42, 26), hinge = new(68, image.Height * .5f);
        Rectangle root = new(0, 0, 68, image.Height);
        batch.Draw(image, world - Main.screenPosition, root, tint, angle, origin, scale, SpriteEffects.None, 0);
        Vector2 pivot = world + ((hinge - origin) * scale).RotatedBy(angle);
        for (int side = -1; side <= 1; side += 2)
        {
            int y = side < 0 ? 0 : image.Height / 2;
            int height = side < 0 ? image.Height / 2 : image.Height - y;
            Rectangle plate = new(68, y, image.Width - 68, height);
            batch.Draw(image, pivot - Main.screenPosition, plate, tint, angle + side * open * .045f,
                hinge - new Vector2(plate.X, plate.Y), scale, SpriteEffects.None, 0);
        }
    }
'''
assert s.count(marker) == 1
s = s.replace(marker, method + marker)
old = '''        RitualArmamentArt.Sprite(batch, 0, center, angle, reach + 50, Color.White * fade,
            slash.Facing < 0, new Vector2(42, 26));
        float open = RitualArmamentRules.Envelope(progress, .22f, .72f, .95f);'''
new = '''        float open = RitualArmamentRules.Envelope(progress, .22f, .72f, .95f);
        RitualArmamentArt.Blade(batch, center, angle, reach + 50, Color.White * fade,
            open * (slash.Combo == 2 ? 1 : .28f));'''
assert s.count(old) == 1
s = s.replace(old, new)
p.write_text(s)

names = ['NullRefrain', 'PaleMeridian', 'LacunaTestament', 'ChoirOfTheUnmade', 'LastWitness', 'ChoirSentinel']
expected = ['9b6d2300482c05eaa314e07b28b8f6447e0d728c', '235ca67b0899dbee1f26e7803713708643e6241f', '10e2ab6703e045736651a1b600a5be7a104036da', 'e18493b5d929f0ebd674ea8e13fdef369a8875c7', '0ddedd923f719289433f90a91fda69448b9582d1', 'cfb36e21c3d884bd788c58e45a34cad9972fe60a']
records = []
for name, sha in zip(names, expected):
    data = (art / (name + '.png')).read_bytes()
    actual = hashlib.sha1(b'blob ' + str(len(data)).encode() + b'\0' + data).hexdigest()
    if actual != sha:
        raise RuntimeError(f'Incorrect uploaded art: {name}: {actual}')
    records.append(f'''- Runtime file: `Assets/Textures/Items/RitualArmaments/{name}.png`
- Asset ID: ritual-{name.lower()}-0225-2026-09-08
- Asset type: {'minion' if name == 'ChoirSentinel' else 'weapon'} texture
- Creator: project-directed original concept with built-in OpenAI ImageGen assistance; assistant runtime export
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: user-approved CONVERGENCE / CONCEPT 01 board, generation d492a069-958c-4f23-9747-c26693b26d66; no third-party reference or extracted game asset
- Tool/model/version: built-in ImageGen backend not surfaced; Python/Pillow 12.3.0
- Human modifications: user approved concept; assistant cropped and alpha-masked silhouettes, rotated the staff, made 32-color transparent runtime exports; gun/book use compact exports for their draw sizes
- License and redistribution terms: project asset license undecided; development branch only, no public release approval
- Required attribution: no external requirement specified; preserve provenance
- Reviewer and review date: assistant alpha/silhouette inspection and exact-byte verification, 2026-09-08; in-game review pending
- Notes: source board SHA256 c833eeba1fc06d53951df33bce597efb29c0b52cc0fb221e73acd3586e93c797; export SHA256 {hashlib.sha256(data).hexdigest()}. Original concept and extraction recipe remain outside the repository. Existing NullRefrain.png and all Boss/audio assets are retained unchanged.
''')
replace('Assets/ATTRIBUTION.md', '## Records\n', '## Records\n\n### Five ritual armaments — 0.2.25 / 2026-09-08\n\n' + '\n'.join(records))

localizations = {
'en-US': {
'NullRefrain': ['Null Refrain', 'Three flowing cuts release strongly homing echoes.\nThe third cut opens the blade and releases three echoes.\nExchange one-for-one with any other ritual armament at a Work Bench.'],
'PaleMeridian': ['Pale Meridian', 'Converts bullets into strongly homing ivory needles.\nEvery sixth shot is a stronger verdict that pierces up to three enemies.\n75% chance not to consume ammunition.\nExchange one-for-one with any other ritual armament at a Work Bench.'],
'LacunaTestament': ['Lacuna Testament', 'Three floating lenses open in sequence and release homing rays.\nEach lens contributes to the same shot damage budget.\nExchange one-for-one with any other ritual armament at a Work Bench.'],
'ChoirOfTheUnmade': ['Choir of the Unmade', 'Summons an articulated sentinel, using one minion slot.\nSentinels form a choir and fire staggered homing notes; every third note is stronger.\nUses the normal minion target command.\nExchange one-for-one with any other ritual armament at a Work Bench.'],
'LastWitness': ['Last Witness', 'A strongly homing ritual blade returns along a smooth arc.\nIts damage is split between the outward and returning passes.\nStealth strikes release three additional homing witnesses.\nExchange one-for-one with any other ritual armament at a Work Bench.'],
},
'ja-JP': {
'NullRefrain': ['断唱', '滑らかな三連斬りから、強く追尾する残響刃を放つ\n三撃目は刃が開き、三本の残響刃を放つ\n作業台で、ほかの儀式武装と1対1で交換できる'],
'PaleMeridian': ['蒼白の子午線', '弾丸を強く追尾する象牙色の針へ変換する\n六発目は威力が増し、最大三体を貫く\n75％の確率で弾薬を消費しない\n作業台で、ほかの儀式武装と1対1で交換できる'],
'LacunaTestament': ['欠落の遺言', '三枚の浮遊レンズが順に開き、追尾光線を放つ\n作業台で、ほかの儀式武装と1対1で交換できる'],
'ChoirOfTheUnmade': ['未成の聖歌隊', 'ミニオン枠を一つ使い、収容機械の唱導体を召喚する\n隊列を組んで追尾音弾を時間差で放ち、三音目は威力が増す\n通常のミニオン標的指定に対応\n作業台で、ほかの儀式武装と1対1で交換できる'],
'LastWitness': ['最後の証人', '強く追尾し、滑らかな弧を描いて戻る儀式刃\n往路と復路に威力を分配する\nステルスストライクは、さらに三本の追尾する証人を放つ\n作業台で、ほかの儀式武装と1対1で交換できる'],
}}
for locale, entries in localizations.items():
    content = {'Mods': {'Convergence': {'RitualArmaments': {name: {'Name': values[0], 'Tooltip': values[1]} for name, values in entries.items()},
        'Buffs': {'ChoirOfTheUnmadeBuff': {'DisplayName': '未成の聖歌隊' if locale == 'ja-JP' else 'Choir of the Unmade',
            'Description': '唱導体があなたのために歌っている' if locale == 'ja-JP' else 'The sentinels sing for you'}}}}}
    folder = ROOT / 'Localization/RitualArmaments'
    folder.mkdir(exist_ok=True)
    (folder / f'{locale}.hjson').write_text(json.dumps(content, ensure_ascii=False, indent=2) + '\n')

replace('build.txt', 'version = 0.2.24', 'version = 0.2.25')
status = ROOT / 'docs/STATUS.md'
s = status.read_text()
a = s.index('Development **0.2.24**')
b = s.index('\n\n- First Severance', a)
s = s[:a] + '''Development **0.2.25**, protocol **21**. The weapon-only branch adds five ritual armament forms, strong bounded homing, continuous client animation, original approved-concept texture exports and one-for-one Work Bench conversions from the existing Victory reward. Boss logic, phase schedules, mechanics, recovery, world data and all music/SFX masters are unchanged. Native weapon projectiles use ordinary tModLoader replication, not a new encounter packet. See [weapon specification](encounters/first-severance/WEAPONS.md). Damage is initial tuning, not measured Calamity-endgame superiority.''' + s[b:]
s = s.replace('## Verification state\n', '''## Verification state

Weapon-branch checks and unrun build acceptance are recorded in [weapon evidence](evidence/2026-09-08-ritual-armaments.json). The initial implementation's existing GitHub Actions checks passed, including the added pure-domain cases. Full Mod compilation, native Rogue integration and visual/DPS acceptance remain user/Codex-owned; neither pure tests nor the source assembly imply they passed.

The following is retained Boss-build evidence, not a weapon-build claim:
''')
s = s.replace('last_reviewed: 2026-09-07', 'last_reviewed: 2026-09-08')
s = s.replace('## Next change\n', '''## Next change

User/Codex-owned0.2.25: build/load the weapon branch, inspect the five silhouettes/continuous motion, native Rogue stealth and minion behavior, then tune the central damage seeds against matched Calamity2.2.4 equipment. Target roughly1.10x a selected class-endgame benchmark, not dominance over every weapon/target. Keep the remaining Boss-only check below separate.
''')
status.write_text(s)

weapon_doc = '''---
doc_id: encounter.first-severance.weapons
document_type: spec
status: provisional
owners:
  - gameplay
  - art
last_reviewed: 2026-09-08
source_of_truth_for:
  - first_severance.reward_weapons
aliases:
  - ritual armaments
  - raid reward weapons
related_code:
  - Content/Encounters/FirstSeverance/Rewards
  - Client/Encounters/FirstSeverance/NullRefrainVisuals.cs
  - Common/Compatibility/Calamity/CalamityRogueArmament.cs
related_docs:
  - project.status
  - encounter.first-severance.spec
---

# First Severance — Five Ritual Armaments

## Scope and acquisition

User-approved concept: obsidian/ivory/aged-gold ritual machinery, hollow apertures and physical material opening before a bright release. This development change touches weapons only. Boss behavior, loot execution, mechanics, recovery and music are unchanged.

Accepted Victory still drops one Null Refrain for each frozen-roster participant as ordinary shared world items. At a Work Bench, any one armament converts into any other, consuming exactly one input and producing one output. The20 directed recipes neither multiply rewards nor allow pre-Raid crafting. No recipes use vanilla materials alone. The prior statement that the reward has no recipe is superseded only for these exchanges.

## Five play styles

| Form | Behavior |
|---|---|
| Null Refrain / 断唱 |22/22/32-tick base three-cut cycle;310/405px reach. First two cuts each release one0.40x homing echo; third physical cut is1.7x and releases three0.30x base-damage echoes. Quintic pose and swept physical collision; only the real blade and echo heads damage. |
| Pale Meridian / 蒼白の子午線 |12-tick bullet-converting rifle,75% ammunition conservation. Sixth shot2.1x and up to three distinct NPC roots. Physical muzzle origin, recoil, split rail light and long harmless wake. |
| Lacuna Testament / 欠落の遺言 |20-tick cast,18 base mana. Three0.75x rays open over3/6/9-tick windups; the floating book and lenses are harmless. |
| Choir of the Unmade / 未成の聖歌隊 |One minion slot per sentinel. Smooth formation/approach,36-tick notes staggered by formation position; every third note1.4x. Normal minion targeting and sacrifice; idle bodies do not deal contact damage. |
| Last Witness / 最後の証人 |40-tick reusable Rogue throw. Damage shares0.70 outbound/0.30 return, once per logical NPC root on each pass. Native Calamity stealth strike adds three0.20x echoes. Smooth targeted outbound flight and recall, not a forced player dash. |

## Homing and presentation

Acquire at1800px, retain at2100px, honor line of sight and chaseable targets. Target identity is selected only by the ordinary projectile owner and synchronized through NPC indices in native projectile AI; observers do not independently switch targets. Manual minion targets take priority. Smooth turn cap0.24rad per game tick and exponential speed response are normalized by extraUpdates; predictive lead is capped to10 ticks. Head sweeps cover fast motion; decorative trails never enlarge damage. Segmented enemies share root hit ledgers to avoid multiplying a single strike across every body segment. These weapons disable PvP damage.

Native player weapon projectiles follow the existing cooperative tModLoader ownership model; this is not an anti-cheat guarantee or a new server-owned Raid damage adapter. No custom encounter packet/ID is added and protocol21 is retained. Participants must nevertheless load the same Mod content build.

Client-only visuals use fractional rendering, tapered connected trails, layered low-opacity blade echoes and local bounded impact accents. Ordinary gunfire stays narrow; sixth shot, third cut and stealth strike carry stronger punctuation. Reduced Effects and Screen Shake remain respected. No game pause, forced zoom, input lock, persistent global flag or full-screen white flash. Effects reset on world/unload, and source textures remain ReLogic-owned. Existing sounds are reused through a separate weapon Identifier group, so weapon playback does not evict Boss cues. No master audio file changes.

## Initial power budget — not measured DPS

Goal: approximately5–15% above a selected same-class Calamity2.2.4 endgame benchmark under matched gear and target conditions. Actual calibration belongs to user/Codex measurements. No finite design can guarantee that ratio against every final weapon, movement pattern and target type.

`RitualArmamentRules` centralizes initial damage seeds and a1.10 design factor. This factor is applied to **provisional project seeds**, not to an empirically measured Calamity DPS baseline. Do not describe it as verified10% superiority.

| Class | Initial base damage | Nominal raw output/sec |
|---|---:|---:|
| Melee |4235 |18054, all physical cuts and echoes connect,76 base ticks |
| Ranged |2002 |11845, six-shot cycle, before ammunition contribution |
| Magic |2024 |13662, all three rays connect |
| Summon |968 |1828 per slot;18284 for10 slots |
| Rogue |9680 |14520, both shares connect, before stealth |

These are arithmetic budgets, not in-game expected DPS: no defense, crits, armor/accessories, attack speed, miss rate, target motion or native Rogue bonuses. Ranged piercing is not three hits on the same root. Rogue return is not another full-damage hit. Magic lenses and VFX counts cannot add unbudgeted damage. Adjust central seeds after selecting matched representative weapons; do not raise Boss HP to conceal reward imbalance.

## Provenance and engine seams

Textures are alpha-masked exports of the user-approved original concept board, generation d492a069-958c-4f23-9747-c26693b26d66. Compact32-color runtime exports are initial game silhouettes, not newly painted production atlases. The original board, extraction script and full-color exports are retained outside the repository. Every shipped PNG is recorded in Assets/ATTRIBUTION.md; previous Boss/weapon/audio files are preserved.

Runtime target: pinned tModLoader2026.07.3.0 / source666f69962d3bdffde54fc14025f02634965b4e7c, Calamity2.2.4. Hook/reference research used official ModItem/ModProjectile/SoundStyle documentation and pinned public Calamity source1a8cebd27ec5615316b78f71973446b5528d2b78 (2.2.2), including RogueWeapon, ScarletDevil, Exoblade, Photoviscerator, Eternity and Endogenesis. That source is **not proven identical** to the2.2.4 runtime. No external implementation/asset is copied. The narrow Rogue bridge retains native RogueWeapon hooks and marks native stealth projectiles only inside Common/Compatibility/Calamity.

## Verification and handoff

Pure tests cover bounded easing, active-window continuity, homing turn/speed bounds, extra-update invariance and burst/return budget accounting. Existing domain/codec CI is not a full Mod build. User/Codex should build/load on the pinned environment and check all five forms, native stealth consumption, minion slots/targeting, texture pivots, projectile cancellation on Down/death and matched single-target damage. Do not mark those checks passed until observed.
'''
(ROOT / 'docs/encounters/first-severance/WEAPONS.md').write_text(weapon_doc)

replace('docs/encounters/first-severance/ENCOUNTER_SPEC.md', '# First Severance Encounter Specification\n', '''# First Severance Encounter Specification

## Weapon-only reward extension — 0.2.25

[Five Ritual Armaments](WEAPONS.md) owns current reward mechanics, art and initial damage budgets. Existing Victory still drops Null Refrain; twenty one-for-one Work Bench exchanges let every class choose a form without changing the Boss loot executor. The older no-recipe/three-stroke tuning below is historical where it conflicts with that weapon specification. Boss actions, HP, damage, field and recovery are unchanged.
''')
replace('docs/encounters/first-severance/VISUAL_SPEC.md', '# First Severance — Null Cantor Visual Pass\n', '''# First Severance — Null Cantor Visual Pass

Weapon-only0.2.25: [Five Ritual Armaments](WEAPONS.md) owns the approved-concept drop-weapon silhouettes, continuous animation and homing presentation. This supersedes only older Null Refrain weapon descriptions below; all Boss presentation is retained.
''')
replace('docs/AUDIO_CUE_SHEET.md', '# First Severance Audio Cue Sheet\n', '''# First Severance Audio Cue Sheet

Weapon-only0.2.25: the five ritual armaments reuse existing SFX masters with a separate `Convergence:RitualWeapon:` sound Identifier group, bounded positional volume, two-voice limits and unload cleanup. Weapon playback does not evict Boss cue voices. All Boss/music masters and cues remain unchanged; [weapon specification](encounters/first-severance/WEAPONS.md) owns the new timing.
''')

evidence = {
    'schema_version': 1, 'date': '2026-09-08', 'build': '0.2.25', 'protocol': 21,
    'scope': 'Five reward weapon forms, native homing projectiles, client animation, approved-concept texture exports and workbench conversions. Boss and music unchanged.',
    'checks': {
        'initial_ci': 'Repository checks run34215395425 passed on e86f2859e1c623fd0e81d987a5132c53d70f9f44; includes five new pure-domain cases. Final assembly checked separately by the one-shot workflow before commit.',
        'asset_bytes': 'Every runtime export compared with its expected Git blob SHA before assembly; no corrupted upload is accepted.',
        'mod_build': 'not_run; full pinned tModLoader/Calamity environment is user/Codex-owned',
        'runtime_fps_dps_audio': 'not_run; not inferred from domain/codec checks or arithmetic budgets',
    },
    'power_target': 'Approximately +10% versus selected matched class-endgame benchmarks; initial seeds are provisional, not measured evidence.',
    'art_source': {'generation': 'd492a069-958c-4f23-9747-c26693b26d66', 'sha256': 'c833eeba1fc06d53951df33bce597efb29c0b52cc0fb221e73acd3586e93c797'},
    'main_changed': False,
}
(ROOT / 'docs/evidence/2026-09-08-ritual-armaments.json').write_text(json.dumps(evidence, indent=2) + '\n')

# Neither helper survives in the finished branch or .tmod package.
for relative in ['.github/workflows/ritual-review-transfer.yml', '.github/workflows/ritual-armament-finalize.yml', 'tools/finalize_ritual_armaments.py']:
    p = ROOT / relative
    if p.exists(): p.unlink()
subprocess.run([sys.executable, 'tools/docs_catalog.py', '--write'], check=True)
