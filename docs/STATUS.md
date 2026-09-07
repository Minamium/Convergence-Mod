---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-07
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

Development **0.2.22**, protocol **20**. Stack's accepted shell fragments gain irregular vibration and thin friction lightning; failed Spread gains a brief white muzzle cross and focused high execution ping. The accepted beam materials, BGM mix, success/failure outcomes and previous Boss-linked emission remain. Only presentation/audio changes: HP, damage, target selection, deadlines and phase schedules are unchanged. Version declarations live in [build.txt](../build.txt) and [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs); exact mechanics/tuning belong to the linked specifications/code.

- First Severance is playable: Core placement/validation, Ready, server-owned combat, participant containment/infinite flight, fixed-site Stack and player-centered Spread.
- Ordered phases: sealed opening and clockwise relay; lattice/twin blades; distant arms/floods/crush; HP-zero Final survival. Required first scores gate phase transitions and Victory.
- Instant reusable ally revival, no item/token cost, untimed Down (no active Eliminated), recipient lockout and immediate all-Down Defeat. Ordinary Terraria lethal-hook integration remains separate.
- Client-only original Boss/materials, continuous beam forecasts, phase music, SFX and start/transition/result cinematics. Presentation and balance remain developmental.
- Build-gated one-member start is available for explicitly requested solo debugging; ordinary workflow remains user GUI plus a friend.

The [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md), [recovery spec](encounters/first-severance/REVIVE_SPEC.md), [visual spec](encounters/first-severance/VISUAL_SPEC.md) and [audio cues](AUDIO_CUE_SHEET.md) own behavior. The [version matrix](VERSION_MATRIX.md) owns the runtime baseline.

## Verification state

[Current check record](evidence/2026-09-07-mechanic-intensity-checks.json) owns scoped automated results and pending visual/audio acceptance. The user broadly accepted0.2.21 and specifically its shell fragments, requesting more intensity; its latest solo run reached Phase III, then failed a Stack. [0.2.21 checks](evidence/2026-09-07-mechanic-presentation-checks.json), [0.2.20 checks](evidence/2026-09-07-beam-readability-checks.json), [0.2.19 checks](evidence/2026-09-07-sanctuaries-reward-checks.json), [preceding playtests](evidence/2026-09-07-pre-0219-playtests.json) and [consolidation](evidence/2026-09-07-development-consolidation.json) remain historical evidence.

**Not run / user-owned:** matching0.2.22 load, shell vibration/lightning readability and reduced-effects behavior, failed Spread cross/ping and live mix. Three-player partial failures remain unobserved; solo results do not substitute. No GUI, game or server was launched for this change. Compilation is not audiovisual, performance, latency or multiplayer proof. Previous checks not explicitly observed remain pending in their evidence record; no blanket retest is required. The manual self-hosted Mod-build CI runner remains unprovisioned.

## Constraints and deferred work

- General Terraria/Calamity lethal-hit interception, robust rejoin/observer identity, outsider admission/ejection and adversarial movement handling are not production-complete.
- Current Raid-owned damage/recovery and participant containment are enabled experiments, not blocked by the deferred production adapters.
- A final production loot table, balance/art/audio acceptance, release packaging and standalone progression replacing the hard Calamity dependency remain deferred. Null Refrain is a development reward, not final progression.
- One-member start must be compiled off before public release. Full release/compatibility gates are separate from normal development checks.

## Next change

User-owned0.2.22 Host & Play: compare the stronger Stack anticipation and failed Spread release against the accepted shell surfaces and quiet successful release. Discuss HP separately from distinct build/roster playtests; no implicit retuning. The [implementation plan](encounters/first-severance/IMPLEMENTATION_PLAN.md) owns production backlog.

## History

[Preserved pre-consolidation record](history/2026-09-07-pre-consolidation.md) contains the preceding build narrative, original slice plan, old Windows handoff and superseded encounter text, with their original evidence links. Historical “inert” and activation-denied instructions are not current gates.
