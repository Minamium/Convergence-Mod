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
