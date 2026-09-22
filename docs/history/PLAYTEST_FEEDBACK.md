---
doc_id: history.playtest-feedback
document_type: evidence
status: historical
owners:
  - project
last_reviewed: 2026-09-22
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

## Azure staged consumption and beetle jaws — 2026-09-22 / 0.3.35 → 0.3.37

Owner reports body overlap spoiling consumption/melting, requests early floor-triggered evacuation, an intact head with beetle-like opening fangs, richer worm art and a longer Fury fight. Latest two solo attempts end in one clean Defeat and one complete Victory/cleanup; the previous missing-chain invalidation does not recur in those logs. Fury lasts only110ticks. Stage all45parts at the20% floor, gate consumption on completed alignment, repeat staging before a flowing melt, split pincers/throat into original PNG layers and raise only the Fury shared pool. [Evidence](../evidence/2026-09-22-azure-staged-mandibles.json) separates observed timings, source diagnosis, offline checks and still-pending native/remote visual acceptance.

## Azure retry background and lethal chain — 2026-09-21 / 0.3.33 → 0.3.35

Owner reports missing background on the second or third attempt and a disappearing worm. Three solo logs show two clean Defeats, then one Fury defeat immediately invalidated by45→40 missing parts; Victory/melting never runs. Actual old-package engine probes reproduce lethal shared-HP child loss and a stale sky flag after native visual reset. Retain all owned native shells at lethal damage and reconcile the actual sky state, preserving gameplay/tuning and incoming Oboro work. [Evidence](../evidence/2026-09-21-azure-lifecycle-fix.json) separates confirmed log events/reproduction from the inferred reset trigger and still-unrun repeated Host & Play / remote visual acceptance.

## Azure Fury refinement — 2026-09-21 / 0.3.32 → 0.3.33

Owner reports too little worm hit uptime, an overly short diagonal entry, solid-looking missiles and a consumption pose already overlapping Liora; requests1/20 worm HP, full-body Fury hits, cyan Vespera-style cuts and new second-form art. September20 solo logs show two clean Defeats:12.15s and342.27s; in the longer attempt Liora dies after50.95s, the worm reaches its floor247.87s later, refills once, then the party dies35.45s into Fury with88.98% worm HP. Implement shared native Fury damage, six-second moving volley passage, streaming energy bolts, Liora cuts and retreat/rush/slow articulated bite before head-to-tail Fury armor change. Preserve Liora HP and other encounters. [Evidence](../evidence/2026-09-21-azure-fury-refinement.json) owns exact logs/checks; new balance, visuals and multiplayer are not owner-approved yet.

## Azure devouring storm — 2026-09-20 / 0.3.31 → 0.3.32

Owner reports flat Liora, transient entrance mask/music loss, short worm and unfair moving-mouth beams; requests Doll-style preparation, frost, segment missiles and a consumption/Fury/melting progression. Latest solo logs contain one12.37s defeat and one198.30s victory with clean cleanup, three Stacks (one success) and three successful solo Spreads; no Cathedral exception or missing-actor invalidation. Rework the full projection transport, Ready pill, native-density contrast,45-part courses, Liora-origin sweep and head-only20% floor; add the two-gate transformation and one-time Fury refill. Preserve music file, scaling and other Raids. [Evidence](../evidence/2026-09-20-azure-devouring.json) separates measured logs from the source-inferred flicker cause, deterministic/GPU checks and still-unrun multiplayer/audible/visual acceptance.

## Azure awakening and recovery — 2026-09-20 / 0.3.30 → 0.3.31

Owner reports strange endings, overly detailed girl art, timid short worm motion and a fish-like asymmetric silhouette. Three solo attempts contain one normal defeat and two invalidations immediately after Vitrion dies; Liora remains alive, with6/9 duplicate defeat events. Make defeat idempotent and stop requiring an already-defeated chain. Fix Liora centrally at native pixel density, adopt broad off-field worm courses and symmetric segmented armor, and stage sealed ice → sword break → awakening/light → rift arrival before combat. Add Stack ice compression, Spread rift/light-sword verdicts and brighter faceted flying crystals. Preserve BGM, HP and previous Raid implementations. [Evidence](../evidence/2026-09-20-azure-awakening.json) separates the confirmed old bug, offline checks and untested new SP/MP acceptance; no owner approval of the new visuals is inferred.

