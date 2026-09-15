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

Provisional Raid name: **Crimson Foundry**. Boss display name: **Ferrum — The Scarlet Machinist**. A red-haired mechanic directs articulated steel weaponry from a separate small command platform. The existing Foundation Core pedestal/field footprint is shared with Doll; neither its Gothic silhouette nor its recovery system carries over. [Status](../../STATUS.md) owns implementation and verification state.

## Summoning and party

Place the existing **Foundation Core** on clear ground, then hold **Crimson Conductor / 紅蓮の指揮装置** and click the pedestal. Theater Doll selects the other Raid on that same pedestal. An arbitrary midair item use no longer summons this encounter. The server validates the actual pedestal, range, field/world bounds and exclusive Fight lease. Temporary Conductor recipe: ten Iron Bars, twenty Wire and three Souls of Night at a Mythril/Orichalcum Anvil. Reusable; the inventory graphic references a vanilla mechanical component. Alternate item use cancels the summoner's Ready-stage preparation.

The server collects all connected non-ghost players, irrespective of distance (1–8; more than eight rejects the whole summon, never silently omits someone). A 150-tick deployment precedes clickable Ready controls above the local player's head. Other ready players show a small Ready! label. Dead players must respawn before confirming. Roster changes before acceptance clear everyone's confirmations. Once all confirm, the server freezes the connection-bound roster and schedules a music epoch 120 ticks ahead. Later arrivals and dead participants observe until the next summon. Everyone dead/disconnected ends in Defeat; **no Doll Downed/revival integration in this prototype**.

