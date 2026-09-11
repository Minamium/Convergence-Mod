---
doc_id: encounter.first-severance.weapons
document_type: spec
status: provisional
owners:
  - gameplay
  - art
last_reviewed: 2026-09-11
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

Current mechanics are below; [Audio](../../AUDIO_CUE_SHEET.md) owns the active sound masters and removal of the claw's overlapping swipe after-sound. Versioned labels identify when a design arrived, not the current package/protocol: those belong to [Status](../../STATUS.md).

## Claw swipe cleanup — 0.2.38

Keep the accepted hands, finger highlights, luminous sweep and hit flash/rings. Normal swipes no longer emit radial line/shard sprays, including their normal-hit aftermath; their ribbon omits its dark opaque underlay. The palm's existing aperture and the entire right-click crush remain unchanged. The shared ribbon helper defaults to its old behavior for other weapons. Motion, hitboxes, damage, resources and audio are untouched.

## Null Cantor's Claws — accepted melee redesign, 0.2.29

This section replaces the original sword/echo prototype. Internal item identity `NullRefrain`, the accepted Victory drop and all one-for-one exchanges are unchanged. The other four forms follow the long-form ritual specification below. The old sword projectile remains only as an unused legacy type; the item cannot fire it. Weapon-only changes do not authorize Raid tuning.

**Left click:** alternate independently articulated left/right five-finger claws using the actual P3 rig material. The hand expands from0.68x to2.30x during the stroke, then retracts; the whole attack stays inside560 world pixels of the player. Base duration28ticks, bounded10–90 after native true-melee speed. Only the palm and swept finger capsules damage, once per logical NPC root per swipe. No homing echo projectiles: this is the user's replacement true-melee design. Calamity's registered `TrueMeleeDamageClass` is resolved through the compatibility adapter, without using its internal singleton.

**Right click (0.2.31):** one charge after360 real game ticks while holding a usable claw. Holding an attack also recharges; unequipping/incapacitation pauses it, execution pauses it, death/world entry clears it. The charge belongs to the player, so extra item copies cannot duplicate it. A click spends it once and fixes a world coordinate within1120px. A fresh right click while left-clicking prioritizes execution and cancels only the owner's current swipe. Gameplay-only input ignores UI/fullscreen map/unfocused/Downed use; the ordinary alternate-use path shares the same one-charge spend. Both hands emerge diagonally in5 ticks, decelerate/brace until11, then accelerate to impact at16; one4.2x ordinary-melee strike is active during16–20 and recovery ends at42. The166x132px axis-aligned damage ellipse is unchanged and forecast; the oblique hands are its presentation, not an enlarged rotated hitbox. No forced NPC/player movement, literal instant kill, invulnerability bypass, homing after target lock, or attack-speed reduction of the six-second charge. Native NPC defenses and damage hooks remain in effect.

The new presentation uses native-resolution P3 palm/bone/talon regions, independently moving finger joints, broad layered violet/white crescents with negative-space interiors, connected fingertip wakes, dislodged dark shards and expanding broken pressure rings. The remote strike closes on a dark center before a vertical flare and ring release. Bright remnants never increase hit range. Both hands and major crescents remain under Reduced Effects; secondary shards and shake are reduced/disabled. Weapon sounds use a separate identifier and tracked, bounded voices; P3 masters are reused unchanged. No global pause, forced zoom or white-screen fill.

`NullCantorClawMotion` owns the current melee budget and timing; the previous72-tick execution calculation predates the faster right-click score and is not current DPS. Measure actual contact with native armor/crit/gear/hooks before comparing endgame output. Do not change Boss HP to disguise a weapon balance problem.

The native128x128 RGBA inventory icon is composed from the existing original P3 hand and palm atlas, not cropped from the concept board and not32-color quantized. The original atlas remains unchanged; runtime limbs keep the native source detail. Its exact derivative record is in `Assets/ATTRIBUTION.md`.

