---
doc_id: encounter.first-severance.weapons
document_type: spec
status: provisional
owners:
  - gameplay
  - art
last_reviewed: 2026-10-03
source_of_truth_for:
  - first_severance.reward_weapons
aliases:
  - ritual armaments
  - raid reward weapons
related_code:
  - Content/Encounters/FirstSeverance/Rewards
  - Client/Encounters/FirstSeverance/Weapons
  - Client/Encounters/FirstSeverance/NullRefrainVisuals.cs
  - Client/Encounters/FirstSeverance/Weapons
  - Common/Compatibility/Calamity/CalamityRogueArmament.cs
related_docs:
  - project.status
  - encounter.first-severance.spec
---

# Requiem of the Hollow Doll — Ritual Armaments

Current mechanics are below; [Audio](../../AUDIO_CUE_SHEET.md) owns the active sound masters and removal of the claw's overlapping swipe after-sound. Versioned labels identify when a design arrived, not the current package/protocol: those belong to [Status](../../STATUS.md).

## Reward refresh (2026-10) — shared rules

The owner approved a full refresh of the five box weapons on 2026-10-02: keep each weapon's concept, class and internal ID (`NullRefrain`, `PaleMeridian`, `LacunaTestament`, `ChoirOfTheUnmade`, `LastWitness`), and redo the art, drawing, motion, effects and sound. The [Curtainfall Treasure Box](#curtainfall-treasure-box) and the [Unbroken Promise](#doll-companion--the-unbroken-promise) recipe are unchanged, and owned copies keep working without migration. The weapon sections further down stay current until each weapon's refresh lands; that change replaces its section, moves the old one to history and lists what changes for players (old → new numbers). This section owns only what all five share.

