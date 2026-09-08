---
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

## Null Cantor's Claws — accepted melee redesign, 0.2.29

This section supersedes the sword/echo row and melee budget below. Internal item identity `NullRefrain`, the accepted Victory drop and all one-for-one exchanges are unchanged. Other four weapons, Boss attacks, Raid timing, recovery, music and protocol23 are untouched. The old sword projectile remains only as an unused legacy type; the item cannot fire it.

**Left click:** alternate independently articulated left/right five-finger claws using the actual P3 rig material. The hand expands from0.68x to2.30x during the stroke, then retracts; the whole attack stays inside560 world pixels of the player. Base duration28ticks, bounded10–90 after native true-melee speed. Only the palm and swept finger capsules damage, once per logical NPC root per swipe. No homing echo projectiles: this is the user's replacement true-melee design. Calamity's registered `TrueMeleeDamageClass` is resolved through the compatibility adapter, without using its internal singleton.

**Right click:** one charge after360 real game ticks while holding a usable claw. Holding an attack also recharges; unequipping/incapacitation pauses it, execution pauses it, death/world entry clears it. The charge belongs to the player, so extra item copies cannot duplicate it. A click spends it once and fixes a world coordinate within1120px. Both hands emerge, close during ticks28–40, and apply one4.2x ordinary-melee strike during40–44; the rest of the72-tick sequence is harmless. The166x132px damage ellipse is forecast. No forced NPC/player movement, literal instant kill, invulnerability bypass, homing after target lock, or attack-speed reduction of the six-second charge. Native NPC defenses and damage hooks remain in effect.

The new presentation uses native-resolution P3 palm/bone/talon regions, independently moving finger joints, broad layered violet/white crescents with negative-space interiors, connected fingertip wakes, dislodged dark shards and expanding broken pressure rings. The remote strike closes on a dark center before a vertical flare and ring release. Bright remnants never increase hit range. Both hands and major crescents remain under Reduced Effects; secondary shards and shake are reduced/disabled. Weapon sounds use a separate identifier and tracked, bounded voices; P3 masters are reused unchanged. No global pause, forced zoom or white-screen fill.

Initial base damage7700 gives16500 nominal unmodified swipe damage/sec at28ticks; execution damage32340, before native armor/crit/gear/hooks. Including its72-tick occupation and360-tick refill gives about18.2k nominal raw output/sec under continuous perfect contact. This is a budgeting calculation, not measured Calamity DPS or a claim of superiority over every final weapon. Tune `NullCantorClawMotion` from the user/Codex comparison, without changing Boss HP.

The native128x128 RGBA inventory icon is composed from the existing original P3 hand and palm atlas, not cropped from the concept board and not32-color quantized. The original atlas remains unchanged; runtime limbs keep the native source detail. Its exact derivative record is in `Assets/ATTRIBUTION.md`.

Design references: the user's annotated P3-arm sketch and approved dual-claw board; Calamity [Earth's growing true-melee silhouette](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/EarthHoldout.cs), [Ark's staged release](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/ArkOfTheCosmos_BlastAttack.cs), HotOG [Parasanguine's articulated cadence](https://github.com/TohruKobayashi/CalamityHunt/blob/5c2825e64c660384500decafa7702793dc4b48dc/Content/Projectiles/Weapons/Melee/ParasanguineHeld.cs), and WotG [Avatar's finger-chain rendering](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.RightArm.cs). These are fixed source design references, not video/playback verification or permission to import their materials. The implementation and all art used are project-authored; no foreign shader, texture, audio or code is copied.

Native projectile ownership is unchanged. Shared pure geometry covers both fractional rendering and collision; one root ledger prevents five fingers multiplying damage on the same enemy. Draw/audio lifetime is client-only. On death, Down, item change or world unload the attack or its client resources are released. No new Encounter packet or authority rule is introduced.

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
