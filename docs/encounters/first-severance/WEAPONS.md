---
doc_id: encounter.first-severance.weapons
document_type: spec
status: provisional
owners:
  - gameplay
  - art
last_reviewed: 2026-09-14
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

# Requiem of the Hollow Doll — Ritual Armaments

Current mechanics are below; [Audio](../../AUDIO_CUE_SHEET.md) owns the active sound masters and removal of the claw's overlapping swipe after-sound. Versioned labels identify when a design arrived, not the current package/protocol: those belong to [Status](../../STATUS.md).

## Claw swipe cleanup — 0.2.38

Keep the accepted hands, finger highlights, luminous sweep and hit flash/rings. Normal swipes no longer emit radial line/shard sprays, including their normal-hit aftermath; their ribbon omits its dark opaque underlay. The palm's existing aperture and the entire right-click crush remain unchanged. The shared ribbon helper defaults to its old behavior for other weapons. Motion, hitboxes, damage, resources and audio are untouched.

## Curtainfall Treasure Box

**閉幕の宝箱 / Curtainfall Treasure Box** (`DollTreasureBox`) replaces the direct weapon drop in every difficulty. Accepted Victory creates the same frozen-party-count number of shared world drops at the Core. Right-click consumes one box and draws **one weapon**, uniformly from `NullRefrain`, `PaleMeridian`, `LacunaTestament`, `ChoirOfTheUnmade`, `LastWitness`: **20% each**, independent of Luck. Duplicate draws are possible; there is no guaranteed collection cycle. No weapon-to-weapon exchange recipes remain. The Doll is excluded from the box. There is no additional NPC death reward or private inventory grant.