## Scarlet crossflow and spatial cuts — 2026-09-20 / 0.3.28 → 0.3.29

Owner approves the seal/beam appearance but clarifies right-to-left crossflow between opposing circles, asks for Orderbringer direct-hit-inspired slashes, larger Spread, beat-shifted ActIII lattices and tighter phrase transitions. Follow-up requests tasteful smoke and continuously changing fine slash amplitude. Latest solo Host & Play reaches ActIII, then fails a Stack and wipes two ticks later; cleanup completes. Ordinary crossflow-to-next-hit gaps were41–63ticks. Replace vertical columns with one seal-to-seal stream, issue four consecutive on-beat strikes, start the next forecast without an extra blank beat, enlarge Spread and add deterministic four-cut lattice sequences. Preserve approved background/rings/audio, HP gates and rehearsal damage. Post-fight unowned Scarlet NPC sync exceptions receive an empty-state envelope; a separate Ghost Samurai invalid-arena packet remains out of scope. [Evidence](../evidence/2026-09-20-scarlet-crossflow.json) separates logs/source/video/GPU/build checks from pending playtest acceptance.

## Shared Luminance direction — 2026-09-18 / source0.3.19

Owner reports a PNG flipbook appearance for Ghost Samurai and requests active Luminance use across all content, including contributor work. Source review confirms the current twelve-pose whole-body atlas renderer and a missing shared requirement despite detailed Doll/Scarlet guidance. Establish the [common policy](../ART_DIRECTION.md#luminance-presentation-policy), route the development Skill and contributor entry points to it, and define the [Ghost Samurai target](../encounters/ghost-samurai/ENCOUNTER_SPEC.md#luminance-presentation-target). This is documentation/Skill work only; connected body motion and evolving materials are targets, not newly implemented or visually approved results. No new game session or renderer acceptance is claimed.

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

## Doll native-damage tuning — 2026-09-15 / 0.3.4 → 0.3.5

Owner reports damage too low, apparent heavy bleed, and main eight-cast beams lingering too long. Latest solo Host & Play wins in267.27s: Stack19/20, Spread23/23, no Down/rescue;41 native receipts total only43 immediate damage, excluding DoT. Chalice is probably equipped; its delayed-damage pattern fits but was not logged. Raise the fixed native hazard source budget, shorten only the main eight-cast hold without changing forecast/cadence, and add read-only buffer/regen diagnostics. Preserve ordinary solo, native equipment, successful mechanics and accepted artwork. [Evidence](../evidence/2026-09-15-doll-damage-tuning.json) owns aggregates; post-change balance/readability and actual bleed attribution await owner testing.

## Doll multiplayer visibility, final check and weapon articulation — 2026-09-15 / 0.3.7

Owner reports remote players cannot see their companion, chopped lattice lanes, short Final target beams, and blunt weak weapon SFX; requests doubled Boss HP and a final5% DPS check. Latest0.3.5 Host & Play: two players, Victory236.48s, Stack19/19, Spread19/22, two Down/two successful revives. Core window DPS185,414.1; Pylons223,602.5 (shield/overkill excluded). Host Chalice is explicitly false this time: ordinary500-source hits deal257–323 before low-HP capping, not the earlier five-point deferred pattern. Weapon voices report playing, so change timbre rather than bypassing playback or user sliders. Fix owner-only minion dismissal, continuous sanctuary lanes, longer Final cannon and the terminal check; replace20 short/charge weapon cues while retaining seven long-beam launch/sustain masters. [Evidence](../evidence/2026-09-15-doll-final-check.json) owns counts/exports and remaining user checks. No new visual/audio or remote-sync approval is inferred from compilation.

## Crimson summoner redesign — 2026-09-16 / 0.3.11

Owner discards the steel-machine concept: a red-haired long-twin-tail summoner controls large independently damageable apparitions, all defeated before her Final. Repeated clarification: **Boss herself and companion remain ordinary NPC size; only apparitions are large.** Requests clearer forecast edges, field-filling safe-corridor patterns rather than player aiming, continuous Ready/start, music fade, a dedicated summon-item icon and Doll-like companion. Implement three native targets plus the small performer, original four-pose NPC/three creature/two item textures, continuously deforming creature meshes, owner-replicated ten-slot companion and unchanged authorized music with epoch-driven fade. Preserve newly merged Ghost Samurai work; use a new combined version/protocol. [Evidence](../evidence/2026-09-16-crimson-invocation.json) separates automated/offline checks from pending playtesting. No new runtime-log session or subjective audio approval was supplied in these follow-ups.

## Crimson loader regression — 2026-09-16 / 0.3.11 → 0.3.12

Owner reports Mod load failure. The00:54:28 client exception identifies `CrimsonRig.Load` creating a GPU effect on the loader worker; tModLoader disables Convergence. Move creation to first draw and queue captured-instance disposal for safe Reload Mods. Earlier type checks/offline art frames did not exercise worker content setup; add two focused guards and a real-FNA linked-renderer worker/reload lifetime check. [Hotfix evidence](../evidence/2026-09-16-crimson-invocation.json) records the unchanged-gameplay package and actual client reload still awaiting owner confirmation.

## Crimson field, vulnerability and articulated machine — 2026-09-15 / 0.3.8 → 0.3.9

Owner rejects the floating illustration, squat silhouette, quiet/short attacks and excessive music/SFX level; reports the Boss could not be hit. Requests shared-pedestal Raid selection, Doll-sized containment, a separate operator/slim post-purge machine and stronger red reactor/Luminance energy. Archived sessions show solo and two-player Defeats after15.65s/47.47s of combat, both followed by cleanup; no purge or per-hit/HP evidence was logged. Code identifies the retained client invulnerability flag, not measured zero DPS. Fix native vulnerability projection; introduce shared field/lease adapter, articulated twelve-part rig, longer release/full-field hazards, lower music/current bounded Portal cues and progress/DPS logging. Supplied video overview and73–76s frames were visually inspected with pinned WoTM source; no third-party assets copied. [Evidence](../evidence/2026-09-15-crimson-stage.json) separates old observations, automated checks and pending owner acceptance.

## Ghost Samurai invisible body — 2026-09-17 / 0.3.13 → 0.3.14

Owner reports the new violet body is invisible and requests a drawing-only repair across idle/movement/attacks/transitions and peers. The new shared cutout cache copied Asset.Value before asynchronous loading completed, retaining the transparent placeholder permanently. A cold-cache CPU fixture reproduces this; ImmediateLoad before pixel processing fixes it. Keep a55% body opacity floor before initial synchronization, preserve all accepted sprites/AI/attacks, and correct the outdated old-atlas description. [Evidence](../evidence/2026-09-17-ghost-samurai-visibility.json) separates automated reproduction from the user's still-pending actual render and multiplayer checks.

## 2026-09-17 — Scarlet material-quality and rehearsal request

Owner reports strong flat-fill/low-detail appearance compared with Doll and requests meaningful Luminance usage before further playtesting. Owner approves the generated fiery cathedral background and requests a new branch from current main, all Scarlet attack damage temporarily1, lower HP without instant full-fight skips, and completing a phase action cycle while holding threshold HP.

Implemented on `feat/scarlet-luminance-presentation-v2`: independent species materials, masked body regions, continuous physical primitives, decorative Verlet/Metaballs, varied musical calls, full12-phrase gates, reduced target budgets and hostile native damage-one caps. No claim that these changes look better in-game has been accepted. The approved painting's binary import was blocked by the conversation's container timeout; importer/renderer readiness is not artwork inclusion. Main merge/install/release was not requested or performed.

## 2026-09-17 — Reload teardown and approved Scarlet integration

At07:48:02 the owner-reported load failure occurred while unloading old0.3.12, after successful GUI compilation: `NullCantorClawArt.Dispose` released textures from tML's worker. Queue captured owned-texture disposal; the same correction applies to the keyed Ghost Samurai texture. Preserve borrowed asset ownership and the Scarlet partial-declaration correction. Local0.3.14/protocol47 rebuilt successfully; the owner approved backup/replacement of the normal package. Actual cold-load acceptance was not recorded at that time.

The owner subsequently requests latest Scarlet v2 → main → Build/Reload. Integrate `5696fdd` with main `cb42e8a`, retaining Ghost Samurai slash/Oboro fixes and these teardown repairs, as0.3.16/protocol48. Native compilation passes; do not treat it as gameplay/visual approval. The approved background remains absent. No Workshop publication or GitHub release is requested.

## Scarlet visibility and material revision — 2026-09-17 / 0.3.16 → 0.3.17

Owner reports weak presentation and visible apparitions but an invisible performer; supplies the missing cathedral PNG and authorizes graphics rework while preserving the basics. Latest solo Host & Play completes all four action cycles, Victory and cleanup; no ERROR/FATAL entries, but the missing-background warning is present. A ceiling perch placed the56px performer about990px above a grounded player. Bring that shared, invulnerable pre-Final perch near the active focus, preserve small character pixels, add the exact supplied painting and recognize packed `.rawimg`. Rework physical-stroke forecast/live separation, flowing fibre materials, per-source pressure and bounded cloth/limb recoil; retain score/HP/damage-one rehearsal and all collision geometry. [Evidence](../evidence/2026-09-17-scarlet-visual-revision.json) separates the old playtest from the new offline GPU/build checks; visual acceptance, zoom/MP and performance remain owner checks.

## Scarlet rhythmic onslaught — 2026-09-18 / 0.3.17 → 0.3.19

Owner approves chorus circles/background but rejects small sparse attacks, slow melee warnings, cheap sounds and negligible shake. Two solo victories/cleanup are logged without ERROR/FATAL. Pull merged Oboro main first; preserve praised art, HP/damage-one rehearsal and cycle gates. Increase beat-derived phrases to5/6notes, shorten calls to1.5beats, expand physical coverage with refuges, add sharper connected extension, original species SFX with natural tails and stronger bounded recoil. [Evidence](../evidence/2026-09-18-scarlet-onslaught.json) records logs, score limitations, geometry/GPU/audio checks and package identity. Actual mix/fairness/camera/MP acceptance remains not_run; no claim that every drum transient was transcribed.

## Scarlet basic pulse — 2026-09-19 / 0.3.19 → 0.3.20

Owner still cannot feel the rhythm and asks for a deliberately simple forecast/strike repetition on the beat of the Stack marker's four inward arrows. Latest solo Host & Play logs show Victory after130.55s, all four action-cycle gates and cleanup, with no ERROR/FATAL; this is not acceptance of the rhythm. Replace5/6-note syncopations with forecast/strike/forecast/strike on four consecutive score beats, uniformly across all acts and Final; remove the warning-pitch ladder. Preserve accepted circles/background, physical techniques, SFX masters, HP/damage-one rehearsal and cycle gates. [Evidence](../evidence/2026-09-19-scarlet-basic-pulse.json) records the old session separately from the new timing/codec/build checks and pending audible/MP acceptance.

## Scarlet flowing pulse — 2026-09-19 / 0.3.20 → 0.3.21

Owner asks to reverse the basic beat roles, reports ActI forecasts only in the upper half and blinking falling attacks, permits flight/aftermath to cross beats and wants larger red energy bodies. The0.3.20 solo fight ends in Victory/cleanup after130.12s without ERROR/FATAL. Confirm Luminance's omitted final segment against pinned source and installed method; the old simplified GPU upload facade masked the half-line error. Add tangent support/UV correction and matching preview contract. Shift warnings/releases one beat, extend flight/residue and their owned leases, add continuous head/tail/ignition envelopes, enlarge energy bodies with honest forecasts. Preserve accepted circles/background, audio and rehearsal balance. [Evidence](../evidence/2026-09-19-scarlet-flow.json) records checks; actual gameplay/readability acceptance remains not_run.

## Scarlet single-beam baseline — 2026-09-20 / 0.3.25 → 0.3.27

Owner accepts the direction but wants to rebuild all ordinary attacks from a single tracking forecast/beam at the current pulse, rejects bespoke SFX and reports an abrupt BGM return. Follow-up explicitly requires field-edge-to-edge coverage, not a Boss-origin beam, and rejects the rail/capsule forecast in favor of Doll's actual material. Latest solo logs show139s Victory/cleanup, four cycle gates and six resolved choruses, without ERROR/FATAL or AudioReanchor. Replace scheduled decks with one server-aimed, connection-bound full-field beam per strike, a brief final aim lock and shared warning/live geometry; reuse Doll PortalForecast/Jet/Dust and ChargeLock/PortalFire. Re-edit the music's middle-section return over four beats, preserve intro/beat map and credit, supply a local seam audition. Approved circles/background and rehearsal tuning stay. [Evidence](../evidence/2026-09-20-scarlet-single-beam.json) separates numeric/offline checks from pending audible and SP/MP acceptance.

## Scarlet invocation choreography — 2026-09-20 / 0.3.27 → 0.3.28

Owner wants orb-only preparation, Vespera materialization/large summoning seal, center-fixed conductor, four-plus-two-plus-two beat attacks, ActII white/red spatial cuts, Vespera's black Stack flames/red Spread cuts and distinct apparitions; reports repetitive edge sounds and the missing BGM climax. Two solo victories/cleanup are observed. The previous99.8s music edit did omit the last34.7s; restore the full134.5s source and append the bridge instead. Small2px floor clamps could repeatedly issue native teleport notifications; remove the artificial inset and reserve notifications for major escapes (code diagnosis, not recorded event proof). Preserve approved circles/cathedral and native authority; refine bounded shader/rig choreography. [Evidence](../evidence/2026-09-20-scarlet-choreography.json) records automated/offline results separately from pending owner audio, fairness, camera and MP acceptance.

## Oboro finisher reference — 2026-09-20 / 0.3.26 → 0.3.30

Owner requests a26F pullback/held charge/explosive final cut, more forward weapon movement, and supplies Murasama plus DMC Vergil clips as the durable motion target. This is a design request/reference, not a reported0.3.30 playtest. Author the five-beat third curve, align its live window after the charge, add violet condensation/release accents and retain the follow-through into the next harmless stance. [Evidence](../evidence/2026-09-20-oboro-third-swing.json) separates shared-geometry/native-package/offline checks from user-owned gameplay and subjective feel; third-party recordings remain local.

## Scarlet sacrificial Final and companion — 2026-09-22 / 0.3.35 → 0.3.36

Owner requests ten simultaneous highest-HP companion targets with opposing beam seals, proper Act summoning, and three bound apparitions sacrificed into **one** giant Final target; also reports failed Stack fire is difficult to see. Latest solo logs reach Final, then Defeat/cleanup after187.53s: Stack0/3, solo Spread2/2; the old four targets are intentionally held at1HP pending their unfinished first cycle. Preserve the total HP budget while merging it into the giant, overlap two attack families, add epoch-driven gates/bindings/absorption, and keep captured verdict visuals briefly through death instead of filtering dead/Out players or deleting the marker immediately. The flame gains an opaque dark core and visible crimson edges. [Evidence](../evidence/2026-09-22-scarlet-ensemble.json) separates log facts and code diagnosis from pending real-game visibility, fairness, companion/remote synchronization and repeat-start acceptance.
