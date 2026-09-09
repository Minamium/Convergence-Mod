---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-09
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

Development **0.2.30**, protocol **23**. The remaining four ritual weapon forms now deploy a three-cannon battery, folio/lens archive, assembling choir organ and triangular stealth verdict. Claw artwork/attack balance are retained; its idle/active hand seams and trail rendering are revised for continuity. [Weapon specification](encounters/first-severance/WEAPONS.md#apparatus-redesign--0230) owns controls, timelines and power budgets. Boss behavior and reward exchanges remain unchanged.

Retained Boss baseline: Final random triples have wider gaps; Phase-III sword wave two shifts the sparse lanes so standing in the first gap is no longer safe. Other timing/damage and the five ritual armaments are retained. The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#random-final-triples) owns tuning. Public-development descriptions and icons are prepared, not published or release-approved; see [publication preparation](RELEASE_PROCESS.md#development-publication-preparation).

- First Severance is playable: Core placement/validation, Ready, server-owned combat, participant containment/infinite flight, fixed-site Stack and player-centered Spread.
- Ordered phases: sealed opening and clockwise relay; lattice/twin blades; distant arms/floods/crush; HP-zero Final survival. Required first scores gate phase transitions and Victory.
- Instant reusable ally revival, no item/token cost, untimed Down (no active Eliminated), recipient lockout and immediate all-Down Defeat. Ordinary Terraria lethal-hook integration remains separate.
- Client-only original Boss/materials, continuous beam forecasts, phase music, SFX and start/transition/result cinematics. Presentation and balance remain developmental.
- Build-gated one-member start is available for explicitly requested solo debugging; ordinary workflow remains user GUI plus a friend.

The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md), [recovery spec](encounters/first-severance/REVIVE_SPEC.md), [visual spec](encounters/first-severance/VISUAL_SPEC.md) and [audio cues](AUDIO_CUE_SHEET.md) own behavior. The [version matrix](VERSION_MATRIX.md) owns the runtime baseline.

## Verification state

[Apparatus v2 evidence](evidence/2026-09-09-ritual-armaments-v2.json) records the feature branch's selected checks and limits. The user accepted the0.2.29 claw design and reported mild animation stutter; the new continuity pass targets identified presentation seams. Native Calamity package compilation and audiovisual/multiplayer/DPS acceptance of0.2.30 remain user/Codex-owned until observed. No GUI session or FPS/log subsystem is added.

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

User/Codex-owned0.2.30: build/load matching peers; inspect repeated left/right claw cycles (including left-facing and high attack speed), battery muzzle alignment, archive fourth-cast accent, choir assembly/retarget/despawn, and the stealth verdict's track/lock/strike. Compare matched class-endgame damage only after loading; no Boss HP adjustment implied.

User/Codex-owned0.2.29: reload the claw branch; confirm giant alternating hands, normal true-melee bonuses, single six-second charge use, clicked-location compression, both clients' silhouettes and the new full-color icon. No FPS/log-collection system or unrelated Boss retuning is added.

User/Codex-owned0.2.25: build/load the weapon branch, inspect the five silhouettes/continuous motion, native Rogue stealth and minion behavior, then tune the central damage seeds against matched Calamity2.2.4 equipment. Target roughly1.10x a selected class-endgame benchmark, not dominance over every weapon/target. Keep the remaining Boss-only check below separate.

User-owned0.2.24 Host & Play: confirm Iron Interdict's sparse half remains readable/dodgeable, Final spacing, Spread radius/flash and the distinct damage sounds. All peers must update together. No further HP adjustment implied. The [implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md) owns production backlog.

## History

[Preserved pre-consolidation record](history/2026-09-07-pre-consolidation.md) contains the preceding build narrative, original slice plan, old Windows handoff and superseded encounter text, with their original evidence links. Historical “inert” and activation-denied instructions are not current gates.
