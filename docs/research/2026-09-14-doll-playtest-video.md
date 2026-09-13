---
doc_id: research.doll-video-20260914
document_type: research
status: provisional
owners:
  - art
  - gameplay
last_reviewed: 2026-09-14
source_of_truth_for: []
aliases:
  - Doll weapon audio missing
  - Now with more ducks recording
related_code:
  - Client/Encounters/FirstSeverance
  - Content/Encounters/FirstSeverance/FirstSeveranceCoreCannonVolley.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceImpalingSwords.cs
  - tools/remix_weapon_impact.py
related_docs:
  - project.status
  - encounter.first-severance.visual
  - encounter.first-severance.weapons
  - research.wotg-raid-benchmark
---

# Doll playtest: video, audio scheduling and revised presentation

## Evidence and limits

User recording: **Terraria_ Now with more ducks! 2026-09-14 04-48-12.mp4**, 404,548,981 bytes, SHA256 `78f3222c666d976cabed87a96dc4ebe351baee5d55bf10ca8a2cf322df3f96e0`. H.264, 1920×1012, approximately 333.92s, variable cadence averaging 29.61fps; AAC stereo/48kHz. This does not prove the game's rendering FPS. Local derivatives include 12-second overview sheets, full-resolution detail frames, closely spaced weapon sequences and a decoded PCM analysis copy. They are retained outside Git under the ignored `video-0277` evidence directory; the recording/original assets are not redistributed.

**Visual frames were actually inspected. Audio was decoded/measured, not personally listened to.** Event positions, silhouettes, attached trails and visible chronology below are observations. Musical/timbral acceptance, room/device loudness and multiplayer readability still require user testing. The mixed soundtrack cannot isolate one game's sample or establish a reference weapon's source waveform.

## Video observations and implementation decisions

Times are relative to this file, not server ticks; labels are approximate except the inspected fine sequences.

| Recording interval | Observed | Decision |
|---|---|---|
| 12–16s | Coffin deployment/capture; detached short gray strokes remain behind it | The offending ring of 16 line-shards was in `FirstSeveranceBossVisuals`, not just the sky texture. Remove that draw loop; retain actual suspension cables, shell chips and pillars. |
| ~28s / Phase I | Accepted flowing pursuit body reads distinctly from its warning | Preserve its material, hold and collision. Do not reopen the accepted lattice/pursuit design. |
| 94–120s | P2 side crater and bell jet are accepted; rotation uses a different release | Share the actual P2 renderer with rotation and Final cannon, not an approximate parallel effect. Opposed rotating exits deform the sphere together. |
| 132–180s | P3 top/bottom jets have markedly different widths/density by side | Use identical jet width, seeded irregular gap sizes with separate lower bounds, and a shifted second wave. Confine peripheral glow so tight gaps are not painted shut. |
| 192–252s | Final bullet section and colored beam score | Add one fully warned P2 cannon inside each bullet action; no new attack during the colored score. |
| 278–290s | Magic assembles then sustains; visible output is not proof of its cue firing | Fix the native subupdate guard, keep its accepted long score. |
| 293–297s | Several complete Ranged guns obscure the player | One receiver/barrel; progressively dock small breech components, compress the mechanism, then enter existing overdrive. One actual muzzle, no duplicated full guns. |
| 299.6–300.1s | Claw fingers appear, then five broad additive sheets merge into a near-white crescent | Preserve detailed articulated hands, sharply narrow/separate fingertip trails and shorten their history. Contact remains bright but the metal silhouette must survive. |
| 309–312s | Rogue apparatus/flight | One suspended execution relic loads six small pressure beats, brakes and releases. Remove the broad rotating crown and thick flight sheet. Retain six early shots, stealth behavior and final damage budget. |
| 314.5–317s | Claw remote crush | Preserve its diagonal grip/rapid closure; replace the shallow sound layers with a short low-bodied impact. |
| 320.6–321.7s | **Scythe of the Old God** remains a readable physical scythe with a textured colored wake | Use a brief moving cut around physical art; do not imitate its sprite, palette or sample. |
| 324.4–324.9s | Calamity sword winds, cuts at ~324.5–.6, then clears the broad arc by ~324.7 | Separate startup, brief decisive sweep and recovery. Do not leave a uniformly white arc across the whole use cycle. |

The top-center combat phase/countdown/guidance block is removed. Preparation Ready and deliberate introduction/transition/result titles remain; this is not removal of the native selected boss bar or world-space Down/Ready indicators.

## Audio: diagnosis before gain

Installed tModLoader `Projectile.Update` initializes `numUpdates` from `extraUpdates`, then **decrements before calling AI/PostAI**. Default `extraUpdates = 0` therefore invokes PostAI at **-1**, not 0. The old `numUpdates != 0` guards in `RitualArmamentProjectileVisuals` and `DollCompanionVisuals` skipped all normal updates: sound milestones, sustain startup and interpolation history could never run. Claw cues had a separate ungated path, explaining why some claw sound existed while other weapons were almost silent.