Preparation immediately establishes the same ground-anchored160×70-tile footprint as Doll, a visible red boundary and black exterior. Participants, including distant connected members, are contained in that rectangle on authority and the local owning client. Wings/rockets are replenished and no-wing jump lift is provided while participating; native equipment/damage otherwise remains intact. Natural NPC spawning is suppressed around participants, without deleting existing NPCs or modifying terrain. The machine moves inside the field; attacks freeze their world-space geometry at warning birth and extend to the field edges. Preparation expires after three minutes; the session has a fifteen-minute fail-safe. Cleanup releases the exact pedestal lease and removes the actor/hazards; field/audio/flight capability expires with that actor. One encounter per World remains the limit. This supersedes the original boundary-free request; see [ADR amendment](../../adr/0026-crimson-score-and-native-projectiles.md#shared-pedestal-and-bounded-stage-amendment--2026-09-15).

## Score, warnings and forms

`Assets/Music/CrimsonFoundry/Score.json` owns the measured beat/dynamics map. It is not a claim that the recording has a perfectly constant BPM. The server schedules immutable hazards from that map; clients cannot report a hit beat or move an accepted target. A 60-tick warning is retained even when events overlap. Positions/directions freeze at warning birth. Color remains crimson; warning axis + sparse footprint glints precede rapid luminous release. Native collision width grows with the same opening curve as the visible strike. Bolts have a visible travelling head/tail, not a teleporting filled rectangle.

- **Armored form:** long-limbed steel frame with attached chest/shoulder/thigh plates, articulated recoil, angular head and a central moving plasma reactor. Diagonal/horizontal/vertical cuts and red energy bolts are introduced by joint tension, reactor charge and source glow. Quiet passages leave several beats between attacks; stronger phrases add parallel lanes and selected offbeats.
- **50% armor purge:** HP cannot skip this reveal via a lethal burst. Select the next measured strong accent within 0.8s (otherwise a short half-second fallback). Attacks clear; armor fragments accelerate outward and the exposed craft emerges. The soundtrack and score epoch continue unchanged. Ninety ticks of protected transition prevent invisible hits during the reveal.
- **Unarmored form:** the exposed slender humanoid machine remains after its actual attached plates separate. Long asymmetric arm/leg poses, sharper banking, continuous crimson exhaust/afterimages and faster orbital movement replace the old squat craft. The small mechanic remains separate. No contact damage; all danger remains explicitly forecast.
- **Ending:** the accepted result clears hazards immediately; a short exit/bars/audio tail precedes idempotent actor teardown and a new summon opportunity. This is an initial result presentation, not a completed bespoke rewards/ending sequence.

Source damage starts at450/510 by form and uses ordinary hostile Projectile damage, armor, accessory and dodge hooks. It is neither percent-HP damage nor direct `statLife` subtraction. Maximum Boss HP is `12,000,000 + 8,000,000 × (participants − 1)`, frozen when spawned, so the initial musical prototype is not over after only a few seconds of endgame DPS. These are provisional balance values, not calibrated difficulty claims.

Slash release lasts42ticks rather than18, with a seven-tick travelling opening and a fourteen-tick non-damaging dissipation tail. Bolts travel over24ticks with an explicit210px tail. Warning remains60ticks; it is not shortened to compensate for longer paths. `CrimsonHazard` owns collision/reach curves consumed by presentation. The Boss projects the same vulnerability rule on server and clients: Performance only, excluding the purge-protection interval. Progress logs every five seconds include remaining/maxHP, interval damage/DPS, stage and vulnerability; they measure received NPC damage, not theoretical loadout DPS.

## Music and musical presentation

Music: **Graceful Ordeal — kuku**, provided by the owner with [the author's video](https://www.youtube.com/watch?v=HnBESyUqx_g), titled 「実はとてもお強いお嬢様からの試練BGM」. See [asset terms and exact hashes](../../../Assets/ATTRIBUTION.md#crimson-foundry--2026-09-15).

The original142.5s stereo48kHz PCM16 WAV remains external and untouched. The current local OGG edit preserves pitch, tempo and dynamics. Its numeric loop is sample821888→6456608 (17.122667→134.512667s), with a300ms smooth tail-to-pre-loop bridge. The final original fade/silence is not replayed. A single native looping audio buffer plays the intro once then the loop; there is no timer-driven restart per cycle. The sample-based modulo prevents accumulated tick rounding at later loops. Intro/title deployment follows the first eight seconds; measured musical intensity also drives furnace/exhaust and attack density. Quiet later passages remain quieter, not constant full aggression.

`CrimsonAudio` decodes the OGG once on clients, uses the Music slider rather than the SFX slider, and disposes its voice on World exit/unload. Focus/pause recovery starts at the accepted score position, not the beginning. A large clock discrepancy can re-anchor on an accent; normal packets and the armor purge do not restart the track. Server timing remains independent of the sound card. Actual device latency, focus recovery and multiplayer drift still need listening/playtest checks.

The playtest mix reduces the music voice ceiling from0.88 to0.39 (about7dB), without rewriting/normalizing the recording or changing the loop. Current project-authored Portal charge/fire sounds replace obsolete direct Lance/Slicer asset paths; bounded shared cues prevent one voice per multiplayer lane. Cue gains and fourteen-tick voice release live in `CrimsonVisuals`; leave Doll's accepted mix unchanged. Numerical comparisons against Doll are in the [revision evidence](../../evidence/2026-09-15-crimson-stage.json); they do not establish identical perceived loudness on every device.

**Distribution:** the owner approved this game-facing loop edit for both the Mod and its public source repository on2026-09-15. The OGG, score and bundled `Credits.txt` travel together. [Attribution](../../../Assets/ATTRIBUTION.md#crimson-foundry--2026-09-15) owns the checked terms, project-use decision and explicit exclusion from Convergence's code/asset license; no standalone music license is asserted. A damaged/incomplete package missing this recording keeps the other encounters available and rejects this summon with `crimson.licensed_music_not_installed`; normal repository builds include the music.

Recreate the local edit from an authorized WAV using:

```powershell
python tools/prepare_crimson_score.py "<authorized Graceful Ordeal.wav>" --report .local/crimson/analysis.json --export Assets/Music/CrimsonFoundry --ffmpeg "<ffmpeg executable>"
```

This also creates an external `loop-seam-audition.wav`. Numerical onset/chroma/RMS matching is not subjective approval of the seam. If revised, export both OGG and Score together and build matching peers. No independent music-only upload or off-device backup is implied.

## Presentation ownership and remaining work

`FoundryRig.png` is a new original twelve-part transparent atlas: torso/reactor housing, head, upper/lower arm, pelvis, thigh, shin/foot, chest armor, shoulder armor, thigh armor, engine and the small operator/platform. Explicit UV rectangles retain whole limbs without adjacent-cell contamination. Previous heavy/exposed illustrations remain preserved but are no longer the active Boss composite. Textures supply surfaces/silhouettes; `CrimsonRig` supplies independent joints, attached armor separation, tension/recoil, banking and fractional-frame interpolation. This is articulated animation, not a newly painted frame-by-frame sheet. Small pilot expressions and bespoke rewards remain later work.

`CrimsonReactor.fx` supplies contained turbulent plasma, a white-hot core, travelling filaments and release pressure. Existing project-authored PortalBeam/RaidEnergy materials supply narrow forecasts, sparse footprint particles, bright moving heads, continuous bodies, corona and source mouths. Luminance supplies its dependency-owned noise at runtime. [WoTM/video observations](../../research/WOTG_RAID_BENCHMARK.md#f17--crimson-articulated-machine-and-energy-release-2026-09-15) informed independent implementation; no third-party art, shader or recording is copied.

Physical-pixel Ready/bars use the captured world transform once; no UI-scale/world-scale mixing. Reduced Effects and shake-off preserve attack footprints and warning timing. Bounded visual commands never allocate textures per frame. Dedicated Server loads score facts but no graphics/audio device.

Owner smoke: solo summon/Ready/start, three-player distant member admission, every peer seeing the same locked warnings,50% purge while the track continues, natural loop at about134.5s, focus/pause, wipe/victory/re-summon, and UI107%/zoom. Source/codec/build checks cannot replace these observations.
