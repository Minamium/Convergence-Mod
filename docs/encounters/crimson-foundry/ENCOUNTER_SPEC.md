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
  - Scarlet Invocation
  - Crimson Invocation (historical name)
  - scarlet conjurer
related_code:
  - Content/Encounters/CrimsonFoundry
  - Client/Encounters/CrimsonFoundry
related_docs:
  - project.status
  - adr.0026
---

# Scarlet Invocation

Player-facing Raid name: **Scarlet Invocation**. Boss: **Vespera — The Scarlet Conjurer**. The former Crimson name is retired to avoid suggesting a connection to Terraria's evil biome. Stable `CrimsonFoundry`, `crimson_foundry`, item, asset, packet and document IDs do not change. Vespera and her companion stay at the existing56px body reference; only her apparitions are large. [Status](../../STATUS.md) owns verification and integration state.

## Summoning and party

Use **Scarlet Grimoire / 紅蓮の魔導書** (`CrimsonConductor`) on the existing **Foundation Core** pedestal. Theater Doll still selects Doll on that same pedestal. Retain the reusable Book/Silk/Souls of Night prototype recipe, server-held-item/range/world/lease checks, server-wide1–8-player preparation, manual Ready and no synthetic participants. Roster changes before Ready acceptance reset confirmations. The frozen roster uses connection tokens; disconnect/world entry reset connection-local tokens and request cursors. Dead/disconnected participants remain out until the next Raid; late joiners spectate. **This feature still uses normal death, not Doll Down/revival.**

Preparation uses the same160×70-tile grounded logical field, bounded owner/server movement, replenished wings/rockets, no-wing lift and natural-spawn suppression. No terrain mutation, automatic rejoin, or unrelated encounter changes are added. Preparation expires after three minutes; the session retains its fifteen-minute fail-safe.

## Sequential acts and the four-target finale

| Act | Present combat target | Transition |
|---|---|---|
| I | **Ember Crown**: hollow crown, veil and thorn legs | At20% of its frozen maximum HP, retreat without dying |
| II | **Sable Mantle**: broad dark mantle, ivory mask and heart | At20%, retreat without dying |
| III | **Thorn Choir**: faceless shroud, horns/claws and tendrils | At20%, retreat without dying |
| Final | All three retained apparitions **and Vespera** | Defeat all four; an observed all-player wipe takes priority over a simultaneous clear |

Only Ember Crown appears in the introduction. Each threshold changes exactly one act. Native incoming damage is capped at the threshold in solo acts, with a CheckDead guard for overkill. A withdrawn NPC keeps its identity and exact20% HP, becomes invulnerable and fades out; it is not killed, looted, respawned or healed. The next apparition arrives during150protected ticks. Final recalls all retained bodies at their remaining HP and exposes Vespera after the same protected transition. The music never restarts at an act change.

The authority owns the phase, transition/unlock epochs, current target, final death mask and performer-defeated flag. Killing only the performer or only the three apparitions cannot win Final. A missing live or withdrawn actor invalidates the encounter, rather than being counted as a successful defeat. Phase changes clear all old hazards; source death in Final cancels that source's hazards. Cleanup covers all retained bodies, partly created actors and exact-pedestal ownership.

The existing total HP budget `12,000,000 + 8,000,000 × (participants − 1)` is still split into four equal targets at Ready acceptance. Solo phases spend80% of each apparition; Final spends the retained20% of each plus the full performer budget. There is no extra refill. Damage450/510 remains **native source damage**, not guaranteed HP loss; defensive equipment, dodge and native immunity remain enabled. Actual balance is uncalibrated.

## Movement and attack posture

The server retains a living frozen-roster target until it becomes invalid, then selects a living replacement. Ordinary movement loosely follows that player's position with apparition-specific standoff offsets, smoothed acceleration and a15px/tick desired-speed cap. Vespera stays above the field while directing solo acts and joins the loose pursuit in Final. World-space attack geometry is never dragged by a moving actor or player after its warning is issued.

During transition and accented Fill/Roll phrases, the bodies take authored upper/side/corner positions with a22px/tick desired-speed cap, then return to pursuit. These formations are presentation/movement, not per-player random targeting. Native NPC position/velocity plus the accepted phase/phrase epochs are replicated; observers do not choose targets or drive authority movement. The existing mesh rig adds charge/recoil and velocity lean. The floating performer now blends into the cast pose rather than remaining locked in the float frame. Ordinary NPC size and the0.3.12 draw-thread GPU lifetime fix are preserved.

## Rhythm choreography: call, breath, response

The player's reported problem was that sparse attacks did not feel like the underlying drum rhythm. The scheduler therefore admits **complete musical phrases**, not individual beat candidates discarded by a72–110tick cooldown. `CrimsonRhythm` consumes the existing measured `Score.json` beat times. It subdivides the actual adjacent beat intervals; there is no hardcoded128BPM loop or accumulated seven-/fourteen-tick rounding.

One four-beat phrase uses a16-position subdivision grid. Forecasts are the call; the corresponding attacks answer two beats later:

