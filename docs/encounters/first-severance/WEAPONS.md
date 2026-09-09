---
doc_id: encounter.first-severance.weapons
document_type: spec
status: provisional
owners:
  - gameplay
  - art
last_reviewed: 2026-09-09
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

This section replaces the original sword/echo prototype. Internal item identity `NullRefrain`, the accepted Victory drop and all one-for-one exchanges are unchanged. Boss attacks, Raid timing, recovery, music and protocol23 are untouched. The other four forms follow the v2 specification below. The old sword projectile remains only as an unused legacy type; the item cannot fire it.

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

## Apparatus redesign — 0.2.30

The four ranged forms are now large, material-bearing weapon rituals rather than small icon sprites with thin lines. Existing item names/IDs, class identities, Work Bench exchanges, ammo/mana cost and base damage are retained. The accepted claw geometry is not retuned.

| Form | Current behavior |
|---|---|
| Null Refrain / 断唱・虚掌 | The giant alternating true-melee claws described above. Six-second charge and remote crush are unchanged. Presentation now enters/exits the same parked two-hand poses. |
| Pale Meridian / 蒼白の子午線 | Three large floating cannon bodies behind the player open hinged buttresses and fire from three shared physical muzzle coordinates. Each 12-tick use consumes at most one bullet, retaining 75% conservation. Its three needles launch after4/6/8 ticks and each carries one third of the use's budget. Sixth use totals2.1x and each needle can pierce three distinct roots; no needle hits one root three times. |
| Lacuna Testament / 欠落の遺言 | A two-cover codex opens inside an orbiting folio archive. Three large lenses fire from distinct matching coordinates after6/9/12 ticks. Uses20 ticks and18 base mana. First three casts use3x0.60 damage; fourth cast uses3x1.20 with larger release surfaces. Four-cast mean remains3x0.75 per cast. |
| Choir of the Unmade / 未成の聖歌隊 | Each 1-slot sentinel carries an articulated pipe/wing assembly. Over108 ticks each fires two staggered notes, then moves into a common organ formation for a shared third accent at tick88. Third notes retain1.4x damage per slot. Large shared crown/rings are ornamental, not extra damage. Normal minion sacrifice/manual targeting remain. |
| Last Witness / 最後の証人 | A much larger rotating triangular key trails a broad dark/violet fracture ribbon. Outbound/return shares remain0.70/0.30. A native stealth strike replaces its old3x0.20 echo budget with ONE0.60 triangular verdict: briefly follows the selected target, locks at16 ticks, converges, strikes at28–31 and dissipates by56. Radius185 triangle vs AABB is the real footprint, not a large square or each blade prop separately. |

Ranged/magic cast apparatus persists across repeated uses instead of being killed and recreated. A serial distinguishes rearming from repeated snapshots. It folds away after use stops, and is canceled on death/Down/incapacitation/item switch. Damage projectiles use native owner creation/replication, not new Encounter requests. Summon bodies stop attacking during Down; existing summoned slots are not forcibly deleted. Damage notes and verdicts terminate for unusable owners.

### Claw continuity correction

The old drawing jumped between a fixed off-hand `SwingPose(.91)`, a faded projectile-local active hand, and an unrelated idle pose whenever an attack projectile started/ended. New shared `PresentedHand` blends only the harmless windup/recovery to a common world-space parked pose. Both hands remain visible. During the live window, all finger joints match the unchanged collision pose exactly; player-root lag is not introduced. Mirroring follows facing. The attack's actual duration, growth, damage and live ticks do not change.

A single fractional render clock drives all five weapons. Flight centers interpolate between completed game ticks; persistent apparatus aim uses short-angle interpolation. Claw trails sample only the actual traversed arc rather than clamping many samples to one endpoint. Continuous tapered mesh strips replace disconnected line-sprite caps; dark volume, saturated glow and white core have separate widths. This addresses identified code discontinuities; user-reported stutter resolution still needs in-game confirmation, not a claim based on a benchmark.

### Presentation constraints

Claws keep their accepted original P3 artwork. Other forms use a new full-color2048x1024 assembly atlas: cannon, buttress, cover, folio lens, pipe, crown, triangular blade and relic. Each rigid part is transformed independently. New128x128 icons replace the previous32-color concept thumbnails for the other four forms. Source images and older exports remain preserved.