Uniform selection uses `ItemDropRule.OneFromOptionsNotScalingWithLuck(1, options)` ([v2026.07 API](https://docs.tmodloader.net/docs/stable/class_item_drop_rule.html), checked 2026-09-12; package compilation checks the installed signature). The native `CanRightClick`/`ModifyItemLoot` container path owns consumption and contents; do not also spawn a weapon in `RightClick`. No `ItemID.Sets.BossBag` flag, since this is an all-difficulty treasure box without injected vanilla developer-armour drops. API checked 2026-09-12 against pinned tML [ModItem](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModItem.cs) and [ExampleMod bag](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod/Content/Items/Consumables/MinionBossBag.cs); independently implemented, no copied art/code.

## Lacrimosa's Claws — accepted melee redesign, 0.2.29

This section replaces the original sword/echo prototype. Internal item identity `NullRefrain` is unchanged; current acquisition is owned by [Curtainfall Treasure Box](#curtainfall-treasure-box). The other four forms follow the long-form ritual specification below. The old sword projectile remains only as an unused legacy type; the item cannot fire it. Weapon-only changes do not authorize Raid tuning.

**Left click:** alternate independently articulated left/right five-finger claws using the actual P3 rig material. The hand expands from0.68x to2.30x during the stroke, then retracts; the whole attack stays inside560 world pixels of the player. Base duration28ticks, bounded10–90 after native true-melee speed. Only the palm and swept finger capsules damage, once per logical NPC root per swipe. No homing echo projectiles: this is the user's replacement true-melee design. Calamity's registered `TrueMeleeDamageClass` is resolved through the compatibility adapter, without using its internal singleton.

**Right click (0.2.31):** one charge after360 real game ticks while holding a usable claw. Holding an attack also recharges; unequipping/incapacitation pauses it, execution pauses it, death/world entry clears it. The charge belongs to the player, so extra item copies cannot duplicate it. A click spends it once and fixes a world coordinate within1120px. A fresh right click while left-clicking prioritizes execution and cancels only the owner's current swipe. Gameplay-only input ignores UI/fullscreen map/unfocused/Downed use; the ordinary alternate-use path shares the same one-charge spend. Both hands emerge diagonally in5 ticks, decelerate/brace until11, then accelerate to impact at16; one4.2x ordinary-melee strike is active during16–20 and recovery ends at42. The166x132px axis-aligned damage ellipse is unchanged and forecast; the oblique hands are its presentation, not an enlarged rotated hitbox. No forced NPC/player movement, literal instant kill, invulnerability bypass, homing after target lock, or attack-speed reduction of the six-second charge. Native NPC defenses and damage hooks remain in effect.

The presentation uses native-resolution P3 palm/bone/talon regions, independently moving finger joints and layered violet/white crescents with negative-space interiors. The remote strike closes on a dark center before a vertical flare and ring release. Bright remnants never increase hit range. Both hands and major crescents remain under Reduced Effects; secondary shards and shake are reduced/disabled. Weapon sounds now use independent weapon masters and bounded voices; [Audio](../../AUDIO_CUE_SHEET.md#weapon-only-foley) owns current choices. No global pause, forced zoom or white-screen fill.

`NullCantorClawMotion` owns the current melee budget and timing; the previous72-tick execution calculation predates the faster right-click score and is not current DPS. Measure actual contact with native armor/crit/gear/hooks before comparing endgame output. Do not change Boss HP to disguise a weapon balance problem.

The native128x128 RGBA inventory icon is composed from the existing original P3 hand and palm atlas, not cropped from the concept board and not32-color quantized. The original atlas remains unchanged; runtime limbs keep the native source detail. Its exact derivative record is in `Assets/ATTRIBUTION.md`.

Design references: the user's annotated P3-arm sketch and approved dual-claw board; Calamity [Earth's growing true-melee silhouette](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/EarthHoldout.cs), [Ark's staged release](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/ArkOfTheCosmos_BlastAttack.cs), HotOG [Parasanguine's articulated cadence](https://github.com/TohruKobayashi/CalamityHunt/blob/5c2825e64c660384500decafa7702793dc4b48dc/Content/Projectiles/Weapons/Melee/ParasanguineHeld.cs), and WotG [Avatar's finger-chain rendering](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.RightArm.cs). These are fixed source design references, not video/playback verification or permission to import their materials. The implementation and all art used are project-authored; no foreign shader, texture, audio or code is copied.

Native projectile ownership is unchanged. Shared pure geometry covers both fractional rendering and collision; one root ledger prevents five fingers multiplying damage on the same enemy. Draw/audio lifetime is client-only. On death, Down, item change or world unload the attack or its client resources are released. No new Encounter packet or authority rule is introduced.

## Scope and acquisition

User-approved concept: obsidian/ivory/aged-gold ritual machinery, hollow apertures and physical material opening before a bright release. This development change touches weapons only. Boss behavior, loot execution, mechanics, recovery and music are unchanged.

The [treasure box](#curtainfall-treasure-box) owns acquisition. The former 20 directed class exchanges and Choir ↔ Doll exchange are removed. Existing owned weapons remain usable; no inventory migration or deletion occurs.

## Long-form non-melee rituals — 0.2.34

The previous short cast/volley cycles are replaced, not merely slowed down. Each weapon has a **multi-second construction score**, with quick individual arrivals, a brief braking/tension beat, rapid final commitment, and continuous recovery. Do not compress the entire construction into one item-use animation when tuning the individual accents. `RitualGrandScore` owns real-tick milestones; attack-speed bonuses do not compress this score.

### Magic — Lacuna Testament

Hold the trigger. One sigil begins firing after0.25s. Six more remain deployed above/below the player at108/183/233/269/296/316 ticks (successive gaps1.8/1.25/0.83/0.6/0.45/0.33s). Each independently fires a small seeking bolt every38 ticks from its actual drawn aperture. At340–362 the whole array accelerates into one large iris;362–410 is a0.8s charge with a quiet tension beat and sharply accelerating final compression.

At410 ticks (6.83s from start), **one persistent beam**, not repeated short projectiles, opens over7 ticks and stays on until release, empty mana, item change or incapacitation. It follows the player's moving hand; aim can turn continuously, capped at0.033rad/tick during sustain (0.075 beforehand). The sigil formation's initial facing is retained so turning across vertical cannot flip the entire array. Range2600px and full collision width116px. One hit per NPC root every10 real ticks; segmented NPCs cannot multiply the beam budget.

Native item use pays8 base mana once. Construction pays35% of the modified item mana cost, rounded up, every30 ticks. Sustain pays the modified item cost every8 ticks, including the release beat. Mana regeneration is delayed during use. Automatic missing-mana potion activation is explicitly blocked for this channel; manual potions remain normal. Zero-cost equipment modifiers are honored. Current weapon damage is reevaluated during sustain, including Mana Sickness. Release stops damage immediately and leaves only20 ticks of fading apparatus. A fresh press starts a fresh ritual.

### Ranged — Pale Meridian

Hold to assemble **one siege gun**, not an array of complete guns. One physical receiver arrives first; four small breech/rail components dock at90/162/213/246 ticks with rapid approach and a braked seating beat. The initial gun shoots during construction; cadence accelerates as components lock. At282–300 the mechanism compresses, then a48-tick pressure charge precedes overdrive at348 (5.8s). One barrel/muzzle remains attached through recoil; no full-gun duplication or large player-centered crown.

Overdrive maintains physical homing needles at one every3 real ticks, using the single assembled muzzle; every36 ticks a heavier penetrating needle replaces an ordinary shot. This is physical needle fire, not the Magic beam recolored. Release/item change/incapacitation/out-of-ammo ends it. One native `PickAmmo` call per actual shot retains ammunition damage, knockback and conservation hooks. The initial harmless holdout consumes no bullet.75% conservation remains. Aim cap0.055rad/tick in overdrive.

### Summon — Choir of the Unmade

Each use summons one persistent,1-slot chorister. The oldest stable native identity conducts one shared target/score; adding voices does not restart it. Normal minion targeting, sacrifice and changing held weapons remain supported.

An11-second concert: independent seeking notes and progressive organ tiers for276 ticks;48-tick assembly;48-tick pressure/charge; **one shared three-second chorus beam** during372–552; disassembly/rest to660. Every living voice contributes its current damage to that one beam; individual notes stop during the chorus. No extra beam per slot. The chorus binds to its conductor's owner+identity, not a reusable projectile slot. Removing the conductor cancels that beam; a remaining voice takes over the next concert. Losing a valid target or becoming Downed/incapacitated cancels the score. Down does not delete the summoned slots.

The beam turns toward the native selected target at0.045rad/tick, reaches2000px, has92px full width and12-tick root immunity. The crown is physical moving art; all ornamental seal/pipe counts remain bounded independently of minion count.

### Rogue — Last Witness

Hold to load one suspended execution relic with six small pressure/cut beats at29-tick intervals. Each still emits one small seeking shard from the real central apparatus, rather than another full weapon on an orbit. At174–194 its edge loads inward;24 ticks of braking/compression precede a single amplified returning blade at218. Flight keeps the physical blade legible with a narrow textured wake instead of a broad spinning light sheet. Recovery completes at282 (4.7s total). Releasing before commitment cancels; holding can begin another full score after recovery. Native Calamity RogueWeapon hooks determine initial damage/stealth once; the final blade inherits the stored stealth flag. A stealth final blade retains the target-locking triangular verdict. The early fragments do not each receive another stealth execution.

## Presentation and native ownership

Four new **text-only image generations** replace the non-melee icons and supply512px runtime apparatus artwork; no previous image was passed as input. The owner approved the built-in generator despite its unexposed backend model: do not label the results GPT Image2.5.128px inventory exports fit a116px maximum opaque envelope, inspected at40px as well. Original generated PNGs and all predecessor assets are preserved. Exact prompts, export procedure and provenance are in [Attribution](../../../Assets/ATTRIBUTION.md#ritual-grand-apparatus-v3--2026-09-09).

The original Luminance-managed `ArmamentEnergy` material replaces flat weapon strips with connected white energy, violet dark folds and flowing noise. Claw swipe/rogue trails and Magic/Choir/Doll beams share it, retaining their accepted geometry and attack clocks. Engraved, interlaced magic seals replace plain rings/ticks; physical apparatus artwork remains. Bolts receive luminous nuclei/wakes, and source vents/impacts receive the accepted Raid pressure/flare treatment. Magic has persistent flowing plasma with a full-width luminous throat, inward energy flow and one launch accent. Ranged has one progressively assembled receiver, connected recoil and continuous induction/motor sound. Choir layers voices/towers before one sustained organ release. The three original four-second sound beds loop continuously; damage ticks do not restart launch sounds. Their voices follow projectile lifetime and stop on cancel/unload. `ActiveSound.Volume` is a multiplier of `Style.Volume`; dynamic fading must not square the intended gain.

Render clocks and short-angle interpolation connect game ticks. Transparent, filamentary layers remain inside a readable continuous beam body, not independent decorative gaps. Reduced Effects reduces density/intensity and disables local shake; no fullscreen white pulses, forced zoom, time manipulation or UI/input ownership is introduced. The accepted melee geometry, controls, articulated art and charge are unchanged. Its five wide additive sheets are replaced with short separated fingertip filaments, so they do not fuse into a white crescent. The new physical cut/impact audio emphasizes low/mid weight and a brief attached air tail. Do not restore radial trail lines, black impact masses or a separate lingering swipe note.

Owner clients alone sample mouse/channel input, spend mana/ammo and create child projectiles, using Terraria's existing cooperative weapon replication. Other peers render/read native projectile state. Raid outcomes remain server-authoritative; this is not a new anti-cheat guarantee. Held controllers cancel on item change/death/Down/CC; launched ordinary projectiles retain normal flight lifetimes and terminate for unusable owners. World unload clears voices; Mod unload disposes material/mesh resources. No weapon-specific Encounter packet is introduced; all peers still need matching current content/protocol as recorded in [Status](../../STATUS.md).

## Video-driven playback correction

The [2026-09-14 recording/source analysis](../../research/2026-09-14-doll-playtest-video.md) distinguishes inspected reference frames from numeric audio evidence. Default-extraUpdates weapon and Doll PostAI hooks now run at native `numUpdates == -1` exactly once per real tick; the old zero-only guard skipped normal audio/interpolation updates. Bounded `RitualWeapon event=AudioVoice` diagnostics check actual voice acceptance, not only an asset path. The [audio sheet](../../AUDIO_CUE_SHEET.md#weapon-only-foley) owns the 12 remixed Claw/Ranged/Rogue/contact masters and unchanged other sounds. No reference-game audio, sprite or shader is imported.

## Initial power budget — not measured DPS

The unchanged seeds in `RitualArmamentRules` are provisional, not measured Calamity baselines. Magic construction bolts carry0.55x base damage; sustain hits carry2x every10 ticks (24288 nominal raw damage/sec at2024 base). Ranged warmup needles carry0.95x; overdrive carries0.62x, replacing one in12 with1.15x (about26593 raw/sec at2002 base, excluding ammo). Choir ordinary notes carry0.85x; shared chorus hits carry1.05x the sum of living voices every12 ticks (5082 raw/sec per968-damage voice **during the chorus**, not averaged over rest). Rogue pays six0.28x early shards plus one5.4x final returning-blade budget over4.7s, before native stealth; that blade splits0.70/0.30 outbound/return.

These are arithmetic bounds before defense, crits, armor/accessories, misses, movement and class hooks—not claims of endgame balance or measured DPS. The goal of a modest improvement over selected same-class final equipment needs matched in-game measurements. Do not change Boss HP to hide weapon imbalance.

## Doll companion — The Unbroken Promise

**ほどけない約束 / The Unbroken Promise** (`DollCovenant`) is an additional summon item, not a replacement for Choir of the Unmade. **Craft at a Work Bench using one of each of all five box weapons**, consuming all five and producing one Promise. No reverse recipe and no free Doll drop. The box options and recipe ingredients use the same `RitualArmamentItems.RewardTypes()` list so they cannot drift.

- Native `minionSlots = 10`, matching staff metadata and a capacity check. At least 10 maximum slots are required; only one Doll per owner. Native sacrifice replaces other sacrificial minions if needed. No slots, invulnerability or Raid participation are granted.
- Grounded follow uses gravity, collision/platform handling and a jump over small obstacles. **Any mounted owner, airborne owner (including horizontal flight/zero-velocity hover), or inverted gravity keeps the Doll broom-riding beside the owner.** Zero vertical velocity alone never proves grounding: three native solid-point probes below the owner's feet check support, including platforms. Dismounted, supported and vertically settled for six ticks permits landing only near the follow position and outside solid tiles. Large gaps/stuck paths still switch to broom catch-up; distant owners trigger an owner-synchronized teleport. Existing 16 flight/casting cels and brief pose blend mount/dismount the broom; attacks continue from its seated casting poses. The local owner changes native projectile `ai[2]` and requests synchronization on each transition; peers consume that mode, not independent mount guesses.
- Right-click enemy targeting is the ordinary minion targeting contract. Three irregular circle births fire violet needles, then merge toward the hand, brake briefly and accelerate into a **continuous purple Lacuna-style beam**. Its live width and fade share collision timing; native local immunity and logical-NPC-root suppression prevent duplicate segmented hits. Timing, damage scales, range and frame bounds have one code owner: `DollCompanionRules`; nominal pre-defense output is a development seed, not measured DPS. Holding the 10-slot minion does not spend ongoing mana.
- Owner-client selects targets/mode and creates child projectiles through native Terraria projectile replication, as the other weapons do. Remote peers/server consume these native objects, not encounter packets. This is not server-side anti-cheat validation. The parent is owner/identity-bound, and children expire when the exact parent/buff disappears or the owner is Downed/dead/disabled. Down suspends attacks while retaining the companion; normal death, dismissal and native sacrifice remove it. No effect on Raid roster, Stack counts, Ready, revive or all-Down defeat.
- Actual pixel cels: original 36 × 48×64 ground/legacy poses retained, plus `DollBroom.png` with 16 × 96×80 flight/casting cels. Foot anchoring/palette reduction are mechanical exports of generated drawings. Code adds breath/tilt, target-facing, frame selection, continuous circle/beam materials and event-bound Raid-derived sounds. Original stage NPC and its capture remain separate. [Asset recipe](../../../tools/asset_recipes/doll_presentation_0257.json) owns the generation prompts and export contract.
- Narrow API check (2026-09-12, existing version-matrix target): [Mount.Active](https://docs.tmodloader.net/docs/stable/class_mount.html) identifies actual mounting, not flight capability; [Collision.IsWorldPointSolid](https://docs.tmodloader.net/docs/stable/class_collision.html) supplies point support with platforms included. The [pinned Collision patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Collision.cs.patch) extends platform handling to Mod tiles. These are read-only support observations, not player movement or collision mutation. Native slopes/platforms still require the scoped playtest below.
- User-owned smoke: summon at 9 vs 10 slots, sacrifice and coexistence with spare slots, stairs/platforms/flight/teleport, right-click retarget, attack/dismiss/Down cleanup, and a second peer observing one Doll with matching shots. No claim of actual in-game readability, DPS or network smoothness from build/CPU preview alone.

While flying, the Doll leaves up to 12 purple twinkles at its recent world positions (six in Reduced Effects), fading over 36 ticks. History resets on teleport/landing and is client-owned per projectile; it creates no combat particles or network state. Existing broom/cast cels are retained. Its seals and sustained beam use the same updated weapon materials.

## Weapon sound and ten-slot companion references

Accessed 2026-09-12. Same pinned Calamity source/version/license caveat as the survey below (public 2.2.2 source, installed 2.2.4); tML commit `666f69962d3bdffde54fc14025f02634965b4e7c` targets the installed 2026.07 family/.NET8/C#12. This was a source/API survey, not a listening comparison or a claim to reproduce another weapon's timbre.

| Verified primary source | Observation and independent decision |
|---|---|
| [Photoviscerator](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/Items/Weapons/Ranged/Photoviscerator.cs) / [holdout](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Ranged/PhotovisceratorHoldout.cs) | Separate use/hit sounds; the holdout tracks position and pitch, renews the sustained firing sound on its own cadence, stops it on mode change. Adopt separate event/body ownership; use our own true periodic bed, not a sound per projectile hit or a copied renewal interval |
| [SubsumingVortex](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/Items/Weapons/Magic/SubsumingVortex.cs) | Native Item84 casting plus a separate custom explosion. Adopt distinct casting/release roles; do not reuse Boss explosion files for all weapons |
| [CosmicImmaterializer](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/Items/Weapons/Summon/CosmicImmaterializer.cs) | Staff metadata/capacity 10, one owned minion, originalDamage, Item60 summon sound. Independently pair 10-slot item/actor contracts; our Doll moves and attacks with its own cels and score |
| [Official ExampleSimpleMinion](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod/Content/Projectiles/Minions/ExampleSimpleMinion.cs) | Buff/death lifetime, minion slots/sacrifice flags, owner-only teleport with netUpdate. Adopt these native API contracts, not its contact-damage movement algorithm |
| [Native SoundID styles](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ID/SoundID.TML.cs) / [current SoundStyle guidance](https://docs.tmodloader.net/docs/stable/struct_sound_style.html) | Per-style volume, pitch, loop/voice limits. Keep original materials and bounded managed voices. Source inspected does not by itself identify every Last Prism/Terraprisma sound path; no such exact mapping is claimed |

No external implementation, recording or sprite is copied. The native ownership pattern is conventional weapon replication, not proof of adversarial server authority. Needed runtime evidence is the focused minion/cancel/audio smoke above, not an unrelated full Raid replay.

## Prior-art findings and engine seams

Research question: how do endgame weapons sustain a developing attack rather than replay a short cosmetic cycle? Scope searched: Yharim's Crystal, Drataliornus and Midnight Sun UFO holdout/beam/score code in the [official Calamity public repository](https://github.com/CalamityTeam/CalamityModPublic), accessed2026-09-09. Maintainer authority is its CalamityTeam official release mirror/readme. Pinned commit1a8cebd27ec5615316b78f71973446b5528d2b78 declares2.2.2 in [build.txt](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/build.txt); installed2.2.4 is not proven source-identical. The [license](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md) permits reference but restricts redistribution. All paths below were retrieved successfully at that commit; no code, textures, music or recordings were copied.

| Verified source / member | Observation | Independent Convergence decision |
|---|---|---|
| [YharimsCrystalPrism.AI / ShouldConsumeMana](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Magic/YharimsCrystalPrism.cs) | Persistent holdout creates its beams once, charges over180 ticks, spends mana on a changing cadence and updates aim/current damage | Adopt persistent lifetime/resource ownership; use our own seven-sigil macro score and a single direct beam, not six copied beams |
| [DrataliornusBow.AI](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Ranged/DrataliornusBow.cs) |360-tick spin-up stages shorten shot delay and change fully-spun-up shots; native PickAmmo is called for actual firing | Adopt a multi-second reached state; independently stage five siege bodies and a bounded one-shot crossfire |
| [MidnightSunUFO.AI](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Summon/MidnightSunUFO.cs) |330-tick score alternates a mobile gun phase with repositioning and a beam phase | Adopt distinct summon action categories; use one identity-bound concert and combined damage instead of beam duplication per minion |

Official pinned tML2026.07.3.0 source666f69962d3bdffde54fc14025f02634965b4e7c: [Player.TML.CheckMana](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.TML.cs) confirms explicit payment and blocking missing-mana effects; [ActiveSound patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Audio/ActiveSound.cs.patch) confirms loop callbacks and multiplicative volume. [Current Player API](https://docs.tmodloader.net/docs/stable/class_player.html) documents PickAmmo's one-shot consumption and native bonuses; unlike the fixed sources it is moving guidance.

Inference needing playtest: staged persistent bodies and reached sustain states should better communicate growing power than short loops. Compilation cannot establish aesthetic quality, multiplayer continuity or FPS. Required focused checks are macro-beat boundaries/resource cadence plus user-owned release/empty-resource/item-switch/Down,1 vs multiple minions/retarget/sacrifice, native stealth, aim continuity and reduced-effects playback. Current results belong only to [Status](../../STATUS.md). Previous0.2.30/0.2.33 implementation evidence remains linked there and in Git history, not active instructions.