This was verified against the installed assembly's IL (initialize `0076`, decrement `0082–0089`, AI call `0499`, loop test `1dc9`), not assumed from the field name. The [pinned official projectile patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Projectile.cs.patch) also uses `numUpdates == -1` for final-subupdate work. `RitualPresentationStep.IsFinal` now gives one update per game tick for both ordinary and extra-updated projectiles. A focused test reproduces the loop for extraUpdates 0–8.

`RitualWeapon event=AudioVoice` now checks whether the device voice is playing two ticks after creation, bounded to three samples per cue per world. A false result exposes suppression/focus/expiry instead of being mistaken for quiet mastering. World unload clears the queue; server paths do not initialize sound. This diagnostic is not a microphone measurement or proof of physical audibility.

Mixed-recording windows (RMS dBFS; low capture level means absolute values are **not runtime gain prescriptions**):

| Window | RMS | Interpretation |
|---|---:|---|
| Idle 276–278s | -78.51 | Local noise/music baseline |
| Magic build 280–285s / sustain 287–290s | -71.50 / -73.44 | Little audio lift despite large visible action |
| Ranged 293–297s | -80.80 | Near the capture floor; consistent with skipped cue hooks |
| Claw 299.6–301.2s | -67.34 | Separate claw playback still has energy |
| Rogue 309–312s | -77.52 | Near idle |
| Claw crush 314.5–317s | -69.02 | Present, but does not establish satisfactory timbre |
| HotOG 320.15–321.65s | -62.60 | ~86.6% measured spectral power below 700Hz |
| Calamity sword 324.35–326s | -60.60 | ~95.0% below 700Hz, brief transient emphasis |

The latter windows motivated low/mid body plus short cutting air, **not copied frequencies, recordings or global slider increases**. Twelve original project-derived Claw/Ranged/Rogue/contact masters are remixed by `remix_weapon_impact.py`; other weapon masters and all BGM/Raid WAVs remain unchanged. Stack's five shell cues and warning tick have lower runtime gain separately. [Exact exports](../evidence/2026-09-14-weapon-impact-assets.json) preserve input/output hashes, peaks and private audition metrics. Current mix policy belongs to [Audio](../AUDIO_CUE_SHEET.md#weapon-only-foley).

## Primary implementation references

Accessed 2026-09-14; read as techniques, not vendored source/assets:

- HotOG [ScytheOfTheOldGodHeld.cs](https://github.com/UriBuilder/CalamityHunt/blob/f6af362a8f46477c939c232c1bc012eb55aa0c9d/Content/Projectiles/Weapons/Melee/ScytheOfTheOldGodHeld.cs): nine historical rotations, segmented swing progress, one stroke cue with bounded gain/pitch, textured colored trails around a physical weapon. This pin is a source reference; exact installed-binary identity and asset reuse rights are not asserted.
- Calamity [ExobladeProj.cs](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/ExobladeProj.cs): separate SlowStart/SwingFast/EndSwing and delayed slash visibility, actual blade drawing and a timed cue. Public source declares 2.2.2; installed 2.2.4 is not proven identical. Existing [license caveat](../encounters/first-severance/WEAPONS.md#prior-art-findings-and-engine-seams) applies.
- Existing accepted WotG/WoTM findings are reused from [F15/F16](WOTG_RAID_BENCHMARK.md#f15--nameless-portal-triplet-2026-09-13). This pass does not claim a new exhaustive review or subjective listening comparison.

## Log outcome and remaining verification

The pre-change run is one **solo victory in 241.60 seconds**, Stack 19/19 and Spread 22/22, no Down/revive. Core damage 5,000,000 across 35.67 open seconds (~140,187 window DPS); two Pylons total 600,000 across 7.8 seconds (~76,923 window DPS). There are 19 accepted RaidDamage calls totaling 2,280 **requested** damage, not measured post-defense HP loss: pursuit 3, Spread pursuit 5, rotation 1, vertical interdict 1, Final bullets 7, Final score 2. One reward event. These are not multiplayer balance/recovery results. [Sanitized log evidence](../evidence/2026-09-14-playtest-0277.json) records hashes and counts.

The recovered initial connection refusal is separate from an end-of-session `GhostSamuraiContainmentSystem.OnWorldUnload` IndexOutOfRange and subsequent SubworldLibrary EOF. No Ghost Samurai fight occurs in these logs; its separate implementation is unchanged here. Do not attribute the missing weapon sounds to those later unload exceptions.

Required user-owned smoke: Reload matching peers; observe rotating crater/beam attachment, randomized P3 safe gaps and wave-two movement, one warned Final cannon per bullet action, absence of coffin sticks/top combat text, charge/Stack mix, and each weapon's audible startup/sustain/cancel. Repeat with a remote peer for shared geometry; compiled code/PCM inspection does not satisfy those checks.
