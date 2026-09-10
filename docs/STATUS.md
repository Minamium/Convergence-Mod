---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-10
source_of_truth_for:
  - project.implementation_status
aliases:
  - current state
  - implementation inventory
related_code:
  - Common
  - Content/Encounters/FirstSeverance
  - Tests/Convergence.DomainTests
related_docs:
  - handoff.windows
  - encounter.first-severance.plan
---

# Project Status

## Current build

Development **0.2.43**, protocol **28**. [Measured audio recovery](AUDIO_CUE_SHEET.md#measured-loudness-recovery--0243) preserves the new orchestral/SFX content while restoring body level in four music and eleven one-shot masters; three comparable sustain voices stay unchanged. [Preparation presentation](encounters/first-severance/VISUAL_SPEC.md#suspended-preparation-and-minimal-ready--0243) is reduced to one Ready toggle/count, with the central iris suspended from larger paired columns instead of solid diagonal braces. The owner accepts the Raid combat body as the development baseline; server-wide preparation, damage, weapons, timings and protocol are unchanged.

Retained Boss baseline: Final random triples have wider gaps; Phase-III sword wave two shifts the sparse lanes so standing in the first gap is no longer safe. The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#random-final-triples) owns tuning. The owner reported submitting the preceding build to Workshop; approval/visibility is unverified. This build has not been uploaded by this task; [publication policy](RELEASE_PROCESS.md#development-publication-preparation) remains separate.

- First Severance is playable: Core placement/validation, Ready, server-owned combat, participant containment/infinite flight, fixed-site Stack and player-centered Spread.
- Ordered phases: sealed opening and clockwise relay; lattice/twin blades; distant arms/floods/crush; HP-zero Final survival. Required first scores gate phase transitions and Victory.
- Instant reusable ally revival, no item/token cost, untimed Down (no active Eliminated), recipient lockout and immediate all-Down Defeat. Ordinary Terraria lethal-hook integration remains separate.
- Client-only original Boss/materials, continuous beam forecasts, phase music, SFX and start/transition/result cinematics. Presentation and balance remain developmental.
- Build-gated one-member start is available for explicitly requested solo debugging; ordinary workflow remains user GUI plus a friend.

The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md), [recovery spec](encounters/first-severance/REVIVE_SPEC.md), [visual spec](encounters/first-severance/VISUAL_SPEC.md) and [audio cues](AUDIO_CUE_SHEET.md) own behavior. The [version matrix](VERSION_MATRIX.md) owns the runtime baseline.

## Verification state

[0.2.43 integration and latest playtest](evidence/2026-09-10-audio-preparation-polish.json):0.2.42 completed solo Victory in222.62s,19Stack/22Spread successes,33accepted hits and no Down;35instrumented audio cues played. The evidence owns the measured asset-level regression, remaster exports and current checks. Multiplayer preparation and in-game0.2.43 listening/visual acceptance remain user-owned `not_run`.

[0.2.42 SFX checks](evidence/2026-09-10-prismatic-sfx-rebuild.json) own the 14-master deterministic export/decode/finite/stereo/48kHz/PCM16/duration/peak/SHA evidence and repository static verification. Cue scheduling, gains, voice limits, C# behavior and protocol remain unchanged. Actual boss/weapon listening, sustain-loop perception and multi-client mix remain user-owned `not_run`; numerical headroom is not acoustic approval.

[0.2.42 BGM checks](evidence/2026-09-10-orchestral-bgm-abc.json) own the four-master decode/finite/stereo/48kHz/duration/peak evidence and repository static verification. The owner accepted the preceding V13 composition/orchestration direction; 0.2.42 additionally removes the multi-bar A-to-B fade by carrying A to the B downbeat. Actual in-game loop seam, phase-change mix and multiplayer listening remain user-owned `not_run`; no gameplay or protocol contract changed.

[0.2.41 checks](evidence/2026-09-09-server-wide-preparation.json) owns preparation/domain/codec/build evidence. Actual3-player distant join, both cinematics,107% UI button alignment and cancellation cleanup remain user-owned `not_run`.

[0.2.40 checks](evidence/2026-09-09-critical-audio-phase-gates.json):142distinct domain cases passed across the suite/scoped corrections,8presentation guards, protocol27codec324round-trips/50rejections and eight audio exports checked. Native packaging passed with zero errors/four existing warnings. Actual listening and Host & Play remain user-owned `not_run`; diagnostic playback acceptance is not acoustic proof.

