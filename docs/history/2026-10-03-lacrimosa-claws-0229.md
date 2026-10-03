---
doc_id: history.lacrimosa-claws-0229
document_type: evidence
status: historical
owners:
  - gameplay
  - art
last_reviewed: 2026-10-03
source_of_truth_for: []
aliases:
  - Lacrimosa's Claws 0.2.29 melee redesign
  - claw swipe cleanup 0.2.38
related_code: []
related_docs:
  - encounter.first-severance.weapons
  - project.status
---

# Lacrimosa's Claws before the 2026-10 refresh

Frozen text of the two claw sections that the [Lacrimosa's Claws refresh](../encounters/first-severance/WEAPONS.md#lacrimosas-claws--refresh-2026-10) replaced on 2026-10-03 (the 0.2.29 melee redesign and the 0.2.38 swipe cleanup), with relocated links. **It is not current behaviour or a test queue**: the item no longer reaches the swipe and crush projectiles described here, which stay in the code as unused legacy types until the weapon cleanup.

### Claw swipe cleanup — 0.2.38

Keep the accepted hands, finger highlights, luminous sweep and hit flash/rings. Normal swipes no longer emit radial line/shard sprays, including their normal-hit aftermath; their ribbon omits its dark opaque underlay. The palm's existing aperture and the entire right-click crush remain unchanged. The shared ribbon helper defaults to its old behavior for other weapons. Motion, hitboxes, damage, resources and audio are untouched.

### Lacrimosa's Claws — accepted melee redesign, 0.2.29

This section replaces the original sword/echo prototype. Internal item identity `NullRefrain` is unchanged; current acquisition is owned by [Curtainfall Treasure Box](../encounters/first-severance/WEAPONS.md#curtainfall-treasure-box). The other four forms follow the long-form ritual specification below. The old sword projectile remains only as an unused legacy type; the item cannot fire it. Weapon-only changes do not authorize Raid tuning.

**Left click:** alternate independently articulated left/right five-finger claws using the actual P3 rig material. The hand expands from0.68x to2.30x during the stroke, then retracts; the whole attack stays inside560 world pixels of the player. Base duration28ticks, bounded10–90 after native true-melee speed. Only the palm and swept finger capsules damage, once per logical NPC root per swipe. No homing echo projectiles: this is the user's replacement true-melee design. Calamity's registered `TrueMeleeDamageClass` is resolved through the compatibility adapter, without using its internal singleton.

**Right click (0.2.31):** one charge after360 real game ticks while holding a usable claw. Holding an attack also recharges; unequipping/incapacitation pauses it, execution pauses it, death/world entry clears it. The charge belongs to the player, so extra item copies cannot duplicate it. A click spends it once and fixes a world coordinate within1120px. A fresh right click while left-clicking prioritizes execution and cancels only the owner's current swipe. Gameplay-only input ignores UI/fullscreen map/unfocused/Downed use; the ordinary alternate-use path shares the same one-charge spend. Both hands emerge diagonally in5 ticks, decelerate/brace until11, then accelerate to impact at16; one4.2x ordinary-melee strike is active during16–20 and recovery ends at42. The166x132px axis-aligned damage ellipse is unchanged and forecast; the oblique hands are its presentation, not an enlarged rotated hitbox. No forced NPC/player movement, literal instant kill, invulnerability bypass, homing after target lock, or attack-speed reduction of the six-second charge. Native NPC defenses and damage hooks remain in effect.

The presentation uses native-resolution P3 palm/bone/talon regions, independently moving finger joints and layered violet/white crescents with negative-space interiors. The remote strike closes on a dark center before a vertical flare and ring release. Bright remnants never increase hit range. Both hands and major crescents remain under Reduced Effects; secondary shards and shake are reduced/disabled. Weapon sounds now use independent weapon masters and bounded voices; [Audio](../AUDIO_CUE_SHEET.md#weapon-only-foley) owns current choices. No global pause, forced zoom or white-screen fill.

`NullCantorClawMotion` owns the current melee budget and timing; the previous72-tick execution calculation predates the faster right-click score and is not current DPS. Measure actual contact with native armor/crit/gear/hooks before comparing endgame output. Do not change Boss HP to disguise a weapon balance problem.

The native128x128 RGBA inventory icon is composed from the existing original P3 hand and palm atlas, not cropped from the concept board and not32-color quantized. The original atlas remains unchanged; runtime limbs keep the native source detail. Its exact derivative record is in `Assets/ATTRIBUTION.md`.

Design references: the user's annotated P3-arm sketch and approved dual-claw board; Calamity [Earth's growing true-melee silhouette](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/EarthHoldout.cs), [Ark's staged release](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/ArkOfTheCosmos_BlastAttack.cs), HotOG [Parasanguine's articulated cadence](https://github.com/TohruKobayashi/CalamityHunt/blob/5c2825e64c660384500decafa7702793dc4b48dc/Content/Projectiles/Weapons/Melee/ParasanguineHeld.cs), and WotG [Avatar's finger-chain rendering](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.RightArm.cs). These are fixed source design references, not video/playback verification or permission to import their materials. The implementation and all art used are project-authored; no foreign shader, texture, audio or code is copied.

Native projectile ownership is unchanged. Shared pure geometry covers both fractional rendering and collision; one root ledger prevents five fingers multiplying damage on the same enemy. Draw/audio lifetime is client-only. On death, Down, item change or world unload the attack or its client resources are released. No new Encounter packet or authority rule is introduced.
