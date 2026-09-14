---
doc_id: history.playtest-feedback
document_type: evidence
status: historical
owners:
  - project
last_reviewed: 2026-09-14
source_of_truth_for: []
aliases:
  - playtest feedback
  - user acceptance history
related_code: []
related_docs:
  - project.status
  - encounter.first-severance.spec
---

# Playtest feedback ledger

Optional history, not a startup reading list or current specification. Search by feature/build only when useful. Each entry distinguishes **player report**, **response**, and **evidence/remaining check**. Append a few lines per meaningful session; combine follow-ups, link detailed evidence, and never paste raw logs or chat. Current behavior belongs to the feature specs, current progress to [Status](../STATUS.md).

## Through 0.3.1 — owner feedback consolidated on 2026-09-14

This is a retrospective summary of the owner's playtests and repeated corrections in the development conversation, not a newly replayed acceptance matrix. Earlier versions below identify decision periods, not invented exact test dates. Superseded experiments stay in the linked histories.

| Topic / period | Player observation and chosen response | Result / evidence boundary |
|---|---|---|
| Multiplayer start and recovery, early 0.2.x | Some connected players were omitted from preparation; Down was unclear and resurrection could become permanently unavailable. Include the whole eligible connected roster, deploy the field before manual Ready, then start a separate introduction. Use a reusable instant kit with a recipient lockout; remove Eliminated/Down timeout. | Start/recovery contracts live in [Arena](../ARENA_INFRASTRUCTURE.md) and [Revive](../encounters/first-severance/REVIVE_SPEC.md). A solo victory cannot validate teammate revival. |
| Stack synchronization, 0.2.15–0.2.16 | Two friends saw each other on opposite sides of the gathering circle. Owner subsequently reported the Stack problem resolved after movement synchronization correction. | Keep fixed world-space gathering positions and authority outcomes; [historical implementation](2026-09-11-encounter-evolution.md), not proof of every latency condition. |
| Cooperation and pacing | Correct Stack/Spread should not hurt; health was first too low, then too high, and three-player damage changed the experience. Freeze roster-scaled HP, measure actual damage windows, keep deliberate gaps between categories and rapid attacks within them. Guarantee the first action cycle, then transition immediately at the HP threshold. | [Combat spec](../encounters/first-severance/ENCOUNTER_SPEC.md) owns current values. No old feedback number overrides its tuning. |
| Fair forecasts and Final | Dense/random last-second beams felt unfair. Move to clearly ordered color forecasts before release, with visible travel and an intelligible damage footprint. Repeatedly tune lane spacing/read time rather than preserve rejected random triples. | Current Final contract is in the combat spec; old schemes are superseded, not backlog requirements. |
| Boss identity and staging | Abstract ritual machinery lacked character; a directly enlarged NPC face went too far. Keep the tragic suspended Doll, restrained protrusions from the coffin, remote original hands, and a smooth mechanical central sphere. The NPC stays intact during preparation and fragments into the existing coffin only at all-Ready introduction. | [Doll Theater](../encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) owns the accepted hybrid. Do not restore either rejected extreme. |
| UI and readability | Exterior masking and cinematic bars shifted for a player's 107% UI scale. Decorative Boss circles, markers, floating sticks and top-center combat text looked detached. | Separate world-space effects from screen cinematics; retain only meaningful player/gather rings, Ready and cinematic titles. Later residual sticks required another fix; a code removal is not visual acceptance. |
| Motion | Slow uniform interpolation felt weak. Favor fast emergence → momentary loaded hesitation → fast release, continuously connected; extend meaningful held shots instead of adding repetitive flashes. | Applies to weapons, muzzle deformation, remote motion and endings; [Visual spec](../encounters/first-severance/VISUAL_SPEC.md) owns presentation. |
| Beam milestone, 0.2.72–0.2.75 | Owner explicitly praised the portal-triplet-inspired beams and new sound direction. Requested longer overlapping opening-beam holds, stronger but bounded beam sound, a mid/high “fan” transient and the restored Spread “ping.” | [Reference analysis](../research/WOTG_RAID_BENCHMARK.md#f15--nameless-portal-triplet-2026-09-13) separates observed video from independent implementation. Keep the accepted flow; avoid solid painted corridors. |
| Core muzzle, 0.2.77–0.2.78 | Owner strongly approved the side crater and bell-shaped purple emission; asked to reuse it for rotating/Final beams and enlarge purple/red heartbeat charges. | Implemented in 0.2.78; [video review](../research/2026-09-14-doll-playtest-video.md) and [implementation evidence](../evidence/2026-09-14-doll-video-feedback.json). |
| Weapons and sound, 0.2.77–0.2.78 | Weapon sounds appeared missing; claws were over-flashed, Ranged's multiple guns looked poor. Use compact claw trails, one assembled ranged receiver, one loading Rogue relic, and heavier original Raid-derived audio. | The native final-subupdate mismatch was a real playback defect, not just taste. 0.2.78 logs below confirm actual voices; perceived mix remains the owner's judgement. |
| BGM and relative mix | Reject the Ninth/chiptune experiment; preserve ominous grandeur and increasing urgency. Owner approved EigHt's music and later edits; many SFX remixes were rejected, while Stack details/beam direction received specific approval. Stack later remained too loud. | [Audio sheet](../AUDIO_CUE_SHEET.md) owns the current mix. Do not roll back approved BGM, revive rejected non-Stack masters or infer listening from numeric analysis. |
| Development workflow | Excessive rereading, duplicate checks and GUI takeovers slow iteration. Normal playtests are owner/friend multiplayer; solo and GUI assistance must be explicitly requested. | Focus verification on changed inputs. Keep build success separate from game success. This ledger adds no mandatory reading/test gate. |
| Release milestone, 2026-09-14 | Owner designated Doll's current Raid as the initially complete prototype and Ghost Samurai as still in development; requested 0.3.1 and public playtesting. | Milestone acceptance is not a claim of final balance, compatibility or full multiplayer validation. |

## 2026-09-14 — 0.2.78 release-baseline playtest

- **Observed:** one solo Host & Play Raid, victory in 240.65s; Stack 19/19 and Spread 22/22 succeeded; no Down/revival. Core damage-window DPS 123,813.5; Pylons 87,591.2. These exclude shielding/overkill and are not whole-fight or personal DPS.
- **Audio:** 24 distinct weapon cues / 48 bounded voice samples report `playing=True`, including Magic/Ranged sustain and Doll beam cues. This confirms the repaired playback path, not subjective loudness or every event.
- **Remaining:** Ghost Samurai `OnWorldUnload` still throws after the Raid; native tile-stream shutdown warnings and a SubworldLibrary EOF also appear. No Ghost Samurai fight was run. The latest solo session does not validate multiplayer recovery/alignment.
- **Evidence:** [sanitized log summary](../evidence/2026-09-14-playtest-0278.json). The owner supplied a 4m31s gameplay clip for a social invitation; original video and logs remain external. No new artistic approval is inferred merely from its attachment.

## Ghost Samurai — 2026-09-14 / development0.3.2

The user reported a fight surviving death and fields extending underground; requested later tracking, low wave test damage, early shout/late visual cue and target damage ownership. Replace unsafe all-slot teardown with exact-Fight leases, end on last living participant, anchor the unchanged field above summoner feet, retain living targets and halve known non-target owner damage. The user clarified that only first-wave warnings shorten60ticks; later30/48tick warnings remain. [Evidence](../evidence/2026-09-14-ghost-samurai-lifecycle.json) records automated results; actual death/re-summon, multiplayer ratios, network timing and arena/dash playability await testing.

## Doll damage review — 2026-09-14 / development0.3.3

Owner objected to Raid-only HP subtraction bypassing equipment and proposed a bound-debuff/native-damage Down model; explicitly confirmed combat text was intentionally removed. Adopt native Hurt with bounded receipts and authoritative recovery, remove the Adrenaline-only bridge, preserve minimal presentation. [Review disposition](../research/2026-09-14-implementation-review.md) separates confirmed defects from proposed tests; [Status](../STATUS.md) owns verification. No new gameplay observation or broad compatibility approval is inferred from the external analysis.

## Solo admission regression — 2026-09-14 / Workshop0.3.1 → source0.3.4

Owner's one-player Host & Play (`al`) was rejected at21:33:32 with `roster_too_small`, then repeated clicks hit rate limiting. Actual package inspection finds the GitHub0.3.1 artifact solo-enabled but the09:44 GUI rebuild/local/Workshop artifact solo-disabled. Owner reiterates **multiplayer recommended, never solo prohibited**, with companion party substitution planned. Remove symbol/release opt-out and check packaged admission, without changing combat, adding fake players or claiming companion support. [Evidence](../evidence/2026-09-14-solo-admission.json) owns hashes/results; a new owner solo start remains unobserved.