[0.2.39 checks](evidence/2026-09-09-audio-standalone-spread.json):7focused Spread domain cases,7presentation guards, protocol26codec324round-trips/50malformed cases and native packaging passed. Four remastered audio exports passed PCM/peak checks. User audio mix and actual standalone-vs-embedded Spread behavior are `not_run`; no GUI/server started.

[0.2.38 checks](evidence/2026-09-09-vanilla-bar-claw.json): five presentation integration guards and native package build passed (zero errors, four existing warnings). No domain/codec rerun for unchanged gameplay/wire contracts. Actual vanilla-style bar, shielded/exposed/Final states, normal/reduced claw trail and both-client display remain user-owned `not_run`.

[Latest log/build record](evidence/2026-09-09-read-ahead-spread-score.json):0.2.36 successfully loaded and completed a two-player Victory in251.48s;20Stack checks (one failure),23Spread successes,36accepted Raid hits (14FinalSlicer), no Down/revive or logged exception. Client0's19hit receipts match its authority hits; actual Adrenaline values remain unobserved. This supersedes the pending0.2.36 reload below, not unrelated manual checks.

0.2.37:139 distinct domain cases passed across the suite and scoped correction of a legacy preview-count expectation; protocol codec324round-trips/50malformed cases and new concurrent1–4-player Spread tests passed. Eight new audio files passed PCM decode/finite/peak checks. Native packaging passed with zero errors/four existing nullable warnings. Actual0.2.37 load, audiovisual mix and multiplayer dodgeability remain **not_run**, user-owned.

Historical0.2.35 load failure (`GlobalNPC` instance fields without `InstancePerEntity`) and the static-cache0.2.36 hotfix remain recorded in the preceding evidence; compilation alone did not establish that fix's success.

[Raid hit/kinetic evidence](evidence/2026-09-09-raid-hit-kinetics.json):137 domain cases passed across the suite and scoped eclosion/flow reruns; compiled codec324 round-trips and50 malformed cases passed. Native package compilation passed, zero errors/four existing nullable warnings. Actual Calamity gauge behavior, remote-owner/terminal hit delivery and visual acceptance are **not_run**, user-owned. The source fix is not an in-game success or measured performance claim.

[Grand armament evidence](evidence/2026-09-09-grand-armaments.json):133 deterministic domain cases passed, including four new macro-score/resource tests; native package compilation passed with zero errors/four existing warnings. Four new alpha-preserved icons were inspected at128px and40px. User-owned checks: held-trigger/empty-resource behavior, item-switch/Down cancellation, one vs multiple minions/retarget/sacrifice, Rogue stealth, matching-peer aim/audio continuity, reduced effects and actual DPS/readability. No GUI/gameplay/FPS acceptance is claimed.

[Cinematic/non-melee evidence](evidence/2026-09-09-cinematics-kinetic-armaments.json):131 domain cases, two cinematic integration guards and native packaging passed (zero errors, four existing warnings). Actual107% peer cinematic alignment, repeated/empowered Magic casts, item-switch/Down interruption and multiplayer audiovisual continuity remain user-owned. No measured FPS/DPS claim.

[Containment mask evidence](evidence/2026-09-09-containment-mask.json): three focused coordinate tests and native packaging passed (zero errors, four existing warnings). User-owned check: both peers on0.2.32, keep takeura's UI at107%, inspect all four edges during movement, camera shake and phase transitions; change game zoom independently. A compile or numerical projection test does not establish actual peer rendering.

[Kinetic presentation evidence](evidence/2026-09-09-kinetic-unmaking.json) records125 passing domain cases, skill validation and native packaging (zero errors, four existing warnings). Actual dual-button input, shared-client impact timing, Final breakup/rift readability, sword anticipation, scenery and reduced-effects acceptance remain user-owned; no FPS/smoothness claim from compilation.

[Apparatus v2 evidence](evidence/2026-09-09-ritual-armaments-v2.json) records branch checks and main integration. Full native Calamity package compilation passed with zero errors and four existing nullable warnings. Four V2 icon images and transparent bounds were inspected; prior claw slot correction is retained. The user accepted the0.2.29 claw design and reported mild animation stutter; the continuity pass targets presentation seams. In-game icon/animation, multiplayer and DPS acceptance of0.2.30 remain user-owned. No GUI session or FPS/log subsystem is added.