Design references: the user's annotated P3-arm sketch and approved dual-claw board; Calamity [Earth's growing true-melee silhouette](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/EarthHoldout.cs), [Ark's staged release](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/ArkOfTheCosmos_BlastAttack.cs), HotOG [Parasanguine's articulated cadence](https://github.com/TohruKobayashi/CalamityHunt/blob/5c2825e64c660384500decafa7702793dc4b48dc/Content/Projectiles/Weapons/Melee/ParasanguineHeld.cs), and WotG [Avatar's finger-chain rendering](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.RightArm.cs). These are fixed source design references, not video/playback verification or permission to import their materials. The implementation and all art used are project-authored; no foreign shader, texture, audio or code is copied.

Native projectile ownership is unchanged. Shared pure geometry covers both fractional rendering and collision; one root ledger prevents five fingers multiplying damage on the same enemy. Draw/audio lifetime is client-only. On death, Down, item change or world unload the attack or its client resources are released. No new Encounter packet or authority rule is introduced.

## Scope and acquisition

User-approved concept: obsidian/ivory/aged-gold ritual machinery, hollow apertures and physical material opening before a bright release. This development change touches weapons only. Boss behavior, loot execution, mechanics, recovery and music are unchanged.

Accepted Victory still drops one Null Refrain for each frozen-roster participant as ordinary shared world items. At a Work Bench, any one armament converts into any other, consuming exactly one input and producing one output. The20 directed recipes neither multiply rewards nor allow pre-Raid crafting. No recipes use vanilla materials alone. The prior statement that the reward has no recipe is superseded only for these exchanges.

## Long-form non-melee rituals — 0.2.34

The previous short cast/volley cycles are replaced, not merely slowed down. Each weapon has a **multi-second construction score**, with quick individual arrivals, a brief braking/tension beat, rapid final commitment, and continuous recovery. Do not compress the entire construction into one item-use animation when tuning the individual accents. `RitualGrandScore` owns real-tick milestones; attack-speed bonuses do not compress this score.

### Magic — Lacuna Testament

Hold the trigger. One sigil begins firing after0.25s. Six more remain deployed above/below the player at108/183/233/269/296/316 ticks (successive gaps1.8/1.25/0.83/0.6/0.45/0.33s). Each independently fires a small seeking bolt every38 ticks from its actual drawn aperture. At340–362 the whole array accelerates into one large iris;362–410 is a0.8s charge with a quiet tension beat and sharply accelerating final compression.

At410 ticks (6.83s from start), **one persistent beam**, not repeated short projectiles, opens over7 ticks and stays on until release, empty mana, item change or incapacitation. It follows the player's moving hand; aim can turn continuously, capped at0.033rad/tick during sustain (0.075 beforehand). The sigil formation's initial facing is retained so turning across vertical cannot flip the entire array. Range2600px and full collision width116px. One hit per NPC root every10 real ticks; segmented NPCs cannot multiply the beam budget.

Native item use pays8 base mana once. Construction pays35% of the modified item mana cost, rounded up, every30 ticks. Sustain pays the modified item cost every8 ticks, including the release beat. Mana regeneration is delayed during use. Automatic missing-mana potion activation is explicitly blocked for this channel; manual potions remain normal. Zero-cost equipment modifiers are honored. Current weapon damage is reevaluated during sustain, including Mana Sickness. Release stops damage immediately and leaves only20 ticks of fading apparatus. A fresh press starts a fresh ritual.

### Ranged — Pale Meridian

Hold to maintain a siege battery. Five separate cannon bodies arrive at0/90/162/213/246 ticks. The first shoots during construction; cadence accelerates as bodies join. Each actual shot selects one muzzle, not five copies of a full damage budget. At282–300 the deployed rails snap into a compact overlapping battery; a48-tick pressure/charge beat precedes overdrive at348 (5.8s). The body, telescoping throat, muzzle and recoil stay connected.

Overdrive maintains physical homing needles at one every3 real ticks, alternating among five muzzles; every36 ticks a heavier penetrating needle replaces an ordinary shot. This is a kinetic crossfire, not the Magic beam recolored. Release/item change/incapacitation/out-of-ammo ends it. One native `PickAmmo` call per actual shot retains ammunition damage, knockback and conservation hooks. The initial harmless holdout consumes no bullet.75% conservation remains. Aim cap0.055rad/tick in overdrive.

### Summon — Choir of the Unmade

Each use summons one persistent,1-slot chorister. The oldest stable native identity conducts one shared target/score; adding voices does not restart it. Normal minion targeting, sacrifice and changing held weapons remain supported.

An11-second concert: independent seeking notes and progressive organ tiers for276 ticks;48-tick assembly;48-tick pressure/charge; **one shared three-second chorus beam** during372–552; disassembly/rest to660. Every living voice contributes its current damage to that one beam; individual notes stop during the chorus. No extra beam per slot. The chorus binds to its conductor's owner+identity, not a reusable projectile slot. Removing the conductor cancels that beam; a remaining voice takes over the next concert. Losing a valid target or becoming Downed/incapacitated cancels the score. Down does not delete the summoned slots.

The beam turns toward the native selected target at0.045rad/tick, reaches2000px, has92px full width and12-tick root immunity. The crown is physical moving art; all ornamental seal/pipe counts remain bounded independently of minion count.

### Rogue — Last Witness

Hold to enroll six witnesses at29-tick intervals. Each casts one small seeking shard during construction. At174–194 they rapidly fold into one heavy relic;24 ticks of braking/compression precede a single amplified returning blade at218. Recovery completes at282 (4.7s total). Releasing before commitment cancels; holding can begin another full score after recovery. Native Calamity RogueWeapon hooks determine initial damage/stealth once; the final blade inherits the stored stealth flag. A stealth final blade retains the target-locking triangular verdict. The early fragments do not each receive another stealth execution.

## Presentation and native ownership

Four new **text-only image generations** replace the non-melee icons and supply512px runtime apparatus artwork; no previous image was passed as input. The owner approved the built-in generator despite its unexposed backend model: do not label the results GPT Image2.5.128px inventory exports fit a116px maximum opaque envelope, inspected at40px as well. Original generated PNGs and all predecessor assets are preserved. Exact prompts, export procedure and provenance are in [Attribution](../../../Assets/ATTRIBUTION.md#ritual-grand-apparatus-v3--2026-09-09).

Magic has persistent flowing plasma with a full-width luminous throat, rotating nested irises, inward energy flow and one launch accent. Ranged has expanding machinery, connected recoil and continuous induction/motor sound. Choir layers voices/towers before one sustained organ release. The three original four-second sound beds loop continuously; damage ticks do not restart launch sounds. Their voices follow projectile lifetime and stop on cancel/unload. `ActiveSound.Volume` is a multiplier of `Style.Volume`; dynamic fading must not square the intended gain.

Render clocks and short-angle interpolation connect game ticks. Transparent, filamentary layers remain inside a readable continuous beam body, not independent decorative gaps. Reduced Effects reduces density/intensity and disables local shake; no fullscreen white pulses, forced zoom, time manipulation or UI/input ownership is introduced. The accepted melee geometry, controls, art and charge are unchanged.

Owner clients alone sample mouse/channel input, spend mana/ammo and create child projectiles, using Terraria's existing cooperative weapon replication. Other peers render/read native projectile state. Raid outcomes remain server-authoritative; this is not a new anti-cheat guarantee. Held controllers cancel on item change/death/Down/CC; launched ordinary projectiles retain normal flight lifetimes and terminate for unusable owners. World unload clears voices; Mod unload disposes material/mesh resources. No weapon-specific Encounter packet is introduced; all peers still need matching current content/protocol as recorded in [Status](../../STATUS.md).

## Initial power budget — not measured DPS

The unchanged seeds in `RitualArmamentRules` are provisional, not measured Calamity baselines. Magic construction bolts carry0.55x base damage; sustain hits carry2x every10 ticks (24288 nominal raw damage/sec at2024 base). Ranged warmup needles carry0.95x; overdrive carries0.62x, replacing one in12 with1.15x (about26593 raw/sec at2002 base, excluding ammo). Choir ordinary notes carry0.85x; shared chorus hits carry1.05x the sum of living voices every12 ticks (5082 raw/sec per968-damage voice **during the chorus**, not averaged over rest). Rogue pays six0.28x early shards plus one5.4x final returning-blade budget over4.7s, before native stealth; that blade splits0.70/0.30 outbound/return.

These are arithmetic bounds before defense, crits, armor/accessories, misses, movement and class hooks—not claims of endgame balance or measured DPS. The goal of a modest improvement over selected same-class final equipment needs matched in-game measurements. Do not change Boss HP to hide weapon imbalance.

## Prior-art findings and engine seams

Research question: how do endgame weapons sustain a developing attack rather than replay a short cosmetic cycle? Scope searched: Yharim's Crystal, Drataliornus and Midnight Sun UFO holdout/beam/score code in the [official Calamity public repository](https://github.com/CalamityTeam/CalamityModPublic), accessed2026-09-09. Maintainer authority is its CalamityTeam official release mirror/readme. Pinned commit1a8cebd27ec5615316b78f71973446b5528d2b78 declares2.2.2 in [build.txt](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/build.txt); installed2.2.4 is not proven source-identical. The [license](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md) permits reference but restricts redistribution. All paths below were retrieved successfully at that commit; no code, textures, music or recordings were copied.

| Verified source / member | Observation | Independent Convergence decision |
|---|---|---|
| [YharimsCrystalPrism.AI / ShouldConsumeMana](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Magic/YharimsCrystalPrism.cs) | Persistent holdout creates its beams once, charges over180 ticks, spends mana on a changing cadence and updates aim/current damage | Adopt persistent lifetime/resource ownership; use our own seven-sigil macro score and a single direct beam, not six copied beams |
| [DrataliornusBow.AI](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Ranged/DrataliornusBow.cs) |360-tick spin-up stages shorten shot delay and change fully-spun-up shots; native PickAmmo is called for actual firing | Adopt a multi-second reached state; independently stage five siege bodies and a bounded one-shot crossfire |
| [MidnightSunUFO.AI](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Summon/MidnightSunUFO.cs) |330-tick score alternates a mobile gun phase with repositioning and a beam phase | Adopt distinct summon action categories; use one identity-bound concert and combined damage instead of beam duplication per minion |

Official pinned tML2026.07.3.0 source666f69962d3bdffde54fc14025f02634965b4e7c: [Player.TML.CheckMana](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.TML.cs) confirms explicit payment and blocking missing-mana effects; [ActiveSound patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Audio/ActiveSound.cs.patch) confirms loop callbacks and multiplicative volume. [Current Player API](https://docs.tmodloader.net/docs/stable/class_player.html) documents PickAmmo's one-shot consumption and native bonuses; unlike the fixed sources it is moving guidance.

Inference needing playtest: staged persistent bodies and reached sustain states should better communicate growing power than short loops. Compilation cannot establish aesthetic quality, multiplayer continuity or FPS. Required focused checks are macro-beat boundaries/resource cadence plus user-owned release/empty-resource/item-switch/Down,1 vs multiple minions/retarget/sacrifice, native stealth, aim continuity and reduced-effects playback. Current results belong only to [Status](../../STATUS.md). Previous0.2.30/0.2.33 implementation evidence remains linked there and in Git history, not active instructions.