The export method in this session is project-owned P3 material composition plus independently authored geometry/PNG generation, **not a new image-model generation**. No new model output or model-version claim is made. The external recipe takes explicit input/output paths; exact assets/hashes/provenance are in `Assets/ATTRIBUTION.md`. No Calamity/HotOG/WotG art or shaders are imported.

Major attacks occupy hundreds of world pixels: three-barrel battery, folio/lens archive, concert organ, triangular verdict. Brightness is localized; there is no white-screen fill, forced zoom, real hitstop or input lock. Reduced Effects keeps major silhouettes and real strike footprints, but reduces trail intensity, ornamental folios, shards and shake. Native weapon audio uses a separate bounded Identifier group; existing Boss/music masters are unchanged. Procedural GPU objects are disposed on Mod unload; per-world impacts/voices are cleared on world unload. Sprite and mesh passes restore render state.

## Homing and synchronization

Acquire at1800px, retain at2100px, honor line of sight and chaseable targets. The projectile owner chooses targets through native projectile AI; replicas follow the chosen index. Manual minion targets take priority. Turn cap0.24rad per real tick, exponential speed response, and10-tick maximum predictive lead remain normalized by extraUpdates. Swept head collision prevents fast needles skipping a target; decorative wake width is not hit width. NPC segments use shared-root hit ledgers. These weapons do not damage PvP players.

The cooperative native projectile ownership model is unchanged; this is not an anti-cheat guarantee or server-owned Raid weapon rewrite. The encounter protocol stays23 because no Encounter DTO/ID changes. The native pose ExtraAI and content set changed: **all peers must update to the same0.2.30 build**, even though the Encounter protocol number did not change.

## Initial power budget — not measured DPS

Goal: approximately5–15% above a selected same-class Calamity2.2.4 endgame benchmark under matched gear and target conditions. Actual calibration belongs to user/Codex measurements. No finite design can guarantee that ratio against every final weapon, movement pattern and target type.

`RitualArmamentRules` centralizes initial damage seeds and a1.10 design factor. This factor is applied to **provisional project seeds**, not to an empirically measured Calamity DPS baseline. Do not describe it as verified10% superiority.

| Class | Initial base damage | Nominal raw output/sec |
|---|---:|---:|
| Melee |7700 |16500 in continuous claw contact; about18.2k including charged-crush occupation/refill |
| Ranged |2002 |11845, six-shot cycle, before ammunition contribution |
| Magic |2024 |13662, all three rays connect |
| Summon |968 |1828 per slot;18284 for10 slots |
| Rogue |9680 |14520, both shares connect, before stealth |

These are arithmetic budgets, not in-game expected DPS: no defense, crits, armor/accessories, attack speed, miss rate, target motion or native Rogue bonuses. Ranged piercing is not three hits on the same root. Rogue return is not another full-damage hit. Magic lenses and VFX counts cannot add unbudgeted damage. Adjust central seeds after selecting matched representative weapons; do not raise Boss HP to conceal reward imbalance.

## Provenance and engine seams

The old32-color concept-derived exports are retained as historical assets but no longer draw the four active weapon forms. The current P3-derived atlas/icons and independent ribbon PNG have separate exact attribution entries. Existing original P3 provenance remains authoritative for the material source. Current implementation/verification status is in [Status](../../STATUS.md).

Runtime target: pinned tModLoader2026.07.3.0 / source666f69962d3bdffde54fc14025f02634965b4e7c, Calamity2.2.4. Hook/reference research used official ModItem/ModProjectile/SoundStyle documentation and pinned public Calamity source1a8cebd27ec5615316b78f71973446b5528d2b78 (2.2.2), including RogueWeapon, ScarletDevil, Exoblade, Photoviscerator, Eternity and Endogenesis. That source is **not proven identical** to the2.2.4 runtime. No external implementation/asset is copied. The narrow Rogue bridge retains native RogueWeapon hooks and marks native stealth projectiles only inside Common/Compatibility/Calamity.

## Verification and handoff

Pure tests additionally cover every claw/idle seam, live joint equality, blend boundary continuity, three-muzzle budget conservation, fourth-cast budget accounting,40-slot choir schedules, and the bounded triangular verdict. Existing bounded easing, homing and codec tests remain. Existing domain/codec CI is not a full Mod build. User/Codex should build/load on the pinned environment and check all five forms, native stealth consumption, minion slots/targeting, texture pivots, projectile cancellation on Down/death and matched single-target damage. Do not mark those checks passed until observed.