[Dual-claw evidence](evidence/2026-09-09-null-cantor-claws.json) records branch checks and main integration. The complete native Calamity-dependent package now builds successfully (zero errors, four existing nullable warnings). Inventory-only padding correction fits the claws to the same slot envelope as other weapons; original artwork is preserved. Actual game audiovisual acceptance, multiplayer behavior and matched DPS remain user-owned.

[Spacing/publication-material evidence](evidence/2026-09-08-spacing-publication-materials.json) records0.2.28 checks and the latest0.2.27 two-player run: Final reached, one successful revival, subsequent missed Stack caused all-Down Defeat. Current user-owned checks: wider random gaps and forced second sword dodge on matching peers; public rights/solo-policy/package acceptance remain separate.

[Random triple checks](evidence/2026-09-08-random-final-triples.json) owns0.2.27 results. User-owned check: matching peers, three-shot direction changes/diagonal warning readability, tight safe gaps and faster final survival. No GUI/server session is launched by this implementation.

[Final beam readability evidence](evidence/2026-09-08-final-beam-readability.json) records the latest0.2.25 two-player Victory and this presentation-only pass. User-owned0.2.26 check: visible warning/active footprint and release/decay at high/low FPS and both clients; no latency or visual correctness claim from a build. Existing local English localization changes are preserved, not rewritten by this pass.

Weapon-branch checks and main integration are recorded in [weapon evidence](evidence/2026-09-08-ritual-armaments.json). The branch fast-forwarded without conflicts. The pinned native Mod compiler built and packaged0.2.25 after correcting Rogue damage-class lookup (zero errors; four existing nullable-context warnings). Existing pure-domain/codec evidence is retained. In-game Rogue behavior, weapon visuals/DPS, conversions and multiplayer acceptance remain user-owned and untested by this build.

The following is retained Boss-build evidence, not a weapon-build claim:

[Current check record](evidence/2026-09-07-iron-interdict-checks.json) owns scoped automated results and pending runtime checks. The user accepted0.2.23 visuals; its latest solo run reached Final and failed the fifth Stack, with no combat warnings/errors. Its shared Spread launch and shell surface are retained. [0.2.23 checks](evidence/2026-09-07-shared-spread-shell-checks.json), [0.2.22 checks](evidence/2026-09-07-mechanic-intensity-checks.json), [0.2.21 checks](evidence/2026-09-07-mechanic-presentation-checks.json) and [consolidation](evidence/2026-09-07-development-consolidation.json) retain preceding evidence/history.

**Not run / user-owned:** matching0.2.24 client/server load, sword gap/forecast/insertion readability, native material-hit sounds, wider four-player Spread pockets, intensified local flash and Final density. Three-player mixed failures/revival remain unobserved by the latest solo run. No GUI, game or server was launched for this change. Compilation is not audiovisual, performance, latency or multiplayer proof. Previous checks not explicitly observed remain pending in their evidence record; no blanket retest is required. The manual self-hosted Mod-build CI runner remains unprovisioned.

## Constraints and deferred work

- General Terraria/Calamity lethal-hit interception, robust rejoin/observer identity, outsider admission/ejection and adversarial movement handling are not production-complete.
- Current Raid-owned damage/recovery and participant containment are enabled experiments, not blocked by the deferred production adapters.
- A final production loot table, balance/art/audio acceptance, release packaging and standalone progression replacing the hard Calamity dependency remain deferred. Null Refrain is a development reward, not final progression.
- One-member start must be compiled off before public release. Full release/compatibility gates are separate from normal development checks.

## Next change

User-owned0.2.43: reload matching peers, compare BGM/SFX at unchanged sliders, listen through music seams, and inspect the hanging iris throughout deployment. Toggle the compact Ready button at107% UI scale. Keep the outstanding distant3-player preparation check below; solo logs do not establish it.

User-owned 0.2.42: reload/restart matching peers, enter each Boss phase and listen through A -> B -> C. Confirm the B downbeat no longer reads as a master fade, Phase I remains restrained, Phase II/III gain urgency through acoustic articulation rather than retro/electronic timbre, and Final reaches its ending before the encounter resolves. Also listen across each ordinary loop seam. In the same run, listen to the rebuilt radiant/beam, dimensional/crush, Stack/shell and three sustained-weapon voices; confirm no cheap clipping texture, missing cue, loop dip or BGM masking.

User-owned0.2.41: restart/reload every peer together, then Host & Play. With3players, leave one well beyond the old80-tile radius and activate the Core. Confirm all3arrive inside the same field, first cinematic/black exterior,3-person Ready denominator, overhead Ready/unready and second cinematic only after all agree. Keep one client at107% UI scale. During a separate preparation, join/leave or cancel and verify no field/input capability remains. No GUI/server launched.

