---
doc_id: encounter.crimson-foundry.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - art
  - audio
last_reviewed: 2026-09-16
source_of_truth_for:
  - encounter.crimson_foundry.experience
  - encounter.crimson_foundry.music
aliases:
  - red mechanic raid
  - Crimson Invocation
  - scarlet conjurer
related_code:
  - Content/Encounters/CrimsonFoundry
  - Client/Encounters/CrimsonFoundry
related_docs:
  - project.status
  - adr.0026
---

# Crimson Invocation

Provisional Raid name: **Crimson Invocation**. Boss display name: **Vespera — The Scarlet Conjurer**. A red-haired, long-twin-tailed summoner controls a red energy sphere and three large apparitions. **The woman herself always stays ordinary NPC size, including the Final and companion; only her apparitions are large.** This replaces the steel-machine/armor-purge concept, not Doll's encounter. Stable `CrimsonFoundry` code, document and network IDs remain unchanged. [Status](../../STATUS.md) owns implementation and verification state.

## Summoning and party

Place the existing **Foundation Core** on clear ground, then hold **Crimson Grimoire / 紅蓮の魔導書** (internal item `CrimsonConductor`) and click the pedestal. Theater Doll selects the other Raid on that same pedestal. The server validates the actual pedestal, range, field/world bounds and exclusive Fight lease. Temporary recipe: one Book, ten Silk and three Souls of Night at a Bookcase. The new icon is a compact black/red grimoire clasped around a crimson orb. Reusable; alternate item use cancels the summoner's Ready-stage preparation.

The server collects all connected non-ghost players, irrespective of distance (1–8; more than eight rejects the whole summon, never silently omits someone). A 150-tick deployment precedes clickable Ready controls above the local player's head. Other ready players show a small Ready! label. Dead players must respawn before confirming. Roster changes before acceptance clear everyone's confirmations. Once all confirm, the server freezes the connection-bound roster and schedules a music epoch 120 ticks ahead. Later arrivals and dead participants observe until the next summon. Everyone dead/disconnected ends in Defeat; **no Doll Downed/revival integration in this prototype**.