| Pattern | Forecast subdivision offsets | Impact offsets | Use |
|---|---|---|---|
| Groove | 0,2,4 | 8,10,12 | Ordinary eighth-note call/response |
| Fill | 0,1,3,5,6 | 8,9,11,13,14 | Every fourth phrase: compressed, uneven `ba-bam-bam-ba-bam` variation |
| Roll | 0,1,2,3 | 8,9,10,11 | Accented Final passages selected from the existing energy map |

For a local28-tick beat interval, Groove warnings occur at0/14/28 and impacts at56/70/84; Fill warnings at0/7/21/35/42 and impacts at56/63/77/91/98. These are illustrations, not a claim that the whole recording has constant tempo. There is a brief breath between the final forecast and the response. Forecasts pulse distinctly at their onsets and leave faint positional evidence; they do not turn into unannounced attacks during a blackout. Short, bounded existing SFX and body charge/recoil articulate each warning/impact once per volley, not once per lane. No explanatory combat-text HUD is restored.

All hits retain40–180ticks of warning under the supported score. Each live pulse lasts at most5ticks and ends before the next pulse; the visual tail is harmless. High visual/event frequency does **not** disable native immunity to force damage on every sixteenth note. Attack density and unavoidable damage are separate decisions.

## Safe corridors and network admission

Each phrase has one shared orientation and a gently drifting common corridor. Ember Crown favors vertical lanes, Sable Mantle horizontal bands, Thorn Choir diagonal lanes; Final mixes orientation **between phrases**. Adjacent offsets shift by28px in solo acts or20px in Final, so even the five-hit fill retains shared space for a whole player body. It does not demand a field-wide move every seven ticks or overlay incompatible live orientations. Final source assignments rotate among surviving apparitions and Vespera, making the ensemble alternate its accents without multiplying the damage fields.

The server publishes the entire bounded phrase at least30ticks ahead of its first forecast. Each immutable hazard records Fight, phase epoch, phrase serial, source, warning/fire/end and accent. A stale phase can never reactivate an old attack. Admission preflights the whole phrase (at most5hits ×40lanes); insufficient native projectile capacity aborts instead of silently losing individual beats. Normal packets do not trigger music restarts, and missed/late warnings are not replayed in a catch-up burst. This is **not** clock-offset or network-latency compensation; matching-peer late delivery and audio/visual/collision alignment still require actual playtests.

The current beat map is an analysis input, not a manually certified drum transcription. Audible downbeat alignment, the chosen fills' fit to the recording and perceived intensity remain owner-listening checks. Do not label them passed from a deterministic timing test.

## HP bar

Main and apparition NPCs explicitly assign `CrimsonBossBar`, which uses tModLoader's ordinary fancy-bar drawing path rather than depending solely on a dynamically set `NPC.boss` flag. During an individual act it shows that target's current/frozen maximum HP. During Final it shows the combined current HP against the remaining Final budget (three20% remnants plus the performer), regardless of which owned NPC vanilla selected to track. It is hidden in preparation/ending and on stale state. No bespoke combat instructions, decorative target rings, or new art are required; the standard fallback icon is used until an authored head icon exists. Other Mods' bar styles and a user-disabled boss bar cannot be overridden by this implementation.

## Companion

**Scarlet Covenant / 紅の盟約** remains the ordinary owner-replicated ten-slot companion, not a Raid participant, Ready vote or revival helper. Existing owner-only attack spawning/buff dismissal and exact parent identity remain. The floating cast-pose improvement is shared visually; no new companion gameplay, recipe gate or asset is introduced.

## Music and musical presentation

Music: **Graceful Ordeal — kuku**, provided by the owner with [the author's video](https://www.youtube.com/watch?v=HnBESyUqx_g), titled 「実はとてもお強いお嬢様からの試練BGM」. See [asset terms and exact hashes](../../../Assets/ATTRIBUTION.md#crimson-foundry--2026-09-15).

The original142.5s stereo48kHz PCM16 WAV remains external and untouched. The OGG preserves pitch, tempo and dynamics. Its numeric loop is sample821888→6456608 (17.122667→134.512667s), with a300ms smooth tail-to-pre-loop bridge. The original terminal fade/silence is not replayed. One native looping audio buffer plays the intro once then the loop; no timer-driven restart or accumulated tick rounding. The introduction/title and Ember Crown's arrival occupy the opening; the other apparitions arrive only at their phase boundaries. Measured intensity drives orb/casting tension and attack density.

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

Owner smoke: build/install a separate matching package; load/reload; start solo and with distant peers; confirm exactly one active apparition, all20% threshold/overkill transitions, stored HP and visible withdrawal/arrival; confirm loose pursuit, target loss and accented formations; Final exposes all four and requires every target; verify bar switching/aggregate health, wipe priority and exact-Fight re-summon. Listen to forecast/impact call-response and Fill/Roll accents against the actual recording. Check native immunity, latency, frame time and reduced effects. Domain, adapter and codec checks do not establish those gameplay results.