User-owned0.2.40: reload/restart all peers together (protocol27), then Host & Play; no second Build + Reload for the packaged source. Listen for Stack pass/fail, both sword waves, the fatal crush (including immediate Defeat) and both accelerating blade turns. Check first-score HP lock, then mid-action80%/40% transitions on a slower-DPS/repeated cycle. If a cue is still missing, its `AudioCue`/`AudioVoice` lines identify scheduling/focus/device gain. No GUI/server launched.

User-owned0.2.39: reload all peers together, no Build + Reload needed. Listen for a single clean claw swing, non-melee assembly/lock/fire/sustain contrast and audible Stack births/results. Confirm3–4pursuits only in standalone Spread, none during lattice/flood combinations. Preserve the accepted visual settings; compare Reduced Effects audio if used.

User-owned0.2.38: reload Mods/restart (no Build + Reload needed). Select vanilla boss bar, start the Raid and inspect it while shielded/exposed and in Final. Swing claws into empty space and an enemy: light/hand remain, radial lines and dark trail mass should be absent; compare Reduced Effects. No GUI/server was launched.

User-owned0.2.37: reload/restart all peers before Host & Play (protocol25); the package is already built, no second Build + Reload needed. Check three forecast arrivals and reading pause before ordered Final shots; pursuit during standalone/embedded Spread and its quiet resolution margin; heavy sword/Stack/crush sound balance. No GUI/server was launched. Previous load issue is confirmed resolved by the0.2.36 run; outstanding unrelated checks below are historical handoff items, not mandatory repeats for this change.

User-owned0.2.35: reload/restart **all** peers before Host & Play (protocol changed; no second Build + Reload required). With standard stored Adrenaline, take a Raid beam hit and check the gauge clears; confirm zero-damage successful mechanics do not clear it. Check a Down-causing/final Defeat hit, active burst and Draedon's Heart separately. For visuals, inspect pursuit launch, lattice source, P3 sword tips/hands, Pylons and eclosion with normal/reduced effects; retained107% viewport and weapon checks below remain outstanding, not repeated automated gates.

User-owned0.2.34: restart/reload matching peers; hold Magic for about7 seconds to see its continuous final beam, and Ranged for about6 seconds for overdrive. Allow the Summon concert to complete; check Rogue enrollment/final blade and release cancellation. Compare Reduced Effects and inspect remaining107% cinematic alignment without changing the accepted melee. No additional Build + Reload is needed for the already-packaged source.

User-owned0.2.32: confirm mask/outline agreement and full outside coverage at107% UI (plus100% comparison), without changing other presentation or gameplay settings. No additional combat/codec gate is required for this client-only correction.

User-owned0.2.31: while charged, hold left click and tap right; verify one execution/canceled swipe on both clients. Check Final's progressive breakup and Victory suction/flash/shake, Phase-III forecast readability and background movement; compare Reduced Effects/shake-off. Do not repeat unchanged whole-release gates for this presentation pass.

User/Codex-owned0.2.30: build/load matching peers; inspect repeated left/right claw cycles (including left-facing and high attack speed), battery muzzle alignment, archive fourth-cast accent, choir assembly/retarget/despawn, and the stealth verdict's track/lock/strike. Compare matched class-endgame damage only after loading; no Boss HP adjustment implied.

User/Codex-owned0.2.29: reload the claw branch; confirm giant alternating hands, normal true-melee bonuses, single six-second charge use, clicked-location compression, both clients' silhouettes and the new full-color icon. No FPS/log-collection system or unrelated Boss retuning is added.

User/Codex-owned0.2.25: build/load the weapon branch, inspect the five silhouettes/continuous motion, native Rogue stealth and minion behavior, then tune the central damage seeds against matched Calamity2.2.4 equipment. Target roughly1.10x a selected class-endgame benchmark, not dominance over every weapon/target. Keep the remaining Boss-only check below separate.

User-owned0.2.24 Host & Play: confirm Iron Interdict's sparse half remains readable/dodgeable, Final spacing, Spread radius/flash and the distinct damage sounds. All peers must update together. No further HP adjustment implied. The [implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md) owns production backlog.

## History

[Preserved pre-consolidation record](history/2026-09-07-pre-consolidation.md) contains the preceding build narrative, original slice plan, old Windows handoff and superseded encounter text, with their original evidence links. Historical “inert” and activation-denied instructions are not current gates.