Preparation immediately establishes the same ground-anchored160×70-tile footprint as Doll, a visible red boundary and black exterior. Participants, including distant connected members, are contained on authority and the local owning client. Wings/rockets are replenished and no-wing jump lift is provided; native equipment/damage otherwise remains intact. Natural NPC spawning is suppressed without deleting existing creatures or modifying terrain. Preparation expires after three minutes; the session has a fifteen-minute fail-safe. Cleanup releases the exact pedestal lease and all owned actors/hazards; field/audio/flight expires with the main actor. One encounter per World remains the limit. See [shared-stage amendment](../../adr/0026-crimson-score-and-native-projectiles.md#shared-pedestal-and-bounded-stage-amendment--2026-09-15).

## Score, warnings and targets

`Assets/Music/CrimsonFoundry/Score.json` owns the measured beat/dynamics map, not a perfectly constant BPM claim. The server schedules immutable hazards; no player position enters a barrage layout. Each volley fills the field outside **one shared, reachable corridor**, with cardinal/diagonal orientation and deterministic cue-based offsets. A60-tick warning includes the thin axis, sparse footprint glints and clearer paired edges; collision grows with the visible opening. Normal/Final minimum volley spacing is90–110/72ticks, still quantized to accepted musical cues. Different orientations do not resolve simultaneously into incompatible safe zones.

- **Introduction:** Ready acceptance starts the cinematic immediately. Bars remain continuous across the120-tick music lead. The performer floats from the pedestal toward the upper background; the three apparitions manifest on measured intro beats. Combat waits for the eight-second musical intro. No sudden replacement of a giant PNG.
- **Ember Crown:** hollow ivory/obsidian crown, crimson veil and three thorn legs; vertical energy lanes.
- **Sable Mantle:** broad asymmetric silk apparition with an ivory mask and red heart; horizontal travelling energy bands.
- **Thorn Choir:** tall faceless shroud, crooked horns/claws and tendrils; diagonal lanes.
- **Independent defeats:** each apparition has its own ordinary native NPC HP and accepts participant weapon damage on every peer. Defeating one removes its hazards and future actions. Despawning/lost identity is an error, never a substitute for killing it.
- **Final:** only all three confirmed defeats trigger150ticks of protected manifestation. The same56px performer moves forward, never grows; she becomes damageable and mixes the corridor orientations. The BGM/score does not restart. The former50% armor purge no longer exists.
- **Ending:** the accepted result clears hazards immediately; a short bars/audio exit precedes exact-Fight cleanup and re-summoning. There is no contact damage, bespoke treasure table or finished defeat animation in this prototype.

Source damage starts at450/510 (apparitions/Final) through ordinary hostile Projectile armor/accessory/dodge hooks, never percent-HP or direct `statLife` subtraction. Total HP budget remains `12,000,000 + 8,000,000 × (participants − 1)`, now split equally between the three apparitions and performer, frozen at Ready acceptance. Solo:3million each; three players:7million each. These are provisional, not calibrated difficulty claims.

Slash release lasts42ticks, with a seven-tick opening and fourteen-tick harmless dissipation. Bolts travel over24ticks with a210px tail. `CrimsonHazard` owns shared collision/reach curves. `CrimsonInvocation` owns masks, Final protection, musical fade and barrage geometry. Progress logs every five seconds include combined remaining/maxHP, interval received damage/DPS, defeated mask and Final state; summon appearance/death records identify each target. Not theoretical loadout DPS.

## Companion

**Scarlet Covenant / 紅の盟約** (`CrimsonPact`) summons the same NPC-sized woman as an ordinary ten-slot minion. She walks on ground; mount/flight/lack of support selects floating follow, with red particle traces and an orbiting energy orb. The orb charges then sustains a red native Summon beam. New companion icon: ivory clasp, red star gem and twin-tail ribbon. Temporary recipe: one Crimson Grimoire, ten Silk and five Souls of Night at a Bookcase. No raid-win prerequisite yet.

Owner alone spawns attacks/chooses locomotion/dismisses for a missing buff; remote peers do not kill the minion merely because private owner buffs are absent. Child beams bind exact owner+projectile identity. Death/buff removal cleans them up. This minion is **not** a Raid participant, Ready vote or revival helper; future companion substitution remains separate work.

## Music and musical presentation

Music: **Graceful Ordeal — kuku**, provided by the owner with [the author's video](https://www.youtube.com/watch?v=HnBESyUqx_g), titled 「実はとてもお強いお嬢様からの試練BGM」. See [asset terms and exact hashes](../../../Assets/ATTRIBUTION.md#crimson-foundry--2026-09-15).

The original142.5s stereo48kHz PCM16 WAV remains external and untouched. The OGG preserves pitch, tempo and dynamics. Its numeric loop is sample821888→6456608 (17.122667→134.512667s), with a300ms smooth tail-to-pre-loop bridge. The original terminal fade/silence is not replayed. One native looping audio buffer plays the intro once then the loop; no timer-driven restart or accumulated tick rounding. Intro/title and the three manifestations follow the first eight seconds. Measured intensity drives orb/casting tension and attack density.

`CrimsonAudio` decodes once on clients, uses the Music slider and disposes on World exit/unload. It fades from silence over150score ticks (2.5s) to the previous calibrated ceiling; late join/focus reanchors also have a short recovery envelope. Normal packets and Final do not restart the track. Server timing is independent of the sound card. Actual device latency, perceived fade/loop and multiplayer drift still require listening/playtest checks.

The playtest mix reduces the music voice ceiling from0.88 to0.39 (about7dB), without rewriting/normalizing the recording or changing the loop. Current project-authored Portal charge/fire sounds replace obsolete direct Lance/Slicer asset paths; bounded shared cues prevent one voice per multiplayer lane. Cue gains and fourteen-tick voice release live in `CrimsonVisuals`; leave Doll's accepted mix unchanged. Numerical comparisons against Doll are in the [revision evidence](../../evidence/2026-09-15-crimson-stage.json); they do not establish identical perceived loudness on every device.

**Distribution:** the owner approved this game-facing loop edit for both the Mod and its public source repository on2026-09-15. The OGG, score and bundled `Credits.txt` travel together. [Attribution](../../../Assets/ATTRIBUTION.md#crimson-foundry--2026-09-15) owns the checked terms, project-use decision and explicit exclusion from Convergence's code/asset license; no standalone music license is asserted. A damaged/incomplete package missing this recording keeps the other encounters available and rejects this summon with `crimson.licensed_music_not_installed`; normal repository builds include the music.

Recreate the local edit from an authorized WAV using:

```powershell
python tools/prepare_crimson_score.py "<authorized Graceful Ordeal.wav>" --report .local/crimson/analysis.json --export Assets/Music/CrimsonFoundry --ffmpeg "<ffmpeg executable>"
```

This also creates an external `loop-seam-audition.wav`. Numerical onset/chroma/RMS matching is not subjective approval of the seam. If revised, export both OGG and Score together and build matching peers. No independent music-only upload or off-device backup is implied.

## Presentation ownership and remaining work

`ScarletConjurer.png` is an original transparent four-pose pixel sprite sheet (idle/cast/float/walk). Explicit UVs and body pivots keep the same56px body reference; both Boss and companion share it. Code adds breathing, pose blending, floating and subtle hair/cloth deformation; four poses are not a claim of newly painted60fps frame animation. `EmberCrown.png`, `SableMantle.png`, `ThornChoir.png` supply distinct silhouettes. A shared fractional-clock24×32 mesh continuously deforms veils/tendrils and casting tension/recoil, instead of translating rigid images. Luminance energy orbs and source mouths complete the action. Old FoundryRig/Engine/Unbound originals remain preserved but inactive.

`CrimsonReactor.fx` supplies contained turbulent plasma, a white-hot core, travelling filaments and release pressure. Existing project-authored PortalBeam/RaidEnergy materials supply narrow forecasts, sparse footprint particles, bright moving heads, continuous bodies, corona and source mouths. Luminance supplies its dependency-owned noise at runtime. [WoTM/video observations](../../research/WOTG_RAID_BENCHMARK.md#f17--crimson-articulated-machine-and-energy-release-2026-09-15) informed independent implementation; no third-party art, shader or recording is copied.

Physical-pixel Ready/bars use the captured world transform once; no UI-scale/world-scale mixing. Reduced Effects and shake-off preserve attack footprints and warning timing. Bounded visual commands never allocate textures per frame. Dedicated Server loads score facts but no graphics/audio device.

Owner smoke: solo/shared-pedestal Ready/start, distant multiplayer admission, each apparition taking independent native damage, dead targets ceasing attacks, all-three-only Final with unchanged NPC size, usable corridor travel, natural loop/fade/focus, wipe/victory/re-summon, UI107%/zoom, remote companion visibility and mount/landing/owner dismissal. Source/codec/build/offline-render checks cannot replace these observations.