- **Signature:** each weapon turns one of Lacrimosa's techniques into the player's — Remote Clasp and Central Crush → the claws; the lattice beams → Pale Meridian; the core cannon → Lacuna Testament; the organ chorus → Choir of the Unmade; the Axiom Blade and Iron Interdict → Last Witness. Every weapon builds something visible over seconds and releases it at once, and each step of the build-up has its own sound and light. Thread, string, lace, chain and sewing motifs are Ebon Manor's and are not used; parts fly, seat and lock on their own.
- **Power:** the same budget as the 0.2.x forms. Base damages stay (claws 7700, Meridian 2002, Lacuna 2024, Choir 968 per voice, Witness 9680; `RitualArmamentRules.Damage` is frozen because the companion's 9680 depends on it). Each refreshed weapon's pure score must reproduce the baseline within ±3% per cycle, sustained, and over the best 600-tick window from a cold press, so player-timed releases (the claw grasp, the Meridian finisher) come out of the budget rather than on top of it. Numbers are raw: before defense, no crit, one target, every hit landing. Area growth (a wider beam, a spinning disc, a lattice) is declared in the weapon's change list; it is outside the single-target budget. Boss HP never changes to hide weapon balance.

| Weapon | Baseline per cycle | Sustained |
|---|---|---|
| Lacrimosa's Claws | 12.857 swipes ×1.0 + crush ×4.2 = 17.057× per 402 ticks | 19,603/s |
| Pale Meridian | build 0–348: 15 needles ×0.95 = 14.25× | overdrive 7.97× per 36 ticks = 26,593/s, plus ammo |
| Lacuna Testament | build 0–410: 26 bolts × 1113 = 28,938 | beam 4,048 per 10 ticks = 24,288/s |
| Choir of the Unmade (per voice) | 660-tick concert, about 20.4× | 5,080/s during the chorus; about 1,800/s averaged |
| Last Witness | 6 shards ×0.28 + blade ×5.4 = 7.08× per 282 ticks | 14,581/s; the stealth verdict adds ×3.24 |

- **Usability and ownership:** unchanged usability (no use while dead, Down or eliminated in the Doll Raid). One held controller projectile per weapon carries every count peers must see (lit beads, seated parts, open irises, voices, testimonies). Only the owner reads input and spends mana (native `CheckMana`) or ammo (one `PickAmmo` per real shot) and spawns children through native projectile replication. An item change, death, Down, crowd control or leaving the world ends the controller; launched projectiles stop once their owner is unusable. No Encounter packet, `ModPacket` or protocol change.
  - **Exception, summon weapons** (Choir of the Unmade; owner-approved 2026-10-03): the minions are native minions and, as native summons do, stay summoned through an item change and a Down. Down, crowd control and item restrictions halt what they do (no new shots, launched ones turn harmless); death, removing the buff or leaving the world ends them. The weapon's section lists the exact cases.
- **Presentation:**
  - Pixel art at the 2-pixel dot: one texel is one dot, drawn at 2 world px with point sampling. Pixel sprites are never scaled at draw time; a weapon that needs two sizes uses two integer export rungs and swaps them on its fastest frames.
  - One shared Doll weapon layer draws weapons **in front of players**; only the Choir's organ (and its gallery row) and Pale Meridian's wind-up key draw behind players (the key is a Meridian addition, owner-approved 2026-10-03). Effects get a one-dot ink outline (#121017).
  - Palette: ink #121017; black iron #1d1a22, #302a29, #49404a; porcelain #9c8070, #d0b69e, #f4e3ce, #fcf4e6; pearl #c9c4c9, #e1dce0; dull brass #684828, #a07b48, #d5b279; light ramp plum #301840 → #5a2c9c → violet #9458ff → #b99cff → pearl-violet #ddd8f8 → bone #fcf4e6 → white; ruby #8c141c as a 1–2 dot accent only.
  - Forecasts are a one-dot pearl-violet hairline or outline with travelling dots. Live damage is an original Luminance material per weapon, never a flat colour: at least four ramp tones, flowing noise, sparkle and a white-hot spine, with pearl, bone and white at least 40% of lit dots so the violet never reads as Ghost Samurai's fire. A void material (Lacuna's black beam) replaces the spine with a one-dot pearl lip. Residue cools to plum within 24 ticks. Other players' damaging light draws at 65% opacity and their void at 60%; bodies stay opaque.
  - Hit shapes are the constants in Content; the art is fitted to them, never the reverse, and an art-fit test keeps drawn anchors (talon tip, muzzle, hole, mouth, blade tip) within one dot of their design anchors.
  - Swings flow through their ends without hard braking. Reduced Effects (the Doll client config) keeps bodies, forecasts, live bodies and counts, halves debris and residue time and removes glow; screen shake follows the config. No fullscreen flash, hit-stop, zoom, slow motion or input lock.
- **Audio:** every cue is new and lives in `Assets/Sounds/Weapons/DollWeapons/`: music-box tines, brass gears and key ratchets, porcelain clinks and cracks, pipe organ, and low weight for impacts. Tonal cues use one home, F minor pentatonic (F A♭ B♭ C E♭; build-up ladder F5 A♭5 B♭5 C6 E♭6 F6 A♭6 B♭6 C7), measured against the four Doll tracks. Every release pairs a warning cue with a firing cue, and a set piece that can fail has its own, different failure cue. Composite cues and loops are never transposed at runtime. Reduced Effects never lowers weapon audio. The 0.2.x masters in `Weapons/DollTheater` stay because the companion and the Raid still play some of them; [Audio](../../AUDIO_CUE_SHEET.md) owns masters and levels.
  - **Exception, a killed target** (Choir of the Unmade; owner-approved 2026-10-03): a chorus whose target is killed with the organ out is not a failure; it closes quietly with the success cue `ChoirChorusEnd`, and every other stop with the organ open keeps the failure cue `ChoirChorusMiss`. The weapon's section lists the exact cases.
- **Art:** Codex pixel art from Claude's brief (DW01–DW05, delivered 2026-10-02 outside the repository under `asset-deliveries/doll-weapons/2026-10-02/`). `tools/export_doll_weapon_art.py` measures each sheet's dot pitch, snaps to the palette, reduces by whole factors and records anchors; runtime PNGs live in `Assets/Textures/Items/DollWeapons/` with exact [Attribution](../../../Assets/ATTRIBUTION.md) records.

| Asset | Use |
|---|---|
| DW01 / DW01B / DW01I | Claw right hand: open, raking, clenched, thrusting (the left hand is the mirror) with the six-bead plate; icon |
| DW02 / DW02E / DW02K / DW02I | Assembled siege gun; bare gun (the four brass parts are cut from the difference); wind-up key, 4 frames; icon |
| DW03 / DW03I / DW03A / DW03B | Testament with a see-through hole; icon; porcelain iris, 4 frames; great iris |
| DW04 / DW04V2 / DW04V3 / DW04P / DW04S / DW04B | Three faceless choristers × 3 singing frames; pipe organ with a see-through mouth; baton; buff icon |
| DW05 / DW05V / DW05S / DW05I | Execution blade with the witness hole; judgement sword; three porcelain shards; icon |

- **Companion:** The Unbroken Promise is unchanged except its summon sound, which plays on the same trigger.
- **Acceptance (owner; `not_run` until played):** each weapon's build-up, release and paired cues; readability on bright and dark ground at zoom 1; weapons in front of the player without hiding the character, and the organ and the Meridian key behind; a second peer seeing the same build-up counts; Reduced Effects; FPS with eight players' weapons.

## Choir of the Unmade (2026-10)

The Raid sings its beams at you: a thin locked axis, then a pilot line that swells to full width. This weapon makes that technique yours. Unfinished porcelain dolls gather around you, you conduct them with a porcelain baton, and on the chorus a pipe organ rises over your head and sings one beam for the whole choir.

The item `ChoirOfTheUnmade` and the buff `ChoirOfTheUnmadeBuff` keep their IDs; the [treasure box](#curtainfall-treasure-box) (20%) and the [Unbroken Promise](#doll-companion--the-unbroken-promise) recipe are unchanged. The item now summons the native types `ChoirChorister` (one voice), `ChoirSungNote` and `ChoirChorus`; the 0.2.34 types stay registered but unused until one cleanup change, and the 0.2.34 form is kept in [history](../../history/2026-10-03-choir-of-the-unmade-0234.md). `ChoirConcertRules` owns every number below; the art is fitted to it.

**Item.** Summon staff, 1 minion slot per use. Base damage 968 (`RitualArmamentRules.Damage(Summon)`, unchanged; the Promise's 9680 stays tied to it), mana 10, use 24 ticks, knockback 6, crit 0. Normal minion target commands, sacrifice and switching weapons all work; the concert never needs the item held. Each use flicks the baton (`ChoirBaton`, held in the front hand) in one continuous downbeat: up behind the head, forward-down, a short follow-through and back, periodic so auto-reuse never stops dead. The new voice appears at the raised baton tip, `MountedCenter + (20·facing, −72)`, fading in from pearl, and glides to its seat.

**Choristers.** Faceless porcelain busts on brass stands (`Chorister0/1/2` by `identity % 3`, 2 world px per dot, 44 × 76 px), each with three singing frames: closed, a small "o", and wide open with the head up. The export lost the closed mouth line of `Chorister0` and `Chorister2`, so frame 0 draws it in code as a two-dot Iron line. Each variant has its own design mouth, the wide-open mouth of frame 2 (on screen from the tick a note leaves it): `(10, −12)`, `(11, −13)` and `(7, −12)` px from the centre facing right for `Chorister0/1/2`, each within one dot (2 px) of its art (domain art-fit test). They never deal contact damage. The lead voice (the owner's lowest native identity) keeps the concert clock in `ai[0]` (0 idle, 1–648 running) and the target in `ai[1]`; every voice copies them, and a new voice joins the running clock. Targets are measured from the owner, not from the dolls: the manual minion target first, otherwise the nearest valid NPC, acquired within 1800 px and kept within 2100 px with line of sight from `owner.MountedCenter`.

**Cloud seats** (idle and verse), around `owner.MountedCenter + (0, −80)`: ring r holds 5 + 2r voices at radius 96 + 46r px over the 140° arc above the player, filled from the top centre outward, bobbing ±3 px across and ±5 px up and down; every stand tip stays at least 40 px above the player's head. Dolls face the target. Movement is velocity steering, `desired = clamp((seat − pos)·0.14, 30 px/tick)`, `v += (desired − v)·0.30`: a 240 px move settles within 4 px in 17 ticks with under 4 px overshoot.

**The concert: 18 beats at 100 BPM (beat 36 ticks, sixteenth 9), 648 ticks = 10.8 s,** looping while the lead has a target.

| Beats | Ticks | Section | What happens | Light | Sound |
|---|---|---|---|---|---|
| 0–1 | 1, 36 | Count-in (verse warning) | Every head lifts one dot on each tap | A one-dot pearl ring blinks at every mouth | `ChoirVerseWarn` (two taps, then an inhale) |
| 2–7 | 72–287 | Verse | Each voice sings one note a beat, a sixteenth × (ordinal mod 4) late. Harmony Fm7 Fm7 B♭7sus B♭7sus A♭maj7 E♭sus | Mouth "o" 6 ticks before each note, wide 8 after; one-dot sound arcs | `ChoirVerseFire0–8`: voices sharing a part sing one cue, a little louder |
| 8 | 288–323 | Gather | Dolls glide from the cloud to the chorus seats; the organ case assembles from its mouth outward in 14 ticks | Pearl-violet reveal rings, pearl motes, motes behind the gliding dolls | `ChoirOrganRise` |
| 9 | 324–359 | Pipes | Raised ranks rise centre-out, rank k from 324 + 5k over 8 ticks, sliding up behind the rail in whole dots | A brass glint as each rank seats | `ChoirPipe0–5` (F3 C4 F4 A♭4 C5 E♭5), raised ranks only |
| 10 | 360–395 | Inhale (chorus warning) | The beam exists but is harmless: a locked one-dot axis from the mouth, turning toward the target | Forecast hairline with pulses running into the mouth; the mouth darkens to a void with a pearl lip as motes spiral in | `ChoirChorusWarn` |
| 11–15 | 396–575 | Chorus | One beam from the organ mouth. B♭7sus (11–12) → Fm9 (13–14) → an open F5 fifth (15) | A wavefront runs down the beam on every beat; Fm9 deepens the hem; the open fifth turns the pearl band brass | `ChoirChorusFire1–6` by chord voices |
| 16–17 | 576–647 | Release | Pipes sink outer-first (576 + 4·(5 − k)); the case fades 604–616; dolls return to the cloud over 36 ticks | The beam's axis cools from lilac to plum in 24 ticks | `ChoirChorusEnd` |

**Notes.** The owner spawns each note at the singer's open mouth (its variant's design mouth, mirrored by facing), launched at 8 px/tick toward the target plus 5 px/tick upward; after 6 ticks it homes, accelerating to 30 px/tick (turn cap 0.24 rad/tick). 16 × 16 swept head, one extra update, pierce 1, one hit per NPC root, life 240 ticks, damage 0.78 × the voice's current damage. Drawn as code pixel glyphs (a quarter, an eighth, a beamed pair) with a short comet tail; never with staves. Each voice sings each verse note once per concert: when its place changes mid-verse (a new voice with a lower identity, a sacrifice) it never repeats a note and still sings the one it was about to sing, at most 29 ticks late; a new voice never sings a note whose tick came before it appeared. Sung pitches are F minor pentatonic, F4 to C6:

| Part | Line (one note per verse beat) |
|---|---|
| 0 (melody, the lead) | A♭5 C6 B♭5 A♭5 C6 B♭5 |
| 1 | F5 E♭5 E♭5 F5 E♭5 E♭5 |
| 2 | C5 A♭4 B♭4 F5 C5 A♭4 |
| 3 | F4 C5 B♭4 E♭5 A♭4 E♭5 |

**Chorus formation**, around the stage anchor S (the organ mouth and beam origin), `S = owner.MountedCenter + (0, −210)`, eased at 0.25 a tick and snapped beyond 1200 px:

| Row | Holds | Arc (y down, 90° straight below) | Stratum |
|---|---|---|---|
| 0 (front) | ordinals 0–6 | centre S + (0, −40), radius 150, 35°–145° (a cup under the mouth) | front |
| 1 | ordinals 7–15 | centre S + (0, −122), radius 196, 30°–150° | front, behind row 0 |
| 2 (gallery) | ordinals 16+ | centre S + (0, −70), radius 175, 200°–340° (an arch over the pipes) | back, behind the organ |

A row's members share its arc evenly; the first voices of a row take the seats nearest its centre (ties go right), so the lead stands centre front under the mouth. Rows 0–1 keep at least 40 px between seats up to 16 voices, the gallery at least 24 px up to 33. During the inhale and the chorus the rows part for the beam (drawing only; the voices have no hitbox): each row stays on its arc, which may stretch (rows 0–1 to −20°…200°, the gallery to 160°…380°); the voices in the beam's corridor (its drawn half width, the throat's near the mouth, plus 40 px) slide along the arc to the nearer side of the gap and their neighbours make room, so no two of a row stand closer than 36 px (or the row's own spacing where that is tighter). The parting eases in over 12 ticks and out as the beam closes.

**Organ** (`ChoirOrgan`, k = 1: one texel is one dot, 190 × 192 px), one per owner, on the back stratum behind every player. Drawn from `S + (−95, −147)`, its see-through mouth sits on S (within one dot) and its 18 px radius is the beam's throat. Voices raise ranks: `clamp(voices + 1, 2, 6)` ranks, so 1 voice raises 3 pipes, 2 → 5, 3 → 7, 4 → 9, 5 or more → all 11. A completed chorus sinks the pipes and fades the case out by tick 616. A concert that stops in its release still closes to that vanish; a target that dies with the organ out closes it quietly as a release from where it stood (pipes sink, the case is gone 40 ticks later); only a lost target crumbles it into porcelain and brass.

**Chorus beam** (`ChoirChorus`, one per owner):
- The lead spawns it at concert tick 360; its age follows the lead's clock (`ai[0]` = clock − 360): ticks 0–35 are the harmless warning, 36–215 are live.
- It turns toward the target at 0.045 rad/tick; the first warning tick snaps.
- Collision runs from 48 px past the mouth to 2000 px × `LengthFactor` (full length in 3 ticks). The half-width starts as the 1.5 px pilot, swells (quintic) to 46 px (92 px full width) by live tick 7 and closes back to the pilot over live ticks 168–180, the Raid's `FirstSeveranceBeamIgnition`; the first live tick cannot hit.
- Damage is `(int)(1.05 × the sum of every living voice's current damage)`, recomputed every tick. One hit per NPC root per 12 ticks, so 15 per chorus. Never one beam per voice.
- If the lead is removed, the next voice carries the clock, the organ and the beam on. A lost or killed target, an unusable owner or no voice left closes it harmlessly in 6 ticks (the owner decides; peers follow its update).
- Drawn by the original `DollChoirEnergy` material. The throat widens from the organ mouth's 18 px to the full half width over the first 48 px, where collision starts: the throat glows over the organ and never hits. Past it the light stays inside the collision body.

**More voices, richer chord.** Chord voices = min(voices, 6) choose `ChoirChorusFire1…6` (more sung lines; the fifth line adds Fm9's ninth) and the number of brass standing waves in the beam (at most 2 under Reduced Effects). Raised pipes grow as above, and the verse fills its four rolling parts; voices beyond four double a part. Per owner the draw is bounded: 40 voices, 64 notes (24 with trails), one beam, one organ.

**Interruptions.** The minions staying through an item change and a Down, and the quiet close on a killed target, are the owner-approved (2026-10-03) exceptions in the [shared rules](#reward-refresh-2026-10--shared-rules).
- **Target lost, or owner unusable** (dead, Doll Raid Down or eliminated, `noItems`, crowd control): the clock returns to 0 and the dolls go back to the cloud; a warning or live beam closes harmlessly in 6 ticks; an open organ crumbles over 18 ticks with `ChoirChorusMiss`, and a sounding chorus fades out over 6 ticks. The next valid target starts again from the count-in.
- **Target killed** (inactive or no life left) with the organ out and no other target in reach: the same stop, but a quiet close instead of a failure: `ChoirChorusEnd` plays, a sounding chorus fades out over 30 ticks and the organ closes as a release from where it stood. Another target in reach is simply taken and the concert goes on.
- **Any stop in the release** (576–647): the pipes keep sinking and the case keeps fading until it is gone at 616.
- **Owner Down:** the choristers stay summoned (slots kept); notes in flight turn harmless and fade in 8 ticks.
- **New voice mid-concert:** it joins the current clock and flies to its seat; its damage joins the beam on the next tick; a chorus already sounding does not change.
- **Death or buff removed:** every chorister ends; notes and the beam stop with their owner.
- **Item change:** nothing happens to the minions; only the baton needs the item.

**Presentation.** Everything draws through the shared Doll weapon layer at one dot = 2 world px, point-sampled and snapped: the gallery row, pipes and case on the back stratum; rows 0–1, the cloud, the baton, the beam, notes and sparks in front. While the owner holds the Choir during a running concert the baton beats time, rising through the inhale and the chorus; the arm follows the baton. Another player's beam, notes and trails draw at 65% and the inhale's void at 60%; bodies stay opaque. Reduced Effects keeps the bodies, forecast, live beam and counts, uses two standing waves and wavefronts only on the chord changes, drops the motes, halves trail length and residue time and removes the glow; the screen shake (3, owner only, through `RitualWeaponFeedback.Kick`) follows the config.

**Audio.** 29 original cues in `Assets/Sounds/Weapons/DollWeapons/` (no recordings): a faceless formant doll voice, porcelain taps, brass gears and pipe organ, all F minor pentatonic. Pairs: `ChoirVerseWarn` → `ChoirVerseFire0–8`, `ChoirChorusWarn` → `ChoirChorusFire1–6`; the success close `ChoirChorusEnd` and the failure `ChoirChorusMiss` sound different. The close is restrained, as the owner chose for big finishers on 2026-10-03: the chorus's open fifth falls away into its cut and `ChoirChorusEnd` (a soft gong on low F and a quiet organ fifth with a long tail) carries the ending. The owner approved all 29 cues on the audition page on 2026-10-03. Cues play on the lead's accepted clock through `DollWeaponAudio`, once per owner and concert tick; peers hear other choirs at 70%; beam contact is an owner-local accent at most every 24 ticks. [Audio](../../AUDIO_CUE_SHEET.md#doll-weapons-2026-10) owns the levels.

**Power** (raw, one target, every hit landing; per voice at 968):

| | Notes | Chorus | Per concert | Per second |
|---|---|---|---|---|
| 0.3.70 baseline | 5–6 × 0.85 (823) by ordinal | 15 × 1016 | 19,766.5 per 660 ticks (mean) | 1,797 |
| 2026-10 | 6 × 0.78 (755), every voice | 15 × 1016 | 19,770 per 648 ticks | 1,831 |

Per cycle +0.02%, sustained +1.86%, best 600-tick window from a cold summon 19,770 against 20,178 (−2.02%): within the shared ±3%. The chorus window stays 5,080/s per voice; ten voices hit for 10,164 per chorus hit. The beam is still one 92 px line per owner.

**Ownership.** The owner client targets, advances the lead's clock and spawns the notes and the beam through native minion and projectile replication; the lead syncs on clock resets and every 60 ticks, every voice every 60 ticks, the beam every 6 ticks (always, turning or not). Only the owner judges the target (range, sight), aims the beam and closes it. Peers advance the replicated clock while the owner keeps a target and stop it only on the owner's update, smooth the beam's angle toward its 6-tick corrections, and play the same cues on that clock. A new voice is spawned with the lead's clock and target, so it can take over as lead at once. No packet; protocol unchanged. Client resources are dropped on world unload; a Dedicated Server never touches graphics or audio.

**What changes for players** (old → new):
- Concert: 11.0 s (660 ticks) → 10.8 s (648 ticks, 18 beats at 100 BPM), with a two-beat count-in and a one-beat chorus warning you can see and hear.
- Notes: 5 or 6 per voice depending on its place (×0.85, every 46 ticks) → 6 for every voice (×0.78, one per beat).
- Notes leave the doll's mouth at 8 px/tick (plus a lift) and home after 6 ticks to 30 px/tick → was 16 px/tick from the doll, homing at once to 36 px/tick.
- Chorus starts at concert tick 372 → 396; still 180 ticks and 15 hits × 1.05 the summed voices.
- Beam origin: 260 px above the target, pointing down → the organ mouth 210 px above you; collision now starts 48 px past the mouth; 2000 px and 92 px unchanged; opens with the Raid's pilot-and-swell (full width by tick 7) instead of a 7-tick ease, and narrows over its last 12 ticks.
- Choristers: orbit the target → stay over you, then line up in rows before the organ; targets are measured from you.
- Removing the lead voice now hands the beam to the next voice instead of cancelling it.
- Sustained output per voice 1,797 → 1,831 raw/s (+1.9%); the best cold window 20,178 → 19,770 (−2.0%); chorus window 5,080/s unchanged.
- New pixel art (choristers, organ, baton, item and buff icons), a new beam material and 29 new cues; the 0.2.x apparatus, pose and Choir sounds no longer appear for this weapon.
- The new voice appears at the baton tip, `(20·facing, −72)`, instead of `(0, −80)`.

**Acceptance** (owner; `not_run` until played): 1, 4 and 10+ voices (formation, chord growth, pipe count); the verse rhythm, organ rise and the warning → chorus pairing; beam readability on bright and dark ground; the closed-mouth line; the parting at 10+ voices; Down, death, sacrifice, retarget and kill-closing cleanup; a second peer seeing the same concert; Reduced Effects; FPS with 10+ voices; the cues over the Raid music.

## Pale Meridian — refreshed ranged (2026-10)

*The boss's lattice beams, made the player's.* Hold the trigger to build a music-box siege rifle in front of you. Four brass parts fly in and click into the bare gun one at a time; every shot rings the next note of a music-box tune, and each part makes the gun fire faster. When the gun is complete its wind-up key rises and is wound, then the spring drives an overcharged stream of homing rounds. Letting go fires one white **meridian** through the cursor, which splits into a small lattice of light. Item `PaleMeridian` (ranged, bullets, channel; base 2002, crit 8, knockback 6); acquisition is the [treasure box](#curtainfall-treasure-box). Projectiles `MeridianHoldout` (the held gun), `MeridianRound`, `MeridianLine`; the 0.2.34 `MeridianBastion`/`MeridianNeedle` types stay in the code, unused. `PaleMeridianScore` owns the clock and factors, `PaleMeridianLattice` the release geometry and live windows, `PaleMeridianRig` the held-gun anchors; ticks are real game ticks from the press and use speed never compresses them. Landed in 0.3.86; the owner approved its look and all twenty-three cues on the audition and review page on 2026-10-03 (in-game play, a second peer and FPS remain `not_run`).

| Score age | Beat | Firing |
|---|---|---|
| 0–12 | The bare gun (its four recesses empty) slides into the hand | — |
| 12 | First note | every 24 ticks: 12, 36, 60, 84 |
| 92 → 108 | Part 1, the music-box cylinder, flies 16 ticks and seats with a click | every 18: 108 … 162 |
| 164 → 180 | Part 2, the barrel shroud | every 12: 180 … 216 |
| 212 → 228 | Part 3, the ring sight | every 9: 228 … 255 |
| 248 → 264 | Part 4, the spring housing; the gun is complete | every 6: 264 … 294 |
| 300 | The wind-up key rises out of the housing in 6 ticks; firing stops | — |
| 300–348 | Winding: 18 held ticks, then 12 ratchet steps of 45° at 318, 324, 328, 331, 334, 337, 339, 341, 343, 345, 347, 348 (`floor(12·((t−300)/48)^2.5)`) | — |
| 348 (5.8 s) | **Overcharge**: the spring lets go and the key spins 45° every 2 ticks | every 3 |

- **Rounds:** one native `PickAmmo` per real round (ammo damage, knockback and conservation stay native; the item keeps its 75% chance not to consume); none during the arrival or the wind. Every round leaves the one muzzle, 150 px along the aim from the player's rotated centre, aimed at the point 1300 px along the aim. Each hits a logical NPC root once (18 px swept box, 150-tick life). Aim turns at most 0.085 rad/tick before overcharge and 0.055 during it.
  - Build notes (22): ×0.65, straight at 52 px/tick, pierce 1.
  - Overcharge rounds: ×0.62, leave the muzzle at 52 px/tick and home at 44 px/tick (acquire 1800 px, keep 2100, 0.24 rad/tick).
  - Heavy rounds: every 36 ticks from 348 one replaces the ordinary round: ×1.15, leave at 62 px/tick and home at 52 px/tick, pierce 3.
- **Notes** (F minor pentatonic ladder of the [shared rules](#reward-refresh-2026-10--shared-rules), one file per step, never transposed): the build plays one bar per stage over Fm7, B♭7sus, A♭maj7 and E♭sus (F5 A♭5 C6 E♭6 · F6 E♭6 B♭5 A♭5 · A♭5 C6 E♭6 A♭6 · B♭6 A♭6 E♭6 B♭5), then a run C6 E♭6 F6 A♭6 B♭6 C7 into the wind; each part seats on a bar's first note. In overcharge a four-bar loop restarts at 348 with one note every 9 ticks (every third round); the heavy rounds fall on its downbeats.
- **Release — Meridian.** Letting go while the weapon is usable fires the finisher for one more `PickAmmo`; without ammo, unfocused or over the fullscreen map it packs away with no finisher (its own failure sound). The direction locks from the player's centre through the cursor; the line starts at the muzzle on that line, and the **node** is the cursor's distance from the muzzle clamped to 160–1200 px.

| Held age | Tier | Finisher (×weapon damage per root) |
|---|---|---|
| under 108 | 0 | none (the gun just packs away) |
| 108–227 | 1 | meridian ×1.5 |
| 228–347 | 2 | meridian ×2.5, lattice ×1.5 |
| 348 and later | 3 | meridian ×4, lattice ×2 (6× at the node) |

  - Meridian (ages from the release): a harmless one-dot forecast through the node for 10 ticks while the gun swings onto it; then a packet whose white head runs the whole line (node + 640 px, at most 1840 px) in 8 ticks with its tail 5 ticks behind, 28 px wide; live ages 10–21.
  - Lattice (tiers 2–3): as the packet's head passes the node the lattice splits from it (6 ticks, harmless), holds 4 ticks, then fires ring by ring 2 ticks apart, each line lighting from its middle to both ends in 6 ticks with its tail 4 ticks behind, 20 px wide. Tier 3 is a '#': parallels at ±120 px (480 px long) crossed by perpendiculars at 0 and ±120 px along (480 px); with the meridian that is three lines each way. Tier 2 is parallels at ±100 px and one perpendicular through the node, 320 px. The lattice never has four or more evenly spaced parallel lines (a staff is Scarlet Invocation's motif).
  - Each NPC root takes at most one meridian hit and one lattice hit per release; lines pass through tiles; nothing damages before its packet arrives or after its tail has passed.
  - Afterwards the gun recoils, the key unwinds and sinks, the parts pop off and the bare gun fades within 24 ticks; a new press is accepted after that.
- **Cancel:** item change, death, Raid Down or elimination, `noItems` or crowd control removes the gun at once, with no finisher and no further ammo; running out of ammo while holding packs it away. Because an item change removes the gun at once, switching away and back during the pack-away allows a new press without the 24-tick wait; this stays inside the budget (even a new press one tick after a release never beats holding, domain-tested). Rounds and a released finisher keep going through an item change and end at once if the owner dies, is Downed or is eliminated.
- **Presentation** (shared [Doll weapon layer](#reward-refresh-2026-10--shared-rules): Front stratum, in front of every player, except the wind-up key):
  - Art: the owner-approved DW02 family at k = 3, one texel per 2 px dot, point-sampled and never scaled: `MeridianBare` plus the four `MeridianParts` flying to their seats, `MeridianGun` (the assembled reference) from the fourth seat until the parts pop off, the 4-frame `MeridianKey` (k = 1, 72×60 px) on the key seat, and the `PaleMeridianIcon` inventory icon. The key stands over the owner's head, so it alone draws on the Back stratum, behind every player (the shared rule's Meridian exception, owner-approved 2026-10-03): heads and faces always draw over it and the gun covers its foot as it rises out of the housing. The art's muzzle anchor sits on the 150 px design muzzle; its grip and key seat land within one dot of the design anchors (domain-tested). Aiming left mirrors the gun about its aim (never upside down). The front arm reaches the grip, the back arm the fore-end.
  - Light: the original `DollMeridianEnergy` material (white-hot spine and head over a flowing pearl-violet body that thins to a plum rim, drifting sparkles, Luminance noise) plus the layer's lines, rings and debris. Every round closes an iris on the muzzle and every note runs a pin of light along the shroud; each part appears ahead of and above the gun in a pearl glint (well in front of the owner's face), flies in leaving a short pearl-to-violet pixel trail and seats under a pearl star while the part flashes white and the whole gun clicks pale; each ratchet step throws brass sparks while a violet iris tightens on the muzzle; ignition bursts a ring; overcharge keeps a pulsing violet muzzle glow; rounds are pearl pins with violet wakes and heavy rounds a 3-dot lance with a halo; the lattice adds brass glints where its lines cross and a star at the node; residue cools to plum within 18 ticks.
  - Recoil (eased): 3 px per note, 1.5 px per overcharge round, 5 px and 0.05 rad climb per heavy round, 8 px and 0.10 rad at the strike (so the ring sight stays clear of the face). After the release the parts pop off forward and up, away from the owner (the spring housing drops off forward).
  - Other players' light draws at 65%; bodies stay opaque. A frame records every Meridian's release lines first (all residue, all bodies, then the glows: one light batch each however many lines are alive), then the guns, the rounds' heads and the rounds' wakes last, so a full lobby that fills the shared budget loses trails first. Reduced Effects halves sparks, sparkles and residue time, uses 4-ray stars and removes every glow disc (muzzle, charge, packet head and node); screen shake goes through the shared feedback (owner only, respects the config). Geometry, timing, damage and sound are unchanged.
- **Audio:** 23 new cues in `Assets/Sounds/Weapons/DollWeapons/` (music box, brass, porcelain, organ): `MeridianAssemble`; `MeridianNote0`–`8`; the pairs `MeridianPartWarn` → `MeridianPartFire`, `MeridianIgniteWarn` → `MeridianIgniteFire`, `MeridianStrikeWarn` → `MeridianStrikeFire` (or `MeridianStrikeMiss` when the release fails), `MeridianLatticeWarn` → `MeridianLatticeFire`; the overcharge loop `MeridianLoop` (2.4 s = 144 ticks, sample-exact WAV); `MeridianHeavy`; `MeridianHit`/`MeridianHitHeavy` on the owner's hits. Every peer plays them once per projectile from the accepted age. The closing strikes (`MeridianStrikeFire`, `MeridianLatticeFire`) are restrained: the music-box chord or cascades over a soft gong-like low resonance with a long tail and a quiet organ, never a loud organ. Owner priority: another player's cues use their own small instance pool and never the last voices, and their notes thin out (none beyond 1600 px; in overcharge only each bar's downbeat), so a full lobby cannot starve the owner's own cues. [Audio](../../AUDIO_CUE_SHEET.md#doll-weapons-2026-10) owns the files and levels.
- **Ownership:** the owner client reads the trigger and cursor, spends ammo and spawns rounds and the two release lines through native projectile replication; peers draw from replicated position, velocity and `ai`. Holdout: `ai[0]` age (frozen at the release), `ai[1]` −1 while held then the finisher tier that fired (0 none), `ai[2]` 0 while held then −1 … −24 while packing away; velocity is the aim, locked at the release. Line: position = muzzle at the release, velocity = unit direction, `ai[0]` age (the lattice starts negative), `ai[1]` node, `ai[2]` tier (+4 for the lattice). No packet; protocol unchanged.
- **Budget** (raw, one target, per-hit rounding; [baseline](#reward-refresh-2026-10--shared-rules)): build 22 × 1301 = 28,622 (baseline 28,530, +0.3%); overcharge 15,953 per 36 ticks = 26,588/s (unchanged); best 600-tick window from a cold press 144,859 (release at 579 so both finisher hits land; baseline 141,442, +2.4%); holding alone 142,595 (+0.8%). Releasing and pressing again never beats holding: the best cycle averages 93% of the overcharge rate (release at 348: 21.45× per 6.2 s). The lattice's area is outside the single-target budget.

**What changes for players**

- Build-up: one receiver plus four docking components firing 15 homing needles ×0.95 (14.25×) → a bare gun that gains four brass parts at 108/180/228/264 ticks and fires 22 straight music-box notes ×0.65 (14.30×) on a 24/18/12/9/6-tick ladder.
- Muzzle: 122 px → 150 px from the player's centre.
- Overcharge: unchanged (348 ticks, a round every 3 ticks ×0.62, a heavy ×1.15 every 36, 26,588 raw/s), now with the wind-up key turning.
- Letting go: stopped firing → fires the meridian (×1.5/×2.5/×4 by tier) and from tier 2 a lattice (×1.5/×2) through the cursor for one more ammo; 6× at the tier-3 node.
- Area: new lattice damage in a 480 px '#' (tier 3) or a 320 px cross (tier 2) around the cursor.
- Burst: best 10 s from a cold press 141,442 → 144,859 raw (+2.4%); sustained damage unchanged.
- After letting go: 20-tick fade → 24 ticks before the next press.
- Look: the painted gun behind the player and upside down when aiming left → the approved pixel gun (174×34 px) in front of the player, mirrored when aiming left, with its wind-up key standing behind the player; new inventory icon.
- Sound: the 0.2.x Ranged and MeridianSustain sounds → 23 new music-box, brass, porcelain and organ cues.

## Rogue — Last Witness

Refreshed 2026-10 under the [shared rules](#reward-refresh-2026-10--shared-rules); the 0.2.34 score is kept in [history](../../history/2026-10-03-last-witness-v1.md). The Doll's **Axiom Blade** (two accelerating revolutions) and **Iron Interdict** (forecasts held in place, then swords driven home) become the player's: a heavy execution blade hangs before you and six testimonies break off its edge as seeking porcelain shards; when the sixth has spoken the sentence is sealed, the blade swings back overhead and is hurled spinning, bites its target, turns in it twice, tears free and returns to hang again. A stealth throw also calls the **Triangle Judgement**. `WitnessRules` owns every number below; the projectiles are `WitnessHang` (held), `WitnessShard`, `WitnessThrownBlade` and `WitnessJudgement`.

Unchanged: item `LastWitness`, Calamity's rogue class and stealth through `CalamityRogueArmament`, base damage 9680, crit 8, use 40, channel and auto-reuse, the [box](#curtainfall-treasure-box) and the [Promise](#doll-companion--the-unbroken-promise) recipe.

**Score.** One use is one 282-tick score (4.70 s) on the `RitualGrandScore` milestones: seal 174, throw 218, end 282; attack speed never compresses it. A held trigger re-uses the item about two ticks after the end, and Calamity reads stealth once per score. Aim turns at most 0.08 rad/tick before the seal and 0.025 after.

- **Hang (0–174):** the blade's tip points toward the cursor, raised 21° above the line of fire, its balance point 94 px from the hand. A freshly drawn blade settles in over 12 ticks (24 px); it breathes ±0.05 rad and ±2 px.
- **Testimonies:** six fire at 16, 45, 74, 103, 132 and 161 (every 29 ticks), each warned 10 ticks earlier. Their seats lie along the cutting edge, 16 px apart from −24 to +56 px of the balance point, filling from the centre outward (3-4-2-5-1-6). Warning: a thread of light runs from the blade's eye along the edge to the seat, the edge cracks and a porcelain shard slides 16 px out, pulling back 4 px before it fires. Fire: the shard leaves along the aim at 44 px/tick (it updates twice a tick, as in 0.2.x), then seeks at 36 px/tick (native targeting); pierce 1, 18 px swept head, once per logical root, ×0.28. The blade kicks back 5 px and 0.05 rad, a pearl notch stays lit where the shard left and the eye glows a sixth brighter.
- **Seal (174):** the six notches run along the edge into the eye, which flares. From here **releasing no longer cancels**: the throw completes on its own.
- **Swing and throw:** the blade lifts over the shoulder to 137° behind the aim (radius 94 → 76 px, heavy quintic ease, 174–202), holds and trembles (202–210), then whips forward on an accelerating curve with no brake and leaves at 218, 92 px out along the aim, already spinning at the whip's speed (0.45 rad/tick). The arm rides the same curve, follows through 0.6 rad past the aim and is back on the hang by 268.

**Thrown blade.**

- **Outbound:** 34 px/tick, homing on the native target at 0.24 rad/tick, spinning 0.45 rad/tick. The hit shape is a **disc of radius 56 px** around the balance point, swept between ticks; the drawn blade's tip reaches 63 px from it. Outbound ends at the first contact; without one the blade stops and turns in the air after the cursor distance at 34 px/tick (4–27 ticks) when nothing is targeted, or after 27 ticks (918 px) while it homes on a target.
- **Strike:** the first contact deals ×0.25 of the blade, once per root.
- **Axiom turns:** the blade brakes onto the struck target and follows its centre (0.35 response, at most 34 px/tick), or holds where it stopped. Exactly two revolutions in 21 ticks, the spin rising from 0.45 to 0.747 rad/tick; a bite on each half turn at turn ticks 6, 12, 17 and 21, ×0.125 each, once per root per bite, no knockback.
- **Return:** at turn tick 22 the blade tears free (×0.25, once per root, the struck target included) and flies home at 46 px/tick to the catch point: the hang's rest point if this owner holds a score (the blade hangs again; a new score's hanging blade stays hidden while the thrown one is out) or the hand. Caught within 28 px with a ring and its own sound; withdrawn beyond 3000 px.
- **Blade per root:** ×5.4 = 0.25 + 4 × 0.125 + 0.25.

**Stealth — Triangle Judgement.** A stealth throw marks the blade; the shards never execute. As the stealth blade's turns begin, the judgement is called on the struck target (or where the blade stopped). Damage timing, footprint and ledger are the 0.2.x verdict's, read from `RitualArmamentChoreography`: it follows the target until the lock at 16, then executes once per root inside the **185 px triangle** (SAT against the NPC box) during 28–31, ×0.60 of the blade; it ends at 56. On screen: the 185 px footprint as a forecast hairline from the call; wavering auras rising 300 px above the 265 px corners (0–10); three `WitnessSword` stakes fading in at the top (4–10), falling point first (10–16) and staking the corners at the lock; light written from stake to stake (17–22); the stakes closing inward 265 → 185 px (23–28); the execution as opaque pearl craquelure over a translucent porcelain ground (55%, so the target and anything inside stay visible), the cracks drawn toward a small black eye with a pearl lip (radius at most 22 px, opening at 28 and shut by 38), all of it cooled away 20 ticks after the live window (by 51) while the swords withdraw upward into light (36–46).

**Release, item change, death and Down.** Releasing before 174 cancels: shards already fired fly on and the hanging blade crumbles. Item change, death, Raid Down or elimination, crowd control or `noItems` end the score at any time before the throw. After the throw the blade and the judgement survive an item change and end at once on death, Down or elimination. The owner alone reads input and spawns the shards, blade and judgement through native projectile replication; the score age and the testimonies spoken ride `ai`, the blade's phase, phase start, anchor and spin ride 15 bytes of `ExtraAI`. No packet and no protocol change.

**Presentation.** Front stratum of the shared layer, one texel = 2 world px. `WitnessBlade` (k = 2, 130 × 24 px) hangs and is thrown: one rung, fitted to the 56 px disc (the drawn tip reaches 63 px from the balance point, so the turning blade shows how far it bites); `WitnessBlade_L` (k = 1) stays exported but is not drawn. `WitnessShards` (three frames) are the testimonies; `WitnessSword` stands on the corners with a one-dot light outline that keeps its dark steel readable; the inventory shows `LastWitnessIcon` (the V3 art no longer draws). The eye hole emits the testimony light (ruby-accented under stealth). Live light is the original `DollWitnessEnergy` material: a narrow wake that starts behind the spin so the blade stays clear, a trailing spin arc on the hit disc's rim (56 px, 0.89 of the drawn tip's reach; never inside 0.8 of it), at most 2 dots wide, 120° long and alpha .7, shard tails, testimony threads, the whip arc, the judgement's edges and the execution fill (only its cracks and lip opaque); each has at least four ramp tones, flowing noise, sparkle and a white-hot spine (the void eye takes a one-dot pearl lip), with pearl, bone and white at least 40% of the lit dots. Another player's light draws at 65% and their void (the execution's eye) at 60%. Reduced Effects shortens the arc to 80° at ×0.65, the wakes to 60% at ×0.7, halves residue and debris and drops the glow; bodies, forecasts, counts and sound stay. The owner's screen kicks on the throw (4.5), the bite (2.5), the stakes (2) and the execution (5.5) through `RitualWeaponFeedback`, following the shake setting.

**Sound.** New cues in `Assets/Sounds/Weapons/DollWeapons/`, owned by the [cue sheet](../../AUDIO_CUE_SHEET.md#doll-weapons-2026-10): every release pairs a warning with a firing (testimonies, throw, Axiom turns, return, stakes, execution), the throw that bites nothing and the execution that finds no one have their own miss cues, and the flight hums on two fixed-pitch loops (cruise and Axiom) crossfaded per stage. No bell; porcelain, brass and organ in F minor pentatonic. The finishing execution stays restrained: a porcelain crack and a soft, gong-like low brass strike ringing out for about 2.5 s over a quiet organ chord, never a loud organ stab.

**Nominal output** (raw, before defense, one target, every hit landing): six shards 6 × 2,710 plus the blade 52,272 (13,068 strike, 4 × 6,534 bites, 13,068 return) = **68,532 per score (7.08×)**, 14,581/s over 282 ticks and 14,479/s with the re-use gap (−0.7%); the best 600 ticks from a cold press land 139,774, the same as 0.2.x. A stealth score adds the judgement, 31,363 (3.24×), before Calamity's own stealth bonuses. **Area growth (outside the single-target budget):** the spinning disc reaches every enemy within 56 px of the turning blade, so crowds take up to four bites each; the per-root cap is unchanged.

**What changes for players** (0.2.34 → refresh):

- The relic becomes an execution blade: it hangs 94 px from the hand at 130 px long and is thrown at the same length.
- Testimony shards: still six at ×0.28 on the same ticks; they now leave from six seats along the blade's edge (before: alternately 12 px either side of a point 128 px out).
- Releasing after the seal (ticks 174–217): cancelled the throw → the throw completes.
- Blade hit shape: a 32 px wide swept line → a 56 px radius swept disc (more crowd hits; per-root cap unchanged).
- Blade split: 0.70 outbound / 0.30 return → 0.25 strike / 4 × 0.125 Axiom bites / 0.25 return; 5.4× per root unchanged.
- Flight: launch 60 px/tick easing to 34 while homing (the old blade updated twice a tick) → a steady 34 px/tick; spin 0.22 → 0.45 rad/tick (0.747 in the turns); turns home 42 ticks after the throw or 8 after a hit → bites, turns 21 ticks in the target and tears free at turn tick 22; caught at the player within 32 px → caught back into the hang within 28 px.
- Stealth: the verdict was cast 14–30 ticks into the flight on the nearest enemy → cast on the struck target as the turns begin (or where the blade stopped); its 185 px footprint, 16/28–31/56 timing and ×0.60 are unchanged.
- Per score 68,532 (7.08×) and the best cold 600 ticks 139,774: unchanged.
- New pixel art, icon, light and sound; the DollTheater cues are no longer played by this weapon.

## Curtainfall Treasure Box

**閉幕の宝箱 / Curtainfall Treasure Box** (`DollTreasureBox`) replaces the direct weapon drop in every difficulty. Accepted Victory creates the same frozen-party-count number of shared world drops at the Core. Right-click consumes one box and draws **one weapon**, uniformly from `NullRefrain`, `PaleMeridian`, `LacunaTestament`, `ChoirOfTheUnmade`, `LastWitness`: **20% each**, independent of Luck. Duplicate draws are possible; there is no guaranteed collection cycle. No weapon-to-weapon exchange recipes remain. The Doll is excluded from the box. There is no additional NPC death reward or private inventory grant.

Uniform selection uses `ItemDropRule.OneFromOptionsNotScalingWithLuck(1, options)` ([v2026.07 API](https://docs.tmodloader.net/docs/stable/class_item_drop_rule.html), checked 2026-09-12; package compilation checks the installed signature). The native `CanRightClick`/`ModifyItemLoot` container path owns consumption and contents; do not also spawn a weapon in `RightClick`. No `ItemID.Sets.BossBag` flag, since this is an all-difficulty treasure box without injected vanilla developer-armour drops. API checked 2026-09-12 against pinned tML [ModItem](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModItem.cs) and [ExampleMod bag](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod/Content/Items/Consumables/MinionBossBag.cs); independently implemented, no copied art/code.

## Lacrimosa's Claws — refresh (2026-10)

The owner-approved refresh of the melee box weapon: the boss's remote hands (Remote Clasp) and its Central Crush become the player's own technique. It replaced the 0.2.29 melee redesign and the 0.2.38 swipe cleanup on 2026-10-03; their text is [kept in history](../../history/2026-10-03-lacrimosa-claws-0229.md). The [shared rules](#reward-refresh-2026-10--shared-rules) apply.

What stays: the internal item `NullRefrain` and its names (断唱・虚掌 / Null Refrain — Lacrimosa's Claws), Calamity true melee through the compatibility adapter, base damage 7700 (`LacrimosaClawMotion.BaseDamage`), crit 8, Red rarity, 40 gold, its place in the [Curtainfall Treasure Box](#curtainfall-treasure-box) and as an ingredient of [The Unbroken Promise](#doll-companion--the-unbroken-promise). No packet, no protocol change (78), no Raid tuning and no Boss HP change.

**The hands.** The DW01 pixel art: a floating right porcelain hand and its mirror image, with ball-jointed fingers, long ivory talons, a black-iron cuff with a brass band and, on the back of each hand, a brass plate holding six dark glass **heart beads**. No arm, sleeve, thread or ribbon. While the claws are held and usable, one held controller keeps the hands beside the owner: the right hand floats above the front shoulder and the left above the back one, both pointing up and out, with a 2 px breathing bob. Hands and their light draw **in front of players** on the shared Doll weapon layer.

### Left click — three-step kata

Holding the button chains A → B → C → A; the next stroke starts on the tick after the last one ends. If no stroke starts within 45 ticks of a stroke's end, and after every grasp, the next one is A.

| Step | Motion | Base ticks | Live (base ticks) | Hit shape | × base |
|---|---|---|---|---|---|
| A · Down-rake | The right hand coils up and back (0–8), then rakes down through the aim | 26 | 9–16 | Right-hand capsule from 12 to 160 px along the hand axis, radius 44; the wrist rides a 104 px orbit, −1.76 → +1.27 rad about the aim | 0.85 |
| B · Up-rake | The left hand coils low behind during A, then rakes up through the aim | 24 | 7–13 | Left-hand capsule, same size; +1.80 → −0.86 rad | 0.85 |
| C · Clap | Both hands fling out wide (0–14), drive together flat along the aim and meet on the aim line at 22, then rebound to rest | 34 | 18–22 | One capsule per hand from the wrist to 196 px, radius 52; the wrists close from (80, ±150) to (84, ±50) px | 1.30 |

- **Rate.** A kata lasts 84 base ticks for 3.0× base: v1's rate of 1× per 28 ticks.
- **Attack speed.** Native true-melee attack speed shortens each stroke to `round(base / speed)` ticks, bounded A 12–90, B 11–90, C 16–90; the live window scales with it.
- **What hits.** Only the active hand's capsule (both hands in C), swept at 9 sub-samples across the last tick so high speed cannot tunnel, with `ownerHitCheck`. A hitbox is hit when the capsule's segment comes within its radius of the box (`LacrimosaClawMotion.CapsuleHitsBox`: round ends, so a body just past the tip or one large enough to contain the whole capsule is hit). Each logical NPC root is hit once per stroke; the clapping hands share one ledger. Wind-ups, the idle hand, rebounds and light never damage. Reach is about 305 px for the rakes and 366 px for the clap; nothing reaches beyond 430 px of the owner's centre.
- **Motion.** Each hand follows one 84-tick track (cubic Hermite through knots with Catmull-Rom tangents, the rest pose at both ends), so pose and speed carry across every stroke join without braking; the wrist's angular acceleration stays under 0.35 rad/tick² while raking. A stroke that starts away from its track (the first stroke, after a turn or a grasp) is shown blending from where the hand was during its harmless wind-up and is exactly on its track from its first live tick; hits always use the track.

### Heart beads

One meter per player, shown on both hands: 360 units, 60 per bead. Extra copies of the item cannot duplicate it.

- **Hits.** A stroke that connects adds 12 units (a rake) or 24 (the clap), once per stroke however many NPCs it hits; critters and town NPCs do not count, target dummies do. The amount is multiplied by the streak's tempo (×1 for its first two connecting strokes, ×1.25 for the 3rd–5th, ×1.5 from the 6th) and by (stroke ticks / base ticks), so attack speed does not change the fill per second. A streak ends 75 ticks after its last connecting stroke and restarts after every grasp.
- **Trickle.** While the claws are held and usable and no grasp is active, the meter also gains 1 unit every 4 ticks: empty to full in 24 s without a hit.
- **Pace.** With every stroke landing from a cold press at attack speed 1 the meter is full at tick 344 (v1 charged in 360 ticks of holding).
- **Freezing and loss.** Frozen while unequipped, Down, crowd-controlled or grasping; death and entering a world empty it.
- **Feedback.** Each bead lights with a ping and its own music-box note; the sixth adds a cadence and full beads beat like a heart (lub-dub every 48 ticks). Peers see the lit count: the controller carries it.

### Right click — Grasp

Grasping needs all six beads and spends them; a fresh right click during a stroke cancels only the owner's own stroke. Ages are real ticks from the click (age 1); attack speed never compresses them.

| Age | Beat |
|---|---|
| 1–2 | The beads go dark, the hands snap flat and draw back 20 px. The target point is the cursor, clamped to 1120 px. The nearest hostile NPC (one that can be chased, or a target dummy) whose hitbox lies within 160 px of that point is grasped, ties going to the lower slot; if there is none, the grasp is empty and stays at the point. The crush ellipse is forecast as a one-dot pearl-violet hairline with travelling heads until the crush. |
| 3–13 | Both hands fly to the target on mirrored bowed curves, fast start and braked arrival, leaving violet-pearl wakes. Harmless. |
| 14–15 | The hands open on either side of the target along a −0.30 rad diagonal and clench into fists. |
| 16–17 | **Contact: 0.3× to the grasped NPC root only**, no knockback. An empty grasp has no contact hit. |
| 18–37 | Hold: the fists press on both sides and follow the grasped NPC, which is never moved, slowed or stunned. Squeezes at 22, 28, 33 and 36 jolt the fists inward and relight the beads two by two; a dark core grows between the fists. |
| 38–39 | Brace: the fists part 14 px and the core swells. |
| 40–43 | **Crush: 4.0× to every NPC whose hitbox touches the axis-aligned ellipse of radii 166 × 132 px at the grasp centre** (v1's ellipse), once per logical root, knockback 1.4 × the item's. |
| 44–53 | The fists burst open, a black lacuna opens inside a violet ring with a vertical pearl flare and porcelain shatters; the hands fly home. |
| 54 | Strokes and the meter resume; the residue cools to plum by 64. |

- **Damage class.** Contact and crush are ordinary melee; only the raking and clapping hands take true-melee bonuses. Native defenses, immunities and hooks apply. No instant kill, invulnerability bypass or forced movement.
- **Lost target.** If the grasped NPC dies, despawns, changes type or jumps more than 64 px (plus its own speed) in one tick, the hands keep its last centre and still crush there.
- **Too early.** A right click with fewer than six beads only plays a dull brass tick and flickers the dark beads (owner only).
- **Aiming aid.** While all six beads are lit, brass corner brackets mark the NPC a grasp would take (owner only, harmless).

### Budget

Nominal raw numbers before defense, no crit, one target, every hit landing on its first live tick (`LacrimosaClawScore`, checked by the domain tests against the [shared baseline](#reward-refresh-2026-10--shared-rules)). Not measured DPS.

- **Per hit at 7700:** rake 6,545; clap 10,010; grasp contact 2,310; crush 30,800.
- **Kata:** 3.0× per 84 ticks = 16,500/s, as v1's swipes.
- **Steady cycle:** the 345-tick fill (13 strokes, 12.85×) plus contact 0.3× and crush 4.0× = 17.15× = 132,055 per 399 ticks against v1's 131,340 per 402 (+0.5%); **19,858/s against 19,603/s (+1.3%)**.
- **Best 600-tick window from a cold press** (grasping when full is best): 191,345 against 194,040 (−1.4%).
- **Attack speed:** 23,390/s at 1.25 and 27,008/s at 1.5 (v1 at the same speeds: 23,633 and 26,602).
- **Holding without hitting:** a grasp every 1,494 ticks = 1,330/s (v1: 4,827/s).

### Presentation

- **Pixel art.** One texel is one dot (2 world px), never scaled. Two integer rungs of the DW01 art: k=2 (`ClawOpen`, `ClawRake`, `ClawClench`, `ClawThrust`, the open hand about 94 px) while parked, winding up, returning and flying; k=1 (the `_L` sprites, 146–200 px) while raking, clapping and grasping. Rungs swap only on the fastest frames, under a two-tick pearl flash that steps the art's light tones to pearl and its dark ones to pearl grey (a two-tone hand, never a white silhouette); a pose change on the same rung, such as the fists closing at contact, does not flash. The raking and thrusting hands are turned by a fixed offset so their longest (rake) or middle front (thrust) talon lies on the capsule axis; the art-fit test keeps those tips and the fist's front within one dot of `RakeTip` 160, `ClapTip` 196 and `FistReach` 140. The left hand is the right-hand art mirrored about its own axis. The clench and thrust poses show four talon tips, as delivered.
- **Beads.** The six bead anchors the exporter found on every pose: lit beads are pearl-violet dots over the dark glass, the newest pings, and full beads flash white on each heartbeat.
- **Damaging phases** use the claw's own Luminance material, `DollClawEnergy.fx`, never a flat colour (four or more ramp tones, flowing noise, sparkle, a white-hot spine; pearl, bone and white at least 40% of lit dots, checked offline):
  - rakes leave three parallel claw-scratch ribbons beside the longest talon's path, exactly over the path the hits swept (white spine and pearl core, violet body, torn plum rim, brass glints at the head), lingering 10 ticks and cooling to plum;
  - the clap streaks the palms' approach, then opens a pearl slit, a violet ring growing to about 90 px and three organ-pipe breaths that rise from the slit's upper side, tallest in the middle, leaning a little outward and lifting off as they cool;
  - the crush rings out to about 150 px with a 330 px vertical pearl flare, then opens a black lacuna with a one-dot pearl lip among porcelain, spark and pearl debris; hot for 6 ticks, it cools to plum and its debris ends by grasp age 64, the grasp's last tick (by 52 with Reduced Effects);
  - hits show a small violet contact star and porcelain chips.
- **Other players** draw their claws' light at 65% and the lacuna at 60%; the hands stay opaque.
- **Reduced Effects** keeps the hands, beads, forecasts and live bodies; it halves debris and residue time, shows the clap and crush bursts at 80% without the pipe breaths and the heartbeat ring, and drops the glow. Screen shake (owner only, through `RitualWeaponFeedback.Kick`, off with Reduced Effects or Screen Shake off): clap 2.5, grasp contact 2, crush 7.
- **Removed:** the painted 0.2.x atlas hands, the parked-hand loop, the charge pips above the player and the world item's glow. No hit-stop, fullscreen flash, zoom, slow motion or HUD meter.
- **Icon:** `NullRefrainIcon` (DW01I, stored at 2×), drawn natively in the inventory and the world.

### Audio

All cues are new and live in `Assets/Sounds/Weapons/DollWeapons/`; [Audio](../../AUDIO_CUE_SHEET.md#doll-weapons-2026-10) owns their levels. Warning → firing pairs: `ClawRakeDownWarn` → `ClawRakeDownFire` (A), `ClawRakeUpWarn` → `ClawRakeUpFire` (B), `ClawClapWarn` → `ClawClapFire` (C), `ClawGraspWarn` → `ClawGraspFire`, or `ClawGraspMiss` on air, and `ClawCrushWarn` (which carries the four squeezes) → `ClawCrushFire`. `ClawHit` sounds on contact. The owner alone hears `ClawBead` (one F5 note moved up the ladder for beads 1–6), `ClawBeadsFull` and `ClawBeadDry`. Firing cues start a fixed lead before their event so the transient lands on it.

### Ownership and lifecycle

- **Ownership.** The owner client reads input, owns the meter and the combo and processes its hits. `LacrimosaClawKata` (one held controller, netImportant) carries the stroke, aim and age in `ai` and the stroke length, lit beads, stroke serial and the stroke's first impact in ExtraAI; `LacrimosaClawGrasp` (a child) carries the grasped NPC slot, its type and age in `ai` and the anchor and approach side in ExtraAI. Peers draw only from these. Raid outcomes stay authoritative on the server or in Single Player.
- **Ending.** An item change (at once on the owner, after 6 ticks on peers), death, Down, crowd control, `noItems` or leaving the world ends the controller and the grasp; spent beads are not refunded. Releasing the button only lets the current stroke finish.
- **Cleanup.** World unload drops the layer sources, arm poses and voices; Mod unload drops the material resolver; a Dedicated Server never loads art, shaders or audio.
- **Legacy.** `NullCantorClawSwipe`, `NullCantorClawCrush`, `NullCantorClawMotion` (and its charge), the swipe/crush presentation and their tests stay as unused legacy until the weapon cleanup; the item no longer reaches them. Only code keyed on the held claws was detached, as the shared rules allow: the parked-hand and charge-pip drawing, the world item's glow and `NullCantorClawPlayer` (the v1 charge and right-click crush, which fired whenever `NullRefrain` was held and would otherwise still launch the old crush from the old charge). `LacrimosaClawPlayer` owns the beads, the combo and the grasp instead.

### What changes for players

- **Left click:** alternating swipes (1.0× every 28 ticks, hands growing 0.68 → 2.30×, reach up to about 560 px) → a three-step kata (A 0.85×, B 0.85×, C 1.30× over 26/24/34 ticks: the same 1× per 28 ticks), rake reach about 305 px and clap about 366 px (cap 560 → 430).
- **Charge:** one charge after 360 ticks of holding → six heart beads filled mostly by hits (full after about 5.7 s of continuous hits, or 24 s of holding without hits).
- **Right click:** a 4.2× crush at the clicked point (impact at tick 16, 42 ticks without strokes) → a grasp of the NPC nearest the cursor (within 160 px): 0.3× contact at tick 16, 4.0× crush at tick 40 on the same 166 × 132 ellipse, the hands following the target; strokes resume at 54.
- **Holding without attacking:** 4,827/s → 1,330/s.
- **Totals:** sustained 19,603 → 19,858/s (+1.3%); best cold 10 s 194,040 → 191,345 (−1.4%).
- **Look and sound:** the painted P3 atlas hands and crescent sheets → the DW01 pixel hands with beads and the claw's own material, in front of the player; the charge pips are gone; a new inventory icon; 15 new cues replace ClawSwipe, ClawGrip, ClawCrush and ClawHit.

### Acceptance (owner; not_run until played)

- A → B → C flow at normal and high attack speed, and the reach (305 / 366 px against v1's 460–560).
- The parked hands' size (k=2, about 94 px, over the shoulders) and the k=1 swaps.
- Bead readability on bright and dark ground at zoom 1, and the full heartbeat.
- Grasp on a moving target, a boss, a worm and empty air; the aiming brackets.
- Reduced Effects; a second peer seeing the hands, beads and grasp; FPS with eight players.
- The cue pairs at unchanged sliders over each phase's music (audition page pending the owner).

## Scope and acquisition

User-approved concept: obsidian/ivory/aged-gold ritual machinery, hollow apertures and physical material opening before a bright release. This development change touches weapons only. Boss behavior, loot execution, mechanics, recovery and music are unchanged.

The [treasure box](#curtainfall-treasure-box) owns acquisition. The former 20 directed class exchanges and Choir ↔ Doll exchange are removed. Existing owned weapons remain usable; no inventory migration or deletion occurs.

## Long-form non-melee rituals — 0.2.34

The previous short cast/volley cycles are replaced, not merely slowed down. Each weapon has a **multi-second construction score**, with quick individual arrivals, a brief braking/tension beat, rapid final commitment, and continuous recovery. Do not compress the entire construction into one item-use animation when tuning the individual accents. `RitualGrandScore` owns real-tick milestones; attack-speed bonuses do not compress this score.

### Magic — Lacuna Testament

Refreshed 2026-10 under the [shared rules](#reward-refresh-2026-10--shared-rules); the 0.2.34 sigil-array text is in [history](../../history/2026-10-03-lacuna-testament-0234.md). Lacrimosa's core cannon (dark recesses open in the shell and violet light floods out) becomes the player's: porcelain irises open one by one round a black testament with a hole through it, each firing small void pellets; the seven then merge before the hand into one **great aperture** that pours out a sustained beam with a **black core**, a one-dot pearl lip and violet/pearl rims, pulsing and slowly widening. `LacunaTestamentScore` is the one owner of every tick, size and multiplier below; the held controller is `LacunaIrisChannel` and the pellets `LacunaPellet` (the 0.2.x `LacunaConvergence` and `LacunaRay` stay in the code, unused). Item: magic, channel, mana 8, base damage 2024, crit 8, Red, 40 gold, from the [box](#curtainfall-treasure-box) and in the [Promise](#doll-companion--the-unbroken-promise) recipe as before. Landed in 0.3.85; the owner approved its look and all seventeen cues on the audition and review page on 2026-10-03 (in-game play, a second peer and FPS remain `not_run`).

**Hold: the irises (ticks 0–339).** The testament floats upright 24 px past the hand, bobbing by one dot; on a steep upward aim it moves further out along the aim, just enough never to cover the player's head. Seven irises are born from its hole at ticks 0/108/183/233/269/296/316 (gaps 1.8/1.25/0.83/0.6/0.45/0.33 s) and glide to their seats in 12 ticks: an arch of radius 156 px round a point 10 px above the body centre, at 21°, 44°, … 159° from the facing side, keeping the facing of the press (and mirrored under reversed gravity). Each opens in four steps (closed, parting at +8, half at +11, open at +15) and fires its first pellet the moment it snaps open, then one every 38 ticks; before each later shot its petals half-close for 4 ticks, then snap open to fire. Pellets leave the actual hole toward the cursor at 36 px/tick, seek within 1800 px, deal 0.55× base, hit each NPC root once and live 150 ticks. No pellet fires at or after 340: 26 per full build, the 0.2.34 schedule unchanged.

**Merge (340–362) and charge (362–410).** At 340 every iris leaves its seat and accelerates along an arc to the muzzle, 150 px along the aim; they dock one by one at 350, 352, … 362 on a 68 px rosette, each lighting a pearl notch ring. At 362 the seven become the great aperture (the brass ring with folded porcelain petals round a see-through hole). It ratchets 7.5° on seven accelerating clicks at 368, 378, 386, 392, 397, 401 and 404 while violet arms spiral into the hole and its black centre grows to fill it; from 386 two pearl hairlines forecast the beam's opening edges. 404–410 is a still, silent breath. Nothing damages before the beam opens; aim turns at up to 0.075 rad/tick until 410.

**Beam (from 410, 6.83 s).** One persistent beam, not repeated shots, opens out of the aperture in 7 ticks, reaches 2600 px from the muzzle and ignores tiles. Its collision width starts at 92 px and widens (smootherstep) to 140 px, reached 6.0 s after 410, then holds; its mean over the widening is 116 px, the former constant. Within 48 px of the muzzle the throat collides at 0.78× the width. The drawn hard edge (a one-dot pearl silhouette) is exactly the collision width all the way to the far end, where only the black core closes; only a dithered halo and the pulses lie outside it, and they never hit. One hit per NPC root every 10 real ticks at 2.0× the current weapon damage (read every tick, so Mana Sickness and equipment count). A pulse leaves the hole every 30 ticks and runs down the beam at 65 px/tick; at +2, +4 and +6 s the ring ratchets one more notch as the beam widens. Aim follows the cursor at up to 0.033 rad/tick, easing to 0.027 at full width.

**Mana.** The item use pays 8 once. Construction pays 35% of the modified item cost, rounded up, every 30 ticks (ticks 30…390); the beam pays the modified cost every 8 ticks from 410 (60 mana/s at cost 8). Mana regeneration is delayed while holding; automatic missing-mana potions are blocked, manual potions work and zero-cost modifiers are honoured — all as before.

**Ending.** A release or an empty mana pool stops damage on that tick: the beam retracts into the hole within 10 ticks, the great aperture breaks back into seven irises that snap shut in reverse order, and everything is gone after 20 ticks; a new press waits for that fade. Releasing before the beam cancels: the open irises snap shut, spent mana is not refunded and pellets in flight keep flying. Running dry is the ritual failing, and it looks and sounds different from a release: damage still stops on that tick, but the beam gutters out where it is instead of retracting (lit, a gap, one weaker flash, gone within 6 ticks, no white flash; under Reduced Effects a plain fade with no flicker), the great aperture — or, during the build, each seated iris — cracks into falling porcelain and brass instead of snapping shut, every hole goes dark at once, the residue is plum from the start and sinks, and the lower, darker `LacunaBeamMiss` plays. Every peer sees and hears the same (the cause rides with the controller's sync). After a release the beam's residue cools from lilac to plum within 24 ticks. Item change, crowd control, `noItems`, death, or Doll-Raid Down or elimination end it at once with no fade and no damage, leaving only a short client-side crumble; pellets die when their owner becomes unusable.

**Ownership.** The owner client samples the cursor, pays mana and spawns pellets; the controller carries the age (`ai[0]`), the arch facing (`ai[1]`), the fade (`ai[2]`: 0 live, −1…−20) and the end cause (ExtraAI), and its velocity is the aim, resent every 6 ticks while turning and every 30 otherwise. Peers derive every iris, dock, click, width and pulse from that state. Native projectile replication only: no Encounter packet, no `ModPacket`, protocol unchanged.

**Look.** Drawn on the shared Doll weapon layer in front of every player, one texel per 2-px dot with point sampling: the testament is the k=2 book (21×28 dots, 42×56 px), chosen over the k=1 rung (84×110 px), which covered the whole front half of a 20×42 player at every aim in the zoom-1 composites; the irises are the 4-frame `LacunaIris` (k=2, 50 px) and the great aperture `LacunaGreatIris` (k=1, 156×160 px), turned only in its ratchet steps. The art is fitted to the design anchors (hole on the seat, mouth on the muzzle, rosette on the ring, each within one dot). Holes, pellets and beam use the original `DollLacunaEnergy` void material: a near-black core with plum streaks drifting inward and rare pearl sparks, a one-dot pearl lip with a violet seam behind it, pearl-and-violet rims whose folds flow outward on Luminance noise, the pearl silhouette, a dithered halo, travelling pulses and a white-hot opening that cools to violet in 10 ticks; inside the great aperture the beam is hidden so the brass ring stays in front of its own light. Each pellet contact closes a pearl ring on the target. The owner's own light draws at 80% and void at 75% (a Lacuna addition to the shared rule, which dims only other players; owner-approved 2026-10-03), so a boss forecast under the beam keeps a fifth (rims) to a quarter (core) of its contrast on dark and bright ground (measured by the offline preview with a stand-in Doll forecast; at the former 92% void only 8% showed); other players' light draws at 65% and their void at 60%. The art never covers the player's head at any aim (a domain test and a preview check every 10°), and everything is drawn from the same origin the beam collides from, so it stays on the player on stairs, slopes and mounts. Reduced Effects keeps every body, frame, ratchet, width, pulse and sound; it slows the flow, thins the sparks, halves the debris and residue and drops the glow; no shake beyond the shared Kick at the fire.

**Sound.** Seventeen new cues ([Audio](../../AUDIO_CUE_SHEET.md#doll-weapons-2026-10) owns levels): each iris's birth `LacunaIrisWarn` (petal slide and two brass detents at +8 and +11) and opening `LacunaIrisFire` (a porcelain clack, played as rendered) with its one music-box tooth `LacunaIrisTine`; each later shot's tell `LacunaPelletWarn` and shot `LacunaPelletFire` — the two single-pitch cues `LacunaIrisTine` and `LacunaPelletFire` climb F5 A♭5 B♭5 C6 E♭6 F6 A♭6 as the irises join, and nothing else is transposed; `LacunaMergeWarn` at 340 (gear spin-up and the seven dock ticks) and `LacunaMergeFire` at 362; `LacunaBeamWarn` at 362 (organ swell, suction, the seven baked ratchet clicks, cut at 404 for the silent breath) and `LacunaBeamFire` at 410 (a soft gong-like strike tuned to F with a long tail over a quiet organ swell, not a loud organ stab); the beam's 4.0 s loop `LacunaBeamLoop` (eight pulse periods, sample-exact, fading in and out); `LacunaWiden1`–`3` at +2/+4/+6 s; `LacunaBeamHit` at most once per 20 ticks on the owner's contacts; `LacunaPelletHit` only when a pellet really hit (never on a timeout or an unusable owner); `LacunaBeamEnd` on a release (quieter on a release before the great aperture forms) and the different, lower `LacunaBeamMiss` when mana runs out. The legacy `LacunaSustain.wav` and Magic masters stay for the boss and the companion.

**Budget (raw, before defense; not measured DPS).** Pellets 26 × 1113 (0.55× of 2024) = 28,938 per build; beam 4048 (2.0×) per root every 10 ticks = 24,288/s; best 600-tick window from a cold press 105,850 (26 pellets and 19 beam hits), and no release-and-repress timing beats holding. All three equal the [baseline](#reward-refresh-2026-10--shared-rules) exactly. Widening adds cross-section for crowds, not single-target damage.

**What changes for players** (old → new):

- Sigils: six staggered above and below behind the player → seven porcelain irises on an arch over the player (radius 156 px), born from the book's hole; timing of births, first shot at 0.25 s and the 38-tick cadence unchanged.
- Bolts → pellets: leave the drawn iris hole instead of a sigil centre; each later shot is told by a 4-tick half-closing of its iris; count (26), ×0.55, speed, seeking, 150-tick life and one hit per root unchanged.
- Merge: the array accelerated into the muzzle → irises dock one by one at 350–362 on a rosette round the muzzle; the aperture still forms at 362.
- Charge: 362–410 unchanged; now seven visible and audible ratchet clicks, a beam forecast from 386 and a silent breath at 404–410.
- Muzzle: 180 px → 150 px from the player; reach still 2600 px from the muzzle (the far end 2780 → 2750 px from the player).
- Beam width: constant 116 px → 92 px widening to 140 px over 6 s (mean 116 over the widening, then +20.7% cross-section); declared area growth outside the single-target budget.
- Throat: full width → 0.78× the width over the first 48 px.
- Aim in the beam: 0.033 rad/tick → 0.033 easing to 0.027 at full width (before the beam 0.075, unchanged).
- Damage and mana: unchanged (1113 per pellet, 4048 per beam hit every 10 ticks, 8 + 35% every 30 ticks + full cost every 8 ticks).
- Release: same 20-tick fade with damage stopping on the release tick; the beam now retracts within 10 ticks and the irises snap shut. Running out of mana now looks and sounds different from letting go: the beam gutters out where it is, the aperture or irises crack into falling porcelain, the residue is plum.
- Pellet hit sound and bite only on a real hit.
- Other players' Lacuna: full-strength energy → light at 65% and void at 60%. Your own: light at 80% and void at 75%, so boss forecasts show through your beam.
- Look and sound: painted V3 relic, sigils and the shared purple beam → the approved pixel book, irises and great aperture with the black-cored void beam; every cue new; new pixel inventory icon.

### Ranged — Pale Meridian (history)

Superseded by [Pale Meridian — refreshed ranged (2026-10)](#pale-meridian--refreshed-ranged-2026-10). The 0.2.34 form assembled one receiver plus four docking components (90/162/213/246 ticks) firing 15 homing needles ×0.95, compressed at 282–300, charged 48 ticks and overdrove from 348 (a needle every 3 ticks ×0.62, a heavy ×1.15 every 36) until release, which only stopped it; its `MeridianBastion`/`MeridianNeedle` types remain in the code, unused, until the set's legacy cleanup. The full text is in Git history.

### Summon — Choir of the Unmade

Replaced by the [2026-10 refresh](#choir-of-the-unmade-2026-10); the 0.2.34 concert is kept in [history](../../history/2026-10-03-choir-of-the-unmade-0234.md).

Last Witness's 0.2.34 score is replaced by its [2026-10 refresh](#rogue--last-witness); the old text is kept in [history](../../history/2026-10-03-last-witness-v1.md).

## Presentation and native ownership

This section describes the 0.2.x set; a refreshed weapon's own section (so far the [Lacuna Testament](#magic--lacuna-testament) and [Lacrimosa's Claws](#lacrimosas-claws--refresh-2026-10), which no longer use the shared legacy materials) supersedes it for that weapon.

Four new **text-only image generations** replace the non-melee icons and supply512px runtime apparatus artwork; no previous image was passed as input. The owner approved the built-in generator despite its unexposed backend model: do not label the results GPT Image2.5.128px inventory exports fit a116px maximum opaque envelope, inspected at40px as well. Original generated PNGs and all predecessor assets are preserved. Exact prompts, export procedure and provenance are in [Attribution](../../../Assets/ATTRIBUTION.md#ritual-grand-apparatus-v3--2026-09-09).

The original Luminance-managed `ArmamentEnergy` material replaces flat weapon strips with connected white energy, violet dark folds and flowing noise. Claw swipe/rogue trails and Magic/Choir/Doll beams share it, retaining their accepted geometry and attack clocks. Engraved, interlaced magic seals replace plain rings/ticks; physical apparatus artwork remains. Bolts receive luminous nuclei/wakes, and source vents/impacts receive the accepted Raid pressure/flare treatment. Magic has persistent flowing plasma with a full-width luminous throat, inward energy flow and one launch accent. Ranged has one progressively assembled receiver, connected recoil and continuous induction/motor sound. Choir layers voices/towers before one sustained organ release. The three original four-second sound beds loop continuously; damage ticks do not restart launch sounds. Their voices follow projectile lifetime and stop on cancel/unload. `ActiveSound.Volume` is a multiplier of `Style.Volume`; dynamic fading must not square the intended gain.

Render clocks and short-angle interpolation connect game ticks. Transparent, filamentary layers remain inside a readable continuous beam body, not independent decorative gaps. Reduced Effects reduces density/intensity and disables local shake; no fullscreen white pulses, forced zoom, time manipulation or UI/input ownership is introduced. The accepted melee geometry, controls, articulated art and charge are unchanged. Its five wide additive sheets are replaced with short separated fingertip filaments, so they do not fuse into a white crescent. The new physical cut/impact audio emphasizes low/mid weight and a brief attached air tail. Do not restore radial trail lines, black impact masses or a separate lingering swipe note.

Owner clients alone sample mouse/channel input, spend mana/ammo and create child projectiles, using Terraria's existing cooperative weapon replication. Other peers render/read native projectile state. Raid outcomes remain server-authoritative; this is not a new anti-cheat guarantee. Held controllers cancel on item change/death/Down/CC; launched ordinary projectiles retain normal flight lifetimes and terminate for unusable owners. World unload clears voices; Mod unload disposes material/mesh resources. No weapon-specific Encounter packet is introduced; all peers still need matching current content/protocol as recorded in [Status](../../STATUS.md).

## Video-driven playback correction

The [2026-09-14 recording/source analysis](../../research/2026-09-14-doll-playtest-video.md) distinguishes inspected reference frames from numeric audio evidence. Default-extraUpdates weapon and Doll PostAI hooks now run at native `numUpdates == -1` exactly once per real tick; the old zero-only guard skipped normal audio/interpolation updates. Bounded `RitualWeapon event=AudioVoice` diagnostics check actual voice acceptance, not only an asset path. The [audio sheet](../../AUDIO_CUE_SHEET.md#weapon-only-foley) owns the 12 remixed Claw/Ranged/Rogue/contact masters and unchanged other sounds. No reference-game audio, sprite or shader is imported.

## Initial power budget — not measured DPS

The unchanged seeds in `RitualArmamentRules` are provisional, not measured Calamity baselines. Magic construction bolts carry0.55x base damage; sustain hits carry2x every10 ticks (24288 nominal raw damage/sec at2024 base). Pale Meridian's current numbers are in [its section](#pale-meridian--refreshed-ranged-2026-10). The Choir's numbers moved to [its 2026-10 section](#choir-of-the-unmade-2026-10). Rogue pays six0.28x early shards plus one5.4x final returning-blade budget over4.7s, before native stealth; since the 2026-10 refresh that blade splits 0.25 strike / 4 × 0.125 Axiom bites / 0.25 return ([Last Witness](#rogue--last-witness)).

These are arithmetic bounds before defense, crits, armor/accessories, misses, movement and class hooks—not claims of endgame balance or measured DPS. The goal of a modest improvement over selected same-class final equipment needs matched in-game measurements. Do not change Boss HP to hide weapon imbalance.

## Doll companion — The Unbroken Promise

**ほどけない約束 / The Unbroken Promise** (`DollCovenant`) is an additional summon item, not a replacement for Choir of the Unmade. **Craft at a Work Bench using one of each of all five box weapons**, consuming all five and producing one Promise. No reverse recipe and no free Doll drop. The box options and recipe ingredients use the same `RitualArmamentItems.RewardTypes()` list so they cannot drift.

- Native `minionSlots = 10`, matching staff metadata and a capacity check. At least 10 maximum slots are required; only one Doll per owner. Native sacrifice replaces other sacrificial minions if needed. No slots, invulnerability or Raid participation are granted.
- Grounded follow uses gravity, collision/platform handling and a jump over small obstacles. **Any mounted owner, airborne owner (including horizontal flight/zero-velocity hover), or inverted gravity keeps the Doll broom-riding beside the owner.** Zero vertical velocity alone never proves grounding: three native solid-point probes below the owner's feet check support, including platforms. Dismounted, supported and vertically settled for six ticks permits landing only near the follow position and outside solid tiles. Large gaps/stuck paths still switch to broom catch-up; distant owners trigger an owner-synchronized teleport. Existing 16 flight/casting cels and brief pose blend mount/dismount the broom; attacks continue from its seated casting poses. The local owner changes native projectile `ai[2]` and requests synchronization on each transition; peers consume that mode, not independent mount guesses.
- Right-click enemy targeting is the ordinary minion targeting contract. Three irregular circle births fire violet needles, then merge toward the hand, brake briefly and accelerate into a **continuous purple beam**. Its live width and fade share collision timing; native local immunity and logical-NPC-root suppression prevent duplicate segmented hits. Timing, damage scales, range and frame bounds have one code owner: `DollCompanionRules`; nominal pre-defense output is a development seed, not measured DPS. Holding the 10-slot minion does not spend ongoing mana.
- Owner-client selects targets/mode and creates child projectiles through native Terraria projectile replication, as the other weapons do. Remote peers/server consume these native objects, not encounter packets. This is not server-side anti-cheat validation. The parent is owner/identity-bound, and children expire when the exact parent/buff disappears or the owner is Downed/dead/disabled. Down suspends attacks while retaining the companion; normal death, dismissal and native sacrifice remove it. No effect on Raid roster, Stack counts, Ready, revive or all-Down defeat.
- Actual pixel cels: original 36 × 48×64 ground/legacy poses retained, plus `DollBroom.png` with 16 × 96×80 flight/casting cels. Foot anchoring/palette reduction are mechanical exports of generated drawings. Code adds breath/tilt, target-facing, frame selection, continuous circle/beam materials and event-bound Raid-derived sounds. Original stage NPC and its capture remain separate. [Asset recipe](../../../tools/asset_recipes/doll_presentation_0257.json) owns the generation prompts and export contract.
- Narrow API check (2026-09-12, existing version-matrix target): [Mount.Active](https://docs.tmodloader.net/docs/stable/class_mount.html) identifies actual mounting, not flight capability; [Collision.IsWorldPointSolid](https://docs.tmodloader.net/docs/stable/class_collision.html) supplies point support with platforms included. The [pinned Collision patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Collision.cs.patch) extends platform handling to Mod tiles. These are read-only support observations, not player movement or collision mutation. Native slopes/platforms still require the scoped playtest below.
- User-owned smoke: summon at 9 vs 10 slots, sacrifice and coexistence with spare slots, stairs/platforms/flight/teleport, right-click retarget, attack/dismiss/Down cleanup, and a second peer observing one Doll with matching shots. No claim of actual in-game readability, DPS or network smoothness from build/CPU preview alone.

While flying, the Doll leaves up to 12 purple twinkles at its recent world positions (six in Reduced Effects), fading over 36 ticks. History resets on teleport/landing and is client-owned per projectile; it creates no combat particles or network state. Existing broom/cast cels are retained. Its seals and sustained beam use the same updated weapon materials.

Multiplayer lifetime: **only the owning player interprets missing `DollCovenantBuff` as dismissal**. Observers/server keep the replicated parent alive without requiring a locally present buff; child lifetime and sustained audio follow that exact parent identity. Owner death/disconnect, native sacrifice and replicated removal still retire it. `ReplicaActive` logs once per parent replica (owner/identity/local side), not per tick. This fixes the owner-visible/observer-invisible path found in the 2026-09-15 playtest; a fresh two-peer summon/dismiss/rejoin check is still required. Narrow API reference: [pinned ExampleSimpleMinion](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod/Content/Projectiles/Minions/ExampleSimpleMinion.cs), accessed 2026-09-15, demonstrates buff-maintained projectile lifetime. The ordering hazard is inferred from our unconditional `HasBuff` kill, not a claim that every peer always lacks every minion buff.

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
