---
doc_id: encounter.crimson-foundry.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-09-15
source_of_truth_for:
  - encounter.crimson_foundry.experience
  - encounter.crimson_foundry.music
aliases:
  - red mechanic raid
related_code:
  - Content/Encounters/CrimsonFoundry
  - Client/Encounters/CrimsonFoundry
related_docs:
  - project.status
  - adr.0026
---

# Crimson Foundry

Provisional Raid name: **Crimson Foundry**. Boss display name: **Ferrum — The Scarlet Machinist**. A red-haired mechanic pilots steel weaponry; neither the Gothic Doll silhouette nor Ghost Samurai's arena/recovery rules carry over. [Status](../../STATUS.md) owns implementation and verification state.

## Summoning and party

Use **Crimson Conductor / 紅蓮の指揮装置**. Temporary recipe: ten Iron Bars, twenty Wire and three Souls of Night at a Mythril/Orichalcum Anvil. Reusable; the initial inventory graphic is a referenced vanilla mechanical component, not a copied asset. Right-click cancels the summoner's Ready-stage preparation.

The server collects all connected non-ghost players, irrespective of distance (1–8; more than eight rejects the whole summon, never silently omits someone). A 150-tick deployment precedes clickable Ready controls above the local player's head. Other ready players show a small Ready! label. Dead players must respawn before confirming. Roster changes before acceptance clear everyone's confirmations. Once all confirm, the server freezes the connection-bound roster and schedules a music epoch 120 ticks ahead. Later arrivals and dead participants observe until the next summon. Everyone dead/disconnected ends in Defeat; **no Doll Downed/revival integration in this prototype**.

No arena rectangle, invisible wall, ejection, forced teleport or exterior black mask. Players retain normal controls, terrain, flight resources and equipment. The Boss follows the living roster; attacks appear in nearby fixed world space. Preparation expires after three minutes; an entire session has a fifteen-minute fail-safe. Existing one-encounter-per-world exclusion remains.

## Score, warnings and forms

`Assets/Music/CrimsonFoundry/Score.json` owns the measured beat/dynamics map. It is not a claim that the recording has a perfectly constant BPM. The server schedules immutable hazards from that map; clients cannot report a hit beat or move an accepted target. A 60-tick warning is retained even when events overlap. Positions/directions freeze at warning birth. Color remains crimson; warning axis + sparse footprint glints precede rapid luminous release. Native collision width grows with the same opening curve as the visible strike. Bolts have a visible travelling head/tail, not a teleporting filled rectangle.

- **Armored form:** heavy furnace chassis, deliberate low-amplitude hover, diagonal/horizontal/vertical cuts and red energy bolts. Quiet passages leave several beats between attacks; stronger phrases add parallel lanes and selected offbeats.
- **50% armor purge:** HP cannot skip this reveal via a lethal burst. Select the next measured strong accent within 0.8s (otherwise a short half-second fallback). Attacks clear; armor fragments accelerate outward and the exposed craft emerges. The soundtrack and score epoch continue unchanged. Ninety ticks of protected transition prevent invisible hits during the reveal.
- **Unarmored form:** the same mechanic pilots an exposed, narrow scythe-shaped steel craft. Faster orbital pursuit, red exhaust/afterimages, additional strong-passage lanes/offbeats. No contact damage; all danger remains explicitly forecast.
- **Ending:** the accepted result clears hazards immediately; a short exit/bars/audio tail precedes idempotent actor teardown and a new summon opportunity. This is an initial result presentation, not a completed bespoke rewards/ending sequence.

Source damage starts at450/510 by form and uses ordinary hostile Projectile damage, armor, accessory and dodge hooks. It is neither percent-HP damage nor direct `statLife` subtraction. Maximum Boss HP is `12,000,000 + 8,000,000 × (participants − 1)`, frozen when spawned, so the initial musical prototype is not over after only a few seconds of endgame DPS. These are provisional balance values, not calibrated difficulty claims.

## Music and musical presentation

Music: **Graceful Ordeal — kuku**, provided by the owner with [the author's video](https://www.youtube.com/watch?v=HnBESyUqx_g), titled 「実はとてもお強いお嬢様からの試練BGM」. See [asset terms and exact hashes](../../../Assets/ATTRIBUTION.md#crimson-foundry--2026-09-15).

The original142.5s stereo48kHz PCM16 WAV remains external and untouched. The current local OGG edit preserves pitch, tempo and dynamics. Its numeric loop is sample821888→6456608 (17.122667→134.512667s), with a300ms smooth tail-to-pre-loop bridge. The final original fade/silence is not replayed. A single native looping audio buffer plays the intro once then the loop; there is no timer-driven restart per cycle. The sample-based modulo prevents accumulated tick rounding at later loops. Intro/title deployment follows the first eight seconds; measured musical intensity also drives furnace/exhaust and attack density. Quiet later passages remain quieter, not constant full aggression.

`CrimsonAudio` decodes the OGG once on clients, uses the Music slider rather than the SFX slider, and disposes its voice on World exit/unload. Focus/pause recovery starts at the accepted score position, not the beginning. A large clock discrepancy can re-anchor on an accent; normal packets and the armor purge do not restart the track. Server timing remains independent of the sound card. Actual device latency, focus recovery and multiplayer drift still need listening/playtest checks.

**Distribution:** the owner approved this game-facing loop edit for both the Mod and its public source repository on2026-09-15. The OGG, score and bundled `Credits.txt` travel together. [Attribution](../../../Assets/ATTRIBUTION.md#crimson-foundry--2026-09-15) owns the checked terms, project-use decision and explicit exclusion from Convergence's code/asset license; no standalone music license is asserted. A damaged/incomplete package missing this recording keeps the other encounters available and rejects this summon with `crimson.licensed_music_not_installed`; normal repository builds include the music.

Recreate the local edit from an authorized WAV using:

```powershell
python tools/prepare_crimson_score.py "<authorized Graceful Ordeal.wav>" --report .local/crimson/analysis.json --export Assets/Music/CrimsonFoundry --ffmpeg "<ffmpeg executable>"
```

This also creates an external `loop-seam-audition.wav`. Numerical onset/chroma/RMS matching is not subjective approval of the seam. If revised, export both OGG and Score together and build matching peers. No independent music-only upload or off-device backup is implied.

## Presentation ownership and remaining work

Two original generated transparent textures own the heavy/exposed silhouettes. Code owns hover/banking, segmented armor shedding, connected exhaust, afterimages and red Luminance energy. Existing project-authored PortalBeam/RaidEnergy shader materials are reused through a feature renderer; Luminance's textures remain supplied by the dependency. The first silhouette is a static illustration with procedural pose/parts, **not a newly drawn full animation atlas**. Small pilot expressions, independently rigged chassis parts, custom new SFX/rewards and final balance remain later work.

Physical-pixel Ready/bars use the captured world transform once; no UI-scale/world-scale mixing. Reduced Effects and shake-off preserve attack footprints and warning timing. Bounded visual commands never allocate textures per frame. Dedicated Server loads score facts but no graphics/audio device.

Owner smoke: solo summon/Ready/start, three-player distant member admission, every peer seeing the same locked warnings,50% purge while the track continues, natural loop at about134.5s, focus/pause, wipe/victory/re-summon, and UI107%/zoom. Source/codec/build checks cannot replace these observations.
